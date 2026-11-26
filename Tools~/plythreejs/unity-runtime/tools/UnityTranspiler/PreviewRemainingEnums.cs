// Generated from audits/core-remaining-enums-unity-reference.json.
namespace UnityEngine
{
    public enum ApplicationMemoryUsage
    {
        Unknown = 0,
        Low = 1,
        Medium = 2,
        High = 3,
        Critical = 4,
    }
    public enum ApplicationSandboxType
    {
        Unknown = 0,
        NotSandboxed = 1,
        Sandboxed = 2,
        SandboxBroken = 3,
    }
    public enum BlendWeights
    {
        OneBone = 1,
        TwoBones = 2,
        FourBones = 4,
    }
    public enum CameraClearFlags
    {
        Skybox = 1,
        Color = 2,
        SolidColor = 2,
        Depth = 3,
        Nothing = 4,
    }
    public enum CameraType
    {
        Game = 1,
        SceneView = 2,
        Preview = 4,
        VR = 8,
        Reflection = 16,
    }
    public enum ColorGamut
    {
        sRGB = 0,
        Rec709 = 1,
        Rec2020 = 2,
        DisplayP3 = 3,
        HDR10 = 4,
        DolbyHDR = 5,
        P3D65G22 = 6,
    }
    public enum ColorPrimaries
    {
        Rec709 = 0,
        Rec2020 = 1,
        P3 = 2,
        Unknown = -1,
    }
    public enum CompressionType
    {
        None = 0,
        Lzma = 1,
        Lz4 = 2,
        Lz4HC = 3,
    }
    public enum ComputeBufferMode
    {
        Immutable = 0,
        Dynamic = 1,
        Circular = 2,
        StreamOut = 3,
        SubUpdates = 4,
    }
    public enum ComputeBufferType
    {
        Default = 0,
        Raw = 1,
        Append = 2,
        Counter = 4,
        Constant = 8,
        Structured = 16,
        DrawIndirect = 256,
        IndirectArguments = 256,
        GPUMemory = 512,
    }
    public enum CursorMode
    {
        Auto = 0,
        ForceSoftware = 1,
    }
    public enum CustomRenderTextureInitializationSource
    {
        TextureAndColor = 0,
        Material = 1,
    }
    public enum CustomRenderTextureUpdateMode
    {
        OnLoad = 0,
        Realtime = 1,
        OnDemand = 2,
    }
    public enum CustomRenderTextureUpdateZoneSpace
    {
        Normalized = 0,
        Pixel = 1,
    }
    public enum D3D12Comparator
    {
        EqualTo = 0,
        NotEqualTo = 1,
        LessThan = 2,
        LessThanOrEqualTo = 3,
        GreaterThan = 4,
        GreaterThanOrEqualTo = 5,
    }
    public enum D3D12GraphicsDeviceType
    {
        DoNotCare = 0,
        Discrete = 1,
        Integrated = 2,
    }
    public enum D3DHDRDisplayBitDepth
    {
        D3DHDRDisplayBitDepth10 = 0,
        D3DHDRDisplayBitDepth16 = 1,
    }
    public enum DepthTextureMode
    {
        None = 0,
        Depth = 1,
        DepthNormals = 2,
        MotionVectors = 4,
    }
    public enum DeviceType
    {
        Unknown = 0,
        Handheld = 1,
        Console = 2,
        Desktop = 3,
    }
    public enum DrivenTransformProperties
    {
        None = 0,
        AnchoredPositionX = 2,
        AnchoredPositionY = 4,
        AnchoredPosition = 6,
        AnchoredPositionZ = 8,
        AnchoredPosition3D = 14,
        Rotation = 16,
        ScaleX = 32,
        ScaleY = 64,
        ScaleZ = 128,
        Scale = 224,
        AnchorMinX = 256,
        AnchorMinY = 512,
        AnchorMin = 768,
        AnchorMaxX = 1024,
        AnchorMaxY = 2048,
        AnchorMax = 3072,
        Anchors = 3840,
        SizeDeltaX = 4096,
        SizeDeltaY = 8192,
        SizeDelta = 12288,
        PivotX = 16384,
        PivotY = 32768,
        Pivot = 49152,
        All = -1,
    }
    public enum FogMode
    {
        Linear = 1,
        Exponential = 2,
        ExponentialSquared = 3,
    }
    public enum FullScreenMovieControlMode
    {
        Full = 0,
        Minimal = 1,
        CancelOnInput = 2,
        Hidden = 3,
    }
    public enum FullScreenMovieScalingMode
    {
        None = 0,
        AspectFit = 1,
        AspectFill = 2,
        Fill = 3,
    }
    public enum GraphicsJobsFilterMode
    {
        Off = 0,
        Native = 1,
        Legacy = 2,
        Split = 3,
    }
    public enum HDRDisplayBitDepth
    {
        BitDepth10 = 0,
        BitDepth16 = 1,
    }
    public enum HDRDisplaySupportFlags
    {
        None = 0,
        Supported = 1,
        RuntimeSwitchable = 2,
        AutomaticTonemapping = 4,
    }
    public enum InspectorSort
    {
        ByName = 0,
        ByValue = 1,
    }
    public enum InspectorSortDirection
    {
        Ascending = 0,
        Descending = 1,
    }
    public enum LightmapBakeType
    {
        Mixed = 1,
        Baked = 2,
        Realtime = 4,
    }
    public enum LightmapCompression
    {
        None = 0,
        LowQuality = 1,
        NormalQuality = 2,
        HighQuality = 3,
    }
    public enum LightmappingMode
    {
        Mixed = 1,
        Baked = 2,
        Realtime = 4,
    }
    public enum LightmapsMode
    {
        NonDirectional = 0,
        Single = 0,
        CombinedDirectional = 1,
        Dual = 1,
        SeparateDirectional = 2,
        Directional = 2,
    }
    public enum LightmapsModeLegacy
    {
        Single = 0,
        Dual = 1,
        Directional = 2,
    }
    public enum LightRenderMode
    {
        Auto = 0,
        ForcePixel = 1,
        ForceVertex = 2,
    }
    public enum LightShadowCasterMode
    {
        Default = 0,
        NonLightmappedOnly = 1,
        Everything = 2,
    }
    public enum LightShadows
    {
        None = 0,
        Hard = 1,
        Soft = 2,
    }
    public enum LightShape
    {
        Cone = 0,
        Pyramid = 1,
        Box = 2,
    }
    public enum LightType
    {
        Spot = 0,
        Directional = 1,
        Point = 2,
        Area = 3,
        Rectangle = 3,
        Disc = 4,
        Pyramid = 5,
        Box = 6,
        Tube = 7,
    }
    public enum LineAlignment
    {
        View = 0,
        Local = 1,
        TransformZ = 1,
    }
    public enum LineTextureMode
    {
        Stretch = 0,
        Tile = 1,
        DistributePerSegment = 2,
        RepeatPerSegment = 3,
        Static = 4,
    }
    public enum LODFadeMode
    {
        None = 0,
        CrossFade = 1,
        SpeedTree = 2,
    }
    public enum MaterialGlobalIlluminationFlags
    {
        None = 0,
        RealtimeEmissive = 1,
        BakedEmissive = 2,
        AnyEmissive = 3,
        EmissiveIsBlack = 4,
    }
    public enum MaterialPropertyType
    {
        Float = 0,
        Int = 1,
        Vector = 2,
        Matrix = 3,
        Texture = 4,
        ConstantBuffer = 5,
        ComputeBuffer = 6,
    }
    public enum MeshTopology
    {
        Triangles = 0,
        Quads = 2,
        Lines = 3,
        LineStrip = 4,
        Points = 5,
    }
    public enum MixedLightingMode
    {
        IndirectOnly = 0,
        Subtractive = 1,
        Shadowmask = 2,
    }
    public enum MotionVectorGenerationMode
    {
        Camera = 0,
        Object = 1,
        ForceNoMotion = 2,
    }
    public enum NetworkReachability
    {
        NotReachable = 0,
        ReachableViaCarrierDataNetwork = 1,
        ReachableViaLocalAreaNetwork = 2,
    }
    public enum NPOTSupport
    {
        None = 0,
        Restricted = 1,
        Full = 2,
    }
    public enum ReceiveGI
    {
        Lightmaps = 1,
        LightProbes = 2,
    }
    public enum RenderTextureReadWrite
    {
        Default = 0,
        Linear = 1,
        sRGB = 2,
    }
    public enum RuntimeInitializeLoadType
    {
        AfterSceneLoad = 0,
        BeforeSceneLoad = 1,
        AfterAssembliesLoaded = 2,
        BeforeSplashScreen = 3,
        SubsystemRegistration = 4,
    }
    public enum ShadowmaskMode
    {
        Shadowmask = 0,
        DistanceShadowmask = 1,
    }
    public enum ShadowObjectsFilter
    {
        AllObjects = 0,
        DynamicOnly = 1,
        StaticOnly = 2,
    }
    public enum SkinQuality
    {
        Auto = 0,
        Bone1 = 1,
        Bone2 = 2,
        Bone4 = 4,
    }
    public enum SnapAxis : byte
    {
        None = 0,
        X = 1,
        Y = 2,
        Z = 4,
        All = 7,
    }
    public enum SpriteAlignment
    {
        Center = 0,
        TopLeft = 1,
        TopCenter = 2,
        TopRight = 3,
        LeftCenter = 4,
        RightCenter = 5,
        BottomLeft = 6,
        BottomCenter = 7,
        BottomRight = 8,
        Custom = 9,
    }
    public enum SpriteDrawMode
    {
        Simple = 0,
        Sliced = 1,
        Tiled = 2,
    }
    public enum SpriteMeshType
    {
        FullRect = 0,
        Tight = 1,
    }
    public enum SpritePackingMode
    {
        Tight = 0,
        Rectangle = 1,
    }
    public enum SpritePackingRotation
    {
        None = 0,
        FlipHorizontal = 1,
        FlipVertical = 2,
        Rotate180 = 3,
        Any = 15,
    }
    public enum SpriteSortPoint
    {
        Center = 0,
        Pivot = 1,
    }
    public enum SpriteTileMode
    {
        Continuous = 0,
        Adaptive = 1,
    }
    public enum StackTraceLogType
    {
        None = 0,
        ScriptOnly = 1,
        Full = 2,
    }
    public enum StereoTargetEyeMask
    {
        None = 0,
        Left = 1,
        Right = 2,
        Both = 3,
    }
    public enum TexGenMode
    {
        None = 0,
        SphereMap = 1,
        Object = 2,
        EyeLinear = 3,
        CubeReflect = 4,
        CubeNormal = 5,
    }
    public enum TextureMipmapLimitBiasMode
    {
        OffsetGlobalLimit = 0,
        OverrideGlobalLimit = 1,
    }
    public enum TouchScreenKeyboardType
    {
        Default = 0,
        ASCIICapable = 1,
        NumbersAndPunctuation = 2,
        URL = 3,
        NumberPad = 4,
        PhonePad = 5,
        NamePhonePad = 6,
        EmailAddress = 7,
        NintendoNetworkAccount = 8,
        Social = 9,
        Search = 10,
        DecimalPad = 11,
        OneTimeCode = 12,
    }
    public enum TransferFunction
    {
        sRGB = 0,
        BT1886 = 1,
        PQ = 2,
        Linear = 3,
        Gamma22 = 4,
        Unknown = -1,
    }
    public enum TransparencySortMode
    {
        Default = 0,
        Perspective = 1,
        Orthographic = 2,
        CustomAxis = 3,
    }
    public enum UserAuthorization
    {
        WebCam = 1,
        Microphone = 2,
    }
    public enum WeightedMode
    {
        None = 0,
        In = 1,
        Out = 2,
        Both = 3,
    }
    public enum WhitePoint
    {
        D65 = 0,
        Unknown = -1,
    }
}
