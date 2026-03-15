using UnityEngine;
using UnityEditor;
using UnityEditorInternal; // for ReorderableList
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Linq;
using System;

public partial class ModuleExporter : EditorWindow
{
	private string moduleId = "";

	private string moduleName = "";
	private string controllerClass = "";
	private string description = "";
	private string matchDescription = "";
	private string author = "";
	private string url = "";

	// Module Type property.
	private string moduleType = "Props";
	private readonly string[] allowedModuleTypes = new string[] { "Game", "Character", "Nature", "Props", "Other" };

	private List<ItemGroup> itemGroups = new List<ItemGroup>();
	// The exportPath is chosen by the user.
	private string exportPath = "";

	// Packages are represented as metadata: a file name plus the matching Assets folder.
	private List<PackageDefinition> unityPackages = new List<PackageDefinition>();
	private List<string> assetsToExport = new List<string>();       // full paths to copied files
	private List<string> customEditors = new List<string>();
	private List<string> dependencies = new List<string>();

	private Vector2 scrollPosition;
	private Item selectedItem = null;

	// Stores foldout states for component sections.
	private Dictionary<int, bool> compFoldouts = new Dictionary<int, bool>();

	// Allowed types for properties.
	private readonly string[] allowedTypes = new string[] { "string", "int", "float", "bool", "enum", "gameitem", "asset", "object" };

	// NEW: A dictionary to track property foldout states (keyed by property key).
	private Dictionary<string, bool> propertyFoldouts = new Dictionary<string, bool>();

	private List<Property> moduleProperties = new List<Property>();

	[System.Serializable]
	public class PackageDefinition
	{
		public string name = "";
		public string fileName = "";
		public string assetFolder = "";
	}

	[System.Serializable]
	public class Item
	{
		public string id;
		public string name;
		public string description;
		public bool unique = false;
		public bool notDraggable = false;
		public bool template = false;
		public GameObject prefab;
		public string prefabPath;
		public string icon;    // Path to the generated thumbnail (relative to the module folder)
		public string modelPath;
		// All properties are now stored in a single dictionary.
		// For component properties, the key is typically "ComponentName.FieldName" and its Property.component is set.
		// For manual properties, we generate a unique key.
		public List<Property> properties = new List<Property>();

		public Vector3 exportTranslation = Vector3.zero;
		public Vector3 exportRotation = Vector3.zero;
		public Vector3 exportScale = Vector3.one;
	}

	[System.Serializable]
	public class Property
	{
		public string name;
		public string type;
		public string data;
		public string value;
	}

	[System.Serializable]
	public class ItemGroup
	{
		public string name;
		public string icon;
		public string category;
		public List<Item> items = new List<Item>();

		// For collapsibility (not serialized)
		[System.NonSerialized]
		public bool isExpanded = true;
	}

	[MenuItem("Plyground/Module Exporter")]
	public static void ShowWindow()
	{
		ModuleExporter window = GetWindow<ModuleExporter>("Plyground Exporter");
		window.minSize = new Vector2(600, 400);
		window.InitModule();
	}

	private string loadedModuleFilePath = "";
	private void InitModule()
	{
		AskForExportFolder();

		// Ask the user if they want to load an existing module file.
		if (EditorUtility.DisplayDialog("Module Editor", "Would you like to load an existing module file?", "Yes", "No"))
		{
			string moduleFilePath = EditorUtility.OpenFilePanel("Select Module File", Application.dataPath, "bgm");
			if (!string.IsNullOrEmpty(moduleFilePath))
			{
				loadedModuleFilePath = moduleFilePath;
				LoadModuleFromFile(moduleFilePath);
			}
		}
	}

	private void LoadModuleFromFile(string filePath)
	{
		// Read the JSON file (assuming it is a valid ExportedModule JSON).
		string json = File.ReadAllText(filePath);
		ExportedModule mod = JsonUtility.FromJson<ExportedModule>(json);

		// Populate module settings.
		moduleId = mod.id;
		moduleName = mod.name;
		moduleType = mod.type;
		controllerClass = mod.controller;
		description = mod.description;
		matchDescription = mod.matchDescription;
		author = mod.author;
		url = mod.url;

		// Populate package metadata.
		unityPackages.Clear();
		if (mod.packages != null)
		{
			foreach (var pkg in mod.packages)
			{
				unityPackages.Add(new PackageDefinition
				{
					name = pkg.name,
					fileName = pkg.fileName,
					assetFolder = pkg.assetFolder
				});
			}
		}

		customEditors.Clear();
		if (mod.customEditors != null)
		{
			foreach (var editor in mod.customEditors)
			{
				customEditors.Add(editor);
			}
		}

		dependencies.Clear();
		if (mod.dependencies != null)
		{
			foreach (var dependency in mod.dependencies)
			{
				dependencies.Add(dependency);
			}
		}

		moduleProperties.Clear();
		if (mod.moduleProperties != null)
		{
			foreach (var property in mod.moduleProperties)
			{
				moduleProperties.Add(property);
			}
		}

		dependencies.Clear();
		if (mod.dependencies != null)
		{
			foreach (var dependency in mod.dependencies)
			{
				dependencies.Add(dependency);
			}
		}

		// Populate item groups.
		itemGroups.Clear();
		if (mod.itemGroups != null)
		{
			foreach (var exportedGroup in mod.itemGroups)
			{
				ItemGroup group = new ItemGroup();
				group.name = exportedGroup.name;
				group.icon = exportedGroup.icon;
				group.items = new List<Item>();
				group.category = exportedGroup.category;

				foreach (var exportedItem in exportedGroup.items)
				{
					Item item = new Item();
					item.id = exportedItem.id;
					item.name = exportedItem.name;
					item.description = exportedItem.description;
					item.unique = exportedItem.unique;
					item.notDraggable = exportedItem.notDraggable;
					item.template = exportedItem.template;
					item.prefabPath = exportedItem.prefab;
					item.prefab = AssetDatabase.LoadAssetAtPath<GameObject>(item.prefabPath);
					item.icon = exportedItem.icon;
					item.modelPath = exportedItem.icon3d;
					item.exportTranslation = exportedItem.exportTranslation;
					item.exportRotation = exportedItem.exportRotation;
					item.exportScale = exportedItem.exportScale;

					// Reconstruct properties.
					item.properties = new List<Property>();
					if (exportedItem.properties != null)
					{
						foreach (var ep in exportedItem.properties)
						{
							// When loading, assume these are manual properties.
							item.properties.Add(new Property
							{
								name = ep.name,
								type = ep.type,
								data = ep.data
							});
						}
					}
					group.items.Add(item);
				}
				itemGroups.Add(group);
			}
		}


		UpdateAssets();
		Debug.Log("Loaded module from " + filePath);
		Repaint();
	}

