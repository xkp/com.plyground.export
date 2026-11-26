import { Awaitable } from './core-awaitable.js';

const boundMethods = new WeakMap();
const missingPersistentMethods = new WeakMap();

// UnityAction is emitted as a JavaScript function. Delegate BeginInvoke/EndInvoke
// need a separate bridge because functions have no delegate instance methods.
export class UnityAction {
  static BeginInvoke(action, ...values) {
    if (typeof action !== 'function') throw new TypeError('UnityAction.BeginInvoke requires a function');
    const state = values.pop();
    const callback = values.pop();
    const result = {
      AsyncState: state, IsCompleted: false, CompletedSynchronously: false,
      action, error: null,
    };
    result.complete = () => {
      if (result.IsCompleted) return;
      try { action(...values); }
      catch (error) { result.error = error; }
      result.IsCompleted = true;
      if (callback != null) callback(result);
    };
    queueMicrotask(result.complete);
    return result;
  }

  static EndInvoke(action, result) {
    if (result?.action !== action || typeof result.complete !== 'function')
      throw new TypeError('UnityAction.EndInvoke requires this delegate’s async result');
    // Browser JavaScript cannot block the main thread for a delegate. Complete
    // the pending call at EndInvoke if its microtask has not run yet.
    result.complete();
    if (result.error) throw result.error;
  }
}

export const UnityEventCallState = Object.freeze({ Off: 0, EditorAndRuntime: 1, RuntimeOnly: 2 });
export const PersistentListenerMode = Object.freeze({
  EventDefined: 0, Void: 1, Object: 2, Int: 3, Float: 4, String: 5, Bool: 6,
});

function matchesMethodArgument(parameter, argument) {
  const directName = argument?.unityTypeName
    ?? (typeof argument === 'function' && Object.hasOwn(argument, '__unityFullName')
      ? argument.__unityFullName : argument);
  if (parameter === directName) return true;
  let type = typeof argument === 'function' ? argument : argument?.runtimeType ?? argument?.constructor;
  while (typeof type === 'function' && type !== Function.prototype) {
    const fullName = Object.hasOwn(type, '__unityFullName') ? type.__unityFullName : type.name;
    if (parameter === fullName || (!Object.hasOwn(type, '__unityFullName')
      && parameter === `UnityEngine.${type.name}`)) return true;
    type = Object.getPrototypeOf(type);
  }
  return false;
}

export class UnityEventBase {
  static GetValidMethodInfo(target, name, argumentTypes = []) {
    let type = typeof target === 'function' ? target : target?.runtimeType ?? target?.constructor;
    while (type && type !== Object) {
      const signatures = Object.hasOwn(type, '__unityMethodSignatures')
        ? type.__unityMethodSignatures : [];
      const match = signatures.find(method => method.name === name
        && method.parameters.length === argumentTypes.length
        && method.parameters.every((parameter, index) =>
          matchesMethodArgument(parameter, argumentTypes[index])));
      if (match) return { Name: name, DeclaringType: type,
        GetParameters: () => match.parameters.map(ParameterType => ({ ParameterType })) };
      type = Object.getPrototypeOf(type);
    }
    return null;
  }

  ToString() {
    if (this._genericTypeNames?.length) {
      const args = this._genericTypeNames.map(name => ({
        int: 'System.Int32', float: 'System.Single', bool: 'System.Boolean',
        string: 'System.String', double: 'System.Double', long: 'System.Int64',
      })[name] ?? name);
      const generic = `UnityEngine.Events.UnityEvent\`${args.length}`;
      const type = `${generic}[${args.join(',')}]`;
      const qualified = args.map(name => name.startsWith('UnityEngine.')
        ? `[${name}, UnityEngine.CoreModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]`
        : `[${name}, mscorlib, Version=4.0.0.0, `
          + 'Culture=neutral, PublicKeyToken=b77a5c561934e089]').join(',');
      return `${type} ${generic}[${qualified}]`;
    }
    const type = this.constructor.__unityFullName ?? 'UnityEngine.Events.UnityEvent';
    return `${type} ${type}`;
  }
}

/** UnityEngine.Events.UnityEvent's runtime listener surface. */
export class UnityEvent extends UnityEventBase {
  constructor(genericTypeNames = null) {
    super();
    this._genericTypeNames = genericTypeNames;
    this._persistent = [];
    this._runtime = [];
  }

