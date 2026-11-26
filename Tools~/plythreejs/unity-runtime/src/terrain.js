import * as THREE from 'three';
import { Vector3 } from './math.js';

/** Authoritative Unity height samples, stored row-major with the Z row first. */
export class TerrainData {
  constructor({ size, resolution, heights, holes = null,
    alphamapResolution = 0, alphamapLayers = 0, alphamaps = null } = {}) {
    if (!Array.isArray(size) || size.length !== 3 || size.some(value => !Number.isFinite(value) || value <= 0)
      || !Number.isInteger(resolution) || resolution < 2
      || !Array.isArray(heights) || heights.length !== resolution * resolution
      || heights.some(value => !Number.isFinite(value) || value < 0 || value > 1))
      throw new Error('Invalid exported TerrainData heightfield');
    if (holes && (!Array.isArray(holes) || holes.length !== (resolution - 1) ** 2
      || holes.some(value => typeof value !== 'boolean')))
      throw new Error('Invalid exported TerrainData holes');
    if (alphamaps && (!Number.isInteger(alphamapResolution) || alphamapResolution < 1
      || !Number.isInteger(alphamapLayers) || alphamapLayers < 2 || alphamapLayers > 8
      || !Array.isArray(alphamaps)
      || alphamaps.length !== alphamapResolution ** 2 * alphamapLayers
      || alphamaps.some(value => !Number.isFinite(value) || value < 0 || value > 1)))
      throw new Error('Invalid exported TerrainData alphamaps');
    this.size = new Vector3(...size);
    this.heightmapResolution = resolution;
    this.heights = Float32Array.from(heights);
    this.holes = holes ? Uint8Array.from(holes.map(Boolean)) : null;
    this.alphamapResolution = alphamaps ? alphamapResolution : 0;
    this.alphamapLayers = alphamaps ? alphamapLayers : 0;
    this.alphamaps = alphamaps ? Float32Array.from(alphamaps) : null;
  }
  GetHeight(x, y) {
    if (!Number.isInteger(x) || !Number.isInteger(y) || x < 0 || y < 0
      || x >= this.heightmapResolution || y >= this.heightmapResolution)
      throw new RangeError('TerrainData height coordinates are outside the heightmap');
    return this.heights[y * this.heightmapResolution + x] * this.size.y;
  }
  GetHeights(xBase, yBase, width, height) {
    if (![xBase, yBase, width, height].every(Number.isInteger) || xBase < 0 || yBase < 0
      || width < 0 || height < 0 || xBase + width > this.heightmapResolution
      || yBase + height > this.heightmapResolution)
      throw new RangeError('TerrainData height rectangle is outside the heightmap');
    return Array.from({ length: height }, (_, row) =>
      Array.from(this.heights.subarray((yBase + row) * this.heightmapResolution + xBase,
        (yBase + row) * this.heightmapResolution + xBase + width)));
  }
  GetAlphamaps(xBase, yBase, width, height) {
    const resolution = this.alphamapResolution;
    if (!resolution || ![xBase, yBase, width, height].every(Number.isInteger)
      || xBase < 0 || yBase < 0 || width < 0 || height < 0
      || xBase + width > resolution || yBase + height > resolution)
      throw new RangeError('TerrainData alphamap rectangle is outside the alphamap');
    return Array.from({ length: height }, (_, row) =>
      Array.from({ length: width }, (_, column) =>
        Array.from(this.alphamaps.subarray(
          ((yBase + row) * resolution + xBase + column) * this.alphamapLayers,
          ((yBase + row) * resolution + xBase + column + 1) * this.alphamapLayers))));
  }
  GetInterpolatedHeight(x, y) {
    const count = this.heightmapResolution - 1;
    const column = Math.max(0, Math.min(count, x * count));
    const row = Math.max(0, Math.min(count, y * count));
    const x0 = Math.min(count - 1, Math.floor(column));
    const y0 = Math.min(count - 1, Math.floor(row));
    const u = column - x0;
    const v = row - y0;
    const a = this.GetHeight(x0, y0), b = this.GetHeight(x0 + 1, y0);
    const c = this.GetHeight(x0, y0 + 1), d = this.GetHeight(x0 + 1, y0 + 1);
    // Match the triangle split used by the render and collider geometry.
    return u + v <= 1 ? a + (b - a) * u + (c - a) * v
      : d + (c - d) * (1 - u) + (b - d) * (1 - v);
  }
  IsHole(x, y) {
    const count = this.heightmapResolution - 1;
    if (x < 0 || y < 0 || x >= count || y >= count) return false;
    return Boolean(this.holes?.[y * count + x]);
  }
  createGeometry() {
    const count = this.heightmapResolution - 1;
    const positions = new Float32Array(this.heightmapResolution ** 2 * 3);
    const uvs = new Float32Array(this.heightmapResolution ** 2 * 2);
    const indices = [];
    for (let y = 0; y <= count; y++) for (let x = 0; x <= count; x++) {
      const offset = (y * this.heightmapResolution + x) * 3;
      positions[offset] = x / count * this.size.x;
      positions[offset + 1] = this.GetHeight(x, y);
      positions[offset + 2] = y / count * this.size.z;
      uvs[(y * this.heightmapResolution + x) * 2] = x / count;
      uvs[(y * this.heightmapResolution + x) * 2 + 1] = y / count;
      if (x === count || y === count || this.IsHole(x, y)) continue;
      const a = y * this.heightmapResolution + x;
      const b = a + 1, c = a + this.heightmapResolution, d = c + 1;
      indices.push(a, c, b, b, c, d);
    }
    const geometry = new THREE.BufferGeometry();
    geometry.setAttribute('position', new THREE.BufferAttribute(positions, 3));
    geometry.setAttribute('uv', new THREE.BufferAttribute(uvs, 2));
    geometry.setIndex(indices);
    geometry.computeVertexNormals();
    return geometry;
  }
}
