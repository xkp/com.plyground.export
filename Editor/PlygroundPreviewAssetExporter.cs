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
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;

namespace Plyground.Preview.Editor
{
    public static class PlygroundPreviewAssetExporter
    {
        private static void RejectExcludedPreviewAsset(UnityEngine.Object source)
        {
            if (source == null) return;
            string type = source.GetType().FullName ?? "";
            if ((type.StartsWith("UnityEngine.", StringComparison.Ordinal)
                    || type.StartsWith("Unity.", StringComparison.Ordinal))
                && type.IndexOf("Experimental", StringComparison.OrdinalIgnoreCase) >= 0)
                throw new InvalidOperationException("Preview excludes Experimental asset " + source.name);
            if (source.GetType().Assembly.GetName().Name.StartsWith("UnityEngine.Cluster", StringComparison.Ordinal))
                throw new InvalidOperationException("Preview excludes UnityEngine.Cluster asset " + source.name);
            if (source.GetType().Assembly.GetName().Name == "UnityEngine.SpriteShapeModule")
                throw new InvalidOperationException("Preview excludes SpriteShape asset " + source.name);
            if (source.GetType().Assembly.GetName().Name == "UnityEngine.SubstanceModule")
                throw new InvalidOperationException("Preview excludes Substance asset " + source.name);
            if (source.GetType().Assembly.GetName().Name == "UnityEngine.SubsystemsModule")
                throw new InvalidOperationException("Preview excludes Subsystems asset " + source.name);
            if (source.GetType().Assembly.GetName().Name == "UnityEngine.TextCoreFontEngineModule"
                || source.GetType().Assembly.GetName().Name == "UnityEngine.TextCoreTextEngineModule"
                || source.GetType().Assembly.GetName().Name == "UnityEngine.TextRenderingModule")
                throw new InvalidOperationException("Preview excludes Unity text rendering asset " + source.name);
            if (source.GetType().Assembly.GetName().Name == "UnityEngine.TilemapModule")
                throw new InvalidOperationException("Preview excludes Tilemap asset " + source.name);
            if (source.GetType().Assembly.GetName().Name == "UnityEngine.UnityAnalyticsCommonModule"
                || source.GetType().Assembly.GetName().Name == "UnityEngine.UnityAnalyticsModule"
                || source.GetType().Assembly.GetName().Name == "UnityEngine.PerformanceReportingModule")
                throw new InvalidOperationException("Preview excludes Analytics asset " + source.name);
            if (source.GetType().Assembly.GetName().Name == "UnityEngine.VFXModule")
                throw new InvalidOperationException("Preview excludes VFX asset " + source.name);
            if (source.GetType().Assembly.GetName().Name == "UnityEngine.VRModule")
                throw new InvalidOperationException("Preview excludes VR asset " + source.name);
            if (source.GetType().Assembly.GetName().Name == "UnityEngine.VideoModule")
                throw new InvalidOperationException("Preview graylists UnityEngine.Video asset "
                    + source.name + ": implementation required for this use case");
            if (source.GetType().Assembly.GetName().Name == "UnityEngine.VirtualTexturingModule")
                throw new InvalidOperationException("Preview excludes VirtualTexturing asset " + source.name);
            if (source.GetType().Assembly.GetName().Name == "UnityEngine.WindModule")
                throw new InvalidOperationException("Preview excludes WindModule asset " + source.name);
            if (source.GetType().Assembly.GetName().Name == "UnityEngine.XRModule")
                throw new InvalidOperationException("Preview excludes XRModule asset " + source.name);
            if (type.StartsWith("UnityEngine.", StringComparison.Ordinal)
                && type.IndexOf("Android", StringComparison.OrdinalIgnoreCase) >= 0)
                throw new InvalidOperationException("Preview excludes Android asset " + source.name);
            if (type.StartsWith("UnityEngine.iPhone", StringComparison.Ordinal))
                throw new InvalidOperationException("Preview excludes iPhone asset " + source.name);
            PropertyInfo maskInteraction = source.GetType().GetProperty("maskInteraction",
                BindingFlags.Instance | BindingFlags.Public);
            if (maskInteraction != null
                && maskInteraction.PropertyType.FullName == "UnityEngine.SpriteMaskInteraction"
                && Convert.ToInt32(maskInteraction.GetValue(source)) != 0)
                throw new InvalidOperationException("Preview excludes SpriteMask interaction on " + source.name);
            foreach (string prefix in new[] { "UnityEngine.Windows", "UnityEngine.WSA",
                "UnityEngine.iOS", "UnityEngine.tvOS", "UnityEngine.Apple", "UnityEngine.Lumin",
                "UnityEngine.ADBannerView", "UnityEngine.ADInterstitialAd",
                "UnityEngine.Social", "UnityEngine.SocialPlatforms",
                "UnityEngine.AdaptivePerformance",
                "UnityEngine.AMD", "UnityEngine.NVIDIA", "UnityEngine.ImageConversion",
                "UnityEngine.Shaders", "UnityEngine.TextCore", "UnityEngine.Tilemaps",
                "UnityEngine.GridBrushBase", "UnityEngine.CustomGridBrushAttribute",
                "UnityEngine.Analytics", "UnityEngine.RemoteSettings", "UnityEngine.RemoteConfigSettings",
                "UnityEngine.VFX", "UnityEngine.XR.GameViewRenderMode", "UnityEngine.XR.TrackingSpaceType",
                "UnityEngine.XR.WSA.RemoteDeviceVersion", "UnityEngine.XR.XRDevice",
                "UnityEngine.XR.XRSettings", "UnityEngine.XR.XRStats",
                "UnityEngine.Rendering.VirtualTexturing", "UnityEngine.WindZone", "UnityEngine.WindZoneMode",
                "UnityEngine.XR",
                "UnityEngine.SpriteMask", "UnityEngine.Timeline",
                "UnityEngine.Rendering.UnifiedRayTracing" })
                if (type == prefix || type.StartsWith(prefix + ".", StringComparison.Ordinal))
                    throw new InvalidOperationException("Preview excludes " + prefix + " asset " + source.name);
        }
        private static string exportOutputDirectory;
        private static HashSet<string> exportedPrefabIds;
        private static HashSet<string> exportedTextureIds;
        private static HashSet<string> exportedSpriteIds;
        private static HashSet<string> exportedTmpFontIds;
        private static List<(string path, string typeName)> exportedRuntimeSources;
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
            exportedTextureIds = new HashSet<string>();
            exportedSpriteIds = new HashSet<string>();
            exportedTmpFontIds = new HashSet<string>();
            var layers = new Dictionary<string, object>();
            var renderingLayers = new Dictionary<string, object>();
            var renderingLayerNames = RenderingLayerMask.GetDefinedRenderingLayerNames();
            for (int index = 0; index < renderingLayerNames.Length; index++)
            {
                string name = renderingLayerNames[index];
                if (!string.IsNullOrEmpty(name)) renderingLayers[name] = index;
            }
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
            string navMeshId = ExportSceneNavMesh(scene, assets, outputDirectory);
            var sceneData = Map("name", scene.name, "path", scene.path,
                "buildIndex", scene.buildIndex,
                "sceneCountInBuildSettings", SceneManager.sceneCountInBuildSettings,
                "roots", roots);
            if (navMeshId != null) sceneData["navMesh"] = navMeshId;
            foreach (string file in Directory.GetFiles(Application.dataPath, "*.inputactions",
                SearchOption.AllDirectories))
            {
                string assetPath = "Assets/" + Path.GetRelativePath(Application.dataPath, file)
                    .Replace('\\', '/');
                var actionAsset = AssetDatabase.LoadMainAssetAtPath(assetPath) as ScriptableObject;
                if (actionAsset != null) ExportScriptableObject(actionAsset, diagnostics, assets, scriptableAssetIds);
            }

            var source = Map(
                "format", "unity-bridge-source",
                "version", 1,
                "coordinateSystem", "unity",
                "sourceUnityVersion", Application.unityVersion,
                "applicationIdentity", Map("companyName", Application.companyName, "productName", Application.productName, "identifier", Application.identifier, "version", Application.version, "unityVersion", Application.unityVersion),
                "assets", assets, "legacyAxes", ExportLegacyAxes(), "physicsSettings", Map("gravity", Numbers(Physics.gravity.x, Physics.gravity.y, Physics.gravity.z)),
                "layers", layers, "renderingLayers", renderingLayers,
                "sortingLayers", SortingLayer.layers.Select(layer => (object)Map(
                    "name", layer.name, "id", layer.id, "value", layer.value)).ToList(), "renderSettings", Map(
                    "fog", RenderSettings.fog, "fogMode", (int)RenderSettings.fogMode,
                    "fogColor", Numbers(RenderSettings.fogColor.r, RenderSettings.fogColor.g,
                        RenderSettings.fogColor.b, RenderSettings.fogColor.a),
                    "fogDensity", RenderSettings.fogDensity,
                    "fogStartDistance", RenderSettings.fogStartDistance,
                    "fogEndDistance", RenderSettings.fogEndDistance), "qualitySettings", Map("names", QualitySettings.names.Select(name => (object)name).ToList(), "level", QualitySettings.GetQualityLevel()),
                "tags", UnityEditorInternal.InternalEditorUtility.tags.Select(tag => (object)tag).ToList(), "buildTags", Application.GetBuildTags().Select(tag => (object)tag).ToList(),
                "scene", sceneData,
                "diagnostics", diagnostics);
            File.WriteAllText(path, Json(source), new UTF8Encoding(false));
            Debug.Log("Unity bridge source exported to " + path + ". Diagnostics: " + diagnostics.Count);
        }

        private static string ExportSceneNavMesh(Scene scene, List<object> assets,
            string outputDirectory)
        {
            var mesh = NavMesh.CalculateTriangulation();
            if (mesh.indices == null || mesh.indices.Length == 0) return null;
            if (SceneManager.sceneCount != 1)
                throw new NotSupportedException("Preview authored NavMesh export requires one loaded Unity scene: "
                    + scene.path);
            if (mesh.areas.Length != mesh.indices.Length / 3
                || mesh.areas.Any(area => area != 0))
                throw new NotSupportedException("Preview cannot export non-default NavMesh areas in "
                    + scene.path);
            string id = scene.path + ":navMesh";
            string relativePath = "navmesh/" + HashId(id) + ".unitynavmesh.json";
            string target = Path.Combine(outputDirectory, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(target));
            var vertices = new List<object>();
            foreach (Vector3 point in mesh.vertices)
            { vertices.Add(point.x); vertices.Add(point.y); vertices.Add(point.z); }
            File.WriteAllText(target, Json(Map("format", "unity-navmesh-triangulation/v1",
                "vertices", vertices,
                "indices", mesh.indices.Select(index => (object)index).ToList(),
                "areas", mesh.areas.Select(area => (object)area).ToList())),
                new UTF8Encoding(false));
            assets.Add(Map("id", id, "kind", "navMesh", "path", relativePath));
            return id;
        }

        /** Export asset creator prefabs without requiring an active scene. */
        public static List<(string path, string typeName)> ExportPrefabsToPath(string path, IEnumerable<GameObject> prefabs)
            => ExportPrefabsToPath(path, prefabs, Array.Empty<UnityEngine.Object>());

