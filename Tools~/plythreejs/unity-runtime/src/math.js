import * as THREE from 'three';
import { ImprovedNoise } from 'three/addons/math/ImprovedNoise.js';

const unityPerlinNoise = new ImprovedNoise();

function smoothDampVector(current, target, currentVelocity, smoothTime, maxSpeed, deltaTime) {
  const velocity = currentVelocity?.value ?? currentVelocity;
  smoothTime = Math.max(0.0001, smoothTime);
  const omega = 2 / smoothTime;
  const x = omega * deltaTime;
  const exp = 1 / (1 + x + 0.48 * x * x + 0.235 * x * x * x);
  const change = current.clone().sub(target);
  const maxChange = maxSpeed * smoothTime;
  if (change.length() > maxChange) change.setLength(maxChange);
  const adjustedTarget = current.clone().sub(change);
  const temp = velocity.clone().addScaledVector(change, omega).multiplyScalar(deltaTime);
  const nextVelocity = velocity.clone().addScaledVector(temp, -omega).multiplyScalar(exp);
  const result = adjustedTarget.clone().add(change.add(temp).multiplyScalar(exp));
  if (target.clone().sub(current).dot(result.clone().sub(target)) > 0) {
    result.copy(target);
    velocity.copy(result.clone().sub(target).divideScalar(deltaTime));
  } else velocity.copy(nextVelocity);
  if (currentVelocity?.value) currentVelocity.value = velocity;
  return result;
}

export class Vector2 extends THREE.Vector2 {
  static get kEpsilon() { return Math.fround(1e-5); }
  static get kEpsilonNormalSqrt() { return Math.fround(1e-15); }
  GetItem(index) { return this.getComponent(index); }
  SetItem(index, value) { this.setComponent(index, value); }
  Normalize() { this.normalize(); }
  Set(x, y) { this.set(x, y); }
  Scale(scale) { this.multiply(scale); }
  SqrMagnitude() { return this.lengthSq(); }
  Equals(other) { return other instanceof Vector2 && this.equals(other); }
  toVector3() { return new Vector3(this.x, this.y, 0); }
  toVector4() { return new Vector4(this.x, this.y, 0, 0); }
  static op_Equality(left, right) {
    const x = left.x - right.x, y = left.y - right.y;
    return x * x + y * y < 1e-10;
  }
  static op_Inequality(left, right) { return !Vector2.op_Equality(left, right); }
  get magnitude() { return this.length(); }
  get sqrMagnitude() { return this.lengthSq(); }
  get normalized() { return this.clone().normalize(); }
  static get zero() { return new Vector2(); }
  static get one() { return new Vector2(1, 1); }
  static get positiveInfinity() { return new Vector2(Infinity, Infinity); }
  static get negativeInfinity() { return new Vector2(-Infinity, -Infinity); }
  static get up() { return new Vector2(0, 1); }
  static get down() { return new Vector2(0, -1); }
  static get left() { return new Vector2(-1, 0); }
  static get right() { return new Vector2(1, 0); }
  static Distance(a, b) { return a.distanceTo(b); }
  static Dot(a, b) { return a.dot(b); }
  static Normalize(v) { return v.clone().normalize(); }
  static SqrMagnitude(v) { return v.lengthSq(); }
  static Lerp(a, b, t) { return a.clone().lerp(b, Mathf.Clamp01(t)); }
  static LerpUnclamped(a, b, t) { return a.clone().lerp(b, t); }
  static Angle(a, b) { return a.lengthSq() * b.lengthSq() < 1e-30 ? 0 : Math.acos(Mathf.Clamp(a.dot(b) / (a.length() * b.length()), -1, 1)) * Mathf.Rad2Deg; }
  static SignedAngle(a, b) { return Vector2.Angle(a, b) * Math.sign(a.x * b.y - a.y * b.x); }
  static ClampMagnitude(v, maxLength) { return v.length() > maxLength ? v.clone().setLength(maxLength) : v.clone(); }
  static MoveTowards(a, b, maxDistanceDelta) { const d = b.clone().sub(a); return d.length() <= maxDistanceDelta || !d.lengthSq() ? b.clone() : a.clone().add(d.setLength(maxDistanceDelta)); }
  static Scale(a, b) { return a.clone().multiply(b); }
  static Min(a, b) { return a.clone().min(b); }
  static Max(a, b) { return a.clone().max(b); }
  static Perpendicular(v) { return new Vector2(-v.y, v.x); }
  static Reflect(v, normal) { return v.clone().addScaledVector(normal, -2 * v.dot(normal)); }
  static SmoothDamp(current, target, currentVelocity, smoothTime, maxSpeed = Infinity, deltaTime = Mathf._defaultDeltaTime()) {
    return smoothDampVector(current, target, currentVelocity, smoothTime, maxSpeed, deltaTime);
  }
  GetHashCode() {
    const bits = value => {
      const view = new DataView(new ArrayBuffer(4));
      view.setFloat32(0, value, true);
      return view.getInt32(0, true);
    };
    return bits(this.x) ^ (bits(this.y) << 2);
  }
  ToString(format = 'F2', _provider = null) {
    const digits = /^F(\d+)$/i.exec(format ?? 'F2');
    const decimals = digits ? Number(digits[1]) : 2;
    return `(${[this.x, this.y].map(value => Number(value).toFixed(decimals)).join(', ')})`;
  }
  toString() { return this.ToString(); }
}

function formatInt32(value, format = null, provider = null) {
  const specifier = format || 'G';
  const match = /^([DFGNEX])(\d*)$/i.exec(specifier);
  if (!match) throw new RangeError(`Unsupported integer numeric format: ${specifier}`);
  const kind = match[1].toUpperCase();
  const precision = match[2] ? Number(match[2]) : kind === 'F' || kind === 'N' ? 2
    : kind === 'E' ? 6 : 0;
  if (precision > 99) throw new RangeError(`Integer numeric format precision is out of range: ${specifier}`);
  const numberFormat = provider?.NumberFormat ?? provider ?? {};
  const decimal = numberFormat.NumberDecimalSeparator ?? '.';
  const group = numberFormat.NumberGroupSeparator ?? ',';
  const integer = value | 0;
  if (kind === 'X') {
    const hex = (integer >>> 0).toString(16);
    const rendered = hex.padStart(precision, '0');
    return match[1] === 'x' ? rendered : rendered.toUpperCase();
  }
  const sign = integer < 0 ? '-' : '';
  const magnitude = Math.abs(integer);
  if (kind === 'D') return sign + String(magnitude).padStart(precision, '0');
  if (kind === 'G') return String(integer);
  if (kind === 'E') {
    const rendered = magnitude.toExponential(precision).replace(/e([+-])(\d+)$/i,
      (_, direction, exponent) => `${match[1] === 'e' ? 'e' : 'E'}${direction}${exponent.padStart(3, '0')}`);
    return sign + rendered.replace('.', decimal);
  }
  let rendered = magnitude.toFixed(precision);
  if (kind === 'N') rendered = rendered.replace(/^\d+/, digits =>
    digits.replace(/\B(?=(\d{3})+(?!\d))/g, group));
  return sign + rendered.replace('.', decimal);
}

export class Vector2Int {
  constructor(x = 0, y = 0) { this.x = x | 0; this.y = y | 0; }
  clone() { return new Vector2Int(this.x, this.y); }
  Set(x, y) { this.x = x | 0; this.y = y | 0; }
  GetItem(index) {
    if (index === 0) return this.x;
    if (index === 1) return this.y;
    throw new RangeError(`Invalid Vector2Int index addressed: ${index}`);
  }
  SetItem(index, value) {
    if (index === 0) this.x = value | 0;
    else if (index === 1) this.y = value | 0;
    else throw new RangeError(`Invalid Vector2Int index addressed: ${index}`);
  }
  get sqrMagnitude() { return (Math.imul(this.x, this.x) + Math.imul(this.y, this.y)) | 0; }
  get magnitude() { return Math.sqrt(this.sqrMagnitude); }
  add(other) { this.x = (this.x + other.x) | 0; this.y = (this.y + other.y) | 0; return this; }
  sub(other) { this.x = (this.x - other.x) | 0; this.y = (this.y - other.y) | 0; return this; }
  negate() { this.x = -this.x | 0; this.y = -this.y | 0; return this; }
  multiply(other) { this.x = Math.imul(this.x, other.x); this.y = Math.imul(this.y, other.y); return this; }
  multiplyScalar(value) { this.x = Math.imul(this.x, value); this.y = Math.imul(this.y, value); return this; }
  divideScalar(value) {
    if (value === 0) throw new RangeError('Vector2Int division by zero');
    this.x = Math.trunc(this.x / value) | 0;
    this.y = Math.trunc(this.y / value) | 0;
    return this;
  }
  Scale(scale) { this.multiply(scale); }
  Clamp(min, max) {
    this.x = Mathf.Clamp(this.x, min.x, max.x);
    this.y = Mathf.Clamp(this.y, min.y, max.y);
  }
  Equals(other) { return other instanceof Vector2Int && this.x === other.x && this.y === other.y; }
  GetHashCode() { return (Math.imul(this.x, 73856093) ^ Math.imul(this.y, 83492791)) | 0; }
  ToString(format = null, provider = null) {
    return `(${[this.x, this.y].map(value => formatInt32(value, format, provider)).join(', ')})`;
  }
  toVector2() { return new Vector2(this.x, this.y); }
  toVector3Int() { return new Vector3Int(this.x, this.y, 0); }
  static Distance(a, b) { return Math.hypot(a.x - b.x, a.y - b.y); }
  static Min(a, b) { return new Vector2Int(Math.min(a.x, b.x), Math.min(a.y, b.y)); }
  static Max(a, b) { return new Vector2Int(Math.max(a.x, b.x), Math.max(a.y, b.y)); }
  static Scale(a, b) { return new Vector2Int(Math.imul(a.x, b.x), Math.imul(a.y, b.y)); }
  static FloorToInt(v) { return new Vector2Int(Math.floor(v.x), Math.floor(v.y)); }
  static CeilToInt(v) { return new Vector2Int(Math.ceil(v.x), Math.ceil(v.y)); }
  static RoundToInt(v) { return new Vector2Int(Mathf.RoundToInt(v.x), Mathf.RoundToInt(v.y)); }
  static op_Equality(a, b) { return a === b || a != null && b != null && a.x === b.x && a.y === b.y; }
  static op_Inequality(a, b) { return !Vector2Int.op_Equality(a, b); }
  static get zero() { return new Vector2Int(); }
  static get one() { return new Vector2Int(1, 1); }
  static get up() { return new Vector2Int(0, 1); }
  static get down() { return new Vector2Int(0, -1); }
  static get left() { return new Vector2Int(-1, 0); }
  static get right() { return new Vector2Int(1, 0); }
}

