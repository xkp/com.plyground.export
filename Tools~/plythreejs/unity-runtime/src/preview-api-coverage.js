import { readdir, readFile } from 'node:fs/promises';
import { join, relative } from 'node:path';
import { pathToFileURL } from 'node:url';
import * as runtime from './index.js';

const runtimeImports = /import\s*\{([^}]+)\}\s*from\s*["']@plythreejs\/runtime["']/g;

export function describePreviewApiGap(item) {
  const location = item.source
    ? `${item.source.file}:${item.source.line}:${item.source.column}`
    : `${item.file}:${item.line}`;
  return `${location} ${item.symbol}`;
}

/** Check static calls in exported module code before Preview opens a browser. */
export async function inspectPreviewApi(root) {
  const missing = [];
  let filesChecked = 0;
  let callsChecked = 0;
  async function visit(folder) {
    for (const entry of await readdir(folder, { withFileTypes: true })) {
      const file = join(folder, entry.name);
      if (entry.isDirectory()) { await visit(file); continue; }
      if (!entry.name.endsWith('.mjs')) continue;
      filesChecked++;
      const source = await readFile(file, 'utf8');
      const locations = [];
      let origin = null;
      for (const [index, line] of source.split('\n').entries()) {
        const marker = /^\s*\/\/ @unity-source (.+):(\d+):(\d+)\s*$/.exec(line);
        if (marker) origin = { file: marker[1], line: Number(marker[2]), column: Number(marker[3]) };
        locations[index + 1] = origin;
      }
      const locationAt = offset => {
        const line = source.slice(0, offset).split('\n').length;
        return { line, ...(locations[line] ? { source: locations[line] } : {}) };
      };
      const imports = [...source.matchAll(runtimeImports)]
        .flatMap(match => match[1].split(',').map(item => item.trim()).filter(Boolean));
      for (const item of imports) {
        const [name, alias = name] = item.split(/\s+as\s+/);
        if (!(name in runtime)) {
          missing.push({ file: relative(root, file).replaceAll('\\', '/'), symbol: name,
            ...locationAt(source.indexOf(item)) });
          continue;
        }
        const pattern = new RegExp(`(?<![\\w.$])${alias}\\.([A-Za-z_][\\w]*)\\s*\\(`, 'g');
        for (const match of source.matchAll(pattern)) {
          callsChecked++;
          if (typeof runtime[name]?.[match[1]] !== 'function')
            missing.push({ file: relative(root, file).replaceAll('\\', '/'),
              symbol: `${name}.${match[1]}`, ...locationAt(match.index) });
        }
      }
    }
  }
  for (const folder of ['modules', 'variation']) {
    const path = join(root, folder);
    if (await readdir(path).catch(error => error.code === 'ENOENT' ? null : Promise.reject(error)))
      await visit(path);
  }
  return { filesChecked, callsChecked, missing };
}

export async function assertPreviewApiCoverage(root) {
  const report = await inspectPreviewApi(root);
  if (!report.missing.length) return report;
  const examples = report.missing.slice(0, 10)
    .map(describePreviewApiGap).join('\n');
  throw new Error(`Preview Unity API coverage is missing ${report.missing.length} call(s):\n${examples}`);
}

if (process.argv[1] && import.meta.url === pathToFileURL(process.argv[1]).href) {
  const root = process.argv[2];
  if (!root) throw new Error('Usage: node src/preview-api-coverage.js <Preview project root>');
  assertPreviewApiCoverage(root).then(report => {
    process.stdout.write(`Checked ${report.callsChecked} runtime calls in ${report.filesChecked} files.\n`);
  }).catch(error => { process.stderr.write(`${error.message}\n`); process.exitCode = 1; });
}
