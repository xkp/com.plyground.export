import { THREE } from './index.js';

// The index keys are Unity HumanBodyBones values exported with the Animator.
const sourceBones = {
  1: ['LeftUpLeg', 'L_Thigh'], 2: ['RightUpLeg', 'R_Thigh'],
  3: ['LeftLeg', 'L_Calf'], 4: ['RightLeg', 'R_Calf'],
  5: ['LeftFoot', 'L_Foot'], 6: ['RightFoot', 'R_Foot'],
  7: ['Spine', 'Spine01'], 8: ['Spine2', 'Spine02'],
  9: ['Neck', 'NeckTwist01'], 10: ['Head'],
  11: ['LeftShoulder', 'L_Clavicle'], 12: ['RightShoulder', 'R_Clavicle'],
  13: ['LeftArm', 'L_Upperarm'], 14: ['RightArm', 'R_Upperarm'],
  15: ['LeftForeArm', 'L_Forearm'], 16: ['RightForeArm', 'R_Forearm'],
  17: ['LeftHand', 'L_Hand'], 18: ['RightHand', 'R_Hand'],
  19: ['LeftToeBase', 'L_ToeBase'], 20: ['RightToeBase', 'R_ToeBase'],
};

function findSourceBone(byName, aliases) {
  for (const alias of aliases) {
    for (const name of [alias, `mixamorig${alias}`, `mixamorig:${alias}`]) {
      const bone = byName.get(name);
      if (bone) return bone;
    }
  }
  return null;
}

/** Bake source motion in world joint space into the target rig's local frames. */
export function retargetPreviewClip(targetMesh, animator, imported, { name,
  fps = 30, alignment = new THREE.Quaternion() } = {}) {
  if (!targetMesh?.isSkinnedMesh) throw new TypeError('Target must be a skinned mesh');
  const sourceByName = new Map(imported.skeleton.bones.map(bone => [bone.name, bone]));
  const targetByName = new Map(targetMesh.skeleton.bones.map(bone => [bone.name, bone]));
  const pairs = Object.entries(sourceBones).map(([index, aliases]) => ({
    target: targetByName.get(animator.humanBones?.[index]?.gameObject?.name),
    source: findSourceBone(sourceByName, aliases),
  })).filter(pair => pair.target && pair.source);
  if (!pairs.length) throw new Error('Source and target rigs have no common humanoid bones');

  targetMesh.skeleton.pose();
  targetMesh.updateWorldMatrix(true, true);
  const targetRootInverse = targetMesh.getWorldQuaternion(new THREE.Quaternion()).invert();
  const rest = new Map();
  for (const { target } of pairs) {
    for (let node = target; node && node !== targetMesh; node = node.parent) {
      if (!rest.has(node)) rest.set(node, targetRootInverse.clone()
        .multiply(node.getWorldQuaternion(new THREE.Quaternion())));
    }
  }
  const mapped = new Map(pairs.map(pair => [pair.target, pair.source]));
  const duration = imported.clips[0].duration;
  const frameCount = Math.ceil(duration * fps) + 1;
  const times = Array.from({ length: frameCount }, (_, frame) => Math.min(frame / fps, duration));
  const values = new Map(pairs.map(({ target }) => [target, new Float32Array(frameCount * 4)]));
  const mixer = new THREE.AnimationMixer(imported.source);
  mixer.clipAction(imported.clips[0]).play();
  const sourceRootInverse = imported.source.getWorldQuaternion(new THREE.Quaternion()).invert();
  const reference = new Map();
  const alignedInverse = alignment.clone().invert();
  for (const [frame, time] of times.entries()) {
    mixer.setTime(Math.min(time, duration - 0.00001));
    imported.source.updateWorldMatrix(true, true);
    const sourceWorld = new Map(pairs.map(({ source }) => [source,
      sourceRootInverse.clone().multiply(source.getWorldQuaternion(new THREE.Quaternion()))]));
    if (frame === 0) for (const [source, rotation] of sourceWorld)
      reference.set(source, rotation.clone().invert());
    const desired = new Map();
    const worldRotation = node => {
      if (!node || !rest.has(node)) return new THREE.Quaternion();
      if (desired.has(node)) return desired.get(node);
      const parent = worldRotation(node.parent);
      const source = mapped.get(node);
      const result = source
        ? alignment.clone().multiply(sourceWorld.get(source)).multiply(reference.get(source))
          .multiply(alignedInverse).multiply(rest.get(node))
        : parent.clone().multiply(rest.get(node.parent)?.clone().invert()
          ?? new THREE.Quaternion()).multiply(rest.get(node));
      desired.set(node, result);
      return result;
    };
    for (const { target } of pairs) {
      const local = worldRotation(target.parent).clone().invert().multiply(worldRotation(target));
      local.toArray(values.get(target), frame * 4);
    }
  }
  return new THREE.AnimationClip(name ?? imported.clips[0].name, duration,
    pairs.map(({ target }) => new THREE.QuaternionKeyframeTrack(
      `.bones[${target.name}].quaternion`, times, values.get(target))));
}