export class Vector3Int {
  constructor(x = 0, y = 0, z = 0) { this.x = x | 0; this.y = y | 0; this.z = z | 0; }
  clone() { return new Vector3Int(this.x, this.y, this.z); }
  Set(x, y, z) { this.x = x | 0; this.y = y | 0; this.z = z | 0; }
  GetItem(index) {
    if (index === 0) return this.x;
    if (index === 1) return this.y;
    if (index === 2) return this.z;
    throw new RangeError(`Invalid Vector3Int index addressed: ${index}`);
  }
  SetItem(index, value) {
    if (index === 0) this.x = value | 0;
    else if (index === 1) this.y = value | 0;
    else if (index === 2) this.z = value | 0;
    else throw new RangeError(`Invalid Vector3Int index addressed: ${index}`);
  }
  get sqrMagnitude() {
    return (Math.imul(this.x, this.x) + Math.imul(this.y, this.y)
      + Math.imul(this.z, this.z)) | 0;
  }
  get magnitude() { return Math.sqrt(this.sqrMagnitude); }
  add(other) {
    this.x = (this.x + other.x) | 0;
    this.y = (this.y + other.y) | 0;
    this.z = (this.z + other.z) | 0;
    return this;
  }
  sub(other) {
    this.x = (this.x - other.x) | 0;
    this.y = (this.y - other.y) | 0;
    this.z = (this.z - other.z) | 0;
    return this;
  }
  negate() { this.x = -this.x | 0; this.y = -this.y | 0; this.z = -this.z | 0; return this; }
  multiply(other) {
    this.x = Math.imul(this.x, other.x);
    this.y = Math.imul(this.y, other.y);
    this.z = Math.imul(this.z, other.z);
    return this;
  }
  multiplyScalar(value) {
    this.x = Math.imul(this.x, value);
    this.y = Math.imul(this.y, value);
    this.z = Math.imul(this.z, value);
    return this;
  }
  divideScalar(value) {
    if (value === 0) throw new RangeError('Vector3Int division by zero');
    this.x = Math.trunc(this.x / value) | 0;
    this.y = Math.trunc(this.y / value) | 0;
    this.z = Math.trunc(this.z / value) | 0;
    return this;
  }
  Scale(scale) { this.multiply(scale); }
  Clamp(min, max) {
    this.x = Mathf.Clamp(this.x, min.x, max.x);
    this.y = Mathf.Clamp(this.y, min.y, max.y);
    this.z = Mathf.Clamp(this.z, min.z, max.z);
  }
  Equals(other) {
    return other instanceof Vector3Int && this.x === other.x
      && this.y === other.y && this.z === other.z;
  }
  GetHashCode() {
    const y = this.y | 0, z = this.z | 0;
    return (this.x ^ y << 4 ^ y >> 28 ^ z >> 4 ^ z << 28) | 0;
  }
  ToString(format = null, provider = null) {
    return `(${[this.x, this.y, this.z].map(value => formatInt32(value, format, provider)).join(', ')})`;
  }
  toVector3() { return new Vector3(this.x, this.y, this.z); }
  toVector2Int() { return new Vector2Int(this.x, this.y); }
  static Distance(a, b) { return Math.hypot(a.x - b.x, a.y - b.y, a.z - b.z); }
  static Min(a, b) { return new Vector3Int(Math.min(a.x, b.x), Math.min(a.y, b.y), Math.min(a.z, b.z)); }
  static Max(a, b) { return new Vector3Int(Math.max(a.x, b.x), Math.max(a.y, b.y), Math.max(a.z, b.z)); }
  static Scale(a, b) { return new Vector3Int(Math.imul(a.x, b.x), Math.imul(a.y, b.y), Math.imul(a.z, b.z)); }
  static FloorToInt(v) { return new Vector3Int(Math.floor(v.x), Math.floor(v.y), Math.floor(v.z)); }
  static CeilToInt(v) { return new Vector3Int(Math.ceil(v.x), Math.ceil(v.y), Math.ceil(v.z)); }
  static RoundToInt(v) { return new Vector3Int(Mathf.RoundToInt(v.x), Mathf.RoundToInt(v.y), Mathf.RoundToInt(v.z)); }
  static op_Equality(a, b) {
    return a === b || a != null && b != null
      && a.x === b.x && a.y === b.y && a.z === b.z;
  }
  static op_Inequality(a, b) { return !Vector3Int.op_Equality(a, b); }
  static get zero() { return new Vector3Int(); }
  static get one() { return new Vector3Int(1, 1, 1); }
  static get up() { return new Vector3Int(0, 1, 0); }
  static get down() { return new Vector3Int(0, -1, 0); }
  static get left() { return new Vector3Int(-1, 0, 0); }
  static get right() { return new Vector3Int(1, 0, 0); }
  static get forward() { return new Vector3Int(0, 0, 1); }
  static get back() { return new Vector3Int(0, 0, -1); }
}

// RectInt stores its origin and signed size. Its min/max accessors normalize
// negative sizes, but equality compares the stored fields (UnityCsReference).
const integerCenter = (position, size) => Math.fround(
  Math.fround(position) + Math.fround(Math.fround(size) * 0.5));

export class RectInt {
  constructor(x = 0, y = 0, width = 0, height = 0) {
    if (x instanceof Vector2Int) {
      const position = x, size = y;
      x = position.x; y = position.y; width = size.x; height = size.y;
    }
    this.x = x | 0; this.y = y | 0;
    this.width = width | 0; this.height = height | 0;
  }
  clone() { return new RectInt(this.x, this.y, this.width, this.height); }
  get center() { return new Vector2(integerCenter(this.x, this.width),
    integerCenter(this.y, this.height)); }
  get xMin() { return Math.min(this.x, (this.x + this.width) | 0); }
  set xMin(value) { const oldMax = this.xMax; this.x = value | 0; this.width = oldMax - this.x | 0; }
  get yMin() { return Math.min(this.y, (this.y + this.height) | 0); }
  set yMin(value) { const oldMax = this.yMax; this.y = value | 0; this.height = oldMax - this.y | 0; }
  get xMax() { return Math.max(this.x, (this.x + this.width) | 0); }
  set xMax(value) { this.width = value - this.x | 0; }
  get yMax() { return Math.max(this.y, (this.y + this.height) | 0); }
  set yMax(value) { this.height = value - this.y | 0; }
  get min() { return new Vector2Int(this.xMin, this.yMin); }
  set min(value) { this.xMin = value.x; this.yMin = value.y; }
  get max() { return new Vector2Int(this.xMax, this.yMax); }
  set max(value) { this.xMax = value.x; this.yMax = value.y; }
  get position() { return new Vector2Int(this.x, this.y); }
  set position(value) { this.x = value.x | 0; this.y = value.y | 0; }
  get size() { return new Vector2Int(this.width, this.height); }
  set size(value) { this.width = value.x | 0; this.height = value.y | 0; }
  SetMinMax(min, max) {
    this.x = min.x | 0; this.y = min.y | 0;
    this.width = max.x - min.x | 0; this.height = max.y - min.y | 0;
  }
  ClampToBounds(bounds) {
    this.x = Math.max(Math.min(bounds.xMax, this.x), bounds.xMin);
    this.y = Math.max(Math.min(bounds.yMax, this.y), bounds.yMin);
    this.width = Math.min((bounds.xMax - this.x) | 0, this.width);
    this.height = Math.min((bounds.yMax - this.y) | 0, this.height);
  }
  Contains(point) {
    return point.x >= this.xMin && point.y >= this.yMin
      && point.x < this.xMax && point.y < this.yMax;
  }
  Overlaps(other) {
    return other.xMin < this.xMax && other.xMax > this.xMin
      && other.yMin < this.yMax && other.yMax > this.yMin;
  }
  Equals(other) {
    return other instanceof RectInt && this.x === other.x && this.y === other.y
      && this.width === other.width && this.height === other.height;
  }
  GetHashCode() {
    const x = this.x | 0, y = this.y | 0, w = this.width | 0, h = this.height | 0;
    return (x ^ y << 4 ^ y >> 28 ^ w >> 4 ^ w << 28 ^ h >> 4 ^ h << 28) | 0;
  }
  ToString(format = null, provider = null) {
    const render = value => formatInt32(value, format, provider);
    return `(x:${render(this.x)}, y:${render(this.y)}, width:${render(this.width)}, height:${render(this.height)})`;
  }
  get allPositionsWithin() {
    return new RectIntPositionEnumerator(this.min, this.max);
  }
  static op_Equality(a, b) { return a === b || a != null && b != null && a.Equals(b); }
  static op_Inequality(a, b) { return !RectInt.op_Equality(a, b); }
  static get zero() { return new RectInt(); }
}

