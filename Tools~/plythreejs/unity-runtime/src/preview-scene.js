import { readFile, stat } from 'node:fs/promises';
import { createHash } from 'node:crypto';
import { dirname, extname, isAbsolute, relative, resolve, sep } from 'node:path';
import { pathToFileURL } from 'node:url';
import { GLTFLoader } from 'three/addons/loaders/GLTFLoader.js';
import { BUNDLE_FORMAT, BUNDLE_VERSION, validateManifest } from './asset-schema.js';
import { legacyBindingsFromAxes } from './input-settings.js';
import { AssetBundleLoader, AssetDatabase, Component, CSharpDictionary, Font, JObject, JToken, Object as UnityObject, PlygroundItem, Quaternion, ScriptableObject, THREE, TerrainData, Texture2D, TextureFormat, UnityRuntime, UnityType, Vector3 } from './index.js';
import { runPreviewImport, serializePreviewObject } from './preview-importer.js';
import { orientUnityNormalMaps } from './preview-model-materials.js';
import { decodePreviewPng } from './preview-png.js';

function member(root, name) {
  if (typeof name !== 'string' || !name || isAbsolute(name) || /^[A-Za-z]:/.test(name)
    || name.replaceAll('\\', '/').split('/').some(part => !part || part === '.' || part === '..')) {
    throw new Error(`Invalid preview project path: ${name}`);
  }
  const file = resolve(root, name);
  const rel = relative(root, file);
  if (rel === '..' || rel.startsWith(`..${sep}`) || isAbsolute(rel)) throw new Error(`Preview path escapes project: ${name}`);
  return file;
}

async function json(file) { return JSON.parse((await readFile(file, 'utf8')).replace(/^\uFEFF/, '')); }

// The materializer needs mesh geometry and Unity material metadata, but does
// not render. Avoid browser image loaders when parsing a GLB under Node.
export function headlessModelBytes(bytes) {
  if (bytes.toString('ascii', 0, 4) !== 'glTF') return bytes;
  if (bytes.toString('ascii', 16, 20) !== 'JSON') throw new Error('Invalid Preview GLB');
  const jsonLength = bytes.readUInt32LE(12);
  const gltf = JSON.parse(bytes.toString('utf8', 20, 20 + jsonLength));
  for (const material of gltf.materials ?? []) {
    if (material.pbrMetallicRoughness) {
      delete material.pbrMetallicRoughness.baseColorTexture;
      delete material.pbrMetallicRoughness.metallicRoughnessTexture;
    }
    delete material.normalTexture;
    delete material.occlusionTexture;
    delete material.emissiveTexture;
  }
  delete gltf.images;
  delete gltf.textures;
  delete gltf.samplers;
  const jsonBytes = Buffer.from(JSON.stringify(gltf));
  const paddedLength = (jsonBytes.length + 3) & ~3;
  const binaryStart = 20 + jsonLength;
  const result = Buffer.alloc(20 + paddedLength + bytes.length - binaryStart, 0x20);
  bytes.copy(result, 0, 0, 20);
  result.writeUInt32LE(result.length, 8);
  result.writeUInt32LE(paddedLength, 12);
  jsonBytes.copy(result, 20);
  bytes.copy(result, 20 + paddedLength, binaryStart);
  return result;
}

export async function headlessPreviewTexture(asset, bundleDirectory, runtime) {
  if (!asset.textureReadable) {
    const texture = new THREE.Texture();
    texture.name = asset.name ?? '';
    return texture;
  }
  const { width, height, rgba } = decodePreviewPng(await readFile(member(bundleDirectory, asset.url)));
  if (asset.textureWidth !== width || asset.textureHeight !== height)
    throw new Error(`Preview texture dimensions disagree with its metadata: ${asset.id}`);
  const texture = new Texture2D(width, height, TextureFormat.RGBA32,
    asset.textureMipCount ?? 1, asset.textureIsDataSRGB === false);
  for (let y = 0; y < height; y++)
    texture._bytes.set(rgba.subarray(y * width * 4, (y + 1) * width * 4),
      (height - 1 - y) * width * 4);
  texture.Apply(true);
  texture._previewRuntime = runtime;
  if (asset.textureFilterMode !== undefined) texture.filterMode = asset.textureFilterMode;
  if (asset.textureWrapModeU !== undefined) texture.wrapModeU = asset.textureWrapModeU;
  if (asset.textureWrapModeV !== undefined) texture.wrapModeV = asset.textureWrapModeV;
  if (asset.textureAnisoLevel !== undefined) texture.anisoLevel = asset.textureAnisoLevel;
  texture.name = asset.name ?? '';
  return texture;
}

function vector(node, keys, fallback) {
  return keys.map((key, index) => {
    const value = node[key] ?? fallback[index];
    const number = typeof value === 'number' ? value : Number(value);
    if (!Number.isFinite(number)) throw new Error(`Invalid Bob transform ${key}`);
    return number;
  });
}