        /** Export prefabs plus assets addressed directly by a module controller. */
        public static List<(string path, string typeName)> ExportPrefabsToPath(string path, IEnumerable<GameObject> prefabs,
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
            exportedTextureIds = new HashSet<string>();
            exportedSpriteIds = new HashSet<string>();
            exportedTmpFontIds = new HashSet<string>();
            exportedRuntimeSources = new List<(string path, string typeName)>();
            var diagnostics = new List<object>();
            var assets = new List<object>();
            var scriptableAssetIds = new HashSet<string>();
            var assetPaths = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            var resources = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            var layers = new Dictionary<string, object>();
            var renderingLayers = new Dictionary<string, object>();
            var renderingLayerNames = RenderingLayerMask.GetDefinedRenderingLayerNames();
            for (int index = 0; index < renderingLayerNames.Length; index++)
            {
                string name = renderingLayerNames[index];
                if (!string.IsNullOrEmpty(name)) renderingLayers[name] = index;
            }
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
            var bundledAssets = AssetDatabase.GetAllAssetBundleNames()
                .SelectMany(name => AssetDatabase.GetAssetPathsFromAssetBundle(name))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(path => AssetDatabase.LoadMainAssetAtPath(path))
                .Where(value => value != null);
            foreach (var asset in additionalAssets.Concat(bundledAssets)
                .Where(value => value != null).GroupBy(value => Id(value)).Select(group => group.First()))
            {
                RejectExcludedPreviewAsset(asset);
                string id = Id(asset);
                if (asset is GameObject gameObject && PrefabUtility.IsPartOfPrefabAsset(gameObject))
                {
                    if (exportedPrefabIds.Add(id))
                        assets.Add(Map("id", id, "kind", "prefab",
                            "root", ExportObject(gameObject, diagnostics, assets, scriptableAssetIds, outputDirectory)));
                }
                else if (asset is TMPro.TMP_FontAsset tmpFont)
                    ExportTmpFont(tmpFont, diagnostics, assets);
                else if (asset is ScriptableObject scriptable)
                    ExportScriptableObject(scriptable, diagnostics, assets, scriptableAssetIds);
                else if (asset is Sprite sprite)
                    ExportSprite(sprite, diagnostics, assets);
                else if (asset is Texture2D texture)
                    ExportTexture2D(texture, diagnostics, assets);
                else if (asset is AudioClip audioClip)
                    ExportAudioClip(audioClip, diagnostics, assets, scriptableAssetIds);
                else if (asset is PhysicsMaterial physicsMaterial)
                    ExportPhysicsMaterial(physicsMaterial, assets, scriptableAssetIds);
                else if (asset is Material material)
                    ExportStandaloneMaterial(material, diagnostics, assets, scriptableAssetIds);
                else if (asset is AnimationClip animationClip)
                    ExportStandaloneAnimationClip(animationClip, diagnostics, assets, scriptableAssetIds);
                else if (asset is AnimatorController animatorController)
                {
                    AddAnimatorRuntimeSources(animatorController);
                    assets.Add(Map("id", id, "kind", "animatorController",
                        "graph", PreviewAnimatorGraphExporter.Export(animatorController)));
                }
                else
                    diagnostics.Add("AssetDatabase asset needs Preview serialization: " +
                        AssetDatabase.GetAssetPath(asset) + " (" + asset.GetType().FullName + ")");
                AddCatalogEntry(asset, assetPaths, resources);
            }
            foreach (string file in Directory.GetFiles(Application.dataPath, "*.inputactions",
                SearchOption.AllDirectories))
            {
                string assetPath = "Assets/" + Path.GetRelativePath(Application.dataPath, file)
                    .Replace('\\', '/');
                var actionAsset = AssetDatabase.LoadMainAssetAtPath(assetPath) as ScriptableObject;
                if (actionAsset != null) ExportScriptableObject(actionAsset, diagnostics, assets, scriptableAssetIds);
            }
            var assetBundles = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            foreach (var entry in assetPaths)
            {
                var importer = AssetImporter.GetAtPath(entry.Key);
                string bundleName = importer == null ? "" : importer.assetBundleName;
                if (string.IsNullOrEmpty(bundleName)) continue;
                if (!string.IsNullOrEmpty(importer.assetBundleVariant))
                    bundleName += "." + importer.assetBundleVariant;
                if (!assetBundles.TryGetValue(bundleName, out object paths))
                    assetBundles[bundleName] = paths = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                ((Dictionary<string, object>)paths)[entry.Key] = entry.Value;
            }
            var source = Map("format", "unity-bridge-source", "version", 1,
                "coordinateSystem", "unity", "sourceUnityVersion", Application.unityVersion,
                "applicationIdentity", Map("companyName", Application.companyName, "productName", Application.productName, "identifier", Application.identifier, "version", Application.version, "unityVersion", Application.unityVersion),
                "assets", assets, "legacyAxes", ExportLegacyAxes(), "physicsSettings", Map("gravity", Numbers(Physics.gravity.x, Physics.gravity.y, Physics.gravity.z)), "scene", Map("name", "Module Prefabs", "roots", new List<object>()),
                "assetPaths", assetPaths, "resources", resources, "assetBundles", assetBundles, "layers", layers, "renderingLayers", renderingLayers,
                "sortingLayers", SortingLayer.layers.Select(layer => (object)Map(
                    "name", layer.name, "id", layer.id, "value", layer.value)).ToList(), "renderSettings", Map(
                    "fog", RenderSettings.fog, "fogMode", (int)RenderSettings.fogMode,
                    "fogColor", Numbers(RenderSettings.fogColor.r, RenderSettings.fogColor.g,
                        RenderSettings.fogColor.b, RenderSettings.fogColor.a),
                    "fogDensity", RenderSettings.fogDensity,
                    "fogStartDistance", RenderSettings.fogStartDistance,
                    "fogEndDistance", RenderSettings.fogEndDistance), "qualitySettings", Map("names", QualitySettings.names.Select(name => (object)name).ToList(), "level", QualitySettings.GetQualityLevel()),
                "tags", UnityEditorInternal.InternalEditorUtility.tags.Select(tag => (object)tag).ToList(), "buildTags", Application.GetBuildTags().Select(tag => (object)tag).ToList(),
                "diagnostics", diagnostics);
            File.WriteAllText(path, Json(source), new UTF8Encoding(false));
            return exportedRuntimeSources;
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
                RejectExcludedPreviewAsset(component);
                if (component == null || component is Transform || component is MeshFilter
                    || component is MeshRenderer || component is SkinnedMeshRenderer
                    || component is Animation) continue;
                var exported = ExportComponent(component, diagnostics, assets, scriptableAssetIds);
                if (exported != null) components.Add(exported);
            }
            var children = new List<object>();
            foreach (Transform child in source.transform)
                if (child.GetComponent<TMPro.TMP_SubMeshUI>() == null
                    && child.GetComponent<TMPro.TMP_SubMesh>() == null)
                    children.Add(ExportObject(child.gameObject, diagnostics, assets,
                        scriptableAssetIds, outputDirectory));
            Transform transform = source.transform;
            var result = Map(
                "id", Id(source), "name", source.name, "active", source.activeSelf,
                "tag", source.tag, "layer", source.layer, "isStatic", source.isStatic,
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
            if (renderer != null && (renderer.additionalVertexStreams != null
                || renderer.enlightenVertexStream != null
                || renderer.subMeshStartIndex != 0))
                throw new NotSupportedException("MeshRenderer vertex streams or static batching on "
                    + source.name + " are outside the single Three.js mesh mapping");
            if (source.GetComponent<TMPro.TextMeshPro>() == null
                && filter != null && renderer != null && filter.sharedMesh != null)
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
                            "receiveShadows", renderer.receiveShadows, "forceRenderingOff", renderer.forceRenderingOff,
                            "sortingLayerID", renderer.sortingLayerID, "sortingLayerName", renderer.sortingLayerName,
                            "sortingOrder", renderer.sortingOrder)));
                }
                catch (NotSupportedException) { throw; }
                catch (Exception error)
                {
                    diagnostics.Add("Mesh conversion failed on " + source.name + ": " + error.Message);
                }
            }
            else if ((filter != null || renderer != null)
                && source.GetComponent<TMPro.TextMeshPro>() == null)
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
                    var blendShapeWeights = new List<object>();
                    for (int shape = 0; shape < skin.sharedMesh.blendShapeCount; shape++)
                        blendShapeWeights.Add(skin.GetBlendShapeWeight(shape));
                    components.Add(Map("id", id, "type", "UnityEngine.SkinnedMeshRenderer",
                        "enabled", skin.enabled,
                        "properties", Map("bones", boneReferences,
                            "rootBone", skin.rootBone == null ? null : Map("$ref", Id(skin.rootBone)),
                            "castShadows", skin.shadowCastingMode != UnityEngine.Rendering.ShadowCastingMode.Off,
                            "receiveShadows", skin.receiveShadows, "forceRenderingOff", skin.forceRenderingOff,
                            "sortingLayerID", skin.sortingLayerID, "sortingLayerName", skin.sortingLayerName,
                            "sortingOrder", skin.sortingOrder,
                            "blendShapeWeights", blendShapeWeights)));
                }
                catch (NotSupportedException) { throw; }
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
            var colors = new List<object>();
            foreach (Color color in mesh.colors)
            { colors.Add(color.r); colors.Add(color.g); colors.Add(color.b); colors.Add(color.a); }
            var tangents = new List<object>();
            foreach (Vector4 tangent in mesh.tangents)
            { tangents.Add(tangent.x); tangents.Add(tangent.y); tangents.Add(tangent.z); tangents.Add(tangent.w); }
            var uvs2 = new List<object>();
            foreach (Vector2 uv in mesh.uv2)
            { uvs2.Add(uv.x); uvs2.Add(1f - uv.y); }
            var uvs3 = new List<object>();
            foreach (Vector2 uv in mesh.uv3)
            { uvs3.Add(uv.x); uvs3.Add(1f - uv.y); }
            var uvs4 = new List<object>();
            foreach (Vector2 uv in mesh.uv4)
            { uvs4.Add(uv.x); uvs4.Add(1f - uv.y); }
            var uvs5 = new List<object>();
            foreach (Vector2 uv in mesh.uv5)
            { uvs5.Add(uv.x); uvs5.Add(1f - uv.y); }
            var uvs6 = new List<object>();
            foreach (Vector2 uv in mesh.uv6)
            { uvs6.Add(uv.x); uvs6.Add(1f - uv.y); }
            var uvs7 = new List<object>();
            foreach (Vector2 uv in mesh.uv7)
            { uvs7.Add(uv.x); uvs7.Add(1f - uv.y); }
            var uvs8 = new List<object>();
            foreach (Vector2 uv in mesh.uv8)
            { uvs8.Add(uv.x); uvs8.Add(1f - uv.y); }
            // glTF texture coordinates stay Vector2. Keep Unity's extra UV components
            // as script-facing data for the unified Three.js renderer.
            var uvFull = new Dictionary<string, object>();
            for (int channel = 0; channel < 8; channel++)
            {
                var attribute = (VertexAttribute)((int)VertexAttribute.TexCoord0 + channel);
                int dimension = mesh.GetVertexAttributeDimension(attribute);
                if (dimension <= 2) continue;
                var values = new List<Vector4>();
                mesh.GetUVs(channel, values);
                var flattened = new List<object>();
                foreach (Vector4 uv in values)
                {
                    flattened.Add(uv.x);
                    flattened.Add(1f - uv.y);
                    flattened.Add(uv.z);
                    if (dimension == 4) flattened.Add(uv.w);
                }
                uvFull[channel.ToString(CultureInfo.InvariantCulture)] =
                    Map("dimension", dimension, "values", flattened);
            }
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
                "uvs", uvs, "uvFull", uvFull, "colors", colors, "tangents", tangents,
                "uvs2", uvs2, "uvs3", uvs3, "uvs4", uvs4,
                "uvs5", uvs5, "uvs6", uvs6, "uvs7", uvs7, "uvs8", uvs8,
                "materials", exportedMaterials, "submeshes", submeshes,
                "blendShapes", ExportBlendShapes(mesh));
        }

        private static List<object> ExportBlendShapes(Mesh mesh)
        {
            var shapes = new List<object>();
            for (int shape = 0; shape < mesh.blendShapeCount; shape++)
            {
                var frames = new List<object>();
                for (int frame = 0; frame < mesh.GetBlendShapeFrameCount(shape); frame++)
                {
                    var vertices = new Vector3[mesh.vertexCount];
                    var normals = new Vector3[mesh.vertexCount];
                    var tangents = new Vector3[mesh.vertexCount];
                    mesh.GetBlendShapeFrameVertices(shape, frame, vertices, normals, tangents);
                    var positions = new List<object>();
                    var normalDeltas = new List<object>();
                    var tangentDeltas = new List<object>();
                    for (int index = 0; index < mesh.vertexCount; index++)
                    {
                        positions.Add(vertices[index].x); positions.Add(vertices[index].y);
                        positions.Add(vertices[index].z);
                        normalDeltas.Add(normals[index].x); normalDeltas.Add(normals[index].y);
                        normalDeltas.Add(normals[index].z);
                        tangentDeltas.Add(tangents[index].x); tangentDeltas.Add(tangents[index].y);
                        tangentDeltas.Add(tangents[index].z);
                    }
                    frames.Add(Map("weight", mesh.GetBlendShapeFrameWeight(shape, frame),
                        "vertices", positions, "normals", normalDeltas, "tangents", tangentDeltas));
                }
                shapes.Add(Map("name", mesh.GetBlendShapeName(shape), "frames", frames));
            }
            return shapes;
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
            if (material.isVariant)
                diagnostics.Add("Material variant flattened to resolved URP Lit inputs: " + material.name);
            if (material.enableInstancing)
                diagnostics.Add("Material GPU instancing omitted by URP Lit fallback: " + material.name);
            if (material.doubleSidedGI)
                diagnostics.Add("Material double-sided GI omitted by URP Lit fallback: " + material.name);
            string shaderGuid = material.shader != null
                ? AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(material.shader)) : "";
            bool juTransparency = shaderGuid == "d20272b3c7d3d5b40bd5ef601dc742a4";
            string shaderName = material.shader != null ? material.shader.name : "";
            bool urpLitShader = shaderName == "Universal Render Pipeline/Lit";
            string shaderCompatibility = urpLitShader ? "urp-lit"
                : juTransparency ? "custom-adapter"
                : shaderName == "Standard" ? "standard-fallback" : "shader-fallback";
            Color color = juTransparency && material.HasProperty("main_color")
                ? material.GetColor("main_color")
                : material.HasProperty("_BaseColor") ? material.GetColor("_BaseColor")
                : material.HasProperty("_Color") ? material.GetColor("_Color") : Color.white;
            if (juTransparency && material.HasProperty("alpha")) color.a = material.GetFloat("alpha");
            var result = Map("id", Id(material), "name", material.name,
                "shaderName", shaderName,
                "shaderGuid", shaderGuid,
                "previewMaterialModel", "urp-lit-v1",
                "shaderCompatibility", shaderCompatibility,
                "shaderAdapter", juTransparency ? "ju-auto-transparency-v1" : "",
                "shaderTarget", juTransparency ? "three-webgl-r180" : "",
                "alphaMode", "OPAQUE",
                "color", Numbers(color.r, color.g, color.b, color.a),
                "metallic", material.HasProperty("_Metallic") ? material.GetFloat("_Metallic") : 0f,
                "roughness", 1f - (material.HasProperty("_Smoothness")
                    ? material.GetFloat("_Smoothness") : material.HasProperty("_Glossiness")
                    ? material.GetFloat("_Glossiness") : 0f),
                "doubleSided", material.HasProperty("_Cull") && material.GetFloat("_Cull") == 0f);
            if (material.HasProperty("_EmissionColor"))
            {
                Color emission = material.GetColor("_EmissionColor");
                bool emissionEnabled = material.IsKeywordEnabled("_EMISSION");
                result["emissionColor"] = Numbers(emission.r, emission.g, emission.b);
                result["emissionEnabled"] = emissionEnabled;
                if (emissionEnabled && emission.maxColorComponent > 0f)
                    result["emissive"] = Numbers(emission.r, emission.g, emission.b);
            }
            bool urpAlphaClip = urpLitShader && material.HasProperty("_AlphaClip")
                && material.GetFloat("_AlphaClip") > 0.5f;
            bool urpTransparent = urpLitShader && material.HasProperty("_Surface")
                && material.GetFloat("_Surface") > 0.5f;
            if (urpLitShader) result["urpSurfaceMode"] = urpTransparent ? 1 : 0;
            if (urpLitShader && material.HasProperty("_Blend"))
                result["urpBlendMode"] = material.GetFloat("_Blend");
            if (urpAlphaClip || material.IsKeywordEnabled("_ALPHATEST_ON"))
            {
                result["alphaMode"] = "MASK";
                result["alphaCutoff"] = material.HasProperty("_Cutoff") ? material.GetFloat("_Cutoff") : 0.5f;
            }
            else if (juTransparency || urpTransparent || material.IsKeywordEnabled("_ALPHABLEND_ON")
                || material.IsKeywordEnabled("_ALPHAPREMULTIPLY_ON")
                || !urpLitShader && color.a < 1f)
                result["alphaMode"] = "BLEND";
            if (urpLitShader && urpTransparent && material.HasProperty("_Blend")
                && material.GetFloat("_Blend") != 0f)
                diagnostics.Add("URP Lit blend mode approximated as alpha blend: " + material.name);
            if (urpLitShader && material.HasProperty("_WorkflowMode")
                && material.GetFloat("_WorkflowMode") != 1f)
                diagnostics.Add("URP Lit specular workflow approximated as metallic: " + material.name);
            if (material.HasProperty("_Cull") && material.GetFloat("_Cull") == 1f)
                diagnostics.Add("Front-face culling approximated on " + material.name);
            if (material.HasProperty("_BumpMap") && material.GetTexture("_BumpMap") != null)
            {
                var normal = material.GetTexture("_BumpMap") as Texture2D;
                string normalPath = normal != null ? AssetDatabase.GetAssetPath(normal) : "";
                var importer = AssetImporter.GetAtPath(normalPath) as TextureImporter;
                if (!material.IsKeywordEnabled("_NORMALMAP") || normal == null
                    || importer == null || importer.textureType != TextureImporterType.NormalMap)
                {
                    string warning = "Normal map on " + material.name
                        + " uses flat fallback: source texture or active normal-map keyword is unavailable";
                    if (!diagnostics.Contains(warning)) diagnostics.Add(warning);
                }
                else
                {
                    try
                    {
                        result["normalTextureId"] = Id(normal);
                        result["normalTexturePng"] = Convert.ToBase64String(
                            importer.convertToNormalmap
                                ? ReadConvertedNormalPng(normalPath) : ReadTexturePng(normal));
                        result["normalScale"] = material.HasProperty("_BumpScale")
                            ? material.GetFloat("_BumpScale") : 1f;
                        Vector2 normalScale = material.GetTextureScale("_BumpMap");
                        Vector2 normalOffset = material.GetTextureOffset("_BumpMap");
                        result["normalTextureScale"] = Numbers(normalScale.x, normalScale.y);
                        result["normalTextureOffset"] = Numbers(normalOffset.x, normalOffset.y);
                    }
                    catch (Exception error)
                    {
                        diagnostics.Add("Normal map conversion failed for " + material.name + ": " + error.Message);
                    }
                }
            }
            if (material.IsKeywordEnabled("_OCCLUSIONMAP") && material.HasProperty("_OcclusionMap")
                && material.GetTexture("_OcclusionMap") != null)
            {
                var occlusion = material.GetTexture("_OcclusionMap") as Texture2D;
                if (occlusion == null)
                    diagnostics.Add("Occlusion map needs a Texture2D source: " + material.name);
                else
                {
                    try
                    {
                        result["occlusionTextureId"] = Id(occlusion);
                        var occlusionImporter = AssetImporter.GetAtPath(
                            AssetDatabase.GetAssetPath(occlusion)) as TextureImporter;
                        if (occlusionImporter == null)
                            throw new InvalidOperationException("Occlusion texture importer is unavailable");
                        result["occlusionTexturePng"] = Convert.ToBase64String(
                            OcclusionPngFromUnityGreen(ReadTexturePng(occlusion),
                                occlusionImporter.sRGBTexture));
                        result["occlusionStrength"] = material.HasProperty("_OcclusionStrength")
                            ? material.GetFloat("_OcclusionStrength") : 1f;
                        Vector2 occlusionScale = material.GetTextureScale("_OcclusionMap");
                        Vector2 occlusionOffset = material.GetTextureOffset("_OcclusionMap");
                        result["occlusionTextureScale"] = Numbers(occlusionScale.x, occlusionScale.y);
                        result["occlusionTextureOffset"] = Numbers(occlusionOffset.x, occlusionOffset.y);
                    }
                    catch (Exception error)
                    {
                        diagnostics.Add("Occlusion map conversion failed for " + material.name + ": " + error.Message);
                    }
                }
            }
            if (material.HasProperty("_MetallicGlossMap") && material.GetTexture("_MetallicGlossMap") != null)
            {
                bool urpLit = material.shader != null
                    && material.shader.name == "Universal Render Pipeline/Lit";
                bool standard = material.shader != null && material.shader.name == "Standard";
                bool active = urpLit ? material.IsKeywordEnabled("_METALLICSPECGLOSSMAP")
                    : material.IsKeywordEnabled("_METALLICGLOSSMAP");
                var packed = material.GetTexture("_MetallicGlossMap") as Texture2D;
                if (!active || (!urpLit && !standard) || packed == null
                    || material.IsKeywordEnabled("_SPECULAR_SETUP")
                    || material.IsKeywordEnabled("_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A"))
                    diagnostics.Add("Metallic/smoothness texture needs supported metallic workflow: " + material.name);
                else
                {
                    try
                    {
                        var packedImporter = AssetImporter.GetAtPath(
                            AssetDatabase.GetAssetPath(packed)) as TextureImporter;
                        if (packedImporter == null)
                            throw new InvalidOperationException("Metallic texture importer is unavailable");
                        float smoothnessScale = urpLit
                            ? material.GetFloat("_Smoothness") : material.GetFloat("_GlossMapScale");
                        result["metallicRoughnessTextureId"] = Id(packed);
                        result["metallicRoughnessTexturePng"] = Convert.ToBase64String(
                            MetallicRoughnessPngFromUnity(ReadTexturePng(packed),
                                packedImporter.sRGBTexture, smoothnessScale));
                        result["metallic"] = 1f;
                        result["roughness"] = 1f;
                        Vector2 packedScale = material.GetTextureScale("_MetallicGlossMap");
                        Vector2 packedOffset = material.GetTextureOffset("_MetallicGlossMap");
                        result["metallicRoughnessTextureScale"] = Numbers(packedScale.x, packedScale.y);
                        result["metallicRoughnessTextureOffset"] = Numbers(packedOffset.x, packedOffset.y);
                    }
                    catch (Exception error)
                    {
                        diagnostics.Add("Metallic/smoothness conversion failed for " + material.name
                            + ": " + error.Message);
                    }
                }
            }
            string textureProperty = juTransparency && material.HasProperty("main_texture") ? "main_texture"
                : material.HasProperty("_BaseMap") ? "_BaseMap" : "_MainTex";
            Texture texture = juTransparency && material.HasProperty("main_texture")
                ? material.GetTexture("main_texture")
                : material.HasProperty("_BaseMap") ? material.GetTexture("_BaseMap")
                : material.mainTexture;
            if (texture is Texture2D image)
            {
                try
                {
                    result["textureId"] = Id(image);
                    result["texturePng"] = Convert.ToBase64String(ReadTexturePng(image));
                    Vector2 scale = material.GetTextureScale(textureProperty);
                    Vector2 offset = material.GetTextureOffset(textureProperty);
                    result["textureScale"] = Numbers(scale.x, scale.y);
                    result["textureOffset"] = Numbers(offset.x, offset.y);
                }
                catch (Exception error)
                {
                    diagnostics.Add("Texture conversion failed for " + material.name + ": " + error.Message);
                }
            }
            else if (texture != null) diagnostics.Add("Unsupported texture on " + material.name);
            if (shaderCompatibility == "shader-fallback")
                diagnostics.Add("Material shader " + shaderName + " uses URP Lit fallback on " + material.name);
            return result;
        }

        private static byte ShaderLinearByte(byte channel, bool sourceSrgb)
        {
            if (!sourceSrgb) return channel;
            float value = channel / 255f;
            float linear = value <= 0.04045f ? value / 12.92f
                : Mathf.Pow((value + 0.055f) / 1.055f, 2.4f);
            return (byte)Mathf.RoundToInt(Mathf.Clamp01(linear) * 255f);
        }

        private static byte[] MetallicRoughnessPngFromUnity(byte[] sourcePng,
            bool sourceSrgb, float smoothnessScale)
        {
            var image = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
            try
            {
                if (!ImageConversion.LoadImage(image, sourcePng))
                    throw new InvalidOperationException("Metallic source PNG could not be decoded");
                Color32[] pixels = image.GetPixels32();
                for (int i = 0; i < pixels.Length; i++)
                {
                    byte metallic = ShaderLinearByte(pixels[i].r, sourceSrgb);
                    byte roughness = (byte)Mathf.RoundToInt(
                        (1f - Mathf.Clamp01(pixels[i].a / 255f * smoothnessScale)) * 255f);
                    pixels[i] = new Color32(255, roughness, metallic, 255);
                }
                image.SetPixels32(pixels);
                return image.EncodeToPNG();
            }
            finally { UnityEngine.Object.DestroyImmediate(image); }
        }

        private static byte[] OcclusionPngFromUnityGreen(byte[] sourcePng, bool sourceSrgb)
        {
            var image = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
            try
            {
                if (!ImageConversion.LoadImage(image, sourcePng))
                    throw new InvalidOperationException("Occlusion source PNG could not be decoded");
                Color32[] pixels = image.GetPixels32();
                for (int i = 0; i < pixels.Length; i++)
                {
                    byte occlusion = ShaderLinearByte(pixels[i].g, sourceSrgb);
                    pixels[i] = new Color32(occlusion, occlusion, occlusion, 255);
                }
                image.SetPixels32(pixels);
                return image.EncodeToPNG();
            }
            finally { UnityEngine.Object.DestroyImmediate(image); }
        }

        private static byte[] ReadConvertedNormalPng(string sourcePath)
        {
            string folder = "Assets/__PlygroundNormal_" + Guid.NewGuid().ToString("N");
            string copyPath = folder + "/source" + Path.GetExtension(sourcePath);
            AssetDatabase.CreateFolder("Assets", Path.GetFileName(folder));
            try
            {
                if (!AssetDatabase.CopyAsset(sourcePath, copyPath))
                    throw new InvalidOperationException("Could not copy normal source " + sourcePath);
                var importer = AssetImporter.GetAtPath(copyPath) as TextureImporter;
                if (importer == null || !importer.convertToNormalmap)
                    throw new InvalidOperationException("Copied normal importer lost height conversion: " + sourcePath);
                importer.isReadable = true;
                importer.SaveAndReimport();
                var imported = AssetDatabase.LoadAssetAtPath<Texture2D>(copyPath);
                var packed = imported.GetPixels32(0);
                var decoded = new Texture2D(imported.width, imported.height,
                    TextureFormat.RGBA32, false, true);
                try
                {
                    // URP Packing.hlsl UnpackNormalMapRGorAG: X is R*A, Y is G.
                    // Encode XYZ for glTF/Three without applying material BumpScale here.
                    for (int i = 0; i < packed.Length; i++)
                    {
                        float x = packed[i].r / 255f * (packed[i].a / 255f) * 2f - 1f;
                        float y = packed[i].g / 255f * 2f - 1f;
                        float z = Mathf.Sqrt(Mathf.Max(0f, 1f - x * x - y * y));
                        packed[i] = new Color32(
                            (byte)Mathf.RoundToInt(Mathf.Clamp01(x * 0.5f + 0.5f) * 255f),
                            (byte)Mathf.RoundToInt(Mathf.Clamp01(y * 0.5f + 0.5f) * 255f),
                            (byte)Mathf.RoundToInt(Mathf.Clamp01(z * 0.5f + 0.5f) * 255f), 255);
                    }
                    decoded.SetPixels32(packed);
                    decoded.Apply();
                    return decoded.EncodeToPNG();
                }
                finally { UnityEngine.Object.DestroyImmediate(decoded); }
            }
            finally
            {
                if (!AssetDatabase.DeleteAsset(folder))
                    Debug.LogWarning("Preview normal scratch asset was not removed: " + folder);
            }
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

        private static byte[] ReadTmpAtlasPng(Texture2D atlas)
        {
            try { return ReadTexturePng(atlas); }
            catch (InvalidOperationException)
            {
                var target = RenderTexture.GetTemporary(atlas.width, atlas.height, 0,
                    RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
                var previous = RenderTexture.active;
                var readable = new Texture2D(atlas.width, atlas.height, TextureFormat.RGBA32,
                    false, true);
                try
                {
                    Graphics.Blit(atlas, target);
                    RenderTexture.active = target;
                    readable.ReadPixels(new Rect(0, 0, atlas.width, atlas.height), 0, 0);
                    readable.Apply();
                    return readable.EncodeToPNG();
                }
                finally
                {
                    RenderTexture.active = previous;
                    UnityEngine.Object.DestroyImmediate(readable);
                    RenderTexture.ReleaseTemporary(target);
                }
            }
        }

        private static Dictionary<string, object> ExportComponent(UnityEngine.Component source,
            List<object> diagnostics, List<object> assets, HashSet<string> scriptableAssetIds)
        {
            string navType = source.GetType().FullName;
            if (navType == "Unity.AI.Navigation.NavMeshSurface"
                && NavMesh.CalculateTriangulation().indices.Length == 0)
                throw new InvalidOperationException("NavMeshSurface has no baked NavMesh on "
                    + source.gameObject.name + " in " + source.gameObject.scene.path);
            if (navType == "Unity.AI.Navigation.NavMeshSurface"
                || navType == "Unity.AI.Navigation.NavMeshModifier"
                || navType == "Unity.AI.Navigation.NavMeshModifierVolume")
            {
                diagnostics.Add("Baked navigation authoring component " + navType + " on "
                    + source.gameObject.name + " into the exported NavMesh");
                return null;
            }
            if (navType == "Unity.AI.Navigation.NavMeshLink")
                throw new NotSupportedException("Preview needs a dynamic navigation mapping for "
                    + navType + " on " + source.gameObject.name + " in " + source.gameObject.scene.path);
            string type = null;
            var properties = new Dictionary<string, object>();
            if (source is Terrain terrain)
            {
                type = "UnityEngine.Terrain";
                properties["terrainData"] = ExportTerrainData(terrain.terrainData, assets);
                properties["drawHeightmap"] = terrain.drawHeightmap;
                var layers = terrain.terrainData != null ? terrain.terrainData.terrainLayers : null;
                if (layers != null && layers.Length > 0 && layers[0] != null)
                {
                    TerrainLayer first = layers[0];
                    if (first.diffuseTexture != null)
                        properties["_previewLayerTexture"] = ExportTexture2D(first.diffuseTexture,
                            diagnostics, assets);
                    Vector2 tileSize = first.tileSize;
                    Vector2 tileOffset = first.tileOffset;
                    properties["_previewLayerTileSize"] = Map("$vector2",
                        Numbers(tileSize.x, tileSize.y));
                    properties["_previewLayerTileOffset"] = Map("$vector2",
                        Numbers(tileOffset.x, tileOffset.y));
                    if (first.normalMapTexture != null || first.maskMapTexture != null)
                        diagnostics.Add("Terrain normal/mask maps are not yet rendered on "
                            + source.gameObject.name);
                }
                if (layers != null && layers.Length > 1)
                {
                    if (layers.Length <= 8 && layers.All(layer => layer != null
                        && layer.diffuseTexture != null))
                    {
                        var exportedLayers = new List<object>();
                        foreach (TerrainLayer layer in layers)
                        {
                            Vector2 tileSize = layer.tileSize;
                            Vector2 tileOffset = layer.tileOffset;
                            exportedLayers.Add(Map("texture", ExportTexture2D(layer.diffuseTexture,
                                diagnostics, assets),
                                "tileSize", Map("$vector2", Numbers(tileSize.x, tileSize.y)),
                                "tileOffset", Map("$vector2", Numbers(tileOffset.x, tileOffset.y))));
                            if (layer.normalMapTexture != null || layer.maskMapTexture != null)
                                diagnostics.Add("Terrain normal/mask maps are not yet rendered on "
                                    + source.gameObject.name + ": " + layer.name);
                        }
                        properties["_previewLayers"] = exportedLayers;
                    }
                    else diagnostics.Add("Terrain blending requires 2-8 diffuse layers on "
                        + source.gameObject.name + " (" + layers.Length + " layers)");
                }
                string templatePath = terrain.materialTemplate != null
                    ? AssetDatabase.GetAssetPath(terrain.materialTemplate) : null;
                if (!string.IsNullOrEmpty(templatePath)
                    && (templatePath.StartsWith("Assets/", StringComparison.Ordinal)
                        || templatePath.StartsWith("Packages/", StringComparison.Ordinal)))
                    diagnostics.Add("Custom Terrain material is not yet rendered on "
                        + source.gameObject.name);
            }
            else if (source is TerrainCollider terrainCollider)
            {
                type = "UnityEngine.TerrainCollider";
                properties["terrainData"] = ExportTerrainData(terrainCollider.terrainData, assets);
                properties["isTrigger"] = terrainCollider.isTrigger;
            }
            else if (source is NavMeshAgent navAgent)
            {
                if (navAgent.agentTypeID != 0 || navAgent.areaMask != NavMesh.AllAreas
                    || navAgent.baseOffset != 0f || navAgent.avoidancePriority != 50
                    || (navAgent.obstacleAvoidanceType != ObstacleAvoidanceType.NoObstacleAvoidance
                        && navAgent.obstacleAvoidanceType != ObstacleAvoidanceType.HighQualityObstacleAvoidance))
                    throw new NotSupportedException("Preview NavMeshAgent needs default agent type, all areas, zero base offset, and default avoidance priority/quality on "
                        + source.gameObject.name + " in " + source.gameObject.scene.path);
                type = "UnityEngine.AI.NavMeshAgent";
                properties["speed"] = navAgent.speed;
                properties["acceleration"] = navAgent.acceleration;
                properties["angularSpeed"] = navAgent.angularSpeed;
                properties["stoppingDistance"] = navAgent.stoppingDistance;
                properties["radius"] = navAgent.radius;
                properties["height"] = navAgent.height;
                properties["autoBraking"] = navAgent.autoBraking;
                properties["autoRepath"] = navAgent.autoRepath;
                properties["updatePosition"] = navAgent.updatePosition;
                properties["updateRotation"] = navAgent.updateRotation;
                properties["isStopped"] = navAgent.isStopped;
                properties["useCrowd"] = navAgent.obstacleAvoidanceType != ObstacleAvoidanceType.NoObstacleAvoidance;
            }
            else if (source is NavMeshObstacle obstacle)
            {
                if (obstacle.shape != NavMeshObstacleShape.Box || !obstacle.carving)
                    throw new NotSupportedException("Preview needs a carving Box NavMeshObstacle on "
                        + source.gameObject.name + " in " + source.gameObject.scene.path);
                type = "UnityEngine.AI.NavMeshObstacle";
                properties["shape"] = (int)obstacle.shape;
                properties["center"] = Vec3(obstacle.center);
                properties["size"] = Vec3(obstacle.size);
                properties["radius"] = obstacle.radius;
                properties["height"] = obstacle.height;
                properties["carving"] = obstacle.carving;
                properties["carveOnlyStationary"] = obstacle.carveOnlyStationary;
                properties["carvingMoveThreshold"] = obstacle.carvingMoveThreshold;
                properties["carvingTimeToStationary"] = obstacle.carvingTimeToStationary;
            }
            else if (source is OffMeshLink offMeshLink)
            {
                if (offMeshLink.startTransform == null || offMeshLink.endTransform == null
                    || offMeshLink.area != 0 || offMeshLink.costOverride > 0f
                    || !offMeshLink.isActiveAndEnabled || !offMeshLink.activated)
                    throw new NotSupportedException("Preview needs an active Default-area OffMeshLink with endpoints and no custom cost on "
                        + source.gameObject.name + " in " + source.gameObject.scene.path);
                type = "UnityEngine.AI.OffMeshLink";
                properties["startTransform"] = Map("$ref", Id(offMeshLink.startTransform));
                properties["endTransform"] = Map("$ref", Id(offMeshLink.endTransform));
                properties["startPosition"] = Vec3(offMeshLink.startTransform.position);
                properties["endPosition"] = Vec3(offMeshLink.endTransform.position);
                properties["radius"] = 0.5f;
                properties["activated"] = offMeshLink.activated;
                properties["bakedActivated"] = offMeshLink.activated;
                properties["bakedEnabled"] = offMeshLink.enabled;
                properties["bakedActive"] = offMeshLink.gameObject.activeInHierarchy;
                properties["biDirectional"] = offMeshLink.biDirectional;
                properties["area"] = offMeshLink.area;
                properties["costOverride"] = offMeshLink.costOverride;
                properties["autoUpdatePositions"] = offMeshLink.autoUpdatePositions;
            }
            else if (source is UnityEngine.UIElements.UIDocument uiDocument)
            {
                type = "UnityEngine.UIElements.UIDocument";
                properties["sortingOrder"] = uiDocument.sortingOrder;
                if (uiDocument.panelSettings != null)
                    properties["panelSettings"] = Map("$asset", Id(uiDocument.panelSettings));
                if (uiDocument.visualTreeAsset != null)
                    properties["visualTreeAsset"] = Map("$asset", Id(uiDocument.visualTreeAsset));
                ExportUIToolkitAssets(uiDocument, assets);
            }
            else if (source is Camera camera)
            {
                type = "UnityEngine.Camera";
                if (camera.usePhysicalProperties || camera.depthTextureMode != DepthTextureMode.None
                    || camera.forceIntoRenderTexture || camera.allowDynamicResolution)
                    throw new InvalidOperationException("Camera physical lens, depth texture, or dynamic resolution is outside the Three.js camera mapping on "
                        + source.gameObject.name);
                if (camera.depth != 0f || camera.targetDisplay != 0 || !camera.allowHDR)
                    diagnostics.Add("Camera depth, HDR, or target display approximated by the single Three.js output on "
                        + source.gameObject.name);
                var viewport = camera.rect;
                if (viewport.x != 0f || viewport.y != 0f || viewport.width != 1f || viewport.height != 1f)
                    throw new InvalidOperationException("Camera rect/pixelRect requires a cropped viewport outside the single full-viewport Three.js camera mapping on "
                        + source.gameObject.name);
                if (camera.clearFlags == CameraClearFlags.Depth || camera.clearFlags == CameraClearFlags.Nothing)
                    throw new InvalidOperationException("Camera Depth/Nothing clear flags require retained framebuffer contents outside the single Three.js camera mapping on "
                        + source.gameObject.name);
                var clear = camera.backgroundColor;
                if (camera.clearFlags == CameraClearFlags.SolidColor
                    && (clear.a < 0f || clear.a > 1f || clear.r < 0f || clear.g < 0f || clear.b < 0f
                        || clear.r > clear.a || clear.g > clear.a || clear.b > clear.a))
                    throw new InvalidOperationException("Camera SolidColor RGB exceeds alpha and cannot be represented by the single Three.js clear mapping on "
                        + source.gameObject.name);
                properties["orthographic"] = camera.orthographic;
                properties["orthographicSize"] = camera.orthographicSize;
                properties["fieldOfView"] = camera.fieldOfView;
                properties["nearClipPlane"] = camera.nearClipPlane;
                properties["farClipPlane"] = camera.farClipPlane;
                properties["cullingMask"] = camera.cullingMask;
                properties["clearFlags"] = (int)camera.clearFlags;
                properties["backgroundColor"] = new Dictionary<string, object> {
                    ["$color"] = new List<object> { clear.r, clear.g, clear.b, clear.a }
                };
            }
            else if (source is Light light)
            {
                type = "UnityEngine.Light";
                if (light.type != LightType.Point && light.type != LightType.Spot
                    && light.type != LightType.Directional)
                    throw new InvalidOperationException("Light type is outside the Three.js light mapping on "
                        + source.gameObject.name + ": " + light.type);
                if (light.shadows != LightShadows.None || light.cookie != null
                    || light.cullingMask != -1 || light.renderingLayerMask != 1u
                    || light.useColorTemperature)
                    throw new InvalidOperationException("Light shadows, cookies, masks, or color temperature are outside the Three.js light mapping on "
                        + source.gameObject.name);                properties["type"] = (int)light.type;
                properties["color"] = Map("$color", Numbers(light.color.r,
                    light.color.g, light.color.b, light.color.a));
                properties["intensity"] = light.intensity;
                properties["range"] = light.range;
                properties["spotAngle"] = light.spotAngle;
                properties["innerSpotAngle"] = light.innerSpotAngle;
            }
            else if (source is TrailRenderer trail)
            {
                type = "UnityEngine.TrailRenderer";
                string shaderName = trail.sharedMaterial != null && trail.sharedMaterial.shader != null
                    ? trail.sharedMaterial.shader.name : null;
                if (shaderName != null && shaderName.Contains("Particles/"))
                    diagnostics.Add("TrailRenderer particle shader approximated by a basic Three.js line on "
                        + source.gameObject.name + ": " + shaderName);
                else if (shaderName != null && shaderName != "Sprites/Default")
                    throw new InvalidOperationException("TrailRenderer shader is outside the single Three.js line material mapping on "
                        + source.gameObject.name + ": " + shaderName);
                if (trail.generateLightingData || trail.textureScale != Vector2.one)
                    throw new InvalidOperationException("TrailRenderer lit geometry or texture scaling is outside the single Three.js line mapping on "
                        + source.gameObject.name);
                var widthKeys = trail.widthCurve.keys;
                bool simpleWidthCurve = widthKeys.Length >= 1 && widthKeys.Length <= 2
                    && Mathf.Approximately(widthKeys[0].time, 0f)
                    && Mathf.Approximately(widthKeys[0].value * trail.widthMultiplier, trail.startWidth)
                    && Mathf.Approximately(widthKeys[0].inTangent, 0f)
                    && Mathf.Approximately(widthKeys[0].outTangent, 0f)
                    && (widthKeys.Length == 1
                        ? Mathf.Approximately(trail.startWidth, trail.endWidth)
                        : Mathf.Approximately(widthKeys[1].time, 1f)
                          && Mathf.Approximately(widthKeys[1].value * trail.widthMultiplier, trail.endWidth)
                          && Mathf.Approximately(widthKeys[1].inTangent, 0f)
                          && Mathf.Approximately(widthKeys[1].outTangent, 0f));
                if ((int)trail.alignment != 0 || !trail.applyActiveColorSpace
                    || trail.numCapVertices != 0 || trail.numCornerVertices != 0
                    || !Mathf.Approximately(trail.shadowBias, .5f)
                    || (int)trail.textureMode != 0 || !simpleWidthCurve)
                    diagnostics.Add("TrailRenderer advanced settings approximated by a Three.js line on "
                        + source.gameObject.name);
                properties["emitting"] = trail.emitting;
                properties["autodestruct"] = trail.autodestruct;
                properties["time"] = trail.time;
                properties["minVertexDistance"] = trail.minVertexDistance;
                properties["startWidth"] = trail.startWidth;
                properties["endWidth"] = trail.endWidth;
                properties["widthMultiplier"] = trail.widthMultiplier;
                properties["startColor"] = Map("$color", Numbers(trail.startColor.r,
                    trail.startColor.g, trail.startColor.b, trail.startColor.a));
                properties["endColor"] = Map("$color", Numbers(trail.endColor.r,
                    trail.endColor.g, trail.endColor.b, trail.endColor.a));
                var positions = new List<object>();
                for (int i = 0; i < trail.positionCount; i++)
                {
                    Vector3 position = trail.GetPosition(i);
                    positions.Add(Map("$vector3", Numbers(position.x, position.y, position.z)));
                }
                properties["positions"] = positions;
                var gradient = trail.colorGradient;
                var colorKeys = new List<object>();
                foreach (var key in gradient.colorKeys)
                    colorKeys.Add(Map("color", Map("$color", Numbers(key.color.r, key.color.g,
                        key.color.b, key.color.a)), "time", key.time));
                var alphaKeys = new List<object>();
                foreach (var key in gradient.alphaKeys)
                    alphaKeys.Add(Map("alpha", key.alpha, "time", key.time));
                properties["colorGradient"] = Map("colorKeys", colorKeys, "alphaKeys", alphaKeys);
            }
            else if (source is LineRenderer line)
            {
                type = "UnityEngine.LineRenderer";
                string shaderName = line.sharedMaterial != null && line.sharedMaterial.shader != null
                    ? line.sharedMaterial.shader.name : null;
                if (shaderName != "Sprites/Default")
                    throw new InvalidOperationException("LineRenderer shader is outside the Three.js line mapping on "
                        + source.gameObject.name + ": " + (shaderName ?? "<none>"));
                var widthKeys = line.widthCurve.keys;
                bool simpleWidthCurve = widthKeys.Length >= 1 && widthKeys.Length <= 2
                    && Mathf.Approximately(widthKeys[0].time, 0f)
                    && Mathf.Approximately(widthKeys[0].value * line.widthMultiplier, line.startWidth)
                    && Mathf.Approximately(widthKeys[0].inTangent, 0f)
                    && Mathf.Approximately(widthKeys[0].outTangent, 0f)
                    && (widthKeys.Length == 1
                        ? Mathf.Approximately(line.startWidth, line.endWidth)
                        : Mathf.Approximately(widthKeys[1].time, 1f)
                          && Mathf.Approximately(widthKeys[1].value * line.widthMultiplier, line.endWidth)
                          && Mathf.Approximately(widthKeys[1].inTangent, 0f)
                          && Mathf.Approximately(widthKeys[1].outTangent, 0f));
                if ((int)line.alignment != 0 || !line.applyActiveColorSpace
                    || line.generateLightingData || line.numCapVertices != 0
                    || line.numCornerVertices != 0 || !Mathf.Approximately(line.shadowBias, .5f)
                    || (int)line.textureMode != 0 || line.textureScale != Vector2.one
                    || !simpleWidthCurve)
                    throw new InvalidOperationException("LineRenderer advanced renderer settings are outside the Three.js line mapping on "
                        + source.gameObject.name);
                properties["shaderName"] = shaderName;
                properties["loop"] = line.loop;
                properties["widthMultiplier"] = line.widthMultiplier;
                properties["startWidth"] = line.startWidth;
                properties["endWidth"] = line.endWidth;
                properties["useWorldSpace"] = line.useWorldSpace;
                properties["startColor"] = Map("$color", Numbers(line.startColor.r,
                    line.startColor.g, line.startColor.b, line.startColor.a));
                properties["endColor"] = Map("$color", Numbers(line.endColor.r,
                    line.endColor.g, line.endColor.b, line.endColor.a));
                var positions = new List<object>();
                for (int i = 0; i < line.positionCount; i++)
                {
                    Vector3 position = line.GetPosition(i);
                    positions.Add(Map("$vector3", Numbers(position.x, position.y, position.z)));
                }
                properties["positions"] = positions;
                var gradient = line.colorGradient;
                var colorKeys = new List<object>();
                foreach (var key in gradient.colorKeys)
                    colorKeys.Add(Map("color", Map("$color", Numbers(key.color.r, key.color.g,
                        key.color.b, key.color.a)), "time", key.time));
                var alphaKeys = new List<object>();
                foreach (var key in gradient.alphaKeys)
                    alphaKeys.Add(Map("alpha", key.alpha, "time", key.time));
                properties["colorGradient"] = Map("colorKeys", colorKeys, "alphaKeys", alphaKeys);
            }
            else if (source is Rigidbody body)
            {
                type = "UnityEngine.Rigidbody";
                properties["mass"] = body.mass;
                properties["automaticCenterOfMass"] = body.automaticCenterOfMass;
                if (!body.automaticCenterOfMass)
                    properties["centerOfMass"] = Vec3(body.centerOfMass);
                properties["useGravity"] = body.useGravity;
                properties["isKinematic"] = body.isKinematic;
                properties["drag"] = body.linearDamping;
                properties["constraints"] = (int)body.constraints;
                properties["collisionDetectionMode"] = (int)body.collisionDetectionMode;
                properties["interpolation"] = (int)body.interpolation;
                properties["angularDamping"] = body.angularDamping;
                properties["linearVelocity"] = Vec3(body.linearVelocity);
                properties["angularVelocity"] = Vec3(body.angularVelocity);
            }
            else if (source is CharacterJoint joint)
            {
                type = "UnityEngine.CharacterJoint";
                properties["connectedBody"] = joint.connectedBody != null
                    ? Map("$ref", Id(joint.connectedBody)) : null;
                properties["anchor"] = Vec3(joint.anchor);
                properties["axis"] = Vec3(joint.axis);
                properties["autoConfigureConnectedAnchor"] = joint.autoConfigureConnectedAnchor;
                properties["connectedAnchor"] = Vec3(joint.connectedAnchor);
                properties["enableCollision"] = joint.enableCollision;
                properties["enablePreprocessing"] = joint.enablePreprocessing;
                properties["massScale"] = joint.massScale;
                properties["connectedMassScale"] = joint.connectedMassScale;
                if (!float.IsInfinity(joint.breakForce)) properties["breakForce"] = joint.breakForce;
                if (!float.IsInfinity(joint.breakTorque)) properties["breakTorque"] = joint.breakTorque;
                properties["swingAxis"] = Vec3(joint.swingAxis);
                properties["lowTwistLimit"] = Map("limit", joint.lowTwistLimit.limit,
                    "bounciness", joint.lowTwistLimit.bounciness,
                    "contactDistance", joint.lowTwistLimit.contactDistance);
                properties["highTwistLimit"] = Map("limit", joint.highTwistLimit.limit,
                    "bounciness", joint.highTwistLimit.bounciness,
                    "contactDistance", joint.highTwistLimit.contactDistance);
                properties["swing1Limit"] = Map("limit", joint.swing1Limit.limit,
                    "bounciness", joint.swing1Limit.bounciness,
                    "contactDistance", joint.swing1Limit.contactDistance);
                properties["swing2Limit"] = Map("limit", joint.swing2Limit.limit,
                    "bounciness", joint.swing2Limit.bounciness,
                    "contactDistance", joint.swing2Limit.contactDistance);
                properties["twistLimitSpring"] = Map("spring", joint.twistLimitSpring.spring,
                    "damper", joint.twistLimitSpring.damper);
                properties["swingLimitSpring"] = Map("spring", joint.swingLimitSpring.spring,
                    "damper", joint.swingLimitSpring.damper);
                properties["enableProjection"] = joint.enableProjection;
                properties["projectionDistance"] = joint.projectionDistance;
                properties["projectionAngle"] = joint.projectionAngle;
            }
            else if (source is Animator animator)
            {
                type = "UnityEngine.Animator";
                properties["applyRootMotion"] = animator.applyRootMotion;
                if (animator.avatar != null)
                    properties["avatarData"] = ExportAvatarMetadata(animator.avatar);
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
                    AddAnimatorRuntimeSources(controller);
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
            else if (source is AudioListener)
            {
                type = "UnityEngine.AudioListener";
            }
            else if (source is AudioSource audio)
            {
                type = "UnityEngine.AudioSource";
                properties["pitch"] = audio.pitch;
                properties["volume"] = audio.volume;
                properties["mute"] = audio.mute;
                properties["priority"] = audio.priority;
                properties["loop"] = audio.loop;
                properties["playOnAwake"] = audio.playOnAwake;
                properties["spatialBlend"] = audio.spatialBlend;
                properties["panStereo"] = audio.panStereo;
                properties["rolloffMode"] = (int)audio.rolloffMode;
                properties["minDistance"] = audio.minDistance;
                properties["maxDistance"] = audio.maxDistance;
                properties["bypassEffects"] = audio.bypassEffects;
                properties["bypassListenerEffects"] = audio.bypassListenerEffects;
                properties["bypassReverbZones"] = audio.bypassReverbZones;
                properties["spatialize"] = audio.spatialize;
                properties["spatializePostEffects"] = audio.spatializePostEffects;
                properties["spread"] = audio.spread;
                properties["dopplerLevel"] = audio.dopplerLevel;
                properties["reverbZoneMix"] = audio.reverbZoneMix;
                if (audio.outputAudioMixerGroup != null)
                    properties["outputAudioMixerGroup"] = Map("id", Id(audio.outputAudioMixerGroup),
                        "name", audio.outputAudioMixerGroup.name,
                        "mixerName", audio.outputAudioMixerGroup.audioMixer != null
                            ? audio.outputAudioMixerGroup.audioMixer.name : "");
                if (audio.clip != null) properties["clip"] = ExportAudioClip(audio.clip,
                    diagnostics, assets, scriptableAssetIds);
            }
            else if (source is AudioLowPassFilter || source is AudioHighPassFilter
                || source is AudioEchoFilter || source is AudioDistortionFilter
                || source is AudioReverbFilter || source is AudioChorusFilter
                || source is AudioReverbZone)
            {
                type = source.GetType().FullName;
                properties["unitySerializedJson"] = EditorJsonUtility.ToJson(source);
                if (source is AudioLowPassFilter lowPass)
                {
                    properties["cutoffFrequency"] = lowPass.cutoffFrequency;
                    properties["lowpassResonanceQ"] = lowPass.lowpassResonanceQ;
                }
                else if (source is AudioHighPassFilter highPass)
                {
                    properties["cutoffFrequency"] = highPass.cutoffFrequency;
                    properties["highpassResonanceQ"] = highPass.highpassResonanceQ;
                }
                else if (source is AudioEchoFilter echo)
                {
                    properties["delay"] = echo.delay;
                    properties["decayRatio"] = echo.decayRatio;
                    properties["dryMix"] = echo.dryMix;
                    properties["wetMix"] = echo.wetMix;
                }
                else if (source is AudioDistortionFilter distortion)
                    properties["distortionLevel"] = distortion.distortionLevel;
                else if (source is AudioReverbZone zone)
                {
                    properties["minDistance"] = zone.minDistance;
                    properties["maxDistance"] = zone.maxDistance;
                }
            }
            else if (source is ParticleSystem particle)
            {
                type = "UnityEngine.ParticleSystem";
                var main = particle.main;
                var emission = particle.emission;
                properties["startLifetime"] = main.startLifetime.constant;
                if (main.startLifetime.mode == ParticleSystemCurveMode.TwoConstants)
                    properties["startLifetimeRange"] = Numbers(main.startLifetime.constantMin, main.startLifetime.constantMax);
                properties["duration"] = main.duration;
                properties["startDelay"] = main.startDelay.constant;
                if (main.startDelay.mode != ParticleSystemCurveMode.Constant)
                    diagnostics.Add("Particle start-delay curve approximated on " + source.gameObject.name);
                properties["startSpeed"] = main.startSpeed.constant;
                if (main.startSpeed.mode == ParticleSystemCurveMode.TwoConstants)
                    properties["startSpeedRange"] = Numbers(main.startSpeed.constantMin, main.startSpeed.constantMax);
                properties["startSize"] = main.startSize.constant;
                if (main.startSize.mode == ParticleSystemCurveMode.TwoConstants)
                    properties["startSizeRange"] = Numbers(main.startSize.constantMin, main.startSize.constantMax);
                properties["gravityModifier"] = main.gravityModifier.constant;
                properties["rateOverTime"] = emission.rateOverTime.constant;
                properties["emissionEnabled"] = emission.enabled;
                if (emission.rateOverTime.mode != ParticleSystemCurveMode.Constant)
                    diagnostics.Add("Particle emission-rate curve approximated on " + source.gameObject.name);
                properties["loop"] = main.loop;
                properties["playOnAwake"] = main.playOnAwake;
                properties["simulationSpace"] = main.simulationSpace.ToString();
                if (main.simulationSpace != ParticleSystemSimulationSpace.Local)
                    diagnostics.Add("Particle simulation space " + main.simulationSpace
                        + " approximated as local on " + source.gameObject.name);
                properties["maxParticles"] = main.maxParticles;
                var shape = particle.shape;
                properties["shapeType"] = shape.enabled ? shape.shapeType.ToString() : "None";
                properties["shapeRadius"] = shape.radius;
                properties["shapeRadiusThickness"] = shape.radiusThickness;
                properties["shapeAngle"] = shape.angle;
                properties["shapeLength"] = shape.length;
                properties["shapeScale"] = Vec3(shape.scale);
                properties["shapePosition"] = Vec3(shape.position);
                properties["shapeRotation"] = Vec3(shape.rotation);
                if (shape.enabled && shape.shapeType != ParticleSystemShapeType.Cone
                    && shape.shapeType != ParticleSystemShapeType.ConeVolume
                    && shape.shapeType != ParticleSystemShapeType.Sphere
                    && shape.shapeType != ParticleSystemShapeType.Box)
                    diagnostics.Add("Particle shape " + shape.shapeType + " approximated on " + source.gameObject.name);
                var particleRenderer = particle.GetComponent<ParticleSystemRenderer>();
                if (particleRenderer != null)
                {
                    if (particleRenderer.sharedMaterial != null)
                        properties["particleMaterial"] = ExportStandaloneMaterial(
                            particleRenderer.sharedMaterial, diagnostics, assets, scriptableAssetIds);
                    if (particleRenderer.renderMode != ParticleSystemRenderMode.Billboard)
                        diagnostics.Add("Particle render mode " + particleRenderer.renderMode
                            + " approximated as points on " + source.gameObject.name);
                }
                var sizeOverLifetime = particle.sizeOverLifetime;
                if (sizeOverLifetime.enabled)
                {
                    var sizeCurve = sizeOverLifetime.size;
                    if (sizeCurve.mode == ParticleSystemCurveMode.Curve)
                    {
                        var keys = new List<object>();
                        foreach (var key in sizeCurve.curve.keys)
                            keys.Add(Map("time", key.time, "value", key.value,
                                "inTangent", key.inTangent, "outTangent", key.outTangent));
                        properties["sizeOverLifetimeData"] = Map("enabled", true,
                            "multiplier", sizeCurve.curveMultiplier, "keys", keys);
                    }
                    else diagnostics.Add("Particle size-over-lifetime mode " + sizeCurve.mode
                        + " approximated on " + source.gameObject.name);
                }
                var colorOverLifetime = particle.colorOverLifetime;
                if (colorOverLifetime.enabled)
                {
                    var lifetimeColor = colorOverLifetime.color;
                    if (lifetimeColor.mode == ParticleSystemGradientMode.Gradient)
                    {
                        var colorKeys = new List<object>();
                        var alphaKeys = new List<object>();
                        foreach (var key in lifetimeColor.gradient.colorKeys)
                            colorKeys.Add(Map("time", key.time, "color", Numbers(
                                key.color.r, key.color.g, key.color.b)));
                        foreach (var key in lifetimeColor.gradient.alphaKeys)
                            alphaKeys.Add(Map("time", key.time, "alpha", key.alpha));
                        properties["colorOverLifetimeData"] = Map("enabled", true,
                            "colorKeys", colorKeys, "alphaKeys", alphaKeys);
                    }
                    else diagnostics.Add("Particle color-over-lifetime mode " + lifetimeColor.mode
                        + " approximated on " + source.gameObject.name);
                }                Color tint = main.startColor.color;
                properties["startColor"] = Map("$color", Numbers(tint.r, tint.g, tint.b, tint.a));
                if (main.startColor.mode != ParticleSystemGradientMode.Color)
                    diagnostics.Add("Particle start-color mode " + main.startColor.mode
                        + " approximated on " + source.gameObject.name);
                if (main.startLifetime.mode != ParticleSystemCurveMode.Constant
                    && main.startLifetime.mode != ParticleSystemCurveMode.TwoConstants
                    || main.startSpeed.mode != ParticleSystemCurveMode.Constant
                    && main.startSpeed.mode != ParticleSystemCurveMode.TwoConstants
                    || main.startSize.mode != ParticleSystemCurveMode.Constant
                    && main.startSize.mode != ParticleSystemCurveMode.TwoConstants)
                    diagnostics.Add("Particle curves approximated on " + source.gameObject.name);
            }
            else if (source is ParticleSystemRenderer particleRendererComponent)
            {
                type = "UnityEngine.ParticleSystemRenderer";
                properties["renderMode"] = (int)particleRendererComponent.renderMode;
                properties["enabled"] = particleRendererComponent.enabled;
                if (particleRendererComponent.sharedMaterial != null)
                    properties["sharedMaterial"] = ExportStandaloneMaterial(
                        particleRendererComponent.sharedMaterial, diagnostics, assets, scriptableAssetIds);
            }
            else if (source is Canvas canvas)
            {
                type = "UnityEngine.Canvas";
                if (canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                {
                    string location = AssetDatabase.GetAssetPath(canvas.gameObject);
                    if (string.IsNullOrEmpty(location)) location = canvas.gameObject.scene.path;
                    if (string.IsNullOrEmpty(location)) location = "<runtime object>";
                    diagnostics.Add("Canvas " + canvas.renderMode + " is outside the browser screen-overlay boundary on "
                        + canvas.gameObject.name + " (" + location + ")");
                }
                properties["sortingOrder"] = canvas.sortingOrder;
                properties["sortingLayerID"] = canvas.sortingLayerID;
                properties["sortingLayerName"] = canvas.sortingLayerName;
                properties["overrideSorting"] = canvas.overrideSorting;
                properties["targetDisplay"] = canvas.targetDisplay;
                properties["renderMode"] = (int)canvas.renderMode;
                properties["planeDistance"] = canvas.planeDistance;
                if (canvas.worldCamera != null)
                    properties["worldCamera"] = Map("$ref", Id(canvas.worldCamera));
            }
            else if (source is CanvasRenderer canvasRenderer)
            {
                type = "UnityEngine.CanvasRenderer";
                Color rendererColor = canvasRenderer.GetColor();
                properties["color"] = Map("$color", Numbers(rendererColor.r, rendererColor.g,
                    rendererColor.b, rendererColor.a));
                properties["cull"] = canvasRenderer.cull;
            }
            else if (source is CanvasGroup canvasGroup)
            {
                type = "UnityEngine.CanvasGroup";
                properties["alpha"] = canvasGroup.alpha;
                properties["interactable"] = canvasGroup.interactable;
                properties["blocksRaycasts"] = canvasGroup.blocksRaycasts;
                properties["ignoreParentGroups"] = canvasGroup.ignoreParentGroups;
            }
            else if (source is UnityEngine.UI.CanvasScaler scaler)
            {
                type = "UnityEngine.UI.CanvasScaler";
                properties["uiScaleMode"] = (int)scaler.uiScaleMode;
                properties["scaleFactor"] = scaler.scaleFactor;
                properties["referencePixelsPerUnit"] = scaler.referencePixelsPerUnit;
                properties["referenceResolution"] = Map("$vector2", Numbers(
                    scaler.referenceResolution.x, scaler.referenceResolution.y));
                properties["screenMatchMode"] = (int)scaler.screenMatchMode;
                properties["matchWidthOrHeight"] = scaler.matchWidthOrHeight;
                properties["physicalUnit"] = (int)scaler.physicalUnit;
                properties["fallbackScreenDPI"] = scaler.fallbackScreenDPI;
                properties["defaultSpriteDPI"] = scaler.defaultSpriteDPI;
            }
            else if (source is UnityEngine.UI.GraphicRaycaster raycaster)
            {
                type = "UnityEngine.UI.GraphicRaycaster";
                properties["ignoreReversedGraphics"] = raycaster.ignoreReversedGraphics;
                properties["blockingObjects"] = (int)raycaster.blockingObjects;
                properties["blockingMask"] = raycaster.blockingMask.value;
            }
            else if (source is UnityEngine.UI.Mask mask)
            {
                type = "UnityEngine.UI.Mask";
                properties["showMaskGraphic"] = mask.showMaskGraphic;
            }
            else if (source is UnityEngine.UI.RectMask2D rectMask)
            {
                type = "UnityEngine.UI.RectMask2D";
                properties["padding"] = Map("$vector4", Numbers(rectMask.padding.x,
                    rectMask.padding.y, rectMask.padding.z, rectMask.padding.w));
                properties["softness"] = Map("$vector2Int", new List<object> {
                    rectMask.softness.x, rectMask.softness.y });
            }
            else if (source is UnityEngine.EventSystems.EventSystem eventSystem)
            {
                type = "UnityEngine.EventSystems.EventSystem";
                properties["sendNavigationEvents"] = eventSystem.sendNavigationEvents;
                properties["pixelDragThreshold"] = eventSystem.pixelDragThreshold;
                if (eventSystem.firstSelectedGameObject != null)
                    properties["firstSelectedGameObject"] = Map("$ref", Id(eventSystem.firstSelectedGameObject));
            }
            else if (source is UnityEngine.EventSystems.StandaloneInputModule inputModule)
            {
                type = "UnityEngine.EventSystems.StandaloneInputModule";
                properties["horizontalAxis"] = inputModule.horizontalAxis;
                properties["verticalAxis"] = inputModule.verticalAxis;
                properties["submitButton"] = inputModule.submitButton;
                properties["cancelButton"] = inputModule.cancelButton;
                properties["inputActionsPerSecond"] = inputModule.inputActionsPerSecond;
                properties["repeatDelay"] = inputModule.repeatDelay;
                if (inputModule.inputOverride != null)
                    properties["inputOverride"] = Map("$ref", Id(inputModule.inputOverride));
            }
            else if (source is UnityEngine.InputSystem.UI.InputSystemUIInputModule uiInputModule)
            {
                type = "UnityEngine.InputSystem.UI.InputSystemUIInputModule";
                properties["deselectOnBackgroundClick"] = uiInputModule.deselectOnBackgroundClick;
                properties["pointerBehavior"] = (int)uiInputModule.pointerBehavior;
                properties["scrollDeltaPerTick"] = uiInputModule.scrollDeltaPerTick;
                properties["moveRepeatDelay"] = uiInputModule.moveRepeatDelay;
                properties["moveRepeatRate"] = uiInputModule.moveRepeatRate;
                if (uiInputModule.actionsAsset != null)
                {
                    if (!string.IsNullOrEmpty(AssetDatabase.GetAssetPath(uiInputModule.actionsAsset)))
                    {
                        ExportScriptableObject(uiInputModule.actionsAsset, diagnostics, assets,
                            scriptableAssetIds);
                        properties["actionsAsset"] = Map("$asset", Id(uiInputModule.actionsAsset));
                    }
                    else diagnostics.Add("Runtime-only UI action asset on " + source.gameObject.name
                        + " (" + Id(source) + ") cannot be exported");
                }
                properties["point"] = ExportUiInputAction(uiInputModule.point, diagnostics,
                    assets, scriptableAssetIds, source);
                properties["scrollWheel"] = ExportUiInputAction(uiInputModule.scrollWheel,
                    diagnostics, assets, scriptableAssetIds, source);
                properties["leftClick"] = ExportUiInputAction(uiInputModule.leftClick,
                    diagnostics, assets, scriptableAssetIds, source);
                properties["middleClick"] = ExportUiInputAction(uiInputModule.middleClick,
                    diagnostics, assets, scriptableAssetIds, source);
                properties["rightClick"] = ExportUiInputAction(uiInputModule.rightClick,
                    diagnostics, assets, scriptableAssetIds, source);
                properties["move"] = ExportUiInputAction(uiInputModule.move, diagnostics,
                    assets, scriptableAssetIds, source);
                properties["submit"] = ExportUiInputAction(uiInputModule.submit, diagnostics,
                    assets, scriptableAssetIds, source);
                properties["cancel"] = ExportUiInputAction(uiInputModule.cancel, diagnostics,
                    assets, scriptableAssetIds, source);
                properties["trackedDevicePosition"] = ExportUiInputAction(
                    uiInputModule.trackedDevicePosition, diagnostics, assets,
                    scriptableAssetIds, source);
                properties["trackedDeviceOrientation"] = ExportUiInputAction(
                    uiInputModule.trackedDeviceOrientation, diagnostics, assets,
                    scriptableAssetIds, source);
            }
            else if (source is UnityEngine.UI.Outline outline)
            {
                type = "UnityEngine.UI.Outline";
                properties["effectColor"] = Map("$color", Numbers(outline.effectColor.r,
                    outline.effectColor.g, outline.effectColor.b, outline.effectColor.a));
                properties["effectDistance"] = Map("$vector2", Numbers(
                    outline.effectDistance.x, outline.effectDistance.y));
                properties["useGraphicAlpha"] = outline.useGraphicAlpha;
            }
            else if (source is UnityEngine.UI.Shadow shadow)
            {
                type = "UnityEngine.UI.Shadow";
                properties["effectColor"] = Map("$color", Numbers(shadow.effectColor.r,
                    shadow.effectColor.g, shadow.effectColor.b, shadow.effectColor.a));
                properties["effectDistance"] = Map("$vector2", Numbers(
                    shadow.effectDistance.x, shadow.effectDistance.y));
                properties["useGraphicAlpha"] = shadow.useGraphicAlpha;
            }
            else if (source is UnityEngine.UI.Image image)
            {
                type = "UnityEngine.UI.Image";
                if (image.material != null && image.material != image.defaultMaterial)
                    diagnostics.Add("Custom uGUI material is outside browser screen UI on "
                        + source.gameObject.name + " (" + Id(source) + ")");
                properties["color"] = Map("$color", Numbers(image.color.r, image.color.g,
                    image.color.b, image.color.a));
                properties["raycastTarget"] = image.raycastTarget;
                properties["maskable"] = image.maskable;
                properties["type"] = (int)image.type;
                properties["preserveAspect"] = image.preserveAspect;
                properties["fillCenter"] = image.fillCenter;
                properties["pixelsPerUnitMultiplier"] = image.pixelsPerUnitMultiplier;
                properties["fillMethod"] = (int)image.fillMethod;
                properties["fillOrigin"] = image.fillOrigin;
                properties["fillClockwise"] = image.fillClockwise;
                properties["fillAmount"] = image.fillAmount;
                if (image.sprite != null)
                    properties["sprite"] = ExportSprite(image.sprite, diagnostics, assets);
            }
            else if (source is UnityEngine.UI.RawImage rawImage)
            {
                type = "UnityEngine.UI.RawImage";
                if (rawImage.material != null && rawImage.material != rawImage.defaultMaterial)
                    diagnostics.Add("Custom uGUI material is outside browser screen UI on "
                        + source.gameObject.name + " (" + Id(source) + ")");
                properties["color"] = Map("$color", Numbers(rawImage.color.r, rawImage.color.g,
                    rawImage.color.b, rawImage.color.a));
                properties["raycastTarget"] = rawImage.raycastTarget;
                properties["maskable"] = rawImage.maskable;
                var uv = rawImage.uvRect;
                properties["uvRect"] = Map("$rect", Numbers(uv.x, uv.y, uv.width, uv.height));
                if (rawImage.texture is Texture2D rawTexture)
                    properties["texture"] = ExportTexture2D(rawTexture, diagnostics, assets);
                else if (rawImage.texture != null)
                    diagnostics.Add("RawImage texture type not converted on " + source.gameObject.name);
            }
            else if (source is LODGroup lodGroup)
            {
                if (lodGroup.fadeMode != LODFadeMode.None)
                    throw new NotSupportedException("LODGroup cross-fade mode is outside the single Three.js renderer mapping on "
                        + source.gameObject.name);
                type = "UnityEngine.LODGroup";
                properties["size"] = lodGroup.size;
                properties["localReferencePoint"] = Map("$vector3", Numbers(
                    lodGroup.localReferencePoint.x, lodGroup.localReferencePoint.y,
                    lodGroup.localReferencePoint.z));
                properties["fadeMode"] = (int)lodGroup.fadeMode;
                properties["animateCrossFading"] = lodGroup.animateCrossFading;
                properties["lastLODBillboard"] = lodGroup.lastLODBillboard;
                properties["lods"] = lodGroup.GetLODs().Select(lod => (object)Map(
                    "screenRelativeTransitionHeight", lod.screenRelativeTransitionHeight,
                    "fadeTransitionWidth", lod.fadeTransitionWidth,
                    "renderers", lod.renderers.Select(renderer => (object)
                        (renderer == null ? null : Map("$ref", Id(renderer)))).ToList())).ToList();
            }
            else if (source is SpriteRenderer spriteRenderer)
            {
                type = "UnityEngine.SpriteRenderer";
                properties["color"] = Map("$color", Numbers(spriteRenderer.color.r,
                    spriteRenderer.color.g, spriteRenderer.color.b, spriteRenderer.color.a));
                properties["sortingLayerID"] = spriteRenderer.sortingLayerID;
                properties["sortingLayerName"] = spriteRenderer.sortingLayerName;
                properties["sortingOrder"] = spriteRenderer.sortingOrder;
                properties["flipX"] = spriteRenderer.flipX;
                properties["flipY"] = spriteRenderer.flipY;
                properties["drawMode"] = (int)spriteRenderer.drawMode;
                properties["tileMode"] = (int)spriteRenderer.tileMode;
                properties["spriteSortPoint"] = (int)spriteRenderer.spriteSortPoint;
                properties["size"] = Map("$vector2", Numbers(spriteRenderer.size.x, spriteRenderer.size.y));
                properties["adaptiveModeThreshold"] = spriteRenderer.adaptiveModeThreshold;
                if (spriteRenderer.sprite != null)
                    properties["sprite"] = ExportSprite(spriteRenderer.sprite, diagnostics, assets);
            }
            else if (source is TMPro.TMP_Text tmp)
            {
                type = source is TMPro.TextMeshProUGUI
                    ? "TMPro.TextMeshProUGUI" : "TMPro.TextMeshPro";
                properties["text"] = tmp.text;
                properties["fontSize"] = tmp.fontSize;
                properties["fontStyle"] = (int)tmp.fontStyle;
                properties["richText"] = tmp.richText;
                properties["color"] = Map("$color", Numbers(tmp.color.r,
                    tmp.color.g, tmp.color.b, tmp.color.a));
                properties["alignment"] = (int)tmp.alignment;
                properties["enableWordWrapping"] = tmp.enableWordWrapping;
                properties["characterSpacing"] = tmp.characterSpacing;
                properties["lineSpacing"] = tmp.lineSpacing;
                properties["margin"] = Map("$vector4", Numbers(tmp.margin.x,
                    tmp.margin.y, tmp.margin.z, tmp.margin.w));
                properties["overflowMode"] = (int)tmp.overflowMode;
                properties["raycastTarget"] = tmp.raycastTarget;
                if (tmp.font != null) properties["font"] = ExportTmpFont(tmp.font, diagnostics, assets);
                if (tmp.richText && !string.IsNullOrEmpty(tmp.text))
                    foreach (System.Text.RegularExpressions.Match tag in
                        System.Text.RegularExpressions.Regex.Matches(tmp.text, @"<[^>]*>"))
                        if (!System.Text.RegularExpressions.Regex.IsMatch(tag.Value,
                            @"^</?[bi]>$|^<size=\d+(\.\d+)?%?>$|^</size>$|^<color=#[0-9a-fA-F]{6}([0-9a-fA-F]{2})?>$|^</color>$"))
                            diagnostics.Add("TMP rich tag " + tag.Value
                                + " is not implemented on " + source.gameObject.name
                                + " (" + Id(source) + ")");
                if (tmp.enableAutoSizing)
                    diagnostics.Add("TMP auto sizing is not implemented on "
                        + source.gameObject.name + " (" + Id(source) + ")");
                if (tmp.spriteAsset != null)
                    diagnostics.Add("TMP sprite asset is not implemented on "
                        + source.gameObject.name + " (" + Id(source) + ")");
                if (tmp.overflowMode != TMPro.TextOverflowModes.Overflow
                    && tmp.overflowMode != TMPro.TextOverflowModes.Truncate)
                    diagnostics.Add("TMP overflow mode " + tmp.overflowMode
                        + " is not implemented on " + source.gameObject.name
                        + " (" + Id(source) + ")");
                if ((tmp.fontStyle & ~(TMPro.FontStyles.Bold | TMPro.FontStyles.Italic)) != 0)
                    diagnostics.Add("TMP font style " + tmp.fontStyle
                        + " is not implemented on " + source.gameObject.name
                        + " (" + Id(source) + ")");
                if (tmp.fontSharedMaterial != null && tmp.font != null
                    && tmp.fontSharedMaterial != tmp.font.material)
                    diagnostics.Add("Custom TMP material is not implemented on "
                        + source.gameObject.name + " (" + Id(source) + ")");
            }
            else if (source is TMPro.TMP_InputField tmpInput)
            {
                type = "TMPro.TMP_InputField";
                properties["text"] = tmpInput.text;
                if (tmpInput.textComponent != null)
                    properties["textComponent"] = Map("$ref", Id(tmpInput.textComponent));
                diagnostics.Add("TMP_InputField editing is stubbed on "
                    + source.gameObject.name + " (" + Id(source) + ")");
            }
            else if (source is TMPro.TMP_Dropdown tmpDropdown)
            {
                type = "TMPro.TMP_Dropdown";
                properties["value"] = tmpDropdown.value;
                if (tmpDropdown.captionText != null)
                    properties["captionText"] = Map("$ref", Id(tmpDropdown.captionText));
                diagnostics.Add("TMP_Dropdown interaction is stubbed on "
                    + source.gameObject.name + " (" + Id(source) + ")");
            }
            else if (source is UnityEngine.UI.Text text)
            {
                type = "UnityEngine.UI.Text";
                if (text.material != null && text.material != text.defaultMaterial)
                    diagnostics.Add("Custom uGUI material is outside browser screen UI on "
                        + source.gameObject.name + " (" + Id(source) + ")");
                properties["text"] = text.text;
                properties["fontSize"] = text.fontSize;
                properties["fontStyle"] = (int)text.fontStyle;
                properties["alignment"] = (int)text.alignment;
                properties["supportRichText"] = text.supportRichText;
                properties["horizontalOverflow"] = (int)text.horizontalOverflow;
                properties["verticalOverflow"] = (int)text.verticalOverflow;
                properties["lineSpacing"] = text.lineSpacing;
                properties["resizeTextForBestFit"] = text.resizeTextForBestFit;
                properties["resizeTextMinSize"] = text.resizeTextMinSize;
                properties["resizeTextMaxSize"] = text.resizeTextMaxSize;
                if (text.font != null)
                {
                    var fontPath = AssetDatabase.GetAssetPath(text.font);
                    var extension = Path.GetExtension(fontPath).ToLowerInvariant();
                    if (extension == ".ttf" || extension == ".otf")
                        properties["font"] = Map("$asset", ExportUIToolkitFontSource(text.font,
                            assets, text.font.name, source.gameObject.scene.path));
                    else if (!fontPath.StartsWith("Library/", StringComparison.OrdinalIgnoreCase))
                        diagnostics.Add("uGUI Text font needs TTF/OTF source on "
                            + source.gameObject.name + " (" + Id(source) + ")");
                }
                properties["color"] = Map("$color", Numbers(text.color.r, text.color.g,
                    text.color.b, text.color.a));
                properties["raycastTarget"] = text.raycastTarget;
                properties["maskable"] = text.maskable;
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
            else if (source is UnityEngine.UI.Dropdown dropdown)
            {
                type = "UnityEngine.UI.Dropdown";
                properties["interactable"] = dropdown.interactable;
                var options = new List<object>();
                foreach (var option in dropdown.options)
                {
                    options.Add(Map("text", option.text ?? ""));
                    if (option.image != null)
                        diagnostics.Add("Dropdown option image is outside browser text-option rendering on "
                            + source.gameObject.name + " (" + Id(dropdown) + ")");
                }
                properties["options"] = options;
                properties["value"] = dropdown.value;
                if (dropdown.template != null) properties["template"] = Map("$ref", Id(dropdown.template));
                if (dropdown.captionText != null) properties["captionText"] = Map("$ref", Id(dropdown.captionText));
                if (dropdown.captionImage != null) properties["captionImage"] = Map("$ref", Id(dropdown.captionImage));
                if (dropdown.itemText != null) properties["itemText"] = Map("$ref", Id(dropdown.itemText));
                if (dropdown.itemImage != null) properties["itemImage"] = Map("$ref", Id(dropdown.itemImage));
                var serialized = new SerializedObject(dropdown);
                var listener = serialized.FindProperty("m_OnValueChanged");
                object changed;
                if (listener != null && TrySerializeField(listener, diagnostics, assets,
                    scriptableAssetIds, out changed)) properties["onValueChanged"] = changed;
            }
            else if (source is UnityEngine.UI.ScrollRect scrollRect)
            {
                type = "UnityEngine.UI.ScrollRect";
                properties["horizontal"] = scrollRect.horizontal;
                properties["vertical"] = scrollRect.vertical;
                properties["movementType"] = (int)scrollRect.movementType;
                properties["elasticity"] = scrollRect.elasticity;
                properties["inertia"] = scrollRect.inertia;
                properties["decelerationRate"] = scrollRect.decelerationRate;
                properties["scrollSensitivity"] = scrollRect.scrollSensitivity;
                if (scrollRect.content != null)
                    properties["content"] = Map("$ref", Id(scrollRect.content));
                if (scrollRect.viewport != null)
                    properties["viewport"] = Map("$ref", Id(scrollRect.viewport));
                if (scrollRect.horizontalScrollbar != null)
                    properties["horizontalScrollbar"] = Map("$ref", Id(scrollRect.horizontalScrollbar));
                if (scrollRect.verticalScrollbar != null)
                    properties["verticalScrollbar"] = Map("$ref", Id(scrollRect.verticalScrollbar));
                properties["horizontalScrollbarVisibility"] = (int)scrollRect.horizontalScrollbarVisibility;
                properties["verticalScrollbarVisibility"] = (int)scrollRect.verticalScrollbarVisibility;
                properties["horizontalScrollbarSpacing"] = scrollRect.horizontalScrollbarSpacing;
                properties["verticalScrollbarSpacing"] = scrollRect.verticalScrollbarSpacing;
                var serialized = new SerializedObject(scrollRect);
                var listener = serialized.FindProperty("m_OnValueChanged");
                object changed;
                if (listener != null && TrySerializeField(listener, diagnostics, assets,
                    scriptableAssetIds, out changed)) properties["onValueChanged"] = changed;
            }
            else if (source is UnityEngine.UI.GridLayoutGroup
                || source is UnityEngine.UI.HorizontalLayoutGroup
                || source is UnityEngine.UI.VerticalLayoutGroup)
            {
                var layoutGroup = (UnityEngine.UI.LayoutGroup)source;
                type = source.GetType().FullName;
                properties["padding"] = Map("left", layoutGroup.padding.left,
                    "right", layoutGroup.padding.right, "top", layoutGroup.padding.top,
                    "bottom", layoutGroup.padding.bottom);
                properties["childAlignment"] = (int)layoutGroup.childAlignment;
                if (layoutGroup is UnityEngine.UI.GridLayoutGroup grid)
                {
                    properties["startCorner"] = (int)grid.startCorner;
                    properties["startAxis"] = (int)grid.startAxis;
                    properties["cellSize"] = Map("$vector2", Numbers(grid.cellSize.x, grid.cellSize.y));
                    properties["spacing"] = Map("$vector2", Numbers(grid.spacing.x, grid.spacing.y));
                    properties["constraint"] = (int)grid.constraint;
                    properties["constraintCount"] = grid.constraintCount;
                }
                else if (layoutGroup is UnityEngine.UI.HorizontalOrVerticalLayoutGroup linear)
                {
                    properties["spacing"] = linear.spacing;
                    properties["childForceExpandWidth"] = linear.childForceExpandWidth;
                    properties["childForceExpandHeight"] = linear.childForceExpandHeight;
                    properties["childControlWidth"] = linear.childControlWidth;
                    properties["childControlHeight"] = linear.childControlHeight;
                    properties["childScaleWidth"] = linear.childScaleWidth;
                    properties["childScaleHeight"] = linear.childScaleHeight;
                    properties["reverseArrangement"] = linear.reverseArrangement;
                }
            }
            else if (source is UnityEngine.UI.LayoutElement layoutElement)
            {
                type = "UnityEngine.UI.LayoutElement";
                properties["ignoreLayout"] = layoutElement.ignoreLayout;
                properties["minWidth"] = layoutElement.minWidth;
                properties["minHeight"] = layoutElement.minHeight;
                properties["preferredWidth"] = layoutElement.preferredWidth;
                properties["preferredHeight"] = layoutElement.preferredHeight;
                properties["flexibleWidth"] = layoutElement.flexibleWidth;
                properties["flexibleHeight"] = layoutElement.flexibleHeight;
                properties["layoutPriority"] = layoutElement.layoutPriority;
            }
            else if (source is UnityEngine.UI.ContentSizeFitter fitter)
            {
                type = "UnityEngine.UI.ContentSizeFitter";
                properties["horizontalFit"] = (int)fitter.horizontalFit;
                properties["verticalFit"] = (int)fitter.verticalFit;
            }
            else if (source is UnityEngine.UI.InputField inputField)
            {
                type = "UnityEngine.UI.InputField";
                properties["interactable"] = inputField.interactable;
                properties["characterLimit"] = inputField.characterLimit;
                properties["contentType"] = (int)inputField.contentType;
                properties["lineType"] = (int)inputField.lineType;
                properties["inputType"] = (int)inputField.inputType;
                properties["characterValidation"] = (int)inputField.characterValidation;
                properties["keyboardType"] = (int)inputField.keyboardType;
                properties["text"] = inputField.text;
                properties["asteriskChar"] = inputField.asteriskChar.ToString();
                properties["caretBlinkRate"] = inputField.caretBlinkRate;
                properties["caretWidth"] = inputField.caretWidth;
                properties["customCaretColor"] = inputField.customCaretColor;
                var caretColor = inputField.caretColor;
                properties["caretColor"] = Map("$color", Numbers(caretColor.r,
                    caretColor.g, caretColor.b, caretColor.a));
                var selectionColor = inputField.selectionColor;
                properties["selectionColor"] = Map("$color", Numbers(selectionColor.r,
                    selectionColor.g, selectionColor.b, selectionColor.a));
                properties["shouldActivateOnSelect"] = inputField.shouldActivateOnSelect;
                properties["readOnly"] = inputField.readOnly;
                if (inputField.textComponent != null)
                    properties["textComponent"] = Map("$ref", Id(inputField.textComponent));
                if (inputField.placeholder != null)
                    properties["placeholder"] = Map("$ref", Id(inputField.placeholder));
                properties["readOnly"] = inputField.readOnly;
                properties["placeholderText"] = (inputField.placeholder as UnityEngine.UI.Text)?.text ?? "";
                var serialized = new SerializedObject(inputField);
                properties["shouldHideMobileInput"] =
                    serialized.FindProperty("m_HideMobileInput")?.boolValue ?? false;
                foreach (var listenerName in new[] { "onValueChanged", "onEndEdit", "onSubmit" })
                {
                    var fieldName = listenerName == "onEndEdit" ? "m_OnDidEndEdit"
                        : listenerName == "onSubmit" ? "m_OnSubmit" : "m_OnValueChanged";
                    var listener = serialized.FindProperty(fieldName);
                    object changed;
                    if (listener != null && TrySerializeField(listener, diagnostics, assets,
                        scriptableAssetIds, out changed)) properties[listenerName] = changed;
                }
            }
            else if (source is UnityEngine.UI.Toggle toggle)
            {
                type = "UnityEngine.UI.Toggle";
                properties["interactable"] = toggle.interactable;
                properties["isOn"] = toggle.isOn;
                if (toggle.graphic != null)
                    properties["graphic"] = Map("$ref", Id(toggle.graphic));
                var serialized = new SerializedObject(toggle);
                object changed;
                var listener = serialized.FindProperty("onValueChanged");
                if (listener != null && TrySerializeField(listener, diagnostics, assets,
                    scriptableAssetIds, out changed)) properties["onValueChanged"] = changed;
            }
            else if (source is UnityEngine.UI.Slider slider)
            {
                type = "UnityEngine.UI.Slider";
                properties["interactable"] = slider.interactable;
                properties["minValue"] = slider.minValue;
                properties["maxValue"] = slider.maxValue;
                properties["wholeNumbers"] = slider.wholeNumbers;
                properties["value"] = slider.value;
                properties["direction"] = (int)slider.direction;
                if (slider.fillRect != null)
                    properties["fillRect"] = Map("$ref", Id(slider.fillRect));
                if (slider.handleRect != null)
                    properties["handleRect"] = Map("$ref", Id(slider.handleRect));
                var serialized = new SerializedObject(slider);
                object changed;
                var listener = serialized.FindProperty("m_OnValueChanged");
                if (listener != null && TrySerializeField(listener, diagnostics, assets,
                    scriptableAssetIds, out changed)) properties["onValueChanged"] = changed;
            }
            else if (source is UnityEngine.UI.Scrollbar scrollbar)
            {
                type = "UnityEngine.UI.Scrollbar";
                properties["interactable"] = scrollbar.interactable;
                properties["size"] = scrollbar.size;
                properties["numberOfSteps"] = scrollbar.numberOfSteps;
                properties["value"] = scrollbar.value;
                properties["direction"] = (int)scrollbar.direction;
                if (scrollbar.handleRect != null)
                    properties["handleRect"] = Map("$ref", Id(scrollbar.handleRect));
                var serialized = new SerializedObject(scrollbar);
                object changed;
                var listener = serialized.FindProperty("m_OnValueChanged");
                if (listener != null && TrySerializeField(listener, diagnostics, assets,
                    scriptableAssetIds, out changed)) properties["onValueChanged"] = changed;
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
            else if (source is UnityEngine.Rendering.SortingGroup)
            {
                diagnostics.Add("Preview defers UnityEngine.Rendering.SortingGroup on "
                    + source.gameObject.name + ": grouped draw ordering needs a Unity-matched mapping");
                return null;
            }
            else if (source is UnityEngine.UI.BaseMeshEffect)
            {
                diagnostics.Add("Custom uGUI mesh effect is outside browser screen UI on "
                    + source.gameObject.name + " (" + Id(source) + ") in "
                    + source.gameObject.scene.path);
                return null;
            }
            else if (source.GetType() == typeof(UnityEngine.EventSystems.BaseInput))
            {
                type = "UnityEngine.EventSystems.BaseInput";
            }
            else if (source is UnityEngine.EventSystems.EventTrigger eventTrigger)
            {
                type = "UnityEngine.EventSystems.EventTrigger";
                var serialized = new SerializedObject(eventTrigger);
                var delegates = serialized.FindProperty("m_Delegates");
                object triggers;
                if (delegates != null && TrySerializeField(delegates, diagnostics, assets,
                    scriptableAssetIds, out triggers)) properties["triggers"] = triggers;
            }
            else if (source is MonoBehaviour script)
            {
                type = source.GetType().FullName;
                AddRuntimeSource(MonoScript.FromMonoBehaviour(script), type);
                ExportScriptFields(script, properties, diagnostics, assets, scriptableAssetIds);
            }
            else if (source.GetType().Assembly.GetName().Name == "UnityEngine.PhysicsModule")
            {
                type = source.GetType().FullName;
                properties["__unsupportedPhysicsType"] = type;
                diagnostics.Add("Browser Physics stub for " + type + " on "
                    + source.gameObject.name + " (" + Id(source) + ") in "
                    + source.gameObject.scene.path);
            }
            else
            {
                diagnostics.Add("Skipped component " + source.GetType().FullName + " on " + source.gameObject.name);
                return null;
            }
            if (source is Collider collider && collider.sharedMaterial != null)
                properties["material"] = ExportPhysicsMaterial(collider.sharedMaterial, assets, scriptableAssetIds);
            if (source is UnityEngine.UI.Selectable selectable)
            {
                var navigation = selectable.navigation;
                var colors = selectable.colors;
                properties["transition"] = (int)selectable.transition;
                if (selectable.targetGraphic != null)
                    properties["targetGraphic"] = Map("$ref", Id(selectable.targetGraphic));
                properties["colors"] = Map(
                    "normalColor", Map("$color", Numbers(colors.normalColor.r,
                        colors.normalColor.g, colors.normalColor.b, colors.normalColor.a)),
                    "highlightedColor", Map("$color", Numbers(colors.highlightedColor.r,
                        colors.highlightedColor.g, colors.highlightedColor.b, colors.highlightedColor.a)),
                    "pressedColor", Map("$color", Numbers(colors.pressedColor.r,
                        colors.pressedColor.g, colors.pressedColor.b, colors.pressedColor.a)),
                    "selectedColor", Map("$color", Numbers(colors.selectedColor.r,
                        colors.selectedColor.g, colors.selectedColor.b, colors.selectedColor.a)),
                    "disabledColor", Map("$color", Numbers(colors.disabledColor.r,
                        colors.disabledColor.g, colors.disabledColor.b, colors.disabledColor.a)),
                    "colorMultiplier", colors.colorMultiplier,
                    "fadeDuration", colors.fadeDuration);
                var spriteState = selectable.spriteState;
                properties["spriteState"] = Map(
                    "highlightedSprite", spriteState.highlightedSprite != null
                        ? ExportSprite(spriteState.highlightedSprite, diagnostics, assets) : null,
                    "pressedSprite", spriteState.pressedSprite != null
                        ? ExportSprite(spriteState.pressedSprite, diagnostics, assets) : null,
                    "selectedSprite", spriteState.selectedSprite != null
                        ? ExportSprite(spriteState.selectedSprite, diagnostics, assets) : null,
                    "disabledSprite", spriteState.disabledSprite != null
                        ? ExportSprite(spriteState.disabledSprite, diagnostics, assets) : null);
                if (selectable.transition == UnityEngine.UI.Selectable.Transition.Animation)
                    diagnostics.Add("Selectable Animation transition is outside browser UI on "
                        + source.gameObject.name + " (" + Id(source) + ") in "
                        + source.gameObject.scene.path);
                properties["navigation"] = Map("mode", (int)navigation.mode,
                    "wrapAround", navigation.wrapAround,
                    "selectOnUp", navigation.selectOnUp != null
                        ? Map("$ref", Id(navigation.selectOnUp)) : null,
                    "selectOnDown", navigation.selectOnDown != null
                        ? Map("$ref", Id(navigation.selectOnDown)) : null,
                    "selectOnLeft", navigation.selectOnLeft != null
                        ? Map("$ref", Id(navigation.selectOnLeft)) : null,
                    "selectOnRight", navigation.selectOnRight != null
                        ? Map("$ref", Id(navigation.selectOnRight)) : null);
            }            if (source is UnityEngine.UI.Shadow)
            {
                var effects = source.GetComponents<UnityEngine.UI.BaseMeshEffect>();
                if (effects.Length > 1 && ReferenceEquals(effects[0], source))
                    diagnostics.Add("Stacked uGUI mesh effects are outside browser screen UI on "
                        + source.gameObject.name + " (" + Id(source) + ") in "
                        + source.gameObject.scene.path);
            }
            bool enabled = !(source is Behaviour behaviour) || behaviour.enabled;
            return Map("id", Id(source), "type", type, "enabled", enabled, "properties", properties);
        }

        private static object ExportUiInputAction(UnityEngine.InputSystem.InputActionReference reference,
            List<object> diagnostics, List<object> assets, HashSet<string> scriptableAssetIds,
            Component owner)
        {
            var action = reference?.action;
            if (action == null) return null;
            var asset = action.actionMap?.asset;
            if (asset == null || string.IsNullOrEmpty(AssetDatabase.GetAssetPath(asset)))
            {
                diagnostics.Add("UI action " + action.name + " on " + owner.gameObject.name
                    + " (" + Id(owner) + ") has no exportable action asset");
                return null;
            }
            ExportScriptableObject(asset, diagnostics, assets, scriptableAssetIds);
            return Map("$uiAction", Map("asset", Id(asset), "id", action.id.ToString(),
                "name", action.name));
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
            var managedReferences = new HashSet<long>();
            bool enterChildren = true;
            while (field.Next(enterChildren))
            {
                enterChildren = false;
                if (field.depth != 0 || field.name.StartsWith("m_", StringComparison.Ordinal)) continue;
                object value;
                if (TrySerializeField(field, diagnostics, assets, scriptableAssetIds, out value,
                    managedReferences))
                    properties[field.name] = value;
            }
        }

        private static bool TrySerializeField(SerializedProperty field, List<object> diagnostics,
            List<object> assets, HashSet<string> scriptableAssetIds, out object value,
            HashSet<long> managedReferences = null)
        {
            value = null;
            if (managedReferences == null) managedReferences = new HashSet<long>();
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
                            "inTangent", key.inTangent, "outTangent", key.outTangent,
                            "inWeight", key.inWeight, "outWeight", key.outWeight,
                            "weightedMode", (int)key.weightedMode));
                    value = Map("$animationCurve", keys,
                        "preWrapMode", (int)field.animationCurveValue.preWrapMode,
                        "postWrapMode", (int)field.animationCurveValue.postWrapMode); return true;
                case SerializedPropertyType.ObjectReference:
                    UnityEngine.Object reference = field.objectReferenceValue;
                    RejectExcludedPreviewAsset(reference);
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
                    if (reference is Sprite sprite)
                    {
                        value = ExportSprite(sprite, diagnostics, assets);
                        return true;
                    }
                    if (reference is Texture2D texture)
                    {
                        value = ExportTexture2D(texture, diagnostics, assets);
                        return true;
                    }
                    if (reference is Material material)
                    {
                        value = ExportStandaloneMaterial(material, diagnostics, assets, scriptableAssetIds);
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
                case SerializedPropertyType.ManagedReference:
                    if (field.managedReferenceValue == null) return true;
                    long referenceId = field.managedReferenceId;
                    string referenceKey = referenceId.ToString(CultureInfo.InvariantCulture);
                    if (!managedReferences.Add(referenceId))
                    {
                        value = Map("$managedRef", referenceKey);
                        return true;
                    }
                    Type runtimeType = field.managedReferenceValue.GetType();
                    value = Map("$managedRef", referenceKey,
                        "$serializedType", runtimeType.FullName.Replace('+', '.'),
                        "properties", SerializeChildProperties(field, diagnostics, assets,
                            scriptableAssetIds, managedReferences));
                    return true;
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
                            // Preserve disabled calls so runtime listener counts and state changes match Unity.
                            object targetValue;
                            if (target == null || method == null || string.IsNullOrEmpty(method.stringValue)
                                || !TrySerializeField(target, diagnostics, assets, scriptableAssetIds,
                                    out targetValue, managedReferences)
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
                                case 2:
                                    object objectArgument;
                                    var objectField = argument == null ? null
                                        : argument.FindPropertyRelative("m_ObjectArgument");
                                    if (objectField == null || !TrySerializeField(
                                        objectField, diagnostics, assets,
                                        scriptableAssetIds, out objectArgument, managedReferences))
                                    {
                                        diagnostics.Add("Unsupported persistent UnityEvent object argument: " + field.propertyPath);
                                        continue;
                                    }
                                    arguments = new List<object> { objectArgument };
                                    break;
                                case 3: arguments = new List<object> { argument.FindPropertyRelative("m_IntArgument").intValue }; break;
                                case 4: arguments = new List<object> { argument.FindPropertyRelative("m_FloatArgument").floatValue }; break;
                                case 5: arguments = new List<object> { argument.FindPropertyRelative("m_StringArgument").stringValue }; break;
                                case 6: arguments = new List<object> { argument.FindPropertyRelative("m_BoolArgument").boolValue }; break;
                                default:
                                    diagnostics.Add("Unsupported persistent UnityEvent argument: " + field.propertyPath);
                                    continue;
                            }
                            listeners.Add(Map("target", targetValue, "method", method.stringValue,
                                "args", arguments, "state", state == null ? 2 : state.enumValueIndex));
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
                                assets, scriptableAssetIds, out item, managedReferences)) items.Add(item);
                        }
                        value = items;
                        return true;
                    }
                    var entries = SerializeChildProperties(field, diagnostics, assets,
                        scriptableAssetIds, managedReferences);
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

        private static Dictionary<string, object> SerializeChildProperties(SerializedProperty field,
            List<object> diagnostics, List<object> assets, HashSet<string> scriptableAssetIds,
            HashSet<long> managedReferences)
        {
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
                    if (TrySerializeField(cursor, diagnostics, assets, scriptableAssetIds,
                        out child, managedReferences)) entries[cursor.name] = child;
                } while (cursor.NextVisible(false));
            }
            return entries;
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

        private static List<object> ExportLegacyAxes()
        {
            var result = new List<object>();
            var manager = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/InputManager.asset")
                .FirstOrDefault();
            if (manager == null) return result;
            var axes = new SerializedObject(manager).FindProperty("m_Axes");
            if (axes == null) return result;
            for (int index = 0; index < axes.arraySize; index++)
            {
                var axis = axes.GetArrayElementAtIndex(index);
                result.Add(Map("m_Name", axis.FindPropertyRelative("m_Name").stringValue,
                    "negativeButton", axis.FindPropertyRelative("negativeButton").stringValue,
                    "positiveButton", axis.FindPropertyRelative("positiveButton").stringValue,
                    "altNegativeButton", axis.FindPropertyRelative("altNegativeButton").stringValue,
                    "altPositiveButton", axis.FindPropertyRelative("altPositiveButton").stringValue,
                    "gravity", axis.FindPropertyRelative("gravity").floatValue,
                    "dead", axis.FindPropertyRelative("dead").floatValue,
                    "sensitivity", axis.FindPropertyRelative("sensitivity").floatValue,
                    "snap", axis.FindPropertyRelative("snap").boolValue ? 1 : 0,
                    "invert", axis.FindPropertyRelative("invert").boolValue ? 1 : 0,
                    "type", axis.FindPropertyRelative("type").intValue,
                    "axis", axis.FindPropertyRelative("axis").intValue,
                    "joyNum", axis.FindPropertyRelative("joyNum").intValue));
            }
            return result;
        }

        private static void ExportScriptableObject(ScriptableObject source, List<object> diagnostics,
            List<object> assets, HashSet<string> scriptableAssetIds)
        {
            RejectExcludedPreviewAsset(source);
            string id = Id(source);
            if (!scriptableAssetIds.Add(id)) return;
            if (source.GetType().FullName == "UnityEngine.InputSystem.InputActionAsset")
            {
                string assetPath = AssetDatabase.GetAssetPath(source);
                string projectRoot = Directory.GetParent(Application.dataPath).FullName;
                string file = Path.Combine(projectRoot, assetPath.Replace('/', Path.DirectorySeparatorChar));
                assets.Add(Map("id", id, "kind", "inputActionAsset", "name", source.name,
                    "dataJson", File.ReadAllText(file)));
                return;
            }
            if (source is UnityEngine.UIElements.StyleSheet sheet)
            {
                string path = AssetDatabase.GetAssetPath(sheet);
                if (string.IsNullOrEmpty(path) || !File.Exists(path))
                    throw new InvalidOperationException("Script StyleSheet USS source missing: " + sheet.name);
                string css = File.ReadAllText(path);
                var styleSources = new List<Tuple<string, string>> { Tuple.Create(path, css) };
                var fonts = ExportUIToolkitFonts(styleSources, assets);
                var images = ExportUIToolkitImages(styleSources, assets);
                assets.Add(Map("id", id, "kind", "uiStyleSheet", "name", sheet.name,
                    "data", Map("css", css, "fontAssets", fonts, "imageAssets", images)));
                return;
            }
            var properties = new Dictionary<string, object>();
            var record = Map("id", id, "kind", "scriptableObject", "type", source.GetType().FullName,
                "name", source.name, "properties", properties);
            assets.Add(record);
            AddRuntimeSource(MonoScript.FromScriptableObject(source), source.GetType().FullName);
            ExportScriptFields(source, properties, diagnostics, assets, scriptableAssetIds);
        }

        private static void AddRuntimeSource(MonoScript script, string typeName)
        {
            if (exportedRuntimeSources == null || script == null) return;
            string assetPath = AssetDatabase.GetAssetPath(script);
            if (string.IsNullOrWhiteSpace(assetPath)
                || !assetPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase)
                || assetPath.IndexOf("/Editor/", StringComparison.OrdinalIgnoreCase) >= 0
                || assetPath.IndexOf("/Editor Library/", StringComparison.OrdinalIgnoreCase) >= 0)
                return;
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string sourcePath = Path.Combine(projectRoot, assetPath.Replace('/', Path.DirectorySeparatorChar));
            if (!exportedRuntimeSources.Any(value => value.path == sourcePath && value.typeName == typeName))
                exportedRuntimeSources.Add((sourcePath, typeName));
        }

        private static void AddAnimatorRuntimeSources(AnimatorController controller)
        {
            foreach (var layer in controller.layers)
                AddAnimatorRuntimeSources(layer.stateMachine);
        }

        private static void AddAnimatorRuntimeSources(AnimatorStateMachine machine)
        {
            foreach (var child in machine.states)
                foreach (var behaviour in child.state.behaviours)
                    if (behaviour != null)
                        AddRuntimeSource(MonoScript.FromScriptableObject(behaviour),
                            behaviour.GetType().FullName);
            foreach (var child in machine.stateMachines)
                AddAnimatorRuntimeSources(child.stateMachine);
        }

        private static void ExportUIToolkitAssets(UnityEngine.UIElements.UIDocument document,
            List<object> assets)
        {
            var panel = document.panelSettings;
            if (panel != null && !HasExportedAsset(assets, Id(panel)))
            {
                if (panel.targetTexture != null || panel.targetDisplay != 0
                    || panel.renderMode != UnityEngine.UIElements.PanelRenderMode.ScreenSpaceOverlay)
                    throw new InvalidOperationException("UIDocument panel target texture/display/mode is outside browser screen UI on "
                        + document.gameObject.name);
                var themePath = AssetDatabase.GetAssetPath(panel.themeStyleSheet);
                var themeCss = !string.IsNullOrEmpty(themePath) && File.Exists(themePath)
                    ? File.ReadAllText(themePath) : "";
                var themeSources = new List<Tuple<string, string>>();
                if (themeCss.Length > 0) themeSources.Add(Tuple.Create(themePath, themeCss));
                var themeFonts = ExportUIToolkitFonts(themeSources, assets);
                var themeImages = ExportUIToolkitImages(themeSources, assets);
                assets.Add(Map("id", Id(panel), "kind", "uiPanelSettings", "name", panel.name,
                    "data", Map("scaleMode", (int)panel.scaleMode, "scale", panel.scale,
                        "referenceResolution", Numbers(panel.referenceResolution.x, panel.referenceResolution.y),
                        "screenMatchMode", (int)panel.screenMatchMode, "match", panel.match,
                        "sortingOrder", panel.sortingOrder, "targetDisplay", panel.targetDisplay,
                        "fallbackDpi", panel.fallbackDpi, "referenceDpi", panel.referenceDpi,
                        "referenceSpritePixelsPerUnit", panel.referenceSpritePixelsPerUnit,
                        "clearColor", panel.clearColor, "clearDepthStencil", panel.clearDepthStencil,
                        "colorClearValue", Numbers(panel.colorClearValue.r, panel.colorClearValue.g,
                            panel.colorClearValue.b, panel.colorClearValue.a),
                        "depthClearValue", panel.depthClearValue, "renderMode", (int)panel.renderMode,
                        "forceGammaRendering", panel.forceGammaRendering,
                        "themeName", panel.themeStyleSheet != null ? panel.themeStyleSheet.name : "",
                        "themeCss", themeCss, "themeFontAssets", themeFonts,
                        "themeImageAssets", themeImages)));
            }
            var tree = document.visualTreeAsset;
            if (tree == null || HasExportedAsset(assets, Id(tree))) return;
            var path = AssetDatabase.GetAssetPath(tree);
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                throw new InvalidOperationException("UIDocument UXML source missing on " + document.gameObject.name);
            var xml = new System.Xml.XmlDocument { XmlResolver = null };
            xml.Load(path);
            var stylesheets = new List<object>();
            var styleSources = new List<Tuple<string, string>>();
            foreach (var sheet in tree.stylesheets)
            {
                if (sheet == null) continue;
                var sheetPath = AssetDatabase.GetAssetPath(sheet);
                if (string.IsNullOrEmpty(sheetPath) || !File.Exists(sheetPath))
                    throw new InvalidOperationException("UIDocument USS source missing: " + sheet.name);
                var css = File.ReadAllText(sheetPath);
                stylesheets.Add(css);
                styleSources.Add(Tuple.Create(sheetPath, css));
            }
            var treeNode = ExportUxmlNode(xml.DocumentElement, path,
                new HashSet<string>(StringComparer.OrdinalIgnoreCase) { Path.GetFullPath(path) },
                stylesheets, styleSources);
            var fonts = ExportUIToolkitFonts(styleSources, assets);
            var images = ExportUIToolkitImages(styleSources, assets);
            assets.Add(Map("id", Id(tree), "kind", "uiVisualTree", "name", tree.name,
                "data", Map("tree", treeNode, "stylesheets", stylesheets,
                    "fontAssets", fonts, "imageAssets", images)));
        }

        private static List<object> ExportUIToolkitImages(List<Tuple<string, string>> sources,
            List<object> assets)
        {
            var result = new List<object>();
            var references = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var source in sources)
            {
                var style = System.Text.RegularExpressions.Regex.Replace(source.Item2,
                    @"/\*.*?\*/", "", System.Text.RegularExpressions.RegexOptions.Singleline);
                foreach (System.Text.RegularExpressions.Match match in
                    System.Text.RegularExpressions.Regex.Matches(style,
                        @"(?:background-image|--unity-image)\s*:\s*url\(\s*[""']?(?<reference>[^""')]+)[""']?\s*\)",
                        System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                {
                    var reference = match.Groups["reference"].Value.Trim();
                    var imagePath = ResolveUxmlReference(source.Item1, reference);
                    var extension = Path.GetExtension(imagePath).ToLowerInvariant();
                    if (extension != ".png" && extension != ".jpg" && extension != ".jpeg")
                        throw new InvalidOperationException("UI Toolkit image needs PNG/JPEG source: "
                            + reference + " in " + source.Item1);
                    var projectRoot = Path.GetFullPath(Directory.GetParent(Application.dataPath).FullName)
                        .TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
                    if (!imagePath.StartsWith(projectRoot, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException("UI Toolkit image outside project: "
                            + reference + " in " + source.Item1);
                    var assetPath = imagePath.Substring(projectRoot.Length).Replace('\\', '/');
                    var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
                    if (texture == null)
                        throw new InvalidOperationException("UI Toolkit image failed to import: "
                            + reference + " in " + source.Item1);
                    var id = Id(texture);
                    if (references.TryGetValue(reference, out var prior))
                    {
                        if (prior != id)
                            throw new InvalidOperationException("Ambiguous UI Toolkit image reference: "
                                + reference + " in " + source.Item1);
                        continue;
                    }
                    references.Add(reference, id);
                    if (!HasExportedAsset(assets, id))
                    {
                        var filename = HashId(id) + extension;
                        var relativePath = "textures/" + filename;
                        var target = Path.Combine(exportOutputDirectory, "textures", filename);
                        Directory.CreateDirectory(Path.GetDirectoryName(target));
                        File.Copy(imagePath, target, true);
                        exportedTextureIds.Add(id);
                        assets.Add(Map("id", id, "kind", "texture", "path", relativePath,
                            "name", texture.name));
                    }
                    result.Add(Map("reference", reference, "assetId", id));
                }
            }
            return result;
        }

        private static List<object> ExportUIToolkitFonts(List<Tuple<string, string>> sources,
            List<object> assets)
        {
            var result = new List<object>();
            var references = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var source in sources)
            {
                var style = System.Text.RegularExpressions.Regex.Replace(source.Item2,
                    @"/\*.*?\*/", "", System.Text.RegularExpressions.RegexOptions.Singleline);
                if (System.Text.RegularExpressions.Regex.IsMatch(style,
                    @"-unity-font(?:-definition)?\s*:\s*resource\s*\(",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                    throw new InvalidOperationException("UI Toolkit resource font needs project asset export: "
                        + source.Item1);
                foreach (System.Text.RegularExpressions.Match match in
                    System.Text.RegularExpressions.Regex.Matches(style,
                        @"-unity-font(?<definition>-definition)?\s*:\s*url\(\s*[""']?(?<reference>[^""')]+)[""']?\s*\)",
                        System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                {
                    var reference = match.Groups["reference"].Value.Trim();
                    var fontPath = ResolveUxmlReference(source.Item1, reference);
                    var extension = Path.GetExtension(fontPath).ToLowerInvariant();
                    var projectRoot = Path.GetFullPath(Directory.GetParent(Application.dataPath).FullName)
                        .TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
                    if (!fontPath.StartsWith(projectRoot, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException("UI Toolkit font outside project: "
                            + reference + " in " + source.Item1);
                    var assetPath = fontPath.Substring(projectRoot.Length).Replace('\\', '/');
                    var ids = new List<string>();
                    if (match.Groups["definition"].Success)
                    {
                        var fontAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.TextCore.Text.FontAsset>(assetPath);
                        if (fontAsset == null)
                            throw new InvalidOperationException("UI Toolkit FontAsset failed to import: "
                                + reference + " in " + source.Item1);
                        AddUIToolkitFontAsset(fontAsset, assets, ids,
                            new HashSet<UnityEngine.TextCore.Text.FontAsset>(), reference, source.Item1);
                    }
                    else
                    {
                        if (extension != ".ttf" && extension != ".otf")
                            throw new InvalidOperationException("UI Toolkit font needs TTF/OTF source: "
                                + reference + " in " + source.Item1);
                        var font = AssetDatabase.LoadAssetAtPath<Font>(assetPath);
                        if (font == null)
                            throw new InvalidOperationException("UI Toolkit font failed to import: "
                                + reference + " in " + source.Item1);
                        ids.Add(ExportUIToolkitFontSource(font, assets, reference, source.Item1));
                    }
                    var id = ids[0];
                    if (references.TryGetValue(reference, out var prior))
                    {
                        if (prior != id)
                            throw new InvalidOperationException("Ambiguous UI Toolkit font reference: "
                                + reference + " in " + source.Item1);
                        continue;
                    }
                    references.Add(reference, id);
                    var fallbackIds = new List<object>();
                    for (var i = 1; i < ids.Count; i++) fallbackIds.Add(ids[i]);
                    result.Add(Map("reference", reference, "assetId", id,
                        "fallbackAssetIds", fallbackIds));
                }
            }
            return result;
        }

        private static void AddUIToolkitFontAsset(UnityEngine.TextCore.Text.FontAsset asset,
            List<object> assets, List<string> ids,
            HashSet<UnityEngine.TextCore.Text.FontAsset> visited, string reference, string styleSource)
        {
            if (!visited.Add(asset)) return;
            if (asset.sourceFontFile == null)
                throw new InvalidOperationException("UI Toolkit FontAsset has no source TTF/OTF: "
                    + reference + " (" + asset.name + ") in " + styleSource);
            ids.Add(ExportUIToolkitFontSource(asset.sourceFontFile, assets, reference, styleSource));
            foreach (var fallback in asset.fallbackFontAssetTable
                ?? new List<UnityEngine.TextCore.Text.FontAsset>())
                if (fallback != null)
                    AddUIToolkitFontAsset(fallback, assets, ids, visited, reference, styleSource);
        }

        private static string ExportUIToolkitFontSource(Font font, List<object> assets,
            string reference, string styleSource)
        {
            var assetPath = AssetDatabase.GetAssetPath(font);
            var extension = Path.GetExtension(assetPath).ToLowerInvariant();
            if (extension != ".ttf" && extension != ".otf")
                throw new InvalidOperationException("UI Toolkit font needs TTF/OTF source: "
                    + reference + " (" + font.name + ") in " + styleSource);
            var projectRoot = Path.GetFullPath(Directory.GetParent(Application.dataPath).FullName)
                .TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            var fullPath = Path.GetFullPath(Path.Combine(projectRoot, assetPath));
            if (!fullPath.StartsWith(projectRoot, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("UI Toolkit font outside project: "
                    + reference + " in " + styleSource);
            var id = Id(font);
            if (!HasExportedAsset(assets, id))
            {
                var filename = HashId(id) + extension;
                var relativePath = "fonts/" + filename;
                var target = Path.Combine(exportOutputDirectory, "fonts", filename);
                Directory.CreateDirectory(Path.GetDirectoryName(target));
                File.Copy(fullPath, target, true);
                assets.Add(Map("id", id, "kind", "uiFont", "path", relativePath,
                    "name", font.name));
            }
            return id;
        }

        private static string ResolveUxmlReference(string sourcePath, string reference)
        {
            if (string.IsNullOrWhiteSpace(reference))
                throw new InvalidOperationException("UXML dependency needs a src on " + sourcePath);
            var clean = reference.Split(new[] { '?', '#' }, 2)[0].Replace('\\', '/');
            const string projectPrefix = "project://database/";
            if (clean.StartsWith(projectPrefix, StringComparison.OrdinalIgnoreCase))
                clean = clean.Substring(projectPrefix.Length);
            else if (clean.StartsWith("/Assets/", StringComparison.OrdinalIgnoreCase)
                || clean.StartsWith("/Packages/", StringComparison.OrdinalIgnoreCase))
                clean = clean.Substring(1);
            else if (!clean.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase)
                && !clean.StartsWith("Packages/", StringComparison.OrdinalIgnoreCase))
                clean = Path.Combine(Path.GetDirectoryName(sourcePath), clean);
            if (!File.Exists(clean))
                throw new InvalidOperationException("UXML dependency missing: " + reference + " on " + sourcePath);
            return Path.GetFullPath(clean);
        }

        private static Dictionary<string, object> ExportUxmlNode(System.Xml.XmlElement element,
            string sourcePath, HashSet<string> activeSources, List<object> stylesheets,
            List<Tuple<string, string>> styleSources)
        {
            if (!new HashSet<string> { "UXML", "VisualElement", "TextElement", "Label", "Box",
                "GroupBox", "PopupWindow", "HelpBox", "Image", "Button", "RepeatButton",
                "TextField", "Toggle", "RadioButton", "RadioButtonGroup", "ToggleButtonGroup", "RectField", "RectIntField", "BoundsField", "BoundsIntField", "TwoPaneSplitView", "Foldout",
                "Slider", "SliderInt", "MinMaxSlider", "EnumField", "Hash128Field", "IntegerField", "FloatField",
                "DoubleField", "LongField", "UnsignedIntegerField", "UnsignedLongField",
                "Vector2Field", "Vector3Field", "Vector4Field",
                "Vector2IntField", "Vector3IntField",
                "DropdownField", "ProgressBar", "ScrollView", "Scroller", "Tab", "TabView",
                "ListView", "TreeView", "MultiColumnListView", "MultiColumnTreeView", "Columns", "Column", "SortColumnDescriptions", "SortColumnDescription", "TemplateContainer", "Style",
                "Template", "Instance", "AttributeOverrides" }.Contains(element.LocalName))
                throw new InvalidOperationException("Unsupported UXML element " + element.LocalName
                    + " in " + sourcePath);
            var attributes = new Dictionary<string, object>();
            foreach (System.Xml.XmlAttribute attribute in element.Attributes)
                if (attribute.Prefix != "xmlns" && attribute.Name != "xmlns")
                {
                    attributes[attribute.LocalName] = attribute.Value;
                    if (attribute.LocalName == "style")
                        styleSources.Add(Tuple.Create(sourcePath, attribute.Value));
                    if (element.LocalName == "Tab" && attribute.LocalName == "icon-image")
                        styleSources.Add(Tuple.Create(sourcePath,
                            "background-image: url(" + attribute.Value + ")"));
                }
            if (element.LocalName == "Template")
            {
                var dependencyPath = ResolveUxmlReference(sourcePath, element.GetAttribute("src"));
                if (!activeSources.Add(dependencyPath))
                    throw new InvalidOperationException("Cyclic UXML template dependency: " + dependencyPath);
                try
                {
                    var dependency = new System.Xml.XmlDocument { XmlResolver = null };
                    dependency.Load(dependencyPath);
                    return Map("type", "Template", "attributes", attributes,
                        "templateTree", ExportUxmlNode(dependency.DocumentElement,
                            dependencyPath, activeSources, stylesheets, styleSources),
                        "children", new List<object>());
                }
                finally { activeSources.Remove(dependencyPath); }
            }
            if (element.LocalName == "Style" && element.HasAttribute("path"))
                throw new InvalidOperationException("UXML Style path needs an exported src on " + sourcePath);
            if (element.LocalName == "Style" && element.HasAttribute("src"))
            {
                var sheetPath = ResolveUxmlReference(sourcePath, element.GetAttribute("src"));
                var sheet = File.ReadAllText(sheetPath);
                if (!stylesheets.Contains(sheet)) stylesheets.Add(sheet);
                styleSources.Add(Tuple.Create(sheetPath, sheet));
            }
            var children = new List<object>();
            foreach (System.Xml.XmlNode child in element.ChildNodes)
                if (child is System.Xml.XmlElement nested)
                    children.Add(ExportUxmlNode(nested, sourcePath, activeSources,
                        stylesheets, styleSources));
            return Map("type", element.LocalName, "attributes", attributes, "children", children);
        }

        private static bool HasExportedAsset(List<object> assets, string id)
        {
            foreach (var item in assets)
                if (item is Dictionary<string, object> asset && asset.TryGetValue("id", out var value)
                    && value is string existing && existing == id) return true;
            return false;
        }

        private static object ExportTerrainData(TerrainData data, List<object> assets)
        {
            if (data == null) return null;
            string id = Id(data);
            if (HasExportedAsset(assets, id)) return Map("$asset", id);
            int resolution = data.heightmapResolution;
            float[,] sourceHeights = data.GetHeights(0, 0, resolution, resolution);
            var heights = new List<object>(resolution * resolution);
            for (int y = 0; y < resolution; y++)
                for (int x = 0; x < resolution; x++) heights.Add(sourceHeights[y, x]);
            int holeResolution = data.holesResolution;
            bool[,] sourceHoles = data.GetHoles(0, 0, holeResolution, holeResolution);
            var holes = new List<object>(holeResolution * holeResolution);
            for (int y = 0; y < holeResolution; y++)
                for (int x = 0; x < holeResolution; x++) holes.Add(!sourceHoles[y, x]);
            string relativePath = "terrain/" + HashId(id) + ".unityterrain.json";
            string target = Path.Combine(exportOutputDirectory, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(target));
            Vector3 size = data.size;
            var payload = Map("format", "unity-terrain-data/v1",
                "size", Numbers(size.x, size.y, size.z), "resolution", resolution,
                "heights", heights, "holes", holes);
            int layerCount = data.terrainLayers.Length;
            if (layerCount >= 2 && layerCount <= 8)
            {
                int alphaResolution = data.alphamapResolution;
                float[,,] sourceAlphas = data.GetAlphamaps(0, 0,
                    alphaResolution, alphaResolution);
                var alphas = new List<object>(alphaResolution * alphaResolution * layerCount);
                for (int y = 0; y < alphaResolution; y++)
                    for (int x = 0; x < alphaResolution; x++)
                        for (int layer = 0; layer < layerCount; layer++)
                            alphas.Add(sourceAlphas[y, x, layer]);
                payload["alphamapResolution"] = alphaResolution;
                payload["alphamapLayers"] = layerCount;
                payload["alphamaps"] = alphas;
            }
            File.WriteAllText(target, Json(payload), new UTF8Encoding(false));
            assets.Add(Map("id", id, "kind", "terrainData", "name", data.name,
                "path", relativePath));
            return Map("$asset", id);
        }

        private static string Id(UnityEngine.Object value)
        {
            return GlobalObjectId.GetGlobalObjectIdSlow(value).ToString();
        }

        private static Dictionary<string, object> Vec3(Vector3 value)
        {
            return Map("$vector3", Numbers(value.x, value.y, value.z));
        }

        private static Dictionary<string, object> ExportAvatarMetadata(Avatar avatar)
        {
            var record = Map("id", AssetDatabase.Contains(avatar) ? Id(avatar) : null,
                "name", avatar.name,
                "isValid", avatar.isValid, "isHuman", avatar.isHuman);
            if (!avatar.isHuman) return record;
            var description = avatar.humanDescription;
            var human = new List<object>();
            foreach (var bone in description.human ?? Array.Empty<HumanBone>())
                human.Add(Map("humanName", bone.humanName, "boneName", bone.boneName,
                    "limit", Map("useDefaultValues", bone.limit.useDefaultValues,
                        "min", Numbers(bone.limit.min.x, bone.limit.min.y, bone.limit.min.z),
                        "max", Numbers(bone.limit.max.x, bone.limit.max.y, bone.limit.max.z),
                        "center", Numbers(bone.limit.center.x, bone.limit.center.y,
                            bone.limit.center.z), "axisLength", bone.limit.axisLength)));
            var skeleton = new List<object>();
            foreach (var bone in description.skeleton ?? Array.Empty<SkeletonBone>())
                skeleton.Add(Map("name", bone.name,
                    "position", Numbers(bone.position.x, bone.position.y, bone.position.z),
                    "rotation", Numbers(bone.rotation.x, bone.rotation.y,
                        bone.rotation.z, bone.rotation.w),
                    "scale", Numbers(bone.scale.x, bone.scale.y, bone.scale.z)));
            record["humanDescription"] = Map("human", human, "skeleton", skeleton,
                "upperArmTwist", description.upperArmTwist,
                "lowerArmTwist", description.lowerArmTwist,
                "upperLegTwist", description.upperLegTwist,
                "lowerLegTwist", description.lowerLegTwist,
                "armStretch", description.armStretch,
                "legStretch", description.legStretch,
                "feetSpacing", description.feetSpacing,
                "hasTranslationDoF", description.hasTranslationDoF);
            return record;
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
            assets.Add(Map("id", id, "kind", "audio", "path", "audio/" + targetName,
                "name", clip.name, "length", clip.length, "samples", clip.samples,
                "frequency", clip.frequency, "channels", clip.channels));
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

        private static object ExportStandaloneMaterial(Material material,
            List<object> diagnostics, List<object> assets, HashSet<string> assetIds)
        {
            string id = Id(material);
            if (assetIds.Add(id))
            {
                var data = ExportMaterial(material, diagnostics);
                if (data.ContainsKey("texturePng"))
                {
                    bool juTransparency = data["shaderGuid"].ToString()
                        == "d20272b3c7d3d5b40bd5ef601dc742a4";
                    string textureProperty = juTransparency && material.HasProperty("main_texture") ? "main_texture"
                : material.HasProperty("_BaseMap") ? "_BaseMap" : "_MainTex";
            Texture texture = juTransparency && material.HasProperty("main_texture")
                        ? material.GetTexture("main_texture")
                        : material.HasProperty("_BaseMap") ? material.GetTexture("_BaseMap")
                        : material.mainTexture;
                    if (texture is Texture2D image
                        && ExportTexture2D(image, diagnostics, assets) != null)
                        data["textureAssetId"] = Id(image);
                    data.Remove("texturePng");
                    data.Remove("textureId");
                }
                assets.Add(Map("id", id, "kind", "material", "name", material.name,
                    "data", data));
            }
            return Map("$asset", id);
        }

        private static object ExportSprite(Sprite sprite,
            List<object> diagnostics, List<object> assets)
        {
            if (sprite.packed)
            {
                diagnostics.Add("Packed Sprite atlas is not mapped on " + sprite.name);
                return null;
            }
            if (ExportTexture2D(sprite.texture, diagnostics, assets) == null) return null;
            string id = Id(sprite);
            if (exportedSpriteIds.Add(id))
            {
                Rect rect = sprite.rect;
                Vector2 pivot = sprite.pivot;
                assets.Add(Map("id", id, "kind", "sprite", "name", sprite.name,
                    "data", Map("textureAssetId", Id(sprite.texture),
                        "textureWidth", sprite.texture.width,
                        "textureHeight", sprite.texture.height,
                        "rect", Numbers(rect.x, rect.y, rect.width, rect.height),
                        "pivot", Numbers(pivot.x / rect.width, pivot.y / rect.height),
                        "border", Numbers(sprite.border.x, sprite.border.y,
                            sprite.border.z, sprite.border.w),
                        "textureWrapMode", (int)sprite.texture.wrapMode,
                        "textureFilterMode", (int)sprite.texture.filterMode,
                        "pixelsPerUnit", sprite.pixelsPerUnit)));
            }
            return Map("$asset", id);
        }

        private static object ExportTmpFont(TMPro.TMP_FontAsset font,
            List<object> diagnostics, List<object> assets)
        {
            string id = Id(font);
            if (exportedTmpFontIds.Contains(id)) return Map("$asset", id);
            if (font.atlasTextures == null || font.atlasTextures.Length != 1
                || !(font.atlasTextures[0] is Texture2D atlas))
            {
                diagnostics.Add("TMP font needs one 2D atlas: " + font.name + " (" + id + ")");
                return null;
            }
            if (ExportTexture2D(atlas, diagnostics, assets, true) == null) return null;
            exportedTmpFontIds.Add(id);
            var glyphs = new List<object>();
            foreach (var character in font.characterTable)
            {
                var glyph = character.glyph;
                if (glyph == null || glyph.atlasIndex != 0) continue;
                var rect = glyph.glyphRect;
                var metrics = glyph.metrics;
                glyphs.Add(Map("unicode", (long)character.unicode,
                    "x", rect.x, "y", rect.y, "width", rect.width, "height", rect.height,
                    "bearingX", metrics.horizontalBearingX,
                    "bearingY", metrics.horizontalBearingY,
                    "advance", metrics.horizontalAdvance));
            }
            var fallbackIds = new List<object>();
            if (font.fallbackFontAssetTable != null)
                foreach (var fallback in font.fallbackFontAssetTable)
                    if (fallback != null && ExportTmpFont(fallback, diagnostics, assets) != null)
                        fallbackIds.Add(Id(fallback));
            assets.Add(Map("id", id, "kind", "tmpFont", "name", font.name,
                "data", Map("atlasTextureAssetId", Id(atlas),
                    "fallbackFontAssetIds", fallbackIds,
                    "pointSize", font.faceInfo.pointSize,
                    "lineHeight", font.faceInfo.lineHeight,
                    "ascentLine", font.faceInfo.ascentLine,
                    "descentLine", font.faceInfo.descentLine,
                    "boldStyle", font.boldStyle,
                    "boldSpacing", font.boldSpacing,
                    "gradientScale", font.material != null
                        && font.material.HasProperty("_GradientScale")
                        ? font.material.GetFloat("_GradientScale") : 10f,
                    "glyphs", glyphs)));
            return Map("$asset", id);
        }

        private static object ExportTexture2D(Texture2D texture,
            List<object> diagnostics, List<object> assets, bool tmpAtlas = false)
        {
            string id = Id(texture);
            if (exportedTextureIds.Add(id))
            {
                try
                {
                    string filename = HashId(id) + ".png";
                    string relativePath = "textures/" + filename;
                    string target = Path.Combine(exportOutputDirectory, "textures", filename);
                    Directory.CreateDirectory(Path.GetDirectoryName(target));
                    File.WriteAllBytes(target, tmpAtlas ? ReadTmpAtlasPng(texture)
                        : ReadTexturePng(texture));
                    assets.Add(Map("id", id, "kind", "texture", "path", relativePath,
                        "name", texture.name, "textureWidth", texture.width,
                        "textureHeight", texture.height, "textureMipCount", texture.mipmapCount,
                        "textureReadable", texture.isReadable, "textureIsDataSRGB", texture.isDataSRGB,
                        "textureFilterMode", (int)texture.filterMode,
                        "textureWrapModeU", (int)texture.wrapModeU,
                        "textureWrapModeV", (int)texture.wrapModeV,
                        "textureAnisoLevel", texture.anisoLevel));
                }
                catch (Exception error)
                {
                    exportedTextureIds.Remove(id);
                    diagnostics.Add("Texture conversion failed for " + texture.name + ": " + error.Message);
                    return null;
                }
            }
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
            if (tracks.Count == 0)
                diagnostics.Add("Animation clip has no playable Transform tracks; sample its imported avatar for Preview: "
                    + AssetDatabase.GetAssetPath(clip) + " (" + clip.name + ")");
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