export class BoundsInt {
  constructor(x = 0, y = 0, z = 0, sizeX = 0, sizeY = 0, sizeZ = 0) {
    if (x instanceof Vector3Int) {
      this._position = x.clone();
      this._size = y.clone();
    } else {
      this._position = new Vector3Int(x, y, z);
      this._size = new Vector3Int(sizeX, sizeY, sizeZ);
    }
  }
  clone() { return new BoundsInt(this._position, this._size); }
  get x() { return this._position.x; }
  set x(value) { this._position.x = value | 0; }
  get y() { return this._position.y; }
  set y(value) { this._position.y = value | 0; }
  get z() { return this._position.z; }
  set z(value) { this._position.z = value | 0; }
  get center() {
    return new Vector3(integerCenter(this.x, this._size.x),
      integerCenter(this.y, this._size.y), integerCenter(this.z, this._size.z));
  }
  get xMin() { return Math.min(this.x, (this.x + this._size.x) | 0); }
  set xMin(value) { const max = this.xMax; this.x = value; this._size.x = max - this.x | 0; }
  get yMin() { return Math.min(this.y, (this.y + this._size.y) | 0); }
  set yMin(value) { const max = this.yMax; this.y = value; this._size.y = max - this.y | 0; }
  get zMin() { return Math.min(this.z, (this.z + this._size.z) | 0); }
  set zMin(value) { const max = this.zMax; this.z = value; this._size.z = max - this.z | 0; }
  get xMax() { return Math.max(this.x, (this.x + this._size.x) | 0); }
  set xMax(value) { this._size.x = value - this.x | 0; }
  get yMax() { return Math.max(this.y, (this.y + this._size.y) | 0); }
  set yMax(value) { this._size.y = value - this.y | 0; }
  get zMax() { return Math.max(this.z, (this.z + this._size.z) | 0); }
  set zMax(value) { this._size.z = value - this.z | 0; }
  get min() { return new Vector3Int(this.xMin, this.yMin, this.zMin); }
  set min(value) { this.xMin = value.x; this.yMin = value.y; this.zMin = value.z; }
  get max() { return new Vector3Int(this.xMax, this.yMax, this.zMax); }
  set max(value) { this.xMax = value.x; this.yMax = value.y; this.zMax = value.z; }
  get position() { return this._position.clone(); }
  set position(value) { this._position = value.clone(); }
  get size() { return this._size.clone(); }
  set size(value) { this._size = value.clone(); }
  SetMinMax(min, max) {
    this._position = min.clone();
    this._size = max.clone().sub(min);
  }
  ClampToBounds(bounds) {
    const x = Math.max(Math.min(bounds.xMax, this.x), bounds.xMin);
    const y = Math.max(Math.min(bounds.yMax, this.y), bounds.yMin);
    const z = Math.max(Math.min(bounds.zMax, this.z), bounds.zMin);
    this._size.Set(Math.min((bounds.xMax - x) | 0, this._size.x),
      Math.min((bounds.yMax - y) | 0, this._size.y),
      Math.min((bounds.zMax - z) | 0, this._size.z));
    this._position.Set(x, y, z);
  }
  Contains(point) {
    return point.x >= this.xMin && point.y >= this.yMin && point.z >= this.zMin
      && point.x < this.xMax && point.y < this.yMax && point.z < this.zMax;
  }
  Equals(other) {
    return other instanceof BoundsInt && this._position.Equals(other._position)
      && this._size.Equals(other._size);
  }
  GetHashCode() { return this._position.GetHashCode() ^ this._size.GetHashCode() << 2; }
  ToString(format = null, provider = null) {
    return `Position: ${this._position.ToString(format, provider)}, Size: ${this._size.ToString(format, provider)}`;
  }
  get allPositionsWithin() {
    return new BoundsIntPositionEnumerator(this.min, this.max);
  }
  static op_Equality(a, b) { return a === b || a != null && b != null && a.Equals(b); }
  static op_Inequality(a, b) { return !BoundsInt.op_Equality(a, b); }
}

export class RectIntPositionEnumerator {
  constructor(min = new Vector2Int(), max = new Vector2Int()) {
    this.min = min.clone(); this.max = max.clone();
    this.Reset();
  }
  get Current() { return this.current.clone(); }
  GetEnumerator() {
    const copy = new RectIntPositionEnumerator(this.min, this.max);
    copy.current = this.current.clone();
    return copy;
  }
  Reset() { this.current = new Vector2Int(this.min.x - 1, this.min.y); }
  MoveNext() {
    if (this.min.x >= this.max.x || this.min.y >= this.max.y) return false;
    if (++this.current.x >= this.max.x) {
      this.current.x = this.min.x;
      this.current.y++;
    }
    return this.current.y < this.max.y;
  }
  *[Symbol.iterator]() {
    const cursor = this.GetEnumerator();
    while (cursor.MoveNext()) yield cursor.Current;
  }
}
RectInt.PositionEnumerator = RectIntPositionEnumerator;

export class BoundsIntPositionEnumerator {
  constructor(min = new Vector3Int(), max = new Vector3Int()) {
    this.min = min.clone(); this.max = max.clone();
    this.Reset();
  }
  get Current() { return this.current.clone(); }
  GetEnumerator() {
    const copy = new BoundsIntPositionEnumerator(this.min, this.max);
    copy.current = this.current.clone();
    return copy;
  }
  Reset() { this.current = new Vector3Int(this.min.x - 1, this.min.y, this.min.z); }
  MoveNext() {
    if (this.min.x >= this.max.x || this.min.y >= this.max.y || this.min.z >= this.max.z)
      return false;
    if (++this.current.x >= this.max.x) {
      this.current.x = this.min.x;
      if (++this.current.y >= this.max.y) {
        this.current.y = this.min.y;
        this.current.z++;
      }
    }
    return this.current.z < this.max.z;
  }
  *[Symbol.iterator]() {
    const cursor = this.GetEnumerator();
    while (cursor.MoveNext()) yield cursor.Current;
  }
}
BoundsInt.PositionEnumerator = BoundsIntPositionEnumerator;

