import * as THREE from 'three';
import { normalizeHumanoidRootCurves } from './humanoid-root-curves.js';

const fingerName = /^(Left|Right) (Thumb|Index|Middle|Ring|Little) (.+)$/;

export function humanMuscleCurveProperty(name) {
  const finger = name.match(fingerName);
  return finger ? `${finger[1]}Hand.${finger[2]}.${finger[3]}` : name;
}

const keyAt = (keys, index, offset) => keys[index * 7 + offset];
const cubic = (a, b, c, d, t) => {
  const u = 1 - t;
  return u * u * u * a + 3 * u * u * t * b
    + 3 * u * t * t * c + t * t * t * d;
};

/** Unity AnimationCurve sampling for sparse exported Humanoid curves. */
export function sampleUnityCurve(keys, time) {
  if (!Array.isArray(keys) || keys.length === 0 || keys.length % 7)
    throw new TypeError('Invalid Unity curve keys');
  const count = keys.length / 7;
  if (count === 1 || time <= keys[0]) return keys[1];
  if (time >= keyAt(keys, count - 1, 0)) return keyAt(keys, count - 1, 1);
  let low = 0;
  let high = count - 1;
  while (high - low > 1) {
    const middle = (low + high) >> 1;
    if (keyAt(keys, middle, 0) <= time) low = middle;
    else high = middle;
  }
  const start = keyAt(keys, low, 0);
  const end = keyAt(keys, high, 0);
  const span = end - start;
  const from = keyAt(keys, low, 1);
  const to = keyAt(keys, high, 1);
  if (!(span > 0)) return to;
  const outTangent = keyAt(keys, low, 3);
  const inTangent = keyAt(keys, high, 2);
  if (!Number.isFinite(outTangent) || !Number.isFinite(inTangent)) return from;
  const outWeight = (keyAt(keys, low, 6) & 2) ? keyAt(keys, low, 5) : 1 / 3;
  const inWeight = (keyAt(keys, high, 6) & 1) ? keyAt(keys, high, 4) : 1 / 3;
  const fraction = (time - start) / span;
  let parameter = fraction;
  if (outWeight !== 1 / 3 || inWeight !== 1 / 3) {
    let left = 0;
    let right = 1;
    for (let pass = 0; pass < 24; pass++) {
      parameter = (left + right) / 2;
      if (cubic(0, outWeight, 1 - inWeight, 1, parameter) < fraction)
        left = parameter;
      else right = parameter;
    }
  }
  return cubic(from, from + outTangent * span * outWeight,
    to - inTangent * span * inWeight, to, parameter);
}

/** Sample raw HumanMotion before target Avatar decoding and root extraction. */
export function sampleHumanoidRawClip(clip, muscleNames, time) {
  const curves = clip._curveMap instanceof Map ? clip._curveMap
    : (clip._curveMap = new Map(clip.curves.map(curve =>
      [curve.property, curve.keys])));
  const at = (property, atTime = time) => {
    const keys = curves.get(property);
    if (!keys) {
      if (!clip.missingCurves?.includes(property))
        throw new TypeError(`Humanoid curve missing: ${property}`);
      return property === 'RootQ.w' ? 1 : 0;
    }
    return sampleUnityCurve(keys, Math.max(0,
      Math.min(Number(atTime) || 0, clip.duration)));
  };
  const muscles = Float32Array.from(muscleNames,
    name => at(humanMuscleCurveProperty(name)));
  const bodyPosition = ['x', 'y', 'z'].map(axis => at(`RootT.${axis}`));
  const bodyRotation = new THREE.Quaternion(...['x', 'y', 'z', 'w']
    .map(axis => at(`RootQ.${axis}`))).normalize().toArray();
  const initialPosition = ['x', 'y', 'z'].map(axis =>
    at(`RootT.${axis}`, 0));
  const initialRotation = ['x', 'y', 'z', 'w'].map(axis =>
    at(`RootQ.${axis}`, 0));
  return { muscles, bodyPosition, bodyRotation, initialPosition,
    initialRotation };
}

