using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public partial class ModuleExporter
{
    private enum CharacterEditorCatalogTab
    {
        Characters,
        Appearance,
        Equipment
    }

    private readonly string[] characterEditorCatalogTabs = { "Characters", "Appearance", "Equipment" };
    private CharacterEditorCatalogTab activeCharacterEditorCatalogTab;

    [Serializable]
    public class CharacterEditorCatalog
    {
        [NonSerialized] public bool enabled;
        public List<string> capabilities = new List<string> { "transform" };
        public List<CharacterEditorCatalogCharacter> characters = new List<CharacterEditorCatalogCharacter>();
        public List<CharacterEditorCatalogItem> equipment = new List<CharacterEditorCatalogItem>();
        public List<CharacterEditorCatalogItem> clothing = new List<CharacterEditorCatalogItem>();
    }

    [Serializable]
    public class CharacterEditorCatalogExport
    {
        public string schemaVersion = "plyground.character-catalog/v2";
        public List<string> capabilities = new List<string>();
        public List<CharacterEditorCatalogCharacter> characters = new List<CharacterEditorCatalogCharacter>();
        public List<CharacterEditorCatalogItem> equipment = new List<CharacterEditorCatalogItem>();
        public List<CharacterEditorCatalogItem> clothing = new List<CharacterEditorCatalogItem>();
    }

    [Serializable]
    public class CharacterEditorCatalogItem
    {
        public string id = "";
        public string displayName = "";
        public string slot = "";
        public string itemId = "";
        public string sourceAssetPath = "";
        public string attachmentBone = "";
        public string exclusiveGroup = "";
        public List<string> incompatibleItemIds = new List<string>();
        public List<string> conflictTags = new List<string>();
        public List<string> compatibleBodyTypes = new List<string>();
    }

    [Serializable]
    public class CharacterEditorCatalogCharacter
    {
        public string id = "";
        public string displayName = "";
        public string itemId = "";
        public string sourceAssetPath = "";
        public string bodyType = "";
    }

    private void DrawCharacterEditorCatalogTab()
    {
        characterEditorCatalog ??= new CharacterEditorCatalog();
        EditorGUILayout.HelpBox(
            "Declare only visual choices this module supports. The WebGL character editor receives this catalog verbatim: " +
            "no equipment or clothing entries means that control is not displayed.",
            MessageType.Info);

        characterEditorCatalog.enabled = EditorGUILayout.Toggle("Provide character editor catalog", characterEditorCatalog.enabled);
        if (!characterEditorCatalog.enabled)
        {
            EditorGUILayout.HelpBox("This module exports no character-editor capability.", MessageType.None);
            return;
        }

        DrawCharacterEditorCapabilities();
        EditorGUILayout.Space(8f);
        activeCharacterEditorCatalogTab = (CharacterEditorCatalogTab)GUILayout.Toolbar(
            (int)activeCharacterEditorCatalogTab,
            characterEditorCatalogTabs);
        EditorGUILayout.Space(6f);
        switch (activeCharacterEditorCatalogTab)
        {
            case CharacterEditorCatalogTab.Characters:
                DrawCharacterEditorCharacters();
                break;
            case CharacterEditorCatalogTab.Appearance:
                DrawCharacterEditorCatalogList("Clothing", characterEditorCatalog.clothing, false);
                break;
            case CharacterEditorCatalogTab.Equipment:
                DrawCharacterEditorCatalogList("Equipment", characterEditorCatalog.equipment, true);
                break;
        }

        string validation = GetCharacterEditorCatalogValidationError();
        if (!string.IsNullOrEmpty(validation)) EditorGUILayout.HelpBox(validation, MessageType.Error);
    }

    private void DrawCharacterEditorCapabilities()
    {
        EditorGUILayout.LabelField("BASE CAPABILITIES", EditorStyles.boldLabel);
        DrawCharacterEditorCapabilityToggle("transform", "Allow world transform");
        DrawCharacterEditorCapabilityToggle("collision", "Allow collision editing");
        EditorGUILayout.HelpBox(
            "Equipment and clothing capabilities are automatically exported only when their respective lists contain valid entries.",
            MessageType.None);
    }

    private void DrawCharacterEditorCharacters()
    {
        characterEditorCatalog.characters ??= new List<CharacterEditorCatalogCharacter>();
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("CHARACTER BODIES", EditorStyles.boldLabel);
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Add skinned mesh", GUILayout.Width(130f))) AddCharacterEditorCharacter();
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.HelpBox(
            "Pick a skinned-mesh prefab or imported model from this Unity project. It is included directly in this module's AssetBundle. Body type filters appearance and equipment from selected modules.",
            MessageType.None);
        if (characterEditorCatalog.characters.Count == 0)
            EditorGUILayout.LabelField("No character bodies. This module can still contribute equipment or appearance to another body.", EditorStyles.miniLabel);

        for (int index = 0; index < characterEditorCatalog.characters.Count; index++)
        {
            CharacterEditorCatalogCharacter entry = characterEditorCatalog.characters[index] ?? new CharacterEditorCatalogCharacter();
            characterEditorCatalog.characters[index] = entry;
            EditorGUILayout.BeginVertical("helpbox");
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(string.IsNullOrWhiteSpace(entry.displayName) ? "New character body" : entry.displayName, EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Remove", GUILayout.Width(70f))) { characterEditorCatalog.characters.RemoveAt(index); EditorGUILayout.EndHorizontal(); EditorGUILayout.EndVertical(); break; }
            EditorGUILayout.EndHorizontal();
            entry.id = EditorGUILayout.TextField("Character ID", entry.id);
            entry.displayName = EditorGUILayout.TextField("Display name", entry.displayName);
            entry.sourceAssetPath = EditorGUILayout.TextField("Prefab asset path", entry.sourceAssetPath);
            entry.itemId = EditorGUILayout.TextField("Module item ID", entry.itemId);
            entry.bodyType = EditorGUILayout.TextField("Body type", entry.bodyType);
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndVertical();
    }

    private void DrawCharacterEditorCapabilityToggle(string capability, string label)
    {
        bool enabled = characterEditorCatalog.capabilities.Any(value => string.Equals(value, capability, StringComparison.OrdinalIgnoreCase));
        bool next = EditorGUILayout.Toggle(label, enabled);
        if (next == enabled) return;
        characterEditorCatalog.capabilities.RemoveAll(value => string.Equals(value, capability, StringComparison.OrdinalIgnoreCase));
        if (next) characterEditorCatalog.capabilities.Add(capability);
    }

    private void DrawCharacterEditorCatalogList(string label, List<CharacterEditorCatalogItem> entries, bool isEquipment)
    {
        entries ??= new List<CharacterEditorCatalogItem>();
        if (isEquipment) characterEditorCatalog.equipment = entries;
        else characterEditorCatalog.clothing = entries;

        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(label.ToUpperInvariant(), EditorStyles.boldLabel);
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Add module item", GUILayout.Width(130f))) AddCharacterEditorCatalogItem(entries, isEquipment);
        EditorGUILayout.EndHorizontal();

        if (entries.Count == 0)
        {
            EditorGUILayout.LabelField($"No {label.ToLowerInvariant()} entries. This control will be hidden.", EditorStyles.miniLabel);
        }

        for (int index = 0; index < entries.Count; index++)
        {
            CharacterEditorCatalogItem entry = entries[index] ?? new CharacterEditorCatalogItem();
            entries[index] = entry;
            EditorGUILayout.BeginVertical("helpbox");
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(string.IsNullOrWhiteSpace(entry.displayName) ? "New entry" : entry.displayName, EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Remove", GUILayout.Width(70f))) { entries.RemoveAt(index); EditorGUILayout.EndHorizontal(); EditorGUILayout.EndVertical(); break; }
            EditorGUILayout.EndHorizontal();

            entry.id = EditorGUILayout.TextField("Catalog ID", entry.id);
            entry.displayName = EditorGUILayout.TextField("Display name", entry.displayName);
            entry.slot = EditorGUILayout.TextField("Slot", entry.slot);
            entry.sourceAssetPath = EditorGUILayout.TextField("Prefab asset path", entry.sourceAssetPath);
            entry.itemId = EditorGUILayout.TextField("Module item ID", entry.itemId);
            if (isEquipment) entry.attachmentBone = EditorGUILayout.TextField("Attachment bone", entry.attachmentBone);
            entry.exclusiveGroup = EditorGUILayout.TextField("Exclusive group", entry.exclusiveGroup);
            DrawCharacterEditorStringList("Compatible body types", entry.compatibleBodyTypes);
            DrawCharacterEditorStringList("Incompatible catalog IDs", entry.incompatibleItemIds);
            DrawCharacterEditorStringList("Conflict tags", entry.conflictTags);
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndVertical();
    }

    private void AddCharacterEditorCatalogItem(List<CharacterEditorCatalogItem> entries, bool isEquipment)
    {
        List<Item> candidates = itemGroups.Where(group => group != null)
            .SelectMany(group => group.items ?? new List<Item>())
            .Where(item => item != null && !string.IsNullOrWhiteSpace(item.id) && !string.IsNullOrWhiteSpace(item.prefabPath))
            .ToList();
        if (candidates.Count == 0)
        {
            EditorUtility.DisplayDialog("Character Editor", "Add a module item with a prefab asset path before adding it to this catalog.", "OK");
            return;
        }

        GenericMenu menu = new GenericMenu();
        foreach (Item item in candidates)
        {
            Item selected = item;
            menu.AddItem(new GUIContent(selected.name + "  [" + selected.id + "]"), false, () => {
                entries.Add(new CharacterEditorCatalogItem
                {
                    id = selected.id,
                    itemId = selected.id,
                    displayName = selected.name,
                    sourceAssetPath = selected.prefabPath,
                    slot = isEquipment ? "right-hand" : "body",
                    attachmentBone = isEquipment ? "RightHand" : "",
                    exclusiveGroup = isEquipment ? "right-hand" : ""
                });
                Repaint();
            });
        }
        menu.ShowAsContext();
    }

    private void AddCharacterEditorCharacter()
    {
        List<string> selectedPaths = new List<string>();
        AssetSelectorWindow.OpenWindow(selectedPaths);
        List<string> skinnedMeshPaths = selectedPaths
            .Where(IsSkinnedMeshAssetPath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (skinnedMeshPaths.Count == 0)
        {
            EditorUtility.DisplayDialog("Character Editor", "Select a prefab or imported model containing a SkinnedMeshRenderer.", "OK");
            return;
        }

        foreach (string path in skinnedMeshPaths)
        {
            string displayName = System.IO.Path.GetFileNameWithoutExtension(path);
            string id = displayName;
            int suffix = 2;
            while (characterEditorCatalog.characters.Any(entry => entry != null && string.Equals(entry.id, id, StringComparison.OrdinalIgnoreCase)))
                id = displayName + "-" + suffix++;
            characterEditorCatalog.characters.Add(new CharacterEditorCatalogCharacter
            {
                id = id,
                itemId = id,
                displayName = displayName,
                sourceAssetPath = path,
                bodyType = "humanoid"
            });
        }
        Repaint();
    }

    private static bool IsSkinnedMeshAssetPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return false;
        GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        return asset != null && asset.GetComponentInChildren<SkinnedMeshRenderer>(true) != null;
    }

    private IEnumerable<string> GetCharacterEditorBodyAssetPaths()
    {
        return (characterEditorCatalog?.characters ?? new List<CharacterEditorCatalogCharacter>())
            .Where(entry => entry != null && !string.IsNullOrWhiteSpace(entry.sourceAssetPath))
            .Select(entry => entry.sourceAssetPath.Trim())
            .Where(IsSkinnedMeshAssetPath)
            .Distinct(StringComparer.OrdinalIgnoreCase);
    }

    private static void DrawCharacterEditorStringList(string label, List<string> values)
    {
        values ??= new List<string>();
        string current = string.Join(", ", values.Where(value => !string.IsNullOrWhiteSpace(value)));
        string edited = EditorGUILayout.TextField(label, current);
        if (edited == current) return;
        values.Clear();
        values.AddRange(edited.Split(',').Select(value => value.Trim()).Where(value => value.Length > 0));
    }

    private ExportedModuleMetadata BuildCharacterEditorMetadata()
    {
        if (characterEditorCatalog == null || !characterEditorCatalog.enabled) return null;
        string validation = GetCharacterEditorCatalogValidationError();
        if (!string.IsNullOrEmpty(validation)) throw new InvalidOperationException(validation);

        CharacterEditorCatalogExport catalog = new CharacterEditorCatalogExport
        {
            capabilities = DistinctCharacterEditorStrings(characterEditorCatalog.capabilities),
            characters = CloneCharacterEditorCharacters(characterEditorCatalog.characters),
            equipment = CloneCharacterEditorEntries(characterEditorCatalog.equipment),
            clothing = CloneCharacterEditorEntries(characterEditorCatalog.clothing)
        };
        if (catalog.equipment.Count > 0) catalog.capabilities.Add("equipment");
        if (catalog.clothing.Count > 0) catalog.capabilities.Add("clothing");
        if (catalog.characters.Count > 0 || catalog.clothing.Count > 0) catalog.capabilities.Add("appearance");
        catalog.capabilities = DistinctCharacterEditorStrings(catalog.capabilities);
        return new ExportedModuleMetadata { characterEditor = catalog };
    }

    private void LoadCharacterEditorCatalog(ExportedModuleMetadata metadata)
    {
        CharacterEditorCatalogExport imported = metadata?.characterEditor;
        if (imported == null)
        {
            characterEditorCatalog = new CharacterEditorCatalog();
            return;
        }
        characterEditorCatalog = new CharacterEditorCatalog
        {
            enabled = true,
            capabilities = DistinctCharacterEditorStrings(imported.capabilities),
            characters = CloneCharacterEditorCharacters(imported.characters),
            equipment = CloneCharacterEditorEntries(imported.equipment),
            clothing = CloneCharacterEditorEntries(imported.clothing)
        };
        characterEditorCatalog.capabilities.RemoveAll(value => value == "equipment" || value == "clothing");
    }

    private string GetCharacterEditorCatalogValidationError()
    {
        if (characterEditorCatalog == null || !characterEditorCatalog.enabled) return "";
        return ValidateCharacterEditorEntries(characterEditorCatalog.equipment, "equipment")
            ?? ValidateCharacterEditorEntries(characterEditorCatalog.clothing, "clothing")
            ?? ValidateCharacterEditorCharacters(characterEditorCatalog.characters)
            ?? "";
    }

    private static string ValidateCharacterEditorCharacters(List<CharacterEditorCatalogCharacter> entries)
    {
        HashSet<string> ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (CharacterEditorCatalogCharacter entry in entries ?? new List<CharacterEditorCatalogCharacter>())
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.id)) return "Each character body needs a character ID.";
            if (!ids.Add(entry.id.Trim())) return "Duplicate character body ID: " + entry.id;
            if (string.IsNullOrWhiteSpace(entry.sourceAssetPath)) return entry.id + " needs a prefab asset path.";
            if (string.IsNullOrWhiteSpace(entry.bodyType)) return entry.id + " needs a body type.";
        }
        return null;
    }

    private static string ValidateCharacterEditorEntries(List<CharacterEditorCatalogItem> entries, string label)
    {
        HashSet<string> ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (CharacterEditorCatalogItem entry in entries ?? new List<CharacterEditorCatalogItem>())
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.id)) return "Each " + label + " entry needs a catalog ID.";
            if (!ids.Add(entry.id.Trim())) return "Duplicate " + label + " catalog ID: " + entry.id;
            if (string.IsNullOrWhiteSpace(entry.sourceAssetPath)) return entry.id + " needs a prefab asset path.";
        }
        return null;
    }

    private static List<string> DistinctCharacterEditorStrings(IEnumerable<string> values)
    {
        return (values ?? Enumerable.Empty<string>()).Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static List<CharacterEditorCatalogItem> CloneCharacterEditorEntries(IEnumerable<CharacterEditorCatalogItem> entries)
    {
        return (entries ?? Enumerable.Empty<CharacterEditorCatalogItem>()).Where(entry => entry != null).Select(entry => new CharacterEditorCatalogItem
        {
            id = entry.id ?? "",
            displayName = entry.displayName ?? "",
            slot = entry.slot ?? "",
            itemId = entry.itemId ?? "",
            sourceAssetPath = entry.sourceAssetPath ?? "",
            attachmentBone = entry.attachmentBone ?? "",
            exclusiveGroup = entry.exclusiveGroup ?? "",
            incompatibleItemIds = DistinctCharacterEditorStrings(entry.incompatibleItemIds),
            conflictTags = DistinctCharacterEditorStrings(entry.conflictTags),
            compatibleBodyTypes = DistinctCharacterEditorStrings(entry.compatibleBodyTypes)
        }).ToList();
    }

    private static List<CharacterEditorCatalogCharacter> CloneCharacterEditorCharacters(IEnumerable<CharacterEditorCatalogCharacter> entries)
    {
        return (entries ?? Enumerable.Empty<CharacterEditorCatalogCharacter>()).Where(entry => entry != null).Select(entry => new CharacterEditorCatalogCharacter
        {
            id = entry.id ?? "",
            displayName = entry.displayName ?? "",
            itemId = entry.itemId ?? "",
            sourceAssetPath = entry.sourceAssetPath ?? "",
            bodyType = entry.bodyType ?? ""
        }).ToList();
    }
}