export class Vector4 extends THREE.Vector4 {
  static get kEpsilon() { return Math.fround(1e-5); }
  constructor(x = 0, y = 0, z = 0, w = 0) { super(x, y, z, w); }
  GetItem(index) { return this.getComponent(index); }
  SetItem(index, value) { this.setComponent(index, value); }
  Set(x, y, z, w) { this.set(x, y, z, w); }
  Normalize() { this.normalize(); }
  Scale(scale) { this.multiply(scale); }
  SqrMagnitude() { return this.lengthSq(); }
  Equals(other) { return other instanceof Vector4 && this.equals(other); }
  toVector2() { return new Vector2(this.x, this.y); }
  toVector3() { return new Vector3(this.x, this.y, this.z); }
  GetHashCode() {
    const bits = value => {
      const view = new DataView(new ArrayBuffer(4));
      view.setFloat32(0, value, true);
      return view.getInt32(0, true);
    };
    return bits(this.x) ^ (bits(this.y) << 2) ^ (bits(this.z) >> 2) ^ (bits(this.w) >> 1);
  }
  ToString(format = 'F2', _provider = null) {
    const digits = /^F(\d+)$/i.exec(format ?? 'F2');
    const decimals = digits ? Number(digits[1]) : 2;
    return `(${[this.x, this.y, this.z, this.w]
      .map(value => Number(value).toFixed(decimals)).join(', ')})`;
  }
  toString() { return this.ToString(); }
  static op_Equality(left, right) {
    const x = left.x - right.x, y = left.y - right.y;
    const z = left.z - right.z, w = left.w - right.w;
    return x * x + y * y + z * z + w * w < 1e-10;
  }
  static op_Inequality(left, right) { return !Vector4.op_Equality(left, right); }
  get magnitude() { return this.length(); }
  get sqrMagnitude() { return this.lengthSq(); }
  get normalized() { return this.clone().normalize(); }
  static get zero() { return new Vector4(); }
  static get one() { return new Vector4(1, 1, 1, 1); }
  static get positiveInfinity() { return new Vector4(Infinity, Infinity, Infinity, Infinity); }
  static get negativeInfinity() { return new Vector4(-Infinity, -Infinity, -Infinity, -Infinity); }
  static Distance(a, b) { return a.clone().sub(b).length(); }
  static Dot(a, b) { return a.dot(b); }
  static Normalize(v) { return v.clone().normalize(); }
  static SqrMagnitude(v) { return v.lengthSq(); }
  static Magnitude(v) { return v.length(); }
  static Lerp(a, b, t) { return a.clone().lerp(b, Mathf.Clamp01(t)); }
  static LerpUnclamped(a, b, t) { return a.clone().lerp(b, t); }
  static MoveTowards(a, b, maxDistanceDelta) { const d = b.clone().sub(a); return d.length() <= maxDistanceDelta || !d.lengthSq() ? b.clone() : a.clone().add(d.setLength(maxDistanceDelta)); }
  static Scale(a, b) { return a.clone().multiply(b); }
  static Project(a, b) { return b.lengthSq() < 1e-12 ? Vector4.zero : b.clone().multiplyScalar(a.dot(b) / b.lengthSq()); }
  static Min(a, b) { return a.clone().min(b); }
  static Max(a, b) { return a.clone().max(b); }
}

export class Vector3 extends THREE.Vector3 {
  static get kEpsilon() { return Math.fround(1e-5); }
  static get kEpsilonNormalSqrt() { return Math.fround(1e-15); }
  GetItem(index) { return this.getComponent(index); }
  SetItem(index, value) { this.setComponent(index, value); }
  Normalize() { this.normalize(); }
  Set(x, y, z) { this.set(x, y, z); }
  Scale(scale) { this.multiply(scale); }
  Equals(other) { return other instanceof Vector3 && this.equals(other); }
  toVector2() { return new Vector2(this.x, this.y); }
  toVector4() { return new Vector4(this.x, this.y, this.z, 0); }
  static op_Equality(left, right) {
    const x = left.x - right.x;
    const y = left.y - right.y;
    const z = left.z - right.z;
    return x * x + y * y + z * z < 1e-10;
  }
  static op_Inequality(left, right) { return !Vector3.op_Equality(left, right); }
  get magnitude() { return this.length(); }
  get sqrMagnitude() { return this.lengthSq(); }
  get normalized() { return this.clone().normalize(); }
  static get zero() { return new Vector3(); }
  static get one() { return new Vector3(1, 1, 1); }
  static get positiveInfinity() { return new Vector3(Infinity, Infinity, Infinity); }
  static get negativeInfinity() { return new Vector3(-Infinity, -Infinity, -Infinity); }
  static get up() { return new Vector3(0, 1, 0); }
  static get down() { return new Vector3(0, -1, 0); }
  static get left() { return new Vector3(-1, 0, 0); }
  static get right() { return new Vector3(1, 0, 0); }
  static get forward() { return new Vector3(0, 0, 1); }
  static get fwd() { return Vector3.forward; }
  static get back() { return new Vector3(0, 0, -1); }
  static Distance(a, b) { return a.distanceTo(b); }
  static Dot(a, b) { return a.dot(b); }
  static Normalize(v) { return v.clone().normalize(); }
  static Magnitude(v) { return v.length(); }
  static SqrMagnitude(v) { return v.lengthSq(); }
  static Cross(a, b) { return new Vector3().crossVectors(a, b); }
  static ClampMagnitude(v, maxLength) { return v.length() > maxLength ? v.clone().setLength(maxLength) : v.clone(); }
  static LerpUnclamped(a, b, t) { return a.clone().lerp(b, t); }
  static SlerpUnclamped(a, b, t) { return Vector3._slerp(a, b, t); }
  static Min(a, b) { return a.clone().min(b); }
  static Max(a, b) { return a.clone().max(b); }
  static Scale(a, b) { return a.clone().multiply(b); }
  static Project(v, onNormal) { return onNormal.lengthSq() < 1e-12 ? Vector3.zero : onNormal.clone().multiplyScalar(v.dot(onNormal) / onNormal.lengthSq()); }
  static RotateTowards(current, target, maxRadiansDelta, maxMagnitudeDelta) {
    const a = current.clone().normalize(), b = target.clone().normalize();
    const angle = Math.acos(Mathf.Clamp(a.dot(b), -1, 1));
    const direction = angle < 1e-12 ? a : Vector3.Slerp(a, b, Mathf.Clamp01(maxRadiansDelta / angle)).normalize();
    return direction.multiplyScalar(Mathf.MoveTowards(current.length(), target.length(), maxMagnitudeDelta));
  }
  static ProjectOnPlane(vector, normal) {
    const lengthSq = normal.lengthSq();
    return lengthSq < 1e-12 ? vector.clone()
      : vector.clone().addScaledVector(normal, -vector.dot(normal) / lengthSq);
  }
  static Reflect(direction, normal) {
    return direction.clone().addScaledVector(normal, -2 * direction.dot(normal));
  }
  static Exclude(excludeThis, fromThat) { return Vector3.ProjectOnPlane(fromThat, excludeThis); }
  static Slerp(a, b, t) { return Vector3._slerp(a, b, Mathf.Clamp01(t)); }
  static _slerp(a, b, t) {
    const aLength = a.length(), bLength = b.length();
    if (aLength < 1e-12 || bLength < 1e-12) return Vector3.Lerp(a, b, t);
    const from = a.clone().divideScalar(aLength);
    const to = b.clone().divideScalar(bLength);
    const dot = Mathf.Clamp(from.dot(to), -1, 1);
    let direction;
    if (dot > 1 - 1e-6) direction = from.lerp(to, t).normalize();
    else if (dot < -1 + 1e-6) {
      const axis = new Vector3().crossVectors(from,
        Math.abs(from.x) < 0.9 ? Vector3.right : Vector3.up).normalize();
      direction = from.multiplyScalar(Math.cos(Math.PI * t))
        .addScaledVector(axis, Math.sin(Math.PI * t));
    } else {
      const angle = Math.acos(dot);
      const sine = Math.sin(angle);
      direction = from.multiplyScalar(Math.sin((1 - t) * angle) / sine)
        .addScaledVector(to, Math.sin(t * angle) / sine);
    }
    return direction.multiplyScalar(Mathf.LerpUnclamped(aLength, bLength, t));
  }
  static Lerp(a, b, t) { return a.clone().lerp(b, Mathf.Clamp01(t)); }
  static MoveTowards(current, target, maxDistanceDelta) {
    const delta = target.clone().sub(current);
    return delta.length() <= maxDistanceDelta || delta.lengthSq() === 0
      ? target.clone() : current.clone().add(delta.setLength(maxDistanceDelta));
  }
  static Angle(a, b) { return a.lengthSq() * b.lengthSq() < 1e-30 ? 0 : a.angleTo(b) * Mathf.Rad2Deg; }
  static AngleBetween(a, b) { return Vector3.Angle(a, b) * Mathf.Deg2Rad; }
  static SignedAngle(a, b, axis) {
    const angle = Vector3.Angle(a, b);
    return new Vector3().crossVectors(a, b).dot(axis) < 0 ? -angle : angle;
  }
  static SmoothDamp(current, target, currentVelocity, smoothTime, maxSpeed = Infinity, deltaTime = Mathf._defaultDeltaTime()) {
    return smoothDampVector(current, target, currentVelocity, smoothTime, maxSpeed, deltaTime);
  }
  GetHashCode() {
    const bits = value => {
      const view = new DataView(new ArrayBuffer(4));
      view.setFloat32(0, value, true);
      return view.getInt32(0, true);
    };
    return bits(this.x) ^ (bits(this.y) << 2) ^ (bits(this.z) >> 2);
  }
  ToString(format = 'F2', _provider = null) {
    const digits = /^F(\d+)$/i.exec(format ?? 'F2');
    const decimals = digits ? Number(digits[1]) : 2;
    return `(${[this.x, this.y, this.z].map(value => Number(value).toFixed(decimals)).join(', ')})`;
  }
  toString() { return this.ToString(); }
  static OrthoNormalize(normal, tangent, binormal = null) {
    const n = (normal.value ?? normal).clone().normalize();
    const t = Vector3.ProjectOnPlane(tangent.value ?? tangent, n).normalize();
    if (normal.value) normal.value = n; else normal.copy(n);
    if (tangent.value) tangent.value = t; else tangent.copy(t);
    if (binormal) {
      const b = Vector3.Cross(t, n).normalize();
      if (binormal.value) binormal.value = b; else binormal.copy(b);
    }
  }
}

