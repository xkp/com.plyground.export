using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

[Serializable]
public sealed class CompactFeatureMapping
{
    public string name = "";
    public string component = "";
}

[Serializable]
public sealed class CompactComponentApi
{
    public string component = "";
    public ModuleExporter.CapabilityComponentEntryV2 editor;
    public List<string> methods = new List<string>();
    public List<string> config = new List<string>();
    public List<string> properties = new List<string>();
    public List<string> publishes = new List<string>();
    public List<string> consumes = new List<string>();
    public List<string> notes = new List<string>();
}

[Serializable]
public sealed class CompactFeatureSchema
{
    public List<CompactFeatureMapping> features = new List<CompactFeatureMapping>();
    public List<CompactComponentApi> components = new List<CompactComponentApi>();
    public static readonly string[] Sections = { "methods", "config", "properties", "publishes", "consumes", "notes" };
    public static string NameKey(string name) { return Regex.Replace((name ?? "").Trim(), @"\s+", " ").ToLowerInvariant(); }
    public static List<string> Section(CompactComponentApi api, string section)
    {
        return (List<string>)typeof(CompactComponentApi).GetField(section).GetValue(api);
    }

    public void Validate()
    {
        var names = new HashSet<string>();
        var types = new HashSet<string>(StringComparer.Ordinal);
        foreach (var api in components)
        {
            if (api == null || !Regex.IsMatch(api.component ?? "", @"^[A-Za-z_]\w*(\.[A-Za-z_]\w*)+$"))
                throw new InvalidDataException("Components need a fully qualified C# type name.");
            if (api.editor != null && (api.editor.typeName != api.component || api.editor.id != api.component))
                throw new InvalidDataException("Editor component identity must match its API key.");
            if (!types.Add(api.component)) throw new InvalidDataException("Duplicate component: " + api.component);
            foreach (var section in Sections)
                if (Section(api, section) == null || Section(api, section).Any(string.IsNullOrWhiteSpace))
                    throw new InvalidDataException(api.component + ": " + section + " must contain non-empty strings.");
            if (!Sections.Any(section => Section(api, section).Count > 0))
                throw new InvalidDataException(api.component + ": document its API or add a setup note for a component with no callable members.");
        }
        foreach (var feature in features)
        {
            if (feature == null || string.IsNullOrWhiteSpace(feature.name)) throw new InvalidDataException("Feature name is required.");
            if (!names.Add(NameKey(feature.name))) throw new InvalidDataException("Duplicate feature: " + feature.name);
            if (!types.Contains(feature.component ?? "")) throw new InvalidDataException(feature.name + ": select a documented component.");
        }
    }

    public static CompactFeatureSchema Import(string json)
    {
        var root = PlyFeatureJson.ParseObject(json);
        foreach (var old in new[] { "capabilities", "featureApis", "featureImplementations", "supportedFeatures", "implementations" })
            if (root.ContainsKey(old)) throw new InvalidDataException("Legacy " + old + " is not supported. Use features and components mappings.");
        if (!root.ContainsKey("features") || !root.ContainsKey("components"))
            throw new InvalidDataException("Both features and components objects are required.");
        var mappings = root["features"] as Dictionary<string, object>;
        var definitions = root["components"] as Dictionary<string, object>;
        if (mappings == null || definitions == null) throw new InvalidDataException("features and components must be objects, not arrays.");
        var result = new CompactFeatureSchema();
        foreach (var entry in mappings)
        {
            if (!(entry.Value is string)) throw new InvalidDataException("Feature mappings must name a component.");
            result.features.Add(new CompactFeatureMapping { name = entry.Key, component = (string)entry.Value });
        }
        foreach (var entry in definitions)
        {
            var fields = entry.Value as Dictionary<string, object>;
            if (fields == null || fields.Keys.Any(key => !Sections.Contains(key) && key != "editor")) throw new InvalidDataException("Invalid compact API: " + entry.Key);
            var api = new CompactComponentApi { component = entry.Key };
            foreach (var field in fields)
            {
                if (field.Key == "editor")
                {
                    if (!(field.Value is Dictionary<string, object>)) throw new InvalidDataException("editor must be an object");
                    api.editor = UnityEngine.JsonUtility.FromJson<ModuleExporter.CapabilityComponentEntryV2>(PlyFeatureJson.SerializeValue(field.Value));
                    continue;
                }
                var values = field.Value as List<object>;
                if (values == null || values.Any(value => !(value is string))) throw new InvalidDataException(field.Key + " must be an array of strings.");
                Section(api, field.Key).AddRange(values.Cast<string>());
            }
            result.components.Add(api);
        }
        result.Validate();
        return result;
    }

    public string Export()
    {
        Validate();
        var maps = features.Select(feature => Quote(Regex.Replace(feature.name.Trim(), @"\s+", " ")) + ":" + Quote(feature.component));
        var apis = components.Select(api => {
            var fields = Sections.Where(section => Section(api, section).Count > 0)
                .Select(section => Quote(section) + ":[" + string.Join(",", Section(api, section).Select(Quote)) + "]").ToList();
            if (api.editor != null) fields.Add("\"editor\":" + UnityEngine.JsonUtility.ToJson(api.editor));
            return Quote(api.component) + ":{" + string.Join(",", fields) + "}";
        });
        return "{\"features\":{" + string.Join(",", maps) + "},\"components\":{" + string.Join(",", apis) + "}}";
    }

    public string AppendToModule(string moduleJson)
    {
        string schema = Export();
        string module = moduleJson.TrimEnd();
        return module.Substring(0, module.Length - 1) + "," + schema.Substring(1);
    }

    private static string Quote(string value)
    {
        var text = new StringBuilder("\"");
        foreach (char ch in value)
        {
            if (ch == '"' || ch == '\\') text.Append('\\').Append(ch);
            else if (ch < 32) text.Append("\\u").Append(((int)ch).ToString("x4"));
            else text.Append(ch);
        }
        return text.Append('"').ToString();
    }
}