	private void AskForExportFolder()
	{
		exportPath = Path.Combine(Application.persistentDataPath, "Plyground");

		if (!Directory.Exists(exportPath))
			Directory.CreateDirectory(exportPath);
	}

	/// <summary>
	/// Returns the module folder. If the chosen exportPath’s name equals moduleName, we use exportPath directly;
	/// otherwise we create a subfolder.
	/// </summary>
	private string GetModuleFolder()
	{
		if (string.Equals(Path.GetFileName(exportPath), moduleName))
		{
			return exportPath;
		}
		else
		{
			return Path.Combine(exportPath, moduleName);
		}
	}

	public static string TranslateType(System.Type type)
	{
		if (type == typeof(string) || type == typeof(char))
			return "string";
		if (type == typeof(int) || type == typeof(short) || type == typeof(long))
			return "int";
		if (type == typeof(float) || type == typeof(double) || type == typeof(decimal))
			return "float";
		if (type == typeof(bool))
			return "bool";
		// For any type that doesn't match, we default to "object"
		return "object";
	}

	private string GetUnityPath(string filePath)
	{
		string assetsPath = Application.dataPath; // absolute path to Assets folder
		string fullPath = Path.GetFullPath(filePath).Replace("\\", "/");

		if (!fullPath.StartsWith(assetsPath.Replace("\\", "/")))
		{
			return string.Empty;
		}

		// Convert absolute path to Unity relative path
		return "Assets" + fullPath.Substring(assetsPath.Length);
	}

	private string GetPackageAssetFolderPath(string folderName)
	{
		if (string.IsNullOrWhiteSpace(folderName))
		{
			return string.Empty;
		}

		string normalized = folderName.Replace("\\", "/").Trim('/');
		if (normalized.StartsWith("Assets/"))
		{
			normalized = normalized.Substring("Assets/".Length);
		}

		if (string.IsNullOrWhiteSpace(normalized) || normalized.Contains("/"))
		{
			return string.Empty;
		}

		return "Assets/" + normalized;
	}

	private bool IsDirectAssetsChildFolder(string folderName)
	{
		string assetPath = GetPackageAssetFolderPath(folderName);
		return !string.IsNullOrEmpty(assetPath) && AssetDatabase.IsValidFolder(assetPath);
	}

	public static uint ComputeFNV1aHash(string text)
	{
		const uint offsetBasis = 2166136261;
		const uint prime = 16777619;
		uint hash = offsetBasis;
		foreach (char c in text)
		{
			hash ^= c;
			hash *= prime;
		}
		return hash;
	}

	private ExportedModule BuildExportedModule()
	{
		ExportedModule mod = new ExportedModule();
		if (string.IsNullOrEmpty(moduleId))
			moduleId = System.Guid.NewGuid().ToString().ToUpper();

		mod.id = moduleId;
		mod.name = moduleName;
		mod.type = moduleType;
		mod.controller = controllerClass;
		mod.description = description;
		mod.matchDescription = matchDescription;
		mod.author = author;
		mod.url = url;
		mod.packages = unityPackages
			.Select(package => new PackageDefinition
			{
				name = package.name,
				fileName = package.fileName,
				assetFolder = package.assetFolder
			})
			.ToList();
		mod.dependencies = new List<string>(dependencies);
		mod.customEditors = new List<string>(customEditors);
		mod.moduleProperties = new List<Property>(moduleProperties);

		mod.itemGroups = new List<ExportedGroup>();
		foreach (var group in itemGroups)
		{
			ExportedGroup eg = new ExportedGroup();
			eg.name = group.name;
			eg.icon = group.icon;
			eg.category = group.category;
			eg.items = new List<ExportedItem>();

			foreach (var item in group.items)
			{
				ExportedItem ei = new ExportedItem();
				ei.id = item.id;
				ei.name = item.name;
				ei.description = item.description;
				ei.unique = item.unique;
				ei.notDraggable = item.notDraggable;
				ei.template = item.template;
				ei.prefab = item.prefabPath;
				ei.icon = item.icon;
				ei.icon3d = item.modelPath;
				ei.exportTranslation = item.exportTranslation;
				ei.exportRotation = item.exportRotation;
				ei.exportScale = item.exportScale;

				ei.properties = new List<ExportedProperty>();
				foreach (var kvp in item.properties)
				{
					ei.properties.Add(CopyProperty(kvp));
				}
				eg.items.Add(ei);
			}
			mod.itemGroups.Add(eg);
		}

		return mod;
	}

	private string SaveModule()
	{
		ExportedModule mod = BuildExportedModule();

		string jsonFilePath = loadedModuleFilePath;
		if (string.IsNullOrEmpty(jsonFilePath))
		{
			jsonFilePath = loadedModuleFilePath = Path.Combine(Application.dataPath, "module.bgm");
		}

		string json = JsonUtility.ToJson(mod, true);
		File.WriteAllText(jsonFilePath, json);
		Debug.Log("Saved module JSON to " + jsonFilePath);
		return jsonFilePath;
	}

