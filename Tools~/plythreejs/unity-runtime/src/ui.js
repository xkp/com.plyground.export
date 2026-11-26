import { AxisEventData, BaseEventData, Canvas, FilterMode, GraphicRaycaster,
  InputSystemUIInputModule, StandaloneInputModule, MoveDirection, PenStatus, PointerEventData, RaycastResult,
  TextureWrapMode, Time,
  Vector2, Vector3 } from './index.js';
import { rebuildCanvasLayout } from './ui-layout.js';
import { drawTmpLabel } from './tmp-text.js';

function radialClipPath(method, amount, origin, clockwise) {
  if (amount >= 1) return '';
  if (!Number.isInteger(origin) || origin < 0 || origin > 3) return null;
  const definitions = {
    2: { centers: [[0, 100], [0, 0], [100, 0], [100, 100]],
      scales: [100, 100], clockwiseStarts: [-90, 0, 90, 180],
      counterclockwiseStarts: [0, 90, 180, 270], sweep: 90 },
    3: { centers: [[50, 100], [0, 50], [50, 0], [100, 50]],
      scales: null, clockwiseStarts: [180, 270, 0, 90],
      counterclockwiseStarts: [0, 90, 180, 270], sweep: 180 },
    4: { centers: [[50, 50], [50, 50], [50, 50], [50, 50]],
      scales: [50, 50], clockwiseStarts: [90, 0, -90, 180],
      counterclockwiseStarts: [90, 0, -90, 180], sweep: 360 },
  };
  const definition = definitions[method];
  if (!definition) return null;
  const [cx, cy] = definition.centers[origin];
  const [sx, sy] = definition.scales ?? (origin % 2 === 0 ? [50, 100] : [100, 50]);
  const direction = clockwise ? 1 : -1;
  const angle = (clockwise ? definition.clockwiseStarts
    : definition.counterclockwiseStarts)[origin];
  const sweep = amount * definition.sweep;
  const percent = value => `${Number(value.toFixed(6))}%`;
  const point = degrees => {
    const radians = degrees * Math.PI / 180;
    const x = Math.abs(Math.cos(radians)) < 1e-10 ? 0 : Math.cos(radians) * sx;
    const y = Math.abs(Math.sin(radians)) < 1e-10 ? 0 : Math.sin(radians) * sy;
    const xDistance = x > 0 ? (100 - cx) / x : x < 0 ? -cx / x : Infinity;
    const yDistance = y > 0 ? (100 - cy) / y : y < 0 ? -cy / y : Infinity;
    const distance = Math.min(xDistance, yDistance);
    return `${percent(cx + x * distance)} ${percent(cy + y * distance)}`;
  };
  if (sweep <= 0) return `polygon(${percent(cx)} ${percent(cy)}, ${percent(cx)} ${percent(cy)})`;
  const points = [`${percent(cx)} ${percent(cy)}`, point(angle)];
  const corners = [[0, 0], [100, 0], [100, 100], [0, 100]]
    .filter(([x, y]) => x !== cx || y !== cy)
    .map(([x, y]) => {
      const corner = Math.atan2((y - cy) / sy, (x - cx) / sx) * 180 / Math.PI;
      const distance = (((corner - angle) * direction) % 360 + 360) % 360;
      return { distance, value: `${percent(x)} ${percent(y)}` };
    })
    .filter(corner => corner.distance > 1e-8 && corner.distance < sweep - 1e-8)
    .sort((a, b) => a.distance - b.distance);
  points.push(...corners.map(corner => corner.value), point(angle + direction * sweep));
  return `polygon(${points.join(', ')})`;
}

