using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Plastic.Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

public class RolePropertyEditor : EditorWindow
{
	private string inputJson = "{\"name\":\"\",\"components\":[]}";
	private string roleName = "";
	private Vector2 componentScroll;
	private List<string> availableComponents = new List<string>();
	private HashSet<string> selectedComponents = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

	private static string resultJson;

	public static string OpenWindow(string jsonString, List<string> availableComponents)
	{
		RolePropertyEditor window = GetWindow<RolePropertyEditor>("Role");
		window.inputJson = string.IsNullOrWhiteSpace(jsonString) ? window.inputJson : jsonString;
		window.availableComponents = (availableComponents ?? new List<string>())
			.Where(component => !string.IsNullOrWhiteSpace(component))
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.OrderBy(component => component, StringComparer.OrdinalIgnoreCase)
			.ToList();
		window.selectedComponents = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		window.roleName = "";
		window.ParseInput();
		window.ShowModal();
		return resultJson;
	}

	private void ParseInput()
	{
		try
		{
			JObject parsedJson = JObject.Parse(inputJson);
			roleName = parsedJson["name"]?.ToString() ?? "";
			foreach (JToken componentToken in parsedJson["components"] as JArray ?? new JArray())
			{
				string componentName = componentToken?.ToString();
				if (!string.IsNullOrWhiteSpace(componentName))
				{
					selectedComponents.Add(componentName);
				}
			}
		}
		catch (Exception)
		{
			roleName = "";
			selectedComponents.Clear();
		}
	}

	private void OnGUI()
	{
		GUILayout.Space(8f);
		EditorGUILayout.LabelField("Role Name");
		roleName = EditorGUILayout.TextField(roleName);

		GUILayout.Space(10f);
		EditorGUILayout.LabelField("Components");

		if (availableComponents.Count == 0)
		{
			EditorGUILayout.HelpBox("No capability components are available yet. Add components in the Caps tab first.", MessageType.Info);
		}
		else
		{
			componentScroll = EditorGUILayout.BeginScrollView(componentScroll, GUILayout.MinHeight(180f));
			foreach (string componentName in availableComponents)
			{
				bool isSelected = selectedComponents.Contains(componentName);
				bool nextSelected = EditorGUILayout.ToggleLeft(componentName, isSelected);
				if (nextSelected)
				{
					selectedComponents.Add(componentName);
				}
				else
				{
					selectedComponents.Remove(componentName);
				}
			}
			EditorGUILayout.EndScrollView();
		}

		GUILayout.Space(16f);
		if (GUILayout.Button("Accept Values"))
		{
			JObject outputJson = new JObject
			{
				{ "name", roleName ?? "" },
				{ "components", new JArray(selectedComponents.OrderBy(component => component, StringComparer.OrdinalIgnoreCase)) }
			};

			resultJson = outputJson.ToString();
			EditorGUIUtility.systemCopyBuffer = resultJson;
			Close();
		}
	}
}
