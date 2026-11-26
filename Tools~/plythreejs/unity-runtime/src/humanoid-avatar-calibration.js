import * as THREE from 'three';

function rotationVector(from, to) {
  const delta = new THREE.Quaternion().fromArray(from).invert()
    .multiply(new THREE.Quaternion().fromArray(to)).normalize();
  if (delta.w < 0) delta.set(-delta.x, -delta.y, -delta.z, -delta.w);
  const sine = Math.hypot(delta.x, delta.y, delta.z);
  if (sine < 1e-8) return new THREE.Vector3();
  return new THREE.Vector3(delta.x, delta.y, delta.z)
    .multiplyScalar(2 * Math.atan2(sine, delta.w) / sine);
}

function quaternionFromVector(vector) {
  const angle = vector.length();
  return angle < 1e-8 ? new THREE.Quaternion()
    : new THREE.Quaternion().setFromAxisAngle(vector.multiplyScalar(1 / angle), angle);
}

function bodyToHipsRotation(calibration, bodyRotation) {
  if (!bodyRotation) return new THREE.Quaternion();
  const neutralBody = new THREE.Quaternion().fromArray(calibration.neutralBodyRotation);
  const targetBody = new THREE.Quaternion().fromArray(bodyRotation);
  const targetDelta = targetBody.multiply(neutralBody.clone().invert());
  const targetVector = rotationVector([0, 0, 0, 1], targetDelta.toArray());
  const neutralHips = calibration.neutralRigBones.find(bone => bone.index === 0);
  if (!neutralHips || calibration.bodyRotationProbes?.length !== 3)
    throw new TypeError('Avatar calibration lacks body rotation probes');
  const result = new THREE.Vector3();
  for (let axis = 0; axis < 3; axis++) {
    const probe = calibration.bodyRotationProbes[axis];
    const input = new THREE.Quaternion().fromArray(probe.inputRotation)
      .multiply(neutralBody.clone().invert());
    const inputVector = rotationVector([0, 0, 0, 1], input.toArray());
    const denominator = inputVector.getComponent(axis);
    if (Math.abs(denominator) < 1e-8)
      throw new TypeError('Avatar body rotation probe is degenerate');
    const hips = new THREE.Quaternion().fromArray(probe.hipsLocalRotation)
      .multiply(new THREE.Quaternion().fromArray(neutralHips.localRotation).invert());
    result.addScaledVector(rotationVector([0, 0, 0, 1], hips.toArray()),
      targetVector.getComponent(axis) / denominator);
  }
  return quaternionFromVector(result);
}

const zyCache = new WeakMap();
const yAxis = new THREE.Vector3(0, 1, 0);
const zAxis = new THREE.Vector3(0, 0, 1);

function zySwing(parameter, yTrait, zTrait, yValue, zValue) {
  const radians = (trait, value) => parameter.limitSign[trait.dof]
    * value * (value >= 0 ? trait.defaultMax : -trait.defaultMin)
    * Math.PI / 180;
  const y = new THREE.Quaternion().setFromAxisAngle(yAxis,
    radians(yTrait, yValue));
  const z = new THREE.Quaternion().setFromAxisAngle(zAxis,
    radians(zTrait, zValue));
  const combined = y.multiply(z);
  // Owner Unity GetZYRoll probes show a twist-free Y/Z swing: drop X and normalize.
  return new THREE.Quaternion(0, combined.y, combined.z, combined.w).normalize();
}

function zyFrames(calibration) {
  if (zyCache.has(calibration)) return zyCache.get(calibration);
  const frames = new Map();
  const parameters = new Map((calibration.boneParameters ?? [])
    .map(value => [value.index, value]));
  for (const index of parameters.keys()) {
    const neutral = calibration.neutralRigBones.find(bone => bone.index === index);
    const parameter = parameters.get(index);
    const yTrait = calibration.muscleTraits?.find(value =>
      value.bone === index && value.dof === 1);
    const zTrait = calibration.muscleTraits?.find(value =>
      value.bone === index && value.dof === 2);
    if (!neutral || !parameter || !yTrait || !zTrait) continue;
    const yNeutral = calibration.neutralMuscles[yTrait.muscle];
    const zNeutral = calibration.neutralMuscles[zTrait.muscle];
    const base = zySwing(parameter, yTrait, zTrait,
      yNeutral, zNeutral);
    const post = new THREE.Quaternion().fromArray(parameter.postRotation);
    const neutralRotation = new THREE.Quaternion().fromArray(neutral.localRotation);
    const cases = [];
    for (const trait of [zTrait, yTrait]) {
      const response = calibration.calibration[trait.muscle];
      for (const [field, valueField] of [['plusRig', 'plusValue'],
        ['minusRig', 'minusValue']]) {
        const bone = response[field].find(item => item.path === neutral.path);
        if (!bone) continue;
        cases.push({ y: trait === yTrait ? response[valueField] : yNeutral,
          z: trait === zTrait ? response[valueField] : zNeutral,
          target: new THREE.Quaternion().fromArray(bone.localRotation) });
      }
    }
    if (cases.length !== 4) continue;
    // Isolated muscle probes locate Unity's effective referential near postRotation.
    const frame = angles => post.clone().multiply(new THREE.Quaternion()
      .setFromEuler(new THREE.Euler(...angles, 'XYZ')));
    const predict = (candidate, y, z) => neutralRotation.clone()
      .multiply(candidate).multiply(base.clone().invert().multiply(
        zySwing(parameter, yTrait, zTrait, y, z)))
      .multiply(candidate.clone().invert());
    const loss = angles => cases.reduce((sum, item) => {
      const error = predict(frame(angles), item.y, item.z)
        .angleTo(item.target);
      return sum + error * error;
    }, 0);
    const correction = [0, 0, 0];
    let current = loss(correction);
    for (const step of [0.1, 0.05, 0.02, 0.005, 0.001, 0.0002])
      for (let pass = 0; pass < 20; pass++) {
        let improved = false;
        for (let axis = 0; axis < 3; axis++)
          for (const sign of [-1, 1]) {
            const trial = [...correction];
            trial[axis] += step * sign;
            const value = loss(trial);
            if (value + 1e-12 < current) {
              correction[axis] = trial[axis];
              current = value;
              improved = true;
            }
          }
        if (!improved) break;
      }
    if (Math.sqrt(current / cases.length) < Math.PI / 180 * 0.25)
      frames.set(neutral.path, { yTrait, zTrait, parameter, base,
        frame: frame(correction) });
  }
  zyCache.set(calibration, frames);
  return frames;
}

