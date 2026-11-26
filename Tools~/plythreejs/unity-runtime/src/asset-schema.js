import { excludedPreviewNamespace, excludedPreviewNamespaces,
  graylistedPreviewNamespace, graylistedPreviewNamespaces } from './preview-exclusions.js';

export const SOURCE_FORMAT = 'unity-bridge-source';
export const BUNDLE_FORMAT = 'unity-three-bundle';
export const BUNDLE_VERSION = 1;

export class AssetFormatError extends Error {
  constructor(message) { super(message); this.name = 'AssetFormatError'; }
}

function requireValue(condition, message) {
  if (!condition) throw new AssetFormatError(message);
}

function validateRenderSettings(settings) {
  requireValue(settings && typeof settings === 'object'
    && typeof settings.fog === 'boolean'
    && [1, 2, 3].includes(settings.fogMode)
    && Array.isArray(settings.fogColor) && settings.fogColor.length === 4
    && settings.fogColor.every(Number.isFinite)
    && Number.isFinite(settings.fogDensity) && settings.fogDensity >= 0
    && Number.isFinite(settings.fogStartDistance)
    && Number.isFinite(settings.fogEndDistance),
  'Manifest renderSettings fog data is invalid');
  requireValue(!settings.fog || settings.fogMode !== 2,
    'Preview defers Unity exponential fog: one Three.js fog path cannot match it');
}

function assertPreviewTypeAllowed(type, location) {
  const prefix = excludedPreviewNamespace(type);
  if (prefix) throw new AssetFormatError(`Preview excludes ${prefix} at ${location}: ${type}`);
  const graylisted = graylistedPreviewNamespace(type);
  if (graylisted) throw new AssetFormatError(
    `Preview graylists ${graylisted} at ${location}: implementation required for ${type}`);
}

function validateUxmlTree(node, location, depth = 0) {
  requireValue(depth <= 32 && node && typeof node.type === 'string'
    && Array.isArray(node.children ?? []), `Invalid UXML node at ${location}`);
  const attributes = node.attributes ?? {};
  requireValue(attributes && typeof attributes === 'object' && !Array.isArray(attributes),
    `Invalid UXML attributes at ${location}`);
  if (node.type === 'Template') {
    requireValue(typeof attributes.name === 'string' && attributes.name.length > 0
      && node.templateTree, `Template needs name and source at ${location}`);
    validateUxmlTree(node.templateTree, `${location}/Template:${attributes.name}`, depth + 1);
  }
  if (node.type === 'Instance') requireValue(typeof attributes.template === 'string'
    && attributes.template.length > 0, `Instance needs template at ${location}`);
  if (node.type === 'AttributeOverrides') requireValue(typeof attributes['element-name'] === 'string'
    && attributes['element-name'].length > 0, `AttributeOverrides needs element-name at ${location}`);
  for (const child of node.children ?? [])
    validateUxmlTree(child, `${location}/${child?.type ?? '?'}`, depth + 1);
}

