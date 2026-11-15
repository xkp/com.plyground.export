export const SOURCE_FORMAT = 'unity-bridge-source';
export const BUNDLE_FORMAT = 'unity-three-bundle';
export const BUNDLE_VERSION = 1;

export class AssetFormatError extends Error {
  constructor(message) { super(message); this.name = 'AssetFormatError'; }
}

function requireValue(condition, message) {
  if (!condition) throw new AssetFormatError(message);
}

export function validateManifest(manifest, format) {
  requireValue(manifest && typeof manifest === 'object', 'Manifest must be an object');
  requireValue(manifest.format === format, `Expected format ${format}`);
  requireValue(manifest.version === BUNDLE_VERSION, `Unsupported manifest version: ${manifest.version}`);
  requireValue(manifest.scene && typeof manifest.scene.name === 'string', 'Scene needs a name');
  requireValue(Array.isArray(manifest.scene.roots), 'Scene needs a roots array');
  requireValue(Array.isArray(manifest.assets), 'Manifest needs an assets array');
  if (manifest.layers !== undefined) {
    requireValue(manifest.layers && typeof manifest.layers === 'object'
      && !Array.isArray(manifest.layers), 'Manifest layers must map names to indices');
    for (const [name, index] of globalThis.Object.entries(manifest.layers))
      requireValue(name.length > 0 && Number.isInteger(index) && index >= 0 && index < 32,
        `Invalid layer index for ${name}`);
  }
  const ids = new Set();
  const assetIds = new Set();
  for (const asset of manifest.assets) {
    requireValue(typeof asset.id === 'string' && asset.id.length > 0, 'Asset needs an id');
    requireValue(!assetIds.has(asset.id), `Duplicate asset id: ${asset.id}`);
    assetIds.add(asset.id);
    requireValue(['model', 'texture', 'audio', 'navMesh', 'scriptableObject', 'prefab',
      'physicsMaterial', 'animationClip', 'animatorController'].includes(asset.kind),
      `Unsupported asset kind: ${asset.kind}`);
    if (asset.kind === 'prefab') {
      requireValue(asset.root && typeof asset.root === 'object', `Prefab ${asset.id} needs a root`);
    } else if (asset.kind === 'scriptableObject') {
      requireValue(typeof asset.type === 'string' && asset.type.length > 0,
        `ScriptableObject ${asset.id} needs a type`);
      requireValue(asset.properties && typeof asset.properties === 'object' && !Array.isArray(asset.properties),
        `ScriptableObject ${asset.id} needs properties`);
    } else if (asset.kind === 'physicsMaterial') {
      for (const key of ['dynamicFriction', 'staticFriction', 'bounciness'])
        requireValue(Number.isFinite(asset[key]) && asset[key] >= 0,
          `PhysicsMaterial ${asset.id} needs ${key}`);
    } else if (asset.kind === 'animatorController') {
      requireValue(asset.graph && Array.isArray(asset.graph.layers)
        && Array.isArray(asset.graph.parameters),
      `AnimatorController ${asset.id} needs a graph`);
    } else if (asset.kind === 'animationClip') {
      requireValue(typeof asset.name === 'string' && Number.isFinite(asset.duration)
        && asset.duration >= 0 && Array.isArray(asset.tracks),
      `AnimationClip ${asset.id} needs name, duration, and tracks`);
    } else {
      const location = format === SOURCE_FORMAT ? asset.path : asset.url;
      requireValue(typeof location === 'string' && location.length > 0, `Asset ${asset.id} needs a location`);
    }
  }
  const visit = node => {
    requireValue(node && typeof node.id === 'string' && node.id.length > 0, 'GameObject needs an id');
    requireValue(!ids.has(node.id), `Duplicate object/component id: ${node.id}`);
    ids.add(node.id);
    requireValue(typeof node.name === 'string', `GameObject ${node.id} needs a name`);
    requireValue(Array.isArray(node.components ?? []), `GameObject ${node.id} needs components array`);
    requireValue(Array.isArray(node.children ?? []), `GameObject ${node.id} needs children array`);
    const transform = node.transform ?? {};
    if (transform.id !== undefined) {
      requireValue(typeof transform.id === 'string' && transform.id.length > 0,
        `GameObject ${node.id} has invalid transform id`);
      requireValue(!ids.has(transform.id), `Duplicate object/component id: ${transform.id}`);
      ids.add(transform.id);
    }
    for (const [key, size] of [['position', 3], ['rotation', 4], ['scale', 3]]) {
      const value = transform[key];
      requireValue(Array.isArray(value) && value.length === size && value.every(Number.isFinite),
        `GameObject ${node.id} has invalid transform.${key}`);
    }
    if (node.rectTransform !== undefined) {
      requireValue(node.rectTransform && typeof node.rectTransform === 'object',
        `GameObject ${node.id} needs rectTransform data`);
      for (const key of ['anchorMin', 'anchorMax', 'pivot', 'sizeDelta'])
        requireValue(Array.isArray(node.rectTransform[key]) && node.rectTransform[key].length === 2
          && node.rectTransform[key].every(Number.isFinite),
        `GameObject ${node.id} has invalid rectTransform.${key}`);
      if (node.rectTransform.anchoredPosition !== undefined)
        requireValue(Array.isArray(node.rectTransform.anchoredPosition)
          && node.rectTransform.anchoredPosition.length === 2
          && node.rectTransform.anchoredPosition.every(Number.isFinite),
        `GameObject ${node.id} has invalid rectTransform.anchoredPosition`);
    }
    for (const component of node.components ?? []) {
      requireValue(component && typeof component.id === 'string' && component.id.length > 0,
        `Component on ${node.id} needs an id`);
      requireValue(!ids.has(component.id), `Duplicate object/component id: ${component.id}`);
      ids.add(component.id);
      requireValue(typeof component.type === 'string' && component.type.length > 0,
        `Component ${component.id} needs a type`);
      requireValue(component.properties === undefined || (component.properties && typeof component.properties === 'object'),
        `Component ${component.id} properties must be an object`);
    }
    if (node.model !== undefined) requireValue(assetIds.has(node.model), `Unknown model asset: ${node.model}`);
    if (node.semanticSurface !== undefined) requireValue(
      typeof node.semanticSurface === 'string' && node.semanticSurface.length > 0,
      `GameObject ${node.id} has invalid semanticSurface`);
    for (const child of node.children ?? []) visit(child);
  };
  for (const root of manifest.scene.roots) visit(root);
  for (const asset of manifest.assets) if (asset.kind === 'prefab') visit(asset.root);
  if (manifest.scene.navMesh !== undefined) {
    requireValue(typeof manifest.scene.navMesh === 'string'
      && manifest.assets.some(asset => asset.id === manifest.scene.navMesh && asset.kind === 'navMesh'),
    `Unknown NavMesh asset: ${manifest.scene.navMesh}`);
  }
  if (manifest.resources !== undefined) {
    requireValue(manifest.resources && typeof manifest.resources === 'object'
      && !Array.isArray(manifest.resources), 'Resources must map paths to asset IDs');
    for (const [path, id] of globalThis.Object.entries(manifest.resources))
      requireValue(path.length > 0 && assetIds.has(id), `Invalid Resources mapping: ${path}`);
  }
  const inspect = value => {
    if (!value || typeof value !== 'object') return;
    if (typeof value.$ref === 'string') requireValue(ids.has(value.$ref), `Unknown object reference: ${value.$ref}`);
    if (typeof value.$asset === 'string') requireValue(assetIds.has(value.$asset), `Unknown asset reference: ${value.$asset}`);
    for (const item of globalThis.Object.values(value)) inspect(item);
  };
  for (const root of manifest.scene.roots) inspect(root);
  for (const asset of manifest.assets) {
    if (asset.kind === 'scriptableObject') inspect(asset.properties);
    if (asset.kind === 'prefab') inspect(asset.root);
  }
  return manifest;
}