/** Browser overlay for the supported screen-space Unity Canvas subset. */
export class BrowserCanvasUI {
  constructor(host = globalThis.document?.body, { fitReferenceResolution = false } = {}) {
    if (!host?.ownerDocument) throw new TypeError('BrowserCanvasUI needs a DOM host');
    this.host = host;
    this.fitReferenceResolution = fitReferenceResolution;
    this.elements = new Map();
    this.diagnostics = new Map();
    this._reportedDiagnostics = new Set();
    this._pressed = new Map();
    this._lastPointerPress = new Map();
    this._suppressClick = new Set();
    this._spriteCropCache = new Map();
    this._fontFaces = new Map();
    this._maskedPointerTargets = new Map();
    this._maskedSyntheticEvents = new WeakSet();
    this.root = host.ownerDocument.createElement('div');
    Object.assign(this.root.style, {
      position: 'absolute', inset: '0', overflow: 'hidden', pointerEvents: 'none',
    });
  }
  attach(runtime) {
    this.runtime = runtime; this.host.appendChild(this.root);
    Canvas._browserSystems.add(this);
    this._keyHandler = event => this._handleKeyDown(event);
    this.host.addEventListener('keydown', this._keyHandler);
    this._backgroundHandler = event => {
      let target = event.target;
      while (target) {
        if (target === this.root) return;
        target = target.parentNode ?? target.parent;
      }
      if (this._uiInputModule?.deselectOnBackgroundClick !== false)
        this.eventSystem?.SetSelectedGameObject(null);
    };
    this.host.addEventListener('pointerdown', this._backgroundHandler);
    this._maskedPointerHandler = event => this._routePaddedMaskPointer(event);
    for (const type of ['pointerdown', 'pointerup', 'pointercancel', 'click'])
      this.host.addEventListener(type, this._maskedPointerHandler, true);
  }
  detach() {
    Canvas._browserSystems.delete(this);
    this._bindInputModule(null);
    for (const element of this.elements.values()) {
      const input = element._unityControl;
      if (input?._unityComponent) input._unityComponent._browserInput = null;
      element._maskEscapePortal?.remove();
    }
    this.root.remove(); this.elements.clear(); this.diagnostics.clear();
    this.host.removeEventListener?.('keydown', this._keyHandler);
    this.host.removeEventListener?.('pointerdown', this._backgroundHandler);
    for (const type of ['pointerdown', 'pointerup', 'pointercancel', 'click'])
      this.host.removeEventListener?.(type, this._maskedPointerHandler, true);
    this._reportedDiagnostics.clear(); this._pressed.clear();
    this._suppressClick.clear(); this._spriteCropCache.clear(); this._fontFaces.clear();
    this.runtime = null;
    this._maskedPointerTargets.clear();
  }
  raycastAll(position) {
    const bounds = this.host.getBoundingClientRect?.() ?? { left: 0, top: 0,
      height: this.host.clientHeight ?? 0 };
    const x = bounds.left + position.x;
    const y = bounds.top + bounds.height - position.y;
    const hits = [];
    for (const [object, node] of this.elements) {
      if (!object.activeInHierarchy || !raycastAllowed(object)
        || !customRaycastAllowed(object, position)
        || !object._components.some(component => component.isActiveAndEnabled
          && ['Image', 'RawImage', 'Text', 'TextMeshProUGUI'].includes(component.constructor.name)
          && component.raycastTarget)
        || !pointInRect(node.getBoundingClientRect?.(), x, y)) continue;
      let canvas = object;
      while (canvas && !canvas._components.some(component =>
        component.constructor.name === 'Canvas' && component.enabled)) canvas = canvas.parent;
      const canvasComponent = canvas?._components.find(component =>
        component.constructor.name === 'Canvas' && component.enabled);
      const module = canvas?._components.find(component =>
        component.constructor.name === 'GraphicRaycaster' && component.isActiveAndEnabled);
      if (!module || canvasComponent?.renderMode !== 0) continue;
      let allowed = true;
      for (let ancestor = object.parent; ancestor && allowed; ancestor = ancestor.parent) {
        const mask = ancestor._components.find(component => component.isActiveAndEnabled
          && (component.constructor.name === 'RectMask2D'
            || component.constructor.name === 'Mask'));
        if (mask && !hasUnmaskableGraphic(object)) {
          const rect = this.elements.get(ancestor)?.getBoundingClientRect?.();
          allowed = mask.constructor.name === 'RectMask2D'
            ? !!rect && pointInPaddedRect(rect, mask.padding, x, y)
            : pointInRect(rect, x, y);
        }
      }
      if (!allowed) continue;
      let depth = 0;
      for (let ancestor = object.parent; ancestor; ancestor = ancestor.parent) depth++;
      hits.push({ gameObject: object, module, sortingOrder: canvasComponent.sortingOrder,
        depth, element: node });
    }
    return hits;
  }
  _routePaddedMaskPointer(event) {
    if (this._maskedSyntheticEvents.has(event)
      || !Number.isFinite(event.clientX) || !Number.isFinite(event.clientY)) return;
    let original = event.target;
    while (original && !original._uiObject) {
      if (original._uiElement) return;
      original = original.parentNode ?? original.parent;
    }
    const originalObject = original?._uiObject;
    const validOriginal = originalObject && paddedMasksAllow(originalObject, event, this.elements);
    const pointerId = event.pointerId ?? 1;
    const stored = this._maskedPointerTargets.get(pointerId);
    const candidate = event.type === 'pointerdown'
      ? this._maskedOverflowHitAt(event.clientX, event.clientY) : stored;
    if (event.type === 'pointerdown' && candidate && originalObject
      && originalObject !== candidate && validOriginal
      && originalObject._components.some(component => component.isActiveAndEnabled
        && ['Image', 'RawImage', 'Text', 'TextMeshProUGUI'].includes(component.constructor.name)
        && component.raycastTarget)) return;
    if (event.type === 'pointerdown' && candidate)
      this._maskedPointerTargets.set(pointerId, candidate);
    else if (event.type === 'pointerdown') this._maskedPointerTargets.delete(pointerId);
    if (event.type === 'pointercancel' || event.type === 'click')
      this._maskedPointerTargets.delete(pointerId);
    if (!candidate && validOriginal !== false) return;
    event.preventDefault?.();
    event.stopImmediatePropagation?.();
    if (!candidate) {
      if (event.type === 'pointerdown'
        && this._uiInputModule?.deselectOnBackgroundClick !== false)
        this.eventSystem?.SetSelectedGameObject(null);
      return;
    }
    const target = this.elements.get(candidate);
    if (!target) return;
    const init = { bubbles: true, cancelable: true, clientX: event.clientX,
      clientY: event.clientY, button: event.button, buttons: event.buttons,
      pointerId, pointerType: event.pointerType ?? 'mouse' };
    const synthetic = event.type === 'click' ? new MouseEvent('click', init)
      : new PointerEvent(event.type, init);
    this._maskedSyntheticEvents.add(synthetic);
    target.dispatchEvent(synthetic);
  }
  _maskedOverflowHitAt(x, y) {
    for (const [object, element] of [...this.elements].reverse()) {
      if (!object.activeInHierarchy || !raycastAllowed(object)
        || !object._components.some(component => component.isActiveAndEnabled
          && ['Image', 'RawImage', 'Text', 'TextMeshProUGUI'].includes(component.constructor.name)
          && component.raycastTarget)) continue;
      if (!pointInRect(element.getBoundingClientRect?.(), x, y)) continue;
      const ignoresMasks = hasUnmaskableGraphic(object);
      let beyondVisualMask = false;
      let allowed = true;
      for (let ancestor = object.parent; ancestor; ancestor = ancestor.parent) {
        const mask = ancestor._components.find(component =>
          component.constructor.name === 'RectMask2D' && component.isActiveAndEnabled);
        if (!mask) continue;
        const rect = this.elements.get(ancestor)?.getBoundingClientRect?.();
        if (!rect || !ignoresMasks && !pointInPaddedRect(rect, mask.padding, x, y)) {
          allowed = false; break;
        }
        if (!pointInRect(rect, x, y)) beyondVisualMask = true;
      }
      if (ignoresMasks)
        for (let ancestor = object.parent; ancestor; ancestor = ancestor.parent) {
          if (!ancestor._components.some(component => component.constructor.name === 'Mask'
            && component.isActiveAndEnabled)) continue;
          const rect = this.elements.get(ancestor)?.getBoundingClientRect?.();
          if (rect && !pointInRect(rect, x, y)) beyondVisualMask = true;
        }
      if (allowed && beyondVisualMask) return object;
    }
    return null;
  }
  _syncMaskEscape(object, element, portalAnchors) {
    const graphic = object._components.find(component =>
      ['Image', 'RawImage', 'Text', 'TextMeshProUGUI'].includes(component.constructor.name)
        && component.isActiveAndEnabled);
    const draw = element._graphicLayer;
    let outerMask = null;
    let hasRectMask = false;
    for (let ancestor = object.parent; ancestor; ancestor = ancestor.parent)
      for (const component of ancestor._components)
        if (component.isActiveAndEnabled
          && ['Mask', 'RectMask2D'].includes(component.constructor.name)) {
          outerMask = ancestor;
          hasRectMask ||= component.constructor.name === 'RectMask2D';
        }
    if (element._maskParent !== object.parent) element._rectClipStale = false;
    if (graphic?.maskable === false && element._lastMaskable === true && hasRectMask)
      element._rectClipStale = true;
    if (graphic?.maskable !== false) element._rectClipStale = false;
    element._lastMaskable = graphic?.maskable;
    element._maskParent = object.parent;
    const container = outerMask && this.elements.get(outerMask.parent);
    const maskNode = outerMask && this.elements.get(outerMask);
    const rect = element.getBoundingClientRect?.();
    const containerRect = container?.getBoundingClientRect?.();
    if (graphic?.maskable !== false || !outerMask || element._rectClipStale
      || !draw?.cloneNode || !maskNode?.after || !rect || !containerRect) {
      draw && (draw.style.visibility = '');
      element._maskEscapePortal?.remove();
      element._maskEscapePortal = null;
      return;
    }
    let portal = element._maskEscapePortal;
    if (!portal) {
      portal = this.host.ownerDocument.createElement('div');
      Object.assign(portal.style, { position: 'absolute', pointerEvents: 'none',
        boxSizing: 'border-box' });
      element._maskEscapePortal = portal;
    }
    const scaleX = container.clientWidth ? containerRect.width / container.clientWidth : 1;
    const scaleY = container.clientHeight ? containerRect.height / container.clientHeight : 1;
    portal.style.left = `${(rect.left - containerRect.left) / scaleX}px`;
    portal.style.top = `${(rect.top - containerRect.top) / scaleY}px`;
    portal.style.width = `${rect.width / scaleX}px`;
    portal.style.height = `${rect.height / scaleY}px`;
    let opacity = 1;
    for (let current = object; current && current !== outerMask.parent; current = current.parent)
      opacity *= groupAlpha(current);
    portal.style.opacity = String(opacity);
    const clonedDraw = draw.cloneNode(true);
    clonedDraw.style.visibility = '';
    portal.replaceChildren(clonedDraw);
    draw.style.visibility = 'hidden';
    (portalAnchors.get(outerMask) ?? maskNode).after(portal);
    portalAnchors.set(outerMask, portal);
  }
  afterLateUpdate(deltaTime = 0) {
    if (!Canvas._forcingUpdate) Canvas.preWillRenderCanvases.Invoke();
    this._frameDeltaTime = deltaTime;
    for (const object of this.runtime?._objects ?? [])
      for (const component of object._components)
        component._advanceColorTween?.(deltaTime, Time.unscaledDeltaTime);
    const seen = new Set();
    const rootElements = [];
    this.diagnostics.clear();
    this.eventSystem = [...(this.runtime?._objects ?? [])].flatMap(object => object._components)
      .find(component => component.constructor.name === 'EventSystem' && component.isActiveAndEnabled) ?? null;
    const inputModule = this.eventSystem?.gameObject?._components.find(component =>
      component instanceof InputSystemUIInputModule && component.isActiveAndEnabled) ?? null;
    this._bindInputModule(inputModule);
    const legacyModule = !inputModule ? this.eventSystem?.gameObject?._components.find(component =>
      component instanceof StandaloneInputModule && component.isActiveAndEnabled) : null;
    if (legacyModule !== this._legacyInputModule) this._legacyMove = null;
    this._legacyInputModule = legacyModule ?? null;
    if (inputModule) {
      if (inputModule.pointerBehavior !== 0)
        this._unsupported(inputModule.gameObject,
          `InputSystemUIInputModule pointerBehavior ${inputModule.pointerBehavior}`);
      if (inputModule.trackedDevicePosition || inputModule.trackedDeviceOrientation)
        this._unsupported(inputModule.gameObject, 'InputSystemUIInputModule tracked-device actions');
      if (inputModule.actionsAsset?.unsupportedBindings?.length)
        this._unsupported(inputModule.gameObject,
          `InputSystemUIInputModule bindings ${inputModule.actionsAsset.unsupportedBindings.join(', ')}`);
    }
    if (this._moveRepeat && inputModule?.move?.action?.enabled) {
      this._moveRepeat.remaining -= deltaTime;
      if (this._moveRepeat.remaining <= 0) {
        const vector = inputModule.move.action.ReadValue('Vector2');
        if (Math.abs(vector.x) > 0.5 || Math.abs(vector.y) > 0.5) {
          this._routeMove(vector);
          this._moveRepeat.remaining = Math.max(0.01, inputModule.moveRepeatRate);
        } else this._moveRepeat = null;
      }
    }
    if (this.eventSystem && !this.eventSystem._browserInitialSelectionApplied) {
      this.eventSystem._browserInitialSelectionApplied = true;
      if (this.eventSystem.firstSelectedGameObject && !this.eventSystem.currentSelectedGameObject)
        this.eventSystem.SetSelectedGameObject(this.eventSystem.firstSelectedGameObject);
    }
    for (const object of this.runtime?._objects ?? []) {
      if (!object.activeInHierarchy) continue;
      const canvas = object._components.find(component =>
        component.constructor.name === 'Canvas' && component.enabled);
      if (!canvas) continue;
      if (canvas.renderMode !== 0) {
        this._unsupportedCanvas(object, canvas);
        continue;
      }
      if (this._parentCanvas(object)) continue;
      const element = this._element(object, seen);
      const viewport = this.host.getBoundingClientRect?.() ?? this.host;
      const width = viewport.width || this.host.clientWidth || globalThis.window?.innerWidth || 0;
      const height = viewport.height || this.host.clientHeight || globalThis.window?.innerHeight || 0;
      canvas._browserViewport = { width, height };
      const scaler = object._components.find(component =>
        component.constructor.name === 'CanvasScaler' && component.enabled);
      const scale = this._canvasScale(object, scaler, width, height);
      canvas.scaleFactor = scale;
      canvas.referencePixelsPerUnit = scaler?.isActiveAndEnabled
        ? scaler.referencePixelsPerUnit : 100;
      if (canvas.pixelPerfect || canvas.overridePixelPerfect)
        this._unsupported(object, 'Canvas pixelPerfect native raster snapping');
      const logicalWidth = width > 0 ? width / scale : object.transform.rect?.width ?? 0;
      const logicalHeight = height > 0 ? height / scale : object.transform.rect?.height ?? 0;
      if (object.transform.sizeDelta && width > 0 && height > 0) {
        const size = object.transform.sizeDelta;
        if (size.x !== logicalWidth || size.y !== logicalHeight)
          resizeCanvasRect(object, logicalWidth, logicalHeight);
        // Unity's overlay Canvas RectTransform occupies the screen in world
        // coordinates even when CanvasScaler changes its logical rect size.
        object.transform.position = new Vector3(width / 2, height / 2, 0);
        object.transform.localScale = new Vector3(scale, scale, 1);
      }
      rebuildCanvasLayout(object);
      const rootScrollRect = object._components.find(component =>
        component.constructor.name === 'ScrollRect' && component.isActiveAndEnabled);
      rootScrollRect?._syncScrollBounds();
      rootScrollRect?._advanceScroll(deltaTime);
      element.style.inset = 'auto';
      element.style.left = '0';
      element.style.top = '0';
      element.style.width = width > 0 ? `${logicalWidth}px` : '100%';
      element.style.height = height > 0 ? `${logicalHeight}px` : '100%';
      element.style.transformOrigin = 'top left';
      element.style.transform = scale === 1 ? '' : `scale(${scale})`;
      element.style.zIndex = String(canvas.sortingOrder * 1000);
      element.style.opacity = String(groupAlpha(object));
      element.style.overflow = scrollViewport(object) ? 'hidden' : '';
      element.style.pointerEvents = rootScrollRect ? 'auto' : 'none';
      if ((element.parentNode ?? element.parent) !== this.root) this.root.appendChild(element);
      rootElements.push(element);
      for (const child of object.children) this._render(child, element, seen);
      syncUiOrder(element, object.children.map(child => this.elements.get(child))
        .filter(child => child && (child.parentNode ?? child.parent) === element));
    }
    syncUiOrder(this.root, rootElements);
    if (legacyModule?.inputOverride) this._processLegacyOverride(legacyModule, deltaTime);
    const portalAnchors = new Map();
    for (const object of seen)
      this._syncMaskEscape(object, this.elements.get(object), portalAnchors);
    for (const [object, element] of this.elements) if (!seen.has(object)) {
      element._maskEscapePortal?.remove();
      element.remove();
      this.elements.delete(object);
    }
    for (const object of this.runtime?._objects ?? [])
      for (const component of object._components)
        if (component.constructor.name === 'ScrollRect' && component.isActiveAndEnabled)
          component._flushScrollChange();
    if (!Canvas._forcingUpdate) Canvas.willRenderCanvases.Invoke();
  }
  _element(object, seen) {
    seen.add(object);
    let element = this.elements.get(object);
    if (!element) {
      element = this.host.ownerDocument.createElement('div');
      element._uiObject = object;
      element.style.position = 'absolute';
      element.style.boxSizing = 'border-box';
      element.addEventListener('pointerenter', event => {
        const data = this._pointerData(object, event);
        if (!raycastAllowed(object) || !customRaycastAllowed(object, data.position)) return;
        this.eventSystem?._pointerOver.add(data.pointerId);
        this._dispatch(object, 'OnPointerEnter', data);
      });
      element.addEventListener('pointerleave', event => {
        const data = this._pointerData(object, event);
        data.pointerEnter = null; data.hovered = []; data.fullyExited = true;
        this.eventSystem?._pointerOver.delete(data.pointerId);
        this._dispatch(object, 'OnPointerExit', data);
      });
      element.addEventListener('pointerdown', event => {
        const data = this._pointerData(object, event);
        if (!raycastAllowed(object) || !customRaycastAllowed(object, data.position)) return;
        data.pressPosition = data.position.clone();
        data.lastPress = this._lastPointerPress.get(data.pointerId) ?? null;
        data.pointerPressRaycast = data.pointerCurrentRaycast;
        data.eligibleForClick = true;
        const downTarget = this._handlerTarget(object, 'OnPointerDown');
        const clickTarget = this._handlerTarget(object, 'OnPointerClick');
        const selectTarget = this._handlerTarget(object, 'OnSelect');
        const pressTarget = downTarget ?? clickTarget;
        data.pointerPress = null;
        data.pointerDrag = this._handlerTarget(object, 'OnDrag');
        const state = { object, start: data.position.clone(),
          last: data.position.clone(), dragging: false,
          pressRaycast: data.pointerPressRaycast, clickTime: data.clickTime,
          useDragThreshold: true, dragTarget: data.pointerDrag,
          pressTarget, clickTarget };
        this._pressed.set(data.pointerId, state);
        this._suppressClick.delete(object);
        this.eventSystem?._pointerOver.add(data.pointerId);
        if (this.eventSystem?.currentSelectedGameObject !== selectTarget)
          this.eventSystem?.SetSelectedGameObject(null, data);
        this._dispatchDirect(downTarget, 'OnPointerDown', data);
        data.pointerPress = pressTarget;
        this._dispatch(data.pointerDrag, 'OnInitializePotentialDrag', data);
        state.useDragThreshold = data.useDragThreshold;
        state.dragTarget = data.pointerDrag;
        if (event.pointerId != null) element.setPointerCapture?.(event.pointerId);
        event.stopPropagation?.();
      });
      element.addEventListener('pointermove', event => {
        const data = this._pointerData(object, event);
        const state = this._pressed.get(data.pointerId);
        if (!state) return;
        data.pressPosition = state.start.clone();
        data.pointerPressRaycast = state.pressRaycast;
        data.clickTime = state.clickTime;
        data.delta = data.position.clone().sub(state.last);
        data.pointerDrag = state.dragTarget;
        data.pointerPress = state.pressTarget;
        state.last = data.position.clone();
        const moved = data.position.clone().sub(state.start).length();
        if (!state.dragging && moved >= (state.useDragThreshold
          ? this.eventSystem?.pixelDragThreshold ?? 10 : 0)) {
          state.dragging = true;
          this._suppressClick.add(state.object);
          data.eligibleForClick = false;
          data.dragging = true;
          this._dispatch(state.dragTarget, 'OnBeginDrag', data);
        }
        if (state.dragging) {
          data.dragging = true;
          data.eligibleForClick = false;
          this._dispatch(state.dragTarget, 'OnDrag', data);
        }
        event.stopPropagation?.();
      });
      element.addEventListener('pointerup', event => {
        const data = this._pointerData(object, event);
        const state = this._pressed.get(data.pointerId);
        this._pressed.delete(data.pointerId);
        if (state) {
          data.pressPosition = state.start.clone();
          data.pointerPressRaycast = state.pressRaycast;
          data.clickTime = state.clickTime;
          data.delta = data.position.clone().sub(state.last);
          data.pointerDrag = state.dragTarget;
          data.pointerPress = state.pressTarget;
          data.pointerClick = state.clickTarget;
          data.lastPress = this._lastPointerPress.get(data.pointerId) ?? null;
          data.dragging = state.dragging;
          const hit = this._pointerHit(event, element, object);
          const releaseTarget = hit ? this._handlerTarget(hit, 'OnPointerClick') : null;
          data.pointerEnter = hit;
          data.hovered = hit ? [hit] : [];
          data.pointerCurrentRaycast = hit
            ? new RaycastResult({ gameObject: hit,
              module: data.pointerCurrentRaycast.module,
              screenPosition: data.position.clone() }) : new RaycastResult();
          data.eligibleForClick = !state.dragging && !!state.clickTarget
            && releaseTarget === state.clickTarget;
          this._dispatchDirect(state.pressTarget, 'OnPointerUp', data);
          if (data.eligibleForClick)
            this._dispatchDirect(state.clickTarget, 'OnPointerClick', data);
          if (state.dragging) {
            this._dispatch(hit, 'OnDrop', data);
            this._dispatchDirect(state.dragTarget, 'OnEndDrag', data);
          }
          this._suppressClick.add(state.object);
          this._lastPointerPress.set(data.pointerId, state.pressTarget);
        }
        if (event.pointerType === 'touch' || event.pointerType === 'pen')
          this.eventSystem?._pointerOver.delete(data.pointerId);
        if (event.pointerId != null) element.releasePointerCapture?.(event.pointerId);
        event.stopPropagation?.();
      });
      element.addEventListener('pointercancel', event => {
        const data = this._pointerData(object, event);
        const state = this._pressed.get(data.pointerId);
        this._pressed.delete(data.pointerId);
        if (state) {
          data.pointerPress = state.pressTarget;
          data.pointerClick = state.clickTarget;
          data.lastPress = this._lastPointerPress.get(data.pointerId) ?? null;
          data.pressPosition = state.start.clone();
          data.pointerPressRaycast = state.pressRaycast;
          data.clickTime = state.clickTime;
          data.pointerDrag = state.dragTarget;
          data.dragging = state.dragging;
          this._dispatchDirect(state.pressTarget, 'OnPointerUp', data);
          if (state.dragging) this._dispatchDirect(state.dragTarget, 'OnEndDrag', data);
          this._suppressClick.add(state.object);
          this._lastPointerPress.set(data.pointerId, state.pressTarget);
        }
        this.eventSystem?._pointerOver.delete(data.pointerId);
        event.stopPropagation?.();
      });
      element.addEventListener('click', event => {
        if (this._suppressClick.delete(object)) {
          event.stopPropagation?.();
          return;
        }
        // Pointer clicks have already been delivered with the press payload on release.
        if (event.isTrusted && event.detail > 0) {
          event.stopPropagation?.();
          return;
        }
        if (raycastAllowed(object)) {
          event.stopPropagation?.();
          this._dispatch(object, 'OnPointerClick', this._pointerData(object, event));
        }
      });
      element.addEventListener('wheel', event => {
        if (!raycastAllowed(object)) return;
        const data = this._pointerData(object, event);
        const unit = event.deltaMode === 0 ? 100 : 1;
        const scale = this._uiInputModule?.scrollDeltaPerTick ?? 1;
        data.scrollDelta = new Vector2(-(event.deltaX ?? 0) / unit * scale,
          -(event.deltaY ?? 0) / unit * scale);
        this._dispatch(this._handlerTarget(object, 'OnScroll'), 'OnScroll', data);
        event.preventDefault?.();
        event.stopPropagation?.();
      });
      element.addEventListener('focusin', event => {
        for (let node = event.target; node && node !== element;
          node = node.parentNode ?? node.parent)
          if (node._uiObject) return;
        this.eventSystem?.SetSelectedGameObject(this._handlerTarget(object, 'OnSelect'));
      });
      element.addEventListener('keydown', event => this._handleKeyDown(event, object));
      this.elements.set(object, element);
    }
    return element;
  }
  _handleKeyDown(event, focusedObject = null) {
    if (this._legacyInputModule?.inputOverride) return;
    if (['INPUT', 'TEXTAREA', 'SELECT'].includes(event.target?.tagName?.toUpperCase())
      || event.target?.isContentEditable) return;
    const selected = this.eventSystem?.currentSelectedGameObject ?? focusedObject;
    if (!selected || !this.elements.has(selected)) return;
    const directions = { ArrowLeft: [MoveDirection.Left, -1, 0],
      ArrowUp: [MoveDirection.Up, 0, 1], ArrowRight: [MoveDirection.Right, 1, 0],
      ArrowDown: [MoveDirection.Down, 0, -1] };
    const move = directions[event.key];
    if (move) {
      if (this._uiInputModule?.move?.action) return;
      if (!this.eventSystem?.sendNavigationEvents) return;
      const data = new AxisEventData(this.eventSystem);
      data.moveDir = move[0]; data.moveVector = new Vector2(move[1], move[2]);
      this._dispatch(selected, 'OnMove', data);
    } else if (event.key === 'Enter' || event.key === ' ') {
      if (this._uiInputModule?.submit?.action) return;
      this._dispatch(selected, 'OnSubmit', new BaseEventData(this.eventSystem));
    } else if (event.key === 'Escape') {
      if (this._uiInputModule?.cancel?.action) return;
      this._dispatch(selected, 'OnCancel', new BaseEventData(this.eventSystem));
    } else return;
    event.preventDefault?.();
    event.stopPropagation?.();
  }
  _bindInputModule(module) {
    const actions = ['move', 'submit', 'cancel'].map(name => module?.[name]?.action ?? null);
    if (module === this._uiInputModule && actions.every((action, index) =>
      action === this._uiActionBindings?.[index]?.action)) return;
    for (const binding of this._uiActionBindings ?? []) {
      binding.action?.performed.RemoveListener(binding.listener);
      binding.action?.canceled.RemoveListener(binding.canceled);
      if (binding.enabledHere) binding.action.Disable();
    }
    this._uiInputModule = module;
    this._moveRepeat = null;
    this._uiActionBindings = actions.map((action, index) => {
      if (!action) return { action: null };
      const enabledHere = !action.enabled;
      if (enabledHere) action.Enable();
      const listener = context => {
        if (index === 0) {
          const vector = context.ReadValue('Vector2');
          this._routeMove(vector);
          this._moveRepeat = { remaining: Math.max(0.01, module.moveRepeatDelay) };
        } else this._routeAction(index === 1 ? 'OnSubmit' : 'OnCancel');
      };
      const canceled = index === 0 ? () => { this._moveRepeat = null; } : () => {};
      action.performed.AddListener(listener);
      if (index === 0) action.canceled.AddListener(canceled);
      return { action, listener, canceled, enabledHere };
    });
  }
  _routeAction(method) {
    const selected = this.eventSystem?.currentSelectedGameObject;
    if (selected && this.elements.has(selected))
      this._dispatch(selected, method, new BaseEventData(this.eventSystem));
  }
  _processLegacyOverride(module, deltaTime) {
    const input = module.inputOverride;
    if (this.eventSystem?.sendNavigationEvents) {
      const x = Number(input.GetAxisRaw(module.horizontalAxis)) || 0;
      const y = Number(input.GetAxisRaw(module.verticalAxis)) || 0;
      if (Math.hypot(x, y) > 0.6) {
        if (!this._legacyMove || this._legacyMove.x !== x || this._legacyMove.y !== y) {
          this._routeMove(new Vector2(x, y));
          this._legacyMove = { x, y, remaining: module.repeatDelay };
        } else {
          this._legacyMove.remaining -= deltaTime;
          if (this._legacyMove.remaining <= 0) {
            this._routeMove(new Vector2(x, y));
            this._legacyMove.remaining = 1 / Math.max(1, module.inputActionsPerSecond);
          }
        }
      } else this._legacyMove = null;
    }
    if (input.GetButtonDown(module.submitButton)) this._routeAction('OnSubmit');
    if (input.GetButtonDown(module.cancelButton)) this._routeAction('OnCancel');
  }
  _routeMove(vector) {
    if (!this.eventSystem?.sendNavigationEvents) return;
    const selected = this.eventSystem.currentSelectedGameObject;
    if (!selected || !this.elements.has(selected)) return;
    const x = Number(vector.x) || 0, y = Number(vector.y) || 0;
    const data = new AxisEventData(this.eventSystem);
    data.moveVector = new Vector2(x, y);
    data.moveDir = Math.abs(x) > Math.abs(y)
      ? (x > 0 ? MoveDirection.Right : MoveDirection.Left)
      : (y > 0 ? MoveDirection.Up : MoveDirection.Down);
    if (Math.max(Math.abs(x), Math.abs(y)) > 0.5)
      this._dispatch(selected, 'OnMove', data);
  }
  _croppedSpriteSource(source, sprite, tint = null) {
    const rect = sprite.rect;
    const key = JSON.stringify([source, rect.x, rect.y, rect.width, rect.height,
      tint?.r, tint?.g, tint?.b]);
    let entry = this._spriteCropCache.get(key);
    if (entry) return entry;
    entry = { url: '', error: false };
    this._spriteCropCache.set(key, entry);
    const ImageClass = this.host.ownerDocument.defaultView?.Image ?? globalThis.Image;
    if (!ImageClass) { entry.error = true; return entry; }
    const image = new ImageClass();
    image.crossOrigin = 'anonymous';
    image.onload = () => {
      try {
        const canvas = this.host.ownerDocument.createElement('canvas');
        canvas.width = rect.width; canvas.height = rect.height;
        const context = canvas.getContext('2d');
        context.drawImage(image, rect.x,
          image.height - rect.y - rect.height, rect.width, rect.height,
          0, 0, rect.width, rect.height);
        if (tint) {
          const pixels = context.getImageData(0, 0, canvas.width, canvas.height);
          for (let index = 0; index < pixels.data.length; index += 4) {
            pixels.data[index] *= tint.r;
            pixels.data[index + 1] *= tint.g;
            pixels.data[index + 2] *= tint.b;
          }
          context.putImageData(pixels, 0, 0);
        }
        entry.url = canvas.toDataURL('image/png');
      } catch { entry.error = true; }
      if (this.runtime) this.afterLateUpdate(0);
    };
    image.onerror = () => {
      entry.error = true;
      if (this.runtime) this.afterLateUpdate(0);
    };
    image.src = source;
    return entry;
  }
  _render(object, parent, seen) {
    if (!object.activeInHierarchy) return;
    const canvas = object._components.find(component =>
      component.constructor.name === 'Canvas' && component.enabled);
    if (canvas && canvas.renderMode !== 0) {
      this._unsupportedCanvas(object, canvas);
      return;
    }
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
      // RectTransform scale applies around its pivot to the graphic and all
      // descendants. Leaving it out enlarges scaled HUD groups and text.
      element.style.transformOrigin = `${rect.pivot.x * 100}% ${(1 - rect.pivot.y) * 100}%`;
      const localScale = rect.localScale;
      element.style.transform = localScale.x === 1 && localScale.y === 1 ? ''
        : `scale(${localScale.x}, ${localScale.y})`;
    }
    const image = object._components.find(component => component.constructor.name === 'Image' && component.enabled);
    const rawImage = object._components.find(component => component.constructor.name === 'RawImage' && component.enabled);
    const graphic = image ?? rawImage;
    const text = object._components.find(component => component.constructor.name === 'Text' && component.enabled);
    const tmpText = object._components.find(component =>
      component.constructor.name === 'TextMeshProUGUI' && component.enabled);
    const button = object._components.find(component => component.constructor.name === 'Button' && component.enabled);
    const selectable = object._components.find(component =>
      ['Toggle', 'Slider', 'Scrollbar', 'InputField', 'Dropdown'].includes(component.constructor.name)
        && component.enabled);
    const scrollRect = object._components.find(component =>
      component.constructor.name === 'ScrollRect' && component.isActiveAndEnabled);
    const mask = object._components.find(component =>
      component.constructor.name === 'Mask' && component.isActiveAndEnabled);
    const rectMask = object._components.find(component =>
      component.constructor.name === 'RectMask2D' && component.isActiveAndEnabled);
    scrollRect?._syncScrollBounds();
    scrollRect?._advanceScroll(this._frameDeltaTime);
    const isScrollViewport = scrollViewport(object);
    const canvasRenderer = object._components.find(component =>
      component.constructor.name === 'CanvasRenderer');
    const baseRendererColor = canvasRenderer?.GetColor() ?? { r: 1, g: 1, b: 1, a: 1 };
    const transition = selectableForGraphic(object, graphic ?? text);
    const sprite = image && transition?.transition === 2
      ? transition.spriteState?.[`${transition._transitionState}Sprite`] ?? image._activeSprite
      : image?._activeSprite;
    if (transition?.transition === 3)
      this._unsupported(transition.gameObject, 'Selectable Animation transition');
    const tint = transition?.transition === 1
      ? transition.colors?.[`${transition._transitionState}Color`] : null;
    const multiplier = tint ? transition.colors.colorMultiplier ?? 1 : 1;
    const rendererColor = tint ? {
      r: tint.r * multiplier,
      g: tint.g * multiplier,
      b: tint.b * multiplier,
      a: tint.a,
    } : baseRendererColor;
    const graphicTint = graphic && { r: graphic.color.r * rendererColor.r,
      g: graphic.color.g * rendererColor.g,
      b: graphic.color.b * rendererColor.b, a: graphic.color.a * (tint?.a ?? 1) };
    let draw = element._graphicLayer;
    if (!draw) {
      draw = this.host.ownerDocument.createElement('div');
      Object.assign(draw.style, { position: 'absolute', inset: '0',
        boxSizing: 'border-box', pointerEvents: 'none' });
      element.prepend(draw);
      element._graphicLayer = draw;
    }
    const texture = sprite?.texture?.threeTexture ?? sprite?.texture
      ?? rawImage?.texture?.threeTexture ?? rawImage?.texture;
    const source = texture?.image?.src;
    if (image?.useSpriteMesh)
      this._unsupported(object, 'Image.useSpriteMesh native sprite mesh');
    if (image?.material)
      this._unsupported(object, 'Image.material native uGUI material');
    draw.style.imageRendering = sprite?.texture?.filterMode === FilterMode.Point
      || rawImage?.texture?.filterMode === FilterMode.Point ? 'pixelated' : 'auto';
    const textureTint = source && graphic && (graphicTint.r !== 1
      || graphicTint.g !== 1 || graphicTint.b !== 1);
    draw.style.backgroundColor = graphic && !source ? color(graphicTint)
      : textureTint ? color({ ...graphicTint, a: 1 }) : 'transparent';
    draw.style.transition = transition?.transition === 1 && transition.colors?.fadeDuration > 0
      ? `background-color ${transition.colors.fadeDuration}s linear, `
        + `color ${transition.colors.fadeDuration}s linear, `
        + `opacity ${transition.colors.fadeDuration}s linear` : '';
    draw.style.backgroundImage = source ? `url(${JSON.stringify(source)})` : '';
    draw.style.backgroundBlendMode = textureTint ? 'multiply' : '';
    draw.style.backgroundRepeat = rawImage ? 'no-repeat' : '';
    if (source && rawImage) {
      const uv = rawImage.uvRect;
      if (uv.width > 0 && uv.height > 0 && uv.x >= 0 && uv.y >= 0
        && uv.x + uv.width <= 1 && uv.y + uv.height <= 1) {
        draw.style.backgroundSize = `${100 / uv.width}% ${100 / uv.height}%`;
        draw.style.backgroundPosition = `${uv.width === 1 ? 0 : 100 * uv.x / (1 - uv.width)}% `
          + `${uv.height === 1 ? 0 : 100 * (1 - uv.y - uv.height) / (1 - uv.height)}%`;
      } else {
        draw.style.backgroundSize = '100% 100%';
        draw.style.backgroundPosition = '0% 0%';
        this._unsupported(object, 'RawImage UV rectangle outside one texture');
      }
    } else if (source && sprite?.rect && sprite.texture?.width && sprite.texture?.height) {
      const rect = sprite.rect;
      const width = sprite.texture.width, height = sprite.texture.height;
      draw.style.backgroundSize = `${100 * width / rect.width}% ${100 * height / rect.height}%`;
      draw.style.backgroundPosition = `${width === rect.width ? 0 : 100 * rect.x / (width - rect.width)}% `
        + `${height === rect.height ? 0 : 100 * (height - rect.y - rect.height) / (height - rect.height)}%`;
    } else {
      draw.style.backgroundSize = '100% 100%';
      draw.style.backgroundPosition = '0% 0%';
    }
    draw.style.clipPath = '';
    draw.style.borderStyle = '';
    draw.style.borderWidth = '';
    draw.style.borderImageSource = '';
    draw.style.borderImageSlice = '';
    draw.style.borderImageWidth = '';
    draw.style.borderImageRepeat = '';
    Object.assign(draw.style, { inset: '0', left: '0', top: '0', right: '0', bottom: '0',
      width: '', height: '' });
    const fullSprite = sprite?.rect && sprite.rect.x === 0 && sprite.rect.y === 0
      && sprite.rect.width === sprite.texture?.width
      && sprite.rect.height === sprite.texture?.height;
    if (image?.preserveAspect && source && (image.type === 0 || image.type === 3)) {
      if (fullSprite || image.type === 0 || image.type === 3) {
        const cropped = fullSprite ? null : this._croppedSpriteSource(source, sprite);
        draw.style.backgroundImage = fullSprite || cropped?.url
          ? `url(${JSON.stringify(cropped?.url ?? source)})` : '';
        draw.style.backgroundSize = 'contain';
        draw.style.backgroundPosition = 'center';
        draw.style.backgroundRepeat = 'no-repeat';
        if (image.type === 3) {
          const imageAspect = sprite.rect.width / sprite.rect.height;
          const boxAspect = rect.rect.width / rect.rect.height;
          if (Number.isFinite(imageAspect) && Number.isFinite(boxAspect)
            && imageAspect > 0 && boxAspect > 0) {
            if (imageAspect < boxAspect) {
              const width = 100 * imageAspect / boxAspect;
              Object.assign(draw.style, { inset: 'auto', left: `${(100 - width) / 2}%`,
                top: '0', width: `${width}%`, height: '100%' });
            } else {
              const height = 100 * boxAspect / imageAspect;
              Object.assign(draw.style, { inset: 'auto', left: '0',
                top: `${(100 - height) / 2}%`, width: '100%', height: `${height}%` });
            }
          }
        }
        if (cropped?.error) this._unsupported(object, 'Atlas sprite crop unavailable');
      }
    }
    const borderedTiled = image?.type === 2 && sprite?.border
      && [sprite.border.x, sprite.border.y, sprite.border.z, sprite.border.w]
        .some(value => value !== 0);
    if ((image?.type === 1 || borderedTiled) && source && sprite.border) {
      const border = sprite.border;
      const scale = spritePixelScale(object, sprite, image);
      // Unity shrinks opposing sprite borders when the RectTransform is thinner
      // than their combined size. CSS borders otherwise spill outside the UI rect.
      const horizontal = Math.max(0, Math.min(1, rect.rect.width
        / Math.max(1e-6, (border.x + border.z) * scale)));
      const vertical = Math.max(0, Math.min(1, rect.rect.height
        / Math.max(1e-6, (border.y + border.w) * scale)));
      const cropped = fullSprite && !textureTint ? null
        : this._croppedSpriteSource(source, sprite, textureTint ? graphicTint : null);
      draw.style.backgroundImage = '';
      draw.style.backgroundColor = 'transparent';
      draw.style.backgroundBlendMode = '';
      draw.style.borderStyle = 'solid';
      draw.style.borderWidth = `${border.w * scale * vertical}px `
        + `${border.z * scale * horizontal}px `
        + `${border.y * scale * vertical}px ${border.x * scale * horizontal}px`;
      draw.style.borderImageSource = fullSprite && !textureTint || cropped?.url
        ? `url(${JSON.stringify(cropped?.url ?? source)})` : '';
      // Unity samples the center UV even when opposite sprite borders touch.
      // CSS border-image needs a nonzero source slice to paint that center.
      const sourceWidth = sprite.rect?.width ?? sprite.texture?.width;
      const sourceHeight = sprite.rect?.height ?? sprite.texture?.height;
      const sliceX = sourceWidth > 2 && border.x + border.z >= sourceWidth
        ? (sourceWidth - 2) / (border.x + border.z) : 1;
      const sliceY = sourceHeight > 2 && border.y + border.w >= sourceHeight
        ? (sourceHeight - 2) / (border.y + border.w) : 1;
      draw.style.borderImageSlice = `${border.w * sliceY} ${border.z * sliceX} `
        + `${border.y * sliceY} ${border.x * sliceX}`
        + (image.fillCenter ? ' fill' : '');
      draw.style.borderImageWidth = '1';
      draw.style.borderImageRepeat = borderedTiled ? 'repeat' : 'stretch';
      if (cropped?.error) this._unsupported(object, 'Atlas sprite crop unavailable');
    } else if (image?.type === 2 && source && sprite.border
      && [sprite.border.x, sprite.border.y, sprite.border.z, sprite.border.w]
        .every(value => value === 0)) {
      const scale = spritePixelScale(object, sprite, image);
      const repeatAtlas = !fullSprite && !sprite.packed
        && sprite.texture?.wrapMode === TextureWrapMode.Repeat;
      const cropped = fullSprite || repeatAtlas ? null
        : this._croppedSpriteSource(source, sprite);
      draw.style.backgroundImage = image.fillCenter && (fullSprite || repeatAtlas || cropped?.url)
        ? `url(${JSON.stringify(cropped?.url ?? source)})` : '';
      draw.style.backgroundRepeat = 'repeat';
      if (repeatAtlas) {
        const countX = rect.rect.width / (sprite.rect.width * scale);
        const countY = rect.rect.height / (sprite.rect.height * scale);
        const x = ((sprite.rect.x * countX) % sprite.texture.width
          + sprite.texture.width) % sprite.texture.width;
        const y = ((sprite.texture.height - (sprite.rect.y + sprite.rect.height)
          * countY) % sprite.texture.height + sprite.texture.height)
          % sprite.texture.height;
        draw.style.backgroundSize = `${sprite.texture.width * scale}px `
          + `${sprite.texture.height * scale}px`;
        draw.style.backgroundPosition = `${-x * scale}px ${-y * scale}px`;
      } else {
        draw.style.backgroundSize = `${sprite.rect.width * scale}px `
          + `${sprite.rect.height * scale}px`;
        draw.style.backgroundPosition = '0% 0%';
      }
      if (cropped?.error) this._unsupported(object, 'Atlas sprite crop unavailable');
    } else if (image?.type === 3 && sprite) {
      const hidden = (1 - image.fillAmount) * 100;
      if (image.fillMethod === 0 && (image.fillOrigin === 0 || image.fillOrigin === 1))
        draw.style.clipPath = image.fillOrigin === 0
          ? `inset(0 ${hidden}% 0 0)` : `inset(0 0 0 ${hidden}%)`;
      else if (image.fillMethod === 1 && (image.fillOrigin === 0 || image.fillOrigin === 1))
        draw.style.clipPath = image.fillOrigin === 0
          ? `inset(${hidden}% 0 0 0)` : `inset(0 0 ${hidden}% 0)`;
      else if ([2, 3, 4].includes(image.fillMethod) && Number.isInteger(image.fillOrigin)
        && image.fillOrigin >= 0 && image.fillOrigin <= 3)
        draw.style.clipPath = radialClipPath(image.fillMethod, image.fillAmount,
          image.fillOrigin, image.fillClockwise);
      else this._unsupported(object, `Image fillMethod ${image.fillMethod} origin ${image.fillOrigin}`);
    } else if (image && image.type !== 0 && sprite)
      this._unsupported(object, `Image type ${image.type}`);
    if (rawImage?.texture && !source) this._unsupported(object, 'RawImage texture has no browser image');
    if (canvasRenderer?._clearRequested)
      this._unsupported(object, 'CanvasRenderer.Clear mesh buffer');
    if (canvasRenderer?.cull) {
      draw.style.backgroundColor = 'transparent';
      draw.style.backgroundImage = '';
      draw.style.borderImageSource = '';
    }
    const maskLayers = [];
    if (mask && source) maskLayers.push(`url(${JSON.stringify(source)})`);
    if (rectMask?.softness.x > 0) {
      const ramp = (rectMask.softness.x + 2) / 2;
      maskLayers.push(`linear-gradient(to right, transparent, black ${ramp}px, `
        + `black calc(100% - ${ramp}px), transparent)`);
    }
    if (rectMask?.softness.y > 0) {
      const ramp = (rectMask.softness.y + 2) / 2;
      maskLayers.push(`linear-gradient(to bottom, transparent, black ${ramp}px, `
        + `black calc(100% - ${ramp}px), transparent)`);
    }
    element.style.maskImage = maskLayers.join(', ');
    element.style.maskSize = maskLayers.map((_, index) => index === 0 && mask && source
      ? draw.style.backgroundSize : '100% 100%').join(', ');
    element.style.maskPosition = maskLayers.map((_, index) => index === 0 && mask && source
      ? draw.style.backgroundPosition : 'center').join(', ');
    element.style.maskRepeat = maskLayers.length ? 'no-repeat' : '';
    element.style.maskComposite = maskLayers.length > 1 ? 'intersect' : '';
    element.style.imageRendering = mask && sprite?.texture?.filterMode === FilterMode.Point
      ? 'pixelated' : '';
    if (mask) {
      if (!graphic || graphic.color.a < 1 || text)
        this._unsupported(object, 'Mask without an opaque Image graphic');
      if (mask.showMaskGraphic === false) {
        draw.style.backgroundColor = 'transparent';
        draw.style.backgroundImage = '';
      }
    }
    draw.style.color = text ? color({ r: text.color.r * rendererColor.r,
      g: text.color.g * rendererColor.g,
      b: text.color.b * rendererColor.b, a: text.color.a * (tint?.a ?? 1) }) : '';
    draw.style.fontSize = text ? `${text.fontSize}px` : '';
    draw.style.fontWeight = text?.fontStyle === 1 || text?.fontStyle === 3
      ? 'bold' : '';
    draw.style.fontStyle = text?.fontStyle === 2 || text?.fontStyle === 3
      ? 'italic' : '';
    const fontUrl = text?.font?.url;
    draw.style.fontFamily = this._legacyFontFamily(text, object);
    const effects = object._components.filter(component => component.enabled
      && ['Shadow', 'Outline'].includes(component.constructor.name));
    draw.style.textShadow = '';
    draw.style.boxShadow = '';
    draw.style.filter = '';
    if (effects.length > 1) this._unsupported(object, 'Multiple uGUI graphic effects');
    const effect = effects.at(-1);
    if (effect && (graphic || text)) {
      const dx = Number(effect.effectDistance?.x) || 0;
      const dy = -(Number(effect.effectDistance?.y) || 0);
      const alpha = effect.effectColor.a * (effect.useGraphicAlpha && !source
        ? (graphic ?? text).color.a : 1);
      const effectColor = color({ ...effect.effectColor, a: alpha });
      const offsets = effect.constructor.name === 'Outline'
        ? [[dx, dy], [dx, -dy], [-dx, dy], [-dx, -dy]] : [[dx, dy]];
      const shadows = offsets.map(([x, y]) => `${x}px ${y}px 0 ${effectColor}`);
      if (text) draw.style.textShadow = shadows.join(', ');
      else if (source) draw.style.filter = offsets.map(([x, y]) =>
        `drop-shadow(${x}px ${y}px 0 ${effectColor})`).join(' ');
      else draw.style.boxShadow = shadows.join(', ');
      if (source && !effect.useGraphicAlpha && graphic.color.a < 1)
        this._unsupported(object, 'Texture effect without graphic alpha');
    }
    const alignment = text?.alignment ?? 0;
    draw.style.textAlign = text ? ['left', 'center', 'right'][alignment % 3] : '';
    draw.style.display = text && !selectable ? 'flex' : '';
    draw.style.justifyContent = text && !selectable
      ? ['flex-start', 'center', 'flex-end'][alignment % 3] : '';
    draw.style.alignItems = text && !selectable
      ? ['flex-start', 'center', 'flex-end'][Math.floor(alignment / 3)] : '';
    draw.style.whiteSpace = text ? (text.horizontalOverflow === 1 ? 'pre' : 'pre-wrap') : '';
    draw.style.lineHeight = text && text.lineSpacing !== 1
      ? `${text.fontSize * text.lineSpacing * 1.3}px` : '';
    const receivesPointer = raycastAllowed(object)
      && (graphic?.raycastTarget || text?.raycastTarget
        || button?.IsInteractable() || selectable?.IsInteractable()
        || scrollRect || isScrollViewport);
    element.tabIndex = button?.IsInteractable() || selectable?.constructor.name === 'Dropdown'
      && selectable.IsInteractable() ? 0 : -1;
    element.style.opacity = String(groupAlpha(object));
    draw.style.opacity = String(canvasRenderer?.cull ? 0
      : (canvasRenderer?.GetAlpha() ?? 1) * (source ? graphicTint?.a ?? 1 : 1));
    element.style.pointerEvents = receivesPointer ? 'auto' : 'none';
    element.style.overflow = isScrollViewport || mask || rectMask ? 'hidden' : '';
    element.style.clipPath = rectMask && !rectMask.softness.x && !rectMask.softness.y
      ? 'inset(0.5px)' : '';
    element.style.cursor = (button?.IsInteractable()
      || selectable?.constructor.name === 'Dropdown' && selectable.IsInteractable())
      && receivesPointer ? 'pointer' : '';
    const visibleText = mask?.showMaskGraphic === false || canvasRenderer?.cull
      ? '' : text?.text ?? '';
    if (text) {
      let label = draw.firstChild;
      if (!label || label.nodeType !== 1 || !label._unityTextRoot) {
        draw.textContent = '';
        label = this.host.ownerDocument.createElement('span');
        label._unityTextRoot = true;
        draw.appendChild(label);
      }
      label.style.display = 'block';
      label.style.width = text.horizontalOverflow === 1 ? 'max-content' : '100%';
      const height = rect.sizeDelta.y > 0 ? rect.sizeDelta.y : element.clientHeight;
      if (text.supportRichText !== false) {
        for (const tag of visibleText.matchAll(/<\/?([a-z][a-z0-9]*)(?=[\s=>])/gi)) {
          if (!['b', 'i', 'color', 'size'].includes(tag[1].toLowerCase()))
            this._unsupported(object, `uGUI Text rich tag <${tag[1]}>`);
        }
      }
      if (text.supportRichText !== false && /<(?:\/?(?:b|i|color|size))(?:[=>\s])/i.test(visibleText)) {
        if (label._unityRichText !== visibleText) {
          label.textContent = '';
          const stack = [{ name: '', node: label }];
          for (const token of visibleText.split(/(<\/?(?:b|i|color(?:=[^>]+)?|size(?:=[^>]+)?)>)/gi)) {
            const match = /^<(\/)?(b|i|color|size)(?:=([^>]+))?>$/i.exec(token);
            if (!match) {
              stack.at(-1).node.appendChild(this.host.ownerDocument.createTextNode(token));
              continue;
            }
            const name = match[2].toLowerCase();
            if (match[1]) {
              if (stack.length > 1 && stack.at(-1).name === name) stack.pop();
              else stack.at(-1).node.appendChild(this.host.ownerDocument.createTextNode(token));
              continue;
            }
            const value = match[3]?.trim();
            const span = this.host.ownerDocument.createElement('span');
            if (name === 'b') span.style.fontWeight = 'bold';
            else if (name === 'i') span.style.fontStyle = 'italic';
            else if (name === 'color' && (/^#[0-9a-f]{6}([0-9a-f]{2})?$/i.test(value)
              || /^[a-z]+$/i.test(value))) span.style.color = value;
            else if (name === 'size' && /^\d+(\.\d+)?$/.test(value))
              span.style.fontSize = `${Number(value)}px`;
            else {
              stack.at(-1).node.appendChild(this.host.ownerDocument.createTextNode(token));
              continue;
            }
            stack.at(-1).node.appendChild(span);
            stack.push({ name, node: span });
          }
          label._unityRichText = visibleText;
        }
      } else {
        if (label._unityRichText !== undefined || label.textContent !== visibleText)
          label.textContent = visibleText;
        delete label._unityRichText;
      }
      let renderedFontSize = text.fontSize;
      if (text.resizeTextForBestFit && label.isConnected && height > 0) {
        const family = this._fontFaces.get(fontUrl);
        const loaded = !family || this.host.ownerDocument.fonts?.check?.(`15px "${family}"`);
        const fitKey = [visibleText, element.clientWidth, height, text.fontStyle,
          text.resizeTextMinSize, text.resizeTextMaxSize, fontUrl, loaded].join('|');
        if (label._unityFitKey === fitKey) renderedFontSize = label._unityFittedSize;
        else {
          label.style.maxHeight = '';
          label.style.overflow = 'visible';
          const minimum = Math.max(1, Math.min(text.resizeTextMinSize, text.resizeTextMaxSize));
          const maximum = Math.max(minimum, text.resizeTextMaxSize);
          for (let size = maximum; size >= minimum; size--) {
            draw.style.fontSize = `${size}px`;
            if (label.scrollHeight * 1.05 <= height) {
              renderedFontSize = size;
              break;
            }
            renderedFontSize = minimum;
          }
          label._unityFitKey = fitKey;
          label._unityFittedSize = renderedFontSize;
        }
      }
      draw.style.fontSize = `${renderedFontSize}px`;
      const lineHeight = renderedFontSize * (text.lineSpacing === 1 ? 1.2
        : text.lineSpacing * 1.3);
      const lines = Math.max(1, Math.floor((height + 0.01) / lineHeight));
      label.style.maxHeight = text.verticalOverflow === 0
        ? `${lines * lineHeight}px` : '';
      label.style.overflow = text.verticalOverflow === 0 && text.horizontalOverflow !== 1
        ? 'hidden' : 'visible';
      label.style.clipPath = text.verticalOverflow === 0 && text.horizontalOverflow === 1
        ? 'inset(0 -1000px 0 -1000px)' : '';
    } else if (tmpText) {
      let canvas = draw.firstChild;
      if (!canvas || canvas.nodeName !== 'CANVAS') {
        draw.textContent = '';
        canvas = this.host.ownerDocument.createElement('canvas');
        Object.assign(canvas.style, { width: '100%', height: '100%', display: 'block' });
        draw.appendChild(canvas);
      }
      const boxWidth = element.clientWidth || rect.rect.width;
      const boxHeight = element.clientHeight || rect.rect.height;
      const overflow = tmpText.overflowMode === 0;
      const drawWidth = overflow && !tmpText.enableWordWrapping
        ? Math.max(boxWidth, tmpText.preferredWidth + 8) : boxWidth;
      const drawHeight = overflow ? Math.max(boxHeight, tmpText.preferredHeight) : boxHeight;
      const horizontal = tmpText.alignment & 0xFF;
      const vertical = tmpText.alignment & 0xFF00;
      Object.assign(canvas.style, { position: 'absolute', width: `${drawWidth}px`,
        height: `${drawHeight}px`, left: `${horizontal === 4 ? boxWidth - drawWidth
          : horizontal === 2 ? (boxWidth - drawWidth) / 2 : 0}px`,
        top: `${vertical === 0x400 ? boxHeight - drawHeight
          : vertical === 0x200 ? (boxHeight - drawHeight) / 2 : 0}px` });
      if (![0, 3].includes(tmpText.overflowMode))
        this._unsupported(object, `TMP overflow mode ${tmpText.overflowMode}`);
      if (!drawTmpLabel(canvas, tmpText, drawWidth, drawHeight,
        character => this._unsupported(object, `TMP missing glyph U+${character.codePointAt(0)
          .toString(16).toUpperCase().padStart(4, '0')}`), undefined,
        tag => this._unsupported(object, `TMP rich tag <${tag}>`)))
        this._unsupported(object, 'TMP font atlas unavailable');
    } else {
      draw.textContent = '';
    }
    this._control(element, selectable, receivesPointer);
    this._syncAuthoredControl(selectable);
    if (selectable?.constructor.name === 'Toggle' && element._unityControl) {
      Object.assign(element._unityControl.style, text ? {
        position: 'absolute', right: '8px', top: '50%', transform: 'translateY(-50%)',
      } : { position: '', right: '', top: '', transform: '' });
    }
    if ((element.parentNode ?? element.parent) !== parent) parent.appendChild(element);
    for (const child of object.children) {
      if (selectable?.constructor.name === 'InputField'
        && (child === selectable.textComponent?.gameObject || child === selectable.placeholder?.gameObject)) {
        continue;
      }
      if (selectable?.constructor.name === 'Dropdown'
        && child === selectable.template?.gameObject) continue;
      this._render(child, element, seen);
    }
    if (selectable?.constructor.name === 'Toggle' && selectable.graphic) {
      const graphicLayer = this.elements.get(selectable.graphic.gameObject)?._graphicLayer;
      if (graphicLayer) {
        graphicLayer.style.opacity = selectable.isOn ? '1' : '0';
        graphicLayer.style.transition = selectable.toggleTransition === 1
          ? 'opacity 0.1s' : '';
        graphicLayer.style.transition = selectable.colors?.fadeDuration > 0
          ? `opacity ${selectable.colors.fadeDuration}s linear` : '';
      }
    }
    syncUiOrder(element, object.children.map(child => this.elements.get(child))
      .filter(child => child && (child.parentNode ?? child.parent) === element));
  }
  _syncAuthoredControl(control) {
    if (!control) return;
    const kind = control.constructor.name;
    if (kind !== 'Slider' && kind !== 'Scrollbar') return;
    const direction = control.direction ?? 0;
    const axis = direction < 2 ? 'x' : 'y';
    const reversed = direction === 1 || direction === 3;
    const value = Math.max(0, Math.min(1, kind === 'Slider'
      ? control.normalizedValue : control.value));
    const place = (rect, lower, upper) => {
      if (!rect) return;
      const min = rect.anchorMin.clone();
      const max = rect.anchorMax.clone();
      const cross = axis === 'x' ? 'y' : 'x';
      if (min[axis] === lower && max[axis] === upper
        && min[cross] === 0 && max[cross] === 1) return;
      min[axis] = lower; max[axis] = upper;
      min[cross] = 0; max[cross] = 1;
      rect.anchorMin = min; rect.anchorMax = max;
    };
    if (kind === 'Slider') {
      place(control.fillRect, reversed ? 1 - value : 0, reversed ? 1 : value);
      place(control.handleRect, reversed ? 1 - value : value,
        reversed ? 1 - value : value);
    } else {
      const size = Math.max(0, Math.min(1, control.size));
      const lower = value * (1 - size);
      place(control.handleRect, reversed ? 1 - lower - size : lower,
        reversed ? 1 - lower : lower + size);
    }
  }
  _pointerData(object, event) {
    const data = new PointerEventData(this.eventSystem);
    data.pointerId = event.pointerType === 'touch' || event.pointerType === 'pen'
      ? event.pointerId ?? 0 : event.button === 2 ? -2 : event.button === 1 ? -3 : -1;
    data.button = event.button === 2 ? 1 : event.button === 1 ? 2 : 0;
    data.clickCount = event.detail || 1;
    data.pointerEnter = object;
    data.hovered = [object];
    const pressedObject = this._pressed.get(data.pointerId)?.object ?? object;
    data.pointerPress = this._handlerTarget(pressedObject, 'OnPointerClick') ?? pressedObject;
    data.pointerClick = data.pointerPress;
    data.rawPointerPress = pressedObject;
    data.displayIndex = Number(event.displayIndex) || 0;
    data.pressure = Number(event.pressure) || 0;
    data.tangentialPressure = Number(event.tangentialPressure) || 0;
    data.altitudeAngle = Number(event.altitudeAngle) || 0;
    data.azimuthAngle = Number(event.azimuthAngle) || 0;
    data.radius = new Vector2(Math.max(0, Number(event.width) || 0) / 2,
      Math.max(0, Number(event.height) || 0) / 2);
    data.tilt = new Vector2(Number(event.tiltX) || 0, Number(event.tiltY) || 0);
    data.twist = Number(event.twist) || 0;
    data.penStatus = event.pointerType === 'pen'
      ? (event.pressure > 0 ? PenStatus.Contact : 0)
        | (event.button === 2 ? PenStatus.Barrel : 0)
        | (event.button === 5 ? PenStatus.Eraser : 0) : PenStatus.None;
    const rect = this.host.getBoundingClientRect?.();
    data.position = new Vector2((event.clientX ?? 0) - (rect?.left ?? 0),
      (rect?.height ?? this.host.clientHeight ?? 0) - ((event.clientY ?? 0) - (rect?.top ?? 0)));
    data.clickTime = Math.max(0, Number(event.timeStamp) || 0) / 1000;
    data._eventTime = data.clickTime;
    let canvas = object;
    while (canvas && !canvas._components.some(component => component.constructor.name === 'Canvas'))
      canvas = canvas.parent;
    const raycaster = canvas?._components.find(component => component instanceof GraphicRaycaster
      && component.isActiveAndEnabled) ?? null;
    data.pointerCurrentRaycast = new RaycastResult({ gameObject: object,
      module: raycaster, screenPosition: data.position.clone() });
    return data;
  }
  _pointerHit(event, capturedElement, capturedObject) {
    const rect = this.host.getBoundingClientRect?.();
    const point = new Vector2((event.clientX ?? 0) - (rect?.left ?? 0),
      (rect?.height ?? this.host.clientHeight ?? 0)
        - ((event.clientY ?? 0) - (rect?.top ?? 0)));
    const node = this.host.ownerDocument.elementFromPoint?.(event.clientX, event.clientY);
    if (node) {
      for (let current = node; current; current = current.parentNode ?? current.parent)
        if (current._uiObject) return raycastAllowed(current._uiObject)
          && customRaycastAllowed(current._uiObject, point)
          ? current._uiObject : null;
      return null;
    }
    return inside(capturedElement, event) && customRaycastAllowed(capturedObject, point)
      ? capturedObject : null;
  }
  _dispatch(object, method, data) {
    if (!object) return;
    this._dispatchDirect(this._handlerTarget(object, method), method, data);
  }
  _dispatchDirect(object, method, data) {
    if (!object) return;
    for (const component of object._components)
      if (component.isActiveAndEnabled) component[method]?.(data);
  }
  _handlerTarget(object, method) {
    for (let current = object; current; current = current.parent)
      if (current._components.some(component => component.isActiveAndEnabled
        && typeof component[method] === 'function')) return current;
    return null;
  }
  _parentCanvas(object) {
    for (let parent = object.parent; parent; parent = parent.parent)
      if (parent.activeInHierarchy && parent._components.some(component =>
        component.constructor.name === 'Canvas' && component.enabled)) return true;
    return false;
  }
  _canvasScale(object, scaler, width, height) {
    if (!scaler || !scaler.isActiveAndEnabled) return 1;
    if (scaler.uiScaleMode === 0) return Math.max(0.01, scaler.scaleFactor || 1);
    if (scaler.uiScaleMode === 1 && width > 0 && height > 0) {
      const reference = scaler.referenceResolution;
      if (!(reference?.x > 0 && reference?.y > 0)) return 1;
      const x = width / reference.x, y = height / reference.y;
      if (scaler.screenMatchMode === 1) return Math.min(x, y);
      if (scaler.screenMatchMode === 2) return this.fitReferenceResolution
        ? Math.min(x, y) : Math.max(x, y);
      const match = Math.max(0, Math.min(1, scaler.matchWidthOrHeight));
      const authored = Math.exp(Math.log(x) * (1 - match) + Math.log(y) * match);
      return this.fitReferenceResolution ? Math.min(authored, x, y) : authored;
    }
    this._unsupported(object, `CanvasScaler uiScaleMode ${scaler.uiScaleMode}`);
    return 1;
  }
  _unsupportedCanvas(object, canvas) {
    const mode = { 1: 'ScreenSpaceCamera', 2: 'WorldSpace' }[canvas.renderMode]
      ?? String(canvas.renderMode);
    this._unsupported(object, `Canvas ${mode} outside browser screen-overlay boundary`);
  }
  _unsupported(object, feature) {
    const message = `Unsupported ${feature} on ${object.name} (${object.assetId ?? 'runtime object'})`;
    const previous = this.diagnostics.get(object);
    if (!previous?.includes(message))
      this.diagnostics.set(object, previous ? `${previous}\n${message}` : message);
    if (this._reportedDiagnostics.has(message)) return;
    this._reportedDiagnostics.add(message);
    globalThis.console?.warn?.(message);
  }
  _legacyFontFamily(text, object) {
    const fontUrl = text?.font?.url;
    if (!fontUrl) return '';
    if (!this._fontFaces.has(fontUrl)) {
      const FontFaceClass = this.host.ownerDocument.defaultView?.FontFace;
      if (!FontFaceClass || !this.host.ownerDocument.fonts?.add) {
        this._unsupported(object, 'uGUI Text font registration unavailable');
        return '';
      }
      const family = `PlyUI_${String(text.font.assetId ?? text.font.name)
        .replace(/\W/g, '_')}`;
      const face = new FontFaceClass(family, `url(${JSON.stringify(fontUrl)})`);
      this.host.ownerDocument.fonts.add(face);
      face.load().catch(() => this._unsupported(object, 'uGUI Text font load failed'));
      this._fontFaces.set(fontUrl, family);
    }
    return `"${this._fontFaces.get(fontUrl)}", Arial, sans-serif`;
  }
  _control(element, selectable, receivesPointer) {
    const kind = selectable?.constructor.name;
    if (!kind) {
      if (element._unityControl?._unityComponent) element._unityControl._unityComponent._browserInput = null;
      element._unityControl?.remove();
      element._unityControl = null;
      return;
    }
    if (kind === 'Button') {
      element._unityControl?.remove();
      element._unityControl = null;
      return;
    }
    let input = element._unityControl;
    if (!input || input._unityKind !== kind || (kind === 'InputField'
      && input._unityMultiline !== (selectable.lineType !== 0))) {
      if (input?._unityComponent) input._unityComponent._browserInput = null;
      input?.remove();
      input = this.host.ownerDocument.createElement(kind === 'Dropdown' ? 'select'
        : kind === 'InputField' && selectable.lineType !== 0 ? 'textarea' : 'input');
      input._unityKind = kind;
      input._unityMultiline = kind === 'InputField' && selectable.lineType !== 0;
      input.addEventListener('input', event => {
        if (kind === 'Dropdown') return;
        const control = element._unityControl?._unityComponent;
        if (!control?.IsInteractable()) return;
        event.stopPropagation();
        if (kind === 'Toggle') control.isOn = Boolean(input.checked);
        else if (kind === 'InputField') {
          if (event.isComposing || input._unityComposing) return;
          const position = input.selectionStart ?? input.value.length;
          control.text = input.value;
          control.caretPosition = Math.min(position, control.text.length);
          if (input.value !== control.text) input.value = control.text;
        }
        else control.value = Number(input.value);
      });
      if (kind === 'Dropdown') input.addEventListener('change', event => {
        const control = element._unityControl?._unityComponent;
        if (!control?.IsInteractable()) return;
        event.stopPropagation();
        control.value = Number(input.value);
      });
      if (kind === 'InputField') {
        input.addEventListener('focus', () => {
          const control = input._unityComponent;
          control.isFocused = true;
          control.wasCanceled = false;
          control._originalText = control.text;
        });
        input.addEventListener('blur', () => {
          const control = input._unityComponent;
          if (!control.isFocused) return;
          control.isFocused = false;
          if (control.wasCanceled) control.text = control._originalText;
          control.onEndEdit.Invoke(control.text);
          control.caretPosition = 0;
        });
        input.addEventListener('keydown', event => {
          const control = input._unityComponent;
          if (event.isComposing || !control.IsInteractable()) return;
          if (event.key === 'Escape') {
            control.wasCanceled = true;
            event.preventDefault?.();
            control.DeactivateInputField();
            return;
          }
          if (event.key !== 'Enter') return;
          if (control.lineType === 2) return;
          event.preventDefault?.();
          event.stopPropagation?.();
          control.onSubmit.Invoke(control.text);
          control.DeactivateInputField();
        });
        input.addEventListener('select', () => {
          const control = input._unityComponent;
          const start = input.selectionStart ?? 0;
          const end = input.selectionEnd ?? start;
          control._selectionAnchor = input.selectionDirection === 'backward' ? end : start;
          control._selectionFocus = input.selectionDirection === 'backward' ? start : end;
        });
        input.addEventListener('compositionstart', () => { input._unityComposing = true; });
        input.addEventListener('compositionend', () => {
          input._unityComposing = false;
          const control = input._unityComponent;
          if (control.IsInteractable()) {
            const position = input.selectionStart ?? input.value.length;
            control.text = input.value;
            control.caretPosition = Math.min(position, control.text.length);
          }
        });
      }
      input.addEventListener('click', event => event.stopPropagation());
      element.appendChild(input);
      element._unityControl = input;
    }
    input._unityComponent = selectable;
    if (kind === 'InputField') {
      selectable._browserInput = input;
      if (input.tagName?.toLowerCase() === 'input' || !input.tagName)
        input.type = selectable.inputType === 2 ? 'password' : 'text';
      input.inputMode = selectable.keyboardType === 4 || selectable.characterValidation === 1
        ? 'numeric' : selectable.keyboardType === 11 || selectable.characterValidation === 2
          ? 'decimal' : selectable.keyboardType === 7 || selectable.characterValidation === 5
            ? 'email' : selectable.keyboardType === 3 ? 'url'
              : selectable.keyboardType === 5 ? 'tel'
                : selectable.keyboardType === 10 ? 'search' : 'text';
      if (selectable.inputType === 2 && selectable.asteriskChar !== '*')
        this._unsupported(selectable.gameObject, 'InputField custom password mask glyph');
      if (selectable.caretWidth !== 1 || selectable.caretBlinkRate !== 0.85)
        this._unsupported(selectable.gameObject, 'InputField custom caret width/blink');
      const selection = selectable.selectionColor;
      if (selection && (Math.abs(selection.r - 168 / 255) > 0.001
        || Math.abs(selection.g - 206 / 255) > 0.001
        || Math.abs(selection.b - 1) > 0.001
        || Math.abs(selection.a - 192 / 255) > 0.001))
        this._unsupported(selectable.gameObject, 'InputField custom selection highlight');
      if (selectable.shouldHideMobileInput)
        this._unsupported(selectable.gameObject, 'InputField native mobile keyboard suppression');
      if (input.value !== selectable.text && !input._unityComposing)
        input.value = selectable.text;
      input.readOnly = selectable.readOnly;
      if (selectable.characterLimit > 0) input.maxLength = selectable.characterLimit;
      else input.removeAttribute?.('maxlength');
      input.placeholder = selectable.placeholderText || selectable.placeholder?.text
        || selectable.placeholder?.gameObject?._components
          ?.find(component => component.constructor.name === 'Text')?.text || '';
      input.style.width = '100%';
      input.style.height = '100%';
      input.style.boxSizing = 'border-box';
      input.style.background = 'transparent';
      input.style.border = '0';
      input.style.outline = 'none';
      input.style.padding = '0';
      input.style.resize = 'none';
      const label = selectable.textComponent;
      input.style.fontSize = label ? `${label.fontSize}px` : '';
      input.style.fontFamily = this._legacyFontFamily(label, selectable.gameObject);
      input.style.fontWeight = label?.fontStyle === 1 || label?.fontStyle === 3 ? 'bold' : '';
      input.style.fontStyle = label?.fontStyle === 2 || label?.fontStyle === 3
        ? 'italic' : '';
      input.style.color = label ? color(label.color) : '';
      input.style.textAlign = label ? ['left', 'center', 'right'][label.alignment % 3] : '';
      input.style.caretColor = color(selectable.customCaretColor
        ? selectable.caretColor : selectable.textComponent?.color ?? selectable.caretColor);
      input.style.pointerEvents = receivesPointer ? 'auto' : 'none';
      input.disabled = !selectable.IsInteractable();
      if (selectable._selected && selectable.shouldActivateOnSelect
        && !selectable.isFocused && selectable.IsInteractable()) input.focus?.();
      return;
    }
    if (kind === 'Dropdown') {
      selectable._browserInput = input;
      const optionsKey = JSON.stringify(selectable.options.map(option => option.text));
      if (input._unityOptionsKey !== optionsKey) {
        for (const option of [...input.children]) option.remove();
        selectable.options.forEach((option, index) => {
          const item = this.host.ownerDocument.createElement('option');
          item.value = String(index);
          item.textContent = option.text;
          input.appendChild(item);
        });
        input._unityOptionsKey = optionsKey;
      }
      input.value = String(selectable.value);
      input.disabled = !selectable.IsInteractable();
      input.style.width = '100%';
      input.style.height = '100%';
      input.style.boxSizing = 'border-box';
      input.style.pointerEvents = receivesPointer ? 'auto' : 'none';
      const authoredCaption = Boolean(selectable.captionText || selectable.captionImage);
      input.style.position = authoredCaption ? 'absolute' : '';
      input.style.inset = authoredCaption ? '0' : '';
      input.style.zIndex = authoredCaption ? '1' : '';
      input.style.opacity = authoredCaption ? '0' : '';
      return;
    }
    input.type = kind === 'Toggle' ? 'checkbox' : 'range';
    input.disabled = !selectable.IsInteractable();
    input.style.pointerEvents = receivesPointer ? 'auto' : 'none';
    const authoredGraphic = Boolean(selectable.targetGraphic || selectable.graphic
      || selectable.fillRect || selectable.handleRect);
    input.style.opacity = authoredGraphic ? '0' : '';
    input.style.position = authoredGraphic ? 'absolute' : '';
    input.style.inset = authoredGraphic ? '0' : '';
    input.style.zIndex = authoredGraphic ? '1' : '';
    input.style.margin = authoredGraphic ? '0' : '';
    input.style.height = authoredGraphic ? '100%' : '';
    if (kind === 'Toggle') input.checked = selectable.isOn;
    else {
      input.style.width = '100%';
      input.style.boxSizing = 'border-box';
      input.min = kind === 'Slider' ? String(selectable.minValue) : '0';
      input.max = kind === 'Slider' ? String(selectable.maxValue) : '1';
      input.step = kind === 'Slider' && selectable.wholeNumbers ? '1' : 'any';
      input.value = String(selectable.value);
    }
  }
}

