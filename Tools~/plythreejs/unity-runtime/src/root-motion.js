import { Quaternion, Vector3 } from './math.js';

function poseAt(clip, time) {
  const { times, rootMotion } = clip;
  if (!rootMotion || times?.length < 2
    || rootMotion.positions?.length !== times.length * 3
    || rootMotion.rotations?.length !== times.length * 4)
    return null;
  const clamped = Math.max(0, Math.min(clip.duration, time));
  let high = 1;
  while (high < times.length - 1 && times[high] < clamped) high++;
  const low = high - 1;
  const fraction = times[high] === times[low] ? 0
    : (clamped - times[low]) / (times[high] - times[low]);
  const position = new Vector3().fromArray(rootMotion.positions, low * 3)
    .lerp(new Vector3().fromArray(rootMotion.positions, high * 3), fraction);
  const rotation = new Quaternion().fromArray(rootMotion.rotations, low * 4)
    .slerp(new Quaternion().fromArray(rootMotion.rotations, high * 4), fraction);
  return { position, rotation };
}

/** Delta in clip space, including a loop boundary when the interval crosses it. */
export function sampleRootMotionDelta(clip, fromTime, toTime) {
  const position = new Vector3();
  const rotation = new Quaternion();
  if (!(clip?.duration > 0) || !(toTime > fromTime)) return { position, rotation };
  let cursor = fromTime;
  while (cursor < toTime - 1e-8) {
    const cycle = Math.floor(cursor / clip.duration);
    const start = cursor - cycle * clip.duration;
    const end = Math.min(clip.duration, toTime - cycle * clip.duration);
    const first = poseAt(clip, start);
    const last = poseAt(clip, end);
    if (!first || !last) break;
    position.add(last.position.sub(first.position));
    rotation.multiply(first.rotation.invert().multiply(last.rotation));
    cursor = cycle * clip.duration + end;
    if (end >= clip.duration) cursor = (cycle + 1) * clip.duration;
  }
  return { position, rotation };
}

export function hasMovingRoot(clip, epsilon = 1e-4) {
  const positions = clip?.rootMotion?.positions;
  const rotations = clip?.rootMotion?.rotations;
  if (!Array.isArray(positions) || !Array.isArray(rotations) || positions.length < 6) return false;
  for (let index = 3; index < positions.length; index += 3)
    if (Math.hypot(positions[index] - positions[0], positions[index + 1] - positions[1],
      positions[index + 2] - positions[2]) > epsilon) return true;
  for (let index = 4; index < rotations.length; index += 4)
    if (Math.abs(rotations[index] * rotations[0] + rotations[index + 1] * rotations[1]
      + rotations[index + 2] * rotations[2] + rotations[index + 3] * rotations[3]) < 1 - epsilon)
      return true;
  return false;
}

/** Feed exported clip deltas to Unity-named Animator and OnAnimatorMove APIs. */
export function applyAnimatorRootMotion(animator, entries) {
  const position = new Vector3();
  const rotation = new Quaternion();
  let weight = 0;
  for (const entry of entries) {
    if (!entry.clip?.rootMotion || !(entry.weight > 0)) continue;
    const delta = sampleRootMotionDelta(entry.clip, entry.fromTime, entry.toTime);
    position.addScaledVector(delta.position, entry.weight);
    rotation.multiply(new Quaternion().slerp(delta.rotation, entry.weight));
    weight += entry.weight;
  }
  animator.deltaPosition.copy(position.applyQuaternion(
    animator.gameObject.object3D.getWorldQuaternion(new Quaternion())));
  animator.deltaRotation.copy(rotation);
  if (weight === 0) return;
  const handlers = animator.gameObject._components.filter(component =>
    component._active && typeof component.OnAnimatorMove === 'function');
  if (handlers.length) for (const component of handlers) component.OnAnimatorMove();
  else if (animator.applyRootMotion) {
    animator.transform.position = animator.transform.position.add(animator.deltaPosition);
    animator.transform.rotation = animator.transform.rotation.multiply(animator.deltaRotation);
  }
}
