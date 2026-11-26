import { readFile, writeFile } from 'node:fs/promises';
import { gunzipSync, gzipSync } from 'node:zlib';
import * as THREE from 'three';
import { assessCalibratedHumanoidClip,
  buildCalibratedHumanoidClip,
  PREVIEW_HUMANOID_LIMITS } from '../src/humanoid-calibrated-clip.js';
import { applyHumanoidMuscleCorrections,
  sampleHumanoidEvaluatedBody } from '../src/humanoid-evaluated-body.js';
import { makeHumanoidPoseCorrections } from '../src/humanoid-pose-corrections.js';

const [controllersPath, rawPath, calibrationPath] = process.argv.slice(2);
if (!controllersPath || !rawPath || !calibrationPath)
  throw new Error('Usage: node select-raw-humanoid-clips.mjs <controllers.json> '
    + '<curves.json.gz> <calibrations.json.gz>');
const controllers = JSON.parse(await readFile(controllersPath, 'utf8'));
const raw = JSON.parse(gunzipSync(await readFile(rawPath)));
const calibration = JSON.parse(gunzipSync(await readFile(calibrationPath)));
if (controllers.format !== 'plyground.preview.humanoid-controllers/v1'
  || raw.format !== 'plyground.preview.humanoid-curves/v1'
  || calibration.format !== 'plyground.preview.avatar-calibrations/v1')
  throw new Error('Unexpected Humanoid export format');

function interleavePoseFrames(sampled, midpoint) {
  if (midpoint?.times?.length !== sampled.times.length - 1)
    throw new TypeError('Humanoid midpoint poses missing');
  const other = new Map(midpoint.bones.map(bone => [bone.index, bone]));
  const interleave = (a, b, stride) => {
    if (a.length !== sampled.times.length * stride
      || b.length !== midpoint.times.length * stride)
      throw new TypeError('Humanoid midpoint bone frames mismatch');
    const result = [];
    for (let frame = 0; frame < sampled.times.length; frame++) {
      result.push(...a.slice(frame * stride, (frame + 1) * stride));
      if (frame < midpoint.times.length)
        result.push(...b.slice(frame * stride, (frame + 1) * stride));
    }
    return result;
  };
  const times = interleave(sampled.times, midpoint.times, 1);
  const bones = sampled.bones.map(bone => {
    const middle = other.get(bone.index);
    if (!middle) throw new TypeError('Humanoid midpoint bone missing');
    return { ...bone,
      rotations: interleave(bone.rotations, middle.rotations, 4),
      positions: bone.index === 0
        ? interleave(bone.positions, middle.positions, 3) : bone.positions };
  });
  return { ...sampled, times, bones };
}