  GetAwaiter(arity = this._genericTypeNames?.length ?? 0) {
    const pending = new Awaitable();
    const listener = (...values) => {
      this.RemoveListener(listener);
      const result = arity === 0 ? undefined : arity === 1 ? values[0]
        : Object.fromEntries(values.slice(0, arity).map((value, index) =>
          [`Item${index + 1}`, value]));
      pending._complete('result', result);
    };
    this.AddListener(listener);
    return pending.GetAwaiter();
  }
  then(resolve, reject) {
    return this.ToPromise().then(resolve, reject);
  }
  ToPromise(arity = this._genericTypeNames?.length ?? 0) {
    const awaiter = this.GetAwaiter(arity);
    return new Promise((accept, fail) => awaiter.OnCompleted(() => {
      try { accept(awaiter.GetResult()); }
      catch (error) { fail(error); }
    }));
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
    if (this._genericTypeNames?.length || !this._runtime.includes(callback))
      this._runtime.push(callback);
  }

  RemoveListener(callback) { this._runtime = this._runtime.filter(listener => listener !== callback); }
  RemoveAllListeners() { this._runtime.length = 0; }

  // Generated setup code can preserve Inspector-authored calls without a generic event serializer.
  _RegisterPersistentListener(target, methodName, args = null,
    state = UnityEventCallState.RuntimeOnly, objectArgumentType = null) {
    if (target == null || typeof methodName !== 'string') {
      throw new TypeError('Persistent UnityEvent listener needs a target and method name');
    }
    this._persistent.push({ target, methodName, args, state, objectArgumentType });
  }

  GetPersistentEventCount() { return this._persistent.length; }
  _persistentAt(index, negativeStateIndex = false) {
    if (!Number.isInteger(index) || index < 0 || index >= this._persistent.length) {
      const error = new RangeError('Persistent UnityEvent listener index is invalid');
      error.name = negativeStateIndex && index < 0
        ? 'IndexOutOfRangeException' : 'ArgumentOutOfRangeException';
      throw error;
    }
    return this._persistent[index];
  }
  GetPersistentTarget(index) { return this._persistentAt(index).target; }
  GetPersistentMethodName(index) { return this._persistentAt(index).methodName; }
  GetPersistentListenerState(index) { return this._persistentAt(index, true).state; }
  SetPersistentListenerState(index, state) {
    this._persistentAt(index).state = state;
  }

  Invoke(...args) {
    const persistent = this._persistent.filter(listener => listener.state !== UnityEventCallState.Off);
    const runtime = [...this._runtime];
    for (const listener of persistent) {
      if (listener.target._destroyed) continue;
      const callArgs = listener.args ?? args;
      let method = listener.target[listener.methodName];
      const declaredSignatures = listener.target.constructor.__unityMethodSignatures
        ?.filter(signature => signature.name === listener.methodName
          && signature.parameters.length === callArgs.length) ?? [];
      if (declaredSignatures.length
        && !Object.hasOwn(listener.target.constructor.prototype, listener.methodName))
        method = undefined;
      if (typeof method !== 'function') {
        if (listener.methodName.startsWith('set_')) {
          const property = listener.methodName.slice(4);
          if (property in listener.target) {
            listener.target[property] = callArgs[0];
            continue;
          }
        }
        const signatures = declaredSignatures;
        const expected = listener.args == null ? this._genericTypeNames : null;
        const exact = expected?.length === callArgs.length
          ? signatures.filter(signature => signature.parameters.every((name, index) =>
            name === expected[index])) : [];
        const matches = exact.length ? exact
          : signatures.filter(signature => signature.parameters.every((name, index) => {
            const value = callArgs[index];
            if (typeof value === 'boolean') return name === 'bool';
            if (typeof value === 'string') return name === 'string';
            if (typeof value === 'number') return name === (Number.isInteger(value) ? 'int' : 'float');
            if (index === 0 && listener.objectArgumentType)
              return name === listener.objectArgumentType.split(',')[0].trim();
            return matchesMethodArgument(name, value);
          }));
        const selected = matches.length === 1 ? matches[0] : signatures.length === 1 ? signatures[0] : null;
        if (selected?.emittedName) method = listener.target[selected.emittedName];
      }
      if (typeof method !== 'function') {
        const type = listener.target?.constructor?.__unityFullName
          ?? listener.target?.constructor?.name ?? typeof listener.target;
        let missing = missingPersistentMethods.get(listener.target);
        if (!missing) missingPersistentMethods.set(listener.target, missing = new Set());
        if (!missing.has(listener.methodName)) {
          missing.add(listener.methodName);
          console.warn(`Preview compatibility: persistent UnityEvent call ${type}.${listener.methodName}`
            + ` (${callArgs.map(value => value == null ? 'null' : typeof value).join(', ')})`
            + ' has no target method; skipped');
        }
        continue;
      }
      const objectArgumentType = listener.objectArgumentType?.split(',')[0].trim();
      if (listener.args?.[0] === null && objectArgumentType
        && objectArgumentType !== 'UnityEngine.Object') {
        const error = new Error(`Object of type 'UnityEngine.Object' cannot be converted to type '${objectArgumentType}'.`);
        error.name = 'ArgumentException';
        throw error;
      }
      method.apply(listener.target, callArgs);
    }
    for (const callback of runtime) callback(...args);
  }
}
