const boundMethods = new WeakMap();

/** UnityEngine.Events.UnityEvent's runtime listener surface. */
export class UnityEvent {
  constructor() {
    this._persistent = [];
    this._runtime = new Set();
  }

  static Bind(target, methodName) {
    if (target == null || (typeof target !== 'object' && typeof target !== 'function')) {
      throw new TypeError('UnityEvent.Bind requires an object target');
    }
    let methods = boundMethods.get(target);
    if (!methods) { methods = new Map(); boundMethods.set(target, methods); }
    if (!methods.has(methodName)) {
      methods.set(methodName, (...args) => {
        if (target._destroyed) return;
        const method = target[methodName];
        if (typeof method !== 'function') throw new TypeError(`UnityEvent target method is missing: ${methodName}`);
        return method.apply(target, args);
      });
    }
    return methods.get(methodName);
  }

  AddListener(callback) {
    if (typeof callback !== 'function') throw new TypeError('UnityEvent.AddListener requires a function');
    this._runtime.add(callback);
  }

  RemoveListener(callback) { this._runtime.delete(callback); }
  RemoveAllListeners() { this._runtime.clear(); }

  // Generated setup code can preserve Inspector-authored calls without a generic event serializer.
  _RegisterPersistentListener(target, methodName, args = null) {
    if (target == null || typeof methodName !== 'string') {
      throw new TypeError('Persistent UnityEvent listener needs a target and method name');
    }
    this._persistent.push({ target, methodName, args });
  }

  GetPersistentEventCount() { return this._persistent.length; }
  GetPersistentTarget(index) { return this._persistent[index]?.target ?? null; }
  GetPersistentMethodName(index) { return this._persistent[index]?.methodName ?? ''; }

  Invoke(...args) {
    for (const listener of [...this._persistent]) {
      if (listener.target._destroyed) continue;
      const method = listener.target[listener.methodName];
      if (typeof method !== 'function') {
        throw new TypeError(`Persistent UnityEvent target method is missing: ${listener.methodName}`);
      }
      method.apply(listener.target, listener.args ?? args);
    }
    for (const callback of [...this._runtime]) callback(...args);
  }
}
