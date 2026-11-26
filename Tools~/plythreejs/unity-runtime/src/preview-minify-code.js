import { createHash } from 'node:crypto';
import { lstat, mkdir, readFile, readdir, writeFile } from 'node:fs/promises';
import { dirname, join, relative, resolve, sep } from 'node:path';
import { gzipSync } from 'node:zlib';
import { pathToFileURL } from 'node:url';
import { minify } from 'terser';

const hash = bytes => `sha256:${createHash('sha256').update(bytes).digest('hex')}`;

async function codeFiles(root, folder = root) {
  const files = [];
  for (const entry of await readdir(folder, { withFileTypes: true })) {
    const file = join(folder, entry.name);
    if (entry.isDirectory()) files.push(...await codeFiles(root, file));
    else if (entry.isFile() && entry.name.endsWith('.mjs'))
      files.push(relative(root, file).replaceAll('\\', '/'));
  }
  return files.sort();
}

/** Keep transpiler output readable; write browser code and sizes separately. */
export async function minifyPreviewCode(sourceDirectory, outputDirectory) {
  const source = resolve(sourceDirectory);
  const output = resolve(outputDirectory);
  if (source === output || output.startsWith(`${source}${sep}`) || source.startsWith(`${output}${sep}`))
    throw new Error('Minified Preview code must use a separate directory');
  if ((await lstat(output).catch(error => error.code === 'ENOENT' ? null : Promise.reject(error))))
    throw new Error('Minified Preview output directory must not exist');
  const files = await codeFiles(source);
  if (!files.length) throw new Error('Preview code has no modules to minify');
  const version = JSON.parse(await readFile(new URL('../node_modules/terser/package.json', import.meta.url), 'utf8')).version;
  const records = [];
  for (const name of files) {
    const plain = await readFile(join(source, name));
    const result = await minify(plain.toString('utf8'), {
      module: true, compress: true, mangle: true,
      keep_classnames: true, keep_fnames: true,
      format: { comments: 'some' }
    });
    if (!result.code) throw new Error(`Terser emitted no Preview code for ${name}`);
    const browser = Buffer.from(`${result.code}\n`);
    const destination = join(output, name);
    await mkdir(dirname(destination), { recursive: true });
    await writeFile(destination, browser);
    records.push({ path: name, source: { bytes: plain.length, gzipBytes: gzipSync(plain).length,
      revision: hash(plain) }, browser: { bytes: browser.length,
      gzipBytes: gzipSync(browser).length, revision: hash(browser) } });
  }
  const sum = key => ({ bytes: records.reduce((total, item) => total + item[key].bytes, 0),
    gzipBytes: records.reduce((total, item) => total + item[key].gzipBytes, 0) });
  const report = { format: 'plyground.preview.code-sizes/v1',
    minifier: { name: 'terser', version }, files: records,
    totals: { source: sum('source'), browser: sum('browser') } };
  await writeFile(join(output, 'manifest.json'), `${JSON.stringify(report, null, 2)}\n`);
  return report;
}

if (process.argv[1] && import.meta.url === pathToFileURL(resolve(process.argv[1])).href) {
  const [source, output] = process.argv.slice(2);
  if (!source || !output) throw new Error('Usage: node preview-minify-code.js <source-code> <browser-code>');
  minifyPreviewCode(source, output).then(report => {
    process.stdout.write(`${JSON.stringify(report.totals)}\n`);
  }).catch(error => { process.stderr.write(`${error.stack}\n`); process.exitCode = 1; });
}
