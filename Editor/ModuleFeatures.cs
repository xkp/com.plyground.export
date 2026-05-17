using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

public partial class ModuleExporter
{
    private const string DefaultFeatureCatalogRelativePath = "Editor/FeatureCatalog/default-feature-catalog.json";
    private static readonly HashSet<string> UnityLifecycleMethods = new HashSet<string>(StringComparer.Ordinal)
    {
        "Awake", "Start", "Update", "LateUpdate", "FixedUpdate", "OnEnable", "OnDisable"
    };
    private static List<PlySemanticFeatureDefinition> cachedDefaultFeatureCatalog;
    private static string cachedDefaultFeatureCatalogPath = "";

    private enum FeatureMappingSection
    {
        Input,
        Output,
        Parameter
    }

    private class AvailableFeatureDefinition
    {
        public string id;
        public string name;
        public string description;
        public bool isBuiltIn;
        public List<string> provides = new List<string>();
        public List<string> requires = new List<string>();
        public List<string> targetRoles = new List<string>();
        public List<string> tags = new List<string>();
        public string category;
        public List<string> intentExamples = new List<string>();
        public List<PlyFeaturePortMapping> ports = new List<PlyFeaturePortMapping>();
        public List<PlyFeatureParameterMapping> parameters = new List<PlyFeatureParameterMapping>();
    }

    private class CompatibleMemberOption
    {
        public string componentType;
        public string memberName;
        public string displayName;
        public string signature;
        public PlyFeatureMemberKind memberKind;
        public PlyFeatureParameterAccess access;
        public bool isStatic;
        public string conversion;
        public PlyFeatureDataType memberDataType;
    }

