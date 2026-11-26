import { Vector2 } from './math.js';
import { Input } from './input.js';

export const InputActionType = Object.freeze({ Value: 0, Button: 1, PassThrough: 2 });
export const InputActionPhase = Object.freeze({ Disabled: 0, Waiting: 1, Started: 2, Performed: 3, Canceled: 4 });
export const InputActionChange = Object.freeze({ ActionEnabled: 0, ActionDisabled: 1,
  ActionMapEnabled: 2, ActionMapDisabled: 3, ActionStarted: 4,
  ActionPerformed: 5, ActionCanceled: 6 });
let actionChangeHandler = null;
export function setInputActionChangeHandler(handler) { actionChangeHandler = handler; }
function actionType(value) {
  if (typeof value !== 'string') return value;
  if (!Object.hasOwn(InputActionType, value))
    throw new Error(`Unsupported InputAction type: ${value}`);
  return InputActionType[value];
}
const bindingFlags = item => item.m_Flags ?? item.flags
  ?? (item.isComposite ? 1 : item.isPartOfComposite ? 2 : 0);
const compositePath = path => path === 'Dpad' ? '2DVector' : path;

const actions = new Set();
const virtualControls = new Map();
let nextBindingId = 1;
const warnedUnsupportedInteractions = new Set();
export function setVirtualControl(path, value, owner = path) {
  if (value == null) {
    if (virtualControls.get(path)?.owner === owner) virtualControls.delete(path);
    return;
  }
  virtualControls.set(path, { owner, value: value instanceof Vector2 ? value.clone() : value });
}
const buttonThreshold = 0.5;
const keyboardAliases = {
  space: ['Space'], shift: ['ShiftLeft', 'ShiftRight'], ctrl: ['ControlLeft', 'ControlRight'],
  alt: ['AltLeft', 'AltRight'], tab: ['Tab'], enter: ['Enter'], escape: ['Escape'],
  leftShift: ['ShiftLeft'], rightShift: ['ShiftRight'],
  leftCtrl: ['ControlLeft'], rightCtrl: ['ControlRight'],
  leftAlt: ['AltLeft'], rightAlt: ['AltRight'],
  upArrow: ['ArrowUp'], downArrow: ['ArrowDown'], leftArrow: ['ArrowLeft'], rightArrow: ['ArrowRight'],
};
const gamepadButtons = {
  buttonSouth: 0, buttonEast: 1, buttonWest: 2, buttonNorth: 3,
  leftShoulder: 4, rightShoulder: 5, leftTrigger: 6, rightTrigger: 7,
  select: 8, start: 9, leftStickPress: 10, rightStickPress: 11,
  'dpad/up': 12, 'dpad/down': 13, 'dpad/left': 14, 'dpad/right': 15,
};

function keyCodes(name) {
  if (keyboardAliases[name]) return keyboardAliases[name];
  if (/^[a-z]$/i.test(name)) return [`Key${name.toUpperCase()}`];
  if (/^[0-9]$/.test(name)) return [`Digit${name}`];
  throw new Error(`Unsupported keyboard control: ${name}`);
}

function gamepads() {
  return [...(globalThis.navigator?.getGamepads?.() ?? [])].filter(Boolean);
}

