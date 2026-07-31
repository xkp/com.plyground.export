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
		public string type = "";
		public string description = "";
		public bool writable;
		public bool userEditable = true;
		public string defaultValue = "";
		public string featureId = "";
	}

	[Serializable]
	private class CapabilityMethodEntryV2
	{
		public bool export = true;
		public string name = "";
		public string declaringType = "";
		public string returnType = "";
		public string description = "";
		public bool isStatic;
		public bool allowedForCodegen = true;
	}

	[Serializable]
	private class CapabilityEventEntryV2
	{
		public bool export = true;
		public string name = "";
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
	private List<CapabilityComponentEntryV2> capabilityComponentsV2 = new List<CapabilityComponentEntryV2>();
	private int selectedCapabilityComponentIndexV2 = -1;

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
		entry.canAdd = DrawCapabilityCanAddPopupV2(entry.canAdd);
	}

	private void DrawCapabilityPropertyInspectorV2(CapabilityComponentEntryV2 entry)
	{
		for (int i = 0; i < entry.properties.Count; i++)
		{
			CapabilityPropertyEntryV2 property = entry.properties[i];
			EditorGUILayout.BeginVertical("helpbox");
			EditorGUILayout.BeginHorizontal();
			property.export = EditorGUILayout.Toggle(property.export, GUILayout.Width(18f));
			GUILayout.Label(string.IsNullOrWhiteSpace(property.name) ? "Property" : property.name, EditorStyles.boldLabel);
			GUILayout.FlexibleSpace();
			if (GUILayout.Button("X", GUILayout.Width(28f)))
			{
				entry.properties.RemoveAt(i);
				EditorGUILayout.EndHorizontal();
				EditorGUILayout.EndVertical();
				return;
			}
			EditorGUILayout.EndHorizontal();
			property.name = EditorGUILayout.TextField("Name", property.name);
			property.type = EditorGUILayout.TextField("Type", property.type);
			property.description = EditorGUILayout.TextField("Description", property.description);
			property.writable = EditorGUILayout.Toggle("Writable", property.writable);
			property.userEditable = EditorGUILayout.Toggle("User Editable", property.userEditable);
			property.defaultValue = EditorGUILayout.TextField("Default", property.defaultValue);
			property.featureId = EditorGUILayout.TextField("Feature Id", property.featureId);
			EditorGUILayout.EndVertical();
		}
	}

	private void DrawCapabilityMethodInspectorV2(CapabilityComponentEntryV2 entry)
	{
		for (int i = 0; i < entry.methods.Count; i++)
		{
			CapabilityMethodEntryV2 method = entry.methods[i];
			EditorGUILayout.BeginVertical("helpbox");
			EditorGUILayout.BeginHorizontal();
			method.export = EditorGUILayout.Toggle(method.export, GUILayout.Width(18f));
			GUILayout.Label(string.IsNullOrWhiteSpace(method.name) ? "Method" : method.name, EditorStyles.boldLabel);
			GUILayout.FlexibleSpace();
			if (GUILayout.Button("X", GUILayout.Width(28f)))
			{
				entry.methods.RemoveAt(i);
				EditorGUILayout.EndHorizontal();
				EditorGUILayout.EndVertical();
				return;
			}
			EditorGUILayout.EndHorizontal();
			method.name = EditorGUILayout.TextField("Name", method.name);
			method.declaringType = EditorGUILayout.TextField("Declaring Type", method.declaringType);
			method.returnType = EditorGUILayout.TextField("Return Type", method.returnType);
			method.description = EditorGUILayout.TextField("Description", method.description);
			method.isStatic = EditorGUILayout.Toggle("Static", method.isStatic);
			method.allowedForCodegen = EditorGUILayout.Toggle("Codegen", method.allowedForCodegen);
			EditorGUILayout.EndVertical();
		}
	}

	private void DrawCapabilityEventInspectorV2(CapabilityComponentEntryV2 entry)
	{
		for (int i = 0; i < entry.events.Count; i++)
		{
			CapabilityEventEntryV2 eventInfo = entry.events[i];
			EditorGUILayout.BeginVertical("helpbox");
			EditorGUILayout.BeginHorizontal();
			eventInfo.export = EditorGUILayout.Toggle(eventInfo.export, GUILayout.Width(18f));
			GUILayout.Label(string.IsNullOrWhiteSpace(eventInfo.name) ? "Event" : eventInfo.name, EditorStyles.boldLabel);
			GUILayout.FlexibleSpace();
			if (GUILayout.Button("X", GUILayout.Width(28f)))
			{
				entry.events.RemoveAt(i);
				EditorGUILayout.EndHorizontal();
				EditorGUILayout.EndVertical();
				return;
			}
			EditorGUILayout.EndHorizontal();
			eventInfo.name = EditorGUILayout.TextField("Name", eventInfo.name);
			eventInfo.payloadType = EditorGUILayout.TextField("Payload Type", eventInfo.payloadType);
			eventInfo.declaringType = EditorGUILayout.TextField("Declaring Type", eventInfo.declaringType);
			eventInfo.description = EditorGUILayout.TextField("Description", eventInfo.description);
			eventInfo.allowedForCodegen = EditorGUILayout.Toggle("Codegen", eventInfo.allowedForCodegen);
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
			description = componentInfo.description ?? "",
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
				type = parameter.type ?? "",
				description = parameter.description ?? "",
				writable = parameter.required,
				userEditable = parameter.userEditable,
				defaultValue = parameter.@default ?? "",
				featureId = parameter.featureId ?? ""
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
				declaringType = method.declaringType ?? "",
				returnType = method.returnType ?? "",
				description = method.description ?? "",
				isStatic = method.isStatic,
				allowedForCodegen = method.allowedForCodegen
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
				payloadType = eventInfo.payloadType ?? "",
				declaringType = eventInfo.declaringType ?? "",
				description = eventInfo.description ?? "",
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
}
