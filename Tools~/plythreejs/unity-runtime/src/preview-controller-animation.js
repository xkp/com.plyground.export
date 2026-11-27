import { Animator, Camera, MonoBehaviour, Object as UnityObject, Quaternion, SkinnedMeshRenderer, THREE, Time,
  applyAnimatorRootMotion, hasMovingRoot } from './index.js';
import { assessCalibratedHumanoidClip,
  buildCalibratedHumanoidClip,
  PREVIEW_HUMANOID_LIMITS } from './humanoid-calibrated-clip.js';
import { applyHumanoidMuscleCorrections,
  sampleHumanoidEvaluatedBody } from './humanoid-evaluated-body.js';
import { humanoidFootGoalRotation,
  sampleHumanoidFootGoals, sampleHumanoidRawClip,
  sampleHumanoidRootTrajectory } from './humanoid-raw-curve-clip.js';
import { humanoidRootDisplacement, localHumanoidBodyPosition,
  normalizeHumanoidRootCurves } from './humanoid-root-curves.js';
import { rigWorldTransforms } from './humanoid-mass-center.js';

const bodyPart = {
  0: 'Root', 1: 'LeftLeg', 2: 'RightLeg', 3: 'LeftLeg', 4: 'RightLeg',
  5: 'LeftLeg', 6: 'RightLeg', 7: 'Body', 8: 'Body', 9: 'Head', 10: 'Head',
  11: 'LeftArm', 12: 'RightArm', 13: 'LeftArm', 14: 'RightArm',
  15: 'LeftArm', 16: 'RightArm', 17: 'LeftArm', 18: 'RightArm',
  19: 'LeftLeg', 20: 'RightLeg',
  21: 'Head', 22: 'Head', 23: 'Head',
  54: 'Body'
};
for (let index = 24; index <= 38; index++) bodyPart[index] = 'LeftFingers';
for (let index = 39; index <= 53; index++) bodyPart[index] = 'RightFingers';

function sampleFraction(clip, kind, low, high, time, fraction) {
  const cutoff = clip[kind]?.find(value =>
    value > clip.times[low] && value <= clip.times[high]);
  return cutoff == null ? fraction : time < cutoff ? 0 : 1;
}

export function sampleRotation(clip, record, time) {
  if (!record || !clip.times?.length) return null;
  if (!(clip.duration > 0) || clip.times.length === 1
    || clip.times.at(-1) <= clip.times[0])
    return new THREE.Quaternion().fromArray(record.rotations, 0);
  const t = clip.loop === false
    ? THREE.MathUtils.clamp(time, 0, clip.times.at(-1))
    : ((time % clip.duration) + clip.duration) % clip.duration;
  let high = clip.times.findIndex(value => value >= t);
  if (high < 0) high = clip.times.length - 1;
  const low = Math.max(0, high - 1);
  const a = new THREE.Quaternion().fromArray(record.rotations, low * 4);
  if (high === low) return a;
  const b = new THREE.Quaternion().fromArray(record.rotations, high * 4);
  const span = clip.times[high] - clip.times[low];
  const fraction = span > 0 ? THREE.MathUtils.clamp(
    (t - clip.times[low]) / span, 0, 1) : 0;
  return a.slerp(b, sampleFraction(clip, 'stepRotationTimes',
    low, high, t, fraction));
}

export function samplePosition(clip, record, time) {
  if (!record?.positions?.length || !record.restPosition?.length || !clip.times?.length)
    return null;
  if (!(clip.duration > 0) || clip.times.length === 1
    || clip.times.at(-1) <= clip.times[0])
    return new THREE.Vector3().fromArray(record.positions, 0);
  const t = clip.loop === false
    ? THREE.MathUtils.clamp(time, 0, clip.times.at(-1))
    : ((time % clip.duration) + clip.duration) % clip.duration;
  let high = clip.times.findIndex(value => value >= t);
  if (high < 0) high = clip.times.length - 1;
  const low = Math.max(0, high - 1);
  const a = new THREE.Vector3().fromArray(record.positions, low * 3);
  if (high === low) return a;
  const b = new THREE.Vector3().fromArray(record.positions, high * 3);
  const span = clip.times[high] - clip.times[low];
  const fraction = span > 0 ? THREE.MathUtils.clamp(
    (t - clip.times[low]) / span, 0, 1) : 0;
  return a.lerp(b, sampleFraction(clip, 'stepPositionTimes',
    low, high, t, fraction));
}

function included(layer, index) {
  const mask = layer?.humanoidMask;
  return !mask?.length || mask.includes(bodyPart[index]);
}

function findBody(root, animator) {
  const renderers = root.GetComponentsInChildren(SkinnedMeshRenderer, true)
    .filter(renderer => renderer.threeMesh?.isSkinnedMesh);
  const names = new Set(Object.values(animator.humanBones ?? {})
    .map(transform => transform?.gameObject?.name).filter(Boolean));
  return renderers.sort((a, b) => b.threeMesh.skeleton.bones
    .filter(bone => names.has(bone.name)).length
    - a.threeMesh.skeleton.bones.filter(bone => names.has(bone.name)).length)[0] ?? null;
}

function restoreHumanoidFingerRoots(bindings) {
  const bones = new Map(bindings.map(({ index, bone }) => [index, bone]));
  for (const [handIndex, fingerRoots] of [
    [17, [24, 27, 30, 33, 36]], [18, [39, 42, 45, 48, 51]]
  ]) {
    const hand = bones.get(handIndex);
    if (!hand) continue;
    for (const index of fingerRoots) {
      const finger = bones.get(index);
      if (finger && !finger.parent?.isBone) hand.attach(finger);
    }
  }
}

