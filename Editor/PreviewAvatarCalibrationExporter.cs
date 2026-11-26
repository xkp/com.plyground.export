using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Plyground.Preview.Editor
{
    internal static class PreviewAvatarCalibrationExporter
    {
        [Serializable] private sealed class Report
        {
            public string format = "plyground.preview.avatar-calibrations/v1";
            public Calibration[] avatars;
        }
        [Serializable] private sealed class Bone
        {
            public int index;
            public string path;
            public string parentPath;
            public float[] localPosition;
            public float[] localRotation;
        }
        [Serializable] private sealed class RotationProbe
        {
            public float[] inputRotation;
            public float[] hipsLocalRotation;
            public float[] hipsLocalPosition;
        }
        [Serializable] private sealed class PositionProbe
        {
            public float[] inputPosition;
            public float[] hipsLocalPosition;
        }
        [Serializable] private sealed class BoneParameter
        {
            public int index;
            public float[] preRotation;
            public float[] postRotation;
            public float[] limitSign;
            public float axisLength;
            public float hierarchyMass;
            public int parentBone;
        }
        [Serializable] private sealed class MuscleTrait
        {
            public int muscle;
            public int bone;
            public int dof;
            public float defaultMin;
            public float defaultMax;
        }
        [Serializable] private sealed class MuscleResponse
        {
            public int muscle;
            public float plusValue;
            public float minusValue;
            public float plusFullValue;
            public float minusFullValue;
            public Bone[] plusRig;
            public Bone[] minusRig;
            public Bone[] plusFullRig;
            public Bone[] minusFullRig;
            public float[] plusHipsPosition;
            public float[] minusHipsPosition;
            public float[] plusFullHipsPosition;
            public float[] minusFullHipsPosition;
        }
        [Serializable] private sealed class Calibration
        {
            public string avatarId;
            public string name;
            public float humanScale;
            public float[] neutralMuscles;
            public float[] neutralBodyPosition;
            public float[] neutralBodyRotation;
            public Bone[] neutralRigBones;
            public BoneParameter[] boneParameters;
            public MuscleTrait[] muscleTraits;
            public RotationProbe[] bodyRotationProbes;
            public PositionProbe[] bodyPositionProbes;
            public MuscleResponse[] calibration;
        }
        private static float[] Array(Vector3 value) =>
            new[] { value.x, value.y, value.z };
        private static float[] Array(Quaternion value) =>
            new[] { value.x, value.y, value.z, value.w };

        internal static bool Export(string path, IEnumerable<GameObject> prefabs)
        {
            var records = new List<Calibration>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var prefab in prefabs.Where(value => value != null).Distinct())
            {
                var source = prefab.GetComponentInChildren<Animator>(true);
                if (source == null || source.avatar == null || !source.avatar.isHuman)
                    continue;
                var id = GlobalObjectId.GetGlobalObjectIdSlow(source.avatar).ToString();
                if (!seen.Add(id)) continue;
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                try
                {
                    var animator = instance.GetComponentInChildren<Animator>(true);
                    animator.transform.SetPositionAndRotation(Vector3.zero,
                        Quaternion.identity);
                    animator.Rebind();
                    try { records.Add(Capture(animator, id)); }
                    catch (Exception error)
                    {
                        Debug.LogWarning("Preview Avatar calibration skipped for "
                            + source.avatar.name + ": " + error.Message);
                    }
                }
                finally { UnityEngine.Object.DestroyImmediate(instance); }
            }
            if (records.Count == 0) return false;
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            using (var stream = File.Create(path))
            using (var gzip = new GZipStream(stream,
                System.IO.Compression.CompressionLevel.Optimal))
            using (var writer = new StreamWriter(gzip, new UTF8Encoding(false)))
                writer.Write(JsonUtility.ToJson(new Report { avatars = records.ToArray() }));
            return true;
        }

        private static Bone[] RigBones(Animator animator)
        {
            var root = animator.transform;
            var names = new HashSet<string>(animator.avatar.humanDescription.skeleton
                .Select(value => value.name));
            var selected = new HashSet<Transform> { root };
            foreach (var transform in root.GetComponentsInChildren<Transform>(true))
            {
                if (!names.Contains(transform.name)) continue;
                for (var node = transform; node != null; node = node.parent)
                {
                    selected.Add(node);
                    if (node == root) break;
                }
            }
            var mapped = Enumerable.Range(0, (int)HumanBodyBones.LastBone)
                .Select(index => new { index, transform = animator.GetBoneTransform(
                    (HumanBodyBones)index) })
                .Where(value => value.transform != null)
                .GroupBy(value => value.transform)
                .ToDictionary(group => group.Key, group => group.First().index);
            string Path(Transform node)
            {
                if (node == root) return "";
                var parts = new List<string>();
                for (var current = node; current != root && current != null;
                    current = current.parent) parts.Add(current.name);
                parts.Reverse();
                return string.Join("/", parts);
            }
            return selected.OrderBy(node => Path(node).Count(c => c == '/'))
                .ThenBy(Path).Select(node => new Bone {
                    index = mapped.TryGetValue(node, out var index) ? index : -1,
                    path = Path(node),
                    parentPath = node == root ? null : Path(node.parent),
                    localPosition = Array(node == root ? Vector3.zero : node.localPosition),
                    localRotation = Array(node == root ? Quaternion.identity
                        : node.localRotation)
                }).ToArray();
        }

        private static Bone[] Sparse(Bone[] pose, Dictionary<string, Bone> neutral) =>
            pose.Where(bone => {
                var rest = neutral[bone.path].localRotation;
                var q = bone.localRotation;
                return Math.Abs(1f - Math.Abs(q[0] * rest[0] + q[1] * rest[1]
                    + q[2] * rest[2] + q[3] * rest[3])) > 1e-10f;
            }).Select(bone => new Bone { path = bone.path,
                localRotation = bone.localRotation }).ToArray();

        private static Calibration Capture(Animator animator, string id)
        {
            using (var handler = new HumanPoseHandler(animator.avatar,
                animator.transform))
            {
                var neutral = new HumanPose();
                handler.GetHumanPose(ref neutral);
                neutral.muscles = (float[])neutral.muscles.Clone();
                handler.SetHumanPose(ref neutral);
                var rig = RigBones(animator);
                var neutralByPath = rig.ToDictionary(bone => bone.path);
                var hips = animator.GetBoneTransform(HumanBodyBones.Hips);
                var rotationProbes = new[] { Vector3.right, Vector3.up,
                    Vector3.forward }.Select(axis => {
                    var pose = new HumanPose { bodyPosition = neutral.bodyPosition,
                        bodyRotation = Quaternion.AngleAxis(10f, axis)
                            * neutral.bodyRotation,
                        muscles = (float[])neutral.muscles.Clone() };
                    handler.SetHumanPose(ref pose);
                    return new RotationProbe { inputRotation = Array(pose.bodyRotation),
                        hipsLocalRotation = Array(hips.localRotation),
                        hipsLocalPosition = Array(hips.localPosition) };
                }).ToArray();
                var positionProbes = new[] { Vector3.right, Vector3.up,
                    Vector3.forward }.Select(axis => {
                    var pose = new HumanPose {
                        bodyPosition = neutral.bodyPosition + 0.1f * axis,
                        bodyRotation = neutral.bodyRotation,
                        muscles = (float[])neutral.muscles.Clone() };
                    handler.SetHumanPose(ref pose);
                    return new PositionProbe { inputPosition = Array(pose.bodyPosition),
                        hipsLocalPosition = Array(hips.localPosition) };
                }).ToArray();
                var responses = new List<MuscleResponse>();
                for (int muscle = 0; muscle < HumanTrait.MuscleCount; muscle++)
                {
                    var pose = new HumanPose { bodyPosition = neutral.bodyPosition,
                        bodyRotation = neutral.bodyRotation,
                        muscles = (float[])neutral.muscles.Clone() };
                    pose.muscles[muscle] = Mathf.Clamp(neutral.muscles[muscle]
                        + 0.5f, -1f, 1f);
                    var plus = pose.muscles[muscle];
                    handler.SetHumanPose(ref pose);
                    var plusRig = Sparse(RigBones(animator), neutralByPath);
                    var plusHips = Array(hips.localPosition);
                    pose.muscles[muscle] = Mathf.Clamp(neutral.muscles[muscle]
                        - 0.5f, -1f, 1f);
                    var minus = pose.muscles[muscle];
                    handler.SetHumanPose(ref pose);
                    var minusRig = Sparse(RigBones(animator), neutralByPath);
                    var minusHips = Array(hips.localPosition);
                    pose.muscles[muscle] = 1f;
                    handler.SetHumanPose(ref pose);
                    var plusFullRig = Sparse(RigBones(animator), neutralByPath);
                    var plusFullHips = Array(hips.localPosition);
                    pose.muscles[muscle] = -1f;
                    handler.SetHumanPose(ref pose);
                    var minusFullRig = Sparse(RigBones(animator), neutralByPath);
                    responses.Add(new MuscleResponse { muscle = muscle,
                        plusValue = plus, minusValue = minus,
                        plusFullValue = 1f, minusFullValue = -1f,
                        plusRig = plusRig, minusRig = minusRig,
                        plusFullRig = plusFullRig,
                        minusFullRig = minusFullRig,
                        plusHipsPosition = plusHips,
                        minusHipsPosition = minusHips,
                        plusFullHipsPosition = plusFullHips,
                        minusFullHipsPosition = Array(hips.localPosition) });
                }
                handler.SetHumanPose(ref neutral);
                var avatar = animator.avatar;
                var flags = BindingFlags.Instance | BindingFlags.NonPublic;
                var type = typeof(Avatar);
                var pre = type.GetMethod("GetPreRotation", flags);
                var post = type.GetMethod("GetPostRotation", flags);
                var sign = type.GetMethod("GetLimitSign", flags);
                var axis = type.GetMethod("GetAxisLength", flags);
                if (pre == null || post == null || sign == null || axis == null)
                    throw new InvalidOperationException("Avatar calibration APIs unavailable");
                var parameters = Enumerable.Range(0, (int)HumanBodyBones.LastBone)
                    .Where(index => animator.GetBoneTransform((HumanBodyBones)index))
                    .Select(index => new BoneParameter {
                        index = index,
                        preRotation = Array((Quaternion)pre.Invoke(avatar,
                            new object[] { index })),
                        postRotation = Array((Quaternion)post.Invoke(avatar,
                            new object[] { index })),
                        limitSign = Array((Vector3)sign.Invoke(avatar,
                            new object[] { index })),
                        axisLength = (float)axis.Invoke(avatar,
                            new object[] { index }),
                        hierarchyMass = HumanTrait.GetBoneDefaultHierarchyMass(index),
                        parentBone = HumanTrait.GetParentBone(index)
                    }).ToArray();
                var traits = Enumerable.Range(0, HumanTrait.MuscleCount)
                    .Select(index => new MuscleTrait { muscle = index,
                        bone = HumanTrait.BoneFromMuscle(index),
                        dof = Enumerable.Range(0, 3).Where(dof =>
                            HumanTrait.MuscleFromBone(HumanTrait.BoneFromMuscle(index),
                                dof) == index).DefaultIfEmpty(-1).First(),
                        defaultMin = HumanTrait.GetMuscleDefaultMin(index),
                        defaultMax = HumanTrait.GetMuscleDefaultMax(index)
                    }).ToArray();
                return new Calibration { avatarId = id, name = avatar.name,
                    humanScale = animator.humanScale,
                    neutralMuscles = neutral.muscles,
                    neutralBodyPosition = Array(neutral.bodyPosition),
                    neutralBodyRotation = Array(neutral.bodyRotation),
                    neutralRigBones = rig, boneParameters = parameters,
                    muscleTraits = traits, bodyRotationProbes = rotationProbes,
                    bodyPositionProbes = positionProbes,
                    calibration = responses.ToArray() };
            }
        }
    }
}
