import { Vector2, Vector3 } from './math.js';
import { InputAction } from './input-actions.js';
import { KeyCode } from './key-code.js';
export { KeyCode } from './key-code.js';

const held = new Set();
const down = new Set();
const up = new Set();
const mouseHeld = new Set();
const mouseDown = new Set();
const mouseUp = new Set();
const gamepadHeld = new Set();
const gamepadDown = new Set();
const gamepadUp = new Set();
const axisSource = new Map();
const axes = new Map();
const rawAxes = new Map();
let mousePosition = Vector3.zero;
let pointerDelta = new Vector2();
const contacts = new Map();
const contactOrder = [];
const touchHistory = new Map();
let touchAvailable = false;
let pointerAvailable = false;
let touchEnabled = true;
const edgeSequence = [];
let inputString = '';
let compositionString = '';
let compositionCursorPos = new Vector2();
let imeCompositionMode = 0;
let pressureAvailable = false;
let stylusAvailable = false;
const penEvents = [];
let lastPenContactEvent = null;
let deviceChangeHandler = null;
export function setInputDeviceChangeHandler(handler) { deviceChangeHandler = handler; }
let rawEventHandler = null;
export function setInputRawEventHandler(handler) { rawEventHandler = handler; }

export const TouchPhase = Object.freeze({ Began: 0, Moved: 1, Stationary: 2, Ended: 3, Canceled: 4 });
export const InputSystemTouchPhase = Object.freeze({ None: 0, Began: 1, Moved: 2,
  Ended: 3, Canceled: 4, Stationary: 5 });
export const TouchType = Object.freeze({ Direct: 0, Indirect: 1, Stylus: 2 });
export const IMECompositionMode = Object.freeze({ Auto: 0, On: 1, Off: 2 });
export const PenEventType = Object.freeze({ NoContact: 0, PenDown: 1, PenUp: 2 });
export const PenStatus = Object.freeze({ None: 0, Contact: 1, Barrel: 2, Inverted: 4, Eraser: 8 });
export class PenData {
  constructor({ position = new Vector2(), deltaPos = new Vector2(), tilt = new Vector2(),
    twist = 0, pressure = 0, penStatus = PenStatus.None,
    contactType = PenEventType.NoContact } = {}) {
    Object.assign(this, { position, deltaPos, tilt, twist, pressure, penStatus, contactType });
  }
}
const copyTouch = touch => ({ ...touch, position: touch.position.clone(),
  deltaPosition: touch.deltaPosition.clone(), rawPosition: touch.rawPosition.clone(),
  startPosition: touch.startPosition.clone(), tilt: touch.tilt.clone() });

const domKeyCodes = new Map([
  ...Array.from({ length: 26 }, (_, index) => [
    `Key${String.fromCharCode(65 + index)}`, KeyCode[String.fromCharCode(65 + index)]]),
  ...Array.from({ length: 10 }, (_, index) => [`Digit${index}`, KeyCode[`Alpha${index}`]]),
  ...Array.from({ length: 10 }, (_, index) => [`Numpad${index}`, KeyCode[`Keypad${index}`]]),
  ...Array.from({ length: 24 }, (_, index) => [`F${index + 1}`, KeyCode[`F${index + 1}`]]),
  ['Space', KeyCode.Space], ['Enter', KeyCode.Return], ['NumpadEnter', KeyCode.KeypadEnter],
  ['Escape', KeyCode.Escape], ['Backspace', KeyCode.Backspace], ['Tab', KeyCode.Tab],
  ['ArrowUp', KeyCode.UpArrow], ['ArrowDown', KeyCode.DownArrow],
  ['ArrowLeft', KeyCode.LeftArrow], ['ArrowRight', KeyCode.RightArrow],
  ['ControlLeft', KeyCode.LeftControl], ['ControlRight', KeyCode.RightControl],
  ['ShiftLeft', KeyCode.LeftShift], ['ShiftRight', KeyCode.RightShift],
  ['AltLeft', KeyCode.LeftAlt], ['AltRight', KeyCode.RightAlt],
  ['MetaLeft', KeyCode.LeftCommand], ['MetaRight', KeyCode.RightCommand],
  ['Insert', KeyCode.Insert], ['Delete', KeyCode.Delete], ['Home', KeyCode.Home],
  ['End', KeyCode.End], ['PageUp', KeyCode.PageUp], ['PageDown', KeyCode.PageDown],
  ['CapsLock', KeyCode.CapsLock], ['NumLock', KeyCode.Numlock],
  ['Backquote', KeyCode.BackQuote], ['Minus', KeyCode.Minus], ['Equal', KeyCode.Equals],
  ['BracketLeft', KeyCode.LeftBracket], ['BracketRight', KeyCode.RightBracket],
  ['Backslash', KeyCode.Backslash], ['Semicolon', KeyCode.Semicolon],
  ['Quote', KeyCode.Quote], ['Comma', KeyCode.Comma], ['Period', KeyCode.Period],
  ['Slash', KeyCode.Slash], ['NumpadDecimal', KeyCode.KeypadPeriod],
  ['NumpadDivide', KeyCode.KeypadDivide], ['NumpadMultiply', KeyCode.KeypadMultiply],
  ['NumpadSubtract', KeyCode.KeypadMinus], ['NumpadAdd', KeyCode.KeypadPlus],
]);
const unityKeyNames = { space: 'Space', return: 'Enter', enter: 'Enter', escape: 'Escape',
  left: 'ArrowLeft', right: 'ArrowRight', up: 'ArrowUp', down: 'ArrowDown',
  leftshift: 'ShiftLeft', rightshift: 'ShiftRight', leftctrl: 'ControlLeft',
  rightctrl: 'ControlRight' };
