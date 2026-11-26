import * as THREE from 'three';
import { decodeCalibratedRigLocalRotations } from './humanoid-avatar-calibration.js';
import { fitHumanoidMassWeights } from './humanoid-mass-weight-fit.js';
import { rigWorldTransforms, solveHumanoidHipsPosition } from './humanoid-mass-center.js';
import { sampleHumanoidRawClip } from './humanoid-raw-curve-clip.js';
import { applyHumanoidPoseCorrections } from './humanoid-pose-corrections.js';
import { humanoidRootDisplacement, localHumanoidBodyPosition,
  normalizeHumanoidRootCurves } from './humanoid-root-curves.js';

/** Field-preview tolerance, applied only by the guarded selector/player. */
export const PREVIEW_HUMANOID_LIMITS = Object.freeze({
  maxDegrees: 5, maxHipsMeters: 0.03,
});

/** Build target-Avatar bone frames from a shared raw Humanoid clip. */
export function buildCalibratedHumanoidClip(rawClip, muscleNames, calibration,
  templateClip, { sampleMuscles, sampleBodyPose } = {}) {
  if (!rawClip || !templateClip?.times?.length
    || !calibration?.neutralRigBones?.length)
    throw new TypeError('Humanoid clip, frames, and Avatar calibration required');
  if (calibration.massWeights == null)
    calibration.massWeights = fitHumanoidMassWeights(calibration);
  const bones = calibration.neutralRigBones.filter(bone => bone.index >= 0);
  const neutral = rigWorldTransforms(calibration.neutralRigBones);
  const records = bones.map(bone => ({ index: bone.index,
    restRotation: neutral.get(bone.path).rotation.toArray(),
    restPosition: bone.index === 0
      ? neutral.get(bone.path).position.toArray() : null,
    rotations: new Array(templateClip.times.length * 4),
    positions: bone.index === 0
      ? new Array(templateClip.times.length * 3) : null }));
  const bodyOnlyLocalRotations = decodeCalibratedRigLocalRotations(calibration,
    calibration.neutralMuscles);
  const localHipsPositions = new Array(templateClip.times.length * 3);
  for (let frame = 0; frame < templateClip.times.length; frame++) {
    const raw = sampleHumanoidRawClip(rawClip, muscleNames,
      templateClip.times[frame]);
    const muscles = sampleMuscles ? sampleMuscles(raw, frame,
      templateClip.times[frame]) : raw.muscles;
    const evaluated = sampleBodyPose?.(templateClip.times[frame]);
    const rootRotation = evaluated ? new THREE.Quaternion().fromArray(
      evaluated.rootRotation).normalize() : new THREE.Quaternion();
    const body = evaluated ? {
      bodyRotation: rootRotation.clone().invert().multiply(
        new THREE.Quaternion().fromArray(evaluated.bodyRotation))
        .normalize().toArray(),
      bodyPosition: new THREE.Vector3().fromArray(evaluated.bodyPosition)
        .multiplyScalar(evaluated.sourceHumanScale)
        .sub(new THREE.Vector3().fromArray(evaluated.rootPosition))
        .applyQuaternion(rootRotation.clone().invert())
        .divideScalar(calibration.humanScale).toArray(),
    } : normalizeHumanoidRootCurves(rawClip, raw.bodyPosition,
      raw.bodyRotation, raw.initialPosition, raw.initialRotation);
    const localRotations = decodeCalibratedRigLocalRotations(calibration,
      muscles, body.bodyRotation);
    const bodyPosition = evaluated ? body.bodyPosition
      : localHumanoidBodyPosition(body.bodyPosition,
        humanoidRootDisplacement(rawClip, raw, calibration.humanScale),
        calibration.humanScale);
    const hipsPosition = solveHumanoidHipsPosition({
      rigBones: calibration.neutralRigBones,
      boneParameters: calibration.boneParameters,
      localRotations, bodyPosition,
      neutralBodyPosition: calibration.neutralBodyPosition,
      humanScale: calibration.humanScale,
      bodyRotationProbes: calibration.bodyRotationProbes,
      bodyPositionProbes: calibration.bodyPositionProbes,
      massWeights: calibration.massWeights,
      bodyOnlyLocalRotations,
    });
    hipsPosition.toArray(localHipsPositions, frame * 3);
    const world = rigWorldTransforms(calibration.neutralRigBones,
      localRotations, hipsPosition);
    const first = new THREE.Quaternion().fromArray(raw.initialRotation);
    const yawLength = Math.hypot(first.y, first.w);
    const extractedYaw = evaluated ? rootRotation
      : !rawClip.keepOriginalOrientation && yawLength > 1e-8
      ? new THREE.Quaternion(0, first.y / yawLength, 0,
        first.w / yawLength) : new THREE.Quaternion();
    for (let index = 0; index < bones.length; index++) {
      const transform = world.get(bones[index].path);
      extractedYaw.clone().multiply(transform.rotation)
        .toArray(records[index].rotations, frame * 4);
      if (records[index].positions)
        transform.position.toArray(records[index].positions, frame * 3);
    }
  }
  const result = { ...templateClip, poseSpace: 'actor', bones: records,
    localHipsPositions };
  return rawClip.poseCorrections
    ? applyHumanoidPoseCorrections(result, rawClip.poseCorrections) : result;
}

/** Keep the sampled fallback when a source-Avatar clip exceeds the field bound. */
export function assessCalibratedHumanoidClip(candidate, sampled,
  { maxDegrees = 0.8, maxHipsMeters = 0.003 } = {}) {
  if (candidate?.times?.length !== sampled?.times?.length)
    throw new TypeError('Humanoid clip frames do not match');
  const original = new Map(sampled.bones.map(bone => [bone.index, bone]));
  let rotationDegrees = 0;
  let hipsMeters = 0;
  for (const bone of candidate.bones) {
    const other = original.get(bone.index);
    if (!other) continue;
    for (let frame = 0; frame < sampled.times.length; frame++) {
      rotationDegrees = Math.max(rotationDegrees,
        new THREE.Quaternion().fromArray(bone.rotations, frame * 4)
          .angleTo(new THREE.Quaternion().fromArray(other.rotations,
            frame * 4)) * 180 / Math.PI);
      if (bone.index === 0) hipsMeters = Math.max(hipsMeters,
        new THREE.Vector3().fromArray(candidate.localHipsPositions,
          frame * 3).distanceTo(new THREE.Vector3().fromArray(other.positions,
            frame * 3)));
    }
  }
  return { accepted: rotationDegrees <= maxDegrees
    && hipsMeters <= maxHipsMeters, rotationDegrees, hipsMeters };
}