export async function importPreparedModule(file, root, cache = new Map(), active = new Set()) {
  if (cache.has(file)) return cache.get(file);
  if (active.has(file)) throw new Error(`Cyclic Preview module import: ${file}`);
  active.add(file);
  let source = await readFile(file, 'utf8');
  // Older immutable module previews were transpiled with strict null comparisons
  // after optional access. JavaScript returns undefined for a missing receiver,
  // while the original C# expression evaluates to null.
  source = source.replace(/(\b[\w$]+(?:\?\.[\w$]+)+)\s*(!==|===)\s*null\b/g,
    (_, value, operator) => `${value} ${operator === '!==' ? '!=' : '=='} null`);
  // Older SDK transpilation omitted the CharacterDescriptor type argument, so
  // ToObject returned the raw build object with lower-case data and no Data.
  if (file.endsWith('system.mjs') && source.includes('class BaseCharacterModule'))
    source = source.replace(/(GetDescriptor\(data\)\s*\{[\s\S]*?let result = )data\?\.ToObject\(\);/,
      '$1data?.ToObject("CharacterDescriptor");');
  // Only match import declarations and generated await-import expressions.
  // A bare `from "..."` can also occur inside transpiled gameplay strings.
  const specifiers = [
    ...source.matchAll(/^\s*import\s*[^'"\n;]+?\s*from\s*(['"])([^'"\n]+)\1/gm),
    ...source.matchAll(/^\s*(?:const|let|var)\s+[\w$]+\s*=\s*await\s+import\s*\(\s*(['"])([^'"\n]+)\1\s*\)/gm),
  ];
  for (const match of specifiers) {
    const specifier = match[2];
    let url;
    if (specifier === '@plythreejs/runtime') url = new URL('./index.js', import.meta.url).href;
    else if (specifier.startsWith('./') || specifier.startsWith('../'))
      url = await importPreparedModule(member(root, relative(root, resolve(dirname(file), specifier))
        .replaceAll('\\', '/')), root, cache, active);
    else throw new Error(`Unsupported Preview module import: ${specifier}`);
    source = source.replaceAll(`${match[1]}${specifier}${match[1]}`, JSON.stringify(url));
  }
  const url = `data:text/javascript;base64,${Buffer.from(source).toString('base64')}`;
  cache.set(file, url);
  active.delete(file);
  return url;
}

async function loadEditorController(file, root, typeName) {
  const imported = await import(await importPreparedModule(file, root));
  const constructor = imported.unityTypes?.[typeName] ?? imported[typeName.split('.').at(-1)];
  if (typeof constructor !== 'function') throw new Error(`Preview controller type is missing: ${typeName}`);
  return new constructor();
}

function applyPrefabModels(object, node) {
  if (node.model) object.previewModel = node.model;
  if (object.children.length !== (node.children ?? []).length)
    throw new Error(`Preview prefab hierarchy changed during import: ${node.name}`);
  object.children.forEach((child, index) => applyPrefabModels(child, node.children[index]));
}

async function loadModulePrefabs(moduleBundle, codeFile, moduleRoot, bundleDirectory) {
  const code = await import(await importPreparedModule(codeFile, moduleRoot));
  const unityTypes = { 'Plyground.Pipeline.Runtime.PlygroundItem': PlygroundItem, ...(code.unityTypes ?? {}) };
  // Asset diagnostics alone must not remove transpiled gameplay components.
  if (moduleBundle.allowRuntimeApproximation) {
    const footstep = unityTypes['JUTPS.FX.JUFootstep'];
    if (footstep && !footstep.prototype.LoadDefaultFootstepInInspector) {
      footstep.prototype.LoadDefaultFootstepInInspector = function () {
        this.FootstepAudioClips ??= [];
      };
      moduleBundle.previewDiagnostics.push('JU default footstep audio setup omitted in exploratory Preview');
    }
  }
  const typeNames = new Map(Object.entries(unityTypes).map(([name, type]) => [type, name]));
  const components = Object.fromEntries(Object.entries(unityTypes)
    .filter(([, type]) => typeof type === 'function' && type.prototype instanceof Component));
  const scriptableObjects = Object.fromEntries(Object.entries(unityTypes)
    .filter(([, type]) => typeof type === 'function' && type.prototype instanceof ScriptableObject));
  const runtime = new UnityRuntime({ editorMode: true });
  const loader = new AssetBundleLoader(runtime, {
    components, scriptableObjects, types: unityTypes,
    assetResolver: async asset => {
      if (asset.kind === 'model') {
        globalThis.self ??= globalThis;
        const bytes = headlessModelBytes(await readFile(member(bundleDirectory, asset.url)));
        return new Promise((resolve, reject) => new GLTFLoader().parse(
          bytes.buffer.slice(bytes.byteOffset, bytes.byteOffset + bytes.byteLength), '',
          model => { orientUnityNormalMaps(model.scene ?? model); resolve(model); }, reject));
      }
      if (asset.kind === 'texture') return headlessPreviewTexture(asset, bundleDirectory, runtime);
      if (asset.kind === 'audio') return new ArrayBuffer(0);
      if (asset.kind === 'uiFont') {
        const font = new Font();
        font.name = asset.name ?? '';
        font.url = member(bundleDirectory, asset.url);
        return font;
      }
      if (asset.kind === 'navMesh') return new Uint8Array();
      if (asset.kind === 'terrainData') {
        const record = await json(member(bundleDirectory, asset.url));
        return new TerrainData(record);
      }
      throw new Error(`Unsupported Preview asset kind: ${asset.kind}`);
    }
  });
  if (moduleBundle.allowRuntimeApproximation) {
    const beforeAssets = moduleBundle.assets.length;
    moduleBundle.assets = moduleBundle.assets.filter(asset => asset.kind !== 'scriptableObject'
      || loader.scriptableObjects.has(asset.type));
    if (beforeAssets !== moduleBundle.assets.length)
      moduleBundle.previewDiagnostics.push(`Skipped ${beforeAssets - moduleBundle.assets.length} unavailable ScriptableObjects`);
    const dropUnsupported = node => {
      const before = node.components?.length ?? 0;
      node.components = (node.components ?? []).filter(record => loader.components.has(record.type));
      if (before !== node.components.length)
        moduleBundle.previewDiagnostics.push(`Skipped ${before - node.components.length} unavailable components on ${node.name}`);
      node.children?.forEach(dropUnsupported);
    };
    for (const asset of moduleBundle.assets)
      if (asset.kind === 'prefab') dropUnsupported(asset.root);
    const retainedIds = new Set(moduleBundle.assets.map(asset => asset.id));
    const collectIds = node => {
      retainedIds.add(node.id);
      if (node.transform?.id) retainedIds.add(node.transform.id);
      for (const component of node.components ?? []) retainedIds.add(component.id);
      node.children?.forEach(collectIds);
    };
    for (const asset of moduleBundle.assets)
      if (asset.kind === 'prefab') collectIds(asset.root);
    moduleBundle.scene?.roots?.forEach(collectIds);
    let missingReferences = 0;
    const clearMissingReferences = (value, parent, key) => {
      if (!value || typeof value !== 'object') return;
      if (typeof value.$ref === 'string' && !retainedIds.has(value.$ref)) {
        parent[key] = null;
        missingReferences++;
        return;
      }
      if (typeof value.$asset === 'string' && !retainedIds.has(value.$asset)) {
        parent[key] = null;
        missingReferences++;
        return;
      }
      for (const [childKey, child] of Object.entries(value)) clearMissingReferences(child, value, childKey);
      if (Array.isArray(value.$unityEvent)) {
        const count = value.$unityEvent.length;
        value.$unityEvent = value.$unityEvent.filter(call => call.target != null);
        missingReferences += count - value.$unityEvent.length;
      }
    };
    for (const asset of moduleBundle.assets) clearMissingReferences(asset, moduleBundle.assets,
      moduleBundle.assets.indexOf(asset));
    for (const mapping of [moduleBundle.resources, moduleBundle.assetPaths])
      for (const [path, id] of Object.entries(mapping ?? {})) if (!retainedIds.has(id)) delete mapping[path];
    if (missingReferences)
      moduleBundle.previewDiagnostics.push(`Cleared ${missingReferences} references to unavailable exploratory components`);
  }
  const loaded = await loader.load(moduleBundle);
  for (const record of moduleBundle.assets.filter(asset => asset.kind === 'prefab')) {
    const prefab = loaded.assets.get(record.id);
    if (prefab) applyPrefabModels(prefab, record.root);
  }
  const prefabs = new Map();
  for (const [templateId, prefabId] of Object.entries(moduleBundle.itemPrefabs ?? {})) {
    const prefab = loaded.assets.get(prefabId);
    const record = moduleBundle.assets.find(asset => asset.id === prefabId && asset.kind === 'prefab');
    if (!prefab || !record) throw new Error(`Preview prefab is missing for template ${templateId}: ${prefabId}`);
    applyPrefabModels(prefab, record.root);
    prefabs.set(templateId, { prefab, root: record.root });
  }
  return { prefabs, typeNames, runtimeTypes: unityTypes, assets: loaded.assets };
}

function findBuildItem(build, id) {
  if (!id) return null;
  const pending = [build];
  while (pending.length) {
    const value = pending.pop();
    if (Array.isArray(value)) { pending.push(...value); continue; }
    if (!value || typeof value !== 'object') continue;
    if (value.id === id || value.buildId === id) return value;
    pending.push(...Object.values(value));
  }
  return null;
}

function itemTransform(item) {
  const vector = (value, length, fallback) => {
    if (value == null) return fallback;
    if (!Array.isArray(value)) value = length === 3
      ? [value.x, value.y, value.z] : [value.x, value.y, value.z, value.w];
    if (value.length !== length || value.some(number => !Number.isFinite(number)))
      throw new Error(`Invalid Preview item transform for ${item.id ?? item.Id}`);
    return value;
  };
  return {
    position: vector(item.position ?? item.Position, 3, [0, 0, 0]),
    rotation: vector(item.rotation ?? item.Rotation, 4, [0, 0, 0, 1]),
    scale: vector(item.scale ?? item.Scale, 3, [1, 1, 1])
  };
}

function applyItemTransform(object, item) {
  const transform = itemTransform(item);
  object.transform.localPosition = new Vector3(...transform.position);
  object.transform.localRotation = new Quaternion(...transform.rotation);
  object.transform.localScale = new Vector3(...transform.scale);
}

/** Convert the canonical Bob hierarchy and execute the transpiled module controllers. */
export async function assemblePreviewScene(projectRoot, project) {
  if (project?.format !== 'plyground.preview.project/v1') throw new Error('Unsupported preview project format');
  UnityType.Clear();
  const bob = await json(member(projectRoot, project.inputs.environment.scene));
  if (!Array.isArray(bob.root)) throw new Error('Bob scene needs a root array');
  const build = await json(member(projectRoot, project.inputs.build));
  const item = await json(member(projectRoot, project.inputs.item));
  const allItems = item.items ?? item.gameItems ?? item.GameItems ?? [];
  if (!Array.isArray(allItems)) throw new Error('Preview item file needs an items array');
  item.GameItems = allItems;
  for (const gameItem of allItems) {
    gameItem.Id ??= gameItem.id;
    gameItem.Name ??= gameItem.name;
    gameItem.ModuleId ??= gameItem.moduleId;
    gameItem.TemplateId ??= gameItem.templateId;
    gameItem.BuildId ??= gameItem.buildId;
    gameItem.RuntimeAsset ??= gameItem.runtimeAsset;
    const transform = itemTransform(gameItem);
    gameItem.Position = new Vector3(...transform.position);
    gameItem.Rotation = new Quaternion(...transform.rotation);
    gameItem.Scale = new Vector3(...transform.scale);
    gameItem.Values = gameItem.Values instanceof CSharpDictionary ? gameItem.Values
      : new CSharpDictionary(Object.entries(gameItem.Values ?? gameItem.values ?? {}));
    gameItem.GetPropertyValue = (key, output) => {
      const exists = gameItem.Values.has(key);
      if (output) output.value = exists ? gameItem.Values.get(key) : null;
      return exists;
    };
  }
  for (const gameItem of allItems) {
    if (gameItem.BuildValues != null) gameItem.BuildValues = JToken.As(gameItem.BuildValues, JObject);
    if (gameItem.buildValues != null) gameItem.buildValues = JToken.As(gameItem.buildValues, JObject);
  }
  const environmentRoot = member(projectRoot, project.inputs.environment.root);
  const environmentAssets = new Map();
  const assets = [];
  const roots = [];
  const usedIds = new Set();
  const uniqueId = value => {
    if (usedIds.has(value)) throw new Error(`Duplicate preview scene id: ${value}`);
    usedIds.add(value);
    return value;
  };
  const environmentNode = async (node, indexPath) => {
    if (!node || typeof node !== 'object' || typeof node.mesh !== 'string' || !node.mesh)
      throw new Error(`Invalid Bob node at ${indexPath}`);
    const mesh = node.mesh.replaceAll('\\', '/');
    const meshKey = mesh.toLowerCase();
    let model = environmentAssets.get(meshKey);
    if (!model) {
      const candidates = extname(mesh) ? [mesh] : [`${mesh}.glb`, `${mesh}.fbx`];
      let relativeMesh;
      for (const candidate of candidates) {
        const file = member(environmentRoot, candidate);
        if ((await stat(file).catch(() => null))?.isFile()) { relativeMesh = candidate; break; }
      }
      // Bob also records named nodes already embedded in their parent's GLB.
      // They have no standalone geometry file and must not be loaded twice.
      if (!relativeMesh && indexPath.includes('/') && !extname(mesh)) return null;
      if (!relativeMesh) throw new Error(`Bob mesh is missing: ${mesh}`);
      // Sibling nodes are prepared concurrently. Recheck after the file lookup
      // before allocating an ID so repeated meshes share one asset.
      model = environmentAssets.get(meshKey);
      if (!model) {
        model = `bob:${environmentAssets.size}`;
        environmentAssets.set(meshKey, model);
        assets.push({ id: model, kind: 'model',
          url: `${project.inputs.environment.root.replaceAll('\\', '/')}/${relativeMesh.replaceAll('\\', '/')}` });
      }
    }
    // Bob ids identify source nodes and may repeat across instances. The
    // hierarchy path makes each materialized scene object stable and unique.
    const id = uniqueId(`bob:${node.id ?? node.config_id ?? 'node'}:${indexPath}`);
    const semanticSurface = node.attrs?.BigGame?.surface;
    const collider = node.collider;
    let components = [];
    if (collider != null) {
      if (collider.type !== 'box' || !Array.isArray(collider.size) || collider.size.length !== 3
        || collider.size.some(value => !Number.isFinite(value) || value <= 0)
        || !Array.isArray(collider.center) || collider.center.length !== 3
        || collider.center.some(value => !Number.isFinite(value)))
        throw new Error(`Invalid Bob box collider at ${indexPath}`);
      components = [{ id: uniqueId(`${id}:collider`), type: 'BoxCollider', properties: {
        size: { $vector3: collider.size }, center: { $vector3: collider.center },
        isTrigger: false } }];
    } else {
      components = [{ id: uniqueId(`${id}:collider`), type: 'MeshCollider', properties: {
        isTrigger: false } }];
    }
    return {
      id, name: mesh, active: true, tag: 'Untagged', layer: 0,
      transform: { id: uniqueId(`${id}:transform`),
        position: vector(node, ['tx', 'ty', 'tz'], [0, 0, 0]),
        rotation: vector(node, ['rx', 'ry', 'rz', 'rw'], [0, 0, 0, 1]),
        scale: vector(node, ['sx', 'sy', 'sz'], [1, 1, 1]) },
      model, mirrorX: true, components,
      ...(typeof semanticSurface === 'string' && semanticSurface.length > 0
        ? { semanticSurface } : {}),
      children: (await Promise.all((node.children ?? []).map((child, index) =>
        environmentNode(child, `${indexPath}/${index}`)))).filter(Boolean)
    };
  };
  for (let index = 0; index < bob.root.length; index++) roots.push(await environmentNode(bob.root[index], `${index}`));

  const modules = [];
  const diagnostics = [];
  const layers = {};
  const renderingLayers = {};
  let qualitySettings = null;
  let renderSettings = null;
  let moduleInputBindings = null;
  let buildTags = null;
  const editorModules = [];
  const componentTypeNames = new Map();
  const editorAssets = new Map();
  const editorResources = new Map();
  const sceneResources = new Map();
  const sceneAssetBundles = new Map();
  const sceneDescriptors = [];
  const legacyBuilders = [];
  const moduleIds = new Set();
  let systemEditorRevision = null;
  for (const record of project.modules ?? []) {
    const manifestFile = member(projectRoot, record.manifest);
    const manifest = await json(manifestFile);
    if (manifest.format !== 'plyground.preview.module/v1' || manifest.moduleId !== record.moduleId
      || manifest.revision !== record.revision) throw new Error(`Preview module manifest mismatch: ${record.moduleId}`);
    if (moduleIds.has(manifest.moduleId)) throw new Error(`Duplicate preview module: ${manifest.moduleId}`);
    moduleIds.add(manifest.moduleId);
    if (manifest.runtime?.id !== project.runtime?.id || manifest.runtime?.apiVersion !== project.runtime?.apiVersion)
      throw new Error(`Incompatible preview module runtime: ${manifest.moduleId}`);
    if (!manifest.editor?.entry && !manifest.builder?.entry)
      throw new Error(`Module ${manifest.moduleId} needs a transpiled editor controller`);
    const moduleRoot = resolve(dirname(manifestFile), '..');
    const moduleBundleFile = member(moduleRoot, manifest.assets.bundle);
    const moduleBundle = validateManifest(await json(moduleBundleFile), BUNDLE_FORMAT);
    if (moduleBundle.renderSettings
      && (!renderSettings || record.moduleId === build.selectedGame))
      renderSettings = moduleBundle.renderSettings;
    const exportedInput = moduleBundle.legacyAxes
      ? legacyBindingsFromAxes(moduleBundle.legacyAxes) : moduleBundle.inputBindings;
    if (exportedInput) {
      for (const message of exportedInput.diagnostics ?? []) diagnostics.push({
        severity: 'warning', phase: 'input', message: `${record.moduleId}: ${message}` });
      const bindings = { buttons: exportedInput.buttons, axes: exportedInput.axes };
      if (!moduleInputBindings || record.moduleId === build.selectedGame)
        moduleInputBindings = bindings;
      else if (JSON.stringify(moduleInputBindings) !== JSON.stringify(bindings))
        diagnostics.push({ severity: 'warning', phase: 'input',
          message: `${record.moduleId} has different input bindings; using the selected game bindings` });
    }
    if (moduleBundle.qualitySettings) {
      if (!qualitySettings || record.moduleId === build.selectedGame)
        qualitySettings = moduleBundle.qualitySettings;
      else if (JSON.stringify(qualitySettings) !== JSON.stringify(moduleBundle.qualitySettings))
        diagnostics.push({ severity: 'warning', phase: 'quality',
          message: `${record.moduleId} has different quality settings; using the selected game settings` });
    }
    if (moduleBundle.buildTags) {
      if (buildTags && JSON.stringify(buildTags) !== JSON.stringify(moduleBundle.buildTags))
        throw new Error('Preview modules disagree on Unity build tags');
      buildTags = moduleBundle.buildTags;
    }
    for (const [name, index] of Object.entries(moduleBundle.layers ?? {})) {
      if (layers[name] !== undefined && layers[name] !== index)
        throw new Error(`Preview modules disagree on Unity layer ${name}`);
      layers[name] = index;
    }
    for (const [name, index] of Object.entries(moduleBundle.renderingLayers ?? {})) {
      if (renderingLayers[name] !== undefined && renderingLayers[name] !== index
        || Object.entries(renderingLayers).some(([existing, value]) =>
          existing !== name && value === index))
        throw new Error(`Preview modules disagree on Unity rendering layer ${name}`);
      renderingLayers[name] = index;
    }
    const bundleDirectory = dirname(moduleBundleFile);
    for (const scene of moduleBundle.scenes ?? [])
      sceneDescriptors.push({ ...scene, recordUrl:
        relative(projectRoot, member(bundleDirectory, scene.recordUrl)).replaceAll('\\', '/') });
    if (manifest.editor?.entry) {
      if (!manifest.editor.controller) throw new Error(`Module ${manifest.moduleId} has no editor controller type`);
      const runtimeData = await loadModulePrefabs(moduleBundle, member(moduleRoot, manifest.code.entry), moduleRoot, bundleDirectory);
      // JU's editor-only helper populates optional footstep audio from Unity asset paths.
      // Runtime transpilation excludes UNITY_EDITOR, and preview audio assets are optional.
      const footstep = runtimeData.runtimeTypes['JUTPS.FX.JUFootstep'];
      if (footstep && !footstep.prototype.LoadDefaultFootstepInInspector)
        footstep.prototype.LoadDefaultFootstepInInspector = function () {
          this.FootstepAudioClips ??= [];
        };
      UnityType.Register(runtimeData.runtimeTypes);
      if (manifest.systemEditor?.entry) {
        const systemFile = member(moduleRoot, manifest.systemEditor.entry);
        const revision = `sha256:${createHash('sha256').update(await readFile(systemFile)).digest('hex')}`;
        if (revision !== manifest.systemEditor.revision)
          throw new Error(`Preview system editor revision mismatch: ${manifest.moduleId}`);
        const systemEditor = await import(await importPreparedModule(systemFile, moduleRoot));
        if (!systemEditorRevision) {
          UnityType.RegisterSystemEditor(systemEditor.unityTypes ?? {});
          systemEditorRevision = revision;
        } else if (systemEditorRevision !== revision) {
          // The selected game owns the shared editor API. Add types missing
          // from older character modules while retaining its base classes.
          const additionalTypes = Object.fromEntries(Object.entries(systemEditor.unityTypes ?? {})
            .filter(([name]) => !UnityType.GetType(name)));
          UnityType.Register(additionalTypes);
          diagnostics.push({ severity: 'warning', phase: 'system-editor',
            message: `Module ${manifest.moduleId} uses a different system editor build; shared types come from the selected game` });
        }
      }
      for (const [type, name] of runtimeData.typeNames) componentTypeNames.set(type, name);
      for (const [path, id] of Object.entries(moduleBundle.assetPaths ?? {})) {
        const asset = runtimeData.assets.get(id);
        if (!asset) throw new Error(`Preview AssetDatabase asset is missing: ${path} (${id})`);
        const key = path.replaceAll('\\', '/').toLowerCase();
        if (editorAssets.has(key) && editorAssets.get(key) !== asset)
          throw new Error(`Preview modules export different assets at ${path}`);
        editorAssets.set(key, asset);
      }
      for (const [path, id] of Object.entries(moduleBundle.resources ?? {})) {
        const asset = runtimeData.assets.get(id);
        if (!asset) throw new Error(`Preview Resources asset is missing: ${path} (${id})`);
        if (editorResources.has(path) && editorResources.get(path) !== asset)
          throw new Error(`Preview modules export different Resources assets at ${path}`);
        editorResources.set(path, asset);
        sceneResources.set(path, id);
      }
      for (const [templateId, prepared] of runtimeData.prefabs) {
        const path = moduleBundle.itemTemplates?.[templateId]?.prefab?.replaceAll('\\', '/');
        if (!path) continue;
        if (editorAssets.has(path) && editorAssets.get(path) !== prepared.prefab)
          throw new Error(`Preview modules export different prefabs at ${path}`);
        editorAssets.set(path, prepared.prefab);
      }
      const controller = await loadEditorController(member(moduleRoot, manifest.editor.entry), moduleRoot, manifest.editor.controller);
      controller.Model = { id: manifest.moduleId, name: manifest.name ?? manifest.moduleId,
        type: manifest.moduleType ?? "Other", itemGroups: [{ items: Object.values(moduleBundle.itemTemplates ?? {}) }] };
      editorModules.push({ moduleId: manifest.moduleId, controllerType: manifest.editor.controller,
        controller,
        moduleBundle, prefabs: runtimeData.prefabs });
    } else {
      const builder = await import(pathToFileURL(member(moduleRoot, manifest.builder.entry)).href);
      if (typeof builder.buildPreview !== 'function') throw new Error(`Module ${manifest.moduleId} has no buildPreview export`);
      legacyBuilders.push({ moduleId: manifest.moduleId, moduleBundle, builder });
    }
    for (const [name, paths] of Object.entries(moduleBundle.assetBundles ?? {})) {
      const key = name.replaceAll('\\', '/').toLowerCase();
      if (!sceneAssetBundles.has(key)) sceneAssetBundles.set(key, { name, paths: new Map() });
      const bundle = sceneAssetBundles.get(key);
      for (const [path, id] of Object.entries(paths)) {
        const normalized = path.replaceAll('\\', '/').toLowerCase();
        if (bundle.paths.has(normalized) && bundle.paths.get(normalized).id !== id)
          throw new Error(`Preview modules export different AssetBundle assets at ${name}/${path}`);
        bundle.paths.set(normalized, { path, id });
      }
    }
    for (const asset of moduleBundle.assets) {
      const existing = assets.find(value => value.id === asset.id);
      if (existing) {
        if (JSON.stringify(existing) !== JSON.stringify(asset))
          throw new Error(`Preview modules export different assets with id ${asset.id}`);
        continue;
      }
      if (asset.url) {
        const source = member(bundleDirectory, asset.url);
        assets.push({ ...asset, url: relative(projectRoot, source).replaceAll('\\', '/') });
      } else assets.push(asset);
    }
    modules.push({ moduleId: manifest.moduleId, revision: manifest.revision,
      ...(record.staticAssetBaseUrl ? { staticAssetBaseUrl: record.staticAssetBaseUrl } : {}),
      codeEntry: relative(projectRoot, member(moduleRoot,
        manifest.code.browserEntry ?? manifest.code.entry)).replaceAll('\\', '/'),
      ...(manifest.previewAnimation?.entry ? { animationEntry: relative(projectRoot,
        member(moduleRoot, manifest.previewAnimation.entry)).replaceAll('\\', '/') } : {}),
      ...(manifest.previewAvatarCalibration?.entry ? { avatarCalibrationEntry: relative(projectRoot,
        member(moduleRoot, manifest.previewAvatarCalibration.entry)).replaceAll('\\', '/') } : {}) });
    if (moduleBundle.previewDiagnostics?.length)
      diagnostics.push({ severity: 'warning', phase: 'module-assets',
        message: `${manifest.name ?? manifest.moduleId}: ${moduleBundle.previewDiagnostics.length} asset approximations or unsupported values; see the module asset bundle for details` });
  }
  const userAssets = new Map((project.inputs.userAssets ?? []).map(asset => [asset.id, asset]));
  AssetDatabase.Configure(editorAssets);
  const editorRuntime = new UnityRuntime({ editorMode: true });
  for (const [path, asset] of editorResources) editorRuntime.registerResource(path, asset);
  // Activate the editor context before controller methods call Resources.Load.
  // registerResource alone does not select the runtime used by Unity statics.
  editorRuntime.addSystem({});
  const moduleItems = [];
  for (const gameItem of allItems) {
    const moduleId = gameItem.moduleId ?? gameItem.ModuleId;
    if (`${moduleId}`.toLowerCase() === 'builtin-banners') {
      const id = `item:${gameItem.id ?? gameItem.Id}`;
      roots.push({ id, name: gameItem.name ?? gameItem.Name ?? 'Banner', active: true,
        tag: 'Untagged', layer: 0,
        transform: { id: `${id}:transform`, ...itemTransform(gameItem) },
        components: [], children: [] });
      diagnostics.push({ severity: 'warning', phase: 'item-import',
        message: `Banner item ${gameItem.id ?? gameItem.Id} has no preview geometry` });
      continue;
    }
    const buildItem = findBuildItem(build, gameItem.buildId ?? gameItem.BuildId);
    const externalId = moduleId?.toUpperCase() === 'USER_ASSET'
      ? gameItem.templateId ?? gameItem.TemplateId
      : buildItem?.data?.userAssetId ?? buildItem?.data?.characterAssetId;
    if (!externalId) { moduleItems.push(gameItem); continue; }
    const asset = userAssets.get(externalId);
    if (!asset) throw new Error(`Preview user asset is missing: ${externalId}`);
    if (asset.kind !== 'model3d' || !/\.(?:glb|fbx)$/i.test(asset.primaryFile))
      throw new Error(`Preview item ${gameItem.id ?? gameItem.Id} requires a GLB or FBX model asset`);
    const source = member(projectRoot, `${asset.root}/${asset.primaryFile}`);
    if (!(await stat(source).catch(() => null))?.isFile())
      throw new Error(`Preview user asset file is missing: ${asset.primaryFile}`);
    const modelId = `user:${externalId}`;
    if (!assets.some(value => value.id === modelId))
      assets.push({ id: modelId, kind: 'model', url: relative(projectRoot, source).replaceAll('\\', '/') });
    const id = `item:${gameItem.id ?? gameItem.Id}`;
    roots.push({ id, name: gameItem.name ?? gameItem.Name ?? externalId, active: true,
      tag: 'Untagged', layer: 0,
      transform: { id: `${id}:transform`, ...itemTransform(gameItem) },
      model: modelId, components: [], children: [] });
  }
  if (editorModules.length) {
    const gameType = systemEditorRevision ? UnityType.GetType('PlygroundGame') : null;
    const game = gameType ? Object.assign(new gameType(), item) : item;
    game.GameItems = allItems;
    if (gameType) game.Modules = editorModules.map(module => module.controller.Model);
    const imported = await runPreviewImport({ modules: editorModules, game, items: moduleItems, build,
      createItem: async (module, gameItem, _build, call) => {
        if (!module) throw new Error(`No Preview controller for item ${gameItem.id ?? gameItem.Id}`);
        const templateId = gameItem.templateId ?? gameItem.TemplateId;
        const prepared = module.prefabs.get(templateId);
        const template = module.moduleBundle.itemTemplates?.[templateId]
          ?? (prepared ? { id: templateId, name: prepared.prefab.name, prefab: '' } : null);
        const buildItem = findBuildItem(build, gameItem.buildId ?? gameItem.BuildId);
        let object;
        try {
          object = await call(module, 'CreateItem', gameItem, template, JToken.As(buildItem, JObject));
        } catch (error) {
          throw new Error(`Preview item ${gameItem.id ?? gameItem.Id} (${gameItem.name ?? gameItem.Name ?? templateId}, build ${gameItem.buildId ?? gameItem.BuildId ?? 'none'}, matched ${buildItem != null}) failed: ${error.message}`, { cause: error });
        }
        if (object == null && prepared) {
          object = UnityObject.Instantiate(prepared.prefab);
          applyPrefabModels(object, prepared.root);
          applyItemTransform(object, gameItem);
        }
        if (object && !object.previewModel) {
          const asset = module.moduleBundle.itemAssets?.[gameItem.templateId ?? gameItem.TemplateId]
            ?? module.moduleBundle.itemAssets?.[gameItem.id ?? gameItem.Id];
          if (asset) object.previewModel = asset;
        }
        if (object == null) throw new Error(`Preview controller ${module.controllerType} did not create item ${gameItem.id ?? gameItem.Id}`);
        return object;
      },
      connectObject: async (module, object, gameItem, context, call) => {
        try {
          await call(module, 'ConnectObject', object, gameItem, context);
        } catch (error) {
          throw new Error(`Preview item ${gameItem.id ?? gameItem.Id} (${gameItem.name ?? gameItem.Name ?? ''}) could not connect through ${module.controllerType}: ${error.message}`, { cause: error });
        }
      } });
    for (const [gameItem, object] of imported.created)
      roots.push(serializePreviewObject(object, `item:${gameItem.id ?? gameItem.Id}`, componentTypeNames));
  }
  for (const { moduleId, moduleBundle, builder } of legacyBuilders) {
    await builder.buildPreview({ build, item, bobScene: bob, moduleId, moduleBundle,
      addRoot(root) { roots.push(root); },
      addAsset(asset) {
        if (assets.some(existing => existing.id === asset?.id)) throw new Error(`Duplicate preview asset id: ${asset?.id}`);
        assets.push(asset);
      } });
  }
  if (!modules.length) throw new Error('Preview project has no game module');
  const inputBindings = project.input?.legacyAxes
    ? legacyBindingsFromAxes(project.input.legacyAxes) : null;
  for (const message of inputBindings?.diagnostics ?? []) diagnostics.push({
    severity: 'warning', phase: 'input', message });
  for (const record of project.input?.actionAssets ?? []) {
    if (typeof record?.id !== 'string' || !record.id)
      throw new Error('Preview Input Action asset needs an ID');
    const data = await json(member(projectRoot, record.path));
    assets.push({ id: record.id, kind: 'inputActionAsset', name: record.name ?? data.name ?? '', data });
  }
  const bundle = { format: BUNDLE_FORMAT, version: BUNDLE_VERSION, coordinateSystem: 'unity',
    assets, layers, renderingLayers: Object.keys(renderingLayers).length
      ? renderingLayers : undefined, qualitySettings: qualitySettings ?? undefined,
    renderSettings: renderSettings ?? undefined,
    inputBindings: inputBindings ? { buttons: inputBindings.buttons,
      axes: inputBindings.axes } : moduleInputBindings ?? undefined,
    buildTags: buildTags ?? undefined,
    resources: Object.fromEntries(sceneResources),
    assetBundles: Object.fromEntries([...sceneAssetBundles.values()]
      .map(bundle => [bundle.name, Object.fromEntries([...bundle.paths.values()]
        .map(({ path, id }) => [path, id]))])),
    ...(sceneDescriptors.length ? { scenes: sceneDescriptors } : {}),
    scene: { name: project.variationId, roots } };
  const exportedClips = new Set(assets.filter(asset => asset.kind === 'animationClip'
    && asset.tracks?.length)
    .map(asset => asset.name?.toLowerCase()));
  const hasSampledClips = modules.some(module => module.animationEntry);
  const inspectAnimation = node => {
    for (const component of node.components ?? []) {
      if (component.type !== 'Animator' && component.type !== 'UnityEngine.Animator') continue;
      const graph = component.properties?.controllerGraph;
      if (!graph?.layers?.length) continue;
      const motions = new Set();
      const visit = value => {
        if (!value || typeof value !== 'object') return;
        if (value.kind === 'clip' && value.name) motions.add(value.name);
        for (const child of Object.values(value)) {
          if (!child || typeof child !== 'object') continue;
          if (Array.isArray(child)) child.forEach(visit);
          else visit(child);
        }
      };
      visit(graph.layers);
      const missing = [...motions].filter(name => !exportedClips.has(name.toLowerCase()));
      if (missing.length && !hasSampledClips) diagnostics.push({ severity: 'warning',
        phase: 'animation', message: `Animator on ${node.name} references ${missing.length} clips absent from Preview animation data`,
        missingClips: missing });
    }
    node.children?.forEach(inspectAnimation);
  };
  roots.forEach(inspectAnimation);
  validateManifest(bundle, BUNDLE_FORMAT);
  return { bundle, modules, diagnostics };
}
