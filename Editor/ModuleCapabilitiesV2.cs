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
		public bool export = true;
		public string name = "";
		public string displayName = "";
		public string type = "";
		public string description = "";
		public bool writable;
		public bool userEditable = true;
		public string defaultValue = "";
	}

	[Serializable]
	private class CapabilityMethodEntryV2
	{
		public bool export = true;
		public string name = "";
		public string displayName = "";
		public string declaringType = "";
		public string returnType = "";
		public string description = "";
		public bool isStatic;
	}

	[Serializable]
	private class CapabilityEventEntryV2
	{
		public bool export = true;
		public string name = "";
		public string displayName = "";
		public string payloadType = "";
		public string declaringType = "";
		public string description = "";
		public bool allowedForCodegen = true;
	}

	private readonly string[] capabilityTabsV2 = { "Components", "Features" };
	private readonly string[] capabilityInspectorTabsV2 = { "Component", "Properties", "Methods", "Events" };
	private readonly string[] capabilityCanAddOptionsV2 = { "No", "Yes", "Characters", "Game", "Nature", "Props", "Other" };
	private CapabilityWorkspaceTabV2 activeCapabilityTabV2;
	private CapabilityComponentInspectorTabV2 activeCapabilityInspectorTabV2;
	private Vector2 capabilitiesV2Scroll;
	private Vector2 capabilityComponentTreeScrollV2;
	private Vector2 capabilityComponentInspectorScrollV2;
	private Vector2 capabilityPropertyListScrollV2;
	private Vector2 capabilityPropertyEditorScrollV2;
	private Vector2 capabilityMethodListScrollV2;
	private Vector2 capabilityMethodEditorScrollV2;
	private Vector2 capabilityEventListScrollV2;
	private Vector2 capabilityEventEditorScrollV2;
	private List<CapabilityComponentEntryV2> capabilityComponentsV2 = new List<CapabilityComponentEntryV2>();
	private int selectedCapabilityComponentIndexV2 = -1;
	private int selectedCapabilityPropertyIndexV2 = -1;
	private int selectedCapabilityMethodIndexV2 = -1;
	private int selectedCapabilityEventIndexV2 = -1;

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
	}

	private void DrawCapabilitiesV2ComponentsTreePane()
	{
		EditorGUILayout.BeginHorizontal();
		if (GUILayout.Button("Add From Source", GUILayout.Width(130f)))
		{
			OpenCapabilitiesV2SourceSelector();
		}

		if (GUILayout.Button("Add Custom", GUILayout.Width(110f)))
		{
			AddCustomCapabilityComponentV2();
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

		EditorGUILayout.BeginVertical("box", GUILayout.Width(Mathf.Max(240f, position.width * 0.28f)), GUILayout.ExpandHeight(true));
		GUILayout.Label("Properties", EditorStyles.boldLabel);
		capabilityPropertyListScrollV2 = EditorGUILayout.BeginScrollView(capabilityPropertyListScrollV2, GUILayout.ExpandHeight(true));
		if (entry.properties.Count > 0)
		{
			EditorGUILayout.BeginHorizontal();
			selectedCapabilityPropertyIndexV2 = Mathf.Clamp(selectedCapabilityPropertyIndexV2 < 0 ? 0 : selectedCapabilityPropertyIndexV2, 0, entry.properties.Count - 1);
			EditorGUILayout.EndHorizontal();
			for (int i = 0; i < entry.properties.Count; i++)
			{
				CapabilityPropertyEntryV2 property = entry.properties[i];
				EditorGUILayout.BeginHorizontal();
				property.export = EditorGUILayout.Toggle(property.export, GUILayout.Width(18f));
				if (DrawSelectableListButton(GetCapabilityPropertyLabelV2(property), selectedCapabilityPropertyIndexV2 == i, GUILayout.Height(28f)))
				{
					selectedCapabilityPropertyIndexV2 = i;
				}

				if (GUILayout.Button("X", GUILayout.Width(28f), GUILayout.Height(28f)))
				{
					entry.properties.RemoveAt(i);
					selectedCapabilityPropertyIndexV2 = Mathf.Clamp(selectedCapabilityPropertyIndexV2, 0, entry.properties.Count - 1);
					if (entry.properties.Count == 0)
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
		CapabilityPropertyEntryV2 selectedProperty = selectedCapabilityPropertyIndexV2 >= 0 && selectedCapabilityPropertyIndexV2 < entry.properties.Count
			? entry.properties[selectedCapabilityPropertyIndexV2]
			: null;
		if (selectedProperty != null)
		{
			using (new EditorGUI.DisabledScope(true))
			{
				EditorGUILayout.TextField("Property Name", selectedProperty.name);
			}

			selectedProperty.displayName = EditorGUILayout.TextField("Display Name", selectedProperty.displayName);
			selectedProperty.type = EditorGUILayout.TextField("Type", selectedProperty.type);
			selectedProperty.description = EditorGUILayout.TextField("Description", selectedProperty.description);
			selectedProperty.writable = EditorGUILayout.Toggle("Writable", selectedProperty.writable);
			selectedProperty.userEditable = EditorGUILayout.Toggle("User Editable", selectedProperty.userEditable);
			selectedProperty.defaultValue = EditorGUILayout.TextField("Default", selectedProperty.defaultValue);
			selectedProperty.export = EditorGUILayout.Toggle("Export", selectedProperty.export);
		}
		EditorGUILayout.EndScrollView();
		EditorGUILayout.EndVertical();

		EditorGUILayout.EndHorizontal();
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
				method.export = EditorGUILayout.Toggle(method.export, GUILayout.Width(18f));
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
			selectedMethod.export = EditorGUILayout.Toggle("Export", selectedMethod.export);
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
				eventInfo.export = EditorGUILayout.Toggle(eventInfo.export, GUILayout.Width(18f));
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
			selectedEvent.export = EditorGUILayout.Toggle("Export", selectedEvent.export);
		}
		EditorGUILayout.EndScrollView();
		EditorGUILayout.EndVertical();

		EditorGUILayout.EndHorizontal();
	}

	private CapabilityComponentEntryV2 GetSelectedCapabilityComponentV2()
	{
		if (selectedCapabilityComponentIndexV2 < 0 || selectedCapabilityComponentIndexV2 >= capabilityComponentsV2.Count)
		{
			return null;
		}

		return capabilityComponentsV2[selectedCapabilityComponentIndexV2];
	}

	private void OpenCapabilitiesV2SourceSelector()
	{
		List<string> existingSelection = capabilityComponentsV2
			.Where(entry => entry != null && !entry.isCustom && !string.IsNullOrWhiteSpace(entry.sourcePath))
			.Select(entry => entry.sourcePath)
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
			.ToList();
		List<string> selectedScripts = new List<string>(existingSelection);
		CSharpScriptSelectorWindow.OpenWindow(selectedScripts);
		ProcessSelectedCapabilitySourceFilesV2(selectedScripts);
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
			.Select(BuildCapabilityComponentEntryFromSourceV2)
			.Where(entry => entry != null)
			.ToList();

		capabilityComponentsV2 = retainedCustomEntries
			.Concat(rebuiltSourceEntries)
			.OrderBy(entry => entry.displayName, StringComparer.OrdinalIgnoreCase)
			.ToList();
		selectedCapabilityComponentIndexV2 = capabilityComponentsV2.Count > 0 ? 0 : -1;
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
			.Select(parameter => new CapabilityPropertyEntryV2
			{
				export = true,
				name = parameter.name ?? "",
				displayName = parameter.name ?? "",
				type = parameter.type ?? "",
				description = NormalizeImportedDescriptionV2(parameter.description),
				writable = parameter.required,
				userEditable = parameter.userEditable,
				defaultValue = parameter.@default ?? ""
			})
			.OrderBy(parameter => parameter.name, StringComparer.OrdinalIgnoreCase)
			.ToList();
	}

	private List<CapabilityMethodEntryV2> BuildCapabilityMethodEntriesV2(List<CapabilityMethodInfo> methods)
	{
		return (methods ?? new List<CapabilityMethodInfo>())
			.Where(method => method != null)
			.Select(method => new CapabilityMethodEntryV2
			{
				export = true,
				name = method.name ?? "",
				displayName = method.name ?? "",
				declaringType = method.declaringType ?? "",
				returnType = method.returnType ?? "",
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
				export = true,
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
}