const normalizeKey = key => {
  if (typeof key !== 'string') return key;
  const code = domKeyCodes.has(key) ? key : unityKeyNames[key.toLowerCase()]
    ?? (/^[a-z]$/i.test(key) ? `Key${key.toUpperCase()}` : key);
  return domKeyCodes.get(code) ?? code;
};
export const browserKeyCode = code => {
  const value = normalizeKey(code);
  return typeof value === 'number' ? value : KeyCode.None;
};
const mouseKey = key => typeof key === 'number' && key >= KeyCode.Mouse0 && key <= KeyCode.Mouse6
  ? key - KeyCode.Mouse0 : -1;
const joystickKey = key => {
  const code = typeof key === 'number' ? key : KeyCode[key];
  if (Number.isInteger(code) && code >= KeyCode.JoystickButton0
    && code <= KeyCode.Joystick8Button19) {
    const offset = code - KeyCode.JoystickButton0;
    return { padIndex: offset < 20 ? null : Math.floor(offset / 20) - 1,
      button: offset % 20 };
  }
  const text = typeof key === 'string'
    ? /^joystick(?: (\d+))? button (\d+)$/i.exec(key) : null;
  return text ? { padIndex: text[1] ? Number(text[1]) - 1 : null,
    button: Number(text[2]) } : null;
};

const buttonBindings = {
  Fire1: { keys: ['ControlLeft', 'ControlRight'], mouse: [0] },
  Fire2: { keys: ['AltLeft', 'AltRight'], mouse: [1] },
  Fire3: { keys: ['ShiftLeft', 'ShiftRight'], mouse: [2] },
  Jump: { keys: ['Space'], mouse: [] },
  Submit: { keys: ['Enter'], mouse: [] },
  Cancel: { keys: ['Escape'], mouse: [] },
};
const axisBindings = {
  Horizontal: { positiveKeys: ['KeyD', 'ArrowRight'], negativeKeys: ['KeyA', 'ArrowLeft'] },
  Vertical: { positiveKeys: ['KeyW', 'ArrowUp'], negativeKeys: ['KeyS', 'ArrowDown'] },
};

const browserGamepads = () => [...(globalThis.navigator?.getGamepads?.() ?? [])].filter(Boolean);
const gamepadButton = (bindings, state) => bindings?.some(({ padIndex, button }) =>
  padIndex == null ? [...state].some(key => key.endsWith(`:${button}`))
    : state.has(`${padIndex}:${button}`)) ?? false;

function buttonState(name, keys, mouse, gamepad) {
  const binding = buttonBindings[name];
  if (!binding) return false;
  return binding.keys.some(key => keys.has(normalizeKey(key)))
    || binding.mouse.some(button => mouse.has(button))
    || gamepadButton(binding.gamepad, gamepad);
}

