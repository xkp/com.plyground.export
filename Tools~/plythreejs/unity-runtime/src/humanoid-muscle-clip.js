import * as THREE from 'three';

/** Sample Unity-exported human-space motion without applying it to a skeleton. */
export function sampleHumanoidMuscleClip(clip, time) {
  const times = clip?.times;
  const count = times?.length ?? 0;
  const muscleCount = count ? clip.muscles?.length / count : 0;
  if (!Array.isArray(times) || !count || !Number.isInteger(muscleCount)
    || muscleCount <= 0 || clip.bodyPositions?.length !== count * 3
    || clip.bodyRotations?.length !== count * 4)
    throw new TypeError('Invalid Humanoid muscle clip');
  const clamped = Math.max(times[0], Math.min(Number(time) || 0, times[count - 1]));
  let high = 1;
  while (high < count && times[high] < clamped) high++;
  high = Math.min(high, count - 1);
  const low = Math.max(0, high - 1);
  const span = times[high] - times[low];
  const alpha = span > 0 ? (clamped - times[low]) / span : 0;
  const muscles = new Float32Array(muscleCount);
  for (let index = 0; index < muscleCount; index++) {
    const from = clip.muscles[low * muscleCount + index];
    const to = clip.muscles[high * muscleCount + index];
    muscles[index] = from + (to - from) * alpha;
  }
  const position = new THREE.Vector3().fromArray(clip.bodyPositions, low * 3)
    .lerp(new THREE.Vector3().fromArray(clip.bodyPositions, high * 3), alpha);
  const rotation = new THREE.Quaternion().fromArray(clip.bodyRotations, low * 4)
    .slerp(new THREE.Quaternion().fromArray(clip.bodyRotations, high * 4), alpha);
  return { muscles, bodyPosition: position, bodyRotation: rotation };
}
