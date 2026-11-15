import * as THREE from 'three';
import { GLTFLoader } from 'three/addons/loaders/GLTFLoader.js';
import { FBXLoader } from 'three/addons/loaders/FBXLoader.js';
import { clone as cloneSkeleton } from 'three/addons/utils/SkeletonUtils.js';
import { List } from './collections.js';
import {
  Animator, AudioSource, BoxCollider, MeshCollider, Button, Camera, Canvas, CapsuleCollider, CharacterController,
  GameObject, LazyResource, Light, Material, MeshFilter, MeshRenderer, Rigidbody, SkinnedMeshRenderer, SpriteRenderer, SphereCollider, WheelCollider,
  ParticleSystem, NavMeshAgent, NavigationSystem, RectTransform, Transform, Image, Text,
  Vector2, Vector3, Quaternion, Color, AnimationCurve, Keyframe, InputAction, LayerMask,
  ScriptableObject, PhysicsMaterial, UnityEvent, UniversalAdditionalCameraData,
  CanvasScaler, GraphicRaycaster, EventSystem, StandaloneInputModule, Mask, Shadow, Outline,
  EventTrigger, ScrollRect, GridLayoutGroup, Scrollbar, VerticalLayoutGroup,
  HorizontalLayoutGroup, Slider, Toggle, Dropdown,
} from './index.js';
import { AssetFormatError, BUNDLE_FORMAT, validateManifest } from './asset-schema.js';

export class UnknownComponentError extends AssetFormatError {
  constructor(type, object = null) {
    super(`Unregistered Unity component: ${type}${object ? ` on ${object.name} (${object.assetId})` : ''}`);
    this.name = 'UnknownComponentError';
  }
}