/** Legacy Unity Input facade. BrowserInput feeds it; tests can inject events. */
export class Input {
  static ConfigureBindings({ buttons = {}, axes: customAxes = {}, replace = false } = {}) {
    if (replace) { for (const name of Object.keys(buttonBindings)) delete buttonBindings[name];
      for (const name of Object.keys(axisBindings)) delete axisBindings[name];
      axes.clear(); rawAxes.clear(); axisSource.clear(); }
    for (const [name, binding] of Object.entries(buttons)) {
      if (!Array.isArray(binding.keys ?? []) || !Array.isArray(binding.mouse ?? [])
        || !Array.isArray(binding.gamepad ?? []))
        throw new TypeError(`Invalid button binding: ${name}`);
      buttonBindings[name] = { keys: [...(binding.keys ?? [])], mouse: [...(binding.mouse ?? [])],
        gamepad: [...(binding.gamepad ?? [])] };
    }
    for (const [name, binding] of Object.entries(customAxes)) {
      if (!Array.isArray(binding.positiveKeys ?? []) || !Array.isArray(binding.negativeKeys ?? []))
        throw new TypeError(`Invalid axis binding: ${name}`);
      axisBindings[name] = { positiveKeys: [...(binding.positiveKeys ?? [])],
        negativeKeys: [...(binding.negativeKeys ?? [])],
        positiveMouse: [...(binding.positiveMouse ?? [])],
        negativeMouse: [...(binding.negativeMouse ?? [])],
        positiveGamepad: [...(binding.positiveGamepad ?? [])],
        negativeGamepad: [...(binding.negativeGamepad ?? [])],
        gamepadAxes: [...(binding.gamepadAxes ?? [])],
        mouseAxes: [...(binding.mouseAxes ?? [])],
        gravity: binding.gravity, sensitivity: binding.sensitivity,
        dead: binding.dead, snap: binding.snap };
    }
  }
  static GetKey(key) { const joystick = joystickKey(key); if (joystick) return gamepadButton([joystick], gamepadHeld);
    const mouse = mouseKey(key); return mouse < 0 ? held.has(normalizeKey(key)) : mouseHeld.has(mouse); }
  static GetKeyDown(key) { const joystick = joystickKey(key); if (joystick) return gamepadButton([joystick], gamepadDown);
    const mouse = mouseKey(key); return mouse < 0 ? down.has(normalizeKey(key)) : mouseDown.has(mouse); }
  static GetKeyUp(key) { const joystick = joystickKey(key); if (joystick) return gamepadButton([joystick], gamepadUp);
    const mouse = mouseKey(key); return mouse < 0 ? up.has(normalizeKey(key)) : mouseUp.has(mouse); }
  static GetMouseButton(button) { return mouseHeld.has(button); }
  static GetMouseButtonDown(button) { return mouseDown.has(button); }
  static GetMouseButtonUp(button) { return mouseUp.has(button); }
  static GetButton(name) { return buttonState(name, held, mouseHeld, gamepadHeld); }
  static GetButtonDown(name) { return buttonState(name, down, mouseDown, gamepadDown); }
  static GetButtonUp(name) { return buttonState(name, up, mouseUp, gamepadUp); }
  static GetAxis(name) { return axes.get(name) ?? 0; }
  static GetAxisRaw(name) { return rawAxes.get(name) ?? Input.GetAxis(name); }
  static get mousePosition() { return mousePosition.clone(); }
  static get mousePositionDelta() { return new Vector3(pointerDelta.x, pointerDelta.y, 0); }
  static get mouseScrollDelta() { return new Vector2(0, axes.get('Mouse ScrollWheel') ?? 0); }
  static get touchCount() { return contactOrder.length; }
  static get touches() { return contactOrder.map(id => copyTouch(contacts.get(id))); }
  static GetTouch(index) {
    if (!Number.isInteger(index) || index < 0 || index >= Input.touchCount)
      throw new RangeError(`Touch index ${index} is out of range`);
    return copyTouch(contacts.get(contactOrder[index]));
  }
  static get touchSupported() { return touchAvailable || (globalThis.navigator?.maxTouchPoints ?? 0) > 0; }
  static get touchPressureSupported() { return pressureAvailable; }
  static get stylusTouchSupported() { return stylusAvailable; }
  static get penEventCount() { return penEvents.length; }
  static GetPenEvent(index) {
    if (!Number.isInteger(index) || index < 0 || index >= penEvents.length)
      throw new RangeError(`Pen event index ${index} is out of range`);
    return penEvents[index];
  }
  static GetLastPenContactEvent() { return lastPenContactEvent ?? new PenData(); }
  static ClearLastPenContactEvent() { lastPenContactEvent = null; }
  static get multiTouchEnabled() { return touchEnabled; }
  static set multiTouchEnabled(value) { touchEnabled = Boolean(value); }
  static get mousePresent() { return pointerAvailable; }
  static GetJoystickNames() { return [...(globalThis.navigator?.getGamepads?.() ?? [])]
    .filter(Boolean).map(pad => pad.id ?? 'Gamepad'); }
  static get anyKey() { return held.size > 0 || mouseHeld.size > 0 || gamepadHeld.size > 0; }
  static get anyKeyDown() { return down.size > 0 || mouseDown.size > 0 || gamepadDown.size > 0; }
  static get inputString() { return inputString; }
  static get compositionString() { return compositionString; }
  static get imeCompositionMode() { return imeCompositionMode; }
  static set imeCompositionMode(value) {
    if (!Object.values(IMECompositionMode).includes(value)) throw new RangeError('Invalid IME composition mode');
    imeCompositionMode = value;
    if (value === IMECompositionMode.Off) compositionString = '';
  }
  static get compositionCursorPos() { return compositionCursorPos.clone(); }
  static set compositionCursorPos(value) { compositionCursorPos = value?.clone?.() ?? new Vector2(value?.x ?? 0, value?.y ?? 0); }
  static get _edgeSequence() { return edgeSequence; }
  static get _touchHistory() { return touchHistory; }
  static _text(value) { inputString += value; }
  static _composition(value) { compositionString = value; }
  static _penEvent(phase, touch, event) {
    if (phase !== TouchPhase.Began && phase !== TouchPhase.Ended && phase !== TouchPhase.Canceled)
      return;
    const status = (phase === TouchPhase.Began ? PenStatus.Contact : PenStatus.None)
      | ((event.buttons ?? 0) & 2 ? PenStatus.Barrel : 0)
      | (event.button === 5 ? PenStatus.Eraser : 0);
    const record = new PenData({ position: touch.position.clone(), deltaPos: touch.deltaPosition.clone(),
      tilt: new Vector2((event.tiltX ?? 0) * Math.PI / 180,
        (event.tiltY ?? 0) * Math.PI / 180),
      twist: (event.twist ?? 0) * Math.PI / 180, pressure: touch.pressure,
      penStatus: status, contactType: phase === TouchPhase.Began
        ? PenEventType.PenDown : PenEventType.PenUp });
    penEvents.push(record); lastPenContactEvent = record;
  }
  static ResetInputAxes() {
    held.clear(); mouseHeld.clear(); down.clear(); up.clear(); mouseDown.clear(); mouseUp.clear();
    axes.clear(); rawAxes.clear(); pointerDelta = new Vector2(); edgeSequence.length = 0;
  }
  static _keyDown(key) { const code = key; key = normalizeKey(key); if (!held.has(key)) { down.add(key); edgeSequence.push({ kind: 'key', key, code, down: true }); } held.add(key); }
  static _keyUp(key) { const code = key; key = normalizeKey(key); if (held.has(key)) { edgeSequence.push({ kind: 'key', key, code, down: false }); up.add(key); } held.delete(key); }
  static _mouseDown(button) { pointerAvailable = true; if (!mouseHeld.has(button)) { mouseDown.add(button); edgeSequence.push({ kind: 'mouse', button, down: true }); } mouseHeld.add(button); }
  static _mouseUp(button) { if (mouseHeld.has(button)) { edgeSequence.push({ kind: 'mouse', button, down: false }); mouseUp.add(button); } mouseHeld.delete(button); }
  static _mousePosition(x, y) { mousePosition = new Vector3(x, y, 0); pointerAvailable = true; }
  static _pointerDelta(x, y) { pointerDelta.add(new Vector2(x, y)); }
  static _touch(id, phase, x, y, { pressure = 1, radius = 0, type = TouchType.Direct,
    time = 0, tiltX = 0, tiltY = 0, twist = 0 } = {}) {
    touchAvailable = true;
    const previous = contacts.get(id);
    if (!previous && phase !== TouchPhase.Began) return;
    if (!previous && !touchEnabled && contactOrder.length) return;
    if (previous && (previous.phase === TouchPhase.Ended || previous.phase === TouchPhase.Canceled)) return;
    const position = new Vector2(x, y);
    const deltaPosition = previous ? position.clone().sub(previous.position) : new Vector2();
    const touch = { fingerId: id, position, rawPosition: position.clone(), deltaPosition,
      deltaTime: previous ? Math.max(0, time - previous.time) : 0,
      phase, type, pressure, radius, startPosition: previous?.startPosition?.clone() ?? position.clone(),
      startTime: previous?.startTime ?? time, time, tapCount: 1,
      tap: phase === TouchPhase.Ended && !!previous
        && position.distanceTo(previous.startPosition) <= 20
        && time - previous.startTime <= 0.5,
      tilt: new Vector2(tiltX, tiltY), twist };
    contacts.set(id, touch);
    const history = touchHistory.get(id) ?? [];
    history.push(copyTouch(touch));
    if (history.length > 32) history.shift();
    touchHistory.set(id, history);
    if (!previous) contactOrder.push(id);
    if (phase === TouchPhase.Began || phase === TouchPhase.Ended || phase === TouchPhase.Canceled)
      edgeSequence.push({ kind: 'touch', id, down: phase === TouchPhase.Began, type });
  }
  static _cancelTouches() {
    for (const touch of contacts.values())
      if (touch.phase !== TouchPhase.Ended && touch.phase !== TouchPhase.Canceled) {
        touch.phase = TouchPhase.Canceled;
        touchHistory.get(touch.fingerId)?.push(copyTouch(touch));
        edgeSequence.push({ kind: 'touch', id: touch.fingerId, down: false, type: touch.type });
      }
  }
  static _cancelTouch(id) {
    const touch = contacts.get(id);
    if (touch && touch.phase !== TouchPhase.Ended && touch.phase !== TouchPhase.Canceled) {
      touch.phase = TouchPhase.Canceled;
      touchHistory.get(id)?.push(copyTouch(touch));
      edgeSequence.push({ kind: 'touch', id, down: false, type: touch.type });
    }
  }
  static _releaseAll() {
    for (const key of held) up.add(key);
    for (const button of mouseHeld) mouseUp.add(button);
    for (const key of held) edgeSequence.push({ kind: 'key', key, down: false });
    for (const button of mouseHeld) edgeSequence.push({ kind: 'mouse', button, down: false });
    held.clear(); mouseHeld.clear(); Input._cancelTouches();
    axes.set('Mouse X', 0); axes.set('Mouse Y', 0); axes.set('Mouse ScrollWheel', 0);
    pointerDelta = new Vector2(); compositionString = '';
  }
  static _axis(name, value) { axes.set(name, value); }
  static _endFrame() {
    down.clear(); up.clear(); mouseDown.clear(); mouseUp.clear();
    gamepadDown.clear(); gamepadUp.clear();
    axes.set('Mouse X', 0); axes.set('Mouse Y', 0); axes.set('Mouse ScrollWheel', 0);
    pointerDelta = new Vector2();
    edgeSequence.length = 0; inputString = '';
    penEvents.length = 0;
    for (const id of [...contactOrder]) {
      const touch = contacts.get(id);
      if (touch.phase === TouchPhase.Ended || touch.phase === TouchPhase.Canceled) {
        contacts.delete(id); contactOrder.splice(contactOrder.indexOf(id), 1);
      } else { touch.phase = TouchPhase.Stationary; touch.deltaPosition = new Vector2(); touch.deltaTime = 0; }
    }
  }
  static _reset() {
    held.clear(); mouseHeld.clear(); axes.clear(); rawAxes.clear();
    gamepadHeld.clear(); gamepadDown.clear(); gamepadUp.clear(); axisSource.clear();
    contacts.clear(); touchHistory.clear();
    contactOrder.length = 0;
    compositionString = '';
    lastPenContactEvent = null;
    Input._endFrame();
  }
}

