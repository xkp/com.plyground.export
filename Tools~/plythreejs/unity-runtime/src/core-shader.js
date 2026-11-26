// Unity property IDs are only stable within one run. Keep a single registry for
// supported material calls; never serialize these generated numbers.
const nameToId = new Map();
const idToName = new Map();

export class Shader {
  constructor(name) { this.name = name; }
  static #known = new Map([
    ['Sprites/Default', new Shader('Sprites/Default')],
    ['Standard', new Shader('Standard')],
    ['Universal Render Pipeline/Lit', new Shader('Universal Render Pipeline/Lit')],
  ]);
  static Find(name) { return this.#known.get(name) ?? null; }
  static registerExported(name) {
    if (!name) return null;
    if (!this.#known.has(name)) this.#known.set(name, new Shader(name));
    return this.#known.get(name);
  }
  static PropertyToID(name) {
    if (typeof name !== 'string') throw new TypeError('Shader.PropertyToID requires a string');
    let id = nameToId.get(name);
    if (id === undefined) {
      id = nameToId.size + 1;
      nameToId.set(name, id);
      idToName.set(id, name);
    }
    return id;
  }
  static propertyName(id) { return idToName.get(id); }
}