	private void ExportModule()
	{
		UpdateExportAssets();

		//AskForExportFolder();
		string moduleFolder = GetModuleFolder();
		Directory.CreateDirectory(moduleFolder);

/*		//Copy custom assets into 
		DirectoryCopy(GetAssetModuleFolder(), moduleFolder, true);
*/
		ExportedModule mod = BuildExportedModule();

		if (moduleType == "Game")
		{
			string projectRoot = Directory.GetParent(Application.dataPath).FullName;
			string destTemplateFolder = Path.Combine(moduleFolder, "Template");
			Directory.CreateDirectory(destTemplateFolder);
			string sourceAssets = Path.Combine(projectRoot, "Assets");
			string sourceProjectSettings = Path.Combine(projectRoot, "ProjectSettings");

			var excludedPackageFolders = new HashSet<string>(
				unityPackages
					.Where(package => IsDirectAssetsChildFolder(package.assetFolder))
					.Select(package => Path.GetFileName(GetPackageAssetFolderPath(package.assetFolder))));

			DirectoryCopy(sourceAssets, Path.Combine(destTemplateFolder, "Assets"), true, excludedPackageFolders);
			if (Directory.Exists(sourceProjectSettings))
			{
				DirectoryCopy(sourceProjectSettings, Path.Combine(destTemplateFolder, "ProjectSettings"), true);
			}

			string sourcePackages = Path.Combine(projectRoot, "Packages");
			if (Directory.Exists(sourcePackages))
			{
				DirectoryCopy(sourcePackages, Path.Combine(destTemplateFolder, "Packages"), true);
			}

			Debug.Log("Copied template files to " + destTemplateFolder);
		}
		foreach (var editor in customEditors)
		{
			string projectRoot = Directory.GetParent(Application.dataPath).FullName;
			string editorZipFilePath = Path.Combine(projectRoot, editor);
			if (File.Exists(editorZipFilePath))
			{
				var editorName = Path.GetFileNameWithoutExtension(editor);
				var editorDest = Path.Combine(moduleFolder, "Editors", editorName);
				Directory.CreateDirectory(editorDest);

				System.IO.Compression.ZipFile.ExtractToDirectory(editorZipFilePath, editorDest);
				//File.Copy(editorZipFilePath, Path.Combine(editorDest, Path.GetFileName(editor)), true);
			}
		}


		string jsonFilePath = SaveModule();
		File.Copy(jsonFilePath, Path.Combine(moduleFolder, "module.bgm"), true);

		//export assets
		var assetsFromGroups = new List<string>();
		foreach (var group in itemGroups)
		{
			foreach (var item in group.items)
			{
				if (!string.IsNullOrEmpty(item.prefabPath))
					assetsFromGroups.Add(item.prefabPath);

				//make sure all meshes are read/write
				if (item.prefab != null)
				{
					var instance = item.prefab;
					HashSet<Mesh> processed = new HashSet<Mesh>();
					foreach (var mf in instance.GetComponentsInChildren<MeshFilter>(true))
					{
						if (mf.sharedMesh)
							MakeMeshRW(mf.sharedMesh, processed);
					}

					foreach (var smr in instance.GetComponentsInChildren<SkinnedMeshRenderer>(true))
					{
						if (smr.sharedMesh)
							MakeMeshRW(smr.sharedMesh, processed);
					}
				}
			}
		}

		assetsFromGroups = assetsFromGroups.Distinct().ToList();
		if (assetsFromGroups.Any())
		{
			BuildBundleFromPaths(assetsFromGroups, "AssetBundle", Path.Combine(moduleFolder, "Assets"));
		}

		//build zip file
		string zipFilePath = Path.Combine(Path.GetDirectoryName(moduleFolder), moduleName + ".3dbg");
		if (File.Exists(zipFilePath))
		{
			File.Delete(zipFilePath);
		}
		System.IO.Compression.ZipFile.CreateFromDirectory(moduleFolder, zipFilePath);
		Debug.Log("Created zip file: " + zipFilePath);

		EditorUtility.RevealInFinder(zipFilePath);
	}

	private static void MakeMeshRW(Mesh mesh, HashSet<Mesh> processed)
	{
		if (processed.Contains(mesh))
			return;

		string path = AssetDatabase.GetAssetPath(mesh);

		if (string.IsNullOrEmpty(path))
		{
			Debug.LogWarning($"Mesh '{mesh.name}' is not an asset (maybe generated at runtime?). Skipped.");
			return;
		}

		var importer = AssetImporter.GetAtPath(path) as ModelImporter;
		if (importer == null)
		{
			Debug.LogWarning($"Mesh '{mesh.name}' is not imported via ModelImporter. Skipped. Path: {path}");
			return;
		}

		if (!importer.isReadable)
		{
			importer.isReadable = true;
			importer.SaveAndReimport();
			Debug.Log($"Set Read/Write Enabled ON → {mesh.name}");
		}

		processed.Add(mesh);
	}
	public static void BuildBundleFromPaths(List<string> assetPaths, string bundleName, string outputDirectory)
	{
		if (assetPaths == null || assetPaths.Count == 0)
		{
			Debug.LogWarning("AssetBundleUtility: No asset paths provided.");
			return;
		}

		// Filter out any folder paths
		var filtered = new List<string>();
		foreach (var path in assetPaths)
		{
			if (!AssetDatabase.IsValidFolder(path))
				filtered.Add(path);
		}

		if (filtered.Count == 0)
		{
			Debug.LogWarning("AssetBundleUtility: No valid files found in provided paths.");
			return;
		}

		// Resolve output directory
		string fullOutput = outputDirectory;
		if (!System.IO.Path.IsPathRooted(fullOutput))
			fullOutput = System.IO.Path.Combine(Application.dataPath, "../", outputDirectory);
		if (!System.IO.Directory.Exists(fullOutput))
			System.IO.Directory.CreateDirectory(fullOutput);

		// Setup build map
		var buildMap = new AssetBundleBuild
		{
			assetBundleName = bundleName,
			assetNames = filtered.ToArray()
		};

		// Execute build
		var manifest = BuildPipeline.BuildAssetBundles(
			fullOutput,
			new[] { buildMap },
			BuildAssetBundleOptions.None,
			EditorUserBuildSettings.activeBuildTarget
		);

		if (manifest == null)
			Debug.LogError($"AssetBundleUtility: Failted Building'{bundleName}' with {filtered.Count} assets at {fullOutput}");
		else
			Debug.Log($"AssetBundleUtility: Built '{bundleName}' with {filtered.Count} assets at {fullOutput}");
	}

