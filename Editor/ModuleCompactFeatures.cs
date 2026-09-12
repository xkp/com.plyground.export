using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public partial class ModuleExporter
{
    [SerializeField] private CompactFeatureSchema compactFeatures = new CompactFeatureSchema();
    private Vector2 compactFeatureScroll;
    private int compactTab;

    private void DrawCompactFeaturesTab()
    {
        compactFeatures = compactFeatures ?? new CompactFeatureSchema();
        EditorGUILayout.HelpBox("Map canonical feature names to Unity components. Describe each component once. Descriptions and aliases for feature matching belong in the shared catalog.", MessageType.Info);
        compactTab = GUILayout.Toolbar(compactTab, new[] { "Feature mappings", "Component APIs" });
        compactFeatureScroll = EditorGUILayout.BeginScrollView(compactFeatureScroll);
        if (compactTab == 0) DrawCompactMappings();
        else DrawCompactApis();
        EditorGUILayout.EndScrollView();
        try { compactFeatures.Validate(); }
        catch (Exception error) { EditorGUILayout.HelpBox(error.Message, MessageType.Error); }
    }

    private void DrawCompactMappings()
    {
        if (GUILayout.Button("Add feature mapping")) compactFeatures.features.Add(new CompactFeatureMapping());
        for (int i = 0; i < compactFeatures.features.Count; i++)
        {
            var feature = compactFeatures.features[i];
            EditorGUILayout.BeginVertical("box");
            feature.name = EditorGUILayout.TextField("Feature name", feature.name);
            var choices = new[] { "Select a component API" }.Concat(compactFeatures.components.Select(api => api.component)).ToArray();
            int current = Array.IndexOf(choices, feature.component);
            int selected = EditorGUILayout.Popup("Component", Math.Max(0, current), choices);
            if (selected > 0) feature.component = choices[selected];
            else if (current >= 0) feature.component = "";
            if (current < 0 && !string.IsNullOrEmpty(feature.component)) EditorGUILayout.HelpBox("Missing component: " + feature.component, MessageType.Error);
            if (GUILayout.Button("Remove mapping")) { compactFeatures.features.RemoveAt(i); i--; }
            EditorGUILayout.EndVertical();
        }
    }

    private void DrawCompactApis()
    {
        EditorGUILayout.HelpBox("Methods and properties use public C# signatures. Config is serialized configuration, not public setters. Publishes/consumes contain GameplayBus event IDs, not C# event names. Enter one signature, ID, or note per line.", MessageType.Info);
        if (GUILayout.Button("Add component API")) compactFeatures.components.Add(new CompactComponentApi());
        for (int i = 0; i < compactFeatures.components.Count; i++)
        {
            var api = compactFeatures.components[i];
            EditorGUILayout.BeginVertical("box");
            string previousType = api.component;
            api.component = EditorGUILayout.TextField("Component type", api.component);
            MonoScript script = EditorGUILayout.ObjectField("Read API from script", null, typeof(MonoScript), false) as MonoScript;
            if (script != null)
            {
                var type = script.GetClass();
                if (type == null || !typeof(MonoBehaviour).IsAssignableFrom(type) || type.IsAbstract || type.ContainsGenericParameters)
                    EditorUtility.DisplayDialog("Component API", "Choose a compiled, concrete MonoBehaviour script.", "OK");
                else
                {
                    api.component = type.FullName;
                    ReadCompactMembers(type, api);
                }
            }
            if (api.component != previousType && !string.IsNullOrEmpty(previousType))
                foreach (var mapping in compactFeatures.features.Where(mapping => mapping.component == previousType)) mapping.component = api.component;
            foreach (string section in CompactFeatureSchema.Sections)
            {
                EditorGUILayout.LabelField(ObjectNames.NicifyVariableName(section));
                var entries = CompactFeatureSchema.Section(api, section);
                string value = string.Join("\n", entries);
                string edited = EditorGUILayout.TextArea(value, GUILayout.MinHeight(40));
                // Preserve an unfinished trailing line so Enter works while editing a multiline list.
                if (edited != value) { entries.Clear(); if (edited.Length > 0) entries.AddRange(edited.Split('\n').Select(line => line.Trim())); }
            }
            bool used = compactFeatures.features.Any(mapping => mapping.component == api.component);
            using (new EditorGUI.DisabledScope(used))
                if (GUILayout.Button("Remove component API")) { compactFeatures.components.RemoveAt(i); i--; }
            if (used) EditorGUILayout.LabelField("Remove its feature mappings before deleting this component.", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
        }
    }

    // Reflection preserves overloads and includes inherited user-component members, excluding Unity's base API.
    public static void ReadCompactMembers(Type type, CompactComponentApi api)
    {
        api.methods.Clear(); api.config.Clear(); api.properties.Clear();
        for (Type current = type; current != null && current != typeof(MonoBehaviour); current = current.BaseType)
        {
            var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
            foreach (var method in current.GetMethods(flags).Where(method => method.IsPublic && !method.IsSpecialName && !method.ContainsGenericParameters))
            {
                var parameters = method.GetParameters().Select(parameter =>
                    (parameter.IsOut ? "out " : parameter.ParameterType.IsByRef ? (parameter.IsIn ? "in " : "ref ") : "") +
                    CompactTypeName(parameter.ParameterType.IsByRef ? parameter.ParameterType.GetElementType() : parameter.ParameterType) + " " + parameter.Name);
                api.methods.Add((method.IsStatic ? "static " : "") + CompactTypeName(method.ReturnType) + " " + method.Name + "(" + string.Join(", ", parameters) + ")");
            }
            foreach (var field in current.GetFields(flags).Where(field => !field.IsStatic && !field.IsInitOnly && !field.IsLiteral && !field.IsNotSerialized &&
                (field.IsPublic || field.IsDefined(typeof(SerializeField), true) || field.IsDefined(typeof(SerializeReference), true))))
                api.config.Add(CompactTypeName(field.FieldType) + " " + field.Name);
            foreach (var property in current.GetProperties(flags).Where(property => property.GetIndexParameters().Length == 0))
            {
                var get = property.GetGetMethod(); var set = property.GetSetMethod();
                if (get == null && set == null) continue;
                api.properties.Add(((get ?? set).IsStatic ? "static " : "") + CompactTypeName(property.PropertyType) + " " + property.Name + " { " + (get != null ? "get; " : "") + (set != null ? "set; " : "") + "}");
            }
        }
        api.methods = api.methods.Distinct().OrderBy(value => value).ToList();
        api.config = api.config.Distinct().OrderBy(value => value).ToList();
        api.properties = api.properties.Distinct().OrderBy(value => value).ToList();
        // Event IDs and setup notes are authored; ordinary C# events do not prove GameplayBus behavior.
    }

    private static string CompactTypeName(Type type)
    {
        if (type.IsByRef) return "ref " + CompactTypeName(type.GetElementType());
        if (type.IsArray) return CompactTypeName(type.GetElementType()) + "[" + new string(',', type.GetArrayRank() - 1) + "]";
        if (type == typeof(void)) return "void";
        if (type == typeof(bool)) return "bool";
        if (type == typeof(int)) return "int";
        if (type == typeof(float)) return "float";
        if (type == typeof(string)) return "string";
        if (type.IsGenericType)
        {
            if (type.GetGenericTypeDefinition() == typeof(Nullable<>)) return CompactTypeName(type.GetGenericArguments()[0]) + "?";
            return (type.GetGenericTypeDefinition().FullName ?? type.Name).Split('`')[0].Replace('+', '.') + "<" + string.Join(", ", type.GetGenericArguments().Select(CompactTypeName)) + ">";
        }
        return type.Namespace == "UnityEngine" ? type.Name : (type.FullName ?? type.Name).Replace('+', '.');
    }
}