export function solveHandIK(hand, upper, lower, state, binding, avatarFrame) {
  if (!hand || !upper || !lower || !state) return;
  const weight = Math.min(1, Math.max(0, state.positionWeight ?? 0));
  if (weight > 0 && state.position) {
    const goal = new THREE.Vector3(state.position.x, state.position.y, state.position.z);
    if (binding) {
      const desiredBoneRotation = state.rotation
        ? new THREE.Quaternion(state.rotation.x, state.rotation.y, state.rotation.z,
          state.rotation.w).multiply(avatarFrame ?? new THREE.Quaternion())
          .multiply(binding.offsetRotation.clone().invert())
        : hand.getWorldQuaternion(new THREE.Quaternion());
      goal.sub(binding.offsetPosition.clone().applyQuaternion(desiredBoneRotation));
    }
    upper.updateWorldMatrix(true, false);
    lower.updateWorldMatrix(true, false);
    hand.updateWorldMatrix(true, false);
    const shoulder = upper.getWorldPosition(new THREE.Vector3());
    const elbow = lower.getWorldPosition(new THREE.Vector3());
    const wrist = hand.getWorldPosition(new THREE.Vector3());
    const upperLength = elbow.distanceTo(shoulder);
    const lowerLength = wrist.distanceTo(elbow);
    const reach = goal.clone().sub(shoulder);
    if (upperLength > 1e-6 && lowerLength > 1e-6 && reach.lengthSq() > 1e-12) {
      const distance = THREE.MathUtils.clamp(reach.length(),
        Math.abs(upperLength - lowerLength) + 1e-6,
        upperLength + lowerLength - 1e-6);
      const direction = reach.normalize();
      const pole = elbow.clone().sub(shoulder);
      pole.addScaledVector(direction, -pole.dot(direction));
      if (pole.lengthSq() > 1e-10) pole.normalize();
      if (state.hintWeight > 0 && state.hintPosition) {
        const hint = new THREE.Vector3(state.hintPosition.x,
          state.hintPosition.y, state.hintPosition.z).sub(shoulder);
        hint.addScaledVector(direction, -hint.dot(direction));
        if (hint.lengthSq() > 1e-10) {
          hint.normalize();
          pole.lerp(hint, THREE.MathUtils.clamp(state.hintWeight, 0, 1));
        }
      }
      if (pole.lengthSq() < 1e-10) {
        pole.copy(elbow).sub(shoulder).cross(direction).cross(direction).negate();
        if (pole.lengthSq() < 1e-10) pole.set(0, 1, 0)
          .addScaledVector(direction, -direction.y);
      }
      pole.normalize();
      const along = (upperLength ** 2 - lowerLength ** 2 + distance ** 2)
        / (2 * distance);
      const away = Math.sqrt(Math.max(0, upperLength ** 2 - along ** 2));
      const wantedElbow = shoulder.clone().addScaledVector(direction, along)
        .addScaledVector(pole, away);
      const originalUpper = upper.quaternion.clone();
      const originalLower = lower.quaternion.clone();
      const rotateJoint = (joint, from, to) => {
        const correction = new THREE.Quaternion().setFromUnitVectors(
          from.normalize(), to.normalize());
        const world = joint.getWorldQuaternion(new THREE.Quaternion())
          .premultiply(correction);
        joint.quaternion.copy(joint.parent.getWorldQuaternion(
          new THREE.Quaternion()).invert().multiply(world));
        joint.updateWorldMatrix(false, true);
      };
      rotateJoint(upper, elbow.clone().sub(shoulder),
        wantedElbow.clone().sub(shoulder));
      const actualElbow = lower.getWorldPosition(new THREE.Vector3());
      const actualWrist = hand.getWorldPosition(new THREE.Vector3());
      const wantedWrist = shoulder.clone().addScaledVector(direction, distance);
      rotateJoint(lower, actualWrist.sub(actualElbow),
        wantedWrist.sub(actualElbow));
      if (weight < 1) {
        upper.quaternion.copy(originalUpper.slerp(upper.quaternion, weight));
        lower.quaternion.copy(originalLower.slerp(lower.quaternion, weight));
        upper.updateWorldMatrix(false, true);
      }
    }
  }
  const rotationWeight = Math.min(1, Math.max(0, state.rotationWeight ?? 0));
  if (rotationWeight > 0 && state.rotation) {
    const parent = hand.parent.getWorldQuaternion(new THREE.Quaternion()).invert();
    const goal = new THREE.Quaternion(state.rotation.x, state.rotation.y,
      state.rotation.z, state.rotation.w).multiply(avatarFrame ?? new THREE.Quaternion());
    if (binding) goal.multiply(binding.offsetRotation.clone().invert());
    hand.quaternion.slerp(parent.multiply(goal), rotationWeight);
  }
}

// Measured against Unity Play Mode for the JU TPS mannequin in the five-weapon
// shooting trace. Unity's Humanoid IK goal uses an Avatar hand frame, while the
// browser skin uses the imported RightHand bone frame.
const juMannequinRightHandFrame = [0.723401, -0.045939, -0.083280, 0.683846];
const juMannequinAvatarId =
  'GlobalObjectId_V1-1-aeb4f7aff45f24d4f9216ed5c1602b3f-9000000-0';
const juMannequinHandPath =
  'RootBone/Hips/Spine/Spine1/RightShoulder/RightArm/RightForeArm/RightHand';
const calibratedClipsByRecord = new WeakMap();

function knownHandFrame(record, mesh) {
  if (record.sourceAvatarId !== juMannequinAvatarId) return null;
  const hand = mesh.skeleton.bones.find(bone => bone.name === 'RightHand');
  if (!hand) return null;
  const path = [];
  for (let node = hand; node; node = node.parent) {
    path.unshift(node.name);
    if (node.name === 'Armature') break;
  }
  return path.join('/').endsWith(juMannequinHandPath)
    ? new THREE.Quaternion().fromArray(juMannequinRightHandFrame) : null;
}

