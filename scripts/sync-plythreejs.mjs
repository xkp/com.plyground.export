import { createHash } from 'node:crypto';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const packageRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const sourceInput = process.argv[2] || process.env.PLYTHREEJS_ROOT;
if (!sourceInput) throw new Error('Usage: node scripts/sync-plythreejs.mjs <plythreejs checkout>');
const sourceRoot = path.resolve(sourceInput);
const source = path.join(sourceRoot, 'unity-runtime');
const destination = path.join(packageRoot, 'Tools~', 'plythreejs', 'unity-runtime');
const bundleDestination = path.resolve(packageRoot, 'Tools~', 'plythreejs');
if (path.dirname(path.dirname(bundleDestination)) !== packageRoot)
  throw new Error('Unexpected Plythreejs bundle destination');
for (const name of ['package.json', 'package-lock.json', 'src/preview-module-assets.js',
  'lib/manifest.json',
  'tools/UnityTranspiler/UnityTranspiler.csproj',
  'tools/select-raw-humanoid-clips.mjs']) {
  if (!fs.existsSync(path.join(source, name))) throw new Error(`Missing Plythreejs source: ${name}`);
}

const hash = createHash('sha256');
function copyTree(relative) {
  const from = path.join(source, relative);
  const to = path.join(destination, relative);
  if (fs.statSync(from).isDirectory()) {
    fs.mkdirSync(to, { recursive: true });
    for (const name of fs.readdirSync(from).sort()) copyTree(path.join(relative, name));
    return;
  }
  hash.update(relative.replaceAll(path.sep, '/'));
  hash.update(fs.readFileSync(from));
  fs.mkdirSync(path.dirname(to), { recursive: true });
  fs.copyFileSync(from, to);
}

fs.rmSync(bundleDestination, { recursive: true, force: true });
for (const name of ['package.json', 'package-lock.json', 'src', 'lib']) copyTree(name);
copyTree('tools/select-raw-humanoid-clips.mjs');
for (const name of fs.readdirSync(path.join(source, 'tools', 'UnityTranspiler')).sort()) {
  if (/\.(?:cs|csproj)$/.test(name)) copyTree(path.join('tools', 'UnityTranspiler', name));
}
const digest = hash.digest('hex');
fs.writeFileSync(path.join(packageRoot, 'Tools~', 'plythreejs', 'bundle.json'),
  JSON.stringify({ format: 'plyground.plythreejs-bundle/v1', sha256: digest }, null, 2) + '\n');
process.stdout.write(`Bundled Plythreejs ${digest} from ${sourceRoot}\n`);