function syncUiOrder(parent, desired) {
  const current = Array.from(parent.children ?? []).filter(child => child._uiObject);
  if (current.length === desired.length && current.every((child, index) => child === desired[index])) return;
  for (const child of desired) parent.appendChild(child);
}

function color(value) {
  const channel = number => Math.round(Math.max(0, Math.min(1, number ?? 1)) * 255);
  return `rgba(${channel(value.r)}, ${channel(value.g)}, ${channel(value.b)}, ${value.a ?? 1})`;
}

function selectableForGraphic(object, graphic) {
  if (!graphic) return null;
  for (let current = object; current; current = current.parent) {
    const selectable = current._components.find(component => component.isActiveAndEnabled
      && component.targetGraphic === graphic);
    if (selectable) return selectable;
  }
  return null;
}

function spritePixelScale(object, sprite, image) {
  let referencePixelsPerUnit = 100;
  for (let current = object; current; current = current.parent) {
    const scaler = current._components.find(component =>
      component.constructor.name === 'CanvasScaler' && component.enabled);
    if (scaler) { referencePixelsPerUnit = scaler.referencePixelsPerUnit; break; }
  }
  return referencePixelsPerUnit / (sprite.pixelsPerUnit
    * Math.max(0.01, image.pixelsPerUnitMultiplier));
}