/** The same exported Animator graph and Humanoid clips drive every Preview actor. */
export class PreviewControllerAnimation extends MonoBehaviour {
  configure(animator, body, record, rawData = null) {
    this.animator = animator;
    this._animatorUpdateDriver = animator;
    this.body = body;
    this.targetCalibration = rawData?.avatarCalibrations?.get(animator.avatarData?.id)
      ?? rawData?.calibrations?.avatars.find(value =>
        value.avatarId === animator.avatarData?.id) ?? null;
    this.sharedCalibratedClips = calibratedClipsByRecord.get(record);
    if (!this.sharedCalibratedClips) {
      this.sharedCalibratedClips = new Map();
      calibratedClipsByRecord.set(record, this.sharedCalibratedClips);
    }
    this.poseSpace = record.poseSpace;
    this.ikHandFrames = {
      left: record.ikHandFrames?.left && new THREE.Quaternion().fromArray(record.ikHandFrames.left),
      right: record.ikHandFrames?.right && new THREE.Quaternion().fromArray(record.ikHandFrames.right)
    };
    this.clips = new Map(record.clips.map(clip => [clip.name.toLowerCase(), {
      ...clip, loop: clip.loop ?? !/^(dying|death)$/i.test(clip.name),
      rootMotion: { positions: clip.rootPositions, rotations: clip.rootRotations },
      bonesByIndex: new Map(clip.bones.map(bone => [bone.index, {
        ...bone, restInverse: new THREE.Quaternion().fromArray(bone.restRotation).invert()
      }]))
    }]));
    this.rawCalibration = rawData?.calibrations?.avatars.find(value =>
      value.avatarId === record.sourceAvatarId) ?? null;
    const rawController = rawData?.rawMotion?.controllers.find(value =>
      value.assetId === record.assetId);
    this.rawClips = this.rawCalibration && rawController
      ? new Map(rawController.clips.filter(clip =>
        record.rawReadyClips?.includes(clip.name)).map(clip =>
        [clip.name.toLowerCase(), clip])) : new Map();
    this.rawMuscleNames = rawData?.rawMotion?.muscleNames ?? null;
    this.rawRootMotionFallback = new Set();
    for (const [name, rawClip] of this.rawClips) {
      const sampled = this.clips.get(name);
      if (!sampled || hasMovingRoot(sampled)) continue;
      const trajectory = sampleHumanoidRootTrajectory(rawClip, sampled.times,
        this.rawCalibration.humanScale);
      const positions = trajectory?.positions;
      const last = positions?.length - 3;
      if (last == null || Math.hypot(positions[last], positions[last + 2]) < 0.15) continue;
      sampled.rootMotion = trajectory;
      this.rawRootMotionFallback.add(name);
    }
    this.rawBodyReadyClips = new Set(record.rawBodyReadyClips?.map(name =>
      name.toLowerCase()) ?? []);
    this.rawMuscleReadyClips = new Set(record.rawMuscleReadyClips?.map(name =>
      name.toLowerCase()) ?? []);
    this.rawChecked = new Set();
    if (record.clips.some(clip => !clip.bones?.length) && !this.rawClips.size)
      console.error('Preview Humanoid raw motion unavailable; animation poses cannot be built for this controller.');
    const hasRootTrajectory = motion => {
      if (!motion) return false;
      if (motion.kind === 'clip') return hasMovingRoot(this.clips.get(motion.name.toLowerCase()));
      return motion.kind === 'blendTree' && motion.children?.length > 0
        && motion.children.every(child => hasRootTrajectory(child.motion));
    };
    animator.SetControllerGraph(animator.controllerGraph, animator._behaviourTypes, {
      availableClip: motion => this.clips.has(motion.name.toLowerCase()),
      clipDuration: motion => Math.max(this.clips.get(motion.name.toLowerCase())?.duration ?? 1,
        1 / 30),
      skipBehaviour: (behaviour, state) => behaviour.type.endsWith('.ForceRootMotion')
        && !hasRootTrajectory(state.motion)
    });
    const mesh = body.threeMesh;
    const juMannequinFrame = knownHandFrame(record, mesh);
    this.ikHandFrames.right ??= juMannequinFrame;
    this.juMannequinGrip = Boolean(juMannequinFrame);
    const byName = new Map(mesh.skeleton.bones.map(bone => [bone.name, bone]));
    const normalizedName = name => name?.replace(/[ _]+/g, '');
    const byNormalizedName = new Map();
    for (const bone of mesh.skeleton.bones) {
      const key = normalizedName(bone.name);
      if (byNormalizedName.has(key)) byNormalizedName.set(key, null);
      else byNormalizedName.set(key, bone);
    }
    const skinBone = name => byName.get(name) ?? byNormalizedName.get(normalizedName(name));
    this.bindings = Object.entries(animator.humanBones ?? {}).map(([key, transform]) => ({
      index: Number(key), bone: skinBone(transform?.gameObject?.name)
    })).filter(entry => entry.bone && this.clips.size);
    // Some Unity avatars map a humanoid slot to a rig control rather than a
    // skinned joint. Animator evaluation restores these controls each frame
    // before OnAnimatorIK/LateUpdate can add procedural motion.
    this.unboundHumanoidControls = [...new Set(Object.values(animator.humanBones ?? {})
      .filter(transform => transform && !skinBone(transform.gameObject?.name)))]
      .map(transform => ({ transform, position: transform.localPosition,
        rotation: transform.localRotation, scale: transform.localScale }));
    restoreHumanoidFingerRoots(this.bindings);
    mesh.updateWorldMatrix(true, true);
    const rootInverse = mesh.getWorldQuaternion(new THREE.Quaternion()).invert();
    const actorInverse = animator.gameObject.object3D
      .getWorldQuaternion(new THREE.Quaternion()).invert();
    this.bindings.forEach(binding => {
      binding.rest = rootInverse.clone().multiply(binding.bone.getWorldQuaternion(new THREE.Quaternion()));
      binding.actorRest = actorInverse.clone().multiply(
        binding.bone.getWorldQuaternion(new THREE.Quaternion()));
      binding.restPosition = binding.bone.position.clone();
      let depth = 0;
      for (let node = binding.bone.parent; node; node = node.parent) depth++;
      binding.depth = depth;
    });
    let neutral = [...this.clips.values()].find(clip => /\bneutral idle\b/i.test(clip.name))
      ?? [...this.clips.values()].find(clip => /\bidle\b/i.test(clip.name));
    if (neutral && !neutral.bones.length && this.rawClips.has(neutral.name.toLowerCase())) {
      this.activateRawClip(neutral.name);
      neutral = this.clips.get(neutral.name.toLowerCase());
    }
    const bindingByIndex = new Map(this.bindings.map(binding => [binding.index, binding]));
    const armDown = (upperIndex, forearmIndex) => {
      const upper = bindingByIndex.get(upperIndex)?.bone;
      const forearm = bindingByIndex.get(forearmIndex)?.bone;
      if (!upper || !forearm) return false;
      const a = upper.getWorldPosition(new THREE.Vector3());
      const b = forearm.getWorldPosition(new THREE.Vector3());
      return b.sub(a).normalize().dot(new THREE.Vector3(0, -1, 0)) > 0.5;
    };
    const leftArmUsesIdle = armDown(13, 15);
    const rightArmUsesIdle = armDown(14, 16);
    for (const binding of this.bindings) {
      if (this.poseSpace === 'actor') continue;
      const source = neutral?.bonesByIndex.get(binding.index);
      if (!source?.rotations?.length) continue;
      const index = binding.index;
      const isLeftArm = [11, 13, 15, 17].includes(index) || (index >= 24 && index <= 38);
      const isRightArm = [12, 14, 16, 18].includes(index) || (index >= 39 && index <= 53);
      // A target bound with its arms down already resembles the source idle,
      // while the source avatar itself is bound in a different arm pose.
      if ((isLeftArm && leftArmUsesIdle) || (isRightArm && rightArmUsesIdle))
        binding.referenceInverse = new THREE.Quaternion().fromArray(source.rotations, 0).invert();
    }
    this.bindings.sort((a, b) => a.depth - b.depth);
    const bindingByBone = new Map(this.bindings.map(binding => [binding.bone, binding]));
    for (const binding of this.bindings) {
      for (let parent = binding.bone.parent; parent; parent = parent.parent) {
        const ancestor = bindingByBone.get(parent);
        if (ancestor) { binding.humanoidParent = ancestor; break; }
      }
    }
    if (this.rawCalibration?.neutralRigBones?.length) {
      const sourceWorld = rigWorldTransforms(this.rawCalibration.neutralRigBones);
      const sourceByIndex = new Map(this.rawCalibration.neutralRigBones
        .filter(bone => bone.index >= 0 && sourceWorld.has(bone.path))
        .map(bone => [bone.index, sourceWorld.get(bone.path).rotation]));
      for (const binding of this.bindings) {
        binding.poseParent = binding.humanoidParent;
        while (binding.poseParent && !sourceByIndex.has(binding.poseParent.index))
          binding.poseParent = binding.poseParent.humanoidParent;
        const index = binding.index;
        const useIdle = (([11, 13, 15, 17].includes(index) || (index >= 24 && index <= 38))
          && leftArmUsesIdle) || (([12, 14, 16, 18].includes(index)
          || (index >= 39 && index <= 53)) && rightArmUsesIdle);
        const reference = slot => useIdle && neutral?.bonesByIndex.get(slot)
          ? sampleRotation(neutral, neutral.bonesByIndex.get(slot), 0)
          : sourceByIndex.get(slot);
        const source = reference(index);
        if (!source) continue;
        const parentSource = reference(binding.poseParent?.index)
          ?? new THREE.Quaternion();
        const parentTarget = binding.poseParent?.actorRest ?? new THREE.Quaternion();
        const sourceLocal = parentSource.clone().invert().multiply(source);
        const targetLocal = parentTarget.clone().invert().multiply(binding.actorRest);
        binding.retargetLocal = targetLocal.multiply(sourceLocal.invert());
      }
    }
    this.byIndex = new Map(this.bindings.map(binding => [binding.index, binding.bone]));
    // Some imported avatars skin the pelvis, legs, and torso under separate
    // root bones while the Humanoid hips slot points at a Unity rig control.
    // Those roots still need the clip's actor-space hips translation (notably
    // the descent in Dying), otherwise only the rotations play and the body
    // remains suspended at its standing height.
    this.unboundHipRoots = this.byIndex.has(0) ? [] : mesh.skeleton.bones
      .filter(bone => !bone.parent?.isBone)
      .map(bone => ({ bone, restPosition: bone.position.clone() }));
    const humanoidHips = animator.humanBones?.[0];
    this.targetHipsHeight = humanoidHips
      ? this.gameObject.object3D.worldToLocal(humanoidHips.position.clone()).y : 0;
    this.eventTimes = new Map();
    body.useExternalAnimation = true;
    animator.BindImportedSkeleton(mesh.skeleton, { preserveOffset: true });
    this.importedBindings = new Map(animator._importedBoneBindings
      .map(binding => [binding.bone, binding]));
    this.clothingRenderers = this.gameObject.GetComponentsInChildren(SkinnedMeshRenderer, true)
      .filter(renderer => renderer._previewBoneBindings);
    this.zombieAI = this.gameObject._components.find(component => component.constructor.name === 'JU_AI_Zombie');
    this.patrolAI = this.gameObject._components.find(component =>
      component.constructor.name === 'JU_AI_PatrolCharacter');
    this.smoothAIBodyOffset = Boolean(this.zombieAI || this.patrolAI);
    this.aiBodyController = this.smoothAIBodyOffset && this.gameObject._components.find(
      component => component.constructor.name === 'JUCharacterController');
    this.zombieHealth = this.zombieAI && this.gameObject._components
      .find(component => component.constructor.name === 'JUHealth');
    this.zombieController = this.zombieAI && this.gameObject._components
      .find(component => component.constructor.name === 'JUCharacterController');
    this.hasAutoDestroy = this.gameObject._components.some(component => component.constructor.name === 'JUAutoDestroy');
    this.deathElapsed = null;
    this.meleeCycle = null;
    this.meleeApplied = false;
    this.meleeLastTime = null;
    const restoreHumanoidControls = () => {
      if (!this._active || !this.animator._active) return;
      for (const { transform, position, rotation, scale } of this.unboundHumanoidControls) {
        transform.localPosition = position;
        transform.localRotation = rotation;
        transform.localScale = scale;
      }
    };
    this._latePoseSystem = { beforeFixedUpdate: restoreHumanoidControls,
      beforeUpdate: restoreHumanoidControls, afterLateUpdate: () => {
      if (!this._active || !this.gameObject.activeInHierarchy) return;
      // A JU ragdoll disables Animator while physics still moves the exported
      // bone transforms. Keep the imported skin following those transforms.
      if (this.animator.enabled && this.zombieAI && this.animator.updateMode === 1
        && !this._poseSampledThisFrame) return;
      this.animator.ApplyImportedSkeletonOverrides();
      // Clothing LateUpdate ran before procedural scripts such as BodyLeanInert.
      // Copy the final body pose to every garment in the same render frame.
      for (const renderer of this.clothingRenderers)
        if (renderer._active) renderer.LateUpdate();
    } };
    this._registerLatePoseSystem();
  }