export function validateManifest(manifest, format) {
  requireValue(manifest && typeof manifest === 'object', 'Manifest must be an object');
  requireValue(manifest.format === format, `Expected format ${format}`);
  requireValue(manifest.version === BUNDLE_VERSION, `Unsupported manifest version: ${manifest.version}`);
  requireValue(manifest.scene && typeof manifest.scene.name === 'string', 'Scene needs a name');
  if (manifest.scene.path !== undefined)
    requireValue(typeof manifest.scene.path === 'string', 'Scene path must be a string');
  if (manifest.scene.buildIndex !== undefined)
    requireValue(Number.isInteger(manifest.scene.buildIndex) && manifest.scene.buildIndex >= -1,
      'Scene build index must be an integer at least -1');
  if (manifest.scene.sceneCountInBuildSettings !== undefined)
    requireValue(Number.isInteger(manifest.scene.sceneCountInBuildSettings)
      && manifest.scene.sceneCountInBuildSettings >= 0,
    'Scene count in build settings must be a nonnegative integer');
  requireValue(Array.isArray(manifest.scene.roots), 'Scene needs a roots array');
  if (manifest.scene.renderSettings !== undefined)
    validateRenderSettings(manifest.scene.renderSettings);
  if (manifest.scenes !== undefined) {
    requireValue(format === BUNDLE_FORMAT && Array.isArray(manifest.scenes),
      'Deferred scenes require a bundle scenes array');
    const names = new Set([manifest.scene.name]);
    const indices = new Set(manifest.scene.buildIndex >= 0 ? [manifest.scene.buildIndex] : []);
    for (const scene of manifest.scenes) {
      requireValue(scene && typeof scene.name === 'string' && scene.name.length > 0
        && !names.has(scene.name), 'Deferred scene needs a unique name');
      names.add(scene.name);
      requireValue(typeof scene.path === 'string' && Number.isInteger(scene.buildIndex)
        && scene.buildIndex >= -1, `Invalid deferred scene metadata: ${scene.name}`);
      if (scene.renderSettings !== undefined) validateRenderSettings(scene.renderSettings);
      if (scene.buildIndex >= 0) {
        requireValue(!indices.has(scene.buildIndex), `Duplicate scene build index: ${scene.buildIndex}`);
        indices.add(scene.buildIndex);
      }
      const recordUrl = scene.recordUrl;
      const published = typeof recordUrl === 'string' && /^https:\/\//.test(recordUrl);
      let validPublished = false;
      if (published) {
        try {
          const url = new URL(recordUrl);
          validPublished = !url.username && !url.password && !url.search && !url.hash
            && url.pathname.includes('/Preview/')
            && url.pathname.endsWith(`/scenes/${scene.sha256}.json`);
        } catch { /* Invalid URLs fail below. */ }
      }
      requireValue(typeof scene.sha256 === 'string' && /^[a-f0-9]{64}$/.test(scene.sha256)
        && (validPublished || typeof recordUrl === 'string'
          && /(?:^|\/)scenes\/[a-f0-9]{64}\.json$/.test(recordUrl)
          && !recordUrl.startsWith('/') && !/[\\:?#]/.test(recordUrl)
          && !recordUrl.split('/').includes('..')
          && recordUrl.endsWith(`${scene.sha256}.json`)),
      `Invalid deferred scene record: ${scene.name}`);
    }
  }
  requireValue(Array.isArray(manifest.assets), 'Manifest needs an assets array');
  for (const diagnostic of manifest.diagnostics ?? []) if (typeof diagnostic === 'string') {
    const excluded = excludedPreviewNamespaces.find(prefix => diagnostic.includes(prefix));
    if (excluded) throw new AssetFormatError(`Preview excludes ${excluded}: ${diagnostic}`);
    const graylisted = graylistedPreviewNamespaces.find(prefix => diagnostic.includes(prefix));
    if (graylisted) throw new AssetFormatError(
      `Preview graylists ${graylisted}: implementation required for ${diagnostic}`);
    if (/Unity(?:Engine)?\.[\w.]*Experimental/i.test(diagnostic))
      throw new AssetFormatError(`Preview excludes Experimental: ${diagnostic}`);
  }
  if (manifest.layers !== undefined) {
    requireValue(manifest.layers && typeof manifest.layers === 'object'
      && !Array.isArray(manifest.layers), 'Manifest layers must map names to indices');
    for (const [name, index] of globalThis.Object.entries(manifest.layers))
      requireValue(name.length > 0 && Number.isInteger(index) && index >= 0 && index < 32,
        `Invalid layer index for ${name}`);
  }
  if (manifest.renderingLayers !== undefined) {
    requireValue(manifest.renderingLayers && typeof manifest.renderingLayers === 'object'
      && !Array.isArray(manifest.renderingLayers),
    'Manifest renderingLayers must map names to indices');
    const indices = new Set();
    for (const [name, index] of globalThis.Object.entries(manifest.renderingLayers)) {
      requireValue(name.length > 0 && Number.isInteger(index) && index >= 0 && index < 32
        && !indices.has(index), `Invalid rendering layer index for ${name}`);
      indices.add(index);
    }
  }
  if (manifest.sortingLayers !== undefined) {
    requireValue(Array.isArray(manifest.sortingLayers)
      && manifest.sortingLayers.length > 0
      && manifest.sortingLayers.every(layer => layer
        && typeof layer.name === 'string' && layer.name.length > 0
        && Number.isInteger(layer.id) && Number.isInteger(layer.value))
      && manifest.sortingLayers.some(layer => layer.name === 'Default'
        && layer.id === 0 && layer.value === 0),
    'Manifest sortingLayers require a Default layer and name, ID, value records');
  }
  if (manifest.renderSettings !== undefined) {
    validateRenderSettings(manifest.renderSettings);
  }
  if (manifest.physicsSettings !== undefined)
    requireValue(Array.isArray(manifest.physicsSettings?.gravity)
      && manifest.physicsSettings.gravity.length === 3
      && manifest.physicsSettings.gravity.every(Number.isFinite),
    'Manifest physicsSettings gravity must be a finite Vector3');
  if (manifest.tags !== undefined) {
    requireValue(Array.isArray(manifest.tags), 'Manifest tags must be an array');
    requireValue(manifest.tags.every(tag => typeof tag === 'string' && tag.length > 0),
      'Manifest tags must be nonempty strings');
  }
  if (manifest.buildTags !== undefined) {
    requireValue(Array.isArray(manifest.buildTags)
      && manifest.buildTags.every(tag => typeof tag === 'string'),
    'Manifest buildTags must be strings');
  }
  if (manifest.applicationIdentity !== undefined) {
    const identity = manifest.applicationIdentity;
    requireValue(identity && typeof identity === 'object' && !Array.isArray(identity),
      'Application identity must be an object');
    for (const key of ['companyName', 'productName', 'identifier', 'version', 'unityVersion'])
      requireValue(typeof identity[key] === 'string',
        `Application identity ${key} must be a string`);
  }
  if (manifest.qualitySettings !== undefined) {
    const quality = manifest.qualitySettings;
    requireValue(quality && Array.isArray(quality.names) && quality.names.length > 0
      && quality.names.every(name => typeof name === 'string' && name.length > 0)
      && Number.isInteger(quality.level) && quality.level >= 0
      && quality.level < quality.names.length, 'Invalid quality settings snapshot');
  }
  if (manifest.inputBindings !== undefined) {
    const input = manifest.inputBindings;
    requireValue(input && typeof input === 'object' && !Array.isArray(input),
      'Manifest inputBindings must be an object');
    for (const key of ['buttons', 'axes'])
      requireValue(input[key] && typeof input[key] === 'object' && !Array.isArray(input[key]),
        `Manifest inputBindings.${key} must be an object`);
  }
  if (manifest.legacyAxes !== undefined)
    requireValue(Array.isArray(manifest.legacyAxes), 'Manifest legacyAxes must be an array');
  const ids = new Set();
  const assetIds = new Set();
  for (const asset of manifest.assets) {
    requireValue(typeof asset.id === 'string' && asset.id.length > 0, 'Asset needs an id');
    requireValue(!assetIds.has(asset.id), `Duplicate asset id: ${asset.id}`);
    assetIds.add(asset.id);
    if (typeof asset.type === 'string') assertPreviewTypeAllowed(asset.type, `asset ${asset.id}`);
    requireValue(['model', 'texture', 'audio', 'navMesh', 'terrainData', 'scriptableObject', 'inputActionAsset', 'prefab',
      'physicsMaterial', 'animationClip', 'animatorController', 'material', 'sprite',
      'uiPanelSettings', 'uiVisualTree', 'uiStyleSheet', 'uiFont', 'tmpFont'].includes(asset.kind),
      `Unsupported asset kind: ${asset.kind}`);
    if (asset.kind === 'uiPanelSettings') {
      requireValue(asset.data && typeof asset.data === 'object'
        && Number.isFinite(asset.data.sortingOrder)
        && Array.isArray(asset.data.referenceResolution)
        && asset.data.referenceResolution.length === 2
        && asset.data.referenceResolution.every(Number.isFinite)
        && (asset.data.fallbackDpi === undefined || asset.data.fallbackDpi > 0)
        && (asset.data.referenceDpi === undefined || asset.data.referenceDpi > 0)
        && (asset.data.colorClearValue === undefined
          || Array.isArray(asset.data.colorClearValue)
            && asset.data.colorClearValue.length === 4
            && asset.data.colorClearValue.every(Number.isFinite))
        && (asset.data.renderMode === undefined || asset.data.renderMode === 0),
      `PanelSettings ${asset.id} needs settings`);
      requireValue((asset.data.themeCss === undefined || typeof asset.data.themeCss === 'string')
        && (asset.data.themeFontAssets === undefined
          || Array.isArray(asset.data.themeFontAssets)
            && asset.data.themeFontAssets.every(font => typeof font.reference === 'string'
              && typeof font.assetId === 'string'
              && (font.fallbackAssetIds === undefined
                || Array.isArray(font.fallbackAssetIds))))
        && (asset.data.themeImageAssets === undefined
          || Array.isArray(asset.data.themeImageAssets)
            && asset.data.themeImageAssets.every(image => typeof image.reference === 'string'
              && typeof image.assetId === 'string')),
      `PanelSettings ${asset.id} has invalid theme references`);
    } else if (asset.kind === 'uiVisualTree' || asset.kind === 'uiStyleSheet') {
      if (asset.kind === 'uiStyleSheet')
        requireValue(typeof asset.data?.css === 'string',
          `StyleSheet ${asset.id} needs USS text`);
      else {
      requireValue(asset.data?.tree && typeof asset.data.tree.type === 'string'
        && Array.isArray(asset.data.stylesheets),
      `VisualTreeAsset ${asset.id} needs a tree and stylesheets`);
      }
      requireValue(asset.data.fontAssets === undefined
        || Array.isArray(asset.data.fontAssets) && asset.data.fontAssets.every(font =>
          typeof font.reference === 'string' && font.reference.length > 0
          && typeof font.assetId === 'string' && font.assetId.length > 0
          && (font.fallbackAssetIds === undefined
            || Array.isArray(font.fallbackAssetIds)
              && font.fallbackAssetIds.every(id => typeof id === 'string' && id.length > 0))),
      `VisualTreeAsset ${asset.id} has invalid font references`);
      requireValue(asset.data.imageAssets === undefined
        || Array.isArray(asset.data.imageAssets) && asset.data.imageAssets.every(image =>
          typeof image.reference === 'string' && image.reference.length > 0
          && typeof image.assetId === 'string' && image.assetId.length > 0),
      `VisualTreeAsset ${asset.id} has invalid image references`);
      if (asset.kind === 'uiVisualTree')
        validateUxmlTree(asset.data.tree, `VisualTreeAsset ${asset.id}`);
    } else if (asset.kind === 'prefab') {
      requireValue(asset.root && typeof asset.root === 'object'
        || format === BUNDLE_FORMAT && typeof asset.recordUrl === 'string'
          && /^catalog\/[a-f0-9]{64}\.json$/.test(asset.recordUrl)
          && Array.isArray(asset.dependencies), `Prefab ${asset.id} needs a root or catalog record`);
    } else if (asset.kind === 'inputActionAsset') {
      requireValue(format === SOURCE_FORMAT && typeof asset.dataJson === 'string'
        || asset.data && typeof asset.data === 'object' && !Array.isArray(asset.data)
        && (Array.isArray(asset.data.maps ?? asset.data.m_ActionMaps)
          || typeof asset.data.m_ActionMaps === 'string'),
      `InputActionAsset ${asset.id} needs action maps`);
    } else if (asset.kind === 'scriptableObject') {
      requireValue(typeof asset.type === 'string' && asset.type.length > 0,
        `ScriptableObject ${asset.id} needs a type`);
      requireValue(asset.properties && typeof asset.properties === 'object' && !Array.isArray(asset.properties),
        `ScriptableObject ${asset.id} needs properties`);
    } else if (asset.kind === 'physicsMaterial') {
      for (const key of ['dynamicFriction', 'staticFriction', 'bounciness'])
        requireValue(Number.isFinite(asset[key]) && asset[key] >= 0,
          `PhysicsMaterial ${asset.id} needs ${key}`);
    } else if (asset.kind === 'material') {
      requireValue(asset.data && typeof asset.data === 'object'
        && typeof asset.data.shaderGuid === 'string'
        && Array.isArray(asset.data.color) && asset.data.color.length === 4
        && asset.data.color.every(Number.isFinite),
      `Material ${asset.id} needs shader identity and RGBA color`);
    } else if (asset.kind === 'tmpFont') {
      const data = asset.data;
      requireValue(data && typeof data.atlasTextureAssetId === 'string'
        && (data.fallbackFontAssetIds === undefined
          || (Array.isArray(data.fallbackFontAssetIds)
            && data.fallbackFontAssetIds.every(id => typeof id === 'string')))
        && Number.isFinite(data.pointSize) && data.pointSize > 0
        && Number.isFinite(data.lineHeight) && data.lineHeight > 0
        && (data.ascentLine === undefined || Number.isFinite(data.ascentLine))
        && (data.descentLine === undefined || Number.isFinite(data.descentLine))
        && (data.boldStyle === undefined || Number.isFinite(data.boldStyle))
        && (data.boldSpacing === undefined || Number.isFinite(data.boldSpacing))
        && (data.gradientScale === undefined || Number.isFinite(data.gradientScale)
          && data.gradientScale > 0)
        && Array.isArray(data.glyphs) && data.glyphs.every(glyph =>
          Number.isInteger(glyph.unicode) && glyph.unicode >= 0
          && ['x', 'y', 'width', 'height', 'bearingX', 'bearingY', 'advance']
            .every(key => Number.isFinite(glyph[key]))),
      `TMP font ${asset.id} needs atlas metrics and glyphs`);
    } else if (asset.kind === 'sprite') {
      const data = asset.data;
      requireValue(data && typeof data.textureAssetId === 'string'
        && Array.isArray(data.rect) && data.rect.length === 4
        && data.rect.every(Number.isFinite)
        && Array.isArray(data.pivot) && data.pivot.length === 2
        && data.pivot.every(Number.isFinite)
        && (data.border === undefined || Array.isArray(data.border)
          && data.border.length === 4 && data.border.every(Number.isFinite))
        && Number.isFinite(data.pixelsPerUnit) && data.pixelsPerUnit > 0
        && Number.isFinite(data.textureWidth) && data.textureWidth > 0
        && Number.isFinite(data.textureHeight) && data.textureHeight > 0
        && (data.textureWrapMode === undefined || Number.isInteger(data.textureWrapMode))
        && (data.textureFilterMode === undefined || Number.isInteger(data.textureFilterMode)),
      `Sprite ${asset.id} needs a texture, rect, pivot, and pixelsPerUnit`);
    } else if (asset.kind === 'animatorController') {
      requireValue(asset.graph && Array.isArray(asset.graph.layers)
        && Array.isArray(asset.graph.parameters),
      `AnimatorController ${asset.id} needs a graph`);
    } else if (asset.kind === 'animationClip') {
      requireValue(typeof asset.name === 'string' && Number.isFinite(asset.duration)
        && asset.duration >= 0 && Array.isArray(asset.tracks),
      `AnimationClip ${asset.id} needs name, duration, and tracks`);
    } else {
      const location = format === SOURCE_FORMAT ? asset.path : asset.url;
      requireValue(typeof location === 'string' && location.length > 0, `Asset ${asset.id} needs a location`);
    }
  }
  const visit = node => {
    requireValue(node && typeof node.id === 'string' && node.id.length > 0, 'GameObject needs an id');
    requireValue(!ids.has(node.id), `Duplicate object/component id: ${node.id}`);
    ids.add(node.id);
    for (const component of node.components ?? [])
      if (typeof component.type === 'string')
        assertPreviewTypeAllowed(component.type, `component ${component.id ?? '?'} on ${node.name}`);
    requireValue(typeof node.name === 'string', `GameObject ${node.id} needs a name`);
    if (node.layer !== undefined)
      requireValue(Number.isInteger(node.layer) && node.layer >= 0 && node.layer < 32,
        `GameObject ${node.id} has invalid layer`);
    if (node.isStatic !== undefined)
      requireValue(typeof node.isStatic === 'boolean', `GameObject ${node.id} has invalid isStatic`);
    requireValue(Array.isArray(node.components ?? []), `GameObject ${node.id} needs components array`);
    requireValue(Array.isArray(node.children ?? []), `GameObject ${node.id} needs children array`);
    const transform = node.transform ?? {};
    if (transform.id !== undefined) {
      requireValue(typeof transform.id === 'string' && transform.id.length > 0,
        `GameObject ${node.id} has invalid transform id`);
      requireValue(!ids.has(transform.id), `Duplicate object/component id: ${transform.id}`);
      ids.add(transform.id);
    }
    for (const [key, size] of [['position', 3], ['rotation', 4], ['scale', 3]]) {
      const value = transform[key];
      requireValue(Array.isArray(value) && value.length === size && value.every(Number.isFinite),
        `GameObject ${node.id} has invalid transform.${key}`);
    }
    if (node.rectTransform !== undefined) {
      requireValue(node.rectTransform && typeof node.rectTransform === 'object',
        `GameObject ${node.id} needs rectTransform data`);
      for (const key of ['anchorMin', 'anchorMax', 'pivot', 'sizeDelta'])
        requireValue(Array.isArray(node.rectTransform[key]) && node.rectTransform[key].length === 2
          && node.rectTransform[key].every(Number.isFinite),
        `GameObject ${node.id} has invalid rectTransform.${key}`);
      if (node.rectTransform.anchoredPosition !== undefined)
        requireValue(Array.isArray(node.rectTransform.anchoredPosition)
          && node.rectTransform.anchoredPosition.length === 2
          && node.rectTransform.anchoredPosition.every(Number.isFinite),
        `GameObject ${node.id} has invalid rectTransform.anchoredPosition`);
    }
    for (const component of node.components ?? []) {
      requireValue(component && typeof component.id === 'string' && component.id.length > 0,
        `Component on ${node.id} needs an id`);
      requireValue(!ids.has(component.id), `Duplicate object/component id: ${component.id}`);
      ids.add(component.id);
      requireValue(typeof component.type === 'string' && component.type.length > 0,
        `Component ${component.id} needs a type`);
      requireValue(component.properties === undefined || (component.properties && typeof component.properties === 'object'),
        `Component ${component.id} properties must be an object`);
    }
    if (node.model !== undefined) requireValue(assetIds.has(node.model), `Unknown model asset: ${node.model}`);
    if (node.semanticSurface !== undefined) requireValue(
      typeof node.semanticSurface === 'string' && node.semanticSurface.length > 0,
      `GameObject ${node.id} has invalid semanticSurface`);
    for (const child of node.children ?? []) visit(child);
  };
  for (const root of manifest.scene.roots) visit(root);
  for (const asset of manifest.assets) if (asset.kind === 'prefab' && asset.root) visit(asset.root);
  if (manifest.scene.navMesh !== undefined) {
    requireValue(typeof manifest.scene.navMesh === 'string'
      && manifest.assets.some(asset => asset.id === manifest.scene.navMesh && asset.kind === 'navMesh'),
    `Unknown NavMesh asset: ${manifest.scene.navMesh}`);
  }
  if (manifest.resources !== undefined) {
    requireValue(manifest.resources && typeof manifest.resources === 'object'
      && !Array.isArray(manifest.resources), 'Resources must map paths to asset IDs');
    const resourcePaths = new Set();
    for (const [path, id] of globalThis.Object.entries(manifest.resources)) {
      requireValue(path.length > 0 && assetIds.has(id), `Invalid Resources mapping: ${path}`);
      const normalized = path.replaceAll('\\', '/').replace(/^\/+|\/+$/g, '').toLowerCase();
      requireValue(normalized.length > 0 && !resourcePaths.has(normalized),
        `Duplicate Resources path: ${path}`);
      resourcePaths.add(normalized);
    }
  }
  if (manifest.assetBundles !== undefined) {
    requireValue(manifest.assetBundles && typeof manifest.assetBundles === 'object'
      && !Array.isArray(manifest.assetBundles), 'AssetBundles must map names to asset paths');
    const names = new Set();
    for (const [name, paths] of globalThis.Object.entries(manifest.assetBundles)) {
      const normalizedName = name.replaceAll('\\', '/').toLowerCase();
      requireValue(normalizedName.length > 0 && !names.has(normalizedName),
        `Duplicate AssetBundle name: ${name}`);
      names.add(normalizedName);
      requireValue(paths && typeof paths === 'object' && !Array.isArray(paths),
        `Invalid AssetBundle paths: ${name}`);
      const aliases = new Set();
      for (const [path, id] of globalThis.Object.entries(paths)) {
        const alias = path.replaceAll('\\', '/').toLowerCase();
        requireValue(alias.length > 0 && !aliases.has(alias) && assetIds.has(id),
          `Invalid AssetBundle mapping: ${name}/${path}`);
        aliases.add(alias);
      }
    }
  }
  const inspect = (value, location) => {
    if (!value || typeof value !== 'object') return;
    if (typeof value.type === 'string') assertPreviewTypeAllowed(value.type, `${location}.type`);
    if (typeof value.$serializedType === 'string')
      assertPreviewTypeAllowed(value.$serializedType, `${location}.$serializedType`);
    if (typeof value.$ref === 'string') requireValue(ids.has(value.$ref), `Unknown object reference: ${value.$ref}`);
    if (typeof value.$asset === 'string') requireValue(assetIds.has(value.$asset), `Unknown asset reference: ${value.$asset}`);
    if (value.$uiAction) requireValue(assetIds.has(value.$uiAction.asset),
      `Unknown UI action asset: ${value.$uiAction.asset}`);
    for (const [key, item] of globalThis.Object.entries(value))
      inspect(item, Array.isArray(value) ? `${location}[${key}]` : `${location}.${key}`);
  };
  for (const root of manifest.scene.roots) inspect(root, `scene root ${root.id}`);
  for (const asset of manifest.assets) {
    if (asset.kind === 'material' && asset.data.textureAssetId !== undefined)
      requireValue(typeof asset.data.textureAssetId === 'string'
        && manifest.assets.some(candidate => candidate.id === asset.data.textureAssetId
          && candidate.kind === 'texture'),
      `Material ${asset.id} needs a referenced texture asset`);
    if (asset.kind === 'sprite')
      requireValue(manifest.assets.some(candidate =>
        candidate.id === asset.data.textureAssetId && candidate.kind === 'texture'),
      `Sprite ${asset.id} needs a referenced texture asset`);
    if (asset.kind === 'tmpFont') {
      requireValue(manifest.assets.some(candidate => candidate.id === asset.data.atlasTextureAssetId
        && candidate.kind === 'texture'), `TMP font ${asset.id} needs an atlas texture`);
      requireValue((asset.data.fallbackFontAssetIds ?? []).every(id =>
        manifest.assets.some(candidate => candidate.id === id && candidate.kind === 'tmpFont')),
      `TMP font ${asset.id} needs referenced fallback fonts`);
    }
    if (asset.kind === 'uiVisualTree' || asset.kind === 'uiStyleSheet'
      || asset.kind === 'uiPanelSettings')
    {
      for (const font of (asset.kind === 'uiPanelSettings'
        ? asset.data.themeFontAssets : asset.data.fontAssets) ?? [])
        requireValue([font.assetId, ...(font.fallbackAssetIds ?? [])].every(id =>
          manifest.assets.some(candidate => candidate.id === id
            && candidate.kind === 'uiFont')),
        `VisualTreeAsset ${asset.id} needs a referenced UI font asset`);
      for (const image of (asset.kind === 'uiPanelSettings'
        ? asset.data.themeImageAssets : asset.data.imageAssets) ?? [])
        requireValue(manifest.assets.some(candidate => candidate.id === image.assetId
          && candidate.kind === 'texture'),
        `VisualTreeAsset ${asset.id} needs a referenced UI image asset`);
    }
    if (asset.kind === 'scriptableObject') inspect(asset.properties, `asset ${asset.id}.properties`);
    if (asset.kind === 'prefab' && asset.root) inspect(asset.root, `prefab ${asset.id}`);
    if (asset.kind === 'prefab' && asset.recordUrl)
      for (const id of asset.dependencies)
        requireValue(assetIds.has(id), `Unknown deferred prefab dependency: ${asset.id} -> ${id}`);
  }
  return manifest;
}
