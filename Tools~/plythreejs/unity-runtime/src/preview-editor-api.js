import { GameObject, Object as UnityObject, Transform } from './index.js';

/** AssetDatabase calls available while a transpiled controller builds a Preview scene. */
export class AssetDatabase {
  static #assets = new Map();

  static Configure(entries) {
    this.#assets = new Map([...entries].map(([path, asset]) =>
      [String(path).replaceAll('\\', '/').toLowerCase(), asset]));
  }

  static LoadAssetAtPath(path, type = null) {
    const asset = this.#assets.get(String(path).replaceAll('\\', '/').toLowerCase()) ?? null;
    if (!asset || !type) return asset;
    if (typeof type === 'string') {
      const name = type.split('.').at(-1);
      // AnimatorController assets are represented by their exported graph in
      // Preview. They still satisfy Unity's RuntimeAnimatorController lookup.
      if ((name === 'RuntimeAnimatorController' || name === 'AnimatorController')
        && Array.isArray(asset.layers) && Array.isArray(asset.parameters)) return asset;
      if (asset.constructor.name === name) return asset;
      if (asset instanceof GameObject)
        return asset._components.find(component => component.constructor.name === name) ?? null;
      return null;
    }
    if (asset instanceof type) return asset;
    return asset instanceof GameObject ? asset.GetComponent(type) : null;
  }

  static FindAssets(query) {
    const text = String(query ?? '').trim();
    const prefabOnly = /^t:prefab\b/i.test(text);
    const term = text.replace(/^t:\w+\s*/i, '').toLowerCase();
    return [...this.#assets].filter(([path, asset]) =>
      (!prefabOnly || asset instanceof GameObject)
      && (!term || asset?.name?.toLowerCase().includes(term)
        || path.split('/').at(-1)?.toLowerCase().includes(term)))
      .map(([path]) => path);
  }
  static GUIDToAssetPath(guid) { return this.#assets.has(String(guid).toLowerCase()) ? guid : ''; }  static SaveAssets() {
    // Preview assets are mutable objects in the scene bundle. The scene writer
    // serializes their current state after the controller finishes.
  }
  static LoadAllAssetsAtPath(path) {
    const asset = this.LoadAssetAtPath(path);
    return asset == null ? [] : [asset];
  }
  static Refresh() { /* In-memory Preview assets are available immediately. */ }
  static Register(path, asset) {
    this.#assets.set(String(path).replaceAll('\\', '/').toLowerCase(), asset);
  }
}

export class PrefabUtility {
  static UnpackPrefabInstance() { /* Preview prefab instances are already mutable. */ }
  static SaveAsPrefabAsset(object, path) {
    if (!(object instanceof GameObject)) throw new TypeError('PrefabUtility.SaveAsPrefabAsset expects a GameObject');
    AssetDatabase.Register(path, object);
    return object;
  }
  static InstantiatePrefab(prefab, sceneOrParent = null) {
    if (!(prefab instanceof GameObject)) throw new TypeError('PrefabUtility.InstantiatePrefab expects an exported GameObject');
    const parent = sceneOrParent instanceof Transform ? sceneOrParent : null;
    if (sceneOrParent !== null && !parent && !sceneOrParent.IsValid?.())
      throw new TypeError('PrefabUtility.InstantiatePrefab expects a valid scene or parent Transform');
    const copy = UnityObject.Instantiate(prefab, parent);
    const models = (source, target) => {
      if (source.previewModel) target.previewModel = source.previewModel;
      source.children.forEach((child, index) => models(child, target.children[index]));
    };
    models(prefab, copy);
    return copy;
  }
}