  _registerLatePoseSystem() {
    const runtime = this.gameObject._runtime;
    if (runtime && this._latePoseSystem && this._latePoseRuntime !== runtime) {
      this._latePoseRuntime?.removeSystem(this._latePoseSystem);
      runtime.addSystem(this._latePoseSystem);
      this._latePoseRuntime = runtime;
    }
  }

  OnEnable() { this._registerLatePoseSystem(); }
  OnDestroy() { this._latePoseRuntime?.removeSystem(this._latePoseSystem); }

  updateAnimatedFootGoals(outputs) {
    const goals = [{ side: 'left', goal: 0, rotation: null, weight: 0, missing: false },
      { side: 'right', goal: 1, rotation: null, weight: 0, missing: false }];
    const actorRotation = this.animator.gameObject.object3D
      .getWorldQuaternion(new THREE.Quaternion());
    for (const entry of outputs[0]?.clips ?? []) {
      if (!(entry.weight > 0)) continue;
      const raw = this.rawClips.get(entry.name.toLowerCase());
      if (!raw) { goals.forEach(goal => { goal.missing = true; }); continue; }
      const time = entry.time ?? (this.animator._controllerRuntime.players[0]?.elapsed ?? 0)
        * (entry.timeScale ?? 1);
      let sampled;
      try { sampled = sampleHumanoidFootGoals(raw, time); }
      catch (error) {
        console.error(`Preview animated foot goals unavailable for ${entry.name}.`, error);
        goals.forEach(goal => { goal.missing = true; });
        continue;
      }
      for (const goal of goals) {
        if (!sampled[goal.side]) { goal.missing = true; continue; }
        const rotation = humanoidFootGoalRotation(sampled.bodyRotation,
          sampled[goal.side].rotation, actorRotation);
        const next = goal.weight + entry.weight;
        goal.rotation = goal.rotation
          ? goal.rotation.slerp(rotation, entry.weight / next) : rotation;
        goal.weight = next;
      }
    }
    this.animator._ikAnimationRotations.clear();
    for (const goal of goals)
      if (!goal.missing && goal.rotation)
        this.animator._ikAnimationRotations.set(goal.goal,
          new Quaternion().copy(goal.rotation));
  }