function groupAlpha(object) {
  return object._components.reduce((alpha, component) =>
    component.constructor.name === 'CanvasGroup' && component.enabled
      ? alpha * Math.max(0, Math.min(1, component.alpha)) : alpha, 1);
}

function raycastAllowed(object) {
  const graphics = object._components.filter(component =>
    ['Image', 'RawImage', 'Text', 'TextMeshProUGUI'].includes(component.constructor.name)
      && component.isActiveAndEnabled);
  if (graphics.length && !graphics.some(component => component.raycastTarget)) return false;
  for (let current = object; current; current = current.parent) {
    const groups = current._components.filter(component =>
      component.constructor.name === 'CanvasGroup' && component.enabled);
    if (groups.some(group => !group.blocksRaycasts)) return false;
    if (groups.some(group => group.ignoreParentGroups)) break;
  }
  return true;
}

function customRaycastAllowed(object, screenPoint) {
  for (let current = object; current; current = current.parent) {
    for (const component of current._components)
      if (component.isActiveAndEnabled
        && component.constructor.name !== 'CanvasGroup'
        && typeof component.IsRaycastLocationValid === 'function'
        && !component.IsRaycastLocationValid(screenPoint, null)) return false;
  }
  return true;
}

function hasUnmaskableGraphic(object) {
  return object._components.some(component => component.isActiveAndEnabled
    && ['Image', 'RawImage', 'Text', 'TextMeshProUGUI'].includes(component.constructor.name)
    && component.maskable === false);
}

