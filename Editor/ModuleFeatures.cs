using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public partial class ModuleExporter
{
    private class AvailableFeatureDefinition
    {
        public string id;
        public string name;
        public string description;
        public string aiMatchDescription;
        public List<string> tags = new List<string>();
        public List<string> categories = new List<string>();
        public List<string> provides = new List<string>();
        public List<string> consumes = new List<string>();
        public List<string> targetRoles = new List<string>();
        public List<PlyFeatureComponentRequirement> componentRequirements = new List<PlyFeatureComponentRequirement>();
        public List<PlyFeaturePortMapping> ports = new List<PlyFeaturePortMapping>();
        public List<PlyFeatureParameterMapping> parameters = new List<PlyFeatureParameterMapping>();
    }

    private PlyFeatureManifest FeatureManifestState
    {
        get
        {
            if (featureManifest is PlyFeatureManifest manifest)
            {
                return manifest;
            }

            PlyFeatureManifest created = new PlyFeatureManifest();
            featureManifest = created;
            return created;
        }
        set
        {
            featureManifest = value;
        }
    }

    private int selectedFeatureProfileIndex = -1;
    private int selectedFeatureCatalogIndex = -1;
    private string featureComponentSearch = "";
    private Vector2 featureListScroll;
    private Vector2 featureDetailsScroll;
    private Vector2 featureEditorScroll;
    private List<PlyFeatureValidationIssue> featureValidationIssues = new List<PlyFeatureValidationIssue>();

    private void InitializeFeatureManifest()
    {
        FeatureManifestState = PlyFeatureSchemaUtility.NormalizeManifest(FeatureManifestState);
        FeatureManifestState.moduleId = moduleId ?? "";
    }

    private PlyFeatureManifest PrepareFeatureManifestForPersistence()
    {
        InitializeFeatureManifest();
        FeatureManifestState.moduleId = moduleId ?? "";
        return PlyFeatureSchemaUtility.NormalizeManifest(FeatureManifestState);
    }

    private void DrawFeaturesTab()
    {
        InitializeFeatureManifest();
        featureValidationIssues = ValidateFeatureManifest(FeatureManifestState);

        EditorGUILayout.HelpBox("Select an available semantic feature and implement it locally using either an adapter component or direct bindings to this module's curated components.", MessageType.Info);

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

        if (GUILayout.Button("Add Example Implementation", GUILayout.Width(170f)))
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
        FeatureManifestState.features ??= new List<PlyFeatureProfile>();
        List<AvailableFeatureDefinition> catalog = GetAvailableFeatureCatalog();
        selectedFeatureCatalogIndex = Mathf.Clamp(selectedFeatureCatalogIndex < 0 ? 0 : selectedFeatureCatalogIndex, 0, Mathf.Max(0, catalog.Count - 1));
        AvailableFeatureDefinition selectedFeature = catalog.Count > 0 ? catalog[selectedFeatureCatalogIndex] : null;
        PlyFeatureProfile implementation = selectedFeature != null ? FindFeatureImplementation(selectedFeature.id) : null;

        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.BeginVertical("box", GUILayout.Width(Mathf.Max(250f, position.width * 0.24f)), GUILayout.Height(620f));
        GUILayout.Label("Available Features", EditorStyles.boldLabel);
        featureListScroll = EditorGUILayout.BeginScrollView(featureListScroll);
        if (catalog.Count == 0)
        {
            EditorGUILayout.HelpBox("No available semantic features were found.", MessageType.Info);
        }

        for (int i = 0; i < catalog.Count; i++)
        {
            AvailableFeatureDefinition feature = catalog[i];
            bool implemented = FindFeatureImplementation(feature.id) != null;
            string label = feature.name + (implemented ? " [Implemented]" : "");
            if (GUILayout.Button(label, selectedFeatureCatalogIndex == i ? EditorStyles.toolbarButton : GUI.skin.button, GUILayout.Height(30f)))
            {
                selectedFeatureCatalogIndex = i;
            }
        }
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();

        EditorGUILayout.BeginVertical("box", GUILayout.Width(Mathf.Max(300f, position.width * 0.32f)), GUILayout.Height(620f));
        if (selectedFeature == null)
        {
            EditorGUILayout.HelpBox("Select a semantic feature to review its definition.", MessageType.Info);
        }
        else
        {
            featureDetailsScroll = EditorGUILayout.BeginScrollView(featureDetailsScroll);
            DrawSelectedFeatureDefinition(selectedFeature, implementation != null);
            EditorGUILayout.EndScrollView();
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.BeginVertical("box", GUILayout.ExpandWidth(true), GUILayout.Height(620f));
        if (selectedFeature == null)
        {
            EditorGUILayout.HelpBox("Select a semantic feature to configure its local implementation.", MessageType.Info);
        }
        else
        {
            featureEditorScroll = EditorGUILayout.BeginScrollView(featureEditorScroll);
            DrawFeatureImplementationEditor(selectedFeature, implementation);
            EditorGUILayout.EndScrollView();
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();
    }

    private void DrawSelectedFeatureDefinition(AvailableFeatureDefinition feature, bool implemented)
    {
        GUILayout.Label("Selected Feature", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Name", feature.name);
        EditorGUILayout.LabelField("Feature Id", feature.id);
        EditorGUILayout.LabelField("Status", implemented ? "Implemented in this module" : "Not implemented");
        GUILayout.Label("Description", EditorStyles.miniBoldLabel);
        EditorGUILayout.HelpBox(feature.description, MessageType.None);
        EditorGUILayout.LabelField("Categories", string.Join(", ", feature.categories.ToArray()));
        EditorGUILayout.LabelField("Tags", string.Join(", ", feature.tags.ToArray()));
        EditorGUILayout.LabelField("Provides", string.Join(", ", feature.provides.ToArray()));
        EditorGUILayout.LabelField("Consumes", string.Join(", ", feature.consumes.ToArray()));
        EditorGUILayout.LabelField("Target Roles", string.Join(", ", feature.targetRoles.ToArray()));
        EditorGUILayout.Space(8f);
        GUILayout.Label("Required Bindings", EditorStyles.boldLabel);

        if (feature.componentRequirements.Count > 0)
        {
            GUILayout.Label("Components", EditorStyles.miniBoldLabel);
            foreach (PlyFeatureComponentRequirement requirement in feature.componentRequirements)
            {
                EditorGUILayout.LabelField((requirement.required ? "Required" : "Optional") + ": component", requirement.typeName);
            }
        }

        if (feature.ports.Count > 0)
        {
            GUILayout.Label("Inputs / Outputs", EditorStyles.miniBoldLabel);
            foreach (PlyFeaturePortMapping port in feature.ports)
            {
                EditorGUILayout.LabelField(port.name, $"{port.direction} {port.kind} {port.dataType}");
            }
        }

        if (feature.parameters.Count > 0)
        {
            GUILayout.Label("Parameters", EditorStyles.miniBoldLabel);
            foreach (PlyFeatureParameterMapping parameter in feature.parameters)
            {
                EditorGUILayout.LabelField(parameter.name, $"{parameter.type} default={parameter.defaultValue}");
            }
        }
    }

    private void DrawFeatureImplementationEditor(AvailableFeatureDefinition feature, PlyFeatureProfile profile)
    {
        GUILayout.Label("Implementation", EditorStyles.boldLabel);
        EditorGUILayout.Space(8f);

        if (profile == null)
        {
            EditorGUILayout.HelpBox("This feature is not implemented in the current module yet.", MessageType.Info);
            if (GUILayout.Button("Implement Feature", GUILayout.Width(150f)))
            {
                CreateFeatureImplementation(feature);
            }
            return;
        }

        profile = PlyFeatureSchemaUtility.NormalizeFeature(profile);
        profile.id = EditorGUILayout.TextField("Implementation Id", profile.id);
        profile.useAdapterComponent = EditorGUILayout.Toggle("Use Adapter Component", profile.useAdapterComponent);

        if (profile.useAdapterComponent)
        {
            DrawAdapterComponentPopup(profile);
        }
        else
        {
            featureComponentSearch = EditorGUILayout.TextField("Component Search", featureComponentSearch);
            DrawFeatureComponentRequirements(profile);
            DrawFeaturePorts(profile);
            DrawFeatureParameters(profile);
        }

        EditorGUILayout.Space(8f);
        if (GUILayout.Button("Remove Implementation", GUILayout.Width(170f)))
        {
            FeatureManifestState.features.Remove(profile);
        }
    }

    private void DrawFeatureComponentRequirements(PlyFeatureProfile profile)
    {
        GUILayout.Label("Component Requirements", EditorStyles.boldLabel);
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

    private void DrawAdapterComponentPopup(PlyFeatureProfile profile)
    {
        List<UnityCapabilityComponentInfo> components = GetAvailableFeatureComponents(featureComponentSearch);
        string[] options = components.Count == 0 ? new[] { "<none>" } : components.Select(component => component.typeName).ToArray();
        int selectedIndex = Mathf.Max(0, components.FindIndex(component => string.Equals(component.typeName, profile.adapterComponentType, StringComparison.OrdinalIgnoreCase)));
        int newIndex = EditorGUILayout.Popup("Adapter Component", selectedIndex < 0 ? 0 : selectedIndex, options);
        if (components.Count > 0 && newIndex >= 0 && newIndex < components.Count)
        {
            profile.adapterComponentType = components[newIndex].typeName;
        }
    }

    private void DrawFeaturePorts(PlyFeatureProfile profile)
    {
        GUILayout.Label("Port Mappings", EditorStyles.boldLabel);
        for (int i = 0; i < profile.ports.Count; i++)
        {
            PlyFeaturePortMapping port = profile.ports[i];
            port.binding ??= new PlyFeatureBinding();
            EditorGUILayout.BeginVertical("helpbox");
            port.name = EditorGUILayout.TextField("Name", port.name);
            port.direction = (PlyFeaturePortDirection)EditorGUILayout.EnumPopup("Direction", port.direction);
            port.kind = (PlyFeaturePortKind)EditorGUILayout.EnumPopup("Kind", port.kind);
            port.dataType = (PlyFeatureDataType)EditorGUILayout.EnumPopup("Data Type", port.dataType);
            DrawBindingEditor(profile, port.binding, port.kind, port.direction);
            EditorGUILayout.EndVertical();
        }
    }

    private void DrawFeatureParameters(PlyFeatureProfile profile)
    {
        GUILayout.Label("Parameter Mappings", EditorStyles.boldLabel);
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
        List<UnityCapabilityComponentInfo> components = GetAvailableFeatureComponents(featureComponentSearch);
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
        CreateFeatureImplementation(GetAvailableFeatureCatalog().FirstOrDefault());
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
      ""featureId"": ""enemy_aggression"",
      ""name"": ""Guard AI"",
      ""description"": ""Maps existing guard AI systems into Plyground semantic gameplay features."",
      ""aiMatchDescription"": ""guard ai, enemy aggression, alert enemy, hostile npc, attack player"",
      ""tags"": [""ai"", ""enemy"", ""combat""],
      ""categories"": [""AI""],
      ""implements"": [""enemy_aggression""],
      ""provides"": [""aggression_control""],
      ""consumes"": [""spotted_state""],
      ""targetRoles"": [""Enemy""],
      ""useAdapterComponent"": false,
      ""adapterComponentType"": """",
      ""componentRequirements"": [
        {
          ""typeName"": ""GuardAI"",
          ""assemblyQualifiedName"": """",
          ""required"": true
        }
      ],
      ""ports"": [
        {
          ""name"": ""IsAggressive"",
          ""direction"": ""input"",
          ""kind"": ""value"",
          ""dataType"": ""bool"",
          ""binding"": {
            ""componentType"": ""GuardAI"",
            ""memberKind"": ""property"",
            ""memberName"": ""IsAggressive"",
            ""access"": ""readWrite""
          }
        },
        {
          ""name"": ""HasTargetInSight"",
          ""direction"": ""output"",
          ""kind"": ""value"",
          ""dataType"": ""bool"",
          ""binding"": {
            ""componentType"": ""GuardAI"",
            ""memberKind"": ""property"",
            ""memberName"": ""HasTargetInSight"",
            ""access"": ""readOnly""
          }
        }
      ],
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
            ReplaceFeatureImplementation(example);
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
            FeatureManifestState = PlyFeatureJson.ImportFromFile(filePath);
            FeatureManifestState.moduleId = moduleId ?? "";
            selectedFeatureProfileIndex = FeatureManifestState.features.Count > 0 ? 0 : -1;
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
            if (string.IsNullOrWhiteSpace(profile.featureId))
            {
                issues.Add(CreateIssue("error", path, "Feature catalog id is required."));
            }
            if (string.IsNullOrWhiteSpace(profile.id))
            {
                issues.Add(CreateIssue("error", path, "Feature id is required."));
            }
            else if (!ids.Add(profile.featureId))
            {
                issues.Add(CreateIssue("error", path, "Only one implementation is allowed per available feature."));
            }

            if (profile.useAdapterComponent)
            {
                if (string.IsNullOrWhiteSpace(profile.adapterComponentType))
                {
                    issues.Add(CreateIssue("error", path, "Adapter component type is required when adapter mode is enabled."));
                }
                else if (FindAvailableFeatureComponent(profile.adapterComponentType) == null)
                {
                    issues.Add(CreateIssue("warning", path, "Adapter component type '" + profile.adapterComponentType + "' was not found in the Components subtab."));
                }
            }
            else
            {
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

    private List<AvailableFeatureDefinition> GetAvailableFeatureCatalog()
    {
        return new List<AvailableFeatureDefinition>
        {
            new AvailableFeatureDefinition
            {
                id = "enemy_aggression",
                name = "Enemy Aggression",
                description = "Hostile NPCs can become aggressive, calm down, and expose aggression-related runtime state.",
                aiMatchDescription = "guard ai, enemy aggression, hostile npc, combat ai",
                tags = new List<string> { "ai", "combat", "enemy" },
                categories = new List<string> { "AI" },
                provides = new List<string> { "aggression_control" },
                consumes = new List<string> { "spotted_state" },
                targetRoles = new List<string> { "Enemy" },
                componentRequirements = new List<PlyFeatureComponentRequirement>
                {
                    new PlyFeatureComponentRequirement { required = true }
                },
                ports = new List<PlyFeaturePortMapping>
                {
                    new PlyFeaturePortMapping { name = "IsAggressive", direction = PlyFeaturePortDirection.Input, kind = PlyFeaturePortKind.Value, dataType = PlyFeatureDataType.Bool },
                    new PlyFeaturePortMapping { name = "HasTargetInSight", direction = PlyFeaturePortDirection.Output, kind = PlyFeaturePortKind.Value, dataType = PlyFeatureDataType.Bool }
                },
                parameters = new List<PlyFeatureParameterMapping>
                {
                    new PlyFeatureParameterMapping { name = "AggroRadius", type = PlyFeatureDataType.Float, defaultValue = "20" }
                }
            },
            new AvailableFeatureDefinition
            {
                id = "health_state",
                name = "Health State",
                description = "Exposes health-like state that gameplay can read or update locally.",
                aiMatchDescription = "health, hp, damageable, hit points",
                tags = new List<string> { "combat", "stats" },
                categories = new List<string> { "Gameplay" },
                provides = new List<string> { "health_value" },
                consumes = new List<string>(),
                targetRoles = new List<string> { "Enemy", "Player", "NPC" },
                componentRequirements = new List<PlyFeatureComponentRequirement>
                {
                    new PlyFeatureComponentRequirement { required = true }
                },
                ports = new List<PlyFeaturePortMapping>
                {
                    new PlyFeaturePortMapping { name = "CurrentHealth", direction = PlyFeaturePortDirection.Output, kind = PlyFeaturePortKind.Value, dataType = PlyFeatureDataType.Float },
                    new PlyFeaturePortMapping { name = "MaxHealth", direction = PlyFeaturePortDirection.Output, kind = PlyFeaturePortKind.Value, dataType = PlyFeatureDataType.Float }
                }
            },
            new AvailableFeatureDefinition
            {
                id = "movement_speed",
                name = "Movement Speed",
                description = "Exposes movement speed configuration or runtime locomotion speed values.",
                aiMatchDescription = "movement speed, locomotion speed, npc speed",
                tags = new List<string> { "movement" },
                categories = new List<string> { "Movement" },
                provides = new List<string> { "speed_control" },
                consumes = new List<string>(),
                targetRoles = new List<string> { "Enemy", "Player", "NPC" },
                componentRequirements = new List<PlyFeatureComponentRequirement>
                {
                    new PlyFeatureComponentRequirement { required = true }
                },
                ports = new List<PlyFeaturePortMapping>
                {
                    new PlyFeaturePortMapping { name = "MoveSpeed", direction = PlyFeaturePortDirection.Input, kind = PlyFeaturePortKind.Value, dataType = PlyFeatureDataType.Float }
                }
            },
            new AvailableFeatureDefinition
            {
                id = "interaction_prompt",
                name = "Interaction Prompt",
                description = "Supports showing or configuring interaction labels or prompts for an interactable object.",
                aiMatchDescription = "interact prompt, use prompt, pickup prompt",
                tags = new List<string> { "interaction", "ui" },
                categories = new List<string> { "Interaction" },
                provides = new List<string> { "prompt_text" },
                consumes = new List<string>(),
                targetRoles = new List<string> { "Interactable" },
                componentRequirements = new List<PlyFeatureComponentRequirement>
                {
                    new PlyFeatureComponentRequirement { required = true }
                },
                ports = new List<PlyFeaturePortMapping>
                {
                    new PlyFeaturePortMapping { name = "PromptText", direction = PlyFeaturePortDirection.Output, kind = PlyFeaturePortKind.Value, dataType = PlyFeatureDataType.String },
                    new PlyFeaturePortMapping { name = "CanInteract", direction = PlyFeaturePortDirection.Output, kind = PlyFeaturePortKind.Value, dataType = PlyFeatureDataType.Bool }
                }
            }
        };
    }

    private PlyFeatureProfile FindFeatureImplementation(string featureId)
    {
        return FeatureManifestState.features.FirstOrDefault(feature =>
            string.Equals(feature.featureId, featureId, StringComparison.OrdinalIgnoreCase));
    }

    private void CreateFeatureImplementation(AvailableFeatureDefinition feature)
    {
        if (feature == null)
        {
            return;
        }

        PlyFeatureProfile existing = FindFeatureImplementation(feature.id);
        if (existing != null)
        {
            return;
        }

        PlyFeatureProfile profile = new PlyFeatureProfile
        {
            id = "impl." + feature.id,
            featureId = feature.id,
            name = feature.name,
            description = feature.description,
            aiMatchDescription = feature.aiMatchDescription,
            tags = new List<string>(feature.tags),
            categories = new List<string>(feature.categories),
            implements = new List<string> { feature.id },
            provides = new List<string>(feature.provides),
            consumes = new List<string>(feature.consumes),
            targetRoles = new List<string>(feature.targetRoles),
            componentRequirements = CloneComponentRequirements(feature.componentRequirements),
            ports = ClonePortMappings(feature.ports),
            parameters = CloneParameterMappings(feature.parameters)
        };
        FeatureManifestState.features.Add(profile);
    }

    private void ReplaceFeatureImplementation(PlyFeatureProfile profile)
    {
        if (profile == null || string.IsNullOrWhiteSpace(profile.featureId))
        {
            return;
        }

        PlyFeatureProfile existing = FindFeatureImplementation(profile.featureId);
        if (existing != null)
        {
            FeatureManifestState.features.Remove(existing);
        }

        FeatureManifestState.features.Add(profile);
    }

    private List<PlyFeatureComponentRequirement> CloneComponentRequirements(List<PlyFeatureComponentRequirement> values)
    {
        return (values ?? new List<PlyFeatureComponentRequirement>())
            .Select(value => new PlyFeatureComponentRequirement
            {
                typeName = value != null ? value.typeName : "",
                assemblyQualifiedName = value != null ? value.assemblyQualifiedName : "",
                required = value == null || value.required
            })
            .ToList();
    }

    private List<PlyFeaturePortMapping> ClonePortMappings(List<PlyFeaturePortMapping> values)
    {
        return (values ?? new List<PlyFeaturePortMapping>())
            .Select(value => new PlyFeaturePortMapping
            {
                name = value != null ? value.name : "",
                direction = value != null ? value.direction : PlyFeaturePortDirection.Input,
                kind = value != null ? value.kind : PlyFeaturePortKind.Value,
                dataType = value != null ? value.dataType : PlyFeatureDataType.Any,
                binding = new PlyFeatureBinding()
            })
            .ToList();
    }

    private List<PlyFeatureParameterMapping> CloneParameterMappings(List<PlyFeatureParameterMapping> values)
    {
        return (values ?? new List<PlyFeatureParameterMapping>())
            .Select(value => new PlyFeatureParameterMapping
            {
                name = value != null ? value.name : "",
                type = value != null ? value.type : PlyFeatureDataType.Any,
                defaultValue = value != null ? value.defaultValue : "",
                binding = new PlyFeatureBinding()
            })
            .ToList();
    }
}
