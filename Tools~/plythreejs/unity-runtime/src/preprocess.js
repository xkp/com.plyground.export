import { createHash } from 'node:crypto';
import { copyFile, mkdir, readFile, realpath, writeFile } from 'node:fs/promises';
import { dirname, extname, isAbsolute, join, relative, resolve, sep } from 'node:path';
import { pathToFileURL } from 'node:url';
import { AssetFormatError, BUNDLE_FORMAT, BUNDLE_VERSION, SOURCE_FORMAT, validateManifest } from './asset-schema.js';
import { compileUnityMesh } from './unity-mesh.js';
import { bakeUnityTriangulation } from './navigation.js';

const extensions = {
  model: new Set(['.glb', '.unitymesh.json']),
  texture: new Set(['.png', '.jpg', '.jpeg', '.webp']),
  audio: new Set(['.mp3', '.ogg', '.wav']),
  uiFont: new Set(['.ttf', '.otf']),
  navMesh: new Set(['.navmesh', '.unitynavmesh.json']),
  terrainData: new Set(['.unityterrain.json']),
};

function isInside(root, file) {
  const rel = relative(root, file);
  return rel !== '..' && !rel.startsWith(`..${sep}`) && !isAbsolute(rel);
}

function hasNavigationObstacle(manifest) {
  const contains = node => (node.components ?? []).some(component =>
    component.type === 'UnityEngine.AI.NavMeshObstacle'
      || component.type === 'NavMeshObstacle')
    || (node.children ?? []).some(contains);
  return manifest.scene.roots.some(contains)
    || manifest.assets.some(asset => asset.kind === 'prefab' && asset.root && contains(asset.root));
}

function disableNavigationObstacles(manifest) {
  const visit = node => {
    for (const component of node.components ?? [])
      if (component.type === 'UnityEngine.AI.NavMeshObstacle'
        || component.type === 'NavMeshObstacle') component.enabled = false;
    for (const child of node.children ?? []) visit(child);
  };
  for (const root of manifest.scene.roots) visit(root);
  for (const asset of manifest.assets)
    if (asset.kind === 'prefab' && asset.root) visit(asset.root);
}

function offMeshConnections(manifest) {
  const links = [];
  const visit = node => {
    for (const component of node.components ?? []) {
      if (component.type !== 'UnityEngine.AI.OffMeshLink'
        && component.type !== 'OffMeshLink') continue;
      if (component.enabled === false || component.properties?.activated === false) continue;
      const start = component.properties?.startPosition?.$vector3;
      const end = component.properties?.endPosition?.$vector3;
      if (!Array.isArray(start) || !Array.isArray(end))
        throw new AssetFormatError(`OffMeshLink ${component.id} needs exported world endpoints`);
      const point = values => ({ x: values[0], y: values[1], z: values[2] });
      links.push({ startPosition: point(start), endPosition: point(end),
        radius: component.properties.radius ?? 0.5,
        bidirectional: component.properties.biDirectional !== false });
    }
    for (const child of node.children ?? []) visit(child);
  };
  for (const root of manifest.scene.roots) visit(root);
  return links;
}

