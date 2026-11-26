import * as THREE from 'three';

const ROTATION_SCALE = 10000;
const POSITION_SCALE = 1000;

function compactRotation(quaternion) {
  const q = quaternion.normalize();
  if (q.w < 0) q.set(-q.x, -q.y, -q.z, -q.w);
  return [q.x, q.y, q.z].map(value => Math.round(value * ROTATION_SCALE));
}

function expandedRotation(values, offset) {
  const x = values[offset] / ROTATION_SCALE;
  const y = values[offset + 1] / ROTATION_SCALE;
  const z = values[offset + 2] / ROTATION_SCALE;
  return new THREE.Quaternion(x, y, z,
    Math.sqrt(Math.max(0, 1 - x * x - y * y - z * z))).normalize();
}

/** Store only the world-space residuals needed on the matching source Avatar. */
export function makeHumanoidPoseCorrections(candidate, sampled) {
  if (candidate.times.length !== sampled.times.length)
    throw new TypeError('Humanoid correction frames do not match');
  const frames = sampled.times.length;
  const original = new Map(sampled.bones.map(bone => [bone.index, bone]));
  const generated = new Map(candidate.bones.map(bone => [bone.index, bone]));
  const sourceHips = original.get(0);
  const candidateHips = generated.get(0);
  if (!sourceHips || !candidateHips || !candidate.localHipsPositions)
    throw new TypeError('Humanoid Hips correction requires both poses');
  const hipsRotations = [];
  const hipsPositions = [];
  for (let frame = 0; frame < frames; frame++) {
    const offset = frame * 4;
    hipsRotations.push(...compactRotation(
      new THREE.Quaternion().fromArray(sourceHips.rotations, offset)
        .multiply(new THREE.Quaternion().fromArray(
          candidateHips.rotations, offset).invert())));
    for (let axis = 0; axis < 3; axis++)
      hipsPositions.push(Math.round((sourceHips.positions[frame * 3 + axis]
        - candidate.localHipsPositions[frame * 3 + axis])
        * POSITION_SCALE));
  }
  const bones = [];
  for (const bone of candidate.bones) {
    if (bone.index === 0) continue;
    const reference = original.get(bone.index);
    if (!reference) continue;
    let maximum = 0;
    const rotations = [];
    for (let frame = 0; frame < frames; frame++) {
      const offset = frame * 4;
      const corrected = expandedRotation(hipsRotations, frame * 3)
        .multiply(new THREE.Quaternion().fromArray(bone.rotations, offset));
      const target = new THREE.Quaternion().fromArray(reference.rotations, offset);
      const residual = target.multiply(corrected.invert());
      maximum = Math.max(maximum, residual.angleTo(new THREE.Quaternion())
        * 180 / Math.PI);
      rotations.push(...compactRotation(residual));
    }
    if (maximum > 2) bones.push({ index: bone.index, rotations });
  }
  return { times: sampled.times, hipsRotations, hipsPositions, bones };
}

function frameAt(times, time) {
  let high = times.findIndex(value => value >= time);
  if (high < 0) high = times.length - 1;
  const low = Math.max(0, high - 1);
  const span = times[high] - times[low];
  return { low, high, fraction: span > 0
    ? THREE.MathUtils.clamp((time - times[low]) / span, 0, 1) : 0 };
}

/** Apply a compact source-Avatar residual after shared muscle decoding. */
export function applyHumanoidPoseCorrections(candidate, record) {
  const frames = record?.times?.length;
  if (!frames || record.hipsRotations?.length !== frames * 3
    || record.hipsPositions?.length !== frames * 3
    || !Array.isArray(record.bones)
    || record.bones.some(bone => bone.rotations?.length !== frames * 3))
    throw new TypeError('Invalid Humanoid pose corrections');
  const byIndex = new Map(record.bones.map(bone => [bone.index, bone.rotations]));
  for (let frame = 0; frame < candidate.times.length; frame++) {
    const { low, high, fraction } = frameAt(record.times,
      candidate.times[frame]);
    const stepFraction = kind => {
      const cutoff = record[kind]?.find(value =>
        value > record.times[low] && value <= record.times[high]);
      return cutoff == null ? fraction
        : candidate.times[frame] < cutoff ? 0 : 1;
    };
    const rotationFraction = stepFraction('stepRotationTimes');
    const positionFraction = stepFraction('stepPositionTimes');
    const hips = expandedRotation(record.hipsRotations, low * 3)
      .slerp(expandedRotation(record.hipsRotations, high * 3),
        rotationFraction);
    for (const bone of candidate.bones) {
      const offset = frame * 4;
      const corrected = hips.clone().multiply(new THREE.Quaternion()
        .fromArray(bone.rotations, offset));
      const values = byIndex.get(bone.index);
      if (values) corrected.premultiply(expandedRotation(values, low * 3)
        .slerp(expandedRotation(values, high * 3), rotationFraction));
      corrected.normalize().toArray(bone.rotations, offset);
      if (bone.index === 0) for (let axis = 0; axis < 3; axis++) {
        const position = (record.hipsPositions[low * 3 + axis]
          * (1 - positionFraction) + record.hipsPositions[high * 3 + axis]
          * positionFraction) / POSITION_SCALE;
        candidate.localHipsPositions[frame * 3 + axis] += position;
        bone.positions[frame * 3 + axis] += position;
      }
    }
  }
  return candidate;
}