export class Quaternion extends THREE.Quaternion {
  static get kEpsilon() { return Math.fround(1e-6); }
  Equals(other) { return other instanceof Quaternion && this.equals(other); }
  GetItem(index) {
    if (!Number.isInteger(index) || index < 0 || index > 3) throw new RangeError('Quaternion index must be 0..3');
    return [this.x, this.y, this.z, this.w][index];
  }
  SetItem(index, value) {
    if (!Number.isInteger(index) || index < 0 || index > 3) throw new RangeError('Quaternion index must be 0..3');
    this[['x', 'y', 'z', 'w'][index]] = value;
  }
  static get identity() { return new Quaternion(); }
  get normalized() { return this.clone().normalize(); }
  Normalize() { this.normalize(); }
  Set(x, y, z, w) { this.set(x, y, z, w); }
  get eulerAngles() {
    const euler = new THREE.Euler().setFromQuaternion(this, 'YXZ');
    const degrees = new Vector3(euler.x, euler.y, euler.z).multiplyScalar(Mathf.Rad2Deg);
    return degrees.set(...degrees.toArray().map(value => {
      const positive = (value % 360 + 360) % 360;
      return positive < 1e-6 || positive > 360 - 1e-6 ? 0 : positive;
    }));
  }
  set eulerAngles(value) { this.copy(Quaternion.Euler(value)); }
  SetFromToRotation(from, to) { this.copy(Quaternion.FromToRotation(from, to)); }
  SetLookRotation(forward, up = Vector3.up) { this.copy(Quaternion.LookRotation(forward, up)); }
  SetAxisAngle(axis, angle) { this.copy(Quaternion.AxisAngle(axis, angle)); }
  SetEulerAngles(x, y, z) { this.copy(Quaternion.EulerAngles(x, y, z)); }
  SetEulerRotation(x, y, z) { this.copy(Quaternion.EulerRotation(x, y, z)); }
  ToEuler() {
    const euler = new THREE.Euler().setFromQuaternion(this, 'YXZ');
    return new Vector3(euler.x, euler.y, euler.z);
  }
  ToEulerAngles() { return this.ToEuler(); }
  ToAxisAngle(axis, angle) {
    const unit = this.clone().normalize();
    const radians = 2 * Math.acos(Mathf.Clamp(unit.w, -1, 1));
    const scale = Math.sqrt(Math.max(0, 1 - unit.w * unit.w));
    if (axis && typeof axis === 'object') axis.value = scale < 1e-6
      ? Vector3.right : new Vector3(unit.x / scale, unit.y / scale, unit.z / scale);
    if (angle && typeof angle === 'object') angle.value = radians;
  }
  ToAngleAxis(angle, axis) {
    const unit = this.clone().normalize();
    const radians = 2 * Math.acos(Mathf.Clamp(unit.w, -1, 1));
    const scale = Math.sqrt(Math.max(0, 1 - unit.w * unit.w));
    if (angle && typeof angle === 'object') angle.value = radians * Mathf.Rad2Deg;
    if (axis && typeof axis === 'object') axis.value = scale < 1e-6
      ? Vector3.right : new Vector3(unit.x / scale, unit.y / scale, unit.z / scale);
  }
  static op_Equality(left, right) { return left.dot(right) > 1 - 1e-6; }
  static op_Inequality(left, right) { return !Quaternion.op_Equality(left, right); }
  static Multiply(left, right) { return left.clone().multiply(right); }
  static RotateVector(rotation, vector) { return vector.clone().applyQuaternion(rotation); }
  static Euler(x, y, z) {
    if (typeof x === 'object') ({ x, y, z } = x);
    const float = Math.fround;
    const halfRadians = degrees => float(float(degrees * float(Mathf.Deg2Rad)) * 0.5);
    const rx = halfRadians(x), ry = halfRadians(y), rz = halfRadians(z);
    const sx = float(Math.sin(rx)), sy = float(Math.sin(ry)), sz = float(Math.sin(rz));
    const cx = float(Math.cos(rx)), cy = float(Math.cos(ry)), cz = float(Math.cos(rz));
    const product = (a, b, c) => float(float(a * b) * c);
    return new Quaternion(
      float(product(sx, cy, cz) + product(cx, sy, sz)),
      float(product(cx, sy, cz) - product(sx, cy, sz)),
      float(product(cx, cy, sz) - product(sx, sy, cz)),
      float(product(cx, cy, cz) + product(sx, sy, sz)),
    );
  }
  static EulerAngles(x, y, z) {
    if (typeof x === 'object') ({ x, y, z } = x);
    return Quaternion.Euler(x * Mathf.Rad2Deg, y * Mathf.Rad2Deg, z * Mathf.Rad2Deg);
  }
  static EulerRotation(x, y, z) { return Quaternion.EulerAngles(x, y, z); }
  static AxisAngle(axis, angle) {
    return new Quaternion().setFromAxisAngle(axis.clone().normalize(), angle);
  }
  static ToEulerAngles(q) { return q.ToEulerAngles(); }
  static Inverse(value) { return value.clone().invert(); }
  static Slerp(a, b, t) { return a.clone().slerp(b, Mathf.Clamp01(t)); }
  static SlerpUnclamped(a, b, t) { return a.clone().slerp(b, t); }
  static Lerp(a, b, t) { return Quaternion.LerpUnclamped(a, b, Mathf.Clamp01(t)); }
  static LerpUnclamped(a, b, t) {
    const sign = a.dot(b) < 0 ? -1 : 1;
    return new Quaternion(a.x + (sign * b.x - a.x) * t,
      a.y + (sign * b.y - a.y) * t, a.z + (sign * b.z - a.z) * t,
      a.w + (sign * b.w - a.w) * t).normalize();
  }
  static Dot(a, b) { return a.dot(b); }
  static Angle(a, b) { return Math.acos(Mathf.Clamp(Math.abs(a.dot(b)), -1, 1)) * 2 * Mathf.Rad2Deg; }
  static Normalize(q) { return q.clone().normalize(); }
  static RotateTowards(a, b, maxDegreesDelta) {
    const angle = Quaternion.Angle(a, b);
    return angle < 1e-12 ? b.clone() : Quaternion.SlerpUnclamped(a, b, Math.min(1, maxDegreesDelta / angle));
  }
  static AngleAxis(angle, axis) {
    return new Quaternion().setFromAxisAngle(axis.clone().normalize(), angle * Mathf.Deg2Rad);
  }
  static FromToRotation(from, to) {
    if (from.lengthSq() === 0 || to.lengthSq() === 0) return Quaternion.identity;
    return new Quaternion().setFromUnitVectors(from.clone().normalize(), to.clone().normalize());
  }
  static LookRotation(forward, up = Vector3.up) {
    const z = forward.clone().normalize();
    if (z.lengthSq() === 0) throw new RangeError('Forward cannot be zero');
    const x = new Vector3().crossVectors(up, z).normalize();
    if (x.lengthSq() === 0) x.set(1, 0, 0);
    const y = new Vector3().crossVectors(z, x).normalize();
    return new Quaternion().setFromRotationMatrix(new THREE.Matrix4().makeBasis(x, y, z));
  }
  GetHashCode() {
    const bits = value => {
      const view = new DataView(new ArrayBuffer(4));
      view.setFloat32(0, value, true);
      return view.getInt32(0, true);
    };
    return bits(this.x) ^ (bits(this.y) << 2) ^ (bits(this.z) >> 2) ^ (bits(this.w) >> 1);
  }
  ToString(format = 'F5', _provider = null) {
    const digits = /^F(\d+)$/i.exec(format ?? 'F5');
    const decimals = digits ? Number(digits[1]) : 5;
    return `(${[this.x, this.y, this.z, this.w].map(value => Number(value).toFixed(decimals)).join(', ')})`;
  }
  toString() { return this.ToString(); }
}

export class Pose {
  constructor(position = Vector3.zero, rotation = Quaternion.identity) {
    this._position = position.clone();
    this._rotation = rotation.clone();
  }
  clone() { return new Pose(this._position, this._rotation); }
  get position() { return this._position; }
  set position(value) { this._position = value.clone(); }
  get rotation() { return this._rotation; }
  set rotation(value) { this._rotation = value.clone(); }
  get forward() { return Vector3.forward.applyQuaternion(this._rotation); }
  get right() { return Vector3.right.applyQuaternion(this._rotation); }
  get up() { return Vector3.up.applyQuaternion(this._rotation); }
  static get identity() { return new Pose(); }
  Equals(other) {
    return other instanceof Pose && this._position.Equals(other._position)
      && this._rotation.Equals(other._rotation);
  }
  static op_Equality(left, right) { return left.Equals(right); }
  static op_Inequality(left, right) { return !left.Equals(right); }
  GetHashCode() {
    return this._position.GetHashCode() ^ (this._rotation.GetHashCode() << 1);
  }
  GetTransformedBy(parent) {
    const position = typeof parent.TransformPoint === 'function'
      ? parent.TransformPoint(this._position)
      : this._position.clone().applyQuaternion(parent.rotation).add(parent.position);
    return new Pose(position, parent.rotation.clone().multiply(this._rotation));
  }
  ToString(format = null) {
    return `(${this._position.ToString(format ?? 'F2')}, ${this._rotation.ToString(format ?? 'F5')})`;
  }
  toString() { return this.ToString(); }
}

