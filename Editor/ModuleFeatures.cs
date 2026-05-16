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
    private static readonly HashSet<string> UnityLifecycleMethods = new HashSet<string>(StringComparer.Ordinal)
    {
        "Awake", "Start", "Update", "LateUpdate", "FixedUpdate", "OnEnable", "OnDisable"
    };

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
        public string aiMatchDescription;
        public List<string> implements = new List<string>();
        public List<string> provides = new List<string>();
        public List<string> consumes = new List<string>();
        public List<string> targetRoles = new List<string>();
        public List<string> tags = new List<string>();
        public List<string> categories = new List<string>();
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
    private List<PlyFeatureValidationIssue> featureValidationIssues = new List<PlyFeatureValidationIssue>();
    private bool includeLifecycleMethods;
    private string componentSearchTerm = "";
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

        EditorGUILayout.HelpBox("Map existing Plyground features to existing project components. Choose a feature from the catalog, then bind its Inputs, Outputs, and Parameters to compatible project members.", MessageType.Info);

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
        List<AvailableFeatureDefinition> catalog = GetAvailableFeatureCatalog();
        selectedFeatureCatalogIndex = Mathf.Clamp(selectedFeatureCatalogIndex < 0 ? 0 : selectedFeatureCatalogIndex, 0, Mathf.Max(0, catalog.Count - 1));
        AvailableFeatureDefinition selectedFeature = catalog.Count > 0 ? catalog[selectedFeatureCatalogIndex] : null;
        PlyFeatureProfile implementation = selectedFeature != null ? FindFeatureImplementation(selectedFeature.id) : null;

        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.BeginVertical("box", GUILayout.Width(Mathf.Max(230f, position.width * 0.22f)), GUILayout.Height(640f));
        GUILayout.Label("Available", EditorStyles.boldLabel);
        featureListScroll = EditorGUILayout.BeginScrollView(featureListScroll);
        foreach (AvailableFeatureDefinition feature in catalog)
        {
            bool implemented = FindFeatureImplementation(feature.id) != null;
            string label = feature.name + (implemented ? " [Mapped]" : "");
            int index = catalog.IndexOf(feature);
            if (GUILayout.Button(label, selectedFeatureCatalogIndex == index ? EditorStyles.toolbarButton : GUI.skin.button, GUILayout.Height(32f)))
            {
                selectedFeatureCatalogIndex = index;
            }
        }
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();

        EditorGUILayout.BeginVertical("box", GUILayout.Width(Mathf.Max(300f, position.width * 0.30f)), GUILayout.Height(640f));
        GUILayout.Label("Selected", EditorStyles.boldLabel);
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
            EditorGUILayout.HelpBox("Select a feature to map.", MessageType.Info);
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
        EditorGUILayout.LabelField("Id", feature.id);
        EditorGUILayout.LabelField("Name", feature.name);
        EditorGUILayout.LabelField("Description", feature.description, EditorStyles.wordWrappedLabel);
        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField("Implements", string.Join(", ", feature.implements.ToArray()));
        EditorGUILayout.LabelField("Provides", string.Join(", ", feature.provides.ToArray()));
        EditorGUILayout.LabelField("Consumes", string.Join(", ", feature.consumes.ToArray()));
        EditorGUILayout.LabelField("Target Roles", string.Join(", ", feature.targetRoles.ToArray()));
        EditorGUILayout.LabelField("Status", implemented ? "Mapped in this module" : "Not mapped");
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

    private void DrawFeatureImplementationEditor(AvailableFeatureDefinition definition, PlyFeatureProfile profile)
    {
        if (GetSelectedCapabilityComponents().Count == 0)
        {
            EditorGUILayout.HelpBox("No components are selected in Capabilities > Components. Add module components there before creating feature mappings.", MessageType.Info);
            return;
        }

        if (profile == null)
        {
            EditorGUILayout.HelpBox("This feature has not been mapped in the current module yet.", MessageType.Info);
            if (GUILayout.Button("Implement Feature", GUILayout.Width(150f)))
            {
                CreateFeatureImplementation(definition);
            }
            return;
        }

        profile = PlyFeatureSchemaUtility.NormalizeFeature(profile);
        profile.id = EditorGUILayout.TextField("Binding Profile Id", profile.id);
        componentSearchTerm = EditorGUILayout.TextField("Component Search", componentSearchTerm);
        EditorGUILayout.Space(6f);

        DrawInputMappingsSection(profile);
        EditorGUILayout.Space(8f);
        DrawOutputMappingsSection(profile);
        EditorGUILayout.Space(8f);
        DrawParameterMappingsSection(profile);
        EditorGUILayout.Space(8f);

        if (GUILayout.Button("Remove Mapping Profile", GUILayout.Width(170f)))
        {
            FeatureManifestState.features.Remove(profile);
            featureValidationDirty = true;
        }
    }

    private void DrawInputMappingsSection(PlyFeatureProfile profile)
    {
        GUILayout.Label("Inputs", EditorStyles.boldLabel);
        foreach (PlyFeaturePortMapping port in profile.ports.Where(entry => entry.direction == PlyFeaturePortDirection.Input))
        {
            DrawPortMappingRow(port, FeatureMappingSection.Input);
        }
    }

    private void DrawOutputMappingsSection(PlyFeatureProfile profile)
    {
        GUILayout.Label("Outputs", EditorStyles.boldLabel);
        foreach (PlyFeaturePortMapping port in profile.ports.Where(entry => entry.direction == PlyFeaturePortDirection.Output))
        {
            DrawPortMappingRow(port, FeatureMappingSection.Output);
        }
    }

    private void DrawParameterMappingsSection(PlyFeatureProfile profile)
    {
        GUILayout.Label("Parameters", EditorStyles.boldLabel);
        foreach (PlyFeatureParameterMapping parameter in profile.parameters)
        {
            DrawParameterMappingRow(parameter);
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
        for (int i = 0; i < manifest.features.Count; i++)
        {
            PlyFeatureProfile profile = manifest.features[i];
            string path = "Feature[" + i + "]";
            if (string.IsNullOrWhiteSpace(profile.featureId))
            {
                issues.Add(CreateIssue("error", path, "Feature catalog id is required."));
            }
            else if (!featureIds.Add(profile.featureId))
            {
                issues.Add(CreateIssue("error", path, "Only one binding profile is allowed per feature."));
            }

            foreach (PlyFeaturePortMapping port in profile.ports.Where(entry => entry.direction == PlyFeaturePortDirection.Input))
            {
                ValidatePortBinding(port, FeatureMappingSection.Input, issues);
            }

            foreach (PlyFeaturePortMapping port in profile.ports.Where(entry => entry.direction == PlyFeaturePortDirection.Output))
            {
                ValidatePortBinding(port, FeatureMappingSection.Output, issues);
            }

            foreach (PlyFeatureParameterMapping parameter in profile.parameters)
            {
                ValidateParameterBinding(parameter, issues);
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
        string filePath = EditorUtility.OpenFilePanel("Import Feature Binding Profile", Application.dataPath, "json");
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
        string filePath = EditorUtility.SaveFilePanel("Export Feature Binding Profile", Application.dataPath, "feature-bindings.json", "json");
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
        return new List<AvailableFeatureDefinition>
        {
            new AvailableFeatureDefinition
            {
                id = "enemy_aggression",
                name = "Enemy Aggression",
                description = "Maps hostile AI aggression controls and state into semantic gameplay bindings.",
                aiMatchDescription = "guard ai, enemy aggression, hostile npc, combat ai",
                implements = new List<string> { "enemy_aggression" },
                provides = new List<string> { "aggression_control" },
                consumes = new List<string> { "spotted_state" },
                targetRoles = new List<string> { "Enemy" },
                ports = new List<PlyFeaturePortMapping>
                {
                    new PlyFeaturePortMapping { name = "SetAggressive", direction = PlyFeaturePortDirection.Input, kind = PlyFeaturePortKind.Action, dataType = PlyFeatureDataType.Void },
                    new PlyFeaturePortMapping { name = "ApplyThreat", direction = PlyFeaturePortDirection.Input, kind = PlyFeaturePortKind.Action, dataType = PlyFeatureDataType.Float },
                    new PlyFeaturePortMapping { name = "OnTargetVisible", direction = PlyFeaturePortDirection.Output, kind = PlyFeaturePortKind.Event, dataType = PlyFeatureDataType.Void },
                    new PlyFeaturePortMapping { name = "OnAggroChanged", direction = PlyFeaturePortDirection.Output, kind = PlyFeaturePortKind.Event, dataType = PlyFeatureDataType.Float }
                },
                parameters = new List<PlyFeatureParameterMapping>
                {
                    new PlyFeatureParameterMapping { name = "AggroRadius", type = PlyFeatureDataType.Float, accessMode = PlyFeatureParameterAccess.ReadWrite, defaultValue = "20" }
                }
            },
            new AvailableFeatureDefinition
            {
                id = "health_state",
                name = "Health State",
                description = "Maps health values, death events, and health configuration into semantic gameplay bindings.",
                aiMatchDescription = "health, hp, damageable, hit points",
                implements = new List<string> { "health_state" },
                provides = new List<string> { "health_value" },
                targetRoles = new List<string> { "Enemy", "Player", "NPC" },
                ports = new List<PlyFeaturePortMapping>
                {
                    new PlyFeaturePortMapping { name = "ApplyDamage", direction = PlyFeaturePortDirection.Input, kind = PlyFeaturePortKind.Action, dataType = PlyFeatureDataType.Float },
                    new PlyFeaturePortMapping { name = "OnDeath", direction = PlyFeaturePortDirection.Output, kind = PlyFeaturePortKind.Event, dataType = PlyFeatureDataType.Void },
                    new PlyFeaturePortMapping { name = "OnHealthChanged", direction = PlyFeaturePortDirection.Output, kind = PlyFeaturePortKind.Event, dataType = PlyFeatureDataType.Float }
                },
                parameters = new List<PlyFeatureParameterMapping>
                {
                    new PlyFeatureParameterMapping { name = "MaxHealth", type = PlyFeatureDataType.Float, accessMode = PlyFeatureParameterAccess.ReadWrite, defaultValue = "100" },
                    new PlyFeatureParameterMapping { name = "CurrentHealth", type = PlyFeatureDataType.Float, accessMode = PlyFeatureParameterAccess.ReadOnly, defaultValue = "" }
                }
            },
            new AvailableFeatureDefinition
            {
                id = "interaction_prompt",
                name = "Interaction Prompt",
                description = "Maps interactable prompt text and availability state into semantic gameplay bindings.",
                aiMatchDescription = "interact prompt, use prompt, pickup prompt",
                implements = new List<string> { "interaction_prompt" },
                provides = new List<string> { "prompt_text" },
                targetRoles = new List<string> { "Interactable" },
                ports = new List<PlyFeaturePortMapping>
                {
                    new PlyFeaturePortMapping { name = "OnPromptShown", direction = PlyFeaturePortDirection.Output, kind = PlyFeaturePortKind.Event, dataType = PlyFeatureDataType.String }
                },
                parameters = new List<PlyFeatureParameterMapping>
                {
                    new PlyFeatureParameterMapping { name = "PromptText", type = PlyFeatureDataType.String, accessMode = PlyFeatureParameterAccess.ReadWrite, defaultValue = "" },
                    new PlyFeatureParameterMapping { name = "CanInteract", type = PlyFeatureDataType.Bool, accessMode = PlyFeatureParameterAccess.ReadOnly, defaultValue = "" }
                }
            }
        };
    }

    private PlyFeatureProfile FindFeatureImplementation(string featureId)
    {
        return FeatureManifestState.features.FirstOrDefault(feature =>
            string.Equals(feature.featureId, featureId, StringComparison.OrdinalIgnoreCase));
    }

    private void CreateFeatureImplementation(AvailableFeatureDefinition definition)
    {
        if (definition == null || FindFeatureImplementation(definition.id) != null)
        {
            return;
        }

        PlyFeatureProfile profile = new PlyFeatureProfile
        {
            id = "binding." + definition.id,
            featureId = definition.id,
            name = definition.name,
            description = definition.description,
            aiMatchDescription = definition.aiMatchDescription,
            implements = new List<string>(definition.implements),
            provides = new List<string>(definition.provides),
            consumes = new List<string>(definition.consumes),
            targetRoles = new List<string>(definition.targetRoles),
            tags = new List<string>(definition.tags),
            categories = new List<string>(definition.categories),
            ports = ClonePorts(definition.ports),
            parameters = CloneParameters(definition.parameters)
        };
        FeatureManifestState.features.Add(profile);
        featureValidationDirty = true;
    }

    private List<PlyFeaturePortMapping> ClonePorts(List<PlyFeaturePortMapping> source)
    {
        return (source ?? new List<PlyFeaturePortMapping>())
            .Select(port => new PlyFeaturePortMapping
            {
                name = port.name,
                direction = port.direction,
                kind = port.kind,
                dataType = port.dataType,
                binding = new PlyFeatureBinding()
            })
            .ToList();
    }

    private List<PlyFeatureParameterMapping> CloneParameters(List<PlyFeatureParameterMapping> source)
    {
        return (source ?? new List<PlyFeatureParameterMapping>())
            .Select(parameter => new PlyFeatureParameterMapping
            {
                name = parameter.name,
                direction = string.IsNullOrWhiteSpace(parameter.direction) ? "parameter" : parameter.direction,
                type = parameter.type,
                defaultValue = parameter.defaultValue,
                accessMode = parameter.accessMode,
                binding = new PlyFeatureBinding()
            })
            .ToList();
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