export class InputDevice {}

export class Keyboard extends InputDevice {
  static get current() {
    return this._current ??= new Proxy(new Keyboard(), { get(target, name) {
      if (typeof name !== 'string') return Reflect.get(target, name);
      const token = name.endsWith('Key') ? name.slice(0, -3) : '';
      const aliases = { space: 'Space', enter: 'Enter', escape: 'Escape',
        tab: 'Tab', backspace: 'Backspace', leftShift: 'ShiftLeft',
        rightShift: 'ShiftRight', leftCtrl: 'ControlLeft',
        rightCtrl: 'ControlRight', leftAlt: 'AltLeft', rightAlt: 'AltRight',
        upArrow: 'ArrowUp', downArrow: 'ArrowDown', leftArrow: 'ArrowLeft',
        rightArrow: 'ArrowRight', pageUp: 'PageUp', pageDown: 'PageDown',
        capsLock: 'CapsLock', home: 'Home', end: 'End', delete: 'Delete',
        insert: 'Insert', minus: 'Minus', equals: 'Equal',
        leftBracket: 'BracketLeft', rightBracket: 'BracketRight',
        backslash: 'Backslash', semicolon: 'Semicolon', quote: 'Quote',
        comma: 'Comma', period: 'Period', slash: 'Slash' };
      const code = /^[a-z]$/i.test(token) ? `Key${token.toUpperCase()}`
        : /^[0-9]$/.test(token) ? `Digit${token}`
          : /^f(?:[1-9]|1[0-9]|2[0-4])$/i.test(token) ? token.toUpperCase()
            : /^numpad[0-9]$/i.test(token) ? `Numpad${token.at(-1)}` : aliases[token];
      if (code) return { isPressed: Input.GetKey(code),
        wasPressedThisFrame: Input.GetKeyDown(code),
        wasReleasedThisFrame: Input.GetKeyUp(code) };
      if (token) throw new Error(`Unsupported browser keyboard control: ${name}`);
      return Reflect.get(target, name);
    } });
  }
  get gKey() { return Keyboard.gKey; }
  static get gKey() { return { isPressed: Input.GetKey('KeyG') }; }
}

