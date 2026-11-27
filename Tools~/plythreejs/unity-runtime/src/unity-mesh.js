import { AssetFormatError } from './asset-schema.js';

const align = size => (size + 3) & ~3;
const clamp01 = value => Math.max(0, Math.min(1, value));

/** Compile Unity Editor mesh data into a self-contained glTF 2.0 binary. */
export function compileUnityMesh(source, { externalImage = null } = {}) {
  if (source?.format !== 'unity-bridge-mesh' || source.version !== 1) {
    throw new AssetFormatError('Expected unity-bridge-mesh version 1');
  }
  const { positions, normals, uvs, submeshes, materials } = source;
  if (!Array.isArray(positions) || positions.length < 9 || positions.length % 3
    || !positions.every(Number.isFinite)) throw new AssetFormatError('Mesh positions are invalid');
  const vertexCount = positions.length / 3;
  if (!Array.isArray(submeshes) || !submeshes.length) throw new AssetFormatError('Mesh needs triangle submeshes');
  for (const [name, values, size] of [['normals', normals, 3], ['uvs', uvs, 2]]) {
    if (!Array.isArray(values) || (values.length && values.length !== vertexCount * size)
      || !values.every(Number.isFinite)) throw new AssetFormatError(`Mesh ${name} are invalid`);
  }
  for (const [name, size] of [['colors', 4], ['tangents', 4],
    ['uvs2', 2], ['uvs3', 2], ['uvs4', 2], ['uvs5', 2], ['uvs6', 2],
    ['uvs7', 2], ['uvs8', 2]]) {
    const values = source[name];
    if (values !== undefined && (!Array.isArray(values)
      || (values.length && values.length !== vertexCount * size)
      || !values.every(Number.isFinite))) {
      throw new AssetFormatError(`Mesh ${name} are invalid`);
    }
  }
  const uvFull = source.uvFull ?? {};
  if (!uvFull || typeof uvFull !== 'object' || Array.isArray(uvFull))
    throw new AssetFormatError('Mesh uvFull is invalid');
  for (const [channel, entry] of Object.entries(uvFull)) {
    const field = channel === '0' ? 'uvs' : `uvs${Number(channel) + 1}`;
    if (!/^[0-7]$/.test(channel) || ![3, 4].includes(entry?.dimension)
      || !Array.isArray(entry.values) || entry.values.length !== vertexCount * entry.dimension
      || !entry.values.every(Number.isFinite) || source[field]?.length !== vertexCount * 2)
      throw new AssetFormatError(`Mesh uvFull channel ${channel} is invalid`);
  }
  if (!Array.isArray(materials)) throw new AssetFormatError('Mesh materials are invalid');
  const blendShapes = source.blendShapes ?? [];
  if (!Array.isArray(blendShapes)) throw new AssetFormatError('Mesh blend shapes are invalid');
  const shapeNames = new Set();
  for (const shape of blendShapes) {
    if (typeof shape?.name !== 'string' || shapeNames.has(shape.name)
      || !Array.isArray(shape.frames) || !shape.frames.length)
      throw new AssetFormatError('Mesh blend shape names or frames are invalid');
    shapeNames.add(shape.name);
    let previousWeight = -Infinity;
    for (const frame of shape.frames) {
      if (!Number.isFinite(frame?.weight) || frame.weight <= previousWeight
        || !['vertices', 'normals', 'tangents'].every(name =>
          Array.isArray(frame[name]) && frame[name].length === vertexCount * 3
          && frame[name].every(Number.isFinite)))
        throw new AssetFormatError(`Mesh blend shape ${shape.name} frame is invalid`);
      previousWeight = frame.weight;
    }
  }

  const chunks = [];
  const bufferViews = [];
  const accessors = [];
  let byteLength = 0;
  const view = (bytes, target) => {
    const index = bufferViews.length;
    const entry = { buffer: 0, byteOffset: byteLength, byteLength: bytes.length };
    if (target) entry.target = target;
    bufferViews.push(entry);
    chunks.push(bytes);
    byteLength += align(bytes.length);
    return index;
  };
  const floatAccessor = (values, size, semantic, target = 34962) => {
    const bytes = Buffer.allocUnsafe(values.length * 4);
    values.forEach((value, index) => bytes.writeFloatLE(value, index * 4));
    const accessor = { bufferView: view(bytes, target), componentType: 5126,
      count: values.length / size, type: { 1: 'SCALAR', 2: 'VEC2', 3: 'VEC3', 4: 'VEC4', 16: 'MAT4' }[size] };
    if (semantic === 'POSITION' || semantic === 'MORPH_POSITION') {
      accessor.min = [Infinity, Infinity, Infinity];
      accessor.max = [-Infinity, -Infinity, -Infinity];
      for (let index = 0; index < values.length; index++) {
        const axis = index % 3;
        accessor.min[axis] = Math.min(accessor.min[axis], values[index]);
        accessor.max[axis] = Math.max(accessor.max[axis], values[index]);
      }
    }
    if (semantic === 'TIME') {
      accessor.min = [Math.min(...values)];
      accessor.max = [Math.max(...values)];
    }
    accessors.push(accessor);
    return accessors.length - 1;
  };
  const attributes = { POSITION: floatAccessor(positions, 3, 'POSITION') };
  if (normals.length) attributes.NORMAL = floatAccessor(normals, 3, 'NORMAL');
  if (uvs.length) attributes.TEXCOORD_0 = floatAccessor(uvs, 2, 'TEXCOORD_0');
  for (const [name, semantic, size] of [['colors', 'COLOR_0', 4],
    ['tangents', 'TANGENT', 4], ['uvs2', 'TEXCOORD_1', 2],
    ['uvs3', 'TEXCOORD_2', 2], ['uvs4', 'TEXCOORD_3', 2],
    ['uvs5', 'TEXCOORD_4', 2], ['uvs6', 'TEXCOORD_5', 2],
    ['uvs7', 'TEXCOORD_6', 2], ['uvs8', 'TEXCOORD_7', 2]]) {
    if (source[name]?.length) attributes[semantic] = floatAccessor(source[name], size, semantic);
  }
  for (const [channel, entry] of Object.entries(uvFull)) {
    const semantic = `_UNITY_UV_${channel}`;
    attributes[semantic] = floatAccessor(entry.values, entry.dimension, semantic);
  }
  if (source.skin) {
    const { bones, joints, weights, inverseBindMatrices } = source.skin;
    if (!Array.isArray(bones) || !bones.length || bones.length > 65535
      || !Array.isArray(joints) || joints.length !== vertexCount * 4
      || !joints.every(value => Number.isInteger(value) && value >= 0 && value < bones.length)
      || !Array.isArray(weights) || weights.length !== vertexCount * 4
      || !weights.every(value => Number.isFinite(value) && value >= 0)
      || !Array.isArray(inverseBindMatrices) || inverseBindMatrices.length !== bones.length * 16
      || !inverseBindMatrices.every(Number.isFinite)) {
      throw new AssetFormatError('Mesh skin is invalid');
    }
    const jointBytes = Buffer.allocUnsafe(joints.length * 2);
    joints.forEach((value, index) => jointBytes.writeUInt16LE(value, index * 2));
    accessors.push({ bufferView: view(jointBytes, 34962), componentType: 5123,
      count: vertexCount, type: 'VEC4' });
    attributes.JOINTS_0 = accessors.length - 1;
    attributes.WEIGHTS_0 = floatAccessor(weights, 4, 'WEIGHTS');
  }
  const morphFrames = blendShapes.flatMap(shape => shape.frames.map(frame => ({ shape, frame })));
  const morphTargets = morphFrames.map(({ frame }) => ({
    POSITION: floatAccessor(frame.vertices, 3, 'MORPH_POSITION'),
    NORMAL: floatAccessor(frame.normals, 3, 'MORPH_NORMAL'),
  }));

  const images = [];
  const textures = [];
  const addImage = (png, textureId) => {
    const image = externalImage
      ? { uri: externalImage(png) }
      : { bufferView: view(png), mimeType: 'image/png' };
    images.push({ ...image, extras: { unityTextureId: textureId ?? null } });
    textures.push({ source: images.length - 1 });
    return textures.length - 1;
  };
  const gltfMaterials = materials.map(material => {
    const color = material?.color ?? [1, 1, 1, 1];
    if (!Array.isArray(color) || color.length !== 4 || !color.every(Number.isFinite)) {
      throw new AssetFormatError('Material color is invalid');
    }
    const pbr = { baseColorFactor: color,
      metallicFactor: clamp01(material.metallic ?? 0),
      roughnessFactor: clamp01(material.roughness ?? 1) };
    if (material.texturePng) {
      const png = Buffer.from(material.texturePng, 'base64');
      if (png.subarray(0, 8).toString('hex') !== '89504e470d0a1a0a') {
        throw new AssetFormatError('Material texture is not PNG');
      }
      pbr.baseColorTexture = { index: addImage(png, material.textureId) };
      const scale = material.textureScale ?? [1, 1];
      const offset = material.textureOffset ?? [0, 0];
      if (![scale, offset].every(values => Array.isArray(values) && values.length === 2
        && values.every(Number.isFinite))) throw new AssetFormatError('Material texture transform is invalid');
      if (scale[0] !== 1 || scale[1] !== 1 || offset[0] !== 0 || offset[1] !== 0) {
        pbr.baseColorTexture.extensions = { KHR_texture_transform: {
          scale,
          offset: [offset[0], 1 - scale[1] - offset[1]],
        } };
      }
    }
    if (material.metallicRoughnessTexturePng) {
      const png = Buffer.from(material.metallicRoughnessTexturePng, 'base64');
      if (png.subarray(0, 8).toString('hex') !== '89504e470d0a1a0a')
        throw new AssetFormatError('Material metallic/roughness texture is not PNG');
      const textureIndex = addImage(png, material.metallicRoughnessTextureId);
      const scale = material.metallicRoughnessTextureScale ?? [1, 1];
      const offset = material.metallicRoughnessTextureOffset ?? [0, 0];
      if (![scale, offset].every(values => Array.isArray(values) && values.length === 2
        && values.every(Number.isFinite)))
        throw new AssetFormatError('Material metallic/roughness texture transform is invalid');
      pbr.metallicRoughnessTexture = { index: textureIndex };
      if (scale[0] !== 1 || scale[1] !== 1 || offset[0] !== 0 || offset[1] !== 0)
        pbr.metallicRoughnessTexture.extensions = { KHR_texture_transform: {
          scale, offset: [offset[0], 1 - scale[1] - offset[1]],
        } };
    }
    const result = { name: material.name ?? '', pbrMetallicRoughness: pbr,
      alphaMode: material.alphaMode ?? (color[3] < 1 ? 'BLEND' : 'OPAQUE'),
      doubleSided: Boolean(material.doubleSided),
      extras: { unityMaterialId: material.id ?? null,
        unityShaderGuid: material.shaderGuid ?? null,
        unityShaderName: material.shaderName ?? null,
        previewMaterialModel: material.previewMaterialModel ?? 'urp-lit-v1',
        shaderCompatibility: material.shaderCompatibility ?? 'legacy-fallback',
        shaderAdapter: material.shaderAdapter ?? null,
        shaderTarget: material.shaderTarget ?? null,
        urpSurfaceMode: material.urpSurfaceMode ?? null,
        urpBlendMode: material.urpBlendMode ?? null } };
    if (material.normalTexturePng) {
      if (!source.tangents?.length) throw new AssetFormatError(
        `Normal-mapped material needs mesh tangents: ${material.name ?? ''}`);
      const png = Buffer.from(material.normalTexturePng, 'base64');
      if (png.subarray(0, 8).toString('hex') !== '89504e470d0a1a0a')
        throw new AssetFormatError('Material normal texture is not PNG');
      const textureIndex = addImage(png, material.normalTextureId);
      const scale = material.normalTextureScale ?? [1, 1];
      const offset = material.normalTextureOffset ?? [0, 0];
      if (![scale, offset].every(values => Array.isArray(values) && values.length === 2
        && values.every(Number.isFinite)))
        throw new AssetFormatError('Material normal texture transform is invalid');
      result.normalTexture = { index: textureIndex,
        scale: Number.isFinite(material.normalScale) ? material.normalScale : 1 };
      if (scale[0] !== 1 || scale[1] !== 1 || offset[0] !== 0 || offset[1] !== 0)
        result.normalTexture.extensions = { KHR_texture_transform: {
          scale, offset: [offset[0], 1 - scale[1] - offset[1]],
        } };
      // The source UV V coordinate is flipped for glTF. Reverse the green
      // tangent-space axis at load while keeping the exported Unity tangents.
      result.extras.unityNormalFlipY = true;
    }
    if (material.occlusionTexturePng) {
      const png = Buffer.from(material.occlusionTexturePng, 'base64');
      if (png.subarray(0, 8).toString('hex') !== '89504e470d0a1a0a')
        throw new AssetFormatError('Material occlusion texture is not PNG');
      const textureIndex = addImage(png, material.occlusionTextureId);
      const scale = material.occlusionTextureScale ?? [1, 1];
      const offset = material.occlusionTextureOffset ?? [0, 0];
      if (![scale, offset].every(values => Array.isArray(values) && values.length === 2
        && values.every(Number.isFinite)))
        throw new AssetFormatError('Material occlusion texture transform is invalid');
      if (!Number.isFinite(material.occlusionStrength ?? 1))
        throw new AssetFormatError('Material occlusion strength is invalid');
      result.occlusionTexture = { index: textureIndex,
        strength: clamp01(material.occlusionStrength ?? 1) };
      if (scale[0] !== 1 || scale[1] !== 1 || offset[0] !== 0 || offset[1] !== 0)
        result.occlusionTexture.extensions = { KHR_texture_transform: {
          scale, offset: [offset[0], 1 - scale[1] - offset[1]],
        } };
    }
    if (!['OPAQUE', 'MASK', 'BLEND'].includes(result.alphaMode)) {
      throw new AssetFormatError('Material alpha mode is invalid');
    }
    if (result.alphaMode === 'MASK') result.alphaCutoff = clamp01(material.alphaCutoff ?? 0.5);
    if (material.emissive) {
      if (!Array.isArray(material.emissive) || material.emissive.length !== 3
        || !material.emissive.every(Number.isFinite)) throw new AssetFormatError('Material emission is invalid');
      const strength = Math.max(1, ...material.emissive);
      result.emissiveFactor = material.emissive.map(channel => clamp01(channel / strength));
      if (strength > 1) result.extensions = { ...result.extensions,
        KHR_materials_emissive_strength: { emissiveStrength: strength } };
    }
    return result;
  });
  if (!gltfMaterials.length) gltfMaterials.push({ pbrMetallicRoughness: {
    baseColorFactor: [1, 1, 1, 1], metallicFactor: 0, roughnessFactor: 1,
  } });

  const primitives = submeshes.map(submesh => {
    const indices = submesh?.indices;
    if (!Array.isArray(indices) || !indices.length || indices.length % 3
      || !indices.every(index => Number.isInteger(index) && index >= 0 && index < vertexCount)) {
      throw new AssetFormatError('Submesh triangle indices are invalid');
    }
    const bytes = Buffer.allocUnsafe(indices.length * 4);
    indices.forEach((index, offset) => bytes.writeUInt32LE(index, offset * 4));
    accessors.push({ bufferView: view(bytes, 34963), componentType: 5125,
      count: indices.length, type: 'SCALAR' });
    const material = submesh.material >= 0 && submesh.material < gltfMaterials.length
      ? submesh.material : 0;
    return { attributes, indices: accessors.length - 1, material, mode: 4,
      ...(morphTargets.length ? { targets: morphTargets,
        extras: { unityBlendShapes: blendShapes } } : {}) };
  });
  const gltf = { asset: { version: '2.0', generator: 'unity-three-runtime' },
    scene: 0, scenes: [{ nodes: [0] }], nodes: [{ mesh: 0 }],
    meshes: [{ primitives, extras: { unityMeshId: source.meshId ?? null,
      ...(morphTargets.length ? { targetNames: morphFrames.map(({ shape, frame }) =>
        `${shape.name}@${frame.weight}`), unityBlendShapes: blendShapes } : {}) } }],
    materials: gltfMaterials, accessors, bufferViews, buffers: [{ byteLength }] };
  if (gltfMaterials.some(material => material.extensions?.KHR_materials_emissive_strength))
    gltf.extensionsUsed = [...(gltf.extensionsUsed ?? []), 'KHR_materials_emissive_strength'];
  if (gltfMaterials.some(material => material.pbrMetallicRoughness.baseColorTexture
    ?.extensions?.KHR_texture_transform || material.pbrMetallicRoughness.metallicRoughnessTexture
    ?.extensions?.KHR_texture_transform || material.normalTexture
    ?.extensions?.KHR_texture_transform || material.occlusionTexture
    ?.extensions?.KHR_texture_transform)) gltf.extensionsUsed = [
      ...(gltf.extensionsUsed ?? []), 'KHR_texture_transform'];
  if (source.skin) {
    const { bones, inverseBindMatrices } = source.skin;
    for (const [index, bone] of bones.entries()) {
      if (!Number.isInteger(bone.parent) || bone.parent >= bones.length || bone.parent === index
        || !['translation', 'scale'].every(key => Array.isArray(bone[key])
          && bone[key].length === 3 && bone[key].every(Number.isFinite))
        || !Array.isArray(bone.rotation) || bone.rotation.length !== 4
        || !bone.rotation.every(Number.isFinite)) throw new AssetFormatError('Skin bone is invalid');
      gltf.nodes.push({ name: bone.name || `Bone${index}`, translation: bone.translation,
        rotation: bone.rotation, scale: bone.scale,
        extras: { unityBonePath: bone.path ?? null } });
    }
    for (const [index, bone] of bones.entries()) {
      if (bone.parent >= 0) {
        (gltf.nodes[bone.parent + 1].children ??= []).push(index + 1);
      } else gltf.scenes[0].nodes.push(index + 1);
    }
    gltf.nodes[0].skin = 0;
    gltf.skins = [{ joints: bones.map((_, index) => index + 1),
      inverseBindMatrices: floatAccessor(inverseBindMatrices, 16, 'INVERSE_BIND', null) }];
  }
  if (source.animations) {
    if (!source.skin || !Array.isArray(source.animations)) {
      throw new AssetFormatError('Animation needs a skin and clip list');
    }
    gltf.animations = source.animations.map(clip => {
      const samplers = [];
      const channels = [];
      for (const track of clip.tracks ?? []) {
        const size = { translation: 3, rotation: 4, scale: 3 }[track.path];
        if (!size || !Number.isInteger(track.bone) || track.bone < 0
          || track.bone >= source.skin.bones.length || !Array.isArray(track.times)
          || !track.times.length || !track.times.every(Number.isFinite)
          || track.times.some((time, index) => index && time <= track.times[index - 1])
          || !Array.isArray(track.values) || track.values.length !== track.times.length * size
          || !track.values.every(Number.isFinite)) {
          throw new AssetFormatError('Animation track is invalid');
        }
        const input = floatAccessor(track.times, 1, 'TIME', null);
        const output = floatAccessor(track.values, size, 'ANIMATION', null);
        channels.push({ sampler: samplers.length,
          target: { node: track.bone + 1, path: track.path } });
        samplers.push({ input, output, interpolation: 'LINEAR' });
      }
      return { name: clip.name || 'Animation', samplers, channels };
    });
  }
  if (images.length) { gltf.images = images; gltf.textures = textures; }
  const json = Buffer.from(JSON.stringify(gltf));
  const paddedJson = Buffer.alloc(align(json.length), 0x20);
  json.copy(paddedJson);
  const binary = Buffer.alloc(byteLength);
  let offset = 0;
  for (const chunk of chunks) { chunk.copy(binary, offset); offset += align(chunk.length); }
  const output = Buffer.alloc(12 + 8 + paddedJson.length + 8 + binary.length);
  output.write('glTF', 0);
  output.writeUInt32LE(2, 4);
  output.writeUInt32LE(output.length, 8);
  output.writeUInt32LE(paddedJson.length, 12);
  output.write('JSON', 16);
  paddedJson.copy(output, 20);
  const binaryHeader = 20 + paddedJson.length;
  output.writeUInt32LE(binary.length, binaryHeader);
  output.write('BIN\0', binaryHeader + 4);
  binary.copy(output, binaryHeader + 8);
  return output;
}
