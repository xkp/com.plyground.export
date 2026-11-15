import { readFile, writeFile } from 'node:fs/promises';
import { resolve, join } from 'node:path';
import { pathToFileURL } from 'node:url';
import { preprocess } from './preprocess.js';

function scopePrefabObjectIds(source) {
  for (const asset of source.assets.filter(value => value.kind === 'prefab')) {
    const refs = new Map();
    const visit = (node, path) => {
      const oldId = node.id;
      node.id = `${asset.id}:node:${path}`;
      if (!refs.has(oldId)) refs.set(oldId, node.id);
      if (node.transform?.id) {
        const oldTransform = node.transform.id;
        node.transform.id = `${node.id}:transform`;
        if (!refs.has(oldTransform)) refs.set(oldTransform, node.transform.id);
      }
      for (let index = 0; index < (node.components ?? []).length; index++) {
        const component = node.components[index];
        const oldComponent = component.id;
        component.id = `${node.id}:component:${index}`;
        if (!refs.has(oldComponent)) refs.set(oldComponent, component.id);
      }
      node.children?.forEach((child, index) => visit(child, `${path}/${index}`));
    };
    visit(asset.root, '0');
    const rewriteRefs = value => {
      if (!value || typeof value !== 'object') return;
      if (typeof value.$ref === 'string' && refs.has(value.$ref)) value.$ref = refs.get(value.$ref);
      for (const child of Object.values(value)) rewriteRefs(child);
    };
    rewriteRefs(asset.root);
  }
}

/** Convert Unity-exported module prefabs and retain the item-template mapping. */
export async function preprocessPreviewModuleAssets(sourcePath, outputDirectory, itemMapPath) {
  const source = JSON.parse((await readFile(sourcePath, 'utf8')).replace(/^\uFEFF/, ''));
  if (source.format !== 'unity-bridge-source' || !Array.isArray(source.assets))
    throw new Error('Invalid Unity Preview asset source');
  const allowApproximations = process.env.PLYGROUND_PREVIEW_ALLOW_APPROXIMATIONS === '1';
  // Unity reports both visual approximations and missing data in diagnostics.
  // Preserve them for validation without treating every skipped particle curve or
  // shader channel as a reason to discard the entire module package. Structural
  // bundle and reference checks below still fail independently.
  if (allowApproximations) {
    const assetIds = new Set(source.assets.map(asset => asset.id));
    for (const [kind, mapping] of [['AssetDatabase', source.assetPaths], ['Resources', source.resources]]) {
      for (const [path, id] of Object.entries(mapping ?? {})) {
        if (assetIds.has(id)) continue;
        delete mapping[path];
        (source.diagnostics ??= []).push(`${kind} asset has no Preview representation: ${path}`);
      }
    }
  }
  scopePrefabObjectIds(source);
  await writeFile(sourcePath, `${JSON.stringify(source)}\n`);
  const itemMap = JSON.parse((await readFile(itemMapPath, 'utf8')).replace(/^\uFEFF/, ''));
  if (itemMap.format !== 'plyground.preview.item-prefabs/v1' || !Array.isArray(itemMap.items))
    throw new Error('Invalid Preview item-to-prefab map');
  const prefabIds = new Set(source.assets.filter(asset => asset.kind === 'prefab').map(asset => asset.id));
  const itemPrefabs = {};
  const itemTemplates = {};
  for (const entry of itemMap.items) {
    if (typeof entry?.id !== 'string' || !entry.id || Object.hasOwn(itemTemplates, entry.id)
      || (entry.prefabId && !prefabIds.has(entry.prefabId)))
      throw new Error(`Invalid Preview prefab mapping for item ${entry?.id ?? '(missing)'}`);
    itemTemplates[entry.id] = { id: entry.id, name: entry.name ?? '',
      description: entry.description ?? '', prefab: entry.prefab ?? '',
      icon: entry.icon ?? '', icon3d: entry.icon3d ?? '',
      unique: entry.unique ?? false, notDraggable: entry.notDraggable ?? false,
      template: entry.template ?? false };
    if (entry.prefabId) itemPrefabs[entry.id] = entry.prefabId;
  }
  const bundle = await preprocess(sourcePath, outputDirectory);
  const assetIds = new Set(bundle.assets.map(asset => asset.id));
  const assetPaths = source.assetPaths ?? {};
  for (const [path, id] of Object.entries(assetPaths)) {
    if (!path.startsWith('Assets/') || !assetIds.has(id))
      throw new Error(`Invalid Preview AssetDatabase mapping: ${path} -> ${id}`);
  }
  bundle.assetPaths = assetPaths;
  bundle.itemPrefabs = itemPrefabs;
  bundle.itemTemplates = itemTemplates;
  if (source.diagnostics?.length)
    bundle.previewDiagnostics = source.diagnostics;
  if (allowApproximations) bundle.allowRuntimeApproximation = true;
  await writeFile(join(outputDirectory, 'bundle.json'), `${JSON.stringify(bundle, null, 2)}\n`);
  return bundle;
}

if (process.argv[1] && import.meta.url === pathToFileURL(resolve(process.argv[1])).href) {
  if (process.argv.length !== 5) {
    process.stderr.write('Usage: node src/preview-module-assets.js <source.json> <output-directory> <item-map.json>\n');
    process.exitCode = 2;
  } else {
    preprocessPreviewModuleAssets(process.argv[2], process.argv[3], process.argv[4])
      .then(bundle => process.stdout.write(`Prepared ${Object.keys(bundle.itemPrefabs).length} Preview item prefabs\n`))
      .catch(error => { process.stderr.write(`${error.stack}\n`); process.exitCode = 1; });
  }
}