export class FrustumPlanes {
  constructor(left = 0, right = 0, bottom = 0, top = 0, zNear = 0, zFar = 0) {
    Object.assign(this, { left, right, bottom, top, zNear, zFar });
  }
  clone() {
    return new FrustumPlanes(this.left, this.right, this.bottom,
      this.top, this.zNear, this.zFar);
  }
}

/** Unity's column-major Matrix4x4, backed by Three's matching element layout. */
export class Matrix4x4 extends THREE.Matrix4 {
  constructor(column0, column1, column2, column3) {
    super();
    if (column0) {
      for (const [index, column] of [column0, column1, column2, column3].entries())
        this.SetColumn(index, column);
    }
  }
  clone() { return new Matrix4x4().copy(this); }
  static get identity() { return new Matrix4x4(); }
  static get zero() { const result = new Matrix4x4(); result.elements.fill(0); return result; }
  get inverse() { return this.clone().invert(); }
  get transpose() {
    const result = this.clone();
    THREE.Matrix4.prototype.transpose.call(result);
    return result;
  }
  get determinant() { return super.determinant(); }
  get decomposeProjection() {
    const e = this.elements;
    const zNear = e[14] / (e[10] - 1);
    const zFar = e[14] / (e[10] + 1);
    return new FrustumPlanes(zNear * (e[8] - 1) / e[0],
      zNear * (e[8] + 1) / e[0], zNear * (e[9] - 1) / e[5],
      zNear * (e[9] + 1) / e[5], zNear, zFar);
  }
  get isIdentity() { return this.elements.every((value, index) => Math.abs(value - (index % 5 === 0 ? 1 : 0)) < 1e-6); }
  get lossyScale() {
    const scale = new Vector3();
    new THREE.Matrix4().copy(this).decompose(new Vector3(), new Quaternion(), scale);
    return scale;
  }
  get rotation() {
    const rotation = new Quaternion();
    new THREE.Matrix4().copy(this).decompose(new Vector3(), rotation, new Vector3());
    return rotation;
  }
  Equals(other) { return other instanceof Matrix4x4 && this.equals(other); }
  GetHashCode() {
    const columns = [0, 1, 2, 3].map(index => this.GetColumn(index).GetHashCode());
    return columns[0] ^ (columns[1] << 2) ^ (columns[2] >> 2) ^ (columns[3] >> 1);
  }
  ToString(format = 'F5', _provider = null) {
    const digits = /^F(\d+)$/i.exec(format ?? 'F5');
    const decimals = digits ? Number(digits[1]) : 5;
    return [0, 1, 2, 3].map(row => [0, 1, 2, 3]
      .map(column => Number(this.GetItem(row, column)).toFixed(decimals)).join('\t')).join('\n') + '\n';
  }
  toString() { return this.ToString(); }
  GetItem(row, column = null) {
    const index = column === null ? row : row + column * 4;
    if (!Number.isInteger(index) || index < 0 || index > 15) throw new RangeError('Matrix index must be 0..15');
    return this.elements[index];
  }
  SetItem(row, column, value) {
    if (value === undefined) { value = column; column = Math.floor(row / 4); row %= 4; }
    const index = row + column * 4;
    if (!Number.isInteger(index) || index < 0 || index > 15) throw new RangeError('Matrix index must be 0..15');
    this.elements[index] = value;
  }
  GetColumn(index) { return new Vector4(...[0, 1, 2, 3].map(row => this.GetItem(row, index))); }
  GetRow(index) { return new Vector4(...[0, 1, 2, 3].map(column => this.GetItem(index, column))); }
  SetColumn(index, value) {
    for (let row = 0; row < 4; row++) this.SetItem(row, index, value.getComponent(row));
  }
  SetRow(index, value) {
    for (let column = 0; column < 4; column++) this.SetItem(index, column, value.getComponent(column));
  }
  GetPosition() { return new Vector3(this.elements[12], this.elements[13], this.elements[14]); }
  SetTRS(position, rotation, scale) { this.compose(position, rotation, scale); }
  MultiplyPoint(point) { return new Vector3(point.x, point.y, point.z).applyMatrix4(this); }
  MultiplyPoint3x4(point) {
    const e = this.elements;
    return new Vector3(e[0] * point.x + e[4] * point.y + e[8] * point.z + e[12],
      e[1] * point.x + e[5] * point.y + e[9] * point.z + e[13],
      e[2] * point.x + e[6] * point.y + e[10] * point.z + e[14]);
  }
  MultiplyVector(vector) {
    const e = this.elements;
    return new Vector3(e[0] * vector.x + e[4] * vector.y + e[8] * vector.z,
      e[1] * vector.x + e[5] * vector.y + e[9] * vector.z,
      e[2] * vector.x + e[6] * vector.y + e[10] * vector.z);
  }
  ValidTRS() {
    const scale = this.lossyScale;
    return this.elements.every(Number.isFinite) && Math.abs(scale.x * scale.y * scale.z) > 1e-12;
  }
  TransformPlane(plane) {
    const coefficients = new Vector4(plane.normal.x, plane.normal.y, plane.normal.z,
      plane.distance).applyMatrix4(this.inverse.transpose);
    const result = new Plane();
    result.normal = new Vector3(coefficients.x, coefficients.y, coefficients.z).normalize();
    result.distance = coefficients.w;
    return result;
  }
  static TRS(position, rotation, scale) { return new Matrix4x4().compose(position, rotation, scale); }
  static LookAt(from, to, up) {
    return Matrix4x4.TRS(from, Quaternion.LookRotation(to.clone().sub(from), up), Vector3.one);
  }
  static Inverse3DAffine(input, result) {
    const e = input.elements;
    const affine = Math.abs(e[3]) < 1e-12 && Math.abs(e[7]) < 1e-12
      && Math.abs(e[11]) < 1e-12 && Math.abs(e[15] - 1) < 1e-12;
    const valid = affine && Math.abs(input.determinant) > 1e-12;
    result.value = valid ? input.inverse : input.clone();
    return valid;
  }
  static Inverse(matrix) { return matrix.inverse; }
  static Transpose(matrix) { return matrix.transpose; }
  static Determinant(matrix) { return matrix.determinant; }
  static Translate(position) { return new Matrix4x4().makeTranslation(position.x, position.y, position.z); }
  static Rotate(rotation) { return new Matrix4x4().makeRotationFromQuaternion(rotation); }
  static Scale(scale) { return new Matrix4x4().makeScale(scale.x, scale.y, scale.z); }
  static Ortho(left, right, bottom, top, near, far) {
    return new Matrix4x4().makeOrthographic(left, right, top, bottom, near, far);
  }
  static Perspective(fov, aspect, near, far) {
    const top = near * Math.tan(fov * Mathf.Deg2Rad / 2);
    return Matrix4x4.Frustum(-top * aspect, top * aspect, -top, top, near, far);
  }
  static Frustum(left, right, bottom, top, near, far) {
    if (typeof left === 'object') {
      ({ left, right, bottom, top, zNear: near, zFar: far } = left);
    }
    return new Matrix4x4().makePerspective(left, right, top, bottom, near, far);
  }
  static op_Equality(a, b) {
    if (a === b) return true;
    if (!(a instanceof Matrix4x4) || !(b instanceof Matrix4x4)) return false;
    for (let column = 0; column < 4; column++) {
      let squareDistance = 0;
      for (let row = 0; row < 4; row++) {
        const delta = a.GetItem(row, column) - b.GetItem(row, column);
        squareDistance += delta * delta;
      }
      if (squareDistance >= 1e-10) return false;
    }
    return true;
  }
  static op_Inequality(a, b) { return !Matrix4x4.op_Equality(a, b); }
  static Multiply(a, b) { return a.clone().multiply(b); }
  static MultiplyVector4(matrix, vector) { return vector.clone().applyMatrix4(matrix); }
}

for (let row = 0; row < 4; row++) {
  for (let column = 0; column < 4; column++) {
    const index = row + column * 4;
    Object.defineProperty(Matrix4x4.prototype, `m${row}${column}`, {
      get() { return this.elements[index]; },
      set(value) { this.elements[index] = value; },
    });
  }
}

