// Unity 6000 Animator.StringToHash matches signed CRC32 of UTF-8 text.
const crcTable = new Uint32Array(256);
for (let value = 0; value < 256; value++) {
  let crc = value;
  for (let bit = 0; bit < 8; bit++)
    crc = (crc >>> 1) ^ ((crc & 1) ? 0xEDB88320 : 0);
  crcTable[value] = crc >>> 0;
}
const encoder = new TextEncoder();

export function animatorStringToHash(value) {
  if (value == null) return 0;
  if (typeof value !== 'string') throw new TypeError('Animator.StringToHash expects a string');
  let crc = 0xFFFFFFFF;
  for (const byte of encoder.encode(value))
    crc = crcTable[(crc ^ byte) & 0xFF] ^ (crc >>> 8);
  return (~crc) | 0;
}
