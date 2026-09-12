using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public class CompactApiFixture : MonoBehaviour
{
    [SerializeField] private GameObject[] prefabs;
    [NonSerialized] public int transient;
    public bool Ready { get; private set; }
    public GameObject Spawn() { return null; }
    public GameObject Spawn(int count) { return null; }
    public static void ResetAll() { }
}

public static class CompactFeatureSmoke
{
    private static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    private static void Reject(string json)
    {
        try { CompactFeatureSchema.Import(json); }
        catch (InvalidDataException) { return; }
        throw new Exception("Accepted invalid schema: " + json);
    }
    public static void Run()
    {
        try
        {
            var schema = new CompactFeatureSchema();
            var api = new CompactComponentApi { component = "Combat.Spawner" };
            ModuleExporter.ReadCompactMembers(typeof(CompactApiFixture), api);
            api.publishes.Add("combat.prefab.spawned");
            api.notes.Add("Quoted \"text\" and newline\nwith slash\\.");
            schema.components.Add(api);
            schema.features.Add(new CompactFeatureMapping { name = "Prefab Spawner", component = api.component });
            schema.features.Add(new CompactFeatureMapping { name = "Enemy Spawner", component = api.component });
            Check(api.methods.Contains("GameObject Spawn()") && api.methods.Contains("GameObject Spawn(int count)"), "Overloads lost");
            Check(api.methods.Contains("static void ResetAll()"), "Static modifier lost");
            Check(api.config.Contains("GameObject[] prefabs") && !api.config.Any(value => value.Contains("transient")), "Config visibility is wrong");
            Check(api.properties.Contains("bool Ready { get; }"), "Private setter exposed");
            var roundTrip = CompactFeatureSchema.Import(schema.Export());
            Check(roundTrip.features.Count == 2 && roundTrip.components.Count == 1, "Shared component duplicated");
            Check(roundTrip.components[0].notes[0] == api.notes[0], "Escaping changed text");
            Check(!schema.Export().Contains("\"consumes\""), "Empty section emitted");
            var moduleJson = schema.AppendToModule("{\"id\":\"COMBAT\",\"itemGroups\":[]}");
            var root = PlyFeatureJson.ParseObject(moduleJson);
            Check(root.ContainsKey("features") && root.ContainsKey("components") && !root.ContainsKey("capabilities"), "Wrong module shape");
            Check((string)root["id"] == "COMBAT", "Module metadata lost");
            Reject("{\"features\":[],\"components\":{}}");
            Reject("{\"features\":{\"Spawner\":\"Missing.Type\"},\"components\":{}}");
            Reject("{\"features\":{},\"components\":{},\"capabilities\":{}}");
            Reject("{\"features\":{},\"features\":{},\"components\":{}}");
            Reject("{\"features\":{},\"components\":{\"Combat.Spawner\":{\"methods\":\"Spawn\"}}}");
            schema.features.Add(new CompactFeatureMapping { name = " prefab   SPAWNER ", component = api.component });
            try { schema.Validate(); throw new Exception("Duplicate normalized feature accepted"); } catch (InvalidDataException) { }
            schema.features.RemoveAt(schema.features.Count - 1);

            var window = ScriptableObject.CreateInstance<ModuleExporter>();
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            typeof(ModuleExporter).GetField("compactFeatures", flags).SetValue(window, schema);
            typeof(ModuleExporter).GetField("moduleId", flags).SetValue(window, "COMBAT");
            string file = Path.Combine(Application.dataPath, "smoke-module.bgm");
            typeof(ModuleExporter).GetField("loadedModuleFilePath", flags).SetValue(window, file);
            typeof(ModuleExporter).GetMethod("SaveModule", flags).Invoke(window, null);
            var exported = CompactFeatureSchema.Import(File.ReadAllText(file));
            Check(exported.features.Count == 2 && exported.components.Count == 1, "SaveModule lost compact schema");
            typeof(ModuleExporter).GetField("compactFeatures", flags).SetValue(window, new CompactFeatureSchema());
            typeof(ModuleExporter).GetMethod("LoadModuleFromFile", flags).Invoke(window, new object[] { file });
            var loaded = (CompactFeatureSchema)typeof(ModuleExporter).GetField("compactFeatures", flags).GetValue(window);
            Check(loaded.features.Count == 2 && loaded.components[0].methods.Count == api.methods.Count, "Module load lost API");
            UnityEngine.Object.DestroyImmediate(window);
            Debug.Log("COMPACT_FEATURE_SMOKE_PASSED");
            EditorApplication.Exit(0);
        }
        catch (Exception error) { Debug.LogException(error); EditorApplication.Exit(1); }
    }
}
