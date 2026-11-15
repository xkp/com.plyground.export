import { createHash } from 'node:crypto';
import { copyFile, mkdir, readFile, realpath, writeFile } from 'node:fs/promises';
import { dirname, extname, isAbsolute, join, relative, resolve, sep } from 'node:path';
import { pathToFileURL } from 'node:url';
import { AssetFormatError, BUNDLE_FORMAT, BUNDLE_VERSION, SOURCE_FORMAT, validateManifest } from './asset-schema.js';
import { compileUnityMesh } from './unity-mesh.js';

const extensions = {
  model: new Set(['.glb', '.unitymesh.json']),
  texture: new Set(['.png', '.jpg', '.jpeg', '.webp']),
  audio: new Set(['.mp3', '.ogg', '.wav']),
  navMesh: new Set(['.navmesh']),
};

function isInside(root, file) {
  const rel = relative(root, file);
  return rel !== '..' && !rel.startsWith(`..${sep}`) && !isAbsolute(rel);
}

/** Convert a Unity-exported source manifest into a browser-ready, content-addressed bundle. */
export async function preprocess(sourceFile, outputDirectory) {
  const input = resolve(sourceFile);
  const sourceRoot = await realpath(dirname(input));
  const source = validateManifest(JSON.parse((await readFile(input, 'utf8')).replace(/^\uFEFF/, '')), SOURCE_FORMAT);
  if (source.coordinateSystem !== 'unity') {
    throw new AssetFormatError('Source coordinateSystem must be unity; convert model geometry before export');
  }
  const outputRoot = resolve(outputDirectory);
  await mkdir(join(outputRoot, 'assets'), { recursive: true });
  const assets = [];
  for (const asset of [...source.assets].sort((a, b) => a.id.localeCompare(b.id))) {
    if (['scriptableObject', 'prefab', 'physicsMaterial', 'animationClip',
      'animatorController'].includes(asset.kind)) {
      assets.push(asset); continue;
    }
    if (isAbsolute(asset.path)) throw new AssetFormatError(`Absolute asset path: ${asset.path}`);
    const file = await realpath(resolve(sourceRoot, asset.path));
    if (!isInside(sourceRoot, file)) throw new AssetFormatError(`Asset escapes source directory: ${asset.path}`);
    const extension = file.toLowerCase().endsWith('.unitymesh.json') ? '.unitymesh.json'
      : extname(file).toLowerCase();
    if (!extensions[asset.kind].has(extension)) {
      throw new AssetFormatError(`Asset ${asset.id} needs a supported ${asset.kind} format`);
    }
    const bytes = extension === '.unitymesh.json'
      ? compileUnityMesh(JSON.parse(await readFile(file, 'utf8')))
      : await readFile(file);
    const sha256 = createHash('sha256').update(bytes).digest('hex');
    const filename = `${sha256}${extension === '.unitymesh.json' ? '.glb' : extension}`;
    if (extension === '.unitymesh.json') await writeFile(join(outputRoot, 'assets', filename), bytes);
    else await copyFile(file, join(outputRoot, 'assets', filename));
    assets.push({ id: asset.id, kind: asset.kind, url: `assets/${filename}`, sha256,
      ...(asset.name ? { name: asset.name } : {}) });
  }
  const bundle = {
    format: BUNDLE_FORMAT,
    version: BUNDLE_VERSION,
    coordinateSystem: 'unity',
    sourceUnityVersion: source.sourceUnityVersion ?? null,
    diagnostics: source.diagnostics ?? [],
    layers: source.layers,
    resources: source.resources,
    assets,
    scene: source.scene,
  };
  validateManifest(bundle, BUNDLE_FORMAT);
  await writeFile(join(outputRoot, 'bundle.json'), `${JSON.stringify(bundle, null, 2)}\n`);
  return bundle;
}

if (process.argv[1] && import.meta.url === pathToFileURL(resolve(process.argv[1])).href) {
  if (process.argv.length !== 4) {
    process.stderr.write('Usage: node src/preprocess.js <source.json> <output-directory>\n');
    process.exitCode = 2;
  } else {
    preprocess(process.argv[2], process.argv[3])
      .then(bundle => process.stdout.write(`Wrote ${bundle.scene.name} with ${bundle.assets.length} assets\n`))
      .catch(error => { process.stderr.write(`${error.stack}\n`); process.exitCode = 1; });
  }
}
