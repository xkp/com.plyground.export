import * as THREE from 'three';
import { GLTFLoader } from 'three/addons/loaders/GLTFLoader.js';
import { FBXLoader } from 'three/addons/loaders/FBXLoader.js';
import { clone as cloneSkeleton } from 'three/addons/utils/SkeletonUtils.js';
import { orientUnityNormalMaps } from './preview-model-materials.js';
import { Shader } from './core-shader.js';
import { List } from './collections.js';
import {
  Animator, AudioChorusFilter, AudioClip, AudioDistortionFilter, AudioEchoFilter, AudioHighPassFilter, AudioListener, AudioLowPassFilter, AudioReverbFilter, AudioReverbZone, AudioSource, BoxCollider, MeshCollider, Button, Camera, Canvas, CanvasRenderer, CanvasGroup, CapsuleCollider, CharacterController, CharacterJoint,
  GameObject, LazyResource, Light, LODGroup, LineRenderer, TrailRenderer, Material, Mesh, MeshFilter, MeshRenderer, Rigidbody, SkinnedMeshRenderer, Sprite, SpriteRenderer, SphereCollider, WheelCollider, Terrain, TerrainCollider, TerrainData,
  ParticleSystem, ParticleSystemRenderer, NavMeshAgent, NavMeshObstacle, OffMeshLink, NavigationSystem, RectTransform, Transform, Image, RawImage, Text, Font, TMP_FontAsset, TMP_Text, TextMeshProUGUI, TextMeshPro, TMP_InputField, TMP_Dropdown,
  Vector2, Vector2Int, Vector3, Vector4, Quaternion, Matrix4x4, Rect, Color, Texture2D, AnimationCurve, Keyframe, InputAction, InputActionAsset, InputActionReference, PlayerInput, LayerMask, RenderingLayerMask, SortingLayer, RenderSettings,
  ScriptableObject, ISerializationCallbackReceiver, ManagedReferenceUtility, Physics, PhysicsMaterial, Resources, SceneManager, UnityEvent, UniversalAdditionalCameraData,
  CanvasScaler, GraphicRaycaster, EventSystem, BaseInput, StandaloneInputModule, InputSystemUIInputModule, Mask, RectMask2D, Shadow, Outline,
  EventTrigger, ScrollRect, GridLayoutGroup, Scrollbar, VerticalLayoutGroup,
  HorizontalLayoutGroup, LayoutElement, ContentSizeFitter, AspectRatioFitter, Slider, Toggle, ToggleGroup,
  InputField, Dropdown,
  UIDocument, PanelSettings, VisualTreeAsset, StyleSheet, ThemeStyleSheet,
} from './index.js';
import { PhysicsUnsupportedComponents } from './physics-api-stubs.js';
import { AssetFormatError, BUNDLE_FORMAT, validateManifest } from './asset-schema.js';
import { assetDependencyIds, startupAssetIds } from './asset-preload.js';

const threeTextureOf = texture => texture instanceof Texture2D
  ? texture.threeTexture : texture;

function toolkitStyleUrls(data, manifest, baseUrl, fontKey = 'fontAssets', imageKey = 'imageAssets') {
  const fontUrls = new Map((data[fontKey] ?? []).map(font => {
    const faces = [font.assetId, ...(font.fallbackAssetIds ?? [])].map(id => {
      const source = manifest.assets.find(candidate => candidate.id === id
        && candidate.kind === 'uiFont');
      return { family: `PlyUI_${id.replace(/\W/g, '_')}`,
        url: new URL(source.url, baseUrl).href };
    });
    return [font.reference, { ...faces[0], fallbacks: faces.slice(1) }];
  }));
  const imageUrls = new Map((data[imageKey] ?? []).map(image => {
    const source = manifest.assets.find(candidate => candidate.id === image.assetId
      && candidate.kind === 'texture');
    return [image.reference, new URL(source.url, baseUrl).href];
  }));
  return { fontUrls, imageUrls };
}

export class UnknownComponentError extends AssetFormatError {
  constructor(type, object = null) {
    super(`Unregistered Unity component: ${type}${object ? ` on ${object.name} (${object.assetId})` : ''}`);
    this.name = 'UnknownComponentError';
  }
}

export class AssetNotReadyError extends AssetFormatError {
  constructor(asset) {
    super(`AssetNotReady: ${asset.id} (${asset.kind}) must be preloaded before synchronous Unity access`);
    this.name = 'AssetNotReadyError';
    this.assetId = asset.id;
  }
}

