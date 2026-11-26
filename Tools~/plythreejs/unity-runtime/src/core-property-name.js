const namesById = new Map();
const crcTable = Array.from({ length: 256 }, (_, value) => {
  let crc = value;
  for (let bit = 0; bit < 8; bit++)
    crc = crc & 1 ? (crc >>> 1) ^ 0xedb88320 : crc >>> 1;
  return crc >>> 0;
});

function stringId(value) {
  if (value === '') return 0;
  let crc = 0xffffffff;
  for (const byte of new TextEncoder().encode(value))
    crc = (crc >>> 8) ^ crcTable[(crc ^ byte) & 0xff];
  return (crc ^ 0xffffffff) | 0;
}

export class PropertyName {
  constructor(value = 0) {
    if (value instanceof PropertyName) this._id = value._id;
    else if (typeof value === 'string') {
      this._id = stringId(value);
      if (value) namesById.set(this._id, value);
    } else if (Number.isInteger(value)) this._id = value | 0;
    else throw new TypeError('PropertyName requires a string, int, or PropertyName');
  }
  static IsNullOrEmpty(value) { return value instanceof PropertyName && value._id === 0; }
  static op_Implicit(value) { return new PropertyName(value); }
  static op_Equality(left, right) { return left instanceof PropertyName
    && right instanceof PropertyName && left._id === right._id; }
  static op_Inequality(left, right) { return !PropertyName.op_Equality(left, right); }
  clone() { return new PropertyName(this); }
  Equals(other) { return PropertyName.op_Equality(this, other); }
  GetHashCode() { return this._id; }
  ToString() {
    if (this._id === 0) return ':0';
    return `${namesById.get(this._id) ?? `#UNKNOWN:${this._id}`}:${this._id}`;
  }
  toString() { return this.ToString(); }
}