const controlValue = value => ({ value, ReadValue() { return this.value; } });
const touchControl = touch => ({
  touchId: controlValue(touch?.fingerId ?? 0),
  position: controlValue(touch?.position ?? new Vector2()),
  delta: controlValue(touch?.deltaPosition ?? new Vector2()),
  phase: controlValue(touch?.phase ?? TouchPhase.Ended),
  pressure: controlValue(touch?.pressure ?? 0),
  tilt: controlValue(touch?.tilt ?? new Vector2()),
  twist: controlValue(touch?.twist ?? 0),
  press: { isPressed: !!touch && touch.phase !== TouchPhase.Ended
    && touch.phase !== TouchPhase.Canceled },
});

export class Pointer extends InputDevice {
  static get current() { return Input.mousePresent ? Mouse.current : Touchscreen.current; }
  get position() { return Pointer.position; }
  get delta() { return Pointer.delta; }
  get press() { return Pointer.press; }
  static get position() {
    const value = Input.mousePosition;
    return controlValue(new Vector2(value.x, value.y));
  }
  static get delta() {
    const value = Input.mousePositionDelta;
    return controlValue(new Vector2(value.x, value.y));
  }
  static get press() { return { isPressed: Input.GetMouseButton(0) }; }
}

export class Mouse extends Pointer {
  static get current() { return this._current ??= new Mouse(); }
  get leftButton() { return Mouse.leftButton; }
  get rightButton() { return Mouse.rightButton; }
  get middleButton() { return Mouse.middleButton; }
  get scroll() { return Mouse.scroll; }
  static get leftButton() { return { isPressed: Input.GetMouseButton(0),
    wasPressedThisFrame: Input.GetMouseButtonDown(0),
    wasReleasedThisFrame: Input.GetMouseButtonUp(0) }; }
  static get rightButton() { return { isPressed: Input.GetMouseButton(1),
    wasPressedThisFrame: Input.GetMouseButtonDown(1),
    wasReleasedThisFrame: Input.GetMouseButtonUp(1) }; }
  static get middleButton() { return { isPressed: Input.GetMouseButton(2),
    wasPressedThisFrame: Input.GetMouseButtonDown(2),
    wasReleasedThisFrame: Input.GetMouseButtonUp(2) }; }
  static get scroll() { return controlValue(Input.mouseScrollDelta); }
}

export class Touchscreen extends Pointer {
  static get current() { return Input.touchSupported ? (this._current ??= new Touchscreen()) : null; }
  get primaryTouch() { return Touchscreen.primaryTouch; }
  get touches() { return Touchscreen.touches; }
  get position() { return Touchscreen.position; }
  get delta() { return Touchscreen.delta; }
  get press() { return Touchscreen.press; }
  static get primaryTouch() { return touchControl(Input.touches[0]); }
  static get touches() { return Input.touches.map(touchControl); }
  static get position() { return this.primaryTouch.position; }
  static get delta() { return this.primaryTouch.delta; }
  static get press() { return this.primaryTouch.press; }
}

const enhancedPhase = [1, 2, 5, 3, 4];
export class EnhancedTouchSupport {
  static _enables = 0;
  static get enabled() { return this._enables > 0; }
  static Enable() { this._enables++; }
  static Disable() { this._enables = Math.max(0, this._enables - 1); }
}

export class EnhancedTouchFinger {
  constructor(id, index) { this._id = id; this.index = index; }
  get currentTouch() { return new EnhancedTouchTouch(Input.touches.find(touch =>
    touch.fingerId === this._id)); }
  get isActive() { return this.currentTouch.inProgress; }
  get history() { return (Input._touchHistory.get(this._id) ?? []).map(touch =>
    new EnhancedTouchTouch(touch)); }
}

