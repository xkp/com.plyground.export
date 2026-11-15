import { access, cp, link, lstat, mkdir, readFile, readdir, rm, writeFile } from 'node:fs/promises';
import { dirname, isAbsolute, join, relative, resolve, sep } from 'node:path';
import { fileURLToPath, pathToFileURL } from 'node:url';
import { gunzipSync } from 'node:zlib';
import { assemblePreviewScene } from './preview-scene.js';
import { applyVariationBindings, transpileVariationSources } from './preview-variation.js';
import { BUNDLE_FORMAT, validateManifest } from './asset-schema.js';
import { assertPreviewApiCoverage, inspectPreviewApi } from './preview-api-coverage.js';

const runtimeRoot = resolve(dirname(fileURLToPath(import.meta.url)), '..');
const compatibleAnimations = new Map([
  ['4FBE2152-E535-47DD-99EF-99F85B524202', {
    revision: 'sha256:2b7f80dc14c34bc0dba258e8d262e8811e51071f90116d2e86558d20cf799451',
    source: 'compat/animation/ju-tps-2b7f80dc14c3-redmono.json.gz'
  }]
]);
const importMap = {
  '@plythreejs/runtime': './runtime/src/index.js',
  three: './runtime/node_modules/three/build/three.module.js',
  'three/addons/': './runtime/node_modules/three/examples/jsm/',
  '@dimforge/rapier3d-compat': './runtime/node_modules/@dimforge/rapier3d-compat/dist/rapier.mjs',
  'recast-navigation': './runtime/node_modules/recast-navigation/index.mjs',
  '@recast-navigation/core': './runtime/node_modules/@recast-navigation/core/dist/index.mjs',
  '@recast-navigation/generators': './runtime/node_modules/@recast-navigation/generators/dist/index.mjs',
  '@recast-navigation/three': './runtime/node_modules/@recast-navigation/three/dist/index.mjs',
  '@recast-navigation/wasm': './runtime/node_modules/@recast-navigation/wasm/dist/recast-navigation.wasm-compat.js'
};

function inside(root, name) {
  if (typeof name !== 'string' || !name || isAbsolute(name) || /^[A-Za-z]:/.test(name)
    || name.replaceAll('\\', '/').split('/').some(part => !part || part === '.' || part === '..'))
    throw new Error(`Invalid preview build path: ${name}`);
  const file = resolve(root, name);
  const rel = relative(root, file);
  if (rel === '..' || rel.startsWith(`..${sep}`) || isAbsolute(rel)) throw new Error(`Preview path escapes root: ${name}`);
  return file;
}

async function copyTree(source, destination, include = () => true, linkFiles = false) {
  if (!include(source)) return;
  const entry = await lstat(source);
  if (entry.isSymbolicLink()) throw new Error(`Preview input contains a symbolic link: ${source}`);
  if (entry.isDirectory()) {
    await mkdir(destination, { recursive: true });
    for (const name of await readdir(source))
      await copyTree(join(source, name), join(destination, name), include, linkFiles);
  } else if (entry.isFile()) {
    await mkdir(dirname(destination), { recursive: true });
    if (linkFiles) {
      try { await link(source, destination); }
      catch (error) {
        if (!['EXDEV', 'EPERM', 'ENOTSUP'].includes(error.code)) throw error;
        await cp(source, destination);
      }
    } else await cp(source, destination);
  }
}

async function listFiles(root, folder = root) {
  const files = [];
  for (const entry of await readdir(folder, { withFileTypes: true })) {
    const file = join(folder, entry.name);
    if (entry.isDirectory()) files.push(...await listFiles(root, file));
    else if (entry.isFile()) files.push({ path: relative(root, file).replaceAll('\\', '/') });
  }
  return files;
}

