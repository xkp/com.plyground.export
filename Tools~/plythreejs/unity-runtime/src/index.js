import * as THREE from 'three';
export const AnimationClip = THREE.AnimationClip;
import { clone as cloneSkeleton } from 'three/addons/utils/SkeletonUtils.js';
import { AssetDatabase, PrefabUtility } from './preview-editor-api.js';
import { AnimatorControllerRuntime } from './animator-graph.js';
export { AnimatorGraphPlayer, AnimatorControllerRuntime, animatorConditionMatches,
  evaluateAnimatorMotion } from './animator-graph.js';
export { loadFbxAnimation, parseFbxAnimation, mapFbxAnimationToSkeleton,
  retargetFbxAnimation } from './source-animation.js';
export { BrowserCanvasUI } from './ui.js';
import { Vector2, Vector3, Vector4, Quaternion, Matrix4x4, Plane, Mathf, AnimationCurve, Keyframe } from './math.js';
import { Input, BrowserInput, KeyCode, Keyboard, Mouse } from './input.js';
import { InputAction, InputActionType, InputActionPhase } from './input-actions.js';
import { UnityEvent } from './events.js';
import { PreviewFile, PreviewFileStream, PreviewEncoding, PreviewArray, PreviewConvert, PreviewMath,
  PreviewAesManaged, PreviewCryptoStream, PreviewMemoryStream, PreviewStreamReader,
  PreviewJsonSerializerSettings, PreviewCustomCreationConverter, PreviewJsonConvert } from './preview-save.js';
import { List, ReadOnlyArray, CSharpDictionary } from './collections.js';
import { NavigationSystem, bakeNavMesh } from './navigation.js';
export { NavigationSystem, bakeNavMesh };
export { Input, BrowserInput, KeyCode, Keyboard, Mouse };
export { InputAction, InputActionType, InputActionPhase };
export { UnityEvent };
export { PreviewFile, PreviewFileStream, PreviewEncoding, PreviewArray, PreviewConvert, PreviewMath,
  PreviewAesManaged, PreviewCryptoStream, PreviewMemoryStream, PreviewStreamReader,
  PreviewJsonSerializerSettings, PreviewCustomCreationConverter, PreviewJsonConvert };
export { List, ReadOnlyArray, CSharpDictionary };

export class PreviewEnumerable {
  static FirstOrDefault(values, predicate = () => true) { return [...(values ?? [])].find(predicate) ?? null; }
  static Any(values, predicate = () => true) { return [...(values ?? [])].some(predicate); }
  static Where(values, predicate) { return [...(values ?? [])].filter(predicate); }
  static Select(values, selector) { return [...(values ?? [])].map(selector); }
  static SelectMany(values, selector) { return [...(values ?? [])].flatMap(value => [...(selector(value) ?? [])]); }
  static Distinct(values) { return [...new Set(values ?? [])]; }
  static OfType(values, type) { return [...(values ?? [])].filter(value => value instanceof type); }
  static OrderBy(values, selector, comparer = null) {
    return [...(values ?? [])].sort((a, b) => comparer?.Compare?.(selector(a), selector(b))
      ?? String(selector(a)).localeCompare(String(selector(b))));
  }
}

export function CSharpString(value) {
  if (value == null) return '';
  if (typeof value === 'boolean') return value ? 'True' : 'False';
  return value.ToString?.() ?? globalThis.String(value);
}
CSharpString.Empty = '';
CSharpString.IsNullOrEmpty = value => value == null || value === '';
CSharpString.IsNullOrWhiteSpace = value => value == null || /^\s*$/.test(value);
CSharpString.Equals = (left, right, comparison = 4) =>
  comparison === 5 || comparison === 1
    ? String(left).toLowerCase() === String(right).toLowerCase() : left === right;
CSharpString.StartsWith = (left, right, comparison = 4) =>
  comparison === 5 || comparison === 1
    ? String(left).toLowerCase().startsWith(String(right).toLowerCase())
    : String(left).startsWith(String(right));
CSharpString.EndsWith = (left, right, comparison = 4) =>
  comparison === 5 || comparison === 1
    ? String(left).toLowerCase().endsWith(String(right).toLowerCase())
    : String(left).endsWith(String(right));

// Older uploaded Preview modules can contain direct C# string method calls.
// Keep them playable until their sidecars are re-exported with current transpiler lowerings.
for (const [method, implementation] of globalThis.Object.entries({
  StartsWith: CSharpString.StartsWith,
  EndsWith: CSharpString.EndsWith,
  Contains: (left, right) => globalThis.String(left).includes(globalThis.String(right)),
  IndexOf: (left, right, startIndex = 0) => globalThis.String(left).indexOf(right, startIndex),
  Substring: (left, startIndex, length) => length === undefined
    ? globalThis.String(left).slice(startIndex)
    : globalThis.String(left).slice(startIndex, startIndex + length),
  Replace: (left, oldValue, newValue) => globalThis.String(left).replaceAll(oldValue, newValue),
  Equals: CSharpString.Equals,
})) {
  if (!globalThis.Object.prototype.hasOwnProperty.call(globalThis.String.prototype, method)) {
    globalThis.Object.defineProperty(globalThis.String.prototype, method, {
      configurable: true,
      value(...args) { return implementation(this.valueOf(), ...args); },
    });
  }
}
if (!globalThis.Object.prototype.hasOwnProperty.call(globalThis.Number.prototype, 'ToString')) {
  globalThis.Object.defineProperty(globalThis.Number.prototype, 'ToString', {
    configurable: true,
    value(format) {
      const value = this.valueOf();
      if (typeof format === 'string' && /^0+$/.test(format))
        return globalThis.String(value).padStart(format.length, '0');
      return globalThis.String(value);
    },
  });
}

export class CSharpArray {
  static Reverse(array) {
    if (!Array.isArray(array)) throw new TypeError('Array.Reverse needs an array');
    array.reverse();
  }
  static Sort(array, comparison) {
    if (!Array.isArray(array)) throw new TypeError('Array.Sort needs an array');
    array.sort(comparison ?? undefined);
  }
  static Resize(reference, length, defaultValue = null) {
    if (!reference || !('value' in reference) || !Number.isSafeInteger(length) || length < 0)
      throw new TypeError('Array.Resize needs a ref array and nonnegative length');
    const source = reference.value;
    if (source != null && !Array.isArray(source)) throw new TypeError('Array.Resize needs an array');
    reference.value = Array.from({ length }, (_, index) => index < (source?.length ?? 0)
      ? source[index] : defaultValue);
  }
}

export class CSharpEnum {
  static TryParse(enumType, text, ignoreCase, output) {
    if (typeof ignoreCase !== 'boolean') { output = ignoreCase; ignoreCase = false; }
    const entry = globalThis.Object.entries(enumType).find(([name]) => ignoreCase
      ? name.toLowerCase() === String(text).toLowerCase() : name === String(text));
    if (!entry) return false;
    output.value = entry[1];
    return true;
  }
}

export class CSharpQueue {
  constructor(values = []) { this._values = Array.from(values); }
  Enqueue(value) { this._values.push(value); }
  Dequeue() {
    if (!this._values.length) throw new Error('Queue is empty');
    return this._values.shift();
  }
  Peek() {
    if (!this._values.length) throw new Error('Queue is empty');
    return this._values[0];
  }
  Clear() { this._values.length = 0; }
  Contains(value) { return this._values.includes(value); }
  get Count() { return this._values.length; }
  [Symbol.iterator]() { return this._values[Symbol.iterator](); }
}

export class CSharpGuid {
  constructor(value = '00000000-0000-0000-0000-000000000000') { this.value = String(value).toLowerCase(); }
  static get Empty() { return new CSharpGuid(); }
  static NewGuid() { return new CSharpGuid(globalThis.crypto.randomUUID()); }
  Equals(other) { return other instanceof CSharpGuid && this.value === other.value; }
  ToString() { return this.value; }
  toString() { return this.value; }
}

export class PreviewThread {
  constructor(action) { this._action = action; this._alive = false; this._aborted = false; }
  get IsAlive() { return this._alive; }
  Start() {
    if (this._alive) throw new Error('Thread has already started');
    this._alive = true;
    queueMicrotask(() => {
      if (this._aborted) return;
      try { this._action(); } finally { this._alive = false; }
    });
  }
  Abort() { this._aborted = true; this._alive = false; }
}

