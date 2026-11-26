export class IObjectPool {
  Get() { throw new Error('IObjectPool.Get needs an implementation'); }
  Release() { throw new Error('IObjectPool.Release needs an implementation'); }
  Clear() { throw new Error('IObjectPool.Clear needs an implementation'); }
}

export class PooledObject {
  constructor(value, pool) {
    this.value = value;
    this.pool = pool;
  }
  Dispose() { this.pool.Release(this.value); }
}

function checkPoolArguments(createFunc, maxSize) {
  if (typeof createFunc !== 'function') {
    const error = new TypeError('createFunc cannot be null');
    error.name = 'ArgumentNullException';
    throw error;
  }
  if (!Number.isInteger(maxSize) || maxSize <= 0) {
    const error = new RangeError('maxSize must be greater than zero');
    error.name = 'ArgumentException';
    throw error;
  }
}

function checkDuplicate(items, value) {
  if (items.includes(value))
    throw new Error('Trying to release an object that has already been released to the pool.');
}

export class ObjectPool extends IObjectPool {
  constructor(createFunc, actionOnGet = null, actionOnRelease = null,
    actionOnDestroy = null, collectionCheck = true, defaultCapacity = 10, maxSize = 10000) {
    super();
    checkPoolArguments(createFunc, maxSize);
    if (!Number.isInteger(defaultCapacity) || defaultCapacity < 0)
      throw new RangeError('defaultCapacity must be nonnegative');
    this._create = createFunc;
    this._onGet = actionOnGet;
    this._onRelease = actionOnRelease;
    this._onDestroy = actionOnDestroy;
    this._collectionCheck = collectionCheck;
    this._maxSize = maxSize;
    this._items = [];
    this._fresh = null;
    this.CountAll = 0;
  }
  get CountInactive() { return this._items.length + (this._fresh == null ? 0 : 1); }
  get CountActive() { return this.CountAll - this.CountInactive; }
  Get(output = null) {
    let item;
    if (this._fresh != null) {
      item = this._fresh;
      this._fresh = null;
    } else if (this._items.length) item = this._items.pop();
    else { item = this._create(); this.CountAll++; }
    this._onGet?.(item);
    if (output != null) {
      output.value = item;
      return new PooledObject(item, this);
    }
    return item;
  }
  Release(item) {
    if (this._collectionCheck)
      checkDuplicate(this._fresh == null ? this._items : [this._fresh, ...this._items], item);
    if (this.CountActive <= 0) this.CountAll++;
    this._onRelease?.(item);
    if (this._fresh == null) this._fresh = item;
    else if (this.CountInactive < this._maxSize) this._items.push(item);
    else { this.CountAll--; this._onDestroy?.(item); }
  }
  Clear() {
    if (this._onDestroy) {
      for (const item of this._items) this._onDestroy(item);
      if (this._fresh != null) this._onDestroy(this._fresh);
    }
    this._items.length = 0;
    this._fresh = null;
    this.CountAll = 0;
  }
  Dispose() { this.Clear(); }
}

export class LinkedPool extends IObjectPool {
  constructor(createFunc, actionOnGet = null, actionOnRelease = null,
    actionOnDestroy = null, collectionCheck = true, maxSize = 10000) {
    super();
    checkPoolArguments(createFunc, maxSize);
    this._create = createFunc;
    this._onGet = actionOnGet;
    this._onRelease = actionOnRelease;
    this._onDestroy = actionOnDestroy;
    this._collectionCheck = collectionCheck;
    this._maxSize = maxSize;
    this._items = [];
  }
  get CountInactive() { return this._items.length; }
  Get(output = null) {
    const item = this._items.length ? this._items.pop() : this._create();
    this._onGet?.(item);
    if (output != null) {
      output.value = item;
      return new PooledObject(item, this);
    }
    return item;
  }
  Release(item) {
    if (this._collectionCheck) checkDuplicate(this._items, item);
    this._onRelease?.(item);
    if (this._items.length < this._maxSize) this._items.push(item);
    else this._onDestroy?.(item);
  }
  Clear() {
    if (this._onDestroy)
      for (let index = this._items.length - 1; index >= 0; index--)
        this._onDestroy(this._items[index]);
    this._items.length = 0;
  }
  Dispose() { this.Clear(); }
}
import { CSharpDictionary, List } from './collections.js';


const collectionPools = new Map();
const genericPools = new Map();
const unsafeGenericPools = new Map();

function staticPool(cache, key, create, clear = false, collectionCheck = true) {
  let pool = cache.get(key);
  if (pool == null) {
    pool = new ObjectPool(create, null, clear
      ? value => { if (typeof value.Clear === 'function') value.Clear(); else value.clear(); }
      : null, null, collectionCheck);
    cache.set(key, pool);
  }
  return pool;
}

export class CollectionPool {
  static Get(key, constructor, output = null) {
    return staticPool(collectionPools, key, () => new constructor(), true).Get(output);
  }
  static Release(key, constructor, value) {
    staticPool(collectionPools, key, () => new constructor(), true).Release(value);
  }
}

export class ListPool {
  static Get(key, output = null) { return CollectionPool.Get(key, List, output); }
  static Release(key, value) { CollectionPool.Release(key, List, value); }
}

export class HashSetPool {
  static Get(key, output = null) { return CollectionPool.Get(key, Set, output); }
  static Release(key, value) { CollectionPool.Release(key, Set, value); }
}

export class DictionaryPool {
  static Get(key, output = null) { return CollectionPool.Get(key, CSharpDictionary, output); }
  static Release(key, value) { CollectionPool.Release(key, CSharpDictionary, value); }
}

export class GenericPool {
  static Get(key, constructor, output = null) {
    return staticPool(genericPools, key, () => new constructor()).Get(output);
  }
  static Release(key, constructor, value) {
    staticPool(genericPools, key, () => new constructor()).Release(value);
  }
}

export class UnsafeGenericPool {
  static Get(key, constructor, output = null) {
    return staticPool(unsafeGenericPools, key, () => new constructor(), false, false).Get(output);
  }
  static Release(key, constructor, value) {
    staticPool(unsafeGenericPools, key, () => new constructor(), false, false).Release(value);
  }
}
