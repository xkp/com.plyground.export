import { Vector2 } from './math.js';
import { Input } from './input.js';

export const InputActionType = Object.freeze({ Value: 0, Button: 1, PassThrough: 2 });
export const InputActionPhase = Object.freeze({ Disabled: 0, Waiting: 1, Started: 2, Performed: 3, Canceled: 4 });

const actions = new Set();
const buttonThreshold = 0.5;
const keyboardAliases = {
  space: ['Space'], shift: ['ShiftLeft', 'ShiftRight'], ctrl: ['ControlLeft', 'ControlRight'],
  alt: ['AltLeft', 'AltRight'], tab: ['Tab'], enter: ['Enter'], escape: ['Escape'],
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
  const match = /^<([^>]+)>\/(.+)$/.exec(path);
  if (!match) throw new Error(`Unsupported InputAction binding path: ${path}`);
  const [, device, name] = match;
  if (device === 'Keyboard') return Math.max(...keyCodes(name).map(code => Number(Input.GetKey(code))));
  if (device === 'Mouse') {
    if (name === 'leftButton') return Number(Input.GetMouseButton(0));
    if (name === 'rightButton') return Number(Input.GetMouseButton(1));
    if (name === 'middleButton') return Number(Input.GetMouseButton(2));
    if (name === 'delta') return new Vector2(Input.GetAxis('Mouse X'), Input.GetAxis('Mouse Y'));
    if (name === 'scroll/y') return Input.GetAxis('Mouse ScrollWheel');
  }
  if (device === 'Gamepad') {
    const pads = gamepads();
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

function process(value, processors) {
  for (const processor of processors) {
    const scale = /^scaleVector2\(x=([-\d.]+),y=([-\d.]+)\)$/i.exec(processor.replace(/\s/g, ''));
    if (scale && value instanceof Vector2) value = new Vector2(value.x * Number(scale[1]), value.y * Number(scale[2]));
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

class InputBinding {
  constructor(action, path, composite = false) {
    this.action = action; this.path = path; this.composite = composite;
    this.parts = new Map(); this.processors = []; this.interactions = [];
  }
  With(name, path) {
    const part = name.toLowerCase();
    this.parts.set(part, [...(this.parts.get(part) ?? []), path]);
    return this;
  }
  WithProcessor(processor) { this.processors.push(processor); return this; }
  WithInteraction(interaction) {
    if (!/^(press|hold(\(.*\))?)$/i.test(interaction))
      throw new Error(`Unsupported InputAction interaction: ${interaction}`);
    this.interactions.push(interaction);
    return this;
  }
  read() {
    if (!this.composite) return process(control(this.path), this.processors);
    const part = name => Math.max(0, ...(this.parts.get(name) ?? []).map(path => Number(control(path))));
    if (this.path === 'Axis')
      return process(Math.max(-1, Math.min(1, part('positive') - part('negative'))), this.processors);
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
      record.m_Type ?? record.type ?? InputActionType.Value, null, null, null,
      record.m_ExpectedControlType || record.expectedControlType || null);
    let composite = null;
    for (const item of record.m_SingletonActionBindings ?? record.bindings ?? []) {
      const flags = item.m_Flags ?? item.flags ?? 0;
      const path = item.m_Path ?? item.path;
      const name = item.m_Name ?? item.name;
      if (flags & (1 | 4)) composite = action.AddCompositeBinding(path);
      else if (flags & (2 | 8)) {
        if (!composite) throw new Error(`InputAction ${action.name} has an orphan composite part`);
        composite.With(name, path);
      } else { composite = null; action.AddBinding(path); }
      const binding = composite ?? action.bindings.at(-1);
      if (item.m_Processors || item.processors) binding.WithProcessor(item.m_Processors || item.processors);
      if (item.m_Interactions || item.interactions) binding.WithInteraction(item.m_Interactions || item.interactions);
    }
    if (record.enabled) action.Enable();
    return action;
  }
  toSerialized() {
    const bindings = this.bindings.flatMap(binding => [
      { path: binding.path, flags: binding.composite ? 1 : 0,
        processors: binding.processors[0] ?? '', interactions: binding.interactions[0] ?? '' },
      ...[...binding.parts].flatMap(([name, paths]) => paths.map(path => ({
        name, path, flags: 2
      })))
    ]);
    return { name: this.name, type: this.type, expectedControlType: this.expectedControlType,
      enabled: this.enabled, bindings };
  }
  AddBinding(path) { const binding = new InputBinding(this, path); this.bindings.push(binding); return binding; }
  AddCompositeBinding(path) { const binding = new InputBinding(this, path, true); this.bindings.push(binding); return binding; }
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
    const action = this;
    this[name].invoke({ action, phase, ReadValue(type) { return action.ReadValue(type); },
      get started() { return phase === InputActionPhase.Started; },
      get performed() { return phase === InputActionPhase.Performed; },
      get canceled() { return phase === InputActionPhase.Canceled; } });
  }
  _update(deltaTime = 0) {
    if (!this.enabled) return;
    const values = this.bindings.map(binding => binding.read());
    const vector = this.expectedControlType === 'Vector2' || values.some(value => value instanceof Vector2);
    let activeBinding = null;
    let value = vector ? new Vector2() : 0;
    values.forEach((current, index) => {
      if (magnitude(current) > magnitude(value)) { value = current; activeBinding = this.bindings[index]; }
    });
    const pressed = magnitude(value) >= buttonThreshold;
    const wasPressed = this._pressed;
    this._wasPressed ||= pressed && !wasPressed;
    this._wasReleased ||= !pressed && wasPressed;
    this._value = value;
    const hold = activeBinding?.interactions.find(item => item.startsWith('hold'));
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