function control(path) {
  if (virtualControls.has(path)) {
    const value = virtualControls.get(path).value;
    return value instanceof Vector2 ? value.clone() : value;
  }
  if (path === '*/{Submit}') return Math.max(Number(Input.GetKey('Enter')),
    ...gamepads().map(pad => pad.buttons?.[0]?.value ?? 0));
  if (path === '*/{Cancel}') return Math.max(Number(Input.GetKey('Escape')),
    ...gamepads().map(pad => pad.buttons?.[1]?.value ?? 0));
  const match = /^<([^>]+)>\/(.+)$/.exec(path);
  if (!match) throw new Error(`Unsupported InputAction binding path: ${path}`);
  const [, device, name] = match;
  if (device === 'Joystick') {
    const mapped = name === 'stick' ? 'leftStick'
      : name === '{Hatswitch}' ? 'dpad'
        : name === 'trigger' ? 'buttonSouth'
          : name.startsWith('stick/') ? `leftStick/${name.slice(6)}` : null;
    if (mapped) return control(`<Gamepad>/${mapped}`);
  }
  if (device === 'Keyboard') return Math.max(...keyCodes(name).map(code => Number(Input.GetKey(code))));
  if (device === 'Mouse') {
    if (name === 'leftButton') return Number(Input.GetMouseButton(0));
    if (name === 'rightButton') return Number(Input.GetMouseButton(1));
    if (name === 'middleButton') return Number(Input.GetMouseButton(2));
    if (name === 'delta') return new Vector2(Input.GetAxis('Mouse X'), Input.GetAxis('Mouse Y'));
    if (name === 'position') {
      const position = Input.mousePosition;
      return new Vector2(position.x, position.y);
    }
    if (name === 'scroll/y') return Input.GetAxis('Mouse ScrollWheel');
    if (name === 'scroll') return Input.mouseScrollDelta;
  }
  if (device === 'Pointer') {
    if (name === 'press') return Number(Input.GetMouseButton(0) || Input.touches.some(touch =>
      touch.phase !== 3 && touch.phase !== 4));
    if (name === 'position') {
      const touch = Input.touches[0];
      const position = touch?.position ?? Input.mousePosition;
      return new Vector2(position.x, position.y);
    }
    if (name === 'delta') {
      const touch = Input.touches[0];
      const delta = touch?.deltaPosition ?? Input.mousePositionDelta;
      return new Vector2(delta.x, delta.y);
    }
  }
  if (device === 'Touchscreen' || device === 'Pen') {
    const touches = Input.touches.filter(touch => device !== 'Pen' || touch.type === 2);
    const touchPath = /^(?:(?:primaryTouch|touch(\d+)|touches\/\*|touch\*)\/)?(press|tip|tap|position|delta|pressure|touchId|tilt|twist)$/.exec(name);
    if (touchPath) {
      const selected = touchPath[1] ? [touches[Number(touchPath[1])]]
        : name.startsWith('touches/*') || name.startsWith('touch*') ? touches : [touches[0]];
      const field = touchPath[2];
      if (field === 'press' || field === 'tip') return Number(selected.some(touch => touch
        && touch.phase !== 3 && touch.phase !== 4));
      if (field === 'tap') return Number(selected.some(touch => touch?.tap));
      if (field === 'position' || field === 'delta' || field === 'tilt') {
        const value = selected.find(Boolean)?.[field === 'position' ? 'position'
          : field === 'delta' ? 'deltaPosition' : 'tilt'];
        return value?.clone() ?? new Vector2();
      }
      if (field === 'pressure') return Math.max(0, ...selected.map(touch => touch?.pressure ?? 0));
      if (field === 'twist') return selected.find(Boolean)?.twist ?? 0;
      if (field === 'touchId') return selected.find(Boolean)?.fingerId ?? 0;
    }
  }
  if (device === 'Gamepad') {
    const pads = gamepads();
    if (name === 'dpad') return pads.map(pad => new Vector2(
      (pad.buttons?.[15]?.value ?? 0) - (pad.buttons?.[14]?.value ?? 0),
      (pad.buttons?.[12]?.value ?? 0) - (pad.buttons?.[13]?.value ?? 0)))
      .reduce((best, value) => value.lengthSq() > best.lengthSq() ? value : best, new Vector2());
    const direction = /^(leftStick|rightStick)\/(up|down|left|right)$/.exec(name);
    if (direction) {
      const offset = direction[1] === 'leftStick' ? 0 : 2;
      const axis = direction[2] === 'left' || direction[2] === 'right' ? offset : offset + 1;
      const sign = direction[2] === 'right' || direction[2] === 'down' ? 1 : -1;
      return Math.max(0, ...pads.map(pad => Math.max(0, (pad.axes?.[axis] ?? 0) * sign)));
    }
    if (name === 'leftStick' || name === 'rightStick') {
      const offset = name === 'leftStick' ? 0 : 2;
      return pads.map(pad => new Vector2(pad.axes?.[offset] ?? 0, -(pad.axes?.[offset + 1] ?? 0)))
        .reduce((best, value) => value.lengthSq() > best.lengthSq() ? value : best, new Vector2());
    }
    if (Object.hasOwn(gamepadButtons, name))
      return Math.max(0, ...pads.map(pad => pad.buttons?.[gamepadButtons[name]]?.value ?? 0));
  }
  throw new Error(`Unsupported InputAction binding path: ${path}`);
}

