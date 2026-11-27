import assert from 'node:assert/strict';
import { createHash } from 'node:crypto';
import { mkdtemp, mkdir, readFile, readdir, rm, writeFile } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import test from 'node:test';
import { preprocess } from './preprocess.js';

const png = Buffer.from('iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVQIHWP4z8DwHwAFgAI/ScL/nwAAAABJRU5ErkJggg==', 'base64');

test('Preview models share one content-addressed texture without sharing renderer identity', async () => {
  const root = await mkdtemp(join(tmpdir(), 'plythree-shared-image-'));
  try {
    const mesh = { format: 'unity-bridge-mesh', version: 1, meshId: 'first-mesh',
      positions: [0, 0, 0, 1, 0, 0, 0, 1, 0], normals: [], uvs: [0, 0, 1, 0, 0, 1],
      submeshes: [{ material: 0, indices: [0, 1, 2] }],
      materials: [{ name: 'Shared', color: [1, 1, 1, 1], texturePng: png.toString('base64') }] };
    const second = structuredClone(mesh);
    second.meshId = 'second-renderer-mesh';
    second.positions[0] += 1;
    await mkdir(join(root, 'models'));
    await writeFile(join(root, 'models', 'first.unitymesh.json'), JSON.stringify(mesh));
    await writeFile(join(root, 'models', 'second.unitymesh.json'), JSON.stringify(second));
    const source = { format: 'unity-bridge-source', version: 1, coordinateSystem: 'unity',
      assets: [{ id: 'first-model', kind: 'model', path: 'models/first.unitymesh.json' },
        { id: 'second-model', kind: 'model', path: 'models/second.unitymesh.json' }],
      scene: { name: 'Module Prefabs', roots: [] }, assetPaths: {}, resources: {} };
    const sourcePath = join(root, 'source.json');
    await writeFile(sourcePath, JSON.stringify(source));
    const output = join(root, 'output');
    const bundle = await preprocess(sourcePath, output);
    const models = bundle.assets.filter(asset => asset.kind === 'model');
    assert.equal(models.length, 2);
    assert.notEqual(models[0].id, models[1].id);
    assert.notEqual(models[0].url, models[1].url);
    const imageName = `${createHash('sha256').update(png).digest('hex')}.png`;
    const files = await readdir(join(output, 'assets'));
    assert.equal(files.filter(name => name.endsWith('.png')).length, 1);
    assert.deepEqual(await readFile(join(output, 'assets', imageName)), png);
    for (const model of models) {
      const glb = await readFile(join(output, model.url));
      const gltf = JSON.parse(glb.toString('utf8', 20, 20 + glb.readUInt32LE(12)));
      assert(gltf.images.some(image => image.uri === imageName));
      assert(gltf.images.every(image => !Object.hasOwn(image, 'bufferView')));
    }
  } finally {
    await rm(root, { recursive: true, force: true });
  }
});
