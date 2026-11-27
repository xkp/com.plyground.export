import assert from 'node:assert/strict';
import { mkdir, mkdtemp, rm, writeFile } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import test from 'node:test';
import { JToken, JObject, PreviewEnumerable, UnityType } from './index.js';
import { importPreparedModule } from './preview-scene.js';
import { inspectPreviewApi } from './preview-api-coverage.js';

test('legacy character descriptor keeps typed JSON data', () => {
  UnityType.Clear();
  const descriptor = new JObject({ data: { gender: 1 } }).ToObject('CharacterDescriptor');
  assert.equal(descriptor.Data instanceof JObject, true);
  assert.equal(descriptor.Data.SelectToken('gender').Value('int'), 1);
  assert.equal(JToken.As(descriptor.Data, JObject), descriptor.Data);
});

test('legacy editor module keeps C# null and descriptor semantics', async () => {
  const root = await mkdtemp(join(tmpdir(), 'plythree-legacy-editor-'));
  try {
    const file = join(root, 'system.mjs');
    await writeFile(file, 'class BaseCharacterModule { GetDescriptor(data) { let result = data?.ToObject(); return result; } }\n'
      + 'export function descriptor(data) { return new BaseCharacterModule().GetDescriptor(data); }\n'
      + 'export function hasData(descriptor) { return descriptor?.Data !== null; }\n');
    const module = await import(await importPreparedModule(file, root));
    UnityType.Clear();
    assert.equal(module.descriptor(new JObject({ data: { gender: 0 } })).Data instanceof JObject, true);
    assert.equal(module.hasData(null), false);
  } finally {
    await rm(root, { recursive: true, force: true });
  }
});

test('published LINQ dispatch works with arrays', () => {
  assert.equal(UnityType.InvokeInterface([1, 2], 'FirstOrDefault', [value => value > 1], []), 2);
  assert.equal(PreviewEnumerable.FirstOrDefault([], () => true), null);
});

test('API coverage checks readable module code once', async () => {
  const root = await mkdtemp(join(tmpdir(), 'plythree-api-coverage-'));
  try {
    const readable = join(root, 'modules', 'sample', 'Preview', 'code');
    const minified = join(root, 'modules', 'sample', 'Preview', 'code-min');
    await mkdir(readable, { recursive: true });
    await mkdir(minified, { recursive: true });
    await writeFile(join(readable, 'type.mjs'), 'import { AnimationCurve } from "@plythreejs/runtime";\nAnimationCurve.Linear(0, 0, 1, 1);\n');
    await writeFile(join(minified, 'type.mjs'), 'import { AnimationCurve } from "@plythreejs/runtime"; AnimationCurve.Missing();');
    const report = await inspectPreviewApi(root);
    assert.equal(report.filesChecked, 1);
    assert.equal(report.missing.length, 0);
  } finally {
    await rm(root, { recursive: true, force: true });
  }
});
