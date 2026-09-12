# Module features and component APIs

The exporter writes two top-level objects in `module.bgm`:

```json
{
  "features": {
    "Prefab Spawner": "Combat.PrefabSpawner"
  },
  "components": {
    "Combat.PrefabSpawner": {
      "methods": ["GameObject Spawn()"],
      "config": ["GameObject[] prefabs"],
      "properties": ["bool IsReady { get; }"],
      "publishes": ["combat.prefab.spawned"],
      "notes": ["Configure prefab assets before spawning."]
    }
  }
}
```

Use the **Features & APIs** tab:

1. Under **Component APIs**, add a fully qualified component type. Optionally select its compiled MonoBehaviour script to read public methods, serialized fields, and public properties. Review the extracted signatures. Reading a script replaces those three lists; it preserves authored event IDs and notes.
2. Enter one signature, event ID, or short note per line. `config` means serialized configuration, not publicly writable fields. Property signatures retain public accessors. `publishes` and `consumes` are GameplayBus IDs; ordinary C# events are not automatically converted into bus contracts.
3. Under **Feature mappings**, enter the canonical human-readable feature name and select its component API. Several features may share one component definition. Feature matching descriptions and aliases belong in the shared catalog, not the module mapping.

Saving and exporting validates that every mapped component is documented, component type names are fully qualified, and feature names are unique after case/whitespace normalization. Empty API sections are omitted. Modules with no features use empty `features` and `components` objects. A component with no public API needs a note describing its setup or automatic behavior.

The old `capabilities`, feature arrays, port bindings, adapters, and `featureApis` shapes are not imported or emitted by this workflow. Existing modules must be reauthored. No legacy feature sidecar is exported. Runtime assets, packages, items, and other module metadata retain their existing export behavior.

The editor's lists are Unity-serializable editing state only. Export uses explicit JSON object maps, since Unity JsonUtility does not serialize dictionaries.

Run `Tests~/test-compact-features.ps1` with a Unity editor executable and the existing Unity.Plastic.Newtonsoft.Json.dll used by the exporter's role editor. The test copies the exporter to a temporary Unity project, compiles it, checks reflection and validation, and exercises module save/load. It does not export a live module or alter a game project.
