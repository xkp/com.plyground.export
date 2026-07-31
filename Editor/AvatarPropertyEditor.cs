using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Plastic.Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

public class AvatarPropertyEditor : EditorWindow
{
	private string inputJson = "{\"components\":[]}";
	private Vector2 componentsScroll;
	private List<string> availableComponents = new List<string>();
	private HashSet<string> selectedComponents = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

	private static string resultJson;

	public static string OpenWindow(string jsonString, List<string> availableComponents)
	{
		AvatarPropertyEditor window = GetWindow<AvatarPropertyEditor>("Avatar");
		window.minSize = new Vector2(420f, 360f);
		window.inputJson = string.IsNullOrWhiteSpace(jsonString) ? "{\"components\":[]}" : jsonString;
		window.availableComponents = (availableComponents ?? new List<string>())
			.Where(component => !string.IsNullOrWhiteSpace(component))
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.OrderBy(component => component, StringComparer.OrdinalIgnoreCase)
			.ToList();
		window.selectedComponents = ParseComponentsFromJson(window.inputJson);
		window.ShowModal();
		return resultJson;
	}

	public static int GetComponentCount(string jsonString)
	{
		return ParseComponentsFromJson(jsonString).Count;
	}

	private static HashSet<string> ParseComponentsFromJson(string jsonString)
	{
		HashSet<string> parsedComponents = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		if (string.IsNullOrWhiteSpace(jsonString))
		{
			return parsedComponents;
		}

		try
		{
			JToken token = JToken.Parse(jsonString);
			if (token is JObject avatarObject)
			{
				foreach (JToken componentToken in avatarObject["components"] as JArray ?? new JArray())
				{
					AddComponent(parsedComponents, componentToken?.ToString());
				}

				return parsedComponents;
			}

			if (token is JArray array)
			{
				foreach (JToken componentToken in array)
				{
					AddComponent(parsedComponents, componentToken?.ToString());
				}
			}
		}
		catch (Exception)
		{
		}

		return parsedComponents;
	}

	private static void AddComponent(HashSet<string> components, string componentName)
	{
		if (!string.IsNullOrWhiteSpace(componentName))
		{
			components.Add(componentName);
		}
	}

	private void OnGUI()
	{
		EditorGUILayout.BeginVertical(GUILayout.ExpandHeight(true));

		GUILayout.Space(8f);
		EditorGUILayout.LabelField("Avatar Components", EditorStyles.boldLabel);
		GUILayout.Space(8f);

		if (availableComponents.Count == 0)
		{
			EditorGUILayout.HelpBox("No capability components are available yet. Add components in the Caps tab first.", MessageType.Info);
		}
		else
		{
			componentsScroll = EditorGUILayout.BeginScrollView(componentsScroll, GUILayout.ExpandHeight(true), GUILayout.MinHeight(220f));
			foreach (string componentName in availableComponents)
			{
				bool selected = selectedComponents.Contains(componentName);
				bool nextSelected = EditorGUILayout.ToggleLeft(componentName, selected);
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

		GUILayout.FlexibleSpace();
		GUILayout.Space(16f);
		if (GUILayout.Button("Save", GUILayout.Height(28f)))
		{
			resultJson = new JObject
			{
				{ "components", new JArray(selectedComponents.OrderBy(component => component, StringComparer.OrdinalIgnoreCase)) }
			}.ToString();
			EditorGUIUtility.systemCopyBuffer = resultJson;
			Close();
		}

		EditorGUILayout.EndVertical();
	}
}
