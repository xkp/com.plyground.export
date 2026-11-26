import { Vector2 } from './math.js';

/** Rebuild the supported uGUI layout controllers before the Canvas display pass. */
export function rebuildCanvasLayout(root) {
  return computeCanvasLayout(root, true);
}

/** Read the same intrinsic measurements without changing RectTransforms. */
export function measureCanvasLayout(root) {
  return computeCanvasLayout(root, false);
}

function computeCanvasLayout(root, arrangeRects) {
  const measured = new Map();
  const measure = object => {
    for (const child of object.children) if (child.activeInHierarchy) measure(child);
    const group = layoutGroup(object);
    const children = layoutChildren(object);
    const padding = group?.padding ?? { left: 0, right: 0, top: 0, bottom: 0 };
    let width = object.transform?.rect?.width ?? 0;
    let height = object.transform?.rect?.height ?? 0;
    let minWidth = width, minHeight = height;
    let flexWidth = 0, flexHeight = 0;
    if (group?.constructor.name === 'GridLayoutGroup') {
      const columns = group.constraint === 1 ? Math.max(1, group.constraintCount)
        : group.constraint === 2 ? Math.max(1, Math.ceil(children.length / Math.max(1, group.constraintCount)))
          : Math.max(1, Math.ceil(Math.sqrt(children.length)));
      const rows = Math.ceil(children.length / columns);
      width = padding.left + padding.right + columns * group.cellSize.x
        + Math.max(0, columns - 1) * group.spacing.x;
      height = padding.top + padding.bottom + rows * group.cellSize.y
        + Math.max(0, rows - 1) * group.spacing.y;
      minWidth = width; minHeight = height;
    } else if (group && children.length) {
      const vertical = group.constructor.name === 'VerticalLayoutGroup';
      const mainAxis = vertical ? 'height' : 'width';
      const crossAxis = vertical ? 'width' : 'height';
      const spacing = Math.max(0, children.length - 1) * group.spacing;
      const mainMin = children.reduce((sum, child) => sum + childSize(child,
        mainAxis, 'min', group, measured) * childScale(child, mainAxis, group), 0) + spacing;
      const mainPreferred = children.reduce((sum, child) => sum + childSize(child,
        mainAxis, 'preferred', group, measured) * childScale(child, mainAxis, group), 0) + spacing;
      const crossMin = Math.max(...children.map(child => childSize(child,
        crossAxis, 'min', group, measured) * childScale(child, crossAxis, group)));
      const crossPreferred = Math.max(...children.map(child => childSize(child,
        crossAxis, 'preferred', group, measured) * childScale(child, crossAxis, group)));
      minWidth = (vertical ? crossMin : mainMin) + padding.left + padding.right;
      minHeight = (vertical ? mainMin : crossMin) + padding.top + padding.bottom;
      width = Math.max(minWidth, (vertical ? crossPreferred : mainPreferred)
        + padding.left + padding.right);
      height = Math.max(minHeight, (vertical ? mainPreferred : crossPreferred)
        + padding.top + padding.bottom);
      const mainFlex = children.reduce((sum, child) => sum + childFlex(child,
        mainAxis, group, measured) * childScale(child, mainAxis, group), 0);
      const crossFlex = Math.max(...children.map(child => childFlex(child,
        crossAxis, group, measured) * childScale(child, crossAxis, group)));
      flexWidth = vertical ? crossFlex : mainFlex;
      flexHeight = vertical ? mainFlex : crossFlex;
    } else if (group) {
      minWidth = width = padding.left + padding.right;
      minHeight = height = padding.top + padding.bottom;
    }
    measured.set(object, { minWidth, minHeight, width, height, flexWidth, flexHeight });
    const fitter = object._components.find(component => component.constructor.name === 'ContentSizeFitter'
      && component.isActiveAndEnabled);
    if (arrangeRects && fitter && object.transform?.sizeDelta) {
      if (fitter.horizontalFit) {
        const target = fitter.horizontalFit === 1 ? minWidth : width;
        if (object.transform.rect.width !== target)
          object.transform.SetSizeWithCurrentAnchors(0, target);
      }
      if (fitter.verticalFit) {
        const target = fitter.verticalFit === 1 ? minHeight : height;
        if (object.transform.rect.height !== target)
          object.transform.SetSizeWithCurrentAnchors(1, target);
      }
    }
  };
  const arrange = object => {
    object._components.find(component => component.constructor.name === 'AspectRatioFitter'
      && component.isActiveAndEnabled)?._updateAspect();
    const group = layoutGroup(object);
    if (group?.constructor.name === 'GridLayoutGroup') arrangeGrid(object, group);
    else if (group) arrangeLinear(object, group, measured);
    for (const child of object.children) if (child.activeInHierarchy) arrange(child);
  };
  measure(root);
  if (arrangeRects) arrange(root);
  return measured;
}

