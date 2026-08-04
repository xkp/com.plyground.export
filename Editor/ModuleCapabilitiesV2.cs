using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public partial class ModuleExporter
{
	private enum CapabilityWorkspaceTabV2
	{
		Components,
		Features
	}

	private enum CapabilityComponentInspectorTabV2
	{
		Component,
		Properties,
		Methods,
		Events
	}

	[Serializable]
	private class CapabilityComponentEntryV2
	{
		public string id = "";
		public string displayName = "";
		public string sourcePath = "";
		public bool isCustom;
		public string typeName = "";
		public string baseType = "";
		public string description = "";
		public string canAdd = "No";
		public List<CapabilityPropertyEntryV2> properties = new List<CapabilityPropertyEntryV2>();
		public List<CapabilityMethodEntryV2> methods = new List<CapabilityMethodEntryV2>();
		public List<CapabilityEventEntryV2> events = new List<CapabilityEventEntryV2>();
	}

	[Serializable]
	private class CapabilityPropertyEntryV2
	{
		public string name = "";
		public string displayName = "";
		public string type = "";
		public List<string> enumValues = new List<string>();
		public string description = "";
		public bool writable;
		public bool userEditable = true;
		public string defaultValue = "";
		public List<CapabilityPropertyEntryV2> children = new List<CapabilityPropertyEntryV2>();
	}

	[Serializable]
	private class CapabilityMethodEntryV2
	{
		public string name = "";
		public string displayName = "";
		public string declaringType = "";
		public string returnType = "";
		public int parameterCount;
		public string primaryParameterType = "";
		public string description = "";
		public bool isStatic;
	}

	[Serializable]
	private class CapabilityEventEntryV2
	{
		public string name = "";
		public string displayName = "";
		public string payloadType = "";
		public string declaringType = "";
		public string description = "";
		public bool allowedForCodegen = true;
	}

	private class CapabilityPropertyTreeEntryV2
	{
		public CapabilityPropertyEntryV2 property;
		public int depth;
	}

	[Serializable]
	private class CapabilityFeatureEntryV2
	{
		public string id = "";
		public string displayName = "";
		public string description = "";
		public List<CapabilityFeaturePortEntryV2> inputs = new List<CapabilityFeaturePortEntryV2>();
		public List<CapabilityFeaturePortEntryV2> outputs = new List<CapabilityFeaturePortEntryV2>();
		public List<CapabilityFeatureParameterEntryV2> parameters = new List<CapabilityFeatureParameterEntryV2>();
	}

	[Serializable]
	private class CapabilityFeatureCatalogFileV2
	{
		public string version = "";
		public string model = "";
		public List<CapabilityFeatureCatalogEntryV2> features = new List<CapabilityFeatureCatalogEntryV2>();
	}

	[Serializable]
	private class CapabilityFeatureCatalogEntryV2
	{
		public string id = "";
		public string name = "";
		public string description = "";
		public List<string> parameters = new List<string>();
		public List<string> inputs = new List<string>();
		public List<string> outputs = new List<string>();
	}

	private enum CapabilityFeatureInspectorTabV2
	{
		Implementation,
		Information
	}

	[Serializable]
	private class CapabilityFeatureBindingV2
	{
		public string componentName = "";
		public string memberName = "";
	}

	[Serializable]
	private class CapabilityFeaturePortEntryV2
	{
		public string name = "";
		public string displayName = "";
		public string dataType = "";
		public string description = "";
		public CapabilityFeatureBindingV2 binding = new CapabilityFeatureBindingV2();
	}

	[Serializable]
	private class CapabilityFeatureParameterEntryV2
	{
		public string name = "";
		public string displayName = "";
		public string type = "";
		public string description = "";
		public string defaultValue = "";
		public CapabilityFeatureBindingV2 binding = new CapabilityFeatureBindingV2();
	}

	[Serializable]
	private class CapabilityExportModelV2
	{
		public List<CapabilityComponentEntryV2> components = new List<CapabilityComponentEntryV2>();
		public List<CapabilityFeatureImplementationEntryV2> implementations = new List<CapabilityFeatureImplementationEntryV2>();
	}

	[Serializable]
	private class CapabilityFeatureImplementationEntryV2
	{
		public string id = "";
		public List<CapabilityFeaturePortBindingEntryV2> inputs = new List<CapabilityFeaturePortBindingEntryV2>();
		public List<CapabilityFeaturePortBindingEntryV2> outputs = new List<CapabilityFeaturePortBindingEntryV2>();
		public List<CapabilityFeatureParameterBindingEntryV2> parameters = new List<CapabilityFeatureParameterBindingEntryV2>();
	}

	[Serializable]
	private class CapabilityFeaturePortBindingEntryV2
	{
		public string name = "";
		public CapabilityFeatureBindingV2 binding = new CapabilityFeatureBindingV2();
	}

	[Serializable]
	private class CapabilityFeatureParameterBindingEntryV2
	{
		public string name = "";
		public CapabilityFeatureBindingV2 binding = new CapabilityFeatureBindingV2();
	}

	private readonly string[] capabilityTabsV2 = { "Components", "Features" };
	private readonly string[] capabilityInspectorTabsV2 = { "Component", "Properties", "Methods", "Events" };
	private readonly string[] capabilityCanAddOptionsV2 = { "No", "Yes", "Characters", "Game", "Nature", "Props", "Other" };
	private readonly string[] capabilityFeatureInspectorTabsV2 = { "Implementation", "Information" };
	private const string CapabilityFeatureCatalogAssetPathV2 = "Editor/FeatureCatalog/default-feature-catalog-v2.json";
	private const string CapabilityFeatureCatalogPackageAssetPathV2 = "Packages/com.plyground.export/Editor/FeatureCatalog/default-feature-catalog-v2.json";
	private const string CapabilityFeatureCatalogFileNameV2 = "default-feature-catalog-v2";
	private CapabilityWorkspaceTabV2 activeCapabilityTabV2;
	private CapabilityComponentInspectorTabV2 activeCapabilityInspectorTabV2;
	private CapabilityFeatureInspectorTabV2 activeCapabilityFeatureInspectorTabV2;
	private Vector2 capabilitiesV2Scroll;
	private Vector2 capabilityComponentTreeScrollV2;
	private Vector2 capabilityComponentInspectorScrollV2;
	private Vector2 capabilityPropertyListScrollV2;
	private Vector2 capabilityPropertyEditorScrollV2;
	private Vector2 capabilityMethodListScrollV2;
	private Vector2 capabilityMethodEditorScrollV2;
	private Vector2 capabilityEventListScrollV2;
	private Vector2 capabilityEventEditorScrollV2;
	private Vector2 capabilityFeatureListScrollV2;
	private Vector2 capabilityFeatureEditorScrollV2;
	private Vector2 capabilityFeatureInformationScrollV2;
	private List<CapabilityComponentEntryV2> capabilityComponentsV2 = new List<CapabilityComponentEntryV2>();
	private List<CapabilityFeatureEntryV2> capabilityFeaturesV2 = new List<CapabilityFeatureEntryV2>();
	private List<CapabilityFeatureCatalogEntryV2> capabilityFeatureCatalogV2;
	private int selectedCapabilityComponentIndexV2 = -1;
	private int selectedCapabilityPropertyIndexV2 = -1;
	private int selectedCapabilityMethodIndexV2 = -1;
	private int selectedCapabilityEventIndexV2 = -1;
	private int selectedCapabilityFeatureIndexV2 = -1;

	private void DrawCapabilitiesV2Tab()
	{
		activeCapabilityTabV2 = (CapabilityWorkspaceTabV2)GUILayout.Toolbar((int)activeCapabilityTabV2, capabilityTabsV2);
		EditorGUILayout.Space(6f);
		EditorGUILayout.BeginVertical("box");
		capabilitiesV2Scroll = EditorGUILayout.BeginScrollView(capabilitiesV2Scroll, GUILayout.ExpandHeight(true));
		switch (activeCapabilityTabV2)
		{
			case CapabilityWorkspaceTabV2.Components:
				DrawCapabilitiesV2ComponentsWorkspace();
				break;
			case CapabilityWorkspaceTabV2.Features:
				DrawCapabilitiesV2FeaturesWorkspace();
				break;
		}

		EditorGUILayout.EndScrollView();
		EditorGUILayout.EndVertical();
	}

	private void DrawCapabilitiesV2ComponentsWorkspace()
	{
		EditorGUILayout.BeginHorizontal();

		EditorGUILayout.BeginVertical("box", GUILayout.Width(Mathf.Max(260f, position.width * 0.32f)), GUILayout.Height(560f));
		DrawCapabilitiesV2ComponentsTreePane();
		EditorGUILayout.EndVertical();

		EditorGUILayout.BeginVertical("box", GUILayout.ExpandWidth(true), GUILayout.Height(560f));
		DrawCapabilitiesV2ComponentsInspectorPane();
		EditorGUILayout.EndVertical();

		EditorGUILayout.EndHorizontal();
	}

	private void DrawCapabilitiesV2FeaturesWorkspace()
	{
		EditorGUILayout.BeginHorizontal();

		EditorGUILayout.BeginVertical("box", GUILayout.Width(Mathf.Max(260f, position.width * 0.32f)), GUILayout.Height(560f));
		DrawCapabilitiesV2FeatureListPane();
		EditorGUILayout.EndVertical();

		EditorGUILayout.BeginVertical("box", GUILayout.ExpandWidth(true), GUILayout.Height(560f));
		DrawCapabilitiesV2FeatureInspectorPane();
		EditorGUILayout.EndVertical();

		EditorGUILayout.EndHorizontal();
	}

	private void DrawCapabilitiesV2ComponentsTreePane()
	{
		EditorGUILayout.BeginHorizontal();
		if (GUILayout.Button("Add From Source", GUILayout.ExpandWidth(true)))
		{
			OpenCapabilitiesV2SourceSelector();
		}

		if (GUILayout.Button("Add From Project", GUILayout.ExpandWidth(true)))
		{
			OpenCapabilitiesV2ProjectSelector();
		}
		EditorGUILayout.EndHorizontal();

		EditorGUILayout.Space(6f);
		capabilityComponentTreeScrollV2 = EditorGUILayout.BeginScrollView(capabilityComponentTreeScrollV2, GUILayout.ExpandHeight(true));

		if (capabilityComponentsV2.Count > 0)
		{
			selectedCapabilityComponentIndexV2 = Mathf.Clamp(
				selectedCapabilityComponentIndexV2 < 0 ? 0 : selectedCapabilityComponentIndexV2,
				0,
				capabilityComponentsV2.Count - 1);

			for (int i = 0; i < capabilityComponentsV2.Count; i++)
			{
				CapabilityComponentEntryV2 entry = capabilityComponentsV2[i];
				EditorGUILayout.BeginHorizontal();
				if (DrawSelectableListButton(GetCapabilityComponentLabelV2(entry), selectedCapabilityComponentIndexV2 == i, GUILayout.Height(30f)))
				{
					selectedCapabilityComponentIndexV2 = i;
					selectedCapabilityPropertyIndexV2 = -1;
					selectedCapabilityMethodIndexV2 = -1;
					selectedCapabilityEventIndexV2 = -1;
				}

				if (GUILayout.Button("X", GUILayout.Width(28f), GUILayout.Height(30f)))
				{
					capabilityComponentsV2.RemoveAt(i);
					selectedCapabilityComponentIndexV2 = Mathf.Clamp(selectedCapabilityComponentIndexV2, 0, capabilityComponentsV2.Count - 1);
					if (capabilityComponentsV2.Count == 0)
					{
						selectedCapabilityComponentIndexV2 = -1;
					}

					EditorGUILayout.EndHorizontal();
					break;
				}
				EditorGUILayout.EndHorizontal();
			}
		}

		EditorGUILayout.EndScrollView();
	}

	private void DrawCapabilitiesV2ComponentsInspectorPane()
	{
		activeCapabilityInspectorTabV2 = (CapabilityComponentInspectorTabV2)GUILayout.Toolbar((int)activeCapabilityInspectorTabV2, capabilityInspectorTabsV2);
		EditorGUILayout.Space(6f);
		capabilityComponentInspectorScrollV2 = EditorGUILayout.BeginScrollView(capabilityComponentInspectorScrollV2, GUILayout.ExpandHeight(true));

		CapabilityComponentEntryV2 entry = GetSelectedCapabilityComponentV2();
		if (entry != null)
		{
			switch (activeCapabilityInspectorTabV2)
			{
				case CapabilityComponentInspectorTabV2.Component:
					DrawCapabilityComponentInspectorV2(entry);
					break;
				case CapabilityComponentInspectorTabV2.Properties:
					DrawCapabilityPropertyInspectorV2(entry);
					break;
				case CapabilityComponentInspectorTabV2.Methods:
					DrawCapabilityMethodInspectorV2(entry);
					break;
				case CapabilityComponentInspectorTabV2.Events:
					DrawCapabilityEventInspectorV2(entry);
					break;
			}
		}

		EditorGUILayout.EndScrollView();
	}

	private void DrawCapabilityComponentInspectorV2(CapabilityComponentEntryV2 entry)
	{
		using (new EditorGUI.DisabledScope(true))
		{
			EditorGUILayout.TextField("Component Name", GetCapabilityComponentNameV2(entry));
		}

		entry.displayName = EditorGUILayout.TextField("Display Name", entry.displayName);
		entry.description = EditorGUILayout.TextField("Description", entry.description);
		entry.canAdd = DrawCapabilityCanAddPopupV2(entry.canAdd);
	}

	private void DrawCapabilityPropertyInspectorV2(CapabilityComponentEntryV2 entry)
	{
		EditorGUILayout.BeginHorizontal();
		List<CapabilityPropertyTreeEntryV2> propertyEntries = BuildCapabilityPropertyTreeEntriesV2(entry.properties);

		EditorGUILayout.BeginVertical("box", GUILayout.Width(Mathf.Max(240f, position.width * 0.28f)), GUILayout.ExpandHeight(true));
		GUILayout.Label("Properties", EditorStyles.boldLabel);
		capabilityPropertyListScrollV2 = EditorGUILayout.BeginScrollView(capabilityPropertyListScrollV2, GUILayout.ExpandHeight(true));
		if (propertyEntries.Count > 0)
		{
			EditorGUILayout.BeginHorizontal();
			selectedCapabilityPropertyIndexV2 = Mathf.Clamp(selectedCapabilityPropertyIndexV2 < 0 ? 0 : selectedCapabilityPropertyIndexV2, 0, propertyEntries.Count - 1);
			EditorGUILayout.EndHorizontal();
			for (int i = 0; i < propertyEntries.Count; i++)
			{
				CapabilityPropertyTreeEntryV2 treeEntry = propertyEntries[i];
				CapabilityPropertyEntryV2 property = treeEntry.property;
				EditorGUILayout.BeginHorizontal();
				GUILayout.Space(treeEntry.depth * 16f);
				if (DrawSelectableListButton(GetCapabilityPropertyLabelV2(property), selectedCapabilityPropertyIndexV2 == i, GUILayout.Height(28f)))
				{
					selectedCapabilityPropertyIndexV2 = i;
				}

				if (GUILayout.Button("X", GUILayout.Width(28f), GUILayout.Height(28f)))
				{
					RemoveCapabilityPropertyEntryV2(entry.properties, property);
					propertyEntries = BuildCapabilityPropertyTreeEntriesV2(entry.properties);
					selectedCapabilityPropertyIndexV2 = Mathf.Clamp(selectedCapabilityPropertyIndexV2, 0, propertyEntries.Count - 1);
					if (propertyEntries.Count == 0)
					{
						selectedCapabilityPropertyIndexV2 = -1;
					}

					EditorGUILayout.EndHorizontal();
					break;
				}
				EditorGUILayout.EndHorizontal();
			}
		}
		EditorGUILayout.EndScrollView();
		EditorGUILayout.EndVertical();

		EditorGUILayout.BeginVertical("box", GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
		capabilityPropertyEditorScrollV2 = EditorGUILayout.BeginScrollView(capabilityPropertyEditorScrollV2, GUILayout.ExpandHeight(true));
		CapabilityPropertyEntryV2 selectedProperty = selectedCapabilityPropertyIndexV2 >= 0 && selectedCapabilityPropertyIndexV2 < propertyEntries.Count
			? propertyEntries[selectedCapabilityPropertyIndexV2].property
			: null;
		if (selectedProperty != null)
		{
			using (new EditorGUI.DisabledScope(true))
			{
				EditorGUILayout.TextField("Property Name", selectedProperty.name);
			}

			selectedProperty.displayName = EditorGUILayout.TextField("Display Name", selectedProperty.displayName);
			selectedProperty.type = EditorGUILayout.TextField("Type", selectedProperty.type);
			DrawStringListEditor("Enum Values", selectedProperty.enumValues);
			selectedProperty.description = EditorGUILayout.TextField("Description", selectedProperty.description);
			selectedProperty.writable = EditorGUILayout.Toggle("Writable", selectedProperty.writable);
			selectedProperty.userEditable = EditorGUILayout.Toggle("User Editable", selectedProperty.userEditable);
			selectedProperty.defaultValue = EditorGUILayout.TextField("Default", selectedProperty.defaultValue);
			if (selectedProperty.children != null && selectedProperty.children.Count > 0)
			{
				EditorGUILayout.LabelField("Nested Properties", selectedProperty.children.Count.ToString());
			}
		}
		EditorGUILayout.EndScrollView();
		EditorGUILayout.EndVertical();

		EditorGUILayout.EndHorizontal();
	}

	private List<CapabilityPropertyTreeEntryV2> BuildCapabilityPropertyTreeEntriesV2(List<CapabilityPropertyEntryV2> properties)
	{
		List<CapabilityPropertyTreeEntryV2> entries = new List<CapabilityPropertyTreeEntryV2>();
		AppendCapabilityPropertyTreeEntriesV2(entries, properties, 0);
		return entries;
	}

	private void AppendCapabilityPropertyTreeEntriesV2(List<CapabilityPropertyTreeEntryV2> entries, List<CapabilityPropertyEntryV2> properties, int depth)
	{
		foreach (CapabilityPropertyEntryV2 property in properties ?? new List<CapabilityPropertyEntryV2>())
		{
			if (property == null)
			{
				continue;
			}

			entries.Add(new CapabilityPropertyTreeEntryV2
			{
				property = property,
				depth = depth
			});

			AppendCapabilityPropertyTreeEntriesV2(entries, property.children, depth + 1);
		}
	}

	private bool RemoveCapabilityPropertyEntryV2(List<CapabilityPropertyEntryV2> properties, CapabilityPropertyEntryV2 target)
	{
		if (properties == null || target == null)
		{
			return false;
		}

		if (properties.Remove(target))
		{
			return true;
		}

		foreach (CapabilityPropertyEntryV2 property in properties)
		{
			if (property != null && RemoveCapabilityPropertyEntryV2(property.children, target))
			{
				return true;
			}
		}

		return false;
	}

	private void DrawCapabilityMethodInspectorV2(CapabilityComponentEntryV2 entry)
	{
		EditorGUILayout.BeginHorizontal();

		EditorGUILayout.BeginVertical("box", GUILayout.Width(Mathf.Max(240f, position.width * 0.28f)), GUILayout.ExpandHeight(true));
		GUILayout.Label("Methods", EditorStyles.boldLabel);
		capabilityMethodListScrollV2 = EditorGUILayout.BeginScrollView(capabilityMethodListScrollV2, GUILayout.ExpandHeight(true));
		if (entry.methods.Count > 0)
		{
			EditorGUILayout.BeginHorizontal();
			selectedCapabilityMethodIndexV2 = Mathf.Clamp(selectedCapabilityMethodIndexV2 < 0 ? 0 : selectedCapabilityMethodIndexV2, 0, entry.methods.Count - 1);
			EditorGUILayout.EndHorizontal();
			for (int i = 0; i < entry.methods.Count; i++)
			{
				CapabilityMethodEntryV2 method = entry.methods[i];
				EditorGUILayout.BeginHorizontal();
				if (DrawSelectableListButton(GetCapabilityMethodLabelV2(method), selectedCapabilityMethodIndexV2 == i, GUILayout.Height(28f)))
				{
					selectedCapabilityMethodIndexV2 = i;
				}

				if (GUILayout.Button("X", GUILayout.Width(28f), GUILayout.Height(28f)))
				{
					entry.methods.RemoveAt(i);
					selectedCapabilityMethodIndexV2 = Mathf.Clamp(selectedCapabilityMethodIndexV2, 0, entry.methods.Count - 1);
					if (entry.methods.Count == 0)
					{
						selectedCapabilityMethodIndexV2 = -1;
					}

					EditorGUILayout.EndHorizontal();
					break;
				}
				EditorGUILayout.EndHorizontal();
			}
		}
		EditorGUILayout.EndScrollView();
		EditorGUILayout.EndVertical();

		EditorGUILayout.BeginVertical("box", GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
		capabilityMethodEditorScrollV2 = EditorGUILayout.BeginScrollView(capabilityMethodEditorScrollV2, GUILayout.ExpandHeight(true));
		CapabilityMethodEntryV2 selectedMethod = selectedCapabilityMethodIndexV2 >= 0 && selectedCapabilityMethodIndexV2 < entry.methods.Count
			? entry.methods[selectedCapabilityMethodIndexV2]
			: null;
		if (selectedMethod != null)
		{
			using (new EditorGUI.DisabledScope(true))
			{
				EditorGUILayout.TextField("Method Name", selectedMethod.name);
			}

			selectedMethod.displayName = EditorGUILayout.TextField("Display Name", selectedMethod.displayName);
			selectedMethod.description = EditorGUILayout.TextField("Description", selectedMethod.description);
		}
		EditorGUILayout.EndScrollView();
		EditorGUILayout.EndVertical();

		EditorGUILayout.EndHorizontal();
	}

	private void DrawCapabilityEventInspectorV2(CapabilityComponentEntryV2 entry)
	{
		EditorGUILayout.BeginHorizontal();

		EditorGUILayout.BeginVertical("box", GUILayout.Width(Mathf.Max(240f, position.width * 0.28f)), GUILayout.ExpandHeight(true));
		GUILayout.Label("Events", EditorStyles.boldLabel);
		capabilityEventListScrollV2 = EditorGUILayout.BeginScrollView(capabilityEventListScrollV2, GUILayout.ExpandHeight(true));
		if (entry.events.Count > 0)
		{
			EditorGUILayout.BeginHorizontal();
			selectedCapabilityEventIndexV2 = Mathf.Clamp(selectedCapabilityEventIndexV2 < 0 ? 0 : selectedCapabilityEventIndexV2, 0, entry.events.Count - 1);
			EditorGUILayout.EndHorizontal();
			for (int i = 0; i < entry.events.Count; i++)
			{
				CapabilityEventEntryV2 eventInfo = entry.events[i];
				EditorGUILayout.BeginHorizontal();
				if (DrawSelectableListButton(GetCapabilityEventLabelV2(eventInfo), selectedCapabilityEventIndexV2 == i, GUILayout.Height(28f)))
				{
					selectedCapabilityEventIndexV2 = i;
				}

				if (GUILayout.Button("X", GUILayout.Width(28f), GUILayout.Height(28f)))
				{
					entry.events.RemoveAt(i);
					selectedCapabilityEventIndexV2 = Mathf.Clamp(selectedCapabilityEventIndexV2, 0, entry.events.Count - 1);
					if (entry.events.Count == 0)
					{
						selectedCapabilityEventIndexV2 = -1;
					}

					EditorGUILayout.EndHorizontal();
					break;
				}
				EditorGUILayout.EndHorizontal();
			}
		}
		EditorGUILayout.EndScrollView();
		EditorGUILayout.EndVertical();

		EditorGUILayout.BeginVertical("box", GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
		capabilityEventEditorScrollV2 = EditorGUILayout.BeginScrollView(capabilityEventEditorScrollV2, GUILayout.ExpandHeight(true));
		CapabilityEventEntryV2 selectedEvent = selectedCapabilityEventIndexV2 >= 0 && selectedCapabilityEventIndexV2 < entry.events.Count
			? entry.events[selectedCapabilityEventIndexV2]
			: null;
		if (selectedEvent != null)
		{
			using (new EditorGUI.DisabledScope(true))
			{
				EditorGUILayout.TextField("Event Name", selectedEvent.name);
			}

			selectedEvent.displayName = EditorGUILayout.TextField("Display Name", selectedEvent.displayName);
			selectedEvent.description = EditorGUILayout.TextField("Description", selectedEvent.description);
		}
		EditorGUILayout.EndScrollView();
		EditorGUILayout.EndVertical();

		EditorGUILayout.EndHorizontal();
	}

	private void DrawCapabilitiesV2FeatureListPane()
	{
		EditorGUILayout.BeginHorizontal();
		if (GUILayout.Button("Add", GUILayout.Width(90f)))
		{
			ShowCapabilityFeatureCatalogMenuV2();
		}
		EditorGUILayout.EndHorizontal();

		EditorGUILayout.Space(6f);
		capabilityFeatureListScrollV2 = EditorGUILayout.BeginScrollView(capabilityFeatureListScrollV2, GUILayout.ExpandHeight(true));

		if (capabilityFeaturesV2.Count > 0)
		{
			selectedCapabilityFeatureIndexV2 = Mathf.Clamp(
				selectedCapabilityFeatureIndexV2 < 0 ? 0 : selectedCapabilityFeatureIndexV2,
				0,
				capabilityFeaturesV2.Count - 1);

			for (int i = 0; i < capabilityFeaturesV2.Count; i++)
			{
				CapabilityFeatureEntryV2 entry = capabilityFeaturesV2[i];
				EditorGUILayout.BeginHorizontal();
				if (DrawSelectableListButton(GetCapabilityFeatureLabelV2(entry), selectedCapabilityFeatureIndexV2 == i, GUILayout.Height(30f)))
				{
					selectedCapabilityFeatureIndexV2 = i;
				}

				if (GUILayout.Button("X", GUILayout.Width(28f), GUILayout.Height(30f)))
				{
					capabilityFeaturesV2.RemoveAt(i);
					selectedCapabilityFeatureIndexV2 = Mathf.Clamp(selectedCapabilityFeatureIndexV2, 0, capabilityFeaturesV2.Count - 1);
					if (capabilityFeaturesV2.Count == 0)
					{
						selectedCapabilityFeatureIndexV2 = -1;
					}

					EditorGUILayout.EndHorizontal();
					break;
				}
				EditorGUILayout.EndHorizontal();
			}
		}

		EditorGUILayout.EndScrollView();
	}

	private void DrawCapabilitiesV2FeatureInspectorPane()
	{
		CapabilityFeatureEntryV2 entry = GetSelectedCapabilityFeatureV2();
		activeCapabilityFeatureInspectorTabV2 = (CapabilityFeatureInspectorTabV2)GUILayout.Toolbar((int)activeCapabilityFeatureInspectorTabV2, capabilityFeatureInspectorTabsV2);
		EditorGUILayout.Space(6f);
		if (entry == null)
		{
			return;
		}

		switch (activeCapabilityFeatureInspectorTabV2)
		{
			case CapabilityFeatureInspectorTabV2.Implementation:
				DrawCapabilityFeatureImplementationInspectorV2(entry);
				break;
			case CapabilityFeatureInspectorTabV2.Information:
				DrawCapabilityFeatureInformationInspectorV2(entry);
				break;
		}
	}

	private void DrawCapabilityFeatureImplementationInspectorV2(CapabilityFeatureEntryV2 entry)
	{
		capabilityFeatureEditorScrollV2 = EditorGUILayout.BeginScrollView(capabilityFeatureEditorScrollV2, GUILayout.ExpandHeight(true));
		DrawCapabilityFeatureBindingsSectionV2("Inputs", entry.inputs, CapabilityFeatureBindingTargetV2.Input);
		EditorGUILayout.Space(8f);
		DrawCapabilityFeatureBindingsSectionV2("Outputs", entry.outputs, CapabilityFeatureBindingTargetV2.Output);
		EditorGUILayout.Space(8f);
		DrawCapabilityFeatureParameterBindingsSectionV2(entry.parameters);
		EditorGUILayout.EndScrollView();
	}

	private void DrawCapabilityFeatureInformationInspectorV2(CapabilityFeatureEntryV2 entry)
	{
		capabilityFeatureInformationScrollV2 = EditorGUILayout.BeginScrollView(capabilityFeatureInformationScrollV2, GUILayout.ExpandHeight(true));
		using (new EditorGUI.DisabledScope(true))
		{
			EditorGUILayout.TextField("Feature Id", entry.id);
		}

		entry.displayName = EditorGUILayout.TextField("Display Name", entry.displayName);
		entry.description = EditorGUILayout.TextField("Description", entry.description);
		EditorGUILayout.Space(8f);
		DrawCapabilityFeaturePortInfoSectionV2("Inputs", entry.inputs);
		EditorGUILayout.Space(8f);
		DrawCapabilityFeaturePortInfoSectionV2("Outputs", entry.outputs);
		EditorGUILayout.Space(8f);
		DrawCapabilityFeatureParameterInfoSectionV2(entry.parameters);
		EditorGUILayout.EndScrollView();
	}

	private enum CapabilityFeatureBindingTargetV2
	{
		Input,
		Output
	}

	private void DrawCapabilityFeatureBindingsSectionV2(string label, List<CapabilityFeaturePortEntryV2> ports, CapabilityFeatureBindingTargetV2 target)
	{
		GUILayout.Label(label, EditorStyles.boldLabel);
		foreach (CapabilityFeaturePortEntryV2 port in (ports ?? new List<CapabilityFeaturePortEntryV2>())
			.Where(port => port != null && !string.IsNullOrWhiteSpace(port.name)))
		{
			port.binding ??= new CapabilityFeatureBindingV2();
			List<string> componentOptions = GetCapabilityBindingComponentOptionsV2(port, target);

			EditorGUILayout.BeginVertical("helpbox");
			EditorGUILayout.BeginHorizontal();
			GUILayout.Label(GetCapabilityFeaturePortLabelV2(port), GUILayout.Width(180f));
			string nextComponentName = DrawCapabilityBindingPopupV2(port.binding.componentName, componentOptions, 200f);
			if (!string.Equals(nextComponentName, port.binding.componentName, StringComparison.OrdinalIgnoreCase))
			{
				port.binding.componentName = nextComponentName;
				port.binding.memberName = "";
			}

			List<string> memberOptions = GetCapabilityBindingMemberOptionsV2(port, port.binding.componentName, target);
			port.binding.memberName = DrawCapabilityBindingPopupV2(port.binding.memberName, memberOptions, 260f);
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.EndVertical();
		}
	}

	private void DrawCapabilityFeatureParameterBindingsSectionV2(List<CapabilityFeatureParameterEntryV2> parameters)
	{
		GUILayout.Label("Parameters", EditorStyles.boldLabel);
		foreach (CapabilityFeatureParameterEntryV2 parameter in (parameters ?? new List<CapabilityFeatureParameterEntryV2>())
			.Where(parameter => parameter != null && !string.IsNullOrWhiteSpace(parameter.name)))
		{
			parameter.binding ??= new CapabilityFeatureBindingV2();
			List<string> componentOptions = GetCapabilityBindingComponentOptionsV2(parameter);

			EditorGUILayout.BeginVertical("helpbox");
			EditorGUILayout.BeginHorizontal();
			GUILayout.Label(GetCapabilityFeatureParameterLabelV2(parameter), GUILayout.Width(180f));
			string nextComponentName = DrawCapabilityBindingPopupV2(parameter.binding.componentName, componentOptions, 200f);
			if (!string.Equals(nextComponentName, parameter.binding.componentName, StringComparison.OrdinalIgnoreCase))
			{
				parameter.binding.componentName = nextComponentName;
				parameter.binding.memberName = "";
			}

			List<string> memberOptions = GetCapabilityParameterBindingMemberOptionsV2(parameter, parameter.binding.componentName);
			parameter.binding.memberName = DrawCapabilityBindingPopupV2(parameter.binding.memberName, memberOptions, 260f);
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.EndVertical();
		}
	}

	private void DrawCapabilityFeaturePortInfoSectionV2(string label, List<CapabilityFeaturePortEntryV2> ports)
	{
		GUILayout.Label(label, EditorStyles.boldLabel);
		if (GUILayout.Button("Add " + label.Substring(0, label.Length - 1), GUILayout.Width(110f)))
		{
			ports.Add(new CapabilityFeaturePortEntryV2
			{
				name = "new_" + label.Substring(0, label.Length - 1).ToLowerInvariant(),
				displayName = "New " + label.Substring(0, label.Length - 1),
				dataType = ""
			});
		}

		for (int i = 0; i < ports.Count; i++)
		{
			CapabilityFeaturePortEntryV2 port = ports[i];
			EditorGUILayout.BeginVertical("helpbox");
			EditorGUILayout.BeginHorizontal();
			GUILayout.Label(GetCapabilityFeaturePortLabelV2(port), EditorStyles.boldLabel);
			GUILayout.FlexibleSpace();
			if (GUILayout.Button("X", GUILayout.Width(28f)))
			{
				ports.RemoveAt(i);
				EditorGUILayout.EndHorizontal();
				EditorGUILayout.EndVertical();
				return;
			}
			EditorGUILayout.EndHorizontal();
			using (new EditorGUI.DisabledScope(true))
			{
				EditorGUILayout.TextField("Port Name", port.name);
			}
			port.displayName = EditorGUILayout.TextField("Display Name", port.displayName);
			port.dataType = EditorGUILayout.TextField("Data Type", port.dataType);
			port.description = EditorGUILayout.TextField("Description", port.description);
			EditorGUILayout.EndVertical();
		}
	}

	private void DrawCapabilityFeatureParameterInfoSectionV2(List<CapabilityFeatureParameterEntryV2> parameters)
	{
		GUILayout.Label("Parameters", EditorStyles.boldLabel);
		if (GUILayout.Button("Add Parameter", GUILayout.Width(110f)))
		{
			parameters.Add(new CapabilityFeatureParameterEntryV2
			{
				name = "new_parameter",
				displayName = "New Parameter",
				type = "",
				defaultValue = ""
			});
		}

		for (int i = 0; i < parameters.Count; i++)
		{
			CapabilityFeatureParameterEntryV2 parameter = parameters[i];
			EditorGUILayout.BeginVertical("helpbox");
			EditorGUILayout.BeginHorizontal();
			GUILayout.Label(GetCapabilityFeatureParameterLabelV2(parameter), EditorStyles.boldLabel);
			GUILayout.FlexibleSpace();
			if (GUILayout.Button("X", GUILayout.Width(28f)))
			{
				parameters.RemoveAt(i);
				EditorGUILayout.EndHorizontal();
				EditorGUILayout.EndVertical();
				return;
			}
			EditorGUILayout.EndHorizontal();
			using (new EditorGUI.DisabledScope(true))
			{
				EditorGUILayout.TextField("Parameter Name", parameter.name);
			}
			parameter.displayName = EditorGUILayout.TextField("Display Name", parameter.displayName);
			parameter.type = EditorGUILayout.TextField("Type", parameter.type);
			parameter.description = EditorGUILayout.TextField("Description", parameter.description);
			parameter.defaultValue = EditorGUILayout.TextField("Default", parameter.defaultValue);
			EditorGUILayout.EndVertical();
		}
	}

	private CapabilityComponentEntryV2 GetSelectedCapabilityComponentV2()
	{
		if (selectedCapabilityComponentIndexV2 < 0 || selectedCapabilityComponentIndexV2 >= capabilityComponentsV2.Count)
		{
			return null;
		}

		return capabilityComponentsV2[selectedCapabilityComponentIndexV2];
	}

	private CapabilityFeatureEntryV2 GetSelectedCapabilityFeatureV2()
	{
		if (selectedCapabilityFeatureIndexV2 < 0 || selectedCapabilityFeatureIndexV2 >= capabilityFeaturesV2.Count)
		{
			return null;
		}

		return capabilityFeaturesV2[selectedCapabilityFeatureIndexV2];
	}

	private void OpenCapabilitiesV2ProjectSelector()
	{
		List<string> existingSelection = capabilityComponentsV2
			.Where(entry => entry != null && !entry.isCustom && !string.IsNullOrWhiteSpace(entry.sourcePath))
			.Select(entry => entry.sourcePath)
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
			.ToList();
		List<string> selectedScripts = new List<string>(existingSelection);
		CSharpScriptSelectorWindow.OpenWindow(selectedScripts);
		List<string> mergedSelection = existingSelection
			.Concat(selectedScripts ?? new List<string>())
			.Where(path => !string.IsNullOrWhiteSpace(path))
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
			.ToList();
		ProcessSelectedCapabilitySourceFilesV2(mergedSelection);
	}

	private void OpenCapabilitiesV2SourceSelector()
	{
		string assetsRoot = Application.dataPath;
		string selectedFile = EditorUtility.OpenFilePanel("Select C# Script", assetsRoot, "cs");
		if (string.IsNullOrWhiteSpace(selectedFile))
		{
			return;
		}

		if (!IsProjectSourceFileV2(selectedFile))
		{
			EditorUtility.DisplayDialog("Invalid Script", "Please choose a .cs file from inside this Unity project.", "OK");
			return;
		}

		string assetPath = AbsolutePathToAssetPathV2(selectedFile);
		if (string.IsNullOrWhiteSpace(assetPath))
		{
			return;
		}

		AddCapabilitySourceFileV2(assetPath);
	}

	private void ProcessSelectedCapabilitySourceFilesV2(List<string> selectedScripts)
	{
		List<string> normalizedSelection = (selectedScripts ?? new List<string>())
			.Where(path => !string.IsNullOrWhiteSpace(path))
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
			.ToList();

		List<CapabilityComponentEntryV2> retainedCustomEntries = capabilityComponentsV2
			.Where(entry => entry != null && entry.isCustom)
			.ToList();
		List<CapabilityComponentEntryV2> rebuiltSourceEntries = normalizedSelection
			.SelectMany(BuildCapabilityComponentEntriesFromSourceV2)
			.Where(entry => entry != null)
			.ToList();

		capabilityComponentsV2 = retainedCustomEntries
			.Concat(rebuiltSourceEntries)
			.OrderBy(entry => entry.displayName, StringComparer.OrdinalIgnoreCase)
			.ToList();
		selectedCapabilityComponentIndexV2 = capabilityComponentsV2.Count > 0 ? 0 : -1;
	}

	private void AddCapabilitySourceFileV2(string assetPath)
	{
		if (string.IsNullOrWhiteSpace(assetPath))
		{
			return;
		}

		List<string> selectedScripts = capabilityComponentsV2
			.Where(entry => entry != null && !entry.isCustom && !string.IsNullOrWhiteSpace(entry.sourcePath))
			.Select(entry => entry.sourcePath)
			.Concat(new[] { assetPath })
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
			.ToList();
		ProcessSelectedCapabilitySourceFilesV2(selectedScripts);
	}

	private CapabilityComponentEntryV2 BuildCapabilityComponentEntryFromSourceV2(string sourcePath)
	{
		MonoScript script = AssetDatabase.LoadAssetAtPath<MonoScript>(sourcePath);
		Type scriptType = script != null ? script.GetClass() : null;
		SourceScriptInfo sourceInfo = ParseSourceScript(sourcePath, scriptType);
		if (sourceInfo == null)
		{
			return CreateCapabilitySourceEntryFallbackV2(sourcePath);
		}

		UnityCapabilityComponentInfo componentInfo = scriptType != null
			? BuildUnityComponentInfo(scriptType, null, sourceInfo)
			: BuildUnityComponentInfo(sourceInfo);
		if (componentInfo == null)
		{
			return CreateCapabilitySourceEntryFallbackV2(sourcePath);
		}

		CapabilityComponentEntryV2 entry = new CapabilityComponentEntryV2
		{
			id = !string.IsNullOrWhiteSpace(componentInfo.componentId) ? componentInfo.componentId : Path.GetFileNameWithoutExtension(sourcePath),
			displayName = !string.IsNullOrWhiteSpace(componentInfo.typeName) ? GetLeafTypeName(componentInfo.typeName) : Path.GetFileNameWithoutExtension(sourcePath),
			sourcePath = sourcePath ?? "",
			isCustom = false,
			typeName = componentInfo.typeName ?? "",
			baseType = componentInfo.baseType ?? "",
			description = NormalizeImportedDescriptionV2(componentInfo.description),
			canAdd = "No",
			properties = BuildCapabilityPropertyEntriesV2(componentInfo.parameters),
			methods = BuildCapabilityMethodEntriesV2(componentInfo.methods),
			events = BuildCapabilityEventEntriesV2(componentInfo.events)
		};

		return entry;
	}

	private List<CapabilityComponentEntryV2> BuildCapabilityComponentEntriesFromSourceV2(string sourcePath)
	{
		List<CapabilityComponentEntryV2> entries = new List<CapabilityComponentEntryV2>();
		List<Type> componentTypes = ResolveComponentTypesDeclaredInSourceV2(sourcePath);
		if (componentTypes.Count == 0)
		{
			CapabilityComponentEntryV2 fallbackEntry = BuildCapabilityComponentEntryFromSourceV2(sourcePath);
			if (fallbackEntry != null)
			{
				entries.Add(fallbackEntry);
			}

			return entries;
		}

		foreach (Type componentType in componentTypes)
		{
			SourceScriptInfo sourceInfo = ParseSourceScript(sourcePath, componentType);
			UnityCapabilityComponentInfo componentInfo = BuildUnityComponentInfo(componentType, null, sourceInfo);
			if (componentInfo == null)
			{
				continue;
			}

			entries.Add(new CapabilityComponentEntryV2
			{
				id = !string.IsNullOrWhiteSpace(componentInfo.componentId) ? componentInfo.componentId : componentType.FullName ?? componentType.Name,
				displayName = !string.IsNullOrWhiteSpace(componentInfo.typeName) ? GetLeafTypeName(componentInfo.typeName) : componentType.Name,
				sourcePath = sourcePath ?? "",
				isCustom = false,
				typeName = componentInfo.typeName ?? "",
				baseType = componentInfo.baseType ?? "",
				description = NormalizeImportedDescriptionV2(componentInfo.description),
				canAdd = "No",
				properties = BuildCapabilityPropertyEntriesV2(componentInfo.parameters),
				methods = BuildCapabilityMethodEntriesV2(componentInfo.methods),
				events = BuildCapabilityEventEntriesV2(componentInfo.events)
			});
		}

		return entries
			.GroupBy(entry => entry.id, StringComparer.OrdinalIgnoreCase)
			.Select(group => group.First())
			.OrderBy(entry => entry.displayName, StringComparer.OrdinalIgnoreCase)
			.ToList();
	}

	private List<Type> ResolveComponentTypesDeclaredInSourceV2(string sourcePath)
	{
		HashSet<Type> resolvedTypes = new HashSet<Type>();
		MonoScript script = AssetDatabase.LoadAssetAtPath<MonoScript>(sourcePath);
		Type primaryType = script != null ? script.GetClass() : null;
		if (primaryType != null && typeof(Component).IsAssignableFrom(primaryType))
		{
			resolvedTypes.Add(primaryType);
		}

		string fullPath = Path.GetFullPath(sourcePath ?? "");
		if (!File.Exists(fullPath))
		{
			return resolvedTypes.OrderBy(type => type.Name, StringComparer.OrdinalIgnoreCase).ToList();
		}

		string namespaceName = "";
		foreach (string rawLine in File.ReadAllLines(fullPath))
		{
			string line = rawLine.Trim();
			if (line.StartsWith("namespace ", StringComparison.Ordinal))
			{
				string namespaceDeclaration = line.Substring("namespace ".Length).Trim();
				int braceIndex = namespaceDeclaration.IndexOf('{');
				namespaceName = braceIndex >= 0
					? namespaceDeclaration.Substring(0, braceIndex).Trim()
					: namespaceDeclaration.Trim();
				continue;
			}

			int classIndex = line.IndexOf(" class ", StringComparison.Ordinal);
			if (classIndex < 0)
			{
				continue;
			}

			string afterClass = line.Substring(classIndex + " class ".Length).Trim();
			string[] classParts = afterClass.Split(new[] { ':', ' ', '\t', '{' }, StringSplitOptions.RemoveEmptyEntries);
			if (classParts.Length == 0)
			{
				continue;
			}

			string className = classParts[0].Trim();
			string fullName = string.IsNullOrWhiteSpace(namespaceName) ? className : namespaceName + "." + className;
			Type resolved = ResolveTypeByName(fullName) ?? ResolveTypeByName(className);
			if (resolved != null && typeof(Component).IsAssignableFrom(resolved))
			{
				resolvedTypes.Add(resolved);
			}
		}

		return resolvedTypes
			.OrderBy(type => type.Name, StringComparer.OrdinalIgnoreCase)
			.ToList();
	}

	private CapabilityComponentEntryV2 CreateCapabilitySourceEntryFallbackV2(string sourcePath)
	{
		string fileName = Path.GetFileNameWithoutExtension(sourcePath) ?? "";
		return new CapabilityComponentEntryV2
		{
			id = fileName,
			displayName = fileName,
			sourcePath = sourcePath ?? "",
			isCustom = false,
			typeName = fileName
		};
	}

	private List<CapabilityPropertyEntryV2> BuildCapabilityPropertyEntriesV2(List<CapabilityParameterInfo> parameters)
	{
		return (parameters ?? new List<CapabilityParameterInfo>())
			.Where(parameter => parameter != null)
			.Select(BuildCapabilityPropertyEntryV2)
			.OrderBy(parameter => parameter.name, StringComparer.OrdinalIgnoreCase)
			.ToList();
	}

	private CapabilityPropertyEntryV2 BuildCapabilityPropertyEntryV2(CapabilityParameterInfo parameter)
	{
		return new CapabilityPropertyEntryV2
		{
			name = parameter.name ?? "",
			displayName = parameter.name ?? "",
			type = parameter.type ?? "",
			enumValues = (parameter.enumValues ?? new List<string>())
				.Where(value => !string.IsNullOrWhiteSpace(value))
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
				.ToList(),
			description = NormalizeImportedDescriptionV2(parameter.description),
			writable = parameter.required,
			userEditable = parameter.userEditable,
			defaultValue = parameter.@default ?? "",
			children = (parameter.children ?? new List<CapabilityParameterInfo>())
				.Where(child => child != null)
				.Select(BuildCapabilityPropertyEntryV2)
				.OrderBy(child => child.name, StringComparer.OrdinalIgnoreCase)
				.ToList()
		};
	}

	private List<CapabilityMethodEntryV2> BuildCapabilityMethodEntriesV2(List<CapabilityMethodInfo> methods)
	{
		return (methods ?? new List<CapabilityMethodInfo>())
			.Where(method => method != null)
			.Select(method => new CapabilityMethodEntryV2
			{
				name = method.name ?? "",
				displayName = method.name ?? "",
				declaringType = method.declaringType ?? "",
				returnType = method.returnType ?? "",
				parameterCount = method.parameters != null ? method.parameters.Count : 0,
				primaryParameterType = method.parameters != null && method.parameters.Count == 1 && method.parameters[0] != null
					? method.parameters[0].type ?? ""
					: "",
				description = NormalizeImportedDescriptionV2(method.description),
				isStatic = method.isStatic
			})
			.OrderBy(method => method.name, StringComparer.OrdinalIgnoreCase)
			.ToList();
	}

	private List<CapabilityEventEntryV2> BuildCapabilityEventEntriesV2(List<CapabilityEventInfo> events)
	{
		return (events ?? new List<CapabilityEventInfo>())
			.Where(eventInfo => eventInfo != null)
			.Select(eventInfo => new CapabilityEventEntryV2
			{
				name = eventInfo.name ?? "",
				displayName = eventInfo.name ?? "",
				payloadType = eventInfo.payloadType ?? "",
				declaringType = eventInfo.declaringType ?? "",
				description = NormalizeImportedDescriptionV2(eventInfo.description),
				allowedForCodegen = eventInfo.allowedForCodegen
			})
			.OrderBy(eventInfo => eventInfo.name, StringComparer.OrdinalIgnoreCase)
			.ToList();
	}

	private string GetCapabilityComponentLabelV2(CapabilityComponentEntryV2 entry)
	{
		if (entry == null)
		{
			return "Component";
		}

		if (!string.IsNullOrWhiteSpace(entry.displayName))
		{
			return entry.displayName;
		}

		if (!string.IsNullOrWhiteSpace(entry.typeName))
		{
			return GetLeafTypeName(entry.typeName);
		}

		return "Component";
	}

	private string GetCapabilityComponentNameV2(CapabilityComponentEntryV2 entry)
	{
		if (entry == null)
		{
			return "";
		}

		if (!string.IsNullOrWhiteSpace(entry.typeName))
		{
			return GetLeafTypeName(entry.typeName);
		}

		if (!string.IsNullOrWhiteSpace(entry.id))
		{
			return entry.id;
		}

		return entry.displayName ?? "";
	}

	private string GetCapabilityFeatureLabelV2(CapabilityFeatureEntryV2 entry)
	{
		if (entry == null)
		{
			return "Feature";
		}

		if (!string.IsNullOrWhiteSpace(entry.displayName))
		{
			return entry.displayName;
		}

		if (!string.IsNullOrWhiteSpace(entry.id))
		{
			return entry.id;
		}

		return "Feature";
	}

	private string GetCapabilityFeaturePortLabelV2(CapabilityFeaturePortEntryV2 port)
	{
		if (port == null)
		{
			return "Port";
		}

		if (!string.IsNullOrWhiteSpace(port.displayName))
		{
			return port.displayName;
		}

		if (!string.IsNullOrWhiteSpace(port.name))
		{
			return port.name;
		}

		return "Port";
	}

	private string GetCapabilityFeatureParameterLabelV2(CapabilityFeatureParameterEntryV2 parameter)
	{
		if (parameter == null)
		{
			return "Parameter";
		}

		if (!string.IsNullOrWhiteSpace(parameter.displayName))
		{
			return parameter.displayName;
		}

		if (!string.IsNullOrWhiteSpace(parameter.name))
		{
			return parameter.name;
		}

		return "Parameter";
	}

	private string GetCapabilityPropertyLabelV2(CapabilityPropertyEntryV2 property)
	{
		if (property == null)
		{
			return "Property";
		}

		if (!string.IsNullOrWhiteSpace(property.displayName))
		{
			return property.displayName;
		}

		if (!string.IsNullOrWhiteSpace(property.name))
		{
			return property.name;
		}

		return "Property";
	}

	private string GetCapabilityMethodLabelV2(CapabilityMethodEntryV2 method)
	{
		if (method == null)
		{
			return "Method";
		}

		if (!string.IsNullOrWhiteSpace(method.displayName))
		{
			return method.displayName;
		}

		if (!string.IsNullOrWhiteSpace(method.name))
		{
			return method.name;
		}

		return "Method";
	}

	private string GetCapabilityEventLabelV2(CapabilityEventEntryV2 eventInfo)
	{
		if (eventInfo == null)
		{
			return "Event";
		}

		if (!string.IsNullOrWhiteSpace(eventInfo.displayName))
		{
			return eventInfo.displayName;
		}

		if (!string.IsNullOrWhiteSpace(eventInfo.name))
		{
			return eventInfo.name;
		}

		return "Event";
	}

	private void AddCustomCapabilityComponentV2()
	{
		CapabilityComponentEntryV2 entry = new CapabilityComponentEntryV2
		{
			id = "custom_component_" + (capabilityComponentsV2.Count(component => component != null && component.isCustom) + 1),
			displayName = "New Custom Component",
			isCustom = true,
			canAdd = "No"
		};

		capabilityComponentsV2.Add(entry);
		capabilityComponentsV2 = capabilityComponentsV2
			.OrderBy(component => component.displayName, StringComparer.OrdinalIgnoreCase)
			.ToList();
		selectedCapabilityComponentIndexV2 = capabilityComponentsV2.FindIndex(component =>
			component != null &&
			string.Equals(component.id, entry.id, StringComparison.OrdinalIgnoreCase));
	}

	private void AddCapabilityFeatureV2(CapabilityFeatureCatalogEntryV2 sourceFeature)
	{
		if (sourceFeature == null || string.IsNullOrWhiteSpace(sourceFeature.id))
		{
			return;
		}

		CapabilityFeatureEntryV2 entry = new CapabilityFeatureEntryV2
		{
			id = sourceFeature.id ?? "",
			displayName = string.IsNullOrWhiteSpace(sourceFeature.name) ? sourceFeature.id : sourceFeature.name,
			description = sourceFeature.description ?? "",
			inputs = BuildCapabilityFeaturePortsV2(sourceFeature.inputs),
			outputs = BuildCapabilityFeaturePortsV2(sourceFeature.outputs),
			parameters = BuildCapabilityFeatureParametersV2(sourceFeature.parameters)
		};

		capabilityFeaturesV2.Add(entry);
		capabilityFeaturesV2 = capabilityFeaturesV2
			.OrderBy(feature => feature.displayName, StringComparer.OrdinalIgnoreCase)
			.ToList();
		selectedCapabilityFeatureIndexV2 = capabilityFeaturesV2.FindIndex(feature =>
			feature != null &&
			string.Equals(feature.id, entry.id, StringComparison.OrdinalIgnoreCase));
	}

	private void ShowCapabilityFeatureCatalogMenuV2()
	{
		List<CapabilityFeatureCatalogEntryV2> catalog = GetCapabilityFeatureCatalogV2();
		if (catalog.Count == 0)
		{
			EditorUtility.DisplayDialog("Feature Catalog", "No features were loaded from the V2 feature catalog.", "OK");
			return;
		}

		List<CapabilityFeatureCatalogEntryV2> addableFeatures = catalog
			.Where(feature => feature != null && !string.IsNullOrWhiteSpace(feature.id))
			.Where(feature => !capabilityFeaturesV2.Any(existing =>
				existing != null &&
				string.Equals(existing.id, feature.id, StringComparison.OrdinalIgnoreCase)))
			.OrderBy(feature => string.IsNullOrWhiteSpace(feature.name) ? feature.id : feature.name, StringComparer.OrdinalIgnoreCase)
			.ToList();
		if (addableFeatures.Count == 0)
		{
			EditorUtility.DisplayDialog("Feature Catalog", "All catalog features have already been added.", "OK");
			return;
		}

		GenericMenu menu = new GenericMenu();
		foreach (CapabilityFeatureCatalogEntryV2 feature in addableFeatures)
		{
			string label = string.IsNullOrWhiteSpace(feature.name) ? feature.id : feature.name;
			menu.AddItem(new GUIContent(label), false, () => AddCapabilityFeatureV2(feature));
		}

		menu.ShowAsContext();
	}

	private List<CapabilityFeatureCatalogEntryV2> GetCapabilityFeatureCatalogV2()
	{
		if (capabilityFeatureCatalogV2 != null)
		{
			return capabilityFeatureCatalogV2;
		}

		string json = LoadCapabilityFeatureCatalogJsonV2();
		if (string.IsNullOrWhiteSpace(json))
		{
			capabilityFeatureCatalogV2 = new List<CapabilityFeatureCatalogEntryV2>();
			return capabilityFeatureCatalogV2;
		}

		CapabilityFeatureCatalogFileV2 catalog = JsonUtility.FromJson<CapabilityFeatureCatalogFileV2>(json);
		capabilityFeatureCatalogV2 = catalog != null && catalog.features != null
			? catalog.features
			: new List<CapabilityFeatureCatalogEntryV2>();
		return capabilityFeatureCatalogV2;
	}

	private string LoadCapabilityFeatureCatalogJsonV2()
	{
		TextAsset asset = LoadCapabilityFeatureCatalogAssetV2();
		if (asset != null && !string.IsNullOrWhiteSpace(asset.text))
		{
			return asset.text;
		}

		string localFilePath = Path.Combine(Directory.GetCurrentDirectory(), CapabilityFeatureCatalogAssetPathV2.Replace('/', Path.DirectorySeparatorChar));
		if (File.Exists(localFilePath))
		{
			return File.ReadAllText(localFilePath);
		}

		string packageFilePath = Path.Combine(Directory.GetCurrentDirectory(), CapabilityFeatureCatalogPackageAssetPathV2.Replace('/', Path.DirectorySeparatorChar));
		if (File.Exists(packageFilePath))
		{
			return File.ReadAllText(packageFilePath);
		}

		return "";
	}

	private TextAsset LoadCapabilityFeatureCatalogAssetV2()
	{
		TextAsset asset = AssetDatabase.LoadAssetAtPath<TextAsset>(CapabilityFeatureCatalogAssetPathV2);
		if (asset != null)
		{
			return asset;
		}

		asset = AssetDatabase.LoadAssetAtPath<TextAsset>(CapabilityFeatureCatalogPackageAssetPathV2);
		if (asset != null)
		{
			return asset;
		}

		string[] guids = AssetDatabase.FindAssets(CapabilityFeatureCatalogFileNameV2 + " t:TextAsset");
		foreach (string guid in guids)
		{
			string assetPath = AssetDatabase.GUIDToAssetPath(guid);
			if (string.IsNullOrWhiteSpace(assetPath) ||
				!assetPath.EndsWith("default-feature-catalog-v2.json", StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}

			TextAsset found = AssetDatabase.LoadAssetAtPath<TextAsset>(assetPath);
			if (found != null)
			{
				return found;
			}
		}

		return null;
	}

	private List<CapabilityFeaturePortEntryV2> BuildCapabilityFeaturePortsV2(List<string> names)
	{
		return (names ?? new List<string>())
			.Where(name => !string.IsNullOrWhiteSpace(name))
			.Select(name => new CapabilityFeaturePortEntryV2
			{
				name = name,
				displayName = name,
				dataType = "",
				description = ""
			})
			.ToList();
	}

	private List<CapabilityFeatureParameterEntryV2> BuildCapabilityFeatureParametersV2(List<string> names)
	{
		return (names ?? new List<string>())
			.Where(name => !string.IsNullOrWhiteSpace(name))
			.Select(name => new CapabilityFeatureParameterEntryV2
			{
				name = name,
				displayName = name,
				type = "",
				description = "",
				defaultValue = ""
			})
			.ToList();
	}

	private string DrawCapabilityBindingPopupV2(string currentValue, List<string> options, float width)
	{
		List<string> labels = new List<string> { "<select>" };
		labels.AddRange(options ?? new List<string>());
		int selectedIndex = 0;
		if (!string.IsNullOrWhiteSpace(currentValue) && options != null)
		{
			int foundIndex = options.FindIndex(option => string.Equals(option, currentValue, StringComparison.OrdinalIgnoreCase));
			selectedIndex = foundIndex >= 0 ? foundIndex + 1 : 0;
		}

		int newIndex = EditorGUILayout.Popup(selectedIndex, labels.ToArray(), GUILayout.Width(width));
		return newIndex <= 0 || options == null ? "" : options[newIndex - 1];
	}

	private List<string> GetCapabilityBindingComponentOptionsV2(CapabilityFeaturePortEntryV2 port, CapabilityFeatureBindingTargetV2 target)
	{
		switch (target)
		{
			case CapabilityFeatureBindingTargetV2.Output:
				return capabilityComponentsV2
					.Where(component => component != null && GetCompatibleEventEntriesV2(component, port).Count > 0)
					.Select(GetCapabilityComponentNameV2)
					.Where(name => !string.IsNullOrWhiteSpace(name))
					.Distinct(StringComparer.OrdinalIgnoreCase)
					.OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
					.ToList();
			default:
				return capabilityComponentsV2
					.Where(component => component != null && GetCompatibleInputMemberNamesV2(component, port).Count > 0)
					.Select(GetCapabilityComponentNameV2)
					.Where(name => !string.IsNullOrWhiteSpace(name))
					.Distinct(StringComparer.OrdinalIgnoreCase)
					.OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
					.ToList();
		}
	}

	private List<string> GetCapabilityBindingComponentOptionsV2(CapabilityFeatureParameterEntryV2 parameter)
	{
		return capabilityComponentsV2
			.Where(component => component != null && GetCompatibleParameterPropertiesV2(component, parameter).Count > 0)
			.Select(GetCapabilityComponentNameV2)
			.Where(name => !string.IsNullOrWhiteSpace(name))
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
			.ToList();
	}

	private List<string> GetCapabilityBindingMemberOptionsV2(CapabilityFeaturePortEntryV2 port, string componentName, CapabilityFeatureBindingTargetV2 target)
	{
		CapabilityComponentEntryV2 component = FindCapabilityComponentByNameV2(componentName);
		if (component == null)
		{
			return new List<string>();
		}

		switch (target)
		{
			case CapabilityFeatureBindingTargetV2.Output:
				return GetCompatibleEventEntriesV2(component, port)
					.Select(eventInfo => eventInfo.name)
					.Where(name => !string.IsNullOrWhiteSpace(name))
					.Distinct(StringComparer.OrdinalIgnoreCase)
					.OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
					.ToList();
			default:
				return GetCompatibleInputMemberNamesV2(component, port)
					.Where(name => !string.IsNullOrWhiteSpace(name))
					.Distinct(StringComparer.OrdinalIgnoreCase)
					.OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
					.ToList();
		}
	}

	private List<string> GetCapabilityParameterBindingMemberOptionsV2(CapabilityFeatureParameterEntryV2 parameter, string componentName)
	{
		CapabilityComponentEntryV2 component = FindCapabilityComponentByNameV2(componentName);
		if (component == null)
		{
			return new List<string>();
		}

		return GetCompatibleParameterPropertiesV2(component, parameter)
			.Select(property => property.name)
			.Where(name => !string.IsNullOrWhiteSpace(name))
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
			.ToList();
	}

	private List<string> GetCompatibleInputMemberNamesV2(CapabilityComponentEntryV2 component, CapabilityFeaturePortEntryV2 port)
	{
		List<string> results = new List<string>();
		foreach (CapabilityMethodEntryV2 method in component.methods ?? new List<CapabilityMethodEntryV2>())
		{
			if (method != null && IsCompatibleInputMethodV2(method, port))
			{
				results.Add(method.name);
			}
		}

		return results;
	}

	private List<CapabilityEventEntryV2> GetCompatibleEventEntriesV2(CapabilityComponentEntryV2 component, CapabilityFeaturePortEntryV2 port)
	{
		return (component.events ?? new List<CapabilityEventEntryV2>())
			.Where(eventInfo => eventInfo != null && IsCompatibleOutputEventV2(eventInfo, port))
			.ToList();
	}

	private List<CapabilityPropertyEntryV2> GetCompatibleParameterPropertiesV2(CapabilityComponentEntryV2 component, CapabilityFeatureParameterEntryV2 parameter)
	{
		return (component.properties ?? new List<CapabilityPropertyEntryV2>())
			.Where(property => property != null && IsCompatibleParameterPropertyV2(property, parameter))
			.ToList();
	}

	private bool IsCompatibleInputMethodV2(CapabilityMethodEntryV2 method, CapabilityFeaturePortEntryV2 port)
	{
		if (method == null || string.IsNullOrWhiteSpace(method.name))
		{
			return false;
		}

		string expectedType = NormalizeCapabilityTypeNameV2(port != null ? port.dataType : "");
		if (string.IsNullOrWhiteSpace(expectedType))
		{
			return method.parameterCount <= 1;
		}

		if (IsVoidTypeV2(expectedType))
		{
			return method.parameterCount == 0;
		}

		return method.parameterCount == 1 && AreCapabilityTypesCompatibleV2(expectedType, method.primaryParameterType);
	}

	private bool IsCompatibleOutputEventV2(CapabilityEventEntryV2 eventInfo, CapabilityFeaturePortEntryV2 port)
	{
		if (eventInfo == null || string.IsNullOrWhiteSpace(eventInfo.name))
		{
			return false;
		}

		string expectedType = NormalizeCapabilityTypeNameV2(port != null ? port.dataType : "");
		return string.IsNullOrWhiteSpace(expectedType) || AreCapabilityTypesCompatibleV2(expectedType, eventInfo.payloadType);
	}

	private bool IsCompatibleParameterPropertyV2(CapabilityPropertyEntryV2 property, CapabilityFeatureParameterEntryV2 parameter)
	{
		if (property == null || string.IsNullOrWhiteSpace(property.name))
		{
			return false;
		}

		string expectedType = NormalizeCapabilityTypeNameV2(parameter != null ? parameter.type : "");
		return string.IsNullOrWhiteSpace(expectedType) || AreCapabilityTypesCompatibleV2(expectedType, property.type);
	}

	private bool AreCapabilityTypesCompatibleV2(string expectedType, string actualType)
	{
		string normalizedExpected = NormalizeCapabilityTypeNameV2(expectedType);
		string normalizedActual = NormalizeCapabilityTypeNameV2(actualType);
		if (string.IsNullOrWhiteSpace(normalizedExpected) || string.IsNullOrWhiteSpace(normalizedActual))
		{
			return true;
		}

		if (string.Equals(normalizedExpected, normalizedActual, StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}

		if ((normalizedExpected == "int" || normalizedExpected == "float") &&
			(normalizedActual == "int" || normalizedActual == "float"))
		{
			return true;
		}

		return false;
	}

	private string NormalizeCapabilityTypeNameV2(string typeName)
	{
		if (string.IsNullOrWhiteSpace(typeName))
		{
			return "";
		}

		string value = typeName.Trim();
		switch (value.ToLowerInvariant())
		{
			case "system.single":
				return "float";
			case "system.int32":
				return "int";
			case "system.boolean":
				return "bool";
			case "system.string":
				return "string";
			case "void":
			case "system.void":
				return "void";
			default:
				return value;
		}
	}

	private bool IsVoidTypeV2(string typeName)
	{
		return string.Equals(NormalizeCapabilityTypeNameV2(typeName), "void", StringComparison.OrdinalIgnoreCase);
	}

	private CapabilityComponentEntryV2 FindCapabilityComponentByNameV2(string componentName)
	{
		return capabilityComponentsV2.FirstOrDefault(component =>
			component != null &&
			string.Equals(GetCapabilityComponentNameV2(component), componentName, StringComparison.OrdinalIgnoreCase));
	}

	private string DrawCapabilityCanAddPopupV2(string currentValue)
	{
		int selectedIndex = Array.IndexOf(capabilityCanAddOptionsV2, string.IsNullOrWhiteSpace(currentValue) ? "No" : currentValue);
		if (selectedIndex < 0)
		{
			selectedIndex = 0;
		}

		selectedIndex = EditorGUILayout.Popup("Can Add", selectedIndex, capabilityCanAddOptionsV2);
		return capabilityCanAddOptionsV2[Mathf.Clamp(selectedIndex, 0, capabilityCanAddOptionsV2.Length - 1)];
	}

	private bool IsProjectSourceFileV2(string absolutePath)
	{
		if (string.IsNullOrWhiteSpace(absolutePath) || !absolutePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}

		string normalizedAssetsRoot = Path.GetFullPath(Application.dataPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
		string normalizedSelectedPath = Path.GetFullPath(absolutePath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
		return normalizedSelectedPath.StartsWith(normalizedAssetsRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
			string.Equals(normalizedSelectedPath, normalizedAssetsRoot, StringComparison.OrdinalIgnoreCase);
	}

	private string AbsolutePathToAssetPathV2(string absolutePath)
	{
		if (string.IsNullOrWhiteSpace(absolutePath))
		{
			return "";
		}

		string normalizedAssetsRoot = Path.GetFullPath(Application.dataPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
		string normalizedSelectedPath = Path.GetFullPath(absolutePath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
		if (!normalizedSelectedPath.StartsWith(normalizedAssetsRoot, StringComparison.OrdinalIgnoreCase))
		{
			return "";
		}

		string relativePath = normalizedSelectedPath.Substring(normalizedAssetsRoot.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
		return string.IsNullOrWhiteSpace(relativePath)
			? "Assets"
			: "Assets/" + relativePath.Replace(Path.DirectorySeparatorChar, '/').Replace(Path.AltDirectorySeparatorChar, '/');
	}

	private string NormalizeImportedDescriptionV2(string description)
	{
		if (string.IsNullOrWhiteSpace(description))
		{
			return "";
		}

		string value = description.Trim();
		if (string.Equals(value, "Serialized field", StringComparison.OrdinalIgnoreCase) ||
			string.Equals(value, "Public property", StringComparison.OrdinalIgnoreCase) ||
			string.Equals(value, "Public method", StringComparison.OrdinalIgnoreCase) ||
			string.Equals(value, "Event field", StringComparison.OrdinalIgnoreCase) ||
			string.Equals(value, "Inspector input field", StringComparison.OrdinalIgnoreCase) ||
			string.Equals(value, "User script type", StringComparison.OrdinalIgnoreCase) ||
			string.Equals(value, "Component inferred from Assets/ script", StringComparison.OrdinalIgnoreCase) ||
			value.StartsWith("Reflected from ", StringComparison.OrdinalIgnoreCase))
		{
			return "";
		}

		return value;
	}

	private CapabilityExportModelV2 BuildCapabilityExportModelV2()
	{
		return new CapabilityExportModelV2
		{
			components = CloneCapabilityComponentsV2(capabilityComponentsV2),
			implementations = BuildCapabilityFeatureImplementationsV2(capabilityFeaturesV2)
		};
	}

	private void LoadCapabilityExportModelV2(CapabilityExportModelV2 model)
	{
		capabilityComponentsV2 = CloneCapabilityComponentsV2(model != null ? model.components : null);
		capabilityFeaturesV2 = BuildCapabilityFeaturesFromImplementationsV2(model != null ? model.implementations : null);
		selectedCapabilityComponentIndexV2 = capabilityComponentsV2.Count > 0 ? 0 : -1;
		selectedCapabilityPropertyIndexV2 = -1;
		selectedCapabilityMethodIndexV2 = -1;
		selectedCapabilityEventIndexV2 = -1;
		selectedCapabilityFeatureIndexV2 = capabilityFeaturesV2.Count > 0 ? 0 : -1;
	}

	private List<CapabilityComponentEntryV2> CloneCapabilityComponentsV2(List<CapabilityComponentEntryV2> source)
	{
		return (source ?? new List<CapabilityComponentEntryV2>())
			.Where(entry => entry != null)
			.Select(entry => JsonUtility.FromJson<CapabilityComponentEntryV2>(JsonUtility.ToJson(entry)) ?? new CapabilityComponentEntryV2())
			.OrderBy(entry => entry.displayName, StringComparer.OrdinalIgnoreCase)
			.ToList();
	}

	private List<CapabilityFeatureEntryV2> CloneCapabilityFeaturesV2(List<CapabilityFeatureEntryV2> source)
	{
		return (source ?? new List<CapabilityFeatureEntryV2>())
			.Where(entry => entry != null)
			.Select(entry => JsonUtility.FromJson<CapabilityFeatureEntryV2>(JsonUtility.ToJson(entry)) ?? new CapabilityFeatureEntryV2())
			.OrderBy(entry => entry.displayName, StringComparer.OrdinalIgnoreCase)
			.ToList();
	}

	private bool HasCapabilityExportModelV2(CapabilityExportModelV2 model)
	{
		return model != null &&
			((model.components != null && model.components.Count > 0) ||
			 (model.implementations != null && model.implementations.Count > 0));
	}

	private CapabilityExportModelV2 BuildCapabilityExportModelV2FromLegacy()
	{
		List<CapabilityComponentEntryV2> components = new List<CapabilityComponentEntryV2>();
		foreach (UnityCapabilityComponentInfo component in moduleCapabilities != null && moduleCapabilities.unity != null
			? moduleCapabilities.unity.components ?? new List<UnityCapabilityComponentInfo>()
			: new List<UnityCapabilityComponentInfo>())
		{
			if (component == null)
			{
				continue;
			}

			components.Add(new CapabilityComponentEntryV2
			{
				id = component.componentId ?? "",
				displayName = !string.IsNullOrWhiteSpace(component.typeName) ? GetLeafTypeName(component.typeName) : component.componentId ?? "",
				sourcePath = "",
				isCustom = false,
				typeName = component.typeName ?? "",
				baseType = component.baseType ?? "",
				description = NormalizeImportedDescriptionV2(component.description),
				canAdd = "No",
				properties = BuildCapabilityPropertyEntriesV2(component.parameters),
				methods = BuildCapabilityMethodEntriesV2(component.methods),
				events = BuildCapabilityEventEntriesV2(component.events)
			});
		}

		List<CapabilityFeatureEntryV2> features = new List<CapabilityFeatureEntryV2>();
		PlyFeatureManifest manifest = featureManifest ?? new PlyFeatureManifest();
		foreach (PlyFeatureImplementation implementation in manifest.implementations ?? new List<PlyFeatureImplementation>())
		{
			if (implementation == null || string.IsNullOrWhiteSpace(implementation.featureId))
			{
				continue;
			}

			PlySemanticFeatureDefinition feature = (manifest.features ?? new List<PlySemanticFeatureDefinition>())
				.FirstOrDefault(entry => entry != null && string.Equals(entry.id, implementation.featureId, StringComparison.OrdinalIgnoreCase));
			if (feature == null)
			{
				continue;
			}

			CapabilityFeatureEntryV2 entry = new CapabilityFeatureEntryV2
			{
				id = feature.id ?? "",
				displayName = string.IsNullOrWhiteSpace(feature.name) ? feature.id : feature.name,
				description = feature.description ?? "",
				inputs = (feature.inputs ?? new List<PlySemanticFeaturePort>())
					.Select(port => new CapabilityFeaturePortEntryV2
					{
						name = port.name ?? "",
						displayName = port.name ?? "",
						dataType = port.dataType.ToString(),
						description = "",
						binding = new CapabilityFeatureBindingV2
						{
							componentName = GetLegacyBindingComponentNameV2(implementation, true, port.name),
							memberName = GetLegacyBindingMemberNameV2(implementation, true, port.name)
						}
					})
					.ToList(),
				outputs = (feature.outputs ?? new List<PlySemanticFeaturePort>())
					.Select(port => new CapabilityFeaturePortEntryV2
					{
						name = port.name ?? "",
						displayName = port.name ?? "",
						dataType = port.dataType.ToString(),
						description = "",
						binding = new CapabilityFeatureBindingV2
						{
							componentName = GetLegacyBindingComponentNameV2(implementation, false, port.name),
							memberName = GetLegacyBindingMemberNameV2(implementation, false, port.name)
						}
					})
					.ToList(),
				parameters = (feature.parameters ?? new List<PlySemanticFeatureParameter>())
					.Select(parameter => new CapabilityFeatureParameterEntryV2
					{
						name = parameter.name ?? "",
						displayName = parameter.name ?? "",
						type = parameter.type.ToString(),
						description = "",
						defaultValue = parameter.defaultValue ?? "",
						binding = new CapabilityFeatureBindingV2
						{
							componentName = GetLegacyParameterBindingComponentNameV2(implementation, parameter.name),
							memberName = GetLegacyParameterBindingMemberNameV2(implementation, parameter.name)
						}
					})
					.ToList()
			};

			features.Add(entry);
		}

		return new CapabilityExportModelV2
		{
			components = CloneCapabilityComponentsV2(components),
			implementations = BuildCapabilityFeatureImplementationsV2(features)
		};
	}

	private List<CapabilityFeatureImplementationEntryV2> BuildCapabilityFeatureImplementationsV2(List<CapabilityFeatureEntryV2> source)
	{
		return (source ?? new List<CapabilityFeatureEntryV2>())
			.Where(entry => entry != null && !string.IsNullOrWhiteSpace(entry.id))
			.Where(HasAnyFeatureBindingsV2)
			.Select(entry => new CapabilityFeatureImplementationEntryV2
			{
				id = entry.id ?? "",
				inputs = (entry.inputs ?? new List<CapabilityFeaturePortEntryV2>())
					.Where(port => port != null && !string.IsNullOrWhiteSpace(port.name))
					.Select(port => new CapabilityFeaturePortBindingEntryV2
					{
						name = port.name ?? "",
						binding = CloneCapabilityBindingV2(port.binding)
					})
					.ToList(),
				outputs = (entry.outputs ?? new List<CapabilityFeaturePortEntryV2>())
					.Where(port => port != null && !string.IsNullOrWhiteSpace(port.name))
					.Select(port => new CapabilityFeaturePortBindingEntryV2
					{
						name = port.name ?? "",
						binding = CloneCapabilityBindingV2(port.binding)
					})
					.ToList(),
				parameters = (entry.parameters ?? new List<CapabilityFeatureParameterEntryV2>())
					.Where(parameter => parameter != null && !string.IsNullOrWhiteSpace(parameter.name))
					.Select(parameter => new CapabilityFeatureParameterBindingEntryV2
					{
						name = parameter.name ?? "",
						binding = CloneCapabilityBindingV2(parameter.binding)
					})
					.ToList()
			})
			.OrderBy(entry => entry.id, StringComparer.OrdinalIgnoreCase)
			.ToList();
	}

	private bool HasAnyFeatureBindingsV2(CapabilityFeatureEntryV2 entry)
	{
		if (entry == null)
		{
			return false;
		}

		return (entry.inputs ?? new List<CapabilityFeaturePortEntryV2>())
				.Any(port => port != null && HasBindingV2(port.binding)) ||
			(entry.outputs ?? new List<CapabilityFeaturePortEntryV2>())
				.Any(port => port != null && HasBindingV2(port.binding)) ||
			(entry.parameters ?? new List<CapabilityFeatureParameterEntryV2>())
				.Any(parameter => parameter != null && HasBindingV2(parameter.binding));
	}

	private bool HasBindingV2(CapabilityFeatureBindingV2 binding)
	{
		return binding != null &&
			(!string.IsNullOrWhiteSpace(binding.componentName) || !string.IsNullOrWhiteSpace(binding.memberName));
	}

	private List<CapabilityFeatureEntryV2> BuildCapabilityFeaturesFromImplementationsV2(List<CapabilityFeatureImplementationEntryV2> implementations)
	{
		List<CapabilityFeatureCatalogEntryV2> catalog = GetCapabilityFeatureCatalogV2();
		List<CapabilityFeatureEntryV2> results = new List<CapabilityFeatureEntryV2>();
		foreach (CapabilityFeatureImplementationEntryV2 implementation in implementations ?? new List<CapabilityFeatureImplementationEntryV2>())
		{
			if (implementation == null || string.IsNullOrWhiteSpace(implementation.id))
			{
				continue;
			}

			CapabilityFeatureCatalogEntryV2 catalogFeature = catalog.FirstOrDefault(feature =>
				feature != null &&
				string.Equals(feature.id, implementation.id, StringComparison.OrdinalIgnoreCase));
			if (catalogFeature == null)
			{
				continue;
			}

			CapabilityFeatureEntryV2 entry = new CapabilityFeatureEntryV2
			{
				id = catalogFeature.id ?? "",
				displayName = string.IsNullOrWhiteSpace(catalogFeature.name) ? catalogFeature.id : catalogFeature.name,
				description = catalogFeature.description ?? "",
				inputs = BuildCapabilityFeaturePortsV2(catalogFeature.inputs),
				outputs = BuildCapabilityFeaturePortsV2(catalogFeature.outputs),
				parameters = BuildCapabilityFeatureParametersV2(catalogFeature.parameters)
			};

			ApplyFeatureBindingsV2(entry.inputs, implementation.inputs);
			ApplyFeatureBindingsV2(entry.outputs, implementation.outputs);
			ApplyFeatureParameterBindingsV2(entry.parameters, implementation.parameters);
			results.Add(entry);
		}

		return CloneCapabilityFeaturesV2(results);
	}

	private void ApplyFeatureBindingsV2(List<CapabilityFeaturePortEntryV2> ports, List<CapabilityFeaturePortBindingEntryV2> bindings)
	{
		foreach (CapabilityFeaturePortEntryV2 port in ports ?? new List<CapabilityFeaturePortEntryV2>())
		{
			if (port == null || string.IsNullOrWhiteSpace(port.name))
			{
				continue;
			}

			CapabilityFeaturePortBindingEntryV2 binding = (bindings ?? new List<CapabilityFeaturePortBindingEntryV2>())
				.FirstOrDefault(entry => entry != null && string.Equals(entry.name, port.name, StringComparison.OrdinalIgnoreCase));
			if (binding != null)
			{
				port.binding = CloneCapabilityBindingV2(binding.binding);
			}
		}
	}

	private void ApplyFeatureParameterBindingsV2(List<CapabilityFeatureParameterEntryV2> parameters, List<CapabilityFeatureParameterBindingEntryV2> bindings)
	{
		foreach (CapabilityFeatureParameterEntryV2 parameter in parameters ?? new List<CapabilityFeatureParameterEntryV2>())
		{
			if (parameter == null || string.IsNullOrWhiteSpace(parameter.name))
			{
				continue;
			}

			CapabilityFeatureParameterBindingEntryV2 binding = (bindings ?? new List<CapabilityFeatureParameterBindingEntryV2>())
				.FirstOrDefault(entry => entry != null && string.Equals(entry.name, parameter.name, StringComparison.OrdinalIgnoreCase));
			if (binding != null)
			{
				parameter.binding = CloneCapabilityBindingV2(binding.binding);
			}
		}
	}

	private CapabilityFeatureBindingV2 CloneCapabilityBindingV2(CapabilityFeatureBindingV2 source)
	{
		return source == null
			? new CapabilityFeatureBindingV2()
			: JsonUtility.FromJson<CapabilityFeatureBindingV2>(JsonUtility.ToJson(source)) ?? new CapabilityFeatureBindingV2();
	}

	private string GetLegacyBindingComponentNameV2(PlyFeatureImplementation implementation, bool isInput, string portName)
	{
		PlyFeaturePortBinding binding = implementation == null
			? null
			: (isInput ? implementation.inputBindings : implementation.outputBindings)?
				.FirstOrDefault(entry =>
					entry != null &&
					string.Equals(isInput ? entry.featureInput : entry.featureOutput, portName, StringComparison.OrdinalIgnoreCase));
		return NormalizeLegacyBindingComponentNameV2(binding != null ? binding.binding : null);
	}

	private string GetLegacyBindingMemberNameV2(PlyFeatureImplementation implementation, bool isInput, string portName)
	{
		PlyFeaturePortBinding binding = implementation == null
			? null
			: (isInput ? implementation.inputBindings : implementation.outputBindings)?
				.FirstOrDefault(entry =>
					entry != null &&
					string.Equals(isInput ? entry.featureInput : entry.featureOutput, portName, StringComparison.OrdinalIgnoreCase));
		return binding != null && binding.binding != null ? binding.binding.memberName ?? "" : "";
	}

	private string GetLegacyParameterBindingComponentNameV2(PlyFeatureImplementation implementation, string parameterName)
	{
		PlyFeatureParameterBinding binding = implementation == null
			? null
			: (implementation.parameterBindings ?? new List<PlyFeatureParameterBinding>())
				.FirstOrDefault(entry => entry != null && string.Equals(entry.featureParameter, parameterName, StringComparison.OrdinalIgnoreCase));
		return NormalizeLegacyBindingComponentNameV2(binding != null ? binding.binding : null);
	}

	private string GetLegacyParameterBindingMemberNameV2(PlyFeatureImplementation implementation, string parameterName)
	{
		PlyFeatureParameterBinding binding = implementation == null
			? null
			: (implementation.parameterBindings ?? new List<PlyFeatureParameterBinding>())
				.FirstOrDefault(entry => entry != null && string.Equals(entry.featureParameter, parameterName, StringComparison.OrdinalIgnoreCase));
		return binding != null && binding.binding != null ? binding.binding.memberName ?? "" : "";
	}

	private string NormalizeLegacyBindingComponentNameV2(PlyFeatureBinding binding)
	{
		if (binding == null || string.IsNullOrWhiteSpace(binding.componentType))
		{
			return "";
		}

		string componentType = binding.componentType.Trim();
		CapabilityComponentEntryV2 component = capabilityComponentsV2.FirstOrDefault(entry =>
			entry != null &&
			(string.Equals(entry.typeName, componentType, StringComparison.OrdinalIgnoreCase) ||
			 string.Equals(entry.id, componentType, StringComparison.OrdinalIgnoreCase)));
		if (component != null)
		{
			return GetCapabilityComponentNameV2(component);
		}

		return GetLeafTypeName(componentType);
	}
}