/** Experimental target-Avatar rotation decoder; never used by live Preview. */
export function decodeCalibratedAvatarRotations(calibration, muscles,
  { space = 'root', rig = false, bodyRotation = null,
    useZYSwing = true } = {}) {
  if (muscles.length !== calibration.neutralMuscles.length)
    throw new TypeError('Humanoid muscle count does not match Avatar calibration');
  if (space !== 'root' && space !== 'local')
    throw new TypeError('Avatar rotation space must be root or local');
  if (bodyRotation && space !== 'local')
    throw new TypeError('Body rotation requires local Avatar rotations');
  const key = space === 'local' ? 'localRotation' : 'rotation';
  const suffix = rig ? 'Rig' : '';
  const neutralBones = rig ? calibration.neutralRigBones : calibration.neutralBones;
  if (!Array.isArray(neutralBones))
    throw new TypeError('Avatar calibration lacks rig bones');
  const hipsAdjustment = bodyToHipsRotation(calibration, bodyRotation);
  const swingFrames = useZYSwing && rig && space === 'local'
    ? zyFrames(calibration) : new Map();
  const result = new Map();
  for (const neutral of neutralBones) {
    const id = rig ? neutral.path : neutral.index;
    if (!neutral[key]) throw new TypeError(`Avatar calibration lacks ${key}`);
    const rotation = new THREE.Quaternion().fromArray(neutral[key]);
    const swing = swingFrames.get(neutral.path);
    let swingApplied = false;
    for (const response of calibration.calibration) {
      const muscle = response.muscle;
      if (swing && (muscle === swing.yTrait.muscle
          || muscle === swing.zTrait.muscle)) {
        if (!swingApplied) {
          const target = zySwing(swing.parameter,
            swing.yTrait, swing.zTrait,
            muscles[swing.yTrait.muscle], muscles[swing.zTrait.muscle]);
          rotation.multiply(swing.frame).multiply(swing.base.clone().invert()
            .multiply(target)).multiply(swing.frame.clone().invert());
          swingApplied = true;
        }
        continue;
      }
      const offset = muscles[muscle] - calibration.neutralMuscles[muscle];
      if (Math.abs(offset) < 1e-8) continue;
      const positive = offset > 0;
      const value = positive ? response.plusValue : response.minusValue;
      const denominator = value - calibration.neutralMuscles[muscle];
      if (Math.abs(denominator) < 1e-8) continue;
      const bone = (positive ? response[`plus${suffix}`] : response[`minus${suffix}`])
        ?.find(entry => (rig ? entry.path : entry.index) === id);
      if (!bone) continue;
      const half = rotationVector(neutral[key], bone[key]);
      const fullValue = positive ? response.plusFullValue : response.minusFullValue;
      const fullBone = (positive ? response[`plusFull${suffix}`] : response[`minusFull${suffix}`])
        ?.find(entry => (rig ? entry.path : entry.index) === id);
      let vector;
      if (!fullBone || !Number.isFinite(fullValue)
          || Math.abs(fullValue - value) < 1e-8
          || Math.abs(offset) <= Math.abs(denominator)) {
        vector = half.multiplyScalar(offset / denominator);
      } else {
        const full = rotationVector(neutral[key], fullBone[key]);
        vector = half.add(full.sub(half).multiplyScalar(
          (offset - denominator) / (fullValue - value)));
      }
      rotation.multiply(quaternionFromVector(vector));
    }
    if (bodyRotation && neutral.index === 0)
      rotation.premultiply(hipsAdjustment);
    result.set(id, rotation.normalize());
  }
  return result;
}

/** Decode all target skeleton joints, including unmapped ancestors. */
export function decodeCalibratedRigLocalRotations(calibration, muscles,
  bodyRotation = null, useZYSwing = true) {
  return decodeCalibratedAvatarRotations(calibration, muscles,
    { space: 'local', rig: true, bodyRotation, useZYSwing });
}
