import assert from 'node:assert/strict';
import { mkdtemp, mkdir, rm, writeFile } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import test from 'node:test';
import { deflateSync } from 'node:zlib';
import { GLTFLoader } from 'three/addons/loaders/GLTFLoader.js';
import { compileUnityMesh } from './unity-mesh.js';
import { headlessModelBytes, headlessPreviewTexture } from './preview-scene.js';

function pngPixel() {
  const signature = Buffer.from('89504e470d0a1a0a', 'hex');
  const crc32 = bytes => {
    let crc = 0xffffffff;
    for (const byte of bytes) {
      crc ^= byte;
      for (let bit = 0; bit < 8; bit++) crc = crc & 1
        ? (crc >>> 1) ^ 0xedb88320 : crc >>> 1;
    }
    return (crc ^ 0xffffffff) >>> 0;
  };
  const chunk = (type, data) => {
    const tag = Buffer.from(type);
    const out = Buffer.alloc(12 + data.length);
    out.writeUInt32BE(data.length, 0);
    tag.copy(out, 4);
    data.copy(out, 8);
    out.writeUInt32BE(crc32(Buffer.concat([tag, data])), 8 + data.length);
    return out;
  };
  const header = Buffer.alloc(13);
  header.writeUInt32BE(1, 0); header.writeUInt32BE(1, 4);
  header[8] = 8; header[9] = 6;
  return Buffer.concat([signature, chunk('IHDR', header),
    chunk('IDAT', deflateSync(Buffer.from([0, 10, 20, 30, 255]))),
    chunk('IEND', Buffer.alloc(0))]);
}

test('headless materializer loads readable exported PNG pixels', async () => {
  const root = await mkdtemp(join(tmpdir(), 'plythree-headless-texture-'));
  try {
    await mkdir(join(root, 'assets'));
    await writeFile(join(root, 'assets', 'texture.png'), pngPixel());
    const texture = await headlessPreviewTexture({ id: 'readable', kind: 'texture',
      url: 'assets/texture.png', textureWidth: 1, textureHeight: 1,
      textureMipCount: 1, textureReadable: true }, root, null);
    assert.equal(texture.isReadable, true);
    assert.deepEqual([...texture.GetRawTextureData()], [10, 20, 30, 255]);
  } finally {
    await rm(root, { recursive: true, force: true });
  }
});

test('headless materializer parses a model with an externally shared PNG', async () => {
  const mesh = { format: 'unity-bridge-mesh', version: 1,
    positions: [0, 0, 0, 1, 0, 0, 0, 1, 0], normals: [], uvs: [0, 0, 1, 0, 0, 1],
    submeshes: [{ indices: [0, 1, 2], material: 0 }],
    materials: [{ color: [1, 1, 1, 1], texturePng: pngPixel().toString('base64') }] };
  const glb = headlessModelBytes(compileUnityMesh(mesh, { externalImage: () => 'shared.png' }));
  const data = glb.buffer.slice(glb.byteOffset, glb.byteOffset + glb.byteLength);
  const loaded = await new Promise((resolve, reject) =>
    new GLTFLoader().parse(data, '', resolve, reject));
  assert.equal(loaded.scene.children[0].isMesh, true);
  assert.equal(loaded.scene.children[0].material.map, null);
});
