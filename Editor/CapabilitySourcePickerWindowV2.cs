using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class CapabilitySourcePickerWindowV2 : EditorWindow
{
	private readonly List<string> selectedPaths = new List<string>();
	private List<string> availablePaths = new List<string>();
	private Vector2 scrollPosition;
	private Action<List<string>> onApply;
	private string searchTerm = "";

	public static void Open(List<string> initialSelection, Action<List<string>> onApplySelection)
	{
		CapabilitySourcePickerWindowV2 window = CreateInstance<CapabilitySourcePickerWindowV2>();
		window.titleContent = new GUIContent("Add From Project");
		window.minSize = new Vector2(520f, 640f);
		window.selectedPaths.Clear();
		if (initialSelection != null)
		{
			window.selectedPaths.AddRange(initialSelection.Where(path => !string.IsNullOrWhiteSpace(path)).Distinct(StringComparer.OrdinalIgnoreCase));
		}

		window.onApply = onApplySelection;
		window.RefreshAvailablePaths();
		window.ShowModal();
	}

	private void RefreshAvailablePaths()
	{
		availablePaths = AssetDatabase.FindAssets("t:MonoScript", new[] { "Assets" })
			.Select(AssetDatabase.GUIDToAssetPath)
			.Where(path => path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
			.ToList();
	}

	private void OnGUI()
	{
		EditorGUILayout.BeginHorizontal();
		GUILayout.Label("Search", GUILayout.Width(46f));
		searchTerm = EditorGUILayout.TextField(searchTerm ?? "");
		EditorGUILayout.EndHorizontal();

		EditorGUILayout.Space(6f);
		EditorGUILayout.BeginHorizontal();
		if (GUILayout.Button("Select All", GUILayout.Width(100f)))
		{
			selectedPaths.Clear();
			selectedPaths.AddRange(GetFilteredPaths());
		}

		if (GUILayout.Button("Clear All", GUILayout.Width(100f)))
		{
			selectedPaths.Clear();
		}
		EditorGUILayout.EndHorizontal();

		EditorGUILayout.Space(6f);
		scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
		foreach (string path in GetFilteredPaths())
		{
			bool isSelected = selectedPaths.Contains(path);
			bool nextSelected = EditorGUILayout.ToggleLeft(path, isSelected);
			if (nextSelected == isSelected)
			{
				continue;
			}

			if (nextSelected)
			{
				selectedPaths.Add(path);
			}
			else
			{
				selectedPaths.RemoveAll(existing => string.Equals(existing, path, StringComparison.OrdinalIgnoreCase));
			}
		}
		EditorGUILayout.EndScrollView();

		EditorGUILayout.Space(8f);
		EditorGUILayout.BeginHorizontal();
		if (GUILayout.Button("OK"))
		{
			onApply?.Invoke(selectedPaths
				.Where(path => !string.IsNullOrWhiteSpace(path))
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
				.ToList());
			Close();
		}

		if (GUILayout.Button("Cancel"))
		{
			Close();
		}
		EditorGUILayout.EndHorizontal();
	}

	private List<string> GetFilteredPaths()
	{
		if (string.IsNullOrWhiteSpace(searchTerm))
		{
			return availablePaths;
		}

		string needle = searchTerm.Trim();
		return availablePaths
			.Where(path => path.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0)
			.ToList();
	}
}