/** Reconstructs Unity-named objects from a preprocessed bundle. */
export class AssetBundleLoader {
  constructor(runtime, { assetResolver, assetConcurrency = 1,
    components = {}, scriptableObjects = {}, types = {} } = {}) {
    this.runtime = runtime;
    this.assetResolver = assetResolver ?? this._defaultResolver.bind(this);
    this.assetConcurrency = Math.max(1, Math.min(8, Math.trunc(assetConcurrency) || 1));
    this.components = new Map();
    this.scriptableObjects = new Map();
    this.types = new Map(globalThis.Object.entries(types));
    const builtins = {
      Animator, AudioSource, BoxCollider, MeshCollider, Button, Camera, Canvas, CapsuleCollider, CharacterController,
      Light, MeshFilter, MeshRenderer, Rigidbody, SkinnedMeshRenderer, SpriteRenderer, SphereCollider, WheelCollider,
      ParticleSystem, NavMeshAgent, Image, Text,
    };
    for (const [name, type] of globalThis.Object.entries(builtins)) {
      this.registerComponent(name, type);
      this.registerComponent(`UnityEngine.${name}`, type);
      if (name === 'NavMeshAgent') this.registerComponent('UnityEngine.AI.NavMeshAgent', type);
      if (['Button', 'Image', 'Text'].includes(name)) this.registerComponent(`UnityEngine.UI.${name}`, type);
    }
    const presentation = {
      'UnityEngine.Rendering.Universal.UniversalAdditionalCameraData': UniversalAdditionalCameraData,
      'UnityEngine.UI.CanvasScaler': CanvasScaler,
      'UnityEngine.UI.GraphicRaycaster': GraphicRaycaster,
      'UnityEngine.EventSystems.EventSystem': EventSystem,
      'UnityEngine.EventSystems.StandaloneInputModule': StandaloneInputModule,
      'UnityEngine.UI.Mask': Mask,
      'UnityEngine.UI.Shadow': Shadow,
      'UnityEngine.UI.Outline': Outline,
      'UnityEngine.EventSystems.EventTrigger': EventTrigger,
      'UnityEngine.UI.ScrollRect': ScrollRect,
      'UnityEngine.UI.GridLayoutGroup': GridLayoutGroup,
      'UnityEngine.UI.Scrollbar': Scrollbar,
      'UnityEngine.UI.VerticalLayoutGroup': VerticalLayoutGroup,
      'UnityEngine.UI.HorizontalLayoutGroup': HorizontalLayoutGroup,
      'UnityEngine.UI.Slider': Slider,
      'UnityEngine.UI.Toggle': Toggle,
      'UnityEngine.UI.Dropdown': Dropdown,
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
    if (asset.kind === 'navMesh') {
      const response = await fetch(url);
      if (!response.ok) throw new Error(`Could not fetch NavMesh: ${response.status}`);
      return new Uint8Array(await response.arrayBuffer());
    }
    if (asset.kind === 'model') return /\.fbx(?:[?#]|$)/i.test(url)
      ? new FBXLoader().loadAsync(url) : new GLTFLoader().loadAsync(url);
    if (asset.kind === 'texture') {
      const texture = await new THREE.TextureLoader().loadAsync(url);
      texture.name = asset.name ?? '';
      texture.colorSpace = THREE.SRGBColorSpace;
      texture.flipY = false;
      texture.needsUpdate = true;
      return texture;
    }
    if (asset.kind === 'audio') return new THREE.AudioLoader().loadAsync(url);
    throw new AssetFormatError(`Unsupported asset kind: ${asset.kind}`);
  }

  async load(manifestOrUrl, { baseUrl, replaceScene = true } = {}) {
    let manifest = manifestOrUrl;
    if (typeof manifestOrUrl === 'string') {
      const response = await fetch(manifestOrUrl);
      if (!response.ok) throw new Error(`Could not fetch bundle: ${response.status}`);
      manifest = await response.json();
      baseUrl ??= new URL('.', response.url).href;
    }
    validateManifest(manifest, BUNDLE_FORMAT);
    if (manifest.coordinateSystem !== 'unity') throw new AssetFormatError('Bundle coordinateSystem must be unity');
    if (manifest.layers) LayerMask.Configure(manifest.layers);
    baseUrl ??= globalThis.location?.href ?? 'file:///';

    const assets = new Map();
    for (const asset of manifest.assets) {
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
      if (asset.kind !== 'scriptableObject') continue;
      const type = this.scriptableObjects.get(asset.type);
      if (!type) throw new AssetFormatError(`Unregistered ScriptableObject: ${asset.type}`);
      const value = ScriptableObject.CreateInstance(type);
      value.name = asset.name ?? '';
      value.assetId = asset.id;
      assets.set(asset.id, value);
    }
    const external = manifest.assets.filter(asset => !['scriptableObject', 'prefab',
      'physicsMaterial', 'animationClip', 'animatorController'].includes(asset.kind));
    const resolved = new Array(external.length);
    let nextAsset = 0;
    await Promise.all(Array.from({ length: Math.min(this.assetConcurrency, external.length) }, async () => {
      while (nextAsset < external.length) {
        const index = nextAsset++;
        const asset = external[index];
        const url = new URL(asset.url, baseUrl).href;
        const value = await this.assetResolver(asset, url);
        if (value == null) throw new AssetFormatError(`Resolver returned no asset: ${asset.id}`);
        if (typeof value === 'object' && value.assetId == null) value.assetId = asset.id;
        resolved[index] = value;
      }
    }));
    for (let index = 0; index < external.length; index++) assets.set(external[index].id, resolved[index]);

    const prefabRecords = new Map(manifest.assets.filter(asset => asset.kind === 'prefab')
      .map(asset => [asset.id, asset]));
    const prefabOwners = new Map();
    const indexPrefabNode = (node, prefabId) => {
      prefabOwners.set(node.id, prefabId);
      if (node.transform.id) prefabOwners.set(node.transform.id, prefabId);
      for (const component of node.components ?? []) prefabOwners.set(component.id, prefabId);
      for (const child of node.children ?? []) indexPrefabNode(child, prefabId);
    };
    for (const asset of prefabRecords.values()) indexPrefabNode(asset.root, asset.id);
    const objects = new Map();
    const components = new Map();
    const pending = [];
    const build = (node, parent = null) => {
      const object = new GameObject(node.name, node.rectTransform ? RectTransform : Transform);
      object.assetId = node.id;
      if (node.semanticSurface !== undefined) object.semanticSurface = node.semanticSurface;
      object.tag = node.tag ?? 'Untagged';
      object.layer = node.layer ?? 0;
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
        if (rect.anchoredPosition) object.transform.anchoredPosition = new Vector2(...rect.anchoredPosition);
      }
      if (node.transform.id) {
        object.transform.assetId = node.transform.id;
        components.set(node.transform.id, object.transform);
      }
      if (parent) object.SetParent(parent, false);
      objects.set(node.id, object);
      let visual = null;
      let visualMesh = null;
      if (node.model) {
        const model = assets.get(node.model);
        const scene = model?.scene ?? model;
        if (!scene?.isObject3D) throw new AssetFormatError(`Model ${node.model} did not provide a Three.js scene`);
        visual = cloneSkeleton(scene);
        // Unity's glTF importer reflects the source mesh across X. Bob item
        // positions are already in Unity coordinates, so mirror its geometry.
        if (node.mirrorX) visual.scale.x *= -1;
        if (node.semanticSurface) visual.visible = false;
        visual.traverse(child => { if (!visualMesh && child.isMesh) visualMesh = child; });
        if (visualMesh && node.materialOverride) {
          const override = node.materialOverride;
          const texture = assets.get(override.texture);
          if (!texture?.isTexture) throw new AssetFormatError(`Missing material texture: ${override.texture}`);
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
        const type = this.components.get(record.type);
        if (!type) throw new UnknownComponentError(record.type, object);
        let component;
        try {
          component = object.AddComponent(type);
        } catch (error) {
          throw new AssetFormatError(`Failed to construct ${record.type} on ${object.name}: ${error.message}`);
        }
        if (component instanceof MeshFilter && visualMesh) component.sharedMesh = visualMesh.geometry;
        if (component instanceof MeshCollider) component.meshRoot = visual;
        if (component instanceof MeshRenderer || component instanceof SkinnedMeshRenderer) {
          component.threeMesh = visualMesh;
          component._renderRoot = visual;
          if (visualMesh) {
            component.sharedMaterial = visualMesh.material;
            if (component instanceof SkinnedMeshRenderer) component.sharedMesh = visualMesh.geometry;
          }
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
    assets.get = id => prefabRecords.has(id) && !assets.has(id)
      ? materializePrefab(id) : getAsset(id);
    const roots = manifest.scene.roots.map(root => build(root));
    const decode = value => {
      if (Array.isArray(value)) return value.map(decode);
      if (!value || typeof value !== 'object') return value;
      if ('$ref' in value) {
        const owner = prefabOwners.get(value.$ref);
        if (owner && !assets.has(owner)) materializePrefab(owner);
        return objects.get(value.$ref) ?? components.get(value.$ref);
      }
      if ('$asset' in value) return assets.get(value.$asset);
      if ('$inputAction' in value) return InputAction.fromSerialized(value.$inputAction);
      if ('$animationCurve' in value) return new AnimationCurve(...value.$animationCurve.map(key =>
        new Keyframe(key.time, key.value, key.inTangent, key.outTangent)));
      if ('$unityEvent' in value) {
        const event = new UnityEvent();
        for (const call of Array.isArray(value.$unityEvent) ? value.$unityEvent : []) {
          const target = decode(call.target);
          if (!target) throw new AssetFormatError(`Unresolved persistent UnityEvent target: ${call.method}`);
          event._RegisterPersistentListener(target, call.method, call.args == null ? null : decode(call.args));
        }
        return event;
      }
      if ('$layerMask' in value) return new LayerMask(value.$layerMask);
      if ('$serializedType' in value) {
        const type = this.types.get(value.$serializedType);
        if (!type) throw new AssetFormatError(`Unregistered serialized type: ${value.$serializedType}`);
        const instance = Number.isInteger(type.__unityDefaultConstructorId)
          ? new type(type.__unityDefaultConstructorId) : new type();
        hydrate(instance, value.properties ?? {});
        return instance;
      }
      if ('$vector2' in value) return new Vector2(...value.$vector2);
      if ('$vector3' in value) return new Vector3(...value.$vector3);
      if ('$quaternion' in value) return new Quaternion(...value.$quaternion);
      if ('$color' in value) return new Color(...value.$color);
      return globalThis.Object.fromEntries(globalThis.Object.entries(value).map(([key, item]) => [key, decode(item)]));
    };
    const hydrate = (target, properties) => {
      for (const [key, encoded] of globalThis.Object.entries(properties)) {
        const existing = target[key];
        if (Array.isArray(encoded) && (existing instanceof List
          || target.constructor.__unityListFields?.includes(key)))
          target[key] = new List(encoded.map(decode));
        else if (encoded && !Array.isArray(encoded) && typeof encoded === 'object'
          && !globalThis.Object.keys(encoded).some(name => name.startsWith('$'))
          && existing && typeof existing === 'object' && !Array.isArray(existing))
          hydrate(existing, encoded);
        else target[key] = decode(encoded);
      }
    };
    for (const asset of manifest.assets) {
      if (asset.kind === 'scriptableObject') hydrate(assets.get(asset.id), asset.properties);
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
          for (const [key, encoded] of globalThis.Object.entries(record.properties ?? {})) {
            if (component instanceof Animator && key === 'clips') {
              const value = decode(encoded);
              const clips = value?.animations ?? value;
              if (!Array.isArray(clips)) throw new AssetFormatError(`Animator ${record.id} clips must be an array or model asset`);
              for (const clip of clips) component.AddClip(clip);
            } else hydrate(component, { [key]: encoded });
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
        }
        for (let index = first; index < hydratedCount; index++) {
          const [component] = pending[index];
          if (component instanceof Animator && component.controllerGraph)
            component.SetControllerGraph(component.controllerGraph,
              globalThis.Object.fromEntries(this.types));
        }
      } finally { hydrating = false; }
    };
    hydratePending();
    const navBytes = manifest.scene.navMesh ? assets.get(manifest.scene.navMesh) : null;
    if (navBytes && !(navBytes instanceof Uint8Array))
      throw new AssetFormatError(`NavMesh ${manifest.scene.navMesh} must resolve to Uint8Array`);
    const navigation = navBytes ? await NavigationSystem.fromBytes(navBytes) : null;
    const resources = new Map(globalThis.Object.entries(manifest.resources ?? {})
      .map(([path, id]) => [path, prefabRecords.has(id)
        ? new LazyResource(() => assets.get(id)) : assets.get(id)]));
    if (replaceScene) {
      for (const root of [...this.runtime._objects].filter(object => !object.parent)) root.Destroy();
      this._navigation?.dispose();
      for (const path of this._resourcePaths ?? []) this.runtime._resources.delete(path);
    }
    for (const [path, asset] of resources) this.runtime.registerResource(path, asset);
    if (replaceScene) this._resourcePaths = [...resources.keys()];
    if (navigation) this.runtime.addSystem(navigation);
    if (replaceScene) this._navigation = navigation;
    this.runtime.scene.name = manifest.scene.name;
    // Every scene root must be discoverable before any Awake callback runs.
    // JU's player, for example, finds the separate camera root in Awake.
    this.runtime.addMany(roots);
    return { scene: manifest.scene.name, roots, objects, components, assets,
      diagnostics: manifest.diagnostics ?? [] };
  }
}