  updateAnimatedBodyPosition(outputs) {
    this.animator._animatedBodyPosition = null;
    const scale = this.rawCalibration?.humanScale;
    if (!(scale > 0) || !this.rawMuscleNames ||
      (this.animator.avatarData?.id && this.animator.avatarData.id !== this.rawCalibration.avatarId))
      return;
    let local = new THREE.Vector3();
    let total = 0;
    for (const entry of outputs[0]?.clips ?? []) {
      if (!(entry.weight > 0)) continue;
      const clip = this.rawClips.get(entry.name.toLowerCase());
      if (!clip) return;
      const time = entry.time
        ?? (this.animator._controllerRuntime.players[0]?.elapsed ?? 0)
          * (entry.timeScale ?? 1);
      try {
        const raw = sampleHumanoidRawClip(clip, this.rawMuscleNames, time);
        const normalized = normalizeHumanoidRootCurves(clip, raw.bodyPosition,
          raw.bodyRotation, raw.initialPosition, raw.initialRotation);
        const displacement = this.rawRootMotionFallback.has(entry.name.toLowerCase())
          ? [(raw.bodyPosition[0] - raw.initialPosition[0]) * scale, 0,
            (raw.bodyPosition[2] - raw.initialPosition[2]) * scale]
          : humanoidRootDisplacement(clip, raw, scale);
        const body = localHumanoidBodyPosition(normalized.bodyPosition,
          displacement, scale);
        local.addScaledVector(new THREE.Vector3().fromArray(body), entry.weight);
        total += entry.weight;
      } catch (error) {
        console.warn(`Preview Animator.bodyPosition unavailable for ${entry.name}.`, error);
        return;
      }
    }
    if (!(total > 0)) return;
    local.divideScalar(total).multiplyScalar(scale);
    this.animator.gameObject.object3D.updateWorldMatrix(true, false);
    this.animator._animatedBodyPosition = this.animator.gameObject.object3D.localToWorld(local);
  }

  activateRawClip(name) {
    const key = name?.toLowerCase();
    if (!key || this.rawChecked.has(key) || !this.rawClips.has(key)) return;
    this.rawChecked.add(key);
    const sampled = this.clips.get(key);
    if (!sampled) return;
    const calibration = this.targetCalibration ?? this.rawCalibration;
    const cacheKey = `${calibration.avatarId}:${key}`;
    this.sharedCalibratedClips ??= new Map();
    const cached = this.sharedCalibratedClips.get(cacheKey);
    if (cached) { this.clips.set(key, cached); return; }
    try {
      const rawClip = this.rawClips.get(key);
      const candidate = buildCalibratedHumanoidClip(rawClip,
        this.rawMuscleNames, calibration, sampled,
        this.rawBodyReadyClips?.has(key) ? {
          sampleBodyPose: time => sampleHumanoidEvaluatedBody(
            rawClip.evaluatedBody, time),
          sampleMuscles: this.rawMuscleReadyClips?.has(key)
            ? (sample, _frame, time) => applyHumanoidMuscleCorrections(
              rawClip.evaluatedBody, sample.muscles, time)
            : undefined,
        } : undefined);
      if (this.rawRootMotionFallback?.has(key)) {
        const trajectory = sampled.rootMotion.positions;
        const hips = candidate.bones.find(bone => bone.index === 0);
        for (let frame = 0; frame < sampled.times.length; frame++)
          for (let axis = 0; axis < 3; axis++) {
            const offset = frame * 3 + axis;
            if (hips?.positions) hips.positions[offset] -= trajectory[offset];
            if (candidate.localHipsPositions)
              candidate.localHipsPositions[offset] -= trajectory[offset];
          }
      }
      if (!candidate.bones?.length || candidate.bones.some(bone =>
        bone.rotations.some(value => !Number.isFinite(value))))
        throw new TypeError('Raw Humanoid pose contains invalid rotations');
      if (sampled.bones?.length && calibration === this.rawCalibration) {
        const assessment = assessCalibratedHumanoidClip(candidate, sampled,
          PREVIEW_HUMANOID_LIMITS);
        if (!assessment.accepted) {
          console.warn(`Preview raw Humanoid ${name} exceeded the source Avatar bound; using sampled clip.`, assessment);
          return;
        }
      }
      candidate.bonesByIndex = new Map(candidate.bones.map(bone =>
        [bone.index, { ...bone, restInverse: new THREE.Quaternion()
          .fromArray(bone.restRotation).invert() }]));
      candidate.targetAvatarId = calibration.avatarId;
      this.clips.set(key, candidate);
      this.sharedCalibratedClips.set(cacheKey, candidate);
    } catch (error) {
      console.error(`Preview raw Humanoid ${name} unavailable; animation pose will remain inactive.`, error);
    }
  }

  removeFinishedZombie() {
    if (!this.zombieAI || this.hasAutoDestroy || !this.zombieHealth) return false;
    if (!this.zombieHealth.IsDead) { this.deathElapsed = null; return false; }
    this.deathElapsed = (this.deathElapsed ?? 0) + Time.deltaTime;
    const duration = this.clips.get('dying')?.duration ?? 5.67;
    if (this.deathElapsed < duration + 0.25) return false;
    UnityObject.Destroy(this.gameObject);
    return true;
  }

