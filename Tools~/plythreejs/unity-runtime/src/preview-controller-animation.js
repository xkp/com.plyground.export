import { Animator, MonoBehaviour, Object as UnityObject, SkinnedMeshRenderer, THREE, Time,
  applyAnimatorRootMotion, hasMovingRoot } from './index.js';

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

function sampleRotation(clip, record, time) {
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
  return a.slerp(b, span > 0 ? THREE.MathUtils.clamp((t - clip.times[low]) / span, 0, 1) : 0);
}

function samplePosition(clip, record, time) {
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
  return a.lerp(b, span > 0 ? THREE.MathUtils.clamp((t - clip.times[low]) / span, 0, 1) : 0);
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
      if (state.hintWeight > 0 && state.hintPosition) {
        const hint = new THREE.Vector3(state.hintPosition.x,
          state.hintPosition.y, state.hintPosition.z).sub(shoulder);
        pole.lerp(hint, THREE.MathUtils.clamp(state.hintWeight, 0, 1));
      }
      pole.addScaledVector(direction, -pole.dot(direction));
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

/** The same exported Animator graph and Humanoid clips drive every Preview actor. */
export class PreviewControllerAnimation extends MonoBehaviour {
  configure(animator, body, record) {
    this.animator = animator;
    this._animatorUpdateDriver = animator;
    this.body = body;
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
    const byName = new Map(mesh.skeleton.bones.map(bone => [bone.name, bone]));
    this.bindings = Object.entries(animator.humanBones ?? {}).map(([key, transform]) => ({
      index: Number(key), bone: byName.get(transform?.gameObject?.name)
    })).filter(entry => entry.bone && this.clips.size);
    // Some Unity avatars map a humanoid slot to a rig control rather than a
    // skinned joint. Animator evaluation restores these controls each frame
    // before OnAnimatorIK/LateUpdate can add procedural motion.
    this.unboundHumanoidControls = [...new Set(Object.values(animator.humanBones ?? {})
      .filter(transform => transform && !byName.has(transform.gameObject?.name)))]
      .map(transform => ({ transform, position: transform.localPosition,
        rotation: transform.localRotation, scale: transform.localScale }));
    restoreHumanoidFingerRoots(this.bindings);
    mesh.updateWorldMatrix(true, true);
    const rootInverse = mesh.getWorldQuaternion(new THREE.Quaternion()).invert();
    this.bindings.forEach(binding => {
      binding.rest = rootInverse.clone().multiply(binding.bone.getWorldQuaternion(new THREE.Quaternion()));
      binding.restPosition = binding.bone.position.clone();
      let depth = 0;
      for (let node = binding.bone.parent; node; node = node.parent) depth++;
      binding.depth = depth;
    });
    const neutral = [...this.clips.values()].find(clip => /\bneutral idle\b/i.test(clip.name))
      ?? [...this.clips.values()].find(clip => /\bidle\b/i.test(clip.name));
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
      if (!this._active || !this.animator._active) return;
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
    if (!this.animator.enabled || !this.animator._graphOutputs) return;
    if (this.removeFinishedZombie()) return;
    const mesh = this.body.threeMesh;
    const outputs = this.animator._graphOutputs;
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
          if (this.poseSpace === 'actor') {
            const sampledParent = clip.bonesByIndex.get(binding.humanoidParent?.index);
            const parentPose = sampledParent && sampleRotation(clip, sampledParent, clipTime);
            // The exported clip rotations are actor-space snapshots. Unity's
            // masked layers apply the joint motion relative to the animated
            // parent, including torso motion from layers outside an arm mask.
            const parentWorld = parentPose
              ? binding.humanoidParent.bone.getWorldQuaternion(new THREE.Quaternion())
              : this.animator.gameObject.object3D.getWorldQuaternion(new THREE.Quaternion());
            target = rootRotation.clone().invert().multiply(parentWorld);
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
              if (this.poseSpace === 'actor') {
                this.animator.gameObject.object3D.updateWorldMatrix(true, false);
                binding.bone.parent.updateWorldMatrix(true, false);
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
    mesh.updateWorldMatrix(true, true);
    this.animator.SyncImportedSkeletonPose();
    this.animator._ik.clear();
    this.animator._bodyPositionOverride = null;
    for (const component of this.animator.gameObject._components)
      if (component._active && typeof component.OnAnimatorIK === 'function') component.OnAnimatorIK(0);
    const hips = this.byIndex.get(0);
    if (hips && this.animator._bodyPositionOverride) {
      const value = this.animator._bodyPositionOverride;
      hips.parent.updateWorldMatrix(true, false);
      hips.position.copy(hips.parent.worldToLocal(new THREE.Vector3(value.x, value.y, value.z)));
    }
    solveHandIK(this.byIndex.get(17), this.byIndex.get(13), this.byIndex.get(15),
      this.animator._ik.get(2), this.importedBindings.get(this.byIndex.get(17)),
      this.ikHandFrames.left);
    solveHandIK(this.byIndex.get(18), this.byIndex.get(14), this.byIndex.get(16),
      this.animator._ik.get(3), this.importedBindings.get(this.byIndex.get(18)),
      this.ikHandFrames.right);
    mesh.updateWorldMatrix(true, true);
    this.animator.SyncImportedSkeletonPose();
    const rootEntries = [];
    for (const entry of outputs[0]?.clips ?? []) {
      const clip = this.clips.get(entry.name.toLowerCase());
      if (!clip) continue;
      const toTime = entry.time ?? (this.animator._controllerRuntime.players[0]?.elapsed ?? 0)
        * (entry.timeScale ?? 1);
      rootEntries.push({ clip, fromTime: Math.max(0, toTime - dt * (entry.timeScale ?? 1)),
        toTime, weight: entry.weight });
      this.dispatchClipEvents(0, clip, toTime);
    }
    applyAnimatorRootMotion(this.animator, rootEntries);
  }
}

export function installPreviewControllerAnimation(roots, data) {
  if (data?.format !== 'plyground.preview.humanoid-controllers/v1')
    throw new Error('Invalid Preview humanoid controller data');
  let count = 0;
  for (const root of roots) {
    const animator = root.GetComponentInChildren(Animator, true);
    if (!animator?.controllerGraph?.layers?.length) continue;
    const matchesRig = controller => !controller.rigBones || Object.entries(controller.rigBones)
      .every(([index, name]) => animator.humanBones?.[index]?.gameObject?.name === name);
    const record = data.controllers.find(controller => controller.assetId === animator.controllerAssetId
      && matchesRig(controller))
      ?? data.controllers.find(controller => matchesRig(controller) && controller.clips.some(clip =>
        animator.controllerGraph.layers.some(layer => layer.states.some(state =>
          state.motion?.name === clip.name))));
    if (!record) continue;
    const body = findBody(root, animator);
    if (!body) continue;
    root.AddComponent(PreviewControllerAnimation).configure(animator, body, record);
    count++;
  }
  return count;
}
