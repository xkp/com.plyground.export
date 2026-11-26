import assert from 'node:assert/strict';
import { mkdtemp, mkdir, readFile, rm, writeFile } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import test from 'node:test';
import { minifyPreviewCode } from './preview-minify-code.js';

test('minifies browser modules while retaining readable source and recording sizes', async () => {
  const root = await mkdtemp(join(tmpdir(), 'preview-minify-'));
  try {
    const source = join(root, 'code');
    const output = join(root, 'code-min');
    await mkdir(source);
    const plain = 'export function sum(first, second) { return first + second; }\n'
      + 'export class PlayerController { Update() { return sum(2, 3); } }\n';
    await writeFile(join(source, 'module.mjs'), plain);
    const report = await minifyPreviewCode(source, output);
    assert.equal(await readFile(join(source, 'module.mjs'), 'utf8'), plain);
    assert.equal(report.minifier.name, 'terser');
    assert.match(report.minifier.version, /^\d+\.\d+\.\d+$/);
    assert(report.totals.browser.bytes < report.totals.source.bytes);
    const browser = await import(`file:///${join(output, 'module.mjs').replaceAll('\\', '/')}`);
    assert.equal(browser.sum(2, 3), 5);
    assert.equal(browser.PlayerController.name, 'PlayerController');
    assert.deepEqual(JSON.parse(await readFile(join(output, 'manifest.json'), 'utf8')), report);
  } finally { await rm(root, { recursive: true, force: true }); }
});