/** Attach revision-matched animation exports missing from older module sidecars. */
export async function installCompatiblePreviewAnimations(outputRoot, modules, diagnostics) {
  const coveredClips = new Set();
  for (const module of modules) {
    if (module.animationEntry) continue;
    const fallback = compatibleAnimations.get(module.moduleId);
    if (!fallback || fallback.revision !== module.revision) continue;
    const source = join(runtimeRoot, fallback.source);
    try { await access(source); }
    catch (error) { if (error.code === 'ENOENT') continue; throw error; }
    const data = JSON.parse(gunzipSync(await readFile(source)).toString('utf8'));
    if (data.format !== 'plyground.preview.humanoid-controllers/v1'
      || !Array.isArray(data.controllers)) throw new Error(`Invalid compatible animation export: ${source}`);
    const entry = `animation/compat/${module.moduleId}.json`;
    await mkdir(dirname(inside(outputRoot, entry)), { recursive: true });
    await writeFile(inside(outputRoot, entry), `${JSON.stringify(data)}\n`);
    module.animationEntry = entry;
    for (const controller of data.controllers)
      for (const clip of controller.clips ?? []) coveredClips.add(clip.name?.toLowerCase());
  }
  return diagnostics.filter(diagnostic => diagnostic.phase !== 'animation'
    || !diagnostic.missingClips?.every(name => coveredClips.has(name.toLowerCase())));
}

/** Supply the JU RedMono pose samples when an older module export has only JU prefab samples. */
export async function installRedMonoPreviewAnimations(outputRoot, bundle, modules, diagnostics) {
  if (!modules.some(module => module.moduleId === '4FBE2152-E535-47DD-99EF-99F85B524202'
    && module.animationEntry)) return;
  const fallback = compatibleAnimations.get('4FBE2152-E535-47DD-99EF-99F85B524202');
  const source = join(runtimeRoot, fallback.source);
  try { await access(source); }
  catch (error) { if (error.code === 'ENOENT') return; throw error; }
  const data = JSON.parse(gunzipSync(await readFile(source)).toString('utf8'));
  const actor = data.controllers.find(controller => controller.poseSpace === 'actor'
    && controller.rigBones);
  if (!actor) return;
  const names = new Map();
  const visit = object => {
    names.set(object.transform?.id, object.name);
    for (const child of object.children ?? []) visit(child);
  };
  for (const root of bundle.scene.roots) visit(root);
  const hasMatchingActor = object => {
    const animator = object.components?.find(component => component.type === 'Animator'
      && component.properties?.controllerAssetId === actor.assetId);
    return animator && Object.entries(actor.rigBones).every(([index, name]) =>
      names.get(animator.properties.humanBones?.[index]?.$ref) === name);
  };
  const matches = object => hasMatchingActor(object)
    || (object.children ?? []).some(matches);
  if (!bundle.scene.roots.some(matches)) return;
  for (const module of modules) {
    if (module.moduleId !== '4FBE2152-E535-47DD-99EF-99F85B524202'
      || !module.animationEntry) continue;
    const current = JSON.parse(await readFile(inside(outputRoot, module.animationEntry), 'utf8'));
    if (current.format !== data.format || !Array.isArray(current.controllers)) continue;
    if (current.controllers.some(controller => controller.assetId === actor.assetId
      && controller.poseSpace === 'actor' && controller.rigBones)) continue;
    const exported = current.controllers.find(controller => controller.assetId === actor.assetId);
    if (!exported) continue;
    const clips = new Map(exported.clips.map(clip => [clip.name, clip]));
    if (actor.clips.length !== clips.size || actor.clips.some(clip => {
      const original = clips.get(clip.name);
      return !original || Math.abs(original.duration - clip.duration) > 0.002
        || original.times?.length !== clip.times?.length;
    })) continue;
    const entry = `animation/compat/${module.moduleId}-redmono.json`;
    await mkdir(dirname(inside(outputRoot, entry)), { recursive: true });
    await writeFile(inside(outputRoot, entry), `${JSON.stringify({ ...current,
      controllers: [actor, ...current.controllers] })}\n`);
    module.animationEntry = entry;
    diagnostics.push({ severity: 'warning', phase: 'animation',
      message: 'JU TPS: using clip-compatible RedMono pose samples for this Preview' });
  }
}