function layoutGroup(object) {
  return object._components.find(component => component.isActiveAndEnabled
    && ['GridLayoutGroup', 'VerticalLayoutGroup', 'HorizontalLayoutGroup']
      .includes(component.constructor.name));
}

function layoutChildren(object) {
  return object.children.filter(child => child.activeInHierarchy && child.transform?.sizeDelta
    && !child._components.some(component => component.constructor.name === 'LayoutElement'
      && component.isActiveAndEnabled && component.ignoreLayout));
}

function childSize(object, axis, kind, group, measured) {
  const controlled = axis === 'width' ? group.childControlWidth : group.childControlHeight;
  if (controlled === false) return object.transform.sizeDelta[axis === 'width' ? 'x' : 'y'];
  const element = object._components.find(component => component.constructor.name === 'LayoutElement'
    && component.isActiveAndEnabled);
  const min = axis === 'width' ? element?.minWidth : element?.minHeight;
  const preferred = axis === 'width' ? element?.preferredWidth : element?.preferredHeight;
  const intrinsic = layoutGroup(object) ? measured.get(object) : null;
  const minimum = min >= 0 ? min : element && preferred >= 0 ? 0
    : intrinsic?.[axis === 'width' ? 'minWidth' : 'minHeight']
    ?? (element ? 0 : object.transform.rect?.[axis] ?? 0);
  if (kind === 'min') return minimum;
  return Math.max(minimum, preferred >= 0 ? preferred
    : intrinsic?.[axis] ?? (element ? 0 : object.transform.rect?.[axis] ?? 0));
}

function childFlex(object, axis, group, measured) {
  const controlled = axis === 'width' ? group.childControlWidth : group.childControlHeight;
  if (controlled === false) return 0;
  const element = object._components.find(component => component.constructor.name === 'LayoutElement'
    && component.isActiveAndEnabled);
  const value = axis === 'width' ? element?.flexibleWidth : element?.flexibleHeight;
  const intrinsic = layoutGroup(object) ? measured.get(object) : null;
  const flexible = value >= 0 ? value : intrinsic?.[axis === 'width' ? 'flexWidth' : 'flexHeight'] ?? 0;
  const expand = axis === 'width' ? group.childForceExpandWidth : group.childForceExpandHeight;
  return Math.max(expand ? 1 : 0, flexible);
}

function childScale(object, axis, group) {
  const useScale = axis === 'width' ? group.childScaleWidth : group.childScaleHeight;
  return useScale ? object.transform.localScale[axis === 'width' ? 'x' : 'y'] : 1;
}

function setChildRect(child, x, y, width, height, scaleX = 1, scaleY = 1) {
  const rect = child.transform;
  if (rect.anchorMin.x !== 0 || rect.anchorMin.y !== 1)
    rect.anchorMin = new Vector2(0, 1);
  if (rect.anchorMax.x !== 0 || rect.anchorMax.y !== 1)
    rect.anchorMax = new Vector2(0, 1);
  if (rect.sizeDelta.x !== width || rect.sizeDelta.y !== height)
    rect.sizeDelta = new Vector2(width, height);
  const anchoredX = x + width * rect.pivot.x * scaleX;
  const anchoredY = -y - height * (1 - rect.pivot.y) * scaleY;
  if (rect.anchoredPosition.x !== anchoredX || rect.anchoredPosition.y !== anchoredY)
    rect.anchoredPosition = new Vector2(anchoredX, anchoredY);
}

function startOffset(group, axis, required, available) {
  const padding = group.padding;
  const leading = axis === 0 ? padding.left : padding.top;
  const trailing = axis === 0 ? padding.right : padding.bottom;
  const alignment = axis === 0 ? group.childAlignment % 3 : Math.floor(group.childAlignment / 3);
  return leading + (available - leading - trailing - required) * alignment * 0.5;
}

function arrangeGrid(object, group) {
  const children = layoutChildren(object);
  if (!children.length) return;
  const width = object.transform.rect.width;
  const height = object.transform.rect.height;
  const count = children.length;
  const columns = group.constraint === 1 ? Math.max(1, group.constraintCount)
    : group.constraint === 2 ? Math.max(1, Math.ceil(count / Math.max(1, group.constraintCount)))
      : group.cellSize.x + group.spacing.x <= 0 ? count
        : Math.max(1, Math.floor((width - group.padding.left - group.padding.right
          + group.spacing.x + 0.001) / (group.cellSize.x + group.spacing.x)));
  const rows = group.constraint === 2 ? Math.max(1, group.constraintCount)
    : Math.max(1, Math.ceil(count / columns));
  const actualColumns = Math.min(columns, count);
  const actualRows = Math.min(rows, Math.ceil(count / (group.startAxis === 0 ? columns : rows)));
  const requiredWidth = actualColumns * group.cellSize.x
    + Math.max(0, actualColumns - 1) * group.spacing.x;
  const requiredHeight = actualRows * group.cellSize.y
    + Math.max(0, actualRows - 1) * group.spacing.y;
  const left = startOffset(group, 0, requiredWidth, width);
  const top = startOffset(group, 1, requiredHeight, height);
  children.forEach((child, index) => {
    let x = group.startAxis === 0 ? index % columns : Math.floor(index / rows);
    let y = group.startAxis === 0 ? Math.floor(index / columns) : index % rows;
    if (group.startCorner % 2) x = actualColumns - 1 - x;
    if (Math.floor(group.startCorner / 2)) y = actualRows - 1 - y;
    setChildRect(child, left + x * (group.cellSize.x + group.spacing.x),
      top + y * (group.cellSize.y + group.spacing.y),
      group.cellSize.x, group.cellSize.y);
  });
}