  dispatchClipEvents(layer, clip, current) {
    if (!clip.events?.length || !(clip.duration > 0)) return;
    const key = `${layer}:${clip.name}`;
    const previous = this.eventTimes.get(key) ?? 0;
    const duration = clip.duration;
    const fire = (start, end) => {
      for (const event of clip.events) {
        if (!(event.time > start && event.time <= end)) continue;
        for (const component of this.animator.gameObject._components) {
          if (!component._active) continue;
          const method = component[event.functionName];
          if (typeof method !== 'function') continue;
          const parameter = event.stringParameter || event.intParameter || event.floatParameter;
          method.call(component, parameter);
        }
      }
    };
    if (current >= previous) {
      const startCycle = Math.floor(previous / duration);
      const endCycle = Math.floor(current / duration);
      if (startCycle === endCycle) fire(previous % duration, current % duration);
      else {
        fire(previous % duration, duration);
        for (let cycle = startCycle + 1; cycle < endCycle; cycle++) fire(0, duration);
        fire(0, current % duration);
      }
    } else fire(0, current % duration);
    this.eventTimes.set(key, current);
  }

  Update() {
    if (this.zombieAI && this.animator?.updateMode === 1) {
      if (!this.animator.enabled || !this.animator._graphOutputs
        || this.removeFinishedZombie()) return;
      const outputs = this.animator._graphOutputs;
      for (const layer of outputs) for (const entry of layer?.clips ?? [])
        this.activateRawClip(entry.name);
      this.advanceRootMotion(outputs, Time.deltaTime);
      return;
    }
    this.updatePose(true);
  }

  LateUpdate() {
    if (this.zombieAI && this.animator?.updateMode === 1) {
      this._poseSampledThisFrame = this.shouldSampleZombiePose();
      if (this._poseSampledThisFrame) this.updatePose(false);
    }
  }

  shouldSampleZombiePose() {
    const camera = this.previewCamera?.isActiveAndEnabled
      ? this.previewCamera : (this.previewCamera = Camera.main);
    if (!camera) return true;
    const offset = this.gameObject.transform.position.sub(camera.transform.position);
    const forward = offset.dot(camera.transform.forward);
    if (forward < -2) return false;
    const halfHeight = Math.max(0, forward)
      * Math.tan(THREE.MathUtils.degToRad(camera.fieldOfView) / 2) + 2;
    const aspect = camera.threeCamera?.aspect ?? 1.5;
    return Math.abs(offset.dot(camera.transform.up)) <= halfHeight
      && Math.abs(offset.dot(camera.transform.right)) <= halfHeight * aspect;
  }

