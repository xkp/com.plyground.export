import * as THREE from 'three';

/** Apply Unity clip root settings before decoding a Humanoid body pose. */
export function normalizeHumanoidRootCurves(clip, bodyPosition, bodyRotation,
  initialPosition, initialRotation) {
  const position = [...bodyPosition];
  if (!clip.keepOriginalPositionXZ) {
    position[0] -= initialPosition[0];
    position[2] -= initialPosition[2];
  }
  const rotation = new THREE.Quaternion().fromArray(bodyRotation).normalize();
  if (!clip.keepOriginalOrientation) {
    const first = new THREE.Quaternion().fromArray(initialRotation).normalize();
    const length = Math.hypot(first.y, first.w);
    if (length > 1e-8) {
      const yaw = new THREE.Quaternion(0, first.y / length, 0,
        first.w / length);
      rotation.premultiply(yaw.invert()).normalize();
    }
  }
  return { bodyPosition: position, bodyRotation: rotation.toArray() };
}

/** Remove GameObject root motion before solving the Avatar's local hips. */
export function localHumanoidBodyPosition(bodyPosition, rootMotionPosition,
  humanScale) {
  if (!(humanScale > 0)) throw new TypeError('Invalid Humanoid scale');
  return bodyPosition.map((value, axis) =>
    value - rootMotionPosition[axis] / humanScale);
}

/** Clip horizontal root displacement, separate from the local hips pose. */
export function humanoidRootDisplacement(clip, raw, humanScale) {
  if (!(humanScale > 0)) throw new TypeError('Invalid Humanoid scale');
  return clip.keepOriginalPositionXZ ? [0, 0, 0] : [
    (raw.bodyPosition[0] - raw.initialPosition[0]) * humanScale,
    0,
    (raw.bodyPosition[2] - raw.initialPosition[2]) * humanScale,
  ];
}