export class EnhancedTouchTouch {
  constructor(touch = null) { this._touch = touch; }
  get valid() { return !!this._touch; }
  get touchId() { return this._touch?.fingerId ?? 0; }
  get phase() { return this._touch ? enhancedPhase[this._touch.phase] : 0; }
  get began() { return this.phase === 1; }
  get ended() { return this.phase === 3 || this.phase === 4; }
  get inProgress() { return this.valid && !this.ended; }
  get screenPosition() { return this._touch?.position.clone() ?? new Vector2(); }
  get delta() { return this._touch?.deltaPosition.clone() ?? new Vector2(); }
  get pressure() { return this._touch?.pressure ?? 0; }
  get finger() { return new EnhancedTouchFinger(this.touchId,
    Input.touches.findIndex(touch => touch.fingerId === this.touchId)); }
  get history() { return this.finger.history; }
  static get activeTouches() { return EnhancedTouchSupport.enabled
    ? Input.touches.map(touch => new EnhancedTouchTouch(touch)) : []; }
  static get fingers() { return EnhancedTouchSupport.enabled
    ? Input.touches.map((touch, index) => new EnhancedTouchFinger(touch.fingerId, index)) : []; }
  static get activeFingers() { return this.fingers.filter(finger => finger.isActive); }
}