  updatePose(includeMotion) {
    if (!this.animator.enabled || !this.animator._graphOutputs) return;
    if (includeMotion && this.removeFinishedZombie()) return;
    const mesh = this.body.threeMesh;
    const outputs = this.animator._graphOutputs;
    for (const layer of outputs) for (const entry of layer?.clips ?? [])
      this.activateRawClip(entry.name);
    const graph = this.animator.controllerGraph;
    const rootRotation = mesh.getWorldQuaternion(new THREE.Quaternion());
    const dt = Time.deltaTime;
    const deathEntries = (outputs[0]?.clips ?? []).filter(entry =>
      /^(dying|death)$/i.test(entry.name));
    if (this.unboundHipRoots.length && (deathEntries.length || this.unboundDeathPoseApplied)) {
      let hipsOffset = new THREE.Vector3();
      let total = 0;
      for (const entry of deathEntries) {
        const clip = this.clips.get(entry.name.toLowerCase());
        const source = clip?.bonesByIndex.get(0);
        const time = entry.time ?? (this.animator._controllerRuntime.players[0]?.elapsed ?? 0)
          * (entry.timeScale ?? 1);
        const position = samplePosition(clip, source, time);
        if (!position || !(entry.weight > 0)) continue;
        const rest = new THREE.Vector3().fromArray(source.restPosition);
        const scale = Math.abs(rest.y) > 1e-5
          ? this.targetHipsHeight / rest.y : 1;
        hipsOffset.addScaledVector(position.sub(rest), entry.weight * scale);
        total += entry.weight;
      }
      if (total > 1) hipsOffset.divideScalar(total);
      const worldOffset = hipsOffset.applyQuaternion(
        this.animator.gameObject.object3D.getWorldQuaternion(new THREE.Quaternion()));
      for (const { bone, restPosition } of this.unboundHipRoots) {
        bone.position.copy(restPosition);
        bone.updateWorldMatrix(true, false);
        const target = bone.getWorldPosition(new THREE.Vector3()).add(worldOffset);
        bone.position.copy(bone.parent.worldToLocal(target));
      }
      this.unboundDeathPoseApplied = deathEntries.length > 0;
    }
    for (const binding of this.bindings) {
      let desired = binding.rest.clone();
      const desiredPosition = binding.index === 0 ? binding.restPosition.clone() : null;
      for (let layer = 0; layer < outputs.length; layer++) {
        if (!included(graph.layers[layer], binding.index)) continue;
        const layerWeight = layer === 0 ? 1 : this.animator.GetLayerWeight(layer);
        if (!(layerWeight > 0)) continue;
        const entries = outputs[layer]?.clips ?? [];
        let blended = null;
        let blendedPosition = null;
        let total = 0;
        let positionTotal = 0;
        for (const entry of entries) {
          const clip = this.clips.get(entry.name.toLowerCase());
          const source = clip?.bonesByIndex.get(binding.index);
          if (!source || !(entry.weight > 0)) continue;
          const clipTime = entry.time
            ?? (this.animator._controllerRuntime.players[layer]?.elapsed ?? 0)
              * (entry.timeScale ?? 1);
          const pose = sampleRotation(clip, source, clipTime);
          if (!pose) continue;
          // Transfer motion in the source joint's rest frame. The sampled JU
          // avatar and the target avatar can have very different arm axes.
          let target;
          if ((clip.poseSpace ?? this.poseSpace) === 'actor') {
            const targetAvatarPose = clip.targetAvatarId
              && clip.targetAvatarId === this.targetCalibration?.avatarId;
            const parentBinding = !targetAvatarPose && binding.retargetLocal ? binding.poseParent
              : binding.humanoidParent;
            const sampledParent = clip.bonesByIndex.get(parentBinding?.index);
            const parentPose = sampledParent && sampleRotation(clip, sampledParent, clipTime);
            // The exported clip rotations are actor-space snapshots. Unity's
            // masked layers apply the joint motion relative to the animated
            // parent, including torso motion from layers outside an arm mask.
            const parentWorld = parentPose
              ? parentBinding.bone.getWorldQuaternion(new THREE.Quaternion())
              : this.animator.gameObject.object3D.getWorldQuaternion(new THREE.Quaternion());
            target = rootRotation.clone().invert().multiply(parentWorld);
            if (!targetAvatarPose && binding.retargetLocal)
              target.multiply(binding.retargetLocal);
            if (parentPose) target.multiply(parentPose.invert());
            target.multiply(pose);
          } else target = binding.rest.clone()
            .multiply(binding.referenceInverse ?? source.restInverse).multiply(pose);
          const next = total + entry.weight;
          blended = blended ? blended.slerp(target, entry.weight / next) : target;
          total = next;
          if (desiredPosition) {
            const position = samplePosition(clip, source, clipTime);
            if (position) {
              if ((clip.poseSpace ?? this.poseSpace) === 'actor') {
                this.animator.gameObject.object3D.updateWorldMatrix(true, false);
                binding.bone.parent.updateWorldMatrix(true, false);
                // Raw Humanoid hips positions use the source Avatar's actor
                // scale. The scene may instantiate that Avatar at another
                // scale (the Shooter player uses an Armature scale of 0.5).
                // Match the target's authored rest height before mapping the
                // actor-space position into the imported skeleton.
                const sourceHeight = source.restPosition?.[1];
                if (Math.abs(sourceHeight ?? 0) > 1e-5 && this.targetHipsHeight > 0)
                  position.multiplyScalar(this.targetHipsHeight / sourceHeight);
                position.applyMatrix4(this.animator.gameObject.object3D.matrixWorld);
                binding.bone.parent.worldToLocal(position);
              } else position.sub(new THREE.Vector3().fromArray(source.restPosition))
                .add(binding.restPosition);
              const nextPosition = positionTotal + entry.weight;
              blendedPosition = blendedPosition
                ? blendedPosition.lerp(position, entry.weight / nextPosition) : position;
              positionTotal = nextPosition;
            }
          }
        }
        if (blended) desired.slerp(blended, Math.min(1, layerWeight * total));
        if (blendedPosition) desiredPosition.lerp(blendedPosition,
          Math.min(1, layerWeight * positionTotal));
      }
      binding.bone.parent.updateWorldMatrix(true, false);
      const parent = binding.bone.parent.getWorldQuaternion(new THREE.Quaternion()).invert();
      binding.bone.quaternion.copy(parent.multiply(rootRotation).multiply(desired));
      if (desiredPosition) binding.bone.position.copy(desiredPosition);
      binding.bone.updateWorldMatrix(false, false);
    }
    const driving = this.gameObject._components.find(component =>
      component.constructor.name === 'DriveVehicles' && component.IsDriving
      && !component.DisableCharacterOnEnter);
    const targets = driving?.CurrentVehicleCharacterIK?.InverseKinematicTargetPositions;
    const drivingHips = this.byIndex.get(0);
    if (drivingHips && targets) {
      // Unity's Humanoid vehicle IK can lower the body to put all four targets
      // within limb reach. The browser's two-bone solver needs that shared
      // body adjustment before it solves individual hands and feet.
      let lowerBodyBy = 0;
      for (const [upperIndex, lowerIndex, endIndex, targetName] of [
        [13, 15, 17, 'LeftHandPositionIK'], [14, 16, 18, 'RightHandPositionIK'],
        [1, 3, 5, 'LeftFootPositionIK'], [2, 4, 6, 'RightFootPositionIK']
      ]) {
        const upper = this.byIndex.get(upperIndex);
        const lower = this.byIndex.get(lowerIndex);
        const end = this.byIndex.get(endIndex);
        const target = targets[targetName];
        if (!upper || !lower || !end || !target) continue;
        const shoulder = upper.getWorldPosition(new THREE.Vector3());
        const elbow = lower.getWorldPosition(new THREE.Vector3());
        const extremity = end.getWorldPosition(new THREE.Vector3());
        const goal = target.position;
        const reach = shoulder.distanceTo(elbow) + elbow.distanceTo(extremity) - 0.005;
        const horizontalSq = (shoulder.x - goal.x) ** 2 + (shoulder.z - goal.z) ** 2;
        if (horizontalSq >= reach ** 2) continue;
        lowerBodyBy = Math.max(lowerBodyBy, shoulder.y - goal.y
          - Math.sqrt(reach ** 2 - horizontalSq));
      }
      if (lowerBodyBy > 0.001) {
        drivingHips.parent.updateWorldMatrix(true, false);
        const lowered = drivingHips.getWorldPosition(new THREE.Vector3());
        lowered.y -= Math.min(lowerBodyBy, 0.5);
        drivingHips.position.copy(drivingHips.parent.worldToLocal(lowered));
        drivingHips.updateWorldMatrix(false, true);
      }
    }
    mesh.updateWorldMatrix(true, true);
    this.animator.SyncImportedSkeletonPose();
    this.animator._ik.clear();
    this.updateAnimatedFootGoals(outputs);
    this.updateAnimatedBodyPosition(outputs);
    const footFrame = (goal, index) => {
      const animated = this.animator._ikAnimationRotations.get(goal);
      const foot = this.byIndex.get(index);
      if (!animated || !foot) return null;
      const binding = this.importedBindings.get(foot);
      return animated.clone().invert().multiply(
        foot.getWorldQuaternion(new THREE.Quaternion()))
        .multiply(binding?.offsetRotation ?? new THREE.Quaternion());
    };
    const leftFootFrame = footFrame(0, 5);
    const rightFootFrame = footFrame(1, 6);
    this.animator._bodyPositionOverride = null;
    // A Fixed Animator's IK normally runs with a fixed timestep. Zombie poses
    // are sampled once per render frame, so preserve that timestep for the
    // callback even when rendering is slower than physics.
    const runtime = this.gameObject._runtime;
    const fixedIK = this.animator.updateMode === 1 && !Time.inFixedTimeStep && runtime;
    const previousDelta = runtime?.time.deltaTime;
    if (fixedIK) runtime.time.deltaTime = runtime.fixedDeltaTime;
    try {
      for (const component of this.animator.gameObject._components)
        if (component._active && typeof component.OnAnimatorIK === 'function') component.OnAnimatorIK(0);
    } finally {
      if (fixedIK) runtime.time.deltaTime = previousDelta;
    }
    const hips = this.byIndex.get(0);
    if (hips && (this.animator._bodyPositionOverride
      || (this.smoothAIBodyOffset && this.animator.updateMode === 1
        && Math.abs(this._renderedBodyOffsetY ?? 0) > 1e-6))) {
      const value = this.animator._bodyPositionOverride;
      const offset = value && this.animator._animatedBodyPosition
        ? new THREE.Vector3(value.x, value.y, value.z).sub(this.animator._animatedBodyPosition)
        : null;
      hips.parent.updateWorldMatrix(true, false);
      if (this.smoothAIBodyOffset && this.animator.updateMode === 1
        && this.animator._animatedBodyPosition) {
        // Foot placement can lose a cast for one rendered frame. Keep the
        // visible AI body offset continuous while Fixed Animator IK catches up.
        const previous = this._renderedBodyOffsetY ?? 0;
        if (offset) {
          this._lastValidBodyOffsetTime = runtime.time.time;
          this._lastValidBodyOffsetRootY = this.gameObject.transform.position.y;
        }
        const holdGroundedAIOffset = !offset && this.aiBodyController?.IsGrounded
          && runtime.time.time - (this._lastValidBodyOffsetTime ?? -Infinity) < 0.6
          && Math.abs(this.gameObject.transform.position.y
            - (this._lastValidBodyOffsetRootY ?? Infinity)) < 0.06;
        const desiredY = offset?.y ?? (holdGroundedAIOffset ? previous : 0);
        this._renderedBodyOffsetY = previous + THREE.MathUtils.clamp(
          desiredY - previous, -0.06, 0.06);
      }
      const target = this.smoothAIBodyOffset && this.animator.updateMode === 1
        && this.animator._animatedBodyPosition
        ? hips.getWorldPosition(new THREE.Vector3()).add(new THREE.Vector3(
          offset?.x ?? 0, this._renderedBodyOffsetY, offset?.z ?? 0))
        : offset ? hips.getWorldPosition(new THREE.Vector3()).add(offset)
          : new THREE.Vector3(value.x, value.y, value.z);
      hips.position.copy(hips.parent.worldToLocal(target));
    }
    solveHandIK(this.byIndex.get(18), this.byIndex.get(14), this.byIndex.get(16),
      this.animator._ik.get(3), this.importedBindings.get(this.byIndex.get(18)),
      this.ikHandFrames.right);
    // JU parents the opposite-hand grip to its right-hand weapon. Sampling the
    // left goal before the right-hand IK moves that weapon leaves a stale world
    // position, sometimes far outside the left arm's reach.
    const juCharacter = this.juMannequinGrip && this.gameObject._components.find(
      component => component.constructor.name === 'JUCharacterController');
    const leftGoal = this.animator._ik.get(2);
    const grip = juCharacter?.IsAiming && !juCharacter.HoldableItemInUseLeftHand
      ? juCharacter.HoldableItemInUseRightHand?.OppositeHandPosition : null;
    if (grip && leftGoal?.positionWeight > 0) {
      const handBinding = this.importedBindings.get(this.byIndex.get(18));
      if (handBinding) {
        const { bone, transform, offsetPosition, offsetRotation } = handBinding;
        bone.updateWorldMatrix(true, false);
        transform.position = bone.localToWorld(offsetPosition.clone());
        transform.rotation = bone.getWorldQuaternion(new THREE.Quaternion())
          .multiply(offsetRotation);
        handBinding.syncedPosition.copy(transform.position);
        handBinding.syncedRotation.copy(transform.rotation);
      }
      leftGoal.position = grip.position;
      leftGoal.rotation = grip.rotation;
    }
    solveHandIK(this.byIndex.get(17), this.byIndex.get(13), this.byIndex.get(15),
      leftGoal, this.importedBindings.get(this.byIndex.get(17)),
      this.ikHandFrames.left);
    solveHandIK(this.byIndex.get(5), this.byIndex.get(1), this.byIndex.get(3),
      this.animator._ik.get(0), this.importedBindings.get(this.byIndex.get(5)),
      leftFootFrame);
    solveHandIK(this.byIndex.get(6), this.byIndex.get(2), this.byIndex.get(4),
      this.animator._ik.get(1), this.importedBindings.get(this.byIndex.get(6)),
      rightFootFrame);
    mesh.updateWorldMatrix(true, true);
    this.animator.SyncImportedSkeletonPose();
    if (includeMotion) this.advanceRootMotion(outputs, dt);
  }

