import assert from 'node:assert/strict';
import test from 'node:test';
import { linkModuleResources } from './preview-resource-links.js';

test('links all browser resources from a published immutable module', () => {
  const prefix = 'modules/shooter/Preview/';
  const base = 'https://assets.example/modules/shooter/file-1/Preview/';
  const modules = [{ moduleId: 'shooter', staticAssetBaseUrl: base,
    codeEntry: `${prefix}code/module.mjs`, animationEntry: `${prefix}animation/data.json` }];
  const bundle = { assets: [{ id: 'mesh', url: `${prefix}assets/mesh.glb` },
    { id: 'game', url: 'assets/user/mesh.glb' }],
  scenes: [{ recordUrl: `${prefix}assets/scenes/${'a'.repeat(64)}.json` }] };
  assert.deepEqual([...linkModuleResources(bundle, modules)], [prefix]);
  assert.equal(modules[0].codeEntry, `${base}code/module.mjs`);
  assert.equal(modules[0].animationEntry, `${base}animation/data.json`);
  assert.equal(bundle.assets[0].url, `${base}assets/mesh.glb`);
  assert.equal(bundle.assets[1].url, 'assets/user/mesh.glb');
  assert.equal(bundle.scenes[0].recordUrl, `${base}assets/scenes/${'a'.repeat(64)}.json`);
  assert.equal('staticAssetBaseUrl' in modules[0], false);
});