/** Recover GameObject root motion when an Editor-mode Playable export recorded zeros. */
export function sampleHumanoidRootTrajectory(clip, times, humanScale) {
  if (!(humanScale > 0) || !Array.isArray(times) || times.length < 2) return null;
  const curves = new Map(clip.curves.map(curve => [curve.property, curve.keys]));
  const x = curves.get('RootT.x'), z = curves.get('RootT.z');
  if (!x || !z) return null;
  const at = (axis, time, fallback) => {
    const keys = curves.get(`RootQ.${axis}`);
    return keys ? sampleUnityCurve(keys, time) : fallback;
  };
  const first = new THREE.Quaternion(at('x', 0, 0), at('y', 0, 0),
    at('z', 0, 0), at('w', 0, 1)).normalize();
  const initialX = sampleUnityCurve(x, 0), initialZ = sampleUnityCurve(z, 0);
  const positions = [], rotations = [];
  for (const time of times) {
    positions.push((sampleUnityCurve(x, time) - initialX) * humanScale,
      0, (sampleUnityCurve(z, time) - initialZ) * humanScale);
    const pose = new THREE.Quaternion(at('x', time, 0), at('y', time, 0),
      at('z', time, 0), at('w', time, 1)).normalize();
    const relative = first.clone().invert().multiply(pose);
    const yawLength = Math.hypot(relative.y, relative.w);
    rotations.push(0, yawLength > 1e-8 ? relative.y / yawLength : 0, 0,
      yawLength > 1e-8 ? relative.w / yawLength : 1);
  }
  return { positions, rotations };
}

/** Read Unity's source Avatar foot goals without assuming their target-rig frame. */
export function sampleHumanoidFootGoals(clip, time) {
  const curves = clip._curveMap instanceof Map ? clip._curveMap
    : (clip._curveMap = new Map(clip.curves.map(curve =>
      [curve.property, curve.keys])));
  const at = property => {
    const keys = curves.get(property);
    if (!keys) throw new TypeError(`Humanoid foot goal curve missing: ${property}`);
    return sampleUnityCurve(keys, Math.max(0, Math.min(Number(time) || 0, clip.duration)));
  };
  const goal = side => {
    const names = [...['x', 'y', 'z'].map(axis => `${side}FootT.${axis}`),
      ...['x', 'y', 'z', 'w'].map(axis => `${side}FootQ.${axis}`)];
    if (names.every(name => !curves.has(name))) return null;
    const position = names.slice(0, 3).map(at);
    const rotation = new THREE.Quaternion(...names.slice(3).map(at));
    if (rotation.lengthSq() < 1e-10)
      throw new TypeError(`Humanoid ${side} foot goal rotation is invalid`);
    return { position, rotation: rotation.normalize().toArray() };
  };
  const root = axis => at(`RootQ.${axis}`);
  const initial = axis => {
    const keys = curves.get(`RootQ.${axis}`);
    if (!keys) throw new TypeError(`Humanoid root rotation curve missing: RootQ.${axis}`);
    return sampleUnityCurve(keys, 0);
  };
  const bodyRotation = normalizeHumanoidRootCurves(clip, [0, 0, 0],
    ['x', 'y', 'z', 'w'].map(root), [0, 0, 0],
    ['x', 'y', 'z', 'w'].map(initial)).bodyRotation;
  return { left: goal('Left'), right: goal('Right'), bodyRotation };
}

const unityFootGoalFrame = new THREE.Quaternion(0.5, -0.5, 0.5, 0.5);

/** Reconstruct the Unity Humanoid foot goal rotation before procedural IK. */
export function humanoidFootGoalRotation(bodyRotation, footRotation,
  actorRotation = new THREE.Quaternion()) {
  return actorRotation.clone().multiply(new THREE.Quaternion()
    .fromArray(bodyRotation)).multiply(new THREE.Quaternion()
      .fromArray(footRotation)).multiply(unityFootGoalFrame).normalize();
}

/** Fetch an optional compressed Unity export during Preview's loading phase. */
export async function fetchHumanoidGzipJson(url) {
  const response = await fetch(url);
  if (!response.ok) throw new Error(`Humanoid data fetch failed: ${response.status} ${url}`);
  const bytes = new Uint8Array(await response.arrayBuffer());
  const stream = bytes[0] === 0x1f && bytes[1] === 0x8b
    ? new Blob([bytes]).stream().pipeThrough(new DecompressionStream('gzip'))
    : new Blob([bytes]).stream();
  return JSON.parse(await new Response(stream).text());
}