/** Browser event adapter. Pointer coordinates use Unity's bottom-left pixel origin. */
export class BrowserInput {
  constructor(target = globalThis.window, { pointerTarget = target,
    mouseDeltaRequiresButton = false, coordinateSize = null } = {}) {
    this.target = target;
    this.pointerTarget = pointerTarget;
    this.mouseDeltaRequiresButton = mouseDeltaRequiresButton;
    this.coordinateSize = coordinateSize;
    this._listeners = [];
    this._lastContactAt = -Infinity;
    this._frameActive = false;
    this._queued = [];
    this._uiRoots = new Set();
    this._seenContacts = new WeakSet();
  }
  addUIRoot(root) {
    if (!root?.addEventListener || this._uiRoots.has(root)) return;
    this._uiRoots.add(root);
    this._bindUIRoot?.(root);
  }
  removeUIRoot(root) {
    this._uiRoots.delete(root);
    this._listeners = this._listeners.filter(([target, type, listener, capture]) => {
      if (target !== root) return true;
      target.removeEventListener(type, listener, capture);
      return false;
    });
  }
  attach() {
    if (!this.target?.addEventListener || !this.pointerTarget?.addEventListener) {
      throw new Error('BrowserInput needs event targets');
    }
    if (this._listeners.length) return;
    if (this.pointerTarget.style) {
      this._previousTouchAction = this.pointerTarget.style.touchAction;
      this.pointerTarget.style.touchAction = 'none';
    }
    const on = (target, type, handler, immediate = false, capture = false) => {
      const listener = event => {
        if (this._frameActive && !immediate) {
          if (target === this.pointerTarget
            && (type.startsWith('pointer') || type.startsWith('touch')) && event.cancelable)
            event.preventDefault?.();
          this._queued.push([handler, event]);
        } else handler(event);
      };
      target.addEventListener(type, listener, capture);
      this._listeners.push([target, type, listener, capture]);
    };
    on(this.target, 'keydown', event => {
      if (event.code) Input._keyDown(event.code);
      rawEventHandler?.(event, 'keyboard');
    });
    on(this.target, 'keyup', event => {
      if (event.code) Input._keyUp(event.code);
      rawEventHandler?.(event, 'keyboard');
    });
    on(this.target, 'gamepadconnected', event => deviceChangeHandler?.(event.gamepad, true));
    on(this.target, 'gamepaddisconnected', event => deviceChangeHandler?.(event.gamepad, false));
    on(this.target, 'beforeinput', event => {
      if (Input.imeCompositionMode === IMECompositionMode.Off
        && event.inputType === 'insertFromComposition') return;
      if ((event.inputType === 'insertText' || event.inputType === 'insertFromComposition')
        && typeof event.data === 'string') Input._text(event.data);
    });
    on(this.target, 'compositionstart', event => {
      if (Input.imeCompositionMode !== IMECompositionMode.Off) Input._composition(event.data ?? '');
    });
    on(this.target, 'compositionupdate', event => {
      if (Input.imeCompositionMode !== IMECompositionMode.Off) Input._composition(event.data ?? '');
    });
    on(this.target, 'compositionend', () => Input._composition(''));
    const unityMouseButton = button => button === 2 ? 1 : button === 1 ? 2 : button;
    const compatibilityMouse = event => event.sourceCapabilities?.firesTouchEvents
      ?? Date.now() - this._lastContactAt < 700;
    const mouseDown = event => {
      if (!compatibilityMouse(event)) Input._mouseDown(unityMouseButton(event.button));
    };
    on(this.pointerTarget, 'mousedown', mouseDown);
    // Unity's right mouse bindings must remain held instead of opening the
    // browser menu and canceling the pointer interaction.
    on(this.pointerTarget, 'contextmenu', event => event.preventDefault());
    on(this.target, 'mouseup', event => {
      const button = unityMouseButton(event.button);
      if (!compatibilityMouse(event) || mouseHeld.has(button)) Input._mouseUp(button);
    });
    const coordinates = (clientX, clientY) => {
      const rect = this.pointerTarget.getBoundingClientRect?.();
      const size = this.coordinateSize?.();
      const x = rect ? clientX - rect.left : clientX;
      const y = rect ? rect.height - (clientY - rect.top)
        : (globalThis.window?.innerHeight ?? 0) - clientY;
      return new Vector2(rect?.width && size?.width ? x * size.width / rect.width : x,
        rect?.height && size?.height ? y * size.height / rect.height : y);
    };
    const contact = (event, phase, captureTarget = this.pointerTarget) => {
      if (event.pointerType !== 'touch' && event.pointerType !== 'pen') return;
      if (this._seenContacts.has(event)) return;
      this._seenContacts.add(event);
      this._lastContactAt = Date.now();
      const position = coordinates(event.clientX, event.clientY);
      Input._touch(event.pointerId, phase, position.x, position.y, {
        type: event.pointerType === 'pen' ? TouchType.Stylus : TouchType.Direct,
        pressure: event.pressure ?? (phase === TouchPhase.Ended ? 0 : 1),
        radius: ((event.width ?? 0) + (event.height ?? 0)) / 4,
        time: (event.timeStamp ?? 0) / 1000,
        tiltX: event.tiltX ?? 0, tiltY: event.tiltY ?? 0, twist: event.twist ?? 0,
      });
      if (typeof event.pressure === 'number') pressureAvailable = true;
      if (event.pointerType === 'pen') {
        stylusAvailable = true;
        const touch = contacts.get(event.pointerId);
        if (touch) Input._penEvent(phase, touch, event);
      }
      try {
        if (phase === TouchPhase.Began) captureTarget?.setPointerCapture?.(event.pointerId);
        if (phase === TouchPhase.Ended || phase === TouchPhase.Canceled)
          captureTarget?.releasePointerCapture?.(event.pointerId);
      } catch {
        // A synthetic event or a detached target may have no active capture.
      }
      if (captureTarget && event.cancelable) event.preventDefault?.();
    };
    on(this.pointerTarget, 'pointerdown', event => contact(event, TouchPhase.Began));
    on(this.pointerTarget, 'pointermove', event => contact(event, TouchPhase.Moved));
    on(this.pointerTarget, 'pointerup', event => contact(event, TouchPhase.Ended));
    on(this.pointerTarget, 'pointercancel', event => contact(event, TouchPhase.Canceled));
    on(this.pointerTarget, 'lostpointercapture', event => Input._cancelTouch(event.pointerId));
    if (this.target !== this.pointerTarget) {
      on(this.target, 'pointerup', event => contact(event, TouchPhase.Ended));
      on(this.target, 'pointercancel', event => contact(event, TouchPhase.Canceled));
    }
    let touchContact = null;
    if (!globalThis.PointerEvent) {
      touchContact = (event, phase, preventBrowser = true) => {
        this._lastContactAt = Date.now();
        for (const touch of event.changedTouches ?? []) {
          const position = coordinates(touch.clientX, touch.clientY);
          Input._touch(touch.identifier, phase, position.x, position.y, {
            pressure: touch.force ?? (phase === TouchPhase.Ended ? 0 : 1),
            radius: ((touch.radiusX ?? 0) + (touch.radiusY ?? 0)) / 2,
            time: (event.timeStamp ?? 0) / 1000,
          });
        }
        if (preventBrowser && event.cancelable) event.preventDefault?.();
      };
      on(this.pointerTarget, 'touchstart', event => touchContact(event, TouchPhase.Began));
      on(this.pointerTarget, 'touchmove', event => touchContact(event, TouchPhase.Moved));
      on(this.pointerTarget, 'touchend', event => touchContact(event, TouchPhase.Ended));
      on(this.pointerTarget, 'touchcancel', event => touchContact(event, TouchPhase.Canceled));
    }
    // Unity's locked cursor reports the screen center. Browsers retain the
    // click coordinates under pointer lock, which misdirects JU crosshair rays.
    const lockedPointerPosition = () => {
      const rect = this.pointerTarget.getBoundingClientRect?.();
      const size = this.coordinateSize?.() ?? rect;
      Input._mousePosition((size?.width ?? globalThis.window?.innerWidth ?? 0) / 2,
        (size?.height ?? globalThis.window?.innerHeight ?? 0) / 2);
    };
    if (globalThis.document?.addEventListener) on(globalThis.document, 'pointerlockchange', () => {
      if (globalThis.document.pointerLockElement === this.pointerTarget) lockedPointerPosition();
    });
    const mouseMove = event => {
      if (compatibilityMouse(event)) return;
      if (globalThis.document?.pointerLockElement === this.pointerTarget) lockedPointerPosition();
      else {
        const position = coordinates(event.clientX, event.clientY);
        Input._mousePosition(position.x, position.y);
      }
      if (!this.mouseDeltaRequiresButton || mouseHeld.size) {
        Input._axis('Mouse X', Input.GetAxis('Mouse X') + (event.movementX ?? 0));
        Input._axis('Mouse Y', Input.GetAxis('Mouse Y') - (event.movementY ?? 0));
        Input._pointerDelta(event.movementX ?? 0, -(event.movementY ?? 0));
      }
    };
    on(this.pointerTarget, 'mousemove', mouseMove);
    const wheel = (event, preventBrowser = true) => {
      const divisor = event.deltaMode === 1 ? 3 : event.deltaMode === 2 ? 1 : 100;
      Input._axis('Mouse ScrollWheel', Input.GetAxis('Mouse ScrollWheel') - event.deltaY / divisor);
      if (preventBrowser && event.cancelable) event.preventDefault?.();
    };
    on(this.pointerTarget, 'wheel', wheel);
    this._bindUIRoot = root => {
      on(root, 'mousedown', mouseDown, false, true);
      on(root, 'mousemove', mouseMove, false, true);
      on(root, 'wheel', event => wheel(event, false), false, true);
      for (const [type, phase] of [['pointerdown', TouchPhase.Began],
        ['pointermove', TouchPhase.Moved], ['pointerup', TouchPhase.Ended],
        ['pointercancel', TouchPhase.Canceled]])
        on(root, type, event => contact(event, phase, null), false, true);
      if (touchContact) for (const [type, phase] of [['touchstart', TouchPhase.Began],
        ['touchmove', TouchPhase.Moved], ['touchend', TouchPhase.Ended],
        ['touchcancel', TouchPhase.Canceled]])
        on(root, type, event => touchContact(event, phase, false), false, true);
    };
    for (const root of this._uiRoots) this._bindUIRoot(root);
    on(this.target, 'blur', () => Input._releaseAll(), true);
    if (globalThis.document?.addEventListener)
      on(globalThis.document, 'visibilitychange', () => {
        if (globalThis.document.hidden) Input._releaseAll();
      }, true);
  }
  beforeUpdate(deltaTime) {
    const queued = this._queued.splice(0);
    for (const [handler, event] of queued) handler(event);
    this._frameActive = true;
    const previousButtons = new Set(gamepadHeld);
    gamepadHeld.clear(); gamepadDown.clear(); gamepadUp.clear();
    const pads = browserGamepads();
    for (const pad of pads) for (let button = 0; button < (pad.buttons?.length ?? 0); button++) {
      if ((pad.buttons[button]?.value ?? Number(pad.buttons[button]?.pressed ?? false)) >= 0.5)
        gamepadHeld.add(`${pad.index}:${button}`);
    }
    for (const key of gamepadHeld) if (!previousButtons.has(key)) gamepadDown.add(key);
    for (const key of previousButtons) if (!gamepadHeld.has(key)) gamepadUp.add(key);
    for (const [name, binding] of Object.entries(axisBindings)) {
      const digital = Number(binding.positiveKeys.some(key => Input.GetKey(key))
        || binding.positiveMouse?.some(button => Input.GetMouseButton(button))
        || gamepadButton(binding.positiveGamepad, gamepadHeld) || false)
        - Number(binding.negativeKeys.some(key => Input.GetKey(key))
          || binding.negativeMouse?.some(button => Input.GetMouseButton(button))
          || gamepadButton(binding.negativeGamepad, gamepadHeld) || false);
      let analog = 0;
      for (const source of binding.gamepadAxes ?? []) for (const pad of pads) {
        if (source.padIndex != null && source.padIndex !== pad.index) continue;
        const value = (pad.axes?.[source.axis] ?? 0) * (source.invert ? -1 : 1);
        if (Math.abs(value) >= source.dead && Math.abs(value) > Math.abs(analog)) analog = value;
      }
      // The browser writes these device deltas before Update. A legacy Unity
      // axis with the same name may also bind a gamepad stick; its zero value
      // must not erase the mouse input used by InputActions.
      let pointer = name === 'Mouse X' || name === 'Mouse Y'
        || name === 'Mouse ScrollWheel' ? Input.GetAxis(name) : 0;
      for (const source of binding.mouseAxes ?? []) {
        const value = Input.GetAxis(source.source) * source.sensitivity * (source.invert ? -1 : 1);
        if (Math.abs(value) > Math.abs(pointer)) pointer = value;
      }
      let raw = digital, source = 'digital';
      if (Math.abs(analog) > Math.abs(raw)) { raw = analog; source = 'gamepad'; }
      if (Math.abs(pointer) > Math.abs(raw)) { raw = pointer; source = 'pointer'; }
      if (raw === 0 && axisSource.get(name) !== 'digital') source = axisSource.get(name) ?? source;
      rawAxes.set(name, raw);
      if (source !== 'digital') { axisSource.set(name, source); Input._axis(name, raw); continue; }
      axisSource.set(name, 'digital');
      if (!Number.isFinite(binding.sensitivity) || !Number.isFinite(binding.gravity)) {
        Input._axis(name, raw); continue;
      }
      let current = Input.GetAxis(name);
      if (binding.snap && raw && Math.sign(raw) !== Math.sign(current)) current = 0;
      const speed = (raw ? binding.sensitivity : binding.gravity) * deltaTime;
      const next = current + Math.max(-speed, Math.min(speed, raw - current));
      Input._axis(name, Math.abs(next) < (binding.dead ?? 0) ? 0 : next);
    }
    for (const name of Object.keys(buttonBindings)) {
      if (!Object.hasOwn(axisBindings, name)) Input._axis(name, Number(Input.GetButton(name)));
    }
    InputAction._updateAll(deltaTime);
  }
  afterLateUpdate() { InputAction._endFrame(); Input._endFrame(); this._frameActive = false; }
  detach() {
    for (const [target, type, handler, capture] of this._listeners)
      target.removeEventListener(type, handler, capture);
    this._listeners.length = 0;
    this._queued.length = 0; this._frameActive = false;
    this._bindUIRoot = null;
    this._seenContacts = new WeakSet();
    if (this.pointerTarget.style) this.pointerTarget.style.touchAction = this._previousTouchAction ?? '';
    Input._reset();
  }
}
