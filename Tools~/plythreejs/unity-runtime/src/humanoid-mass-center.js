import * as THREE from 'three';

const bodyCenterCache = new WeakMap();

function bodyCenterFromProbes(probes, hips) {
  if (bodyCenterCache.has(probes)) return bodyCenterCache.get(probes);
  if (probes.length !== 3 || probes.some(probe =>
    !probe.hipsLocalPosition || !probe.hipsLocalRotation))
    throw new TypeError('Avatar needs three body rotation position probes');
  const neutralRotation = new THREE.Quaternion().fromArray(hips.localRotation);
  const neutralPosition = new THREE.Vector3().fromArray(hips.localPosition);
  const matrix = Array.from({ length: 3 }, () => [0, 0, 0, 0]);
  for (const probe of probes) {
    const rotation = new THREE.Quaternion().fromArray(probe.hipsLocalRotation)
      .multiply(neutralRotation.clone().invert());
    const columns = [new THREE.Vector3(1, 0, 0),
      new THREE.Vector3(0, 1, 0), new THREE.Vector3(0, 0, 1)]
      .map(axis => axis.clone().sub(axis.clone().applyQuaternion(rotation)));
    const shift = new THREE.Vector3().fromArray(probe.hipsLocalPosition)
      .sub(neutralPosition);
    for (let row = 0; row < 3; row++) {
      const coefficients = columns.map(column => column.getComponent(row));
      for (let i = 0; i < 3; i++) {
        matrix[i][3] += coefficients[i] * shift.getComponent(row);
        for (let j = 0; j < 3; j++)
          matrix[i][j] += coefficients[i] * coefficients[j];
      }
    }
  }
  for (let i = 0; i < 3; i++) {
    let pivot = i;
    for (let j = i + 1; j < 3; j++)
      if (Math.abs(matrix[j][i]) > Math.abs(matrix[pivot][i])) pivot = j;
    [matrix[i], matrix[pivot]] = [matrix[pivot], matrix[i]];
    const divisor = matrix[i][i];
    if (Math.abs(divisor) < 1e-10)
      throw new TypeError('Avatar body rotation probes are degenerate');
    for (let j = i; j < 4; j++) matrix[i][j] /= divisor;
    for (let row = 0; row < 3; row++) if (row !== i) {
      const factor = matrix[row][i];
      for (let j = i; j < 4; j++) matrix[row][j] -= factor * matrix[i][j];
    }
  }
  const center = new THREE.Vector3(...matrix.map(row => row[3]));
  bodyCenterCache.set(probes, center);
  return center;
}

function bodyTranslationFromProbes(probes, hips, bodyPosition,
  neutralBodyPosition) {
  if (probes.length !== 3)
    throw new TypeError('Avatar needs three body translation probes');
  const neutralHips = new THREE.Vector3().fromArray(hips.localPosition);
  const translation = new THREE.Vector3();
  for (let axis = 0; axis < 3; axis++) {
    const probe = probes[axis];
    const denominator = probe.inputPosition[axis] - neutralBodyPosition[axis];
    if (Math.abs(denominator) < 1e-8)
      throw new TypeError('Avatar body translation probe is degenerate');
    translation.addScaledVector(new THREE.Vector3()
      .fromArray(probe.hipsLocalPosition).sub(neutralHips),
    (bodyPosition[axis] - neutralBodyPosition[axis]) / denominator);
  }
  return translation;
}

export function rigWorldTransforms(bones, localRotations, hipsPosition = null) {
  const byPath = new Map(bones.map(bone => [bone.path, bone]));
  const world = new Map();
  const at = path => {
    if (world.has(path)) return world.get(path);
    const bone = byPath.get(path);
    if (!bone) throw new TypeError(`Missing Avatar joint: ${path}`);
    if (path === '') {
      const root = { position: new THREE.Vector3(), rotation: new THREE.Quaternion() };
      world.set(path, root);
      return root;
    }
    const parent = at(bone.parentPath);
    const position = bone.index === 0 && hipsPosition
      ? hipsPosition.clone() : new THREE.Vector3().fromArray(bone.localPosition);
    position.applyQuaternion(parent.rotation).add(parent.position);
    const rotation = parent.rotation.clone().multiply(
      localRotations?.get(path) ?? new THREE.Quaternion().fromArray(bone.localRotation));
    const result = { position, rotation };
    world.set(path, result);
    return result;
  };
  for (const bone of bones) at(bone.path);
  return world;
}

