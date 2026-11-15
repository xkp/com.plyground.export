import { Animator, MonoBehaviour, Physics, SkinnedMeshRenderer, THREE, Time } from './index.js';
import { loadFbxAnimation } from './source-animation.js';
import { retargetPreviewClip } from './preview-rig-retarget.js';

/** Follow the body rig while preserving each garment's own inverse bind pose. */
export function bindPreviewClothingToBody(root) {
  const skins = [];
  const visit = object => {
    skins.push(...object.GetComponents(SkinnedMeshRenderer));
    for (const child of object.children) visit(child);
  };
  visit(root);
  const body = skins.find(renderer => renderer.gameObject.name === 'Body'
    && renderer.bones.length > 0);
  if (!body) return 0;
  const bodyBones = new Map(body.threeMesh.skeleton.bones.map(bone => [bone.name, bone]));
  let count = 0;
  for (const skin of skins) {
    if (skin === body || !skin.threeMesh?.isSkinnedMesh) continue;
    const matched = skin.threeMesh.skeleton.bones.map(bone => ({ bone, driver: bodyBones.get(bone.name) }));
    if (matched.some(({ driver }) => !driver)) continue;
    skin._previewBoneBindings = matched.map(({ bone, driver }) => {
      bone.updateWorldMatrix(true, false);
      driver.updateWorldMatrix(true, false);
      return { bone, driver, bindOffset: driver.matrixWorld.clone().invert()
        .multiply(bone.matrixWorld) };
    });
    const depth = bone => { let count = 0; for (let parent = bone.parent; parent; parent = parent.parent) count++; return count; };
    skin._previewBoneBindings.sort((a, b) => depth(a.bone) - depth(b.bone));
    count++;
  }
  return count;
}

/** Reconnect finger roots that the GLB importer left at the skeleton root. */
export function restoreDetachedFingerRoots(bones) {
  for (const bone of bones) {
    const side = bone.name.match(/ArmDigit_([LR])_2x1_/)?.[1];
    if (!side || bone.parent?.isBone) continue;
    const palm = bones.find(candidate => candidate.name.includes(`ArmPalm_${side}_O_`));
    if (palm) palm.attach(bone);
  }
}

/** Move a wrist toward its grip target by rotating the arm joints. */
export function solvePreviewArmIK(hand, target, weight = 1) {
  if (!hand || !target || weight <= 0) return;
  const side = hand.name.includes('ArmPalm_L_') ? 'L' : 'R';
  const joints = [];
  for (let bone = hand.parent; bone?.isBone; bone = bone.parent) {
    if (bone.name.includes(`ArmForeArm_${side}_1_`)
      || bone.name.includes(`ArmUpperArm_${side}_1_`)) joints.push(bone);
  }
  if (joints.length !== 2) return;
  const wrist = new THREE.Vector3();
  const pivot = new THREE.Vector3();
  const toWrist = new THREE.Vector3();
  const toTarget = new THREE.Vector3();
  const parentRotation = new THREE.Quaternion();
  const correction = new THREE.Quaternion();
  for (let iteration = 0; iteration < 5; iteration++) {
    for (const joint of joints) {
      hand.getWorldPosition(wrist);
      joint.getWorldPosition(pivot);
      toWrist.subVectors(wrist, pivot);
      toTarget.subVectors(target.position, pivot);
      if (toWrist.lengthSq() < 1e-8 || toTarget.lengthSq() < 1e-8) continue;
      correction.setFromUnitVectors(toWrist.normalize(), toTarget.normalize());
      // Keep each step small so the sampled elbow bend remains the starting
      // solution and the arm does not flip when the grip moves across it.
      correction.slerp(new THREE.Quaternion(), 1 - Math.min(weight, 1) * 0.5);
      joint.parent.getWorldQuaternion(parentRotation).invert();
      joint.quaternion.premultiply(parentRotation.clone().multiply(correction)
        .multiply(parentRotation.invert()));
      joint.updateWorldMatrix(true, true);
    }
  }
  if (target.rotation) {
    const parentWorld = hand.parent.getWorldQuaternion(new THREE.Quaternion());
    const localTarget = parentWorld.invert().multiply(target.rotation);
    hand.quaternion.slerp(localTarget, Math.min(weight, 1));
    hand.updateWorldMatrix(true, true);
  }
}

