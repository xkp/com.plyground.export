/** The executable subset of System.Collections.Generic.List<T>. */
export class List extends Array {
  static get [Symbol.species]() { return Array; }

  constructor(capacityOrItems = undefined) {
    super();
    if (capacityOrItems === undefined) return;
    if (typeof capacityOrItems === 'number') {
      if (!Number.isSafeInteger(capacityOrItems) || capacityOrItems < 0)
        throw new RangeError('List capacity must be a nonnegative integer');
      return;
    }
    if (capacityOrItems == null || typeof capacityOrItems[Symbol.iterator] !== 'function')
      throw new TypeError('List constructor requires a capacity or iterable');
    for (const item of capacityOrItems) this.push(item);
  }

  get Count() { return this.length; }
  GetItem(index) {
    if (!Number.isInteger(index) || index < 0 || index >= this.length)
      throw new RangeError('List index is out of range');
    return this[index];
  }
  SetItem(index, item) {
    if (!Number.isInteger(index) || index < 0 || index >= this.length)
      throw new RangeError('List index is out of range');
    this[index] = item;
  }
  Add(item) { this.push(item); }
  AddRange(items) { for (const item of items) this.push(item); }
  Clear() { this.length = 0; }
  Contains(item) { return this.includes(item); }
  IndexOf(item) { return this.indexOf(item); }
  Insert(index, item) {
    if (!Number.isInteger(index) || index < 0 || index > this.length)
      throw new RangeError('List index is out of range');
    this.splice(index, 0, item);
  }
  Remove(item) {
    const index = this.indexOf(item);
    if (index < 0) return false;
    this.splice(index, 1);
    return true;
  }
  RemoveAt(index) {
    if (!Number.isInteger(index) || index < 0 || index >= this.length)
      throw new RangeError('List index is out of range');
    this.splice(index, 1);
  }
  ToArray() { return Array.from(this); }
}

/** Unity InputSystem's array view used by generated AI code. */
export class ReadOnlyArray {
  constructor(items = []) { this.items = items ?? []; }
  get Count() { return this.items.length; }
  GetItem(index) {
    if (!Number.isInteger(index) || index < 0 || index >= this.items.length)
      throw new RangeError('ReadOnlyArray index is out of range');
    return this.items[index];
  }
  [Symbol.iterator]() { return this.items[Symbol.iterator](); }
}

/** Indexed C# Dictionary semantics over Map. */
export class CSharpDictionary extends Map {
  constructor(itemsOrComparer = undefined) {
    super(itemsOrComparer != null && typeof itemsOrComparer[Symbol.iterator] === 'function'
      ? itemsOrComparer : undefined);
  }
  get Count() { return this.size; }
  Add(key, value) {
    if (this.has(key)) throw new Error('An item with the same key has already been added');
    this.set(key, value);
  }
  GetItem(key) {
    if (!this.has(key)) throw new Error(`Key not found: ${key}`);
    return this.get(key);
  }
  SetItem(key, value) { this.set(key, value); }
  ContainsKey(key) { return this.has(key); }
  TryGetValue(key, output) {
    const found = this.has(key);
    if (output) output.value = found ? this.get(key) : null;
    return found;
  }
  Remove(key) { return this.delete(key); }
  Clear() { this.clear(); }
}
