import * as THREE from 'three';
import { FBXLoader } from 'three/addons/loaders/FBXLoader.js';
import { retargetClip } from 'three/addons/utils/SkeletonUtils.js';

/** Parse an animation directly from its source FBX bytes. Animation-only FBXs are supported. */
export function parseFbxAnimation(bytes) {
  const buffer = bytes instanceof ArrayBuffer ? bytes
    : bytes.buffer.slice(bytes.byteOffset, bytes.byteOffset + bytes.byteLength);
  const source = new FBXLoader().parse(buffer, '');
  const bones = [];
  source.traverse(object => { if (object.isBone) bones.push(object); });
  if (!bones.length || !source.animations.length)
    throw new Error('FBX source needs a skeleton and at least one animation clip');
  return { source, skeleton: new THREE.Skeleton(bones), clips: source.animations };
}

export async function loadFbxAnimation(url) {
  const response = await fetch(url);
  if (!response.ok) throw new Error(`Could not fetch FBX animation: ${response.status} ${url}`);
  return parseFbxAnimation(await response.arrayBuffer());
}

/** Retarget an original FBX clip to an imported Three.js SkinnedMesh. */
export function retargetFbxAnimation(target, imported, {
  clip = imported.clips[0], boneName = bone => bone.name,
  rootBone = 'Hips', keepRootPosition = false, fps,
} = {}) {
  if (!target?.isSkinnedMesh || !target.skeleton) throw new TypeError('Target must be a SkinnedMesh');
  const sourceNames = new Set(imported.skeleton.bones.map(bone => bone.name));
  const matched = target.skeleton.bones.filter(bone => sourceNames.has(boneName(bone)));
  if (!matched.length) throw new Error('FBX and target skeleton have no mapped bones');
  const result = retargetClip(target, imported.skeleton, clip, {
    getBoneName: boneName,
    hip: boneName(target.skeleton.bones.find(bone => bone.name === rootBone) ?? { name: rootBone }),
    hipInfluence: keepRootPosition ? new THREE.Vector3(1, 1, 1) : new THREE.Vector3(),
    preserveBonePositions: true,
    ...(fps ? { fps } : {}),
  });
  if (!keepRootPosition)
    result.tracks = result.tracks.filter(track => !track.name.endsWith(`[${rootBone}].position`));
  if (!result.tracks.length || result.tracks.some(track => [...track.values].some(value => !Number.isFinite(value))))
    throw new Error('FBX retargeting produced no usable bone tracks');
  return result;
}

/** Play original source keys in the target rig's local frames using one reference pose. */
export function mapFbxAnimationToSkeleton(target, imported, {
  clip = imported.clips[0], boneName = bone => bone.name,
  referenceRotations = new Map(), name = clip.name,
} = {}) {
  if (!target?.isSkinnedMesh || !target.skeleton) throw new TypeError('Target must be a SkinnedMesh');
  const tracks = new Map(clip.tracks.map(track => [track.name, track]));
  const output = [];
  const source0 = new THREE.Quaternion();
  const value = new THREE.Quaternion();
  for (const bone of target.skeleton.bones) {
    const track = tracks.get(`${boneName(bone)}.quaternion`);
    if (!track) continue;
    const reference = referenceRotations.get(bone.name) ?? bone.quaternion;
    source0.fromArray(track.createInterpolant().evaluate(0));
    const correction = reference.clone().multiply(source0.invert());
    const values = new Float32Array(track.values.length);
    for (let index = 0; index < values.length; index += 4) {
      value.fromArray(track.values, index).premultiply(correction).normalize();
      value.toArray(values, index);
    }
    output.push(new THREE.QuaternionKeyframeTrack(
      `.bones[${bone.name}].quaternion`, track.times, values));
  }
  if (!output.length) throw new Error('FBX and target skeleton have no mapped rotation tracks');
  return new THREE.AnimationClip(name, clip.duration, output);
}
