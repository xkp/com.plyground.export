using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Plyground.Preview.Editor
{
    // Samples imported Humanoid clips in Unity, where the source Avatar and
    // muscle curves can be evaluated accurately. The browser retargets these
    // joint-space samples onto each materialized character's own skeleton.
    internal static class PreviewHumanoidClipExporter
    {
        [Serializable] private sealed class Report
        {
            public string format = "plyground.preview.humanoid-controllers/v1";
            public string rawMotionEntry;
            public string calibrationEntry;
            public ControllerRecord[] controllers;
        }
        [Serializable] private sealed class RawClipRecord
        {
            public string name;
            public float duration;
            public bool keepOriginalOrientation;
            public bool keepOriginalPositionXZ;
            public RawCurveRecord[] curves;
            public string[] missingCurves;
            public EvaluatedBodyRecord evaluatedBody;
            public ClipRecord midpointPose;
            public ClipRecord quarterPose;
            public ClipRecord eighthPose;
        }
        [Serializable] private sealed class EvaluatedBodyRecord
        {
            public float sourceHumanScale;
            public float[] times;
            public float[] rootPositions;
            public float[] rootRotations;
            public float[] bodyPositions;
            public float[] bodyRotations;
            public MuscleCorrectionRecord[] muscleCorrections;
        }
        [Serializable] private sealed class MuscleCorrectionRecord
        {
            public int index;
            public int[] values;
        }
        [Serializable] private sealed class RawCurveRecord
        {
            public string property;
            public float[] keys;
        }
        [Serializable] private sealed class RawControllerRecord
        {
            public string assetId;
            public RawClipRecord[] clips;
            public ClipRecord[] fallbackClips;
        }
        [Serializable] private sealed class ControllerRecord
        {
            public string assetId;
            public string sourcePrefab;
            public string sourceAvatarId;
            public ClipRecord[] clips;
        }
        [Serializable] private sealed class ClipRecord
        {
            public string name;
            public float duration;
            public float[] times;
            public BoneRecord[] bones;
            public EventRecord[] events;
            public float[] rootPositions;
            public float[] rootRotations;
        }
        [Serializable] private sealed class BoneRecord
        {
            public int index;
            public float[] restRotation;
            public float[] rotations;
            public float[] restPosition;
            public float[] positions;
        }
        [Serializable] private sealed class EventRecord
        {
            public float time;
            public string functionName;
            public string stringParameter;
            public float floatParameter;
            public int intParameter;
        }

        internal static bool Export(string path, IEnumerable<UnityEngine.Object> catalogAssets,
            IEnumerable<GameObject> selectedPrefabs)
        {
            var selected = selectedPrefabs.Where(value => value != null).ToArray();
            var preferred = selected.Concat(catalogAssets.OfType<GameObject>())
                .Where(value => value != null).Distinct().ToList();
            var dependencyPaths = preferred.Cast<UnityEngine.Object>().Concat(catalogAssets)
                .Where(value => value != null).Select(AssetDatabase.GetAssetPath)
                .Where(value => !string.IsNullOrEmpty(value)).Distinct()
                .SelectMany(path => AssetDatabase.GetDependencies(path, true))
                .Where(path => path.EndsWith(".controller", StringComparison.OrdinalIgnoreCase))
                .Distinct(StringComparer.OrdinalIgnoreCase);
            var controllers = catalogAssets.OfType<AnimatorController>()
                .Concat(preferred.SelectMany(prefab => prefab.GetComponentsInChildren<Animator>(true))
                    .Select(animator => animator.runtimeAnimatorController).OfType<AnimatorController>())
                .Concat(dependencyPaths.Select(AssetDatabase.LoadAssetAtPath<AnimatorController>)
                    .Where(value => value != null))
                .Distinct().ToArray();
            if (controllers.Length == 0) return false;
            var records = new List<ControllerRecord>();
            var rawRecords = new List<RawControllerRecord>();
            foreach (var controller in controllers)
            {
                var clips = controller.animationClips.Where(clip => clip != null && clip.isHumanMotion)
                    .GroupBy(clip => clip.name, StringComparer.Ordinal).Select(group => group.First()).ToArray();
                if (clips.Length == 0) continue;
                var prefab = FindReferencePrefab(controller, preferred);
                if (prefab == null)
                    throw new InvalidOperationException("Preview cannot sample Humanoid controller " + controller.name
                        + ": no project prefab has a valid Humanoid Animator.");
                records.Add(new ControllerRecord {
                    assetId = GlobalObjectId.GetGlobalObjectIdSlow(controller).ToString(),
                    sourcePrefab = AssetDatabase.GetAssetPath(prefab),
                    sourceAvatarId = GlobalObjectId.GetGlobalObjectIdSlow(prefab
                        .GetComponentInChildren<Animator>(true).avatar).ToString(),
                    clips = clips.Select(clip => Sample(prefab, clip)).ToArray()
                });
                rawRecords.Add(new RawControllerRecord {
                    assetId = GlobalObjectId.GetGlobalObjectIdSlow(controller).ToString(),
                    clips = clips.Select(clip => {
                        try { return ExportRawClip(clip, prefab); }
                        catch (Exception error)
                        {
                            Debug.LogWarning("Preview raw Humanoid clip skipped: "
                                + clip.name + ": " + error.Message);
                            return null;
                        }
                    }).Where(value => value != null).ToArray(),
                    fallbackClips = clips.Select(clip =>
                        Sample(prefab, clip, false, false, 4)).ToArray()
                });
            }
            if (records.Count == 0) return false;
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var rawPath = Path.Combine(Path.GetDirectoryName(path), "humanoid-curves.json.gz");
            using (var stream = File.Create(rawPath))
            using (var gzip = new GZipStream(stream,
                System.IO.Compression.CompressionLevel.Optimal))
            using (var writer = new StreamWriter(gzip, new UTF8Encoding(false)))
                writer.Write(JsonUtility.ToJson(new RawMotionReport {
                    controllers = rawRecords.ToArray() }));
            var calibrationPath = Path.Combine(Path.GetDirectoryName(path),
                "avatar-calibrations.json.gz");
            var calibrated = PreviewAvatarCalibrationExporter.Export(calibrationPath,
                selected.Concat(records.Select(record =>
                    AssetDatabase.LoadAssetAtPath<GameObject>(record.sourcePrefab))));
            File.WriteAllText(path, JsonUtility.ToJson(new Report {
                controllers = records.ToArray(), rawMotionEntry = "humanoid-curves.json.gz",
                calibrationEntry = calibrated ? "avatar-calibrations.json.gz" : null }),
                new UTF8Encoding(false));
            return true;
        }

        [Serializable] private sealed class RawMotionReport
        {
            public string format = "plyground.preview.humanoid-curves/v1";
            public string[] muscleNames = HumanTrait.MuscleName;
            public RawControllerRecord[] controllers;
        }

        private static RawClipRecord ExportRawClip(AnimationClip clip,
            GameObject prefab)
        {
            var serialized = new SerializedObject(clip);
            var orientation = serialized.FindProperty(
                "m_AnimationClipSettings.m_KeepOriginalOrientation");
            var position = serialized.FindProperty(
                "m_AnimationClipSettings.m_KeepOriginalPositionXZ");
            if (orientation == null || position == null)
                return null;
            var curves = AnimationUtility.GetCurveBindings(clip)
                .Where(binding => binding.type == typeof(Animator))
                .Select(binding => {
                    var curve = AnimationUtility.GetEditorCurve(clip, binding);
                    var keys = new float[curve.length * 7];
                    for (int index = 0; index < curve.length; index++)
                    {
                        var key = curve.keys[index];
                        int offset = index * 7;
                        keys[offset] = key.time;
                        keys[offset + 1] = key.value;
                        keys[offset + 2] = key.inTangent;
                        keys[offset + 3] = key.outTangent;
                        keys[offset + 4] = key.inWeight;
                        keys[offset + 5] = key.outWeight;
                        keys[offset + 6] = (int)key.weightedMode;
                    }
                    return new RawCurveRecord { property = binding.propertyName,
                        keys = keys };
                }).ToArray();
            var properties = new HashSet<string>(curves.Select(value => value.property),
                StringComparer.Ordinal);
            var expected = HumanTrait.MuscleName.Select(FingerCurveProperty)
                .Concat(new[] { "RootT.x", "RootT.y", "RootT.z",
                    "RootQ.x", "RootQ.y", "RootQ.z", "RootQ.w" });
            var missingCurves = expected.Where(value => !properties.Contains(value))
                .ToArray();
            return new RawClipRecord { name = clip.name, duration = clip.length,
                keepOriginalOrientation = orientation.boolValue,
                keepOriginalPositionXZ = position.boolValue, curves = curves,
                missingCurves = missingCurves,
                evaluatedBody = ExportEvaluatedBody(clip, prefab),
                midpointPose = Sample(prefab, clip, true),
                quarterPose = Sample(prefab, clip, false, true),
                eighthPose = Sample(prefab, clip, false, false, 1, true) };
        }

        private static EvaluatedBodyRecord ExportEvaluatedBody(AnimationClip clip,
            GameObject prefab)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            try
            {
                var animator = instance.GetComponentInChildren<Animator>(true);
                if (animator == null || !animator.isHuman)
                    throw new InvalidOperationException("Reference prefab has no Humanoid Animator");
                var originPosition = instance.transform.position;
                var originInverse = Quaternion.Inverse(instance.transform.rotation);
                int count = Math.Max(1, Mathf.CeilToInt(clip.length
                    * Mathf.Max(60f, clip.frameRate)));
                var record = new EvaluatedBodyRecord {
                    sourceHumanScale = animator.humanScale,
                    times = new float[count + 1],
                    rootPositions = new float[(count + 1) * 3],
                    rootRotations = new float[(count + 1) * 4],
                    bodyPositions = new float[(count + 1) * 3],
                    bodyRotations = new float[(count + 1) * 4]
                };
                var muscleBindings = AnimationUtility.GetCurveBindings(clip)
                    .Where(binding => binding.type == typeof(Animator))
                    .GroupBy(binding => binding.propertyName,
                        StringComparer.Ordinal)
                    .ToDictionary(group => group.Key, group => group.First(),
                        StringComparer.Ordinal);
                var muscleCurves = HumanTrait.MuscleName.Select(name => {
                    var property = FingerCurveProperty(name);
                    return muscleBindings.TryGetValue(property, out var binding)
                        ? AnimationUtility.GetEditorCurve(clip, binding) : null;
                }).ToArray();
                var deltas = Enumerable.Range(0, HumanTrait.MuscleCount)
                    .Select(_ => new int[count + 1]).ToArray();
                using (var handler = new HumanPoseHandler(animator.avatar,
                    animator.transform))
                for (int frame = 0; frame <= count; frame++)
                {
                    float time = frame * clip.length / count;
                    record.times[frame] = time;
                    clip.SampleAnimation(instance, time);
                    var pose = new HumanPose();
                    handler.GetHumanPose(ref pose);
                    for (int muscle = 0; muscle < deltas.Length; muscle++)
                        deltas[muscle][frame] = Mathf.RoundToInt(10000f
                            * (pose.muscles[muscle]
                                - (muscleCurves[muscle] != null
                                    ? muscleCurves[muscle].Evaluate(time) : 0f)));
                    var rootPosition = originInverse
                        * (animator.transform.position - originPosition);
                    var rootRotation = originInverse * animator.transform.rotation;
                    var bodyPosition = originInverse
                        * (pose.bodyPosition * animator.humanScale - originPosition)
                        / animator.humanScale;
                    var bodyRotation = originInverse * pose.bodyRotation;
                    int offset3 = frame * 3;
                    int offset4 = frame * 4;
                    record.rootPositions[offset3] = rootPosition.x;
                    record.rootPositions[offset3 + 1] = rootPosition.y;
                    record.rootPositions[offset3 + 2] = rootPosition.z;
                    record.rootRotations[offset4] = rootRotation.x;
                    record.rootRotations[offset4 + 1] = rootRotation.y;
                    record.rootRotations[offset4 + 2] = rootRotation.z;
                    record.rootRotations[offset4 + 3] = rootRotation.w;
                    record.bodyPositions[offset3] = bodyPosition.x;
                    record.bodyPositions[offset3 + 1] = bodyPosition.y;
                    record.bodyPositions[offset3 + 2] = bodyPosition.z;
                    record.bodyRotations[offset4] = bodyRotation.x;
                    record.bodyRotations[offset4 + 1] = bodyRotation.y;
                    record.bodyRotations[offset4 + 2] = bodyRotation.z;
                    record.bodyRotations[offset4 + 3] = bodyRotation.w;
                }
                record.muscleCorrections = Enumerable.Range(0, deltas.Length)
                    .Where(index => deltas[index].Any(value => Math.Abs(value) > 10))
                    .Select(index => new MuscleCorrectionRecord {
                        index = index, values = deltas[index]
                    }).ToArray();
                return record;
            }
            finally { UnityEngine.Object.DestroyImmediate(instance); }
        }

        private static string FingerCurveProperty(string name)
        {
            foreach (var side in new[] { "Left", "Right" })
            foreach (var finger in new[] { "Thumb", "Index", "Middle", "Ring", "Little" })
            {
                var prefix = side + " " + finger + " ";
                if (name.StartsWith(prefix, StringComparison.Ordinal))
                    return side + "Hand." + finger + "." + name.Substring(prefix.Length);
            }
            return name;
        }

        private static GameObject FindReferencePrefab(AnimatorController controller, List<GameObject> preferred)
        {
            bool Matches(GameObject prefab, bool exact)
            {
                var animator = prefab.GetComponentInChildren<Animator>(true);
                return animator != null && animator.avatar != null && animator.avatar.isHuman
                    && (!exact || animator.runtimeAnimatorController == controller);
            }
            var candidates = preferred.Concat(AssetDatabase.FindAssets("t:Prefab")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(path => AssetDatabase.LoadAssetAtPath<GameObject>(path))
                .Where(value => value != null)).Distinct().ToArray();
            return candidates.FirstOrDefault(value => Matches(value, true))
                ?? candidates.FirstOrDefault(value => Matches(value, false));
        }

        private static ClipRecord Sample(GameObject prefab, AnimationClip clip,
            bool midpoints = false, bool quarters = false,
            int subdivisions = 1, bool eighths = false)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            try
            {
                var animator = instance.GetComponentInChildren<Animator>(true);
                if (animator == null || !animator.isHuman)
                    throw new InvalidOperationException("Reference prefab has no Humanoid Animator: " + prefab.name);
                var rootInverse = Quaternion.Inverse(instance.transform.rotation);
                var bones = new List<(int index, Transform transform, BoneRecord record)>();
                for (int index = 0; index < (int)HumanBodyBones.LastBone; index++)
                {
                    var bone = animator.GetBoneTransform((HumanBodyBones)index);
                    if (bone == null) continue;
                    var rest = rootInverse * bone.rotation;
                    var restPosition = bone.localPosition;
                    bones.Add((index, bone, new BoneRecord {
                        index = index, restRotation = new[] { rest.x, rest.y, rest.z, rest.w },
                        rotations = null,
                        restPosition = index == (int)HumanBodyBones.Hips
                            ? new[] { restPosition.x, restPosition.y, restPosition.z } : null
                    }));
                }
                var duration = Mathf.Max(clip.length, 1f / 30f);
                var count = Math.Max(1, Mathf.CeilToInt(duration * Mathf.Max(30f, clip.frameRate)));
                var times = eighths
                    ? Enumerable.Range(0, count * 4)
                        .Select(frame => (frame + 0.5f) * duration
                            / (count * 4)).ToArray()
                    : subdivisions > 1
                    ? Enumerable.Range(0, count * subdivisions + 1)
                        .Select(frame => frame * duration
                            / (count * subdivisions)).ToArray()
                    : quarters
                    ? Enumerable.Range(0, count * 2)
                        .Select(frame => (frame / 2
                            + (frame % 2 == 0 ? 0.25f : 0.75f))
                            * duration / count).ToArray()
                    : midpoints
                    ? Enumerable.Range(0, count)
                        .Select(frame => (frame + 0.5f) * duration / count).ToArray()
                    : Enumerable.Range(0, count + 1)
                        .Select(frame => frame * duration / count).ToArray();
                foreach (var entry in bones)
                {
                    entry.record.rotations = new float[times.Length * 4];
                    if (entry.record.restPosition != null)
                        entry.record.positions = new float[times.Length * 3];
                }
                for (int frame = 0; frame < times.Length; frame++)
                {
                    clip.SampleAnimation(instance, Mathf.Min(times[frame], clip.length));
                    foreach (var entry in bones)
                    {
                        var rotation = rootInverse * entry.transform.rotation;
                        int offset = frame * 4;
                        entry.record.rotations[offset] = rotation.x;
                        entry.record.rotations[offset + 1] = rotation.y;
                        entry.record.rotations[offset + 2] = rotation.z;
                        entry.record.rotations[offset + 3] = rotation.w;
                        if (entry.record.positions != null)
                        {
                            var position = entry.transform.localPosition;
                            int positionOffset = frame * 3;
                            entry.record.positions[positionOffset] = position.x;
                            entry.record.positions[positionOffset + 1] = position.y;
                            entry.record.positions[positionOffset + 2] = position.z;
                        }
                    }
                }
                var trajectory = midpoints || quarters || eighths ? null
                    : SampleRootMotion(animator, clip, times);
                return new ClipRecord {
                    name = clip.name, duration = duration, times = times,
                    bones = bones.Select(entry => entry.record).ToArray(),
                    events = midpoints || quarters || eighths ? null : AnimationUtility.GetAnimationEvents(clip).Select(value => new EventRecord {
                        time = value.time, functionName = value.functionName,
                        stringParameter = value.stringParameter, floatParameter = value.floatParameter,
                        intParameter = value.intParameter
                    }).ToArray(),
                    rootPositions = trajectory?.Item1, rootRotations = trajectory?.Item2
                };
            }
            finally { UnityEngine.Object.DestroyImmediate(instance); }
        }

        private static Tuple<float[], float[]> SampleRootMotion(Animator animator, AnimationClip clip, float[] times)
        {
            var positions = new float[times.Length * 3];
            var rotations = new float[times.Length * 4];
            if (clip.length <= 0f)
            {
                for (int frame = 0; frame < times.Length; frame++) rotations[frame * 4 + 3] = 1f;
                return Tuple.Create(positions, rotations);
            }
            var graph = PlayableGraph.Create("PlygroundPreviewRootMotion");
            graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            try
            {
                var playable = AnimationClipPlayable.Create(graph, clip);
                var output = AnimationPlayableOutput.Create(graph, "RootMotion", animator);
                output.SetSourcePlayable(playable);
                animator.applyRootMotion = false;
                graph.Play();
                graph.Evaluate(0f);
                var position = Vector3.zero;
                var rotation = Quaternion.identity;
                for (int frame = 0; frame < times.Length; frame++)
                {
                    if (frame > 0)
                    {
                        graph.Evaluate(times[frame] - times[frame - 1]);
                        position += animator.deltaPosition;
                        rotation *= animator.deltaRotation;
                    }
                    int p = frame * 3, r = frame * 4;
                    positions[p] = position.x; positions[p + 1] = position.y; positions[p + 2] = position.z;
                    rotations[r] = rotation.x; rotations[r + 1] = rotation.y;
                    rotations[r + 2] = rotation.z; rotations[r + 3] = rotation.w;
                }
            }
            finally { graph.Destroy(); }
            return Tuple.Create(positions, rotations);
        }
    }
}