function arrangeLinear(object, group, measured) {
  const children = layoutChildren(object);
  if (!children.length) return;
  if (group.reverseArrangement) children.reverse();
  const vertical = group.constructor.name === 'VerticalLayoutGroup';
  const mainAxis = vertical ? 'height' : 'width';
  const crossAxis = vertical ? 'width' : 'height';
  const padding = group.padding;
  const mainSize = vertical ? object.transform.rect.height : object.transform.rect.width;
  const crossSize = vertical ? object.transform.rect.width : object.transform.rect.height;
  const mainPadding = vertical ? padding.top + padding.bottom : padding.left + padding.right;
  const crossPadding = vertical ? padding.left + padding.right : padding.top + padding.bottom;
  const availableMain = mainSize - mainPadding;
  const availableCross = crossSize - crossPadding;
  const spacing = group.spacing * Math.max(0, children.length - 1);
  const minimums = children.map(child => childSize(child, mainAxis, 'min', group, measured));
  const preferreds = children.map(child => childSize(child, mainAxis, 'preferred', group, measured));
  const flexible = children.map(child => childFlex(child, mainAxis, group, measured));
  const scales = children.map(child => childScale(child, mainAxis, group));
  const totalMin = minimums.reduce((sum, size, index) => sum + size * scales[index], 0) + spacing;
  const totalPreferred = preferreds.reduce((sum, size, index) => sum + size * scales[index], 0) + spacing;
  const totalFlexible = flexible.reduce((sum, size, index) => sum + size * scales[index], 0);
  const lerp = totalPreferred === totalMin ? 0
    : Math.max(0, Math.min(1, (availableMain - totalMin) / (totalPreferred - totalMin)));
  const extraPerFlex = totalFlexible > 0
    ? Math.max(0, availableMain - totalPreferred) / totalFlexible : 0;
  const sizes = children.map((_, index) => minimums[index]
    + (preferreds[index] - minimums[index]) * lerp
    + flexible[index] * extraPerFlex);
  let cursor = vertical ? padding.top : padding.left;
  if (availableMain > totalPreferred && totalFlexible === 0)
    cursor = startOffset(group, vertical ? 1 : 0, totalPreferred, mainSize);
  children.forEach((child, index) => {
    const crossMin = childSize(child, crossAxis, 'min', group, measured);
    const crossPreferred = childSize(child, crossAxis, 'preferred', group, measured);
    const crossFlexible = childFlex(child, crossAxis, group, measured);
    const allocatedCross = Math.max(crossMin, Math.min(availableCross,
      crossFlexible > 0 ? crossSize : crossPreferred));
    const cross = startOffset(group, vertical ? 0 : 1,
      allocatedCross * childScale(child, crossAxis, group), crossSize);
    const controlledMain = vertical ? group.childControlHeight : group.childControlWidth;
    const controlledCross = vertical ? group.childControlWidth : group.childControlHeight;
    const actualMain = controlledMain ? sizes[index]
      : child.transform.sizeDelta[vertical ? 'y' : 'x'];
    const actualCross = controlledCross ? allocatedCross
      : child.transform.sizeDelta[vertical ? 'x' : 'y'];
    const mainAlign = vertical ? Math.floor(group.childAlignment / 3) * 0.5
      : (group.childAlignment % 3) * 0.5;
    const crossAlign = vertical ? (group.childAlignment % 3) * 0.5
      : Math.floor(group.childAlignment / 3) * 0.5;
    const main = cursor + (sizes[index] - actualMain) * mainAlign;
    const alignedCross = cross + (allocatedCross - actualCross) * crossAlign;
    setChildRect(child, vertical ? alignedCross : main, vertical ? main : alignedCross,
      vertical ? actualCross : actualMain, vertical ? actualMain : actualCross,
      childScale(child, 'width', group), childScale(child, 'height', group));
    cursor += sizes[index] * scales[index] + group.spacing;
  });
}
