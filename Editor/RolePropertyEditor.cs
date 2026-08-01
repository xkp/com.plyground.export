using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Plastic.Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

public class RolePropertyEditor : EditorWindow
{
	private class RoleDefinition
	{
		public string name = "";
		public string behaviorUrl = "";
		public HashSet<string> components = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
	}

	private string inputJson = "[]";
	private Vector2 rolesScroll;
	private List<string> availableComponents = new List<string>();
	private List<RoleDefinition> roles = new List<RoleDefinition>();
	private Dictionary<int, bool> roleFoldouts = new Dictionary<int, bool>();

	private static string resultJson;

	public static string OpenWindow(string jsonString, List<string> availableComponents)
	{
		RolePropertyEditor window = GetWindow<RolePropertyEditor>("Roles");
		window.minSize = new Vector2(460f, 420f);
		window.inputJson = string.IsNullOrWhiteSpace(jsonString) ? "[]" : jsonString;
		window.availableComponents = (availableComponents ?? new List<string>())
			.Where(component => !string.IsNullOrWhiteSpace(component))
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.OrderBy(component => component, StringComparer.OrdinalIgnoreCase)
			.ToList();
		window.roles = window.ParseRoles(window.inputJson);
		window.roleFoldouts = new Dictionary<int, bool>();
		window.ShowModal();
		return resultJson;
	}

	public static int GetRoleCount(string jsonString)
	{
		return ParseRolesFromJson(jsonString).Count;
	}

	private List<RoleDefinition> ParseRoles(string jsonString)
	{
		return ParseRolesFromJson(jsonString);
	}

	private static List<RoleDefinition> ParseRolesFromJson(string jsonString)
	{
		List<RoleDefinition> parsedRoles = new List<RoleDefinition>();
		if (string.IsNullOrWhiteSpace(jsonString))
		{
			return parsedRoles;
		}

		try
		{
			JToken token = JToken.Parse(jsonString);
			if (token is JObject singleRoleObject)
			{
				parsedRoles.Add(ParseRole(singleRoleObject));
				return parsedRoles;
			}

			if (token is JArray array)
			{
				foreach (JObject roleObject in array.OfType<JObject>())
				{
					parsedRoles.Add(ParseRole(roleObject));
				}
			}
		}
		catch (Exception)
		{
		}

		return parsedRoles;
	}

	private static RoleDefinition ParseRole(JObject roleObject)
	{
		RoleDefinition role = new RoleDefinition
		{
			name = roleObject["name"]?.ToString() ?? "",
			behaviorUrl = roleObject["behaviorUrl"]?.ToString() ?? ""
		};

		foreach (JToken componentToken in roleObject["components"] as JArray ?? new JArray())
		{
			string componentName = componentToken?.ToString();
			if (!string.IsNullOrWhiteSpace(componentName))
			{
				role.components.Add(componentName);
			}
		}

		return role;
	}

	private void OnGUI()
	{
		EditorGUILayout.BeginVertical(GUILayout.ExpandHeight(true));

		GUILayout.Space(8f);
		EditorGUILayout.BeginHorizontal();
		EditorGUILayout.LabelField("Roles", EditorStyles.boldLabel);
		GUILayout.FlexibleSpace();
		if (GUILayout.Button("Add Role", GUILayout.Width(90f)))
		{
			roles.Add(new RoleDefinition());
		}
		EditorGUILayout.EndHorizontal();

		GUILayout.Space(8f);
		if (roles.Count == 0)
		{
			EditorGUILayout.HelpBox("No roles yet. Add a role to assign capability components.", MessageType.Info);
		}
		else
		{
			rolesScroll = EditorGUILayout.BeginScrollView(rolesScroll, GUILayout.ExpandHeight(true), GUILayout.MinHeight(220f));
			for (int i = 0; i < roles.Count; i++)
			{
				DrawRoleEditor(i, roles[i]);
			}
			EditorGUILayout.EndScrollView();
		}

		GUILayout.FlexibleSpace();
		GUILayout.Space(16f);
		if (GUILayout.Button("Save", GUILayout.Height(28f)))
		{
			JArray output = new JArray();
			foreach (RoleDefinition role in roles)
			{
				output.Add(new JObject
				{
					{ "name", role.name ?? "" },
					{ "behaviorUrl", role.behaviorUrl ?? "" },
					{ "components", new JArray(role.components.OrderBy(component => component, StringComparer.OrdinalIgnoreCase)) }
				});
			}

			resultJson = output.ToString();
			EditorGUIUtility.systemCopyBuffer = resultJson;
			Close();
		}

		EditorGUILayout.EndVertical();
	}

	private void DrawRoleEditor(int index, RoleDefinition role)
	{
		if (!roleFoldouts.ContainsKey(index))
		{
			roleFoldouts[index] = true;
		}

		EditorGUILayout.BeginVertical("box");
		EditorGUILayout.BeginHorizontal();
		string label = string.IsNullOrWhiteSpace(role.name) ? $"Role {index + 1}" : role.name;
		roleFoldouts[index] = EditorGUILayout.Foldout(roleFoldouts[index], label, true);
		GUILayout.FlexibleSpace();
		if (GUILayout.Button("Remove", GUILayout.Width(70f)))
		{
			roles.RemoveAt(index);
			roleFoldouts.Remove(index);
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.EndVertical();
			GUIUtility.ExitGUI();
			return;
		}
		EditorGUILayout.EndHorizontal();

		if (roleFoldouts[index])
		{
			role.name = EditorGUILayout.TextField("Name", role.name);
			role.behaviorUrl = EditorGUILayout.TextField("Behavior URL", role.behaviorUrl);

			GUILayout.Space(6f);
			EditorGUILayout.LabelField("Components");
			if (availableComponents.Count == 0)
			{
				EditorGUILayout.HelpBox("No capability components are available yet. Add components in the Caps tab first.", MessageType.Info);
			}
			else
			{
				foreach (string componentName in availableComponents)
				{
					string displayName = GetComponentDisplayName(componentName);
					bool selected = role.components.Contains(componentName) || role.components.Contains(displayName);
					bool nextSelected = EditorGUILayout.ToggleLeft(displayName, selected);
					if (nextSelected)
					{
						role.components.Add(componentName);
						role.components.Remove(displayName);
					}
					else
					{
						role.components.Remove(componentName);
						role.components.Remove(displayName);
					}
				}
			}
		}

		EditorGUILayout.EndVertical();
	}

	private static string GetComponentDisplayName(string componentName)
	{
		if (string.IsNullOrWhiteSpace(componentName))
		{
			return "";
		}

		int lastDot = componentName.LastIndexOf('.');
		return lastDot >= 0 && lastDot < componentName.Length - 1
			? componentName.Substring(lastDot + 1)
			: componentName;
	}
}