export class Plane {
  constructor(a = null, b = 0, c = null) {
    this._normal = Vector3.zero;
    this.distance = 0;
    if (a == null) return;
    if (c) this.Set3Points(a, b, c);
    else if (typeof b === 'number') {
      this.normal = a.clone().normalize();
      this.distance = b;
    } else this.SetNormalAndPosition(a, b);
  }
  get normal() { return this._normal.clone(); }
  set normal(value) { this._normal = value.clone(); }
  clone() {
    const copy = new Plane();
    copy.normal = this.normal.clone();
    copy.distance = this.distance;
    return copy;
  }
  get flipped() { return new Plane(this.normal.clone().negate(), -this.distance); }
  Flip() { this._normal.negate(); this.distance = -this.distance; }
  SetNormalAndPosition(normal, point) {
    this.normal = normal.clone().normalize();
    this.distance = -this.normal.dot(point);
  }
  Set3Points(a, b, c) {
    const normal = new Vector3().crossVectors(b.clone().sub(a), c.clone().sub(a)).normalize();
    this.SetNormalAndPosition(normal, a);
  }
  GetDistanceToPoint(point) { return this.normal.dot(point) + this.distance; }
  ClosestPointOnPlane(point) { return point.clone().addScaledVector(this.normal, -this.GetDistanceToPoint(point)); }
  GetSide(point) { return this.GetDistanceToPoint(point) > 0; }
  SameSide(a, b) { return this.GetSide(a) === this.GetSide(b); }
  Raycast(ray, enter) {
    const denominator = this.normal.dot(ray.direction);
    const distance = Math.abs(denominator) < 1e-12 ? 0
      : -(this.normal.dot(ray.origin) + this.distance) / denominator;
    if (enter && typeof enter === 'object') enter.value = distance;
    return Math.abs(denominator) >= 1e-12 && distance >= 0;
  }
  Translate(translation) { this.distance += this.normal.dot(translation); }
  static Translate(plane, translation) {
    const moved = new Plane(plane.normal, plane.distance);
    moved.Translate(translation);
    return moved;
  }
  Equals(other) {
    return other instanceof Plane && this.normal.equals(other.normal)
      && this.distance === other.distance;
  }
  static op_Equality(left, right) { return left instanceof Plane && left.Equals(right); }
  static op_Inequality(left, right) { return !Plane.op_Equality(left, right); }
  GetHashCode() {
    const bits = value => {
      const view = new DataView(new ArrayBuffer(4));
      view.setFloat32(0, value, true);
      return view.getInt32(0, true);
    };
    const normal = this.normal;
    const hashNormal = bits(normal.x) ^ (bits(normal.y) << 2) ^ (bits(normal.z) >> 2);
    return bits(this.distance) ^ (hashNormal << 2);
  }
  ToString(format = 'F2', _provider = null) {
    const digits = /^F(\d+)$/i.exec(format ?? 'F2');
    const decimals = digits ? Number(digits[1]) : 2;
    const normal = this.normal;
    const values = [normal.x, normal.y, normal.z]
      .map(value => Number(value).toFixed(decimals)).join(', ');
    return `(normal:(${values}), distance:${Number(this.distance).toFixed(decimals)})`;
  }
  toString() { return this.ToString(); }
}

export class Keyframe {
  constructor(time = 0, value = 0, inTangent = 0, outTangent = 0,
    inWeight = 0, outWeight = 0) {
    this.time = Math.fround(time);
    this.value = Math.fround(value);
    this.inTangent = Math.fround(inTangent);
    this.outTangent = Math.fround(outTangent);
    this.inWeight = Math.fround(inWeight);
    this.outWeight = Math.fround(outWeight);
    this.weightedMode = arguments.length >= 6 ? 3 : 0;
    this.tangentMode = 0;
  }
  clone() {
    const key = new Keyframe(this.time, this.value, this.inTangent,
      this.outTangent, this.inWeight, this.outWeight);
    key.weightedMode = this.weightedMode;
    key.tangentMode = this.tangentMode;
    return key;
  }
}

function fieldsEqual(left, right) {
  return ['time', 'value', 'inTangent', 'outTangent', 'inWeight',
    'outWeight', 'weightedMode', 'tangentMode']
    .every(field => left[field] === right[field]);
}

function bezierUnit(a, b, time) {
  const inverse = 1 - time;
  return 3 * inverse * inverse * time * a
    + 3 * inverse * time * time * b + time * time * time;
}

function weightedCurveSample(left, right, duration, fraction) {
  const leftWeight = left.weightedMode & 2 ? left.outWeight : 1 / 3;
  const rightWeight = right.weightedMode & 1 ? right.inWeight : 1 / 3;
  const firstX = leftWeight, secondX = 1 - rightWeight;
  let low = 0, high = 1;
  for (let iteration = 0; iteration < 32; iteration++) {
    const middle = (low + high) / 2;
    if (bezierUnit(firstX, secondX, middle) < fraction) low = middle;
    else high = middle;
  }
  const time = (low + high) / 2;
  const firstY = left.value + left.outTangent * duration * leftWeight;
  const secondY = right.value - right.inTangent * duration * rightWeight;
  const inverse = 1 - time;
  return Math.fround(inverse ** 3 * left.value
    + 3 * inverse ** 2 * time * firstY
    + 3 * inverse * time ** 2 * secondY
    + time ** 3 * right.value);
}

