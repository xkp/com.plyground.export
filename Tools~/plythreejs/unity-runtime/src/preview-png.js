import { inflateSync } from 'node:zlib';

/** Decode exported 8-bit RGB/RGBA PNGs for headless Unity Texture2D reads. */
export function decodePreviewPng(bytes) {
  if (bytes.subarray(0, 8).toString('hex') !== '89504e470d0a1a0a')
    throw new Error('Preview texture is not a PNG');
  let width = 0, height = 0, channels = 0;
  const chunks = [];
  for (let offset = 8; offset + 12 <= bytes.length;) {
    const length = bytes.readUInt32BE(offset);
    const end = offset + 12 + length;
    if (end > bytes.length) throw new Error('Preview PNG chunk is truncated');
    const type = bytes.toString('ascii', offset + 4, offset + 8);
    if (type === 'IHDR') {
      width = bytes.readUInt32BE(offset + 8);
      height = bytes.readUInt32BE(offset + 12);
      const depth = bytes[offset + 16], color = bytes[offset + 17];
      if (depth !== 8 || ![2, 6].includes(color) || bytes[offset + 20] !== 0)
        throw new Error(`Unsupported Preview PNG format: depth ${depth}, color ${color}`);
      channels = color === 6 ? 4 : 3;
    } else if (type === 'IDAT') chunks.push(bytes.subarray(offset + 8, offset + 8 + length));
    if (type === 'IEND') break;
    offset = end;
  }
  if (!width || !height || width * height > 100_000_000 || !chunks.length)
    throw new Error('Preview PNG has invalid dimensions or no image data');
  const stride = width * channels;
  const raw = inflateSync(Buffer.concat(chunks));
  if (raw.length !== (stride + 1) * height) throw new Error('Preview PNG pixel data is incomplete');
  const rgba = new Uint8Array(width * height * 4);
  let previous = new Uint8Array(stride);
  for (let y = 0; y < height; y++) {
    const row = new Uint8Array(stride);
    const start = y * (stride + 1);
    const filter = raw[start];
    if (filter > 4) throw new Error(`Unsupported Preview PNG filter ${filter}`);
    for (let x = 0; x < stride; x++) {
      const left = x >= channels ? row[x - channels] : 0;
      const above = previous[x];
      const upperLeft = x >= channels ? previous[x - channels] : 0;
      const predictor = left + above - upperLeft;
      const distances = [Math.abs(predictor - left), Math.abs(predictor - above),
        Math.abs(predictor - upperLeft)];
      const paeth = distances[0] <= distances[1] && distances[0] <= distances[2] ? left
        : distances[1] <= distances[2] ? above : upperLeft;
      row[x] = (raw[start + 1 + x] + [0, left, above, Math.floor((left + above) / 2), paeth][filter]) & 255;
    }
    for (let x = 0; x < width; x++) {
      const from = x * channels, to = (y * width + x) * 4;
      rgba[to] = row[from]; rgba[to + 1] = row[from + 1]; rgba[to + 2] = row[from + 2];
      rgba[to + 3] = channels === 4 ? row[from + 3] : 255;
    }
    previous = row;
  }
  return { width, height, rgba };
}