	private static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs, HashSet<string> excludedRootDirectories = null, bool isRoot = true)
	{
		DirectoryInfo dir = new DirectoryInfo(sourceDirName);
		if (!dir.Exists)
		{
			throw new DirectoryNotFoundException("Source directory does not exist: " + sourceDirName);
		}
		DirectoryInfo[] dirs = dir.GetDirectories();
		if (isRoot && excludedRootDirectories != null && excludedRootDirectories.Count > 0)
		{
			dirs = dirs.Where(subdir => !excludedRootDirectories.Contains(subdir.Name)).ToArray();
		}
		if (!Directory.Exists(destDirName))
		{
			Directory.CreateDirectory(destDirName);
		}
		FileInfo[] files = dir.GetFiles();
		foreach (FileInfo file in files)
		{
			string temppath = Path.Combine(destDirName, file.Name);
			file.CopyTo(temppath, true);
		}
		if (copySubDirs)
		{
			foreach (DirectoryInfo subdir in dirs)
			{
				string temppath = Path.Combine(destDirName, subdir.Name);
				DirectoryCopy(subdir.FullName, temppath, copySubDirs, excludedRootDirectories, false);
			}
		}
	}

	private ExportedProperty CopyProperty(Property prop)
	{
		ExportedProperty ep = new ExportedProperty();
		ep.name = prop.name;
		ep.type = prop.type;
		ep.data = prop.data;
		return ep;
	}

	[System.Serializable]
	public class ExportedModule
	{
		public string id;
		public string name;
		public string type;
		public string controller;
		public string description;
		public string matchDescription;
		public string author;
		public string url;

		public List<PackageDefinition> packages;
		public List<string> customEditors;
		public List<string> dependencies;
		public List<ExportedGroup> itemGroups;
		public List<Property> moduleProperties;
	}

	[System.Serializable]
	public class ExportedGroup
	{
		public string name;
		public string icon;
		public string category;
		public List<ExportedItem> items;
	}

	[System.Serializable]
	public class ExportedItem
	{
		public string id;
		public string name;
		public string description;
		public string category;
		public bool unique = false;
		public bool notDraggable = false;
		public bool template = false;
		public string prefab;
		public string icon;
		public string icon3d;
		public List<ExportedProperty> properties;
		public Vector3 exportTranslation = Vector3.zero;
		public Vector3 exportRotation = Vector3.zero;
		public Vector3 exportScale = Vector3.one;
	}

	[System.Serializable]
	public class ExportedProperty
	{
		public string name;
		public string type;
		public string data;
	}

	private void CreateCustomItem(ItemGroup group)
	{
		Item newItem = new Item();
		newItem.id = System.Guid.NewGuid().ToString().ToUpper();
		newItem.name = "Custom Item";
		newItem.prefab = null;
		newItem.prefabPath = "";
		newItem.icon = "";
		newItem.modelPath = "";
		newItem.properties = new List<Property>();
		newItem.exportTranslation = Vector3.zero;
		newItem.exportRotation = Vector3.zero;
		newItem.exportScale = Vector3.one;
		group.items.Add(newItem);
	}

	private void AddItemsFromFolder(ItemGroup group, string folderPath)
	{
		string relativePath = "Assets" + folderPath.Substring(Application.dataPath.Length);
		string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { relativePath });
		foreach (string guid in guids)
		{
			string assetPath = AssetDatabase.GUIDToAssetPath(guid);
			GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
			if (prefab != null)
			{
				group.items.Add(new Item
				{
					id = System.Guid.NewGuid().ToString().ToUpper(),
					name = prefab.name,
					prefab = prefab,
					prefabPath = assetPath,
					properties = new List<Property>(),
					exportTranslation = Vector3.zero,
					exportRotation = Vector3.zero,
					exportScale = Vector3.one
				});
			}
		}
	}

	private void UpdateAssets()
	{
		var modulePath = GetModuleFolder();
		var texCache = new Dictionary<Texture2D, string>();
		foreach (var group in itemGroups)
		{
			foreach (var item in group.items)
			{
				if (item.prefab != null)
				{
					if (string.IsNullOrEmpty(item.icon) || !File.Exists(Path.Combine(modulePath, item.icon)))
					{
						GenerateThumbnail(item);
					}

					if (string.IsNullOrEmpty(item.modelPath) || !File.Exists(Path.Combine(modulePath, item.modelPath)))
					{
						GenerateModel(item, ExportFormat.OBJ, true, texCache);
					}
				}
			}
		}
	}

	private void UpdateExportAssets()
	{
		var modulePath = GetModuleFolder();
		var texCache = new Dictionary<Texture2D, string>();
		foreach (var group in itemGroups)
		{
			foreach (var item in group.items)
			{
				if (item.prefab != null)
				{
					if (string.IsNullOrEmpty(item.icon) || !File.Exists(Path.Combine(modulePath, item.icon)))
					{
						GenerateThumbnail(item);
					}

					if (string.IsNullOrEmpty(item.modelPath) || !File.Exists(Path.Combine(modulePath, item.modelPath)))
					{
						GenerateModel(item, ExportFormat.OBJ, true, texCache);
					}
				}
			}
		}
	}

	private void GenerateExportThumbnail(Item item)
	{

		if (item.prefab == null)
			return;

		string moduleFolder = GetModuleFolder();
		string assetsDirectory = Path.Combine(moduleFolder, "Assets");
		Directory.CreateDirectory(assetsDirectory);
		string thumbDirectory = Path.Combine(assetsDirectory, "Thumbnails");
		Directory.CreateDirectory(thumbDirectory);
		Texture2D preview = ThumbnailGenerator.RenderPrefabThumbnail(item.prefab, 256, Color.clear);

		if (preview != null)
		{
			try
			{
				byte[] pngData = preview.EncodeToPNG();
				if (pngData != null)
				{
					string thumbPath = Path.Combine(thumbDirectory, item.name + ".png");
					File.WriteAllBytes(thumbPath, pngData);
					string relativeThumbPath = Path.Combine("Assets", "Thumbnails", item.name + ".png");
					item.icon = relativeThumbPath;
				}
			}
			catch
			{
				item.icon = string.Empty;
			}
		}
	}


	private void GenerateModelsForGroup(ItemGroup group)
	{
		foreach (var item in group.items)
		{
			GenerateModel(item);
			GenerateThumbnail(item);
		}
		Debug.Log($"Models and thumbnails for group '{group.name}' extracted successfully!");
	}

	public enum ExportFormat { OBJ, GLB }

	private void GenerateModel(Item item, ExportFormat format = ExportFormat.OBJ, bool includeMaterials = true, Dictionary<Texture2D, string> texCache = null)
	{
		string moduleFolder = GetModuleFolder();
		string assetsDirectory = Path.Combine(moduleFolder, "Assets");
		Directory.CreateDirectory(assetsDirectory);
		string modelDirectory = Path.Combine(assetsDirectory, "Models");
		Directory.CreateDirectory(modelDirectory);

		if (item.prefab == null)
		{
			Debug.LogWarning($"No prefab for item: {item.name}");
			return;
		}

		GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(item.prefab);
		instance.hideFlags = HideFlags.DontSave;
		instance.transform.position = item.exportTranslation;
		instance.transform.rotation = Quaternion.Euler(item.exportRotation);
		instance.transform.localScale = item.exportScale;

		try
		{
			var merged = MergeAllMeshes(instance);
			if (merged == null || merged.Mesh == null)
			{
				Debug.LogWarning($"No valid mesh found for item: {item.name}");
				return;
			}

			if (format == ExportFormat.GLB)
			{
				string glbPath = Path.Combine(modelDirectory, item.name + ".glb");
				if (TryExportGLB(merged, glbPath, includeMaterials))
				{
					item.modelPath = Path.Combine("Assets", "Models", item.name + ".glb");
					return;
				}

				Debug.LogWarning("GLB export unavailable (UniGLTF not detected). Falling back to OBJ.");
			}

			// OBJ (+ optional MTL)
			string objPath = Path.Combine(modelDirectory, item.name + ".obj");
			SaveMeshAsOBJ(merged, objPath, item.name, includeMaterials, texCache);
			item.modelPath = Path.Combine("Assets", "Models", item.name + ".obj");
		}
		finally
		{
			if (Application.isEditor) UnityEngine.Object.DestroyImmediate(instance);
			else UnityEngine.Object.Destroy(instance);
		}
	}

	private class MergedMesh
	{
		public Mesh Mesh;
		public Material[] Materials;          // per-submesh
		public List<Texture2D> MainTextures;  // aligned with Materials
		public string[] MaterialNames;
	}

	private MergedMesh MergeAllMeshes(GameObject root)
	{
		var meshFilters = root.GetComponentsInChildren<MeshFilter>(true);
		var skinned = root.GetComponentsInChildren<SkinnedMeshRenderer>(true);

		var combineInstances = new List<CombineInstance>();
		var materialList = new List<Material>();
		var textureList = new List<Texture2D>();
		var materialNameList = new List<string>();

		void AddMesh(Mesh mesh, Transform t, Material[] mats)
		{
			if (mesh == null || mats == null || mats.Length == 0) return;

			for (int si = 0; si < mesh.subMeshCount; si++)
			{
				combineInstances.Add(new CombineInstance { mesh = mesh, subMeshIndex = si, transform = t.localToWorldMatrix });
				var mat = mats[Mathf.Min(si, mats.Length - 1)];
				materialList.Add(mat);
				textureList.Add(GetMainTexture(mat));
				materialNameList.Add(SafeMaterialName(mat));
			}
		}

		foreach (var mf in meshFilters)
		{
			var mr = mf.GetComponent<MeshRenderer>();
			if (mr == null || !mr.enabled) continue;
			AddMesh(mf.sharedMesh, mf.transform, mr.sharedMaterials);
		}

		foreach (var smr in skinned)
		{
			if (smr == null || !smr.enabled) continue;
			var baked = new Mesh();
			smr.BakeMesh(baked, true);
			AddMesh(baked, smr.transform, smr.sharedMaterials);
		}

		if (combineInstances.Count == 0) return null;

		var merged = new Mesh { name = root.name + "_Merged" };
		merged.CombineMeshes(combineInstances.ToArray(), /*mergeSubMeshes*/ false, /*useMatrices*/ true, /*hasLightmapData*/ false);
		if (merged.normals == null || merged.normals.Length == 0) merged.RecalculateNormals();
		if (merged.tangents == null || merged.tangents.Length == 0) merged.RecalculateTangents();
		merged.RecalculateBounds();

		return new MergedMesh
		{
			Mesh = merged,
			Materials = materialList.ToArray(),
			MainTextures = textureList,
			MaterialNames = materialNameList.ToArray()
		};
	}

	private static Texture2D GetMainTexture(Material m)
	{
		if (m == null) return null;
		var props = new[] { "_BaseMap", "_MainTex" };
		foreach (var p in props) if (m.HasProperty(p)) return m.GetTexture(p) as Texture2D;
		return null;
	}

	private static string SafeMaterialName(Material m)
	{
		var n = (m != null && !string.IsNullOrEmpty(m.name)) ? m.name : "Material";
		foreach (var c in Path.GetInvalidFileNameChars()) n = n.Replace(c, '_');
		return n.Replace(' ', '_');
	}

	// ---------- OBJ EXPORT (with switch) ----------
	private void SaveMeshAsOBJ(MergedMesh merged, string objPath, string baseName, bool includeMaterials, Dictionary<Texture2D, string> texCache)
	{
		var mesh = merged.Mesh;
		var dir = Path.GetDirectoryName(objPath);
		Directory.CreateDirectory(dir);

		string objFileNameNoExt = Path.GetFileNameWithoutExtension(objPath);
		string mtlPath = Path.Combine(dir, objFileNameNoExt + ".mtl");

		var sb = new StringBuilder();
		sb.AppendLine("# Exported by YourExporterClass (OBJ)");

		if (includeMaterials)
			sb.AppendLine($"mtllib {objFileNameNoExt}.mtl");

		sb.AppendLine($"o {mesh.name}");

		// v
		foreach (var v in mesh.vertices) sb.AppendLine($"v {v.x} {v.y} {v.z}");

		// vt
		var uvs = new List<Vector2>();
		mesh.GetUVs(0, uvs);
		if (uvs == null || uvs.Count != mesh.vertexCount)
			uvs = Enumerable.Repeat(Vector2.zero, mesh.vertexCount).ToList();
		foreach (var uv in uvs) sb.AppendLine($"vt {uv.x} {uv.y}");

		// vn
		var normals = mesh.normals;
		if (normals == null || normals.Length != mesh.vertexCount)
		{
			mesh.RecalculateNormals();
			normals = mesh.normals;
		}
		foreach (var n in normals) sb.AppendLine($"vn {n.x} {n.y} {n.z}");

		// Faces
		int vertexOffset = 1;
		for (int sm = 0; sm < mesh.subMeshCount; sm++)
		{
			if (includeMaterials)
			{
				string mName = merged.MaterialNames[Mathf.Min(sm, merged.MaterialNames.Length - 1)];
				sb.AppendLine($"usemtl {mName}");
			}

			var tris = mesh.GetTriangles(sm);
			for (int i = 0; i < tris.Length; i += 3)
			{
				int a = tris[i] + vertexOffset;
				int b = tris[i + 1] + vertexOffset;
				int c = tris[i + 2] + vertexOffset;
				sb.AppendLine($"f {a}/{a}/{a} {b}/{b}/{b} {c}/{c}/{c}");
			}
		}

		File.WriteAllText(objPath, sb.ToString(), Encoding.UTF8);

		// MTL (only if requested)
		if (includeMaterials)
		{
			var mtl = new StringBuilder();
			mtl.AppendLine("# Exported by Plyground");
			for (int i = 0; i < merged.Materials.Length; i++)
			{
				var mat = merged.Materials[i];
				var matName = merged.MaterialNames[i];
				mtl.AppendLine($"newmtl {matName}");

				Color col = Color.white;
				if (mat != null && mat.HasProperty("_BaseColor")) col = mat.GetColor("_BaseColor");
				else if (mat != null && mat.HasProperty("_Color")) col = mat.GetColor("_Color");
				mtl.AppendLine($"Kd {col.r} {col.g} {col.b}");
				mtl.AppendLine("Ks 0 0 0");
				mtl.AppendLine("Ns 0");
				mtl.AppendLine("d 1");

				var tex = merged.MainTextures[i];
				if (tex != null)
				{
					string texFile = $"{objFileNameNoExt}_{matName}.png";
					if (texCache != null && texCache.TryGetValue(tex, out string cached))
					{
						texFile = cached;
					}
					else
					{
						texCache[tex] = texFile;
						string texPath = Path.Combine(dir, texFile);
						WriteTextureToPng(tex, texPath);
					}

					mtl.AppendLine($"map_Kd {texFile}");
				}
				mtl.AppendLine();
			}
			File.WriteAllText(mtlPath, mtl.ToString(), Encoding.UTF8);
		}

		Debug.Log($"OBJ exported: {objPath} {(includeMaterials ? "(with materials)" : "(geometry only)")}");
	}

	private static void WriteTextureToPng(Texture2D source, string outPath)
	{
		Texture2D readable = source;
		bool createdTemp = false;
		try
		{
			if (!source.isReadable)
			{
				var rt = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
				Graphics.Blit(source, rt);
				var prev = RenderTexture.active;
				RenderTexture.active = rt;

				readable = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false, true);
				readable.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0, false);
				readable.Apply();

				RenderTexture.active = prev;
				RenderTexture.ReleaseTemporary(rt);
				createdTemp = true;
			}
			var bytes = readable.EncodeToPNG();
			File.WriteAllBytes(outPath, bytes);
		}
		finally
		{
			if (createdTemp && readable != null) UnityEngine.Object.DestroyImmediate(readable);
		}
	}

	// ---------- GLB EXPORT (with switch) ----------
	private bool TryExportGLB(MergedMesh merged, string glbPath, bool includeMaterials)
	{
		// Build a transient GO for export
		var go = new GameObject("GLB_Export_Temp");
		try
		{
			var mf = go.AddComponent<MeshFilter>();
			Mesh meshForExport = merged.Mesh;

			// If we don't want materials, collapse to a single submesh so the exporter
			// doesn't emit multiple primitives that might each get a default material.
			Material[] matsForExport = includeMaterials ? merged.Materials : Array.Empty<Material>();
			if (!includeMaterials && meshForExport.subMeshCount > 1)
			{
				meshForExport = new Mesh { name = merged.Mesh.name + "_SingleSub" };
				meshForExport.CombineMeshes(new[]
				{
				new CombineInstance{ mesh = merged.Mesh, transform = Matrix4x4.identity, subMeshIndex = 0 }
			}, /*mergeSubMeshes*/ true, /*useMatrices*/ false, /*hasLightmap*/ false);
				// The above with subMeshIndex=0 merges all only if original had 1 submesh.
				// To force merge-all, do a manual rebuild:
				meshForExport = ForceCombineAllSubmeshes(merged.Mesh);
			}

			mf.sharedMesh = meshForExport;

			var mr = go.AddComponent<MeshRenderer>();
			mr.sharedMaterials = matsForExport;

			// UniGLTF reflection
			var gltfExporterType = Type.GetType("UniGLTF.GltfExporter, UniGLTF");
			var exportSettingsType = Type.GetType("UniGLTF.ExportSettings, UniGLTF");
			var glbExportType = Type.GetType("UniGLTF.GlbFile, UniGLTF");

			if (gltfExporterType == null || exportSettingsType == null || glbExportType == null)
				return false;

			var exportSettings = Activator.CreateInstance(exportSettingsType);

			// Best-effort: if ExportSettings has a flag to disable materials, flip it.
			// Different UniGLTF versions vary; try a few property names.
			var maybeMaterialFlag = exportSettingsType.GetProperty("ExportMaterials")
									?? exportSettingsType.GetProperty("exportMaterials")
									?? exportSettingsType.GetProperty("Materials");
			if (maybeMaterialFlag != null && maybeMaterialFlag.PropertyType == typeof(bool))
			{
				maybeMaterialFlag.SetValue(exportSettings, includeMaterials);
			}

			var exporter = Activator.CreateInstance(gltfExporterType, new object[] { exportSettings });

			var prepare = gltfExporterType.GetMethod("Prepare", new[] { typeof(GameObject) });
			prepare.Invoke(exporter, new object[] { go });

			byte[] bytes = null;
			var exportAsGlbBytes = gltfExporterType.GetMethod("ExportAsGlbBytes", Type.EmptyTypes);
			if (exportAsGlbBytes != null)
			{
				bytes = (byte[])exportAsGlbBytes.Invoke(exporter, null);
			}
			else
			{
				var exportMethod = gltfExporterType.GetMethod("Export", Type.EmptyTypes);
				exportMethod?.Invoke(exporter, null);

				var fromExporter = glbExportType.GetMethod("FromGltfExporter", new[] { gltfExporterType });
				var glbObj = fromExporter?.Invoke(null, new[] { exporter });
				var toBytes = glbExportType.GetMethod("ToBytes", Type.EmptyTypes);
				bytes = (byte[])toBytes?.Invoke(glbObj, null);
			}

			if (bytes == null || bytes.Length == 0)
			{
				Debug.LogWarning("UniGLTF export produced no data.");
				return false;
			}

			Directory.CreateDirectory(Path.GetDirectoryName(glbPath));
			File.WriteAllBytes(glbPath, bytes);
			Debug.Log($"GLB exported: {glbPath} {(includeMaterials ? "(with materials)" : "(geometry-only / minimal material)")}");

			return true;
		}
		catch (Exception ex)
		{
			Debug.LogWarning($"GLB export failed: {ex.Message}");
			return false;
		}
		finally
		{
			UnityEngine.Object.DestroyImmediate(go);
		}
	}

	// Force-merge all submeshes into one (keeps vertex data/uvs/normals)
	private Mesh ForceCombineAllSubmeshes(Mesh src)
	{
		var dst = new Mesh { name = src.name + "_MergedOneSub" };
		dst.vertices = src.vertices;
		dst.normals = (src.normals != null && src.normals.Length == src.vertexCount) ? src.normals : null;
		if (dst.normals == null) { dst.RecalculateNormals(); }
		var uvs = new List<Vector2>();
		src.GetUVs(0, uvs);
		if (uvs != null && uvs.Count == src.vertexCount) dst.SetUVs(0, uvs);

		// Concatenate all triangles into a single submesh
		var all = new List<int>();
		for (int i = 0; i < src.subMeshCount; i++) all.AddRange(src.GetTriangles(i));
		dst.subMeshCount = 1;
		dst.SetTriangles(all, 0);
		dst.RecalculateBounds();
		return dst;
	}

	private void GenerateThumbnail(Item item)
	{

		if (item.prefab == null)
			return;
		string moduleFolder = GetModuleFolder();
		string assetsDirectory = Path.Combine(moduleFolder, "Assets");
		Directory.CreateDirectory(assetsDirectory);
		string thumbDirectory = Path.Combine(assetsDirectory, "Thumbnails");
		Directory.CreateDirectory(thumbDirectory);
		Texture2D preview = AssetPreview.GetAssetPreview(item.prefab);
		if (preview == null)
		{
			double start = EditorApplication.timeSinceStartup;
			float timeoutSeconds = 2f;
			// Poll without blocking editor message pump
			while (AssetPreview.IsLoadingAssetPreview(item.prefab.GetInstanceID()))
			{
				// Try to get the texture each tick
				preview = AssetPreview.GetAssetPreview(item.prefab);
				if (preview != null) break;

				// Give the editor a breath so jobs advance
				System.Threading.Thread.Sleep(15);

				// Bail on timeout to avoid infinite loops
				if (EditorApplication.timeSinceStartup - start > timeoutSeconds)
					break;
			}

			// Fallback: a tiny icon so you at least have something
			//preview ??= AssetPreview.GetMiniThumbnail(item.prefab);
		}

		if (preview == null)
		{
			//preview = EditorGUIUtility.IconContent("DefaultAsset Icon").image as Texture2D;
		}

		if (preview != null)
		{
			try
			{
				byte[] pngData = preview.EncodeToPNG();
				if (pngData != null)
				{
					string thumbPath = Path.Combine(thumbDirectory, item.name + ".png");
					File.WriteAllBytes(thumbPath, pngData);
					string relativeThumbPath = Path.Combine("Assets", "Thumbnails", item.name + ".png");
					item.icon = relativeThumbPath;
				}
			}
			catch
			{
				item.icon = string.Empty;
			}
		}
		else
		{
			GenerateExportThumbnail(item);
		}
	}

	private string CopyCustomIcon(string imagePath)
	{
		string moduleFolder = GetAssetModuleFolder();
		string assetsDirectory = Path.Combine(moduleFolder, "Assets");
		Directory.CreateDirectory(assetsDirectory);
		string thumbDirectory = Path.Combine(assetsDirectory, "Thumbnails");
		Directory.CreateDirectory(thumbDirectory);

		var filename = Path.GetFileName(imagePath);
		File.Copy(imagePath, Path.Combine(thumbDirectory, filename), true);

		return Path.Combine("Assets", "Thumbnails", filename);
	}

	private string GetAssetModuleFolder()
	{
		return Path.Combine(Application.dataPath, "Plyground/Module");
	}

	private Mesh GetLowestLODMesh(GameObject prefab)
	{
		LODGroup lodGroup = prefab.GetComponent<LODGroup>();
		if (lodGroup != null && lodGroup.GetLODs().Length > 0)
		{
			return ExtractMesh(lodGroup.GetLODs()[lodGroup.GetLODs().Length - 1].renderers[0]);
		}
		MeshRenderer meshRenderer = prefab.GetComponentInChildren<MeshRenderer>();
		return meshRenderer != null ? ExtractMesh(meshRenderer) : prefab.GetComponentInChildren<SkinnedMeshRenderer>()?.sharedMesh;
	}

	private Mesh ExtractMesh(Renderer renderer)
	{
		try
		{
			return renderer is MeshRenderer meshRenderer ? meshRenderer.GetComponent<MeshFilter>()?.sharedMesh : null;
		}
		catch
		{
			return null;
		}
	}

	private Texture2D LoadTextureFromFile(string filePath)
	{
		if (!File.Exists(filePath))
			return null;
		byte[] data = File.ReadAllBytes(filePath);
		Texture2D tex = new Texture2D(2, 2);
		tex.LoadImage(data);
		return tex;
	}

	private bool IsSimpleType(System.Type type)
	{
		if (type.IsPrimitive || type == typeof(string) || type.IsEnum)
			return true;
		if (type == typeof(Vector2) || type == typeof(Vector3) ||
			type == typeof(Vector4) || type == typeof(Color))
			return true;
		return false;
	}
}