function edgeMatches(path, edge) {
  if (path === '*/{Submit}') return edge.kind === 'key' && edge.code === 'Enter';
  if (path === '*/{Cancel}') return edge.kind === 'key' && edge.code === 'Escape';
  const match = /^<([^>]+)>\/(.+)$/.exec(path);
  if (!match) return false;
  const [, device, name] = match;
  if (device === 'Keyboard' && edge.kind === 'key')
    return keyCodes(name).includes(edge.code);
  if (device === 'Mouse' && edge.kind === 'mouse')
    return edge.button === ({ leftButton: 0, rightButton: 1, middleButton: 2 }[name]);
  if (device === 'Pointer' && name === 'press')
    return edge.kind === 'touch' || edge.kind === 'mouse' && edge.button === 0;
  if (device === 'Touchscreen' && edge.kind === 'touch' && edge.type !== 2) {
    if (name === 'touches/*/press' || name === 'touch*/press') return true;
    const index = /^touch(\d+)\/press$/.exec(name)?.[1];
    if (name === 'primaryTouch/press' || index != null)
      return Input.touches[Number(index ?? 0)]?.fingerId === edge.id;
  }
  if (device === 'Pen' && edge.kind === 'touch' && edge.type === 2)
    return name === 'tip' || name === 'press' || name === 'primaryTouch/press'
      || name === 'touches/*/press';
  return false;
}

function process(value, processors) {
  for (const processor of processors) {
    const scale = /^scaleVector2\(x=([-\d.]+),y=([-\d.]+)\)$/i.exec(processor.replace(/\s/g, ''));
    if (scale && value instanceof Vector2) value = new Vector2(value.x * Number(scale[1]), value.y * Number(scale[2]));
    else if (/^scale\(factor=([-\d.]+)\)$/i.test(processor.replace(/\s/g, ''))) {
      const factor = Number(/^scale\(factor=([-\d.]+)\)$/i.exec(processor.replace(/\s/g, ''))[1]);
      value = value instanceof Vector2 ? value.clone().multiplyScalar(factor) : value * factor;
    }
    else if (processor.startsWith('axisDeadzone(') && typeof value === 'number') {
      const min = Number(/min=([-\d.]+)/.exec(processor)?.[1] ?? 0.125);
      value = Math.abs(value) < min ? 0 : value;
    } else throw new Error(`Unsupported InputAction processor: ${processor}`);
  }
  return value;
}

function magnitude(value) { return value instanceof Vector2 ? value.length() : Math.abs(value); }
function zero(value) { return value instanceof Vector2 ? new Vector2() : 0; }
function equal(a, b) { return a instanceof Vector2 && b instanceof Vector2 ? a.equals(b) : a === b; }

class InputActionEvent {
  constructor() { this.listeners = new Set(); }
  AddListener(listener) { this.listeners.add(listener); }
  RemoveListener(listener) { this.listeners.delete(listener); }
  invoke(context) { for (const listener of [...this.listeners]) listener(context); }
}

export class InputControlScheme {
  constructor(record = {}) {
    this.name = record.name ?? '';
    this.bindingGroup = record.bindingGroup ?? this.name;
    this.devices = record.devices ?? [];
  }
  SupportsDevice(device) {
    const type = device?.constructor?.name;
    return this.devices.some(requirement => {
      const path = requirement.devicePath;
      return path === `<${type}>` || (type === 'Mouse' && path === '<Pointer>');
    });
  }
  // The transpiled Nullable<InputControlScheme> accessors also appear after
  // LINQ First selects a concrete scheme.
  get HasValue() { return true; }
  get Value() { return this; }
  static FindControlSchemeForDevices(devices, schemes) {
    const scheme = schemes?.find(item => devices.some(device => item.SupportsDevice(device)));
    return scheme ?? { HasValue: false, Value: null };
  }
}

