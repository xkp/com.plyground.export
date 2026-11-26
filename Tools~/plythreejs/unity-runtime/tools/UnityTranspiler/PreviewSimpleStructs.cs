// Generated from audits/core-simple-structs-unity-reference.json.
namespace UnityEngine
{
    public struct CacheIndex
    {
        public string name;
        public int bytesUsed;
        public int expires;
    }
}
namespace UnityEngine
{
    public struct CustomRenderTextureUpdateZone
    {
        public UnityEngine.Vector3 updateZoneCenter;
        public UnityEngine.Vector3 updateZoneSize;
        public float rotation;
        public int passIndex;
        public bool needSwap;
    }
}
namespace UnityEngine
{
    public struct LightBakingOutput
    {
        public int probeOcclusionLightIndex;
        public int occlusionMaskChannel;
        public UnityEngine.LightmapBakeType lightmapBakeType;
        public UnityEngine.MixedLightingMode mixedLightingMode;
        public bool isBaked;
    }
}
namespace UnityEngine.Rendering
{
    public struct MetadataValue
    {
        public int NameID;
        public uint Value;
    }
}
namespace UnityEngine.Rendering
{
    public struct RayTracingInstanceCullingTest
    {
        public uint instanceMask;
        public int layerMask;
        public int shadowCastingModeMask;
        public bool allowOpaqueMaterials;
        public bool allowTransparentMaterials;
        public bool allowAlphaTestedMaterials;
        public bool allowVisualEffects;
    }
}
namespace UnityEngine.Rendering
{
    public struct RayTracingInstanceMaterialCRC
    {
        public int instanceID;
        public int crc;
    }
}
namespace UnityEngine.Rendering
{
    public struct RayTracingInstanceTriangleCullingConfig
    {
        public string[] optionalDoubleSidedShaderKeywords;
        public bool frontTriangleCounterClockwise;
        public bool checkDoubleSidedGIMaterial;
        public bool forceDoubleSided;
    }
}
