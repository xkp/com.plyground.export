using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public partial class ModuleExporter
{
    [Serializable]
    private sealed class PreviewRuntimeReference
    {
        public string id = "plythreejs";
        public string apiVersion = "1";
    }

    [Serializable]
    private sealed class PreviewTypeReference
    {
        public string type;
        public string assembly;
    }

    [Serializable]
    private sealed class PreviewTypeReferences
    {
        public PreviewTypeReference[] references;
    }

    [Serializable]
    private sealed class PreviewAssemblyName
    {
        public string name;
    }

    [Serializable]
    private sealed class PreviewEntry
    {
        public string entry;
        public string controller;
        public string revision;
        public string sourceRevision;
    }

    [Serializable]
    private sealed class PreviewPath
    {
        public string bundle;
        public string manifest;
    }

    [Serializable]
    private sealed class PreviewModuleManifest
    {
        public string format = "plyground.preview.module/v1";
        public string moduleId;
        public string moduleType;
        public string name;
        public string revision;
        public PreviewRuntimeReference runtime = new PreviewRuntimeReference();
        public PreviewEntry code;
        public PreviewEntry systemEditor;
        public PreviewEntry editor;
        public PreviewPath assets;
        public PreviewPath solution;
    }

    [Serializable]
    private sealed class PreviewSolutionManifest
    {
        public string format = "plyground.preview.solution/v1";
        public string unityVersion;
        public string controller;
        public string[] sources;
        public PreviewAssetSnapshot[] assets;
    }

    [Serializable]
    private sealed class PreviewAssetSnapshot
    {
        public string path;
        public string dependencyHash;
    }

    [Serializable]
    private sealed class PreviewItemPrefabMap
    {
        public string format = "plyground.preview.item-prefabs/v1";
        public PreviewItemPrefab[] items;
    }

    [Serializable]
    private sealed class PreviewItemPrefab
    {
        public string id;
        public string prefabId;
        public string name;
        public string description;
        public string prefab;
        public string icon;
        public string icon3d;
        public bool unique;
        public bool notDraggable;
        public bool template;
    }

    // Unity -batchmode -executeMethod ModuleExporter.ExportPreviewDataBatch
    public static void ExportPreviewDataBatch()
    {
        string toolRoot = Environment.GetEnvironmentVariable("PLYGROUND_PREVIEW_TOOL_ROOT");
        string output = Environment.GetEnvironmentVariable("PLYGROUND_PREVIEW_OUTPUT");
        if (string.IsNullOrWhiteSpace(toolRoot) || string.IsNullOrWhiteSpace(output))
            throw new InvalidOperationException("Set PLYGROUND_PREVIEW_TOOL_ROOT and PLYGROUND_PREVIEW_OUTPUT for batch export.");
        string definition = Path.Combine(Application.dataPath, "module.bgm");
        if (!File.Exists(definition)) throw new FileNotFoundException("Module definition is missing.", definition);
        EditorPrefs.SetString("Plyground.PreviewToolRoot", toolRoot);
        var window = CreateInstance<ModuleExporter>();
        try
        {
            window.exportPath = output;
            window.LoadModuleFromFile(definition);
            window.ExportPreviewData();
        }
        finally { DestroyImmediate(window); }
    }
    private void ExportPreviewData()
    {
        if (string.IsNullOrWhiteSpace(moduleId))
            throw new InvalidOperationException("Save the module before exporting Preview data so its module ID is stable.");
        string toolRoot = EditorPrefs.GetString("Plyground.PreviewToolRoot", "").Trim();
        string transpiler = Path.Combine(toolRoot, "unity-runtime", "tools", "UnityTranspiler", "UnityTranspiler.csproj");
        if (!File.Exists(transpiler))
            throw new FileNotFoundException("Set Plythreejs Root to a checkout containing unity-runtime/tools/UnityTranspiler.", transpiler);
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        string controllerSource = FindPreviewControllerSource(projectRoot);
        string outputDirectory = Path.GetDirectoryName(GetModuleFolder());
        Directory.CreateDirectory(outputDirectory);
        string archive = Path.Combine(outputDirectory, moduleName + ".preview.zip");
        string staging = Path.Combine(Path.GetTempPath(), "plyground-preview-" + Guid.NewGuid().ToString("N"));
        string preview = Path.Combine(staging, "Preview");
        Directory.CreateDirectory(preview);
        try
        {
            string systemEditorAsmdef = FindPreviewSystemEditorAssembly(projectRoot);
            string systemEditorOutput = Path.Combine(preview, "editor", "system.mjs");
            EditorUtility.DisplayProgressBar("Export Preview Data", "Transpiling shared editor SDK", 0.1f);
            RunPreviewTool("dotnet", "run", "--project", transpiler, "--", "--editor-project",
                projectRoot, systemEditorAsmdef, systemEditorOutput, "@plythreejs/runtime",
                "--root", "BaseGameModule", "--root", "BaseCharacterModule");
            EditorUtility.DisplayProgressBar("Export Preview Data", "Transpiling editor controller", 0.2f);
            string editorOutput = Path.Combine(preview, "editor", "controller.mjs");
            string controllerAsmdef = FindPreviewAssemblyDefinition(controllerSource, projectRoot);
            if (controllerAsmdef == null)
                RunPreviewTool("dotnet", "run", "--project", transpiler, "--", "--editor-closure",
                    projectRoot, controllerSource, editorOutput, "@plythreejs/runtime");
            else
                RunPreviewTool("dotnet", "run", "--project", transpiler, "--", "--editor-project",
                    projectRoot, controllerAsmdef, editorOutput, "@plythreejs/runtime", "--root", controllerClass);

            var itemPrefabs = ResolvePreviewItemPrefabs();
            var catalogAssets = ResolvePreviewCatalogAssets(controllerSource, projectRoot);
            // An exploratory export only needs types referenced by the importing
            // controller. Optional catalog prefabs can contain scripts that are
            // unrelated to this scene and still lack browser runtime support.
            var runtimeSources = Environment.GetEnvironmentVariable("PLYGROUND_PREVIEW_ALLOW_APPROXIMATIONS") == "1"
                ? new List<(string path, string typeName)>()
                : GetPreviewRuntimeSources(projectRoot, itemPrefabs, catalogAssets);
            var assemblyDefinitions = Directory.GetFiles(Path.Combine(projectRoot, "Assets"), "*.asmdef",
                SearchOption.AllDirectories).Select(path => new {
                    path,
                    name = JsonUtility.FromJson<PreviewAssemblyName>(File.ReadAllText(path))?.name
                }).Where(value => !string.IsNullOrWhiteSpace(value.name))
                .ToDictionary(value => value.name, value => value.path, StringComparer.OrdinalIgnoreCase);
            var referencedTypes = JsonUtility.FromJson<PreviewTypeReferences>(
                File.ReadAllText(editorOutput + ".references.json"));
            var editorSources = new List<(string path, string typeName)>();
            foreach (var reference in referencedTypes?.references ?? new PreviewTypeReference[0])
            {
                if (!string.IsNullOrWhiteSpace(reference?.assembly)
                    && assemblyDefinitions.TryGetValue(reference.assembly, out string definition))
                {
                    if (reference.assembly.EndsWith(".Editor", StringComparison.OrdinalIgnoreCase)
                        || IsPreviewEditorType(definition, reference.type, projectRoot))
                        editorSources.Add((definition, reference.type));
                    else runtimeSources.Add((definition, reference.type));
                }
            }
            var editorEntries = new List<string> { "import { UnityType } from '@plythreejs/runtime';" };
            int editorIndex = 0;
            var emittedEditorTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            while (true)
            {
                var assembly = editorSources.Where(source => !emittedEditorTypes.Contains(source.path + "|" + source.typeName))
                    .GroupBy(source => source.path, StringComparer.OrdinalIgnoreCase).FirstOrDefault();
                if (assembly == null) break;
                var roots = assembly.Select(source => source.typeName).Distinct(StringComparer.Ordinal).ToArray();
                foreach (string type in roots) emittedEditorTypes.Add(assembly.Key + "|" + type);
                int index = editorIndex++;
                string output = Path.Combine(preview, "editor", "type-" + index + ".mjs");
                var arguments = new[] { "run", "--project", transpiler, "--", "--editor-project",
                    projectRoot, assembly.Key, output, "@plythreejs/runtime" }
                    .Concat(roots.SelectMany(type => new[] { "--root", type })).ToArray();
                RunPreviewTool("dotnet", arguments);
                editorEntries.Insert(1, "const type" + index + " = await import('./type-" + index + ".mjs');");
                editorEntries.Insert(2, "UnityType.RegisterModuleEditor(type" + index + ".unityTypes);");
                var dependencies = JsonUtility.FromJson<PreviewTypeReferences>(File.ReadAllText(output + ".references.json"));
                foreach (var dependency in dependencies?.references ?? new PreviewTypeReference[0])
                {
                    if (assemblyDefinitions.TryGetValue(dependency.assembly ?? "", out string definition)
                        && IsPreviewEditorType(definition, dependency.type, projectRoot))
                        editorSources.Add((definition, dependency.type));
                }
            }
            editorEntries.Add("const controller = await import('./controller.mjs');");
            editorEntries.Add("export const unityTypes = controller.unityTypes;");
            WritePreviewFile(Path.Combine(preview, "editor", "module.mjs"), string.Join("\n", editorEntries) + "\n");
            EditorUtility.DisplayProgressBar("Export Preview Data", "Transpiling runtime scripts", 0.5f);
            var entries = new List<string> { "import { UnityType } from '@plythreejs/runtime';" };
            var registrations = new List<string>();
            int codeIndex = 0;
            var groups = runtimeSources.GroupBy(source =>
                FindPreviewAssemblyDefinition(source.path, projectRoot), StringComparer.OrdinalIgnoreCase).ToArray();
            foreach (var assembly in groups.Where(group => group.Key != null))
            {
                int index = codeIndex++;
                string output = Path.Combine(preview, "code", "type-" + index + ".mjs");
                var arguments = new[] { "run", "--project", transpiler, "--", "--project",
                    projectRoot, assembly.Key, output, "@plythreejs/runtime" }
                    .Concat(assembly.Select(source => source.typeName).Distinct(StringComparer.Ordinal)
                        .SelectMany(type => new[] { "--root", type })).ToArray();
                RunPreviewTool("dotnet", arguments);
                entries.Add("import * as type" + index + " from './type-" + index + ".mjs';");
                registrations.Add("UnityType.Register(type" + index + ".unityTypes);");
            }
            var defaultRoots = groups.Where(group => group.Key == null)
                .SelectMany(group => group.Select(source => source.typeName))
                .Concat((referencedTypes?.references ?? new PreviewTypeReference[0])
                    .Where(reference => reference.assembly == "Assembly-CSharp")
                    .Select(reference => reference.type))
                .Distinct(StringComparer.Ordinal).ToArray();
            if (defaultRoots.Length > 0)
            {
                int index = codeIndex++;
                string output = Path.Combine(preview, "code", "type-" + index + ".mjs");
                var arguments = new[] { "run", "--project", transpiler, "--", "--runtime-default-project",
                    projectRoot, output, "@plythreejs/runtime" }
                    .Concat(defaultRoots.SelectMany(type => new[] { "--root", type })).ToArray();
                RunPreviewTool("dotnet", arguments);
                registrations.Add("const type" + index + " = await import('./type-" + index + ".mjs');");
                registrations.Add("UnityType.Register(type" + index + ".unityTypes);");
            }
            var registries = Enumerable.Range(0, codeIndex).Select(index => "type" + index + ".unityTypes").ToArray();
            WritePreviewFile(Path.Combine(preview, "code", "module.mjs"),
                string.Join("\n", entries) + "\n" + string.Join("\n", registrations)
                + "\nexport const unityTypes = Object.assign({}, "
                + string.Join(", ", registries) + ");\n");

            EditorUtility.DisplayProgressBar("Export Preview Data", "Preparing assets and source snapshot", 0.75f);
            string assetSource = Path.Combine(staging, "asset-source", "source.json");
            Directory.CreateDirectory(Path.GetDirectoryName(assetSource));
            Plyground.Preview.Editor.PlygroundPreviewAssetExporter.ExportPrefabsToPath(assetSource,
                itemPrefabs.Select(value => value.prefab), catalogAssets);
            string itemMapPath = Path.Combine(staging, "item-prefabs.json");
            WritePreviewFile(itemMapPath, JsonUtility.ToJson(new PreviewItemPrefabMap {
                items = itemGroups.SelectMany(group => group.items).Select(value => new PreviewItemPrefab {
                    id = value.id, name = value.name, description = value.description,
                    prefab = value.prefabPath, icon = value.icon, icon3d = value.modelPath,
                    unique = value.unique, notDraggable = value.notDraggable, template = value.template,
                    prefabId = itemPrefabs.Where(entry => entry.id == value.id)
                        .Select(entry => Plyground.Preview.Editor.PlygroundPreviewAssetExporter.GetPreviewAssetId(entry.prefab))
                        .FirstOrDefault()
                }).ToArray()
            }, true));
            string assetPreprocessor = Path.Combine(toolRoot, "unity-runtime", "src", "preview-module-assets.js");
            if (!File.Exists(assetPreprocessor)) throw new FileNotFoundException("Preview asset preprocessor is missing.", assetPreprocessor);
            RunPreviewTool("node", assetPreprocessor, assetSource, Path.Combine(preview, "assets"), itemMapPath);
            Directory.Delete(Path.GetDirectoryName(assetSource), true);
            File.Delete(itemMapPath);
            var sourcePaths = new HashSet<string>(runtimeSources.Where(value => value.path.EndsWith(".cs",
                StringComparison.OrdinalIgnoreCase)).Select(value => value.path), StringComparer.OrdinalIgnoreCase);
            if (defaultRoots.Length > 0)
            {
                string runtimeProject = Path.Combine(projectRoot, "Assembly-CSharp.csproj");
                string assetsRoot = Path.Combine(projectRoot, "Assets") + Path.DirectorySeparatorChar;
                foreach (var compile in XDocument.Load(runtimeProject).Descendants()
                    .Where(node => node.Name.LocalName == "Compile"))
                {
                    string include = compile.Attribute("Include")?.Value;
                    if (string.IsNullOrWhiteSpace(include)) continue;
                    string file = Path.GetFullPath(Path.Combine(projectRoot, include));
                    if (file.StartsWith(assetsRoot, StringComparison.OrdinalIgnoreCase) && File.Exists(file))
                        sourcePaths.Add(file);
                }
            }
            if (controllerAsmdef == null)
            {
                string editorProject = Path.Combine(projectRoot, "Assembly-CSharp-Editor.csproj");
                string assetsRoot = Path.Combine(projectRoot, "Assets") + Path.DirectorySeparatorChar;
                foreach (var compile in XDocument.Load(editorProject).Descendants()
                    .Where(node => node.Name.LocalName == "Compile"))
                {
                    string include = compile.Attribute("Include")?.Value;
                    if (string.IsNullOrWhiteSpace(include)) continue;
                    string file = Path.GetFullPath(Path.Combine(projectRoot, include));
                    if (file.StartsWith(assetsRoot, StringComparison.OrdinalIgnoreCase) && File.Exists(file))
                        sourcePaths.Add(file);
                }
            }
            foreach (string definition in runtimeSources.Select(value => FindPreviewAssemblyDefinition(value.path, projectRoot))
                .Concat(editorSources.Select(value => value.path))
                .Where(value => value != null).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                string assemblyRoot = Path.GetDirectoryName(definition);
                foreach (string file in Directory.GetFiles(assemblyRoot, "*.cs", SearchOption.AllDirectories))
                {
                    if (!string.Equals(FindPreviewAssemblyDefinition(file, projectRoot), definition,
                        StringComparison.OrdinalIgnoreCase)) continue;
                    if (!editorSources.Any(value => string.Equals(value.path, definition, StringComparison.OrdinalIgnoreCase))
                        && Path.GetRelativePath(assemblyRoot, file).Split(Path.DirectorySeparatorChar)
                        .Contains("Editor", StringComparer.OrdinalIgnoreCase)) continue;
                    sourcePaths.Add(file);
                }
            }
            var sources = new List<string>();
            foreach (string source in new[] { controllerSource }.Concat(sourcePaths.OrderBy(value => value, StringComparer.Ordinal)))
            {
                string relative = Path.GetRelativePath(projectRoot, source).Replace('\\', '/');
                if (relative.StartsWith("../", StringComparison.Ordinal) || Path.IsPathRooted(relative))
                    throw new InvalidOperationException("Preview source is outside the Unity project: " + source);
                string destination = Path.Combine(preview, "solution", relative.Replace('/', Path.DirectorySeparatorChar));
                Directory.CreateDirectory(Path.GetDirectoryName(destination));
                File.Copy(source, destination, true);
                if (File.Exists(source + ".meta")) File.Copy(source + ".meta", destination + ".meta", true);
                sources.Add(relative);
            }
            WritePreviewFile(Path.Combine(preview, "solution", "manifest.json"),
                JsonUtility.ToJson(new PreviewSolutionManifest { unityVersion = Application.unityVersion,
                    controller = controllerClass, sources = sources.Distinct().ToArray(),
                    assets = itemPrefabs.Select(value => AssetDatabase.GetAssetPath(value.prefab))
                        .Concat(catalogAssets.Select(AssetDatabase.GetAssetPath)).Distinct()
                        .Select(path => new PreviewAssetSnapshot { path = path,
                            dependencyHash = AssetDatabase.GetAssetDependencyHash(path).ToString() }).ToArray()
                }, true));

            var manifest = new PreviewModuleManifest {
                moduleId = moduleId,
                moduleType = moduleType,
                name = moduleName,
                code = new PreviewEntry { entry = "Preview/code/module.mjs" },
                systemEditor = new PreviewEntry { entry = "Preview/editor/system.mjs",
                    revision = ComputePreviewFileRevision(systemEditorOutput),
                    sourceRevision = ComputePreviewRevision(Path.GetDirectoryName(systemEditorAsmdef)) },
                editor = new PreviewEntry { entry = "Preview/editor/module.mjs", controller = controllerClass },
                assets = new PreviewPath { bundle = "Preview/assets/bundle.json" },
                solution = new PreviewPath { manifest = "Preview/solution/manifest.json" },
                revision = ComputePreviewRevision(preview)
            };
            WritePreviewFile(Path.Combine(preview, "manifest.json"), JsonUtility.ToJson(manifest, true));
            string temporaryArchive = archive + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                ZipFile.CreateFromDirectory(staging, temporaryArchive);
                if (File.Exists(archive)) File.Replace(temporaryArchive, archive, null);
                else File.Move(temporaryArchive, archive);
            }
            finally { if (File.Exists(temporaryArchive)) File.Delete(temporaryArchive); }
            PublishPreviewFolder(preview, Path.Combine(GetModuleFolder(), "Preview"));
            UnityEngine.Debug.Log("Exported Preview data to " + archive);
            EditorUtility.RevealInFinder(archive);
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            if (Directory.Exists(staging)) Directory.Delete(staging, true);
        }
    }

    private string FindPreviewControllerSource(string projectRoot)
    {
        var matches = AssetDatabase.FindAssets("t:MonoScript")
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(path => path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            .Where(path => Path.GetFileNameWithoutExtension(path) == controllerClass.Split('.').Last())
            .Where(path => path.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
            .Select(path => Path.Combine(projectRoot, path.Replace('/', Path.DirectorySeparatorChar)))
            .Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        if (matches.Length != 1)
            throw new InvalidOperationException("Expected one source file for controller " + controllerClass + "; found " + matches.Length + ".");
        return matches[0];
    }

    private static string FindPreviewSystemEditorAssembly(string projectRoot)
    {
        var roots = new[] { Path.Combine(projectRoot, "Packages"), Path.Combine(projectRoot, "Library", "PackageCache") };
        var matches = roots.Where(Directory.Exists)
            .SelectMany(root => Directory.GetFiles(root, "Plyground.SDK.Editor.asmdef", SearchOption.AllDirectories))
            .Where(path => Path.GetFileName(Path.GetDirectoryName(Path.GetDirectoryName(path)))
                .StartsWith("com.plyground.sdk", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        if (matches.Length != 1)
            throw new InvalidOperationException("Expected one installed Plyground SDK editor assembly; found " + matches.Length + ".");
        return matches[0];
    }

    private static bool IsPreviewEditorType(string asmdef, string typeName, string projectRoot)
    {
        if (string.IsNullOrWhiteSpace(typeName)) return false;
        string root = Path.GetDirectoryName(asmdef);
        string shortName = typeName.Split('.').Last();
        string declaration = @"\b(class|struct|interface|enum)\s+" + Regex.Escape(shortName) + @"\b";
        foreach (string file in Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories))
        {
            if (!string.Equals(FindPreviewAssemblyDefinition(file, projectRoot), asmdef,
                StringComparison.OrdinalIgnoreCase)) continue;
            if (!Path.GetRelativePath(root, file).Split(Path.DirectorySeparatorChar)
                .Any(part => part.IndexOf("Editor", StringComparison.OrdinalIgnoreCase) >= 0)) continue;
            if (Regex.IsMatch(File.ReadAllText(file), declaration)) return true;
        }
        return false;
    }

    private bool HasCurrentPreviewData(string moduleFolder)
    {
        try
        {
            string preview = Path.Combine(moduleFolder, "Preview");
            string manifestPath = Path.Combine(preview, "manifest.json");
            if (!File.Exists(manifestPath)) return false;
            var manifest = JsonUtility.FromJson<PreviewModuleManifest>(File.ReadAllText(manifestPath));
            if (manifest == null || !string.Equals(manifest.moduleId, moduleId, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(manifest.editor?.controller, controllerClass, StringComparison.Ordinal)) return false;
            if (manifest.systemEditor == null || string.IsNullOrWhiteSpace(manifest.systemEditor.revision)
                || !File.Exists(Path.Combine(preview, "editor", "system.mjs"))
                || !string.Equals(manifest.systemEditor.sourceRevision,
                    ComputePreviewRevision(Path.GetDirectoryName(FindPreviewSystemEditorAssembly(
                        Directory.GetParent(Application.dataPath).FullName))), StringComparison.Ordinal)) return false;
            string solutionPath = Path.Combine(preview, "solution", "manifest.json");
            var solution = JsonUtility.FromJson<PreviewSolutionManifest>(File.ReadAllText(solutionPath));
            if (solution?.sources == null || solution.sources.Length == 0) return false;
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            foreach (string relative in solution.sources)
            {
                string current = Path.GetFullPath(Path.Combine(projectRoot, relative.Replace('/', Path.DirectorySeparatorChar)));
                string snapshot = Path.GetFullPath(Path.Combine(preview, "solution", relative.Replace('/', Path.DirectorySeparatorChar)));
                if (!current.StartsWith(projectRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
                    || !snapshot.StartsWith(Path.Combine(preview, "solution") + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
                    || !File.Exists(current) || !File.Exists(snapshot)
                    || !File.ReadAllBytes(current).SequenceEqual(File.ReadAllBytes(snapshot))) return false;
            }
            foreach (var asset in solution.assets ?? new PreviewAssetSnapshot[0])
            {
                if (string.IsNullOrWhiteSpace(asset.path) || string.IsNullOrWhiteSpace(asset.dependencyHash)
                    || !string.Equals(AssetDatabase.GetAssetDependencyHash(asset.path).ToString(),
                        asset.dependencyHash, StringComparison.OrdinalIgnoreCase)) return false;
            }
            return true;
        }
        catch { return false; }
    }

    private static string FindPreviewAssemblyDefinition(string source, string projectRoot)
    {
        string directory = Path.GetDirectoryName(source);
        string assetsRoot = Path.Combine(projectRoot, "Assets");
        while (directory != null && directory.StartsWith(assetsRoot, StringComparison.OrdinalIgnoreCase))
        {
            string[] definitions = Directory.GetFiles(directory, "*.asmdef", SearchOption.TopDirectoryOnly);
            if (definitions.Length > 1)
                throw new InvalidOperationException("Multiple assembly definitions beside Preview controller: " + directory);
            if (definitions.Length == 1) return definitions[0];
            directory = Path.GetDirectoryName(directory);
        }
        return null;
    }

    private List<(string id, GameObject prefab)> ResolvePreviewItemPrefabs()
    {
        var result = new List<(string id, GameObject prefab)>();
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in itemGroups.SelectMany(group => group.items))
        {
            if (string.IsNullOrWhiteSpace(item.id))
                throw new InvalidOperationException("Every Preview item template needs an ID.");
            if (!ids.Add(item.id)) throw new InvalidOperationException("Duplicate Preview item template ID: " + item.id);
            string path = item.prefabPath;
            GameObject prefab = item.prefab != null ? item.prefab
                : !string.IsNullOrWhiteSpace(path) ? AssetDatabase.LoadAssetAtPath<GameObject>(path) : null;
            if (prefab == null)
            {
                if (!string.IsNullOrWhiteSpace(path))
                    throw new InvalidOperationException("Preview item prefab could not be loaded: " + path);
                continue;
            }
            if (!PrefabUtility.IsPartOfPrefabAsset(prefab))
                throw new InvalidOperationException("Preview item must reference a saved prefab asset: " + item.id);
            result.Add((item.id, prefab));
        }
        return result;
    }

    private static List<UnityEngine.Object> ResolvePreviewCatalogAssets(string controllerSource, string projectRoot)
    {
        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var resourceNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var resourcePrefabFolders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        string editorRoot = Path.Combine(projectRoot, "Assets", "plyground", "Editor");
        var sources = Directory.Exists(editorRoot)
            ? Directory.GetFiles(editorRoot, "*.cs", SearchOption.AllDirectories)
            : new[] { controllerSource };
        foreach (string source in sources)
        {
            string text = File.ReadAllText(source);
            foreach (System.Text.RegularExpressions.Match match in System.Text.RegularExpressions.Regex.Matches(
                text, "Assets/[^\"\\r\\n]+"))
            {
                string path = match.Value.Replace('\\', '/');
                if (AssetDatabase.LoadMainAssetAtPath(path) != null) paths.Add(path);
            }
            foreach (System.Text.RegularExpressions.Match match in System.Text.RegularExpressions.Regex.Matches(
                text, "Resources\\.Load(?:<[^>]+>)?\\s*\\(\\s*\"([^\"]+)\""))
                resourceNames.Add(match.Groups[1].Value);
        }
        // Runtime character builders commonly enumerate clothing prefabs by folder.
        // Include those prefabs in the module package so Resources.LoadAll can
        // materialize the requested variation without opening Unity again.
        foreach (string source in Directory.GetFiles(Path.Combine(projectRoot, "Assets"), "*.cs",
            SearchOption.AllDirectories))
        {
            string text = File.ReadAllText(source);
            foreach (System.Text.RegularExpressions.Match match in System.Text.RegularExpressions.Regex.Matches(
                text, "Resources\\.LoadAll\\s*\\(\\s*\"([^\"]+)\"\\s*,\\s*typeof\\s*\\(\\s*GameObject\\s*\\)"))
                resourcePrefabFolders.Add(match.Groups[1].Value.TrimEnd('/'));
        }
        foreach (string path in AssetDatabase.GetAllAssetPaths())
            if (path.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase)
                && path.IndexOf("/Resources/", StringComparison.OrdinalIgnoreCase) >= 0
                && (resourceNames.Contains(Path.ChangeExtension(path.Substring(
                    path.LastIndexOf("/Resources/", StringComparison.OrdinalIgnoreCase)
                    + "/Resources/".Length), null).Replace('\\', '/'))
                    || (path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase)
                        && resourcePrefabFolders.Any(folder => path.Substring(
                            path.LastIndexOf("/Resources/", StringComparison.OrdinalIgnoreCase)
                            + "/Resources/".Length).StartsWith(folder + "/", StringComparison.OrdinalIgnoreCase))))
                && AssetDatabase.LoadMainAssetAtPath(path) != null)
                paths.Add(path);
        // Character modules such as RedMono choose GEN_* prefabs by descriptor at import time.
        // Their template records have no direct prefab path, so include these lookup targets.
        if (sources.Any(source => File.ReadAllText(source).Contains("GEN_")))
            foreach (string path in AssetDatabase.GetAllAssetPaths())
                if (path.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase)
                    && path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase)
                    && Path.GetFileNameWithoutExtension(path).StartsWith("GEN_", StringComparison.OrdinalIgnoreCase)
                    && AssetDatabase.LoadMainAssetAtPath(path) != null)
                    paths.Add(path);        return paths.OrderBy(value => value, StringComparer.Ordinal)
            .Select(AssetDatabase.LoadMainAssetAtPath).ToList();
    }

    private List<(string path, string typeName)> GetPreviewRuntimeSources(string projectRoot,
        List<(string id, GameObject prefab)> itemPrefabs, List<UnityEngine.Object> catalogAssets)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var prefab in itemPrefabs.Select(item => item.prefab).Concat(catalogAssets.OfType<GameObject>()).Distinct())
        {
            foreach (var component in prefab.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (component == null) throw new InvalidOperationException("A Preview prefab has a missing MonoBehaviour script.");
                var script = MonoScript.FromMonoBehaviour(component);
                string assetPath = AssetDatabase.GetAssetPath(script);
                if (string.IsNullOrWhiteSpace(assetPath) || !assetPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                    continue; // Package components are handled by the Preview runtime or reported during scene import.
                if (assetPath.IndexOf("/Editor/", StringComparison.OrdinalIgnoreCase) >= 0
                    || assetPath.IndexOf("/Editor Library/", StringComparison.OrdinalIgnoreCase) >= 0)
                    continue;
                result[Path.Combine(projectRoot, assetPath.Replace('/', Path.DirectorySeparatorChar))] = component.GetType().FullName;
            }
        }
        foreach (var scriptable in catalogAssets.OfType<ScriptableObject>())
        {
            var script = MonoScript.FromScriptableObject(scriptable);
            if (script == null) continue;
            string assetPath = AssetDatabase.GetAssetPath(script);
            if (!assetPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase)
                || assetPath.IndexOf("/Editor/", StringComparison.OrdinalIgnoreCase) >= 0
                || assetPath.IndexOf("/Editor Library/", StringComparison.OrdinalIgnoreCase) >= 0)
                continue;
            result[Path.Combine(projectRoot, assetPath.Replace('/', Path.DirectorySeparatorChar))] =
                scriptable.GetType().FullName;
        }
        var controllers = itemPrefabs.Select(item => item.prefab)
            .Concat(catalogAssets.OfType<GameObject>()).Distinct()
            .SelectMany(prefab => prefab.GetComponentsInChildren<Animator>(true))
            .Select(animator => animator.runtimeAnimatorController)
            .OfType<AnimatorController>()
            .Concat(catalogAssets.OfType<AnimatorController>()).Distinct();
        foreach (var controller in controllers)
            foreach (var layer in controller.layers)
                AddPreviewStateBehaviourSources(layer.stateMachine, projectRoot, result);
        return result.OrderBy(value => value.Key, StringComparer.Ordinal)
            .Select(value => (value.Key, value.Value)).ToList();
    }

    private static void AddPreviewStateBehaviourSources(AnimatorStateMachine machine,
        string projectRoot, Dictionary<string, string> result)
    {
        foreach (var child in machine.states)
            foreach (var behaviour in child.state.behaviours)
            {
                if (behaviour == null) continue;
                var script = MonoScript.FromScriptableObject(behaviour);
                string assetPath = script == null ? null : AssetDatabase.GetAssetPath(script);
                if (string.IsNullOrWhiteSpace(assetPath)
                    || !assetPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase)
                    || assetPath.IndexOf("/Editor/", StringComparison.OrdinalIgnoreCase) >= 0)
                    continue;
                result[Path.Combine(projectRoot, assetPath.Replace('/', Path.DirectorySeparatorChar))] =
                    behaviour.GetType().FullName;
            }
        foreach (var child in machine.stateMachines)
            AddPreviewStateBehaviourSources(child.stateMachine, projectRoot, result);
    }

    private static void RunPreviewTool(string executable, params string[] arguments)
    {
        using (var process = new Process())
        {
            process.StartInfo.FileName = executable;
            process.StartInfo.Arguments = string.Join(" ", arguments.Select(QuotePreviewArgument));
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.Start();
            var stderrTask = System.Threading.Tasks.Task.Run(() => process.StandardError.ReadToEnd());
            string stdout = process.StandardOutput.ReadToEnd();
            string stderr = stderrTask.GetAwaiter().GetResult();
            process.WaitForExit();
            if (process.ExitCode != 0)
                throw new InvalidOperationException("Preview transpilation failed for "
                    + process.StartInfo.Arguments + ".\n" + stderr + "\n" + stdout);
        }
    }

    private static string QuotePreviewArgument(string value)
    {
        var result = new StringBuilder("\"");
        int slashes = 0;
        foreach (char character in value)
        {
            if (character == '\\') { slashes++; continue; }
            if (character == '"')
            {
                result.Append('\\', slashes * 2 + 1).Append('"');
                slashes = 0;
                continue;
            }
            result.Append('\\', slashes).Append(character);
            slashes = 0;
        }
        result.Append('\\', slashes * 2).Append('"');
        return result.ToString();
    }

    private static string PreviewJsString(string value) =>
        "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";

    private static void PublishPreviewFolder(string source, string destination)
    {
        if (!string.Equals(Path.GetFileName(Path.GetFullPath(destination)), "Preview",
            StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Preview output must target a Preview directory: " + destination);
        Directory.CreateDirectory(Path.GetDirectoryName(destination));
        if (Directory.Exists(destination))
        {
            foreach (string file in Directory.GetFiles(destination)) File.Delete(file);
            foreach (string directory in Directory.GetDirectories(destination)) Directory.Delete(directory, true);
        }
        CopyPreviewTree(source, destination);
    }

    private static void CopyPreviewTree(string source, string destination)
    {
        Directory.CreateDirectory(destination);
        foreach (string directory in Directory.GetDirectories(source))
            CopyPreviewTree(directory, Path.Combine(destination, Path.GetFileName(directory)));
        foreach (string file in Directory.GetFiles(source).Where(path =>
            !string.Equals(Path.GetFileName(path), "manifest.json", StringComparison.OrdinalIgnoreCase)))
            File.Copy(file, Path.Combine(destination, Path.GetFileName(file)), true);
        string manifest = Path.Combine(source, "manifest.json");
        if (File.Exists(manifest)) File.Copy(manifest, Path.Combine(destination, "manifest.json"), true);
    }

    private static void WritePreviewFile(string path, string content)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllText(path, content, new UTF8Encoding(false));
    }

    private static string ComputePreviewRevision(string root)
    {
        using (var sha = SHA256.Create())
        using (var data = new MemoryStream())
        {
            foreach (string file in Directory.GetFiles(root, "*", SearchOption.AllDirectories).OrderBy(value => value, StringComparer.Ordinal))
            {
                byte[] relative = Encoding.UTF8.GetBytes(Path.GetRelativePath(root, file).Replace('\\', '/'));
                data.Write(relative, 0, relative.Length);
                byte[] bytes = File.ReadAllBytes(file);
                data.Write(bytes, 0, bytes.Length);
            }
            data.Position = 0;
            return "sha256:" + BitConverter.ToString(sha.ComputeHash(data)).Replace("-", "").ToLowerInvariant();
        }
    }

    private static string ComputePreviewFileRevision(string path)
    {
        using (var sha = SHA256.Create())
            return "sha256:" + BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path)))
                .Replace("-", "").ToLowerInvariant();
    }
}
