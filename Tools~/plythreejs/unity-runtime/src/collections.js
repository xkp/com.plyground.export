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
  Find(predicate) { return this.find(predicate) ?? null; }
  FindAll(predicate) { return new List(this.filter(predicate)); }
  ConvertAll(converter) { return new List(this.map(converter)); }
  FindIndex(predicate) { return this.findIndex(predicate); }
  ForEach(action) { this.forEach(action); }
  Sort(comparison = null) {
    const compare = typeof comparison === 'function' ? comparison
      : comparison?.Compare ? (left, right) => comparison.Compare(left, right)
        : (left, right) => left?.CompareTo ? left.CompareTo(right)
          : left < right ? -1 : left > right ? 1 : 0;
    this.sort(compare);
  }
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
  RemoveAll(predicate) {
    if (typeof predicate !== 'function') throw new TypeError('List.RemoveAll requires a predicate');
    let write = 0;
    for (let read = 0; read < this.length; read++)
      if (!predicate(this[read])) this[write++] = this[read];
    const removed = this.length - write;
    this.length = write;
    return removed;
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
  TryAdd(key, value) {
    if (this.has(key)) return false;
    this.set(key, value);
    return true;
  }
  GetItem(key) {
    if (!this.has(key)) throw new Error(`Key not found: ${key}`);
    return this.get(key);
  }
  SetItem(key, value) { this.set(key, value); }
  GetValueOrDefault(key, fallback = null) { return this.has(key) ? this.get(key) : fallback; }
  ContainsKey(key) { return this.has(key); }
  TryGetValue(key, output) {
    const found = this.has(key);
    if (output) output.value = found ? this.get(key) : null;
    return found;
  }
  Remove(key) { return this.delete(key); }
  Clear() { this.clear(); }
}
export class CSharpHashSet extends Set {
  constructor(capacityOrItems = undefined) {
    super(typeof capacityOrItems === 'number' ? undefined : capacityOrItems);
    if (typeof capacityOrItems === 'number'
      && (!Number.isSafeInteger(capacityOrItems) || capacityOrItems < 0))
      throw new RangeError('HashSet capacity must be a nonnegative integer');
  }
  Add(value) {
    const added = !this.has(value);
    this.add(value);
    return added;
  }
  Remove(value) { return this.delete(value); }
  Contains(value) { return this.has(value); }
  Clear() { this.clear(); }
  get Count() { return this.size; }
}