function pointInRect(rect, x, y) {
  return !!rect && x >= rect.left && x <= rect.right
    && y >= rect.top && y <= rect.bottom;
}

function pointInPaddedRect(rect, padding, x, y) {
  return x >= rect.left + (padding?.x ?? 0)
    && x <= rect.right - (padding?.z ?? 0)
    && y >= rect.top + (padding?.w ?? 0)
    && y <= rect.bottom - (padding?.y ?? 0);
}

function paddedMasksAllow(object, event, elements) {
  if (hasUnmaskableGraphic(object)) return true;
  for (let current = object; current; current = current.parent) {
    const mask = current._components.find(component =>
      component.constructor.name === 'RectMask2D' && component.isActiveAndEnabled);
    if (!mask) continue;
    if (![mask.padding.x, mask.padding.y, mask.padding.z, mask.padding.w]
      .some(value => value !== 0)) continue;
    const rect = elements.get(current)?.getBoundingClientRect?.();
    if (rect && !pointInPaddedRect(rect, mask.padding, event.clientX, event.clientY))
      return false;
  }
  return true;
}

function scrollViewport(object) {
  for (let current = object; current; current = current.parent) {
    const scroll = current._components.find(component =>
      component.constructor.name === 'ScrollRect' && component.isActiveAndEnabled);
    if (scroll && (scroll.viewport?.gameObject ?? current) === object) return scroll;
  }
  return null;
}

function resizeCanvasRect(root, width, height) {
  const anchored = [];
  const snapshot = object => {
    for (const child of object.children) {
      if (child.transform?.anchoredPosition && object.transform?.rect)
        anchored.push([child.transform, child.transform.anchoredPosition]);
      snapshot(child);
    }
  };
  snapshot(root);
  root.transform.sizeDelta = { x: width, y: height };
  for (const [rect, position] of anchored) rect.anchoredPosition = position;
}

function inside(element, event) {
  const rect = element.getBoundingClientRect?.();
  if (!rect || !Number.isFinite(event.clientX) || !Number.isFinite(event.clientY)) return true;
  return event.clientX >= rect.left && event.clientX <= rect.right
    && event.clientY >= rect.top && event.clientY <= rect.bottom;
}
