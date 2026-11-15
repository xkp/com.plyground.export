import { Vector2, Vector3 } from './math.js';
import { InputAction } from './input-actions.js';

const held = new Set();
const down = new Set();
const up = new Set();
const mouseHeld = new Set();
const mouseDown = new Set();
const mouseUp = new Set();
const axes = new Map();
let mousePosition = Vector3.zero;

export const KeyCode = globalThis.Object.freeze({
  W: 'KeyW', A: 'KeyA', S: 'KeyS', D: 'KeyD', T: 'KeyT', Space: 'Space',
  LeftControl: 'ControlLeft', RightControl: 'ControlRight',
  LeftAlt: 'AltLeft', RightAlt: 'AltRight',
  LeftShift: 'ShiftLeft', RightShift: 'ShiftRight', Escape: 'Escape',
  Return: 'Enter',
  UpArrow: 'ArrowUp', DownArrow: 'ArrowDown', LeftArrow: 'ArrowLeft', RightArrow: 'ArrowRight',
  Mouse0: 0, Mouse1: 1, Mouse2: 2,
});

const buttonBindings = {
  Fire1: { keys: ['ControlLeft', 'ControlRight'], mouse: [0] },
  Fire2: { keys: ['AltLeft', 'AltRight'], mouse: [1] },
  Fire3: { keys: ['ShiftLeft', 'ShiftRight'], mouse: [2] },
  Jump: { keys: ['Space'], mouse: [] },
  Submit: { keys: ['Enter'], mouse: [] },
  Cancel: { keys: ['Escape'], mouse: [] },
};

function buttonState(name, keys, mouse) {
  const binding = buttonBindings[name];
  if (!binding) return false;
  return binding.keys.some(key => keys.has(key)) || binding.mouse.some(button => mouse.has(button));
}

/** Legacy Unity Input facade. BrowserInput feeds it; tests can inject events. */
export class Input {
  static GetKey(key) { return held.has(key === 13 ? KeyCode.Return : key === 116 ? KeyCode.T : key); }
  static GetKeyDown(key) { return down.has(key === 13 ? KeyCode.Return : key === 116 ? KeyCode.T : key); }
  static GetKeyUp(key) { return up.has(key === 13 ? KeyCode.Return : key === 116 ? KeyCode.T : key); }
  static GetMouseButton(button) { return mouseHeld.has(button); }
  static GetMouseButtonDown(button) { return mouseDown.has(button); }
  static GetMouseButtonUp(button) { return mouseUp.has(button); }
  static GetButton(name) { return buttonState(name, held, mouseHeld); }
  static GetButtonDown(name) { return buttonState(name, down, mouseDown); }
  static GetButtonUp(name) { return buttonState(name, up, mouseUp); }
  static GetAxis(name) { return axes.get(name) ?? 0; }
  static GetAxisRaw(name) { return Input.GetAxis(name); }
  static get mousePosition() { return mousePosition.clone(); }
  static _keyDown(key) { if (!held.has(key)) down.add(key); held.add(key); }
  static _keyUp(key) { held.delete(key); up.add(key); }
  static _mouseDown(button) { if (!mouseHeld.has(button)) mouseDown.add(button); mouseHeld.add(button); }
  static _mouseUp(button) { mouseHeld.delete(button); mouseUp.add(button); }
  static _mousePosition(x, y) { mousePosition = new Vector3(x, y, 0); }
  static _axis(name, value) { axes.set(name, value); }
  static _endFrame() {
    down.clear(); up.clear(); mouseDown.clear(); mouseUp.clear();
    axes.set('Mouse X', 0); axes.set('Mouse Y', 0); axes.set('Mouse ScrollWheel', 0);
  }
  static _reset() { held.clear(); mouseHeld.clear(); axes.clear(); Input._endFrame(); }
}

export class Keyboard {
  static get current() { return this; }
  static get gKey() { return { isPressed: Input.GetKey('KeyG') }; }
}

export class Mouse {
  static get current() { return this; }
  static get position() {
    const value = Input.mousePosition;
    return { value: new Vector2(value.x, value.y) };
  }
}

