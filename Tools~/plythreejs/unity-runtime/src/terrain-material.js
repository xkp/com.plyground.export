import * as THREE from 'three';

/** One focused WebGL terrain mapping on the existing lit Three.js material. */
export function createTerrainLayerMaterial(data, layers) {
  if (!data.alphamaps || layers.length !== data.alphamapLayers
    || layers.length < 2 || layers.length > 8
    || layers.some(layer => !(layer?.texture?.threeTexture ?? layer?.texture)?.isTexture
      || !(layer.tileSize?.x > 0) || !(layer.tileSize?.y > 0)))
    return null;
  const weightTextures = Array.from({ length: Math.ceil(layers.length / 4) }, (_, group) => {
    const pixels = new Uint8Array(data.alphamapResolution ** 2 * 4);
    for (let pixel = 0; pixel < data.alphamapResolution ** 2; pixel++)
      for (let channel = 0; channel < 4 && group * 4 + channel < layers.length; channel++)
        pixels[pixel * 4 + channel] = Math.round(
          data.alphamaps[pixel * layers.length + group * 4 + channel] * 255);
    const texture = new THREE.DataTexture(pixels, data.alphamapResolution,
      data.alphamapResolution, THREE.RGBAFormat);
    texture.magFilter = THREE.LinearFilter;
    texture.minFilter = THREE.LinearFilter;
    texture.needsUpdate = true;
    return texture;
  });
  const maps = layers.map(layer => {
    const map = (layer.texture.threeTexture ?? layer.texture).clone();
    map.wrapS = map.wrapT = THREE.RepeatWrapping;
    map.flipY = true;
    map.needsUpdate = true;
    return map;
  });
  const material = new THREE.MeshStandardMaterial({ color: 0xffffff, side: THREE.DoubleSide });
  material.defines = { USE_UV: '' };
  material.userData.terrainLayerMaps = maps;
  material.userData.terrainWeights = weightTextures;
  material.customProgramCacheKey = () => `unity-terrain-layers-${layers.length}`;
  material.onBeforeCompile = shader => {
    const uniforms = [];
    const samples = ['vec4 terrainBlend = vec4(0.0);', 'float terrainWeightSum = 0.0;'];
    for (let group = 0; group < weightTextures.length; group++) {
      shader.uniforms[`terrainWeights${group}`] = { value: weightTextures[group] };
      uniforms.push(`uniform sampler2D terrainWeights${group};`);
      samples.push(`vec4 terrainWeight${group} = texture2D(terrainWeights${group}, vUv);`);
    }
    const channels = 'rgba';
    for (let index = 0; index < layers.length; index++) {
      const layer = layers[index];
      const tile = new THREE.Vector4(data.size.x / layer.tileSize.x,
        data.size.z / layer.tileSize.y, layer.tileOffset?.x ?? 0, layer.tileOffset?.y ?? 0);
      shader.uniforms[`terrainMap${index}`] = { value: maps[index] };
      shader.uniforms[`terrainTile${index}`] = { value: tile };
      uniforms.push(`uniform sampler2D terrainMap${index};`,
        `uniform vec4 terrainTile${index};`);
      samples.push(`terrainBlend += texture2D(terrainMap${index},`
        + ` vUv * terrainTile${index}.xy + terrainTile${index}.zw)`
        + ` * terrainWeight${Math.floor(index / 4)}.${channels[index % 4]};`);
      samples.push(`terrainWeightSum += terrainWeight${Math.floor(index / 4)}`
        + `.${channels[index % 4]};`);
    }
    samples.push('diffuseColor.rgb *= terrainBlend.rgb / max(terrainWeightSum, 0.0001);');
    shader.fragmentShader = shader.fragmentShader
      .replace('#include <common>', `#include <common>\n${uniforms.join('\n')}`)
      .replace('#include <map_fragment>', samples.join('\n'));
  };
  return material;
}

export function disposeTerrainLayerMaterial(material) {
  for (const map of material.userData.terrainLayerMaps ?? []) map.dispose();
  for (const weights of material.userData.terrainWeights ?? []) weights.dispose();
}
