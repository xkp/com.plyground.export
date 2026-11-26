// Generated from audits/core-simple-structs-unity-reference.json.
// Regenerate with node tools/UnityApiInventory/generate-core-simple-structs.mjs.
import { Vector3 } from './math.js';

const copyField = value => value instanceof Vector3 ? value.clone()
  : Array.isArray(value) ? [...value] : value;

export class CacheIndex {
  constructor(initial = {}) {
    this.name = copyField(initial.name ?? null);
    this.bytesUsed = copyField(initial.bytesUsed ?? 0);
    this.expires = copyField(initial.expires ?? 0);
  }
  clone() { return new CacheIndex(this); }
}

export class CustomRenderTextureUpdateZone {
  constructor(initial = {}) {
    this.updateZoneCenter = copyField(initial.updateZoneCenter ?? new Vector3());
    this.updateZoneSize = copyField(initial.updateZoneSize ?? new Vector3());
    this.rotation = copyField(initial.rotation ?? 0);
    this.passIndex = copyField(initial.passIndex ?? 0);
    this.needSwap = copyField(initial.needSwap ?? false);
  }
  clone() { return new CustomRenderTextureUpdateZone(this); }
}

export class LightBakingOutput {
  constructor(initial = {}) {
    this.probeOcclusionLightIndex = copyField(initial.probeOcclusionLightIndex ?? 0);
    this.occlusionMaskChannel = copyField(initial.occlusionMaskChannel ?? 0);
    this.lightmapBakeType = copyField(initial.lightmapBakeType ?? 0);
    this.mixedLightingMode = copyField(initial.mixedLightingMode ?? 0);
    this.isBaked = copyField(initial.isBaked ?? false);
  }
  clone() { return new LightBakingOutput(this); }
}

export class MetadataValue {
  constructor(initial = {}) {
    this.NameID = copyField(initial.NameID ?? 0);
    this.Value = copyField(initial.Value ?? 0);
  }
  clone() { return new MetadataValue(this); }
}

export class RayTracingInstanceCullingTest {
  constructor(initial = {}) {
    this.instanceMask = copyField(initial.instanceMask ?? 0);
    this.layerMask = copyField(initial.layerMask ?? 0);
    this.shadowCastingModeMask = copyField(initial.shadowCastingModeMask ?? 0);
    this.allowOpaqueMaterials = copyField(initial.allowOpaqueMaterials ?? false);
    this.allowTransparentMaterials = copyField(initial.allowTransparentMaterials ?? false);
    this.allowAlphaTestedMaterials = copyField(initial.allowAlphaTestedMaterials ?? false);
    this.allowVisualEffects = copyField(initial.allowVisualEffects ?? false);
  }
  clone() { return new RayTracingInstanceCullingTest(this); }
}

export class RayTracingInstanceMaterialCRC {
  constructor(initial = {}) {
    this.instanceID = copyField(initial.instanceID ?? 0);
    this.crc = copyField(initial.crc ?? 0);
  }
  clone() { return new RayTracingInstanceMaterialCRC(this); }
}

export class RayTracingInstanceTriangleCullingConfig {
  constructor(initial = {}) {
    this.optionalDoubleSidedShaderKeywords = copyField(initial.optionalDoubleSidedShaderKeywords ?? null);
    this.frontTriangleCounterClockwise = copyField(initial.frontTriangleCounterClockwise ?? false);
    this.checkDoubleSidedGIMaterial = copyField(initial.checkDoubleSidedGIMaterial ?? false);
    this.forceDoubleSided = copyField(initial.forceDoubleSided ?? false);
  }
  clone() { return new RayTracingInstanceTriangleCullingConfig(this); }
}
