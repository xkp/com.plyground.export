// Unity-side asset bridge for the module Preview export. Keep the serialization
// format aligned with plythreejs/unity-runtime/unity-exporter.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Plyground.Preview.Editor
{
    public static class PlygroundPreviewAssetExporter
    {
        private static string exportOutputDirectory;
        private static HashSet<string> exportedPrefabIds;
        public static void ExportActiveScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded || string.IsNullOrEmpty(scene.path))
            {
                Debug.LogError("Open and save a Unity scene before exporting it.");
                return;
            }
            string path = EditorUtility.SaveFilePanel("Export Unity bridge source", Application.dataPath,
                scene.name + "-source", "json");
            if (string.IsNullOrEmpty(path)) return;

            ExportSceneToPath(path);
        }

        public static void ExportSceneToPath(string path)
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded || string.IsNullOrEmpty(scene.path))
                throw new InvalidOperationException("Open and save a Unity scene before exporting it.");

            var diagnostics = new List<object>();
            var roots = new List<object>();
            var assets = new List<object>();
            var scriptableAssetIds = new HashSet<string>();
            exportedPrefabIds = new HashSet<string>();
            var layers = new Dictionary<string, object>();
            for (int index = 0; index < 32; index++)
            {
                string name = LayerMask.LayerToName(index);
                if (!string.IsNullOrEmpty(name)) layers[name] = index;
            }
            string outputDirectory = Path.GetDirectoryName(Path.GetFullPath(path));
            exportOutputDirectory = outputDirectory;
            Directory.CreateDirectory(outputDirectory);
            foreach (GameObject root in scene.GetRootGameObjects())
                roots.Add(ExportObject(root, diagnostics, assets, scriptableAssetIds, outputDirectory));

            var source = Map(
                "format", "unity-bridge-source",
                "version", 1,
                "coordinateSystem", "unity",
                "sourceUnityVersion", Application.unityVersion,
                "assets", assets,
                "layers", layers,
                "scene", Map("name", scene.name, "roots", roots),
                "diagnostics", diagnostics);
            File.WriteAllText(path, Json(source), new UTF8Encoding(false));
            Debug.Log("Unity bridge source exported to " + path + ". Diagnostics: " + diagnostics.Count);
        }

        /** Export asset creator prefabs without requiring an active scene. */
        public static void ExportPrefabsToPath(string path, IEnumerable<GameObject> prefabs)
            => ExportPrefabsToPath(path, prefabs, Array.Empty<UnityEngine.Object>());

        /** Export prefabs plus assets addressed directly by a module controller. */
        public static void ExportPrefabsToPath(string path, IEnumerable<GameObject> prefabs,
            IEnumerable<UnityEngine.Object> additionalAssets)
        {
            var selected = new List<GameObject>();
            var seen = new HashSet<string>();
            foreach (var prefab in prefabs)
            {
                if (prefab == null || !PrefabUtility.IsPartOfPrefabAsset(prefab))
                    throw new InvalidOperationException("Preview prefab export requires saved prefab assets.");
                if (seen.Add(Id(prefab))) selected.Add(prefab);
            }
            string outputDirectory = Path.GetDirectoryName(Path.GetFullPath(path));
            Directory.CreateDirectory(outputDirectory);
            exportOutputDirectory = outputDirectory;
            exportedPrefabIds = new HashSet<string>(seen);
            var diagnostics = new List<object>();
            var assets = new List<object>();
            var scriptableAssetIds = new HashSet<string>();
            var assetPaths = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            var resources = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            var layers = new Dictionary<string, object>();
            for (int index = 0; index < 32; index++)
            {
                string name = LayerMask.LayerToName(index);
                if (!string.IsNullOrEmpty(name)) layers[name] = index;
            }
            foreach (var prefab in selected)
            {
                assets.Add(Map("id", Id(prefab), "kind", "prefab",
                    "root", ExportObject(prefab, diagnostics, assets, scriptableAssetIds, outputDirectory)));
                AddCatalogEntry(prefab, assetPaths, resources);
            }
            foreach (var asset in additionalAssets.Where(value => value != null))
            {
                string id = Id(asset);
                if (asset is GameObject gameObject && PrefabUtility.IsPartOfPrefabAsset(gameObject))
                {
                    if (exportedPrefabIds.Add(id))
                        assets.Add(Map("id", id, "kind", "prefab",
                            "root", ExportObject(gameObject, diagnostics, assets, scriptableAssetIds, outputDirectory)));
                }
                else if (asset is ScriptableObject scriptable)
                    ExportScriptableObject(scriptable, diagnostics, assets, scriptableAssetIds);
                else if (asset is PhysicsMaterial physicsMaterial)
                    ExportPhysicsMaterial(physicsMaterial, assets, scriptableAssetIds);
                else if (asset is AnimationClip animationClip)
                    ExportStandaloneAnimationClip(animationClip, diagnostics, assets, scriptableAssetIds);
                else if (asset is AnimatorController animatorController)
                    assets.Add(Map("id", id, "kind", "animatorController",
                        "graph", PreviewAnimatorGraphExporter.Export(animatorController)));
                else
                    diagnostics.Add("AssetDatabase asset needs Preview serialization: " +
                        AssetDatabase.GetAssetPath(asset) + " (" + asset.GetType().FullName + ")");
                AddCatalogEntry(asset, assetPaths, resources);
            }
            var source = Map("format", "unity-bridge-source", "version", 1,
                "coordinateSystem", "unity", "sourceUnityVersion", Application.unityVersion,
                "assets", assets, "scene", Map("name", "Module Prefabs", "roots", new List<object>()),
                "assetPaths", assetPaths, "resources", resources, "layers", layers,
                "diagnostics", diagnostics);
            File.WriteAllText(path, Json(source), new UTF8Encoding(false));
        }

        private static void AddCatalogEntry(UnityEngine.Object asset,
            Dictionary<string, object> assetPaths, Dictionary<string, object> resources)
        {
            string path = AssetDatabase.GetAssetPath(asset).Replace('\\', '/');
            if (string.IsNullOrEmpty(path)) return;
            assetPaths[path] = Id(asset);
            int marker = path.LastIndexOf("/Resources/", StringComparison.OrdinalIgnoreCase);
            if (marker < 0) return;
            string resourcePath = Path.ChangeExtension(path.Substring(marker + "/Resources/".Length), null)
                .Replace('\\', '/');
            if (resources.ContainsKey(resourcePath) && !Equals(resources[resourcePath], Id(asset)))
                throw new InvalidOperationException("Duplicate Preview Resources path: " + resourcePath);
            resources[resourcePath] = Id(asset);
        }

        public static string GetPreviewAssetId(UnityEngine.Object asset) => Id(asset);

        private static Dictionary<string, object> ExportObject(GameObject source, List<object> diagnostics,
            List<object> assets, HashSet<string> scriptableAssetIds, string outputDirectory)
        {
            var components = new List<object>();
            foreach (UnityEngine.Component component in source.GetComponents<UnityEngine.Component>())
            {
                if (component == null || component is Transform || component is MeshFilter
                    || component is MeshRenderer || component is SkinnedMeshRenderer
                    || component is Animation) continue;
                var exported = ExportComponent(component, diagnostics, assets, scriptableAssetIds);
                if (exported != null) components.Add(exported);
            }
            var children = new List<object>();
            foreach (Transform child in source.transform)
                children.Add(ExportObject(child.gameObject, diagnostics, assets, scriptableAssetIds, outputDirectory));
            Transform transform = source.transform;
            var result = Map(
                "id", Id(source), "name", source.name, "active", source.activeSelf,
                "tag", source.tag, "layer", source.layer,
                "transform", Map(
                    "id", Id(transform),
                    "position", Numbers(transform.localPosition.x, transform.localPosition.y, transform.localPosition.z),
                    "rotation", Numbers(transform.localRotation.x, transform.localRotation.y,
                        transform.localRotation.z, transform.localRotation.w),
                    "scale", Numbers(transform.localScale.x, transform.localScale.y, transform.localScale.z)),
                "components", components, "children", children);
            if (transform is RectTransform rect)
                result["rectTransform"] = Map(
                    "anchorMin", Numbers(rect.anchorMin.x, rect.anchorMin.y),
                    "anchorMax", Numbers(rect.anchorMax.x, rect.anchorMax.y),
                    "pivot", Numbers(rect.pivot.x, rect.pivot.y),
                    "sizeDelta", Numbers(rect.sizeDelta.x, rect.sizeDelta.y),
                    "anchoredPosition", Numbers(rect.anchoredPosition.x, rect.anchoredPosition.y));
            MeshFilter filter = source.GetComponent<MeshFilter>();
            MeshRenderer renderer = source.GetComponent<MeshRenderer>();
            if (filter != null && renderer != null && filter.sharedMesh != null)
            {
                try
                {
                    string id = Id(renderer);
                    string filename = HashId(id) + ".unitymesh.json";
                    string relativePath = "models/" + filename;
                    string target = Path.Combine(outputDirectory, "models", filename);
                    Directory.CreateDirectory(Path.GetDirectoryName(target));
                    File.WriteAllText(target, Json(ExportMesh(filter.sharedMesh,
                        renderer.sharedMaterials, diagnostics)), new UTF8Encoding(false));
                    assets.Add(Map("id", id, "kind", "model", "path", relativePath));
                    result["model"] = id;
                    components.Add(Map("id", Id(filter), "type", "UnityEngine.MeshFilter",
                        "enabled", true, "properties", Map()));
                    components.Add(Map("id", id, "type", "UnityEngine.MeshRenderer",
                        "enabled", renderer.enabled,
                        "properties", Map("castShadows", renderer.shadowCastingMode != UnityEngine.Rendering.ShadowCastingMode.Off,
                            "receiveShadows", renderer.receiveShadows)));
                }
                catch (Exception error)
                {
                    diagnostics.Add("Mesh conversion failed on " + source.name + ": " + error.Message);
                }
            }
            else if (filter != null || renderer != null)
                diagnostics.Add("MeshFilter/MeshRenderer pair missing on " + source.name);
            SkinnedMeshRenderer skin = source.GetComponent<SkinnedMeshRenderer>();
            if (skin != null && skin.sharedMesh != null)
            {
                try
                {
                    string id = Id(skin);
                    string filename = HashId(id) + ".unitymesh.json";
                    string relativePath = "models/" + filename;
                    string target = Path.Combine(outputDirectory, "models", filename);
                    Directory.CreateDirectory(Path.GetDirectoryName(target));
                    var model = ExportSkinnedMesh(skin, diagnostics);
                    Animation animation = source.GetComponent<Animation>();
                    if (animation != null && animation.clip != null)
                    {
                        model["animations"] = ExportAnimations(animation.clip, skin, diagnostics);
                        components.Add(Map("id", Id(animation), "type", "UnityEngine.Animator",
                            "enabled", animation.enabled,
                            "properties", Map("clips", Map("$asset", id),
                                "defaultClip", animation.clip.name)));
                    }
                    File.WriteAllText(target, Json(model), new UTF8Encoding(false));
                    assets.Add(Map("id", id, "kind", "model", "path", relativePath));
                    result["model"] = id;
                    var boneReferences = new List<object>();
                    foreach (Transform bone in skin.bones) boneReferences.Add(Map("$ref", Id(bone)));
                    components.Add(Map("id", id, "type", "UnityEngine.SkinnedMeshRenderer",
                        "enabled", skin.enabled,
                        "properties", Map("bones", boneReferences,
                            "rootBone", skin.rootBone == null ? null : Map("$ref", Id(skin.rootBone)),
                            "castShadows", skin.shadowCastingMode != UnityEngine.Rendering.ShadowCastingMode.Off,
                            "receiveShadows", skin.receiveShadows)));
                }
                catch (Exception error)
                {
                    diagnostics.Add("Skinned mesh conversion failed on " + source.name + ": " + error.Message);
                }
            }
            else if (skin != null) diagnostics.Add("Skinned mesh is missing on " + source.name);
            return result;
        }

        private static string HashId(string id)
        {
            using (SHA256 hash = SHA256.Create())
                return BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(id)))
                    .Replace("-", "").ToLowerInvariant();
        }

        private static Dictionary<string, object> ExportMesh(Mesh mesh, Material[] materials,
            List<object> diagnostics)
        {
            if (!mesh.isReadable) throw new InvalidOperationException("Mesh is not readable: " + mesh.name);
            var positions = new List<object>();
            foreach (Vector3 point in mesh.vertices)
            { positions.Add(point.x); positions.Add(point.y); positions.Add(point.z); }
            var normals = new List<object>();
            foreach (Vector3 normal in mesh.normals)
            { normals.Add(normal.x); normals.Add(normal.y); normals.Add(normal.z); }
            var uvs = new List<object>();
            foreach (Vector2 uv in mesh.uv)
            { uvs.Add(uv.x); uvs.Add(1f - uv.y); }
            var exportedMaterials = new List<object>();
            foreach (Material material in materials)
                exportedMaterials.Add(ExportMaterial(material, diagnostics));
            var submeshes = new List<object>();
            for (int index = 0; index < mesh.subMeshCount; index++)
            {
                if (mesh.GetTopology(index) != MeshTopology.Triangles)
                {
                    diagnostics.Add("Skipped non-triangle submesh on " + mesh.name);
                    continue;
                }
                var triangles = new List<object>();
                foreach (int triangle in mesh.GetIndices(index)) triangles.Add(triangle);
                submeshes.Add(Map("material", index < materials.Length ? index : -1,
                    "indices", triangles));
            }
            return Map("format", "unity-bridge-mesh", "version", 1,
                "meshId", Id(mesh), "positions", positions, "normals", normals,
                "uvs", uvs, "materials", exportedMaterials, "submeshes", submeshes);
        }

        private static Dictionary<string, object> ExportSkinnedMesh(SkinnedMeshRenderer skin,
            List<object> diagnostics)
        {
            Mesh mesh = skin.sharedMesh;
            Transform[] bones = skin.bones;
            BoneWeight[] weights = mesh.boneWeights;
            Matrix4x4[] bindposes = mesh.bindposes;
            if (bones.Length == 0 || weights.Length != mesh.vertexCount || bindposes.Length != bones.Length)
                throw new InvalidOperationException("Bones, weights, and bindposes do not match the mesh");
            var model = ExportMesh(mesh, skin.sharedMaterials, diagnostics);
            var joints = new List<object>();
            var influences = new List<object>();
            foreach (BoneWeight weight in weights)
            {
                joints.Add(weight.boneIndex0); joints.Add(weight.boneIndex1);
                joints.Add(weight.boneIndex2); joints.Add(weight.boneIndex3);
                influences.Add(weight.weight0); influences.Add(weight.weight1);
                influences.Add(weight.weight2); influences.Add(weight.weight3);
            }
            var exportedBones = new List<object>();
            var inverseBindMatrices = new List<object>();
            for (int index = 0; index < bones.Length; index++)
            {
                Transform bone = bones[index];
                if (bone == null) throw new InvalidOperationException("Missing bone at index " + index);
                int parent = Array.IndexOf(bones, bone.parent);
                Matrix4x4 local = parent >= 0 ? bones[parent].worldToLocalMatrix * bone.localToWorldMatrix
                    : skin.transform.worldToLocalMatrix * bone.localToWorldMatrix;
                Vector3 translation = local.GetColumn(3);
                Quaternion rotation = local.rotation;
                Vector3 scale = local.lossyScale;
                exportedBones.Add(Map("name", bone.name, "path", BonePath(bone, skin.transform),
                    "parent", parent,
                    "translation", Numbers(translation.x, translation.y, translation.z),
                    "rotation", Numbers(rotation.x, rotation.y, rotation.z, rotation.w),
                    "scale", Numbers(scale.x, scale.y, scale.z)));
                Matrix4x4 inverse = bindposes[index];
                for (int column = 0; column < 4; column++)
                    for (int row = 0; row < 4; row++) inverseBindMatrices.Add(inverse[row, column]);
            }
            model["skin"] = Map("bones", exportedBones, "joints", joints,
                "weights", influences, "inverseBindMatrices", inverseBindMatrices);
            return model;
        }

        private static string BonePath(Transform bone, Transform root)
        {
            var segments = new List<string>();
            Transform current = bone;
            while (current != null && current != root)
            {
                segments.Insert(0, current.name);
                current = current.parent;
            }
            // Humanoid prefab renderers often sit beside the skeleton, so their
            // bones have a common ancestor rather than being descendants here.
            // The runtime links them by exported Transform ID, and this path is
            // only metadata for the generated GLB.
            return string.Join("/", segments.ToArray());
        }

        private static List<object> ExportAnimations(AnimationClip clip, SkinnedMeshRenderer skin,
            List<object> diagnostics)
        {
            var curves = new Dictionary<string, AnimationCurve>();
            foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(clip))
            {
                if (binding.type != typeof(Transform))
                {
                    diagnostics.Add("Unsupported animation binding: " + binding.path + "." + binding.propertyName);
                    continue;
                }
                curves[binding.path + "|" + binding.propertyName] = AnimationUtility.GetEditorCurve(clip, binding);
            }
            int steps = Math.Max(1, Math.Min(3000, (int)Math.Ceiling(clip.length * 30f)));
            var times = new List<object>();
            for (int step = 0; step <= steps; step++) times.Add(clip.length * step / steps);
            var tracks = new List<object>();
            for (int index = 0; index < skin.bones.Length; index++)
            {
                Transform bone = skin.bones[index];
                string path = BonePath(bone, skin.transform);
                string prefix = path + "|";
                bool position = HasCurve(curves, prefix, "m_LocalPosition.");
                bool rotation = HasCurve(curves, prefix, "m_LocalRotation.")
                    || HasCurve(curves, prefix, "localEulerAnglesRaw.");
                bool scale = HasCurve(curves, prefix, "m_LocalScale.");
                var translations = new List<object>();
                var rotations = new List<object>();
                var scales = new List<object>();
                foreach (object item in times)
                {
                    float time = (float)item;
                    Vector3 p = bone.localPosition;
                    Vector3 s = bone.localScale;
                    Quaternion q = bone.localRotation;
                    if (position)
                    {
                        translations.Add(Eval(curves, prefix + "m_LocalPosition.x", time, p.x));
                        translations.Add(Eval(curves, prefix + "m_LocalPosition.y", time, p.y));
                        translations.Add(Eval(curves, prefix + "m_LocalPosition.z", time, p.z));
                    }
                    if (rotation)
                    {
                        if (HasCurve(curves, prefix, "m_LocalRotation."))
                            q = new Quaternion(Eval(curves, prefix + "m_LocalRotation.x", time, q.x),
                                Eval(curves, prefix + "m_LocalRotation.y", time, q.y),
                                Eval(curves, prefix + "m_LocalRotation.z", time, q.z),
                                Eval(curves, prefix + "m_LocalRotation.w", time, q.w)).normalized;
                        else
                        {
                            Vector3 euler = bone.localEulerAngles;
                            q = Quaternion.Euler(Eval(curves, prefix + "localEulerAnglesRaw.x", time, euler.x),
                                Eval(curves, prefix + "localEulerAnglesRaw.y", time, euler.y),
                                Eval(curves, prefix + "localEulerAnglesRaw.z", time, euler.z));
                        }
                        rotations.Add(q.x); rotations.Add(q.y); rotations.Add(q.z); rotations.Add(q.w);
                    }
                    if (scale)
                    {
                        scales.Add(Eval(curves, prefix + "m_LocalScale.x", time, s.x));
                        scales.Add(Eval(curves, prefix + "m_LocalScale.y", time, s.y));
                        scales.Add(Eval(curves, prefix + "m_LocalScale.z", time, s.z));
                    }
                }
                if (position) tracks.Add(Map("bone", index, "path", "translation", "times", times,
                    "values", translations));
                if (rotation) tracks.Add(Map("bone", index, "path", "rotation", "times", times,
                    "values", rotations));
                if (scale) tracks.Add(Map("bone", index, "path", "scale", "times", times,
                    "values", scales));
            }
            return new List<object> { Map("name", clip.name, "tracks", tracks) };
        }

        private static bool HasCurve(Dictionary<string, AnimationCurve> curves, string prefix, string property)
        {
            foreach (string key in curves.Keys)
                if (key.StartsWith(prefix + property, StringComparison.Ordinal)) return true;
            return false;
        }

        private static float Eval(Dictionary<string, AnimationCurve> curves, string key, float time, float fallback)
        {
            AnimationCurve curve;
            return curves.TryGetValue(key, out curve) ? curve.Evaluate(time) : fallback;
        }

        private static Dictionary<string, object> ExportMaterial(Material material, List<object> diagnostics)
        {
            if (material == null) return Map("id", "", "color", Numbers(1, 1, 1, 1));
            Color color = material.HasProperty("_BaseColor") ? material.GetColor("_BaseColor")
                : material.HasProperty("_Color") ? material.GetColor("_Color") : Color.white;
            var result = Map("id", Id(material), "name", material.name,
                "color", Numbers(color.r, color.g, color.b, color.a),
                "metallic", material.HasProperty("_Metallic") ? material.GetFloat("_Metallic") : 0f,
                "roughness", 1f - (material.HasProperty("_Smoothness")
                    ? material.GetFloat("_Smoothness") : material.HasProperty("_Glossiness")
                    ? material.GetFloat("_Glossiness") : 0f),
                "doubleSided", material.HasProperty("_Cull") && material.GetFloat("_Cull") == 0f);
            if (material.HasProperty("_EmissionColor"))
            {
                Color emission = material.GetColor("_EmissionColor");
                if (emission.maxColorComponent > 0f)
                    result["emissive"] = Numbers(emission.r, emission.g, emission.b);
            }
            if (material.IsKeywordEnabled("_ALPHATEST_ON"))
            {
                result["alphaMode"] = "MASK";
                result["alphaCutoff"] = material.HasProperty("_Cutoff") ? material.GetFloat("_Cutoff") : 0.5f;
            }
            else if (material.IsKeywordEnabled("_ALPHABLEND_ON")
                || material.IsKeywordEnabled("_ALPHAPREMULTIPLY_ON") || color.a < 1f)
                result["alphaMode"] = "BLEND";
            if (material.HasProperty("_BumpMap") && material.GetTexture("_BumpMap") != null)
                diagnostics.Add("Normal map needs tangent-space conversion: " + material.name);
            if (material.HasProperty("_MetallicGlossMap") && material.GetTexture("_MetallicGlossMap") != null)
                diagnostics.Add("Metallic/smoothness texture needs channel conversion: " + material.name);
            Texture texture = material.HasProperty("_BaseMap") ? material.GetTexture("_BaseMap")
                : material.mainTexture;
            if (texture is Texture2D image)
            {
                try
                {
                    result["textureId"] = Id(image);
                    result["texturePng"] = Convert.ToBase64String(ReadTexturePng(image));
                }
                catch (Exception error)
                {
                    diagnostics.Add("Texture conversion failed for " + material.name + ": " + error.Message);
                }
            }
            else if (texture != null) diagnostics.Add("Unsupported texture on " + material.name);
            if (material.shader != null && material.shader.name != "Standard"
                && material.shader.name != "Universal Render Pipeline/Lit")
                diagnostics.Add("Material shader approximated: " + material.name + " (" + material.shader.name + ")");
            return result;
        }

        private static byte[] ReadTexturePng(Texture2D image)
        {
            string path = AssetDatabase.GetAssetPath(image);
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
            {
                string extension = Path.GetExtension(path).ToLowerInvariant();
                if (extension == ".png") return File.ReadAllBytes(path);
                if (extension == ".jpg" || extension == ".jpeg")
                {
                    var decoded = new Texture2D(2, 2);
                    try
                    {
                        if (!ImageConversion.LoadImage(decoded, File.ReadAllBytes(path)))
                            throw new InvalidOperationException("Could not decode " + path);
                        return decoded.EncodeToPNG();
                    }
                    finally { UnityEngine.Object.DestroyImmediate(decoded); }
                }
            }
            if (image.isReadable) return image.EncodeToPNG();
            throw new InvalidOperationException("Texture has no readable PNG/JPEG source: " + image.name);
        }

        private static Dictionary<string, object> ExportComponent(UnityEngine.Component source,
            List<object> diagnostics, List<object> assets, HashSet<string> scriptableAssetIds)
        {
            string type = null;
            var properties = new Dictionary<string, object>();
            if (source is Camera camera)
            {
                type = "UnityEngine.Camera";
                properties["fieldOfView"] = camera.fieldOfView;
                properties["nearClipPlane"] = camera.nearClipPlane;
                properties["farClipPlane"] = camera.farClipPlane;
            }
            else if (source is Light light)
            {
                type = "UnityEngine.Light";
                properties["type"] = light.type == LightType.Point ? "Point" : "Directional";
                properties["color"] = Convert.ToInt32(ColorUtility.ToHtmlStringRGB(light.color), 16);
                properties["intensity"] = light.intensity;
                properties["range"] = light.range;
                if (light.type != LightType.Point && light.type != LightType.Directional)
                    diagnostics.Add("Light type approximated: " + source.name);
            }
            else if (source is Rigidbody body)
            {
                type = "UnityEngine.Rigidbody";
                properties["mass"] = body.mass;
                properties["useGravity"] = body.useGravity;
                properties["isKinematic"] = body.isKinematic;
                properties["drag"] = body.drag;
                properties["constraints"] = (int)body.constraints;
            }
            else if (source is Animator animator)
            {
                type = "UnityEngine.Animator";
                properties["applyRootMotion"] = animator.applyRootMotion;
                var bones = new Dictionary<string, object>();
                if (animator.isHuman)
                {
                    for (int bone = 0; bone < (int)HumanBodyBones.LastBone; bone++)
                    {
                        Transform transform = animator.GetBoneTransform((HumanBodyBones)bone);
                        if (transform != null) bones[bone.ToString(CultureInfo.InvariantCulture)] = Map("$ref", Id(transform));
                    }
                }
                properties["humanBones"] = bones;
                if (animator.runtimeAnimatorController is AnimatorController controller)
                {
                    var layers = new List<object>();
                    foreach (AnimatorControllerLayer layer in controller.layers)
                    {
                        var names = new List<object>();
                        CollectAnimatorStates(layer.stateMachine, names);
                        layers.Add(Map("name", layer.name, "states", names));
                    }
                    properties["controllerLayers"] = layers;
                    properties["controllerGraph"] = PreviewAnimatorGraphExporter.Export(controller);
                }
                else if (animator.runtimeAnimatorController != null)
                    diagnostics.Add("Animator controller clips and transitions not converted on " + source.gameObject.name
                        + ": " + animator.runtimeAnimatorController.name);
            }
            else if (source is AudioSource audio)
            {
                type = "UnityEngine.AudioSource";
                properties["pitch"] = audio.pitch;
                properties["volume"] = audio.volume;
                properties["loop"] = audio.loop;
                properties["playOnAwake"] = audio.playOnAwake;
                properties["spatialBlend"] = audio.spatialBlend;
                properties["minDistance"] = audio.minDistance;
                properties["maxDistance"] = audio.maxDistance;
                if (audio.clip != null) properties["clip"] = ExportAudioClip(audio.clip,
                    diagnostics, assets, scriptableAssetIds);
            }
            else if (source is ParticleSystem particle)
            {
                type = "UnityEngine.ParticleSystem";
                var main = particle.main;
                var emission = particle.emission;
                properties["startLifetime"] = main.startLifetime.constant;
                properties["startSpeed"] = main.startSpeed.constant;
                properties["startSize"] = main.startSize.constant;
                properties["gravityModifier"] = main.gravityModifier.constant;
                properties["rateOverTime"] = emission.enabled ? emission.rateOverTime.constant : 0f;
                properties["loop"] = main.loop;
                properties["playOnAwake"] = main.playOnAwake;
                properties["maxParticles"] = main.maxParticles;
                Color tint = main.startColor.color;
                properties["startColor"] = Map("$color", Numbers(tint.r, tint.g, tint.b, tint.a));
                if (main.startLifetime.mode != ParticleSystemCurveMode.Constant
                    || main.startSpeed.mode != ParticleSystemCurveMode.Constant
                    || main.startSize.mode != ParticleSystemCurveMode.Constant)
                    diagnostics.Add("Particle curves approximated on " + source.gameObject.name);
            }
            else if (source is Canvas canvas)
            {
                type = "UnityEngine.Canvas";
                properties["sortingOrder"] = canvas.sortingOrder;
                properties["renderMode"] = (int)canvas.renderMode;
            }
            else if (source is UnityEngine.UI.Image image)
            {
                type = "UnityEngine.UI.Image";
                properties["color"] = Map("$color", Numbers(image.color.r, image.color.g,
                    image.color.b, image.color.a));
                if (image.sprite != null) diagnostics.Add("UI sprite not converted on " + source.gameObject.name);
            }
            else if (source is SpriteRenderer spriteRenderer)
            {
                type = "UnityEngine.SpriteRenderer";
                properties["color"] = Map("$color", Numbers(spriteRenderer.color.r,
                    spriteRenderer.color.g, spriteRenderer.color.b, spriteRenderer.color.a));
                properties["sortingOrder"] = spriteRenderer.sortingOrder;
                if (spriteRenderer.sprite != null)
                    diagnostics.Add("Sprite texture not converted on " + source.gameObject.name);
            }
            else if (source is UnityEngine.UI.Text text)
            {
                type = "UnityEngine.UI.Text";
                properties["text"] = text.text;
                properties["fontSize"] = text.fontSize;
                properties["color"] = Map("$color", Numbers(text.color.r, text.color.g,
                    text.color.b, text.color.a));
            }
            else if (source is UnityEngine.UI.Button button)
            {
                type = "UnityEngine.UI.Button";
                properties["interactable"] = button.interactable;
                var serialized = new SerializedObject(button);
                var onClick = serialized.FindProperty("m_OnClick");
                object click;
                if (onClick != null && TrySerializeField(onClick, diagnostics, assets,
                    scriptableAssetIds, out click)) properties["onClick"] = click;
            }
            else if (source is BoxCollider box)
            {
                type = "UnityEngine.BoxCollider";
                properties["center"] = Vec3(box.center);
                properties["size"] = Vec3(box.size);
                properties["isTrigger"] = box.isTrigger;
            }
            else if (source is SphereCollider sphere)
            {
                type = "UnityEngine.SphereCollider";
                properties["center"] = Vec3(sphere.center);
                properties["radius"] = sphere.radius;
                properties["isTrigger"] = sphere.isTrigger;
            }
            else if (source is CapsuleCollider capsule)
            {
                type = "UnityEngine.CapsuleCollider";
                properties["center"] = Vec3(capsule.center);
                properties["radius"] = capsule.radius;
                properties["height"] = capsule.height;
                properties["isTrigger"] = capsule.isTrigger;
                if (capsule.direction != 1)
                    diagnostics.Add("Capsule direction needs manual conversion: " + source.name);
            }
            else if (source is WheelCollider wheel)
            {
                type = "UnityEngine.WheelCollider";
                properties["center"] = Vec3(wheel.center);
                properties["radius"] = wheel.radius;
                properties["suspensionDistance"] = wheel.suspensionDistance;
                properties["mass"] = wheel.mass;
                properties["wheelDampingRate"] = wheel.wheelDampingRate;
                properties["forceAppPointDistance"] = wheel.forceAppPointDistance;
                properties["isTrigger"] = wheel.isTrigger;
            }
            else if (source is MonoBehaviour script)
            {
                type = source.GetType().FullName;
                ExportScriptFields(script, properties, diagnostics, assets, scriptableAssetIds);
            }
            else
            {
                diagnostics.Add("Skipped component " + source.GetType().FullName + " on " + source.gameObject.name);
                return null;
            }
            if (source is Collider collider && collider.sharedMaterial != null)
                properties["material"] = ExportPhysicsMaterial(collider.sharedMaterial, assets, scriptableAssetIds);
            bool enabled = !(source is Behaviour behaviour) || behaviour.enabled;
            return Map("id", Id(source), "type", type, "enabled", enabled, "properties", properties);
        }

        private static void CollectAnimatorStates(AnimatorStateMachine machine, List<object> names)
        {
            foreach (ChildAnimatorState state in machine.states) names.Add(state.state.name);
            foreach (ChildAnimatorStateMachine child in machine.stateMachines)
                CollectAnimatorStates(child.stateMachine, names);
        }

        private static void ExportScriptFields(UnityEngine.Object script,
            Dictionary<string, object> properties, List<object> diagnostics,
            List<object> assets, HashSet<string> scriptableAssetIds)
        {
            var serialized = new SerializedObject(script);
            SerializedProperty field = serialized.GetIterator();
            bool enterChildren = true;
            while (field.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (field.depth != 0 || field.name.StartsWith("m_", StringComparison.Ordinal)) continue;
                object value;
                if (TrySerializeField(field, diagnostics, assets, scriptableAssetIds, out value))
                    properties[field.name] = value;
            }
        }

        private static bool TrySerializeField(SerializedProperty field, List<object> diagnostics,
            List<object> assets, HashSet<string> scriptableAssetIds, out object value)
        {
            value = null;
            switch (field.propertyType)
            {
                case SerializedPropertyType.Integer: value = field.intValue; return true;
                case SerializedPropertyType.Boolean: value = field.boolValue; return true;
                case SerializedPropertyType.Float: value = field.floatValue; return true;
                case SerializedPropertyType.String: value = field.stringValue; return true;
                case SerializedPropertyType.Enum: value = field.enumValueIndex; return true;
                case SerializedPropertyType.LayerMask:
                    value = Map("$layerMask", field.intValue); return true;
                case SerializedPropertyType.Vector2:
                    value = Map("$vector2", Numbers(field.vector2Value.x, field.vector2Value.y)); return true;
                case SerializedPropertyType.Vector3: value = Vec3(field.vector3Value); return true;
                case SerializedPropertyType.Color:
                    Color color = field.colorValue;
                    value = Map("$color", Numbers(color.r, color.g, color.b)); return true;
                case SerializedPropertyType.AnimationCurve:
                    var keys = new List<object>();
                    foreach (Keyframe key in field.animationCurveValue.keys)
                        keys.Add(Map("time", key.time, "value", key.value,
                            "inTangent", key.inTangent, "outTangent", key.outTangent));
                    value = Map("$animationCurve", keys); return true;
                case SerializedPropertyType.ObjectReference:
                    UnityEngine.Object reference = field.objectReferenceValue;
                    if (reference == null) return true;
                    string ownerPath = AssetDatabase.GetAssetPath(field.serializedObject.targetObject);
                    string referencePath = AssetDatabase.GetAssetPath(reference);
                    bool samePrefab = !string.IsNullOrEmpty(ownerPath)
                        && string.Equals(ownerPath, referencePath, StringComparison.OrdinalIgnoreCase);
                    if (reference is GameObject gameObject
                            && (gameObject.scene == SceneManager.GetActiveScene() || samePrefab)
                        || reference is UnityEngine.Component component
                            && (component.gameObject.scene == SceneManager.GetActiveScene() || samePrefab))
                    {
                        value = Map("$ref", Id(reference));
                        return true;
                    }
                    if (reference is GameObject prefab && PrefabUtility.IsPartOfPrefabAsset(prefab))
                    {
                        string id = Id(prefab);
                        if (exportedPrefabIds.Add(id))
                            assets.Add(Map("id", id, "kind", "prefab",
                                "root", ExportObject(prefab, diagnostics, assets,
                                    scriptableAssetIds, exportOutputDirectory)));
                        value = Map("$asset", id);
                        return true;
                    }
                    if (reference is ScriptableObject scriptable)
                    {
                        string id = Id(scriptable);
                        ExportScriptableObject(scriptable, diagnostics, assets, scriptableAssetIds);
                        value = Map("$asset", id);
                        return true;
                    }
                    if (reference is AudioClip)
                    {
                        value = ExportAudioClip((AudioClip)reference, diagnostics, assets, scriptableAssetIds);
                        return true;
                    }
                    if (reference is PhysicsMaterial physicsMaterial)
                    {
                        value = ExportPhysicsMaterial(physicsMaterial, assets, scriptableAssetIds);
                        return true;
                    }
                    if (reference is AnimationClip animationClip)
                    {
                        value = ExportStandaloneAnimationClip(animationClip, diagnostics, assets, scriptableAssetIds);
                        return true;
                    }
                    break;
                case SerializedPropertyType.Generic:
                    Type fieldType = SerializedFieldType(field);
                    if (fieldType != null && typeof(UnityEngine.Events.UnityEventBase).IsAssignableFrom(fieldType))
                    {
                        SerializedProperty calls = field.FindPropertyRelative("m_PersistentCalls.m_Calls");
                        var listeners = new List<object>();
                        if (calls != null) for (int i = 0; i < calls.arraySize; i++)
                        {
                            var call = calls.GetArrayElementAtIndex(i);
                            var target = call.FindPropertyRelative("m_Target");
                            var method = call.FindPropertyRelative("m_MethodName");
                            var mode = call.FindPropertyRelative("m_Mode");
                            var state = call.FindPropertyRelative("m_CallState");
                            if (state != null && state.enumValueIndex == 0) continue;
                            object targetValue;
                            if (target == null || method == null || string.IsNullOrEmpty(method.stringValue)
                                || !TrySerializeField(target, diagnostics, assets, scriptableAssetIds, out targetValue)
                                || targetValue == null)
                            {
                                diagnostics.Add("Unresolved persistent UnityEvent call: " + field.propertyPath);
                                continue;
                            }
                            object arguments = null;
                            var argument = call.FindPropertyRelative("m_Arguments");
                            switch (mode == null ? 0 : mode.enumValueIndex)
                            {
                                case 0: break; // EventDefined: forward Invoke arguments.
                                case 1: arguments = new List<object>(); break;
                                case 3: arguments = new List<object> { argument.FindPropertyRelative("m_IntArgument").intValue }; break;
                                case 4: arguments = new List<object> { argument.FindPropertyRelative("m_FloatArgument").floatValue }; break;
                                case 5: arguments = new List<object> { argument.FindPropertyRelative("m_StringArgument").stringValue }; break;
                                case 6: arguments = new List<object> { argument.FindPropertyRelative("m_BoolArgument").boolValue }; break;
                                default:
                                    diagnostics.Add("Unsupported persistent UnityEvent argument: " + field.propertyPath);
                                    continue;
                            }
                            listeners.Add(Map("target", targetValue, "method", method.stringValue,
                                "args", arguments));
                        }
                        value = Map("$unityEvent", listeners);
                        return true;
                    }
                    if (field.isArray)
                    {
                        var items = new List<object>();
                        for (int i = 0; i < field.arraySize; i++)
                        {
                            object item;
                            if (TrySerializeField(field.GetArrayElementAtIndex(i), diagnostics,
                                assets, scriptableAssetIds, out item)) items.Add(item);
                        }
                        value = items;
                        return true;
                    }
                    var entries = new Dictionary<string, object>();
                    SerializedProperty cursor = field.Copy();
                    SerializedProperty end = field.GetEndProperty();
                    int childDepth = field.depth + 1;
                    if (cursor.NextVisible(true))
                    {
                        do
                        {
                            if (SerializedProperty.EqualContents(cursor, end)) break;
                            if (cursor.depth != childDepth) continue;
                            object child;
                            if (TrySerializeField(cursor, diagnostics, assets, scriptableAssetIds, out child))
                                entries[cursor.name] = child;
                        } while (cursor.NextVisible(false));
                    }
                    if (field.type == "InputAction" || entries.ContainsKey("m_SingletonActionBindings"))
                        value = Map("$inputAction", entries);
                    else
                    {
                        Type declaredType = SerializedFieldType(field);
                        string typeName = declaredType == null ? null
                            : declaredType.FullName.Replace('+', '.');
                        value = typeName != null && !typeName.StartsWith("UnityEngine.", StringComparison.Ordinal)
                            && !typeName.StartsWith("System.", StringComparison.Ordinal)
                            ? Map("$serializedType", typeName, "properties", entries) : (object)entries;
                    }
                    return true;
            }
            diagnostics.Add("Unsupported field: " + field.serializedObject.targetObject.GetType().FullName
                + "." + field.propertyPath + " (" + field.propertyType + ")");
            return false;
        }

        private static Type SerializedFieldType(SerializedProperty field)
        {
            Type current = field.serializedObject.targetObject.GetType();
            foreach (string segment in field.propertyPath.Split('.'))
            {
                if (segment == "Array") continue;
                if (segment.StartsWith("data[", StringComparison.Ordinal))
                {
                    current = current.IsArray ? current.GetElementType()
                        : current.IsGenericType ? current.GetGenericArguments()[0] : null;
                    continue;
                }
                if (current == null) return null;
                FieldInfo info = null;
                for (Type owner = current; owner != null && info == null; owner = owner.BaseType)
                    info = owner.GetField(segment, BindingFlags.Instance | BindingFlags.Public
                        | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                current = info == null ? null : info.FieldType;
            }
            return current;
        }

        private static void ExportScriptableObject(ScriptableObject source, List<object> diagnostics,
            List<object> assets, HashSet<string> scriptableAssetIds)
        {
            string id = Id(source);
            if (!scriptableAssetIds.Add(id)) return;
            var properties = new Dictionary<string, object>();
            var record = Map("id", id, "kind", "scriptableObject", "type", source.GetType().FullName,
                "name", source.name, "properties", properties);
            assets.Add(record);
            ExportScriptFields(source, properties, diagnostics, assets, scriptableAssetIds);
        }

        private static string Id(UnityEngine.Object value)
        {
            return GlobalObjectId.GetGlobalObjectIdSlow(value).ToString();
        }

        private static Dictionary<string, object> Vec3(Vector3 value)
        {
            return Map("$vector3", Numbers(value.x, value.y, value.z));
        }

        private static List<object> Numbers(params float[] values)
        {
            var result = new List<object>();
            foreach (float value in values) result.Add(value);
            return result;
        }

        private static object ExportAudioClip(AudioClip clip, List<object> diagnostics,
            List<object> assets, HashSet<string> assetIds)
        {
            string id = Id(clip);
            if (assetIds.Contains(id)) return Map("$asset", id);
            string source = AssetDatabase.GetAssetPath(clip);
            string extension = Path.GetExtension(source).ToLowerInvariant();
            if (extension != ".wav" && extension != ".ogg" && extension != ".mp3")
            {
                diagnostics.Add("Audio clip source needs conversion: " + clip.name + " (" + source + ")");
                return null;
            }
            string targetName = HashId(id) + extension;
            string target = Path.Combine(exportOutputDirectory, "audio", targetName);
            Directory.CreateDirectory(Path.GetDirectoryName(target));
            string sourceFile = Path.Combine(Directory.GetParent(Application.dataPath).FullName, source);
            File.Copy(sourceFile, target, true);
            assets.Add(Map("id", id, "kind", "audio", "path", "audio/" + targetName));
            assetIds.Add(id);
            return Map("$asset", id);
        }

        private static object ExportPhysicsMaterial(PhysicsMaterial material,
            List<object> assets, HashSet<string> assetIds)
        {
            string id = Id(material);
            if (assetIds.Add(id))
                assets.Add(Map("id", id, "kind", "physicsMaterial", "name", material.name,
                    "dynamicFriction", material.dynamicFriction,
                    "staticFriction", material.staticFriction,
                    "bounciness", material.bounciness,
                    "frictionCombine", material.frictionCombine.ToString(),
                    "bounceCombine", material.bounceCombine.ToString()));
            return Map("$asset", id);
        }

        private static object ExportStandaloneAnimationClip(AnimationClip clip,
            List<object> diagnostics, List<object> assets, HashSet<string> assetIds)
        {
            string id = Id(clip);
            if (!assetIds.Add(id)) return Map("$asset", id);
            var curves = new Dictionary<string, AnimationCurve>();
            foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(clip))
            {
                string property = binding.propertyName;
                if (binding.type != typeof(Transform)
                    || !(property.StartsWith("m_LocalPosition.", StringComparison.Ordinal)
                        || property.StartsWith("m_LocalRotation.", StringComparison.Ordinal)
                        || property.StartsWith("m_LocalScale.", StringComparison.Ordinal)
                        || property.StartsWith("localEulerAnglesRaw.", StringComparison.Ordinal)))
                {
                    diagnostics.Add("Unsupported standalone animation binding: " + clip.name + " "
                        + binding.path + "." + property);
                    continue;
                }
                curves[binding.path + "|" + property] = AnimationUtility.GetEditorCurve(clip, binding);
            }
            foreach (EditorCurveBinding binding in AnimationUtility.GetObjectReferenceCurveBindings(clip))
                diagnostics.Add("Unsupported object animation binding: " + clip.name + " "
                    + binding.path + "." + binding.propertyName);
            if (clip.length <= 0f)
                diagnostics.Add("Standalone animation has no duration: " + clip.name);
            int steps = Math.Max(1, Math.Min(3000, (int)Math.Ceiling(clip.length * 30f)));
            var times = new List<object>();
            for (int step = 0; step <= steps; step++) times.Add(clip.length * step / steps);
            var tracks = new List<object>();
            var targets = new HashSet<string>(StringComparer.Ordinal);
            foreach (string path in curves.Keys.Select(key => key.Substring(0, key.IndexOf('|'))).Distinct())
            {
                string prefix = path + "|";
                string target = path.Length == 0 ? "" : path.Split('/').Last();
                if (!targets.Add(target))
                    diagnostics.Add("Standalone animation has duplicate target name: " + clip.name + " " + target);
                bool position = HasCurve(curves, prefix, "m_LocalPosition.");
                bool rotation = HasCurve(curves, prefix, "m_LocalRotation.");
                bool euler = HasCurve(curves, prefix, "localEulerAnglesRaw.");
                bool scale = HasCurve(curves, prefix, "m_LocalScale.");
                if (rotation && euler)
                    diagnostics.Add("Standalone animation has quaternion and Euler curves: " + clip.name + " " + path);
                if (position || scale || rotation || euler)
                {
                    var positions = new List<object>();
                    var rotations = new List<object>();
                    var scales = new List<object>();
                    foreach (object item in times)
                    {
                        float time = (float)item;
                        if (position)
                        {
                            positions.Add(Eval(curves, prefix + "m_LocalPosition.x", time, 0f));
                            positions.Add(Eval(curves, prefix + "m_LocalPosition.y", time, 0f));
                            positions.Add(Eval(curves, prefix + "m_LocalPosition.z", time, 0f));
                        }
                        if (rotation || euler)
                        {
                            Quaternion q = rotation
                                ? new Quaternion(Eval(curves, prefix + "m_LocalRotation.x", time, 0f),
                                    Eval(curves, prefix + "m_LocalRotation.y", time, 0f),
                                    Eval(curves, prefix + "m_LocalRotation.z", time, 0f),
                                    Eval(curves, prefix + "m_LocalRotation.w", time, 1f)).normalized
                                : Quaternion.Euler(Eval(curves, prefix + "localEulerAnglesRaw.x", time, 0f),
                                    Eval(curves, prefix + "localEulerAnglesRaw.y", time, 0f),
                                    Eval(curves, prefix + "localEulerAnglesRaw.z", time, 0f));
                            rotations.Add(q.x); rotations.Add(q.y); rotations.Add(q.z); rotations.Add(q.w);
                        }
                        if (scale)
                        {
                            scales.Add(Eval(curves, prefix + "m_LocalScale.x", time, 1f));
                            scales.Add(Eval(curves, prefix + "m_LocalScale.y", time, 1f));
                            scales.Add(Eval(curves, prefix + "m_LocalScale.z", time, 1f));
                        }
                    }
                    if (position) tracks.Add(Map("target", target, "property", "position", "times", times,
                        "values", positions));
                    if (rotation || euler) tracks.Add(Map("target", target, "property", "quaternion", "times", times,
                        "values", rotations));
                    if (scale) tracks.Add(Map("target", target, "property", "scale", "times", times,
                        "values", scales));
                }
            }
            assets.Add(Map("id", id, "kind", "animationClip", "name", clip.name,
                "duration", clip.length, "tracks", tracks));
            return Map("$asset", id);
        }

        private static Dictionary<string, object> Map(params object[] entries)
        {
            var result = new Dictionary<string, object>();
            for (int i = 0; i < entries.Length; i += 2) result[(string)entries[i]] = entries[i + 1];
            return result;
        }

        private static string Json(object value)
        {
            var result = new StringBuilder();
            AppendJson(result, value);
            return result.ToString();
        }

        private static void AppendJson(StringBuilder result, object value)
        {
            if (value == null) { result.Append("null"); return; }
            if (value is string text) { AppendString(result, text); return; }
            if (value is bool boolean) { result.Append(boolean ? "true" : "false"); return; }
            if (value is Dictionary<string, object> map)
            {
                result.Append('{');
                bool first = true;
                foreach (var pair in map)
                {
                    if (!first) result.Append(',');
                    first = false;
                    AppendString(result, pair.Key);
                    result.Append(':');
                    AppendJson(result, pair.Value);
                }
                result.Append('}');
                return;
            }
            if (value is List<object> list)
            {
                result.Append('[');
                for (int i = 0; i < list.Count; i++)
                {
                    if (i > 0) result.Append(',');
                    AppendJson(result, list[i]);
                }
                result.Append(']');
                return;
            }
            result.Append(Convert.ToString(value, CultureInfo.InvariantCulture));
        }

        private static void AppendString(StringBuilder result, string value)
        {
            result.Append('"');
            foreach (char character in value)
            {
                if (character == '"' || character == '\\') result.Append('\\').Append(character);
                else if (character == '\n') result.Append("\\n");
                else if (character == '\r') result.Append("\\r");
                else if (character == '\t') result.Append("\\t");
                else if (character < 32) result.Append("\\u").Append(((int)character).ToString("x4"));
                else result.Append(character);
            }
            result.Append('"');
        }
    }
}

