import { createHash } from 'node:crypto';
import { mkdir, readFile, writeFile } from 'node:fs/promises';
import { dirname, join } from 'node:path';
import * as THREE from 'three';
import { GLTFLoader } from 'three/addons/loaders/GLTFLoader.js';
import { FBXLoader } from 'three/addons/loaders/FBXLoader.js';
import { bakeNavMesh } from './navigation.js';

function needsNavigation(nodes) {
  return nodes.some(node => (node.components ?? []).some(component =>
    ['NavMeshAgent', 'UnityEngine.AI.NavMeshAgent',
      'NavMeshObstacle', 'UnityEngine.AI.NavMeshObstacle',
      'OffMeshLink', 'UnityEngine.AI.OffMeshLink'].includes(component.type))
    || needsNavigation(node.children ?? []));
}

function hasObstacles(nodes) {
  return nodes.some(node => (node.components ?? []).some(component =>
    ['NavMeshObstacle', 'UnityEngine.AI.NavMeshObstacle'].includes(component.type))
    || hasObstacles(node.children ?? []));
}

function offMeshConnections(nodes) {
  const links = [];
  const visit = node => {
    for (const component of node.components ?? []) {
      if (!['OffMeshLink', 'UnityEngine.AI.OffMeshLink'].includes(component.type)
        || component.enabled === false || component.properties?.activated === false) continue;
      const start = component.properties?.startPosition?.$vector3;
      const end = component.properties?.endPosition?.$vector3;
      if (!Array.isArray(start) || !Array.isArray(end))
        throw new Error(`OffMeshLink ${component.id} needs exported world endpoints`);
      const point = values => ({ x: values[0], y: values[1], z: values[2] });
      links.push({ startPosition: point(start), endPosition: point(end),
        radius: component.properties.radius ?? 0.5,
        bidirectional: component.properties.biDirectional !== false });
    }
    for (const child of node.children ?? []) visit(child);
  };
  for (const node of nodes) visit(node);
  return links;
}

// Navigation only needs mesh geometry. Avoid loading image resources while parsing
// Preview GLBs in Node, where browser texture decoders are unavailable.
function geometryOnlyGlb(bytes) {
  if (bytes.readUInt32LE(0) !== 0x46546c67 || bytes.readUInt32LE(4) !== 2)
    throw new Error('Navigation needs a GLB 2.0 environment model');
  const jsonLength = bytes.readUInt32LE(12);
  if (bytes.readUInt32LE(16) !== 0x4e4f534a || 20 + jsonLength > bytes.length)
    throw new Error('Navigation environment GLB has no valid JSON chunk');
  const json = JSON.parse(bytes.toString('utf8', 20, 20 + jsonLength));
  for (const mesh of json.meshes ?? [])
    for (const primitive of mesh.primitives ?? []) delete primitive.material;
  const encoded = Buffer.from(JSON.stringify(json));
  const paddedLength = Math.ceil(encoded.length / 4) * 4;
  const tail = bytes.subarray(20 + jsonLength);
  const result = Buffer.alloc(20 + paddedLength + tail.length);
  bytes.copy(result, 0, 0, 12);
  result.writeUInt32LE(result.length, 8);
  result.writeUInt32LE(paddedLength, 12);
  result.writeUInt32LE(0x4e4f534a, 16);
  encoded.copy(result, 20);
  result.fill(0x20, 20 + encoded.length, 20 + paddedLength);
  tail.copy(result, 20 + paddedLength);
  return result.buffer.slice(result.byteOffset, result.byteOffset + result.byteLength);
}

/** Bake generated environment geometry only when the scene instantiates a NavMeshAgent. */
export async function bakePreviewNavigation(bundle, outputRoot, resolveAsset,
  { staticNavMeshCalls = false } = {}) {
  const prefabRoots = bundle.assets.filter(asset => asset.kind === 'prefab' && asset.root)
    .map(asset => asset.root);
  if (bundle.scene.navMesh || !(needsNavigation([...bundle.scene.roots, ...prefabRoots])
    || staticNavMeshCalls))
    return false;
  const tileCache = hasObstacles([...bundle.scene.roots, ...prefabRoots]);
  const links = offMeshConnections(bundle.scene.roots);
  const records = new Map(bundle.assets.map(asset => [asset.id, asset]));
  const cache = new Map();
  const load = async id => {
    if (cache.has(id)) return cache.get(id);
    const asset = records.get(id);
    if (!asset || asset.kind !== 'model' || !asset.url)
      throw new Error(`Navigation environment model is missing: ${id}`);
    const file = resolveAsset(outputRoot, asset.url);
    const bytes = await readFile(file);
    globalThis.self ??= globalThis;
    let visual;
    if (/\.glb$/i.test(asset.url)) {
      visual = await new Promise((resolve, reject) => new GLTFLoader().parse(
        geometryOnlyGlb(bytes),
        `${dirname(file)}/`, resolve, reject));
      visual = visual.scene;
    } else if (/\.fbx$/i.test(asset.url))
      visual = new FBXLoader().parse(bytes.buffer.slice(
        bytes.byteOffset, bytes.byteOffset + bytes.byteLength), `${dirname(file)}/`);
    else throw new Error(`Navigation needs a GLB or FBX model: ${asset.url}`);
    cache.set(id, visual);
    return visual;
  };
  const meshes = [];
  const stage = async (node, parent) => {
    if (!node.id?.startsWith('bob:')) return;
    const group = new THREE.Group();
    group.position.fromArray(node.transform.position);
    group.quaternion.fromArray(node.transform.rotation);
    group.scale.fromArray(node.transform.scale);
    parent.add(group);
    if (node.model) {
      const visual = (await load(node.model)).clone(true);
      if (node.mirrorX) visual.scale.x *= -1;
      group.add(visual);
      visual.traverse(child => { if (child.isMesh) meshes.push(child); });
    }
    for (const child of node.children ?? []) await stage(child, group);
  };
  const world = new THREE.Group();
  for (const root of bundle.scene.roots) await stage(root, world);
  if (!meshes.length) throw new Error('NavMeshAgent needs walkable Preview environment geometry');
  world.updateMatrixWorld(true);
  let bytes;
  try { bytes = await bakeNavMesh(meshes, { tileCache, offMeshConnections: links }); }
  catch (error) { throw new Error(`Could not bake Preview navigation: ${error.message}`); }
  const sha256 = createHash('sha256').update(bytes).digest('hex');
  const url = `assets/${sha256}.${tileCache ? 'tilecache' : 'navmesh'}`;
  await mkdir(join(outputRoot, 'assets'), { recursive: true });
  await writeFile(join(outputRoot, url), bytes);
  const id = `bob:navMesh:${sha256}`;
  bundle.assets.push({ id, kind: 'navMesh', url, sha256,
    ...(tileCache && links.length ? { offMeshConnections: links } : {}) });
  bundle.scene.navMesh = id;
  return true;
}
