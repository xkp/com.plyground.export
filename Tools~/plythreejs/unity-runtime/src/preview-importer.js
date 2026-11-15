import { Behaviour, CSharpDictionary, Color, Component, GameObject, InputAction, LayerMask, Quaternion, RaycastHit, RectTransform, Renderer, Rigidbody, Transform, UnityEvent, Vector2, Vector3 } from './index.js';

function values(vector, keys) { return keys.map(key => vector[key]); }

function serializedPrivateFields(type) {
  const names = new Set();
  for (let current = type; current && current !== Function.prototype; current = Object.getPrototypeOf(current))
    for (const name of current.__unitySerializedFields ?? []) names.add(name);
  return names;
}

function serializeValue(value, typeNames = new Map()) {
  if (value == null || ['string', 'number', 'boolean'].includes(typeof value)) return value;
  if (Array.isArray(value)) return value.map(item => serializeValue(item, typeNames));
  if (value instanceof GameObject) return { $ref: value.assetId };
  if (value instanceof Component) return { $ref: value.assetId };
  if (typeof value === 'object' && value.assetId) return { $asset: value.assetId };
  if (value instanceof Vector2) return { $vector2: values(value, ['x', 'y']) };
  if (value instanceof Vector3) return { $vector3: values(value, ['x', 'y', 'z']) };
  if (value instanceof Quaternion) return { $quaternion: values(value, ['x', 'y', 'z', 'w']) };
  if (value instanceof Color) return { $color: values(value, ['r', 'g', 'b', 'a']) };
  if (value instanceof LayerMask) return { $layerMask: value.value };
  if (value instanceof UnityEvent) return { $unityEvent: [] };
  if (value instanceof InputAction) return { $inputAction: value.toSerialized() };
  if (typeof value === 'object' && value.constructor === Object)
    return Object.fromEntries(Object.entries(value).map(([key, member]) => [key, serializeValue(member, typeNames)]));
  const type = typeNames.get(value.constructor);
  if (type) return { $serializedType: type, properties: Object.fromEntries(
    Object.entries(value).filter(([key, member]) => (!key.startsWith('_')
      || serializedPrivateFields(value.constructor).has(key))
      && !(member instanceof Map && member.size === 0))
      .map(([key, member]) => [key, serializeValue(member, typeNames)])) };
  throw new Error(`Cannot serialize Preview component value: ${value?.constructor?.name}`);
}

function assignIds(object, id) {
  object.assetId = id;
  object.transform.assetId = `${id}:transform`;
  object._components.filter(component => !(component instanceof Transform))
    .forEach((component, index) => { component.assetId = `${id}:component:${index}`; });
  object.children.forEach((child, index) => assignIds(child, `${id}:child:${index}`));
}

/** Convert an editor-mode GameObject into the scene format consumed by the player. */
export function serializePreviewObject(object, id, typeNames = new Map()) {
  if (!(object instanceof GameObject)) throw new TypeError('CreateItem must return a GameObject');
  if (!object.assetId) assignIds(object, id);
  const key = object.assetId;
  const selectedMaterial = object._components.find(component => component instanceof Renderer)?.sharedMaterial;
  const textureId = selectedMaterial?.map?.assetId;
  const components = object._components.filter(component => !(component instanceof Transform))
    .map((component, index) => {
      const properties = {};
      const privateFields = serializedPrivateFields(component.constructor);
      for (const [name, value] of Object.entries(component)) {
        if ((name.startsWith('_') && !privateFields.has(name)) || ['gameObject', 'assetId', 'threeMesh', 'threeCamera', 'threeLight',
          'sharedMesh', 'sharedMaterial', 'sharedMaterials', 'object3D'].includes(name)) continue;
        if (value instanceof Map && value.size === 0) continue;
        if (value instanceof RaycastHit) continue;
        try { properties[name] = serializeValue(value, typeNames); }
        catch (error) { throw new Error(`Cannot serialize ${component.constructor.name}.${name}: ${error.message}`, { cause: error }); }
      }
      if (component instanceof Rigidbody) {
        properties.constraints = component.constraints;
        properties.angularDamping = component.angularDamping;
        properties.collisionDetectionMode = component.collisionDetectionMode;
      }
      return { id: component.assetId ?? `${key}:component:${index}`, type: typeNames.get(component.constructor)
        ?? component.constructor.name, ...(component instanceof Behaviour ? { enabled: component.enabled } : {}),
        properties };
    });
  return {
    id: key, name: object.name, active: object.activeSelf, tag: object.tag, layer: object.layer,
    transform: { id: object.transform.assetId ?? `${key}:transform`,
      position: values(object.transform.localPosition, ['x', 'y', 'z']),
      rotation: values(object.transform.localRotation, ['x', 'y', 'z', 'w']),
      scale: values(object.transform.localScale, ['x', 'y', 'z']) },
    ...(object.previewModel ? { model: object.previewModel } : {}),
    ...(textureId ? { materialOverride: { texture: textureId,
      color: [selectedMaterial.color.r, selectedMaterial.color.g, selectedMaterial.color.b],
      roughness: selectedMaterial.roughness,
      metalness: selectedMaterial.metalness } } : {}),
    ...(object.transform instanceof RectTransform ? { rectTransform: {
      anchorMin: values(object.transform.anchorMin, ['x', 'y']),
      anchorMax: values(object.transform.anchorMax, ['x', 'y']),
      pivot: values(object.transform.pivot, ['x', 'y']),
      sizeDelta: values(object.transform.sizeDelta, ['x', 'y']),
      anchoredPosition: values(object.transform.anchoredPosition, ['x', 'y'])
    } } : {}),
    components, children: object.children.map((child, index) =>
      serializePreviewObject(child, `${key}:child:${index}`, typeNames))
  };
}

/** Run transpiled module controllers in the same phase order as PlygroundLoader. */
export async function runPreviewImport({ modules, game, items, build, preprocess = [], createItem, connectObject }) {
  if (!Array.isArray(modules) || !Array.isArray(items)) throw new TypeError('Preview modules and items are required');
  const controllers = modules.map(module => module.controller);
  const call = async (module, phase, ...args) => {
    const method = module.controller?.[phase];
    if (typeof method !== 'function') return undefined;
    try { return await method.apply(module.controller, args); }
    catch (error) { throw new Error(`Preview controller ${module.controllerType} failed during ${phase}: ${error.message}`, { cause: error }); }
  };
  for (const module of modules) await call(module, 'Init', controllers, game);
  for (const module of modules) await call(module, 'ConfigProject');
  for (const module of modules) if (preprocess.length) await call(module, 'Preprocess', preprocess);
  const created = new Map();
  for (const item of items) {
    const module = modules.find(value => value.moduleId?.toLowerCase() === `${item.moduleId ?? item.ModuleId ?? ''}`.toLowerCase())
      ?? (modules.length === 1 ? modules[0] : null);
    const object = await createItem(module, item, build, call);
    if (object != null) created.set(item, object);
  }
  for (const module of modules) await call(module, 'Build');
  const connectionContext = new CSharpDictionary();
  for (const item of items) {
    const object = created.get(item);
    if (object == null) continue;
    for (const module of modules) await connectObject(module, object, item, connectionContext, call);
  }
  for (const module of modules) await call(module, 'AfterConnection', connectionContext);
  for (const module of modules) await call(module, 'Cleanup');
  return { created, connectionContext };
}
