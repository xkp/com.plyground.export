// Generated from audits/core-namespaced-enums-unity-reference.json.
namespace UnityEngine
{
    public partial class Camera
    {
        public enum FieldOfViewAxis
        {
            Vertical = 0,
            Horizontal = 1,
        }
        public enum GateFitMode
        {
            None = 0,
            Vertical = 1,
            Horizontal = 2,
            Fill = 3,
            Overscan = 4,
        }
        public enum MonoOrStereoscopicEye
        {
            Left = 0,
            Right = 1,
            Mono = 2,
        }
        public enum SceneViewFilterMode
        {
            Off = 0,
            ShowFiltered = 1,
        }
        public enum StereoscopicEye
        {
            Left = 0,
            Right = 1,
        }
    }
    public class GraphicsBuffer
    {
        public enum Target
        {
            Vertex = 1,
            Index = 2,
            CopySource = 4,
            CopyDestination = 8,
            Structured = 16,
            Raw = 32,
            Append = 64,
            Counter = 128,
            IndirectArguments = 256,
            Constant = 512,
        }
        public enum UsageFlags
        {
            None = 0,
            LockBufferForWrite = 1,
        }
    }
    public class LightingSettings
    {
        public enum DenoiserType
        {
            None = 0,
            Optix = 1,
            OpenImage = 2,
            RadeonPro = 3,
        }
        public enum FilterMode
        {
            None = 0,
            Auto = 1,
            Advanced = 2,
        }
        public enum FilterType
        {
            Gaussian = 0,
            ATrous = 1,
            None = 2,
        }
        public enum LightmapPackingMethod
        {
            Unity = 0,
            XAtlas = 1,
        }
        public enum LightmapPackingMode
        {
            Auto = 0,
            Custom = 1,
        }
        public enum Lightmapper
        {
            Enlighten = 0,
            ProgressiveCPU = 1,
            ProgressiveGPU = 2,
        }
        public enum Sampling
        {
            Auto = 0,
            Fixed = 1,
        }
    }
    public class LightProbeProxyVolume
    {
        public enum BoundingBoxMode
        {
            AutomaticLocal = 0,
            AutomaticWorld = 1,
            Custom = 2,
        }
        public enum DataFormat
        {
            HalfFloat = 0,
            Float = 1,
        }
        public enum ProbePositionMode
        {
            CellCorner = 0,
            CellCenter = 1,
        }
        public enum QualityMode
        {
            Low = 0,
            Normal = 1,
        }
        public enum RefreshMode
        {
            Automatic = 0,
            EveryFrame = 1,
            ViaScripting = 2,
        }
        public enum ResolutionMode
        {
            Automatic = 0,
            Custom = 1,
        }
    }
    public partial class RectTransform
    {
        public enum Axis
        {
            Horizontal = 0,
            Vertical = 1,
        }
        public enum Edge
        {
            Left = 0,
            Right = 1,
            Top = 2,
            Bottom = 3,
        }
    }
    public class ReflectionProbe
    {
        public enum ReflectionProbeEvent
        {
            ReflectionProbeAdded = 0,
            ReflectionProbeRemoved = 1,
        }
    }
    public partial class Texture2D
    {
        public enum EXRFlags
        {
            None = 0,
            OutputAsFloat = 1,
            CompressZIP = 2,
            CompressRLE = 4,
            CompressPIZ = 8,
        }
    }
    public class TouchScreenKeyboard
    {
        public enum InputFieldAppearance
        {
            Customizable = 0,
            AlwaysVisible = 1,
            AlwaysHidden = 2,
        }
        public enum Status
        {
            Visible = 0,
            Done = 1,
            Canceled = 2,
            LostFocus = 3,
        }
    }
}
namespace UnityEngine.DedicatedServer
{
    public static class Arguments
    {
        public enum ArgumentErrorPolicy
        {
            Ignore = 0,
            Warn = 1,
            Fatal = 2,
        }
    }
}
namespace UnityEngine.Diagnostics
{
    public enum ForcedCrashCategory
    {
        AccessViolation = 0,
        FatalError = 1,
        Abort = 2,
        PureVirtualFunction = 3,
        MonoAbort = 4,
    }
}
namespace UnityEngine.Networking.PlayerConnection
{
    public enum ConnectionTarget
    {
        None = 0,
        Player = 1,
        Editor = 2,
    }
}
namespace UnityEngine.Playables
{
    public enum DataStreamType
    {
        Animation = 0,
        Audio = 1,
        Texture = 2,
        None = 3,
    }
    public enum DirectorUpdateMode
    {
        DSPClock = 0,
        GameTime = 1,
        UnscaledGameTime = 2,
        Manual = 3,
    }
    public enum DirectorWrapMode
    {
        Hold = 0,
        Loop = 1,
        None = 2,
    }
    public enum PlayableTraversalMode
    {
        Mix = 0,
        Passthrough = 1,
    }
    public enum PlayState
    {
        Paused = 0,
        Playing = 1,
        Delayed = 2,
    }
    public partial struct FrameData
    {
        public enum EvaluationType
        {
            Evaluate = 0,
            Playback = 1,
        }
    }
}
namespace UnityEngine.Profiling
{
    public enum ProfilerArea
    {
        CPU = 0,
        GPU = 1,
        Rendering = 2,
        Memory = 3,
        Audio = 4,
        Video = 5,
        Physics = 6,
        Physics2D = 7,
        NetworkMessages = 8,
        NetworkOperations = 9,
        UI = 10,
        UIDetails = 11,
        GlobalIllumination = 12,
        VirtualTexturing = 13,
    }
}
namespace UnityEngine.Rendering
{
    public class RayTracingAccelerationStructure
    {
        public enum ManagementMode
        {
            Manual = 0,
            Automatic = 1,
        }
        public enum RayTracingModeMask
        {
            Nothing = 0,
            Static = 2,
            DynamicTransform = 4,
            DynamicGeometry = 8,
            DynamicGeometryManualUpdate = 16,
            Everything = 30,
        }
    }
    public class SplashScreen
    {
        public enum StopBehavior
        {
            StopImmediate = 0,
            FadeOut = 1,
        }
    }
    public class SupportedOnRenderPipelineAttribute
    {
        public enum SupportedMode
        {
            Unsupported = 0,
            Supported = 1,
            SupportedByBaseClass = 2,
        }
    }
    public class SupportedRenderingFeatures
    {
        public enum LightmapMixedBakeModes
        {
            None = 0,
            IndirectOnly = 1,
            Subtractive = 2,
            Shadowmask = 4,
        }
        public enum ReflectionProbeModes
        {
            None = 0,
            Rotation = 1,
        }
    }
}
namespace UnityEngine.Scripting
{
    public static class GarbageCollector
    {
        public enum Mode
        {
            Disabled = 0,
            Enabled = 1,
            Manual = 2,
        }
    }
}
namespace UnityEngine.Search
{
    public enum SearchViewFlags
    {
        None = 0,
        Debug = 16,
        NoIndexing = 32,
        Packages = 256,
        OpenLeftSidePanel = 2048,
        OpenInspectorPreview = 4096,
        Centered = 8192,
        HideSearchBar = 16384,
        CompactView = 32768,
        ListView = 65536,
        GridView = 131072,
        TableView = 262144,
        EnableSearchQuery = 524288,
        DisableInspectorPreview = 1048576,
        DisableSavedSearchQuery = 2097152,
        OpenInBuilderMode = 4194304,
        OpenInTextMode = 8388608,
        DisableBuilderModeToggle = 16777216,
        Borderless = 33554432,
        ContextSwitchPreservedMask = 33560576,
        DisableQueryHelpers = 67108864,
        DisableNoResultTips = 134217728,
        IgnoreSavedSearches = 268435456,
        ObjectPicker = 536870912,
        ObjectPickerAdvancedUI = 1073741824,
    }
}
