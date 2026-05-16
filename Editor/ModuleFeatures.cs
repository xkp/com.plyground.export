using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public partial class ModuleExporter
{
    private int selectedFeatureProfileIndex = -1;
    private string featureComponentSearch = "";
    private Vector2 featureListScroll;
    private Vector2 featureEditorScroll;
    private Vector2 featureDiscoveryScroll;
    private List<PlyFeatureValidationIssue> featureValidationIssues = new List<PlyFeatureValidationIssue>();

    private void InitializeFeatureManifest()
    {
        featureManifest = PlyFeatureSchemaUtility.NormalizeManifest(featureManifest);
        featureManifest.moduleId = moduleId ?? "";
    }

    private PlyFeatureManifest PrepareFeatureManifestForPersistence()
    {
        InitializeFeatureManifest();
        featureManifest.moduleId = moduleId ?? "";
        return PlyFeatureSchemaUtility.NormalizeManifest(featureManifest);
    }

    private void DrawFeaturesTab()
    {
        InitializeFeatureManifest();
        featureValidationIssues = ValidateFeatureManifest(featureManifest);

        EditorGUILayout.HelpBox("Create semantic gameplay features from the curated capability components in this module. Feature bindings are limited to components and members defined in the Components subtab.", MessageType.Info);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Refresh Reflection Cache", GUILayout.Width(170f)))
        {
            PlyFeatureTypeCache.Refresh();
        }

        if (GUILayout.Button("Import JSON", GUILayout.Width(100f)))
        {
            ImportFeatureManifestFromJson();
        }

        if (GUILayout.Button("Export JSON", GUILayout.Width(100f)))
        {
            ExportFeatureManifestToJson();
        }

        if (GUILayout.Button("Add Feature", GUILayout.Width(100f)))
        {
            AddFeatureProfile();
        }

        if (GUILayout.Button("Add Guard AI Example", GUILayout.Width(160f)))
        {
            AddGuardAiExampleProfile();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.LabelField("Reflection Cache", string.IsNullOrWhiteSpace(PlyFeatureTypeCache.Snapshot.generatedAtUtc)
            ? "Not scanned yet"
            : PlyFeatureTypeCache.Snapshot.generatedAtUtc, EditorStyles.miniLabel);

        if (featureValidationIssues.Count > 0)
        {
            int errorCount = featureValidationIssues.Count(issue => string.Equals(issue.severity, "error", StringComparison.OrdinalIgnoreCase));
            EditorGUILayout.HelpBox($"Validation found {featureValidationIssues.Count} issue(s), including {errorCount} error(s).", errorCount > 0 ? MessageType.Warning : MessageType.Info);
        }
        EditorGUILayout.Space(6f);
        DrawFeatureWorkspace();
        EditorGUILayout.Space(6f);
        DrawFeatureValidationSection();
    }

    private void DrawFeatureWorkspace()
    {
        InitializeFeatureManifest();
        featureManifest.features ??= new List<PlyFeatureProfile>();

        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.BeginVertical("box", GUILayout.Width(Mathf.Max(250f, position.width * 0.24f)), GUILayout.Height(620f));
        GUILayout.Label("Feature Profiles", EditorStyles.boldLabel);
        featureListScroll = EditorGUILayout.BeginScrollView(featureListScroll);
        if (featureManifest.features.Count == 0)
        {
            EditorGUILayout.HelpBox("No feature profiles yet.", MessageType.Info);
        }

        for (int i = 0; i < featureManifest.features.Count; i++)
        {
            PlyFeatureProfile profile = featureManifest.features[i];
            string label = string.IsNullOrWhiteSpace(profile.name) ? (string.IsNullOrWhiteSpace(profile.id) ? "New Feature" : profile.id) : profile.name;
            if (GUILayout.Button(label, selectedFeatureProfileIndex == i ? EditorStyles.toolbarButton : GUI.skin.button, GUILayout.Height(30f)))
            {
                selectedFeatureProfileIndex = i;
            }
        }
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();

        EditorGUILayout.BeginVertical("box", GUILayout.ExpandWidth(true), GUILayout.Height(620f));
        if (featureManifest.features.Count == 0)
        {
            EditorGUILayout.HelpBox("Create or import a feature profile to begin mapping components, members, and semantic capabilities.", MessageType.Info);
        }
        else
        {
            selectedFeatureProfileIndex = Mathf.Clamp(selectedFeatureProfileIndex < 0 ? 0 : selectedFeatureProfileIndex, 0, featureManifest.features.Count - 1);
            featureEditorScroll = EditorGUILayout.BeginScrollView(featureEditorScroll);
            DrawFeatureProfileEditor(featureManifest.features[selectedFeatureProfileIndex]);
            EditorGUILayout.EndScrollView();
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.BeginVertical("box", GUILayout.Width(Mathf.Max(280f, position.width * 0.26f)), GUILayout.Height(620f));
        GUILayout.Label("Available Components", EditorStyles.boldLabel);
        featureComponentSearch = EditorGUILayout.TextField("Search", featureComponentSearch);
        featureDiscoveryScroll = EditorGUILayout.BeginScrollView(featureDiscoveryScroll);
        List<UnityCapabilityComponentInfo> components = GetAvailableFeatureComponents(featureComponentSearch);
        if (components.Count == 0)
        {
            EditorGUILayout.HelpBox("No curated capability components matched the current search. Add or infer components in the Components subtab first.", MessageType.Info);
        }

        foreach (UnityCapabilityComponentInfo component in components.Take(200))
        {
            EditorGUILayout.BeginVertical("helpbox");
            EditorGUILayout.LabelField(component.typeName, EditorStyles.miniBoldLabel);
            EditorGUILayout.LabelField(component.componentId, EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.LabelField($"Methods: {(component.methods ?? new List<CapabilityMethodInfo>()).Count}  Events: {(component.events ?? new List<CapabilityEventInfo>()).Count}  Fields: {(component.parameters ?? new List<CapabilityParameterInfo>()).Count}", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();
    }

    private void DrawFeatureProfileEditor(PlyFeatureProfile profile)
    {
        profile = PlyFeatureSchemaUtility.NormalizeFeature(profile);

        GUILayout.Label("Feature Profile", EditorStyles.boldLabel);
        profile.id = EditorGUILayout.TextField("Id", profile.id);
        profile.name = EditorGUILayout.TextField("Name", profile.name);
        GUILayout.Label("Description", EditorStyles.miniBoldLabel);
        profile.description = EditorGUILayout.TextArea(profile.description, GUILayout.MinHeight(40f));
        GUILayout.Label("AI Match Description", EditorStyles.miniBoldLabel);
        profile.aiMatchDescription = EditorGUILayout.TextArea(profile.aiMatchDescription, GUILayout.MinHeight(40f));

        DrawStringListEditor("Tags", profile.tags);
        DrawStringListEditor("Categories", profile.categories);
        DrawStringListEditor("Implements", profile.implements);
        DrawStringListEditor("Provides", profile.provides);
        DrawStringListEditor("Consumes", profile.consumes);
        DrawStringListEditor("Target Roles", profile.targetRoles);
        DrawFeatureComponentRequirements(profile);
        DrawFeaturePorts(profile);
        DrawFeatureParameters(profile);

        EditorGUILayout.Space(8f);
        if (GUILayout.Button("Remove Feature Profile", GUILayout.Width(170f)))
        {
            featureManifest.features.Remove(profile);
            selectedFeatureProfileIndex = Mathf.Clamp(selectedFeatureProfileIndex - 1, -1, featureManifest.features.Count - 1);
        }
    }

    private void DrawFeatureComponentRequirements(PlyFeatureProfile profile)
    {
        GUILayout.Label("Component Requirements", EditorStyles.boldLabel);
        if (GUILayout.Button("Add Component Requirement", GUILayout.Width(180f)))
        {
            profile.componentRequirements.Add(new PlyFeatureComponentRequirement());
        }

        if (profile.componentRequirements.Count == 0)
        {
            EditorGUILayout.HelpBox("Choose from the module's curated capability components.", MessageType.Info);
            return;
        }

        for (int i = 0; i < profile.componentRequirements.Count; i++)
        {
            PlyFeatureComponentRequirement requirement = profile.componentRequirements[i];
            EditorGUILayout.BeginVertical("helpbox");
            DrawComponentRequirementPicker(requirement, "Component");
            requirement.required = EditorGUILayout.Toggle("Required", requirement.required);
            if (GUILayout.Button("Remove Requirement", GUILayout.Width(140f)))
            {
                profile.componentRequirements.RemoveAt(i);
                i--;
            }
            EditorGUILayout.EndVertical();
        }
    }

    private void DrawFeaturePorts(PlyFeatureProfile profile)
    {
        GUILayout.Label("Port Mappings", EditorStyles.boldLabel);
        if (GUILayout.Button("Add Port", GUILayout.Width(90f)))
        {
            profile.ports.Add(new PlyFeaturePortMapping
            {
                kind = PlyFeaturePortKind.Value
            });
        }

        for (int i = 0; i < profile.ports.Count; i++)
        {
            PlyFeaturePortMapping port = profile.ports[i];
            port.binding ??= new PlyFeatureBinding();
            port.kind = PlyFeaturePortKind.Value;
            EditorGUILayout.BeginVertical("helpbox");
            port.name = EditorGUILayout.TextField("Name", port.name);
            port.direction = (PlyFeaturePortDirection)EditorGUILayout.EnumPopup("Direction", port.direction);
            port.dataType = (PlyFeatureDataType)EditorGUILayout.EnumPopup("Data Type", port.dataType);
            DrawBindingEditor(profile, port.binding, port.kind, port.direction);
            if (GUILayout.Button("Remove Port", GUILayout.Width(110f)))
            {
                profile.ports.RemoveAt(i);
                i--;
            }
            EditorGUILayout.EndVertical();
        }
    }

    private void DrawFeatureParameters(PlyFeatureProfile profile)
    {
        GUILayout.Label("Parameter Mappings", EditorStyles.boldLabel);
        if (GUILayout.Button("Add Parameter", GUILayout.Width(110f)))
        {
            profile.parameters.Add(new PlyFeatureParameterMapping());
        }

        for (int i = 0; i < profile.parameters.Count; i++)
        {
            PlyFeatureParameterMapping parameter = profile.parameters[i];
            parameter.binding ??= new PlyFeatureBinding();
            EditorGUILayout.BeginVertical("helpbox");
            parameter.name = EditorGUILayout.TextField("Name", parameter.name);
            parameter.type = (PlyFeatureDataType)EditorGUILayout.EnumPopup("Type", parameter.type);
            parameter.defaultValue = EditorGUILayout.TextField("Default Value", parameter.defaultValue);
            DrawBindingEditor(profile, parameter.binding, PlyFeaturePortKind.Value, PlyFeaturePortDirection.Input);
            parameter.binding.access = (PlyFeatureParameterAccess)EditorGUILayout.EnumPopup("Access", parameter.binding.access);
            if (GUILayout.Button("Remove Parameter", GUILayout.Width(130f)))
            {
                profile.parameters.RemoveAt(i);
                i--;
            }
            EditorGUILayout.EndVertical();
        }
    }

    private void DrawBindingEditor(PlyFeatureProfile profile, PlyFeatureBinding binding, PlyFeaturePortKind kind, PlyFeaturePortDirection direction)
    {
        binding = PlyFeatureSchemaUtility.NormalizeBinding(binding);
        DrawComponentTypePopup(profile, binding);

        PlyFeatureMemberKind[] allowedKinds = GetAllowedBindingKinds(kind, direction);
        binding.memberKind = DrawMemberKindPopup(binding.memberKind, allowedKinds);
        PlyFeatureComponentRequirement requirement = (profile.componentRequirements ?? new List<PlyFeatureComponentRequirement>())
            .FirstOrDefault(entry => string.Equals(entry.typeName, binding.componentType, StringComparison.OrdinalIgnoreCase));
        List<PlyFeatureMemberDescriptor> members = GetFeatureMembers(requirement != null ? requirement.typeName : string.Empty, allowedKinds);
        string[] memberOptions = members.Count == 0 ? new[] { "<none>" } : members.Select(member => member.displayName).ToArray();
        int selectedIndex = Mathf.Max(0, members.FindIndex(member => string.Equals(member.memberName, binding.memberName, StringComparison.Ordinal)));
        int newIndex = EditorGUILayout.Popup("Member", selectedIndex < 0 ? 0 : selectedIndex, memberOptions);
        if (members.Count > 0 && newIndex >= 0 && newIndex < members.Count)
        {
            PlyFeatureMemberDescriptor selected = members[newIndex];
            binding.memberName = selected.memberName;
            binding.memberKind = selected.memberKind;
            binding.isStatic = selected.isStatic;
            if (kind == PlyFeaturePortKind.Value)
            {
                binding.access = selected.access;
            }
        }
        else
        {
            binding.memberName = "";
        }

        binding.isStatic = EditorGUILayout.Toggle("Static", binding.isStatic);
    }

    private void DrawComponentRequirementPicker(PlyFeatureComponentRequirement requirement, string label)
    {
        List<UnityCapabilityComponentInfo> components = GetAvailableFeatureComponents();
        string[] options = components.Count == 0 ? new[] { "<none>" } : components.Select(component => component.typeName).ToArray();
        int selectedIndex = Mathf.Max(0, components.FindIndex(component =>
            string.Equals(component.typeName, requirement.typeName, StringComparison.OrdinalIgnoreCase)));
        int newIndex = EditorGUILayout.Popup(label, selectedIndex < 0 ? 0 : selectedIndex, options);
        if (components.Count > 0 && newIndex >= 0 && newIndex < components.Count)
        {
            UnityCapabilityComponentInfo component = components[newIndex];
            requirement.typeName = component.typeName;
            requirement.assemblyQualifiedName = string.Empty;
        }
    }

    private void DrawComponentTypePopup(PlyFeatureProfile profile, PlyFeatureBinding binding)
    {
        List<PlyFeatureComponentRequirement> requirements = profile.componentRequirements ?? new List<PlyFeatureComponentRequirement>();
        string[] options = requirements.Count == 0
            ? new[] { "<no component requirements>" }
            : requirements.Select(requirement => requirement.typeName).ToArray();
        int selectedIndex = Mathf.Max(0, requirements.FindIndex(requirement => string.Equals(requirement.typeName, binding.componentType, StringComparison.OrdinalIgnoreCase)));
        int newIndex = EditorGUILayout.Popup("Component Type", selectedIndex < 0 ? 0 : selectedIndex, options);
        if (requirements.Count > 0 && newIndex >= 0 && newIndex < requirements.Count)
        {
            binding.componentType = requirements[newIndex].typeName;
        }
        else if (requirements.Count == 0)
        {
            binding.componentType = "";
        }
    }

    private PlyFeatureMemberKind DrawMemberKindPopup(PlyFeatureMemberKind current, PlyFeatureMemberKind[] allowedKinds)
    {
        string[] options = allowedKinds.Select(kind => kind.ToString()).ToArray();
        int currentIndex = Array.IndexOf(allowedKinds, current);
        if (currentIndex < 0)
        {
            currentIndex = 0;
        }

        int newIndex = EditorGUILayout.Popup("Member Kind", currentIndex, options);
        return allowedKinds.Length == 0 ? current : allowedKinds[newIndex];
    }

    private PlyFeatureMemberKind[] GetAllowedBindingKinds(PlyFeaturePortKind kind, PlyFeaturePortDirection direction)
    {
        return new[] { PlyFeatureMemberKind.Property };
    }

    private void DrawFeatureValidationSection()
    {
        GUILayout.Label("Validation", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");
        if (featureValidationIssues.Count == 0)
        {
            EditorGUILayout.HelpBox("No feature validation issues found.", MessageType.Info);
        }
        else
        {
            foreach (PlyFeatureValidationIssue issue in featureValidationIssues)
            {
                MessageType messageType = string.Equals(issue.severity, "error", StringComparison.OrdinalIgnoreCase)
                    ? MessageType.Error
                    : MessageType.Warning;
                EditorGUILayout.HelpBox((string.IsNullOrWhiteSpace(issue.path) ? "" : issue.path + ": ") + issue.message, messageType);
            }
        }
        EditorGUILayout.EndVertical();
    }

    private void AddFeatureProfile()
    {
        InitializeFeatureManifest();
        featureManifest.features.Add(new PlyFeatureProfile
        {
            id = "feature." + Guid.NewGuid().ToString("N").Substring(0, 8),
            name = "New Feature"
        });
        selectedFeatureProfileIndex = featureManifest.features.Count - 1;
    }

    private void AddGuardAiExampleProfile()
    {
        InitializeFeatureManifest();
        PlyFeatureProfile example = PlyFeatureJson.Import(@"
{
  ""schemaVersion"": ""1.0"",
  ""moduleId"": """",
  ""features"": [
    {
      ""id"": ""thirdparty.guard_ai.profile"",
      ""name"": ""Guard AI"",
      ""description"": ""Maps existing guard AI systems into Plyground semantic gameplay features."",
      ""aiMatchDescription"": ""guard ai, enemy aggression, alert enemy, hostile npc, attack player"",
      ""tags"": [""ai"", ""enemy"", ""combat""],
      ""categories"": [""AI""],
      ""implements"": [""enemy_aggression""],
      ""provides"": [""aggression_control""],
      ""consumes"": [""spotted_state""],
      ""targetRoles"": [""Enemy""],
      ""componentRequirements"": [
        {
          ""typeName"": ""GuardAI"",
          ""assemblyQualifiedName"": """",
          ""required"": true
        }
      ],
      ""ports"": [],
      ""parameters"": [
        {
          ""name"": ""AggroRadius"",
          ""type"": ""float"",
          ""defaultValue"": 20,
          ""binding"": {
            ""componentType"": ""GuardAI"",
            ""memberKind"": ""property"",
            ""memberName"": ""aggroRadius"",
            ""access"": ""readWrite""
          }
        }
      ]
    }
  ]
}").features.FirstOrDefault();
        if (example != null)
        {
            featureManifest.features.Add(example);
            selectedFeatureProfileIndex = featureManifest.features.Count - 1;
        }
    }

    private void ImportFeatureManifestFromJson()
    {
        string filePath = EditorUtility.OpenFilePanel("Import Feature Manifest", Application.dataPath, "json");
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return;
        }

        try
        {
            featureManifest = PlyFeatureJson.ImportFromFile(filePath);
            featureManifest.moduleId = moduleId ?? "";
            selectedFeatureProfileIndex = featureManifest.features.Count > 0 ? 0 : -1;
        }
        catch (Exception exception)
        {
            EditorUtility.DisplayDialog("Import Failed", exception.Message, "OK");
        }
    }

    private void ExportFeatureManifestToJson()
    {
        string filePath = EditorUtility.SaveFilePanel("Export Feature Manifest", Application.dataPath, "features.json", "json");
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return;
        }

        try
        {
            PlyFeatureJson.ExportToFile(PrepareFeatureManifestForPersistence(), filePath);
            EditorUtility.RevealInFinder(filePath);
        }
        catch (Exception exception)
        {
            EditorUtility.DisplayDialog("Export Failed", exception.Message, "OK");
        }
    }

    private void ExportFeatureManifestToModuleFolder(string moduleFolder)
    {
        if (string.IsNullOrWhiteSpace(moduleFolder))
        {
            return;
        }

        string featureFolder = Path.Combine(moduleFolder, "Plyground");
        Directory.CreateDirectory(featureFolder);
        PlyFeatureJson.ExportToFile(PrepareFeatureManifestForPersistence(), Path.Combine(featureFolder, "features.json"));
    }

    private List<PlyFeatureValidationIssue> ValidateFeatureManifest(PlyFeatureManifest manifest)
    {
        List<PlyFeatureValidationIssue> issues = new List<PlyFeatureValidationIssue>();
        manifest = PlyFeatureSchemaUtility.NormalizeManifest(manifest);

        HashSet<string> ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < manifest.features.Count; i++)
        {
            PlyFeatureProfile profile = manifest.features[i];
            string path = "Feature[" + i + "]";
            if (string.IsNullOrWhiteSpace(profile.id))
            {
                issues.Add(CreateIssue("error", path, "Feature id is required."));
            }
            else if (!ids.Add(profile.id))
            {
                issues.Add(CreateIssue("error", path, "Feature id must be unique."));
            }

            if (string.IsNullOrWhiteSpace(profile.name))
            {
                issues.Add(CreateIssue("error", path, "Feature name is required."));
            }

            if (profile.componentRequirements.Count == 0)
            {
                issues.Add(CreateIssue("warning", path, "Feature has no component requirements."));
            }

            foreach (PlyFeatureComponentRequirement requirement in profile.componentRequirements)
            {
                if (string.IsNullOrWhiteSpace(requirement.typeName))
                {
                    issues.Add(CreateIssue("error", path, "Component requirement is missing a type name."));
                }
                else if (FindAvailableFeatureComponent(requirement.typeName) == null)
                {
                    issues.Add(CreateIssue("warning", path, "Required component type '" + requirement.typeName + "' was not found in the Components subtab."));
                }
            }

            for (int portIndex = 0; portIndex < profile.ports.Count; portIndex++)
            {
                ValidateBinding(profile, profile.ports[portIndex].binding, path + ".ports[" + portIndex + "]", issues);
            }

            for (int parameterIndex = 0; parameterIndex < profile.parameters.Count; parameterIndex++)
            {
                ValidateBinding(profile, profile.parameters[parameterIndex].binding, path + ".parameters[" + parameterIndex + "]", issues);
            }
        }

        return issues;
    }

    private void ValidateBinding(PlyFeatureProfile profile, PlyFeatureBinding binding, string path, List<PlyFeatureValidationIssue> issues)
    {
        if (binding == null)
        {
            issues.Add(CreateIssue("error", path, "Binding is required."));
            return;
        }

        if (string.IsNullOrWhiteSpace(binding.componentType))
        {
            issues.Add(CreateIssue("error", path, "Binding component type is required."));
            return;
        }

        PlyFeatureComponentRequirement requirement = (profile.componentRequirements ?? new List<PlyFeatureComponentRequirement>())
            .FirstOrDefault(entry => string.Equals(entry.typeName, binding.componentType, StringComparison.OrdinalIgnoreCase));
        if (requirement == null)
        {
            issues.Add(CreateIssue("error", path, "Binding component type must match one of the feature's component requirements."));
            return;
        }

        List<PlyFeatureMemberDescriptor> members = GetFeatureMembers(requirement.typeName);
        PlyFeatureMemberDescriptor member = members.FirstOrDefault(entry =>
            entry.memberKind == binding.memberKind &&
            string.Equals(entry.memberName, binding.memberName, StringComparison.Ordinal));
        if (member == null)
        {
            issues.Add(CreateIssue("warning", path, "Binding member '" + binding.memberName + "' was not found on component '" + binding.componentType + "'."));
        }
    }

    private List<UnityCapabilityComponentInfo> GetAvailableFeatureComponents(string searchTerm = "")
    {
        moduleCapabilities ??= new CapabilityManifest();
        moduleCapabilities.unity ??= new CapabilityUnityInfo();
        List<UnityCapabilityComponentInfo> components = moduleCapabilities.unity.components ?? new List<UnityCapabilityComponentInfo>();
        IEnumerable<UnityCapabilityComponentInfo> query = components.Where(component => component != null && !string.IsNullOrWhiteSpace(component.typeName));
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            string needle = searchTerm.Trim();
            query = query.Where(component =>
                ContainsFeatureSearch(component.typeName, needle) ||
                ContainsFeatureSearch(component.componentId, needle) ||
                ContainsFeatureSearch(component.description, needle));
        }

        return query
            .OrderBy(component => component.typeName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private UnityCapabilityComponentInfo FindAvailableFeatureComponent(string typeName)
    {
        return GetAvailableFeatureComponents().FirstOrDefault(component =>
            string.Equals(component.typeName, typeName, StringComparison.OrdinalIgnoreCase));
    }

    private List<PlyFeatureMemberDescriptor> GetFeatureMembers(string componentType, params PlyFeatureMemberKind[] allowedKinds)
    {
        UnityCapabilityComponentInfo component = FindAvailableFeatureComponent(componentType);
        if (component == null)
        {
            return new List<PlyFeatureMemberDescriptor>();
        }

        HashSet<PlyFeatureMemberKind> allowed = allowedKinds == null || allowedKinds.Length == 0
            ? null
            : new HashSet<PlyFeatureMemberKind>(allowedKinds);
        List<PlyFeatureMemberDescriptor> members = new List<PlyFeatureMemberDescriptor>();

        foreach (CapabilityParameterInfo parameter in component.parameters ?? new List<CapabilityParameterInfo>())
        {
            AddFeatureMemberIfAllowed(members, allowed, new PlyFeatureMemberDescriptor
            {
                componentTypeName = component.typeName,
                memberName = parameter.name,
                displayName = parameter.name + " : " + parameter.type,
                memberKind = PlyFeatureMemberKind.Property,
                dataType = ParseFeatureDataType(parameter.type),
                access = parameter.required ? PlyFeatureParameterAccess.ReadWrite : PlyFeatureParameterAccess.ReadOnly
            });
        }

        return members
            .Where(member => !string.IsNullOrWhiteSpace(member.memberName))
            .OrderBy(member => member.memberKind.ToString())
            .ThenBy(member => member.memberName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private void AddFeatureMemberIfAllowed(List<PlyFeatureMemberDescriptor> members, HashSet<PlyFeatureMemberKind> allowed, PlyFeatureMemberDescriptor member)
    {
        if (member == null)
        {
            return;
        }

        if (allowed != null && !allowed.Contains(member.memberKind))
        {
            return;
        }

        bool exists = members.Any(existing =>
            existing.memberKind == member.memberKind &&
            string.Equals(existing.memberName, member.memberName, StringComparison.Ordinal));
        if (!exists)
        {
            members.Add(member);
        }
    }

    private PlyFeatureDataType ParseFeatureDataType(string rawType)
    {
        if (string.IsNullOrWhiteSpace(rawType))
        {
            return PlyFeatureDataType.Any;
        }

        string normalized = rawType.Trim();
        switch (normalized.ToLowerInvariant())
        {
            case "void":
                return PlyFeatureDataType.Void;
            case "bool":
            case "boolean":
                return PlyFeatureDataType.Bool;
            case "float":
            case "double":
            case "single":
                return PlyFeatureDataType.Float;
            case "int":
            case "int32":
            case "long":
                return PlyFeatureDataType.Int;
            case "string":
                return PlyFeatureDataType.String;
            case "gameobject":
                return PlyFeatureDataType.GameObject;
            case "vector3":
                return PlyFeatureDataType.Vector3;
            default:
                return PlyFeatureDataType.Any;
        }
    }

    private bool ContainsFeatureSearch(string haystack, string needle)
    {
        return !string.IsNullOrWhiteSpace(haystack) &&
            haystack.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private PlyFeatureValidationIssue CreateIssue(string severity, string path, string message)
    {
        return new PlyFeatureValidationIssue
        {
            severity = severity,
            path = path,
            message = message
        };
    }
}