// Unity HumanBodyBones indices. The sampled JU clips use Mixamo's names.
const humanoidBones = {
  LeftUpLeg: 1, RightUpLeg: 2, LeftLeg: 3, RightLeg: 4,
  LeftFoot: 5, RightFoot: 6, Spine: 7, Spine1: 8, Neck: 9, Head: 10,
  LeftShoulder: 11, RightShoulder: 12, LeftArm: 13, RightArm: 14,
  LeftForeArm: 15, RightForeArm: 16, LeftHand: 17, RightHand: 18,
  LeftToeBase: 19, RightToeBase: 20,
};

// The JU samples are from a different humanoid avatar. Its limb swing is
// useful here, but copying upper-body rotations into RedMono's control rig
// pulls the shoulders and hands apart.
const compatibleBones = new Set(['LeftUpLeg', 'RightUpLeg', 'LeftLeg', 'RightLeg',
  'LeftFoot', 'RightFoot']);

function sampleRotation(clip, bone, time, out) {
  const times = clip.times;
  const t = time % clip.duration;
  let right = times.findIndex(value => value > t);
  if (right < 0) right = times.length - 1;
  const left = Math.max(0, right - 1);
  const values = bone.rotations;
  const start = new THREE.Quaternion().fromArray(values, left * 4);
  const end = new THREE.Quaternion().fromArray(values, right * 4);
  out.copy(start).slerp(end, right === left ? 0
    : THREE.MathUtils.clamp((t - times[left]) / (times[right] - times[left]), 0, 1));
  return out;
}

class PreviewHumanoidAnimation extends MonoBehaviour {
  configure(controller, animator, clips) {
    this.controller = controller;
    this.clips = clips;
    this.time = 0;
    this.blend = 0;
    this.bindings = [];
    for (const [name, index] of Object.entries(humanoidBones)) {
      if (!compatibleBones.has(name)) continue;
      const target = animator.humanBones?.[index];
      const idle = clips.idle.bones.find(bone => bone.name === name);
      const walk = clips.walk.bones.find(bone => bone.name === name);
      const run = clips.run.bones.find(bone => bone.name === name);
      if (!target || !idle || !walk || !run) continue;
      this.bindings.push({ target, base: target.localRotation,
        idle, walk, run,
        idleReference: new THREE.Quaternion().fromArray(idle.rotations).invert(),
        walkReference: new THREE.Quaternion().fromArray(walk.rotations).invert(),
        runReference: new THREE.Quaternion().fromArray(run.rotations).invert() });
    }
  }
  LateUpdate() {
    if (!this.bindings?.length) return;
    const moving = this.controller.IsMoving && this.controller.IsGrounded;
    const amount = 1 - Math.exp(-10 * Time.deltaTime);
    this.blend = THREE.MathUtils.lerp(this.blend, moving ? 1 : 0, amount);
    this.time += Time.deltaTime;
    const motion = this.controller.IsRunning ? this.clips.run : this.clips.walk;
    const motionBone = this.controller.IsRunning ? 'run' : 'walk';
    const motionReference = this.controller.IsRunning ? 'runReference' : 'walkReference';
    const idlePose = new THREE.Quaternion();
    const movePose = new THREE.Quaternion();
    for (const binding of this.bindings) {
      sampleRotation(this.clips.idle, binding.idle, this.time, idlePose)
        .premultiply(binding.idleReference);
      sampleRotation(motion, binding[motionBone], this.time, movePose)
        .premultiply(binding[motionReference]);
      binding.target.localRotation = binding.base.clone()
        .multiply(idlePose.slerp(movePose, this.blend));
    }
  }
}

class PreviewRetargetedAnimation extends MonoBehaviour {
  configure(controller, animator, body, clips) {
    this.controller = controller;
    this.animator = animator;
    this.body = body;
    this.mixer = new THREE.AnimationMixer(body.threeMesh);
    this.actions = Object.fromEntries(Object.entries(clips).map(([name, clip]) =>
      [name, this.mixer.clipAction(clip)]));
    this.current = 'idle';
    this.actions.idle.play();
    body.useExternalAnimation = true;
    animator.BindImportedSkeleton(body.threeMesh.skeleton);
  }
  Update() {
    const next = this.controller.IsMoving && this.controller.IsGrounded
      ? this.controller.IsRunning ? 'run' : 'walk' : 'idle';
    if (next !== this.current) {
      this.actions[next].reset().play().crossFadeFrom(this.actions[this.current], 0.2, false);
      this.current = next;
    }
    this.mixer.update(Time.deltaTime);
    this.animator.SyncImportedSkeletonPose();
  }
}

