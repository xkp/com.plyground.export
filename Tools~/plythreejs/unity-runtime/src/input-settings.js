// Convert the browser-representable keyboard/mouse subset of Unity's InputManager axes.
const keyNames = {
  space: 'Space', return: 'Enter', enter: 'Enter', escape: 'Escape', tab: 'Tab',
  backspace: 'Backspace', left: 'ArrowLeft', right: 'ArrowRight', up: 'ArrowUp',
  down: 'ArrowDown', 'left ctrl': 'ControlLeft', 'right ctrl': 'ControlRight',
  'left shift': 'ShiftLeft', 'right shift': 'ShiftRight',
  'left alt': 'AltLeft', 'right alt': 'AltRight',
};

function browserControl(name) {
  const token = String(name ?? '').trim().toLowerCase();
  if (!token) return null;
  const mouse = /^mouse ([0-6])$/.exec(token);
  if (mouse) return { mouse: Number(mouse[1]) };
  const joystick = /^joystick(?: (\d+))? button (\d+)$/.exec(token);
  if (joystick) return { gamepad: { padIndex: joystick[1]
    ? Number(joystick[1]) - 1 : null, button: Number(joystick[2]) } };
  const key = keyNames[token] ?? (/^[a-z]$/.test(token) ? `Key${token.toUpperCase()}`
    : /^[0-9]$/.test(token) ? `Digit${token}` : null);
  return key ? { key } : null;
}

export function legacyBindingsFromAxes(records) {
  if (!Array.isArray(records)) throw new TypeError('Legacy input axes must be an array');
  const buttons = {}, axes = {}, diagnostics = [];
  for (const record of records) {
    const name = record?.m_Name ?? record?.name;
    if (typeof name !== 'string' || !name) throw new TypeError('Legacy input axis needs a name');
    const type = Number(record.type ?? 0);
    if (type === 1 && ['Mouse X', 'Mouse Y', 'Mouse ScrollWheel'].includes(name))
      continue; // BrowserInput already populates these standard mouse axes each frame.
    if (type === 1) {
      const source = ['Mouse X', 'Mouse Y', 'Mouse ScrollWheel'][Number(record.axis)];
      if (!source) { diagnostics.push(`${name}: unsupported mouse axis ${record.axis}`); continue; }
      const target = axes[name] ??= { positiveKeys: [], negativeKeys: [],
        positiveMouse: [], negativeMouse: [], positiveGamepad: [], negativeGamepad: [] };
      (target.mouseAxes ??= []).push({ source, sensitivity: Number(record.sensitivity ?? 1),
        invert: Number(record.invert ?? 0) !== 0 });
      continue;
    }
    if (type === 2) {
      const axis = Number(record.axis);
      if (!Number.isInteger(axis) || axis < 0) {
        diagnostics.push(`${name}: joystick axis index is missing`); continue;
      }
      const target = axes[name] ??= { positiveKeys: [], negativeKeys: [],
        positiveMouse: [], negativeMouse: [], positiveGamepad: [], negativeGamepad: [] };
      (target.gamepadAxes ??= []).push({ axis, padIndex: Number(record.joyNum ?? 0)
        ? Number(record.joyNum) - 1 : null, dead: Number(record.dead ?? 0),
      invert: Number(record.invert ?? 0) !== 0 });
      continue;
    }
    if (type !== 0) {
      diagnostics.push(`${name}: Unity input axis type ${type} needs a browser mapping`);
      continue;
    }
    const positiveKeys = [], negativeKeys = [], positiveMouse = [], negativeMouse = [];
    const positiveGamepad = [], negativeGamepad = [];
    for (const [field, target] of [
      ['positiveButton', positiveKeys], ['altPositiveButton', positiveKeys],
      ['negativeButton', negativeKeys], ['altNegativeButton', negativeKeys]]) {
      const raw = record[field];
      if (!raw) continue;
      const control = browserControl(raw);
      if (!control) { diagnostics.push(`${name}: unsupported Unity key ${raw}`); continue; }
      if (control.key) target.push(control.key);
      if (control.mouse != null) (field.includes('Negative') || field === 'negativeButton'
        ? negativeMouse : positiveMouse).push(control.mouse);
      if (control.gamepad) (field.includes('Negative') || field === 'negativeButton'
        ? negativeGamepad : positiveGamepad).push(control.gamepad);
    }
    const button = buttons[name] ??= { keys: [], mouse: [], gamepad: [] };
    button.keys.push(...positiveKeys, ...negativeKeys);
    button.mouse.push(...positiveMouse, ...negativeMouse);
    button.gamepad.push(...positiveGamepad, ...negativeGamepad);
    const axis = axes[name] ??= { positiveKeys: [], negativeKeys: [],
      positiveMouse: [], negativeMouse: [], positiveGamepad: [], negativeGamepad: [],
      gravity: Number(record.gravity ?? 3), sensitivity: Number(record.sensitivity ?? 3),
      dead: Number(record.dead ?? 0), snap: Number(record.snap ?? 0) !== 0 };
    axis.gravity = Number(record.gravity ?? 3); axis.sensitivity = Number(record.sensitivity ?? 3);
    axis.dead = Number(record.dead ?? 0); axis.snap = Number(record.snap ?? 0) !== 0;
    const invert = Number(record.invert ?? 0) !== 0;
    axis.positiveKeys.push(...(invert ? negativeKeys : positiveKeys));
    axis.negativeKeys.push(...(invert ? positiveKeys : negativeKeys));
    axis.positiveMouse.push(...(invert ? negativeMouse : positiveMouse));
    axis.negativeMouse.push(...(invert ? positiveMouse : negativeMouse));
    axis.positiveGamepad.push(...(invert ? negativeGamepad : positiveGamepad));
    axis.negativeGamepad.push(...(invert ? positiveGamepad : negativeGamepad));
  }
  return { buttons, axes, diagnostics };
}
