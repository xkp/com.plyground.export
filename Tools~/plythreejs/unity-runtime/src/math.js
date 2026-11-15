import * as THREE from 'three';

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
  GetItem(index) { return this.getComponent(index); }
  SetItem(index, value) { this.setComponent(index, value); }
  Normalize() { this.normalize(); }
  Set(x, y) { this.set(x, y); }
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
  static Reflect(v, normal) { return v.clone().addScaledVector(normal, -2 * v.dot(normal) / (normal.lengthSq() || 1)); }
  static SmoothDamp(current, target, currentVelocity, smoothTime, maxSpeed = Infinity, deltaTime = 0.02) {
    return smoothDampVector(current, target, currentVelocity, smoothTime, maxSpeed, deltaTime);
  }
}

export class Vector4 extends THREE.Vector4 {
  GetItem(index) { return this.getComponent(index); }
  SetItem(index, value) { this.setComponent(index, value); }
  Set(x, y, z, w) { this.set(x, y, z, w); }
  Normalize() { this.normalize(); }
  get magnitude() { return this.length(); }
  get sqrMagnitude() { return this.lengthSq(); }
  get normalized() { return this.clone().normalize(); }
  static get zero() { return new Vector4(); }
  static get one() { return new Vector4(1, 1, 1, 1); }
  static get positiveInfinity() { return new Vector4(Infinity, Infinity, Infinity, Infinity); }
  static get negativeInfinity() { return new Vector4(-Infinity, -Infinity, -Infinity, -Infinity); }
  static Distance(a, b) { return a.distanceTo(b); }
  static Dot(a, b) { return a.dot(b); }
  static Normalize(v) { return v.clone().normalize(); }
  static SqrMagnitude(v) { return v.lengthSq(); }
  static Lerp(a, b, t) { return a.clone().lerp(b, Mathf.Clamp01(t)); }
  static LerpUnclamped(a, b, t) { return a.clone().lerp(b, t); }
  static MoveTowards(a, b, maxDistanceDelta) { const d = b.clone().sub(a); return d.length() <= maxDistanceDelta || !d.lengthSq() ? b.clone() : a.clone().add(d.setLength(maxDistanceDelta)); }
  static Scale(a, b) { return a.clone().multiply(b); }
  static Min(a, b) { return a.clone().min(b); }
  static Max(a, b) { return a.clone().max(b); }
}

export class Vector3 extends THREE.Vector3 {
  GetItem(index) { return this.getComponent(index); }
  SetItem(index, value) { this.setComponent(index, value); }
  Normalize() { this.normalize(); }
  Set(x, y, z) { this.set(x, y, z); }
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
  static get back() { return new Vector3(0, 0, -1); }
  static Distance(a, b) { return a.distanceTo(b); }
  static Dot(a, b) { return a.dot(b); }
  static Normalize(v) { return v.clone().normalize(); }
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
    const lengthSq = normal.lengthSq();
    return lengthSq < 1e-12 ? direction.clone()
      : direction.clone().addScaledVector(normal, -2 * direction.dot(normal) / lengthSq);
  }
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
    return direction.multiplyScalar(Mathf.Lerp(aLength, bLength, t));
  }
  static Lerp(a, b, t) { return a.clone().lerp(b, Mathf.Clamp01(t)); }
  static MoveTowards(current, target, maxDistanceDelta) {
    const delta = target.clone().sub(current);
    return delta.length() <= maxDistanceDelta || delta.lengthSq() === 0
      ? target.clone() : current.clone().add(delta.setLength(maxDistanceDelta));
  }
  static Angle(a, b) { return a.lengthSq() * b.lengthSq() < 1e-30 ? 0 : a.angleTo(b) * Mathf.Rad2Deg; }
  static SignedAngle(a, b, axis) {
    const angle = Vector3.Angle(a, b);
    return new Vector3().crossVectors(a, b).dot(axis) < 0 ? -angle : angle;
  }
  static SmoothDamp(current, target, currentVelocity, smoothTime, maxSpeed = Infinity, deltaTime = 0.02) {
    return smoothDampVector(current, target, currentVelocity, smoothTime, maxSpeed, deltaTime);
  }
  static OrthoNormalize(normal, tangent, binormal = null) {
    const n = (normal.value ?? normal).clone().normalize();
    const t = Vector3.ProjectOnPlane(tangent.value ?? tangent, n).normalize();
    if (normal.value) normal.value = n; else normal.copy(n);
    if (tangent.value) tangent.value = t; else tangent.copy(t);
    if (binormal) {
      const b = Vector3.Cross(n, t).normalize();
      if (binormal.value) binormal.value = b; else binormal.copy(b);
    }
  }
}

