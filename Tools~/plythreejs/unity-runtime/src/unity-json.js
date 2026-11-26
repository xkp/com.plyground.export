// Unity JsonUtility serializes fields, not arbitrary JavaScript properties.
function fieldNames(value) {
  const type = value?.constructor;
  const inherited = [];
  let hasMetadata = false;
  for (let current = type; typeof current === 'function'; current = Object.getPrototypeOf(current))
    if (Object.hasOwn(current, '__unityJsonFields')) {
      hasMetadata = true;
      inherited.unshift(...current.__unityJsonFields);
    }
  if (hasMetadata) return [...new Set(inherited)];
  const privateFields = new Set(type?.__unitySerializedFields ?? []);
  const excluded = new Set(type?.__unityNonSerializedFields ?? []);
  return Object.keys(value ?? {}).filter(key => !excluded.has(key)
    && (!key.startsWith('_') || privateFields.has(key)));
}

function serialize(value, seen = new Set()) {
  if (value == null || typeof value !== 'object') return value;
  if (seen.has(value)) throw new Error('JsonUtility cannot serialize a cyclic object graph');
  seen.add(value);
  const output = Array.isArray(value) ? value.map(item => serialize(item, seen)) : {};
  if (!Array.isArray(value)) for (const key of fieldNames(value)) {
    const item = value[key];
    if (typeof item !== 'function' && item !== undefined) output[key] = serialize(item, seen);
  }
  seen.delete(value);
  return output;
}

function overwrite(target, data) {
  if (!data || typeof data !== 'object' || Array.isArray(data)) return target;
  const allowed = new Set(fieldNames(target));
  for (const [key, value] of Object.entries(data)) {
    if (!allowed.has(key)) continue;
    const old = target[key];
    if (Array.isArray(value)) target[key] = value.map((item, index) => {
      const prior = old?.[index];
      return prior && item && typeof item === 'object' && !Array.isArray(item)
        ? overwrite(new prior.constructor(), item) : item;
    });
    else if (old && typeof old === 'object' && value && typeof value === 'object')
      target[key] = overwrite(old, value);
    else target[key] = value;
  }
  return target;
}

export class JsonUtility {
  static ToJson(value, prettyPrint = false) {
    if (value == null) return '';
    if (Array.isArray(value) || typeof value !== 'object')
      throw new Error('JsonUtility.ToJson requires a serializable object');
    return JSON.stringify(serialize(value), null, prettyPrint ? 4 : 0);
  }
  static FromJson(text, type) {
    if (!text) return null;
    if (typeof type !== 'function') throw new Error('JsonUtility.FromJson requires a type');
    return overwrite(new type(), JSON.parse(text));
  }
  static FromJsonOverwrite(text, target) {
    if (target == null) throw new Error('JsonUtility.FromJsonOverwrite requires an object');
    overwrite(target, JSON.parse(text));
  }
}