const isArmBone = name => /ArmShoulder|ArmUpperArm|ArmForeArm|ArmPalm|ArmDigit|Finger|Thumb/.test(name);
const isRightArmBone = name => /_R_|Right/.test(name) && isArmBone(name);

/** Play poses sampled by Unity on the target avatar, including JU's pistol layers. */
class PreviewUnitySampledAnimation extends MonoBehaviour {
  configure(controller, animator, body, data, { zombie = false, ai = null, healthType = null } = {}) {
    this.controller = controller;
    this.animator = animator;
    this.body = body;
    this.zombie = zombie;
    this.ai = ai;
    this.healthType = healthType;
    this.attackTime = 0;
    this.attackHit = false;
    this.reloadTime = null;
    this.reloadApplied = false;
    this.punchTime = null;
    this.punchApplied = false;
    this.clips = Object.fromEntries(data.clips.map(clip => [clip.name, {
      duration: clip.duration,
      frames: clip.frames,
      bones: clip.frames.map(frame => new Map(frame.bones.map(bone => [bone.name, bone]))),
    }]));
    this.bones = body.threeMesh.skeleton.bones.filter(bone => data.rest.some(rest => rest.name === bone.name));
    restoreDetachedFingerRoots(this.bones);
    const byName = new Map(this.bones.map(bone => [bone.name, bone]));
    const restByName = new Map(data.rest.map(rest => [rest.name, rest]));
    this.bindPose = new Map(this.bones.map(bone => {
      const rest = restByName.get(bone.name);
      return [bone.name, {
        positionOffset: bone.position.clone().sub(new THREE.Vector3().fromArray(rest.position)),
        rotationBase: bone.quaternion.clone(),
        rotationRestInverse: new THREE.Quaternion().fromArray(rest.rotation).invert(),
      }];
    }));
    this.hands = {
      right: byName.get(animator.humanBones?.[18]?.gameObject?.name),
      left: byName.get(animator.humanBones?.[17]?.gameObject?.name),
    };
    const depth = bone => { let value = 0; for (let node = bone.parent; node; node = node.parent) value++; return value; };
    this.bones.sort((a, b) => depth(a) - depth(b));
    this.state = 'freeIdle';
    this.time = 0;
    this.airTime = 0;
    this.overlay = 0;
    this.shotRecoil = 0;
    this.lastBullets = controller.RightHandWeapon?.BulletsAmounts ?? null;
    body.useExternalAnimation = true;
    animator.BindImportedSkeleton(body.threeMesh.skeleton);
  }
  sample(clip, boneName, time, out) {
    if (!clip) return null;
    const frames = clip.frames;
    const t = Math.min(time, clip.duration);
    let right = frames.findIndex(frame => frame.time >= t);
    if (right < 0) right = frames.length - 1;
    const left = Math.max(0, right - 1);
    const a = clip.bones[left].get(boneName)?.rotation;
    const b = clip.bones[right].get(boneName)?.rotation;
    if (!a || !b) return null;
    const span = frames[right].time - frames[left].time;
    return out.fromArray(a).slerp(new THREE.Quaternion().fromArray(b),
      span > 0 ? THREE.MathUtils.clamp((t - frames[left].time) / span, 0, 1) : 0);
  }
  samplePosition(clip, boneName, time, out) {
    if (!clip) return null;
    const frames = clip.frames;
    const t = Math.min(time, clip.duration);
    let right = frames.findIndex(frame => frame.time >= t);
    if (right < 0) right = frames.length - 1;
    const left = Math.max(0, right - 1);
    const a = clip.bones[left].get(boneName)?.position;
    const b = clip.bones[right].get(boneName)?.position;
    if (!a || !b) return null;
    const span = frames[right].time - frames[left].time;
    return out.fromArray(a).lerp(new THREE.Vector3().fromArray(b),
      span > 0 ? THREE.MathUtils.clamp((t - frames[left].time) / span, 0, 1) : 0);
  }
  applyHandIK(bone, target, weight) {
    solvePreviewArmIK(bone, target, weight);
  }
  Update() {
    const dt = Time.deltaTime;
    const weapon = this.controller.RightHandWeapon;
    const hasWeapon = Boolean(weapon);
    const pistol = /P226|pistol/i.test(weapon?.ItemName ?? '');
    const fireMode = hasWeapon && (this.controller.FiringMode || this.controller.IsAiming);
    const dead = this.controller.IsDead || this.controller.CharacterHealth?.IsDead;
    const airborne = !dead && !this.controller.IsGrounded;
    this.airTime = airborne ? this.airTime + dt : 0;
    if (!this.zombie && !dead && this.controller.IsReloading && this.reloadTime === null) {
      this.reloadTime = 0;
      this.reloadApplied = false;
      this.reloadClipName = pistol ? 'reloadPistol' : 'reloadRifle';
    }
    const reloadClip = this.clips[this.reloadClipName];
    if (this.reloadTime !== null) {
      this.reloadTime += dt;
      // JU's reload state runs at 1.5x speed and calls its completion event
      // at normalized time 0.7. The browser runtime has no Animator state
      // machine events, so reproduce that event here.
      const duration = (reloadClip?.duration ?? 2.4) / 1.5;
      if (!this.reloadApplied && this.reloadTime >= duration * 0.7) {
        this.reloadApplied = true;
        this.controller.reloadRightHandWeapon();
      }
      if (this.reloadTime >= duration) this.reloadTime = null;
    }
    const reloading = this.reloadTime !== null && Boolean(reloadClip);
    const controllerState = this.animator._controllerRuntime
      ? this.animator._stateByLayer.get(0) : null;
    const controllerPunching = /^Punch Attack/.test(controllerState ?? '');
    if (!this.zombie && !dead && !hasWeapon
      && (this.animator._controllerRuntime ? controllerPunching : this.controller.IsPunching)
      && this.punchTime === null && this.clips.punchRight) {
      this.punchTime = 0;
      this.punchApplied = false;
    }
    const punchClip = this.clips[controllerState === 'Punch Attack 2' ? 'punchLeft' : 'punchRight'];
    if (this.punchTime !== null) {
      this.punchTime += dt;
      if (this.animator._controllerRuntime ? !controllerPunching
        : this.punchTime >= (punchClip?.duration ?? 1) / 1.5) {
        this.punchTime = null;
        if (!this.animator._controllerRuntime) {
          this.controller.IsPunching = false;
          this.animator.ResetTrigger(this.controller.AnimatorParameters.Punch);
        }
      }
    }
    const punching = this.punchTime !== null && Boolean(punchClip);
    const attacking = this.zombie && !dead && this.clips.zombiePunch
      && this.ai?.Control?.IsAttacking;
    if (attacking) {
      this.attackTime += dt;
      const duration = this.clips.zombiePunch.duration;
      if (!this.attackHit && this.attackTime >= duration * 0.45) {
        this.attackHit = true;
        const target = this.ai.CurrentTarget?.gameObject;
        const health = this.healthType && target?.GetComponent(this.healthType);
        // JU normally applies a punch through Animator events and hand
        // damagers. Sampled clips have neither, so deliver one hit at impact.
        if (health && !health.IsDead)
          health.DoDamage__0(this.controller.LeftHandDamager?.Damage ?? 10);
      }
      if (this.attackTime >= duration + 0.35) {
        this.attackTime = 0;
        this.attackHit = false;
      }
    } else {
      this.attackTime = 0;
      this.attackHit = false;
    }
    const next = dead ? this.zombie && this.clips.zombieDeath ? 'zombieDeath' : 'freeIdle'
      : this.zombie && this.clips.zombieIdle
        ? attacking && this.attackTime < this.clips.zombiePunch.duration ? 'zombiePunch'
          : this.controller.IsMoving ? this.controller.IsRunning && this.clips.zombieRun ? 'zombieRun'
          : this.clips.zombieWalk ? 'zombieWalk' : 'freeWalk' : 'zombieIdle'
        : reloading ? this.reloadClipName
          : punching ? controllerState === 'Punch Attack 2' ? 'punchLeft' : 'punchRight'
          : airborne ? this.airTime < (this.clips.jump?.duration ?? 0.3) ? 'jump' : 'jumping'
          : this.controller.IsMoving ? this.controller.IsRunning ? fireMode ? 'run' : 'freeRun'
            : fireMode ? 'walk' : 'freeWalk' : fireMode ? 'idle' : 'freeIdle';
    if (next !== this.state) { this.state = next; this.time = 0; }
    else this.time += dt;
    const base = this.clips[this.state];
    const baseTime = reloading ? Math.min(this.reloadTime * 1.5, base.duration)
      : punching ? Math.min(this.punchTime * 1.5, base.duration)
      : this.state === 'zombiePunch' ? Math.min(this.attackTime, base.duration)
      : this.state === 'jump' || this.state === 'zombieDeath'
        ? Math.min(this.time, base.duration) : this.time % Math.max(base.duration, 0.001);
    const overlayTarget = dead || this.zombie || reloading || punching ? 0 : hasWeapon ? fireMode ? 1 : 0.8 : 0;
    this.overlay = reloading || punching ? 0 : this.overlay + (overlayTarget - this.overlay)
      * (1 - Math.exp(-10 * dt));
    const overlayClip = this.clips[pistol
      ? fireMode ? 'pistolTwoHands' : 'pistolOneHand'
      : fireMode ? 'rifleAim' : 'rifleHold'];
    const bullets = this.controller.RightHandWeapon?.BulletsAmounts ?? null;
    if (bullets != null && this.lastBullets != null && bullets < this.lastBullets)
      this.shotRecoil = 1;
    this.lastBullets = bullets;
    this.shotRecoil = Math.max(0, this.shotRecoil - dt * 8);
    const basePose = new THREE.Quaternion();
    const overlayPose = new THREE.Quaternion();
    const basePosition = new THREE.Vector3();
    const overlayPosition = new THREE.Vector3();
    const blend = 1 - Math.exp(-16 * dt);
    for (const bone of this.bones) {
      const target = this.sample(base, bone.name, baseTime, basePose);
      const targetPosition = this.samplePosition(base, bone.name, baseTime, basePosition);
      if (!target || !targetPosition) continue;
      if (!dead && !reloading && !punching && this.overlay > 0.001 && (fireMode || !pistol ? isArmBone(bone.name) : isRightArmBone(bone.name))) {
        const armed = this.sample(overlayClip, bone.name,
          fireMode ? this.time % overlayClip.duration : 0, overlayPose);
        const armedPosition = this.samplePosition(overlayClip, bone.name,
          fireMode ? this.time % overlayClip.duration : 0, overlayPosition);
        if (armed) target.slerp(armed, this.overlay);
        if (armedPosition) targetPosition.lerp(armedPosition, this.overlay);
      }
      if (this.shotRecoil > 0 && /ArmUpperArm_R_1/.test(bone.name))
        target.multiply(new THREE.Quaternion().setFromAxisAngle(
          new THREE.Vector3(1, 0, 0), -0.12 * this.shotRecoil));
      const bind = this.bindPose.get(bone.name);
      target.premultiply(bind.rotationRestInverse).premultiply(bind.rotationBase);
      targetPosition.add(bind.positionOffset);
      // The sampled clips and imported skin share the same bind skeleton.
      // A few root bones have different parent transforms in the exported
      // prefab, so apply each clip as a delta from Unity's sampled rest pose.
      // Local transforms keep the head, shoulders, and weapon with their parents.
      // IK rotates these two joints after sampling. Reset them to the sampled
      // pose each frame so IK corrections cannot accumulate and flip the arm.
      if (/Arm(?:UpperArm|ForeArm)_[LR]_1_/.test(bone.name))
        bone.quaternion.copy(target);
      else bone.quaternion.slerp(target, blend);
      bone.position.lerp(targetPosition, blend);
    }
    this.body.threeMesh.updateWorldMatrix(true, true);
    if (!dead && !this.zombie && !reloading && !punching) {
      this.applyHandIK(this.hands.right, this.controller.IKPositionRightHand,
        this.controller.RightHandWeightIK);
      this.applyHandIK(this.hands.left, this.controller.IKPositionLeftHand,
        this.controller.LeftHandWeightIK);
    }
    if (!this.animator._controllerRuntime && punching && this.healthType && !this.punchApplied
      && this.punchTime >= punchClip.duration * 0.293 / 1.5
      && this.punchTime <= punchClip.duration * 0.931 / 1.5) {
      // JU's MeleeAttack state enables the right hand damager over this
      // normalized interval. Sampled clips need the same contact window.
      const fist = this.hands.right?.getWorldPosition(new THREE.Vector3());
      if (fist) for (const collider of Physics.OverlapSphere(fist, 0.45)) {
        const health = collider.GetComponentInParent(this.healthType);
        if (!health || health.gameObject === this.controller.gameObject || health.IsDead) continue;
        health.DoDamage__0(this.controller.RightHandDamager?.Damage ?? 20);
        this.punchApplied = true;
        break;
      }
    }
    this.animator.SyncImportedSkeletonPose();
  }
}

