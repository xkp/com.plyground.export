using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

#if UNITY_6000_0_OR_NEWER
using TreeViewStateType = UnityEditor.IMGUI.Controls.TreeViewState<int>;
using TreeViewType = UnityEditor.IMGUI.Controls.TreeView<int>;
using TreeViewItemType = UnityEditor.IMGUI.Controls.TreeViewItem<int>;
#else
using TreeViewStateType = UnityEditor.IMGUI.Controls.TreeViewState;
using TreeViewType = UnityEditor.IMGUI.Controls.TreeView;
using TreeViewItemType = UnityEditor.IMGUI.Controls.TreeViewItem;
#endif

public class CSharpScriptSelectorWindow : EditorWindow
{
	private TreeViewStateType treeState;
	private ScriptTreeView treeView;
	private List<string> externalSelection;
	private bool shouldApplySelection;

	public static void OpenWindow(List<string> selectedScripts)
	{
		CSharpScriptSelectorWindow window = CreateInstance<CSharpScriptSelectorWindow>();
		window.titleContent = new GUIContent("Select C# Scripts");
		window.minSize = new Vector2(420, 620);
		window.externalSelection = selectedScripts;
		window.ShowModal();
	}

	private void OnEnable()
	{
		if (treeState == null)
		{
			treeState = new TreeViewStateType();
		}

		treeView = new ScriptTreeView(treeState);
		treeView.Reload();
		InitializeSelection();
	}

	private void InitializeSelection()
	{
		if (externalSelection == null || treeView == null)
		{
			return;
		}

		InitializeNode(treeView.Root, treeView.CheckedIds, externalSelection);
	}

	private void InitializeNode(TreeViewItemType node, HashSet<int> checkedIds, List<string> input)
	{
		foreach (TreeViewItemType child in node.children ?? Enumerable.Empty<TreeViewItemType>())
		{
			ScriptTreeView.ScriptItem scriptNode = child as ScriptTreeView.ScriptItem;
			if (scriptNode != null && !scriptNode.isFolder && input.Contains(scriptNode.assetPath))
			{
				checkedIds.Add(scriptNode.id);
			}

			InitializeNode(child, checkedIds, input);
		}
	}

	private void OnGUI()
	{
		GUILayout.Label("Select C# Scripts", EditorStyles.boldLabel);
		EditorGUILayout.HelpBox("Choose the source .cs files that should drive capability component discovery and reflection.", MessageType.Info);
		EditorGUILayout.BeginHorizontal();
		if (GUILayout.Button("Select All", GUILayout.Width(100f)))
		{
			treeView?.SetAllChecked(true);
		}
		if (GUILayout.Button("Clear All", GUILayout.Width(100f)))
		{
			treeView?.SetAllChecked(false);
		}
		GUILayout.FlexibleSpace();
		EditorGUILayout.EndHorizontal();
		Rect treeRect = GUILayoutUtility.GetRect(0, position.width, 0, position.height - 70f);
		treeView.OnGUI(treeRect);

		GUILayout.FlexibleSpace();
		EditorGUILayout.BeginHorizontal();
		if (GUILayout.Button("OK"))
		{
			shouldApplySelection = true;
			ApplySelection();
			Close();
		}

		if (GUILayout.Button("Cancel"))
		{
			Close();
		}
		EditorGUILayout.EndHorizontal();
	}

	private void ApplySelection()
	{
		if (externalSelection == null || treeView == null)
		{
			return;
		}

		externalSelection.Clear();
		AddSelectedFromNode(treeView.Root, treeView.CheckedIds, externalSelection);
	}

	private void AddSelectedFromNode(TreeViewItemType node, HashSet<int> checkedIds, List<string> output)
	{
		foreach (TreeViewItemType child in node.children ?? Enumerable.Empty<TreeViewItemType>())
		{
			ScriptTreeView.ScriptItem scriptNode = child as ScriptTreeView.ScriptItem;
			if (scriptNode != null && !scriptNode.isFolder && checkedIds.Contains(scriptNode.id))
			{
				output.Add(scriptNode.assetPath);
			}

			AddSelectedFromNode(child, checkedIds, output);
		}
	}

	private void OnDisable()
	{
		if (shouldApplySelection)
		{
			ApplySelection();
		}
	}

	private class ScriptTreeView : TreeViewType
	{
		public HashSet<int> CheckedIds = new HashSet<int>();
		public TreeViewItemType Root;

		public class ScriptItem : TreeViewItemType
		{
			public string assetPath;
			public bool isFolder;

			public ScriptItem(int id, int depth, string name, string path, bool folder) : base(id, depth, name)
			{
				assetPath = path;
				isFolder = folder;
			}
		}

		public ScriptTreeView(TreeViewStateType state) : base(state)
		{
			showBorder = true;
			showAlternatingRowBackgrounds = false;
		}

		protected override TreeViewItemType BuildRoot()
		{
			TreeViewItemType root = Root = new TreeViewItemType { id = 0, depth = -1, displayName = "Root" };
			int idCounter = 1;
			string[] guids = AssetDatabase.FindAssets("t:MonoScript", new[] { "Assets" });
			IEnumerable<string> paths = guids
				.Select(AssetDatabase.GUIDToAssetPath)
				.Where(path => path.EndsWith(".cs"))
				.Distinct();
			foreach (string path in paths)
			{
				AddPathNode(path, root, ref idCounter);
			}

			SetupDepthsFromParentsAndChildren(root);
			return root;
		}

		private void AddPathNode(string path, TreeViewItemType parent, ref int idCounter)
		{
			string[] parts = path.Split('/');
			TreeViewItemType currentParent = parent;
			for (int depth = 0; depth < parts.Length; depth++)
			{
				string part = parts[depth];
				TreeViewItemType existing = currentParent.children?.FirstOrDefault(child => child.displayName == part);
				if (existing == null)
				{
					bool isFolder = depth < parts.Length - 1;
					ScriptItem node = new ScriptItem(idCounter++, currentParent.depth + 1, part, isFolder ? null : path, isFolder);
					if (currentParent.children == null)
					{
						currentParent.children = new List<TreeViewItemType>();
					}

					currentParent.AddChild(node);
					currentParent = node;
				}
				else
				{
					currentParent = existing;
				}
			}
		}

		protected override void RowGUI(RowGUIArgs args)
		{
			ScriptItem item = (ScriptItem)args.item;
			Rect rowRect = args.rowRect;
			float indent = GetContentIndent(item);
			Rect toggleRect = new Rect(rowRect.x + indent, rowRect.y, 18f, rowRect.height);

			bool isChecked = CheckedIds.Contains(item.id);
			bool newChecked = EditorGUI.Toggle(toggleRect, isChecked);
			if (newChecked != isChecked)
			{
				SetCheckedRecursive(item, newChecked);
			}

			Rect labelRect = new Rect(toggleRect.x + 18f, rowRect.y, rowRect.width - indent - 18f, rowRect.height);
			EditorGUI.LabelField(labelRect, item.displayName);
		}

		private void SetCheckedRecursive(TreeViewItemType item, bool isChecked)
		{
			if (isChecked)
			{
				CheckedIds.Add(item.id);
			}
			else
			{
				CheckedIds.Remove(item.id);
			}

			if (item.children == null)
			{
				return;
			}

			foreach (ScriptItem child in item.children.Cast<ScriptItem>())
			{
				SetCheckedRecursive(child, isChecked);
			}
		}

		public void SetAllChecked(bool isChecked)
		{
			CheckedIds.Clear();
			if (isChecked && Root != null)
			{
				SetCheckedRecursive(Root, true);
			}

			Repaint();
		}
	}
}