  advanceRootMotion(outputs, dt) {
    const rootEntries = [];
    for (const entry of outputs[0]?.clips ?? []) {
      const clip = this.clips.get(entry.name.toLowerCase());
      if (!clip) continue;
      const toTime = entry.time ?? (this.animator._controllerRuntime.players[0]?.elapsed ?? 0)
        * (entry.timeScale ?? 1);
      rootEntries.push({ clip, fromTime: entry.previousTime
        ?? Math.max(0, toTime - dt * (entry.timeScale ?? 1)),
        toTime, weight: entry.weight });
      this.dispatchClipEvents(0, clip, toTime);
    }
    applyAnimatorRootMotion(this.animator, rootEntries);
  }
}

export function installPreviewControllerAnimation(roots, data, rawData = null,
  deferReconcile = false) {
  if (data?.format !== 'plyground.preview.humanoid-controllers/v1')
    throw new Error('Invalid Preview humanoid controller data');
  let count = 0;
  for (const root of roots) {
    if (root.GetComponent(PreviewControllerAnimation)) continue;
    const animator = root.GetComponentInChildren(Animator, true);
    if (!animator?.controllerGraph?.layers?.length) continue;
    const matchesRig = controller => !controller.rigBones || Object.entries(controller.rigBones)
      .every(([index, name]) => animator.humanBones?.[index]?.gameObject?.name === name);
    const compatible = data.controllers.filter(matchesRig);
    const record = compatible.find(controller =>
      controller.assetId === animator.controllerAssetId)
      ?? (!animator.controllerAssetId && compatible.length === 1 ? compatible[0] : null);
    if (!record) {
      console.warn(`Preview Humanoid controller data missing for ${root.name}: `
        + `${animator.controllerAssetId ?? 'controller ID unavailable'}.`);
      continue;
    }
    const body = findBody(root, animator);
    if (!body) continue;
    root.AddComponent(PreviewControllerAnimation, { deferReconcile }).configure(animator, body,
      record, rawData);
    count++;
  }
  return count;
}