function fallbackStepTimes(dense, rawClip) {
  const hips = dense.bones.find(bone => bone.index === 0);
  if (!hips) throw new TypeError('Dense Humanoid fallback has no Hips');
  const rotationTimes = [];
  const positionTimes = [];
  const rotationJump = frame => frame < 1 || frame >= dense.times.length
    ? 0 : new THREE.Quaternion().fromArray(hips.rotations,
      (frame - 1) * 4).angleTo(new THREE.Quaternion().fromArray(
      hips.rotations, frame * 4)) * 180 / Math.PI;
  const positionJump = frame => frame < 1 || frame >= dense.times.length
    ? 0 : new THREE.Vector3().fromArray(hips.positions,
      (frame - 1) * 3).distanceTo(new THREE.Vector3().fromArray(
      hips.positions, frame * 3));
  const curveTimes = rawClip?.curves?.flatMap(curve =>
    curve.keys.filter((_, index) => index % 7 === 0)) ?? [];
  for (let frame = 1; frame < dense.times.length; frame++) {
    const rotation = rotationJump(frame);
    const position = positionJump(frame);
    if (!(rotation > 15 && rotationJump(frame - 1) < 5
        && rotationJump(frame + 1) < 5)
      && !(position > 0.03 && positionJump(frame - 1) < 0.01
        && positionJump(frame + 1) < 0.01)) continue;
    const low = dense.times[frame - 1];
    const high = dense.times[frame];
    const center = (low + high) / 2;
    const keys = curveTimes.filter(time => time > low && time <= high);
    const grid = Math.round(center * 30) / 30;
    const cutoff = keys.length ? keys.reduce((a, b) =>
      Math.abs(a - center) < Math.abs(b - center) ? a : b)
      : grid > low && grid <= high ? grid : high;
    if (rotation > 15) rotationTimes.push(cutoff);
    if (position > 0.03) positionTimes.push(cutoff);
  }
  return { rotationTimes, positionTimes };
}
for (const controller of controllers.controllers) {
  controller.rawReadyClips = [];
  controller.rawBodyReadyClips = [];
  controller.rawMuscleReadyClips = [];
  controller.rawPoseReadyClips = [];
  const avatar = calibration.avatars.find(value =>
    value.avatarId === controller.sourceAvatarId);
  const rawController = raw.controllers.find(value =>
    value.assetId === controller.assetId);
  if (!avatar || !rawController)
    throw new TypeError('Humanoid controller lacks raw curves or Avatar calibration');
  const rawClips = new Map(rawController.clips.map(clip =>
    [clip.name, clip]));
  const denseByName = new Map(rawController.fallbackClips?.map(clip =>
    [clip.name, clip]) ?? []);
  for (const clip of controller.clips) {
    const source = rawClips.get(clip.name);
    if (!source) throw new TypeError(`No raw Humanoid curves for ${clip.name}`);
    delete source.poseCorrections;
    try {
      const candidate = buildCalibratedHumanoidClip(source,
        raw.muscleNames, avatar, clip);
      const result = assessCalibratedHumanoidClip(candidate, clip,
        PREVIEW_HUMANOID_LIMITS);
      const variants = [{ candidate, result, body: false, muscle: false }];
      if (result.accepted) {
        controller.rawReadyClips.push(clip.name);
      } else if (source.evaluatedBody) {
        const bodyOptions = {
            sampleBodyPose: time => sampleHumanoidEvaluatedBody(
              source.evaluatedBody, time),
          };
        const evaluated = buildCalibratedHumanoidClip(source,
          raw.muscleNames, avatar, clip, bodyOptions);
        const bodyResult = assessCalibratedHumanoidClip(evaluated, clip,
          PREVIEW_HUMANOID_LIMITS);
        variants.push({ candidate: evaluated, result: bodyResult,
          options: bodyOptions, body: true, muscle: false });
        if (bodyResult.accepted) {
          controller.rawReadyClips.push(clip.name);
          controller.rawBodyReadyClips.push(clip.name);
        } else if (source.evaluatedBody.muscleCorrections?.length) {
          const combinedOptions = {
            ...bodyOptions,
            sampleMuscles: (sample, _frame, time) =>
              applyHumanoidMuscleCorrections(source.evaluatedBody,
                sample.muscles, time),
          };
          const combined = buildCalibratedHumanoidClip(source,
            raw.muscleNames, avatar, clip, combinedOptions);
          const combinedResult = assessCalibratedHumanoidClip(combined,
            clip, PREVIEW_HUMANOID_LIMITS);
          variants.push({ candidate: combined, result: combinedResult,
            options: combinedOptions, body: true, muscle: true });
          if (combinedResult.accepted) {
            controller.rawReadyClips.push(clip.name);
            controller.rawBodyReadyClips.push(clip.name);
            controller.rawMuscleReadyClips.push(clip.name);
          }
        }
      }
      if (controller.rawReadyClips.includes(clip.name)) {
        const body = controller.rawBodyReadyClips.includes(clip.name);
        const muscle = controller.rawMuscleReadyClips.includes(clip.name);
        const quarterOptions = body ? {
          sampleBodyPose: time => sampleHumanoidEvaluatedBody(
            source.evaluatedBody, time),
          sampleMuscles: muscle ? (sample, _frame, time) =>
            applyHumanoidMuscleCorrections(source.evaluatedBody,
              sample.muscles, time) : undefined,
        } : undefined;
        const checks = [source.quarterPose, source.eighthPose];
        if (checks.some(reference => !reference?.times?.length
          || !assessCalibratedHumanoidClip(
            buildCalibratedHumanoidClip(source, raw.muscleNames,
              avatar, reference, quarterOptions), reference,
            PREVIEW_HUMANOID_LIMITS).accepted)) {
          controller.rawReadyClips = controller.rawReadyClips.filter(
            name => name !== clip.name);
          controller.rawBodyReadyClips = controller.rawBodyReadyClips.filter(
            name => name !== clip.name);
          controller.rawMuscleReadyClips = controller.rawMuscleReadyClips.filter(
            name => name !== clip.name);
        }
      }
      if (!controller.rawReadyClips.includes(clip.name)) {
        const reference = interleavePoseFrames(clip, source.midpointPose);
        const best = variants.sort((a, b) => Math.max(
          a.result.rotationDegrees / PREVIEW_HUMANOID_LIMITS.maxDegrees,
          a.result.hipsMeters / PREVIEW_HUMANOID_LIMITS.maxHipsMeters)
          - Math.max(
            b.result.rotationDegrees / PREVIEW_HUMANOID_LIMITS.maxDegrees,
            b.result.hipsMeters / PREVIEW_HUMANOID_LIMITS.maxHipsMeters))[0];
        const calibrationCandidate = buildCalibratedHumanoidClip(source,
          raw.muscleNames, avatar, reference, best.options);
        source.poseCorrections = makeHumanoidPoseCorrections(
          calibrationCandidate, reference);
        const corrected = buildCalibratedHumanoidClip(source,
          raw.muscleNames, avatar, reference, best.options);
        const quarter = source.quarterPose;
        if (!quarter?.times?.length)
          throw new TypeError('Humanoid quarter-frame validation missing');
        const heldout = buildCalibratedHumanoidClip(source,
          raw.muscleNames, avatar, quarter, best.options);
        const eighth = source.eighthPose;
        if (!eighth?.times?.length)
          throw new TypeError('Humanoid eighth-frame validation missing');
        const eighthCandidate = buildCalibratedHumanoidClip(source,
          raw.muscleNames, avatar, eighth, best.options);
        if (assessCalibratedHumanoidClip(corrected, reference,
          PREVIEW_HUMANOID_LIMITS).accepted
          && assessCalibratedHumanoidClip(heldout, quarter,
            PREVIEW_HUMANOID_LIMITS).accepted
          && assessCalibratedHumanoidClip(eighthCandidate, eighth,
            PREVIEW_HUMANOID_LIMITS).accepted) {
          controller.rawReadyClips.push(clip.name);
          controller.rawPoseReadyClips.push(clip.name);
          if (best.body) controller.rawBodyReadyClips.push(clip.name);
          if (best.muscle) controller.rawMuscleReadyClips.push(clip.name);
        } else delete source.poseCorrections;
      }
      if (!controller.rawReadyClips.includes(clip.name)) {
        const dense = denseByName.get(clip.name);
        const validation = source.eighthPose;
        if (!dense?.times?.length || !validation?.times?.length)
          throw new TypeError('Dense Humanoid calibration or validation missing');
        const steps = fallbackStepTimes(dense, source);
        for (const variant of variants) {
          delete source.poseCorrections;
          const denseCandidate = buildCalibratedHumanoidClip(source,
            raw.muscleNames, avatar, dense, variant.options);
          source.poseCorrections = makeHumanoidPoseCorrections(
            denseCandidate, dense);
          source.poseCorrections.stepRotationTimes = steps.rotationTimes;
          source.poseCorrections.stepPositionTimes = steps.positionTimes;
          const heldout = buildCalibratedHumanoidClip(source,
            raw.muscleNames, avatar, validation, variant.options);
          if (!assessCalibratedHumanoidClip(heldout, validation,
            PREVIEW_HUMANOID_LIMITS).accepted) continue;
          controller.rawReadyClips.push(clip.name);
          controller.rawPoseReadyClips.push(clip.name);
          if (variant.body) controller.rawBodyReadyClips.push(clip.name);
          if (variant.muscle) controller.rawMuscleReadyClips.push(clip.name);
          break;
        }
        if (!controller.rawReadyClips.includes(clip.name))
          throw new Error('Dense Humanoid correction exceeds Preview bound');
      }
    } catch (error) {
      throw new Error(`Raw Humanoid ${clip.name}: ${error.message}`,
        { cause: error });
    }
  }
  if (controller.rawReadyClips.length !== controller.clips.length)
    throw new Error('Humanoid export has unselected clips');
  controller.clips = controller.clips.map(clip => ({ ...clip, bones: [] }));
}
for (const rawController of raw.controllers) {
  delete rawController.fallbackClips;
  const selected = controllers.controllers.find(value =>
    value.assetId === rawController.assetId);
  const bodyReady = new Set(selected?.rawBodyReadyClips ?? []);
  const muscleReady = new Set(selected?.rawMuscleReadyClips ?? []);
  const poseReady = new Set(selected?.rawPoseReadyClips ?? []);
  for (const clip of rawController.clips) {
    delete clip._curveMap;
    delete clip.midpointPose;
    delete clip.quarterPose;
    delete clip.eighthPose;
    if (!poseReady.has(clip.name)) delete clip.poseCorrections;
    if (!bodyReady.has(clip.name)) delete clip.evaluatedBody;
    else if (!muscleReady.has(clip.name))
      delete clip.evaluatedBody.muscleCorrections;
  }
}
await writeFile(rawPath, gzipSync(JSON.stringify(raw), { level: 9 }));
await writeFile(controllersPath, JSON.stringify(controllers));