function html() {
  return `<!doctype html>\n<html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1"><link rel="icon" href="data:,">\n<title>Plyground Preview</title><style>html,body{margin:0;width:100%;height:100%;overflow:hidden;background:#171717;color:white;font:14px system-ui}canvas{display:block;width:100%;height:100%}#status{position:fixed;top:12px;left:12px;max-width:80vw;padding:8px 12px;background:#101820cc;white-space:pre-wrap;pointer-events:none}#status[hidden]{display:none}</style>\n<script type="importmap">${JSON.stringify({ imports: importMap })}</script></head><body><canvas id="scene"></canvas><div id="status">Loading preview…</div><script type="module" src="./runtime/src/preview-player.js"></script></body></html>\n`;
}

/** Build a standalone browser scene from a prepared Plyground Preview project. */
export async function buildPreview(request, { copyRuntime = true } = {}) {
  if (request?.format !== 'plyground.preview.materialization/v2') throw new Error('Unsupported preview request format');
  const projectRoot = resolve(request.project?.root ?? '');
  const manifestPath = resolve(request.project?.manifestPath ?? '');
  if (manifestPath !== join(projectRoot, 'project.json')) throw new Error('Preview project manifest must be project.json');
  const outputRoot = resolve(request.outputDirectory ?? '');
  if (outputRoot === projectRoot || outputRoot.startsWith(`${projectRoot}${sep}`)
    || projectRoot.startsWith(`${outputRoot}${sep}`)) throw new Error('Preview output must be separate from project');
  const project = JSON.parse((await readFile(manifestPath, 'utf8')).replace(/^\uFEFF/, ''));
  if (project.variationId !== request.variationId || project.runtime?.id !== request.runtime?.id
    || project.runtime?.apiVersion !== request.runtime?.apiVersion) throw new Error('Preview project and request disagree');
  const { bundle, modules, diagnostics = [] } = await assemblePreviewScene(projectRoot, project);
  const variationCode = await applyVariationBindings(projectRoot, project, bundle);
  validateManifest(bundle, BUNDLE_FORMAT);
  const existing = await readdir(outputRoot).catch(error => error.code === 'ENOENT' ? [] : Promise.reject(error));
  if (existing.length) throw new Error('Preview output directory must be empty');
  await mkdir(outputRoot, { recursive: true });
  let variationModule;
  try {
    variationModule = await transpileVariationSources(projectRoot, outputRoot, variationCode.sources, project.modules);
  } catch (error) {
    if (!/Preview C# transpilation failed/i.test(error.message)) throw error;
    // A partial Preview is still useful. Omit only the game C# bindings whose
    // module could not be generated; keep the rendered scene and module code.
    const skipped = new Set(variationCode.bindings.map(binding =>
      `variation:${binding.itemId}:${binding.typeName}`));
    for (const root of bundle.scene.roots)
      root.components = root.components.filter(component => !skipped.has(component.id));
    diagnostics.push({ severity: 'warning', phase: 'code-compatibility',
      message: `The rendered scene is available, but this Preview does not run the saved game C#. ${error.message}` });
  }
  if (variationModule) modules.push(variationModule);
  for (const folder of ['environment', 'modules', 'assets', 'variation-files']) {
    const source = join(projectRoot, folder);
    if ((await lstat(source).catch(() => null))?.isDirectory()) {
      const include = folder === 'modules'
        ? file => !file.split(sep).includes('solution') && !/\.(?:cs|meta)$/i.test(file)
        : undefined;
      await copyTree(source, join(outputRoot, folder), include, folder === 'modules');
    }
  }
  if (variationModule) {
    const coverage = await inspectPreviewApi(outputRoot);
    const unsupportedGameCalls = coverage.missing.filter(item => item.file.startsWith('variation/'));
    if (unsupportedGameCalls.length) {
      modules.splice(modules.indexOf(variationModule), 1);
      const skipped = new Set(variationCode.bindings.map(binding =>
        `variation:${binding.itemId}:${binding.typeName}`));
      for (const root of bundle.scene.roots)
        root.components = root.components.filter(component => !skipped.has(component.id));
      await rm(join(outputRoot, 'variation'), { recursive: true, force: true });
      diagnostics.push({ severity: 'warning', phase: 'code-compatibility',
        message: `The rendered scene is available, but this Preview does not run the saved game C#. `
          + `Unsupported runtime API: ${unsupportedGameCalls.slice(0, 10).map(item => item.symbol).join(', ')}` });
    }
  }
  await assertPreviewApiCoverage(outputRoot);
  if (copyRuntime) {
    await copyTree(join(runtimeRoot, 'src'), join(outputRoot, 'runtime', 'src'), undefined, true);
    await copyTree(join(runtimeRoot, 'node_modules'), join(outputRoot, 'runtime', 'node_modules'),
      file => !file.split(sep).includes('@types')
        && !/\.(?:[cm]?ts|map|md|txt)$/i.test(file)
        && !/(?:^|[\\/])package(?:-lock)?\.json$/i.test(file), true);
  }
  const remainingDiagnostics = await installCompatiblePreviewAnimations(outputRoot, modules, diagnostics);
  await installRedMonoPreviewAnimations(outputRoot, bundle, modules, remainingDiagnostics);
  for (const asset of bundle.assets) if (asset.url && !(await lstat(inside(outputRoot, asset.url)).catch(() => null))?.isFile())
    throw new Error(`Preview asset is missing: ${asset.url}`);
  for (const module of modules) if (!(await lstat(inside(outputRoot, module.codeEntry)).catch(() => null))?.isFile())
    throw new Error(`Preview module code is missing: ${module.codeEntry}`);
  for (const module of modules) if (module.animationEntry
    && !(await lstat(inside(outputRoot, module.animationEntry)).catch(() => null))?.isFile())
    throw new Error(`Preview module animation is missing: ${module.animationEntry}`);
  await writeFile(join(outputRoot, 'bundle.json'), `${JSON.stringify(bundle, null, 2)}\n`);
  await writeFile(join(outputRoot, 'modules.json'), `${JSON.stringify(modules, null, 2)}\n`);
  await writeFile(join(outputRoot, 'index.html'), html());
  return { format: 'plyground.preview.result/v1', status: 'succeeded', entrypoint: 'index.html',
    files: await listFiles(outputRoot), diagnostics: remainingDiagnostics };
}

async function cli(args) {
  const requestIndex = args.indexOf('--request');
  const resultIndex = args.indexOf('--result');
  if (requestIndex < 0 || resultIndex < 0 || !args[requestIndex + 1] || !args[resultIndex + 1])
    throw new Error('Usage: node src/preview-build.js --request <request.json> --result <result.json>');
  const resultPath = resolve(args[resultIndex + 1]);
  try {
    const request = JSON.parse(await readFile(resolve(args[requestIndex + 1]), 'utf8'));
    const result = await buildPreview(request);
    await writeFile(resultPath, `${JSON.stringify(result, null, 2)}\n`);
  } catch (error) {
    await writeFile(resultPath, `${JSON.stringify({ format: 'plyground.preview.result/v1', status: 'failed',
      diagnostics: [{ severity: 'error', phase: 'build', message: error.message }] }, null, 2)}\n`);
    throw error;
  }
}

if (process.argv[1] && import.meta.url === pathToFileURL(resolve(process.argv[1])).href) {
  cli(process.argv.slice(2)).catch(error => { process.stderr.write(`${error.stack}\n`); process.exitCode = 1; });
}
