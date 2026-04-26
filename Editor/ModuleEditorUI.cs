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
		}

		if (GUILayout.Button("Infer All Item Capabilities", GUILayout.Width(180f)))
		{
			foreach (ItemGroup group in itemGroups)
			{
				foreach (Item item in group.items)
				{
					item.capabilities = item.prefab != null ? InferItemCapabilities(item) : new ItemCapabilitySet();
				}
			}
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
		moduleCapabilities.manifestVersion = EditorGUILayout.TextField("Manifest Version", moduleCapabilities.manifestVersion);
		moduleCapabilities.module.displayName = EditorGUILayout.TextField("Display Name", moduleCapabilities.module.displayName);
		moduleCapabilities.module.version = EditorGUILayout.TextField("Version", moduleCapabilities.module.version);
		moduleCapabilities.module.category = EditorGUILayout.TextField("Category", moduleCapabilities.module.category);
		moduleCapabilities.module.description = EditorGUILayout.TextField("Description", moduleCapabilities.module.description);
		moduleCapabilities.module.codegenEnabled = EditorGUILayout.Toggle("Codegen Enabled", moduleCapabilities.module.codegenEnabled);
		moduleCapabilities.module.availability.local = EditorGUILayout.Toggle("Available Local", moduleCapabilities.module.availability.local);
		moduleCapabilities.module.availability.cloud = EditorGUILayout.Toggle("Available Cloud", moduleCapabilities.module.availability.cloud);
		DrawStringListEditor("Assembly Names", moduleCapabilities.module.assemblyNames);
		DrawStringListEditor("Namespace Roots", moduleCapabilities.module.namespaceRoots);
		DrawStringListEditor("Dependencies", moduleCapabilities.module.dependencies);
		DrawStringListEditor("Tags", moduleCapabilities.module.tags);
		DrawStringListEditor("Supported Features", moduleCapabilities.supportedFeatures);
		DrawCapabilityUnityEditor("Unity", moduleCapabilities.unity);
		DrawStringListEditor("Constraints", moduleCapabilities.constraints);
		DrawTypeListEditor(moduleCapabilities.types);
		DrawEventListEditor(moduleCapabilities.events);
		DrawMethodListEditor(moduleCapabilities.methods);
		DrawParameterListEditor(moduleCapabilities.parameters);
		EditorGUILayout.LabelField("Producer", moduleCapabilities.exportInfo.producerName);
		EditorGUILayout.LabelField("Producer Version", moduleCapabilities.exportInfo.producerVersion);
		EditorGUILayout.LabelField("Last Exported At", string.IsNullOrWhiteSpace(moduleCapabilities.exportInfo.exportedAt) ? "Not exported yet" : moduleCapabilities.exportInfo.exportedAt);
		EditorGUILayout.EndVertical();
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

		EditorGUILayout.BeginVertical("box");
		EditorGUILayout.LabelField("Prefab", EditorStyles.miniBoldLabel);
		EditorGUILayout.LabelField(string.IsNullOrWhiteSpace(itemToEdit.prefabPath) ? "Custom / none" : itemToEdit.prefabPath, EditorStyles.wordWrappedLabel);
		EditorGUILayout.BeginHorizontal();
		GUI.enabled = itemToEdit.prefab != null;
		if (GUILayout.Button("Infer Selected Item", GUILayout.Width(150f)))
		{
			itemToEdit.capabilities = InferItemCapabilities(itemToEdit);
		}
		GUI.enabled = true;
		if (GUILayout.Button("Clear Item Capabilities", GUILayout.Width(150f)))
		{
			itemToEdit.capabilities = new ItemCapabilitySet();
		}
		EditorGUILayout.EndHorizontal();
		DrawStringListEditor("Supported Features", itemToEdit.capabilities.supportedFeatures);
		DrawCapabilityUnityEditor("Unity", itemToEdit.capabilities.unity);
		DrawStringListEditor("Constraints", itemToEdit.capabilities.constraints);
		EditorGUILayout.EndVertical();
	}

	private void DrawCapabilityUnityEditor(string label, CapabilityUnityInfo unity)
	{
		unity ??= new CapabilityUnityInfo();
		GUILayout.Label(label, EditorStyles.miniBoldLabel);
		EditorGUILayout.BeginVertical("helpbox");
		DrawStringListEditor("Components", unity.components);
		DrawStringListEditor("Systems", unity.systems);
		DrawStringListEditor("GameObject Roles", unity.gameObjectRoles);
		DrawStringListEditor("Behavior Shapes", unity.behaviorShapes);
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

	private void DrawTypeListEditor(List<CapabilityTypeInfo> values)
	{
		values ??= new List<CapabilityTypeInfo>();
		GUILayout.Label("Types", EditorStyles.miniBoldLabel);
		if (GUILayout.Button("Add Type", GUILayout.Width(90f)))
		{
			values.Add(new CapabilityTypeInfo());
		}
		for (int i = 0; i < values.Count; i++)
		{
			CapabilityTypeInfo entry = values[i];
			EditorGUILayout.BeginVertical("helpbox");
			entry.name = EditorGUILayout.TextField("Name", entry.name);
			entry.fullName = EditorGUILayout.TextField("Full Name", entry.fullName);
			entry.assemblyName = EditorGUILayout.TextField("Assembly", entry.assemblyName);
			entry.kind = EditorGUILayout.TextField("Kind", entry.kind);
			entry.description = EditorGUILayout.TextField("Description", entry.description);
			if (GUILayout.Button("Remove Type", GUILayout.Width(110f)))
			{
				values.RemoveAt(i);
				i--;
			}
			EditorGUILayout.EndVertical();
		}
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
			entry.declaringType = EditorGUILayout.TextField("Declaring Type", entry.declaringType);
			entry.eventType = EditorGUILayout.TextField("Event Type", entry.eventType);
			entry.source = EditorGUILayout.TextField("Source", entry.source);
			entry.description = EditorGUILayout.TextField("Description", entry.description);
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
			entry.returnType = EditorGUILayout.TextField("Return Type", entry.returnType);
			entry.signature = EditorGUILayout.TextField("Signature", entry.signature);
			entry.source = EditorGUILayout.TextField("Source", entry.source);
			entry.description = EditorGUILayout.TextField("Description", entry.description);
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
			entry.source = EditorGUILayout.TextField("Source", entry.source);
			entry.defaultValue = EditorGUILayout.TextField("Default Value", entry.defaultValue);
			entry.description = EditorGUILayout.TextField("Description", entry.description);
			entry.required = EditorGUILayout.Toggle("Required", entry.required);
			if (GUILayout.Button("Remove Parameter", GUILayout.Width(130f)))
			{
				values.RemoveAt(i);
				i--;
			}
			EditorGUILayout.EndVertical();
		}
	}
}
