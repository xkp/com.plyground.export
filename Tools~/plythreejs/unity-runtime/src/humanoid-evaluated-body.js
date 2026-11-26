import * as THREE from 'three';

/** Interpolate Unity's evaluated Body and Animator root in actor space. */
export function sampleHumanoidEvaluatedBody(record, time) {
  const times = record?.times;
  if (!times?.length || !(record.sourceHumanScale > 0))
    throw new TypeError('Invalid evaluated Humanoid body');
  let high = times.findIndex(value => value >= time);
  if (high < 0) high = times.length - 1;
  const low = Math.max(0, high - 1);
  const span = times[high] - times[low];
  const fraction = span > 0
    ? THREE.MathUtils.clamp((time - times[low]) / span, 0, 1) : 0;
  const position = values => new THREE.Vector3().fromArray(values, low * 3)
    .lerp(new THREE.Vector3().fromArray(values, high * 3), fraction).toArray();
  const rotation = values => new THREE.Quaternion().fromArray(values, low * 4)
    .slerp(new THREE.Quaternion().fromArray(values, high * 4), fraction)
    .normalize().toArray();
  return { sourceHumanScale: record.sourceHumanScale,
    rootPosition: position(record.rootPositions),
    rootRotation: rotation(record.rootRotations),
    bodyPosition: position(record.bodyPositions),
    bodyRotation: rotation(record.bodyRotations) };
}

/** Sparse Unity muscle corrections layered on the shared raw clip. */
export function applyHumanoidMuscleCorrections(record, rawMuscles, time) {
  if (!record?.times?.length || !record.muscleCorrections?.length)
    throw new TypeError('Missing evaluated Humanoid muscle corrections');
  let high = record.times.findIndex(value => value >= time);
  if (high < 0) high = record.times.length - 1;
  const low = Math.max(0, high - 1);
  const span = record.times[high] - record.times[low];
  const fraction = span > 0
    ? THREE.MathUtils.clamp((time - record.times[low]) / span, 0, 1) : 0;
  const muscles = Array.from(rawMuscles);
  for (const correction of record.muscleCorrections) {
    if (correction.values.length !== record.times.length
      || correction.index < 0 || correction.index >= muscles.length)
      throw new TypeError('Invalid Humanoid muscle correction');
    muscles[correction.index] += (correction.values[low] * (1 - fraction)
      + correction.values[high] * fraction) / 10000;
  }
  return muscles;
}