export class Quaternion extends THREE.Quaternion {
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
    return new Vector3(euler.x, euler.y, euler.z).multiplyScalar(180 / Math.PI);
  }
  set eulerAngles(value) { this.copy(Quaternion.Euler(value)); }
  SetFromToRotation(from, to) { this.copy(Quaternion.FromToRotation(from, to)); }
  SetLookRotation(forward, up = Vector3.up) { this.copy(Quaternion.LookRotation(forward, up)); }
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
    return new Quaternion().setFromEuler(new THREE.Euler(
      x * Mathf.Deg2Rad, y * Mathf.Deg2Rad, z * Mathf.Deg2Rad, 'YXZ',
    ));
  }
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
  get isIdentity() { return this.elements.every((value, index) => Math.abs(value - (index % 5 === 0 ? 1 : 0)) < 1e-6); }
  get lossyScale() {
    const scale = new Vector3();
    this.decompose(new Vector3(), new Quaternion(), scale);
    return scale;
  }
  get rotation() {
    const rotation = new Quaternion();
    this.decompose(new Vector3(), rotation, new Vector3());
    return rotation;
  }
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
    const point = this.MultiplyPoint3x4(plane.normal.clone().multiplyScalar(-plane.distance));
    const inverseTranspose = this.inverse.transpose;
    const normal = inverseTranspose.MultiplyVector(plane.normal).normalize();
    return new Plane(normal, point);
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
    return new Matrix4x4().makePerspective(left, right, top, bottom, near, far);
  }
  static Multiply(a, b) { return a.clone().multiply(b); }
  static MultiplyVector4(matrix, vector) { return vector.clone().applyMatrix4(matrix); }
}

export class Plane {
  constructor(a = Vector3.up, b = 0, c = null) {
    this.normal = Vector3.up;
    this.distance = 0;
    if (c) this.Set3Points(a, b, c);
    else if (typeof b === 'number') {
      this.normal = a.clone().normalize();
      this.distance = b;
    } else this.SetNormalAndPosition(a, b);
  }
  get flipped() { return new Plane(this.normal.clone().negate(), -this.distance); }
  Flip() { this.normal.negate(); this.distance = -this.distance; }
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
  Translate(translation) { this.distance -= this.normal.dot(translation); }
  static Translate(plane, translation) {
    const moved = new Plane(plane.normal, plane.distance);
    moved.Translate(translation);
    return moved;
  }
}

export class Keyframe {
  constructor(time = 0, value = 0, inTangent = 0, outTangent = 0) {
    this.time = time;
    this.value = value;
    this.inTangent = inTangent;
    this.outTangent = outTangent;
  }
}

export class AnimationCurve {
  constructor(...keys) { this.keys = [...keys].sort((a, b) => a.time - b.time); }
  get length() { return this.keys.length; }
  AddKey(key) { this.keys.push(key); this.keys.sort((a, b) => a.time - b.time); return this.keys.indexOf(key); }
  Evaluate(time) {
    const keys = this.keys;
    if (!keys.length) return 0;
    if (time <= keys[0].time) return keys[0].value;
    if (time >= keys.at(-1).time) return keys.at(-1).value;
    for (let index = 1; index < keys.length; index++) {
      const right = keys[index];
      if (time > right.time) continue;
      const left = keys[index - 1];
      const span = right.time - left.time;
      if (span <= 0) return right.value;
      const t = (time - left.time) / span;
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
  static PI = Math.PI;
  static Deg2Rad = Math.PI / 180;
  static Rad2Deg = 180 / Math.PI;
  static Infinity = Infinity;
  static NegativeInfinity = -Infinity;
  static Epsilon = 1.401298e-45;
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
  static SmoothDamp(current, target, currentVelocity, smoothTime, maxSpeed = Infinity, deltaTime = 0.02) {
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
  static SmoothDampAngle(current, target, currentVelocity, smoothTime, maxSpeed = Infinity, deltaTime = 0.02) {
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
  static PerlinNoise(x, y) {
    const ix = Math.floor(x), iy = Math.floor(y);
    const smooth = value => value * value * (3 - 2 * value);
    const hash = (a, b) => {
      const value = Math.sin(a * 127.1 + b * 311.7) * 43758.5453123;
      return value - Math.floor(value);
    };
    const tx = smooth(x - ix), ty = smooth(y - iy);
    return Mathf.Lerp(
      Mathf.Lerp(hash(ix, iy), hash(ix + 1, iy), tx),
      Mathf.Lerp(hash(ix, iy + 1), hash(ix + 1, iy + 1), tx), ty);
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
