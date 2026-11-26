function invalidCompletion() {
  const error = new Error('Awaitable has already been completed.');
  error.name = 'InvalidOperationException';
  return error;
}

function canceledCompletion() {
  const error = new Error('The operation was canceled.');
  error.name = 'OperationCanceledException';
  return error;
}

export class Awaitable {
  static _runtimeScheduler = null;
  static _mainThreadContinuation = null;
  static _schedule(kind, seconds = 0, token = null) {
    if (!this._runtimeScheduler) throw new Error('Awaitable scheduling requires a UnityRuntime');
    return this._runtimeScheduler(kind, seconds, token);
  }
  static NextFrameAsync(token = null) { return this._schedule('next', 0, token); }
  static FixedUpdateAsync(token = null) { return this._schedule('fixed', 0, token); }
  static EndOfFrameAsync(token = null) { return this._schedule('end', 0, token); }
  static MainThreadAsync() {
    const pending = new Awaitable();
    pending._onCompletedAfterComplete = this._mainThreadContinuation;
    pending._complete('result');
    return pending;
  }
  static WaitForSecondsAsync(seconds, token = null) {
    if (!Number.isFinite(seconds) || seconds < 0)
      throw new RangeError('WaitForSecondsAsync requires a nonnegative delay');
    return this._schedule('seconds', seconds, token);
  }
  static FromAsyncOperation(operation, token = null) {
    const pending = new Awaitable();
    const listener = () => {
      operation.completed.RemoveListener(listener);
      pending._complete('result');
    };
    if (token?.IsCancellationRequested && !operation._ignoresAwaitableCancellation) pending.Cancel();
    else {
      operation.completed.AddListener(listener);
      if (!operation._ignoresAwaitableCancellation) token?.Register?.(() => {
        operation.completed.RemoveListener(listener);
        pending.Cancel();
      });
    }
    return pending;
  }
  constructor() {
    this._state = 'pending';
    this._value = undefined;
    this._error = null;
    this._continuations = [];
  }
  get IsCompleted() { return this._state !== 'pending'; }
  GetAwaiter() { return new AwaitableAwaiter(this); }
  Cancel() { this._complete('canceled', undefined, canceledCompletion()); }
  _complete(state, value, error = null) {
    if (this.IsCompleted) return false;
    this._state = state;
    this._value = value;
    this._error = error;
    const continuations = this._continuations.splice(0);
    for (const continuation of continuations) continuation();
    return true;
  }
  _reset() {
    this._state = 'pending';
    this._value = undefined;
    this._error = null;
    this._continuations.length = 0;
  }
  then(resolve, reject) {
    return new Promise((accept, fail) => {
      const continuation = () => {
        try { accept(this.GetAwaiter().GetResult()); }
        catch (error) { fail(error); }
      };
      this.GetAwaiter().OnCompleted(continuation);
    }).then(resolve, reject);
  }
}

export class AwaitableAwaiter {
  constructor(awaitable) { this._awaitable = awaitable; }
  get IsCompleted() { return this._awaitable.IsCompleted; }
  OnCompleted(continuation) {
    if (this.IsCompleted) {
      if (this._awaitable._onCompletedAfterComplete)
        this._awaitable._onCompletedAfterComplete(continuation);
      else continuation();
    }
    else this._awaitable._continuations.push(continuation);
  }
  GetResult() {
    if (this._awaitable._state === 'canceled' || this._awaitable._state === 'error')
      throw this._awaitable._error;
    if (!this.IsCompleted) throw invalidCompletion();
    return this._awaitable._value;
  }
}

Awaitable.Awaiter = AwaitableAwaiter;

export class AwaitableCompletionSource {
  constructor() { this._awaitable = new Awaitable(); }
  get Awaitable() { return this._awaitable; }
  Reset() { this._awaitable._reset(); }
  TrySetResult(value) { return this._awaitable._complete('result', value); }
  TrySetCanceled() { return this._awaitable._complete('canceled', undefined, canceledCompletion()); }
  TrySetException(error) { return this._awaitable._complete('error', undefined, error); }
  SetResult(value) { if (!this.TrySetResult(value)) throw invalidCompletion(); }
  SetCanceled() { if (!this.TrySetCanceled()) throw invalidCompletion(); }
  SetException(error) { if (!this.TrySetException(error)) throw invalidCompletion(); }
}