public class CustomPropertiesPopup : EditorWindow
{
	private List<ModuleExporter.Property> properties;
	private ReorderableList reorderableList;
	private readonly string[] allowedTypes = new string[] { "string", "int", "float", "bool", "object" };

	public static void ShowPopup(List<ModuleExporter.Property> properties)
	{
		CustomPropertiesPopup window = ScriptableObject.CreateInstance<CustomPropertiesPopup>();
		window.properties = properties;
		window.InitReorderableList();
		window.titleContent = new GUIContent("Edit Global Properties");
		window.minSize = new Vector2(400, 300);
		window.ShowUtility();
	}

	private void InitReorderableList()
	{
		reorderableList = new ReorderableList(properties, typeof(ModuleExporter.Property), true, true, true, true);
		reorderableList.drawHeaderCallback = (Rect rect) =>
		{
			EditorGUI.LabelField(rect, "Global Properties");
		};

		reorderableList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
		{
			if (index < properties.Count)
			{
				var prop = properties[index];
				float labelW = 50f;
				float nameFieldW = 150f;
				float typeLabelW = 40f;
				float typeFieldW = 80f;
				float valueLabelW = 50f;
				float valueFieldW = 150f;
				float editorLabelW = 50f;
				float editorFieldW = 80f;
				float x = rect.x;
				float y = rect.y + 2;

				Rect r = new Rect(x, y, labelW, EditorGUIUtility.singleLineHeight);
				EditorGUI.LabelField(r, "Name");
				x += labelW;
				r = new Rect(x, y, nameFieldW, EditorGUIUtility.singleLineHeight);
				prop.name = EditorGUI.TextField(r, prop.name);
				x += nameFieldW;

				Rect r2 = new Rect(x, y, typeLabelW, EditorGUIUtility.singleLineHeight);
				EditorGUI.LabelField(r2, "Type");
				x += typeLabelW;
				int typeIndex = System.Array.IndexOf(allowedTypes, prop.type);
				if (typeIndex < 0)
				{
					typeIndex = 0;
					prop.type = allowedTypes[0];
				}
				Rect r3 = new Rect(x, y, typeFieldW, EditorGUIUtility.singleLineHeight);
				typeIndex = EditorGUI.Popup(r3, typeIndex, allowedTypes);
				prop.type = allowedTypes[typeIndex];
				x += typeFieldW;

				/*				Rect r4 = new Rect(x, y, valueLabelW, EditorGUIUtility.singleLineHeight);
								EditorGUI.LabelField(r4, "Value");
								x += valueLabelW;
								Rect r5 = new Rect(x, y, valueFieldW, EditorGUIUtility.singleLineHeight);
								string currentVal = prop.value != null ? prop.value.ToString() : "";
								string newVal = EditorGUI.TextField(r5, currentVal);
								if (newVal != currentVal)
								{
									prop.value = newVal;
								}
								x += valueFieldW;
				*/
				if (prop.type == "object")
				{
					Rect r6 = new Rect(x, y, editorLabelW, EditorGUIUtility.singleLineHeight);
					EditorGUI.LabelField(r6, "Editor");
					x += editorLabelW;
					Rect r7 = new Rect(x, y, editorFieldW, EditorGUIUtility.singleLineHeight);
					prop.data = EditorGUI.TextField(r7, prop.data);
				}
				else if (prop.type == "gameitem")
				{
					if (GUILayout.Button("Edit..."))
					{
					}
				}
			}
		};

		reorderableList.onAddCallback = (ReorderableList list) =>
		{
			properties.Add(new ModuleExporter.Property { name = "NewProperty", type = "string", data = "" });
		};

		reorderableList.onRemoveCallback = (ReorderableList list) =>
		{
			properties.RemoveAt(list.index);
		};
	}