/** Convert a Unity-exported source manifest into a browser-ready, content-addressed bundle. */
export async function preprocess(sourceFile, outputDirectory, additionalSources = [],
  { transformSource = null, standaloneNavFallback = false } = {}) {
  const inputs = [sourceFile, ...additionalSources];
  const sources = await Promise.all(inputs.map(async (file, index) => {
    const input = resolve(file);
    const parsed = JSON.parse((await readFile(input, 'utf8')).replace(/^\uFEFF/, ''));
    return { root: await realpath(dirname(input)),
      manifest: validateManifest(index === 0 && transformSource
        ? transformSource(parsed) : parsed, SOURCE_FORMAT) };
  }));
  const source = sources[0].manifest;
  if (source.coordinateSystem !== 'unity') {
    throw new AssetFormatError('Source coordinateSystem must be unity; convert model geometry before export');
  }
  const outputRoot = resolve(outputDirectory);
  await mkdir(join(outputRoot, 'assets'), { recursive: true });
  const assets = [];
  const assetsById = new Map();
  const addAsset = record => {
    const previous = assetsById.get(record.id);
    if (previous) {
      if (JSON.stringify(previous) !== JSON.stringify(record))
        throw new AssetFormatError(`Scenes export different data for asset ${record.id}`);
      return;
    }
    assetsById.set(record.id, record);
    assets.push(record);
  };
  const assetEntries = sources.flatMap(({ root, manifest }) => manifest.assets
    .map(asset => ({ asset, root, manifest })))
    .sort((a, b) => a.asset.id.localeCompare(b.asset.id));
  for (const { asset, root: sourceRoot, manifest } of assetEntries) {
    if (asset.kind === 'inputActionAsset') {
      addAsset({ id: asset.id, kind: asset.kind, name: asset.name,
        data: asset.data ?? JSON.parse(asset.dataJson) });
      continue;
    }
    if (['scriptableObject', 'prefab', 'physicsMaterial', 'animationClip',
      'animatorController', 'material', 'sprite', 'uiPanelSettings', 'uiVisualTree',
      'uiStyleSheet', 'tmpFont'].includes(asset.kind)) {
      addAsset(asset); continue;
    }
    if (isAbsolute(asset.path)) throw new AssetFormatError(`Absolute asset path: ${asset.path}`);
    const file = await realpath(resolve(sourceRoot, asset.path));
    if (!isInside(sourceRoot, file)) throw new AssetFormatError(`Asset escapes source directory: ${asset.path}`);
    const lower = file.toLowerCase();
    const extension = lower.endsWith('.unitymesh.json') ? '.unitymesh.json'
      : lower.endsWith('.unitynavmesh.json') ? '.unitynavmesh.json'
      : lower.endsWith('.unityterrain.json') ? '.unityterrain.json'
      : extname(lower);
    const tileCache = asset.kind === 'navMesh' && hasNavigationObstacle(manifest);
    let effectiveTileCache = tileCache;
    const links = asset.kind === 'navMesh' ? offMeshConnections(manifest) : [];
    if (tileCache && extension !== '.unitynavmesh.json')
      throw new AssetFormatError(`NavMesh ${asset.id} needs Unity triangulation for obstacle carving`);
    if (!extensions[asset.kind].has(extension)) {
      throw new AssetFormatError(`Asset ${asset.id} needs a supported ${asset.kind} format`);
    }
    let bytes;
    try {
      if (extension === '.unitymesh.json')
        bytes = compileUnityMesh(JSON.parse(await readFile(file, 'utf8')));
      else if (extension === '.unitynavmesh.json') {
        const triangulation = JSON.parse(await readFile(file, 'utf8'));
        try {
          bytes = await bakeUnityTriangulation(triangulation,
            { tileCache, offMeshConnections: links });
        } catch (error) {
          if (!standaloneNavFallback) throw error;
          bytes = await bakeUnityTriangulation(triangulation,
            { tiled: true, cellSize: 1, offMeshConnections: links });
          effectiveTileCache = false;
          disableNavigationObstacles(manifest);
          source.diagnostics ??= [];
          source.diagnostics.push(`Standalone NavMesh ${asset.id} uses a 1 m tiled bake; obstacle carving is unavailable (${error.message})`);
        }
      } else bytes = await readFile(file);
    } catch (error) {
      throw new AssetFormatError(`Could not convert ${asset.kind} ${asset.id}: ${error.message}`);
    }
    const sha256 = createHash('sha256').update(bytes).digest('hex');
    const filename = `${sha256}${extension === '.unitymesh.json' ? '.glb'
      : extension === '.unitynavmesh.json' ? effectiveTileCache ? '.tilecache' : '.navmesh' : extension}`;
    if (!assetsById.has(asset.id)) {
      if (extension === '.unitymesh.json' || extension === '.unitynavmesh.json')
        await writeFile(join(outputRoot, 'assets', filename), bytes);
      else await copyFile(file, join(outputRoot, 'assets', filename));
    }
    addAsset({ id: asset.id, kind: asset.kind, url: `assets/${filename}`, sha256,
      ...(effectiveTileCache && links.length ? { offMeshConnections: links } : {}),
      ...(asset.name ? { name: asset.name } : {}),
      ...(asset.kind === 'texture' ? {
        textureWidth: asset.textureWidth, textureHeight: asset.textureHeight,
        textureMipCount: asset.textureMipCount, textureReadable: asset.textureReadable,
        textureIsDataSRGB: asset.textureIsDataSRGB,
        textureFilterMode: asset.textureFilterMode,
        textureWrapModeU: asset.textureWrapModeU,
        textureWrapModeV: asset.textureWrapModeV,
        textureAnisoLevel: asset.textureAnisoLevel,
      } : {}),
      ...(asset.kind === 'audio' ? { length: asset.length, samples: asset.samples,
        frequency: asset.frequency, channels: asset.channels } : {}) });
  }
  const scenes = [];
  for (const { manifest } of sources.slice(1)) {
    const scene = manifest.scene;
    if (manifest.coordinateSystem !== source.coordinateSystem)
      throw new AssetFormatError(`Scene ${scene.name} uses a different coordinate system`);
    const record = JSON.stringify({ ...scene, path: scene.path ?? '',
      buildIndex: scene.buildIndex ?? -1,
      ...(manifest.renderSettings ? { renderSettings: manifest.renderSettings } : {}) });
    const sha256 = createHash('sha256').update(record).digest('hex');
    await mkdir(join(outputRoot, 'scenes'), { recursive: true });
    await writeFile(join(outputRoot, 'scenes', `${sha256}.json`), record);
    scenes.push({ name: scene.name, path: scene.path ?? '',
      buildIndex: scene.buildIndex ?? -1,
      ...(manifest.renderSettings ? { renderSettings: manifest.renderSettings } : {}),
      recordUrl: `scenes/${sha256}.json`, sha256 });
  }
  const bundle = {
    format: BUNDLE_FORMAT,
    version: BUNDLE_VERSION,
    coordinateSystem: 'unity',
    sourceUnityVersion: source.sourceUnityVersion ?? null,
    applicationIdentity: source.applicationIdentity,
    diagnostics: source.diagnostics ?? [],
    layers: source.layers,
    renderingLayers: source.renderingLayers,
    sortingLayers: source.sortingLayers,
    renderSettings: source.renderSettings,
    physicsSettings: source.physicsSettings,
    tags: source.tags,
    buildTags: source.buildTags,
    qualitySettings: source.qualitySettings,
    inputBindings: source.inputBindings,
    legacyAxes: source.legacyAxes,
    resources: source.resources,
    assetBundles: source.assetBundles,
    assets,
    ...(scenes.length ? { scenes } : {}),
    scene: source.scene,
  };
  validateManifest(bundle, BUNDLE_FORMAT);
  await writeFile(join(outputRoot, 'bundle.json'), `${JSON.stringify(bundle, null, 2)}\n`);
  return bundle;
}

if (process.argv[1] && import.meta.url === pathToFileURL(resolve(process.argv[1])).href) {
  if (process.argv.length < 4) {
    process.stderr.write('Usage: node src/preprocess.js <active-source.json> <output-directory> [other-scene-source.json...]\n');
    process.exitCode = 2;
  } else {
    preprocess(process.argv[2], process.argv[3], process.argv.slice(4))
      .then(bundle => process.stdout.write(`Wrote ${bundle.scene.name} with ${bundle.assets.length} assets\n`))
      .catch(error => { process.stderr.write(`${error.stack}\n`); process.exitCode = 1; });
  }
}