export class InputBinding {
  static MaskByGroup(...groups) { return { groups: groups.flat().join(';') }; }
  constructor(action, path, composite = false) {
    this.action = action; this.path = path; this.composite = composite;
    this.parts = new Map(); this.partBindings = []; this.processors = []; this.interactions = [];
    this.isPartOfComposite = false; this.name = '';
    this.groups = ''; this.id = globalThis.crypto?.randomUUID?.() ?? `binding-${nextBindingId++}`;
    this.overridePath = null;
  }
  With(name, path) {
    const part = name.toLowerCase();
    const binding = new InputBinding(this.action, path);
    binding.isPartOfComposite = true; binding.name = part;
    this.partBindings.push(binding); this.action.bindings.push(binding);
    this.parts.set(part, [...(this.parts.get(part) ?? []), binding]);
    return this;
  }
  WithProcessor(processor) { this.processors.push(processor); return this; }
  WithInteraction(interaction) {
    if (!/^(press|hold(\(.*\))?|multitap\(.*\))$/i.test(interaction))
      throw new Error(`Unsupported InputAction interaction: ${interaction}`);
    this.interactions.push(interaction);
    return this;
  }
  read() {
    if (!this.composite) return process(control(this.overridePath ?? this.path), this.processors);
    const part = name => Math.max(0, ...(this.parts.get(name) ?? [])
      .filter(binding => this.action._bindingEnabled(binding))
      .map(binding => Number(control(binding.overridePath ?? binding.path))));
    if (/^(?:Axis|1DAxis)(?:\(|$)/.test(this.path)) {
      const positive = part('positive'), negative = part('negative');
      const side = Number(/whichSideWins=(\d+)/i.exec(this.path)?.[1] ?? 0);
      const value = positive > 0 && negative > 0 && side
        ? (side === 1 ? positive : -negative) : positive - negative;
      return process(Math.max(-1, Math.min(1, value)), this.processors);
    }
    if (this.path !== '2DVector') throw new Error(`Unsupported InputAction composite: ${this.path}`);
    const value = new Vector2(part('right') - part('left'), part('up') - part('down'));
    if (value.lengthSq() > 1) value.normalize();
    return process(value, this.processors);
  }
}

/** Unity InputAction surface backed by browser keyboard, pointer and Gamepad API state. */
export class InputAction {
  get inProgress() {
    return this.phase === InputActionPhase.Started || this.phase === InputActionPhase.Performed;
  }
  constructor(name = '', type = InputActionType.Value, binding = null, interactions = null,
    processors = null, expectedControlType = null) {
    this.name = name; this.type = type; this.expectedControlType = expectedControlType;
    this.id = globalThis.crypto?.randomUUID?.() ?? `input-action-${actions.size + 1}`;
    this.actionMap = null;
    this.activeControl = null; this._activePath = null;
    this.bindingMask = null;
    this.bindings = []; this.enabled = false; this.phase = InputActionPhase.Disabled;
    this.triggered = false; this._pressed = false; this._wasPressed = false;
    this._wasReleased = false; this._wasPerformed = false;
    this._value = 0; this._previous = 0; this._holdElapsed = 0;
    this.started = new InputActionEvent(); this.performed = new InputActionEvent();
    this.canceled = new InputActionEvent();
    if (binding) this.AddBinding(binding);
    if (interactions) this.bindings.at(-1)?.WithInteraction(interactions);
    if (processors) this.bindings.at(-1)?.WithProcessor(processors);
    actions.add(this);
  }
  /** Hydrates a preprocessed Unity InputAction record, including composite bindings. */
  static fromSerialized(record) {
    const action = new InputAction(record.m_Name ?? record.name ?? '',
      actionType(record.m_Type ?? record.type ?? InputActionType.Value), null, null, null,
      record.m_ExpectedControlType || record.expectedControlType || null);
    action.id = record.m_Id ?? record.id ?? action.id;
    let composite = null;
    for (const item of record.m_SingletonActionBindings ?? record.bindings ?? []) {
      const flags = bindingFlags(item);
      const path = item.m_Path ?? item.path;
      const name = item.m_Name ?? item.name;
      if (flags & (1 | 4)) composite = action.AddCompositeBinding(compositePath(path));
      else if (flags & (2 | 8)) {
        if (!composite) throw new Error(`InputAction ${action.name} has an orphan composite part`);
        composite.With(name, path);
      } else { composite = null; action.AddBinding(path); }
      const binding = flags & (2 | 8) ? composite.partBindings.at(-1)
        : composite ?? action.bindings.at(-1);
      if (flags & (2 | 8)) {
        const group = item.m_Groups ?? item.groups ?? '';
        if (group) composite.groups += `${composite.groups ? ';' : ''}${group}`;
        binding.id = item.m_Id ?? item.id ?? binding.id;
        binding.groups = group;
      } else {
        binding.id = item.m_Id ?? item.id ?? binding.id;
        binding.groups = item.m_Groups ?? item.groups ?? '';
      }
      if (item.m_Processors || item.processors) binding.WithProcessor(item.m_Processors || item.processors);
      if (item.m_Interactions || item.interactions) binding.WithInteraction(item.m_Interactions || item.interactions);
    }
    if (record.enabled) action.Enable();
    return action;
  }
  toSerialized() {
    const bindings = this.bindings.map(binding => ({ id: binding.id,
      name: binding.name, path: binding.path,
      flags: binding.composite ? 1 : binding.isPartOfComposite ? 2 : 0,
      processors: binding.processors[0] ?? '', interactions: binding.interactions[0] ?? '' }));
    return { id: this.id, name: this.name, type: this.type, expectedControlType: this.expectedControlType,
      enabled: this.enabled, bindings };
  }
  AddBinding(path) { const binding = new InputBinding(this, path); this.bindings.push(binding); return binding; }
  AddCompositeBinding(path) { const binding = new InputBinding(this, path, true); this.bindings.push(binding); return binding; }
  ApplyBindingOverride(index, path) {
    const binding = this.bindings[index];
    if (!binding || typeof path !== 'string' || !path)
      throw new Error(`Invalid binding override on ${this.name}: ${index}`);
    binding.overridePath = path;
  }
  RemoveBindingOverride(index) {
    const binding = this.bindings[index];
    if (!binding) throw new Error(`Invalid binding index on ${this.name}: ${index}`);
    binding.overridePath = null;
  }
  RemoveAllBindingOverrides() { for (const binding of this.bindings) binding.overridePath = null; }
  _bindingEnabled(binding) {
    for (const mask of [this.bindingMask, this.actionMap?.bindingMask,
      this.actionMap?.asset?.bindingMask]) {
      const group = typeof mask === 'string' ? mask : mask?.groups;
      if (group && !group.split(';').some(value =>
        binding.groups.split(';').includes(value))) return false;
    }
    return true;
  }
  Enable() { if (!this.enabled) { this.enabled = true; this.phase = InputActionPhase.Waiting; } }
  Disable() {
    if (!this.enabled) return;
    if (this._pressed) this._emit('canceled', InputActionPhase.Canceled);
    this.enabled = false; this.phase = InputActionPhase.Disabled; this._pressed = false; this._value = 0;
  }
  Dispose() { this.Disable(); actions.delete(this); }
  ReadValue(type = this.expectedControlType) {
    if (!this.enabled) return type === 'Vector2' ? new Vector2() : 0;
    if (type === 'Vector2') return this._value instanceof Vector2 ? this._value.clone() : new Vector2();
    return typeof this._value === 'number' ? this._value : this._value.length();
  }
  WasPressedThisFrame() { return this._wasPressed; }
  WasReleasedThisFrame() { return this._wasReleased; }
  WasPerformedThisFrame() { return this._wasPerformed; }
  IsPressed() { return this._pressed; }
  _emit(name, phase) {
    this.phase = phase;
    if (name === 'performed') { this.triggered = true; this._wasPerformed = true; }
    actionChangeHandler?.(this, InputActionChange[`Action${name[0].toUpperCase()}${name.slice(1)}`],
      this._activePath, virtualControls.has(this._activePath));
    const action = this;
    this[name].invoke({ action, phase, control: action.activeControl,
      ReadValue(type) { return action.ReadValue(type); },
      get started() { return phase === InputActionPhase.Started; },
      get performed() { return phase === InputActionPhase.Performed; },
      get canceled() { return phase === InputActionPhase.Canceled; } });
  }
  _update(deltaTime = 0) {
    if (!this.enabled) return;
    const activeBindings = this.bindings.filter(binding =>
      !binding.isPartOfComposite && this._bindingEnabled(binding));
    const values = activeBindings.map(binding => binding.read());
    const vector = this.expectedControlType === 'Vector2' || values.some(value => value instanceof Vector2);
    let activeBinding = null;
    let value = vector ? new Vector2() : 0;
    values.forEach((current, index) => {
      if (magnitude(current) > magnitude(value)) { value = current; activeBinding = activeBindings[index]; }
    });
    if (activeBinding) {
      const part = activeBinding.composite ? activeBinding.partBindings.find(binding =>
        magnitude(control(binding.overridePath ?? binding.path)) > 0) : null;
      this._activePath = (part ?? activeBinding).overridePath ?? (part ?? activeBinding).path;
    }
    const pressed = magnitude(value) >= buttonThreshold;
    const wasPressed = this._pressed;
    this._wasPressed ||= pressed && !wasPressed;
    this._wasReleased ||= !pressed && wasPressed;
    this._value = value;
    const hold = activeBinding?.interactions.find(item => item.startsWith('hold'));
    const interactionBinding = activeBinding ?? activeBindings.find(binding =>
      (binding.overridePath ?? binding.path) === this._activePath);
    const multiTap = interactionBinding?.interactions.find(item => /^multitap\(/i.test(item));
    if (multiTap) {
      if (pressed && !wasPressed) {
        const key = `${this.name}:${multiTap}`;
        if (!warnedUnsupportedInteractions.has(key)) {
          warnedUnsupportedInteractions.add(key);
          console.warn(`InputAction ${this.name}: ${multiTap} is unavailable in browser preview`);
        }
      }
      this._pressed = pressed;
      this._previous = value instanceof Vector2 ? value.clone() : value;
      return;
    }
    if (this.type === InputActionType.Button && !pressed && !wasPressed && !hold) {
      const edges = Input._edgeSequence.filter(edge => activeBindings.some(binding =>
        !binding.composite && edgeMatches(binding.overridePath ?? binding.path, edge)));
      const downAt = edges.findIndex(edge => edge.down);
      if (downAt >= 0 && edges.slice(downAt + 1).some(edge => !edge.down)) {
        const source = activeBindings.find(binding => !binding.composite
          && edgeMatches(binding.overridePath ?? binding.path, edges[downAt]));
        this._activePath = source?.overridePath ?? source?.path ?? this._activePath;
        this._wasPressed = true; this._wasReleased = true;
        this._value = 1;
        this._emit('started', InputActionPhase.Started);
        this._emit('performed', InputActionPhase.Performed);
        this._value = 0;
        this._emit('canceled', InputActionPhase.Canceled);
        this.phase = InputActionPhase.Waiting; this._previous = 0;
        return;
      }
    }
    if (hold) {
      const duration = Number(/duration=([-\d.]+)/.exec(hold)?.[1] ?? 0.4);
      if (pressed && !wasPressed) { this._holdElapsed = 0; this._emit('started', InputActionPhase.Started); }
      if (pressed) {
        this._holdElapsed += deltaTime;
        if (this._holdElapsed >= duration && this.phase !== InputActionPhase.Performed)
          this._emit('performed', InputActionPhase.Performed);
      } else if (wasPressed) {
        this._emit('canceled', InputActionPhase.Canceled);
        this.phase = InputActionPhase.Waiting;
        this._holdElapsed = 0;
      }
    } else if (this.type === InputActionType.PassThrough) {
      if (!equal(value, this._previous)) this._emit('performed', InputActionPhase.Performed);
      this.phase = InputActionPhase.Waiting;
    } else if (this.type === InputActionType.Button) {
      if (pressed && !wasPressed) { this._emit('started', InputActionPhase.Started); this._emit('performed', InputActionPhase.Performed); }
      else if (!pressed && wasPressed) { this._emit('canceled', InputActionPhase.Canceled); this.phase = InputActionPhase.Waiting; }
    } else if (!equal(value, this._previous)) {
      if (magnitude(this._previous) === 0 && magnitude(value) > 0) this._emit('started', InputActionPhase.Started);
      if (magnitude(value) > 0) this._emit('performed', InputActionPhase.Performed);
      else { this._emit('canceled', InputActionPhase.Canceled); this.phase = InputActionPhase.Waiting; }
    }
    this._pressed = pressed;
    this._previous = value instanceof Vector2 ? value.clone() : value;
  }
  static _updateAll(deltaTime) { for (const action of [...actions]) action._update(deltaTime); }
  static _endFrame() {
    for (const action of actions) {
      action.triggered = false; action._wasPressed = false;
      action._wasReleased = false; action._wasPerformed = false;
    }
  }
}

/** Browser-backed action grouping; maps share the same device timeline as standalone actions. */
export class InputActionMap {
  constructor(name = '') { this.name = name; this.id = null; this.actions = []; this.enabled = false;
    this.bindingMask = null; this.asset = null; }
  AddAction(name, type = InputActionType.Value, binding = null, interactions = null,
    processors = null, expectedControlType = null) {
    const action = name instanceof InputAction ? name
      : new InputAction(name, type, binding, interactions, processors, expectedControlType);
    if (action.actionMap && action.actionMap !== this)
      throw new Error(`InputAction ${action.name} already belongs to another map`);
    if (!this.actions.includes(action)) { action.actionMap = this; this.actions.push(action); }
    if (this.enabled) action.Enable();
    return action;
  }
  FindAction(nameOrId, throwIfNotFound = false) {
    const action = this.actions.find(item => item.name === nameOrId || item.id === nameOrId) ?? null;
    if (!action && throwIfNotFound) throw new Error(`InputAction not found: ${nameOrId}`);
    return action;
  }
  Enable() { this.enabled = true; for (const action of this.actions) action.Enable(); }
  Disable() { this.enabled = false; for (const action of this.actions) action.Disable(); }
  Dispose() { this.Disable(); for (const action of this.actions) action.Dispose(); this.actions.length = 0; }
}

export class InputActionAsset {
  constructor() { this.name = ''; this.actionMaps = []; this.bindingMask = null;
    this.controlSchemes = []; this.unsupportedBindings = []; }
  *[Symbol.iterator]() {
    for (const map of this.actionMaps) yield* map.actions;
  }
  AddActionMap(name) {
    const map = name instanceof InputActionMap ? name : new InputActionMap(name);
    if (!this.actionMaps.includes(map)) { map.asset = this; this.actionMaps.push(map); }
    return map;
  }
  FindActionMap(name, throwIfNotFound = false) {
    const map = this.actionMaps.find(item => item.name === name) ?? null;
    if (!map && throwIfNotFound) throw new Error(`InputActionMap not found: ${name}`);
    return map;
  }
  FindAction(nameOrId, throwIfNotFound = false) {
    for (const map of this.actionMaps) {
      const action = map.FindAction(nameOrId);
      if (action) return action;
    }
    if (throwIfNotFound) throw new Error(`InputAction not found: ${nameOrId}`);
    return null;
  }
  FindControlScheme(name) {
    return this.controlSchemes.find(scheme => scheme.name === name) ?? null;
  }
  Enable() { for (const map of this.actionMaps) map.Enable(); }
  Disable() { for (const map of this.actionMaps) map.Disable(); }
  Dispose() { for (const map of this.actionMaps) map.Dispose(); this.actionMaps.length = 0; }
  SaveBindingOverridesAsJson() {
    const bindings = this.actionMaps.flatMap(map => map.actions.flatMap(action =>
      action.bindings.flatMap(binding => binding.overridePath == null ? [] : [{
        action: `${map.name}/${action.name}`, id: binding.id,
        path: binding.overridePath, interactions: 'null', processors: 'null' }])));
    return bindings.length ? JSON.stringify({ bindings }) : '';
  }
  LoadBindingOverridesFromJson(json, removeExisting = true) {
    if (removeExisting) for (const map of this.actionMaps)
      for (const action of map.actions) action.RemoveAllBindingOverrides();
    if (!json) return;
    for (const record of JSON.parse(json).bindings ?? []) {
      for (const map of this.actionMaps) for (const action of map.actions) {
        const index = action.bindings.findIndex(binding => binding.id === record.id);
        if (index >= 0) {
          if (record.path && record.path !== 'null') action.ApplyBindingOverride(index, record.path);
          else action.RemoveBindingOverride(index);
        }
      }
    }
  }
  static fromSerialized(record) {
    const asset = new InputActionAsset(); asset.name = record.m_Name ?? record.name ?? '';
    asset.controlSchemes = (record.controlSchemes ?? record.m_ControlSchemes ?? [])
      .map(scheme => new InputControlScheme(scheme));
    const maps = record.m_ActionMaps ?? record.maps ?? [];
    for (const source of typeof maps === 'string' ? JSON.parse(maps) : maps) {
      const map = asset.AddActionMap(source.m_Name ?? source.name ?? '');
      map.id = source.m_Id ?? source.id ?? null;
      for (const item of source.m_Actions ?? source.actions ?? [])
        map.AddAction(InputAction.fromSerialized(item));
      let composite = null;
      for (const binding of source.m_Bindings ?? source.bindings ?? []) {
        const action = map.FindAction(binding.m_Action ?? binding.action)
          ?? map.FindAction(binding.m_ActionId ?? binding.actionId);
        if (!action) throw new Error(`InputAction binding has unknown action in ${map.name}`);
        const flags = bindingFlags(binding);
        const path = binding.m_Path ?? binding.path;
        if (!(flags & (1 | 4))) {
          try { control(path); }
          catch (error) {
            asset.unsupportedBindings.push(`${map.name}/${action.name}: ${path}`);
            continue;
          }
        }
        if (flags & (1 | 4)) composite = action.AddCompositeBinding(compositePath(path));
        else if (flags & (2 | 8)) {
          if (!composite || composite.action !== action)
            throw new Error(`InputAction ${action.name} has an orphan composite part`);
          composite.With(binding.m_Name ?? binding.name, path);
        } else { composite = null; action.AddBinding(path); }
        const target = flags & (2 | 8) ? composite.partBindings.at(-1)
          : composite ?? action.bindings.at(-1);
        if (flags & (2 | 8)) {
          const group = binding.m_Groups ?? binding.groups ?? '';
          if (group) composite.groups += `${composite.groups ? ';' : ''}${group}`;
          target.id = binding.m_Id ?? binding.id ?? target.id;
          target.groups = group;
        } else {
          target.id = binding.m_Id ?? binding.id ?? target.id;
          target.groups = binding.m_Groups ?? binding.groups ?? '';
        }
        if (binding.m_Processors || binding.processors)
          target.WithProcessor(binding.m_Processors ?? binding.processors);
        if (binding.m_Interactions || binding.interactions)
          target.WithInteraction(binding.m_Interactions ?? binding.interactions);
      }
    }
    return asset;
  }
}

export class InputActionReference {
  constructor(action = null) { this.action = action; }
  static Create(action) { return new InputActionReference(action); }
}
