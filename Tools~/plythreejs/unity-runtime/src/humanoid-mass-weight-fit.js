import * as THREE from 'three';
import { decodeCalibratedRigLocalRotations } from './humanoid-avatar-calibration.js';
import { rigWorldTransforms } from './humanoid-mass-center.js';

function solveLinear(matrix, rhs) {
  const rows = matrix.map((row, index) => [...row, rhs[index]]);
  const size = rows.length;
  for (let i = 0; i < size; i++) {
    let pivot = i;
    for (let j = i + 1; j < size; j++)
      if (Math.abs(rows[j][i]) > Math.abs(rows[pivot][i])) pivot = j;
    [rows[i], rows[pivot]] = [rows[pivot], rows[i]];
    if (Math.abs(rows[i][i]) < 1e-12)
      throw new TypeError('Avatar mass calibration is degenerate');
    const divisor = rows[i][i];
    for (let j = i; j <= size; j++) rows[i][j] /= divisor;
    for (let row = 0; row < size; row++) if (row !== i) {
      const factor = rows[row][i];
      for (let j = i; j <= size; j++) rows[row][j] -= factor * rows[i][j];
    }
  }
  return rows.map(row => row[size]);
}

/** Fit target-Avatar joint mass weights from isolated Unity muscle probes. */
export function fitHumanoidMassWeights(calibration, ridge = 1e-4) {
  const mapped = calibration.neutralRigBones.filter(bone => bone.index >= 0);
  const hips = mapped.find(bone => bone.index === 0);
  if (!hips || !calibration.boneParameters?.length)
    throw new TypeError('Avatar mass calibration lacks mapped bones');
  const neutral = calibration.neutralMuscles;
  const neutralRotations = decodeCalibratedRigLocalRotations(calibration, neutral);
  const rest = rigWorldTransforms(calibration.neutralRigBones, neutralRotations);
  const parentInverse = rest.get(hips.parentPath).rotation.clone().invert();
  const samples = [];
  for (const response of calibration.calibration)
    for (const [valueField, targetField] of [
      ['plusValue', 'plusHipsPosition'],
      ['minusValue', 'minusHipsPosition']]) {
      const muscles = [...neutral];
      muscles[response.muscle] = response[valueField];
      if (!response[targetField])
        throw new TypeError('Avatar calibration lacks hips muscle probes');
      const pose = rigWorldTransforms(calibration.neutralRigBones,
        decodeCalibratedRigLocalRotations(calibration, muscles));
      const features = mapped.map(bone => pose.get(bone.path).position.clone()
        .sub(rest.get(bone.path).position).applyQuaternion(parentInverse));
      const target = new THREE.Vector3().fromArray(hips.localPosition)
        .sub(new THREE.Vector3().fromArray(response[targetField]));
      samples.push({ features, target });
    }
  const children = new Map();
  for (const parameter of calibration.boneParameters) {
    if (!children.has(parameter.parentBone))
      children.set(parameter.parentBone, []);
    children.get(parameter.parentBone).push(parameter);
  }
  const priorByIndex = new Map(mapped.map(bone => [bone.index, 0]));
  let total = 0;
  for (const parameter of calibration.boneParameters) {
    const descendants = children.get(parameter.index) ?? [];
    const mass = parameter.hierarchyMass - descendants.reduce((sum, child) =>
      sum + child.hierarchyMass, 0);
    if (mass <= 0 || !priorByIndex.has(parameter.index)) continue;
    total += mass;
    const mappedChildren = descendants.filter(child =>
      priorByIndex.has(child.index));
    priorByIndex.set(parameter.index,
      priorByIndex.get(parameter.index)
        + (mappedChildren.length ? mass * 0.5 : mass));
    for (const child of mappedChildren)
      priorByIndex.set(child.index, priorByIndex.get(child.index)
        + mass * 0.5 / mappedChildren.length);
  }
  if (total <= 0) throw new TypeError('Avatar has no humanoid mass');
  const size = mapped.length;
  const matrix = Array.from({ length: size }, (_, i) =>
    Array.from({ length: size }, (_, j) => i === j ? ridge : 0));
  const rhs = mapped.map(bone => ridge
    * priorByIndex.get(bone.index) / total);
  for (const sample of samples)
    for (let axis = 0; axis < 3; axis++)
      for (let i = 0; i < size; i++) {
        const first = sample.features[i].getComponent(axis);
        rhs[i] += first * sample.target.getComponent(axis);
        for (let j = 0; j < size; j++)
          matrix[i][j] += first * sample.features[j].getComponent(axis);
      }
  const weights = solveLinear(matrix, rhs);
  return mapped.map((bone, index) => ({ index: bone.index,
    weight: weights[index] }));
}