export class Profiler {
  static #stack = [];
  static samples = [];
  static BeginSample(name) { this.#stack.push({ name: String(name), start: performance.now() }); }
  static EndSample() {
    const sample = this.#stack.pop();
    if (sample) {
      if (this.samples.length >= 256) this.samples.shift();
      this.samples.push({ name: sample.name, durationMs: performance.now() - sample.start });
    }
  }
}

export class Application {
  static quitRequested = false;
  static Quit() {
    this.quitRequested = true;
    activeRuntime?.stop();
  }
  static get persistentDataPath() { return 'preview'; }
  static get platform() {
    if (typeof navigator === 'undefined') return 0;
    return /iPad|iPhone|iPod/.test(navigator.userAgent) ? 8 : 17;
  }
  static get isMobilePlatform() {
    return typeof navigator !== 'undefined' && /Android|iPad|iPhone|iPod/.test(navigator.userAgent);
  }
}

export class SystemInfo {
  static get deviceModel() { return typeof navigator === 'undefined' ? '' : navigator.userAgent; }
}

export class Screen {
  static get width() { return globalThis.innerWidth ?? 1280; }
  static get height() { return globalThis.innerHeight ?? 720; }
  static get currentResolution() {
    return { width: globalThis.innerWidth ?? 1280, height: globalThis.innerHeight ?? 720, refreshRate: 60 };
  }
  static get resolutions() { return [this.currentResolution]; }
  static get fullScreen() { return typeof document !== 'undefined' && Boolean(document.fullscreenElement); }
  static SetResolution() {}
}

export class Resolution {
  constructor() { this.width = 1280; this.height = 720; this.refreshRate = 60; }
}

export class PlayerPrefs {
  static #values = new Map();
  static GetFloat(key, fallback = 0) { return Number(this.#values.get(key) ?? fallback); }
  static SetFloat(key, value) { this.#values.set(key, Number(value)); }
  static GetInt(key, fallback = 0) { return Math.trunc(this.GetFloat(key, fallback)); }
  static SetInt(key, value) { this.#values.set(key, Math.trunc(value)); }
  static GetString(key, fallback = '') { return String(this.#values.get(key) ?? fallback); }
  static SetString(key, value) { this.#values.set(key, String(value)); }
  static HasKey(key) { return this.#values.has(key); }
  static DeleteKey(key) { this.#values.delete(key); }
  static DeleteAll() { this.#values.clear(); }
  static Save() {}
}

export class QualitySettings {
  static #level = 0;
  static GetQualityLevel() { return this.#level; }
  static SetQualityLevel(value) { this.#level = Math.trunc(value); }
}

export class CSharpMath {
  static Max(a, b) { return Math.max(a, b); }
  static Min(a, b) { return Math.min(a, b); }
  static Abs(value) { return Math.abs(value); }
  static Round(value) { return Math.round(value); }
  static Floor(value) { return Math.floor(value); }
  static Ceiling(value) { return Math.ceil(value); }
  static Sqrt(value) { return Math.sqrt(value); }
  static Pow(value, power) { return Math.pow(value, power); }
}

export class Rect {
  constructor(x = 0, y = 0, width = 0, height = 0) {
    this.x = x; this.y = y; this.width = width; this.height = height;
  }
}

export class Cursor {
  static _lockState = 0;
  static _visible = true;
  static _browserPointerLock = true;
  static _browserHide = true;
  static _pointerTarget = null;
  static _pointerDownHandler = null;
  static ConfigureBrowser({ pointerLock = true, hide = true, pointerTarget = null } = {}) {
    this._browserPointerLock = Boolean(pointerLock);
    this._browserHide = Boolean(hide);
    if (typeof document === 'undefined') return;
    if (this._pointerTarget && this._pointerDownHandler)
      this._pointerTarget.removeEventListener('pointerdown', this._pointerDownHandler);
    this._pointerTarget = pointerTarget ?? document.body;
    this._pointerDownHandler = () => {
      if (this._browserPointerLock && this._lockState === 1 && !document.pointerLockElement)
        this._requestPointerLock();
    };
    this._pointerTarget?.addEventListener('pointerdown', this._pointerDownHandler);
    if (!this._browserPointerLock && document.pointerLockElement) document.exitPointerLock?.();
    if (document.body) document.body.style.cursor = this._browserHide && !this._visible ? 'none' : '';
  }
  static _requestPointerLock() {
    try { this._pointerTarget?.requestPointerLock?.()?.catch?.(() => {}); }
    catch { /* Browsers may reject pointer lock when the gesture is unavailable. */ }
  }
  static get lockState() { return this._lockState; }
  static set lockState(value) {
    this._lockState = value;
    if (typeof document === 'undefined') return;
    if (value === 1 && this._browserPointerLock && globalThis.navigator?.userActivation?.isActive)
      this._requestPointerLock();
    else if (value !== 1 && document.pointerLockElement) document.exitPointerLock?.();
  }
  static get visible() { return this._visible; }
  static set visible(value) {
    this._visible = Boolean(value);
    if (typeof document !== 'undefined' && document.body)
      document.body.style.cursor = this._browserHide && !this._visible ? 'none' : '';
  }
}

export class LayerMask {
  constructor(value = 0) { this.value = value; }
  valueOf() { return this.value; }
  static _names = new Map([['Default', 0], ['TransparentFX', 1], ['Ignore Raycast', 2],
    ['Water', 4], ['UI', 5]]);
  static Configure(names) {
    const entries = names instanceof Map ? names : globalThis.Object.entries(names);
    const configured = new Map(entries);
    for (const [name, index] of configured) {
      if (typeof name !== 'string' || !Number.isInteger(index) || index < 0 || index > 31)
        throw new TypeError('Layer names must map to indices from 0 through 31');
    }
    this._names = configured;
  }
  static NameToLayer(name) { return this._names.get(name) ?? -1; }
  static LayerToName(index) { return [...this._names].find(([, value]) => value === index)?.[0] ?? ''; }
  static GetMask(...names) {
    return names.reduce((mask, name) => {
      const index = this.NameToLayer(name);
      if (index < 0) throw new Error(`Unknown Unity layer: ${name}`);
      return mask | (1 << index);
    }, 0);
  }
}

let activeRuntime = null;

/** Unity's static Time surface for scripts running in the active runtime. */
export class Time {
  static get time() { return activeRuntime?.time.time ?? 0; }
  static get deltaTime() { return activeRuntime?.time.deltaTime ?? 0; }
  static get unscaledTime() { return activeRuntime?.time.unscaledTime ?? 0; }
  static get unscaledDeltaTime() { return activeRuntime?.time.unscaledDeltaTime ?? 0; }
  static get fixedDeltaTime() { return activeRuntime?.fixedDeltaTime ?? 1 / 50; }
  static set fixedDeltaTime(value) {
    if (!activeRuntime) throw new Error('No active UnityRuntime');
    if (!Number.isFinite(value) || value <= 0) throw new RangeError('fixedDeltaTime must be positive');
    activeRuntime.fixedDeltaTime = value;
    activeRuntime.time.fixedDeltaTime = value;
    if (Physics._system?.runtime === activeRuntime)
      Physics._system.world.integrationParameters.dt = value;
  }
  static get timeScale() { return activeRuntime?.timeScale ?? 1; }
  static set timeScale(value) {
    if (!activeRuntime) throw new Error('No active UnityRuntime');
    if (!Number.isFinite(value) || value < 0) throw new RangeError('timeScale must be nonnegative');
    activeRuntime.timeScale = value;
  }
  static get frameCount() { return activeRuntime?.time.frameCount ?? 0; }
}

/** Unity Object's destruction entry point. */
export class Object {
  GetType() { return this.constructor; }
  static op_Equality(left, right) {
    const live = value => value != null && !value._destroyed;
    if (!live(left) || !live(right)) return !live(left) && !live(right);
    return left === right;
  }
  static op_Inequality(left, right) { return !Object.op_Equality(left, right); }
  static Destroy(target, delay = 0) {
    if (!target) return;
    const runtime = target instanceof GameObject ? target._runtime : target.gameObject?._runtime;
    if (!Number.isFinite(delay) || delay < 0) throw new RangeError('Destroy delay must be nonnegative');
    if (runtime && delay > 0) runtime._delayedDestroy.set(target, delay);
    else if (runtime) runtime._destroyQueue.add(target);
    else target.Destroy?.();
  }
  static DestroyImmediate(target) { target?.Destroy(); }
  static DontDestroyOnLoad(target) {
    const object = target instanceof GameObject ? target : target?.gameObject;
    if (!(object instanceof GameObject) || object._destroyed)
      throw new TypeError('DontDestroyOnLoad expects a live GameObject or Component');
    let root = object;
    while (root.parent) root = root.parent;
    root._persistentAcrossScenes = true;
  }
  static Instantiate(target, positionOrParent = null, rotation = null, parent = null) {
    if (positionOrParent instanceof Vector3) {
      return instantiate(target, parent, positionOrParent, rotation);
    }
    return instantiate(target, positionOrParent);
  }
  static FindObjectOfType(type, includeInactive = false) {
    for (const object of activeRuntime?._objects ?? []) {
      if (!includeInactive && !object.activeInHierarchy) continue;
      const found = object.GetComponent(type);
      if (found) return found;
    }
    return null;
  }
  static FindFirstObjectByType(type, includeInactive = false) {
    return this.FindObjectOfType(type, includeInactive === true || includeInactive === 1);
  }
  static FindAnyObjectByType(type, includeInactive = false) {
    return this.FindFirstObjectByType(type, includeInactive);
  }
  static FindObjectsOfType(type, includeInactive = false) {
    return this.FindObjectsByType(type, includeInactive);
  }
  static FindObjectsByType(type, inactiveOrSort = 0, sortMode = 0) {
    const includeInactive = typeof inactiveOrSort === 'boolean' ? inactiveOrSort
      : arguments.length >= 3 && inactiveOrSort === 1;
    const found = [];
    for (const object of activeRuntime?._objects ?? []) {
      if (!includeInactive && !object.activeInHierarchy) continue;
      if (object instanceof type) found.push(object);
      for (const component of object._components)
        if (component instanceof type) found.push(component);
    }
    // Unity does not promise a stable order for FindObjectsSortMode.None.
    return found;
  }
}

export class TextAsset extends Object {
  constructor(text, name = '') {
    super();
    this.text = text;
    this.name = name;
  }
  get bytes() { return new TextEncoder().encode(this.text); }
}

export class Avatar extends Object {
  constructor(name = '') { super(); this.name = name; }
}

/** Unity Resources paths resolve to assets selected by the generated bundle. */
export class LazyResource {
  constructor(resolve) {
    this.resolve = resolve;
    this.value = undefined;
  }
  get() { return this.value ??= this.resolve(); }
}

export class Resources {
  static Load(path, type = null) {
    const entry = activeRuntime?._resources.get(String(path)) ?? null;
    const asset = entry instanceof LazyResource ? entry.get() : entry;
    return type && asset && !(asset instanceof type) ? null : asset;
  }
  static LoadAll(path, type = null) {
    const prefix = String(path).replace(/\/$/, '');
    return [...(activeRuntime?._resources ?? new Map())]
      .filter(([key]) => key === prefix || key.startsWith(`${prefix}/`))
      .map(([, entry]) => entry instanceof LazyResource ? entry.get() : entry)
      .filter(asset => !type || asset instanceof type);
  }
  static UnloadAsset(asset) {
    for (const [key, value] of activeRuntime?._resources ?? [])
      if (value === asset || (value instanceof LazyResource && value.value === asset))
        activeRuntime._resources.delete(key);
  }
}

export class ScriptableObject extends Object {
  static CreateInstance(type) {
    if (typeof type !== 'function' || !(type.prototype instanceof ScriptableObject))
      throw new TypeError('ScriptableObject.CreateInstance needs a ScriptableObject type');
    return new type();
  }
}

export class PhysicsMaterial extends Object {
  constructor(name = '') {
    super();
    this.name = name;
    this.dynamicFriction = 0.6;
    this.staticFriction = 0.6;
    this.bounciness = 0;
    this.frictionCombine = 'Average';
    this.bounceCombine = 'Average';
  }
}

export function UnityInterface(name) {
  return {
    unityTypeName: name,
    [Symbol.hasInstance](value) {
      for (let type = value?.constructor; typeof type === 'function'; type = globalThis.Object.getPrototypeOf(type)) {
        if (type.__unityInterfaces?.includes(this)) return true;
      }
      return false;
    },
  };
}

export class UnityType {
  static #types = new Map();
  static Clear() {
    this.#types.clear();
    this.Register({ BasePlygroundModule, BaseGameModule, BaseCharacterModule, CharacterDescriptor,
      'System.Math': CSharpMath,
      'System.StringComparer': { Ordinal: { Compare: (a, b) => a < b ? -1 : a > b ? 1 : 0 } },
      'Plyground.Pipeline.Runtime.PlygroundItem': PlygroundItem,
      'System.Threading.Tasks.Task': PreviewTask,
      'UnityEditor.EditorBuildSettings': { scenes: [] },
      'UnityEditor.EditorBuildSettingsScene': class EditorBuildSettingsScene {
        constructor(path, enabled) { this.path = path; this.enabled = enabled; }
      },
      'UnityEditor.EditorStyles': { label: {}, helpBox: {} },
      'UnityEditor.EditorGUILayout': { LabelField() {} },
      'UnityEditor.EditorUtility': PreviewEditorUtility,
      'UnityEditor.SceneManagement.EditorSceneManager': PreviewEditorSceneManager,
      'UnityEditor.Undo': PreviewUndo,
      'JUTPSEditor.MessageWindow': PreviewMessageWindow });
  }
  static Register(types) {
    for (const [name, constructor] of globalThis.Object.entries(types)) {
      if (typeof constructor !== 'function' && (constructor === null || typeof constructor !== 'object'))
        throw new TypeError(`Preview type ${name} is neither a constructor nor an enum`);
      const existing = this.#types.get(name);
      if (existing && existing !== constructor) throw new Error(`Duplicate Preview type: ${name}`);
      this.#types.set(name, constructor);
    }
  }
  static RegisterSystemEditor(types) {
    for (const name of ['BasePlygroundModule', 'BaseGameModule', 'BaseCharacterModule', 'CharacterDescriptor'])
      this.#types.delete(name);
    this.Register(types);
  }
  static RegisterModuleEditor(types) {
    if (types['JUTPSEditor.MessageWindow']
      && this.#types.get('JUTPSEditor.MessageWindow') === PreviewMessageWindow)
      this.#types.delete('JUTPSEditor.MessageWindow');
    this.Register(types);
  }
  static Resolve(name) {
    const type = this.#types.get(name);
    if (!type) throw new Error(`Preview type is missing from exported module code: ${name}`);
    return type;
  }
  static GetType(qualifiedName) {
    return this.#types.get(String(qualifiedName).split(',')[0].trim()) ?? null;
  }
  static Of(value) {
    if (value == null) throw new TypeError('GetType requires a value');
    return typeof value === 'string' ? String
      : typeof value === 'number' ? Number
      : typeof value === 'boolean' ? Boolean : value.constructor;
  }
  static IsPrimitive(type) { return type === String || type === Number || type === Boolean; }
  static Name(type) { return type?.unityTypeName?.split('.').at(-1) ?? type?.name ?? ''; }
  static IsSubclassOf(type, base) {
    return typeof type === 'function' && typeof base === 'function'
      && type !== base && type.prototype instanceof base;
  }
}

/** Controller lifecycle shared with the Unity editor SDK. */
export class BasePlygroundModule {
  constructor() { this.Model = null; this._game = null; this._modules = []; this._gameModule = null; }
  Init(modules, game) {
    this._modules = [...modules];
    this._game = game;
    // Exported controllers may extend a transpiled SDK base rather than this
    // runtime's BaseGameModule. Unity resolves IPlygroundGameModule by interface.
    this._gameModule = this._modules.find(module => module != null
      && typeof module.GetBuildValue === 'function'
      && typeof module.GetAlias === 'function'
      && typeof module.AddScene === 'function') ?? null;
  }
  ConfigProject() {}
  CreateItem() { return null; }
  Build() {}
  ConnectObject() {}
  AfterConnection() {}
  Cleanup() {}
  GetTemplateItem(id) {
    for (const group of this.Model?.itemGroups ?? []) {
      const item = group.items?.find(candidate => candidate.id === id);
      if (item) return item;
    }
    return this.Model?.userTemplates?.find(candidate => candidate.id === id) ?? null;
  }
  GetInstance(name) { return GameObject.Find(name); }
  HideObjects(...objects) { for (const object of objects) object?.SetActive(false); }
  DestroyObjects(...objects) { for (const object of objects) Object.DestroyImmediate(object); }
  GetModule(id) { return this._modules.find(module => module.Model?.id === id) ?? null; }
  get IsMultiplayerEnabled() { return this._game?.IsMultiplayerEnabled === true; }
  get Multiplayer() { return this._game?.Multiplayer ?? null; }
}

export class BaseGameModule extends BasePlygroundModule {
  constructor() { super(); this._scenes = ['MainScene']; }
  GetBuildValue(key) {
    const item = (this._game?.GameItems ?? []).find(candidate => !candidate.ModuleId && candidate.Id === 'Build');
    return item?.Values?.[key] ?? null;
  }
  AddScene(name, starter = false) { if (starter) this._scenes.unshift(name); else this._scenes.push(name); }
  GetAlias() { return this.Model?.name ?? ''; }
}

/** Unity SDK character-module workflow used by transpiled editor controllers. */
export class CharacterDescriptor {
  constructor() { this.Name = null; this.Gender = null; this.Role = null; this.Data = null; }
}

export class BaseCharacterModule extends BasePlygroundModule {
  GetDescriptor(data) {
    const source = data instanceof JToken ? data.ToObject() : data;
    if (source == null) return null;
    const descriptor = globalThis.Object.assign(new CharacterDescriptor(), source);
    descriptor.Name = source.Name ?? source.name ?? null;
    descriptor.Data = JToken.Wrap(source.Data ?? source.data);
    const values = descriptor.Data?.ToObject() ?? {};
    descriptor.Gender = values.gender == null ? null : String(values.gender);
    descriptor.Role = values.role == null ? null : String(values.role);
    return descriptor;
  }
  async CreateItem(item, template, buildItem) {
    const position = item.Position ?? item.position ?? Vector3.zero;
    const rotation = item.Rotation ?? item.rotation ?? Quaternion.identity;
    const scale = item.Scale ?? item.scale ?? Vector3.one;
    if (buildItem != null) {
      const descriptor = this.GetDescriptor(buildItem);
      if (descriptor) {
        item.Values ??= {};
        if (typeof item.Values.Add === 'function') item.Values.Add('descriptor', descriptor);
        else item.Values.descriptor = descriptor;
      }
      const prefab = this.constructor.InstantiateTemplateByName(
        this.GetTemplateName(descriptor, item, template));
      if (prefab) {
        prefab.transform.position = position;
        prefab.transform.rotation = rotation;
        prefab.transform.localScale = scale;
        return await this.BuildCharacter(prefab, descriptor, item, template) ? prefab : null;
      }
    }
    const created = new GameObject(item.Id ?? item.id);
    created.transform.position = position;
    created.transform.rotation = rotation;
    created.transform.localScale = scale;
    return await this.BuildCharacter(created, null, item, template) ? created : null;
  }
  BuildCharacter() { throw new Error('Character module must implement BuildCharacter'); }
  GetTemplateName() { throw new Error('Character module must implement GetTemplateName'); }
  static FindPrefabPathByName(name) {
    const paths = AssetDatabase.FindAssets(`t:prefab ${name}`)
      .map(id => AssetDatabase.GUIDToAssetPath(id));
    return paths.find(path => AssetDatabase.LoadAssetAtPath(path)?.name === name)
      ?? paths[0] ?? null;
  }
  static FindPrefabByName(name) {
    const path = this.FindPrefabPathByName(name);
    return path ? AssetDatabase.LoadAssetAtPath(path, GameObject) : null;
  }
  static InstantiateTemplateByName(name, parent = null, position = null, rotation = null) {
    if (!name) throw new Error('Character prefab name is required');
    const prefab = this.FindPrefabByName(name);
    if (!prefab) return null;
    const instance = PrefabUtility.InstantiatePrefab(prefab);
    if (parent) instance.transform.SetParent(parent);
    instance.transform.position = position ?? Vector3.zero;
    instance.transform.rotation = rotation ?? Quaternion.identity;
    instance.name = prefab.name;
    return instance;
  }
}
/** JSON token surface used by controller build values. */
export class JToken {
  constructor(value) { this._value = value; }
  static Wrap(value) {
    if (value instanceof JToken || value == null) return value;
    if (Array.isArray(value)) return new JArray(value);
    if (typeof value === 'object') return new JObject(value);
    return new JToken(value);
  }
  static As(value, type) {
    const token = this.Wrap(value);
    return token instanceof type ? token : null;
  }
  Value(type, key) {
    const token = key === undefined ? this : this.GetItem(key);
    if (token == null) return null;
    if (type?.startsWith('Newtonsoft.Json.Linq.'))
      return JToken.As(token, type.endsWith('JArray') ? JArray : JObject);
    const value = token instanceof JToken ? token._value : token;
    if (value == null) return null;
    if (type?.startsWith('bool')) return Boolean(value);
    if (/^(?:int|float|double|long|decimal)/.test(type ?? '')) {
      const number = Number(value);
      if (!Number.isFinite(number)) throw new TypeError(`JSON value is not numeric: ${value}`);
      return type.startsWith('int') || type.startsWith('long') ? Math.trunc(number) : number;
    }
    return type === 'string' ? String(value) : value;
  }
  ToObject(typeName = null) {
    const value = this._value;
    if (value == null || typeof value !== 'object') return value;
    const constructor = typeName ? UnityType.GetType(typeName) : null;
    if (!constructor) return globalThis.structuredClone(value);
    const result = new constructor();
    const names = new Map();
    for (let current = result; current && current !== globalThis.Object.prototype;
      current = globalThis.Object.getPrototypeOf(current)) {
      for (const name of globalThis.Object.getOwnPropertyNames(current))
        if (name !== 'constructor') names.set(name.toLowerCase(), name);
    }
    for (const [name, item] of globalThis.Object.entries(value))
      result[names.get(name.toLowerCase()) ?? name] = item;
    return result;
  }  GetItem(key) { return JToken.Wrap(this._value?.[key]); }
  SelectToken(path) {
    if (typeof path !== 'string' || !path) return null;
    let value = this._value;
    for (const key of path.split('.')) {
      if (value == null || typeof value !== 'object' || !(key in value)) return null;
      value = value[key];
    }
    return JToken.Wrap(value);
  }
  ToString() { return JSON.stringify(this._value); }
}

export class JObject extends JToken {
  constructor(value = {}) { super(value); }
  get Count() { return globalThis.Object.keys(this._value).length; }
  SetItem(key, value) { this._value[key] = value instanceof JToken ? value._value : value; }
}

export class JArray extends JToken {
  constructor(value = []) { super(value); }
  get Count() { return this._value.length; }
  *[Symbol.iterator]() { for (const value of this._value) yield JToken.Wrap(value); }
}

export class StringBuilder {
  constructor(value = '') { this._value = String(value); }
  Append(value) { this._value += String(value); return this; }
  AppendLine(value = '') { this._value += `${value}\n`; return this; }
  Clear() { this._value = ''; return this; }
  ToString() { return this._value; }
  get Length() { return this._value.length; }
}

export class PreviewTask {
  static get CompletedTask() { return Promise.resolve(); }
  static FromResult(value) { return Promise.resolve(value); }
}

export class PreviewEditorUtility {
  static SetDirty() { /* The scene is serialized after controller import. */ }
}

export class PreviewEditorSceneManager {
  static MarkSceneDirty() { /* The scene is serialized after controller import. */ }
  static SaveScene(scene) { return scene?.IsValid?.() === true; }
}

export class PreviewUndo {
  static RegisterCreatedObjectUndo() { /* Preview materialization has no undo stack. */ }
}

export class PreviewMessageWindow {
  static ShowMessage(message) { throw new Error(String(message)); }
}

export class PreviewPath {
  static Combine(...parts) { return parts.map(part => String(part).replaceAll('\\', '/').replace(/^\/+|\/+$/g, '')).join('/'); }
  static GetDirectoryName(path) {
    const normalized = String(path).replaceAll('\\', '/');
    const index = normalized.lastIndexOf('/');
    return index < 0 ? '' : normalized.slice(0, index);
  }
}

export class PreviewDirectory {
  static Exists() { return true; }
  static CreateDirectory(path) { return String(path); }
}

export class EditorWindow extends ScriptableObject {
  static #windows = new Map();
  static GetWindow(type) {
    if (typeof type !== 'function') throw new TypeError('EditorWindow.GetWindow expects a window type');
    if (!this.#windows.has(type)) this.#windows.set(type, new type());
    return this.#windows.get(type);
  }
  constructor() { super(); this.titleContent = { text: '' }; this.position = null; }
  Close() { EditorWindow.#windows.delete(this.constructor); }
}

export class GUI {
  static _commands = [];
  static _root = null;
  static _beginFrame() { this._commands.length = 0; }
  static Label(position, text) { this._commands.push({ position, text, box: false }); }
  static Box(position, text) { this._commands.push({ position, text, box: true }); }
  static _endFrame() {
    const doc = globalThis.document;
    if (!doc?.body) return;
    if (!this._root || !this._root.isConnected) {
      this._root = doc.createElement('div');
      globalThis.Object.assign(this._root.style, { position: 'fixed', inset: '0', zIndex: '30',
        pointerEvents: 'none', color: '#fff', fontFamily: 'sans-serif' });
      doc.body.appendChild(this._root);
    }
    this._commands.forEach(({ position, text, box }, index) => {
      let item = this._root.children[index];
      if (!item) { item = doc.createElement('div'); this._root.appendChild(item); }
      globalThis.Object.assign(item.style, { position: 'absolute', boxSizing: 'border-box',
        left: `${position.x}px`, top: `${position.y}px`, width: `${position.width}px`,
        height: `${position.height}px`, padding: box ? '8px' : '2px',
        background: box ? 'rgba(10, 18, 27, .78)' : 'transparent',
        border: box ? '1px solid rgba(255,255,255,.35)' : 'none',
        whiteSpace: 'pre-line', overflow: 'hidden', pointerEvents: 'none' });
      item.textContent = String(text ?? '');
    });
    while (this._root.children.length > this._commands.length) this._root.lastChild.remove();
  }
}
export class GUIStyle {
  constructor(source = null) { globalThis.Object.assign(this, typeof source === 'object' ? source ?? {} : {}); this.normal ??= {}; }
}
export class GUILayout {
  static BeginHorizontal() {}
  static EndHorizontal() {}
  static BeginVertical() {}
  static EndVertical() {}
  static Space() {}
  static ExpandWidth(value) { return { expandWidth: Boolean(value) }; }
  static ExpandHeight(value) { return { expandHeight: Boolean(value) }; }
  static Label() {}
  static Button() { return false; }
  static Height(value) { return { height: value }; }
  static Width(value) { return { width: value }; }
}
export class Font extends Object {}
export class Texture2D extends Object {}

export class Color {
  constructor(r = 0, g = 0, b = 0, a = 1) {
    this.r = r; this.g = g; this.b = b; this.a = a;
  }
  clone() { return new Color(this.r, this.g, this.b, this.a); }
  multiplyScalar(value) {
    this.r *= value; this.g *= value; this.b *= value; this.a *= value;
    return this;
  }
  static get white() { return new Color(1, 1, 1); }
  static get red() { return new Color(1, 0, 0); }
  static get green() { return new Color(0, 1, 0); }
  static get cyan() { return new Color(0, 1, 1); }
  static get yellow() { return new Color(1, 1, 0); }
  static get clear() { return new Color(0, 0, 0, 0); }
  static Lerp(left, right, t) {
    const amount = Math.min(1, Math.max(0, t));
    return new Color(left.r + (right.r - left.r) * amount,
      left.g + (right.g - left.g) * amount,
      left.b + (right.b - left.b) * amount,
      left.a + (right.a - left.a) * amount);
  }
  static op_Equality(left, right) {
    return left === right || left != null && right != null
      && left.r === right.r && left.g === right.g && left.b === right.b && left.a === right.a;
  }
  static op_Inequality(left, right) { return !Color.op_Equality(left, right); }
}

Mathf.CorrelatedColorTemperatureToRGB = kelvin => {
  const temperature = Math.min(40000, Math.max(1000, kelvin)) / 100;
  const red = temperature <= 66 ? 255
    : 329.698727446 * (temperature - 60) ** -0.1332047592;
  const green = temperature <= 66
    ? 99.4708025861 * Math.log(temperature) - 161.1195681661
    : 288.1221695283 * (temperature - 60) ** -0.0755148492;
  const blue = temperature >= 66 ? 255 : temperature <= 19 ? 0
    : 138.5177312231 * Math.log(temperature - 10) - 305.0447927307;
  return new Color(...[red, green, blue].map(value => Math.min(255, Math.max(0, value)) / 255));
};

export class ColorUtility {
  static TryParseHtmlString(text, output) {
    if (!output || typeof text !== 'string') return false;
    const match = /^#?([\da-f]{6})([\da-f]{2})?$/i.exec(text);
    if (!match) { output.value = new Color(); return false; }
    const value = match[1];
    output.value = new Color(...[0, 2, 4].map(index => parseInt(value.slice(index, index + 2), 16) / 255),
      match[2] ? parseInt(match[2], 16) / 255 : 1);
    return true;
  }
  static ToHtmlStringRGB(color) {
    return [color.r, color.g, color.b].map(value => Math.round(Math.max(0, Math.min(1, value)) * 255)
      .toString(16).padStart(2, '0').toUpperCase()).join('');
  }
}
export class UnsupportedUnityAPIError extends Error {
  constructor(api) { super(`${api} is not implemented by this Unity bridge`); this.name = 'UnsupportedUnityAPIError'; }
}

export class MissingComponentException extends Error {
  constructor(message) { super(message); this.name = 'MissingComponentException'; }
}

export class UnityException extends Error {}

export class Gizmos {
  static color = Color.white;
  static DrawSphere() {}
  static DrawWireSphere() {}
  static DrawLine() {}
}

export const Space = globalThis.Object.freeze({ World: 0, Self: 1 });
export const PrimitiveType = globalThis.Object.freeze({
  Sphere: 0, Capsule: 1, Cylinder: 2, Cube: 3, Plane: 4, Quad: 5,
});

export class Debug {
  static Log(...values) { console.log(...values); }
  static LogWarning(...values) { console.warn(...values); }
  static LogError(...values) { console.error(...values); }
  static Assert(condition, ...values) {
    if (!condition) console.error('Unity assertion failed:', ...values);
  }
  static DrawRay() {}
  static DrawLine() {}
}

export class Random {
  static _state = new Uint32Array(4);
  static {
    this.InitState((Math.random() * 0x100000000) | 0);
  }
  static InitState(seed) {
    this._state[0] = seed | 0;
    for (let index = 1; index < 4; index++) {
      const previous = this._state[index - 1];
      this._state[index] = (Math.imul(1812433253, previous) + 1) >>> 0;
    }
  }
  static get state() {
    const [s0, s1, s2, s3] = this._state;
    return { s0: s0 | 0, s1: s1 | 0, s2: s2 | 0, s3: s3 | 0 };
  }
  static set state(value) {
    this._state.set([value.s0, value.s1, value.s2, value.s3]);
  }
  static _nextRaw() {
    const state = this._state;
    const shifted = state[0] ^ (state[0] << 11);
    state[0] = state[1];
    state[1] = state[2];
    state[2] = state[3];
    state[3] = state[3] ^ (state[3] >>> 19) ^ shifted ^ (shifted >>> 8);
    return state[3] >>> 0;
  }
  static _nextValue() { return Math.fround((this._nextRaw() & 0x7fffff) / 0x7fffff); }
  static get value() { return this._nextValue(); }
  static RangeFloat(min, max) {
    return Math.fround(max - Math.fround(this._nextValue() * (max - min)));
  }
  static Range(min, max) {
    return Number.isInteger(min) && Number.isInteger(max)
      ? (max <= min ? min : this._nextRaw() % (max - min) + min)
      : this.RangeFloat(min, max);
  }
}

// Transpiled scripts subclass Component and override PascalCase Unity callbacks.
export class Component extends Object {
  constructor() {
    super();
    this.gameObject = null;
    this._enabled = true;
    this._awoken = false;
    this._active = false;
    this._started = false;
    this._destroyed = false;
  }
  get transform() { return this.gameObject?.transform ?? null; }
  get name() { return this.gameObject?.name ?? ''; }
  set name(value) { if (this.gameObject) this.gameObject.name = value; }
  get tag() { return this.gameObject?.tag ?? 'Untagged'; }
  set tag(value) { if (this.gameObject) this.gameObject.tag = value; }
  get enabled() { return this._enabled; }
  set enabled(value) {
    if (this._destroyed) throw new Error('Component has been destroyed');
    this._enabled = Boolean(value);
    this.gameObject?._runtime?._reconcile();
  }
  GetComponent(type) { return this.gameObject?.GetComponent(type) ?? null; }
  GetComponents(type) { return this.gameObject?.GetComponents(type) ?? []; }
  GetComponentInChildren(type, includeInactive = false) {
    return this.gameObject?.GetComponentInChildren(type, includeInactive) ?? null;
  }
  GetComponentsInChildren(type, includeInactive = false) {
    return this.gameObject?.GetComponentsInChildren(type, includeInactive) ?? [];
  }
  GetComponentInParent(type, includeInactive = false) {
    return this.gameObject?.GetComponentInParent(type, includeInactive) ?? null;
  }
  GetComponentsInParent(type, includeInactive = false) {
    return this.gameObject?.GetComponentsInParent(type, includeInactive) ?? [];
  }
  CompareTag(tag) { return this.gameObject?.CompareTag(tag) ?? false; }
  Destroy() {
    if (this._destroyed) return;
    if (this instanceof Transform) throw new Error('Cannot destroy a Transform');
    if (this._active) { this._active = false; this.OnDisable?.(); }
    if (this._awoken) this.OnDestroy?.();
    const components = this.gameObject?._components;
    if (components) components.splice(components.indexOf(this), 1);
    if (components) this.gameObject._runtime?._invalidateComponents();
    this._destroyed = true;
  }
}

export class Behaviour extends Component {
  get isActiveAndEnabled() { return this._active; }
}
export class StateMachineBehaviour extends Object {
  OnStateEnter() {}
  OnStateUpdate() {}
  OnStateExit() {}
  OnStateMove() {}
  OnStateIK() {}
}
export class Canvas extends Behaviour {
  constructor() { super(); this.sortingOrder = 0; this.renderMode = 0; }
}
export class Button extends Behaviour {
  constructor() { super(); this.interactable = true; this.onClick = new UnityEvent(); }
}
export class Image extends Behaviour {
  constructor() { super(); this.color = Color.white; this.sprite = null; }
}
export class Text extends Behaviour {
  constructor() { super(); this.color = Color.white; this.text = ''; this.fontSize = 14; }
}
// Serialized presentation components must survive prefab import even when the
// browser player does not render their Unity-specific inspector settings.
export class UniversalAdditionalCameraData extends Behaviour {}
export class CanvasScaler extends Behaviour {}
export class GraphicRaycaster extends Behaviour {}
export class EventSystem extends Behaviour {}
export class StandaloneInputModule extends Behaviour {}
export class Mask extends Behaviour {}
export class Shadow extends Behaviour {}
export class Outline extends Shadow {}
export class EventTrigger extends Behaviour {}
export class ScrollRect extends Behaviour {}
export class GridLayoutGroup extends Behaviour {}
export class Scrollbar extends Behaviour {}
export class VerticalLayoutGroup extends Behaviour {}
export class HorizontalLayoutGroup extends Behaviour {}
export class Slider extends Behaviour {}
export class Toggle extends Behaviour {}
export class Dropdown extends Behaviour {}
export class AudioListener extends Behaviour {
  static volume = 1;
}

export class AudioSource extends Behaviour {
  static backend = null;
  static ConfigureBackend(backend) { this.backend = backend; }
  constructor() {
    super();
    this.clip = null;
    this.pitch = 1;
    this.volume = 1;
    this.loop = false;
    this.playOnAwake = false;
    this.spatialBlend = 0;
    this.minDistance = 1;
    this.maxDistance = 500;
    this._context = null;
    this._playing = new Set();
  }
  get isPlaying() { return this._playing.size > 0; }
  Start() { if (this.playOnAwake) this.Play(); }
  Play() { if (this.clip) this._playClip(this.clip, this.loop, 1); }
  PlayOneShot(clip, volumeScale = 1) { this._playClip(clip, false, volumeScale); }
  _playClip(clip, loop, volumeScale) {
    if (!this.enabled || !clip) return;
    if (AudioSource.backend) return AudioSource.backend.play(this, clip, { loop, volumeScale });
    if (typeof AudioContext === 'undefined') throw new UnsupportedUnityAPIError('AudioSource.PlayOneShot outside browser audio');
    if (!(clip instanceof AudioBuffer)) throw new TypeError('AudioSource.PlayOneShot needs a decoded AudioBuffer');
    this._context ??= new AudioContext();
    const source = this._context.createBufferSource();
    const gain = this._context.createGain();
    source.buffer = clip;
    source.loop = loop;
    source.playbackRate.value = this.pitch;
    gain.gain.value = this.volume * volumeScale;
    const blend = Math.max(0, Math.min(1, this.spatialBlend));
    let panner = null;
    if (blend > 0) {
      panner = this._context.createPanner();
      panner.distanceModel = 'linear';
      panner.refDistance = this.minDistance;
      panner.maxDistance = this.maxDistance;
      const spatialGain = this._context.createGain();
      spatialGain.gain.value = blend;
      source.connect(panner).connect(spatialGain).connect(gain);
    }
    if (blend < 1) {
      const directGain = this._context.createGain();
      directGain.gain.value = 1 - blend;
      source.connect(directGain).connect(gain);
    }
    gain.connect(this._context.destination);
    const playing = { source, panner };
    this._playing.add(playing);
    source.onended = () => this._playing.delete(playing);
    this._context.resume();
    this.LateUpdate();
    source.start();
  }
  LateUpdate() {
    if (!this._context || !this._playing.size) return;
    const position = this.transform.position;
    for (const { panner } of this._playing) if (panner) {
      panner.positionX.value = position.x;
      panner.positionY.value = position.y;
      panner.positionZ.value = position.z;
    }
    const camera = Camera.main;
    if (camera) {
      const listener = this._context.listener;
      const eye = camera.transform.position;
      const forward = camera.transform.forward;
      listener.positionX.value = eye.x;
      listener.positionY.value = eye.y;
      listener.positionZ.value = eye.z;
      listener.forwardX.value = forward.x;
      listener.forwardY.value = forward.y;
      listener.forwardZ.value = forward.z;
    }
  }
  Stop() {
    AudioSource.backend?.stop?.(this);
    for (const { source } of this._playing) source.stop();
    this._playing.clear();
  }
  OnDestroy() { this.Stop(); this._context?.close(); }
}
export class ParticleSystem extends Behaviour {
  constructor() {
    super();
    this.emittedCount = 0;
    this.startLifetime = 1;
    this.startSpeed = 1;
    this.startSize = 0.1;
    this.startColor = Color.white;
    this.gravityModifier = 0;
    this.rateOverTime = 0;
    this.loop = false;
    this.playOnAwake = false;
    this.maxParticles = 1000;
    this.isPlaying = false;
    this._particles = [];
    this._emissionRemainder = 0;
    this._duration = 0;
  }
  Awake() {
    this._geometry = new THREE.BufferGeometry();
    this._material = new THREE.PointsMaterial({ size: this.startSize,
      color: new THREE.Color(this.startColor.r, this.startColor.g, this.startColor.b),
      transparent: true, opacity: this.startColor.a, depthWrite: false });
    this._points = new THREE.Points(this._geometry, this._material);
    this.gameObject.object3D.add(this._points);
  }
  Start() { if (this.playOnAwake) this.Play(); }
  Play() { this.isPlaying = true; }
  Stop() { this.isPlaying = false; }
  Clear() { this._particles.length = 0; this._syncGeometry(); }
  Emit(count) {
    if (!Number.isInteger(count) || count < 0) throw new RangeError('Particle count must be nonnegative');
    this.emittedCount += count;
    for (let i = 0; i < count && this._particles.length < this.maxParticles; i++) {
      const direction = new Vector3(Math.random() - 0.5, Math.random(), Math.random() - 0.5).normalize();
      this._particles.push({ position: new Vector3(), velocity: direction.multiplyScalar(this.startSpeed),
        age: 0, lifetime: Math.max(0.001, this.startLifetime) });
    }
    this._syncGeometry();
  }
  LateUpdate() {
    const dt = Time.deltaTime;
    if (this.isPlaying && this.rateOverTime > 0) {
      this._emissionRemainder += this.rateOverTime * dt;
      const count = Math.floor(this._emissionRemainder);
      this._emissionRemainder -= count;
      if (count) this.Emit(count);
    }
    for (const particle of this._particles) {
      particle.age += dt;
      particle.velocity.y += Physics.gravity.y * this.gravityModifier * dt;
      particle.position.addScaledVector(particle.velocity, dt);
    }
    this._particles = this._particles.filter(particle => particle.age < particle.lifetime);
    this._syncGeometry();
  }
  _syncGeometry() {
    if (!this._geometry) return;
    const positions = new Float32Array(this._particles.length * 3);
    this._particles.forEach((particle, index) => particle.position.toArray(positions, index * 3));
    this._geometry.setAttribute('position', new THREE.BufferAttribute(positions, 3));
    this._geometry.computeBoundingSphere();
    this._material.size = this.startSize;
    this._material.color.setRGB(this.startColor.r, this.startColor.g, this.startColor.b);
    this._material.opacity = this.startColor.a;
  }
  OnDestroy() {
    this._points?.removeFromParent();
    this._geometry?.dispose();
    this._material?.dispose();
  }
}
export class WaitForSeconds {
  constructor(seconds) { this.seconds = seconds; }
}
export class WaitForEndOfFrame {}
export class WaitUntil {
  constructor(predicate) {
    if (typeof predicate !== 'function') throw new TypeError('WaitUntil needs a predicate');
    this.predicate = predicate;
  }
}

export class MonoBehaviour extends Behaviour {
  constructor() { super(); this._invocations = []; }
  Invoke(methodName, time) {
    if (typeof this[methodName] !== 'function') throw new Error(`Unknown invoked method: ${methodName}`);
    if (!Number.isFinite(time) || time < 0) throw new RangeError('Invoke delay must be nonnegative');
    const runtime = this.gameObject?._runtime;
    this._invocations.push({ methodName, remaining: time, repeating: false,
      fixedDueTick: runtime?._insideUpdate
        ? runtime._fixedTickCount + Math.floor(time / runtime.fixedDeltaTime) + 1 : null });
  }
  InvokeRepeating(methodName, time, repeatRate) {
    if (typeof this[methodName] !== 'function') throw new Error(`Unknown invoked method: ${methodName}`);
    if (!Number.isFinite(time) || time < 0 || !Number.isFinite(repeatRate) || repeatRate <= 0)
      throw new RangeError('InvokeRepeating needs a nonnegative delay and positive repeat rate');
    this._invocations.push({ methodName, remaining: time, repeating: true, repeatRate });
  }
  IsInvoking(methodName = null) {
    return this._invocations.some(call => methodName === null || call.methodName === methodName);
  }
  CancelInvoke(methodName = null) {
    this._invocations = this._invocations.filter(call => methodName !== null && call.methodName !== methodName);
  }
  StartCoroutine(routine) {
    const runtime = this.gameObject?._runtime;
    if (!runtime) throw new Error('StartCoroutine needs a GameObject in a runtime');
    const iterator = typeof routine === 'function' ? routine() : routine;
    if (!iterator?.next) throw new TypeError('Coroutine must be a generator');
    const state = { owner: this, iterator, remaining: 0, frame: runtime.time.frameCount };
    runtime._coroutines.add(state);
    runtime._resumeCoroutine(state);
    return state;
  }
  StopCoroutine(state) { this.gameObject?._runtime?._coroutines.delete(state); }
  StopAllCoroutines() {
    const runtime = this.gameObject?._runtime;
    if (runtime) for (const state of runtime._coroutines) {
      if (state.owner === this) runtime._coroutines.delete(state);
    }
  }
  _advanceInvocations(deltaTime) {
    if (!this._active) return;
    for (const call of [...this._invocations]) {
      if (!this._invocations.includes(call)) continue;
      if (call.fixedDueTick != null) {
        if ((this.gameObject?._runtime?._fixedTickCount ?? 0) < call.fixedDueTick) continue;
      } else {
        call.remaining -= deltaTime;
        if (call.remaining > 0) continue;
      }
      if (call.repeating) call.remaining += call.repeatRate;
      else this._invocations.splice(this._invocations.indexOf(call), 1);
      this[call.methodName]();
    }
  }
}

export class NetworkBehaviour extends MonoBehaviour {
  constructor() { super(); this.IsOwner = true; this.IsServer = true; this.IsClient = true; }
}

export class InputSystem {
  static onActionChange = new UnityEvent();
  static onDeviceChange = new UnityEvent();
  static onEvent = new UnityEvent();
}

export class Gamepad { static get current() { return null; } }
export class DualShockGamepad extends Gamepad {}
export class Pointer { static get current() { return null; } }
export class Touchscreen extends Pointer { static get current() { return null; } }

export class OnScreenControl extends MonoBehaviour {
  get controlPathInternal() { return this._controlPath ?? ''; }
  set controlPathInternal(value) { this._controlPath = value; }
  SendValueToControl(value) { this._controlValue = value; }
}

export class Transform extends Component {
  get object3D() { return this.gameObject.object3D; }
  get parent() { return this.gameObject.parent?.transform ?? null; }
  set parent(value) { this.SetParent(value); }
  get root() {
    let current = this;
    while (current.parent) current = current.parent;
    return current;
  }
  get childCount() { return this.gameObject.children.length; }
  GetChild(index) { return this.gameObject.children[index]?.transform ?? null; }
  *[Symbol.iterator]() { for (const child of this.gameObject.children) yield child.transform; }
  Find(path) {
    if (typeof path !== 'string' || !path) return null;
    return path.split('/').reduce((parent, name) =>
      parent?.gameObject.children.find(child => child.name === name)?.transform ?? null, this);
  }
  SetParent(parent, worldPositionStays = true) { this.gameObject.SetParent(parent?.gameObject ?? null, worldPositionStays); }
  get localPosition() { return this.object3D.position.clone(); }
  set localPosition(value) { this.object3D.position.copy(value); }
  get position() { return this.object3D.getWorldPosition(new Vector3()); }
  set position(value) {
    const local = value.clone();
    const parent = this.object3D.parent;
    if (parent) { parent.updateWorldMatrix(true, false); parent.worldToLocal(local); }
    this.object3D.position.copy(local);
  }
  get localRotation() { return this.object3D.quaternion.clone(); }
  set localRotation(value) { this.object3D.quaternion.copy(value); }
  get rotation() { return this.object3D.getWorldQuaternion(new Quaternion()); }
  set rotation(value) {
    const local = value.clone();
    const parent = this.object3D.parent;
    if (parent) {
      parent.updateWorldMatrix(true, false);
      local.premultiply(parent.getWorldQuaternion(new Quaternion()).invert());
    }
    this.object3D.quaternion.copy(local);
  }
  SetPositionAndRotation(position, rotation) {
    this.position = position;
    this.rotation = rotation;
  }
  get localScale() { return this.object3D.scale.clone(); }
  set localScale(value) { this.object3D.scale.copy(value); }
  get lossyScale() { return this.object3D.getWorldScale(new Vector3()); }
  get localEulerAngles() {
    const euler = new THREE.Euler().setFromQuaternion(this.localRotation, 'YXZ');
    return new Vector3(euler.x, euler.y, euler.z).multiplyScalar(Mathf.Rad2Deg);
  }
  set localEulerAngles(value) { this.localRotation = Quaternion.Euler(value); }
  get eulerAngles() {
    const euler = new THREE.Euler().setFromQuaternion(this.rotation, 'YXZ');
    return new Vector3(euler.x, euler.y, euler.z).multiplyScalar(Mathf.Rad2Deg);
  }
  set eulerAngles(value) { this.rotation = Quaternion.Euler(value); }
  LookAt(target) {
    const point = target instanceof Transform ? target.position : target;
    this.rotation = Quaternion.LookRotation(point.clone().sub(this.position));
  }
  TransformPoint(point) {
    this.object3D.updateWorldMatrix(true, false);
    return point.clone().applyMatrix4(this.object3D.matrixWorld);
  }
  InverseTransformPoint(point) {
    this.object3D.updateWorldMatrix(true, false);
    return this.object3D.worldToLocal(point.clone());
  }
  TransformDirection(direction) { return direction.clone().applyQuaternion(this.rotation); }
  InverseTransformDirection(direction) { return direction.clone().applyQuaternion(this.rotation.invert()); }
  Translate(translation, relativeTo = Space.Self, z = 0, space = Space.Self) {
    if (typeof translation === 'number') {
      translation = new Vector3(translation, relativeTo, z);
      relativeTo = space;
    }
    const delta = translation.clone();
    if (relativeTo === Space.Self) delta.applyQuaternion(this.rotation);
    this.position = this.position.add(delta);
  }
  Rotate(eulerAngles, relativeTo = Space.Self, z = 0, space = Space.Self) {
    if (typeof eulerAngles === 'number') {
      eulerAngles = new Vector3(eulerAngles, relativeTo, z);
      relativeTo = space;
    }
    const turn = Quaternion.Euler(eulerAngles);
    if (relativeTo === Space.Self) this.localRotation = this.localRotation.clone().multiply(turn);
    else this.rotation = turn.multiply(this.rotation);
  }
  get forward() { return Vector3.forward.applyQuaternion(this.rotation); }
  get up() { return Vector3.up.applyQuaternion(this.rotation); }
  get right() { return Vector3.right.applyQuaternion(this.rotation); }
}

export class RectTransform extends Transform {
  constructor() {
    super();
    this.anchorMin = new Vector2(0.5, 0.5);
    this.anchorMax = new Vector2(0.5, 0.5);
    this.pivot = new Vector2(0.5, 0.5);
    this.sizeDelta = new Vector2(100, 100);
    this.rect = { x: -50, y: -50, width: 100, height: 100 };
  }
  get anchoredPosition() { return new Vector2(this.localPosition.x, this.localPosition.y); }
  set anchoredPosition(value) { this.localPosition = new Vector3(value.x, value.y, this.localPosition.z); }
}

export class RectTransformUtility {
  static ScreenPointToLocalPointInRectangle(rectTransform, screenPoint, camera, output) {
    if (!rectTransform || !screenPoint) return false;
    const origin = rectTransform.position;
    output.value = new Vector2(screenPoint.x - origin.x, screenPoint.y - origin.y);
    return true;
  }
}

export class GameObject extends Object {
  static CreatePrimitive(type) {
    const definitions = [
      ['Sphere', () => new THREE.SphereGeometry(0.5), SphereCollider],
      ['Capsule', () => new THREE.CapsuleGeometry(0.5, 1), CapsuleCollider],
      ['Cylinder', () => new THREE.CylinderGeometry(0.5, 0.5, 2), MeshCollider],
      ['Cube', () => new THREE.BoxGeometry(1, 1, 1), BoxCollider],
      ['Plane', () => new THREE.PlaneGeometry(10, 10).rotateX(-Math.PI / 2), MeshCollider],
      ['Quad', () => new THREE.PlaneGeometry(1, 1), MeshCollider],
    ];
    const definition = definitions[type];
    if (!definition) throw new RangeError(`Unsupported PrimitiveType: ${type}`);
    const [name, createGeometry, colliderType] = definition;
    const object = new GameObject(name);
    object.AddComponent(MeshFilter).sharedMesh = createGeometry();
    object.AddComponent(MeshRenderer);
    object.AddComponent(colliderType);
    activeRuntime?.add(object);
    return object;
  }
  constructor(name = 'GameObject', transformType = Transform) {
    super();
    this.object3D = new THREE.Object3D();
    this.object3D.name = name;
    this.object3D.userData.gameObject = this;
    this._activeSelf = true;
    this.tag = 'Untagged';
    this.layer = 0;
    this._parent = null;
    this._children = [];
    this._runtime = null;
    this._destroyed = false;
    this._components = [];
    this.transform = this.AddComponent(transformType);
  }
  get gameObject() { return this; }
  get scene() { return this._runtime ? new Scene(this._runtime) : PreviewInvalidScene; }
  get name() { return this.object3D.name; }
  set name(value) { this.object3D.name = String(value); }
  get parent() { return this._parent; }
  get children() { return [...this._children]; }
  get activeSelf() { return this._activeSelf; }
  get activeInHierarchy() { return this._activeSelf && (!this._parent || this._parent.activeInHierarchy); }
  SetActive(value) {
    if (this._destroyed) throw new Error('GameObject has been destroyed');
    this._activeSelf = Boolean(value);
    this.object3D.visible = this._activeSelf;
    this._runtime?._reconcile();
  }
  AddComponent(typeOrInstance) {
    if (this._destroyed) throw new Error('GameObject has been destroyed');
    const component = typeof typeOrInstance === 'function' ? new typeOrInstance() : typeOrInstance;
    if (!(component instanceof Component) || component.gameObject) throw new TypeError('Expected an unattached Component');
    if (component instanceof Transform && this.transform) {
      if (component instanceof RectTransform && !(this.transform instanceof RectTransform)) {
        const previous = this.transform;
        component.gameObject = this;
        component.assetId = previous.assetId;
        this._components.splice(this._components.indexOf(previous), 1, component);
        this.transform = component;
        this._runtime?._invalidateComponents();
        this._runtime?._reconcile();
        return component;
      }
      throw new Error('GameObject already has a Transform');
    }
    component.gameObject = this;
    this._components.push(component);
    this._runtime?._invalidateComponents();
    if (this._runtime?.editorMode && typeof typeOrInstance === 'function') component.Reset?.();
    this._runtime?._reconcile();
    return component;
  }
  GetComponent(type) { return this._components.find(component => component instanceof type) ?? null; }
  GetComponents(type) { return this._components.filter(component => component instanceof type); }
  CompareTag(tag) { return this.tag === tag; }
  GetComponentInChildren(type, includeInactive = false) {
    if (includeInactive || this.activeInHierarchy) {
      const own = this.GetComponent(type);
      if (own) return own;
    }
    for (const child of this._children) {
      if (includeInactive || child.activeInHierarchy) {
        const found = child.GetComponentInChildren(type, includeInactive);
        if (found) return found;
      }
    }
    return null;
  }
  GetComponentsInChildren(type, includeInactive = false) {
    const result = [];
    const visit = object => {
      if (!includeInactive && !object.activeInHierarchy) return;
      result.push(...object.GetComponents(type));
      for (const child of object._children) visit(child);
    };
    visit(this);
    return result;
  }
  GetComponentsInParent(type, includeInactive = false) {
    const result = [];
    for (let object = this; object; object = object._parent) {
      if (includeInactive || object.activeInHierarchy) result.push(...object.GetComponents(type));
    }
    return result;
  }
  static Find(name) {
    return [...(activeRuntime?._objects ?? [])].find(object => object.activeInHierarchy && object.name === name) ?? null;
  }
  static FindWithTag(tag) {
    return [...(activeRuntime?._objects ?? [])].find(object => object.activeInHierarchy && object.CompareTag(tag)) ?? null;
  }
  static FindGameObjectWithTag(tag) { return this.FindWithTag(tag); }
  static FindGameObjectsWithTag(tag) {
    return [...(activeRuntime?._objects ?? [])].filter(object => object.activeInHierarchy && object.CompareTag(tag));
  }
  GetComponentInParent(type, includeInactive = false) {
    for (let object = this; object; object = object._parent) {
      if (includeInactive || object.activeInHierarchy) {
        const found = object.GetComponent(type);
        if (found) return found;
      }
    }
    return null;
  }
  SetParent(parent, worldPositionStays = true) {
    if (this._destroyed || parent?._destroyed) throw new Error('Cannot parent a destroyed GameObject');
    if (parent !== null && !(parent instanceof GameObject)) throw new TypeError('Parent must be a GameObject or null');
    for (let cursor = parent; cursor; cursor = cursor._parent) {
      if (cursor === this) {
        console.error(`Cannot parent ${this.name} to itself or one of its descendants`);
        return;
      }
    }
    if (parent === this._parent) return;
    if (parent && this._runtime && parent._runtime !== this._runtime) throw new Error('Cannot parent outside this runtime');
    const oldRuntime = this._runtime;
    const newRuntime = parent?._runtime ?? oldRuntime;
    if (oldRuntime && newRuntime !== oldRuntime) oldRuntime._unregisterTree(this);
    if (this._parent) this._parent._children.splice(this._parent._children.indexOf(this), 1);
    this._parent = parent;
    if (parent) parent._children.push(this);
    const destination = parent?.object3D ?? oldRuntime?.scene ?? null;
    if (destination) {
      if (worldPositionStays) destination.attach(this.object3D);
      else destination.add(this.object3D);
    } else this.object3D.removeFromParent();
    if (newRuntime && newRuntime !== oldRuntime) newRuntime._registerTree(this);
    oldRuntime?._reconcile();
    if (newRuntime !== oldRuntime) newRuntime?._reconcile();
  }
  Destroy() {
    if (this._destroyed) return;
    this._runtime?._unregisterTree(this);
    for (const child of [...this._children]) child.Destroy();
    for (const component of this._components) {
      if (component._awoken) component.OnDestroy?.();
      component._destroyed = true;
    }
    if (this._parent) this._parent._children.splice(this._parent._children.indexOf(this), 1);
    this._parent = null;
    this.object3D.removeFromParent();
    this._destroyed = true;
  }
}

export class UnityRuntime {
  constructor({ scene = new THREE.Scene(), fixedDeltaTime = 1 / 50, maxFixedSteps = 5,
    editorMode = false } = {}) {
    if (fixedDeltaTime <= 0 || maxFixedSteps < 1) throw new RangeError('Invalid fixed timestep settings');
    this.scene = scene;
    this.editorMode = editorMode;
    this.fixedDeltaTime = fixedDeltaTime;
    this.maxFixedSteps = maxFixedSteps;
    this.time = { time: 0, deltaTime: 0, unscaledTime: 0, unscaledDeltaTime: 0, fixedDeltaTime, frameCount: 0 };
    this.timeScale = 1;
    this._objects = new Set();
    this._componentCache = null;
    this._resources = new Map();
    this._systems = new Set();
    this._destroyQueue = new Set();
    this._delayedDestroy = new Map();
    this._coroutines = new Set();
    this.errors = [];
    this._accumulator = 0;
    this._fixedTickCount = 0;
    this._insideUpdate = false;
    this._running = false;
  }
  add(gameObject) {
    this.addMany([gameObject]);
    return gameObject;
  }
  addMany(gameObjects) {
    for (const gameObject of gameObjects) {
      if (!(gameObject instanceof GameObject) || gameObject._destroyed) throw new TypeError('Expected a live GameObject');
      if (gameObject._runtime && gameObject._runtime !== this) throw new Error('GameObject belongs to another runtime');
      if (gameObject.parent && gameObject.parent._runtime !== this && !gameObjects.includes(gameObject.parent))
        throw new Error('Add the parent first');
    }
    activeRuntime = this;
    for (const gameObject of gameObjects) {
      if (!gameObject.parent) this.scene.add(gameObject.object3D);
      this._registerTree(gameObject);
    }
    this._reconcile();
    return gameObjects;
  }
  remove(gameObject) {
    if (gameObject._runtime !== this) return;
    this._unregisterTree(gameObject);
    if (gameObject._parent) {
      const siblings = gameObject._parent._children;
      siblings.splice(siblings.indexOf(gameObject), 1);
      gameObject._parent = null;
    }
    gameObject.object3D.removeFromParent();
  }
  addSystem(system) {
    if (!system || typeof system !== 'object') throw new TypeError('Expected a runtime system');
    activeRuntime = this;
    this._systems.add(system);
    system.attach?.(this);
    return system;
  }
  registerResource(path, asset) {
    if (typeof path !== 'string' || !path || asset == null)
      throw new TypeError('Expected a Resources path and asset');
    this._resources.set(path, asset);
    return asset;
  }
  removeSystem(system) {
    if (this._systems.delete(system)) system.detach?.(this);
  }
  _phase(name, ...args) {
    for (const system of [...this._systems]) system[name]?.(...args);
  }
  _registerTree(root) {
    const visit = object => {
      if (object._runtime && object._runtime !== this) throw new Error('Hierarchy belongs to another runtime');
      object._runtime = this;
      this._objects.add(object);
      for (const child of object._children) visit(child);
    };
    visit(root);
    this._invalidateComponents();
  }
  _unregisterTree(root) {
    const visit = object => {
      for (const component of object._components) {
        if (component._active) { component._active = false; component.OnDisable?.(); }
      }
      this._objects.delete(object);
      object._runtime = null;
      for (const child of object._children) visit(child);
    };
    visit(root);
    this._invalidateComponents();
  }
  _invalidateComponents() { this._componentCache = null; }
  _components() {
    return this._componentCache ??= [...this._objects].flatMap(object => object._components);
  }
  _reconcile() {
    activeRuntime = this;
    if (this.editorMode) return;
    for (const component of this._components()) {
      if (component._destroyed) continue;
      const activeObject = component.gameObject._runtime === this && component.gameObject.activeInHierarchy;
      if (activeObject && !component._awoken) { component._awoken = true; component.Awake?.(); }
      const shouldEnable = activeObject && component.enabled && !component._destroyed;
      if (shouldEnable !== component._active) {
        component._active = shouldEnable;
        if (shouldEnable) component.OnEnable?.();
        else component.OnDisable?.();
      }
    }
  }
  _componentError(component, method, error) {
    const detail = { component: component.constructor.name, object: component.gameObject?.name ?? '',
      method, message: error instanceof Error ? error.message : String(error) };
    this.errors.push(detail);
    console.error(`Preview script ${detail.component}.${method} failed on ${detail.object}: ${detail.message}`, error);
    component._enabled = false;
    component._active = false;
    try { component.OnDisable?.(); } catch (disableError) {
      console.error(`Preview script ${detail.component}.OnDisable failed`, disableError);
    }
  }
  _invoke(method) {
    for (const component of this._components()) {
      if (component._active && component.gameObject._runtime === this) {
        if (method === 'Update' && component._animatorUpdateDriver?.updateMode === 1) continue;
        try { component[method]?.(); }
        catch (error) { this._componentError(component, method, error); }
      }
    }
  }
  _updateFixedAnimators() {
    // Unity's Fixed Animator mode evaluates once per physics tick, before FixedUpdate.
    // Run controller graphs first so their Preview pose drivers receive fresh outputs.
    for (const poseDriver of [false, true]) {
      for (const component of this._components()) {
        if (!component._active || component.gameObject._runtime !== this
          || component._animatorUpdateDriver?.updateMode !== 1
          || (component._animatorUpdateDriver !== component) !== poseDriver) continue;
        try { component.Update?.(); }
        catch (error) { this._componentError(component, 'Update', error); }
      }
    }
  }
  _resumeCoroutine(state) {
    const next = state.iterator.next();
    if (next.done) { this._coroutines.delete(state); return; }
    state.wait = next.value;
    state.remaining = next.value instanceof WaitForSeconds ? next.value.seconds : 0;
    state.frame = this.time.frameCount;
  }
  _advanceCoroutines(deltaTime) {
    for (const state of [...this._coroutines]) {
      if (!state.owner._active || state.owner._destroyed) {
        this._coroutines.delete(state);
        continue;
      }
      if (state.frame === this.time.frameCount) continue;
      if (state.wait instanceof WaitForEndOfFrame) continue;
      if (state.wait instanceof WaitUntil && !state.wait.predicate()) continue;
      state.remaining -= deltaTime;
      if (state.remaining <= 0) this._resumeCoroutine(state);
    }
  }
  _advanceEndOfFrameCoroutines() {
    for (const state of [...this._coroutines]) {
      if (!(state.wait instanceof WaitForEndOfFrame)) continue;
      if (!state.owner._active || state.owner._destroyed) {
        this._coroutines.delete(state);
        continue;
      }
      this._resumeCoroutine(state);
    }
  }
  step(deltaTime) {
    if (!Number.isFinite(deltaTime) || deltaTime < 0) throw new RangeError('deltaTime must be a nonnegative finite number');
    activeRuntime = this;
    for (const component of this._components()) {
      if (component._active && !component._started) {
        component._started = true;
        try { component.Start?.(); }
        catch (error) { this._componentError(component, 'Start', error); }
      }
    }
    const scaledDelta = deltaTime * this.timeScale;
    this.time.unscaledDeltaTime = deltaTime;
    this.time.unscaledTime += deltaTime;
    this._accumulator += scaledDelta;
    let fixedSteps = 0;
    while (this._accumulator >= this.fixedDeltaTime && fixedSteps < this.maxFixedSteps) {
      this.time.deltaTime = this.fixedDeltaTime;
      this._phase('beforeFixedUpdate', this.fixedDeltaTime);
      this._updateFixedAnimators();
      this._invoke('FixedUpdate');
      this._fixedTickCount++;
      this._phase('afterFixedUpdate', this.fixedDeltaTime);
      this._accumulator -= this.fixedDeltaTime;
      fixedSteps++;
    }
    if (fixedSteps === this.maxFixedSteps) this._accumulator = Math.min(this._accumulator, this.fixedDeltaTime);
    this.time.deltaTime = scaledDelta;
    this.time.time += scaledDelta;
    this.time.frameCount++;
    this._phase('beforeUpdate', scaledDelta);
    for (const component of this._components()) component._advanceInvocations?.(scaledDelta);
    this._insideUpdate = true;
    try { this._invoke('Update'); }
    finally { this._insideUpdate = false; }
    this._advanceCoroutines(scaledDelta);
    this._invoke('LateUpdate');
    this._advanceEndOfFrameCoroutines();
    GUI._beginFrame();
    this._invoke('OnGUI');
    GUI._endFrame();
    this._phase('afterLateUpdate', scaledDelta);
    for (const [target, remaining] of this._delayedDestroy) {
      if (remaining <= scaledDelta) {
        this._delayedDestroy.delete(target);
        this._destroyQueue.add(target);
      } else this._delayedDestroy.set(target, remaining - scaledDelta);
    }
    for (const target of this._destroyQueue) target.Destroy?.();
    this._destroyQueue.clear();
  }
  start({ render, requestFrame = globalThis.requestAnimationFrame, cancelFrame = globalThis.cancelAnimationFrame } = {}) {
    if (this._running) return;
    if (typeof requestFrame !== 'function') throw new Error('requestAnimationFrame is required');
    this._running = true;
    let last;
    const frame = now => {
      if (!this._running) return;
      if (last !== undefined) this.step(Math.min((now - last) / 1000, 0.25));
      last = now;
      render?.(this.scene);
      this._frame = requestFrame(frame);
    };
    this._cancelFrame = frameId => cancelFrame?.call(globalThis, frameId);
    this._frame = requestFrame(frame);
  }
  stop() {
    this._running = false;
    if (this._frame !== undefined) this._cancelFrame?.(this._frame);
    this._frame = undefined;
  }
}

function instantiate(source, parent, position = null, rotation = null) {
  const original = source instanceof GameObject ? source : source?.gameObject;
  if (!original) throw new TypeError('Object.Instantiate expects a GameObject or Component');
  const map = new Map();
  const visualMap = new Map();
  const cloneTree = object => {
    const clone = new GameObject(object.name, object.transform.constructor);
    map.set(object, clone);
    map.set(object.transform, clone.transform);
    clone.tag = object.tag;
    clone.layer = object.layer;
    clone.previewModel = object.previewModel;
    clone.SetActive(object.activeSelf);
    clone.object3D.position.copy(object.object3D.position);
    clone.object3D.quaternion.copy(object.object3D.quaternion);
    clone.object3D.scale.copy(object.object3D.scale);
    for (const child3D of object.object3D.children) {
      if (!child3D.userData.gameObject && !child3D.userData._unityBridgeOwned) {
        const visual = cloneSkeleton(child3D);
        clone.object3D.add(visual);
        const originals = [], copies = [];
        child3D.traverse(node => originals.push(node));
        visual.traverse(node => copies.push(node));
        originals.forEach((node, index) => visualMap.set(node, copies[index]));
      }
    }
    for (const component of object._components) {
      if (component === object.transform) continue;
      const copy = clone.AddComponent(component.constructor);
      map.set(component, copy);
    }
    for (const child of object._children) cloneTree(child).SetParent(clone, false);
    return clone;
  };
  const root = cloneTree(original);
  const copyValue = value => {
    if (map.has(value)) return map.get(value);
    if (value instanceof List) return new List(value.map(copyValue));
    if (Array.isArray(value)) return value.map(copyValue);
    if (value instanceof THREE.Vector2 || value instanceof THREE.Vector3 || value instanceof THREE.Quaternion) return value.clone();
    if (value && value.constructor === globalThis.Object) {
      return globalThis.Object.fromEntries(globalThis.Object.entries(value).map(([key, item]) => [key, copyValue(item)]));
    }
    return value;
  };
  for (const [old, copy] of map) {
    if (!(old instanceof Component) || old instanceof Transform) continue;
    for (const [key, value] of globalThis.Object.entries(old)) {
      if ((!key.startsWith('_') || old.__unitySerializedFields?.includes(key)) && key !== 'gameObject'
        && !['threeCamera', 'threeMesh', 'threeLight'].includes(key)) copy[key] = copyValue(value);
    }
    if (old.__unitySerializedFields) globalThis.Object.defineProperty(copy, '__unitySerializedFields', {
      value: old.__unitySerializedFields, configurable: true
    });
    if (old instanceof Renderer) {
      copy.threeMesh = visualMap.get(old.threeMesh) ?? null;
      copy._renderRoot = visualMap.get(old._renderRoot) ?? null;
    }
    copy.enabled = old.enabled;
  }
  if (parent) root.SetParent(parent instanceof Transform ? parent.gameObject : parent, false);
  if (position) root.transform.position = position;
  if (rotation) root.transform.rotation = rotation;
  if (original._runtime || original._prefabAsset)
    (original._runtime ?? activeRuntime)?.add(root);
  return source instanceof GameObject ? root : map.get(source);
}

const PreviewInvalidScene = globalThis.Object.freeze({ isLoaded: false, IsValid: () => false });

export class Scene {
  constructor(runtime) { this._runtime = runtime; }
  get name() { return this._runtime.scene.name || 'Scene'; }
  get buildIndex() { return 0; }
  get isLoaded() { return true; }
  IsValid() { return true; }
  GetRootGameObjects() { return [...this._runtime._objects].filter(object => !object.parent); }
}

export class SceneManager {
  static _scenes = new Map();
  static GetActiveScene() { return activeRuntime ? new Scene(activeRuntime) : null; }
  /** Register a transpiled scene factory before loading it by name. */
  static RegisterScene(name, factory) { SceneManager._scenes.set(name, factory); }
  static LoadScene(name) {
    const factory = SceneManager._scenes.get(name);
    if (!factory) {
      if (name === 0 && typeof globalThis.location?.reload === 'function') {
        globalThis.location.reload();
        return null;
      }
      throw new Error(`Scene is not registered: ${name}`);
    }
    if (!activeRuntime) throw new Error('No active UnityRuntime');
    const runtime = activeRuntime;
    for (const root of new Scene(runtime).GetRootGameObjects())
      if (!root._persistentAcrossScenes) root.Destroy();
    runtime.scene.name = name;
    factory(runtime);
    return new Scene(runtime);
  }
  static LoadSceneAsync(name) {
    const scene = this.LoadScene(name);
    return { isDone: true, progress: 1, allowSceneActivation: true, scene };
  }
}

export class Ray {
  constructor(origin = Vector3.zero, direction = Vector3.forward) {
    this.origin = origin.clone();
    this.direction = direction.clone().normalize();
  }
  GetPoint(distance) { return this.origin.clone().addScaledVector(this.direction, distance); }
}

export class Camera extends Behaviour {
  static get main() {
    return [...(activeRuntime?._objects ?? [])]
      .flatMap(object => object.GetComponents(Camera))
      .find(camera => camera._active && camera.gameObject.CompareTag('MainCamera'))
      ?? [...(activeRuntime?._objects ?? [])]
        .flatMap(object => object.GetComponents(Camera))
        .find(camera => camera._active)
      ?? null;
  }
  constructor() {
    super();
    this._fieldOfView = 60;
    this._nearClipPlane = 0.1;
    this._farClipPlane = 1000;
    this.aspect = 1;
    this.pixelWidth = 1;
    this.pixelHeight = 1;
    this.useUnityRenderSpace = false;
    this.threeCamera = new THREE.PerspectiveCamera();
    this.renderCamera = new THREE.PerspectiveCamera();
    // Three cameras look down -Z. A Unity Transform's forward is +Z.
    this.threeCamera.rotation.y = Math.PI;
  }
  get fieldOfView() { return this._fieldOfView; }
  set fieldOfView(value) { this._fieldOfView = value; this._syncProjection(); }
  get nearClipPlane() { return this._nearClipPlane; }
  set nearClipPlane(value) { this._nearClipPlane = value; this._syncProjection(); }
  get farClipPlane() { return this._farClipPlane; }
  set farClipPlane(value) { this._farClipPlane = value; this._syncProjection(); }
  Awake() {
    this.threeCamera.userData._unityBridgeOwned = true;
    this.gameObject.object3D.add(this.threeCamera);
    this._syncProjection();
  }
  OnDestroy() { this.threeCamera.removeFromParent(); }
  _syncProjection() {
    this.threeCamera.fov = this.fieldOfView;
    this.threeCamera.near = this.nearClipPlane;
    this.threeCamera.far = this.farClipPlane;
    this.threeCamera.aspect = this.aspect;
    this.threeCamera.updateProjectionMatrix();
    this.renderCamera.fov = this.fieldOfView;
    this.renderCamera.near = this.nearClipPlane;
    this.renderCamera.far = this.farClipPlane;
    this.renderCamera.aspect = this.aspect;
    this.renderCamera.updateProjectionMatrix();
  }
  _syncRenderPose() {
    const position = this.transform.position;
    const rotation = this.transform.rotation;
    // Reflection across Z converts Unity's left-handed world to Three's
    // right-handed render world. Conjugating rotation keeps it a proper pose.
    this.renderCamera.position.set(position.x, position.y, -position.z);
    this.renderCamera.quaternion.set(-rotation.x, -rotation.y, rotation.z, rotation.w);
    this.renderCamera.updateMatrixWorld(true);
  }
  Render(renderer, scene) {
    if (!this.useUnityRenderSpace) {
      renderer.render(scene, this.threeCamera);
      return;
    }
    this._syncProjection();
    this._syncRenderPose();
    scene.scale.z *= -1;
    try { renderer.render(scene, this.renderCamera); }
    finally {
      scene.scale.z *= -1;
      scene.updateMatrixWorld(true);
    }
  }
  SetViewport(width, height) {
    this.pixelWidth = width;
    this.pixelHeight = height;
    this.aspect = width / height;
    this._syncProjection();
  }
  ScreenPointToRay(point) {
    this._syncProjection();
    if (this.useUnityRenderSpace) {
      this._syncRenderPose();
      const renderOrigin = this.renderCamera.position.clone();
      const projected = new Vector3(
        point.x / this.pixelWidth * 2 - 1,
        point.y / this.pixelHeight * 2 - 1, 0.5,
      ).unproject(this.renderCamera);
      const origin = new Vector3(renderOrigin.x, renderOrigin.y, -renderOrigin.z);
      const direction = projected.sub(renderOrigin);
      direction.z *= -1;
      return new Ray(origin, direction);
    }
    this.threeCamera.updateWorldMatrix(true, false);
    const origin = this.threeCamera.getWorldPosition(new Vector3());
    const projected = new Vector3(
      point.x / this.pixelWidth * 2 - 1,
      point.y / this.pixelHeight * 2 - 1,
      0.5,
    ).unproject(this.threeCamera);
    return new Ray(origin, projected.sub(origin));
  }
  WorldToScreenPoint(point) {
    this._syncProjection();
    if (this.useUnityRenderSpace) {
      this._syncRenderPose();
      const renderPoint = new Vector3(point.x, point.y, -point.z);
      const ndc = renderPoint.project(this.renderCamera);
      return new Vector3((ndc.x + 1) * this.pixelWidth / 2,
        (ndc.y + 1) * this.pixelHeight / 2, ndc.z);
    }
    this.threeCamera.updateWorldMatrix(true, false);
    const ndc = point.clone().project(this.threeCamera);
    return new Vector3((ndc.x + 1) * this.pixelWidth / 2,
      (ndc.y + 1) * this.pixelHeight / 2, ndc.z);
  }
}

export class MeshFilter extends Component {
  constructor() { super(); this.sharedMesh = null; }
  get mesh() { return this.sharedMesh; }
  set mesh(value) { this.sharedMesh = value; }
}

export class Material extends THREE.MeshStandardMaterial {
  constructor(source = null) {
    super();
    if (source instanceof THREE.Material) this.copy(source);
    if (source?.mainTexture) this.mainTexture = source.mainTexture;
  }
  get mainTexture() { return this.map; }
  set mainTexture(value) { this.map = value ?? null; this.needsUpdate = true; }
  SetFloat(name, value) { this[name] = Number(value); }
  SetColor(name, value) {
    if (name === '_Color') this.color.setRGB(value.r, value.g, value.b);
    else this[name] = value;
  }
}
export class Renderer extends Behaviour {
  constructor() {
    super();
    this._sharedMaterial = new Material();
    this.castShadows = false;
    this.receiveShadows = false;
    this.threeMesh = null;
    this._renderRoot = null;
  }
  OnEnable() { const visual = this._renderRoot ?? this.threeMesh; if (visual) visual.visible = true; }
  OnDisable() { const visual = this._renderRoot ?? this.threeMesh; if (visual) visual.visible = false; }
  OnDestroy() { (this._renderRoot ?? this.threeMesh)?.removeFromParent(); }
  get sharedMaterial() { return this._sharedMaterial; }
  set sharedMaterial(value) {
    this._sharedMaterial = value;
    if (this.threeMesh) this.threeMesh.material = value;
  }
  get material() { return this.sharedMaterial; }
  set material(value) { this.sharedMaterial = value; }
}

export class MeshRenderer extends Renderer {
  Awake() {
    if (this.threeMesh) return;
    const geometry = this.GetComponent(MeshFilter)?.sharedMesh;
    if (!geometry) return;
    this.threeMesh = new THREE.Mesh(geometry, this.sharedMaterial);
    this.threeMesh.userData._unityBridgeOwned = true;
    this.gameObject.object3D.add(this.threeMesh);
  }
}

export class SkinnedMeshRenderer extends Renderer {
  constructor() {
    super();
    this.sharedMesh = null;
    this.bones = [];
    this.rootBone = null;
    this.useExternalAnimation = false;
  }
  Awake() {
    // Unity permits an unassigned renderer; asset setup can attach its mesh later.
  }
  LateUpdate() {
    if (this.useExternalAnimation) return;
    if (this._previewBoneBindings) {
      const local = new THREE.Matrix4();
      const inverseParent = new THREE.Matrix4();
      for (const { bone, driver, bindOffset } of this._previewBoneBindings) {
        driver.updateWorldMatrix(true, false);
        bone.parent.updateWorldMatrix(true, false);
        inverseParent.copy(bone.parent.matrixWorld).invert();
        local.multiplyMatrices(inverseParent, driver.matrixWorld).multiply(bindOffset);
        bone.matrixAutoUpdate = false;
        bone.matrix.copy(local);
        bone.updateWorldMatrix(false, false);
      }
      return;
    }
    const visualBones = this.threeMesh?.skeleton?.bones ?? [];
    const action = this.GetComponent(Animator)?._action;
    const tracks = action?.isRunning() ? action.getClip().tracks : [];
    const localMatrix = new THREE.Matrix4();
    const parentInverse = new THREE.Matrix4();
    for (let index = 0; index < Math.min(this.bones.length, visualBones.length); index++) {
      const transform = this.bones[index];
      const visual = visualBones[index];
      if (!transform || !visual) continue;
      const animated = tracks.some(track => track.name.startsWith(`${visual.name}.`)
        || track.name.includes(`bones[${visual.name}]`));
      if (animated) {
        transform.localPosition.copy(visual.position);
        transform.localRotation.copy(visual.quaternion);
        transform.localScale.copy(visual.scale);
      } else {
        // Exported skins omit intermediate Unity transforms that are not joints.
        // Their local transforms therefore cannot be copied straight to a GLB bone.
        transform.object3D.updateWorldMatrix(true, false);
        visual.parent.updateWorldMatrix(true, false);
        parentInverse.copy(visual.parent.matrixWorld).invert();
        localMatrix.multiplyMatrices(parentInverse, transform.object3D.matrixWorld);
        localMatrix.decompose(visual.position, visual.quaternion, visual.scale);
      }
    }
  }
}

/** Keeps SpriteRenderer references and visibility while sprite texture conversion is pending. */
export class SpriteRenderer extends Renderer {
  constructor() { super(); this.color = Color.white; this.sprite = null; this.sortingOrder = 0; }
}

export class Light extends Behaviour {
  constructor() {
    super();
    this.type = 'Directional';
    this.color = 0xffffff;
    this.intensity = 1;
    this.range = 10;
    this.threeLight = null;
  }
  Awake() {
    this.threeLight = this.type === 'Point'
      ? new THREE.PointLight(this.color, this.intensity, this.range)
      : new THREE.DirectionalLight(this.color, this.intensity);
    this.threeLight.userData._unityBridgeOwned = true;
    this.gameObject.object3D.add(this.threeLight);
    if (this.type !== 'Point') {
      this.threeLight.target.userData._unityBridgeOwned = true;
      this.threeLight.target.position.set(0, 0, 1);
      this.gameObject.object3D.add(this.threeLight.target);
    }
  }
  OnEnable() { if (this.threeLight) this.threeLight.visible = true; }
  OnDisable() { if (this.threeLight) this.threeLight.visible = false; }
  OnDestroy() {
    this.threeLight?.target?.removeFromParent();
    this.threeLight?.removeFromParent();
  }
}

export class Animator extends Behaviour {
  constructor() {
    super();
    this.clips = new Map();
    this.parameters = new Map();
    this.applyRootMotion = false;
    this.updateMode = 0;
    this._animatorUpdateDriver = this;
    this.deltaPosition = new Vector3();
    this.deltaRotation = new Quaternion();
    this.humanBones = {};
    this.controllerLayers = [];
    this.controllerGraph = null;
    this._controllerRuntime = null;
    this._runtimeAnimatorController = null;
    this.controllerAssetId = null;
    this._stateByLayer = new Map();
    this._layerWeights = new Map();
    this._ik = new Map();
    this._mixer = null;
    this._action = null;
    this._importedBoneBindings = [];
    this._importedBonePosition = new Vector3();
    this._importedBoneRotation = new Quaternion();
  }
  get isHuman() { return globalThis.Object.keys(this.humanBones ?? {}).length > 0; }
  get bodyPosition() { return this._bodyPositionOverride?.clone()
    ?? (this.GetBoneTransform(0) ?? this.transform).position; }
  set bodyPosition(value) { this._bodyPositionOverride = value.clone(); }
  get runtimeAnimatorController() { return this._runtimeAnimatorController; }
  set runtimeAnimatorController(value) {
    this._runtimeAnimatorController = value;
    this.controllerGraph = value?.graph ?? value ?? null;
    this.controllerAssetId = value?.__previewAssetId ?? null;
  }
  get avatar() { return this._avatar ?? (this.isHuman ? new Avatar(this.gameObject?.name ?? '') : null); }
  set avatar(value) { this._avatar = value; }
  Awake() { this._mixer = new THREE.AnimationMixer(this.gameObject.object3D); }
  Start() { if (this.defaultClip) this.Play(this.defaultClip); }
  SetControllerGraph(graph, behaviourTypes = {}, options = {}) {
    this.controllerGraph = graph;
    this._behaviourTypes = behaviourTypes;
    this.controllerLayers = graph.layers.map(layer => ({ name: layer.name,
      states: layer.states.map(state => state.name) }));
    for (const parameter of graph.parameters ?? []) {
      if (this.parameters.has(parameter.name)) continue;
      const value = parameter.type === 'Float' ? parameter.defaultFloat
        : parameter.type === 'Int' ? parameter.defaultInt : parameter.defaultBool;
      this.parameters.set(parameter.name, value ?? false);
    }
    this._controllerRuntime = new AnimatorControllerRuntime(graph, this, behaviourTypes,
      { allowMissingClips: true, ...options });
  }
  Update() { this._graphOutputs = this._controllerRuntime?.update(Time.deltaTime) ?? null; }
  AddClip(clip) { this.clips.set(clip.name, clip); }
  Play(name, layer = 0, normalizedTime = 0) {
    const clip = this.clips.get(name);
    // Unity ignores Play requests for states absent from the controller. Asset
    // exports can omit optional layers, so a missing state must not stop Update.
    if (!clip && !this.controllerLayers[layer]?.states?.includes(name)) return;
    this._stateByLayer.set(layer, name);
    this._controllerRuntime?.play(name, layer, normalizedTime);
    this._visualControllerRuntime?.play(name, layer, normalizedTime);
    if (!clip) return;
    this._action?.stop();
    this._action = this._mixer.clipAction(clip);
    this._action.reset().play();
  }
  CrossFade(name, duration) {
    const clip = this.clips.get(name);
    if (!clip) throw new Error(`Unknown animation clip: ${name}`);
    const next = this._mixer.clipAction(clip).reset().play();
    if (this._action) next.crossFadeFrom(this._action, duration, false);
    this._action = next;
  }
  SetFloat(name, value) { this.parameters.set(name, Number(value)); }
  GetFloat(name) { return Number(this.parameters.get(name) ?? 0); }
  SetInteger(name, value) { this.parameters.set(name, Math.trunc(value)); }
  GetInteger(name) { return Math.trunc(this.parameters.get(name) ?? 0); }
  SetBool(name, value) { this.parameters.set(name, Boolean(value)); }
  GetBool(name) { return Boolean(this.parameters.get(name)); }
  SetTrigger(name) { this.parameters.set(name, true); }
  ResetTrigger(name) { this.parameters.set(name, false); }
  GetBoneTransform(bone) { return this.humanBones?.[bone] ?? null; }
  /** Connect imported skin bones to the exported Unity Transform hierarchy. */
  BindImportedSkeleton(skeleton, { excludeNames = [], preserveOffset = false } = {}) {
    if (!Array.isArray(skeleton?.bones)) throw new TypeError('Expected a Three.js Skeleton');
    const transforms = new Map();
    const visit = object => {
      if (!transforms.has(object.name)) transforms.set(object.name, object.transform);
      for (const child of object.children) visit(child);
    };
    visit(this.gameObject);
    const excluded = new Set(excludeNames);
    this._importedBoneBindings = skeleton.bones
      .filter(bone => !excluded.has(bone.name) && transforms.has(bone.name))
      .map(bone => {
        const transform = transforms.get(bone.name);
        bone.updateWorldMatrix(true, false);
        transform.object3D.updateWorldMatrix(true, false);
        return { bone, transform, syncedPosition: new Vector3(),
          syncedRotation: new Quaternion(),
          offsetPosition: preserveOffset
            ? bone.worldToLocal(transform.position) : new Vector3(),
          offsetRotation: preserveOffset
            ? bone.getWorldQuaternion(new Quaternion()).invert().multiply(transform.rotation)
            : new Quaternion() };
      });
    // The imported skeleton array need not follow the exported transform tree.
    // World-space copies must place parents before children, or a later parent
    // update displaces an already positioned hand (and its attached weapon).
    const depth = transform => {
      let value = 0;
      for (let parent = transform.parent; parent; parent = parent.parent) value++;
      return value;
    };
    this._importedBoneBindings.sort((a, b) => depth(a.transform) - depth(b.transform));
    return this._importedBoneBindings.length;
  }
  /** Call after an imported skeleton's pose changes. */
  SyncImportedSkeletonPose() {
    for (const binding of this._importedBoneBindings) {
      const { bone, transform, offsetPosition, offsetRotation } = binding;
      bone.updateWorldMatrix(true, false);
      transform.position = bone.localToWorld(this._importedBonePosition.copy(offsetPosition));
      transform.rotation = bone.getWorldQuaternion(this._importedBoneRotation)
        .multiply(offsetRotation);
      binding.syncedPosition.copy(transform.position);
      binding.syncedRotation.copy(transform.rotation);
    }
  }
  /** Apply procedural Unity Transform edits made after the imported animation pose. */
  ApplyImportedSkeletonOverrides() {
    for (const binding of this._importedBoneBindings) {
      const { bone, transform, offsetPosition, offsetRotation,
        syncedPosition, syncedRotation } = binding;
      const position = transform.position;
      const rotation = transform.rotation;
      if (position.distanceToSquared(syncedPosition) < 1e-10
        && Math.abs(rotation.dot(syncedRotation)) > 1 - 1e-8) continue;
      const boneRotation = rotation.multiply(offsetRotation.clone().invert());
      const bonePosition = position.sub(offsetPosition.clone().applyQuaternion(boneRotation));
      bone.parent.updateWorldMatrix(true, false);
      bone.position.copy(bone.parent.worldToLocal(bonePosition));
      bone.quaternion.copy(bone.parent.getWorldQuaternion(new Quaternion()).invert()
        .multiply(boneRotation));
      bone.updateWorldMatrix(false, false);
    }
  }
  GetCurrentAnimatorStateInfo(layer = 0) {
    const state = this._stateByLayer.get(layer) ?? '';
    return { IsName: name => state === name, normalizedTime: 0, length: 0 };
  }
  SetLayerWeight(layer, weight) { this._layerWeights.set(layer, weight); }
  GetLayerWeight(layer) { return this._layerWeights.get(layer)
    ?? this.controllerGraph?.layers?.[layer]?.defaultWeight ?? (layer === 0 ? 1 : 0); }
  SetBoneLocalRotation(bone, rotation) {
    const transform = this.GetBoneTransform(bone);
    if (transform) transform.localRotation = rotation;
  }
  _setIK(kind, goal, value) {
    let state = this._ik.get(goal);
    if (!state) this._ik.set(goal, state = {});
    state[kind] = value;
  }
  SetIKPosition(goal, value) { this._setIK('position', goal, value); }
  SetIKRotation(goal, value) { this._setIK('rotation', goal, value); }
  GetIKRotation(goal) { return this._ik.get(goal)?.rotation ?? Quaternion.identity; }
  SetIKPositionWeight(goal, value) { this._setIK('positionWeight', goal, value); }
  SetIKRotationWeight(goal, value) { this._setIK('rotationWeight', goal, value); }
  SetIKHintPosition(goal, value) { this._setIK('hintPosition', goal, value); }
  SetIKHintPositionWeight(goal, value) { this._setIK('hintWeight', goal, value); }
  SetLookAtWeight(weight, bodyWeight = 0, headWeight = 1, eyesWeight = 0, clampWeight = 0.5) {
    this._lookAtWeight = { weight, bodyWeight, headWeight, eyesWeight, clampWeight };
  }
  SetLookAtPosition(position) { this._lookAtPosition = position.clone(); }
  LateUpdate() { this._mixer.update(Time.deltaTime); }
  OnDestroy() { this._mixer?.stopAllAction(); }
}

export const ForceMode = globalThis.Object.freeze({ Force: 0, Impulse: 1, VelocityChange: 2, Acceleration: 5 });
export const CollisionDetectionMode = globalThis.Object.freeze({ Discrete: 0, Continuous: 1,
  ContinuousDynamic: 2, ContinuousSpeculative: 3 });
export const RigidbodyConstraints = globalThis.Object.freeze({
  None: 0, FreezePositionX: 2, FreezePositionY: 4, FreezePositionZ: 8,
  FreezeRotationX: 16, FreezeRotationY: 32, FreezeRotationZ: 64,
  FreezePosition: 14, FreezeRotation: 112, FreezeAll: 126,
});

export class Rigidbody extends Component {
  constructor() {
    super();
    this.mass = 1;
    this.useGravity = true;
    this.isKinematic = false;
    this.drag = 0;
    this._angularDamping = 0.05;
    this._constraints = RigidbodyConstraints.None;
    this._collisionDetectionMode = CollisionDetectionMode.Discrete;
    this._body = null;
    this._pendingVelocity = Vector3.zero;
  }
  get velocity() {
    const value = this._body?.linvel() ?? this._pendingVelocity;
    return new Vector3(value.x, value.y, value.z);
  }
  set velocity(value) {
    this._pendingVelocity.copy(value);
    this._body?.setLinvel(value, true);
  }
  get linearVelocity() { return this.velocity; }
  set linearVelocity(value) { this.velocity = value; }
  get angularDamping() { return this._angularDamping; }
  set angularDamping(value) {
    this._angularDamping = Number(value);
    this._body?.setAngularDamping(this._angularDamping);
  }
  get position() { return this.transform.position; }
  set position(value) {
    this.transform.position = value;
    this._body?.setTranslation(value, true);
    const record = Physics._system?.records.get(this.gameObject);
    record?.previousPosition.copy(value);
    record?.currentPosition.copy(value);
  }
  get rotation() { return this.transform.rotation; }
  set rotation(value) {
    this.transform.rotation = value;
    this._body?.setRotation(value, true);
    const record = Physics._system?.records.get(this.gameObject);
    record?.previousRotation.copy(value);
    record?.currentRotation.copy(value);
  }
  get constraints() { return this._constraints; }
  set constraints(value) {
    this._constraints = Number(value);
    this._applyConstraints();
  }
  get collisionDetectionMode() { return this._collisionDetectionMode; }
  set collisionDetectionMode(value) {
    this._collisionDetectionMode = Number(value);
    this._body?.enableCcd(this._collisionDetectionMode !== CollisionDetectionMode.Discrete);
  }
  _applyConstraints() {
    if (!this._body) return;
    const bits = this._constraints;
    this._body.setEnabledTranslations(!(bits & 2), !(bits & 4), !(bits & 8), true);
    this._body.setEnabledRotations(!(bits & 16), !(bits & 32), !(bits & 64), true);
  }
  AddForce(force, mode = ForceMode.Force) {
    if (!this._body) Physics._system?._ensure(this.gameObject);
    if (!this._body) throw new Error('Rigidbody needs an attached PhysicsSystem');
    if (mode === ForceMode.Force || mode === 'Force') this._body.addForce(force, true);
    else if (mode === ForceMode.Impulse || mode === 'Impulse') this._body.applyImpulse(force, true);
    else if (mode === ForceMode.Acceleration || mode === 'Acceleration')
      this._body.addForce(force.clone().multiplyScalar(this.mass), true);
    else if (mode === ForceMode.VelocityChange || mode === 'VelocityChange')
      this._body.applyImpulse(force.clone().multiplyScalar(this.mass), true);
    else throw new UnsupportedUnityAPIError(`ForceMode.${mode}`);
  }
  AddRelativeForce(force, mode = ForceMode.Force) {
    this.AddForce(force.clone().applyQuaternion(this.transform.rotation), mode);
  }
  MovePosition(position) {
    if (!this.isKinematic) throw new Error('MovePosition requires a kinematic Rigidbody');
    this._body?.setNextKinematicTranslation(position);
    this.transform.position = position;
  }
  OnDestroy() { this._body = null; }
}

export class Collider extends Component {
  constructor() {
    super();
    this.center = Vector3.zero;
    this.isTrigger = false;
    this.material = null;
    this._collider = null;
  }
  get attachedRigidbody() { return this.GetComponent(Rigidbody); }
  get bounds() {
    const diameter = this.radius * 2;
    const size = this instanceof BoxCollider ? this.size
      : this instanceof SphereCollider ? new Vector3(diameter, diameter, diameter)
        : new Vector3(diameter, this.height, diameter);
    this.gameObject.object3D.updateWorldMatrix(true, false);
    const box = new THREE.Box3().setFromCenterAndSize(this.center, size)
      .applyMatrix4(this.gameObject.object3D.matrixWorld);
    return new Bounds(box.getCenter(new Vector3()), box.getSize(new Vector3()));
  }
  ClosestPoint(point) {
    this.gameObject.object3D.updateWorldMatrix(true, false);
    const matrix = this.gameObject.object3D.matrixWorld;
    const local = point.clone().applyMatrix4(matrix.clone().invert());
    const center = this.center;
    if (this instanceof BoxCollider) {
      const half = this.size.clone().multiplyScalar(0.5);
      local.clamp(center.clone().sub(half), center.clone().add(half));
    } else {
      const spine = center.clone();
      if (this instanceof CapsuleCollider) {
        const halfLine = Math.max(0, this.height * 0.5 - this.radius);
        spine.y = Math.max(center.y - halfLine, Math.min(center.y + halfLine, local.y));
      }
      const offset = local.clone().sub(spine);
      if (offset.lengthSq() > this.radius * this.radius)
        local.copy(spine).add(offset.setLength(this.radius));
    }
    return local.applyMatrix4(matrix);
  }
  OnDestroy() { this._collider = null; }
}

export class Bounds {
  constructor(center = Vector3.zero, size = Vector3.zero) {
    this.center = center;
    this.size = size;
  }
  get extents() { return this.size.clone().multiplyScalar(0.5); }
  set extents(value) { this.size = value.clone().multiplyScalar(2); }
  get min() { return this.center.clone().sub(this.extents); }
  set min(value) { this.SetMinMax(value, this.max); }
  get max() { return this.center.clone().add(this.extents); }
  set max(value) { this.SetMinMax(this.min, value); }
  ClosestPoint(point) { return new Vector3().copy(new THREE.Box3(this.min, this.max).clampPoint(point, new THREE.Vector3())); }
  Contains(point) { return new THREE.Box3(this.min, this.max).containsPoint(point); }
  Encapsulate(value) {
    const box = new THREE.Box3(this.min, this.max);
    if (value instanceof Bounds) box.union(new THREE.Box3(value.min, value.max));
    else box.expandByPoint(value);
    this.SetMinMax(box.min, box.max);
  }
  Expand(amount) {
    if (typeof amount === 'number') this.size.addScalar(amount);
    else this.size.add(amount);
  }
  Intersects(other) { return new THREE.Box3(this.min, this.max).intersectsBox(new THREE.Box3(other.min, other.max)); }
  SetMinMax(min, max) {
    this.center = min.clone().add(max).multiplyScalar(0.5);
    this.size = max.clone().sub(min);
  }
  SqrDistance(point) { return this.ClosestPoint(point).distanceToSquared(point); }
  IntersectRay(ray, enter = null) {
    const point = new THREE.Ray(ray.origin, ray.direction).intersectBox(
      new THREE.Box3(this.min, this.max), new THREE.Vector3());
    if (enter && typeof enter === 'object') enter.value = point ? point.distanceTo(ray.origin) : 0;
    return point !== null;
  }
}

export class BoxCollider extends Collider {
  constructor() { super(); this.size = Vector3.one; }
}
export class MeshCollider extends Collider {
  constructor() { super(); this.meshRoot = null; }
  get bounds() {
    const box = new THREE.Box3().setFromObject(this.meshRoot ?? this.gameObject.object3D);
    return new Bounds(box.getCenter(new Vector3()), box.getSize(new Vector3()));
  }
  ClosestPoint(point) {
    const box = new THREE.Box3().setFromObject(this.meshRoot ?? this.gameObject.object3D);
    return box.clampPoint(point, new Vector3());
  }
}
export class SphereCollider extends Collider {
  constructor() { super(); this.radius = 0.5; }
}
export class CapsuleCollider extends Collider {
  constructor() { super(); this.radius = 0.5; this.height = 2; }
}

export class WheelHit {
  constructor(hit = null) {
    this.collider = hit?.collider ?? null;
    this.point = hit?.point ?? Vector3.zero;
    this.normal = hit?.normal ?? Vector3.up;
    this.forwardSlip = 0;
    this.sidewaysSlip = 0;
    this.force = 0;
  }
}

/** Raycast wheel approximation for JU vehicles; torque is applied to the parent Rigidbody. */
export class WheelCollider extends Component {
  constructor() {
    super();
    this.center = Vector3.zero;
    this.radius = 0.5;
    this.suspensionDistance = 0.3;
    this.mass = 20;
    this.wheelDampingRate = 0.25;
    this.forceAppPointDistance = 0;
    this.motorTorque = 0;
    this.brakeTorque = 0;
    this.steerAngle = 0;
    this.rpm = 0;
    this._spin = 0;
  }
  get attachedRigidbody() { return this.GetComponentInParent(Rigidbody); }
  _contact() {
    const origin = this.transform.position.add(this.center.clone().applyQuaternion(this.transform.rotation));
    const up = this.transform.up;
    const body = this.attachedRigidbody;
    const reach = this.radius + this.suspensionDistance;
    // Cast from above the wheel so a chassis that briefly sinks below the
    // surface can still recover. Unity's WheelCollider carries the chassis.
    return Physics.RaycastAll(origin.clone().addScaledVector(up, reach),
      up.clone().negate(), reach * 2)
      .find(hit => hit.collider && !hit.collider.isTrigger
        && hit.normal.dot(up) > 0.3
        && hit.collider.GetComponentInParent(Rigidbody) !== body) ?? null;
  }
  GetGroundHit(output) {
    const hit = this._contact();
    if (output && typeof output === 'object') output.value = new WheelHit(hit);
    return Boolean(hit);
  }
  GetWorldPose(position, rotation) {
    const contact = this._contact();
    const up = this.transform.up;
    const center = this.transform.position.add(this.center.clone().applyQuaternion(this.transform.rotation));
    const wheelPosition = contact ? contact.point.clone().addScaledVector(up, this.radius)
      : center.addScaledVector(up, -this.suspensionDistance);
    const wheelRotation = this.transform.rotation
      .multiply(Quaternion.AngleAxis(this.steerAngle, Vector3.up))
      .multiply(Quaternion.AngleAxis(this._spin, Vector3.right));
    if (position && typeof position === 'object') position.value = wheelPosition;
    if (rotation && typeof rotation === 'object') rotation.value = wheelRotation;
  }
  _step(deltaTime) {
    const body = this.attachedRigidbody;
    if (!body?._body) return;
    const contact = this._contact();
    if (!contact) return;
    // Support the chassis against the surface normal. The lightweight wheel
    // model has no contact patch torque, so using a leaned wheel's local up
    // here otherwise accelerates a motorcycle sideways on level ground.
    const up = contact.normal.clone().normalize();
    const wheelCenter = this.transform.position.add(this.center.clone().applyQuaternion(this.transform.rotation));
    const wheelHeight = wheelCenter.clone().sub(contact.point).dot(up);
    const desiredHeight = this.radius + this.suspensionDistance * 0.5;
    const compression = desiredHeight - wheelHeight;
    const wheels = body.gameObject.GetComponentsInChildren(WheelCollider, true).length || 1;
    const massPerWheel = body.mass / wheels;
    const gravity = Physics.gravity.length();
    const verticalSpeed = body.linearVelocity.dot(up);
    const springFrequency = 18;
    const springForce = massPerWheel * Math.max(0, Math.min(gravity * 25,
      gravity + springFrequency * springFrequency * compression
        - 2 * 0.9 * springFrequency * verticalSpeed));
    body.AddForce(up.multiplyScalar(springForce));
    const forward = this.transform.forward.applyQuaternion(Quaternion.AngleAxis(this.steerAngle, up))
      .projectOnPlane(up).normalize();
    const sideways = up.clone().cross(forward).normalize();
    const sidewaysSpeed = body.linearVelocity.dot(sideways);
    const grip = Math.min(8, 1 / Math.max(deltaTime, 1e-4));
    body.AddForce(sideways.multiplyScalar(-sidewaysSpeed * massPerWheel * grip));
    const speed = body.linearVelocity.dot(forward);
    const brakeForce = Math.min(Math.abs(speed) * body.mass / Math.max(deltaTime, 1e-4),
      Math.abs(this.brakeTorque) / Math.max(this.radius, 0.05));
    body.AddForce(forward.multiplyScalar(this.motorTorque / Math.max(this.radius, 0.05)
      - Math.sign(speed) * brakeForce));
    this.rpm = speed / Math.max(2 * Math.PI * this.radius, 0.01) * 60;
    this._spin = (this._spin + this.rpm * 6 * deltaTime) % 360;
    if (Math.abs(this.steerAngle) > 0.01 && Math.abs(speed) > 0.1) {
      const yaw = this.steerAngle * Math.PI / 180 * speed * deltaTime * 0.15;
      body.rotation = Quaternion.AngleAxis(yaw * 180 / Math.PI, Vector3.up).multiply(body.rotation);
    }
  }
}

export const CollisionFlags = globalThis.Object.freeze({ None: 0, Sides: 1, Above: 2, Below: 4 });

/** Movement-and-slide facade; requires a Collider and the Rapier backend. */
export class CharacterController extends Component {
  constructor() {
    super();
    this.skinWidth = 0.02;
    this.stepOffset = 0.3;
    this.slopeLimit = 45;
    this.isGrounded = false;
    this._controller = null;
  }
  Move(motion) {
    const system = Physics._system;
    if (!system || this.gameObject._runtime !== system.runtime) throw new Error('CharacterController needs PhysicsSystem');
    system._ensure(this.gameObject);
    const collider = this.GetComponent(Collider)?._collider;
    if (!collider) throw new Error('CharacterController needs a Collider');
    if (!this._controller) {
      this._controller = system.world.createCharacterController(this.skinWidth);
    }
    if (this.stepOffset > 0) this._controller.enableAutostep(this.stepOffset, this.skinWidth, true);
    else this._controller.disableAutostep();
    this._controller.setMaxSlopeClimbAngle(this.slopeLimit * Mathf.Deg2Rad);
    this._controller.setMinSlopeSlideAngle(this.slopeLimit * Mathf.Deg2Rad);
    this._controller.computeColliderMovement(collider, motion);
    const moved = this._controller.computedMovement();
    this.isGrounded = this._controller.computedGrounded();
    const position = this.transform.position.add(new Vector3(moved.x, moved.y, moved.z));
    this.transform.position = position;
    system.records.get(this.gameObject)?.body.setTranslation(position, true);
    let flags = this.isGrounded ? CollisionFlags.Below : CollisionFlags.None;
    for (let index = 0; index < this._controller.numComputedCollisions(); index++) {
      const normal = this._controller.computedCollision(index)?.normal1;
      if (!normal) continue;
      flags |= normal.y < -0.5 ? CollisionFlags.Above
        : normal.y > 0.5 ? CollisionFlags.Below : CollisionFlags.Sides;
    }
    return flags;
  }
  OnDestroy() {
    if (this._controller && Physics._system) Physics._system.world.removeCharacterController(this._controller);
    this._controller = null;
  }
}

export class RaycastHit {
  constructor(collider = null, point = Vector3.zero, normal = Vector3.zero, distance = 0) {
    this.collider = collider;
    this.rigidbody = collider?.attachedRigidbody ?? null;
    this.transform = collider?.transform ?? null;
    this.point = point;
    this.normal = normal;
    this.distance = distance;
  }
}

export const QueryTriggerInteraction = globalThis.Object.freeze({ UseGlobal: 0, Ignore: 1, Collide: 2 });

export class Physics {
  static gravity = new Vector3(0, -9.81, 0);
  static IgnoreRaycastLayer = 1 << 2;
  static DefaultRaycastLayers = ~Physics.IgnoreRaycastLayer;
  static AllLayers = ~0;
  static queriesHitTriggers = true;
  static _system = null;
  static _ignored = new WeakMap();
  static IgnoreCollision(first, second, ignore = true) {
    if (!(first instanceof Collider) || !(second instanceof Collider))
      throw new TypeError('Physics.IgnoreCollision needs two Colliders');
    for (const [left, right] of [[first, second], [second, first]]) {
      let partners = this._ignored.get(left);
      if (!partners) this._ignored.set(left, partners = new WeakSet());
      if (ignore) partners.add(right);
      else partners.delete(right);
    }
  }
  static _isIgnored(first, second) { return this._ignored.get(first)?.has(second) ?? false; }
  static _rayArguments(originOrRay, args) {
    const isOut = value => value && typeof value === 'object' && 'value' in value;
    const ray = originOrRay instanceof Ray;
    const origin = ray ? originOrRay.origin : originOrRay;
    const direction = ray ? originOrRay.direction : args.shift();
    const output = isOut(args[0]) ? args.shift() : null;
    return {
      origin, direction, output,
      distance: args.length > 0 ? args[0] : Infinity,
      mask: args.length > 1 ? args[1] : Physics.DefaultRaycastLayers,
      query: args.length > 2 ? args[2] : QueryTriggerInteraction.UseGlobal,
    };
  }
  static Raycast(originOrRay, ...args) {
    const { origin, direction, output, distance, mask, query } = Physics._rayArguments(originOrRay, args);
    const hit = Physics._system?.raycast(origin, direction, distance, mask, query) ?? null;
    if (!output) return hit !== null;
    output.value = hit ?? new RaycastHit();
    return hit !== null;
  }
  static Linecast(start, end, ...args) {
    const output = args[0] && typeof args[0] === 'object' && 'value' in args[0] ? args.shift() : null;
    const layerMask = args.length > 0 ? args[0] : Physics.DefaultRaycastLayers;
    const query = args.length > 1 ? args[1] : QueryTriggerInteraction.UseGlobal;
    const displacement = end.clone().sub(start);
    const distance = displacement.length();
    const hit = distance > 0 ? Physics._system?.raycast(start, displacement, distance, layerMask, query) ?? null : null;
    if (!output) return hit !== null;
    output.value = hit ?? new RaycastHit();
    return hit !== null;
  }
  static OverlapSphere(center, radius, layerMask = -1) {
    return Physics._system?.overlap(center, new Physics._system.RAPIER.Ball(radius), Quaternion.identity, layerMask) ?? [];
  }
  static OverlapSphereNonAlloc(center, radius, results, layerMask = -1) {
    if (!Array.isArray(results)) throw new TypeError('Physics.OverlapSphereNonAlloc needs a collider array');
    const hits = this.OverlapSphere(center, radius, layerMask);
    const count = Math.min(hits.length, results.length);
    for (let index = 0; index < count; index++) results[index] = hits[index];
    return count;
  }
  static OverlapBox(center, halfExtents, orientation = Quaternion.identity, layerMask = -1) {
    const system = Physics._system;
    return system?.overlap(center,
      new system.RAPIER.Cuboid(halfExtents.x, halfExtents.y, halfExtents.z), orientation, layerMask) ?? [];
  }
  static SphereCast(origin, radius, direction, output, maxDistance = Infinity, layerMask = -1) {
    const hit = Physics._system?.sphereCast(origin, radius, direction, maxDistance, layerMask) ?? null;
    if (!output || typeof output !== 'object' || !('value' in output)) return hit;
    output.value = hit ?? new RaycastHit();
    return hit !== null;
  }
  static RaycastAll(originOrRay, ...args) {
    const { origin, direction, distance, mask, query } = Physics._rayArguments(originOrRay, args);
    return Physics._system?.raycastAll(origin, direction, distance, mask, query) ?? [];
  }
  static RaycastNonAlloc(originOrRay, ...args) {
    const direction = originOrRay instanceof Ray ? null : args.shift();
    const results = args.shift();
    if (!Array.isArray(results)) throw new TypeError('Physics.RaycastNonAlloc needs a RaycastHit array');
    const hits = originOrRay instanceof Ray
      ? Physics.RaycastAll(originOrRay, ...args)
      : Physics.RaycastAll(originOrRay, direction, ...args);
    const count = Math.min(hits.length, results.length);
    for (let index = 0; index < count; index++) results[index] = hits[index];
    return count;
  }
}

/** Rapier backend. Initialize asynchronously before advancing the runtime. */
export class PhysicsSystem {
  static async create(runtime) {
    const { default: RAPIER } = await import('@dimforge/rapier3d-compat');
    await RAPIER.init();
    return runtime.addSystem(new PhysicsSystem(RAPIER));
  }
  constructor(RAPIER) {
    this.RAPIER = RAPIER;
    this.world = new RAPIER.World(Physics.gravity);
    this.queue = new RAPIER.EventQueue(true);
    this.records = new Map();
    this.byHandle = new Map();
    this.hooks = {
      filterContactPair: (first, second) => Physics._isIgnored(this.byHandle.get(first), this.byHandle.get(second))
        ? null : RAPIER.SolverFlags.COMPUTE_IMPULSE,
      filterIntersectionPair: (first, second) => !Physics._isIgnored(this.byHandle.get(first), this.byHandle.get(second)),
    };
  }
  attach(runtime) {
    this.runtime = runtime;
    this.world.integrationParameters.dt = runtime.fixedDeltaTime;
    Physics._system = this;
  }
  detach() {
    for (const object of [...this.records.keys()]) this._remove(object);
    if (Physics._system === this) Physics._system = null;
    this.world.free();
  }
  _remove(object) {
    const record = this.records.get(object);
    if (!record) return;
    for (const collider of record.colliders.keys()) collider._collider = null;
    for (const handle of record.colliders.values()) this.byHandle.delete(handle.handle);
    object.GetComponent(Rigidbody)?._body && (object.GetComponent(Rigidbody)._body = null);
    this.world.removeRigidBody(record.body);
    this.records.delete(object);
  }
  _colliderDesc(component, scale) {
    const R = this.RAPIER;
    let desc;
    if (component instanceof BoxCollider) {
      desc = R.ColliderDesc.cuboid(
        component.size.x * Math.abs(scale.x) / 2,
        component.size.y * Math.abs(scale.y) / 2,
        component.size.z * Math.abs(scale.z) / 2,
      );
    } else if (component instanceof MeshCollider) {
      const root = component.gameObject.object3D;
      root.updateWorldMatrix(true, true);
      const inverse = root.matrixWorld.clone().invert();
      const vertices = [];
      const indices = [];
      component.meshRoot?.traverse(mesh => {
        if (!mesh.isMesh || !mesh.geometry?.attributes?.position) return;
        const positions = mesh.geometry.attributes.position;
        const matrix = inverse.clone().multiply(mesh.matrixWorld);
        const offset = vertices.length / 3;
        const point = new THREE.Vector3();
        for (let index = 0; index < positions.count; index++) {
          point.fromBufferAttribute(positions, index).applyMatrix4(matrix);
          vertices.push(point.x * scale.x, point.y * scale.y, point.z * scale.z);
        }
        const source = mesh.geometry.index;
        const count = source?.count ?? positions.count;
        const mirrored = matrix.determinant() < 0;
        for (let index = 0; index < count; index += 3) {
          const a = offset + (source ? source.getX(index) : index);
          const b = offset + (source ? source.getX(index + 1) : index + 1);
          const c = offset + (source ? source.getX(index + 2) : index + 2);
          indices.push(a, mirrored ? c : b, mirrored ? b : c);
        }
      });
      if (!indices.length) return null;
      desc = R.ColliderDesc.trimesh(new Float32Array(vertices), new Uint32Array(indices));
    } else if (component instanceof SphereCollider) {
      desc = R.ColliderDesc.ball(component.radius * Math.max(Math.abs(scale.x), Math.abs(scale.y), Math.abs(scale.z)));
    } else if (component instanceof CapsuleCollider) {
      const radius = component.radius * Math.max(Math.abs(scale.x), Math.abs(scale.z));
      desc = R.ColliderDesc.capsule(Math.max(0, component.height * Math.abs(scale.y) / 2 - radius), radius);
    } else throw new UnsupportedUnityAPIError(component.constructor.name);
    desc.setTranslation(component.center.x * scale.x, component.center.y * scale.y, component.center.z * scale.z);
    desc.setSensor(component.isTrigger);
    if (component.material instanceof PhysicsMaterial) {
      const material = component.material;
      const combine = { Average: R.CoefficientCombineRule.Average,
        Minimum: R.CoefficientCombineRule.Min, Multiply: R.CoefficientCombineRule.Multiply,
        Maximum: R.CoefficientCombineRule.Max };
      desc.setFriction(material.dynamicFriction);
      desc.setRestitution(material.bounciness);
      if (combine[material.frictionCombine] !== undefined)
        desc.setFrictionCombineRule(combine[material.frictionCombine]);
      if (combine[material.bounceCombine] !== undefined)
        desc.setRestitutionCombineRule(combine[material.bounceCombine]);
    }
    desc.setActiveEvents(R.ActiveEvents.COLLISION_EVENTS);
    desc.setActiveHooks(R.ActiveHooks.FILTER_CONTACT_PAIRS | R.ActiveHooks.FILTER_INTERSECTION_PAIRS);
    desc.setDensity(0);
    return desc;
  }
  _ensure(object) {
    const rigidbody = object.GetComponent(Rigidbody);
    const colliders = object.GetComponents(Collider).filter(component => component.enabled);
    if (!rigidbody && colliders.length === 0) return;
    let record = this.records.get(object);
    if (record && (record.rigidbody !== rigidbody || record.kinematic !== Boolean(rigidbody?.isKinematic))) {
      this._remove(object);
      record = null;
    }
    if (!record) {
      const R = this.RAPIER;
      const desc = rigidbody
        ? (rigidbody.isKinematic ? R.RigidBodyDesc.kinematicPositionBased() : R.RigidBodyDesc.dynamic())
        : R.RigidBodyDesc.fixed();
      const position = object.transform.position;
      const rotation = object.transform.rotation;
      desc.setTranslation(position.x, position.y, position.z).setRotation(rotation);
      if (rigidbody) {
        desc.setGravityScale(rigidbody.useGravity ? 1 : 0);
        desc.setLinearDamping(rigidbody.drag);
        desc.setAngularDamping(rigidbody.angularDamping);
        desc.setAdditionalMass(rigidbody.mass);
      }
      const body = this.world.createRigidBody(desc);
      if (rigidbody) {
        rigidbody._body = body;
        body.enableCcd(rigidbody.collisionDetectionMode !== CollisionDetectionMode.Discrete);
        rigidbody._applyConstraints();
        body.setLinvel(rigidbody._pendingVelocity, true);
      }
      record = { body, colliders: new Map(), rigidbody, kinematic: Boolean(rigidbody?.isKinematic),
        previousPosition: position.clone(), currentPosition: position.clone(),
        previousRotation: rotation.clone(), currentRotation: rotation.clone() };
      this.records.set(object, record);
    }
    if (rigidbody) {
      const ccd = rigidbody.collisionDetectionMode !== CollisionDetectionMode.Discrete;
      if (record.body.isCcdEnabled() !== ccd) record.body.enableCcd(ccd);
      const gravityScale = rigidbody.useGravity ? 1 : 0;
      if (record.body.gravityScale() !== gravityScale)
        record.body.setGravityScale(gravityScale, true);
      if (record.body.linearDamping() !== rigidbody.drag)
        record.body.setLinearDamping(rigidbody.drag);
    }
    const scale = object.object3D.getWorldScale(new Vector3());
    for (const component of colliders) {
      if (record.colliders.has(component)) continue;
      const colliderDesc = this._colliderDesc(component, scale);
      if (!colliderDesc) continue;
      const handle = this.world.createCollider(colliderDesc, record.body);
      component._collider = handle;
      record.colliders.set(component, handle);
      this.byHandle.set(handle.handle, component);
    }
    for (const [component, handle] of [...record.colliders]) {
      if (!colliders.includes(component) || component._destroyed) {
        this.byHandle.delete(handle.handle);
        this.world.removeCollider(handle, true);
        record.colliders.delete(component);
      } else if (handle.isSensor() !== component.isTrigger)
        handle.setSensor(component.isTrigger);
    }
  }
  beforeFixedUpdate() {
    this.world.gravity = Physics.gravity;
    this.world.integrationParameters.dt = this.runtime.fixedDeltaTime;
    for (const object of [...this.records.keys()]) {
      if (object._runtime !== this.runtime || !object.activeInHierarchy || object._destroyed) this._remove(object);
    }
    for (const object of this.runtime._objects) {
      if (!object.activeInHierarchy) continue;
      this._ensure(object);
      const record = this.records.get(object);
      if (!record) continue;
      const position = object.transform.position;
      const rotation = object.transform.rotation;
      if (record.rigidbody && !record.rigidbody.isKinematic) {
        // Unity scripts may set Transform directly on a dynamic Rigidbody. A frozen
        // physics rotation still permits that scripted turn; carry it into Rapier.
        if (position.distanceToSquared(record.currentPosition) > 1e-10) {
          record.body.setTranslation(position, true);
          record.previousPosition.copy(position);
          record.currentPosition.copy(position);
        }
        if (1 - Math.abs(rotation.dot(record.currentRotation)) > 1e-10) {
          record.body.setRotation(rotation, true);
          record.previousRotation.copy(rotation);
          record.currentRotation.copy(rotation);
        }
        continue;
      }
      if (record.rigidbody) {
        record.body.setNextKinematicTranslation(position);
        record.body.setNextKinematicRotation(rotation);
      } else {
        record.body.setTranslation(position, false);
        record.body.setRotation(rotation, false);
      }
    }
  }
  afterFixedUpdate() {
    // FixedUpdate scripts can move a dynamic Rigidbody through Transform.Translate.
    // Apply those writes before stepping physics, then read back the resolved pose.
    for (const [object, record] of this.records) {
      if (!record.rigidbody || record.rigidbody.isKinematic) continue;
      const position = object.transform.position;
      const rotation = object.transform.rotation;
      if (position.distanceToSquared(record.currentPosition) > 1e-10) {
        record.body.setTranslation(position, true);
        record.previousPosition.copy(position);
        record.currentPosition.copy(position);
      }
      if (1 - Math.abs(rotation.dot(record.currentRotation)) > 1e-10) {
        record.body.setRotation(rotation, true);
        record.previousRotation.copy(rotation);
        record.currentRotation.copy(rotation);
      }
    }
    for (const object of this.runtime._objects) {
      if (!object.activeInHierarchy) continue;
      for (const wheel of object.GetComponents(WheelCollider))
        if (wheel.enabled) wheel._step(this.runtime.fixedDeltaTime);
    }
    this.world.step(this.queue, this.hooks);
    for (const [object, record] of this.records) {
      if (!record.rigidbody || record.rigidbody.isKinematic) continue;
      // Unity consumes AddForce for one physics step; Rapier retains it until reset.
      record.body.resetForces(false);
      const position = record.body.translation();
      const rotation = record.body.rotation();
      record.previousPosition.copy(record.currentPosition);
      record.previousRotation.copy(record.currentRotation);
      record.currentPosition.set(position.x, position.y, position.z);
      record.currentRotation.set(rotation.x, rotation.y, rotation.z, rotation.w);
      object.transform.position = new Vector3(position.x, position.y, position.z);
      object.transform.rotation = new Quaternion(rotation.x, rotation.y, rotation.z, rotation.w);
    }
    this.queue.drainCollisionEvents((first, second, started) => {
      const a = this.byHandle.get(first);
      const b = this.byHandle.get(second);
      if (!a || !b) return;
      const trigger = a.isTrigger || b.isTrigger;
      const method = trigger
        ? (started ? 'OnTriggerEnter' : 'OnTriggerExit')
        : (started ? 'OnCollisionEnter' : 'OnCollisionExit');
      let contact = null;
      if (!trigger && started) {
        this.world.contactPair(a._collider, b._collider, (manifold, flipped) => {
          if (contact || manifold.numSolverContacts() === 0) return;
          const point = manifold.solverContactPoint(0);
          if (!point) return;
          const normal = manifold.normal();
          contact = {
            point: new Vector3(point.x, point.y, point.z),
            normal: new Vector3(normal.x, normal.y, normal.z).multiplyScalar(flipped ? -1 : 1),
          };
        });
      }
      if (!trigger && started && !contact) {
        const position = b.gameObject.transform.position;
        const point = a.ClosestPoint(position);
        const normal = position.clone().sub(point).normalize();
        contact = { point, normal };
      }
      for (const [own, other] of [[a, b], [b, a]]) {
        const event = trigger ? other : {
          collider: other,
          gameObject: other.gameObject,
          contacts: contact ? [{ point: contact.point, normal: own === a
            ? contact.normal : contact.normal.clone().negate() }] : [],
        };
        for (const component of own.gameObject._components) {
          if (component._active) component[method]?.(event);
        }
      }
    });
  }
  getInterpolatedPose(object) {
    const record = this.records.get(object);
    if (!record?.rigidbody || record.rigidbody.isKinematic) return null;
    // An Update script may have positioned a dynamic body after the last
    // physics step. Render that authored pose instead of an older physics pose.
    const scriptedPosition = object.transform.position;
    const scriptedRotation = object.transform.rotation;
    if (scriptedPosition.distanceToSquared(record.currentPosition) > 1e-10
      || 1 - Math.abs(scriptedRotation.dot(record.currentRotation)) > 1e-10)
      return { position: scriptedPosition, rotation: scriptedRotation };
    const alpha = Math.max(0, Math.min(1,
      this.runtime._accumulator / this.runtime.fixedDeltaTime));
    return {
      position: record.previousPosition.clone().lerp(record.currentPosition, alpha),
      rotation: record.previousRotation.clone().slerp(record.currentRotation, alpha),
    };
  }
  raycast(origin, direction, maxDistance = Infinity, mask = Physics.DefaultRaycastLayers,
    query = QueryTriggerInteraction.UseGlobal) {
    if (direction.lengthSq() === 0 || maxDistance < 0) return null;
    const unit = direction.clone().normalize();
    const ray = new this.RAPIER.Ray(origin, unit);
    const hit = this.world.castRayAndGetNormal(ray, maxDistance, true,
      undefined, undefined, undefined, undefined,
      handle => this._matchesLayer(handle, mask, query) && !handle.containsPoint(origin));
    if (!hit) return null;
    const distance = hit.timeOfImpact ?? hit.toi;
    const collider = this.byHandle.get(hit.collider.handle) ?? null;
    return new RaycastHit(collider, origin.clone().addScaledVector(unit, distance),
      new Vector3(hit.normal.x, hit.normal.y, hit.normal.z), distance);
  }
  _matchesLayer(handle, mask, query = QueryTriggerInteraction.UseGlobal) {
    const collider = this.byHandle.get(handle.handle);
    const bits = Number(mask?.value ?? mask);
    const hitTriggers = query === QueryTriggerInteraction.Collide
      || (query === QueryTriggerInteraction.UseGlobal && Physics.queriesHitTriggers);
    return collider && (bits & (1 << collider.gameObject.layer)) !== 0
      && (hitTriggers || !collider.isTrigger);
  }
  overlap(center, shape, rotation, mask = -1) {
    const result = [];
    this.world.intersectionsWithShape(center, rotation, shape, handle => {
      if (this._matchesLayer(handle, mask)) result.push(this.byHandle.get(handle.handle));
      return true;
    });
    return result;
  }
  sphereCast(origin, radius, direction, maxDistance = Infinity, mask = -1) {
    const unit = direction.clone().normalize();
    const hit = this.world.castShape(origin, Quaternion.identity, unit, new this.RAPIER.Ball(radius),
      0, maxDistance, true, undefined, undefined, undefined, undefined,
      handle => this._matchesLayer(handle, mask));
    if (!hit) return null;
    const collider = this.byHandle.get(hit.collider.handle) ?? null;
    const point = hit.witness1;
    const normal = hit.normal1;
    return new RaycastHit(collider, new Vector3(point.x, point.y, point.z),
      new Vector3(normal.x, normal.y, normal.z), hit.time_of_impact);
  }
  raycastAll(origin, direction, maxDistance = Infinity, mask = Physics.DefaultRaycastLayers,
    query = QueryTriggerInteraction.UseGlobal) {
    if (direction.lengthSq() === 0 || maxDistance < 0) return [];
    const unit = direction.clone().normalize();
    const ray = new this.RAPIER.Ray(origin, unit);
    const hits = [];
    this.world.intersectionsWithRay(ray, maxDistance, true, hit => {
      if (this._matchesLayer(hit.collider, mask, query)) {
        const distance = hit.timeOfImpact;
        hits.push(new RaycastHit(this.byHandle.get(hit.collider.handle),
          origin.clone().addScaledVector(unit, distance),
          new Vector3(hit.normal.x, hit.normal.y, hit.normal.z), distance));
      }
      return true;
    });
    return hits.sort((left, right) => left.distance - right.distance);
  }
}

export const NavMeshPathStatus = globalThis.Object.freeze({ PathComplete: 0, PathPartial: 1, PathInvalid: 2 });

export class NavMeshPath {
  constructor() { this.corners = []; this.status = NavMeshPathStatus.PathInvalid; }
  ClearCorners() { this.corners = []; this.status = NavMeshPathStatus.PathInvalid; }
}

export class NavMeshHit {
  constructor() {
    this.position = new Vector3();
    this.normal = new Vector3();
    this.distance = 0;
    this.mask = 0;
    this.hit = false;
  }
}

export class NavMesh {
  static AllAreas = -1;
  static get _navigation() { return activeRuntime?.navigation ?? null; }
  static _allowsDefaultArea(areaMask) { return areaMask === NavMesh.AllAreas || Boolean(areaMask & 1); }
  static CalculatePath(sourcePosition, targetPosition, areaMask, path) {
    if (!(path instanceof NavMeshPath)) throw new TypeError('NavMesh.CalculatePath requires a NavMeshPath');
    path.ClearCorners();
    if (!this._allowsDefaultArea(areaMask)) return false;
    const corners = this._navigation?.calculatePath(sourcePosition, targetPosition);
    if (!corners?.length) return false;
    path.corners = corners;
    path.status = NavMeshPathStatus.PathComplete;
    return true;
  }
  static SamplePosition(sourcePosition, hit, maxDistance, areaMask = NavMesh.AllAreas) {
    const result = this._allowsDefaultArea(areaMask)
      ? this._navigation?.samplePosition(sourcePosition, maxDistance) : null;
    const value = new NavMeshHit();
    if (result) {
      value.position = result.position;
      value.distance = result.position.distanceTo(sourcePosition);
      value.mask = 1;
      value.hit = true;
    }
    hit.value = value;
    return Boolean(result);
  }
  static Raycast(sourcePosition, targetPosition, hit, areaMask = NavMesh.AllAreas) {
    const result = this._allowsDefaultArea(areaMask)
      ? this._navigation?.raycast(sourcePosition, targetPosition) : null;
    const value = new NavMeshHit();
    if (result) globalThis.Object.assign(value, result, { mask: 1, hit: true });
    hit.value = value;
    return Boolean(result);
  }
  static FindClosestEdge(sourcePosition, hit, areaMask = NavMesh.AllAreas) {
    const result = this._allowsDefaultArea(areaMask)
      ? this._navigation?.findClosestEdge(sourcePosition) : null;
    const value = new NavMeshHit();
    if (result) globalThis.Object.assign(value, result, { mask: 1, hit: true });
    hit.value = value;
    return Boolean(result);
  }
}

export class NavMeshAgent extends Behaviour {
  constructor() {
    super();
    this.speed = 3.5;
    this.acceleration = 8;
    this.radius = 0.5;
    this.height = 2;
    this.useCrowd = false;
    this.angularSpeed = 120;
    this.stoppingDistance = 0;
    this.autoBraking = true;
    this.updatePosition = true;
    this.updateRotation = true;
    this.isStopped = false;
    this.destination = new Vector3();
    this.velocity = new Vector3();
    this.desiredVelocity = new Vector3();
    this.path = new NavMeshPath();
    this._corner = 0;
    this._targetVersion = 0;
  }
  get hasPath() { return this.path.status !== NavMeshPathStatus.PathInvalid && this._corner < this.path.corners.length; }
  get pathPending() { return false; }
  get pathStatus() { return this.path.status; }
  get remainingDistance() {
    if (!this.hasPath) return 0;
    let distance = this.transform.position.distanceTo(this.path.corners[this._corner]);
    for (let i = this._corner + 1; i < this.path.corners.length; i++)
      distance += this.path.corners[i - 1].distanceTo(this.path.corners[i]);
    return distance;
  }
  get steeringTarget() { return this.path.corners[this._corner] ?? this.transform.position; }
  SetDestination(target) {
    this.destination = target.clone();
    const path = new NavMeshPath();
    NavMesh.CalculatePath(this.transform.position, target, NavMesh.AllAreas, path);
    return this.SetPath(path);
  }
  SetPath(path) {
    if (!(path instanceof NavMeshPath)) throw new TypeError('NavMeshAgent.SetPath requires a NavMeshPath');
    this.path = path;
    if (path.corners.length) this.destination = path.corners.at(-1).clone();
    this._corner = path.corners.length > 1 ? 1 : 0;
    this._targetVersion++;
    return this.hasPath;
  }
  ResetPath() {
    this.path = new NavMeshPath(); this._corner = 0; this.velocity.set(0, 0, 0);
    this._targetVersion++;
  }
  Warp(position) {
    this.transform.position = position;
    this.gameObject?._runtime?.navigation?.crowdAgents.get(this)?.agent.teleport(position);
    this.ResetPath();
    return true;
  }
  _advanceNavigation(navigation, deltaTime) {
    if (!this.enabled || this.isStopped || !this.hasPath || !this.updatePosition) return;
    const current = this.transform.position;
    const target = this.steeringTarget;
    const offset = target.clone().sub(current);
    const distance = offset.length();
    if (distance <= Math.max(this.stoppingDistance, 1e-3)) {
      if (++this._corner >= this.path.corners.length) this.ResetPath();
      return;
    }
    this.desiredVelocity.copy(offset.normalize().multiplyScalar(this.speed));
    const change = this.desiredVelocity.clone().sub(this.velocity);
    this.velocity.add(change.clampLength(0, this.acceleration * deltaTime));
    const rigidbody = this.GetComponent(Rigidbody);
    if (rigidbody && !rigidbody.isKinematic) {
      const velocity = rigidbody.velocity;
      rigidbody.velocity = new Vector3(this.velocity.x, velocity.y, this.velocity.z);
    } else {
      this.transform.position = current.add(this.velocity.clone().multiplyScalar(
        Math.min(deltaTime, distance / Math.max(this.velocity.length(), 1e-6))));
    }
    this._faceVelocity(deltaTime);
  }
  _advanceCrowd(agent, deltaTime) {
    const velocity = agent.velocity();
    const desiredVelocity = agent.desiredVelocity();
    this.velocity = new Vector3(velocity.x, velocity.y, velocity.z);
    this.desiredVelocity = new Vector3(desiredVelocity.x, desiredVelocity.y, desiredVelocity.z);
    if (this.updatePosition) {
      const position = agent.position();
      const rigidbody = this.GetComponent(Rigidbody);
      if (rigidbody && !rigidbody.isKinematic) {
        const velocity = rigidbody.velocity;
        rigidbody.velocity = new Vector3(this.velocity.x, velocity.y, this.velocity.z);
      } else this.transform.position = new Vector3(position.x, position.y, position.z);
    }
    this._faceVelocity(deltaTime);
  }
  _faceVelocity(deltaTime) {
    if (this.updateRotation && this.velocity.lengthSq() > 1e-8) {
      const forward = new Vector3(this.velocity.x, 0, this.velocity.z);
      if (forward.lengthSq() > 1e-8) {
        const desired = Quaternion.LookRotation(forward);
        this.transform.rotation = this.transform.rotation.rotateTowards(desired, this.angularSpeed * Mathf.Deg2Rad * deltaTime);
      }
    }
  }
}

export const UnityEngine = globalThis.Object.freeze({
  Object, TextAsset, ScriptableObject, PhysicsMaterial, Resources, GameObject, Component, Behaviour, MonoBehaviour, Transform,
  Vector2, Vector3, Vector4, Quaternion, Matrix4x4, Plane, Color, Mathf, AnimationCurve, Keyframe, Ray, Time, WaitForSeconds, WaitForEndOfFrame, WaitUntil,
  Space, PrimitiveType, Debug, Random, Application, SystemInfo, Screen, Rect, Cursor, LayerMask,
  Scene, SceneManager, Camera, Renderer, MeshFilter, MeshRenderer, SkinnedMeshRenderer, Light, Animator, Avatar,
  StateMachineBehaviour, GUI,
  Rigidbody, Collider, Bounds, BoxCollider, MeshCollider, SphereCollider, CapsuleCollider, WheelCollider, WheelHit,
  CharacterController, CollisionFlags, ForceMode, RigidbodyConstraints, RaycastHit, Physics,
  Input, KeyCode, UnityEvent, InputSystem, Gamepad, DualShockGamepad, Pointer, Touchscreen, OnScreenControl,
  Canvas, Button, Image, Text, RectTransform, RectTransformUtility, AudioSource, ParticleSystem,
  AI: globalThis.Object.freeze({ NavMesh, NavMeshPath, NavMeshPathStatus, NavMeshHit, NavMeshAgent }),
  Profiling: globalThis.Object.freeze({ Profiler }),
});

export class PlygroundItem extends MonoBehaviour {
  itemId = '';
  displayName = '';
  moduleId = '';
  templateId = '';
  buildId = '';
  sourceHash = '';
  importedAtUtc = '';
  tags = [];
  get ItemId() { return this.itemId; }
  get DisplayName() { return this.displayName; }
  get ModuleId() { return this.moduleId; }
  get TemplateId() { return this.templateId; }
  get BuildId() { return this.buildId; }
  get SourceHash() { return this.sourceHash; }
  get ImportedAtUtc() { return this.importedAtUtc; }
  get Tags() { return this.tags; }
  HasTag(value) { return typeof value === 'string' && this.tags.some(tag => tag.toLowerCase() === value.toLowerCase()); }
  SetImportState(id, name, module, template, build, hash, importedUtc, itemTags = null) {
    globalThis.Object.assign(this, { itemId: id, displayName: name, moduleId: module, templateId: template,
      buildId: build, sourceHash: hash, importedAtUtc: importedUtc, tags: itemTags ?? [] });
  }
}
export { THREE, Vector2, Vector3, Vector4, Quaternion, Matrix4x4, Plane, Mathf, AnimationCurve, Keyframe };
export { AssetBundleLoader, UnknownComponentError } from './assets.js';
export { AssetDatabase, PrefabUtility } from './preview-editor-api.js';
export { AssetFormatError } from './asset-schema.js';
export { applyAnimatorRootMotion, hasMovingRoot, sampleRootMotionDelta } from './root-motion.js';