/** Reconstructs Unity-named objects from a preprocessed bundle. */
export class AssetBundleLoader {
  constructor(runtime, { assetResolver, recordResolver, sceneResolver, assetConcurrency = 1,
    components = {}, scriptableObjects = {}, types = {}, unknownComponentFactory = null } = {}) {
    this.runtime = runtime;
    this.assetResolver = assetResolver ?? this._defaultResolver.bind(this);
    this.recordResolver = recordResolver ?? this._defaultRecordResolver.bind(this);
    this.sceneResolver = sceneResolver ?? this._defaultSceneResolver.bind(this);
    this.assetConcurrency = Math.max(1, Math.min(8, Math.trunc(assetConcurrency) || 1));
    this.unknownComponentFactory = unknownComponentFactory;
    this.components = new Map();
    this.scriptableObjects = new Map();
    this.types = new Map(globalThis.Object.entries(types));
    const builtins = {
      Animator, AudioChorusFilter, AudioDistortionFilter, AudioEchoFilter, AudioHighPassFilter, AudioListener, AudioLowPassFilter, AudioReverbFilter, AudioReverbZone, AudioSource, BoxCollider, MeshCollider, Button, Camera, Canvas, CanvasRenderer, CanvasGroup, CapsuleCollider, CharacterController, CharacterJoint,
      Light, LODGroup, LineRenderer, TrailRenderer, MeshFilter, MeshRenderer, Rigidbody, SkinnedMeshRenderer, SpriteRenderer, SphereCollider, WheelCollider, Terrain, TerrainCollider,
      ParticleSystem, ParticleSystemRenderer, NavMeshAgent, NavMeshObstacle, OffMeshLink, Image, RawImage, Text,
    };
    for (const [name, type] of globalThis.Object.entries(builtins)) {
      this.registerComponent(name, type);
      this.registerComponent(`UnityEngine.${name}`, type);
      if (name === 'NavMeshAgent' || name === 'NavMeshObstacle' || name === 'OffMeshLink')
        this.registerComponent(`UnityEngine.AI.${name}`, type);
      if (['Button', 'Image', 'RawImage', 'Text'].includes(name)) this.registerComponent(`UnityEngine.UI.${name}`, type);
    }
    for (const [name, type] of globalThis.Object.entries(PhysicsUnsupportedComponents)) {
      this.registerComponent(name, type);
      this.registerComponent(`UnityEngine.${name}`, type);
    }
    const presentation = {
      'UnityEngine.Rendering.Universal.UniversalAdditionalCameraData': UniversalAdditionalCameraData,
      'UnityEngine.UI.CanvasScaler': CanvasScaler,
      'UnityEngine.UI.GraphicRaycaster': GraphicRaycaster,
      'UnityEngine.EventSystems.EventSystem': EventSystem,
      'UnityEngine.EventSystems.BaseInput': BaseInput,
      'UnityEngine.EventSystems.StandaloneInputModule': StandaloneInputModule,
      'UnityEngine.InputSystem.UI.InputSystemUIInputModule': InputSystemUIInputModule,
      'UnityEngine.InputSystem.PlayerInput': PlayerInput,
      'UnityEngine.UI.Mask': Mask,
      'UnityEngine.UI.RectMask2D': RectMask2D,
      'UnityEngine.UI.Shadow': Shadow,
      'UnityEngine.UI.Outline': Outline,
      'UnityEngine.EventSystems.EventTrigger': EventTrigger,
      'UnityEngine.UI.ScrollRect': ScrollRect,
      'UnityEngine.UI.GridLayoutGroup': GridLayoutGroup,
      'UnityEngine.UI.Scrollbar': Scrollbar,
      'UnityEngine.UI.VerticalLayoutGroup': VerticalLayoutGroup,
      'UnityEngine.UI.HorizontalLayoutGroup': HorizontalLayoutGroup,
      'UnityEngine.UI.LayoutElement': LayoutElement,
      'UnityEngine.UI.ContentSizeFitter': ContentSizeFitter,
      'UnityEngine.UI.AspectRatioFitter': AspectRatioFitter,
      'UnityEngine.UI.Slider': Slider,
      'UnityEngine.UI.Toggle': Toggle,
      'UnityEngine.UI.ToggleGroup': ToggleGroup,
      'UnityEngine.UI.InputField': InputField,
      'UnityEngine.UI.Dropdown': Dropdown,
      'UnityEngine.UIElements.UIDocument': UIDocument,
      'TMPro.TMP_Text': TMP_Text,
      'TMPro.TextMeshProUGUI': TextMeshProUGUI,
      'TMPro.TextMeshPro': TextMeshPro,
      'TMPro.TMP_InputField': TMP_InputField,
      'TMPro.TMP_Dropdown': TMP_Dropdown,
    };
    for (const [name, type] of globalThis.Object.entries(presentation)) this.registerComponent(name, type);
    for (const [name, type] of globalThis.Object.entries(components)) this.registerComponent(name, type);
    for (const [name, type] of globalThis.Object.entries(scriptableObjects))
      this.registerScriptableObject(name, type);
  }

  registerComponent(name, type) {
    if (typeof name !== 'string' || typeof type !== 'function') throw new TypeError('Expected a component name and constructor');
    this.components.set(name, type);
    return this;
  }

  registerScriptableObject(name, type) {
    if (typeof name !== 'string' || typeof type !== 'function'
      || !(type.prototype instanceof ScriptableObject))
      throw new TypeError('Expected a ScriptableObject name and constructor');
    this.scriptableObjects.set(name, type);
    this.types.set(name, type);
    const visited = new Set();
    const registerNested = (parentName, parentType) => {
      if (visited.has(parentType)) return;
      visited.add(parentType);
      for (const [member, child] of globalThis.Object.entries(parentType)) {
        if (typeof child !== 'function' || child === parentType) continue;
        const nestedName = `${parentName}.${member}`;
        this.types.set(nestedName, child);
        registerNested(nestedName, child);
      }
    };
    registerNested(name, type);
    return this;
  }