function massCenter(world, bones, parameters, segmentFraction = 0.5,
  massWeights = null) {
  const byIndex = new Map(bones.filter(bone => bone.index >= 0)
    .map(bone => [bone.index, bone]));
  if (massWeights) {
    const center = new THREE.Vector3();
    for (const { index, weight } of massWeights) {
      const bone = byIndex.get(index);
      if (bone) center.addScaledVector(world.get(bone.path).position, weight);
    }
    return center;
  }
  const children = new Map();
  for (const parameter of parameters) {
    if (!children.has(parameter.parentBone)) children.set(parameter.parentBone, []);
    children.get(parameter.parentBone).push(parameter);
  }
  const center = new THREE.Vector3();
  let total = 0;
  for (const parameter of parameters) {
    const bone = byIndex.get(parameter.index);
    if (!bone) continue;
    const descendants = children.get(parameter.index) ?? [];
    const mass = parameter.hierarchyMass - descendants.reduce(
      (sum, child) => sum + child.hierarchyMass, 0);
    if (mass <= 0) continue;
    const position = world.get(bone.path).position.clone();
    const mappedChildren = descendants.filter(child => byIndex.has(child.index));
    if (mappedChildren.length) {
      const midpoint = new THREE.Vector3();
      for (const child of mappedChildren)
        midpoint.add(world.get(byIndex.get(child.index).path).position);
      midpoint.divideScalar(mappedChildren.length);
      position.lerp(midpoint, segmentFraction);
    }
    center.addScaledVector(position, mass);
    total += mass;
  }
  if (total <= 0) throw new TypeError('Avatar has no humanoid mass');
  return center.divideScalar(total);
}

/** Infer hips translation from Unity's normalized body center and bone masses. */
export function solveHumanoidHipsPosition({ rigBones, boneParameters,
  localRotations, bodyPosition, neutralBodyPosition, humanScale,
  segmentFraction = 0.5, bodyRotationProbes = null,
  bodyPositionProbes = null, massWeights = null,
  bodyOnlyLocalRotations = null }) {
  const hips = rigBones.find(bone => bone.index === 0);
  if (!hips || !Number.isFinite(humanScale) || humanScale <= 0)
    throw new TypeError('Avatar needs hips and a valid human scale');
  const rest = rigWorldTransforms(rigBones);
  const pose = rigWorldTransforms(rigBones, localRotations);
  const restCenter = massCenter(rest, rigBones, boneParameters,
    segmentFraction, massWeights);
  const poseCenter = massCenter(pose, rigBones, boneParameters,
    segmentFraction, massWeights);
  const parentInverse = pose.get(hips.parentPath).rotation.clone().invert();
  const translation = bodyPositionProbes
    ? bodyTranslationFromProbes(bodyPositionProbes, hips, bodyPosition,
      neutralBodyPosition)
    : new THREE.Vector3().fromArray(bodyPosition)
      .sub(new THREE.Vector3().fromArray(neutralBodyPosition))
      .multiplyScalar(humanScale).applyQuaternion(parentInverse);
  const modeledCorrection = restCenter.clone().sub(poseCenter)
    .applyQuaternion(parentInverse);
  const result = new THREE.Vector3().fromArray(hips.localPosition)
    .add(translation).add(modeledCorrection);
  if (bodyRotationProbes) {
    const bodyHipsRotation = (bodyOnlyLocalRotations ?? localRotations)
      .get(hips.path);
    const bodyOnlyRotations = new Map(rigBones.map(bone => [bone.path,
      new THREE.Quaternion().fromArray(bone.localRotation)]));
    bodyOnlyRotations.set(hips.path, bodyHipsRotation);
    const bodyOnlyCenter = massCenter(rigWorldTransforms(rigBones,
      bodyOnlyRotations), rigBones, boneParameters, segmentFraction,
    massWeights);
    const modeledShift = restCenter.sub(bodyOnlyCenter)
      .applyQuaternion(parentInverse);
    const center = bodyCenterFromProbes(bodyRotationProbes, hips);
    const rotation = bodyHipsRotation.clone().multiply(
      new THREE.Quaternion().fromArray(hips.localRotation).invert());
    const exactShift = center.clone().sub(center.clone().applyQuaternion(rotation));
    result.add(exactShift.sub(modeledShift));
  }
  return result;
}
