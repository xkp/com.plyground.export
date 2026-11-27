import { readFile, stat, writeFile } from 'node:fs/promises';
import { isAbsolute, relative, resolve, sep } from 'node:path';
import { pathToFileURL } from 'node:url';
import { AssetBundleLoader, Component, PhysicsSystem, PlygroundItem, ScriptableObject, THREE, UnityRuntime } from './index.js';
import { headlessPreviewTexture, importPreparedModule } from './preview-scene.js';

/** Load a built browser Preview under Node and run deterministic simulation frames. */
export async function validatePreview(outputRoot, { frames = 3 } = {}) {
  if (!Number.isInteger(frames) || frames < 0 || frames > 10000)
    throw new Error('Preview frame count must be between 0 and 10000');
  const root = resolve(outputRoot);
  const bundle = JSON.parse(await readFile(resolve(root, 'bundle.json'), 'utf8'));
  const modules = JSON.parse(await readFile(resolve(root, 'modules.json'), 'utf8'));
  const outputMember = name => {
    const file = resolve(root, name);
    const member = relative(root, file);
    if (!member || member === '..' || member.startsWith(`..${sep}`) || isAbsolute(member))
      throw new Error(`Preview path escapes output: ${name}`);
    return file;
  };
  for (const asset of bundle.assets ?? []) {
    if (!asset.url) continue;
    if (!(await stat(outputMember(asset.url)).catch(() => null))?.isFile())
      throw new Error(`Preview asset is missing: ${asset.url}`);
  }
  const types = { 'Plyground.Pipeline.Runtime.PlygroundItem': PlygroundItem };
  for (const record of modules) {
    const file = outputMember(record.codeEntry);
    const imported = await import(await importPreparedModule(file, root));
    if (!imported.unityTypes || typeof imported.unityTypes !== 'object')
      throw new Error(`Preview module ${record.moduleId} has no unityTypes registry`);
    for (const [name, type] of Object.entries(imported.unityTypes)) {
      if (Object.hasOwn(types, name)) throw new Error(`Duplicate Preview type: ${name}`);
      types[name] = type;
    }
  }
  const components = Object.fromEntries(Object.entries(types)
    .filter(([, type]) => typeof type === 'function' && type.prototype instanceof Component));
  const scriptableObjects = Object.fromEntries(Object.entries(types)
    .filter(([, type]) => typeof type === 'function' && type.prototype instanceof ScriptableObject));
  const runtime = new UnityRuntime();
  await PhysicsSystem.create(runtime);
  const loaded = await new AssetBundleLoader(runtime, {
    components, scriptableObjects, types,
    assetResolver: async asset => {
      if (asset.kind === 'model') return new THREE.Group();
      if (asset.kind === 'texture') return headlessPreviewTexture(asset, root, runtime);
      if (asset.kind === 'audio') return new ArrayBuffer(0);
      if (asset.kind === 'navMesh') throw new Error('Headless Preview navMesh validation needs an asset decoder');
      throw new Error(`Unsupported Preview asset kind: ${asset.kind}`);
    }
  }).load(bundle);
  for (let frame = 0; frame < frames; frame++) runtime.step(1 / 60);
  return { format: 'plyground.preview.validation/v1', status: 'succeeded',
    scene: loaded.scene, objects: loaded.objects.size, components: loaded.components.size,
    modules: modules.map(record => record.moduleId), frames };
}

if (process.argv[1] && import.meta.url === pathToFileURL(resolve(process.argv[1])).href) {
  const root = process.argv[2];
  if (!root) {
    process.stderr.write('Usage: node src/preview-validate.js <built-preview-directory> [frames] [report.json]\n');
    process.exitCode = 2;
  } else {
    const reportPath = process.argv[4];
    validatePreview(root, { frames: process.argv[3] == null ? 3 : Number(process.argv[3]) })
      .then(async report => {
        if (reportPath) await writeFile(reportPath, `${JSON.stringify(report, null, 2)}\n`);
        process.stdout.write(`${JSON.stringify(report)}\n`);
      })
      .catch(async error => {
        const report = { format: 'plyground.preview.validation/v1', status: 'failed',
          error: error.message };
        if (reportPath) await writeFile(reportPath, `${JSON.stringify(report, null, 2)}\n`);
        process.stderr.write(`${error.stack}\n`);
        process.exitCode = 1;
      });
  }
}
