/** Browser overlay for the supported screen-space Unity Canvas subset. */
export class BrowserCanvasUI {
  constructor(host = globalThis.document?.body) {
    if (!host?.ownerDocument) throw new TypeError('BrowserCanvasUI needs a DOM host');
    this.host = host;
    this.elements = new Map();
    this.root = host.ownerDocument.createElement('div');
    Object.assign(this.root.style, {
      position: 'absolute', inset: '0', overflow: 'hidden', pointerEvents: 'none',
    });
  }
  attach(runtime) { this.runtime = runtime; this.host.appendChild(this.root); }
  detach() { this.root.remove(); this.elements.clear(); this.runtime = null; }
  afterLateUpdate() {
    const seen = new Set();
    for (const object of this.runtime?._objects ?? []) {
      if (!object.activeInHierarchy || !object._components.some(component => component.constructor.name === 'Canvas'))
        continue;
      const canvas = object._components.find(component => component.constructor.name === 'Canvas');
      const element = this._element(object, seen);
      element.style.inset = '0';
      element.style.zIndex = String(canvas.sortingOrder);
      this.root.appendChild(element);
      for (const child of object.children) this._render(child, element, seen);
    }
    for (const [object, element] of this.elements) if (!seen.has(object)) {
      element.remove();
      this.elements.delete(object);
    }
  }
  _element(object, seen) {
    seen.add(object);
    let element = this.elements.get(object);
    if (!element) {
      element = this.host.ownerDocument.createElement('div');
      element.style.position = 'absolute';
      element.style.boxSizing = 'border-box';
      element.addEventListener('click', event => {
        const button = object._components.find(component => component.constructor.name === 'Button');
        if (button?.enabled && button.interactable) {
          event.stopPropagation();
          button.onClick.Invoke();
        }
      });
      this.elements.set(object, element);
    }
    return element;
  }
  _render(object, parent, seen) {
    if (!object.activeInHierarchy) return;
    const element = this._element(object, seen);
    const rect = object.transform;
    if (rect.anchorMin && rect.anchorMax) {
      const width = rect.sizeDelta.x;
      const height = rect.sizeDelta.y;
      const x = rect.anchoredPosition.x;
      const y = rect.anchoredPosition.y;
      element.style.left = `calc(${rect.anchorMin.x * 100}% + ${x - rect.pivot.x * width}px)`;
      element.style.top = `calc(${(1 - rect.anchorMax.y) * 100}% + ${-y - (1 - rect.pivot.y) * height}px)`;
      element.style.width = `calc(${(rect.anchorMax.x - rect.anchorMin.x) * 100}% + ${width}px)`;
      element.style.height = `calc(${(rect.anchorMax.y - rect.anchorMin.y) * 100}% + ${height}px)`;
    }
    const image = object._components.find(component => component.constructor.name === 'Image' && component.enabled);
    const text = object._components.find(component => component.constructor.name === 'Text' && component.enabled);
    const button = object._components.find(component => component.constructor.name === 'Button' && component.enabled);
    element.style.backgroundColor = image ? color(image.color) : 'transparent';
    element.style.backgroundImage = image?.sprite?.image?.src
      ? `url(${JSON.stringify(image.sprite.image.src)})` : '';
    element.style.backgroundSize = '100% 100%';
    element.style.color = text ? color(text.color) : '';
    element.style.fontSize = text ? `${text.fontSize}px` : '';
    element.style.pointerEvents = button?.interactable ? 'auto' : 'none';
    element.style.cursor = button?.interactable ? 'pointer' : '';
    let label = element.firstChild;
    if (!label || label.nodeType !== 3) {
      label = this.host.ownerDocument.createTextNode('');
      element.prepend(label);
    }
    label.textContent = text?.text ?? '';
    parent.appendChild(element);
    for (const child of object.children) this._render(child, element, seen);
  }
}

function color(value) {
  const channel = number => Math.round(Math.max(0, Math.min(1, number ?? 1)) * 255);
  return `rgba(${channel(value.r)}, ${channel(value.g)}, ${channel(value.b)}, ${value.a ?? 1})`;
}
