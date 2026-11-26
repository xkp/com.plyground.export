import { UIDocument, UIElementsButton, RepeatButton, UIElementsEnumField,
  UIElementsHash128Field, UIElementsTextField, UIElementsToggle,
  UIElementsRadioButton, UIElementsFoldout, UIElementsIntegerField,
  UIElementsFloatField, UIElementsSlider,
  UIElementsSliderInt,
  UIElementsRadioButtonGroup, ToggleButtonGroup, UIElementsDropdownField, PopupField,
  UIElementsProgressBar,
  UIElementsVector2Field, UIElementsVector3Field, UIElementsVector4Field,
  UIElementsVector2IntField, UIElementsVector3IntField,
  UIElementsRectField, UIElementsRectIntField,
  UIElementsBoundsField, UIElementsBoundsIntField,
  UIElementsMinMaxSlider,
  ScrollView, ScrollViewMode, Scroller, ScrollerVisibility, SliderDirection,
  Tab, TabView, ListView, TreeView, MultiColumnListView, MultiColumnTreeView,
  TwoPaneSplitView,
  TwoPaneSplitViewOrientation, SelectionType,
  CollectionVirtualizationMethod, ListViewReorderMode,
  DragVisualMode, DragAndDropPosition,
  StartDragArgs, DragAndDropData, Box, GroupBox, PopupWindow, HelpBox, UIElementsImage, ScaleMode,
  FontStyle, TextAnchor,
  HelpBoxMessageType, Label, TextElement, VisualElement, FocusController,
  TemplateContainer, PickingMode,
  Vector2, Vector3, Rect,
  PanelRaycaster, GameObject, DropdownMenu, DropdownMenuAction,
  ContextClickEvent, ContextualMenuPopulateEvent, GenericDropdownMenu,
  TooltipEvent,
  toolkitStyleEnumCssValues, EventModifiers, PenStatus } from './index.js';
import { browserKeyCode } from './input.js';

const pixelStyleProperty = /^(?:width|height|minWidth|minHeight|maxWidth|maxHeight|top|right|bottom|left|fontSize|letterSpacing|wordSpacing|flexBasis|gap|rowGap|columnGap|borderRadius|border(?:Top|Right|Bottom|Left)(?:Left|Right)?Radius|(?:margin|padding|border)(?:Top|Right|Bottom|Left)?(?:Width)?)$/;
function browserStyleValue(property, value) {
  if (value == null) return '';
  if (property === 'cursor') {
    const cursor = value.value ?? value;
    const texture = cursor?.texture;
    const url = texture?.threeTexture?.image?.src ?? texture?.image?.src ?? texture?.url;
    return url ? `url(${JSON.stringify(url)}) ${cursor.hotspot?.x ?? 0} ${cursor.hotspot?.y ?? 0}, auto`
      : 'auto';
  }
  if (property === 'unityTextAutoSize') return '';
  if (['transitionDelay', 'transitionDuration', 'transitionProperty',
    'transitionTimingFunction'].includes(property)) {
    if (value.keyword && value.keyword !== 'Undefined')
      return value.keyword === 'Null' ? '' : value.keyword.toLowerCase();
    const items = Array.isArray(value) ? value : value.value ?? [];
    if (!Array.isArray(items)) return '';
    return items.map(item => {
      if (property === 'transitionTimingFunction') {
        const css = ['ease', 'ease-in', 'ease-out', 'ease-in-out', 'linear',
          'cubic-bezier(0.47, 0, 0.745, 0.715)',
          'cubic-bezier(0.39, 0.575, 0.565, 1)',
          'cubic-bezier(0.445, 0.05, 0.55, 0.95)'];
        return css[item?.mode ?? item] ?? 'ease';
      }
      if (property === 'transitionProperty')
        return String(item?.name ?? item).replace(/[A-Z]/g, match => `-${match.toLowerCase()}`);
      return item?.ToString?.() ?? String(item);
    }).join(', ');
  }
  if (property === 'textShadow') {
    if (value.keyword && value.keyword !== 'Undefined')
      return value.keyword === 'Null' ? '' : value.keyword.toLowerCase();
    return (value.value ?? value).ToCss?.() ?? value;
  }
  if (property === 'transformOrigin') {
    if (value.keyword && value.keyword !== 'Undefined')
      return value.keyword === 'Null' ? '' : value.keyword.toLowerCase();
    return (value.value ?? value).ToCss?.() ?? value;
  }
  if (['translate', 'rotate', 'scale'].includes(property)) {
    if (value.keyword && value.keyword !== 'Undefined')
      return value.keyword === 'Null' ? '' : value.keyword.toLowerCase();
    return (value.value ?? value).ToCss?.() ?? value;
  }
  if (['backgroundPositionX', 'backgroundPositionY', 'backgroundRepeat',
    'backgroundSize'].includes(property)) {
    if (value.keyword && value.keyword !== 'Undefined')
      return value.keyword === 'Null' ? '' : value.keyword.toLowerCase();
    const background = value.value ?? value;
    return background.ToCss?.() ?? background.ToString?.() ?? background;
  }
  if (property === 'aspectRatio') {
    if (value.keyword && value.keyword !== 'Undefined')
      return value.keyword === 'Null' ? '' : value.keyword.toLowerCase();
    const ratio = value.value?.value !== undefined ? value.value : value;
    return ratio.IsAuto?.() ? 'auto' : ratio.value ?? ratio;
  }
  const length = value?.value?.IsAuto || value?.value?.IsNone ? value.value : value;
  if (length?.IsAuto?.()) return 'auto';
  if (length?.IsNone?.()) return 'none';
  if (typeof value === 'string' && ['Auto', 'None', 'Initial', 'Null'].includes(value))
    return value === 'Null' ? '' : value.toLowerCase();
  if (typeof value === 'object' && 'keyword' in value
    && typeof value.keyword === 'string' && value.keyword !== 'Undefined') {
    const keyword = value.keyword.toLowerCase();
    if (keyword === 'null') return '';
    if (['auto', 'none', 'initial'].includes(keyword)) return keyword;
  }
  if (typeof value === 'object' && 'value' in value) {
    const unit = value.unit;
    if (unit != null)
      return `${value.value}${unit === 1 || /percent/i.test(String(unit)) ? '%' : 'px'}`;
    value = value.value;
  }
  if (typeof value === 'object' && 'unit' in value && 'value' in value)
    return `${value.value}${value.unit === 1 || /percent/i.test(String(value.unit)) ? '%' : 'px'}`;
  if (typeof value === 'object' && 'r' in value && 'g' in value && 'b' in value)
    return `rgba(${Math.round(value.r * 255)}, ${Math.round(value.g * 255)}, ${Math.round(value.b * 255)}, ${value.a ?? 1})`;
  if (typeof value === 'number' && property in toolkitStyleEnumCssValues)
    return toolkitStyleEnumCssValues[property][value] ?? '';
  if (typeof value === 'number' && pixelStyleProperty.test(property)) return `${value}px`;
  if (typeof value === 'string' && /^(?:alignContent|alignItems|alignSelf|display|flexDirection|flexWrap|justifyContent|overflow|position|textOverflow|visibility|whiteSpace)$/.test(property)
    && /^[A-Z][a-zA-Z]+$/.test(value))
    return value.replace(/([a-z])([A-Z])/g, '$1-$2').toLowerCase();
  return value;
}

function insidePanel(node, x, y) {
  const rect = node?.getBoundingClientRect?.();
  return !!rect && x >= rect.left && x <= rect.right
    && y >= rect.top && y <= rect.bottom;
}

class BrowserRuntimePanel {
  constructor(ui, settings) {
    this.ui = ui;
    this.settings = settings;
    this.visualTree = new VisualElement();
    this.visualTree.name = 'unity-panel-root';
    Object.defineProperty(this.visualTree, 'children', {
      get: () => [...ui.documents.keys()]
        .filter(document => document.panelSettings === settings
          && document.isActiveAndEnabled && document.rootVisualElement)
        .map(document => document.rootVisualElement),
    });
  }
  get panelSettings() { return this.settings; }
  get focusController() { return this.ui._publicFocusController ??= new FocusController(this.ui); }
  get scaledPixelsPerPoint() {
    const document = [...this.ui.documents.keys()].find(item =>
      item.panelSettings === this.settings && item.isActiveAndEnabled);
    return this.ui.documents.get(document)?._uiPanelScale ?? 1;
  }
  Pick(point) { return this.PickAll(point, null); }
  PickAll(point, picked) {
    const panelRoot = this.ui.panels.get(this.settings);
    const origin = panelRoot?.getBoundingClientRect?.();
    if (!origin) return null;
    const scale = this.scaledPixelsPerPoint;
    const x = origin.left + (point?.x ?? 0) * scale;
    const y = origin.top + (point?.y ?? 0) * scale;
    const hits = [];
    const visit = element => {
      if (!element.visible || !element.enabledInHierarchy
        || element.style.display === 'none' || element.style.visibility === 'hidden') return;
      const node = this.ui.elements.get(element);
      const rendered = node?._wrapper ?? node;
      for (let ancestor = element.parent; ancestor; ancestor = ancestor.parent)
        if (ancestor.style.overflow === 'hidden'
          || ancestor.style.overflowX === 'hidden'
          || ancestor.style.overflowY === 'hidden') {
          const ancestorNode = this.ui.elements.get(ancestor);
          if (!insidePanel(ancestorNode?._wrapper ?? ancestorNode, x, y)) return;
        }
      if (rendered && insidePanel(rendered, x, y)
        && element.pickingMode !== PickingMode.Ignore
        && element.style.pointerEvents !== 'none') hits.push(element);
      for (const child of element.children) visit(child);
    };
    for (const document of this.ui.documents.keys())
      if (document.panelSettings === this.settings && document.isActiveAndEnabled
        && document.rootVisualElement) visit(document.rootVisualElement);
    hits.reverse();
    if (picked) for (const element of hits) picked.push(element);
    return hits[0] ?? null;
  }
}

