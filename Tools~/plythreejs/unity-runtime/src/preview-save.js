import { CSharpDictionary, List } from './collections.js';

const memoryFiles = new Map();
const fileKey = path => `plythreejs:save:${String(path).replaceAll('\\', '/')}`;
const storage = () => {
  try { return globalThis.localStorage ?? null; } catch { return null; }
};
const bytesToString = bytes => String.fromCharCode(...bytes);
const stringToBytes = text => Uint8Array.from(text, char => char.charCodeAt(0));
const read = path => storage()?.getItem(fileKey(path)) ?? memoryFiles.get(fileKey(path)) ?? null;
const write = (path, bytes) => {
  const value = bytesToString(bytes);
  const target = storage();
  if (target) target.setItem(fileKey(path), value);
  else memoryFiles.set(fileKey(path), value);
};

export class PreviewFileStream {
  constructor(path) { this.path = path; this.bytes = []; }
  Write(bytes, offset, count) { this.bytes.push(...Array.from(bytes).slice(offset, offset + count)); }
  Close() { this.Dispose(); }
  Dispose() { write(this.path, this.bytes); }
}

export class PreviewFile {
  static Create(path) { return new PreviewFileStream(path); }
  static Exists(path) { return read(path) !== null; }
  static ReadAllBytes(path) {
    const value = read(path);
    if (value === null) throw new Error(`Preview save file not found: ${path}`);
    return stringToBytes(value);
  }
  static WriteAllBytes(path, bytes) { write(path, bytes); }
  static Delete(path) { storage()?.removeItem(fileKey(path)); memoryFiles.delete(fileKey(path)); }
  static Copy(source, destination, overwrite = false) {
    if (!overwrite && this.Exists(destination)) throw new Error(`Preview save file already exists: ${destination}`);
    this.WriteAllBytes(destination, this.ReadAllBytes(source));
  }
}

export class PreviewEncoding {
  static UTF8 = { GetBytes: value => new TextEncoder().encode(String(value)) };
}

export class PreviewArray {
  static Copy(source, destination, count) {
    for (let index = 0; index < count; index++) destination[index] = source[index];
  }
  static Reverse(values) { values.reverse(); }
  static IndexOf(values, value) { return values.indexOf(value); }
}

export class PreviewConvert {
  static ChangeType(value, type) {
    if (type === Number) return Number(value);
    if (type === String) return String(value);
    if (type === Boolean) return Boolean(value);
    return value;
  }
}

export class PreviewMath {
  static Min(a, b) { return Math.min(a, b); }
  static Max(a, b) { return Math.max(a, b); }
  static Abs(value) { return Math.abs(value); }
}

export class PreviewAesManaged {
  Key = null;
  IV = null;
  CreateEncryptor() { return { mode: 'write' }; }
  CreateDecryptor() { return { mode: 'read' }; }
  Dispose() {}
}

// Preview saves stay in local browser storage. The Unity AES stream's API is
// preserved so generated JU code can run; these local saves are not encrypted.
export class PreviewCryptoStream {
  constructor(stream, transform, mode) { this.stream = stream; this.mode = mode; }
  Write(bytes, offset, count) { this.stream.Write(bytes, offset, count); }
  ReadAllBytes() { return this.stream.ReadAllBytes(); }
  Dispose() { this.stream.Dispose?.(); }
}

export class PreviewMemoryStream {
  constructor(bytes) { this.bytes = bytes; }
  ReadAllBytes() { return this.bytes; }
  Dispose() {}
}

export class PreviewStreamReader {
  constructor(stream) { this.stream = stream; }
  ReadToEnd() { return new TextDecoder().decode(this.stream.ReadAllBytes()); }
  Dispose() {}
}

export class PreviewJsonSerializerSettings {
  constructor() { this.Converters = new List(); }
}
export class PreviewCustomCreationConverter {}

function plain(value) {
  if (value instanceof CSharpDictionary || value instanceof Map)
    return { $dictionary: [...value].map(([key, item]) => [key, plain(item)]) };
  if (Array.isArray(value)) return value.map(plain);
  if (value && typeof value === 'object')
    return Object.fromEntries(Object.entries(value).map(([key, item]) => [key, plain(item)]));
  return value;
}

function revive(value, exemplar) {
  if (value?.$dictionary) return new CSharpDictionary(value.$dictionary.map(([key, item]) => [key, revive(item)]));
  if (Array.isArray(value)) return value.map(item => revive(item));
  if (value && typeof value === 'object') {
    const target = exemplar && typeof exemplar === 'object' ? exemplar : {};
    for (const [key, item] of Object.entries(value)) target[key] = revive(item, target[key]);
    return target;
  }
  return value;
}

export class PreviewJsonConvert {
  static SerializeObject(value) { return JSON.stringify(plain(value)); }
  static DeserializeObject(text, settings, type) {
    return revive(JSON.parse(text), typeof type === 'function' ? new type() : undefined);
  }
}