    private PlyFeatureManifest FeatureManifestState
    {
        get
        {
            if (featureManifest != null)
            {
                return featureManifest;
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

    private int selectedFeatureCatalogIndex = -1;
    private Vector2 featureListScroll;
    private Vector2 featureDetailsScroll;
    private Vector2 featureEditorScroll;
    private Vector2 catalogAddScroll;
    private List<PlyFeatureValidationIssue> featureValidationIssues = new List<PlyFeatureValidationIssue>();
    private bool includeLifecycleMethods;
    private string componentSearchTerm = "";
    private string catalogAddSearchTerm = "";
    private bool showCatalogAddBrowser;
    private bool featureValidationDirty = true;
    private string cachedComponentSearchTerm = "";
    private bool cachedIncludeLifecycleMethods;
    private string cachedSnapshotVersion = "";
    private string cachedCapabilityComponentSignature = "";
    private readonly Dictionary<string, List<CompatibleMemberOption>> compatibleOptionsCache = new Dictionary<string, List<CompatibleMemberOption>>(StringComparer.Ordinal);

    private void InitializeFeatureManifest()
    {
        FeatureManifestState = PlyFeatureSchemaUtility.NormalizeManifest(FeatureManifestState);
        FeatureManifestState.moduleId = moduleId ?? "";
        MergeAvailableFeatureCatalogIntoManifest();
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
        EnsureFeatureEditorCacheState();

        EditorGUILayout.HelpBox("Define semantic features for planning, then attach module-specific implementations. Implementations are exported separately and can use either adapter mode or direct bindings.", MessageType.Info);

        EditorGUI.BeginChangeCheck();
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Refresh Registry", GUILayout.Width(130f)))
        {
            PlyFeatureTypeCache.Refresh();
            InvalidateFeatureEditorCaches();
        }

        if (GUILayout.Button("Import JSON", GUILayout.Width(100f)))
        {
            ImportFeatureManifestFromJson();
        }

        if (GUILayout.Button("Export JSON", GUILayout.Width(100f)))
        {
            ExportFeatureManifestToJson();
        }

        if (GUILayout.Button("New Feature", GUILayout.Width(100f)))
        {
            CreateSemanticFeature();
        }

        if (GUILayout.Button("Add From Catalog", GUILayout.Width(130f)))
        {
            showCatalogAddBrowser = !showCatalogAddBrowser;
        }

        includeLifecycleMethods = EditorGUILayout.ToggleLeft("Include Lifecycle Methods", includeLifecycleMethods, GUILayout.Width(180f));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(6f);
        DrawFeatureWorkspace();
        if (EditorGUI.EndChangeCheck())
        {
            featureValidationDirty = true;
        }

        if (featureValidationDirty)
        {
            featureValidationIssues = ValidateFeatureManifest(FeatureManifestState);
            featureValidationDirty = false;
        }

        EditorGUILayout.Space(6f);
        DrawFeatureValidationSection();
    }

    private void DrawFeatureWorkspace()
    {
        List<AvailableFeatureDefinition> activeFeatures = GetActiveFeatureList();
        selectedFeatureCatalogIndex = Mathf.Clamp(selectedFeatureCatalogIndex < 0 ? 0 : selectedFeatureCatalogIndex, 0, Mathf.Max(0, activeFeatures.Count - 1));
        AvailableFeatureDefinition selectedFeature = activeFeatures.Count > 0 ? activeFeatures[selectedFeatureCatalogIndex] : null;
        PlyFeatureImplementation implementation = selectedFeature != null ? FindFeatureImplementation(selectedFeature.id) : null;

        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.BeginVertical("box", GUILayout.Width(Mathf.Max(220f, position.width * 0.18f)), GUILayout.Height(640f));
        GUILayout.Label("Module Features", EditorStyles.boldLabel);
        if (showCatalogAddBrowser)
        {
            DrawCatalogAddBrowser();
            EditorGUILayout.Space(6f);
        }

        featureListScroll = EditorGUILayout.BeginScrollView(featureListScroll);
        foreach (AvailableFeatureDefinition feature in activeFeatures)
        {
            bool implemented = FindFeatureImplementation(feature.id) != null;
            string label = feature.name + (feature.isBuiltIn ? " [Catalog]" : " [Custom]");
            if (implemented)
            {
                label += " [Implemented]";
            }

            int index = activeFeatures.IndexOf(feature);
            if (DrawSelectableListButton(label, selectedFeatureCatalogIndex == index, GUILayout.Height(32f)))
            {
                selectedFeatureCatalogIndex = index;
            }
        }
        EditorGUILayout.EndScrollView();
        if (activeFeatures.Count == 0)
        {
            EditorGUILayout.HelpBox("No active features yet. Add one from the catalog or create a new feature.", MessageType.Info);
        }

        if (selectedFeature != null)
        {
            EditorGUILayout.Space(6f);
            string removeLabel = selectedFeature.isBuiltIn ? "Remove Implementation" : "Remove Feature";
            if (GUILayout.Button(removeLabel, GUILayout.Width(150f)))
            {
                RemoveSelectedFeature(selectedFeature.id);
                selectedFeatureCatalogIndex = 0;
                GUIUtility.ExitGUI();
            }
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.BeginVertical("box", GUILayout.Width(Mathf.Max(280f, position.width * 0.24f)), GUILayout.Height(640f));
        GUILayout.Label("Feature Definition", EditorStyles.boldLabel);
        featureDetailsScroll = EditorGUILayout.BeginScrollView(featureDetailsScroll);
        if (selectedFeature == null)
        {
            EditorGUILayout.HelpBox("Select a feature from the left.", MessageType.Info);
        }
        else
        {
            DrawSelectedFeatureDefinition(selectedFeature, implementation != null);
        }
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();

        EditorGUILayout.BeginVertical("box", GUILayout.ExpandWidth(true), GUILayout.Height(640f));
        GUILayout.Label("Implementation", EditorStyles.boldLabel);
        featureEditorScroll = EditorGUILayout.BeginScrollView(featureEditorScroll);
        if (selectedFeature == null)
        {
            EditorGUILayout.HelpBox("Select a feature to implement.", MessageType.Info);
        }
        else
        {
            DrawFeatureImplementationEditor(selectedFeature, implementation);
        }
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();
    }

    private void DrawSelectedFeatureDefinition(AvailableFeatureDefinition feature, bool implemented)
    {
        PlySemanticFeatureDefinition semanticFeature = FindSemanticFeature(feature.id);
        string featureName = semanticFeature != null ? semanticFeature.name : feature.name;
        string featureDescription = semanticFeature != null ? semanticFeature.description : feature.description;
        string featureCategory = semanticFeature != null ? semanticFeature.category : feature.category;
        List<string> featureProvides = semanticFeature != null ? semanticFeature.provides : feature.provides;
        List<string> featureRequires = semanticFeature != null ? semanticFeature.requires : feature.requires;
        List<string> featureTargetRoles = semanticFeature != null ? semanticFeature.targetRoles : feature.targetRoles;
        List<string> featureIntentExamples = semanticFeature != null ? semanticFeature.intentExamples : feature.intentExamples;

        EditorGUILayout.LabelField("Id", feature.id);
        if (semanticFeature != null && !feature.isBuiltIn)
        {
            semanticFeature.name = EditorGUILayout.TextField("Name", semanticFeature.name);
            GUILayout.Label("Description", EditorStyles.label);
            semanticFeature.description = EditorGUILayout.TextArea(semanticFeature.description ?? "", GUILayout.MinHeight(54f));
            semanticFeature.category = EditorGUILayout.TextField("Category", semanticFeature.category);
            DrawEditableStringList("Intent Examples", semanticFeature.intentExamples);
            DrawEditableStringList("Provides", semanticFeature.provides);
            DrawEditableStringList("Requires", semanticFeature.requires);
            DrawEditableStringList("Target Roles", semanticFeature.targetRoles);
            DrawEditableStringList("Tags", semanticFeature.tags);
            featureValidationDirty = true;
        }
        else
        {
            EditorGUILayout.LabelField("Name", featureName);
            EditorGUILayout.LabelField("Description", featureDescription, EditorStyles.wordWrappedLabel);
        }

        EditorGUILayout.LabelField("Source", feature.isBuiltIn ? "Built-in catalog feature" : "Module feature");
        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField("Category", string.IsNullOrWhiteSpace(featureCategory) ? "-" : featureCategory);
        EditorGUILayout.LabelField("Provides", string.Join(", ", (featureProvides ?? new List<string>()).ToArray()));
        EditorGUILayout.LabelField("Requires", string.Join(", ", (featureRequires ?? new List<string>()).ToArray()));
        EditorGUILayout.LabelField("Target Roles", string.Join(", ", (featureTargetRoles ?? new List<string>()).ToArray()));
        EditorGUILayout.LabelField("Status", implemented ? "Implemented in this module" : "No implementation yet");
        if ((featureIntentExamples ?? new List<string>()).Count > 0)
        {
            EditorGUILayout.LabelField("Intent Examples", string.Join(", ", featureIntentExamples.ToArray()), EditorStyles.wordWrappedLabel);
        }
        EditorGUILayout.Space(8f);

        GUILayout.Label("Inputs", EditorStyles.boldLabel);
        foreach (PlyFeaturePortMapping input in feature.ports.Where(port => port.direction == PlyFeaturePortDirection.Input))
        {
            EditorGUILayout.LabelField(input.name, $"{input.kind} {input.dataType}");
        }

        GUILayout.Label("Outputs", EditorStyles.boldLabel);
        foreach (PlyFeaturePortMapping output in feature.ports.Where(port => port.direction == PlyFeaturePortDirection.Output))
        {
            EditorGUILayout.LabelField(output.name, $"{output.kind} {output.dataType}");
        }

        GUILayout.Label("Parameters", EditorStyles.boldLabel);
        foreach (PlyFeatureParameterMapping parameter in feature.parameters)
        {
            EditorGUILayout.LabelField(parameter.name, $"{parameter.type} {parameter.accessMode}");
        }
    }

    private void DrawFeatureImplementationEditor(AvailableFeatureDefinition definition, PlyFeatureImplementation implementation)
    {
        if (GetSelectedCapabilityComponents().Count == 0)
        {
            EditorGUILayout.HelpBox("No components are selected in Capabilities > Components. Add module components there before creating feature mappings.", MessageType.Info);
            return;
        }

        if (implementation == null)
        {
            EditorGUILayout.HelpBox("This feature does not have a concrete implementation in the current module yet.", MessageType.Info);
            if (GUILayout.Button("Create Implementation", GUILayout.Width(160f)))
            {
                CreateFeatureImplementation(definition);
            }
            return;
        }

        implementation = PlyFeatureSchemaUtility.NormalizeImplementation(implementation);
        implementation.id = BuildImplementationId(definition.id);
        EditorGUILayout.LabelField("Implementation Id", implementation.id);
        implementation.name = EditorGUILayout.TextField("Name", implementation.name);
        implementation.description = EditorGUILayout.TextField("Description", implementation.description);
        implementation.integrationMode = DrawStringPopup("Integration Mode", implementation.integrationMode, new[] { "bindings", "adapter" });
        DrawEditableStringList("Target Roles", implementation.targetRoles);
        DrawEditableStringList("Tags", implementation.tags);
        EditorGUILayout.Space(6f);

        if (string.Equals(implementation.integrationMode, "adapter", StringComparison.OrdinalIgnoreCase))
        {
            EditorGUILayout.HelpBox("Adapter implementations skip direct parameter/input/output bindings and rely on an adapter contract instead.", MessageType.Info);
            implementation.adapter.adapterId = EditorGUILayout.TextField("Adapter Id", implementation.adapter.adapterId);
            implementation.adapter.setupAdapter = EditorGUILayout.TextField("Setup Adapter", implementation.adapter.setupAdapter);
            implementation.adapter.factoryId = EditorGUILayout.TextField("Factory Id", implementation.adapter.factoryId);
        }
        else
        {
            DrawInputMappingsSection(definition, implementation);
            EditorGUILayout.Space(8f);
            DrawOutputMappingsSection(definition, implementation);
            EditorGUILayout.Space(8f);
            DrawParameterMappingsSection(definition, implementation);
        }

        EditorGUILayout.Space(8f);

        if (GUILayout.Button("Remove Implementation", GUILayout.Width(170f)))
        {
            FeatureManifestState.implementations.Remove(implementation);
            featureValidationDirty = true;
        }
    }

    private void DrawInputMappingsSection(AvailableFeatureDefinition definition, PlyFeatureImplementation implementation)
    {
        GUILayout.Label("Inputs", EditorStyles.boldLabel);
        foreach (PlyFeaturePortMapping port in definition.ports.Where(entry => entry.direction == PlyFeaturePortDirection.Input))
        {
            PlyFeaturePortBinding binding = GetOrCreateInputBinding(implementation, port.name);
            DrawPortMappingRow(CreateBoundPortMapping(port, binding.binding), FeatureMappingSection.Input);
        }
    }

    private void DrawOutputMappingsSection(AvailableFeatureDefinition definition, PlyFeatureImplementation implementation)
    {
        GUILayout.Label("Outputs", EditorStyles.boldLabel);
        foreach (PlyFeaturePortMapping port in definition.ports.Where(entry => entry.direction == PlyFeaturePortDirection.Output))
        {
            PlyFeaturePortBinding binding = GetOrCreateOutputBinding(implementation, port.name);
            DrawPortMappingRow(CreateBoundPortMapping(port, binding.binding), FeatureMappingSection.Output);
        }
    }

    private void DrawParameterMappingsSection(AvailableFeatureDefinition definition, PlyFeatureImplementation implementation)
    {
        GUILayout.Label("Parameters", EditorStyles.boldLabel);
        foreach (PlyFeatureParameterMapping parameter in definition.parameters)
        {
            PlyFeatureParameterBinding binding = GetOrCreateParameterBinding(implementation, parameter.name);
            DrawParameterMappingRow(CreateBoundParameterMapping(parameter, binding.binding));
        }
    }

    private void DrawPortMappingRow(PlyFeaturePortMapping port, FeatureMappingSection section)
    {
        port.binding ??= new PlyFeatureBinding();
        List<CompatibleMemberOption> allOptions = GetCompatibleOptionsForPort(port, section);
        List<string> componentOptions = allOptions.Select(option => option.componentType).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(value => value).ToList();

        EditorGUILayout.BeginVertical("helpbox");
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label(port.name, GUILayout.Width(160f));
        GUILayout.Label(port.dataType.ToString(), GUILayout.Width(90f));
        DrawCompatibleComponentPopup(port.binding, componentOptions, 200f);
        List<CompatibleMemberOption> filteredOptions = allOptions.Where(option => string.Equals(option.componentType, port.binding.componentType, StringComparison.OrdinalIgnoreCase)).ToList();
        DrawCompatibleMemberPopup(port.binding, filteredOptions, 220f);
        GUILayout.Label(string.IsNullOrWhiteSpace(port.binding.memberSignature) ? "-" : port.binding.memberSignature, EditorStyles.miniLabel, GUILayout.Width(220f));
        GUILayout.Label(GetValidationStatusText(port, section), EditorStyles.miniBoldLabel, GUILayout.Width(90f));
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
    }

    private void DrawParameterMappingRow(PlyFeatureParameterMapping parameter)
    {
        parameter.binding ??= new PlyFeatureBinding();
        List<CompatibleMemberOption> allOptions = GetCompatibleOptionsForParameter(parameter);
        List<string> componentOptions = allOptions.Select(option => option.componentType).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(value => value).ToList();

        EditorGUILayout.BeginVertical("helpbox");
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label(parameter.name, GUILayout.Width(160f));
        GUILayout.Label(parameter.type.ToString(), GUILayout.Width(90f));
        DrawCompatibleComponentPopup(parameter.binding, componentOptions, 200f);
        List<CompatibleMemberOption> filteredOptions = allOptions.Where(option => string.Equals(option.componentType, parameter.binding.componentType, StringComparison.OrdinalIgnoreCase)).ToList();
        DrawCompatibleMemberPopup(parameter.binding, filteredOptions, 220f);
        string defaultLabel = string.IsNullOrWhiteSpace(parameter.defaultValue) ? "-" : parameter.defaultValue;
        GUILayout.Label(defaultLabel, EditorStyles.miniLabel, GUILayout.Width(90f));
        parameter.accessMode = (PlyFeatureParameterAccess)EditorGUILayout.EnumPopup(parameter.accessMode, GUILayout.Width(100f));
        GUILayout.Label(GetValidationStatusText(parameter), EditorStyles.miniBoldLabel, GUILayout.Width(90f));
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
    }

    private void DrawCompatibleComponentPopup(PlyFeatureBinding binding, List<string> componentOptions, float width)
    {
        List<string> labels = new List<string> { "<select>" };
        labels.AddRange(componentOptions);

        int selectedIndex = 0;
        if (!string.IsNullOrWhiteSpace(binding.componentType))
        {
            int foundIndex = componentOptions.FindIndex(option => string.Equals(option, binding.componentType, StringComparison.OrdinalIgnoreCase));
            selectedIndex = foundIndex >= 0 ? foundIndex + 1 : 0;
        }

        int newIndex = EditorGUILayout.Popup(selectedIndex, labels.ToArray(), GUILayout.Width(width));
        string newComponent = newIndex <= 0 ? "" : componentOptions[newIndex - 1];
        if (!string.Equals(binding.componentType, newComponent, StringComparison.OrdinalIgnoreCase))
        {
            binding.componentType = newComponent;
            binding.memberName = "";
            binding.memberSignature = "";
            binding.conversion = "";
            binding.memberKind = PlyFeatureMemberKind.Method;
        }
    }

    private void DrawCompatibleMemberPopup(PlyFeatureBinding binding, List<CompatibleMemberOption> options, float width)
    {
        List<string> labels = new List<string> { "<select>" };
        labels.AddRange(options.Select(option => option.displayName));

        int selectedIndex = 0;
        if (!string.IsNullOrWhiteSpace(binding.memberName))
        {
            int foundIndex = options.FindIndex(option =>
                option.memberKind == binding.memberKind &&
                string.Equals(option.memberName, binding.memberName, StringComparison.Ordinal) &&
                string.Equals(option.signature, binding.memberSignature, StringComparison.Ordinal));
            selectedIndex = foundIndex >= 0 ? foundIndex + 1 : 0;
        }

        int newIndex = EditorGUILayout.Popup(selectedIndex, labels.ToArray(), GUILayout.Width(width));
        if (newIndex <= 0)
        {
            binding.memberName = "";
            binding.memberSignature = "";
            binding.conversion = "";
            return;
        }

        CompatibleMemberOption selected = options[newIndex - 1];
        binding.memberKind = selected.memberKind;
        binding.memberName = selected.memberName;
        binding.memberSignature = selected.signature;
        binding.conversion = selected.conversion;
        binding.isStatic = selected.isStatic;
        binding.access = selected.access;
    }

    private string GetValidationStatusText(PlyFeaturePortMapping port, FeatureMappingSection section)
    {
        if (port.binding == null || string.IsNullOrWhiteSpace(port.binding.componentType) || string.IsNullOrWhiteSpace(port.binding.memberName))
        {
            return "Pending";
        }

        List<PlyFeatureValidationIssue> issues = new List<PlyFeatureValidationIssue>();
        ValidatePortBinding(port, section, issues);
        if (issues.Count == 0)
        {
            return "Valid";
        }

        return issues.Any(issue => string.Equals(issue.severity, "error", StringComparison.OrdinalIgnoreCase)) ? "Error" : "Warn";
    }

    private string GetValidationStatusText(PlyFeatureParameterMapping parameter)
    {
        if (parameter.binding == null || string.IsNullOrWhiteSpace(parameter.binding.componentType) || string.IsNullOrWhiteSpace(parameter.binding.memberName))
        {
            return "Pending";
        }

        List<PlyFeatureValidationIssue> issues = new List<PlyFeatureValidationIssue>();
        ValidateParameterBinding(parameter, issues);
        if (issues.Count == 0)
        {
            return "Valid";
        }

        return issues.Any(issue => string.Equals(issue.severity, "error", StringComparison.OrdinalIgnoreCase)) ? "Error" : "Warn";
    }

    private List<CompatibleMemberOption> GetCompatibleOptionsForPort(PlyFeaturePortMapping port, FeatureMappingSection section)
    {
        string cacheKey = "port|" + section + "|" + port.name + "|" + port.kind + "|" + port.dataType;
        if (compatibleOptionsCache.TryGetValue(cacheKey, out List<CompatibleMemberOption> cached))
        {
            return cached;
        }

        List<CompatibleMemberOption> results = new List<CompatibleMemberOption>();
        foreach (PlyFeatureComponentDescriptor component in GetProjectFeatureComponents(componentSearchTerm))
        {
            foreach (PlyFeatureMemberDescriptor member in component.members ?? new List<PlyFeatureMemberDescriptor>())
            {
                if (TryCreateCompatiblePortOption(component, member, port, section, out CompatibleMemberOption option))
                {
                    results.Add(option);
                }
            }
        }

        compatibleOptionsCache[cacheKey] = results;
        return results;
    }

    private List<CompatibleMemberOption> GetCompatibleOptionsForParameter(PlyFeatureParameterMapping parameter)
    {
        string cacheKey = "parameter|" + parameter.name + "|" + parameter.type + "|" + parameter.accessMode;
        if (compatibleOptionsCache.TryGetValue(cacheKey, out List<CompatibleMemberOption> cached))
        {
            return cached;
        }

        List<CompatibleMemberOption> results = new List<CompatibleMemberOption>();
        foreach (PlyFeatureComponentDescriptor component in GetProjectFeatureComponents(componentSearchTerm))
        {
            foreach (PlyFeatureMemberDescriptor member in component.members ?? new List<PlyFeatureMemberDescriptor>())
            {
                if (TryCreateCompatibleParameterOption(component, member, parameter, out CompatibleMemberOption option))
                {
                    results.Add(option);
                }
            }
        }

        compatibleOptionsCache[cacheKey] = results;
        return results;
    }

    private bool TryCreateCompatiblePortOption(PlyFeatureComponentDescriptor component, PlyFeatureMemberDescriptor member, PlyFeaturePortMapping port, FeatureMappingSection section, out CompatibleMemberOption option)
    {
        option = null;
        if (component == null || member == null || port == null)
        {
            return false;
        }

        string conversion;
        switch (section)
        {
            case FeatureMappingSection.Input:
                if (member.memberKind == PlyFeatureMemberKind.Method)
                {
                    if ((!includeLifecycleMethods && member.isLifecycleMethod) || !TryMatchInputMethod(member, port, out conversion))
                    {
                        return false;
                    }
                }
                else if (port.kind == PlyFeaturePortKind.Value && member.memberKind == PlyFeatureMemberKind.Property)
                {
                    if (member.access == PlyFeatureParameterAccess.ReadOnly || !TryMatchFeatureToTarget(port.dataType, member.dataType, out conversion))
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
                break;
            case FeatureMappingSection.Output:
                if (member.memberKind != PlyFeatureMemberKind.UnityEvent && member.memberKind != PlyFeatureMemberKind.CSharpEvent)
                {
                    return false;
                }

                if (!TryMatchSourceToExpected(member.dataType, port.dataType, out conversion))
                {
                    return false;
                }
                break;
            default:
                return false;
        }

        option = CreateCompatibleOption(component, member, conversion);
        return true;
    }

    private bool TryCreateCompatibleParameterOption(PlyFeatureComponentDescriptor component, PlyFeatureMemberDescriptor member, PlyFeatureParameterMapping parameter, out CompatibleMemberOption option)
    {
        option = null;
        if (component == null || member == null || parameter == null)
        {
            return false;
        }

        if (member.memberKind != PlyFeatureMemberKind.Field && member.memberKind != PlyFeatureMemberKind.Property)
        {
            return false;
        }

        if (TryMatchParameterDataType(member.dataType, parameter, member.access, out string conversion))
        {
            option = CreateCompatibleOption(component, member, conversion);
            return true;
        }

        return false;
    }

    private CompatibleMemberOption CreateCompatibleOption(PlyFeatureComponentDescriptor component, PlyFeatureMemberDescriptor member, string conversion)
    {
        return new CompatibleMemberOption
        {
            componentType = component.typeName,
            memberName = member.memberName,
            displayName = member.displayName,
            signature = member.displayName,
            memberKind = member.memberKind,
            access = member.access,
            isStatic = member.isStatic,
            conversion = conversion ?? "",
            memberDataType = member.dataType
        };
    }

    private bool TryMatchInputMethod(PlyFeatureMemberDescriptor member, PlyFeaturePortMapping port, out string conversion)
    {
        conversion = "";
        if (member == null || member.memberKind != PlyFeatureMemberKind.Method)
        {
            return false;
        }

        if (port.dataType == PlyFeatureDataType.Void)
        {
            return member.parameterCount == 0 && member.dataType == PlyFeatureDataType.Void;
        }

        return member.parameterCount == 1 && TryMatchFeatureToTarget(port.dataType, member.dataType, out conversion);
    }

    private bool TryMatchParameterDataType(PlyFeatureDataType memberDataType, PlyFeatureParameterMapping parameter, PlyFeatureParameterAccess memberAccess, out string conversion)
    {
        conversion = "";
        switch (parameter.accessMode)
        {
            case PlyFeatureParameterAccess.ReadOnly:
                if (memberAccess == PlyFeatureParameterAccess.WriteOnly)
                {
                    return false;
                }

                return TryMatchSourceToExpected(memberDataType, parameter.type, out conversion);
            case PlyFeatureParameterAccess.WriteOnly:
                if (memberAccess == PlyFeatureParameterAccess.ReadOnly)
                {
                    return false;
                }

                return TryMatchFeatureToTarget(parameter.type, memberDataType, out conversion);
            default:
                if (memberAccess != PlyFeatureParameterAccess.ReadWrite)
                {
                    return false;
                }

                return TryMatchSourceToExpected(memberDataType, parameter.type, out string readConversion)
                    && TryMatchFeatureToTarget(parameter.type, memberDataType, out string writeConversion)
                    && MergeConversions(readConversion, writeConversion, out conversion);
        }
    }

    private IEnumerable<CompatibleMemberOption> GetCompatibleInputMembers(Type type, string componentTypeName, PlyFeaturePortMapping port)
    {
        BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly;
        foreach (MethodInfo method in type.GetMethods(flags))
        {
            if (method.IsSpecialName)
            {
                continue;
            }

            if (!includeLifecycleMethods && UnityLifecycleMethods.Contains(method.Name))
            {
                continue;
            }

            if (TryMatchInputMethod(method, port, out string conversion))
            {
                yield return new CompatibleMemberOption
                {
                    componentType = componentTypeName,
                    memberName = method.Name,
                    signature = BuildMethodSignature(method),
                    memberKind = PlyFeatureMemberKind.Method,
                    access = PlyFeatureParameterAccess.ReadWrite,
                    isStatic = method.IsStatic,
                    conversion = conversion,
                    memberDataType = method.ReturnType == typeof(void) && method.GetParameters().Length == 0
                        ? PlyFeatureDataType.Void
                        : GetPrimaryMethodDataType(method)
                };
            }
        }

        if (port.kind == PlyFeaturePortKind.Value)
        {
            foreach (PropertyInfo property in type.GetProperties(flags))
            {
                MethodInfo setter = property.GetSetMethod();
                if (setter == null)
                {
                    continue;
                }

                if (TryMatchPropertyForInput(property.PropertyType, port.dataType, out string conversion))
                {
                    yield return new CompatibleMemberOption
                    {
                        componentType = componentTypeName,
                        memberName = property.Name,
                        signature = BuildPropertySignature(property),
                        memberKind = PlyFeatureMemberKind.Property,
                        access = PlyFeatureParameterAccess.ReadWrite,
                        isStatic = setter.IsStatic,
                        conversion = conversion,
                        memberDataType = PlyFeatureReflectionScanner.MapType(property.PropertyType)
                    };
                }
            }
        }
    }

    private IEnumerable<CompatibleMemberOption> GetCompatibleOutputMembers(Type type, string componentTypeName, PlyFeaturePortMapping port)
    {
        BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
        foreach (FieldInfo field in type.GetFields(flags))
        {
            if (!IsEventLikeField(field))
            {
                continue;
            }

            PlyFeatureDataType payloadType = GetUnityEventPayloadType(field.FieldType);
            if (TryMatchSourceToExpected(payloadType, port.dataType, out string conversion))
            {
                yield return new CompatibleMemberOption
                {
                    componentType = componentTypeName,
                    memberName = field.Name,
                    signature = field.Name + " : " + field.FieldType.Name,
                    memberKind = PlyFeatureMemberKind.UnityEvent,
                    access = PlyFeatureParameterAccess.ReadOnly,
                    conversion = conversion,
                    memberDataType = payloadType
                };
            }
        }

        foreach (PropertyInfo property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
        {
            if (!typeof(UnityEventBase).IsAssignableFrom(property.PropertyType))
            {
                continue;
            }

            PlyFeatureDataType payloadType = GetUnityEventPayloadType(property.PropertyType);
            if (TryMatchSourceToExpected(payloadType, port.dataType, out string conversion))
            {
                yield return new CompatibleMemberOption
                {
                    componentType = componentTypeName,
                    memberName = property.Name,
                    signature = property.Name + " : " + property.PropertyType.Name,
                    memberKind = PlyFeatureMemberKind.UnityEvent,
                    access = PlyFeatureParameterAccess.ReadOnly,
                    conversion = conversion,
                    memberDataType = payloadType
                };
            }
        }

        foreach (EventInfo eventInfo in type.GetEvents(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
        {
            PlyFeatureDataType payloadType = GetCSharpEventPayloadType(eventInfo.EventHandlerType);
            if (TryMatchSourceToExpected(payloadType, port.dataType, out string conversion))
            {
                yield return new CompatibleMemberOption
                {
                    componentType = componentTypeName,
                    memberName = eventInfo.Name,
                    signature = BuildEventSignature(eventInfo),
                    memberKind = PlyFeatureMemberKind.CSharpEvent,
                    access = PlyFeatureParameterAccess.ReadOnly,
                    conversion = conversion,
                    memberDataType = payloadType
                };
            }
        }
    }

    private IEnumerable<CompatibleMemberOption> GetCompatibleParameterMembers(Type type, string componentTypeName, PlyFeatureParameterMapping parameter)
    {
        BindingFlags fieldFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
        foreach (FieldInfo field in type.GetFields(fieldFlags))
        {
            if (!IsParameterField(field))
            {
                continue;
            }

            PlyFeatureParameterAccess memberAccess = field.IsInitOnly ? PlyFeatureParameterAccess.ReadOnly : PlyFeatureParameterAccess.ReadWrite;
            if (TryMatchParameterType(field.FieldType, parameter, memberAccess, out string conversion))
            {
                yield return new CompatibleMemberOption
                {
                    componentType = componentTypeName,
                    memberName = field.Name,
                    signature = BuildFieldSignature(field),
                    memberKind = PlyFeatureMemberKind.Field,
                    access = memberAccess,
                    conversion = conversion,
                    memberDataType = PlyFeatureReflectionScanner.MapType(field.FieldType)
                };
            }
        }

        foreach (PropertyInfo property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
        {
            MethodInfo getter = property.GetGetMethod();
            MethodInfo setter = property.GetSetMethod();
            if (getter == null && setter == null)
            {
                continue;
            }

            if (typeof(UnityEventBase).IsAssignableFrom(property.PropertyType))
            {
                continue;
            }

            PlyFeatureParameterAccess memberAccess = getter != null && setter != null
                ? PlyFeatureParameterAccess.ReadWrite
                : getter != null ? PlyFeatureParameterAccess.ReadOnly : PlyFeatureParameterAccess.WriteOnly;
            if (TryMatchParameterType(property.PropertyType, parameter, memberAccess, out string conversion))
            {
                yield return new CompatibleMemberOption
                {
                    componentType = componentTypeName,
                    memberName = property.Name,
                    signature = BuildPropertySignature(property),
                    memberKind = PlyFeatureMemberKind.Property,
                    access = memberAccess,
                    isStatic = (getter != null && getter.IsStatic) || (setter != null && setter.IsStatic),
                    conversion = conversion,
                    memberDataType = PlyFeatureReflectionScanner.MapType(property.PropertyType)
                };
            }
        }
    }

    private bool TryMatchInputMethod(MethodInfo method, PlyFeaturePortMapping port, out string conversion)
    {
        conversion = "";
        ParameterInfo[] parameters = method.GetParameters();
        if (port.dataType == PlyFeatureDataType.Void)
        {
            return parameters.Length == 0;
        }

        if (parameters.Length != 1)
        {
            return false;
        }

        return TryMatchFeatureToTarget(port.dataType, PlyFeatureReflectionScanner.MapType(parameters[0].ParameterType), out conversion);
    }

    private bool TryMatchPropertyForInput(Type propertyType, PlyFeatureDataType expectedType, out string conversion)
    {
        return TryMatchFeatureToTarget(expectedType, PlyFeatureReflectionScanner.MapType(propertyType), out conversion);
    }

    private bool TryMatchParameterType(Type memberType, PlyFeatureParameterMapping parameter, PlyFeatureParameterAccess memberAccess, out string conversion)
    {
        conversion = "";
        PlyFeatureDataType memberDataType = PlyFeatureReflectionScanner.MapType(memberType);
        switch (parameter.accessMode)
        {
            case PlyFeatureParameterAccess.ReadOnly:
                if (memberAccess == PlyFeatureParameterAccess.WriteOnly)
                {
                    return false;
                }

                return TryMatchSourceToExpected(memberDataType, parameter.type, out conversion);
            case PlyFeatureParameterAccess.WriteOnly:
                if (memberAccess == PlyFeatureParameterAccess.ReadOnly)
                {
                    return false;
                }

                return TryMatchFeatureToTarget(parameter.type, memberDataType, out conversion);
            default:
                if (memberAccess != PlyFeatureParameterAccess.ReadWrite)
                {
                    return false;
                }

                return TryMatchSourceToExpected(memberDataType, parameter.type, out string readConversion)
                    && TryMatchFeatureToTarget(parameter.type, memberDataType, out string writeConversion)
                    && MergeConversions(readConversion, writeConversion, out conversion);
        }
    }

    private bool TryMatchFeatureToTarget(PlyFeatureDataType featureType, PlyFeatureDataType targetType, out string conversion)
    {
        conversion = "";
        if (featureType == PlyFeatureDataType.Any || targetType == PlyFeatureDataType.Any)
        {
            return true;
        }

        if (featureType == targetType)
        {
            return true;
        }

        if (featureType == PlyFeatureDataType.Int && targetType == PlyFeatureDataType.Float)
        {
            conversion = "int_to_float";
            return true;
        }

        return false;
    }

    private bool TryMatchSourceToExpected(PlyFeatureDataType sourceType, PlyFeatureDataType expectedType, out string conversion)
    {
        conversion = "";
        if (sourceType == PlyFeatureDataType.Any || expectedType == PlyFeatureDataType.Any)
        {
            return true;
        }

        if (sourceType == expectedType)
        {
            return true;
        }

        if (sourceType == PlyFeatureDataType.Int && expectedType == PlyFeatureDataType.Float)
        {
            conversion = "int_to_float";
            return true;
        }

        return false;
    }

    private bool MergeConversions(string left, string right, out string merged)
    {
        if (string.IsNullOrWhiteSpace(left) && string.IsNullOrWhiteSpace(right))
        {
            merged = "";
            return true;
        }

        if (string.Equals(left, right, StringComparison.OrdinalIgnoreCase))
        {
            merged = left ?? "";
            return true;
        }

        if (string.IsNullOrWhiteSpace(left))
        {
            merged = right ?? "";
            return true;
        }

        if (string.IsNullOrWhiteSpace(right))
        {
            merged = left ?? "";
            return true;
        }

        merged = left + "|" + right;
        return true;
    }

    private bool IsEventLikeField(FieldInfo field)
    {
        bool serialized = field.IsPublic || field.GetCustomAttribute<SerializeField>() != null;
        return serialized && typeof(UnityEventBase).IsAssignableFrom(field.FieldType);
    }

    private bool IsParameterField(FieldInfo field)
    {
        bool serialized = field.IsPublic || field.GetCustomAttribute<SerializeField>() != null;
        return serialized && !typeof(UnityEventBase).IsAssignableFrom(field.FieldType);
    }

    private string BuildMethodSignature(MethodInfo method)
    {
        string parameterList = string.Join(", ", method.GetParameters().Select(parameter => parameter.ParameterType.Name + " " + parameter.Name).ToArray());
        return method.Name + "(" + parameterList + ")";
    }

    private string BuildPropertySignature(PropertyInfo property)
    {
        string access = property.CanRead && property.CanWrite ? "get/set" : property.CanRead ? "get" : "set";
        return property.Name + " : " + property.PropertyType.Name + " [" + access + "]";
    }

    private string BuildFieldSignature(FieldInfo field)
    {
        return field.Name + " : " + field.FieldType.Name;
    }

    private string BuildEventSignature(EventInfo eventInfo)
    {
        return eventInfo.Name + " : " + (eventInfo.EventHandlerType != null ? eventInfo.EventHandlerType.Name : "event");
    }

    private PlyFeatureDataType GetPrimaryMethodDataType(MethodInfo method)
    {
        ParameterInfo[] parameters = method.GetParameters();
        if (parameters.Length == 1)
        {
            return PlyFeatureReflectionScanner.MapType(parameters[0].ParameterType);
        }

        return PlyFeatureReflectionScanner.MapType(method.ReturnType);
    }

    private PlyFeatureDataType GetUnityEventPayloadType(Type eventType)
    {
        Type current = eventType;
        while (current != null)
        {
            if (current == typeof(UnityEvent))
            {
                return PlyFeatureDataType.Void;
            }

            if (current.IsGenericType && current.GetGenericTypeDefinition() == typeof(UnityEvent<>))
            {
                return PlyFeatureReflectionScanner.MapType(current.GetGenericArguments()[0]);
            }

            current = current.BaseType;
        }

        return PlyFeatureDataType.Void;
    }

    private PlyFeatureDataType GetCSharpEventPayloadType(Type eventHandlerType)
    {
        if (eventHandlerType == null)
        {
            return PlyFeatureDataType.Void;
        }

        MethodInfo invoke = eventHandlerType.GetMethod("Invoke");
        if (invoke == null)
        {
            return PlyFeatureDataType.Any;
        }

        ParameterInfo[] parameters = invoke.GetParameters();
        if (parameters.Length == 0)
        {
            return PlyFeatureDataType.Void;
        }

        return PlyFeatureReflectionScanner.MapType(parameters[parameters.Length - 1].ParameterType);
    }

    private List<PlyFeatureValidationIssue> ValidateFeatureManifest(PlyFeatureManifest manifest)
    {
        List<PlyFeatureValidationIssue> issues = new List<PlyFeatureValidationIssue>();
        manifest = PlyFeatureSchemaUtility.NormalizeManifest(manifest);

        HashSet<string> featureIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        HashSet<string> semanticIds = new HashSet<string>(manifest.features.Select(feature => feature.id), StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < manifest.implementations.Count; i++)
        {
            PlyFeatureImplementation implementation = manifest.implementations[i];
            string path = "Implementation[" + i + "]";
            if (string.IsNullOrWhiteSpace(implementation.featureId))
            {
                issues.Add(CreateIssue("error", path, "Feature id is required."));
            }
            else if (!semanticIds.Contains(implementation.featureId))
            {
                issues.Add(CreateIssue("error", path, "Feature id does not match any semantic feature definition."));
            }
            else if (!featureIds.Add(implementation.featureId))
            {
                issues.Add(CreateIssue("error", path, "Only one implementation is allowed per semantic feature in this module."));
            }

            if (string.Equals(implementation.integrationMode, "adapter", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(implementation.adapter.adapterId))
                {
                    issues.Add(CreateIssue("error", path, "Adapter implementations must provide an adapter id."));
                }

                continue;
            }

            AvailableFeatureDefinition definition = GetAvailableFeatureCatalog()
                .FirstOrDefault(feature => string.Equals(feature.id, implementation.featureId, StringComparison.OrdinalIgnoreCase));
            if (definition == null)
            {
                continue;
            }

            foreach (PlyFeaturePortMapping port in definition.ports.Where(entry => entry.direction == PlyFeaturePortDirection.Input))
            {
                ValidatePortBinding(CreateBoundPortMapping(port, GetOrCreateInputBinding(implementation, port.name).binding), FeatureMappingSection.Input, issues);
            }

            foreach (PlyFeaturePortMapping port in definition.ports.Where(entry => entry.direction == PlyFeaturePortDirection.Output))
            {
                ValidatePortBinding(CreateBoundPortMapping(port, GetOrCreateOutputBinding(implementation, port.name).binding), FeatureMappingSection.Output, issues);
            }

            foreach (PlyFeatureParameterMapping parameter in definition.parameters)
            {
                ValidateParameterBinding(CreateBoundParameterMapping(parameter, GetOrCreateParameterBinding(implementation, parameter.name).binding), issues);
            }
        }

        return issues;
    }

    private void ValidatePortBinding(PlyFeaturePortMapping port, FeatureMappingSection section, List<PlyFeatureValidationIssue> issues)
    {
        if (port.binding == null)
        {
            issues.Add(CreateIssue("error", port.name, "Binding is required."));
            return;
        }

        if (string.IsNullOrWhiteSpace(port.binding.componentType) || string.IsNullOrWhiteSpace(port.binding.memberName))
        {
            return;
        }

        List<CompatibleMemberOption> options = GetCompatibleOptionsForPort(port, section);
        ValidateBindingSelection(port.binding, options, issues, port.name);
    }

    private void ValidateParameterBinding(PlyFeatureParameterMapping parameter, List<PlyFeatureValidationIssue> issues)
    {
        if (parameter.binding == null)
        {
            issues.Add(CreateIssue("error", parameter.name, "Binding is required."));
            return;
        }

        if (string.IsNullOrWhiteSpace(parameter.binding.componentType) || string.IsNullOrWhiteSpace(parameter.binding.memberName))
        {
            return;
        }

        List<CompatibleMemberOption> options = GetCompatibleOptionsForParameter(parameter);
        ValidateBindingSelection(parameter.binding, options, issues, parameter.name);
    }

    private void ValidateBindingSelection(PlyFeatureBinding binding, List<CompatibleMemberOption> options, List<PlyFeatureValidationIssue> issues, string path)
    {
        if (string.IsNullOrWhiteSpace(binding.componentType))
        {
            issues.Add(CreateIssue("error", path, "Selected component type is missing."));
            return;
        }

        PlyFeatureComponentDescriptor component = GetProjectFeatureComponents("")
            .FirstOrDefault(entry => string.Equals(entry.typeName, binding.componentType, StringComparison.OrdinalIgnoreCase) ||
                                     string.Equals(entry.fullName, binding.componentType, StringComparison.OrdinalIgnoreCase));
        if (component == null)
        {
            issues.Add(CreateIssue("error", path, "Selected component type is not available in Capabilities > Components."));
            return;
        }

        CompatibleMemberOption selected = options.FirstOrDefault(option =>
            string.Equals(option.componentType, binding.componentType, StringComparison.OrdinalIgnoreCase) &&
            option.memberKind == binding.memberKind &&
            string.Equals(option.memberName, binding.memberName, StringComparison.Ordinal));
        if (selected == null)
        {
            issues.Add(CreateIssue("error", path, "Selected member is missing or incompatible."));
        }
    }

    private void DrawFeatureValidationSection()
    {
        GUILayout.Label("Validation", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");
        if (featureValidationIssues.Count == 0)
        {
            EditorGUILayout.HelpBox("No mapping validation issues found.", MessageType.Info);
        }
        else
        {
            foreach (PlyFeatureValidationIssue issue in featureValidationIssues)
            {
                EditorGUILayout.HelpBox((string.IsNullOrWhiteSpace(issue.path) ? "" : issue.path + ": ") + issue.message,
                    string.Equals(issue.severity, "error", StringComparison.OrdinalIgnoreCase) ? MessageType.Error : MessageType.Warning);
            }
        }
        EditorGUILayout.EndVertical();
    }

    private void AddGuardAiExampleProfile()
    {
        AvailableFeatureDefinition feature = GetAvailableFeatureCatalog().FirstOrDefault(entry => string.Equals(entry.id, "enemy_aggression", StringComparison.OrdinalIgnoreCase));
        if (feature == null)
        {
            return;
        }

        CreateFeatureImplementation(feature);
    }

    private void ImportFeatureManifestFromJson()
    {
        string filePath = EditorUtility.OpenFilePanel("Import Feature Catalog", Application.dataPath, "json");
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return;
        }

        try
        {
            FeatureManifestState = PlyFeatureJson.ImportFromFile(filePath);
            FeatureManifestState.moduleId = moduleId ?? "";
            InvalidateFeatureEditorCaches();
        }
        catch (Exception exception)
        {
            EditorUtility.DisplayDialog("Import Failed", exception.Message, "OK");
        }
    }

    private void ExportFeatureManifestToJson()
    {
        string filePath = EditorUtility.SaveFilePanel("Export Feature Catalog", Application.dataPath, "feature-catalog.json", "json");
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

    private void EnsureFeatureEditorCacheState()
    {
        string snapshotVersion = PlyFeatureTypeCache.Snapshot.generatedAtUtc ?? "";
        string capabilityComponentSignature = GetSelectedCapabilityComponentSignature();
        if (!string.Equals(cachedComponentSearchTerm, componentSearchTerm ?? "", StringComparison.Ordinal) ||
            cachedIncludeLifecycleMethods != includeLifecycleMethods ||
            !string.Equals(cachedSnapshotVersion, snapshotVersion, StringComparison.Ordinal) ||
            !string.Equals(cachedCapabilityComponentSignature, capabilityComponentSignature, StringComparison.Ordinal))
        {
            InvalidateFeatureEditorCaches();
            cachedComponentSearchTerm = componentSearchTerm ?? "";
            cachedIncludeLifecycleMethods = includeLifecycleMethods;
            cachedSnapshotVersion = snapshotVersion;
            cachedCapabilityComponentSignature = capabilityComponentSignature;
        }
    }

    private void InvalidateFeatureEditorCaches()
    {
        compatibleOptionsCache.Clear();
        featureValidationDirty = true;
    }

    private List<PlyFeatureComponentDescriptor> GetProjectFeatureComponents(string searchTerm)
    {
        Dictionary<string, PlyFeatureComponentDescriptor> availableByKey = (PlyFeatureTypeCache.Snapshot.components ?? new List<PlyFeatureComponentDescriptor>())
            .GroupBy(component => component.fullName ?? component.typeName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        List<PlyFeatureComponentDescriptor> curated = new List<PlyFeatureComponentDescriptor>();
        foreach (UnityCapabilityComponentInfo selectedComponent in GetSelectedCapabilityComponents())
        {
            string preferredKey = !string.IsNullOrWhiteSpace(selectedComponent.typeName)
                ? selectedComponent.typeName
                : selectedComponent.componentId;
            if (!string.IsNullOrWhiteSpace(preferredKey) && availableByKey.TryGetValue(preferredKey, out PlyFeatureComponentDescriptor exactMatch))
            {
                curated.Add(exactMatch);
                continue;
            }

            PlyFeatureComponentDescriptor fallback = PlyFeatureTypeCache.FindComponent(selectedComponent.typeName)
                ?? PlyFeatureTypeCache.FindComponent(selectedComponent.componentId);
            if (fallback != null)
            {
                curated.Add(fallback);
            }
        }

        IEnumerable<PlyFeatureComponentDescriptor> query = curated
            .GroupBy(component => component.fullName ?? component.typeName, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First());
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            string needle = searchTerm.Trim();
            query = query.Where(component =>
                ContainsIgnoreCase(component.typeName, needle) ||
                ContainsIgnoreCase(component.fullName, needle) ||
                ContainsIgnoreCase(component.namespaceName, needle));
        }

        return query.OrderBy(component => component.typeName, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private List<UnityCapabilityComponentInfo> GetSelectedCapabilityComponents()
    {
        moduleCapabilities ??= new CapabilityManifest();
        moduleCapabilities.unity ??= new CapabilityUnityInfo();
        return (moduleCapabilities.unity.components ?? new List<UnityCapabilityComponentInfo>())
            .Where(component =>
                component != null &&
                (!string.IsNullOrWhiteSpace(component.typeName) || !string.IsNullOrWhiteSpace(component.componentId)))
            .ToList();
    }

    private string GetSelectedCapabilityComponentSignature()
    {
        return string.Join("|", GetSelectedCapabilityComponents()
            .Select(component => (component.typeName ?? "").Trim() + "::" + (component.componentId ?? "").Trim())
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToArray());
    }

    private Type ResolveComponentType(PlyFeatureComponentDescriptor descriptor)
    {
        if (descriptor == null)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(descriptor.assemblyQualifiedName))
        {
            Type resolved = Type.GetType(descriptor.assemblyQualifiedName, false);
            if (resolved != null)
            {
                return resolved;
            }
        }

        return AppDomain.CurrentDomain.GetAssemblies()
            .Select(assembly => assembly.GetType(descriptor.fullName, false))
            .FirstOrDefault(type => type != null);
    }

    private bool ContainsIgnoreCase(string haystack, string needle)
    {
        return !string.IsNullOrWhiteSpace(haystack) &&
            haystack.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private List<AvailableFeatureDefinition> GetAvailableFeatureCatalog()
    {
        Dictionary<string, AvailableFeatureDefinition> catalog = GetBuiltInSemanticFeatures()
            .ToDictionary(feature => feature.id, CreateAvailableFeatureDefinition, StringComparer.OrdinalIgnoreCase);

        foreach (PlySemanticFeatureDefinition feature in FeatureManifestState.features ?? new List<PlySemanticFeatureDefinition>())
        {
            if (feature == null || string.IsNullOrWhiteSpace(feature.id))
            {
                continue;
            }

            catalog[feature.id] = CreateAvailableFeatureDefinition(feature);
        }

        return catalog.Values.OrderBy(feature => feature.name, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private List<AvailableFeatureDefinition> GetActiveFeatureList()
    {
        return GetAvailableFeatureCatalog()
            .Where(feature => !feature.isBuiltIn || FindFeatureImplementation(feature.id) != null)
            .OrderBy(feature => feature.name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private List<AvailableFeatureDefinition> GetAddableCatalogFeatures()
    {
        IEnumerable<AvailableFeatureDefinition> query = GetAvailableFeatureCatalog()
            .Where(feature => feature.isBuiltIn && FindFeatureImplementation(feature.id) == null);

        if (!string.IsNullOrWhiteSpace(catalogAddSearchTerm))
        {
            string needle = catalogAddSearchTerm.Trim();
            query = query.Where(feature =>
                ContainsIgnoreCase(feature.name, needle) ||
                ContainsIgnoreCase(feature.id, needle) ||
                ContainsIgnoreCase(feature.description, needle) ||
                feature.tags.Any(tag => ContainsIgnoreCase(tag, needle)));
        }

        return query.OrderBy(feature => feature.name, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private PlyFeatureImplementation FindFeatureImplementation(string featureId)
    {
        return FeatureManifestState.implementations.FirstOrDefault(feature =>
            string.Equals(feature.featureId, featureId, StringComparison.OrdinalIgnoreCase));
    }

    private void CreateFeatureImplementation(AvailableFeatureDefinition definition)
    {
        if (definition == null || FindFeatureImplementation(definition.id) != null)
        {
            return;
        }

        PlyFeatureImplementation implementation = new PlyFeatureImplementation
        {
            id = BuildImplementationId(definition.id),
            featureId = definition.id,
            name = definition.name,
            description = definition.description,
            targetRoles = new List<string>(definition.targetRoles),
            tags = new List<string>(definition.tags),
            integrationMode = "bindings",
            source = new PlyFeatureImplementationSource
            {
                kind = "module",
                moduleId = moduleId ?? ""
            },
            capabilities = new PlyFeatureCapabilitySet
            {
                provides = new List<string>(definition.provides),
                requires = new List<string>(definition.requires)
            }
        };
        FeatureManifestState.implementations.Add(implementation);
        featureValidationDirty = true;
    }

    private void MergeAvailableFeatureCatalogIntoManifest()
    {
        Dictionary<string, PlySemanticFeatureDefinition> merged = FeatureManifestState.features
            .Where(feature => feature != null && !string.IsNullOrWhiteSpace(feature.id))
            .ToDictionary(feature => feature.id, feature => feature, StringComparer.OrdinalIgnoreCase);

        foreach (PlySemanticFeatureDefinition feature in GetBuiltInSemanticFeatures())
        {
            if (!merged.ContainsKey(feature.id))
            {
                merged[feature.id] = feature;
            }
        }

        FeatureManifestState.features = merged.Values
            .Select(PlyFeatureSchemaUtility.NormalizeFeatureDefinition)
            .OrderBy(feature => feature.name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private AvailableFeatureDefinition CreateAvailableFeatureDefinition(PlySemanticFeatureDefinition feature)
    {
        return new AvailableFeatureDefinition
        {
            id = feature.id,
            name = feature.name,
            description = feature.description,
            isBuiltIn = string.Equals(feature.origin, "catalog", StringComparison.OrdinalIgnoreCase),
            provides = new List<string>(feature.provides ?? new List<string>()),
            requires = new List<string>(feature.requires ?? new List<string>()),
            targetRoles = new List<string>(feature.targetRoles ?? new List<string>()),
            tags = new List<string>(feature.tags ?? new List<string>()),
            category = feature.category,
            intentExamples = new List<string>(feature.intentExamples ?? new List<string>()),
            ports = CreatePortMappings(feature),
            parameters = CreateParameterMappings(feature)
        };
    }

    private void CreateSemanticFeature()
    {
        int suffix = 1;
        string featureId;
        do
        {
            featureId = "custom_feature_" + suffix;
            suffix++;
        }
        while (FindSemanticFeature(featureId) != null);

        FeatureManifestState.features.Add(PlyFeatureSchemaUtility.NormalizeFeatureDefinition(new PlySemanticFeatureDefinition
        {
            id = featureId,
            name = "New Feature",
            description = "Describe this semantic feature.",
            origin = "user",
            category = "custom"
        }));

        List<AvailableFeatureDefinition> activeFeatures = GetActiveFeatureList();
        selectedFeatureCatalogIndex = activeFeatures.FindIndex(feature => string.Equals(feature.id, featureId, StringComparison.OrdinalIgnoreCase));
        if (selectedFeatureCatalogIndex < 0)
        {
            selectedFeatureCatalogIndex = 0;
        }

        featureValidationDirty = true;
    }

    private void DrawCatalogAddBrowser()
    {
        List<AvailableFeatureDefinition> addableFeatures = GetAddableCatalogFeatures();
        EditorGUILayout.BeginVertical("helpbox");
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Catalog", EditorStyles.miniBoldLabel);
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Close", GUILayout.Width(60f)))
        {
            showCatalogAddBrowser = false;
            GUIUtility.ExitGUI();
        }

        EditorGUILayout.EndHorizontal();
        catalogAddSearchTerm = EditorGUILayout.TextField("Search", catalogAddSearchTerm);

        if (addableFeatures.Count == 0)
        {
            EditorGUILayout.HelpBox("No catalog features match the current filter.", MessageType.Info);
        }
        else
        {
            catalogAddScroll = EditorGUILayout.BeginScrollView(catalogAddScroll, GUILayout.Height(180f));
            foreach (AvailableFeatureDefinition feature in addableFeatures)
            {
                string label = feature.name + " [" + feature.id + "]";
                if (GUILayout.Button(label, GUILayout.Height(28f)))
                {
                    CreateFeatureImplementation(feature);
                    List<AvailableFeatureDefinition> activeFeatures = GetActiveFeatureList();
                    selectedFeatureCatalogIndex = activeFeatures.FindIndex(entry => string.Equals(entry.id, feature.id, StringComparison.OrdinalIgnoreCase));
                    showCatalogAddBrowser = false;
                    GUIUtility.ExitGUI();
                }
            }

            EditorGUILayout.EndScrollView();
        }

        EditorGUILayout.EndVertical();
    }

    private void RemoveSelectedFeature(string featureId)
    {
        AvailableFeatureDefinition feature = GetAvailableFeatureCatalog()
            .FirstOrDefault(entry => string.Equals(entry.id, featureId, StringComparison.OrdinalIgnoreCase));
        if (feature == null)
        {
            return;
        }

        if (feature.isBuiltIn)
        {
            RemoveFeatureImplementation(featureId);
            return;
        }

        RemoveSemanticFeature(featureId);
    }

    private void RemoveFeatureImplementation(string featureId)
    {
        PlyFeatureImplementation implementation = FindFeatureImplementation(featureId);
        if (implementation != null)
        {
            FeatureManifestState.implementations.Remove(implementation);
            featureValidationDirty = true;
        }
    }

    private void RemoveSemanticFeature(string featureId)
    {
        PlySemanticFeatureDefinition feature = FindSemanticFeature(featureId);
        if (string.IsNullOrWhiteSpace(featureId) || feature == null || string.Equals(feature.origin, "catalog", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (feature != null)
        {
            FeatureManifestState.features.Remove(feature);
        }

        PlyFeatureImplementation implementation = FindFeatureImplementation(featureId);
        if (implementation != null)
        {
            FeatureManifestState.implementations.Remove(implementation);
        }

        featureValidationDirty = true;
    }

    private PlySemanticFeatureDefinition FindSemanticFeature(string featureId)
    {
        return FeatureManifestState.features.FirstOrDefault(feature =>
            string.Equals(feature.id, featureId, StringComparison.OrdinalIgnoreCase));
    }

    private List<PlySemanticFeatureDefinition> GetBuiltInSemanticFeatures()
    {
        string catalogPath = GetDefaultFeatureCatalogPath();
        if (cachedDefaultFeatureCatalog != null &&
            string.Equals(cachedDefaultFeatureCatalogPath, catalogPath, StringComparison.OrdinalIgnoreCase))
        {
            return cachedDefaultFeatureCatalog
                .Select(CloneSemanticFeatureAsCatalog)
                .ToList();
        }

        List<PlySemanticFeatureDefinition> loadedCatalog = new List<PlySemanticFeatureDefinition>();
        try
        {
            if (File.Exists(catalogPath))
            {
                loadedCatalog = (PlyFeatureJson.ImportFromFile(catalogPath).features ?? new List<PlySemanticFeatureDefinition>())
                    .Where(feature => feature != null && !string.IsNullOrWhiteSpace(feature.id))
                    .Select(CloneSemanticFeatureAsCatalog)
                    .ToList();
            }
        }
        catch (Exception exception)
        {
            Debug.LogWarning("Failed to load default feature catalog from " + catalogPath + ": " + exception.Message);
        }

        if (loadedCatalog.Count == 0)
        {
            Debug.LogWarning("Default feature catalog is empty or could not be loaded from " + catalogPath + ".");
        }

        cachedDefaultFeatureCatalogPath = catalogPath;
        cachedDefaultFeatureCatalog = loadedCatalog
            .Select(CloneSemanticFeatureAsCatalog)
            .ToList();
        return loadedCatalog;
    }

    private string GetDefaultFeatureCatalogPath()
    {
        return Path.Combine(Directory.GetCurrentDirectory(), DefaultFeatureCatalogRelativePath.Replace('/', Path.DirectorySeparatorChar));
    }

    private PlySemanticFeatureDefinition CloneSemanticFeatureAsCatalog(PlySemanticFeatureDefinition feature)
    {
        PlySemanticFeatureDefinition clone = feature == null
            ? new PlySemanticFeatureDefinition()
            : JsonUtility.FromJson<PlySemanticFeatureDefinition>(JsonUtility.ToJson(feature)) ?? new PlySemanticFeatureDefinition();
        clone.origin = "catalog";
        return PlyFeatureSchemaUtility.NormalizeFeatureDefinition(clone);
    }

    private List<PlyFeaturePortMapping> CreatePortMappings(PlySemanticFeatureDefinition feature)
    {
        List<PlyFeaturePortMapping> mappings = new List<PlyFeaturePortMapping>();
        foreach (PlySemanticFeaturePort input in feature.inputs ?? new List<PlySemanticFeaturePort>())
        {
            mappings.Add(new PlyFeaturePortMapping
            {
                name = input.name,
                direction = PlyFeaturePortDirection.Input,
                kind = input.kind,
                dataType = input.dataType
            });
        }

        foreach (PlySemanticFeaturePort output in feature.outputs ?? new List<PlySemanticFeaturePort>())
        {
            mappings.Add(new PlyFeaturePortMapping
            {
                name = output.name,
                direction = PlyFeaturePortDirection.Output,
                kind = output.kind,
                dataType = output.dataType
            });
        }

        return mappings;
    }

    private List<PlyFeatureParameterMapping> CreateParameterMappings(PlySemanticFeatureDefinition feature)
    {
        return (feature.parameters ?? new List<PlySemanticFeatureParameter>())
            .Select(parameter => new PlyFeatureParameterMapping
            {
                name = parameter.name,
                type = parameter.type,
                defaultValue = parameter.defaultValue,
                required = parameter.required,
                accessMode = PlyFeatureParameterAccess.ReadWrite
            })
            .ToList();
    }

    private PlyFeaturePortBinding GetOrCreateInputBinding(PlyFeatureImplementation implementation, string featureInput)
    {
        implementation.inputBindings ??= new List<PlyFeaturePortBinding>();
        PlyFeaturePortBinding binding = implementation.inputBindings.FirstOrDefault(entry =>
            string.Equals(entry.featureInput, featureInput, StringComparison.OrdinalIgnoreCase));
        if (binding != null)
        {
            return binding;
        }

        binding = new PlyFeaturePortBinding { featureInput = featureInput, binding = new PlyFeatureBinding() };
        implementation.inputBindings.Add(binding);
        return binding;
    }

    private PlyFeaturePortBinding GetOrCreateOutputBinding(PlyFeatureImplementation implementation, string featureOutput)
    {
        implementation.outputBindings ??= new List<PlyFeaturePortBinding>();
        PlyFeaturePortBinding binding = implementation.outputBindings.FirstOrDefault(entry =>
            string.Equals(entry.featureOutput, featureOutput, StringComparison.OrdinalIgnoreCase));
        if (binding != null)
        {
            return binding;
        }

        binding = new PlyFeaturePortBinding { featureOutput = featureOutput, binding = new PlyFeatureBinding() };
        implementation.outputBindings.Add(binding);
        return binding;
    }

    private PlyFeatureParameterBinding GetOrCreateParameterBinding(PlyFeatureImplementation implementation, string featureParameter)
    {
        implementation.parameterBindings ??= new List<PlyFeatureParameterBinding>();
        PlyFeatureParameterBinding binding = implementation.parameterBindings.FirstOrDefault(entry =>
            string.Equals(entry.featureParameter, featureParameter, StringComparison.OrdinalIgnoreCase));
        if (binding != null)
        {
            return binding;
        }

        binding = new PlyFeatureParameterBinding { featureParameter = featureParameter, binding = new PlyFeatureBinding() };
        implementation.parameterBindings.Add(binding);
        return binding;
    }

    private PlyFeaturePortMapping CreateBoundPortMapping(PlyFeaturePortMapping source, PlyFeatureBinding binding)
    {
        return new PlyFeaturePortMapping
        {
            name = source.name,
            direction = source.direction,
            kind = source.kind,
            dataType = source.dataType,
            binding = binding ?? new PlyFeatureBinding()
        };
    }

    private PlyFeatureParameterMapping CreateBoundParameterMapping(PlyFeatureParameterMapping source, PlyFeatureBinding binding)
    {
        return new PlyFeatureParameterMapping
        {
            name = source.name,
            direction = source.direction,
            type = source.type,
            defaultValue = source.defaultValue,
            required = source.required,
            accessMode = source.accessMode,
            binding = binding ?? new PlyFeatureBinding()
        };
    }

    private string DrawStringPopup(string label, string currentValue, string[] options)
    {
        int index = Array.FindIndex(options, option => string.Equals(option, currentValue, StringComparison.OrdinalIgnoreCase));
        index = Mathf.Max(0, index);
        int newIndex = EditorGUILayout.Popup(label, index, options);
        return options[Mathf.Clamp(newIndex, 0, options.Length - 1)];
    }

    private string BuildImplementationId(string featureId)
    {
        return NormalizeFeatureToken(!string.IsNullOrWhiteSpace(moduleName) ? moduleName : moduleId) + "." + featureId;
    }

    private string NormalizeFeatureToken(string value)
    {
        string normalized = new string((value ?? "")
            .Trim()
            .ToLowerInvariant()
            .Select(character => char.IsLetterOrDigit(character) ? character : '_')
            .ToArray())
            .Trim('_');

        return string.IsNullOrWhiteSpace(normalized) ? "module" : normalized;
    }

    private void DrawEditableStringList(string label, List<string> values)
    {
        values ??= new List<string>();
        string current = string.Join(", ", values.ToArray());
        string updated = EditorGUILayout.TextField(label, current);
        if (!string.Equals(current, updated, StringComparison.Ordinal))
        {
            values.Clear();
            values.AddRange(updated.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(value => value.Trim())
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(value => value, StringComparer.OrdinalIgnoreCase));
        }
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
