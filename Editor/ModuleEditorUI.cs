using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public partial class ModuleExporter
{
	private enum ModuleEditorTab
	{
		Overview,
		Files,
		Items,
		Capabilities,
		Export
	}

	private readonly string[] topTabs = { "Overview", "Files", "Items", "Capabilities", "Export" };

	private ModuleEditorTab activeTab;
	private Vector2 _assetScroll;
	private Vector2 _itemGridScroll;
	private GUIStyle brandCardStyle;
	private GUIStyle brandTitleStyle;
	private GUIStyle brandSubtitleStyle;
	private Texture2D brandLogoTexture;
	private int activeItemGroupIndex;
	private int capabilityItemGroupIndex;
	private int capabilityItemIndex;
	private int selectedModuleFeatureIndex;
	private int selectedItemFeatureIndex;
	private int selectedTypeIndex = -1;
	private int selectedTypeFieldIndex = -1;
	private int selectedComponentIndex = -1;
	private int selectedComponentArtifactIndex = -1;
	private Dictionary<string, bool> capabilitySectionFoldouts = new Dictionary<string, bool>();
	private Dictionary<string, bool> componentNamespaceFoldouts = new Dictionary<string, bool>();
	private const string PackageLogoAssetPath = "Packages/com.plyground.export/Editor/Branding/plyground-logo.png";
	private const string LocalLogoAssetPath = "Editor/Branding/plyground-logo.png";

	private void OnGUI()
	{
		DrawRefactoredUi();
	}

	private void DrawRefactoredUi()
	{
		EnsureUiStyles();

		EditorGUILayout.BeginVertical();
		DrawBrandHeader();
		DrawTabs();

		scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.ExpandHeight(true));

		switch (activeTab)
		{
			case ModuleEditorTab.Overview:
				DrawOverviewTab();
				break;
			case ModuleEditorTab.Files:
				DrawFilesTab();
				break;
			case ModuleEditorTab.Items:
				DrawItemsTab();
				break;
			case ModuleEditorTab.Export:
				DrawExportTab();
				break;
			case ModuleEditorTab.Capabilities:
				DrawCapabilitiesTab();
				break;
		}

		EditorGUILayout.EndScrollView();
		EditorGUILayout.EndVertical();
	}

	private void EnsureUiStyles()
	{
		if (brandCardStyle != null)
		{
			return;
		}

		brandCardStyle = new GUIStyle(EditorStyles.helpBox)
		{
			padding = new RectOffset(16, 16, 14, 14),
			margin = new RectOffset(10, 10, 10, 8)
		};

		brandTitleStyle = new GUIStyle(EditorStyles.boldLabel)
		{
			fontSize = 18
		};

		brandSubtitleStyle = new GUIStyle(EditorStyles.miniLabel)
		{
			fontSize = 11,
			wordWrap = true
		};

		brandLogoTexture = CreateBrandLogoTexture();
	}

	private void DrawBrandHeader()
	{
		EditorGUILayout.BeginVertical(brandCardStyle);
		EditorGUILayout.BeginHorizontal();

		if (brandLogoTexture != null)
		{
			GUILayout.Label(brandLogoTexture, GUILayout.Width(44f), GUILayout.Height(72f));
			GUILayout.Space(10f);
		}

		EditorGUILayout.BeginVertical();
		GUILayout.Label("Plyground EXPORTER", brandTitleStyle);
		GUILayout.Label("Build module metadata, curate item groups, and export from one branded workspace.", brandSubtitleStyle);
		EditorGUILayout.EndVertical();

		GUILayout.FlexibleSpace();

		EditorGUILayout.BeginVertical(GUILayout.Width(200f));
		GUILayout.Label($"Active Module: {(string.IsNullOrWhiteSpace(moduleName) ? "Untitled Module" : moduleName)}", EditorStyles.miniBoldLabel);
		GUILayout.Label($"Type: {moduleType}", EditorStyles.miniLabel);
		GUILayout.Label($"Export Path: {(string.IsNullOrWhiteSpace(exportPath) ? "Not set" : exportPath)}", EditorStyles.wordWrappedMiniLabel);
		EditorGUILayout.EndVertical();

		EditorGUILayout.EndHorizontal();
		EditorGUILayout.EndVertical();
	}

	private Texture2D CreateBrandLogoTexture()
	{
		Texture2D logo = AssetDatabase.LoadAssetAtPath<Texture2D>(PackageLogoAssetPath);
		if (logo != null)
		{
			return logo;
		}

		logo = AssetDatabase.LoadAssetAtPath<Texture2D>(LocalLogoAssetPath);
		if (logo != null)
		{
			return logo;
		}

		const int width = 88;
		const int height = 144;

		Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
		{
			hideFlags = HideFlags.HideAndDontSave,
			filterMode = FilterMode.Bilinear,
			wrapMode = TextureWrapMode.Clamp
		};

		Color clear = new Color(0f, 0f, 0f, 0f);
		Color leftBottom = new Color32(61, 120, 128, 255);
		Color leftTop = new Color32(39, 166, 131, 255);
		Color rightBottom = new Color32(69, 195, 124, 255);
		Color rightTop = new Color32(28, 193, 122, 255);

		for (int y = 0; y < height; y++)
		{
			for (int x = 0; x < width; x++)
			{
				texture.SetPixel(x, y, clear);
			}
		}

		for (int y = 8; y < height - 8; y++)
		{
			for (int x = 4; x < 26; x++)
			{
				if (IsInsideRoundedRect(x, y, 4, 8, 22, height - 16, 10f))
				{
					texture.SetPixel(x, y, Color.Lerp(leftBottom, leftTop, y / (float)(height - 1)));
				}
			}
		}

		for (int y = 8; y < height - 8; y++)
		{
			for (int x = 36; x < width - 4; x++)
			{
				if (!IsInsideRoundedRect(x, y, 36, 8, width - 40, height - 16, 10f))
				{
					continue;
				}

				bool inOuterArc = IsInsideCircle(x, y, 52f, 72f, 38f) || IsInsideCircle(x, y, 52f, 116f, 38f);
				bool inInnerCut = IsInsideRoundedRect(x, y, 50, 28, 18, 88, 8f);
				if ((x < 60 || inOuterArc) && !inInnerCut)
				{
					float tx = Mathf.InverseLerp(36f, width - 4f, x);
					float ty = Mathf.InverseLerp(8f, height - 8f, y);
					Color horizontal = Color.Lerp(leftTop, rightTop, tx);
					Color vertical = Color.Lerp(rightBottom, horizontal, ty);
					texture.SetPixel(x, y, vertical);
				}
			}
		}

		texture.Apply();
		return texture;
	}

	private static bool IsInsideRoundedRect(int x, int y, int rectX, int rectY, int rectWidth, int rectHeight, float radius)
	{
		float minX = rectX;
		float maxX = rectX + rectWidth - 1;
		float minY = rectY;
		float maxY = rectY + rectHeight - 1;
		float px = x + 0.5f;
		float py = y + 0.5f;

		if (px >= minX + radius && px <= maxX - radius)
		{
			return py >= minY && py <= maxY;
		}

		if (py >= minY + radius && py <= maxY - radius)
		{
			return px >= minX && px <= maxX;
		}

		float cornerX = px < minX + radius ? minX + radius : maxX - radius;
		float cornerY = py < minY + radius ? minY + radius : maxY - radius;
		float dx = px - cornerX;
		float dy = py - cornerY;
		return dx * dx + dy * dy <= radius * radius;
	}

	private static bool IsInsideCircle(int x, int y, float centerX, float centerY, float radius)
	{
		float dx = x + 0.5f - centerX;
		float dy = y + 0.5f - centerY;
		return dx * dx + dy * dy <= radius * radius;
	}

	private void DrawTabs()
	{
		EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
		activeTab = (ModuleEditorTab)GUILayout.Toolbar((int)activeTab, topTabs, EditorStyles.toolbarButton);
		GUILayout.FlexibleSpace();
		if (GUILayout.Button("Save", EditorStyles.toolbarButton, GUILayout.Width(110f)))
		{
			SaveModule();
		}
		EditorGUILayout.EndHorizontal();
		EditorGUILayout.Space(8f);
	}

	private void DrawOverviewTab()
	{
		DrawModuleSettingsSection();
		EditorGUILayout.Space();
		DrawModulePropertiesSection();
	}

	private void DrawModuleSettingsSection()
	{
		GUILayout.Label("MODULE SETTINGS", EditorStyles.boldLabel);
		EditorGUILayout.BeginHorizontal();

		float sectionWidth = Mathf.Max(280f, position.width * 0.5f - 24f);

		EditorGUILayout.BeginVertical("box", GUILayout.Width(sectionWidth));
		moduleName = EditorGUILayout.TextField("Module Name", moduleName);
		controllerClass = EditorGUILayout.TextField("Controller Class", controllerClass);

		int moduleTypeIndex = System.Array.IndexOf(allowedModuleTypes, moduleType);
		if (moduleTypeIndex < 0)
		{
			moduleTypeIndex = 0;
			moduleType = allowedModuleTypes[0];
		}

		moduleTypeIndex = EditorGUILayout.Popup("Module Type", moduleTypeIndex, allowedModuleTypes);
		moduleType = allowedModuleTypes[moduleTypeIndex];
		EditorGUILayout.Space(6f);
		GUILayout.Label("Description", EditorStyles.miniBoldLabel);
		description = EditorGUILayout.TextArea(description, GUILayout.MinHeight(70f));
		EditorGUILayout.EndVertical();

		EditorGUILayout.BeginVertical("box", GUILayout.Width(sectionWidth));
		author = EditorGUILayout.TextField("Author", author);
		url = EditorGUILayout.TextField("URL", url);
		GUILayout.Label($"Module ID: {(string.IsNullOrEmpty(moduleId) ? "Generated on export" : moduleId)}", EditorStyles.miniLabel);
		EditorGUILayout.Space(6f);
		GUILayout.Label("Match Description", EditorStyles.miniBoldLabel);
		matchDescription = EditorGUILayout.TextArea(matchDescription, GUILayout.MinHeight(70f));
		EditorGUILayout.EndVertical();

		EditorGUILayout.EndHorizontal();
	}

	private void DrawPackagesSection()
	{
		GUILayout.Label("PACKAGES", EditorStyles.boldLabel);
		EditorGUILayout.BeginVertical("box");
		DrawUnityPackageList();
		EditorGUILayout.Space(6f);
		DrawDependencyList();
		EditorGUILayout.Space(6f);
		DrawCustomEditorList();
		EditorGUILayout.Space(6f);
		DrawFilesToRemoveSection();
		EditorGUILayout.EndVertical();
	}

	private void DrawFilesTab()
	{
		DrawPackagesSection();
	}

	private void DrawModulePropertiesSection()
	{
		GUILayout.Label("MODULE PROPERTIES", EditorStyles.boldLabel);
		DrawModulePropertiesEditor();
	}

	private void DrawModulePropertiesEditor()
	{
		float sectionWidth = Mathf.Max(280f, position.width * 0.5f - 24f);
		EditorGUILayout.BeginVertical("box", GUILayout.Width(sectionWidth));

		EditorGUILayout.BeginHorizontal();
		GUILayout.Label("Module Properties", EditorStyles.boldLabel);
		GUILayout.FlexibleSpace();
		if (GUILayout.Button("Add Property", GUILayout.Width(110f)))
		{
			moduleProperties ??= new List<Property>();
			moduleProperties.Add(new Property
			{
				name = "NewProperty",
				type = allowedTypes[0],
				data = string.Empty,
				value = string.Empty
			});
		}
		EditorGUILayout.EndHorizontal();

		float reserved = 44f;
		float gap = 3f;
		float usable = sectionWidth - reserved - (gap * 2f);
		float nameWidth = Mathf.Max(60f, usable * 0.40f);
		float typeWidth = Mathf.Max(60f, usable * 0.20f);
		float valueWidth = Mathf.Max(60f, usable * 0.40f);

		EditorGUILayout.BeginHorizontal();
		GUILayout.Label("Name", EditorStyles.miniBoldLabel, GUILayout.Width(nameWidth));
		GUILayout.Label("Type", EditorStyles.miniBoldLabel, GUILayout.Width(typeWidth));
		GUILayout.Label("Value", EditorStyles.miniBoldLabel, GUILayout.Width(valueWidth));
		GUILayout.Space(reserved);
		EditorGUILayout.EndHorizontal();

		if (moduleProperties == null || moduleProperties.Count == 0)
		{
			EditorGUILayout.HelpBox("No module properties yet. Click 'Add Property' to create one.", MessageType.Info);
			EditorGUILayout.EndVertical();
			return;
		}

		_assetScroll = EditorGUILayout.BeginScrollView(_assetScroll, GUILayout.Height(110f));
		for (int i = 0; i < moduleProperties.Count; i++)
		{
			Property prop = moduleProperties[i];
			EditorGUILayout.BeginHorizontal("helpbox");
			prop.name = EditorGUILayout.TextField(prop.name, GUILayout.Width(nameWidth));
			GUILayout.Space(gap);

			int typeIndex = System.Array.IndexOf(allowedTypes, prop.type);
			if (typeIndex < 0)
			{
				typeIndex = 0;
				prop.type = allowedTypes[0];
			}

			typeIndex = EditorGUILayout.Popup(typeIndex, allowedTypes, GUILayout.Width(typeWidth));
			prop.type = allowedTypes[typeIndex];
			GUILayout.Space(gap);
			DrawInlinePropertyValue(prop, valueWidth);

			GUILayout.Space(6f);
			if (GUILayout.Button("X", GUILayout.Width(24f)))
			{
				moduleProperties.RemoveAt(i);
				i--;
				EditorGUILayout.EndHorizontal();
				continue;
			}

			EditorGUILayout.EndHorizontal();
		}
		EditorGUILayout.EndScrollView();
		EditorGUILayout.EndVertical();
	}

	private void DrawUnityPackageList()
	{
		GUILayout.Label("Packages", EditorStyles.boldLabel);
		if (GUILayout.Button("Add Package"))
		{
			unityPackages.Add(new PackageDefinition
			{
				name = string.Empty,
				fileName = string.Empty,
				assetFolder = string.Empty
			});
			packageFoldouts[unityPackages.Count - 1] = false;
		}

		for (int i = 0; i < unityPackages.Count; i++)
		{
			PackageDefinition package = unityPackages[i];

			if (!packageFoldouts.ContainsKey(i))
			{
				packageFoldouts[i] = false;
			}

			string packageLabel = !string.IsNullOrWhiteSpace(package.name)
				? package.name
				: !string.IsNullOrWhiteSpace(package.fileName)
					? package.fileName
					: $"Package {i + 1}";

			EditorGUILayout.BeginVertical("helpbox");
			EditorGUILayout.BeginHorizontal();
			packageFoldouts[i] = EditorGUILayout.Foldout(packageFoldouts[i], packageLabel, true);
			GUILayout.FlexibleSpace();
			if (GUILayout.Button("Remove", GUILayout.Width(80f)))
			{
				unityPackages.RemoveAt(i);
				packageFoldouts.Remove(i);
				var shiftedStates = new Dictionary<int, bool>();
				foreach (var entry in packageFoldouts)
				{
					shiftedStates[entry.Key > i ? entry.Key - 1 : entry.Key] = entry.Value;
				}
				packageFoldouts = shiftedStates;
				i--;
				EditorGUILayout.EndHorizontal();
				EditorGUILayout.EndVertical();
				continue;
			}
			EditorGUILayout.EndHorizontal();

			if (!packageFoldouts[i])
			{
				EditorGUILayout.EndVertical();
				continue;
			}

			package.name = EditorGUILayout.TextField("Name", package.name);
			package.fileName = EditorGUILayout.TextField("File Name", package.fileName);
			package.assetFolder = EditorGUILayout.TextField("Asset Folder", package.assetFolder);

			if (!string.IsNullOrWhiteSpace(package.assetFolder) && !IsDirectAssetsChildFolder(package.assetFolder))
			{
				EditorGUILayout.HelpBox("Asset Folder should match a folder directly under Assets, for example entering MyPackage for Assets/MyPackage.", MessageType.Warning);
			}

			EditorGUILayout.EndVertical();
		}
	}

	private void DrawFilesToRemoveSection()
	{
		GUILayout.Label("Files To Remove", EditorStyles.boldLabel);
		filesToRemove ??= new List<string>();

		if (GUILayout.Button("Add File To Remove"))
		{
			filesToRemove.Add(string.Empty);
		}

		if (filesToRemove.Count == 0)
		{
			EditorGUILayout.HelpBox("Add project-relative file paths that should be deleted after package install.", MessageType.Info);
		}

		for (int i = 0; i < filesToRemove.Count; i++)
		{
			EditorGUILayout.BeginHorizontal();
			filesToRemove[i] = EditorGUILayout.TextField(filesToRemove[i]);
			if (GUILayout.Button("Remove", GUILayout.Width(80f)))
			{
				filesToRemove.RemoveAt(i);
				i--;
			}
			EditorGUILayout.EndHorizontal();
		}
	}

	private void DrawDependencyList()
	{
		GUILayout.Label("Dependencies", EditorStyles.boldLabel);
		if (GUILayout.Button("Add Dependency"))
		{
			dependencies.Add(string.Empty);
		}

		for (int i = 0; i < dependencies.Count; i++)
		{
			EditorGUILayout.BeginHorizontal();
			dependencies[i] = EditorGUILayout.TextField(dependencies[i]);
			if (GUILayout.Button("Remove", GUILayout.Width(80f)))
			{
				dependencies.RemoveAt(i);
				i--;
			}
			EditorGUILayout.EndHorizontal();
		}
	}

	private void DrawCustomEditorList()
	{
		GUILayout.Label("Custom Editors", EditorStyles.boldLabel);
		if (GUILayout.Button("Add Custom Editor"))
		{
			if (string.IsNullOrEmpty(moduleName))
			{
				EditorUtility.DisplayDialog("Warning", "Please set the module name before adding a custom editor.", "OK");
			}
			else
			{
				string editorOriginalPath = EditorUtility.OpenFilePanel("Select Custom Editor", "", "zip");
				if (!string.IsNullOrEmpty(editorOriginalPath))
				{
					string destPath = GetUnityPath(editorOriginalPath);
					if (string.IsNullOrEmpty(destPath))
					{
						EditorUtility.DisplayDialog("Error", "Unity packages must be inside the asset folders.", "OK");
					}
					else
					{
						customEditors.Add(destPath);
					}
				}
			}
		}

		for (int i = 0; i < customEditors.Count; i++)
		{
			EditorGUILayout.BeginHorizontal();
			GUILayout.Label(Path.GetFileName(customEditors[i]));
			if (GUILayout.Button("Remove", GUILayout.Width(80f)))
			{
				customEditors.RemoveAt(i);
				i--;
			}
			EditorGUILayout.EndHorizontal();
		}
	}

	private void DrawItemsTab()
	{
		GUILayout.Label("ITEM GROUPS", EditorStyles.boldLabel);
		if (string.IsNullOrEmpty(moduleName))
		{
			EditorGUILayout.HelpBox("Set a Module Name before adding Item Groups.", MessageType.Warning);
		}

		if (GUILayout.Button("Add Item Group") && !string.IsNullOrEmpty(moduleName))
		{
			itemGroups.Add(new ItemGroup
			{
				name = $"New Group {itemGroups.Count + 1}",
				isExpanded = true
			});
			activeItemGroupIndex = itemGroups.Count - 1;
		}

		ValidateActiveItemGroup();
		EditorGUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));
		DrawItemGroupBrowser();
		ValidateSelectedItem();
		DrawSelectedItemEditor();
		EditorGUILayout.EndHorizontal();
	}

	private void DrawItemGroupBrowser()
	{
		EditorGUILayout.BeginVertical(GUILayout.Width(position.width * 0.6f));
		if (itemGroups.Count == 0)
		{
			EditorGUILayout.HelpBox("Add an item group to start curating prefabs.", MessageType.Info);
			EditorGUILayout.EndVertical();
			return;
		}

		string[] groupTabs = itemGroups.Select(group => string.IsNullOrEmpty(group.name) ? "New Group" : group.name).ToArray();
		activeItemGroupIndex = GUILayout.Toolbar(activeItemGroupIndex, groupTabs);

		ItemGroup activeGroup = itemGroups[activeItemGroupIndex];
		EditorGUILayout.BeginVertical("box");
		EditorGUILayout.BeginHorizontal();
		GUILayout.Label("Group Details", EditorStyles.boldLabel);
		GUILayout.FlexibleSpace();
		if (GUILayout.Button("Remove Group", GUILayout.Width(110f)))
		{
			if (selectedItem != null && activeGroup.items.Contains(selectedItem))
			{
				selectedItem = null;
			}

			itemGroups.RemoveAt(activeItemGroupIndex);
			activeItemGroupIndex = Mathf.Clamp(activeItemGroupIndex, 0, Mathf.Max(0, itemGroups.Count - 1));
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.EndVertical();
			EditorGUILayout.EndVertical();
			return;
		}
		EditorGUILayout.EndHorizontal();

		activeGroup.name = EditorGUILayout.TextField("Name", activeGroup.name);
		activeGroup.icon = IconPickerUI.DrawIconField(activeGroup.icon, CopyCustomIcon);
		activeGroup.category = EditorGUILayout.TextField("Category", activeGroup.category);

		if (string.IsNullOrEmpty(activeGroup.name))
		{
			EditorGUILayout.HelpBox("Set a name before adding items to this group.", MessageType.Warning);
		}

		EditorGUILayout.BeginHorizontal();
		GUI.enabled = !string.IsNullOrEmpty(activeGroup.name);
		if (GUILayout.Button("Add Items from Folder"))
		{
			string folderPath = EditorUtility.OpenFolderPanel("Select Prefab Folder", "", "");
			if (!string.IsNullOrEmpty(folderPath))
			{
				AddItemsFromFolder(activeGroup, folderPath);
				UpdateAssets();
			}
		}

		if (GUILayout.Button("Add Selected Prefabs"))
		{
			List<string> selectedAssets = new List<string>();
			AssetSelectorWindow.OpenWindow(selectedAssets);
			List<string> prefabPaths = selectedAssets
				.Where(path => AssetDatabase.GetMainAssetTypeAtPath(path) == typeof(GameObject))
				.ToList();
			if (prefabPaths.Count > 0)
			{
				AddItemsFromAssetPaths(activeGroup, prefabPaths);
				UpdateAssets();
			}
		}

		if (GUILayout.Button("Refresh Group Images"))
		{
			RecalculateGroupThumbnailsWithUnity(activeGroup);
		}

		if (GUILayout.Button("Create Custom Item"))
		{
			CreateCustomItem(activeGroup);
		}

		if (GUILayout.Button("Transform"))
		{
			GenericMenu transformMenu = new GenericMenu();
			transformMenu.AddItem(new GUIContent("Reset"), false, () => ResetTransformsForGroup(activeGroup));
			transformMenu.AddItem(new GUIContent("Project To Bottom"), false, () => ProjectGroupToBottomPivot(activeGroup));
			transformMenu.DropDown(GUILayoutUtility.GetLastRect());
		}
		GUI.enabled = true;
		EditorGUILayout.EndHorizontal();

		_itemGridScroll = EditorGUILayout.BeginScrollView(_itemGridScroll, GUILayout.ExpandHeight(true));
		DrawItemGrid(activeGroup);
		EditorGUILayout.EndScrollView();
		EditorGUILayout.EndVertical();

		EditorGUILayout.EndVertical();
	}

	private void DrawItemGrid(ItemGroup group)
	{
		int columns = Mathf.Max(1, Mathf.FloorToInt((position.width * 0.6f - 20f) / 70f));
		int count = 0;
		List<Item> itemsToRemove = new List<Item>();

		EditorGUILayout.BeginVertical();
		for (int j = 0; j < group.items.Count; j++)
		{
			if (count == 0)
			{
				EditorGUILayout.BeginHorizontal();
			}

			Item item = group.items[j];
			EditorGUILayout.BeginVertical(GUILayout.Width(70f));
			Texture2D thumbnail = GetItemThumbnail(item);

			if (GUILayout.Button(thumbnail ?? Texture2D.blackTexture, GUILayout.Width(64f), GUILayout.Height(64f)))
			{
				selectedItem = item;
			}

			if (GUILayout.Button("Remove", GUILayout.Width(64f)))
			{
				itemsToRemove.Add(item);
			}

			EditorGUILayout.EndVertical();
			count++;

			if (count >= columns || j == group.items.Count - 1)
			{
				EditorGUILayout.EndHorizontal();
				count = 0;
			}
		}

		foreach (Item item in itemsToRemove)
		{
			group.items.Remove(item);
		}

		EditorGUILayout.EndVertical();
	}

	private void ValidateSelectedItem()
	{
		if (selectedItem == null)
		{
			return;
		}

		foreach (ItemGroup group in itemGroups)
		{
			if (group.items.Contains(selectedItem))
			{
				return;
			}
		}

		selectedItem = null;
	}

	private void ValidateActiveItemGroup()
	{
		if (itemGroups.Count == 0)
		{
			activeItemGroupIndex = 0;
			return;
		}

		activeItemGroupIndex = Mathf.Clamp(activeItemGroupIndex, 0, itemGroups.Count - 1);
	}

	private void DrawSelectedItemEditor()
	{
		EditorGUILayout.BeginVertical("box", GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
		if (selectedItem == null)
		{
			GUILayout.Label("No item selected", EditorStyles.boldLabel);
			EditorGUILayout.HelpBox("Click on an item thumbnail from the left panel to view and edit its details here.", MessageType.Info);
			EditorGUILayout.EndVertical();
			return;
		}

		Texture2D thumb = GetEditorThumbnail(selectedItem);

		EditorGUILayout.BeginHorizontal();
		GUILayout.Label(thumb, GUILayout.Width(128f), GUILayout.Height(128f));

		EditorGUILayout.BeginVertical();
		selectedItem.exportTranslation = EditorGUILayout.Vector3Field("Translation", selectedItem.exportTranslation);
		selectedItem.exportRotation = EditorGUILayout.Vector3Field("Rotation", selectedItem.exportRotation);
		selectedItem.exportScale = EditorGUILayout.Vector3Field("Scale", selectedItem.exportScale);
		GUI.enabled = selectedItem.prefab != null;
		if (GUILayout.Button("Project Pivot To Bottom"))
		{
			SetPivotOffsetToBottom(selectedItem);
		}
		if (GUILayout.Button("Recalculate Image"))
		{
			RecalculateThumbnailWithUnity(selectedItem);
		}
		GUI.enabled = true;
		EditorGUILayout.EndVertical();

		EditorGUILayout.EndHorizontal();

		GUILayout.Label($"EDITING: {selectedItem.name}", EditorStyles.boldLabel);
		selectedItem.name = EditorGUILayout.TextField("Item Name", selectedItem.name);
		selectedItem.description = EditorGUILayout.TextField("Description", selectedItem.description);
		selectedItem.unique = EditorGUILayout.Toggle("Unique", selectedItem.unique);
		selectedItem.notDraggable = EditorGUILayout.Toggle("Not Visual", selectedItem.notDraggable);
		selectedItem.template = EditorGUILayout.Toggle("Is Template", selectedItem.template);

		GUILayout.Label("ASSETS", EditorStyles.boldLabel);
		selectedItem.prefabPath = EditorGUILayout.TextField("Prefab:", selectedItem.prefabPath);
		selectedItem.icon = IconPickerUI.DrawIconField(selectedItem.icon, CopyCustomIcon);

		GUILayout.Label("PROPERTIES", EditorStyles.boldLabel);
		DrawSelectedItemProperties();

		EditorGUILayout.Space();
		GUILayout.Label("CAPABILITIES", EditorStyles.boldLabel);
		EditorGUILayout.BeginVertical("helpbox");
		selectedItem.capabilities ??= selectedItem.prefab != null ? InferItemCapabilities(selectedItem) : new ItemCapabilitySet();
		EditorGUILayout.LabelField("Supported Features", selectedItem.capabilities.supportedFeatures != null ? selectedItem.capabilities.supportedFeatures.Count.ToString() : "0");
		EditorGUILayout.LabelField("Component Records", selectedItem.capabilities.unity != null && selectedItem.capabilities.unity.components != null ? selectedItem.capabilities.unity.components.Count.ToString() : "0");
		EditorGUILayout.LabelField("Constraints", selectedItem.capabilities.constraints != null ? selectedItem.capabilities.constraints.Count.ToString() : "0");
		EditorGUILayout.BeginHorizontal();
		GUI.enabled = selectedItem.prefab != null;
		if (GUILayout.Button("Infer Item Capabilities"))
		{
			selectedItem.capabilities = InferItemCapabilities(selectedItem);
		}
		GUI.enabled = true;
		if (GUILayout.Button("Open Capability Editor"))
		{
			FocusCapabilityEditorForSelectedItem();
		}
		EditorGUILayout.EndHorizontal();
		EditorGUILayout.EndVertical();

		EditorGUILayout.Space();
		EditorGUILayout.BeginHorizontal();
		if (GUILayout.Button("Add Property"))
		{
			selectedItem.properties ??= new List<Property>();
			selectedItem.properties.Add(new Property
			{
				name = "NewProperty",
				type = "string",
				data = string.Empty,
				value = string.Empty
			});
		}

		if (GUILayout.Button("Add Component Property"))
		{
			ComponentPropertiesPopup.ShowPopup(selectedItem);
		}
		EditorGUILayout.EndHorizontal();
		EditorGUILayout.EndVertical();
	}

	private void FocusCapabilityEditorForSelectedItem()
	{
		if (selectedItem == null)
		{
			return;
		}

		for (int groupIndex = 0; groupIndex < itemGroups.Count; groupIndex++)
		{
			ItemGroup group = itemGroups[groupIndex];
			if (group?.items == null)
			{
				continue;
			}

			int itemIndex = group.items.IndexOf(selectedItem);
			if (itemIndex >= 0)
			{
				capabilityItemGroupIndex = groupIndex;
				capabilityItemIndex = itemIndex;
				activeTab = ModuleEditorTab.Capabilities;
				GUI.FocusControl(null);
				Repaint();
				return;
			}
		}
	}

	private void DrawSelectedItemProperties()
	{
		List<(Property prop, string key)> unifiedProps = new List<(Property, string)>();
		if (selectedItem.properties != null)
		{
			foreach (Property property in selectedItem.properties)
			{
				unifiedProps.Add((property, property.name));
			}
		}

		foreach ((Property prop, string key) entry in unifiedProps)
		{
			if (!propertyFoldouts.ContainsKey(entry.key))
			{
				propertyFoldouts[entry.key] = true;
			}

			string header = $"{entry.prop.name} ({entry.prop.type})";
			EditorGUILayout.BeginHorizontal();
			propertyFoldouts[entry.key] = EditorGUILayout.Foldout(propertyFoldouts[entry.key], header, true);
			if (GUILayout.Button("Remove", GUILayout.Width(70f)))
			{
				Property prop = selectedItem.properties.FirstOrDefault(p => p.name == entry.key);
				if (prop != null)
				{
					selectedItem.properties.Remove(prop);
				}
			}
			EditorGUILayout.EndHorizontal();

			if (!propertyFoldouts[entry.key])
			{
				continue;
			}

			EditorGUILayout.BeginVertical("box");
			entry.prop.name = EditorGUILayout.TextField("Name", entry.prop.name);

			int typeIndex = System.Array.IndexOf(allowedTypes, entry.prop.type);
			if (typeIndex < 0)
			{
				typeIndex = 0;
				entry.prop.type = allowedTypes[0];
			}

			typeIndex = EditorGUILayout.Popup("Type", typeIndex, allowedTypes);
			entry.prop.type = allowedTypes[typeIndex];
			DrawExpandedPropertyValue(entry.prop);
			EditorGUILayout.EndVertical();
		}
	}

	private void DrawExpandedPropertyValue(Property prop)
	{
		switch (prop.type)
		{
			case "object":
				prop.data = EditorGUILayout.TextField("Editor", prop.data);
				break;
			case "gameitem":
				if (GUILayout.Button(string.IsNullOrEmpty(prop.data) ? "Edit..." : prop.data))
				{
					prop.data = GameItemPropertyEditor.OpenWindow(prop.data);
				}
				break;
			case "enum":
				if (GUILayout.Button(string.IsNullOrEmpty(prop.data) ? "Edit..." : prop.data))
				{
					prop.data = EnumPropertyEditor.OpenWindow(prop.data, CopyCustomIcon);
				}
				break;
			default:
				prop.value = EditorGUILayout.TextField("Value", prop.value);
				break;
		}
	}

	private void DrawInlinePropertyValue(Property prop, float width)
	{
		switch (prop.type)
		{
			case "object":
				prop.data = EditorGUILayout.TextField(prop.data, GUILayout.Width(width));
				break;
			case "gameitem":
				if (GUILayout.Button(string.IsNullOrEmpty(prop.data) ? "Edit..." : prop.data, GUILayout.Width(width)))
				{
					prop.data = GameItemPropertyEditor.OpenWindow(prop.data);
				}
				break;
			case "enum":
				if (GUILayout.Button(string.IsNullOrEmpty(prop.data) ? "Edit..." : prop.data, GUILayout.Width(width)))
				{
					prop.data = EnumPropertyEditor.OpenWindow(prop.data, CopyCustomIcon);
				}
				break;
			default:
				prop.value = EditorGUILayout.TextField(prop.value, GUILayout.Width(width));
				break;
		}
	}

	private Texture2D GetItemThumbnail(Item item)
	{
		if (!string.IsNullOrEmpty(item.icon))
		{
			string assetIconPath = Path.Combine(GetAssetModuleFolder(), item.icon);
			Texture2D thumbnail = LoadTextureFromFile(assetIconPath);
			if (thumbnail != null)
			{
				return thumbnail;
			}

			string moduleIconPath = Path.Combine(GetModuleFolder(), item.icon);
			thumbnail = LoadTextureFromFile(moduleIconPath);
			if (thumbnail != null)
			{
				return thumbnail;
			}
		}

		if (item.prefab != null)
		{
			Texture2D preview = AssetPreview.GetAssetPreview(item.prefab);
			if (preview != null)
			{
				return preview;
			}
		}

		return EditorGUIUtility.IconContent("DefaultAsset Icon").image as Texture2D;
	}

	private Texture2D GetEditorThumbnail(Item item)
	{
		if (item.prefab != null)
		{
			Texture2D preview = AssetPreview.GetAssetPreview(item.prefab);
			if (preview != null)
			{
				return preview;
			}
		}

		if (!string.IsNullOrEmpty(item.icon))
		{
			string fullIconPath = Path.Combine(GetModuleFolder(), item.icon);
			Texture2D loaded = LoadTextureFromFile(fullIconPath);
			if (loaded != null)
			{
				return loaded;
			}
		}

		return EditorGUIUtility.IconContent("DefaultAsset Icon").image as Texture2D;
	}

	private void DrawExportTab()
	{
		GUILayout.Label("EXPORT", EditorStyles.boldLabel);

		EditorGUILayout.BeginVertical("box");
		EditorGUILayout.LabelField("Module", string.IsNullOrWhiteSpace(moduleName) ? "Untitled Module" : moduleName);
		EditorGUILayout.LabelField("Type", moduleType);
		EditorGUILayout.LabelField("Groups", itemGroups.Count.ToString());
		EditorGUILayout.LabelField("Items", itemGroups.Sum(group => group.items.Count).ToString());
		EditorGUILayout.LabelField("Packages", unityPackages.Count.ToString());
		EditorGUILayout.LabelField("Dependencies", dependencies.Count.ToString());
		EditorGUILayout.LabelField("Custom Editors", customEditors.Count.ToString());
		EditorGUILayout.EndVertical();

		EditorGUILayout.Space();
		EditorGUILayout.HelpBox("Guessed tab grouping: Overview for metadata, Items for groups and item editing, Export for the final pass. We can rename or reshuffle these easily.", MessageType.Info);

		GUI.enabled = !string.IsNullOrWhiteSpace(moduleName);
		if (GUILayout.Button("EXPORT MODULE", GUILayout.Height(52f)))
		{
			ExportModule();
		}
		GUI.enabled = true;
	}

	private void DrawCapabilitiesTab()
	{
		moduleCapabilities ??= new CapabilityManifest();
		PopulateCapabilityModuleMetadata(moduleCapabilities);

		GUILayout.Label("CAPABILITIES", EditorStyles.boldLabel);
		EditorGUILayout.BeginVertical("box");
		EditorGUILayout.HelpBox("Capability data is persisted inside this module's existing module.bgm document. Use inference to populate from controller types, prefab components, Unity callbacks, and serialized fields, then adjust anything manually before save/export.", MessageType.Info);

		EditorGUILayout.BeginHorizontal();
		if (GUILayout.Button("Infer Module Capabilities", GUILayout.Width(180f)))
		{
			moduleCapabilities = InferModuleCapabilities();
			PopulateCapabilityModuleMetadata(moduleCapabilities);
			NormalizeModuleCapabilities(moduleCapabilities);
			selectedModuleFeatureIndex = 0;
		}

		if (GUILayout.Button("Infer All Item Capabilities", GUILayout.Width(180f)))
		{
			foreach (ItemGroup group in itemGroups)
			{
				foreach (Item item in group.items)
				{
					item.capabilities = item.prefab != null ? InferItemCapabilities(item) : new ItemCapabilitySet();
					NormalizeItemCapabilities(item.capabilities);
				}
			}
			selectedItemFeatureIndex = 0;
		}

		if (GUILayout.Button("Sync Metadata", GUILayout.Width(120f)))
		{
			PopulateCapabilityModuleMetadata(moduleCapabilities);
		}
		EditorGUILayout.EndHorizontal();
		EditorGUILayout.EndVertical();

		EditorGUILayout.Space(6f);
		DrawModuleCapabilityManifestEditor();
		EditorGUILayout.Space(10f);
		DrawItemCapabilityEditor();
	}

	private void DrawModuleCapabilityManifestEditor()
	{
		moduleCapabilities.module ??= new CapabilityModuleInfo();
		moduleCapabilities.unity ??= new CapabilityUnityInfo();
		moduleCapabilities.exportInfo ??= new CapabilityExportInfo();

		GUILayout.Label("MODULE-WIDE MANIFEST", EditorStyles.boldLabel);
		EditorGUILayout.BeginVertical("box");
		if (BeginCapabilitySection("manifest-header", "Header"))
		{
			moduleCapabilities.manifestVersion = EditorGUILayout.TextField("Manifest Version", moduleCapabilities.manifestVersion);
			moduleCapabilities.module.displayName = EditorGUILayout.TextField("Display Name", moduleCapabilities.module.displayName);
			moduleCapabilities.module.version = EditorGUILayout.TextField("Version", moduleCapabilities.module.version);
			moduleCapabilities.module.category = EditorGUILayout.TextField("Category", moduleCapabilities.module.category);
			moduleCapabilities.module.description = EditorGUILayout.TextField("Description", moduleCapabilities.module.description);
			moduleCapabilities.module.codegenEnabled = EditorGUILayout.Toggle("Codegen Enabled", moduleCapabilities.module.codegenEnabled);
			moduleCapabilities.module.availability.local = EditorGUILayout.Toggle("Available Local", moduleCapabilities.module.availability.local);
			moduleCapabilities.module.availability.cloud = EditorGUILayout.Toggle("Available Cloud", moduleCapabilities.module.availability.cloud);
			EndCapabilitySection();
		}

		if (BeginCapabilitySection("manifest-components", "Components"))
		{
			DrawComponentBrowser(moduleCapabilities.unity.components);
			EndCapabilitySection();
		}

		if (BeginCapabilitySection("manifest-assemblies", "Assembly Names"))
		{
			DrawStringListEditor("Assembly Names", moduleCapabilities.module.assemblyNames);
			EndCapabilitySection();
		}

		if (BeginCapabilitySection("manifest-namespaces", "Namespace Roots"))
		{
			DrawStringListEditor("Namespace Roots", moduleCapabilities.module.namespaceRoots);
			EndCapabilitySection();
		}

		if (BeginCapabilitySection("manifest-dependencies", "Dependencies"))
		{
			DrawStringListEditor("Dependencies", moduleCapabilities.module.dependencies);
			EndCapabilitySection();
		}

		if (BeginCapabilitySection("manifest-tags", "Tags"))
		{
			DrawStringListEditor("Tags", moduleCapabilities.module.tags);
			EndCapabilitySection();
		}

		if (BeginCapabilitySection("manifest-features", "Supported Features"))
		{
			DrawFeatureTileEditor("Supported Features", moduleCapabilities.supportedFeatures, ref selectedModuleFeatureIndex);
			EndCapabilitySection();
		}

		if (BeginCapabilitySection("manifest-constraints", "Constraints"))
		{
			DrawConstraintListEditor("Constraints", moduleCapabilities.constraints);
			EndCapabilitySection();
		}

		if (BeginCapabilitySection("manifest-events", "Events"))
		{
			DrawEventListEditor(moduleCapabilities.events);
			EndCapabilitySection();
		}

		if (BeginCapabilitySection("manifest-methods", "Methods"))
		{
			DrawMethodListEditor(moduleCapabilities.methods);
			EndCapabilitySection();
		}

		if (BeginCapabilitySection("manifest-parameters", "Parameters"))
		{
			DrawParameterListEditor(moduleCapabilities.parameters);
			EndCapabilitySection();
		}

		EditorGUILayout.LabelField("Producer", moduleCapabilities.exportInfo.producerName);
		EditorGUILayout.LabelField("Producer Version", moduleCapabilities.exportInfo.producerVersion);
		EditorGUILayout.LabelField("Last Exported At", string.IsNullOrWhiteSpace(moduleCapabilities.exportInfo.exportedAt) ? "Not exported yet" : moduleCapabilities.exportInfo.exportedAt);
		EditorGUILayout.EndVertical();
	}

	private void DrawComponentBrowser(List<UnityCapabilityComponentInfo> components)
	{
		components ??= new List<UnityCapabilityComponentInfo>();

		EditorGUILayout.BeginHorizontal();
		GUILayout.Label("Components", EditorStyles.miniBoldLabel);
		GUILayout.FlexibleSpace();
		if (GUILayout.Button("Add Component", GUILayout.Width(120f)))
		{
			components.Add(new UnityCapabilityComponentInfo());
			selectedComponentIndex = components.Count - 1;
			selectedComponentArtifactIndex = -1;
		}
		EditorGUILayout.EndHorizontal();

		if (components.Count == 0)
		{
			EditorGUILayout.LabelField("No component records", EditorStyles.miniLabel);
			return;
		}

		selectedComponentIndex = Mathf.Clamp(selectedComponentIndex < 0 ? 0 : selectedComponentIndex, 0, components.Count - 1);
		UnityCapabilityComponentInfo selectedComponent = components[selectedComponentIndex];
		selectedComponent.methods ??= new List<CapabilityMethodInfo>();
		selectedComponent.events ??= new List<CapabilityEventInfo>();
		selectedComponent.parameters ??= new List<CapabilityParameterInfo>();
		selectedComponent.requiredComponents ??= new List<string>();
		selectedComponent.optionalComponents ??= new List<string>();
		selectedComponent.allowedFeatures ??= new List<string>();
		selectedComponent.tags ??= new List<string>();

		List<ComponentArtifactEntry> artifacts = BuildComponentArtifacts(selectedComponent);
		if (artifacts.Count == 0)
		{
			selectedComponentArtifactIndex = -1;
		}
		else
		{
			selectedComponentArtifactIndex = Mathf.Clamp(selectedComponentArtifactIndex < 0 ? 0 : selectedComponentArtifactIndex, 0, artifacts.Count - 1);
		}

		EditorGUILayout.BeginHorizontal();

		EditorGUILayout.BeginVertical("box", GUILayout.Width(Mathf.Max(220f, position.width * 0.22f)));
		GUILayout.Label("Component List", EditorStyles.boldLabel);
		DrawComponentNamespaceTree(components);
		EditorGUILayout.EndVertical();

		EditorGUILayout.BeginVertical("box", GUILayout.Width(Mathf.Max(220f, position.width * 0.22f)));
		GUILayout.Label("Artifacts", EditorStyles.boldLabel);
		EditorGUILayout.BeginHorizontal();
		if (GUILayout.Button("Add Method", GUILayout.Width(90f)))
		{
			selectedComponent.methods.Add(new CapabilityMethodInfo
			{
				declaringType = selectedComponent.typeName
			});
			artifacts = BuildComponentArtifacts(selectedComponent);
			selectedComponentArtifactIndex = artifacts.FindIndex(artifact => artifact.kind == ComponentArtifactKind.Method && artifact.method == selectedComponent.methods.Last());
		}
		if (GUILayout.Button("Add Event", GUILayout.Width(90f)))
		{
			selectedComponent.events.Add(new CapabilityEventInfo
			{
				declaringType = selectedComponent.typeName
			});
			artifacts = BuildComponentArtifacts(selectedComponent);
			selectedComponentArtifactIndex = artifacts.FindIndex(artifact => artifact.kind == ComponentArtifactKind.Event && artifact.eventInfo == selectedComponent.events.Last());
		}
		if (GUILayout.Button("Add Field", GUILayout.Width(90f)))
		{
			selectedComponent.parameters.Add(new CapabilityParameterInfo());
			artifacts = BuildComponentArtifacts(selectedComponent);
			selectedComponentArtifactIndex = artifacts.FindIndex(artifact => artifact.kind == ComponentArtifactKind.Parameter && artifact.parameter == selectedComponent.parameters.Last());
		}
		EditorGUILayout.EndHorizontal();

		if (artifacts.Count == 0)
		{
			EditorGUILayout.LabelField("No artifacts", EditorStyles.miniLabel);
		}
		else
		{
			for (int i = 0; i < artifacts.Count; i++)
			{
				ComponentArtifactEntry artifact = artifacts[i];
				string label = artifact.label;
				if (GUILayout.Button(label, selectedComponentArtifactIndex == i ? EditorStyles.toolbarButton : GUI.skin.button, GUILayout.Height(26f)))
				{
					selectedComponentArtifactIndex = i;
				}
			}
		}
		EditorGUILayout.EndVertical();

		EditorGUILayout.BeginVertical("box", GUILayout.ExpandWidth(true));
		if (selectedComponentArtifactIndex >= 0 && selectedComponentArtifactIndex < artifacts.Count)
		{
			DrawSelectedComponentArtifactEditor(selectedComponent, artifacts[selectedComponentArtifactIndex]);
		}
		else
		{
			DrawSelectedComponentEditor(components, selectedComponent);
		}
		EditorGUILayout.EndVertical();

		EditorGUILayout.EndHorizontal();
	}

	private void DrawSelectedComponentEditor(List<UnityCapabilityComponentInfo> components, UnityCapabilityComponentInfo component)
	{
		GUILayout.Label("Component Properties", EditorStyles.boldLabel);
		component.componentId = EditorGUILayout.TextField("Component Id", component.componentId);
		component.typeName = EditorGUILayout.TextField("Type Name", component.typeName);
		component.baseType = EditorGUILayout.TextField("Base Type", component.baseType);
		component.attachTarget = EditorGUILayout.TextField("Attach Target", component.attachTarget);
		component.description = EditorGUILayout.TextField("Description", component.description);
		component.codegenAllowed = EditorGUILayout.Toggle("Codegen Allowed", component.codegenAllowed);
		DrawStringListEditor("Required Components", component.requiredComponents);
		DrawStringListEditor("Optional Components", component.optionalComponents);
		DrawStringListEditor("Allowed Features", component.allowedFeatures);
		DrawStringListEditor("Tags", component.tags);
		if (GUILayout.Button("Remove Component", GUILayout.Width(140f)))
		{
			components.RemoveAt(selectedComponentIndex);
			selectedComponentIndex = Mathf.Clamp(selectedComponentIndex - 1, 0, Mathf.Max(0, components.Count - 1));
			selectedComponentArtifactIndex = -1;
		}
	}

	private void DrawComponentNamespaceTree(List<UnityCapabilityComponentInfo> components)
	{
		List<ComponentNamespaceNode> roots = BuildComponentNamespaceTreeNodes(components);
		if (roots.Count == 0)
		{
			EditorGUILayout.LabelField("No components", EditorStyles.miniLabel);
			return;
		}

		foreach (ComponentNamespaceNode root in roots)
		{
			DrawComponentNamespaceNode(root, components, 0);
		}
	}

	private void DrawComponentNamespaceNode(ComponentNamespaceNode node, List<UnityCapabilityComponentInfo> components, int depth)
	{
		if (node == null)
		{
			return;
		}

		if (!string.IsNullOrWhiteSpace(node.fullPath))
		{
			if (!componentNamespaceFoldouts.ContainsKey(node.fullPath))
			{
				componentNamespaceFoldouts[node.fullPath] = true;
			}

			EditorGUILayout.BeginHorizontal();
			GUILayout.Space(depth * 14f);
			componentNamespaceFoldouts[node.fullPath] = EditorGUILayout.Foldout(componentNamespaceFoldouts[node.fullPath], node.label, true);
			EditorGUILayout.EndHorizontal();

			if (!componentNamespaceFoldouts[node.fullPath])
			{
				return;
			}
		}

		foreach (ComponentNamespaceNode child in node.children.OrderBy(child => child.label))
		{
			DrawComponentNamespaceNode(child, components, depth + (string.IsNullOrWhiteSpace(node.fullPath) ? 0 : 1));
		}

		foreach (int componentIndex in node.componentIndexes)
		{
			if (componentIndex < 0 || componentIndex >= components.Count)
			{
				continue;
			}

			UnityCapabilityComponentInfo entry = components[componentIndex];
			string label = string.IsNullOrWhiteSpace(entry.typeName)
				? (string.IsNullOrWhiteSpace(entry.componentId) ? "New Component" : entry.componentId)
				: GetLeafTypeName(entry.typeName);
			EditorGUILayout.BeginHorizontal();
			GUILayout.Space((depth + (string.IsNullOrWhiteSpace(node.fullPath) ? 0 : 1)) * 14f);
			if (GUILayout.Button(label, selectedComponentIndex == componentIndex ? EditorStyles.toolbarButton : GUI.skin.button, GUILayout.Height(26f)))
			{
				selectedComponentIndex = componentIndex;
				selectedComponentArtifactIndex = -1;
			}
			EditorGUILayout.EndHorizontal();
		}
	}

	private List<ComponentNamespaceNode> BuildComponentNamespaceTreeNodes(List<UnityCapabilityComponentInfo> components)
	{
		ComponentNamespaceNode root = new ComponentNamespaceNode
		{
			label = "",
			fullPath = ""
		};

		for (int i = 0; i < components.Count; i++)
		{
			UnityCapabilityComponentInfo component = components[i];
			string typeName = !string.IsNullOrWhiteSpace(component.typeName) ? component.typeName : component.componentId;
			string[] namespaceParts = GetNamespaceParts(typeName);
			ComponentNamespaceNode current = root;

			for (int partIndex = 0; partIndex < namespaceParts.Length; partIndex++)
			{
				string part = namespaceParts[partIndex];
				string path = string.Join(".", namespaceParts.Take(partIndex + 1).ToArray());
				ComponentNamespaceNode child = current.children.FirstOrDefault(existing => existing.fullPath == path);
				if (child == null)
				{
					child = new ComponentNamespaceNode
					{
						label = part,
						fullPath = path
					};
					current.children.Add(child);
				}

				current = child;
			}

			current.componentIndexes.Add(i);
		}

		return root.children;
	}

	private string[] GetNamespaceParts(string typeName)
	{
		if (string.IsNullOrWhiteSpace(typeName) || !typeName.Contains("."))
		{
			return Array.Empty<string>();
		}

		string[] parts = typeName.Split('.');
		if (parts.Length <= 1)
		{
			return Array.Empty<string>();
		}

		return parts.Take(parts.Length - 1).ToArray();
	}

	private string GetLeafTypeName(string typeName)
	{
		if (string.IsNullOrWhiteSpace(typeName))
		{
			return "New Component";
		}

		int lastDot = typeName.LastIndexOf('.');
		return lastDot >= 0 && lastDot < typeName.Length - 1
			? typeName.Substring(lastDot + 1)
			: typeName;
	}

	private void DrawSelectedComponentArtifactEditor(UnityCapabilityComponentInfo component, ComponentArtifactEntry artifact)
	{
		switch (artifact.kind)
		{
			case ComponentArtifactKind.Method:
				DrawMethodArtifactEditor(component, artifact.method);
				break;
			case ComponentArtifactKind.Event:
				DrawEventArtifactEditor(component, artifact.eventInfo);
				break;
			case ComponentArtifactKind.Parameter:
				DrawParameterArtifactEditor(component, artifact.parameter);
				break;
		}
	}

	private void DrawMethodArtifactEditor(UnityCapabilityComponentInfo component, CapabilityMethodInfo method)
	{
		GUILayout.Label("Method Properties", EditorStyles.boldLabel);
		method.name = EditorGUILayout.TextField("Name", method.name);
		method.declaringType = EditorGUILayout.TextField("Declaring Type", method.declaringType);
		method.description = EditorGUILayout.TextField("Description", method.description);
		method.returnType = EditorGUILayout.TextField("Return Type", method.returnType);
		method.isStatic = EditorGUILayout.Toggle("Is Static", method.isStatic);
		method.allowedForCodegen = EditorGUILayout.Toggle("Allowed For Codegen", method.allowedForCodegen);
		DrawMethodParameterListEditor(method.parameters);
		DrawStringListEditor("Constraints", method.constraints);
		DrawStringListEditor("Tags", method.tags);
		if (GUILayout.Button("Remove Method", GUILayout.Width(130f)))
		{
			component.methods.Remove(method);
			selectedComponentArtifactIndex = -1;
		}
	}

	private void DrawEventArtifactEditor(UnityCapabilityComponentInfo component, CapabilityEventInfo eventInfo)
	{
		GUILayout.Label("Event Properties", EditorStyles.boldLabel);
		eventInfo.name = EditorGUILayout.TextField("Name", eventInfo.name);
		eventInfo.direction = EditorGUILayout.TextField("Direction", eventInfo.direction);
		eventInfo.payloadType = EditorGUILayout.TextField("Payload Type", eventInfo.payloadType);
		eventInfo.declaringType = EditorGUILayout.TextField("Declaring Type", eventInfo.declaringType);
		eventInfo.description = EditorGUILayout.TextField("Description", eventInfo.description);
		eventInfo.allowedForCodegen = EditorGUILayout.Toggle("Allowed For Codegen", eventInfo.allowedForCodegen);
		eventInfo.scope = EditorGUILayout.TextField("Scope", eventInfo.scope);
		eventInfo.authority = EditorGUILayout.TextField("Authority", eventInfo.authority);
		DrawStringListEditor("Tags", eventInfo.tags);
		if (GUILayout.Button("Remove Event", GUILayout.Width(120f)))
		{
			component.events.Remove(eventInfo);
			selectedComponentArtifactIndex = -1;
		}
	}

	private void DrawParameterArtifactEditor(UnityCapabilityComponentInfo component, CapabilityParameterInfo parameter)
	{
		GUILayout.Label("Field Properties", EditorStyles.boldLabel);
		parameter.name = EditorGUILayout.TextField("Name", parameter.name);
		parameter.type = EditorGUILayout.TextField("Type", parameter.type);
		parameter.required = EditorGUILayout.Toggle("Required", parameter.required);
		parameter.@default = EditorGUILayout.TextField("Default", parameter.@default);
		parameter.min = EditorGUILayout.FloatField("Min", parameter.min);
		parameter.max = EditorGUILayout.FloatField("Max", parameter.max);
		DrawStringListEditor("Enum Values", parameter.enumValues);
		parameter.description = EditorGUILayout.TextField("Description", parameter.description);
		parameter.moduleScoped = EditorGUILayout.Toggle("Module Scoped", parameter.moduleScoped);
		parameter.featureId = EditorGUILayout.TextField("Feature Id", parameter.featureId);
		DrawStringListEditor("Tags", parameter.tags);
		if (GUILayout.Button("Remove Field", GUILayout.Width(120f)))
		{
			component.parameters.Remove(parameter);
			selectedComponentArtifactIndex = -1;
		}
	}

	private List<ComponentArtifactEntry> BuildComponentArtifacts(UnityCapabilityComponentInfo component)
	{
		List<ComponentArtifactEntry> artifacts = new List<ComponentArtifactEntry>();
		if (component == null)
		{
			return artifacts;
		}

		foreach (CapabilityMethodInfo method in component.methods ?? new List<CapabilityMethodInfo>())
		{
			artifacts.Add(new ComponentArtifactEntry
			{
				kind = ComponentArtifactKind.Method,
				method = method,
				label = "[Method] " + (string.IsNullOrWhiteSpace(method.name) ? "New Method" : method.name)
			});
		}

		foreach (CapabilityEventInfo eventInfo in component.events ?? new List<CapabilityEventInfo>())
		{
			artifacts.Add(new ComponentArtifactEntry
			{
				kind = ComponentArtifactKind.Event,
				eventInfo = eventInfo,
				label = "[Event] " + (string.IsNullOrWhiteSpace(eventInfo.name) ? "New Event" : eventInfo.name)
			});
		}

		foreach (CapabilityParameterInfo parameter in component.parameters ?? new List<CapabilityParameterInfo>())
		{
			artifacts.Add(new ComponentArtifactEntry
			{
				kind = ComponentArtifactKind.Parameter,
				parameter = parameter,
				label = "[Field] " + (string.IsNullOrWhiteSpace(parameter.name) ? "New Field" : parameter.name)
			});
		}

		return artifacts;
	}

	private enum ComponentArtifactKind
	{
		Method,
		Event,
		Parameter
	}

	private class ComponentArtifactEntry
	{
		public ComponentArtifactKind kind;
		public string label;
		public CapabilityMethodInfo method;
		public CapabilityEventInfo eventInfo;
		public CapabilityParameterInfo parameter;
	}

	private class ComponentNamespaceNode
	{
		public string label;
		public string fullPath;
		public List<ComponentNamespaceNode> children = new List<ComponentNamespaceNode>();
		public List<int> componentIndexes = new List<int>();
	}

	private void DrawItemCapabilityEditor()
	{
		GUILayout.Label("ITEM / PREFAB CAPABILITIES", EditorStyles.boldLabel);
		if (itemGroups.Count == 0)
		{
			EditorGUILayout.HelpBox("Add item groups and prefabs in the Items tab to author item-level capabilities.", MessageType.Info);
			return;
		}

		capabilityItemGroupIndex = Mathf.Clamp(capabilityItemGroupIndex, 0, itemGroups.Count - 1);
		ItemGroup group = itemGroups[capabilityItemGroupIndex];
		string[] groupNames = itemGroups.Select(itemGroup => string.IsNullOrWhiteSpace(itemGroup.name) ? "New Group" : itemGroup.name).ToArray();
		capabilityItemGroupIndex = EditorGUILayout.Popup("Group", capabilityItemGroupIndex, groupNames);
		group = itemGroups[capabilityItemGroupIndex];

		if (group.items == null || group.items.Count == 0)
		{
			EditorGUILayout.HelpBox("The selected group has no items yet.", MessageType.Info);
			return;
		}

		capabilityItemIndex = Mathf.Clamp(capabilityItemIndex, 0, group.items.Count - 1);
		string[] itemNames = group.items.Select(item => string.IsNullOrWhiteSpace(item.name) ? "Unnamed Item" : item.name).ToArray();
		capabilityItemIndex = EditorGUILayout.Popup("Item", capabilityItemIndex, itemNames);
		Item itemToEdit = group.items[capabilityItemIndex];
		selectedItem = itemToEdit;
		itemToEdit.capabilities ??= itemToEdit.prefab != null ? InferItemCapabilities(itemToEdit) : new ItemCapabilitySet();
		NormalizeItemCapabilities(itemToEdit.capabilities);

		EditorGUILayout.BeginVertical("box");
		EditorGUILayout.LabelField("Prefab", EditorStyles.miniBoldLabel);
		EditorGUILayout.LabelField(string.IsNullOrWhiteSpace(itemToEdit.prefabPath) ? "Custom / none" : itemToEdit.prefabPath, EditorStyles.wordWrappedLabel);
		EditorGUILayout.BeginHorizontal();
		GUI.enabled = itemToEdit.prefab != null;
		if (GUILayout.Button("Infer Selected Item", GUILayout.Width(150f)))
		{
			itemToEdit.capabilities = InferItemCapabilities(itemToEdit);
			NormalizeItemCapabilities(itemToEdit.capabilities);
			selectedItemFeatureIndex = 0;
		}
		GUI.enabled = true;
		if (GUILayout.Button("Clear Item Capabilities", GUILayout.Width(150f)))
		{
			itemToEdit.capabilities = new ItemCapabilitySet();
			selectedItemFeatureIndex = 0;
		}
		EditorGUILayout.EndHorizontal();
		if (BeginCapabilitySection("item-features", "Supported Features"))
		{
			DrawFeatureTileEditor("Supported Features", itemToEdit.capabilities.supportedFeatures, ref selectedItemFeatureIndex);
			EndCapabilitySection();
		}
		if (BeginCapabilitySection("item-constraints", "Constraints"))
		{
			DrawConstraintListEditor("Constraints", itemToEdit.capabilities.constraints);
			EndCapabilitySection();
		}
		EditorGUILayout.EndVertical();
	}

	private bool BeginCapabilitySection(string key, string label)
	{
		if (!capabilitySectionFoldouts.ContainsKey(key))
		{
			capabilitySectionFoldouts[key] = false;
		}

		EditorGUILayout.BeginVertical("helpbox");
		capabilitySectionFoldouts[key] = EditorGUILayout.Foldout(capabilitySectionFoldouts[key], label, true);
		if (!capabilitySectionFoldouts[key])
		{
			EditorGUILayout.EndVertical();
			return false;
		}

		return true;
	}

	private void EndCapabilitySection()
	{
		EditorGUILayout.EndVertical();
	}

	private void DrawFeatureTileEditor(string label, List<CapabilityFeatureInfo> values, ref int selectedIndex)
	{
		values ??= new List<CapabilityFeatureInfo>();
		EditorGUILayout.BeginVertical("helpbox");
		EditorGUILayout.BeginHorizontal();
		GUILayout.Label(label, EditorStyles.miniBoldLabel);
		GUILayout.FlexibleSpace();
		if (GUILayout.Button("Add", GUILayout.Width(60f)))
		{
			values.Add(new CapabilityFeatureInfo());
			selectedIndex = values.Count - 1;
		}
		EditorGUILayout.EndHorizontal();

		if (values.Count == 0)
		{
			EditorGUILayout.LabelField("No entries", EditorStyles.miniLabel);
			EditorGUILayout.EndVertical();
			return;
		}

		selectedIndex = Mathf.Clamp(selectedIndex, 0, values.Count - 1);
		EditorGUILayout.BeginHorizontal();
		EditorGUILayout.BeginVertical(GUILayout.Width(Mathf.Max(180f, position.width * 0.28f)));
		for (int i = 0; i < values.Count; i++)
		{
			CapabilityFeatureInfo entry = values[i];
			string tileLabel = string.IsNullOrWhiteSpace(entry.featureId) ? "New Feature" : entry.featureId;
			if (GUILayout.Button(tileLabel, selectedIndex == i ? EditorStyles.toolbarButton : GUI.skin.button, GUILayout.Height(32f)))
			{
				selectedIndex = i;
			}
		}
		EditorGUILayout.EndVertical();

		EditorGUILayout.BeginVertical("box", GUILayout.ExpandWidth(true));
		CapabilityFeatureInfo selectedFeature = values[selectedIndex];
		selectedFeature.featureId = EditorGUILayout.TextField("Feature Id", selectedFeature.featureId);
		selectedFeature.description = EditorGUILayout.TextField("Description", selectedFeature.description);
		selectedFeature.codegenAllowed = EditorGUILayout.Toggle("Codegen Allowed", selectedFeature.codegenAllowed);
		DrawStringListEditor("Required Dependencies", selectedFeature.requiredDependencies);
		DrawStringListEditor("Incompatible Features", selectedFeature.incompatibleFeatures);
		DrawStringListEditor("Recommended Templates", selectedFeature.recommendedTemplates);
		if (GUILayout.Button("Remove Feature", GUILayout.Width(120f)))
		{
			values.RemoveAt(selectedIndex);
			selectedIndex = Mathf.Clamp(selectedIndex - 1, 0, Mathf.Max(0, values.Count - 1));
		}
		EditorGUILayout.EndVertical();
		EditorGUILayout.EndHorizontal();

		EditorGUILayout.EndVertical();
	}

	private void DrawConstraintListEditor(string label, List<CapabilityConstraintInfo> values)
	{
		values ??= new List<CapabilityConstraintInfo>();
		EditorGUILayout.BeginVertical("helpbox");
		EditorGUILayout.BeginHorizontal();
		GUILayout.Label(label, EditorStyles.miniBoldLabel);
		GUILayout.FlexibleSpace();
		if (GUILayout.Button("Add", GUILayout.Width(60f)))
		{
			values.Add(new CapabilityConstraintInfo());
		}
		EditorGUILayout.EndHorizontal();

		if (values.Count == 0)
		{
			EditorGUILayout.LabelField("No entries", EditorStyles.miniLabel);
		}

		for (int i = 0; i < values.Count; i++)
		{
			CapabilityConstraintInfo entry = values[i];
			EditorGUILayout.BeginVertical("box");
			entry.code = EditorGUILayout.TextField("Code", entry.code);
			entry.description = EditorGUILayout.TextField("Description", entry.description);
			entry.severity = EditorGUILayout.TextField("Severity", entry.severity);
			entry.appliesToType = EditorGUILayout.TextField("Applies To Type", entry.appliesToType);
			entry.appliesToId = EditorGUILayout.TextField("Applies To Id", entry.appliesToId);
			if (GUILayout.Button("Remove Constraint", GUILayout.Width(130f)))
			{
				values.RemoveAt(i);
				i--;
			}
			EditorGUILayout.EndVertical();
		}

		EditorGUILayout.EndVertical();
	}

	private void DrawStringListEditor(string label, List<string> values)
	{
		values ??= new List<string>();
		EditorGUILayout.BeginVertical("helpbox");
		EditorGUILayout.BeginHorizontal();
		GUILayout.Label(label, EditorStyles.miniBoldLabel);
		GUILayout.FlexibleSpace();
		if (GUILayout.Button("Add", GUILayout.Width(60f)))
		{
			values.Add(string.Empty);
		}
		EditorGUILayout.EndHorizontal();

		if (values.Count == 0)
		{
			EditorGUILayout.LabelField("No entries", EditorStyles.miniLabel);
		}

		for (int i = 0; i < values.Count; i++)
		{
			EditorGUILayout.BeginHorizontal();
			values[i] = EditorGUILayout.TextField(values[i]);
			if (GUILayout.Button("X", GUILayout.Width(24f)))
			{
				values.RemoveAt(i);
				i--;
			}
			EditorGUILayout.EndHorizontal();
		}

		EditorGUILayout.EndVertical();
	}

	private void DrawTypeTreeEditor(List<CapabilityTypeInfo> values)
	{
		values ??= new List<CapabilityTypeInfo>();
		EditorGUILayout.BeginHorizontal();
		GUILayout.Label("Types", EditorStyles.miniBoldLabel);
		GUILayout.FlexibleSpace();
		if (GUILayout.Button("Add Type", GUILayout.Width(90f)))
		{
			values.Add(new CapabilityTypeInfo());
			selectedTypeIndex = values.Count - 1;
			selectedTypeFieldIndex = -1;
		}
		EditorGUILayout.EndHorizontal();

		if (values.Count == 0)
		{
			EditorGUILayout.LabelField("No entries", EditorStyles.miniLabel);
			return;
		}

		selectedTypeIndex = Mathf.Clamp(selectedTypeIndex < 0 ? 0 : selectedTypeIndex, 0, values.Count - 1);
		CapabilityTypeInfo selectedType = values[selectedTypeIndex];
		selectedType.fields ??= new List<CapabilityTypeFieldInfo>();

		EditorGUILayout.BeginHorizontal();
		EditorGUILayout.BeginVertical(GUILayout.Width(Mathf.Max(220f, position.width * 0.28f)));
		for (int i = 0; i < values.Count; i++)
		{
			CapabilityTypeInfo typeEntry = values[i];
			string typeLabel = string.IsNullOrWhiteSpace(typeEntry.name) ? "New Type" : typeEntry.name;
			if (GUILayout.Button(typeLabel, selectedTypeIndex == i && selectedTypeFieldIndex < 0 ? EditorStyles.toolbarButton : GUI.skin.button, GUILayout.Height(30f)))
			{
				selectedTypeIndex = i;
				selectedTypeFieldIndex = -1;
			}

			if (selectedTypeIndex == i)
			{
				for (int fieldIndex = 0; fieldIndex < typeEntry.fields.Count; fieldIndex++)
				{
					CapabilityTypeFieldInfo fieldEntry = typeEntry.fields[fieldIndex];
					string fieldLabel = string.IsNullOrWhiteSpace(fieldEntry.name) ? "    New Field" : "    " + fieldEntry.name;
					if (GUILayout.Button(fieldLabel, selectedTypeFieldIndex == fieldIndex ? EditorStyles.miniButtonMid : EditorStyles.miniButton, GUILayout.Height(22f)))
					{
						selectedTypeIndex = i;
						selectedTypeFieldIndex = fieldIndex;
					}
				}
			}
		}
		EditorGUILayout.EndVertical();

		EditorGUILayout.BeginVertical("box", GUILayout.ExpandWidth(true));
		if (selectedTypeFieldIndex >= 0 && selectedTypeFieldIndex < selectedType.fields.Count)
		{
			CapabilityTypeFieldInfo selectedField = selectedType.fields[selectedTypeFieldIndex];
			GUILayout.Label("Field Editor", EditorStyles.boldLabel);
			selectedField.name = EditorGUILayout.TextField("Name", selectedField.name);
			selectedField.type = EditorGUILayout.TextField("Type", selectedField.type);
			selectedField.description = EditorGUILayout.TextField("Description", selectedField.description);
			selectedField.required = EditorGUILayout.Toggle("Required", selectedField.required);
			if (GUILayout.Button("Remove Field", GUILayout.Width(120f)))
			{
				selectedType.fields.RemoveAt(selectedTypeFieldIndex);
				selectedTypeFieldIndex = -1;
			}
		}
		else
		{
			GUILayout.Label("Type Editor", EditorStyles.boldLabel);
			selectedType.name = EditorGUILayout.TextField("Name", selectedType.name);
			selectedType.fullName = EditorGUILayout.TextField("Full Name", selectedType.fullName);
			selectedType.kind = EditorGUILayout.TextField("Kind", selectedType.kind);
			selectedType.@namespace = EditorGUILayout.TextField("Namespace", selectedType.@namespace);
			selectedType.description = EditorGUILayout.TextField("Description", selectedType.description);
			selectedType.exposed = EditorGUILayout.Toggle("Exposed", selectedType.exposed);
			DrawStringListEditor("Enum Values", selectedType.enumValues);

			EditorGUILayout.BeginHorizontal();
			if (GUILayout.Button("Add Field", GUILayout.Width(100f)))
			{
				selectedType.fields.Add(new CapabilityTypeFieldInfo());
				selectedTypeFieldIndex = selectedType.fields.Count - 1;
			}
			if (GUILayout.Button("Remove Type", GUILayout.Width(110f)))
			{
				values.RemoveAt(selectedTypeIndex);
				selectedTypeIndex = Mathf.Clamp(selectedTypeIndex - 1, 0, Mathf.Max(0, values.Count - 1));
				selectedTypeFieldIndex = -1;
				EditorGUILayout.EndHorizontal();
				EditorGUILayout.EndVertical();
				EditorGUILayout.EndHorizontal();
				return;
			}
			EditorGUILayout.EndHorizontal();
		}

		EditorGUILayout.EndVertical();
		EditorGUILayout.EndHorizontal();
	}

	private void DrawEventListEditor(List<CapabilityEventInfo> values)
	{
		values ??= new List<CapabilityEventInfo>();
		GUILayout.Label("Events", EditorStyles.miniBoldLabel);
		if (GUILayout.Button("Add Event", GUILayout.Width(90f)))
		{
			values.Add(new CapabilityEventInfo());
		}
		for (int i = 0; i < values.Count; i++)
		{
			CapabilityEventInfo entry = values[i];
			EditorGUILayout.BeginVertical("helpbox");
			entry.name = EditorGUILayout.TextField("Name", entry.name);
			entry.direction = EditorGUILayout.TextField("Direction", entry.direction);
			entry.payloadType = EditorGUILayout.TextField("Payload Type", entry.payloadType);
			entry.declaringType = EditorGUILayout.TextField("Declaring Type", entry.declaringType);
			entry.description = EditorGUILayout.TextField("Description", entry.description);
			entry.allowedForCodegen = EditorGUILayout.Toggle("Allowed For Codegen", entry.allowedForCodegen);
			entry.scope = EditorGUILayout.TextField("Scope", entry.scope);
			entry.authority = EditorGUILayout.TextField("Authority", entry.authority);
			DrawStringListEditor("Tags", entry.tags);
			if (GUILayout.Button("Remove Event", GUILayout.Width(110f)))
			{
				values.RemoveAt(i);
				i--;
			}
			EditorGUILayout.EndVertical();
		}
	}

	private void DrawMethodListEditor(List<CapabilityMethodInfo> values)
	{
		values ??= new List<CapabilityMethodInfo>();
		GUILayout.Label("Methods", EditorStyles.miniBoldLabel);
		if (GUILayout.Button("Add Method", GUILayout.Width(100f)))
		{
			values.Add(new CapabilityMethodInfo());
		}
		for (int i = 0; i < values.Count; i++)
		{
			CapabilityMethodInfo entry = values[i];
			EditorGUILayout.BeginVertical("helpbox");
			entry.name = EditorGUILayout.TextField("Name", entry.name);
			entry.declaringType = EditorGUILayout.TextField("Declaring Type", entry.declaringType);
			entry.description = EditorGUILayout.TextField("Description", entry.description);
			DrawMethodParameterListEditor(entry.parameters);
			entry.returnType = EditorGUILayout.TextField("Return Type", entry.returnType);
			entry.isStatic = EditorGUILayout.Toggle("Is Static", entry.isStatic);
			entry.allowedForCodegen = EditorGUILayout.Toggle("Allowed For Codegen", entry.allowedForCodegen);
			DrawStringListEditor("Constraints", entry.constraints);
			DrawStringListEditor("Tags", entry.tags);
			if (GUILayout.Button("Remove Method", GUILayout.Width(120f)))
			{
				values.RemoveAt(i);
				i--;
			}
			EditorGUILayout.EndVertical();
		}
	}

	private void DrawParameterListEditor(List<CapabilityParameterInfo> values)
	{
		values ??= new List<CapabilityParameterInfo>();
		GUILayout.Label("Parameters", EditorStyles.miniBoldLabel);
		if (GUILayout.Button("Add Parameter", GUILayout.Width(110f)))
		{
			values.Add(new CapabilityParameterInfo());
		}
		for (int i = 0; i < values.Count; i++)
		{
			CapabilityParameterInfo entry = values[i];
			EditorGUILayout.BeginVertical("helpbox");
			entry.name = EditorGUILayout.TextField("Name", entry.name);
			entry.type = EditorGUILayout.TextField("Type", entry.type);
			entry.required = EditorGUILayout.Toggle("Required", entry.required);
			entry.@default = EditorGUILayout.TextField("Default", entry.@default);
			entry.min = EditorGUILayout.FloatField("Min", entry.min);
			entry.max = EditorGUILayout.FloatField("Max", entry.max);
			DrawStringListEditor("Enum Values", entry.enumValues);
			entry.description = EditorGUILayout.TextField("Description", entry.description);
			entry.moduleScoped = EditorGUILayout.Toggle("Module Scoped", entry.moduleScoped);
			entry.featureId = EditorGUILayout.TextField("Feature Id", entry.featureId);
			DrawStringListEditor("Tags", entry.tags);
			if (GUILayout.Button("Remove Parameter", GUILayout.Width(130f)))
			{
				values.RemoveAt(i);
				i--;
			}
			EditorGUILayout.EndVertical();
		}
	}

	private void DrawMethodParameterListEditor(List<CapabilityMethodParameterInfo> values)
	{
		values ??= new List<CapabilityMethodParameterInfo>();
		GUILayout.Label("Parameters", EditorStyles.miniBoldLabel);
		if (GUILayout.Button("Add Method Parameter", GUILayout.Width(140f)))
		{
			values.Add(new CapabilityMethodParameterInfo());
		}
		for (int i = 0; i < values.Count; i++)
		{
			CapabilityMethodParameterInfo entry = values[i];
			EditorGUILayout.BeginVertical("helpbox");
			entry.name = EditorGUILayout.TextField("Name", entry.name);
			entry.type = EditorGUILayout.TextField("Type", entry.type);
			entry.description = EditorGUILayout.TextField("Description", entry.description);
			entry.required = EditorGUILayout.Toggle("Required", entry.required);
			if (GUILayout.Button("Remove Method Parameter", GUILayout.Width(170f)))
			{
				values.RemoveAt(i);
				i--;
			}
			EditorGUILayout.EndVertical();
		}
	}
}