/** Browser event adapter. Pointer coordinates use Unity's bottom-left pixel origin. */
export class BrowserInput {
  constructor(target = globalThis.window, { pointerTarget = target, mouseDeltaRequiresButton = false } = {}) {
    this.target = target;
    this.pointerTarget = pointerTarget;
    this.mouseDeltaRequiresButton = mouseDeltaRequiresButton;
    this._listeners = [];
  }
  attach() {
    if (!this.target?.addEventListener || !this.pointerTarget?.addEventListener) {
      throw new Error('BrowserInput needs event targets');
    }
    if (this._listeners.length) return;
    const on = (target, type, handler) => {
      target.addEventListener(type, handler);
      this._listeners.push([target, type, handler]);
    };
    on(this.target, 'keydown', event => Input._keyDown(event.code));
    on(this.target, 'keyup', event => Input._keyUp(event.code));
    const unityMouseButton = button => button === 2 ? 1 : button === 1 ? 2 : button;
    on(this.pointerTarget, 'mousedown', event => Input._mouseDown(unityMouseButton(event.button)));
    // Unity's right mouse bindings must remain held instead of opening the
    // browser menu and canceling the pointer interaction.
    on(this.pointerTarget, 'contextmenu', event => event.preventDefault());
    on(this.target, 'mouseup', event => Input._mouseUp(unityMouseButton(event.button)));
    // Unity's locked cursor reports the screen center. Browsers retain the
    // click coordinates under pointer lock, which misdirects JU crosshair rays.
    const lockedPointerPosition = () => {
      const rect = this.pointerTarget.getBoundingClientRect?.();
      Input._mousePosition((rect?.width ?? globalThis.window?.innerWidth ?? 0) / 2,
        (rect?.height ?? globalThis.window?.innerHeight ?? 0) / 2);
    };
    if (globalThis.document?.addEventListener) on(globalThis.document, 'pointerlockchange', () => {
      if (globalThis.document.pointerLockElement === this.pointerTarget) lockedPointerPosition();
    });
    on(this.pointerTarget, 'mousemove', event => {
      const rect = this.pointerTarget.getBoundingClientRect?.();
      if (globalThis.document?.pointerLockElement === this.pointerTarget) lockedPointerPosition();
      else {
        const x = rect ? event.clientX - rect.left : event.clientX;
        const y = rect ? rect.height - (event.clientY - rect.top)
          : (globalThis.window?.innerHeight ?? 0) - event.clientY;
        Input._mousePosition(x, y);
      }
      if (!this.mouseDeltaRequiresButton || mouseHeld.size) {
        Input._axis('Mouse X', Input.GetAxis('Mouse X') + (event.movementX ?? 0));
        Input._axis('Mouse Y', Input.GetAxis('Mouse Y') - (event.movementY ?? 0));
      }
    });
    on(this.pointerTarget, 'wheel', event => {
      Input._axis('Mouse ScrollWheel', Input.GetAxis('Mouse ScrollWheel') - event.deltaY / 100);
    });
    on(this.target, 'blur', () => Input._reset());
  }
  beforeUpdate(deltaTime) {
    Input._axis('Horizontal', Number(Input.GetKey('KeyD') || Input.GetKey('ArrowRight'))
      - Number(Input.GetKey('KeyA') || Input.GetKey('ArrowLeft')));
    Input._axis('Vertical', Number(Input.GetKey('KeyW') || Input.GetKey('ArrowUp'))
      - Number(Input.GetKey('KeyS') || Input.GetKey('ArrowDown')));
    for (const name of ['Fire1', 'Fire2', 'Fire3', 'Jump']) {
      Input._axis(name, Number(Input.GetButton(name)));
    }
    InputAction._updateAll(deltaTime);
  }
  afterLateUpdate() { InputAction._endFrame(); Input._endFrame(); }
  detach() {
    for (const [target, type, handler] of this._listeners) target.removeEventListener(type, handler);
    this._listeners.length = 0;
    Input._reset();
  }
}