/** DOM projection of the script-visible UI Toolkit visual tree. */
export class BrowserToolkitUI {
  constructor(host = globalThis.document?.body) {
    if (!host?.ownerDocument) throw new TypeError('BrowserToolkitUI needs a DOM host');
    this.host = host;
    this.documents = new Map();
    this.panels = new Map();
    this.elements = new Map();
    this.diagnostics = new Map();
    this._fontFaces = new Map();
    this._imageVariants = new Map();
    this.root = host.ownerDocument.createElement('div');
    Object.assign(this.root.style, { position: 'absolute', inset: '0',
      overflow: 'hidden', pointerEvents: 'none' });
  }
  _collectionAtPoint(event, fallback) {
    const hitTest = this.host.ownerDocument.elementFromPoint;
    if (!hitTest) return fallback;
    const hit = hitTest.call(this.host.ownerDocument, event.clientX, event.clientY);
    for (let node = hit; node && node !== this.host; node = node.parentNode) {
      const element = node._uiElement;
      if (element instanceof ListView || element instanceof TreeView) return element;
      if (element?._listOwner) return element._listOwner;
    }
    for (const [element, node] of [...this.elements].reverse()) {
      if (!(element instanceof ListView || element instanceof TreeView)
        || !element.visible || !element.enabledInHierarchy) continue;
      const rect = node.getBoundingClientRect?.();
      if (rect && event.clientX >= rect.left && event.clientX < rect.right
        && event.clientY >= rect.top && event.clientY < rect.bottom) return element;
    }
    return null;
  }
  _collectionDropArgs(drag, event, destination = drag.owner) {
    const listNode = this.elements.get(destination);
    const bounds = listNode?.getBoundingClientRect?.() ?? { left: 0, top: 0 };
    const scale = panelScaleOf(listNode);
    const count = destination._itemCount();
    let to = Math.max(0, Math.min(count - 1, Math.floor(
      ((event.clientY - bounds.top) / scale + (listNode?.scrollTop ?? 0))
        / Math.max(1, destination.fixedItemHeight))));
    if (destination.virtualizationMethod === CollectionVirtualizationMethod.DynamicHeight)
      for (const row of destination._rows) {
        const rect = this.elements.get(row)?.getBoundingClientRect?.();
        if (rect && event.clientY >= rect.top && event.clientY < rect.bottom) {
          to = row._listIndex; break;
        }
      }
    const target = destination instanceof TreeView
      ? destination._visibleTreeItems[to] : destination._itemAt(to);
    const depth = destination instanceof TreeView ? target?.depth ?? 0 : 0;
    const asChild = destination instanceof TreeView
      && (event.clientX - bounds.left) / scale > 28 + depth * 16;
    return { position: new Vector2(event.clientX ?? 0, event.clientY ?? 0),
      target: destination instanceof TreeView ? target?.data ?? null : target,
      insertAtIndex: to, parentId: destination instanceof TreeView
        ? asChild ? target?.id ?? -1 : target?.parentId ?? -1 : -1,
      childIndex: -1, dropPosition: asChild ? DragAndDropPosition.OverItem
        : DragAndDropPosition.BetweenItems,
      dragAndDropData: drag.data, _asChild: asChild };
  }
  _collectionDragEvent(target, type, drag, event) {
    if (!target) return;
    const node = this.elements.get(target);
    const pointer = node ? pointerEventData(event, node) : {};
    target._dispatchEvent(type, { ...pointer,
      dragAndDropData: drag.data,
      position: new Vector2(event.clientX ?? 0, event.clientY ?? 0),
      mousePosition: new Vector2(pointer.position?.x ?? event.clientX ?? 0,
        pointer.position?.y ?? event.clientY ?? 0),
      localMousePosition: new Vector2(pointer.localPosition?.x ?? 0,
        pointer.localPosition?.y ?? 0),
      mouseDelta: new Vector2(pointer.deltaPosition?.x ?? 0,
        pointer.deltaPosition?.y ?? 0),
      _domEvent: event,
    });
  }
  _finishAnimatedCollectionDrag(drag) {
    const node = drag?._animatedNode;
    if (!node) return;
    node.style.transition = 'translate 140ms ease-out';
    node.style.translate = drag._animatedStyles.translate;
    setTimeout(() => {
      if (node.style.transition === 'translate 140ms ease-out')
        node.style.transition = drag._animatedStyles.transition;
      node.style.zIndex = drag._animatedStyles.zIndex;
    }, 160);
  }
  attach(runtime) { this.runtime = runtime; this.host.appendChild(this.root); }
  raycastAll(position) {
    const bounds = this.host.getBoundingClientRect?.() ?? { left: 0, top: 0,
      height: this.host.clientHeight ?? 0 };
    const x = bounds.left + position.x;
    const y = bounds.top + bounds.height - position.y;
    const hits = [];
    for (const [settings, panel] of this.panels) {
      if (!insidePanel(panel, x, y)) continue;
      const documents = [...this.documents.keys()].filter(document =>
        document.panelSettings === settings && document.isActiveAndEnabled);
      let target = null;
      for (const document of documents) {
        const visit = (element, depth) => {
          const node = this.elements.get(element);
          const rect = (node?._wrapper ?? node)?.getBoundingClientRect?.();
          if (!element.visible || element.style.display === 'none'
            || element.style.visibility === 'hidden') return;
          let clipped = false;
          for (let ancestor = element.parent; ancestor && !clipped;
            ancestor = ancestor.parent) {
            if (ancestor.style.overflow === 'hidden'
              || ancestor.style.overflowX === 'hidden'
              || ancestor.style.overflowY === 'hidden') {
              const ancestorNode = this.elements.get(ancestor);
              clipped = !insidePanel(ancestorNode?._wrapper ?? ancestorNode, x, y);
            }
          }
          if (clipped) return;
          if (insidePanel(node?._wrapper ?? node, x, y)
            && element.pickingMode !== PickingMode.Ignore
            && element.style.pointerEvents !== 'none'
            && !(element instanceof TemplateContainer))
            target = { element, document, depth };
          for (const child of element.children) visit(child, depth + 1);
        };
        visit(document.rootVisualElement, 0);
      }
      if (!target) continue;
      if (!panel._raycaster) {
        const panelObject = new GameObject('');
        panel._raycaster = panelObject.AddComponent(PanelRaycaster);
        panel._raycaster._browserUiSystem = this;
      }
      const panelOrder = [...this.panels.keys()].sort((a, b) =>
        (a.sortingOrder ?? 0) - (b.sortingOrder ?? 0));
      panel._raycaster._panelSettings = settings;
      panel._raycaster._resolvedSortIndex = panelOrder.indexOf(settings);
      panel._raycaster._resolvedSortMax = panelOrder.length - 1;
      hits.push({ gameObject: panel._raycaster.gameObject,
        module: panel._raycaster, sortingOrder: settings.sortingOrder,
        depth: target.depth, element: target.element, document: target.document });
    }
    return hits;
  }
  detach() {
    for (const element of this.elements.keys()) {
      const panel = this._attachedPanels?.get(element);
      if (panel) element._dispatchEvent('DetachFromPanelEvent',
        { originPanel: panel, destinationPanel: null },
        { trickles: false, bubbles: false });
      element._uiRuntime = null;
      element._focusController = null;
      element._browserPanel = null;
    }
    for (const document of this.documents.keys()) document._browserPanel = null;
    for (const node of this.elements.values()) node._stopRepeat?.();
    for (const face of this._fontFaces.values()) this.host.ownerDocument.fonts?.delete?.(face);
    this.root.remove(); this.documents.clear(); this.panels.clear(); this.elements.clear();
    this.diagnostics.clear(); this._fontFaces.clear(); this._focusedElement = null; this.runtime = null;
    this._imageVariants.clear(); this._attachedPanels?.clear();
    this._lastGeometry?.clear(); this._lastCustomStyles?.clear();
  }
  _imageVariant(source, element, node) {
    const uv = element.uv ?? { x: 0, y: 0, width: 1, height: 1 };
    const rect = element.sourceRect;
    const spriteRect = element.sprite?.rect;
    const tint = element.tintColor ?? { r: 1, g: 1, b: 1, a: 1 };
    const alphaInPixels = !!element.tintAlphaInPixels;
    const hasRect = rect?.width > 0 && rect?.height > 0;
    const hasUv = uv.x !== 0 || uv.y !== 0 || uv.width !== 1 || uv.height !== 1;
    const crop = hasRect ? ['rect', rect.x, rect.y, rect.width, rect.height]
      : hasUv ? ['uv', uv.x, uv.y, uv.width, uv.height]
        : spriteRect ? ['sprite', spriteRect.x, spriteRect.y,
          spriteRect.width, spriteRect.height] : null;
    if (!crop && tint.r === 1 && tint.g === 1 && tint.b === 1
      && (!alphaInPixels || (tint.a ?? 1) === 1)) {
      node._imageVariantKey = null;
      return source;
    }
    const key = JSON.stringify([source, crop, tint.r, tint.g, tint.b,
      alphaInPixels ? tint.a ?? 1 : 1]);
    node._imageVariantKey = key;
    let entry = this._imageVariants.get(key);
    if (entry) {
      if (entry.error) this.diagnostics.set(element,
        `UI Toolkit Image crop/tint source unavailable on ${element.name || 'unnamed image'}: ${source}`);
      return entry.url || '';
    }
    entry = { url: '', error: false };
    this._imageVariants.set(key, entry);
    const failed = () => {
      entry.error = true;
      this.diagnostics.set(element,
        `UI Toolkit Image crop/tint source unavailable on ${element.name || 'unnamed image'}: ${source}`);
    };
    const ImageClass = this.host.ownerDocument.defaultView?.Image ?? globalThis.Image;
    if (!ImageClass) { failed(); return ''; }
    const image = new ImageClass();
    image.crossOrigin = 'anonymous';
    image.onload = () => {
      try {
        const width = image.naturalWidth || image.width;
        const height = image.naturalHeight || image.height;
        const [kind, x, y, w, h] = crop ?? ['rect', 0, 0, width, height];
        const sx = kind === 'uv' ? x * width : x;
        const sy = kind === 'uv' ? (1 - y - h) * height
          : kind === 'sprite' ? height - y - h : y;
        const sw = kind === 'uv' ? w * width : w;
        const sh = kind === 'uv' ? h * height : h;
        const canvas = this.host.ownerDocument.createElement('canvas');
        canvas.width = Math.max(1, Math.round(sw));
        canvas.height = Math.max(1, Math.round(sh));
        const context = canvas.getContext('2d');
        context.drawImage(image, sx, sy, sw, sh, 0, 0, canvas.width, canvas.height);
        if (tint.r !== 1 || tint.g !== 1 || tint.b !== 1
          || alphaInPixels && (tint.a ?? 1) !== 1) {
          const pixels = context.getImageData(0, 0, canvas.width, canvas.height);
          for (let i = 0; i < pixels.data.length; i += 4) {
            pixels.data[i] *= tint.r;
            pixels.data[i + 1] *= tint.g;
            pixels.data[i + 2] *= tint.b;
            if (alphaInPixels) pixels.data[i + 3] *= tint.a ?? 1;
          }
          context.putImageData(pixels, 0, 0);
        }
        entry.url = canvas.toDataURL('image/png');
        if (node._imageVariantKey === key)
          node.style.backgroundImage = `url(${JSON.stringify(entry.url)})`;
      } catch { failed(); }
    };
    image.onerror = failed;
    image.src = source;
    return '';
  }
  afterLateUpdate() {
    const seen = new Set();
    const rendered = new Set();
    const panelOrder = [];
    const documentsByPanel = new Map();
    this.diagnostics.clear();
    const selectedGameObject = [...(this.runtime?._objects ?? [])]
      .flatMap(object => object._components)
      .find(component => component.constructor.name === 'EventSystem'
        && component.isActiveAndEnabled)?.currentSelectedGameObject;
    if (selectedGameObject && this._focusedElement) {
      this._focusedElement.Blur();
      if (this._focusedElement) this._setFocus(null);
    }
    const documents = [...(this.runtime?._objects ?? [])]
      .flatMap(object => object._components)
      .filter(component => component instanceof UIDocument && component.isActiveAndEnabled)
      .sort((a, b) => (a.panelSettings?.sortingOrder ?? 0) - (b.panelSettings?.sortingOrder ?? 0)
        || a.sortingOrder - b.sortingOrder);
    for (const document of documents) {
      if (!document.panelSettings) {
        this.diagnostics.set(document, `UIDocument ${document.gameObject?.name ?? ''} needs PanelSettings`);
        continue;
      }
      if (document.panelSettings.renderMode !== 0
        || document.panelSettings.targetTexture
        || document.panelSettings.targetDisplay !== 0) {
        this.diagnostics.set(document,
          `UIDocument ${document.gameObject?.name ?? ''} panel target is outside browser screen UI`);
        continue;
      }
      const nativeSettings = [];
      if (!document.panelSettings.clearDepthStencil) nativeSettings.push('clearDepthStencil');
      if (document.panelSettings.forceGammaRendering) nativeSettings.push('forceGammaRendering');
      if (document.panelSettings.referenceSpritePixelsPerUnit !== 100)
        nativeSettings.push('referenceSpritePixelsPerUnit');
      if (document.panelSettings.textureSlotCount !== 8) nativeSettings.push('textureSlotCount');
      if (document.panelSettings.vertexBudget) nativeSettings.push('vertexBudget');
      if (document.panelSettings.dynamicAtlasSettings) nativeSettings.push('dynamicAtlasSettings');
      if (document.panelSettings.textSettings) nativeSettings.push('textSettings');
      if (nativeSettings.length)
        this.diagnostics.set(document.panelSettings,
          `PanelSettings browser renderer owns ${nativeSettings.join(', ')}; authored values remain script-visible`);
      seen.add(document);
      const panel = this._panel(document);
      const panelRoot = this._panelRoot(document.panelSettings);
      document._browserPanel = panelRoot._panelAdapter;
      panel._panelAdapter = panelRoot._panelAdapter;
      panelRoot.style.backgroundColor = document.panelSettings.clearColor
        ? browserStyleValue('backgroundColor', document.panelSettings.colorClearValue) : '';
      if (!documentsByPanel.has(panelRoot)) {
        documentsByPanel.set(panelRoot, []);
        panelOrder.push(panelRoot);
      }
      const documentOrder = documentsByPanel.get(panelRoot);
      const bounds = this.host.getBoundingClientRect?.() ?? this.host;
      const width = bounds.width || this.host.clientWidth || globalThis.window?.innerWidth || 0;
      const height = bounds.height || this.host.clientHeight || globalThis.window?.innerHeight || 0;
      const scale = panelScale(document.panelSettings, width, height);
      panel._uiPanelScale = scale;
      panel.style.width = `${width / scale}px`;
      panel.style.height = `${height / scale}px`;
      panel.style.transform = `scale(${scale})`;
      panel.style.transformOrigin = 'top left';
      panel.style.zIndex = String(documentOrder.length);
      if ((panel.parentNode ?? panel.parent) !== panelRoot) panelRoot.appendChild(panel);
      documentOrder.push(panel);
      const theme = document.panelSettings.themeStyleSheet;
      const themeSource = theme?.text?.replace(
        /@import\s+url\(\s*["']?unity-theme:\/\/default["']?\s*\)\s*;?/gi, '') ?? '';
      if (/@import\s+url\(/i.test(themeSource))
        this.diagnostics.set(document,
          `UI Toolkit theme import unresolved on ${document.gameObject?.name ?? 'UIDocument'}`);
      const themeCss = themeSource.replace(/@import\s+url\([^)]*\)\s*;?/gi, '');
      const fontUrls = new Map([...(theme?.fontUrls ?? []),
        ...(document.visualTreeAsset?.fontUrls ?? [])]);
      const imageUrls = new Map([...(theme?.imageUrls ?? []),
        ...(document.visualTreeAsset?.imageUrls ?? [])]);
      for (const font of [...fontUrls.values()].flatMap(face => [face, ...(face.fallbacks ?? [])])) {
        if (!this._fontFaces.has(font.family)
          && typeof globalThis.FontFace === 'function' && this.host.ownerDocument.fonts?.add) {
          const face = new FontFace(font.family, `url(${JSON.stringify(font.url)})`);
          this.host.ownerDocument.fonts.add(face);
          this._fontFaces.set(font.family, face);
          void face.load().catch(() => {});
        }
        if (this._fontFaces.get(font.family)?.status === 'error')
          this.diagnostics.set(document, `UI Toolkit font failed to load: ${font.url}`);
      }
      const stylesheets = [themeCss, ...(document.visualTreeAsset?.stylesheets ?? [])]
        .map(sheet => typeof sheet === 'string' ? sheet : sheet?.text ?? sheet?.cssText)
        .filter(Boolean).map(sheet =>
        sheet.replace(/-unity-font(-definition)?\s*:\s*url\(\s*["']?([^"')]+)["']?\s*\)/gi,
          (declaration, definition, reference) => {
            const font = fontUrls.get(reference.trim());
            if (!font) {
              this.diagnostics.set(document, `UI Toolkit font unresolved: ${reference}`);
              return declaration;
            }
            const stack = [font, ...(font.fallbacks ?? [])]
              .map(face => JSON.stringify(face.family)).join(', ');
            return `font-family: ${stack}${definition ? ' !important' : ''}`;
          }).replace(/(background-image|--unity-image)\s*:\s*url\(\s*["']?([^"')]+)["']?\s*\)/gi,
          (declaration, property, reference) => {
            const url = imageUrls.get(reference.trim());
            if (!url) {
              this.diagnostics.set(document, `UI Toolkit image unresolved: ${reference}`);
              return declaration;
            }
            return `${property}: url(${JSON.stringify(url)})`;
          }));
      const dynamicSheets = [];
      const collectSheets = element => {
        if (element.styleSheets?.count) {
          element._styleScopeId ??= ++toolkitStyleScopeId;
          for (const sheet of element.styleSheets) {
            const source = typeof sheet === 'string' ? sheet : sheet?.text ?? sheet?.cssText;
            if (!source) continue;
            for (const font of [...(sheet?.fontUrls?.values?.() ?? [])]
              .flatMap(face => [face, ...(face.fallbacks ?? [])])) {
              if (!this._fontFaces.has(font.family)
                && typeof globalThis.FontFace === 'function' && this.host.ownerDocument.fonts?.add) {
                const face = new FontFace(font.family, `url(${JSON.stringify(font.url)})`);
                this.host.ownerDocument.fonts.add(face);
                this._fontFaces.set(font.family, face);
                void face.load().catch(() => {});
              }
            }
            const resolved = source.replace(/-unity-font(-definition)?\s*:\s*url\(\s*["']?([^"')]+)["']?\s*\)/gi,
              (declaration, definition, reference) => {
                const font = sheet?.fontUrls?.get(reference.trim());
                if (!font) { this.diagnostics.set(element,
                  `UI Toolkit runtime font unresolved: ${reference}`); return declaration; }
                const stack = [font, ...(font.fallbacks ?? [])]
                  .map(face => JSON.stringify(face.family)).join(', ');
                return `font-family: ${stack}${definition ? ' !important' : ''}`;
              }).replace(/(background-image|--unity-image)\s*:\s*url\(\s*["']?([^"')]+)["']?\s*\)/gi,
              (declaration, property, reference) => {
                const url = sheet?.imageUrls?.get(reference.trim());
                if (!url) { this.diagnostics.set(element,
                  `UI Toolkit runtime image unresolved: ${reference}`); return declaration; }
                return `${property}: url(${JSON.stringify(url)})`;
              });
            dynamicSheets.push(scopeUssToElement(resolved, element._styleScopeId));
          }
        }
        for (const child of element.children) collectSheets(child);
      };
      collectSheets(document.rootVisualElement);
      const stylesheet = ':where([data-ui-type]) { box-sizing: border-box; display: flex; '
        + 'flex-direction: column; pointer-events: auto; }\n'
        + ':where([data-ui-type="Label"]) { margin: 4px 4px 2px 2px; '
        + 'padding: 4px 2px 4px 1px; font-size: 14px; line-height: 17px; }\n'
        + ':where([data-ui-type="Button"], [data-ui-type="RepeatButton"]) { '
        + 'margin: 2px 4px; padding: 10px 12px; border-width: 1px; '
        + 'border-style: solid; font-size: 14px; line-height: 17px; }\n'
        + ':where([data-ui-type="TextField"]) { margin: 2px 4px; }\n'
        + 'input::selection, textarea::selection { '
        + 'background-color: var(--unity-selection-color, rgba(61,128,223,.65)); }\n'
        + '.unity-list-view__item--selected { background-color: rgba(64, 128, 220, .45); }\n'
        + '.unity-list-view__item--alternate { background-color: rgba(127, 127, 127, .08); }\n'
        + '.unity-list-view__item--selected.unity-list-view__item--alternate { background-color: rgba(64, 128, 220, .45); }\n'
        + stylesheets.map(sheet => translateUssSelectors(translateUssDeclarations(sheet))).join('\n')
        + '\n' + dynamicSheets.join('\n');
      if (panel._stylesheet.textContent !== stylesheet)
        panel._stylesheet.textContent = stylesheet;
      this._render(document.rootVisualElement, panel._content, rendered);
    }
    syncToolkitOrder(this.root, panelOrder);
    for (const [panelRoot, order] of documentsByPanel)
      syncToolkitOrder(panelRoot, order);
    for (const element of rendered) if (element._focusRequested) {
      element._focusRequested = false;
      element.Focus();
    }
    this._attachedPanels ??= new Map();
    for (const element of rendered) {
      const previous = this._attachedPanels.get(element) ?? null;
      const current = element._browserPanel;
      if (previous === current) continue;
      if (previous) element._dispatchEvent('DetachFromPanelEvent',
        { originPanel: previous, destinationPanel: current },
        { trickles: false, bubbles: false });
      if (current) element._dispatchEvent('AttachToPanelEvent',
        { originPanel: previous, destinationPanel: current },
        { trickles: false, bubbles: false });
      this._attachedPanels.set(element, current);
    }
    this._lastGeometry ??= new Map();
    this._lastCustomStyles ??= new Map();
    for (const element of rendered) {
      const callbacks = element._callbacks?.get('GeometryChangedEvent');
      if (callbacks?.bubble?.size || callbacks?.trickle?.size) {
        const current = element.layout;
        const previous = this._lastGeometry.get(element) ?? new Rect();
        if (current.x !== previous.x || current.y !== previous.y
          || current.width !== previous.width || current.height !== previous.height)
          element._dispatchEvent('GeometryChangedEvent', {
            oldRect: previous, newRect: current }, { trickles: false, bubbles: false });
        this._lastGeometry.set(element, current);
      }
      const styleCallbacks = element._callbacks?.get('CustomStyleResolvedEvent');
      if (!styleCallbacks?.bubble?.size && !styleCallbacks?.trickle?.size) continue;
      const node = element._browserNode?._wrapper ?? element._browserNode;
      const computed = node && globalThis.getComputedStyle?.(node);
      if (!computed) continue;
      const properties = [];
      for (let index = 0; index < (computed.length ?? 0); index++) {
        const name = computed.item?.(index) ?? computed[index];
        if (name?.startsWith('--'))
          properties.push([name, computed.getPropertyValue?.(name)?.trim() ?? '']);
      }
      const signature = JSON.stringify(properties.sort(([a], [b]) => a.localeCompare(b)));
      if (signature !== this._lastCustomStyles.get(element) && properties.length)
        element._dispatchEvent('CustomStyleResolvedEvent',
          { customStyle: element.customStyle }, { trickles: false, bubbles: false });
      this._lastCustomStyles.set(element, signature);
    }
    for (const [document, panel] of this.documents) if (!seen.has(document)) {
      panel.remove(); document._browserPanel = null; this.documents.delete(document);
    }
    for (const [settings, panelRoot] of this.panels)
      if (!documentsByPanel.has(panelRoot)) {
        panelRoot.remove(); this.panels.delete(settings);
      }
    for (const [element, node] of this.elements) if (!rendered.has(element)) {
      const panel = this._attachedPanels.get(element);
      if (panel) element._dispatchEvent('DetachFromPanelEvent',
        { originPanel: panel, destinationPanel: null },
        { trickles: false, bubbles: false });
      this._attachedPanels.delete(element);
      this._lastGeometry.delete(element);
      this._lastCustomStyles.delete(element);
      if (this._focusedElement === element) this._setFocus(null);
      element._browserNode = null; element._uiRuntime = null;
      element._browserPanel = null;
      (node._wrapper ?? node).remove(); this.elements.delete(element);
    }
    const deltaMs = (this.runtime?.time.unscaledDeltaTime ?? 0) * 1000;
    const frame = this.runtime?.time.frameCount ?? 0;
    const now = (this.runtime?.time.unscaledTime ?? 0) * 1000;
    for (const element of rendered) element._scheduler?._tick(deltaMs, frame, now);
  }
  _panel(document) {
    let panel = this.documents.get(document);
    if (panel) return panel;
    panel = this.host.ownerDocument.createElement('div');
    panel._uiElement = document;
    Object.assign(panel.style, { position: 'absolute', left: '0', top: '0',
      overflow: 'hidden', pointerEvents: 'none' });
    const surface = panel.attachShadow?.({ mode: 'open' }) ?? panel;
    panel._stylesheet = this.host.ownerDocument.createElement('style');
    panel._content = this.host.ownerDocument.createElement('div');
    Object.assign(panel._content.style, { width: '100%', height: '100%',
      display: 'flex', flexDirection: 'column', pointerEvents: 'none' });
    surface.appendChild(panel._stylesheet);
    surface.appendChild(panel._content);
    this.documents.set(document, panel);
    return panel;
  }
  _panelRoot(settings) {
    let root = this.panels.get(settings);
    if (!root) {
      root = this.host.ownerDocument.createElement('div');
      root._uiElement = settings;
      root._panelAdapter = new BrowserRuntimePanel(this, settings);
      Object.assign(root.style, { position: 'absolute', inset: '0',
        overflow: 'hidden', pointerEvents: 'none' });
      this.panels.set(settings, root);
    }
    root.style.zIndex = String(Math.round((settings.sortingOrder ?? 0) * 1000));
    if ((root.parentNode ?? root.parent) !== this.root) this.root.appendChild(root);
    return root;
  }
  _render(element, parent, seen) {
    seen.add(element);
    element._uiRuntime = this.runtime;
    element._focusController = this._publicFocusController ??= new FocusController(this);
    element._browserPanel = null;
    for (let node = parent; node && !element._browserPanel;
      node = node.parentNode ?? node.host)
      if (node._panelAdapter) element._browserPanel = node._panelAdapter;
    let node = this.elements.get(element);
    const tag = element instanceof UIElementsButton ? 'button'
      : element instanceof Scroller ? 'input'
      : element instanceof PopupField || element instanceof UIElementsEnumField ? 'select'
        : element instanceof UIElementsProgressBar ? 'progress'
          : element instanceof UIElementsTextField && element.multiline ? 'textarea'
            : isToolkitField(element) ? 'input' : 'div';
    if (node && node._uiTag !== tag) {
      if (element instanceof UIElementsTextField) {
        element._readSelection(); element._selectionDirty = true;
      }
      if (this._focusedElement === element) this._setFocus(null);
      (node._wrapper ?? node).remove();
      this.elements.delete(element);
      node = null;
    }
    if (!node) {
      node = this.host.ownerDocument.createElement(tag);
      node._uiTag = tag;
      this.elements.set(element, node);
      node._visualElement = element;
      if (isToolkitField(element) || element instanceof UIElementsProgressBar) {
        const wrapper = this.host.ownerDocument.createElement('label');
        const caption = this.host.ownerDocument.createElement('span');
        wrapper.appendChild(caption);
        wrapper.appendChild(node);
        node._wrapper = wrapper;
        wrapper._caption = caption;
        if (element instanceof UIElementsToggle && !(element instanceof UIElementsFoldout)) {
          const text = this.host.ownerDocument.createElement('span');
          wrapper.appendChild(text);
          wrapper._boolText = text;
          for (const labelPart of [caption, text])
            labelPart.addEventListener('click', event => {
              if (element.toggleOnLabelClick) return;
              event.preventDefault?.();
              event.stopPropagation?.();
            });
        }
        if (element instanceof UIElementsSlider) {
          const valueInput = this.host.ownerDocument.createElement('input');
          valueInput.type = 'number';
          valueInput.addEventListener('change', event => {
            event.stopPropagation?.(); element.value = Number(valueInput.value);
          });
          wrapper.appendChild(valueInput);
          wrapper._sliderInput = valueInput;
        }
      }
      if (element instanceof UIElementsFoldout) {
        node._foldoutHeader = this.host.ownerDocument.createElement('button');
        node._foldoutArrow = this.host.ownerDocument.createElement('span');
        node._foldoutText = this.host.ownerDocument.createElement('span');
        node._foldoutHeader.appendChild(node._foldoutArrow);
        node._foldoutHeader.appendChild(node._foldoutText);
        node._foldoutContent = this.host.ownerDocument.createElement('div');
        node.appendChild(node._foldoutHeader);
        node.appendChild(node._foldoutContent);
        node._foldoutHeader.addEventListener('click', event => {
          event.stopPropagation();
          if (!element.toggleOnLabelClick && event.target === node._foldoutText) return;
          element.value = !element.value;
        });
      }
      if (element instanceof TabView) {
        node._tabHeaders = this.host.ownerDocument.createElement('div');
        node._tabContent = this.host.ownerDocument.createElement('div');
        node.appendChild(node._tabHeaders);
        node.appendChild(node._tabContent);
        node._tabHeaderButtons = new Map();
      }
      if (element instanceof GroupBox || element instanceof PopupWindow) {
        node._groupTitle = this.host.ownerDocument.createElement('div');
        node._groupContent = this.host.ownerDocument.createElement('div');
        node.appendChild(node._groupTitle); node.appendChild(node._groupContent);
      }
      if (element instanceof HelpBox) {
        node._helpIcon = this.host.ownerDocument.createElement('span');
        node._helpText = this.host.ownerDocument.createElement('span');
        node.appendChild(node._helpIcon); node.appendChild(node._helpText);
      }
      if (isCompoundField(element)) {
        node._multiCaption = this.host.ownerDocument.createElement('span');
        node._multiControls = this.host.ownerDocument.createElement('div');
        node._multiControls.style.display = 'flex';
        node.appendChild(node._multiCaption); node.appendChild(node._multiControls);
        const axes = compoundAxes(element);
        node._multiInputs = axes.map(axis => {
          const input = this.host.ownerDocument.createElement('input');
          input.type = element instanceof UIElementsMinMaxSlider ? 'range' : 'number';
          input.step = element instanceof UIElementsVector2IntField
            || element instanceof UIElementsVector3IntField
            || element instanceof UIElementsRectIntField
            || element instanceof UIElementsBoundsIntField ? '1' : 'any';
          input.setAttribute?.('aria-label', `${element.label} ${axis}`.trim());
          for (const domType of ['input', 'change'])
            input.addEventListener(domType, event => {
              event.stopPropagation?.();
              if (element.isDelayed && domType === 'input') {
                input._pendingComponentText = input.value;
                return;
              }
              if (domType === 'change') input._pendingComponentText = undefined;
              const value = Number(input.value);
              if (element instanceof UIElementsMinMaxSlider) {
                if (axis === 'x') element.minValue = value;
                else element.maxValue = value;
              } else if (element._components) element.SetComponent(axis, value);
              else element.value = { ...element.value, [axis]: value };
            });
          node._multiControls.appendChild(input);
          return input;
        });
      }
      if (element instanceof UIElementsRadioButtonGroup) {
        node._radioCaption = this.host.ownerDocument.createElement('span');
        node.appendChild(node._radioCaption);
        node._radioOptions = [];
      }
      if (element instanceof ToggleButtonGroup) {
        node._toggleCaption = this.host.ownerDocument.createElement('span');
        node._toggleButtons = this.host.ownerDocument.createElement('div');
        node.appendChild(node._toggleCaption);
        node.appendChild(node._toggleButtons);
      }
      if (element instanceof TwoPaneSplitView) {
        node._splitter = this.host.ownerDocument.createElement('div');
        node._splitter.addEventListener('pointerdown', event => {
          event.stopPropagation?.();
          node._splitStart = { pointerId: event.pointerId, x: event.clientX,
            y: event.clientY, dimension: element.fixedPaneInitialDimension };
          node._splitter.setPointerCapture?.(event.pointerId);
        });
        node._splitter.addEventListener('pointermove', event => {
          const start = node._splitStart;
          if (!start || start.pointerId !== event.pointerId) return;
          const horizontal = element.orientation === TwoPaneSplitViewOrientation.Horizontal;
          const delta = (horizontal ? event.clientX - start.x : event.clientY - start.y)
            * (element.fixedPaneIndex === 0 ? 1 : -1);
          element.fixedPaneInitialDimension = Math.max(0, start.dimension + delta);
        });
        const endDrag = event => {
          if (node._splitStart?.pointerId === event.pointerId) node._splitStart = null;
        };
        node._splitter.addEventListener('pointerup', endDrag);
        node._splitter.addEventListener('pointercancel', endDrag);
      }
      if (element instanceof RepeatButton) {
        node._stopRepeat = () => {
          clearTimeout(node._repeatDelayTimer); clearInterval(node._repeatIntervalTimer);
          node._repeatDelayTimer = null; node._repeatIntervalTimer = null;
          node._repeatPointerId = null;
        };
        node.addEventListener('pointerdown', event => {
          if (event.button !== 0 || !element.enabledInHierarchy) return;
          node._stopRepeat();
          node._repeatPointerId = event.pointerId;
          node._suppressRepeatClick = true;
          element._click(clickPointerEventData(event, node));
          node._repeatDelayTimer = setTimeout(() => {
            if (node._repeatPointerId !== event.pointerId) return;
            if (element.interval <= 0) return;
            if (element.enabledInHierarchy) element._click(clickPointerEventData(event, node));
            node._repeatIntervalTimer = setInterval(() => {
              if (element.enabledInHierarchy) element._click(clickPointerEventData(event, node));
            }, Math.max(1, element.interval));
          }, Math.max(0, element.delay));
        });
        node.addEventListener('pointerup', () => {
          node._stopRepeat();
          setTimeout(() => { node._suppressRepeatClick = false; }, 0);
        });
        for (const name of ['pointercancel', 'pointerleave'])
          node.addEventListener(name, () => {
            node._stopRepeat(); node._suppressRepeatClick = false;
          });
      }
      if (element instanceof UIElementsButton && !(element instanceof RepeatButton)) {
        node.addEventListener('pointerdown', event => {
          if (element.enabledInHierarchy)
            node._unityButtonPress = event.pointerId;
        });
        node.addEventListener('pointerup', event => {
          if (node._unityButtonPress !== event.pointerId) return;
          node._unityButtonPress = null;
          if (!element.enabledInHierarchy || !insidePanel(node, event.clientX, event.clientY))
            return;
          const modifiers = (event.shiftKey ? EventModifiers.Shift : 0)
            | (event.ctrlKey ? EventModifiers.Control : 0)
            | (event.altKey ? EventModifiers.Alt : 0)
            | (event.metaKey ? EventModifiers.Command : 0);
          const activation = { button: event.button ?? 0,
            clickCount: event.detail || 1, modifiers,
            position: new Vector2(event.clientX ?? 0, event.clientY ?? 0),
            _domEvent: event };
          if (!element.clickable.Matches(activation)) {
            node._unityPointerRejected = true;
            setTimeout(() => { node._unityPointerRejected = false; }, 0);
            return;
          }
          element.clickable.Invoke(activation);
          node._unityPointerClicked = true;
          setTimeout(() => { node._unityPointerClicked = false; }, 0);
        });
        node.addEventListener('pointercancel', () => { node._unityButtonPress = null; });
      }
      if (element instanceof UIElementsButton)
        node.addEventListener('click', event => {
          event.stopPropagation();
          if (node._suppressRepeatClick) { node._suppressRepeatClick = false; return; }
          if (node._unityPointerRejected) { node._unityPointerRejected = false; return; }
          if (node._unityPointerClicked) {
            node._unityPointerClicked = false;
            element._dispatchEvent('ClickEvent', clickPointerEventData(event, node));
          } else element._click(clickPointerEventData(event, node), event.detail === 0);
        });
      else node.addEventListener('click', event => {
        if (node._suppressCollectionClick) {
          node._suppressCollectionClick = false;
          event.stopPropagation?.(); return;
        }
        if (event.target !== node && !element._multiCells) return;
        if (element._listOwner) {
          const list = element._listOwner;
          if (list instanceof TreeView) {
            const item = list._visibleTreeItems[element._listIndex];
            if (item?.children.length && event.offsetX < 16 + item.depth * 16) {
              if (list.IsExpanded(item.id)) list.CollapseItem(item.id);
              else list.ExpandItem(item.id);
              return;
            }
          }
          if (list.selectionType === SelectionType.Multiple && (event.ctrlKey || event.metaKey)
            && list.selectedIndices.includes(element._listIndex))
            list.RemoveFromSelection(element._listIndex);
          else if (list.selectionType === SelectionType.Multiple && (event.ctrlKey || event.metaKey))
            list.AddToSelection(element._listIndex);
          else list.SetSelection(element._listIndex);
          list.Focus();
        }
        element._dispatchEvent('ClickEvent', clickPointerEventData(event, node));
      });
      if (element._listOwner)
        node.addEventListener('dblclick', event => {
          if (event.target === node) element._listOwner.itemsChosen.Invoke(
            element._listOwner.selectedItems);
        });
      if (element._listOwner) {
        node.addEventListener('pointerdown', event => {
          const owner = element._listOwner;
          if ((!owner.reorderable && !owner.canStartDrag.hasListeners
            && !owner.setupDragAndDrop.hasListeners) || (event.button ?? 0) !== 0) return;
          this._collectionDrag = { owner: element._listOwner, from: element._listIndex,
            element, x: event.clientX, y: event.clientY,
            pointerId: event.pointerId, active: false };
          node.setPointerCapture?.(event.pointerId);
        });
        node.addEventListener('pointermove', event => {
          const drag = this._collectionDrag;
          if (!drag || drag.pointerId !== event.pointerId) return;
          const destination = this._collectionAtPoint(event, element._listOwner);
          if (!drag.active && Math.hypot(event.clientX - drag.x, event.clientY - drag.y) >= 5) {
            const owner = drag.owner;
            const id = owner instanceof TreeView
              ? owner.GetIdForIndex(drag.from) : drag.from;
            const selectedIds = owner instanceof TreeView
              ? owner.selectedIndices.map(index => owner.GetIdForIndex(index))
              : owner.selectedIndices;
            const args = { draggedElement: drag.element, id, selectedIds };
            if (owner.canStartDrag.Invoke(args, true) === false) {
              this._collectionDrag = null;
              this.elements.get(drag.element)?.releasePointerCapture?.(event.pointerId);
              return;
            }
            drag.active = true;
            if (owner instanceof ListView
              && owner.reorderMode === ListViewReorderMode.Animated) {
              const animated = this.elements.get(drag.element);
              if (animated) {
                drag._animatedNode = animated;
                drag._animatedStyles = { translate: animated.style.translate,
                  transition: animated.style.transition, zIndex: animated.style.zIndex };
                animated.style.transition = 'none';
                animated.style.zIndex = '1';
              }
            }
            const start = new StartDragArgs(String(owner._itemAt(drag.from) ?? ''));
            drag.startArgs = owner.setupDragAndDrop.Invoke({ draggedElement: drag.element,
              selectedIds, startDragArgs: start }, start) ?? start;
            drag.data = new DragAndDropData(owner, drag.startArgs);
          }
          if (drag.active && destination
            && (destination !== drag.owner || drag.owner.reorderable)
            && destination.dragAndDropUpdate.hasListeners)
            destination.dragAndDropUpdate.Invoke(this._collectionDropArgs(drag, event, destination),
              DragVisualMode.None);
          if (drag.active) {
            if (drag._animatedNode)
              drag._animatedNode.style.translate = `0px ${event.clientY - drag.y}px`;
            if (drag.hover !== destination) {
              this._collectionDragEvent(drag.hover, 'DragLeaveEvent', drag, event);
              this._collectionDragEvent(destination, 'DragEnterEvent', drag, event);
              drag.hover = destination;
            }
            this._collectionDragEvent(destination, 'DragUpdatedEvent', drag, event);
          }
          if (drag.active) { event.preventDefault?.(); event.stopPropagation?.(); }
        });
        node.addEventListener('pointerup', event => {
          const drag = this._collectionDrag;
          if (!drag || drag.pointerId !== event.pointerId) return;
          const destination = this._collectionAtPoint(event, element._listOwner);
          this._collectionDrag = null;
          this.elements.get(drag.element)?.releasePointerCapture?.(event.pointerId);
          this._finishAnimatedCollectionDrag(drag);
          if (!drag.active) return;
          if (destination) this._collectionDragEvent(destination, 'DragPerformEvent', drag, event);
          this._collectionDragEvent(drag.hover, 'DragExitedEvent', drag, event);
          if (!destination) return;
          if (destination === drag.owner && !drag.owner.reorderable) {
            node._suppressCollectionClick = true;
            event.preventDefault?.(); event.stopPropagation?.();
            return;
          }
          const args = this._collectionDropArgs(drag, event, destination);
          const handled = destination.handleDrop.Invoke(args, DragVisualMode.None);
          if (handled !== DragVisualMode.None || destination === drag.owner
            && drag.owner._moveItem(drag.from, args.insertAtIndex, args._asChild))
            node._suppressCollectionClick = true;
          event.preventDefault?.(); event.stopPropagation?.();
        });
        node.addEventListener('pointercancel', event => {
          if (this._collectionDrag?.pointerId === event.pointerId) {
            this._finishAnimatedCollectionDrag(this._collectionDrag);
            this._collectionDragEvent(this._collectionDrag.hover, 'DragExitedEvent',
              this._collectionDrag, event);
            this._collectionDrag = null;
          }
        });
      }
      for (const [domType, unityType] of [
        ['pointerdown', 'PointerDownEvent'], ['pointerup', 'PointerUpEvent'],
        ['pointermove', 'PointerMoveEvent'], ['pointercancel', 'PointerCancelEvent'],
        ['pointerover', 'PointerOverEvent'], ['pointerout', 'PointerOutEvent'],
        ['pointerenter', 'PointerEnterEvent'], ['pointerleave', 'PointerLeaveEvent'],
      ]) node.addEventListener(domType, event => {
        if (event.target !== node) return;
        if (domType === 'pointerup') node._lastPointerRelease = {
          pointerId: event.pointerId, pointerType: event.pointerType,
          clientX: event.clientX, clientY: event.clientY, at: Date.now() };
        else if (domType === 'pointercancel') node._lastPointerRelease = null;
        const payload = pointerEventData(event, node);
        element._dispatchEvent(unityType, payload,
          { trickles: !['PointerEnterEvent', 'PointerLeaveEvent'].includes(unityType),
            bubbles: !['PointerEnterEvent', 'PointerLeaveEvent'].includes(unityType),
            stopBeforeTarget: unityType === 'PointerDownEvent'
              && element instanceof UIElementsButton });
        if (payload.pointerType === 'mouse') {
          const mouseType = ({ PointerDownEvent: 'MouseDownEvent',
            PointerUpEvent: 'MouseUpEvent', PointerMoveEvent: 'MouseMoveEvent',
            PointerOverEvent: 'MouseOverEvent', PointerOutEvent: 'MouseOutEvent',
            PointerEnterEvent: 'MouseEnterEvent',
            PointerLeaveEvent: 'MouseLeaveEvent' })[unityType];
          if (mouseType) element._dispatchEvent(mouseType, {
            ...payload,
            mousePosition: new Vector2(payload.position.x, payload.position.y),
            localMousePosition: new Vector2(payload.localPosition.x, payload.localPosition.y),
            mouseDelta: new Vector2(payload.deltaPosition.x, payload.deltaPosition.y),
          }, { trickles: !['MouseEnterEvent', 'MouseLeaveEvent'].includes(mouseType),
            bubbles: !['MouseEnterEvent', 'MouseLeaveEvent'].includes(mouseType) });
        }
        if (domType === 'pointerenter' && payload.pointerType !== 'touch') {
          const tooltip = new TooltipEvent({ tooltip: element.tooltip ?? '',
            rect: element.worldBound, _domEvent: event });
          element._dispatchEvent('TooltipEvent', tooltip);
          node._tooltipOverride = tooltip.tooltip;
          node.title = tooltip.tooltip;
        } else if (domType === 'pointerleave') node._tooltipOverride = undefined;
      });
      node.addEventListener('wheel', event => {
        if (event.target !== node) return;
        element._dispatchEvent('WheelEvent', { ...pointerEventData(event, node),
          delta: new Vector3(event.deltaX ?? 0, event.deltaY ?? 0, event.deltaZ ?? 0) });
      });
      if (isToolkitField(element))
        for (const domType of element._textSelectionEnabled ? ['input', 'change']
          : [element instanceof PopupField || element instanceof UIElementsEnumField
            ? 'change' : 'input'])
          node.addEventListener(domType, event => {
            event.stopPropagation();
            if (domType === 'input' && element._textSelectionEnabled)
              element._dispatchEvent('InputEvent', {
                previousData: String(element.value ?? ''), newData: String(node.value ?? '') });
            if (element instanceof UIElementsHash128Field) {
              node._pendingHashValue = node.value;
              if (!node._unityComposing
                && (!element.isDelayed || domType === 'change')
                && /^[0-9a-f]{32}$/i.test(node.value)) {
                element.value = node.value;
                node._pendingHashValue = undefined;
              }
              return;
            }
            if (element instanceof UIElementsIntegerField
              || element instanceof UIElementsFloatField) {
              node._pendingNumericText = node.value;
              if (!event.isComposing && !node._unityComposing
                && (!element.isDelayed || domType === 'change')) element.value = node.value;
              return;
            }
            if (element instanceof UIElementsTextField
              && (event.isComposing || node._unityComposing)) {
              node._pendingTextValue = node.value;
              return;
            }
            if (element instanceof UIElementsTextField && element.isDelayed && domType === 'input') {
              node._pendingTextValue = node.value;
              return;
            }
            node._pendingTextValue = undefined;
            element.value = element instanceof UIElementsToggle ? node.checked
              : element instanceof PopupField && !(element instanceof UIElementsDropdownField)
                ? element.choices[Number(node.value)] : node.value;
          });
      if (element._textSelectionEnabled) {
        node.addEventListener('pointerup', () => {
          if (node._selectionOnFirstMouseUp && element.selectAllOnMouseUp)
            element.SelectAll();
          node._selectionOnFirstMouseUp = false;
        });
        node.addEventListener('compositionstart', () => { node._unityComposing = true; });
        node.addEventListener('compositionend', () => {
          node._unityComposing = false;
          if (element instanceof UIElementsHash128Field) {
            node._pendingHashValue = node.value;
            if (/^[0-9a-f]{32}$/i.test(node.value)) {
              element.value = node.value; node._pendingHashValue = undefined;
            }
          } else if (element instanceof UIElementsIntegerField
            || element instanceof UIElementsFloatField) {
            node._pendingNumericText = node.value;
            if (!element.isDelayed) element.value = node.value;
          } else if (element.isDelayed) node._pendingTextValue = node.value;
          else {
            node._pendingTextValue = undefined;
            element.value = node.value;
          }
        });
      }
      if (element instanceof UIElementsHash128Field)
        node.addEventListener('blur', () => {
          if (/^[0-9a-f]{32}$/i.test(node._pendingHashValue ?? ''))
            element.value = node._pendingHashValue;
          node._pendingHashValue = undefined;
          node.value = element.value.ToString();
        });
      if (element instanceof ScrollView)
        node.addEventListener('scroll', () => {
          element.scrollOffset = new Vector2(node.scrollLeft, node.scrollTop);
        });
      if (element instanceof ScrollView)
        node.addEventListener('wheel', event => {
          if (element.mouseWheelScrollSize === 18) return;
          const size = Math.max(0, Number(element.mouseWheelScrollSize) || 0);
          const amount = (event.deltaMode === 1 ? event.deltaY
            : Math.sign(event.deltaY) * Math.max(1, Math.abs(event.deltaY) / 100)) * size;
          if (element.mode === ScrollViewMode.Horizontal) node.scrollLeft += amount;
          else node.scrollTop += amount;
          element.scrollOffset = new Vector2(node.scrollLeft, node.scrollTop);
          event.preventDefault?.();
        });
      if (element instanceof Scroller)
        node.addEventListener('input', event => {
          event.stopPropagation?.(); element.value = Number(node.value);
        });
      if (element instanceof ListView || element instanceof TreeView)
        node.addEventListener('scroll', () => element._setViewport(node.scrollTop,
          node.clientHeight || Number.parseFloat(element.style.height) || 220));
      node.addEventListener('focus', () => {
        node._syncPopupOptionLabels?.(true);
        if (element._textSelectionEnabled) {
          element._placeholderFocused = true;
          if (element.hidePlaceholderOnFocus) node.placeholder = '';
          node._selectionOnFirstMouseUp = true;
          if (element.selectAllOnFocus) element.SelectAll();
        }
        this._setFocus(element);
      });
      node.addEventListener('blur', event => {
        node._syncPopupOptionLabels?.(false);
        if (element._textSelectionEnabled) {
          element._placeholderFocused = false;
          node.placeholder = element.placeholder ?? '';
        }
        if (element instanceof UIElementsTextField && node._pendingTextValue !== undefined) {
          element.value = node.value;
          node._pendingTextValue = undefined;
        }
        if (element instanceof UIElementsIntegerField
          || element instanceof UIElementsFloatField) {
          if (node._pendingNumericText !== undefined) element.value = node.value;
          node._pendingNumericText = undefined;
        }
        this._setFocus(event.relatedTarget?._visualElement ?? null);
      });
      for (const [domType, unityType] of [['keydown', 'KeyDownEvent'], ['keyup', 'KeyUpEvent']])
        node.addEventListener(domType, event => {
          if (event.target !== node) return;
          if ((element instanceof ListView || element instanceof TreeView) && domType === 'keydown') {
            if (element.OnKeyDown({ keyCode: browserKeyCode(event.code) }))
              event.preventDefault?.();
          }
          const modifiers = (event.shiftKey ? EventModifiers.Shift : 0)
            | (event.ctrlKey ? EventModifiers.Control : 0)
            | (event.altKey ? EventModifiers.Alt : 0)
            | (event.metaKey ? EventModifiers.Command : 0);
          element._dispatchEvent(unityType, {
            keyCode: browserKeyCode(event.code),
            character: event.key?.length === 1 ? event.key : '\0',
            shiftKey: event.shiftKey, ctrlKey: event.ctrlKey,
            altKey: event.altKey, commandKey: event.metaKey,
            actionKey: !!(globalThis.navigator?.platform?.includes?.('Mac')
              ? event.metaKey : event.ctrlKey),
            modifiers, _domEvent: event,
          });
          if (domType === 'keydown' && !event.isComposing && !event.defaultPrevented
            && !element._textSelectionEnabled) {
            const direction = ({ ArrowLeft: 1, ArrowUp: 2,
              ArrowRight: 3, ArrowDown: 4 })[event.key];
            if (direction) element._dispatchEvent('NavigationMoveEvent', {
              direction, modifiers,
              move: new Vector2(direction === 3 ? 1 : direction === 1 ? -1 : 0,
                direction === 2 ? 1 : direction === 4 ? -1 : 0),
              _domEvent: event });
            else if (event.key === 'Enter')
              element._dispatchEvent('NavigationSubmitEvent', { modifiers, _domEvent: event });
            else if (event.key === 'Escape')
              element._dispatchEvent('NavigationCancelEvent', { modifiers, _domEvent: event });
          }
        });
    }
    element._browserNode = node;
    const view = node._wrapper ?? node;
    if (!view._uiContextClickBound) {
      view._uiContextClickBound = true;
      view.addEventListener('contextmenu', event => {
        event.stopPropagation?.();
        const pointer = pointerEventData(event, node);
        const contextClick = new ContextClickEvent({ ...pointer,
          mousePosition: new Vector2(pointer.position.x, pointer.position.y),
          localMousePosition: new Vector2(pointer.localPosition.x,
            pointer.localPosition.y), mouseDelta: new Vector2() });
        element._dispatchEvent('ContextClickEvent', contextClick);
        const menu = new DropdownMenu();
        const contextual = new ContextualMenuPopulateEvent(menu, contextClick);
        contextual.target = element;
        contextual.mousePosition = contextClick.mousePosition;
        contextual.localMousePosition = contextClick.localMousePosition;
        contextual.modifiers = contextClick.modifiers;
        element._dispatchEvent('ContextualMenuPopulateEvent', contextual);
        const items = menu.PrepareForDisplay(contextClick);
        if (items.length) {
          event.preventDefault?.();
          view._contextMenu?.Close();
          const popup = new GenericDropdownMenu();
          for (const item of items) {
            if (item.separator) { popup.AddSeparator(item.path); continue; }
            const checked = Boolean(item.status & DropdownMenuAction.Status.Checked);
            if (item.status === DropdownMenuAction.Status.None
              || item.status & DropdownMenuAction.Status.Disabled)
              popup.AddDisabledItem(item.name, checked);
            else popup.AddItem(item.name, checked, () => item.context.Execute());
          }
          let panel = view;
          while (panel && !panel._uiPanelScale) panel = panel.parentNode ?? panel.host;
          const origin = panel?.getBoundingClientRect?.() ?? { left: 0, top: 0 };
          const scale = panel?._uiPanelScale || 1;
          popup.DropDown({ x: ((event.clientX ?? origin.left) - origin.left) / scale,
            y: ((event.clientY ?? origin.top) - origin.top) / scale,
            width: 0, height: 0 }, element, false, true);
          view._contextMenu = popup;
        }
        if (contextClick.isDefaultPrevented) event.preventDefault?.();
      });
    }
    if (!view._uiTransitionsBound) {
      view._uiTransitionsBound = true;
      for (const [domType, unityType] of [
        ['transitionrun', 'TransitionRunEvent'],
        ['transitionstart', 'TransitionStartEvent'],
        ['transitionend', 'TransitionEndEvent'],
        ['transitioncancel', 'TransitionCancelEvent'],
      ]) view.addEventListener(domType, event => {
        if (event.target !== view) return;
        element._dispatchEvent(unityType, {
          stylePropertyNames: [event.propertyName],
          elapsedTime: event.elapsedTime ?? 0,
          _domEvent: event,
        });
      });
    }
    view.id = element.name;
    view.className = [...element._classes,
      ...(element.enabledInHierarchy ? [] : [VisualElement.disabledUssClassName]),
      ...(element._activePseudoState ? ['unity-active'] : []),
      ...(element._checkedPseudoState ? ['unity-checked'] : []),
      ...(element.showMixedValue ? ['unity-mixed-value'] : [])].join(' ');
    view.dataset.uiType = element.constructor.name.replace(/^UIElements/, '');
    if (element._labelElement)
      element._labelElement._browserNode = view._caption ?? node._radioCaption
        ?? node._toggleCaption ?? node._multiCaption ?? null;
    view.dir = element.languageDirection === 2 ? 'rtl'
      : element.languageDirection === 1 ? 'ltr' : '';
    if (element._styleScopeId) view.dataset.uiStyleScope = String(element._styleScopeId);
    else delete view.dataset.uiStyleScope;
    view.dataset.enabled = String(element.enabledInHierarchy);
    node.tabIndex = element.canGrabFocus ? element.tabIndex : -1;
    if (element instanceof UIElementsRadioButtonGroup) {
      node._radioCaption.textContent = element.label;
      const key = JSON.stringify(element.choices);
      if (node._choicesKey !== key) {
        for (const option of node._radioOptions) option.remove();
        node._radioOptions = element.choices.map((choice, index) => {
          const option = this.host.ownerDocument.createElement('label');
          const input = this.host.ownerDocument.createElement('input');
          input.type = 'radio';
          input.name = element._radioGroupName ??= `toolkit-radio-${++radioGroupId}`;
          input.value = String(index);
          input.addEventListener('change', event => {
            event.stopPropagation();
            if (input.checked) element.value = index;
          });
          option.appendChild(input);
          const caption = this.host.ownerDocument.createElement('span');
          caption.textContent = choice;
          option.appendChild(caption);
          node.appendChild(option);
          return option;
        });
        node._choicesKey = key;
      }
      node._radioOptions.forEach((option, index) => {
        option.children[0].checked = element.value === index;
      });
    } else if (element instanceof ToggleButtonGroup) {
      node._toggleCaption.textContent = element.label;
      node._toggleCaption.style.display = element.label ? '' : 'none';
      node._toggleButtons.style.display = 'flex';
      node._toggleButtons.style.flexDirection = 'row';
    } else if (element instanceof TwoPaneSplitView) {
      view.style.display = 'flex';
      view.style.position = 'relative';
      view.style.flexDirection = element.orientation === TwoPaneSplitViewOrientation.Horizontal
        ? 'row' : 'column';
    } else if (isCompoundField(element)) {
      node._multiCaption.textContent = element.label;
      const axes = compoundAxes(element);
      node._multiInputs.forEach((input, index) => {
        if (input._pendingComponentText === undefined)
          input.value = String(element._components
            ? element._read(element.value, axes[index]) : element.value[axes[index]]);
        if (element instanceof UIElementsMinMaxSlider) {
          input.min = String(element.lowLimit); input.max = String(element.highLimit);
        }
      });
    } else if (element instanceof Scroller) {
      node.type = 'range';
      node.min = String(element.lowValue);
      node.max = String(Number.isFinite(element.highValue) ? element.highValue : 100);
      node.value = String(element.value);
      node.style.writingMode = element.direction === SliderDirection.Vertical
        ? 'vertical-lr' : '';
    } else if (element instanceof HelpBox) {
      node._helpText.textContent = element.text;
      node._helpIcon.textContent = ['', 'ℹ', '⚠', '⛔'][element.messageType] ?? '';
      node.dataset.messageType = Object.keys(HelpBoxMessageType)
        .find(key => HelpBoxMessageType[key] === element.messageType) ?? 'None';
      node.style.display = 'flex';
    } else if (element instanceof GroupBox || element instanceof PopupWindow) {
      renderToolkitText(node._groupTitle, element.text,
        element instanceof PopupWindow && element.enableRichText);
      node._groupTitle.style.display = element.text ? '' : 'none';
      if (element instanceof PopupWindow) node.dataset.popupWindow = '';
    } else if (element instanceof UIElementsFoldout) {
      node._foldoutArrow.textContent = element.value ? '▾ ' : '▸ ';
      node._foldoutText.textContent = element.text;
      node._foldoutContent.style.display = element.value ? 'flex' : 'none';
      node._foldoutContent.style.flexDirection = 'column';
    } else if (element instanceof UIElementsToggle) {
      node.type = element instanceof UIElementsRadioButton ? 'radio' : 'checkbox';
      if (element instanceof UIElementsRadioButton) {
        let group = element.parent;
        while (group?.parent && !(group instanceof GroupBox)) group = group.parent;
        if (group) {
          group._radioGroupName ??= `toolkit-group-${++radioGroupId}`;
          node.name = group._radioGroupName;
        }
      }
      node.checked = element.value;
      view._caption.textContent = element.label;
      view._boolText.textContent = element.text ?? '';
      view._boolText.style.display = element.text ? '' : 'none';
      node.setAttribute?.('aria-label', element.label);
    } else if (element instanceof UIElementsHash128Field) {
      node.type = element.isPasswordField ? 'password' : 'text';
      node.maxLength = element.maxLength;
      if (node._pendingHashValue === undefined) node.value = element.value.ToString();
      if (element._selectionDirty) element._applySelection();
      node.readOnly = element.isReadOnly;
      node.placeholder = element.hidePlaceholderOnFocus && element._placeholderFocused
        ? '' : element.placeholder;
      node.spellcheck = element.autoCorrection;
      view._caption.textContent = element.label;
      node.setAttribute?.('aria-label', element.label);
    } else if (element instanceof UIElementsTextField) {
      node.style.boxSizing = 'border-box';
      node.style.height = '100%';
      if (!element.multiline) node.type = element.isPasswordField ? 'password' : 'text';
      else node.style.webkitTextSecurity = element.isPasswordField ? 'disc' : 'none';
      if (node.value !== element.value && node._pendingTextValue === undefined)
        node.value = element.value;
      if (element._selectionDirty) element._applySelection();
      node.readOnly = element.isReadOnly;
      node.placeholder = element.hidePlaceholderOnFocus && element._placeholderFocused
        ? '' : element.placeholder;
      node.spellcheck = element.autoCorrection;
      node.inputMode = ({ 3: 'url', 4: 'numeric', 5: 'tel', 7: 'email',
        10: 'search', 11: 'decimal' })[element.keyboardType] ?? 'text';
      node.style.caretColor = browserStyleValue('color', element.cursorColor);
      node.style.setProperty?.('--unity-selection-color',
        browserStyleValue('backgroundColor', element.selectionColor));
      node.style.overflowY = element.multiline
        && element.verticalScrollerVisibility !== ScrollerVisibility.Hidden
        ? element.verticalScrollerVisibility === ScrollerVisibility.AlwaysVisible
          ? 'scroll' : 'auto' : 'hidden';
      if (element.maxLength >= 0) node.maxLength = element.maxLength;
      else node.removeAttribute?.('maxlength');
      view._caption.textContent = element.label;
      node.setAttribute?.('aria-label', element.label);
      if (element.isPasswordField && element.maskChar !== '*')
        this.diagnostics.set(element, `UI Toolkit TextField custom maskChar on ${element.name || 'unnamed field'}`);
      if (element.hideMobileInput || element.hideSoftKeyboard)
        this.diagnostics.set(element, `${this.diagnostics.get(element) ?? ''} UI Toolkit native soft-keyboard flags on ${element.name || 'unnamed field'}`.trim());
    } else if (element instanceof UIElementsSlider) {
      node.type = 'range';
      node.min = String(element.lowValue); node.max = String(element.highValue);
      node.step = element instanceof UIElementsSliderInt ? '1' : 'any';
      node.value = String(element.value);
      const vertical = element.direction === SliderDirection.Vertical;
      node.style.writingMode = vertical ? 'vertical-lr' : '';
      node.style.direction = element.inverted ? 'rtl' : 'ltr';
      const progress = Math.max(0, Math.min(100,
        100 * (element.value - element.lowValue) / (element.range || 1)));
      node.style.backgroundImage = element.fill
        ? `linear-gradient(${vertical ? 'to top' : 'to right'}, currentColor ${progress}%, transparent ${progress}%)`
        : '';
      if (view._sliderInput) {
        view._sliderInput.style.display = element.showInputField ? '' : 'none';
        view._sliderInput.value = String(element.value);
        view._sliderInput.min = String(element.lowValue);
        view._sliderInput.max = String(element.highValue);
        view._sliderInput.step = element instanceof UIElementsSliderInt ? '1' : 'any';
      }
      view._caption.textContent = element.label;
      node.setAttribute?.('aria-label', element.label);
    } else if (element instanceof UIElementsProgressBar) {
      node.max = 1;
      node.value = Math.max(0, Math.min(1,
        (element.value - element.lowValue) / (element.highValue - element.lowValue || 1)));
      view._caption.textContent = element.title;
    } else if (element instanceof UIElementsIntegerField
      || element instanceof UIElementsFloatField) {
      if (element.formatString && !/^0(?:\.0+|\.#+)?$/.test(element.formatString))
        this.diagnostics.set(element,
          `UI Toolkit numeric format ${element.formatString} on ${element.name || 'unnamed field'} is outside browser text formatting`);
      node.type = element.isPasswordField ? 'password' : 'text';
      node.inputMode = ({ 3: 'url', 4: 'numeric', 5: 'tel', 7: 'email',
        10: 'search', 11: 'decimal' })[element.keyboardType]
        ?? (element instanceof UIElementsIntegerField ? 'numeric' : 'decimal');
      node.spellcheck = element.autoCorrection;
      node.style.caretColor = browserStyleValue('color', element.cursorColor);
      node.style.setProperty?.('--unity-selection-color',
        browserStyleValue('backgroundColor', element.selectionColor));
      if (element.hideMobileInput || element.hideSoftKeyboard)
        this.diagnostics.set(element, `UI Toolkit native soft-keyboard flags on ${element.name || 'unnamed field'}`);
      if (node._pendingNumericText === undefined) node.value = element.text;
      if (element._selectionDirty) element._applySelection();
      node.readOnly = element.isReadOnly;
      node.placeholder = element.hidePlaceholderOnFocus && element._placeholderFocused
        ? '' : element.placeholder;
      if (element.maxLength >= 0) node.maxLength = element.maxLength;
      else node.removeAttribute?.('maxlength');
      view._caption.textContent = element.label;
      node.setAttribute?.('aria-label', element.label);
    } else if (element instanceof UIElementsEnumField) {
      const choices = globalThis.Object.entries(element._enumType ?? {});
      if (!choices.length && element.value != null)
        this.diagnostics.set(element,
          `UI Toolkit EnumField choices unavailable on ${element.name || 'unnamed field'}`);
      if (!choices.length && element.value != null) choices.push([element.text, element.value]);
      const key = JSON.stringify(choices);
      if (node._choicesKey !== key) {
        for (const option of Array.from(node.children)) option.remove();
        for (const [name, value] of choices) {
          const option = this.host.ownerDocument.createElement('option');
          option.value = String(value); option.textContent = name;
          node.appendChild(option);
        }
        node._choicesKey = key;
      }
      node.value = String(element.value ?? '');
      view._caption.textContent = element.label;
      node.setAttribute?.('aria-label', element.label);
    } else if (element instanceof PopupField) {
      const choices = Array.from(element.choices ?? []);
      if (!node._choiceValues || node._choiceValues.length !== choices.length
        || choices.some((choice, index) => node._choiceValues[index] !== choice)) {
        for (const option of [...node.children]) option.remove();
        for (const [index, choice] of choices.entries()) {
          const option = this.host.ownerDocument.createElement('option');
          option.value = element instanceof UIElementsDropdownField
            ? String(choice) : String(index);
          node.appendChild(option);
        }
        node._choiceValues = choices;
      }
      node._syncPopupOptionLabels = (open = false) => {
        for (const [index, option] of Array.from(node.children).entries()) {
          const choice = choices[index];
          const formatter = !open && index === element.index
            ? element.formatSelectedValueCallback : element.formatListItemCallback;
          option.textContent = String(formatter?.(choice) ?? choice ?? '');
        }
      };
      node._syncPopupOptionLabels(this._focusedElement === element);
      node.value = element instanceof UIElementsDropdownField
        ? element.value : String(element.index);
      view._caption.textContent = element.label;
      node.setAttribute?.('aria-label', element.label);
    } else if (element instanceof TextElement) {
      if (!element.emojiFallbackSupport)
        this.diagnostics.set(element,
          `UI Toolkit emoji fallback is browser-owned on ${element.name || 'unnamed element'}`);
      if (element.enableRichText
        && /<(?:sprite|gradient|style|link|font|material|mark)\b/i.test(element.text))
        this.diagnostics.set(element,
          `UI Toolkit rich text asset/style tag is outside browser DOM text on ${element.name || 'unnamed element'}`);
      const content = element.parseEscapeSequences
        ? String(element.text).replace(/\\([ntr\\])/g, (_, escape) =>
          ({ n: '\n', t: '\t', r: '\r', '\\': '\\' })[escape])
        : String(element.text);
      const displayText = element._listOwner instanceof TreeView
        ? `${element._listOwner._visibleTreeItems[element._listIndex]?.children.length
          ? element._listOwner.IsExpanded(element._listOwner._visibleTreeItems[element._listIndex].id)
            ? '▾ ' : '▸ ' : '  '}${content}` : content;
      const iconAsset = element instanceof UIElementsButton
        ? element.iconImage?.GetSelectedImage?.() : null;
      const iconUrl = iconAsset?.threeTexture?.image?.src
        ?? iconAsset?.texture?.threeTexture?.image?.src
        ?? iconAsset?.image?.src ?? iconAsset?.url
        ?? (typeof iconAsset === 'string' ? element._imageUrls?.get(iconAsset) : null);
      if (element instanceof UIElementsButton && iconUrl) {
        if (!node._buttonIcon) {
          node.textContent = '';
          node._buttonIcon = this.host.ownerDocument.createElement('img');
          node._buttonIcon.style.width = '1em';
          node._buttonIcon.style.height = '1em';
          node._buttonIcon.style.objectFit = 'contain';
          node._buttonIcon.style.verticalAlign = 'middle';
          node._buttonIcon.style.marginRight = '0.35em';
          node._buttonIcon.setAttribute?.('aria-hidden', 'true');
          node._buttonText = this.host.ownerDocument.createElement('span');
          node.appendChild(node._buttonIcon); node.appendChild(node._buttonText);
        }
        node._buttonIcon.src = this._imageVariant(iconUrl,
          { sprite: element.iconImage?.sprite, name: element.name }, node) || iconUrl;
        renderToolkitText(node._buttonText, displayText, element.enableRichText);
      } else {
        if (node._buttonIcon) {
          node._buttonIcon.remove(); node._buttonText.remove();
          node._buttonIcon = null; node._buttonText = null;
          node._renderedTextKey = null;
        }
        renderToolkitText(node, displayText, element.enableRichText);
        if (iconAsset && !iconUrl)
          this.diagnostics.set(element,
            `UI Toolkit Button icon source unresolved on ${element.name || 'unnamed button'}`);
      }
      if (element.displayTooltipWhenElided)
        node.title = node._tooltipOverride ?? (element.isElided
          ? element.text : element.tooltip ?? '');
    }
    if (!(element instanceof TextElement && element.displayTooltipWhenElided))
      node.title = node._tooltipOverride ?? element.tooltip ?? '';
    if (element instanceof UIElementsButton || isToolkitField(element))
      node.disabled = !!element._disabled || !element.enabledInHierarchy;
    const inlineKeys = new Set(Object.keys(element.style));
    for (const key of view._inlineStyleKeys ?? [])
      if (!inlineKeys.has(key)) view.style[key] = '';
    view._inlineStyleKeys = inlineKeys;
    for (const [property, value] of Object.entries(element.style))
      view.style[property] = browserStyleValue(property, value);
    view.style.flexDirection = element.style.flexDirection == null
      ? (element instanceof UIElementsToggle && !(element instanceof UIElementsFoldout)
        ? 'row' : '')
      : browserStyleValue('flexDirection', element.style.flexDirection);
    view.style.pointerEvents = !element.enabledInHierarchy ? 'none' : element.style.pointerEvents
      ?? (element instanceof TemplateContainer ? 'none' : '');
    view.style.visibility = element.visible
      ? browserStyleValue('visibility', element.style.visibility) : 'hidden';
    const authoredFontStyle = element.style.unityFontStyleAndWeight
      ?? element.style.unityFontStyle;
    const fontStyleValue = authoredFontStyle?.value ?? authoredFontStyle;
    const fontStyle = typeof fontStyleValue === 'number'
      ? Object.keys(FontStyle).find(key => FontStyle[key] === fontStyleValue)?.toLowerCase() ?? ''
      : String(browserStyleValue('unityFontStyleAndWeight', authoredFontStyle) ?? '').toLowerCase();
    view.style.fontWeight = fontStyle ? (fontStyle.includes('bold') ? 'bold' : 'normal')
      : (element.style.fontWeight ?? '');
    view.style.fontStyle = fontStyle ? (fontStyle.includes('italic') ? 'italic' : 'normal')
      : (element.style.fontStyle ?? '');
    const authoredAlignment = element.style.unityTextAlign;
    const alignmentValue = authoredAlignment?.value ?? authoredAlignment;
    const alignment = (typeof alignmentValue === 'number'
      ? Object.keys(TextAnchor).find(key => TextAnchor[key] === alignmentValue) ?? ''
      : String(browserStyleValue('unityTextAlign', authoredAlignment) ?? ''))
      .replace(/([a-z])([A-Z])/g, '$1-$2').toLowerCase();
    view.style.textAlign = alignment ? alignment.split('-').at(-1)
      : (element.style.textAlign ?? '');
    view.style.justifyContent = alignment
      ? ({ upper: 'flex-start', middle: 'center', lower: 'flex-end' }
        [alignment.split('-')[0]] ?? '')
      : browserStyleValue('justifyContent', element.style.justifyContent);
    view.style.fontFamily = element.style.fontFamily ?? '';
    const background = element.style.backgroundImage;
    const backgroundValue = background?.value ?? background;
    const backgroundAsset = backgroundValue?.sprite ?? backgroundValue?.texture
      ?? backgroundValue;
    const backgroundSource = backgroundAsset?.threeTexture?.image?.src
      ?? backgroundAsset?.texture?.threeTexture?.image?.src
      ?? backgroundAsset?.image?.src ?? backgroundAsset?.url;
    const backgroundReference = typeof background === 'string'
      ? /^url\(\s*["']?([^"')]+)["']?\s*\)$/i.exec(background)?.[1] : null;
    const backgroundUrl = backgroundSource
      ?? element._imageUrls?.get(backgroundReference?.trim());
    const backgroundTint = element.style.unityBackgroundImageTintColor?.value
      ?? element.style.unityBackgroundImageTintColor;
    const backgroundVariant = backgroundUrl
      ? this._imageVariant(backgroundUrl,
        { sprite: backgroundValue?.sprite, name: element.name,
          tintColor: backgroundTint, tintAlphaInPixels: true }, view) : '';
    view.style.backgroundImage = backgroundVariant
      ? `url(${JSON.stringify(backgroundVariant)})`
      : backgroundUrl && !backgroundValue?.sprite ? `url(${JSON.stringify(backgroundUrl)})`
        : typeof background === 'string' ? background
          : background?.keyword && background.keyword !== 'Undefined'
            ? browserStyleValue('backgroundImage', background.keyword) : '';
    const authoredBackgroundScale = element.style.unityBackgroundScaleMode;
    const backgroundScale = authoredBackgroundScale?.value ?? authoredBackgroundScale;
    if (backgroundScale != null) {
      view.style.backgroundSize = backgroundScale === ScaleMode.ScaleAndCrop
        ? 'cover' : backgroundScale === ScaleMode.ScaleToFit ? 'contain' : '100% 100%';
      view.style.backgroundPosition = 'center';
      view.style.backgroundRepeat = 'no-repeat';
    }
    const outlineColor = element.style.unityTextOutlineColor;
    const outlineWidth = element.style.unityTextOutlineWidth;
    if (outlineColor != null)
      view.style.webkitTextStrokeColor = browserStyleValue('color', outlineColor);
    if (outlineWidth != null)
      view.style.webkitTextStrokeWidth = `${Number(outlineWidth?.value ?? outlineWidth)}px`;
    if (element instanceof UIElementsImage) {
      const source = element.image?.threeTexture?.image?.src
        ?? element.image?.image?.src ?? element.sprite?.texture?.threeTexture?.image?.src
        ?? element.image?.url ?? element.sprite?.url;
      const inline = element.style.unityImage;
      const reference = /^url\(\s*["']?([^"')]+)["']?\s*\)$/i.exec(inline ?? '')?.[1];
      const url = source ?? element._imageUrls?.get(reference?.trim());
      const variant = url ? this._imageVariant(url, element, view) : '';
      view.style.backgroundImage = variant ? `url(${JSON.stringify(variant)})`
        : url || inline ? '' : 'var(--unity-image)';
      if (inline && !url) this.diagnostics.set(element,
        `UI Toolkit Image source unresolved on ${element.name || 'unnamed image'}`);
      const mode = element.style.unityImageSize ?? element.scaleMode;
      view.style.backgroundSize = mode === ScaleMode.ScaleAndCrop || mode === 'scale-and-crop'
        ? 'cover' : mode === ScaleMode.StretchToFill || mode === 'stretch-to-fill'
          ? '100% 100%' : 'contain';
      view.style.backgroundPosition = 'center';
      view.style.backgroundRepeat = 'no-repeat';
      view.style.opacity = String(element.tintColor?.a ?? 1);
    }
    if (element.style.unityFont || element.style.unityFontDefinition) {
      const authored = element.style.unityFontDefinition ?? element.style.unityFont;
      const fontKeyword = authored?.keyword && authored.keyword !== 'Undefined';
      const definition = authored?.value ?? authored;
      const selectedFont = definition?.fontAsset ?? definition?.font ?? definition;
      if (fontKeyword) view.style.fontFamily = '';
      else if (selectedFont?.family || selectedFont?.name)
        view.style.fontFamily = JSON.stringify(selectedFont.family ?? selectedFont.name);
      const match = /^url\(\s*["']?([^"')]+)["']?\s*\)$/i.exec(
        typeof definition === 'string' ? definition : '');
      const font = element._fontUrls?.get(match?.[1]?.trim());
      if (font) view.style.fontFamily = [font, ...(font.fallbacks ?? [])]
        .map(face => JSON.stringify(face.family)).join(', ');
      else if (!fontKeyword && !selectedFont?.family && !selectedFont?.name)
        this.diagnostics.set(element,
        `UI Toolkit inline font unresolved on ${element.name || 'unnamed element'}`);
    }
    const autoSize = element.style.unityTextAutoSize?.value
      ?? element.style.unityTextAutoSize;
    if (autoSize?.mode === 1 && node?.style) {
      node._autoSized = true;
      const minimum = Math.max(1, Number(autoSize.minSize?.value ?? 1));
      const maximum = Math.max(minimum, Number(autoSize.maxSize?.value ?? minimum));
      node.style.fontSize = `${maximum}px`;
      const width = Number(view.clientWidth) || 0;
      const height = Number(view.clientHeight) || 0;
      if (width > 0 && Number(node.scrollWidth) > width
        || height > 0 && Number(node.scrollHeight) > height) {
        let low = minimum, high = maximum;
        for (let attempt = 0; attempt < 7; attempt++) {
          const middle = (low + high) / 2;
          node.style.fontSize = `${middle}px`;
          if (width > 0 && Number(node.scrollWidth) > width
            || height > 0 && Number(node.scrollHeight) > height) high = middle;
          else low = middle;
        }
        node.style.fontSize = `${low}px`;
      }
    } else if (node?._autoSized) {
      node.style.fontSize = element.style.fontSize == null
        ? '' : browserStyleValue('fontSize', element.style.fontSize);
      node._autoSized = false;
    }
    if (element.style.backgroundImage) {
      const match = /^url\(\s*["']?([^"')]+)["']?\s*\)$/i.exec(element.style.backgroundImage);
      const url = element._imageUrls?.get(match?.[1]?.trim());
      if (url) view.style.backgroundImage = `url(${JSON.stringify(url)})`;
      else this.diagnostics.set(element,
        `UI Toolkit inline image unresolved on ${element.name || 'unnamed element'}`);
    }
    if (element instanceof ScrollView) {
      view.style.overflowX = element.mode === ScrollViewMode.Vertical
        || element.horizontalScrollerVisibility === ScrollerVisibility.Hidden ? 'hidden'
          : element.showHorizontal
            || element.horizontalScrollerVisibility === ScrollerVisibility.AlwaysVisible
            ? 'scroll' : 'auto';
      view.style.overflowY = element.mode === ScrollViewMode.Horizontal
        || element.verticalScrollerVisibility === ScrollerVisibility.Hidden ? 'hidden'
          : element.showVertical
            || element.verticalScrollerVisibility === ScrollerVisibility.AlwaysVisible
            ? 'scroll' : 'auto';
      view.style.overscrollBehavior = element.nestedInteractionKind
        === ScrollView.NestedInteractionKind.StopScrolling ? 'contain' : 'auto';
    }
    if (element instanceof TabView) {
      view.style.display = element.style.display ?? 'flex';
      view.style.flexDirection = element.style.flexDirection ?? 'column';
      node._tabHeaders.style.display = 'flex';
      node._tabContent.style.display = 'flex';
      node._tabContent.style.flexDirection = 'column';
      for (const [tab, header] of node._tabHeaderButtons)
        if (!element.children.includes(tab)) {
          header.remove(); node._tabHeaderButtons.delete(tab);
        }
      for (const tab of element.children) {
        let header = node._tabHeaderButtons.get(tab);
        if (!header) {
          header = this.host.ownerDocument.createElement('div');
          const button = this.host.ownerDocument.createElement('button');
          const icon = this.host.ownerDocument.createElement('img');
          const caption = this.host.ownerDocument.createElement('span');
          button.appendChild(icon); button.appendChild(caption);
          button.addEventListener('click', event => {
            event.stopPropagation?.(); element.activeTab = tab;
            tab.tabHeader._dispatchEvent('ClickEvent', pointerEventData(event, button));
          });
          button.addEventListener('dragstart', () => { node._draggedTab = tab; });
          button.addEventListener('dragover', event => event.preventDefault?.());
          button.addEventListener('drop', event => {
            event.preventDefault?.();
            if (element.reorderable && node._draggedTab)
              element.ReorderTab(element.children.indexOf(node._draggedTab),
                element.children.indexOf(tab));
            node._draggedTab = null;
          });
          header.appendChild(button);
          const close = this.host.ownerDocument.createElement('button');
          close.addEventListener('click', event => {
            event.stopPropagation?.(); element._closeTab(tab);
          });
          header.appendChild(close);
          header._tabButton = button; header._tabIcon = icon;
          header._tabCaption = caption; header._closeButton = close;
          node._tabHeaderButtons.set(tab, header);
        }
        header._tabCaption.textContent = tab.label;
        const iconUrl = typeof tab.iconImage === 'string'
          ? tab._imageUrls?.get(tab.iconImage) : tab.iconImage?.url;
        header._tabIcon.style.display = iconUrl ? '' : 'none';
        if (iconUrl) header._tabIcon.src = iconUrl;
        header._tabButton.draggable = element.reorderable;
        header._tabButton.setAttribute?.('aria-selected', String(element.activeTab === tab));
        header._closeButton.textContent = '×';
        header._closeButton.style.display = tab.closeable ? '' : 'none';
        tab.tabHeader._browserNode = header._tabButton;
        node._tabHeaders.appendChild(header);
      }
    }
    if (element instanceof Tab)
      view.style.display = element.parent?.activeTab === element ? '' : 'none';
    if (element instanceof Box && element.style.border == null)
      view.style.border = '1px solid #777';
    if (element instanceof ListView || element instanceof TreeView) {
      view.style.overflowY = 'auto';
      view.style.overflowX = element.horizontalScrollingEnabled ? 'auto' : 'hidden';
      view.style.overflowAnchor = 'none';
      view.style.border = element.showBorder ? '1px solid #777' : '';
    }
    if (element._listOwner) {
      view.style.flexShrink = '0';
      view.style.touchAction = element._listOwner.reorderable ? 'none' : '';
      view.style.height = element._listOwner.virtualizationMethod
        === CollectionVirtualizationMethod.DynamicHeight ? ''
          : `${element._listOwner.fixedItemHeight}px`;
      if (element._listOwner instanceof TreeView)
        view.style.paddingLeft = `${element._listOwner._visibleTreeItems[element._listIndex]?.depth * 16 ?? 0}px`;
    }
    if (element._multiCells) for (const [column, cell] of element._multiCells) {
      const width = Number(column.width?.value ?? column.width);
      cell.style.flex = width > 0
        ? `${column.stretchable ? '1 1' : '0 0'} ${width}px` : '1 1 0';
      cell.style.minWidth = `${Math.max(0,
        Number(column.minWidth?.value ?? column.minWidth) || 0)}px`;
      cell.style.maxWidth = Number(column.maxWidth?.value ?? column.maxWidth) > 0
        ? `${Number(column.maxWidth?.value ?? column.maxWidth)}px` : '';
    }
    const scrollParent = element._scrollViewOwner ?? null;
    if (scrollParent && element === scrollParent.contentContainer)
      view.style.flexDirection = scrollParent.mode === ScrollViewMode.Horizontal ? 'row' : 'column';
    if (scrollParent && (element === scrollParent.contentContainer
      || element === scrollParent.contentViewport)) {
      view.style.flexShrink = '0';
      if (scrollParent.mode === ScrollViewMode.Horizontal) view.style.width = 'max-content';
    }
    view._uiElement = element;
    if ((view.parentNode ?? view.parent) !== parent) parent.appendChild(view);
    let collectionScrollTarget = null;
    let collectionScrollIndex = null;
    if (element instanceof ListView || element instanceof TreeView) {
      const headerHeight = node._columnHeader?.offsetHeight ?? 0;
      if (Number.isInteger(element._scrollToIndex))
        collectionScrollIndex = element._scrollToIndex;
      if (collectionScrollIndex != null && element.virtualizationMethod
        === CollectionVirtualizationMethod.FixedHeight)
        collectionScrollTarget = Math.max(0, element._scrollToIndex) * element.fixedItemHeight
          + headerHeight;
      element._setViewport(Math.max(0, (collectionScrollTarget ?? view.scrollTop) - headerHeight),
        (view.clientHeight || Number.parseFloat(element.style.height) || 220) - headerHeight);
    }
    const childHost = node._foldoutContent ?? node._tabContent
      ?? node._groupContent ?? node._toggleButtons ?? view;
    for (const child of element.children) this._render(child, childHost, seen);
    if (element instanceof UIElementsButton && element.parent instanceof ToggleButtonGroup) {
      const selected = !!element.parent.value[element.parent.children.indexOf(element)];
      node.setAttribute?.('aria-pressed', String(selected));
      node.dataset.selected = String(selected);
      if (!element.style.backgroundColor)
        node.style.backgroundColor = selected ? 'rgba(88, 106, 129, .95)' : '';
    }
    const orderedChildren = element.children.map(child => {
      const node = this.elements.get(child);
      return node?._wrapper ?? node;
    }).filter(child => child && (child.parentNode ?? child.parent) === childHost);
    syncToolkitOrder(childHost, orderedChildren);
    if (element instanceof TwoPaneSplitView) {
      const horizontal = element.orientation === TwoPaneSplitViewOrientation.Horizontal;
      view.style.display = 'flex';
      view.style.position = 'relative';
      view.style.flexDirection = horizontal ? 'row' : 'column';
      for (let index = 0; index < orderedChildren.length; index++) {
        const child = orderedChildren[index];
        child.style.display = element._collapsedIndex === index ? 'none'
          : (child._uiElement?.style.display ?? '');
        child.style.flex = element._collapsedIndex < 0 && index === element.fixedPaneIndex
          ? `0 0 ${element.fixedPaneInitialDimension}px` : '1 1 0';
      }
      const splitter = node._splitter;
      splitter.style.display = element._collapsedIndex >= 0 ? 'none' : '';
      splitter.style.position = 'absolute';
      splitter.style.backgroundColor = '#777';
      splitter.style.cursor = horizontal ? 'col-resize' : 'row-resize';
      splitter.style[horizontal ? 'width' : 'height'] = '4px';
      splitter.style[horizontal ? 'height' : 'width'] = '100%';
      splitter.style[horizontal ? element.fixedPaneIndex === 0 ? 'left' : 'right'
        : element.fixedPaneIndex === 0 ? 'top' : 'bottom']
        = `${Math.max(0, element.fixedPaneInitialDimension - 2)}px`;
      view.appendChild(splitter);
    }
    if (element instanceof ListView || element instanceof TreeView) {
      const needsHeader = element.showFoldoutHeader || element.headerTitle
        || element.showBoundCollectionSize || element.makeHeader;
      if (needsHeader) {
        node._listHeader ??= this.host.ownerDocument.createElement('div');
        const header = node._listHeader;
        Object.assign(header.style, { position: 'sticky', top: '0', zIndex: '2',
          backgroundColor: 'inherit', flexShrink: '0' });
        if (element.makeHeader) {
          if (header._factory !== element.makeHeader) {
            header._factory = element.makeHeader;
            header.textContent = '';
            header._headerElement = element.makeHeader();
            if (!(header._headerElement instanceof VisualElement))
              throw new TypeError('BaseListView.makeHeader must return a VisualElement');
          }
          this._render(header._headerElement, header, seen);
        } else {
          this.elements.get(header._headerElement)?.remove();
          header._headerElement = null; header._factory = null;
          header.textContent = `${element.showFoldoutHeader ? element._foldoutOpen ? '▾ ' : '▸ ' : ''}`
          + `${element.headerTitle || ''}`
          + `${element.showBoundCollectionSize ? ` (${element._itemCount()})` : ''}`;
        }
        if (!header._foldoutListener) {
          header.addEventListener('click', () => {
            if (element.showFoldoutHeader) element._foldoutOpen = !element._foldoutOpen;
          });
          header._foldoutListener = true;
        }
      } else node._listHeader?.remove();
      if (element.showAddRemoveFooter || element.makeFooter) {
        node._listFooter ??= this.host.ownerDocument.createElement('div');
        const footer = node._listFooter;
        Object.assign(footer.style, { position: 'sticky', bottom: '0', zIndex: '2',
          display: 'flex', backgroundColor: 'inherit', flexShrink: '0' });
        if (element.makeFooter) {
          if (footer._factory !== element.makeFooter) {
            footer._factory = element.makeFooter;
            footer._buttons?.add.remove(); footer._buttons?.remove.remove();
            footer._buttons = null;
            footer._footerElement = element.makeFooter();
            if (!(footer._footerElement instanceof VisualElement))
              throw new TypeError('BaseListView.makeFooter must return a VisualElement');
          }
          this._render(footer._footerElement, footer, seen);
        } else if (!footer._buttons) {
          this.elements.get(footer._footerElement)?.remove();
          footer._footerElement = null; footer._factory = null;
          const add = this.host.ownerDocument.createElement('button');
          const remove = this.host.ownerDocument.createElement('button');
          add.textContent = '+'; remove.textContent = '−';
          add.addEventListener('click', () => {
            if (typeof element.overridingAddButtonBehavior === 'function')
              element.overridingAddButtonBehavior(element);
            else element._addFromFooter();
          });
          remove.addEventListener('click', () => element._removeFromFooter());
          footer.appendChild(add); footer.appendChild(remove);
          footer._buttons = { add, remove };
        }
        if (footer._buttons) {
          footer._buttons.add.disabled = !element.allowAdd;
          footer._buttons.remove.disabled = !element.allowRemove || !element._itemCount();
        }
      } else node._listFooter?.remove();
      if (!element._itemCount() && element.makeNoneElement) {
        node._listEmpty ??= this.host.ownerDocument.createElement('div');
        if (node._listEmpty._factory !== element.makeNoneElement) {
          node._listEmpty._factory = element.makeNoneElement;
          node._listEmpty._emptyElement = element.makeNoneElement();
          if (!(node._listEmpty._emptyElement instanceof VisualElement))
            throw new TypeError('BaseListView.makeNoneElement must return a VisualElement');
        }
        this._render(node._listEmpty._emptyElement, node._listEmpty, seen);
      } else node._listEmpty?.remove();
      if (element instanceof MultiColumnListView || element instanceof MultiColumnTreeView) {
        node._columnHeader ??= this.host.ownerDocument.createElement('div');
        const visibleColumns = element.columns.filter(column => column.visible);
        if (!node._visibleColumns || node._visibleColumns.length !== visibleColumns.length
          || visibleColumns.some((column, index) => node._visibleColumns[index] !== column
            || node._columnHeadings?.get(column)?._makeHeader !== column.makeHeader
            || node._columnHeadings?.get(column)?._headerTemplate !== column.headerTemplate)) {
          for (const [column, heading] of node._columnHeadings ?? []) {
            column.unbindHeader?.(heading._headerElement);
            column.destroyHeader?.(heading._headerElement);
          }
          for (const child of [...node._columnHeader.children]) child.remove();
          node._columnHeadings = new Map();
          for (const column of visibleColumns) {
            const heading = this.host.ownerDocument.createElement('div');
            const caption = this.host.ownerDocument.createElement('span');
            const icon = this.host.ownerDocument.createElement('img');
            const handle = this.host.ownerDocument.createElement('div');
            heading._caption = caption;
            heading._icon = icon;
            heading._handle = handle;
            heading._makeHeader = column.makeHeader;
            heading._headerTemplate = column.headerTemplate;
            const headerElement = column.makeHeader?.()
              ?? column.headerTemplate?.CloneTree() ?? new Label(column.title || column.name);
            if (!(headerElement instanceof VisualElement))
              throw new TypeError('Column.makeHeader must return a VisualElement');
            heading._headerElement = headerElement;
            heading._defaultHeader = !column.makeHeader && !column.headerTemplate;
            caption.style.display = 'none';
            icon.style.width = '1em'; icon.style.height = '1em';
            icon.style.objectFit = 'contain';
            heading.appendChild(icon); heading.appendChild(caption); heading.appendChild(handle);
            column.bindHeader?.(headerElement);
            heading.addEventListener('click', event => {
              if (event.target === handle) return;
              element._sortByColumn(column, event.ctrlKey || event.metaKey);
            });
            heading.addEventListener('dragstart', event => {
              if (!element.columns.reorderable || event.target === handle) {
                event.preventDefault?.(); return;
              }
              node._draggedColumn = column;
              event.dataTransfer?.setData('text/plain', column.name);
              if (event.dataTransfer) event.dataTransfer.effectAllowed = 'move';
            });
            heading.addEventListener('dragover', event => {
              if (node._draggedColumn && element.columns.reorderable)
                event.preventDefault?.();
            });
            heading.addEventListener('drop', event => {
              const dragged = node._draggedColumn;
              node._draggedColumn = null;
              if (!dragged || !element.columns.reorderable) return;
              event.preventDefault?.(); event.stopPropagation?.();
              element.columns.ReorderDisplay(element.columns.IndexOf(dragged),
                element.columns.IndexOf(column));
            });
            heading.addEventListener('dragend', () => { node._draggedColumn = null; });
            heading.addEventListener('contextmenu', event => {
              event.preventDefault?.();
              event.stopPropagation?.();
              const menu = new DropdownMenu();
              for (const option of element.columns) if (option.optional)
                menu.AppendAction(option.title || option.name,
                  () => { option.visible = !option.visible; },
                  option.visible ? DropdownMenuAction.Status.Checked
                    : DropdownMenuAction.Status.Normal);
              const contextual = new ContextualMenuPopulateEvent(menu, event);
              contextual.target = heading._headerElement;
              heading._headerElement._dispatchEvent('ContextualMenuPopulateEvent',
                contextual);
              element.headerContextMenuPopulateEvent.Invoke(contextual, column);
              const items = menu.PrepareForDisplay();
              node._contextMenu?.Close();
              if (!items.length) return;
              const popup = new GenericDropdownMenu();
              for (const item of items) {
                if (item.separator) { popup.AddSeparator(item.path); continue; }
                const checked = Boolean(item.status & DropdownMenuAction.Status.Checked);
                if (item.status === DropdownMenuAction.Status.None
                  || item.status & DropdownMenuAction.Status.Disabled)
                  popup.AddDisabledItem(item.name, checked);
                else popup.AddItem(item.name, checked, () => item.context.Execute());
              }
              let panel = node;
              while (panel && !panel._uiPanelScale) panel = panel.parentNode ?? panel.parent;
              const origin = panel?.getBoundingClientRect?.() ?? { left: 0, top: 0 };
              const scale = panel?._uiPanelScale || 1;
              const bounds = heading.getBoundingClientRect?.() ?? origin;
              popup.DropDown({ x: ((event.clientX ?? bounds.left) - origin.left) / scale,
                y: ((event.clientY ?? bounds.top) - origin.top) / scale,
                width: 0, height: 0 }, element, false, true);
              node._contextMenu = popup;
            });
            handle.addEventListener('pointerdown', event => {
              if (!element.columns.resizable || !column.resizable) return;
              handle._resize = { x: event.clientX, width: Number(column.width?.value ?? column.width)
                || (heading.getBoundingClientRect?.().width || 0) / panelScaleOf(node),
              pointerId: event.pointerId };
              handle.setPointerCapture?.(event.pointerId);
              event.preventDefault?.(); event.stopPropagation?.();
            });
            handle.addEventListener('pointermove', event => {
              const drag = handle._resize;
              if (!drag || drag.pointerId !== event.pointerId) return;
              const minimum = Math.max(0, Number(column.minWidth?.value ?? column.minWidth) || 0);
              const maximum = Number(column.maxWidth?.value ?? column.maxWidth) || Infinity;
              column.width = Math.min(maximum, Math.max(minimum,
                drag.width + (event.clientX - drag.x) / panelScaleOf(node)));
              event.preventDefault?.(); event.stopPropagation?.();
            });
            const endResize = event => {
              if (handle._resize?.pointerId !== event.pointerId) return;
              handle._resize = null; handle.releasePointerCapture?.(event.pointerId);
            };
            handle.addEventListener('pointerup', endResize);
            handle.addEventListener('pointercancel', endResize);
            Object.assign(heading.style, { display: 'flex', alignItems: 'center',
              boxSizing: 'border-box', overflow: 'hidden' });
            Object.assign(handle.style, { width: '6px', alignSelf: 'stretch',
              marginLeft: 'auto', cursor: 'col-resize', touchAction: 'none' });
            node._columnHeadings.set(column, heading);
            node._columnHeader.appendChild(heading);
          }
          node._visibleColumns = [...visibleColumns];
        }
        for (const column of visibleColumns) {
          const heading = node._columnHeadings.get(column);
          heading._caption.textContent = column.title || column.name;
          const iconAsset = column.icon?.GetSelectedImage?.() ?? column.icon;
          const iconUrl = iconAsset?.threeTexture?.image?.src
            ?? iconAsset?.texture?.threeTexture?.image?.src
            ?? iconAsset?.image?.src ?? iconAsset?.url
            ?? (typeof iconAsset === 'string' ? column._imageUrls?.get(iconAsset) : null);
          heading._icon.style.display = iconUrl ? '' : 'none';
          if (iconUrl) heading._icon.src = iconUrl;
          if (heading._defaultHeader) heading._headerElement.text = column.title || column.name;
          this._render(heading._headerElement, heading, seen);
          this.elements.get(heading._headerElement).style.flex = '1 1 0';
          heading.appendChild(heading._handle);
          const width = Number(column.width?.value ?? column.width);
          heading.style.flex = width > 0
            ? `${column.stretchable ? '1 1' : '0 0'} ${width}px` : '1 1 0';
          heading.style.minWidth = `${Math.max(0,
            Number(column.minWidth?.value ?? column.minWidth) || 0)}px`;
          heading.style.maxWidth = Number(column.maxWidth?.value ?? column.maxWidth) > 0
            ? `${Number(column.maxWidth?.value ?? column.maxWidth)}px` : '';
          heading.style.cursor = element.sortingEnabled && column.sortable ? 'pointer' : '';
          heading.draggable = element.columns.reorderable;
          heading._handle.style.display = element.columns.resizable && column.resizable
            ? '' : 'none';
        }
        Object.assign(node._columnHeader.style, { display: 'flex', position: 'sticky',
          top: '0', zIndex: '1', backgroundColor: 'inherit', flexShrink: '0' });
        view.appendChild(node._columnHeader);
      }
      if (element._isVirtual) {
        if (!node._virtualTop) {
          node._virtualTop = this.host.ownerDocument.createElement('div');
          node._virtualBottom = this.host.ownerDocument.createElement('div');
        }
        node._virtualTop.style.height = `${element._virtualTop}px`;
        node._virtualBottom.style.height = `${element._virtualBottom}px`;
        node._virtualTop.style.flexShrink = '0';
        node._virtualBottom.style.flexShrink = '0';
        view.appendChild(node._virtualTop);
        for (const child of orderedChildren) view.appendChild(child);
        view.appendChild(node._virtualBottom);
      } else if (node._virtualTop) {
        node._virtualTop.remove(); node._virtualBottom.remove();
        node._virtualTop = null; node._virtualBottom = null;
      }
      if (!element._isVirtual && node._columnHeader)
        for (const child of orderedChildren) view.appendChild(child);
      if (needsHeader || element.showAddRemoveFooter || element.makeFooter
        || node._listEmpty && !element._itemCount() && element.makeNoneElement) {
        if (node._listHeader && needsHeader) view.appendChild(node._listHeader);
        if (node._columnHeader) view.appendChild(node._columnHeader);
        if (node._virtualTop) view.appendChild(node._virtualTop);
        for (const child of orderedChildren) view.appendChild(child);
        if (node._virtualBottom) view.appendChild(node._virtualBottom);
      }
      if (node._listEmpty && !element._itemCount() && element.makeNoneElement)
        view.appendChild(node._listEmpty);
      if (node._listFooter && (element.showAddRemoveFooter || element.makeFooter))
        view.appendChild(node._listFooter);
      if (element.showFoldoutHeader && !element._foldoutOpen)
        for (const child of orderedChildren) child.style.display = 'none';
      else for (const child of orderedChildren) child.style.display = '';
      if (collectionScrollIndex != null) {
        const row = element._rows.find(item => item._listIndex === collectionScrollIndex);
        const rowNode = this.elements.get(row);
        view.scrollTop = collectionScrollTarget
          ?? (rowNode?.offsetTop ?? 0);
        element._scrollToIndex = null;
      }
    }
    if (element instanceof ScrollView) {
      if (Number.isFinite(view.scrollWidth) && Number.isFinite(view.clientWidth))
        element.horizontalScroller.highValue = Math.max(0, view.scrollWidth - view.clientWidth);
      if (Number.isFinite(view.scrollHeight) && Number.isFinite(view.clientHeight))
        element.verticalScroller.highValue = Math.max(0, view.scrollHeight - view.clientHeight);
      view.scrollLeft = element.scrollOffset.x;
      view.scrollTop = element.scrollOffset.y;
    }
    if (element instanceof ScrollView && element._scrollTo) {
      const targetNode = this.elements.get(element._scrollTo);
      const target = targetNode?._wrapper ?? targetNode;
      if (target) {
        const targetBounds = target.getBoundingClientRect?.();
        const viewBounds = view.getBoundingClientRect?.();
        view.scrollTop = targetBounds && viewBounds
          ? view.scrollTop + (targetBounds.top - viewBounds.top) / panelScaleOf(view)
          : (target.offsetTop ?? 0) - (view.offsetTop ?? 0);
        element.scrollOffset = new Vector2(view.scrollLeft, view.scrollTop);
        element._scrollTo = null;
      }
    }
    if (!node._wrapper)
      for (const child of [...view.children]) if (child !== node._virtualTop
        && child !== node._virtualBottom && child !== node._tabHeaders
        && child !== node._columnHeader && child !== node._listHeader
        && child !== node._listFooter && child !== node._listEmpty
        && child !== node._tabContent && child !== node._groupTitle
        && child !== node._groupContent && child !== node._helpIcon
        && child !== node._helpText && child !== node._multiCaption
        && child !== node._multiControls && child !== node._buttonIcon
        && child !== node._buttonText && !child._uiRichText
        && !seen.has(child._visualElement)) child.remove();
    view._visualElement = element;
    element._markedForRepaint = false;
  }
  _setFocus(next) {
    if (next === this._focusedElement) return;
    if (next && !next._programmaticFocus) {
      const eventSystem = [...(this.runtime?._objects ?? [])]
        .flatMap(object => object._components)
        .find(component => component.constructor.name === 'EventSystem'
          && component.isActiveAndEnabled);
      if (eventSystem?.currentSelectedGameObject)
        eventSystem.SetSelectedGameObject(null);
    }
    const previous = this._focusedElement;
    if (previous) previous._dispatchEvent('FocusOutEvent', { relatedTarget: next });
    if (next) next._dispatchEvent('FocusInEvent', { relatedTarget: previous });
    this._focusedElement = next;
    if (previous) previous._dispatchEvent('BlurEvent', { relatedTarget: next }, { bubbles: false });
    if (next) next._dispatchEvent('FocusEvent', { relatedTarget: previous }, { bubbles: false });
  }
}

const ussTypeSelector = /(?<![.#\w-])(TwoPaneSplitView|ToggleButtonGroup|RadioButtonGroup|DropdownField|TemplateContainer|BindableElement|VisualElement|ProgressBar|UnsignedIntegerField|UnsignedLongField|IntegerField|LongField|FloatField|DoubleField|Vector2IntField|Vector3IntField|Vector2Field|Vector3Field|Vector4Field|RectIntField|RectField|BoundsIntField|BoundsField|MinMaxSlider|RadioButton|SliderInt|ScrollView|MultiColumnTreeView|MultiColumnListView|TreeView|ListView|TextField|Hash128Field|EnumField|PopupWindow|GroupBox|HelpBox|TabView|Scroller|RepeatButton|Button|Foldout|Slider|Toggle|TextElement|Label|Image|Box|Tab)(?=[\s.#:[>+~,)\]]|$)/g;
function translateUssDeclarations(source) {
  return source.replace(/-unity-font-style\s*:\s*([\w-]+)/gi, (_, value) =>
    `font-weight: ${value.toLowerCase().includes('bold') ? 'bold' : 'normal'}; `
    + `font-style: ${value.toLowerCase().includes('italic') ? 'italic' : 'normal'}`)
    .replace(/-unity-text-align\s*:\s*(upper|middle|lower)-(left|center|right)/gi,
      (_, vertical, horizontal) => `text-align: ${horizontal}; justify-content: `
        + ({ upper: 'flex-start', middle: 'center', lower: 'flex-end' }
          [vertical.toLowerCase()]));
}
function translateUssSelectors(source) {
  return source.replace(/(^|[{}])([^{}]+)\{/g, (rule, boundary, selector) => {
    if (selector.trimStart().startsWith('@')) return rule;
    return `${boundary}${selector.replace(ussTypeSelector,
      type => `[data-ui-type="${type}"]`)
      .replace(/:active\b/g, ':is(:active,.unity-active)')
      .replace(/:checked\b/g, ':is(:checked,.unity-checked)')}{`;
  });
}

let toolkitStyleScopeId = 0;
function scopeUssToElement(source, id) {
  const scope = `[data-ui-style-scope="${id}"]`;
  return translateUssSelectors(translateUssDeclarations(source))
    .replace(/(^|[{}])([^{}]+)\{/g, (rule, boundary, selectors) => {
      if (selectors.trimStart().startsWith('@')) return rule;
      const scoped = selectors.split(',').flatMap(selector => {
        const value = selector.trim();
        if (!value) return [];
        if (value === ':root') return [scope];
        return [`${scope}${value}`, `${scope} ${value}`];
      });
      return `${boundary}${scoped.join(', ')}{`;
    });
}

function syncToolkitOrder(parent, desired) {
  const current = Array.from(parent.children ?? []).filter(child => child._uiElement);
  if (current.length === desired.length && current.every((child, index) => child === desired[index])) return;
  for (const child of desired) parent.appendChild(child);
}

function renderToolkitText(node, value, richText) {
  const text = String(value ?? '');
  const key = `${richText ? 'rich' : 'plain'}:${text}`;
  if (node._renderedTextKey === key) return;
  node._renderedTextKey = key;
  const tags = /<\/?(?:b|i|u|s|color|size|font-weight)(?:=[^<>]+)?>/gi;
  if (!richText || !tags.test(text)) { node.textContent = text; return; }
  tags.lastIndex = 0;
  node.textContent = '';
  const root = node.ownerDocument.createElement('span');
  root._uiRichText = true;
  node.appendChild(root);
  const stack = [{ name: '', node: root }];
  const append = content => {
    if (!content) return;
    const span = node.ownerDocument.createElement('span');
    span.textContent = content;
    stack.at(-1).node.appendChild(span);
  };
  let previous = 0;
  for (const match of text.matchAll(tags)) {
    append(text.slice(previous, match.index));
    previous = match.index + match[0].length;
    const parsed = /^<(\/)?([\w-]+)(?:=([^<>]+))?>$/i.exec(match[0]);
    const name = parsed?.[2]?.toLowerCase();
    if (parsed?.[1]) {
      if (stack.length > 1 && stack.at(-1).name === name) stack.pop();
      else append(match[0]);
      continue;
    }
    const span = node.ownerDocument.createElement('span');
    const argument = (parsed?.[3] ?? '').trim().replace(/^['"]|['"]$/g, '');
    if (name === 'b') span.style.fontWeight = 'bold';
    else if (name === 'i') span.style.fontStyle = 'italic';
    else if (name === 'u') span.style.textDecoration = 'underline';
    else if (name === 's') span.style.textDecoration = 'line-through';
    else if (name === 'color' && /^(?:#[0-9a-f]{3,8}|[a-z]+)$/i.test(argument ?? ''))
      span.style.color = argument;
    else if (name === 'size' && /^\d+(?:\.\d+)?(?:px|em|%)?$/i.test(argument ?? ''))
      span.style.fontSize = /[a-z%]$/i.test(argument) ? argument : `${argument}px`;
    else if (name === 'font-weight' && /^(?:[1-9]00|bold|normal)$/i.test(argument ?? ''))
      span.style.fontWeight = argument;
    else { append(match[0]); continue; }
    stack.at(-1).node.appendChild(span);
    stack.push({ name, node: span });
  }
  append(text.slice(previous));
}

let radioGroupId = 0;

function pointerEventData(event, node) {
  let panel = node;
  while (panel && !panel._uiPanelScale) panel = panel.parentNode ?? panel.host;
  const scale = panel?._uiPanelScale ?? 1;
  const panelBounds = panel?.getBoundingClientRect?.() ?? { left: 0, top: 0 };
  const nodeBounds = node.getBoundingClientRect?.() ?? panelBounds;
  const pointerId = event.pointerId ?? 0;
  const now = event.timeStamp ?? globalThis.performance?.now?.() ?? Date.now();
  const historyOwner = panel ?? node.ownerDocument ?? node;
  const history = historyOwner._toolkitPointerHistory ??= new Map();
  const previous = history.get(pointerId);
  history.set(pointerId, { x: event.clientX ?? 0, y: event.clientY ?? 0, at: now });
  const modifiers = (event.shiftKey ? EventModifiers.Shift : 0)
    | (event.ctrlKey ? EventModifiers.Control : 0)
    | (event.altKey ? EventModifiers.Alt : 0)
    | (event.metaKey ? EventModifiers.Command : 0);
  return {
    _domEvent: event,
    timestamp: now,
    pointerId,
    pointerType: event.pointerType ?? 'mouse',
    button: event.button ?? 0,
    pressedButtons: event.buttons ?? 0,
    clickCount: event.detail ?? 1,
    isPrimary: event.isPrimary ?? true,
    modifiers,
    shiftKey: !!event.shiftKey, ctrlKey: !!event.ctrlKey,
    altKey: !!event.altKey, commandKey: !!event.metaKey,
    actionKey: !!(globalThis.navigator?.platform?.includes?.('Mac')
      ? event.metaKey : event.ctrlKey),
    pressure: event.pressure ?? 0,
    tangentialPressure: event.tangentialPressure ?? 0,
    altitudeAngle: event.altitudeAngle ?? 0,
    azimuthAngle: event.azimuthAngle ?? 0,
    twist: event.twist ?? 0,
    tilt: new Vector2((event.tiltX ?? 0) * Math.PI / 180,
      (event.tiltY ?? 0) * Math.PI / 180),
    radius: new Vector2((event.width ?? 0) / 2, (event.height ?? 0) / 2),
    radiusVariance: new Vector2(),
    penStatus: event.penStatus ?? (event.pointerType === 'pen'
      ? ((event.buttons ? PenStatus.Contact : 0)
        | ((event.buttons ?? 0) & 2 ? PenStatus.Barrel : 0)
        | (event.button === 5 ? PenStatus.Eraser : 0)) : 0),
    deltaPosition: new Vector3((event.movementX ?? (previous
      ? (event.clientX ?? 0) - previous.x : 0)) / scale,
    (event.movementY ?? (previous ? (event.clientY ?? 0) - previous.y : 0)) / scale, 0),
    deltaTime: event.deltaTime ?? (previous ? Math.max(0, now - previous.at) / 1000 : 0),
    position: new Vector3(((event.clientX ?? 0) - panelBounds.left) / scale,
      ((event.clientY ?? 0) - panelBounds.top) / scale, 0),
    localPosition: new Vector3(((event.clientX ?? 0) - nodeBounds.left) / scale,
      ((event.clientY ?? 0) - nodeBounds.top) / scale, 0),
  };
}

function clickPointerEventData(event, node) {
  const release = node._lastPointerRelease;
  node._lastPointerRelease = null;
  const legacyClick = event.pointerType == null && event.isTrusted && event.detail > 0
    && release && Date.now() - release.at < 1000
    && Math.abs((event.clientX ?? 0) - release.clientX) <= 1
    && Math.abs((event.clientY ?? 0) - release.clientY) <= 1;
  return pointerEventData(legacyClick ? {
    pointerId: release.pointerId, pointerType: release.pointerType,
    button: event.button, buttons: event.buttons,
    clientX: event.clientX, clientY: event.clientY,
  } : event, node);
}

function isToolkitField(element) {
  return !(element instanceof UIElementsProgressBar)
    && !(element instanceof UIElementsFoldout)
    && (element instanceof UIElementsTextField || element instanceof UIElementsToggle
    || element instanceof UIElementsIntegerField || element instanceof UIElementsFloatField
    || element instanceof UIElementsSlider || element instanceof PopupField
    || element instanceof UIElementsEnumField || element instanceof UIElementsHash128Field);
}

function isCompoundField(element) {
  return element instanceof UIElementsVector2Field
    || element instanceof UIElementsVector3Field
    || element instanceof UIElementsVector4Field
    || element instanceof UIElementsVector2IntField
    || element instanceof UIElementsVector3IntField
    || element instanceof UIElementsRectField
    || element instanceof UIElementsRectIntField
    || element instanceof UIElementsBoundsField
    || element instanceof UIElementsBoundsIntField
    || element instanceof UIElementsMinMaxSlider;
}

function compoundAxes(element) {
  if (element._components) return element._components;
  if (element instanceof UIElementsVector4Field) return ['x', 'y', 'z', 'w'];
  if (element instanceof UIElementsVector3Field || element instanceof UIElementsVector3IntField)
    return ['x', 'y', 'z'];
  return ['x', 'y'];
}

function panelScaleOf(node) {
  for (let item = node; item; item = item.parentNode ?? item.host)
    if (item._uiPanelScale) return item._uiPanelScale;
  return 1;
}

function panelScale(settings, width, height) {
  if (settings.scaleMode === 0) return Math.max(0.01, settings.scale || 1);
  if (settings.scaleMode === 1) return Math.max(0.01,
    (settings.fallbackDpi || 96) / (settings.referenceDpi || 96));
  const reference = settings.referenceResolution;
  if (!reference?.x || !reference?.y || !width || !height) return 1;
  const x = width / reference.x, y = height / reference.y;
  if (settings.screenMatchMode === 1) return Math.max(x, y);
  if (settings.screenMatchMode === 2) return Math.min(x, y);
  const match = Math.max(0, Math.min(1, settings.match));
  return Math.exp(Math.log(x) * (1 - match) + Math.log(y) * match);
}
