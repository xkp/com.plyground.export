import { spookyHash128 } from './spookyhash-v2.js';

// Hash128 stores 16 bytes. Unity's uint/ulong constructors write each word in
// little-endian order; Parse and ToString operate on the displayed byte order.
function unsigned64(value) {
  if (typeof value === 'number' && !Number.isSafeInteger(value))
    throw new RangeError('Hash128 ulong constructor requires exact integers or BigInt');
  const integer = BigInt(value);
  if (integer < 0n || integer > 0xffffffffffffffffn)
    throw new RangeError('Hash128 ulong constructor requires a 64-bit unsigned value');
  return integer;
}

function genericBytes(values, start, count, elementType) {
  if (!Array.isArray(values) && !ArrayBuffer.isView(values))
    throw new TypeError('Hash128 generic data requires an array, list, or NativeArray');
  const length = values.length;
  if (!Number.isInteger(start) || !Number.isInteger(count)
    || start < 0 || count < 0 || start + count > length)
    throw new RangeError('Hash128 generic data range is invalid');
  const stride = elementType === 'byte' ? 1 : 4;
  if (!['byte', 'int', 'float'].includes(elementType))
    throw new TypeError(`Hash128 generic element type ${elementType} is not supported`);
  const bytes = new Uint8Array(count * stride);
  const view = new DataView(bytes.buffer);
  for (let index = 0; index < count; index++) {
    const value = values[start + index];
    if (elementType === 'byte') view.setUint8(index, value);
    else if (elementType === 'float') view.setFloat32(index * 4, value, true);
    else view.setInt32(index * 4, value, true);
  }
  return bytes;
}

export class Hash128 {
  constructor(a = 0, b = 0, c, d) {
    this._bytes = new Uint8Array(16);
    const view = new DataView(this._bytes.buffer);
    if (c !== undefined && d !== undefined) {
      [a, b, c, d].forEach((word, index) => view.setUint32(index * 4, Number(word) >>> 0, true));
    } else if (arguments.length === 2) {
      view.setBigUint64(0, unsigned64(a), true);
      view.setBigUint64(8, unsigned64(b), true);
    }
  }
  static Parse(value) {
    const result = new Hash128();
    const text = String(value ?? '');
    for (let index = 0; index < 16 && index * 2 + 1 < text.length; index++) {
      const pair = text.slice(index * 2, index * 2 + 2);
      if (!/^[0-9a-f]{2}$/i.test(pair)) break;
      result._bytes[index] = Number.parseInt(pair, 16);
    }
    return result;
  }
  static Compute(value) { const result = new Hash128(); result.Append(value); return result; }
  static ComputeFloat(value) { const result = new Hash128(); result.AppendFloat(value); return result; }
  static ComputeGeneric(values, start = 0, count = values.length - start, elementType = 'int') {
    const result = new Hash128();
    result.AppendGeneric(values, start, count, elementType);
    return result;
  }
  Append(value) {
    let bytes;
    if (typeof value === 'string') bytes = new TextEncoder().encode(value);
    else if (typeof value === 'number') {
      bytes = new Uint8Array(4);
      new DataView(bytes.buffer).setInt32(0, value | 0, true);
    } else if (value instanceof Uint8Array) bytes = value;
    else if (Array.isArray(value)) bytes = Uint8Array.from(value);
    else throw new TypeError('Hash128.Append requires a string, int, float, or byte array');
    this._appendBytes(bytes);
  }
  AppendFloat(value) {
    const bytes = new Uint8Array(4);
    new DataView(bytes.buffer).setFloat32(0, value, true);
    this._appendBytes(bytes);
  }
  AppendGeneric(values, start = 0, count = values.length - start, elementType = 'int') {
    this._appendBytes(genericBytes(values, start, count, elementType));
  }
  _appendBytes(bytes) {
    const view = new DataView(this._bytes.buffer);
    const [first, second] = spookyHash128(bytes, view.getBigUint64(0, true), view.getBigUint64(8, true));
    view.setBigUint64(0, first, true);
    view.setBigUint64(8, second, true);
  }
  get isValid() { return this._bytes.some(byte => byte !== 0); }
  ToString() { return Array.from(this._bytes, byte => byte.toString(16).padStart(2, '0')).join(''); }
  toString() { return this.ToString(); }
  clone() { const result = new Hash128(); result._bytes.set(this._bytes); return result; }
  Equals(other) {
    return other instanceof Hash128 && this._bytes.every((byte, index) => byte === other._bytes[index]);
  }
  CompareTo(other) {
    if (!(other instanceof Hash128)) return 1;
    const left = new DataView(this._bytes.buffer);
    const right = new DataView(other._bytes.buffer);
    for (const offset of [0, 8]) {
      const a = left.getBigUint64(offset, true);
      const b = right.getBigUint64(offset, true);
      if (a !== b) return a < b ? -1 : 1;
    }
    return 0;
  }
  GetHashCode() {
    const view = new DataView(this._bytes.buffer);
    return (view.getInt32(0, true) ^ view.getInt32(4, true)
      ^ view.getInt32(8, true) ^ view.getInt32(12, true)) | 0;
  }
  static op_Equality(left, right) { return left.Equals(right); }
  static op_Inequality(left, right) { return !left.Equals(right); }
  static op_LessThan(left, right) { return left.CompareTo(right) < 0; }
  static op_GreaterThan(left, right) { return left.CompareTo(right) > 0; }
}
