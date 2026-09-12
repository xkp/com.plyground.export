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

1. Under **Component APIs**, use **Add From Source** for a project C# file or **Add From Project** for multiple files. The component list and Component/Properties/Methods/Events inspectors retain source paths, parsed documentation, defaults, enums, nested property data, and editing permissions. Adding existing files preserves authored changes. **Restore Missing** adds extracted members without replacing edits.
2. Set **Can Add** in the Component inspector (`No`, `Yes`, or a target item category). This controls availability in the game-item component picker independently of feature mappings. Use **Compact API** to review full public signatures, config, bus IDs, and notes. **Refresh API from source** replaces reflected methods/config/properties and restores missing editor metadata; it preserves bus IDs, notes, permissions, and labels. Ordinary C# events in the Events inspector are not GameplayBus IDs.
3. Under **Feature mappings**, enter the canonical human-readable feature name and select its component API. Several features may share one component definition. Feature matching descriptions and aliases belong in the shared catalog, not the module mapping.

Saving and exporting validates that every mapped component is documented, component type names are fully qualified, and feature names are unique after case/whitespace normalization. Empty API sections are omitted. Modules with no feature mappings use `features: {}` and may still export any number of components. A component with no public API needs a note describing its setup or automatic behavior.

The old `capabilities`, feature arrays, port bindings, adapters, and `featureApis` shapes are not imported or emitted by this workflow. Existing modules must be reauthored. No legacy feature sidecar is exported. Runtime assets, packages, items, and other module metadata retain their existing export behavior.

The editor's lists are Unity-serializable editing state only. Export uses explicit JSON object maps, since Unity JsonUtility does not serialize dictionaries.

Run `Tests~/test-compact-features.ps1` with a Unity editor executable and the existing Unity.Plastic.Newtonsoft.Json.dll used by the exporter's role editor. The test copies the exporter to a temporary Unity project, compiles it, checks reflection and validation, and exercises module save/load. It does not export a live module or alter a game project.


Each `components[fullyQualifiedType]` can include an `editor` object with `id` and `typeName` matching that key, `displayName`, `description`, `sourcePath`, `baseType`, `canAdd`, `attachTarget`, `requiredComponents`, and structured `properties`, `methods`, and `events`. Property metadata includes `type`, `defaultValue`, `enumValues`, `writable`, `userEditable`, and `children`. This is the restored component authoring/item-inspector data, not a feature mapping. The backend validates the compact API and omits `editor` from `get_feature_api` responses to keep codegen tokens small. The game-item picker and inspector read `components.*.editor`; no legacy capability fallback is used there.
