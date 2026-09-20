# Module capabilities

The exporter no longer imports or emits the retired top-level `features` or `capabilities` formats. Existing modules that contain those fields can still be opened; they are ignored on load and omitted on the next save. The top-level `components` catalog remains available under **Capabilities > Components**.

## Character editor

Use **Capabilities > Character Editor** to export a module-declared catalog under `metadata.characterEditor`. A consuming editor uses it to show only visual controls that the module explicitly supports. The module may also publish its selectable base character prefabs and their body types:

```json
{
  "metadata": {
    "characterEditor": {
      "schemaVersion": "plyground.character-catalog/v2",
      "capabilities": ["transform", "collision", "appearance", "equipment"],
      "characters": [{
        "id": "hero-body",
        "displayName": "Hero Body",
        "itemId": "hero-body",
        "sourceAssetPath": "Assets/Characters/Hero.prefab",
        "bodyType": "humanoid"
      }],
      "equipment": [{
        "id": "steel-sword",
        "displayName": "Steel Sword",
        "slot": "right-hand",
        "itemId": "weapon-sword",
        "sourceAssetPath": "Assets/Weapons/SteelSword.prefab",
        "attachmentBone": "RightHand",
        "exclusiveGroup": "right-hand",
        "incompatibleItemIds": [],
        "conflictTags": ["two-handed"],
        "compatibleBodyTypes": ["humanoid"]
      }],
      "clothing": []
    }
  }
}
```

Use **Characters** to pick each base body directly from a skinned-mesh prefab or imported model in the Unity project, then assign its stable `bodyType` (for example `humanoid`, `redmono-male`, or `quadruped`). Bodies are added directly to the module AssetBundle; they do not need to be draggable module items. Add existing module items to equipment or appearance, then refine their catalog ID, target slot, asset path, attachment bone, compatible body types, exclusivity, and conflicts. Equipment and clothing capabilities are emitted automatically when their respective lists are nonempty; `appearance` is emitted when a module supplies character bodies or appearance. Empty lists are meaningful: the consuming editor must hide that visual control instead of showing an empty picker. Catalog IDs must be unique within each list and every entry needs a prefab asset path. Item OBJ/GLB extraction is retired; the AssetBundle is the sole runtime mesh payload. A custom item icon is also packed as a texture asset and can be selected as the palette visual through `visualAssetPath`; its prefab remains bundled so existing RTE placement continues to work. **Icon 3D** is an optional GameObject in that same bundle: RTE tries it first, then uses the item's prefab exactly as before if it is missing. Exported items retain `icon` for older tooling and add `iconAssetPath` plus `visualAssetPath`, the exact AssetBundle key a consumer should load.

Run `Tests~/test-compact-features.ps1` with a Unity editor executable and the existing Unity.Plastic.Newtonsoft.Json.dll used by the exporter's role editor. The test copies the exporter to a temporary Unity project, compiles it, and exercises catalog save/load. It does not export a live module or alter a game project.
