const referenceKeys = new Set([
  '$asset', 'asset', 'model', 'navMesh', 'texture', 'textureAssetId',
  'atlasTextureAssetId', 'assetId',
]);

function prefabOwners(manifest) {
  const owners = new Map();
  const index = (node, id) => {
    owners.set(node.id, id);
    if (node.transform?.id) owners.set(node.transform.id, id);
    for (const component of node.components ?? []) owners.set(component.id, id);
    for (const child of node.children ?? []) index(child, id);
  };
  for (const asset of manifest.assets) if (asset.kind === 'prefab' && asset.root)
    index(asset.root, asset.id);
  return owners;
}

function referencedIds(value, known, owners, result) {
  if (!value || typeof value !== 'object') return;
  if (Array.isArray(value)) {
    for (const item of value) referencedIds(item, known, owners, result);
    return;
  }
  for (const [key, child] of Object.entries(value)) {
    if (referenceKeys.has(key) && typeof child === 'string' && known.has(child))
      result.add(child);
    if (key === '$ref' && typeof child === 'string' && owners.has(child))
      result.add(owners.get(child));
    if (['fallbackFontAssetIds', 'dependencies'].includes(key) && Array.isArray(child))
      for (const id of child) if (known.has(id)) result.add(id);
    referencedIds(child, known, owners, result);
  }
}

/** Asset IDs needed before the active scene's first Unity callback. */
export function startupAssetIds(manifest, { includeResources = true } = {}) {
  const records = new Map(manifest.assets.map(asset => [asset.id, asset]));
  const owners = prefabOwners(manifest);
  const selected = new Set();
  const queue = [];
  const enqueue = id => {
    if (!records.has(id) || selected.has(id)) return;
    selected.add(id);
    queue.push(id);
  };
  const initial = new Set();
  referencedIds(manifest.scene, records, owners, initial);
  if (includeResources)
    for (const id of Object.values(manifest.resources ?? {})) initial.add(id);
  // The current loader hydrates every ScriptableObject before scene callbacks.
  for (const record of manifest.assets) if (record.kind === 'scriptableObject')
    initial.add(record.id);
  for (const id of initial) enqueue(id);
  while (queue.length) {
    const record = records.get(queue.shift());
    const dependencies = new Set();
    referencedIds(record, records, owners, dependencies);
    for (const id of dependencies) enqueue(id);
  }
  return selected;
}

/** Transitive dependencies of an explicitly requested prepared asset. */
export function assetDependencyIds(manifest, id) {
  const records = new Map(manifest.assets.map(asset => [asset.id, asset]));
  const owners = prefabOwners(manifest);
  if (!records.has(id)) return null;
  const selected = new Set([id]);
  const queue = [id];
  while (queue.length) {
    const dependencies = new Set();
    referencedIds(records.get(queue.shift()), records, owners, dependencies);
    for (const dependency of dependencies) if (!selected.has(dependency)) {
      selected.add(dependency);
      queue.push(dependency);
    }
  }
  return selected;
}