  async _defaultResolver(asset, url) {
    if (asset.kind === 'terrainData') {
      const response = await fetch(url);
      if (!response.ok) throw new Error(`Could not fetch TerrainData: HTTP ${response.status}`);
      const record = await response.json();
      if (record.format !== 'unity-terrain-data/v1') throw new AssetFormatError(`Invalid TerrainData format: ${asset.id}`);
      const data = new TerrainData(record);
      data.name = asset.name ?? '';
      return data;
    }
    if (asset.kind === 'navMesh') {
      const response = await fetch(url);
      if (!response.ok) throw new Error(`Could not fetch NavMesh: ${response.status}`);
      return new Uint8Array(await response.arrayBuffer());
    }
    if (asset.kind === 'model') {
      const model = await (/\.fbx(?:[?#]|$)/i.test(url)
        ? new FBXLoader().loadAsync(url) : new GLTFLoader().loadAsync(url));
      const scene = model.scene ?? model;
      orientUnityNormalMaps(scene);
      scene.traverse(object => {
        if (!object.isMesh) return;
        const attributes = object.geometry?.morphAttributes;
        if (!attributes) return;
        for (const [name, values] of globalThis.Object.entries(attributes))
          if (!values?.length) delete attributes[name];
        object.updateMorphTargets?.();
      });
      return model;
    }
    if (asset.kind === 'texture') {
      const texture = await new THREE.TextureLoader().loadAsync(url);
      texture.name = asset.name ?? '';
      texture.colorSpace = THREE.SRGBColorSpace;
      texture.flipY = false;
      texture.needsUpdate = true;
      return texture;
    }
    if (asset.kind === 'audio') {
      const response = await fetch(url);
      if (!response.ok) throw new Error(`Could not fetch audio: HTTP ${response.status}`);
      const buffer = await AudioSource._audioContext(this.runtime)
        .decodeAudioData(await response.arrayBuffer());
      return new AudioClip(buffer, asset);
    }
    if (asset.kind === 'uiFont') {
      const font = new Font();
      font.name = asset.name ?? '';
      font.url = url;
      return font;
    }
    throw new AssetFormatError(`Unsupported asset kind: ${asset.kind}`);
  }

  async _defaultRecordResolver(asset, url) {
    const response = await fetch(url);
    if (!response.ok) throw new AssetFormatError(
      `Could not fetch prefab record ${asset.id} from ${url}: HTTP ${response.status}`);
    const bytes = await response.arrayBuffer();
    const expected = asset.recordUrl.match(/([a-f0-9]{64})\.json$/)?.[1];
    const digest = await crypto.subtle.digest('SHA-256', bytes);
    const actual = [...new Uint8Array(digest)].map(byte => byte.toString(16).padStart(2, '0')).join('');
    if (actual !== expected) throw new AssetFormatError(
      `Prefab record hash mismatch: ${asset.id} from ${url}`);
    return JSON.parse(new TextDecoder().decode(bytes));
  }

  async _defaultSceneResolver(scene, url) {
    const response = await fetch(url);
    if (!response.ok) throw new AssetFormatError(
      `Could not fetch scene ${scene.name} from ${url}: HTTP ${response.status}`);
    const bytes = await response.arrayBuffer();
    const digest = await crypto.subtle.digest('SHA-256', bytes);
    const actual = [...new Uint8Array(digest)]
      .map(byte => byte.toString(16).padStart(2, '0')).join('');
    if (actual !== scene.sha256) throw new AssetFormatError(
      `Scene record hash mismatch: ${scene.name} from ${url}`);
    return JSON.parse(new TextDecoder().decode(bytes));
  }

  async load(manifestOrUrl, { baseUrl, replaceScene = true,
    loadingPolicy = 'eager' } = {}) {
    let manifest = manifestOrUrl;
    if (typeof manifestOrUrl === 'string') {
      const response = await fetch(manifestOrUrl);
      if (!response.ok) throw new Error(`Could not fetch bundle: ${response.status}`);
      manifest = await response.json();
      baseUrl ??= new URL('.', response.url).href;
    }
    validateManifest(manifest, BUNDLE_FORMAT);
    if (!['eager', 'scene-and-resources', 'scene'].includes(loadingPolicy))
      throw new AssetFormatError(`Unknown asset loading policy: ${loadingPolicy}`);
    if (manifest.coordinateSystem !== 'unity') throw new AssetFormatError('Bundle coordinateSystem must be unity');
    if (manifest.layers) LayerMask.Configure(manifest.layers);
    RenderingLayerMask.Configure(manifest.renderingLayers ?? { Default: 0 });
    SortingLayer.Configure(manifest.sortingLayers ?? [{ name: 'Default', id: 0, value: 0 }]);
    RenderSettings.Configure(manifest.renderSettings ?? {}, this.runtime.scene);
    Physics.gravity = new Vector3(...(manifest.physicsSettings?.gravity ?? [0, -9.81, 0]));
    if (Array.isArray(manifest.tags))
      for (const tag of manifest.tags) this.runtime._tags.add(tag);
    baseUrl ??= globalThis.location?.href ?? 'file:///';

    const assets = new Map();
    for (const asset of manifest.assets) {
      if (asset.kind === 'uiPanelSettings') {
        const value = new PanelSettings();
        globalThis.Object.assign(value, asset.data);
        value.referenceResolution = new Vector2Int(...asset.data.referenceResolution);
        if (asset.data.colorClearValue)
          value.colorClearValue = new Color(...asset.data.colorClearValue);
        if (typeof asset.data.themeCss === 'string' && asset.data.themeCss) {
          const theme = new ThemeStyleSheet(asset.data.themeCss);
          theme.name = asset.data.themeName ?? '';
          globalThis.Object.assign(theme, toolkitStyleUrls(asset.data, manifest, baseUrl,
            'themeFontAssets', 'themeImageAssets'));
          value.themeStyleSheet = theme;
        }
        value.name = asset.name ?? '';
        value.assetId = asset.id;
        assets.set(asset.id, value);
        continue;
      }
      if (asset.kind === 'uiStyleSheet') {
        const value = new StyleSheet(asset.data.css);
        value.name = asset.name ?? '';
        value.assetId = asset.id;
        globalThis.Object.assign(value, toolkitStyleUrls(asset.data, manifest, baseUrl));
        assets.set(asset.id, value);
        continue;
      }
      if (asset.kind === 'uiVisualTree') {
        const value = new VisualTreeAsset(asset.data);
        value.name = asset.name ?? '';
        value.assetId = asset.id;
          value.fontUrls = new Map((asset.data.fontAssets ?? []).map(font => {
            const faces = [font.assetId, ...(font.fallbackAssetIds ?? [])].map(id => {
              const source = manifest.assets.find(candidate => candidate.id === id
                && candidate.kind === 'uiFont');
              return { family: `PlyUI_${id.replace(/\W/g, '_')}`,
                url: new URL(source.url, baseUrl).href };
            });
            return [font.reference, { ...faces[0], fallbacks: faces.slice(1) }];
          }));
        value.imageUrls = new Map((asset.data.imageAssets ?? []).map(image => {
          const source = manifest.assets.find(candidate => candidate.id === image.assetId
            && candidate.kind === 'texture');
          return [image.reference, new URL(source.url, baseUrl).href];
        }));
        assets.set(asset.id, value);
        continue;
      }
      if (asset.kind === 'material') {
        const data = asset.data;
        const value = new Material();
        value.name = asset.name ?? data.name ?? '';
        value.assetId = asset.id;
        value.userData.unityShaderGuid = data.shaderGuid;
        value.userData.unityShaderName = data.shaderName ?? '';
        value.userData.previewMaterialModel = data.previewMaterialModel ?? 'urp-lit-v1';
        value.userData.shaderCompatibility = data.shaderCompatibility ?? 'legacy-fallback';
        value.userData.shaderAdapter = data.shaderAdapter ?? null;
        value.userData.shaderTarget = data.shaderTarget ?? null;
        value.configureExportedShader?.();
        value._unityShader = Shader.registerExported(data.shaderName);
        value.color.setRGB(...data.color.slice(0, 3));
        value.opacity = data.color[3];
        value.transparent = data.alphaMode === 'BLEND'
          || data.alphaMode == null && value.opacity < 1;
        value.alphaTest = data.alphaMode === 'MASK' ? data.alphaCutoff ?? 0.5 : 0;
        value._unityAlphaCutoff = data.alphaCutoff ?? 0.5;
        value.userData.urpSurfaceMode = data.urpSurfaceMode ?? Number(value.transparent);
        value.userData.urpAlphaClip = data.alphaMode === 'MASK';
        value.userData.urpBlendMode = data.urpBlendMode ?? 0;
        value.side = data.doubleSided ? THREE.DoubleSide : THREE.FrontSide;
        value.metalness = data.metallic ?? 0;
        value.roughness = data.roughness ?? 1;
        const emissionColor = data.emissionColor ?? data.emissive ?? [0, 0, 0];
        value._unityEmissionColor.setRGB(...emissionColor);
        value._unityEmissionKeyword = data.emissionEnabled ?? Boolean(data.emissive);
        if (value._unityEmissionKeyword && data.emissive)
          value.emissive.setRGB(...data.emissive);
        for (const [key, fallback] of [['textureScale', [1, 1]], ['textureOffset', [0, 0]]]) {
          const pair = data[key] ?? fallback;
          if (!Array.isArray(pair) || pair.length !== 2 || !pair.every(Number.isFinite))
            throw new AssetFormatError(`Material ${key} is invalid: ${asset.id}`);
          value[key === 'textureScale' ? 'mainTextureScale' : 'mainTextureOffset'] = new Vector2(...pair);
        }
        assets.set(asset.id, value);
        continue;
      }
      if (asset.kind === 'physicsMaterial') {
        const value = new PhysicsMaterial(asset.name ?? '');
        value.assetId = asset.id;
        value.dynamicFriction = asset.dynamicFriction;
        value.staticFriction = asset.staticFriction;
        value.bounciness = asset.bounciness;
        value.frictionCombine = asset.frictionCombine ?? 'Average';
        value.bounceCombine = asset.bounceCombine ?? 'Average';
        assets.set(asset.id, value);
        continue;
      }
      if (asset.kind === 'animationClip') {
        const tracks = asset.tracks.map(track => {
          const size = track.property === 'quaternion' ? 4 : 3;
          if (!['position', 'quaternion', 'scale'].includes(track.property)
            || typeof track.target !== 'string' || !Array.isArray(track.times)
            || !track.times.length || !track.times.every(Number.isFinite)
            || track.times.some((time, index) => index > 0 && time <= track.times[index - 1])
            || !Array.isArray(track.values) || track.values.length !== track.times.length * size
            || !track.values.every(Number.isFinite))
            throw new AssetFormatError(`Invalid AnimationClip track: ${asset.id}`);
          const name = `${track.target}.${track.property}`;
          return track.property === 'quaternion'
            ? new THREE.QuaternionKeyframeTrack(name, track.times, track.values)
            : new THREE.VectorKeyframeTrack(name, track.times, track.values);
        });
        const value = new THREE.AnimationClip(asset.name, asset.duration, tracks);
        value.assetId = asset.id;
        value.componentCurves = asset.componentCurves ?? [];
        if (asset.rootMotion?.times?.length > 1) {
          value.times = asset.rootMotion.times;
          value.rootMotion = { positions: asset.rootMotion.positions,
            rotations: asset.rootMotion.rotations };
        }
        assets.set(asset.id, value);
        continue;
      }
      if (asset.kind === 'animatorController') {
        globalThis.Object.defineProperty(asset.graph, '__previewAssetId', {
          value: asset.id, configurable: true
        });
        assets.set(asset.id, asset.graph);
        continue;
      }
      if (asset.kind === 'inputActionAsset') {
        const value = InputActionAsset.fromSerialized(asset.data);
        value.assetId = asset.id;
        assets.set(asset.id, value);
        continue;
      }
      if (asset.kind !== 'scriptableObject') continue;
      const type = this.scriptableObjects.get(asset.type);
      if (!type) throw new AssetFormatError(`Unregistered ScriptableObject: ${asset.type}`);
      const value = ScriptableObject.CreateInstance(type);
      value.name = asset.name ?? '';
      value.assetId = asset.id;
      assets.set(asset.id, value);
    }
    const external = manifest.assets.filter(asset => !['scriptableObject', 'prefab', 'tmpFont',
      'physicsMaterial', 'animationClip', 'animatorController', 'inputActionAsset', 'material', 'sprite',
      'uiPanelSettings', 'uiVisualTree', 'uiStyleSheet'].includes(asset.kind));
    const externalById = new Map(external.map(asset => [asset.id, asset]));
    const assetRecords = new Map(manifest.assets.map(asset => [asset.id, asset]));
    const startup = loadingPolicy === 'eager' ? null : startupAssetIds(manifest,
      { includeResources: loadingPolicy === 'scene-and-resources' });
    const neededAtStartup = external.filter(asset => loadingPolicy === 'eager'
      || startup.has(asset.id));
    const pendingFetches = new Map();
    const fetchQueue = [];
    let activeFetches = 0;
    const pumpFetches = () => {
      while (activeFetches < this.assetConcurrency && fetchQueue.length) {
        const { asset, resolve, reject } = fetchQueue.shift();
        activeFetches++;
        const url = new URL(asset.url, baseUrl).href;
        Promise.resolve().then(() => this.assetResolver(asset, url)).then(value => {
          if (value == null) throw new AssetFormatError(`Resolver returned no asset: ${asset.id}`);
          if (asset.kind === 'texture' && !(value instanceof Texture2D))
            value = Texture2D._fromThreeTexture(value, asset, this.runtime);
          if (typeof value === 'object' && value.assetId == null) value.assetId = asset.id;
          assets.set(asset.id, value);
          return value;
        }).then(resolve, error => reject(new AssetFormatError(
          `Could not load ${asset.kind} ${asset.id} from ${url}: ${error.message}`))).finally(() => {
          activeFetches--;
          pendingFetches.delete(asset.id);
          pumpFetches();
        });
      }
    };
    const fetchExternal = asset => {
      if (assets.has(asset.id)) return Promise.resolve(assets.get(asset.id));
      if (pendingFetches.has(asset.id)) return pendingFetches.get(asset.id);
      const pending = new Promise((resolve, reject) => {
        fetchQueue.push({ asset, resolve, reject });
        pumpFetches();
      });
      pendingFetches.set(asset.id, pending);
      return pending;
    };
    await Promise.all(neededAtStartup.map(fetchExternal));
    // Preserve the eager loader's manifest-order iteration despite parallel fetches.
    for (const asset of neededAtStartup) {
      const value = assets.get(asset.id);
      assets.delete(asset.id);
      assets.set(asset.id, value);
    }
    const rawAsset = assets.get.bind(assets);
    const readyAsset = id => {
      if (externalById.has(id) && !assets.has(id))
        throw new AssetNotReadyError(externalById.get(id));
      return rawAsset(id);
    };
    const materialized = new Set();
    const materializeDerived = id => {
      if (materialized.has(id)) return rawAsset(id);
      const asset = assetRecords.get(id);
      if (!asset || !['tmpFont', 'sprite', 'material'].includes(asset.kind))
        return rawAsset(id);
      if (asset.kind === 'tmpFont') {
        const atlas = readyAsset(asset.data.atlasTextureAssetId);
        if (!threeTextureOf(atlas)?.isTexture) throw new AssetFormatError(`Missing TMP atlas texture: ${asset.id}`);
        const font = Object.assign(new TMP_FontAsset(), asset.data,
          { name: asset.name ?? '', assetId: asset.id, atlas,
            glyphs: new Map(asset.data.glyphs.map(glyph => [glyph.unicode, glyph])) });
        assets.set(asset.id, font);
        materialized.add(id);
        font.fallbackFonts = (asset.data.fallbackFontAssetIds ?? []).map(fallbackId => {
          const fallback = materializeDerived(fallbackId);
          if (!fallback?.glyphs) throw new AssetFormatError(`Missing TMP fallback font: ${fallbackId}`);
          return fallback;
        });
        return font;
      }
      if (asset.kind === 'material') {
        if (asset.data.textureAssetId) {
          const texture = readyAsset(asset.data.textureAssetId);
          if (!threeTextureOf(texture)?.isTexture) throw new AssetFormatError(
            `Missing material texture: ${asset.data.textureAssetId}`);
          rawAsset(asset.id).mainTexture = texture;
        }
        materialized.add(id);
        return rawAsset(id);
      }
      const data = asset.data;
      const imported = readyAsset(data.textureAssetId);
      if (!threeTextureOf(imported)?.isTexture)
        throw new AssetFormatError(`Missing sprite texture: ${data.textureAssetId}`);
      const texture = imported;
      if (!texture.threeTexture.image?.width) texture._width = data.textureWidth;
      if (!texture.threeTexture.image?.height) texture._height = data.textureHeight;
      if (data.textureWrapMode !== undefined) texture.wrapMode = data.textureWrapMode;
      if (data.textureFilterMode !== undefined) texture.filterMode = data.textureFilterMode;
      const sprite = Sprite.Create(texture, new Rect(...data.rect),
        new Vector2(...data.pivot), data.pixelsPerUnit, 0, undefined,
        new Vector4(...(data.border ?? [0, 0, 0, 0])));
      sprite.name = asset.name ?? '';
      sprite.assetId = asset.id;
      assets.set(asset.id, sprite);
      materialized.add(id);
      return sprite;
    };
    for (const asset of manifest.assets)
      if (loadingPolicy === 'eager' || startup.has(asset.id))
        materializeDerived(asset.id);

    const prefabRecords = new Map(manifest.assets.filter(asset => asset.kind === 'prefab')
      .map(asset => [asset.id, asset]));
    const prefabOwners = new Map();
    const indexPrefabNode = (node, prefabId) => {
      prefabOwners.set(node.id, prefabId);
      if (node.transform.id) prefabOwners.set(node.transform.id, prefabId);
      for (const component of node.components ?? []) prefabOwners.set(component.id, prefabId);
      for (const child of node.children ?? []) indexPrefabNode(child, prefabId);
    };
    for (const asset of prefabRecords.values()) if (asset.root) indexPrefabNode(asset.root, asset.id);
    const pendingRecords = new Map();
    const fetchPrefabRecord = asset => {
      if (prefabRecords.get(asset.id)?.root) return Promise.resolve(prefabRecords.get(asset.id));
      if (pendingRecords.has(asset.id)) return pendingRecords.get(asset.id);
      const url = new URL(asset.recordUrl, baseUrl).href;
      const pending = Promise.resolve().then(() => this.recordResolver(asset, url)).then(record => {
        if (record?.id !== asset.id || record.kind !== 'prefab' || !record.root)
          throw new AssetFormatError(`Invalid deferred prefab record: ${asset.id} from ${url}`);
        prefabRecords.set(asset.id, record);
        assetRecords.set(asset.id, record);
        indexPrefabNode(record.root, record.id);
        return record;
      }).catch(error => { throw new AssetFormatError(
        `Could not load prefab record ${asset.id} from ${url}: ${error.message}`); })
        .finally(() => pendingRecords.delete(asset.id));
      pendingRecords.set(asset.id, pending);
      return pending;
    };
    const objects = new Map();
    const components = new Map();
    const pending = [];
    const build = (node, parent = null) => {
      const object = new GameObject(node.name, node.rectTransform ? RectTransform : Transform);
      object.assetId = node.id;
      if (node.semanticSurface !== undefined) object.semanticSurface = node.semanticSurface;
      object.tag = node.tag ?? 'Untagged';
      object.layer = node.layer ?? 0;
      object.isStatic = node.isStatic ?? false;
      object.SetActive(node.active ?? true);
      object.transform.localPosition = new Vector3(...node.transform.position);
      object.transform.localRotation = new Quaternion(...node.transform.rotation);
      object.transform.localScale = new Vector3(...node.transform.scale);
      if (node.rectTransform) {
        const rect = node.rectTransform;
        object.transform.anchorMin = new Vector2(...rect.anchorMin);
        object.transform.anchorMax = new Vector2(...rect.anchorMax);
        object.transform.pivot = new Vector2(...rect.pivot);
        object.transform.sizeDelta = new Vector2(...rect.sizeDelta);
      }
      if (node.transform.id) {
        object.transform.assetId = node.transform.id;
        components.set(node.transform.id, object.transform);
      }
      if (parent) object.SetParent(parent, false);
      if (node.rectTransform?.anchoredPosition)
        object.transform.anchoredPosition = new Vector2(...node.rectTransform.anchoredPosition);
      objects.set(node.id, object);
      if (node.splineOriginMesh) object._previewSplineOriginMesh = true;
      let visual = null;
      let visualMesh = null;
      let visualMeshes = [];
      if (node.model) {
        const model = assets.get(node.model);
        const scene = model?.scene ?? model;
        if (!scene?.isObject3D) throw new AssetFormatError(`Model ${node.model} did not provide a Three.js scene`);
        visual = cloneSkeleton(scene);
        // Unity's glTF importer reflects the source mesh across X. Bob item
        // positions are already in Unity coordinates, so mirror its geometry.
        if (node.mirrorX) visual.scale.x *= -1;
        if (node.semanticSurface) visual.visible = false;
        visual.traverse(child => {
          if (child.isMesh) {
            visualMeshes.push(child);
            if (!visualMesh) visualMesh = child;
          }
        });
        if (visualMesh && node.materialOverride) {
          const override = node.materialOverride;
          const texture = assets.get(override.texture);
          if (!threeTextureOf(texture)?.isTexture) throw new AssetFormatError(`Missing material texture: ${override.texture}`);
          const material = new Material(visualMesh.material);
          material.mainTexture = texture;
          if (Array.isArray(override.color)) material.color.setRGB(...override.color);
          if (Number.isFinite(override.roughness)) material.roughness = override.roughness;
          if (Number.isFinite(override.metalness)) material.metalness = override.metalness;
          visualMesh.material = material;
        }
        object.object3D.add(visual);
      }
      for (const record of node.components ?? []) {
        let type = this.components.get(record.type);
        if (!type && this.unknownComponentFactory) {
          type = this.unknownComponentFactory(record.type);
          if (type) this.registerComponent(record.type, type);
        }
        if (!type) throw new UnknownComponentError(record.type, object);
        let component;
        try {
          component = object.AddComponent(type);
        } catch (error) {
          throw new AssetFormatError(`Failed to construct ${record.type} on ${object.name}: ${error.message}`);
        }
        // RequireComponent can create a component before its own serialized
        // prefab record is visited. Hydrate that instance instead of adding it twice.
        if (!component) component = object.GetComponent(type);
        if (!component) throw new AssetFormatError(
          `Could not attach exported component ${record.type} (${record.id}) on ${object.name}`);
        if (component instanceof MeshFilter && visualMesh) {
          const unityMeshId = visualMesh.userData.unityMeshId;
          const sameUnityMesh = unityMeshId && visualMeshes.length > 1
            && visualMeshes.every(mesh => mesh.userData.unityMeshId === unityMeshId);
          if (sameUnityMesh) {
            const firstPositions = visualMesh.geometry.getAttribute('position')?.array;
            if (!firstPositions || !visualMeshes.every(mesh => {
              const positions = mesh.geometry.getAttribute('position')?.array;
              return positions?.length === firstPositions.length
                && positions.every((value, index) => value === firstPositions[index]);
            })) throw new AssetFormatError(`Exported Unity mesh ${unityMeshId} has mismatched primitive vertices`);
            const logical = new Mesh().copy(visualMesh.geometry);
            logical._unityPrimitiveCount = visualMeshes.length;
            logical.subMeshCount = visualMeshes.length;
            visualMeshes.forEach((mesh, index) => {
              const geometry = mesh.geometry;
              const indices = geometry.getIndex()
                ? Array.from(geometry.getIndex().array)
                : Array.from({ length: geometry.getAttribute('position').count }, (_, i) => i);
              logical.SetTriangles(indices, index, false);
            });
            logical.RecalculateBounds();
            component._setImportedMesh(logical);
          } else {
            visualMesh.geometry = visualMesh.geometry instanceof Mesh
              ? visualMesh.geometry : new Mesh().copy(visualMesh.geometry);
            component._setImportedMesh(visualMesh.geometry);
          }
        }
        if (component instanceof MeshCollider) component.meshRoot = visual;
        if (component instanceof MeshRenderer || component instanceof SkinnedMeshRenderer) {
          component.threeMesh = visualMesh;
          component._renderRoot = visual;
          component._materialMeshes = visualMeshes.length > 1 ? visualMeshes : null;
          component._syncVisualState();
          if (visualMesh) {
            const wrap = material => material instanceof THREE.MeshStandardMaterial
              && !(material instanceof Material) ? new Material(material) : material;
            visualMesh.material = Array.isArray(visualMesh.material)
              ? visualMesh.material.map(wrap) : wrap(visualMesh.material);
            if (component._materialMeshes) {
              component._materialMeshes.forEach(mesh => {
                mesh.material = Array.isArray(mesh.material)
                  ? mesh.material.map(wrap) : wrap(mesh.material);
              });
              component.sharedMaterials = component._materialMeshes.map(mesh => mesh.material);
            } else component.sharedMaterials = Array.isArray(visualMesh.material)
              ? visualMesh.material : [visualMesh.material];
            if (component instanceof SkinnedMeshRenderer) {
              visualMesh.geometry = visualMesh.geometry instanceof Mesh
                ? visualMesh.geometry : new Mesh().copy(visualMesh.geometry);
              if (visualMesh.skeleton?.boneInverses)
                visualMesh.geometry.bindposes = visualMesh.skeleton.boneInverses.map(
                  inverse => new Matrix4x4().copy(inverse));
              component.sharedMesh = visualMesh.geometry;
            }
          }
          object.GetComponent(MeshFilter)?._syncVisibleMesh();
        }
        component.assetId = record.id;
        components.set(record.id, component);
        pending.push([component, record]);
      }
      for (const child of node.children ?? []) build(child, object);
      return object;
    };
    let hydratePending;
    const materializePrefab = id => {
      if (!prefabRecords.has(id) || assets.has(id)) return assets.get(id);
      const template = build(prefabRecords.get(id).root);
      template._prefabAsset = true;
      assets.set(id, template);
      hydratePending?.();
      return template;
    };
    const getAsset = assets.get.bind(assets);
    const assertAssetReady = id => {
      for (const dependency of assetDependencyIds(manifest, id) ?? [])
        if (prefabRecords.has(dependency) && !prefabRecords.get(dependency).root)
          throw new AssetNotReadyError({ id: dependency, kind: 'prefab record' });
      for (const dependency of assetDependencyIds(manifest, id) ?? [])
        if (externalById.has(dependency) && !assets.has(dependency))
          throw new AssetNotReadyError(externalById.get(dependency));
    };
    assets.get = id => {
      if (externalById.has(id) && !assets.has(id))
        throw new AssetNotReadyError(externalById.get(id));
      if (['tmpFont', 'sprite', 'material'].includes(assetRecords.get(id)?.kind)
        && !materialized.has(id))
        materializeDerived(id);
      if (prefabRecords.has(id) && !assets.has(id)) assertAssetReady(id);
      return prefabRecords.has(id) && !assets.has(id)
        ? materializePrefab(id) : getAsset(id);
    };
    const preloadAsset = async id => {
      const dependencies = assetDependencyIds(manifest, id);
      if (!dependencies) throw new AssetFormatError(`Unknown asset: ${id}`);
      await Promise.all([...dependencies].filter(dependency =>
        prefabRecords.get(dependency)?.recordUrl && !prefabRecords.get(dependency)?.root)
        .map(dependency => fetchPrefabRecord(prefabRecords.get(dependency))));
      await Promise.all([...dependencies].filter(dependency => externalById.has(dependency))
        .map(dependency => fetchExternal(externalById.get(dependency))));
      return assets.get(id);
    };
    const roots = manifest.scene.roots.map(root => build(root));
    let hydrationOwner = null;
    const decode = (value, managedReferences) => {
      if (Array.isArray(value)) return value.map(item => decode(item, managedReferences));
      if (!value || typeof value !== 'object') return value;
      if ('$managedRef' in value) {
        const id = String(value.$managedRef);
        if (!('$serializedType' in value)) {
          if (!managedReferences?.has(id)) throw new AssetFormatError(`Unresolved managed reference: ${id}`);
          return managedReferences.get(id);
        }
        if (managedReferences?.has(id)) throw new AssetFormatError(`Duplicate managed reference: ${id}`);
      }
      if ('$ref' in value) {
        const localId = hydrationOwner && `${value.$ref}@${hydrationOwner}`;
        if (localId && (objects.has(localId) || components.has(localId)))
          return objects.get(localId) ?? components.get(localId);
        const owner = prefabOwners.get(value.$ref);
        if (owner && !assets.has(owner)) materializePrefab(owner);
        return objects.get(value.$ref) ?? components.get(value.$ref);
      }
      if ('$asset' in value) {
        const localId = hydrationOwner && `${value.$asset}@${hydrationOwner}`;
        if (localId && (objects.has(localId) || components.has(localId)))
          return objects.get(localId) ?? components.get(localId);
        return assets.get(value.$asset);
      }
      if ('m_Actions' in value && 'm_Asset' in value && 'm_Id' in value) {
        const asset = decode(value.m_Asset, managedReferences);
        const map = asset?.FindActionMap(value.m_Name)
          ?? asset?.actionMaps?.find(item => item.id === value.m_Id);
        if (map) return map;
      }
      if ('$uiAction' in value) {
        const reference = value.$uiAction;
        const asset = assets.get(reference.asset);
        const action = asset?.FindAction(reference.id) ?? asset?.FindAction(reference.name);
        if (!action) throw new AssetFormatError(`Unresolved UI action ${reference.name ?? reference.id}
          in asset ${reference.asset}`);
        return new InputActionReference(action);
      }
      if ('$inputAction' in value) return InputAction.fromSerialized(value.$inputAction);
      if ('$animationCurve' in value) {
        const curve = new AnimationCurve(...value.$animationCurve.map(key => {
          const frame = new Keyframe(key.time, key.value, key.inTangent, key.outTangent,
            key.inWeight ?? 0, key.outWeight ?? 0);
          frame.weightedMode = key.weightedMode ?? 0;
          return frame;
        }));
        curve.preWrapMode = value.preWrapMode ?? 8;
        curve.postWrapMode = value.postWrapMode ?? 8;
        return curve;
      }
      if ('$unityEvent' in value) {
        const event = new UnityEvent(Array.isArray(value.$unityEventTypes)
          ? value.$unityEventTypes : null);
        for (const call of Array.isArray(value.$unityEvent) ? value.$unityEvent : []) {
          const target = decode(call.target, managedReferences);
          if (!target) throw new AssetFormatError(`Unresolved persistent UnityEvent target: ${call.method}`);
          event._RegisterPersistentListener(target, call.method,
            call.args == null ? null : decode(call.args, managedReferences), call.state ?? 2,
            call.objectArgumentType ?? null);
        }
        return event;
      }
      if ('$layerMask' in value) return new LayerMask(value.$layerMask);
      if ('$serializedType' in value) {
        const type = this.types.get(value.$serializedType);
        if (!type) throw new AssetFormatError(`Unregistered serialized type: ${value.$serializedType}`);
        const instance = Number.isInteger(type.__unityDefaultConstructorId)
          ? new type(type.__unityDefaultConstructorId)
          : type.__unitySerializationBypassConstructor
            ? Object.create(type.prototype) : new type();
        if ('$managedRef' in value) managedReferences?.set(String(value.$managedRef), instance);
        hydrate(instance, value.properties ?? {}, managedReferences);
        return instance;
      }
      if ('$vector2' in value) return new Vector2(...value.$vector2);
      if ('$vector2Int' in value) return new Vector2Int(...value.$vector2Int);
      if ('$vector3' in value) return new Vector3(...value.$vector3);
      if ('$vector4' in value) return new Vector4(...value.$vector4);
      if ('$rect' in value) return new Rect(...value.$rect);
      if ('$quaternion' in value) return new Quaternion(...value.$quaternion);
      if ('$color' in value) return new Color(...value.$color);
      return globalThis.Object.fromEntries(globalThis.Object.entries(value)
        .map(([key, item]) => [key, decode(item, managedReferences)]));
    };
    const hydrate = (target, properties, managedReferences = new Map()) => {
      const formerNames = new Map();
      for (let type = target.constructor; typeof type === 'function';
        type = globalThis.Object.getPrototypeOf(type))
        for (const [oldName, currentName] of globalThis.Object.entries(
          globalThis.Object.hasOwn(type, '__unityFormerlySerializedFields')
            ? type.__unityFormerlySerializedFields : {}))
          if (!formerNames.has(oldName)) formerNames.set(oldName, currentName);
      for (const [serializedName, encoded] of globalThis.Object.entries(properties)) {
        const key = formerNames.get(serializedName) ?? serializedName;
        if (key !== serializedName && globalThis.Object.hasOwn(properties, key)) continue;
        const existing = target[key];
        if (Array.isArray(encoded) && (existing instanceof List
          || target.constructor.__unityListFields?.includes(key)))
          target[key] = new List(encoded.map(item => decode(item, managedReferences)));
        else if (encoded && !Array.isArray(encoded) && typeof encoded === 'object'
          && !globalThis.Object.keys(encoded).some(name => name.startsWith('$'))
          && existing && typeof existing === 'object' && !Array.isArray(existing))
          hydrate(existing, encoded, managedReferences);
        else target[key] = decode(encoded, managedReferences);
      }
      ManagedReferenceUtility._RegisterHydrated(target, managedReferences);
    };
    for (const asset of manifest.assets) {
      if (asset.kind !== 'animatorController') continue;
      for (const layer of asset.graph?.layers ?? [])
        for (const state of layer.states ?? [])
          for (const behaviour of state.behaviours ?? [])
            behaviour.properties = decode(behaviour.properties ?? {}, new Map());
    }
    for (const asset of manifest.assets) {
      if (asset.kind === 'scriptableObject') hydrate(assets.get(asset.id), asset.properties);
    }
    for (const asset of manifest.assets) {
      if (asset.kind !== 'scriptableObject') continue;
      const value = assets.get(asset.id);
      if (value instanceof ISerializationCallbackReceiver) value.OnAfterDeserialize();
    }
    let hydratedCount = 0;
    let hydrating = false;
    hydratePending = () => {
      if (hydrating) return;
      hydrating = true;
      try {
        const first = hydratedCount;
        while (hydratedCount < pending.length) {
          const [component, record] = pending[hydratedCount++];
          hydrationOwner = prefabOwners.get(record.id) ?? null;
          const managedReferences = new Map();
          const propertyEntries = globalThis.Object.entries(record.properties ?? {});
          if (component instanceof LineRenderer)
            propertyEntries.sort(([left], [right]) =>
              Number(right === 'widthMultiplier') - Number(left === 'widthMultiplier'));
          for (const [key, encoded] of propertyEntries) {
            if (component instanceof Animator && key === 'clips') {
              const value = decode(encoded, managedReferences);
              const clips = value?.animations ?? value;
              if (!Array.isArray(clips)) throw new AssetFormatError(`Animator ${record.id} clips must be an array or model asset`);
              for (const clip of clips) component.AddClip(clip);
            } else if (component instanceof SkinnedMeshRenderer && key === 'blendShapeWeights') {
              const weights = decode(encoded, managedReferences);
              if (!Array.isArray(weights) || weights.length !== component.sharedMesh?.blendShapeCount
                || !weights.every(Number.isFinite))
                throw new AssetFormatError(`SkinnedMeshRenderer ${record.id} blendShapeWeights are invalid`);
              weights.forEach((weight, index) => component.SetBlendShapeWeight(index, weight));
            } else if (component instanceof CanvasRenderer && key === 'color') {
              component.SetColor(decode(encoded, managedReferences));
            } else if (component instanceof CharacterJoint &&
              ['lowTwistLimit', 'highTwistLimit', 'swing1Limit', 'swing2Limit',
                'twistLimitSpring', 'swingLimitSpring'].includes(key)) {
              component[key] = decode(encoded, managedReferences);
            } else hydrate(component, { [key]: encoded }, managedReferences);
          }
          Object.defineProperty(component, '__unitySerializedFields', {
            value: globalThis.Object.keys(record.properties ?? {}), configurable: true
          });
          if (record.enabled !== undefined) {
            component.enabled = record.enabled;
            // Prefab components are hydrated before joining a runtime. An initially
            // disabled renderer will not receive OnDisable from reconciliation.
            if (!component.enabled && (component instanceof MeshRenderer
              || component instanceof SkinnedMeshRenderer)) component.OnDisable();
          }
          hydrationOwner = null;
          if (component instanceof Terrain) component._syncTerrainMesh();
        }
        for (let index = first; index < hydratedCount; index++) {
          const [component] = pending[index];
          if (component instanceof Animator && Array.isArray(component.exportedClips)) {
            for (const clip of component.exportedClips) if (clip instanceof THREE.AnimationClip)
              component.AddClip(clip);
            component._genericClipPlayback = component.clips.size > 0;
          }
          if (component instanceof Animator && component.controllerGraph)
            component.SetControllerGraph(component.controllerGraph,
              globalThis.Object.fromEntries(this.types));
        }
        for (let index = first; index < hydratedCount; index++) {
          const [component] = pending[index];
          if (component instanceof ISerializationCallbackReceiver)
            component.OnAfterDeserialize();
        }
      } finally { hydrating = false; }
    };
    hydratePending();
    const navBytes = manifest.scene.navMesh ? assets.get(manifest.scene.navMesh) : null;
    if (navBytes && !(navBytes instanceof Uint8Array))
      throw new AssetFormatError(`NavMesh ${manifest.scene.navMesh} must resolve to Uint8Array`);
    const activeNavRecord = manifest.assets.find(asset => asset.id === manifest.scene.navMesh);
    const navigation = navBytes ? await NavigationSystem.fromBytes(navBytes,
      { tileCache: activeNavRecord?.url?.endsWith('.tilecache') ?? false,
        offMeshConnections: activeNavRecord?.offMeshConnections ?? [] }) : null;
    const resources = new Map(globalThis.Object.entries(manifest.resources ?? {})
      .map(([path, id]) => [path, prefabRecords.has(id)
        || (startup && !startup.has(id))
        ? new LazyResource(() => assets.get(id)) : assets.get(id)]));
    if (replaceScene) {
      for (const root of [...this.runtime._objects].filter(object => !object.parent)) root.Destroy();
      this.runtime._advanceSceneIdentity();
      if (this._navigation?.runtime) this._navigation.dispose();
      for (const path of this._resourcePaths ?? []) this.runtime._resources.delete(path);
    }
    for (const [path, asset] of resources) this.runtime.registerResource(path, asset);
    const resourceIds = new Map(globalThis.Object.entries(manifest.resources ?? {})
      .map(([path, id]) => [Resources._Path(path), id]));
    const previousResourceLoad = replaceScene ? null : this.runtime._resourceAsyncLoad;
    this.runtime._resourceAsyncLoad = (path, type) => {
      const id = resourceIds.get(Resources._Path(path));
      if (!id) return previousResourceLoad?.(path, type) ?? Promise.resolve(null);
      return preloadAsset(id).then(asset => type && !(asset instanceof type) ? null : asset);
    };
    if (replaceScene) {
      for (const handle of this.runtime._loadedCloudBundles ?? []) handle._unloaded = true;
      this.runtime._loadedCloudBundles?.clear();
    }
    this.runtime._cloudAssetBundles = new Map([
      ...(replaceScene ? [] : this.runtime._cloudAssetBundles ?? []),
      ...globalThis.Object.entries(manifest.assetBundles ?? {})
      .map(([name, paths]) => [name.replaceAll('\\', '/').toLowerCase(),
        { name, paths: new Map(globalThis.Object.entries(paths)
          .map(([path, id]) => [path.replaceAll('\\', '/').toLowerCase(), id])),
          assets, preloadAsset, assertAssetReady }])]);
    if (replaceScene) this._resourcePaths = [...resources.keys()];
    if (navigation) this.runtime.addSystem(navigation);
    if (replaceScene) this._navigation = navigation;
    this.runtime.scene.name = manifest.scene.name;
    if (replaceScene) this.runtime._sceneMetadata = {
      buildIndex: manifest.scene.buildIndex ?? -1,
      path: manifest.scene.path ?? '',
    };
    if (replaceScene) this.runtime._buildSceneCount =
      manifest.scene.sceneCountInBuildSettings
      ?? Math.max(0, (manifest.scene.buildIndex ?? -1) + 1);
    if (replaceScene) this.runtime._levelLoadTime = this.runtime.time.time;
    if (replaceScene) {
      const activeRecord = this.runtime._loadedScenes.find(scene =>
        scene.handle === this.runtime._activeSceneHandle);
      if (activeRecord) {
        activeRecord.name = manifest.scene.name;
        activeRecord.metadata = this.runtime._sceneMetadata;
        activeRecord.renderSettings = manifest.renderSettings;
      }
    }
    for (const descriptor of manifest.scenes ?? []) {
      let recordPromise;
      SceneManager.RegisterScene(descriptor.name, null,
        { path: descriptor.path, buildIndex: descriptor.buildIndex,
          renderSettings: descriptor.renderSettings }, async ({ mode } = {}) => {
          recordPromise ??= Promise.resolve().then(() => this.sceneResolver(descriptor,
            new URL(descriptor.recordUrl, baseUrl).href));
          let record;
          try { record = await recordPromise; }
          catch (error) { recordPromise = null; throw new AssetFormatError(
            `Could not load scene ${descriptor.name}: ${error.message}`); }
          if (record?.name !== descriptor.name || record.path !== descriptor.path
            || record.buildIndex !== descriptor.buildIndex || !Array.isArray(record.roots)
            || JSON.stringify(record.renderSettings) !== JSON.stringify(descriptor.renderSettings))
            throw new AssetFormatError(`Invalid deferred scene record: ${descriptor.name}`);
          validateManifest({ ...manifest, scenes: undefined, scene: record }, BUNDLE_FORMAT);
          const needed = startupAssetIds({ ...manifest, scene: record },
            { includeResources: false });
          await Promise.all([...needed].map(preloadAsset));
          if (record.navMesh && mode === 1)
            throw new AssetFormatError(`Additive authored NavMesh is unsupported: ${descriptor.name}`);
          const staged = record.roots.map(root => build(root));
          hydratePending();
          const navBytes = record.navMesh ? assets.get(record.navMesh) : null;
          if (navBytes && !(navBytes instanceof Uint8Array))
            throw new AssetFormatError(`NavMesh ${record.navMesh} must resolve to Uint8Array`);
          const navRecord = manifest.assets.find(asset => asset.id === record.navMesh);
          return { roots: staged, navigation: navBytes
            ? await NavigationSystem.fromBytes(navBytes,
              { tileCache: navRecord?.url?.endsWith('.tilecache') ?? false,
                offMeshConnections: navRecord?.offMeshConnections ?? [] }) : null };
        });
    }
    if (replaceScene && manifest.scenes?.length) this.runtime._buildSceneCount =
      Math.max(this.runtime._buildSceneCount,
        ...manifest.scenes.map(scene => scene.buildIndex + 1));
    // Every scene root must be discoverable before any Awake callback runs.
    // JU's player, for example, finds the separate camera root in Awake.
    this.runtime.addMany(roots);
    navigation?._syncObstacles(0);
    return { scene: manifest.scene.name, roots, objects, components, assets,
      preloadAsset, startupAssetIds: startup ?? new Set(manifest.assets.map(asset => asset.id)),
      diagnostics: manifest.diagnostics ?? [] };
  }
}
