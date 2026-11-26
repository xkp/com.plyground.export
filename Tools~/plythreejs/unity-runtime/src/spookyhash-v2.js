// SpookyHash V2 by Bob Jenkins (public domain), translated to BigInt for exact
// 64-bit arithmetic. Unity Hash128 uses this hash for byte sequences.
const mask = (1n << 64n) - 1n;
const constant = 0xdeadbeefdeadbeefn;
const wrap = value => value & mask;
const rot = (value, bits) => wrap((value << BigInt(bits)) | (value >> BigInt(64 - bits)));

function word(bytes, offset, length = 8) {
  let value = 0n;
  for (let index = 0; index < length; index++) value |= BigInt(bytes[offset + index]) << BigInt(index * 8);
  return value;
}

function shortMix(h) {
  for (const [target, bits, add, xor] of [
    [2, 50, 3, 0], [3, 52, 0, 1], [0, 30, 1, 2], [1, 41, 2, 3],
    [2, 54, 3, 0], [3, 48, 0, 1], [0, 38, 1, 2], [1, 37, 2, 3],
    [2, 62, 3, 0], [3, 34, 0, 1], [0, 5, 1, 2], [1, 36, 2, 3],
  ]) {
    h[target] = wrap(rot(h[target], bits) + h[add]);
    h[xor] ^= h[target];
  }
}

function shortEnd(h) {
  for (const [xor, source, bits, add] of [
    [3, 2, 15, 3], [0, 3, 52, 0], [1, 0, 26, 1], [2, 1, 51, 2],
    [3, 2, 28, 3], [0, 3, 9, 0], [1, 0, 47, 1], [2, 1, 54, 2],
    [3, 2, 32, 3], [0, 3, 25, 0], [1, 0, 63, 1],
  ]) {
    h[xor] ^= h[source];
    h[source] = rot(h[source], bits);
    h[add] = wrap(h[add] + h[source]);
  }
}

function shortHash(bytes, seed0, seed1) {
  const h = [seed0, seed1, constant, constant];
  let offset = 0;
  if (bytes.length > 15) {
    while (bytes.length - offset >= 32) {
      h[2] = wrap(h[2] + word(bytes, offset));
      h[3] = wrap(h[3] + word(bytes, offset + 8));
      shortMix(h);
      h[0] = wrap(h[0] + word(bytes, offset + 16));
      h[1] = wrap(h[1] + word(bytes, offset + 24));
      offset += 32;
    }
    if (bytes.length - offset >= 16) {
      h[2] = wrap(h[2] + word(bytes, offset));
      h[3] = wrap(h[3] + word(bytes, offset + 8));
      shortMix(h);
      offset += 16;
    }
  }
  const remainder = bytes.length - offset;
  h[3] = wrap(h[3] + (BigInt(bytes.length) << 56n));
  if (remainder === 0) {
    h[2] = wrap(h[2] + constant);
    h[3] = wrap(h[3] + constant);
  } else {
    h[2] = wrap(h[2] + word(bytes, offset, Math.min(remainder, 8)));
    if (remainder > 8) h[3] = wrap(h[3] + word(bytes, offset + 8, remainder - 8));
  }
  shortEnd(h);
  return h.slice(0, 2);
}

function mix(data, h) {
  const rotations = [11, 32, 43, 31, 17, 28, 39, 57, 55, 54, 22, 46];
  for (let i = 0; i < 12; i++) {
    h[i] = wrap(h[i] + data[i]);
    h[(i + 2) % 12] ^= h[(i + 10) % 12];
    h[(i + 11) % 12] ^= h[i];
    h[i] = rot(h[i], rotations[i]);
    h[(i + 11) % 12] = wrap(h[(i + 11) % 12] + h[(i + 1) % 12]);
  }
}

function endPartial(h) {
  const rotations = [44, 15, 34, 21, 38, 33, 10, 13, 38, 53, 42, 54];
  for (let i = 0; i < 12; i++) {
    h[(i + 11) % 12] = wrap(h[(i + 11) % 12] + h[(i + 1) % 12]);
    h[(i + 2) % 12] ^= h[(i + 11) % 12];
    h[(i + 1) % 12] = rot(h[(i + 1) % 12], rotations[i]);
  }
}

function longHash(bytes, seed0, seed1) {
  const h = Array.from({ length: 12 }, (_, i) => i % 3 === 0 ? seed0 : i % 3 === 1 ? seed1 : constant);
  let offset = 0;
  while (offset + 96 <= bytes.length) {
    mix(Array.from({ length: 12 }, (_, i) => word(bytes, offset + i * 8)), h);
    offset += 96;
  }
  const tail = new Uint8Array(96);
  tail.set(bytes.subarray(offset));
  tail[95] = bytes.length - offset;
  for (let i = 0; i < 12; i++) h[i] = wrap(h[i] + word(tail, i * 8));
  endPartial(h); endPartial(h); endPartial(h);
  return h.slice(0, 2);
}

export function spookyHash128(bytes, seed0 = 0n, seed1 = 0n) {
  if (!(bytes instanceof Uint8Array)) throw new TypeError('SpookyHash input must be bytes');
  return bytes.length < 192 ? shortHash(bytes, seed0, seed1) : longHash(bytes, seed0, seed1);
}