/** Add sampled, in-place humanoid clips to actors with no exported Animator clips. */
export async function installPreviewHumanoidAnimation(roots, data, types, { baseUrl } = {}) {
  if (data?.format === 'plyground.preview.unity-sampled-clips/v1') {
    const controllerType = types['JUTPS.JUCharacterController'];
    if (!controllerType) return 0;
    let count = 0;
    for (const root of roots) {
      if (!root.assetId?.startsWith('item:')) continue;
      const controller = root.GetComponent(controllerType);
      const animator = root.GetComponent(Animator);
      const body = root.children.find(child => child.name === 'Body')?.GetComponent(SkinnedMeshRenderer);
      if (!controller || !animator || !body?.threeMesh?.isSkinnedMesh || animator.clips.size) continue;
      const ai = types['JU.CharacterSystem.AI.JU_AI_Zombie']
        && root.GetComponent(types['JU.CharacterSystem.AI.JU_AI_Zombie']);
      root.AddComponent(PreviewUnitySampledAnimation).configure(controller, animator, body, data,
        { zombie: Boolean(ai), ai, healthType: types['JU.JUHealth'] });
      count++;
    }
    return count;
  }
  if (data?.format === 'plyground.preview.fbx-humanoid/v1') {
    const imported = Object.fromEntries(await Promise.all(['idle', 'walk', 'run'].map(async name => {
      const path = data.clips?.[name];
      if (!path) throw new Error(`Preview humanoid animation lacks ${name}`);
      return [name, await loadFbxAnimation(new URL(path, baseUrl).href)];
    })));
    const controllerType = types['JUTPS.JUCharacterController'];
    if (!controllerType) return 0;
    let count = 0;
    for (const root of roots) {
      if (!root.assetId?.startsWith('item:')) continue;
      const controller = root.GetComponent(controllerType);
      const animator = root.GetComponent(Animator);
      const body = root.children.find(child => child.name === 'Body')?.GetComponent(SkinnedMeshRenderer);
      if (!controller || !animator || !body?.threeMesh?.isSkinnedMesh || animator.clips.size) continue;
      const clips = Object.fromEntries(Object.entries(imported).map(([name, source]) =>
        [name, retargetPreviewClip(body.threeMesh, animator, source, { name })]));
      root.AddComponent(PreviewRetargetedAnimation).configure(controller, animator, body, clips);
      count++;
    }
    return count;
  }
  if (data?.format !== 'plyground.preview.humanoid-clips/v1')
    throw new Error('Invalid Preview humanoid animation data');
  const clips = Object.fromEntries(data.clips.map(clip => [clip.name, clip]));
  if (!clips.idle || !clips.walk || !clips.run)
    throw new Error('Preview humanoid animation needs idle, walk, and run clips');
  const controllerType = types['JUTPS.JUCharacterController'];
  if (!controllerType) return 0;
  let count = 0;
  for (const root of roots) {
    if (!root.assetId?.startsWith('item:')) continue;
    const controller = root.GetComponent(controllerType);
    const animator = root.GetComponent(Animator);
    if (!controller || !animator || animator.clips.size) continue;
    const player = root.AddComponent(PreviewHumanoidAnimation);
    player.configure(controller, animator, clips);
    count++;
  }
  return count;
}