	private void OnGUI()
	{
		reorderableList.DoLayoutList();
		if (GUILayout.Button("Close"))
		{
			Close();
		}
	}
}

public class ComponentPropertiesPopup : EditorWindow
{
	private ModuleExporter.Item selectedItem;
	private Vector2 scrollPos;
	// A list of available fields to add. Each entry contains the component, its field, and a key string.
	private List<(MonoBehaviour comp, FieldInfo field, string key)> availableFields = new List<(MonoBehaviour, FieldInfo, string)>();
	// Allowed types.
	private readonly string[] allowedTypes = new string[] { "string", "int", "float", "bool", "object" };

	public static void ShowPopup(ModuleExporter.Item item)
	{
		ComponentPropertiesPopup window = ScriptableObject.CreateInstance<ComponentPropertiesPopup>();
		window.selectedItem = item;
		window.titleContent = new GUIContent("Add Component Property");
		window.position = new Rect(Screen.width / 2, Screen.height / 2, 400, 300);
		window.PopulateAvailableFields();
		window.ShowUtility();
	}

	private void PopulateAvailableFields()
	{
		availableFields.Clear();
		if (selectedItem.prefab == null)
			return;

		MonoBehaviour[] comps = selectedItem.prefab.GetComponents<MonoBehaviour>();
		foreach (var comp in comps)
		{
			if (comp == null) continue;
			FieldInfo[] allFields = comp.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public);
			foreach (var field in allFields)
			{
				// Skip Unity internal fields.
				if (field.DeclaringType == typeof(MonoBehaviour) ||
					(field.DeclaringType.Namespace != null && field.DeclaringType.Namespace.StartsWith("UnityEngine")))
					continue;
				// Only allow simple types.
				if (!IsSimpleType(field.FieldType))
					continue;
				string key = comp.GetType().Name + "." + field.Name;
				if (selectedItem.properties == null || !selectedItem.properties.Any(p => p.name == key))
				{
					availableFields.Add((comp, field, key));
				}
			}
		}
	}

	private bool IsSimpleType(System.Type type)
	{
		if (type.IsPrimitive || type == typeof(string) || type.IsEnum)
			return true;
		if (type == typeof(Vector2) || type == typeof(Vector3) ||
			type == typeof(Vector4) || type == typeof(Color))
			return true;
		return false;
	}

	private void OnGUI()
	{
		if (selectedItem.prefab == null)
		{
			EditorGUILayout.HelpBox("No prefab available.", MessageType.Warning);
			if (GUILayout.Button("Close"))
			{
				Close();
			}
			return;
		}

		GUILayout.Label("Available Component Properties", EditorStyles.boldLabel);
		scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
		if (availableFields.Count == 0)
		{
			EditorGUILayout.HelpBox("No available properties to add.", MessageType.Info);
		}
		else
		{
			foreach (var entry in availableFields)
			{
				if (GUILayout.Button(entry.key))
				{
					object defaultVal = entry.field.GetValue(entry.comp);
					if (selectedItem.properties == null)
						selectedItem.properties = new List<ModuleExporter.Property>();

					selectedItem.properties.Add(new ModuleExporter.Property
					{
						name = entry.field.Name,
						type = ModuleExporter.TranslateType(entry.field.FieldType),
						data = entry.comp.GetType().Name,
					});

					PopulateAvailableFields();
					Close();
					break;
				}
			}
		}
		EditorGUILayout.EndScrollView();
		if (GUILayout.Button("Close"))
		{
			Close();
		}
	}
}