export class AnimationCurve {
  #keys = [];
  constructor(...keys) {
    this.preWrapMode = 8;
    this.postWrapMode = 8;
    this.keys = keys.length === 1 && Array.isArray(keys[0]) ? keys[0] : keys;
  }
  get keys() { return this.#keys.map(key => key.clone()); }
  set keys(value) {
    this.#keys = [...value].map(key => key instanceof Keyframe ? key.clone()
      : Object.assign(new Keyframe(key.time, key.value, key.inTangent, key.outTangent,
        key.inWeight, key.outWeight), key)).sort((a, b) => a.time - b.time);
  }
  get length() { return this.#keys.length; }
  AddKey(keyOrTime, value) {
    const key = keyOrTime instanceof Keyframe ? keyOrTime.clone()
      : new Keyframe(keyOrTime, value);
    this.#keys.push(key);
    this.#keys.sort((a, b) => a.time - b.time);
    return this.#keys.indexOf(key);
  }
  MoveKey(index, key) {
    this.#keys.splice(index, 1);
    return this.AddKey(key);
  }
  RemoveKey(index) { this.#keys.splice(index, 1); }
  ClearKeys() { this.#keys = []; }
  CopyFrom(source) {
    this.keys = source.keys;
    this.preWrapMode = source.preWrapMode;
    this.postWrapMode = source.postWrapMode;
  }
  GetKeys(destination) {
    if (destination.length < this.#keys.length) {
      const error = new RangeError('Destination array must be large enough to store the keys');
      error.name = 'ArgumentException';
      throw error;
    }
    for (let index = 0; index < this.#keys.length; index++)
      destination[index] = this.#keys[index].clone();
  }
  SetKeys(source) { this.keys = source; }
  GetItem(index) { return this.#keys[index]?.clone(); }
  SmoothTangents(index, weight) {
    const current = this.#keys[index];
    if (!current) return;
    const previous = this.#keys[index - 1];
    const next = this.#keys[index + 1];
    const before = previous ? (current.value - previous.value) / (current.time - previous.time) : null;
    const after = next ? (next.value - current.value) / (next.time - current.time) : null;
    const tangent = before == null ? after : after == null ? before
      : (before + after) / 2 * (1 - weight) + before * weight;
    current.inTangent = Math.fround(tangent ?? 0);
    current.outTangent = Math.fround(tangent ?? 0);
  }
  Equals(other) {
    return other instanceof AnimationCurve
      && this.preWrapMode === other.preWrapMode && this.postWrapMode === other.postWrapMode
      && this.#keys.length === other.#keys.length
      && this.#keys.every((key, index) => fieldsEqual(key, other.#keys[index]));
  }
  GetHashCode() {
    let hash = 17;
    const data = JSON.stringify([this.preWrapMode, this.postWrapMode,
      this.#keys.map(key => [key.time, key.value, key.inTangent, key.outTangent,
        key.inWeight, key.outWeight, key.weightedMode, key.tangentMode])]);
    for (let index = 0; index < data.length; index++)
      hash = (Math.imul(hash, 31) + data.charCodeAt(index)) | 0;
    return hash;
  }
  static Constant(startTime, endTime, value) {
    return this.Linear(startTime, value, endTime, value);
  }
  static Linear(startTime, startValue, endTime, endValue) {
    if (startTime === endTime)
      return new AnimationCurve(new Keyframe(startTime, startValue));
    const tangent = (endValue - startValue) / (endTime - startTime);
    return new AnimationCurve(new Keyframe(startTime, startValue, 0, tangent),
      new Keyframe(endTime, endValue, tangent, 0));
  }
  static EaseInOut(startTime, startValue, endTime, endValue) {
    if (startTime === endTime)
      return new AnimationCurve(new Keyframe(startTime, startValue));
    return new AnimationCurve(new Keyframe(startTime, startValue),
      new Keyframe(endTime, endValue));
  }
  Evaluate(time) {
    const keys = this.#keys;
    if (!keys.length) return 0;
    if (keys.length > 1 && (time < keys[0].time || time > keys.at(-1).time)) {
      const start = keys[0].time, end = keys.at(-1).time, span = end - start;
      const mode = time < start ? this.preWrapMode : this.postWrapMode;
      if (span > 0 && mode === 2) time = start + ((time - start) % span + span) % span;
      if (span > 0 && mode === 4) {
        const phase = ((time - start) % (2 * span) + 2 * span) % (2 * span);
        time = start + (phase > span ? 2 * span - phase : phase);
      }
    }
    if (time <= keys[0].time) return keys[0].value;
    if (time >= keys.at(-1).time) return keys.at(-1).value;
    for (let index = 1; index < keys.length; index++) {
      const right = keys[index];
      if (time > right.time) continue;
      const left = keys[index - 1];
      const span = right.time - left.time;
      if (span <= 0) return right.value;
      const t = (time - left.time) / span;
      if (!Number.isFinite(left.outTangent) || !Number.isFinite(right.inTangent))
        return left.value;
      if (left.weightedMode & 2 || right.weightedMode & 1)
        return weightedCurveSample(left, right, span, t);
      const m0 = Number.isFinite(left.outTangent) ? left.outTangent * span : 0;
      const m1 = Number.isFinite(right.inTangent) ? right.inTangent * span : 0;
      return (2 * t ** 3 - 3 * t ** 2 + 1) * left.value
        + (t ** 3 - 2 * t ** 2 + t) * m0
        + (-2 * t ** 3 + 3 * t ** 2) * right.value
        + (t ** 3 - t ** 2) * m1;
    }
    return keys.at(-1).value;
  }
}

export class Mathf {
  static _defaultDeltaTime = () => 0;
  static get PI() { return Math.fround(Math.PI); }
  static get Deg2Rad() { return Math.fround(Math.PI / 180); }
  static get Rad2Deg() { return Math.fround(180 / Math.PI); }
  static get Infinity() { return Infinity; }
  static get NegativeInfinity() { return -Infinity; }
  static get Epsilon() { return Math.fround(1.401298464324817e-45); }
  static Abs(value) { return Math.abs(value); }
  static Min(...values) { return Math.min(...(values.length === 1 && Array.isArray(values[0]) ? values[0] : values)); }
  static Max(...values) { return Math.max(...(values.length === 1 && Array.isArray(values[0]) ? values[0] : values)); }
  static Sqrt(value) { return Math.sqrt(value); }
  static Sin(value) { return Math.sin(value); }
  static Cos(value) { return Math.cos(value); }
  static Tan(value) { return Math.tan(value); }
  static Asin(value) { return Math.asin(value); }
  static Acos(value) { return Math.acos(value); }
  static Atan(value) { return Math.atan(value); }
  static Pow(value, power) { return Math.pow(value, power); }
  static Exp(value) { return Math.exp(value); }
  static GammaToLinearSpace(value) {
    return value <= 0.04045 ? value / 12.92 : ((value + 0.055) / 1.055) ** 2.4;
  }
  static LinearToGammaSpace(value) {
    return value <= 0.0031308 ? value * 12.92 : 1.055 * value ** (1 / 2.4) - 0.055;
  }
  static FloatToHalf(value) {
    const buffer = new ArrayBuffer(2);
    const view = new DataView(buffer);
    value = Math.fround(value);
    view.setFloat16(0, value);
    let bits = view.getUint16(0);
    const magnitude = bits & 0x7fff;
    if (Number.isFinite(value) && magnitude < 0x7bff) {
      const rounded = view.getFloat16(0);
      view.setUint16(0, (bits & 0x8000) | (magnitude + 1));
      const farther = view.getFloat16(0);
      if (Math.abs(value - rounded) === Math.abs(value - farther)
        && Math.abs(farther) > Math.abs(rounded)) bits = view.getUint16(0);
    }
    return bits;
  }
  static HalfToFloat(value) {
    const view = new DataView(new ArrayBuffer(2));
    view.setUint16(0, value);
    return view.getFloat16(0);
  }
  static Log(value, base = Math.E) { return Math.log(value) / Math.log(base); }
  static Log10(value) { return Math.log10(value); }
  static Floor(value) { return Math.floor(value); }
  static Ceil(value) { return Math.ceil(value); }
  static Round(value) { return Mathf.RoundToInt(value); }
  static FloorToInt(value) { return Math.floor(value); }
  static CeilToInt(value) { return Math.ceil(value); }
  static RoundToInt(value) {
    const floor = Math.floor(value);
    const fraction = value - floor;
    if (fraction < 0.5) return floor;
    if (fraction > 0.5) return floor + 1;
    return floor % 2 === 0 ? floor : floor + 1;
  }
  static Atan2(y, x) { return Math.atan2(y, x); }
  static Clamp(value, min, max) { return Math.min(Math.max(value, min), max); }
  static Clamp01(value) { return Mathf.Clamp(value, 0, 1); }
  static Lerp(a, b, t) { return a + (b - a) * Mathf.Clamp01(t); }
  static LerpUnclamped(a, b, t) { return a + (b - a) * t; }
  static InverseLerp(a, b, value) { return a === b ? 0 : Mathf.Clamp01((value - a) / (b - a)); }
  static Sign(value) { return value < 0 ? -1 : 1; }
  static Repeat(t, length) { return length === 0 ? 0 : Mathf.Clamp(t - Math.floor(t / length) * length, 0, length); }
  static PingPong(t, length) { return length - Math.abs(Mathf.Repeat(t, length * 2) - length); }
  static SmoothStep(from, to, t) { t = Mathf.Clamp01(t); return Mathf.LerpUnclamped(from, to, t * t * (3 - 2 * t)); }
  static SmoothDamp(current, target, currentVelocity, smoothTime, maxSpeed = Infinity, deltaTime = Mathf._defaultDeltaTime()) {
    const velocity = currentVelocity?.value ?? currentVelocity;
    smoothTime = Math.max(0.0001, smoothTime);
    const omega = 2 / smoothTime;
    const x = omega * deltaTime;
    const exp = 1 / (1 + x + 0.48 * x * x + 0.235 * x * x * x);
    const difference = current - target;
    const change = Mathf.Clamp(difference, -maxSpeed * smoothTime, maxSpeed * smoothTime);
    const adjustedTarget = current - change;
    const temp = (velocity + omega * change) * deltaTime;
    const nextVelocity = (velocity - omega * temp) * exp;
    let result = adjustedTarget + (change + temp) * exp;
    if ((target - current > 0) === (result > target)) {
      result = target;
      currentVelocity.value = (result - target) / deltaTime;
    } else currentVelocity.value = nextVelocity;
    return result;
  }
  static SmoothDampAngle(current, target, currentVelocity, smoothTime, maxSpeed = Infinity, deltaTime = Mathf._defaultDeltaTime()) {
    return Mathf.SmoothDamp(current, current + Mathf.DeltaAngle(current, target),
      currentVelocity, smoothTime, maxSpeed, deltaTime);
  }
  static IsPowerOfTwo(value) { return value > 0 && (value & (value - 1)) === 0; }
  static NextPowerOfTwo(value) { return value <= 0 ? 1 : 2 ** Math.ceil(Math.log2(value)); }
  static ClosestPowerOfTwo(value) {
    if (value <= 1) return 1;
    const lower = 2 ** Math.floor(Math.log2(value));
    return value - lower < lower * 2 - value ? lower : lower * 2;
  }
  static Gamma(value, absmax, gamma) {
    const negative = value < 0;
    const magnitude = Math.abs(value);
    if (magnitude > absmax) return negative ? -magnitude : magnitude;
    const result = Math.pow(magnitude / absmax, gamma) * absmax;
    return negative ? -result : result;
  }
  static PerlinNoise(x, y) {
    // Unity 6000.3.23f1 samples Ken Perlin's improved noise with mirrored
    // coordinates, then maps its native range through these measured bounds.
    const raw = unityPerlinNoise.noise(Math.abs(Math.fround(x)), Math.abs(Math.fround(y)), 0);
    return Math.fround((raw + 0.69) / 1.483);
  }
  static PerlinNoise1D(x) { return Mathf.PerlinNoise(x, 0); }
  static DeltaAngle(current, target) {
    let delta = ((target - current) % 360 + 360) % 360;
    if (delta > 180) delta -= 360;
    return delta;
  }
  static LerpAngle(a, b, t) { return a + Mathf.DeltaAngle(a, b) * Mathf.Clamp01(t); }
  static MoveTowardsAngle(current, target, maxDelta) {
    const delta = Mathf.DeltaAngle(current, target);
    return Math.abs(delta) <= maxDelta ? current + delta
      : current + Math.sign(delta) * maxDelta;
  }
  static MoveTowards(current, target, maxDelta) {
    return Math.abs(target - current) <= maxDelta
      ? target : current + Math.sign(target - current) * maxDelta;
  }
  static Approximately(a, b) {
    return Math.abs(a - b) <= Math.max(1e-6 * Math.max(Math.abs(a), Math.abs(b)), 1e-8);
  }
}
