using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

static class PreviewExcludedApis
{
    private static readonly HashSet<string> RendererDataGraylist = [
        "UnityEngine.LightmapData", "UnityEngine.LightmapSettings",
        "UnityEngine.U2D.SpriteAtlas", "UnityEngine.U2D.SpriteAtlasManager",
        "UnityEngine.U2D.SpriteBone", "UnityEngine.U2D.SpriteDataAccessExtensions",
        "UnityEngine.U2D.SpriteRendererDataAccessExtensions",
    ];
    private static readonly HashSet<string> HostCallbackGraylist = [
        "UnityEngine.Application.AdvertisingIdentifierCallback",
        "UnityEngine.Application.LowMemoryCallback",
        "UnityEngine.Application.MemoryUsageChangedCallback",
        "UnityEngine.Display.DisplaysUpdatedDelegate",
        "UnityEngine.SortingLayer.LayerCallback",
    ];
    private static readonly HashSet<string> RendererCallbackGraylist = [
        "UnityEngine.Rendering.BatchRendererGroup.OnFinishedCulling",
        "UnityEngine.Rendering.BatchRendererGroup.OnPerformCulling",
    ];
    private static readonly HashSet<string> BrowserUiGraylist = [
        "UnityEngine.TextEditor",
        "UnityEngine.GUIElement",
        "UnityEngine.GUILayer",
        "UnityEngine.GUITexture",
        "UnityEngine.GUITargetAttribute",
        "UnityEngine.UIElements.PanelEventHandler",
        "UnityEngine.UIElements.WorldDocumentRaycaster",
        "UnityEngine.EventSystems.PhysicsRaycaster",
        "UnityEngine.EventSystems.Physics2DRaycaster",
        "UnityEngine.EventSystems.TouchInputModule",
        "UnityEngine.EventSystems.RaycasterManager",
        "UnityEngine.UI.CanvasUpdateRegistry",
        "UnityEngine.UI.GraphicRegistry",
        "UnityEngine.UI.ClipperRegistry",
        "UnityEngine.UI.StencilMaterial",
        "UnityEngine.UI.DefaultControls",
        "UnityEngine.UI.FontUpdateTracker",
        "UnityEngine.UI.GraphicRebuildTracker",
        "UnityEngine.UI.BaseVertexEffect",
        "UnityEngine.UI.IMask",
        "UnityEngine.UI.IMaterialModifier",
    ];
    private static readonly HashSet<string> BrowserUiCollectionControllerGraylist = [
        "UnityEngine.UIElements.CollectionViewController",
        "UnityEngine.UIElements.BaseListViewController",
        "UnityEngine.UIElements.ListViewController",
        "UnityEngine.UIElements.BaseTreeViewController",
        "UnityEngine.UIElements.TreeViewController",
        "UnityEngine.UIElements.DefaultTreeViewController",
        "UnityEngine.UIElements.MultiColumnController",
        "UnityEngine.UIElements.MultiColumnListViewController",
        "UnityEngine.UIElements.MultiColumnTreeViewController",
        "UnityEngine.UIElements.DefaultMultiColumnTreeViewController",
    ];
    private static readonly HashSet<string> BrowserUiToolkitBackendGraylist = [
        "UnityEngine.UIElements.DynamicAtlasSettings",
        "UnityEngine.UIElements.EventDispatcherGate",
        "UnityEngine.UIElements.FillGradient",
        "UnityEngine.UIElements.FilterFunction",
        "UnityEngine.UIElements.FilterFunctionDefinition",
        "UnityEngine.UIElements.FilterParameter",
        "UnityEngine.UIElements.FilterParameterDeclaration",
        "UnityEngine.UIElements.FilterPassContext",
        "UnityEngine.UIElements.PostProcessingMargins",
        "UnityEngine.UIElements.DynamicAtlasCustomFilter",
        "UnityEngine.UIElements.ImmediateModeElement",
        "UnityEngine.UIElements.IMGUIContainer",
        "UnityEngine.UIElements.ITransform",
        "UnityEngine.UIElements.VisualElementFocusRing",
        "UnityEngine.UIElements.VisualElementFocusChangeDirection",
        "UnityEngine.UIElements.BaseFieldMouseDragger",
        "UnityEngine.UIElements.FieldMouseDragger",
        "UnityEngine.UIElements.FieldMouseDragger<T>",
        "UnityEngine.UIElements.ContextualMenuManager",
        "UnityEngine.UIElements.AbstractGenericMenu",
        "UnityEngine.UIElements.UIToolkitInputConfiguration",
        "UnityEngine.UIElements.InputSystem.InputSystemEventSystem",
        "UnityEngine.UIElements.AlignmentUtils",
        "UnityEngine.UIElements.VectorImage",
        "UnityEngine.UIElements.VisualElementDebugExtensions",
        "UnityEngine.UIElements.IDebugPanelChangeReceiver",
        "UnityEngine.UIElements.EventDispatcher",
        "UnityEngine.UIElements.IFocusRing",
        "UnityEngine.UIElements.IManipulator",
        "UnityEngine.UIElements.Manipulator",
        "UnityEngine.UIElements.ContextualMenuManipulator",
        "UnityEngine.UIElements.IMGUIEvent",
        "UnityEngine.UIElements.PanelTextSettings",
        "UnityEngine.UIElements.PanelExtensions",
        "UnityEngine.UIElements.PanelInputConfiguration",
        "UnityEngine.UIElements.PostProcessingPass",
        "UnityEngine.UIElements.KeyboardNavigationManipulator",
        "UnityEngine.UIElements.MouseManipulator",
        "UnityEngine.UIElements.PointerManipulator",
    ];
    private static readonly HashSet<string> BrowserUiRendererStyleMembers = [
        "unityMaterial", "unityTextGenerator", "unitySliceBottom",
        "unitySliceLeft", "unitySliceRight", "unitySliceScale",
        "unitySliceTop", "unitySliceType",
    ];
    private static readonly HashSet<string> BrowserUiNativeStyleMembers = [
        "filter", "unityEditorTextRenderingMode", "unityOverflowClipBox",
        "unityParagraphSpacing", "unityTextOverflowPosition",
    ];
    private static readonly HashSet<string> BrowserInputFieldHostMembers = [
        "ProcessEvent", "OnUpdateSelected", "OnBeginDrag", "OnDrag", "OnEndDrag",
        "touchScreenKeyboard",
    ];
    private static readonly HashSet<string> BrowserPanelNativeMembers = [
        "dynamicAtlasSettings",
        "textureSlotCount", "vertexBudget", "textSettings",
        "SetPanelChangeReceiver", "SetScreenToPanelSpaceFunction",
        "SetScreenToPanelSpaceFunction3D",
    ];
    private static readonly HashSet<string> BrowserToolkitPanelNativeMembers = [
        "contextualMenuManager", "dispatcher", "isDirty", "contextType",
        "CameraTransformWorldToPanel", "CameraTransformWorldToPanelRect",
        "ResetDynamicAtlas", "ResetRenderer", "SetTextureDirty",
        "pivot", "pivotReferenceSize", "position", "worldSpaceSize",
        "worldSpaceSizeMode", "contentHash", "importedWithErrors",
        "importedWithWarnings", "templateDependencies", "selectableGameObject",
    ];
    private static readonly HashSet<string> BrowserVisualElementNativeMembers = [
        "cacheAsBitmap", "disablePlayModeTint", "transform", "worldTransform",
        "visualTreeAssetSource", "PostProcessTextVertices",
        "selection",
    ];
    private static readonly HashSet<string> BrowserCanvasNativeMembers = [
        "GetDefaultCanvasMaterial", "GetDefaultCanvasTextMaterial",
        "GetETC1SupportedCanvasMaterial", "additionalShaderChannels",
        "cachedSortingLayerValue", "normalizedSortingGridSize", "renderOrder",
        "sortingGridNormalizedSize", "updateRectTransformForStandalone",
        "vertexColorAlwaysGammaSpace",
    ];
    private static readonly HashSet<string> BrowserCanvasRendererNativeMembers = [
        "onRequestRebuild", "Clear", "DisableRectClipping", "EnableRectClipping",
        "GetInheritedAlpha", "absoluteDepth", "clippingSoftness",
        "cullTransparentMesh", "hasMoved", "hasRectClipping", "isMask",
        "relativeDepth", "SetMesh",
    ];
    private static readonly HashSet<string> BrowserBaseSliderNativeMembers = [
        "dragContainerUssClassName", "draggerBorderUssClassName",
        "draggerUssClassName", "fillUssClassName",
        "horizontalVariantUssClassName", "inputUssClassName",
        "labelUssClassName", "movableUssClassName", "textFieldClassName",
        "trackerUssClassName", "ussClassName", "verticalVariantUssClassName",
        "AdjustDragElement", "ApplyInputDeviceDelta",
    ];
    private static readonly HashSet<string> BrowserTextInputHostMembers = [
        "inputUssClassName", "labelUssClassName",
        "multilineInputUssClassName", "placeholderUssClassName",
        "singleLineInputUssClassName", "textInputUssName", "ussClassName",
        "MeasureTextSize", "cursorPosition", "hideMobileInput",
        "hideSoftKeyboard", "touchScreenKeyboard",
    ];
    private static readonly HashSet<string> BrowserTextSelectionNativeMembers = [
        "OnCursorIndexChange", "OnSelectIndexChange",
        "GetCursorPositionFromStringIndex", "cursorPosition",
    ];
    private static readonly HashSet<string> BrowserTextValueNativeMembers = [
        "ApplyInputDeviceDelta", "StartDragging", "StopDragging",
        "supportExpressions",
    ];
    private static string GraylistMessage(string name) => RendererDataGraylist.Contains(name)
        ? $"Preview defers {name} API: exported lighting and sprite data lack a mapping on the single Three.js renderer; see core-threejs-renderer-boundary.md"
        : HostCallbackGraylist.Contains(name)
            ? $"Preview defers {name} API: the native Unity host event has no browser trigger"
        : RendererCallbackGraylist.Contains(name)
            ? $"Preview defers {name} API: Unity batch-culling callbacks are outside the single Three.js renderer"
        : BrowserUiGraylist.Contains(name)
            ? name == "UnityEngine.UI.DefaultControls"
                ? $"Preview defers {name} API: runtime construction of Unity's built-in control prefabs and resources is outside exported, Unity-authored browser UI; see ui-browser-boundary.md"
                : name is "UnityEngine.UI.FontUpdateTracker"
                    or "UnityEngine.UI.GraphicRebuildTracker"
                    ? $"Preview defers {name} API: native font-atlas and graphic rebuild trackers are replaced by the browser Canvas frame; see ui-browser-boundary.md"
                : name == "UnityEngine.UI.BaseVertexEffect"
                    ? $"Preview defers {name} API: custom uGUI vertex effects require the excluded mesh renderer; see ui-browser-boundary.md"
                : name is "UnityEngine.UI.IMask" or "UnityEngine.UI.IMaterialModifier"
                    ? $"Preview defers {name} API: custom native stencil and material modifiers are outside the browser Canvas renderer; see ui-browser-boundary.md"
                : name == "UnityEngine.TextEditor"
                ? $"Preview defers {name} API: direct IMGUI text-editor state is outside native browser text editing; see ui-browser-boundary.md"
                : name is "UnityEngine.GUIElement" or "UnityEngine.GUILayer"
                    or "UnityEngine.GUITexture" or "UnityEngine.GUITargetAttribute"
                    ? $"Preview defers {name} API: legacy GUI components and display-targeted IMGUI are outside the single browser UI renderer; use OnGUI controls; see ui-browser-boundary.md"
                : name is "UnityEngine.UIElements.PanelEventHandler"
                    or "UnityEngine.UIElements.WorldDocumentRaycaster"
                    ? $"Preview defers {name} API: native panel callbacks and world-document raycasts are outside the screen UI event route; see ui-browser-boundary.md"
                : name.StartsWith("UnityEngine.EventSystems.", StringComparison.Ordinal)
                    ? $"Preview defers {name} API: native physics raycasts, deprecated touch module, and raycaster registry are outside the screen UI event route; use StandaloneInputModule or InputSystemUIInputModule for browser input; see ui-browser-boundary.md"
                : $"Preview defers {name} API: Unity uGUI rebuild, graphic, clip, and stencil registries belong to the native renderer; Preview uses one DOM layout and event route; see ui-browser-boundary.md"
        : BrowserUiCollectionControllerGraylist.Contains(name)
            ? $"Preview defers {name} API: Unity collection controller internals are outside the DOM collection bridge; use the public ListView, TreeView, and multi-column view APIs; see ui-browser-boundary.md"
        : BrowserUiToolkitBackendGraylist.Contains(name)
            ? $"Preview defers {name} API: native UI Toolkit renderer, input, and manipulator internals are outside the DOM event route; see ui-browser-boundary.md"
        : name is "UnityEngine.UIElements.BindablePropertyChangedEventArgs"
            or "UnityEngine.UIElements.INotifyBindablePropertyChanged"
            ? $"Preview defers {name} API: Unity automatic binding updates are outside explicit browser UI value callbacks; see ui-browser-boundary.md"
        : name.StartsWith("UnityEngine.UIElements.TextElement.Glyph", StringComparison.Ordinal)
            ? $"Preview defers {name} API: native glyph vertices belong to Unity text rendering; browser text uses DOM fonts; see ui-browser-boundary.md"
        : name.StartsWith("UnityEngine.UIElements.Uxml", StringComparison.Ordinal)
            || name.StartsWith("UnityEngine.UIElements.IUxml", StringComparison.Ordinal)
            || name == "UnityEngine.UIElements.CreationContext"
            || name is "UnityEngine.UIElements.TypedUxmlAttributeDescription"
                or "UnityEngine.UIElements.BaseUxmlTraits"
                or "UnityEngine.UIElements.TextValueFieldTraits"
                or "UnityEngine.UIElements.BaseFieldTraits"
                or "UnityEngine.UIElements.RegisterUxmlCacheAttribute"
                or "UnityEngine.UIElements.EventInterestAttribute"
                or "UnityEngine.UIElements.ToggleButtonGroupStatePropertiesAttribute"
            || name is "UnityEngine.UIElements.BaseUxmlFactory"
                or "UnityEngine.UIElements.IBaseUxmlFactory"
            ? $"Preview defers {name} API: UXML authoring metadata is exported as assets, not executed in browser scripts; see ui-browser-boundary.md"
        : $"Preview graylists {name} API: implementation required for this use case";
    private static readonly string[] Namespaces =
    [
        "UnityEngine.Android",
        "UnityEngine.InputSystem.Android",
        "UnityEngine.Windows",
        "UnityEngine.WSA",
        "UnityEngine.iOS",
        "UnityEngine.tvOS",
        "UnityEngine.Apple",
        "UnityEngine.Lumin",
        "UnityEngine.ADBannerView",
        "UnityEngine.ADInterstitialAd",
        "UnityEngine.Social",
        "UnityEngine.SocialPlatforms",
        "UnityEngine.ClusterInput",
        "UnityEngine.ClusterInputType",
        "UnityEngine.ClusterNetwork",
        "UnityEngine.ClusterSerialization",
        "UnityEngine.AdaptivePerformance",
        "UnityEngine.AMD",
        "UnityEngine.NVIDIA",
        "UnityEngine.Shaders",
        "UnityEngine.TextCore",
        "UnityEngine.CharacterInfo",
        "UnityEngine.GUIText",
        "UnityEngine.TextAlignment",
        "UnityEngine.TextGenerationSettings",
        "UnityEngine.TextGeneratorType",
        "UnityEngine.TextGenerator",
        "UnityEngine.TextMesh",
        "UnityEngine.UICharInfo",
        "UnityEngine.UILineInfo",
        "UnityEngine.UIVertex",
        "UnityEngine.Tilemaps",
        "UnityEngine.GridBrushBase",
        "UnityEngine.CustomGridBrushAttribute",
        "UnityEngine.Analytics",
        "UnityEngine.RemoteSettings",
        "UnityEngine.RemoteConfigSettings",
        "UnityEngine.VFX",
        "UnityEngine.XR.GameViewRenderMode",
        "UnityEngine.XR.TrackingSpaceType",
        "UnityEngine.XR.WSA.RemoteDeviceVersion",
        "UnityEngine.XR.XRDevice",
        "UnityEngine.XR.XRSettings",
        "UnityEngine.XR.XRStats",
        "UnityEngine.XR",
        "UnityEngine.Rendering.VirtualTexturing",
        "UnityEngine.WindZone",
        "UnityEngine.WindZoneMode",
        "UnityEngine.SpriteMask",
        "UnityEngine.SpriteMaskInteraction",
        "UnityEngine.U2D.SpriteShapeRenderer",
        "UnityEngine.U2D.SpriteShapeParameters",
        "UnityEngine.U2D.SpriteShapeMetaData",
        "UnityEngine.U2D.SpriteShapeSegment",
        "UnityEngine.U2D.SpriteShapeUtility",
        "UnityEngine.U2D.ShapeControlPoint",
        "UnityEngine.U2D.AngleRangeInfo",
        "UnityEngine.ProceduralMaterial",
        "UnityEngine.ProceduralTexture",
        "UnityEngine.SubsystemManager",
        "UnityEngine.Subsystems",
        "UnityEngine.SubsystemsImplementation",
        "UnityEngine.Timeline",
        "UnityEngine.Rendering.UnifiedRayTracing",
    ];

    private static readonly string[] GraylistedNamespaces = [
        "UnityEngine.GUIElement",
        "UnityEngine.GUILayer",
        "UnityEngine.GUITexture",
        "UnityEngine.GUITargetAttribute",
        "UnityEngine.UIElements.PanelEventHandler",
        "UnityEngine.UIElements.WorldDocumentRaycaster",
        "UnityEngine.EventSystems.PhysicsRaycaster",
        "UnityEngine.EventSystems.Physics2DRaycaster",
        "UnityEngine.EventSystems.TouchInputModule",
        "UnityEngine.EventSystems.RaycasterManager",
        "UnityEngine.UIElements.CollectionViewController",
        "UnityEngine.UIElements.BaseUxmlFactory",
        "UnityEngine.UIElements.IBaseUxmlFactory",
        "UnityEngine.UIElements.DynamicAtlasSettings",
        "UnityEngine.UIElements.EventDispatcherGate",
        "UnityEngine.UIElements.FillGradient",
        "UnityEngine.UIElements.FilterFunction",
        "UnityEngine.UIElements.FilterFunctionDefinition",
        "UnityEngine.UIElements.FilterParameter",
        "UnityEngine.UIElements.FilterParameterDeclaration",
        "UnityEngine.UIElements.FilterPassContext",
        "UnityEngine.UIElements.PostProcessingMargins",
        "UnityEngine.UIElements.DynamicAtlasCustomFilter",
        "UnityEngine.UIElements.ImmediateModeElement",
        "UnityEngine.UIElements.IMGUIContainer",
        "UnityEngine.UIElements.ITransform",
        "UnityEngine.UIElements.VisualElementFocusRing",
        "UnityEngine.UIElements.VisualElementFocusChangeDirection",
        "UnityEngine.UIElements.BaseFieldMouseDragger",
        "UnityEngine.UIElements.FieldMouseDragger",
        "UnityEngine.UIElements.FieldMouseDragger<T>",
        "UnityEngine.UIElements.ContextualMenuManager",
        "UnityEngine.UIElements.AbstractGenericMenu",
        "UnityEngine.UIElements.UIToolkitInputConfiguration",
        "UnityEngine.UIElements.InputSystem.InputSystemEventSystem",
        "UnityEngine.UIElements.AlignmentUtils",
        "UnityEngine.UIElements.VectorImage",
        "UnityEngine.UIElements.VisualElementDebugExtensions",
        "UnityEngine.UIElements.IDebugPanelChangeReceiver",
        "UnityEngine.UIElements.EventDispatcher",
        "UnityEngine.UIElements.IFocusRing",
        "UnityEngine.UIElements.IManipulator",
        "UnityEngine.UIElements.Manipulator",
        "UnityEngine.UIElements.ContextualMenuManipulator",
        "UnityEngine.UIElements.IMGUIEvent",
        "UnityEngine.UIElements.PanelTextSettings",
        "UnityEngine.UIElements.PanelExtensions",
        "UnityEngine.UIElements.BindablePropertyChangedEventArgs",
        "UnityEngine.UIElements.INotifyBindablePropertyChanged",
        "UnityEngine.UIElements.TextElement.Glyph",
        "UnityEngine.UIElements.TextElement.GlyphsEnumerable",
        "UnityEngine.UIElements.TextElement.GlyphsEnumerator",
        "UnityEngine.UIElements.TypedUxmlAttributeDescription",
        "UnityEngine.UIElements.BaseUxmlTraits",
        "UnityEngine.UIElements.TextValueFieldTraits",
        "UnityEngine.UIElements.BaseFieldTraits",
        "UnityEngine.UIElements.RegisterUxmlCacheAttribute",
        "UnityEngine.UIElements.EventInterestAttribute",
        "UnityEngine.UIElements.ToggleButtonGroupStatePropertiesAttribute",
        "UnityEngine.UIElements.PanelInputConfiguration",
        "UnityEngine.UIElements.PostProcessingPass",
        "UnityEngine.UIElements.KeyboardNavigationManipulator",
        "UnityEngine.UIElements.MouseManipulator",
        "UnityEngine.UIElements.PointerManipulator",
        "UnityEngine.UIElements.BaseListViewController",
        "UnityEngine.UIElements.ListViewController",
        "UnityEngine.UIElements.BaseTreeViewController",
        "UnityEngine.UIElements.TreeViewController",
        "UnityEngine.UIElements.DefaultTreeViewController",
        "UnityEngine.UIElements.MultiColumnController",
        "UnityEngine.UIElements.MultiColumnListViewController",
        "UnityEngine.UIElements.MultiColumnTreeViewController",
        "UnityEngine.UIElements.DefaultMultiColumnTreeViewController",
        "UnityEngine.UIElements.Uxml",
        "UnityEngine.UIElements.IUxml",
        "UnityEngine.UIElements.CreationContext",
        "UnityEngine.TextEditor",
        "UnityEngine.UI.CanvasUpdateRegistry",
        "UnityEngine.UI.GraphicRegistry",
        "UnityEngine.UI.ClipperRegistry",
        "UnityEngine.UI.StencilMaterial",
        "UnityEngine.UI.DefaultControls",
        "UnityEngine.UI.FontUpdateTracker",
        "UnityEngine.UI.GraphicRebuildTracker",
        "UnityEngine.UI.BaseVertexEffect",
        "UnityEngine.UI.IMask",
        "UnityEngine.UI.IMaterialModifier",
        "UnityEngine.LightmapData",
        "UnityEngine.LightmapSettings",
        "UnityEngine.U2D.SpriteAtlas",
        "UnityEngine.U2D.SpriteAtlasManager",
        "UnityEngine.U2D.SpriteBone",
        "UnityEngine.U2D.SpriteDataAccessExtensions",
        "UnityEngine.U2D.SpriteRendererDataAccessExtensions",
        "UnityEngine.Application.AdvertisingIdentifierCallback",
        "UnityEngine.Application.LowMemoryCallback",
        "UnityEngine.Application.MemoryUsageChangedCallback",
        "UnityEngine.Display.DisplaysUpdatedDelegate",
        "UnityEngine.SortingLayer.LayerCallback",
        "UnityEngine.Rendering.BatchRendererGroup.OnFinishedCulling",
        "UnityEngine.Rendering.BatchRendererGroup.OnPerformCulling",
        "UnityEngine.Awaitable.AwaitableAsyncMethodBuilder",
        "UnityEngine.BackgroundThreadAwaitable",
        "UnityEngine.MainThreadAwaitable",
        "UnityEngine.Video",
        "UnityEngine.BuildCompression",
        "UnityEngine.Caching",
        "UnityEngine.Cache",
        "UnityEngine.Jobs",
        "UnityEngine.Profiling.Recorder",
        "UnityEngine.Profiling.Sampler",
        "UnityEngine.Profiling.CustomSampler",
        "UnityEngine.Scripting.GarbageCollector",
        "UnityEngine.Ping",
        "UnityEngine.TrackedReference",
        "UnityEngine.Rendering.SplashScreen",
        "UnityEngine.Rendering.Watermark",
        "UnityEngine.TouchScreenKeyboard",
        "UnityEngine.Camera.CameraCallback",
        "UnityEngine.RectTransform.ReapplyDrivenProperties",
        "UnityEngine.Camera.GateFitParameters",
        "UnityEngine.Mesh.LodSelectionCurve",
        "UnityEngine.LowLevel.PlayerLoop",
        "UnityEngine.LowLevel.PlayerLoopSystem",
        "UnityEngine.PlayerLoop",
        "UnityEngine.Rendering.CommandBuffer",
        "UnityEngine.ComputeShader",
        "UnityEngine.ComputeBuffer",
        "UnityEngine.GraphicsBuffer.IndirectDrawArgs",
        "UnityEngine.GraphicsBuffer.IndirectDrawIndexedArgs",
        "UnityEngine.GraphicsBuffer",
        "UnityEngine.GraphicsBufferHandle",
        "UnityEngine.Rendering.RayTracingShader",
        "UnityEngine.Rendering.MachineLearningContext",
        "UnityEngine.Rendering.MachineLearningOperator",
        "UnityEngine.Rendering.MachineLearningOperatorDispatcher",
        "UnityEngine.Rendering.MachineLearningOperatorFactory.ConvDescriptor",
        "UnityEngine.Rendering.MachineLearningOperatorFactory.GemmDescriptor",
        "UnityEngine.Rendering.MachineLearningOperatorFactory.IdentityDescriptor",
        "UnityEngine.Rendering.MachineLearningOperatorFactory.ReduceDescriptor",
        "UnityEngine.Rendering.MachineLearningOperatorFactory",
        "UnityEngine.Rendering.MachineLearningTensorDescriptor",
        "UnityEngine.Rendering.MachineLearningTensorShape",
        "UnityEngine.LightProbesQuery",
        "UnityEngine.VulkanDeviceFilterData",
        "UnityEngine.CullingGroup",
        "UnityEngine.CullingGroupEvent",
        "UnityEngine.BillboardAsset",
        "UnityEngine.Projector",
        "UnityEngine.LightProbeProxyVolume",
        "UnityEngine.LensFlare",
        "UnityEngine.LightProbeGroup",
        "UnityEngine.OcclusionArea",
        "UnityEngine.SparseTexture",
        "UnityEngine.TextureMipmapLimitGroups",
        "UnityEngine.VulkanDeviceFilterLists",
        "UnityEngine.Rendering.ShaderKeyword",
        "UnityEngine.Rendering.LocalKeyword",
        "UnityEngine.Rendering.GlobalKeyword",
        "UnityEngine.Rendering.LocalKeywordSpace",
        "UnityEngine.Rendering.ShaderKeywordSet",
        "UnityEngine.Rendering.PlatformKeywordSet",
        "UnityEngine.ShaderVariantCollection.ShaderVariant",
        "UnityEngine.ShaderVariantCollection",
        "UnityEngine.Rendering.RayTracingAccelerationStructure.BuildSettings",
        "UnityEngine.Rendering.RayTracingAccelerationStructure.RASSettings",
        "UnityEngine.Rendering.RayTracingAccelerationStructure.Settings",
        "UnityEngine.Rendering.RayTracingAccelerationStructure",
        "UnityEngine.Rendering.AsyncGPUReadback",
        "UnityEngine.Rendering.ScriptableRenderContext",
        "UnityEngine.Rendering.IRenderPipelineGraphicsSettings",
        "UnityEngine.Rendering.IRenderPipelineResources",
        "UnityEngine.Rendering.RenderPipelineGlobalSettings",
        "UnityEngine.Rendering.RenderPipelineGraphicsSettingsCollection",
        "UnityEngine.Rendering.RenderPipelineGraphicsSettingsExtensions",
        "UnityEngine.Rendering.SupportedOnRenderPipelineAttribute",
        "UnityEngine.Rendering.RecreatePipelineOnChangeAttribute",
        "UnityEngine.Rendering.ResourcePathsBaseAttribute",
        "UnityEngine.Rendering.ResourceFormattedPathsAttribute",
        "UnityEngine.Rendering.ResourcePathAttribute",
        "UnityEngine.Rendering.ResourcePathsAttribute",
        "UnityEngine.Rendering.ScopedRenderPass",
        "UnityEngine.Rendering.ScopedSubPass",
        "UnityEngine.Rendering.CommandBufferExtensions",
        "UnityEngine.Rendering.RendererList",
        "UnityEngine.Rendering.LoadStoreActionDebugModeSettings",
        "UnityEngine.LightingSettings",
        "UnityEngine.Rendering.SupportedRenderingFeatures",
        "UnityEngine.GL",
        "UnityEngine.Gizmos",
        "UnityEngine.ReflectionProbe",
        "UnityEngine.LightProbes",
        "UnityEngine.Rendering.BatchRendererGroup",
        "UnityEngine.Rendering.BatchCullingContext",
        "UnityEngine.Rendering.BatchCullingOutput",
        "UnityEngine.Rendering.BatchDrawCommand",
        "UnityEngine.Rendering.BatchDrawCommandIndirect",
        "UnityEngine.Rendering.BatchDrawCommandProcedural",
        "UnityEngine.Rendering.BatchDrawCommandProceduralIndirect",
        "UnityEngine.Rendering.BatchDrawRange",
        "UnityEngine.Rendering.BatchRendererGroupCreateInfo",
        "UnityEngine.Rendering.CullingSplit",
        "UnityEngine.Rendering.LightShadowCasterCullingInfo",
        "UnityEngine.Rendering.ShadowCastersCullingInfos",
        "UnityEngine.Rendering.SubPassDescriptor",
        "UnityEngine.Rendering.ScriptableCullingParameters",
        "UnityEngine.Rendering.CullingResults",
        "UnityEngine.Rendering.DrawingSettings",
        "UnityEngine.Rendering.FilteringSettings",
        "UnityEngine.Rendering.ShadowDrawingSettings",
        "UnityEngine.Rendering.AttachmentDescriptor",
        "UnityEngine.Rendering.RenderTargetIdentifier",
        "UnityEngine.Rendering.RendererUtils.RendererListDesc",
        "UnityEngine.Rendering.RendererListParams",
        "UnityEngine.Rendering.RayTracingGeometryInstanceConfig",
        "UnityEngine.Rendering.RayTracingMeshInstanceConfig",
        "UnityEngine.Rendering.RayTracingMultiGeometryInstanceConfig",
        "UnityEngine.Rendering.RayTracingAABBsInstanceConfig",
        "UnityEngine.Rendering.BatchCullingOutputDrawCommands",
        "UnityEngine.RenderParams",
        "UnityEngine.Rendering.RenderPipelineManager",
        "UnityEngine.Rendering.RenderPipeline.StandardRequest",
        "UnityEngine.Rendering.RenderPipeline",
        "UnityEngine.Rendering.StencilState",
        "UnityEngine.Rendering.BlendState",
        "UnityEngine.Rendering.RenderTargetBlendState",
        "UnityEngine.Rendering.RasterState",
        "UnityEngine.Rendering.RenderStateBlock",
        "UnityEngine.Rendering.DepthState",
        "UnityEngine.Rendering.VisibleLight",
        "UnityEngine.Rendering.VisibleReflectionProbe",
        "UnityEngine.Rendering.ShadowSplitData",
        "UnityEngine.Rendering.RenderQueueRange",
        "UnityEngine.Rendering.SortingSettings",
        "UnityEngine.Rendering.AsyncGPUReadbackRequest",
        "UnityEngine.Rendering.RenderTargetBinding",
        "UnityEngine.Rendering.CameraProperties",
        "UnityEngine.Rendering.LODParameters",
        "UnityEngine.Rendering.ShaderTagId",
        "UnityEngine.Rendering.BatchFilterSettings",
        "UnityEngine.Rendering.AttachmentIndexArray",
        "UnityEngine.Rendering.BatchID",
        "UnityEngine.Rendering.BatchPackedCullingViewID",
        "UnityEngine.Rendering.BatchMaterialID",
        "UnityEngine.Rendering.BatchMeshID",
        "UnityEngine.Rendering.ThreadedBatchContext",
        "UnityEngine.Rendering.RayTracingInstanceCullingConfig",
        "UnityEngine.Rendering.RayTracingInstanceMaterialConfig",
        "UnityEngine.Rendering.RayTracingSubGeometryDesc",
        "UnityEngine.Rendering.GraphicsTextureDescriptor",
        "UnityEngine.Rendering.GraphicsTexture",
        "UnityEngine.Rendering.ShadingRateInfo",
        "UnityEngine.Rendering.ShadingRateImage",
        "UnityEngine.Rendering.ObjectIdRequest",
        "UnityEngine.Rendering.PIX",
        "UnityEngine.FrameTiming",
        "UnityEngine.FrameTimingManager",
        "UnityEngine.RenderTargetSetup",
        "UnityEngine.DynamicGI",
        "UnityEngine.HDROutputSettings",
        "UnityEngine.D3D12DeviceFilterData",
        "UnityEngine.D3D12DeviceFilterLists",
        "UnityEngine.D3D12GraphicsJobsDeviceFilterData",
        "UnityEngine.VulkanGraphicsJobsDeviceFilterData",
        "UnityEngine.Rendering.SphericalHarmonicsL2",
        "UnityEngine.Rendering.PassIdentifier",
        "UnityEngine.Rendering.OnDemandRendering",
        "UnityEngine.Rendering.RayTracingSubMeshFlagsConfig",
        "UnityEngine.Rendering.RayTracingInstanceCullingMaterialTest",
        "UnityEngine.Rendering.RayTracingInstanceCullingShaderTagConfig",
        "UnityEngine.Rendering.RayTracingInstanceCullingResults",
        "UnityEngine.Rendering.ObjectIdResult",
        "UnityEngine.Rendering.GPUFence",
    ];

    // Unity shader globals and keywords control Unity's shader execution.
    // They have no equivalent in the bounded Three.js material mapping.
    private static bool IsRendererBoundShaderMember(string name) =>
        name.StartsWith("SetGlobal", StringComparison.Ordinal)
        || name.StartsWith("GetGlobal", StringComparison.Ordinal)
        || name is "EnableKeyword" or "DisableKeyword" or "IsKeywordEnabled"
            or "SetKeyword" or "WarmupAllShaders" or "enabledGlobalKeywords"
            or "globalKeywords" or "globalMaximumLOD" or "globalRenderPipeline"
            or "globalShaderHardwareTier" or "maximumChunksOverride";

    private static readonly HashSet<string> RendererBoundPropertyBlockMembers = [
        "CopyProbeOcclusionArrayFrom", "CopySHCoefficientArraysFrom",
        "GetFloatArray", "SetFloatArray", "GetVectorArray", "SetVectorArray",
        "GetMatrix", "SetMatrix", "HasMatrix", "AddMatrix",
        "GetMatrixArray", "SetMatrixArray",
        "GetBuffer", "SetBuffer", "GetConstantBuffer", "SetConstantBuffer",
        "HasBuffer", "HasConstantBuffer",
    ];

    private static readonly HashSet<string> RendererBoundRenderTextureMembers = [
        "ApplyDynamicScale", "ConvertToEquirect", "DiscardContents", "GenerateMips",
        "GetNativeDepthBufferPtr", "GetTexelOffset", "MarkRestoreExpected",
        "ResolveAntiAliasedSurface", "SetBorderColor", "SetGlobalShaderProperty",
        "SupportsStencil", "antiAliasing", "autoGenerateMips", "bindTextureMS",
        "colorBuffer", "depthBuffer", "depthStencilFormat", "descriptor", "dimension",
        "enableRandomWrite", "enableShadingRate", "enabled", "generateMips",
        "graphicsFormat", "isCubemap", "isPowerOfTwo", "isVolume",
        "memorylessMode", "stencilFormat", "useDynamicScale",
        "useDynamicScaleExplicit", "useMipMap", "volumeDepth", "vrUsage",
    ];

    private static readonly HashSet<string> RendererBoundLightMembers = [
        "AddCommandBuffer", "AddCommandBufferAsync", "GetCommandBuffers",
        "RemoveAllCommandBuffers", "RemoveCommandBuffer", "RemoveCommandBuffers",
        "Reset", "SetLightDirty", "GetLights", "alreadyLightmapped", "areaSize", "attenuate",
        "bakedIndex", "bakingOutput", "bounceIntensity", "boundingSphereOverride",
        "commandBufferCount", "cookie", "cookieSize", "cookieSize2D", "dilatedRange",
        "colorTemperature", "useColorTemperature", "cullingMask", "renderingLayerMask",
        "enableSpotReflector", "flare", "forceVisible", "isBaked",
        "layerShadowCullDistances", "lightShadowCasterMode", "lightUnit",
        "lightmapBakeType", "lightmappingMode", "luxAtDistance", "pixelLightCount",
        "renderMode", "shadowAngle", "shadowBias", "shadowConstantBias",
        "shadowCustomResolution", "shadowMatrixOverride", "shadowNearPlane",
        "shadowNormalBias", "shadowObjectSizeBias", "shadowRadius",
        "shadowResolution", "shadowSoftness", "shadowSoftnessFade",
        "shadowStrength", "shadows", "shape", "useBoundingSphereOverride",
        "useShadowMatrixOverride", "useViewFrustumForShadowCasterCull",
    ];

    private static readonly HashSet<string> RendererBoundMeshMembers = [
        "AcquireReadOnlyMeshData", "AllocateWritableMeshData",
        "ApplyAndDisposeWritableMeshData", "GetIndexBuffer", "GetVertexBuffer",
        "GetNativeIndexBufferPtr", "GetNativeVertexBufferPtr",
        "GetBlendShapeBuffer", "GetBoneWeightBuffer", "GetBlendShapeBufferRange",
        "SetIndexBufferData", "SetVertexBufferData", "SetIndexBufferParams",
        "SetVertexBufferParams", "indexBufferTarget", "vertexBufferTarget",
        "vertexBufferCount", "OptimizeIndexBuffers", "OptimizeReorderVertexBuffer",
        "Optimize", "MarkModified", "RecalculateUVDistributionMetric",
        "RecalculateUVDistributionMetrics", "GetUVDistributionMetric",
        "SetLods", "GetLods", "SetLod", "GetLod", "lodSelectionCurve",
        "lodCount", "skinWeightBufferLayout",
    ];

    private static readonly HashSet<string> RendererBoundLineRendererMembers = [
        "BakeMesh", "alignment", "applyActiveColorSpace", "generateLightingData",
        "numCapVertices", "numCornerVertices", "shadowBias", "textureMode",
        "textureScale", "widthCurve",
    ];
    private static readonly HashSet<string> RendererBoundTrailRendererMembers = [
        "BakeMesh", "alignment", "applyActiveColorSpace",
        "generateLightingData", "numCapVertices", "numCornerVertices",
        "shadowBias", "textureMode", "textureScale", "widthCurve",
    ];
    private static readonly HashSet<string> RendererBoundMeshRendererMembers = [
        "GetShaderUserValue", "SetShaderUserValue", "additionalVertexStreams",
        "enlightenVertexStream", "globalIlluminationMeshLod", "receiveGI",
        "scaleInLightmap", "stitchLightmapSeams", "subMeshStartIndex",
    ];

    private static readonly HashSet<string> RendererBoundRendererMembers = [
        "GetClosestReflectionProbes", "LODGroup", "allowOcclusionWhenDynamic",
        "forceMeshLod", "isLOD0", "isPartOfStaticBatch", "isVisible",
        "lightProbeAnchor", "lightProbeProxyVolumeOverride", "lightProbeUsage",
        "lightmapIndex", "lightmapScaleOffset", "lightmapTilingOffset",
        "meshLodSelectionBias", "motionVectorGenerationMode", "motionVectors",
        "probeAnchor", "rayTracingAccelerationStructureBuildFlags",
        "rayTracingAccelerationStructureBuildFlagsOverride", "rayTracingMode",
        "realtimeLightmapIndex", "realtimeLightmapScaleOffset",
        "reflectionProbeUsage", "rendererPriority", "renderingLayerMask",
        "staticShadowCaster", "useLightProbes",
    ];

    private static readonly HashSet<string> RendererBoundSkinnedMembers = [
        "GetPreviousVertexBuffer", "GetShaderUserValue", "GetVertexBuffer",
        "SetShaderUserValue", "skinnedMotionVectors", "vertexBufferTarget",
    ];

    private static readonly HashSet<string> RendererBoundCameraMembers = [
        "onPostRender", "onPreCull", "onPreRender", "AddCommandBuffer",
        "AddCommandBufferAsync", "GetCommandBuffers", "RemoveAllCommandBuffers",
        "RemoveCommandBuffer", "RemoveCommandBuffers",
        "CopyStereoDeviceProjectionMatrixToNonJittered", "DoClear",
        "GetStereoNonJitteredProjectionMatrix", "GetStereoProjectionMatrices",
        "GetStereoProjectionMatrix", "GetStereoViewMatrices", "GetStereoViewMatrix",
        "RenderDontRestore", "RenderToCubemap", "RenderWithShader",
        "ResetReplacementShader", "ResetStereoProjectionMatrices", "ResetStereoViewMatrices",
        "SetReplacementShader", "SetStereoProjectionMatrices",
        "SetStereoProjectionMatrix", "SetStereoViewMatrices", "SetStereoViewMatrix",
        "SetTargetBuffers", "SetupCurrent", "SubmitRenderRequest",
        "TryGetCullingParameters", "activeTexture", "actualRenderingPath",
        "allowDynamicResolution", "allowHDR", "allowMSAA", "hdr",
        "areVRStereoViewMatricesWithinSingleCullTolerance",
        "clearStencilAfterLightingPass", "commandBufferCount", "depthTextureMode",
        "eventMask", "forceIntoRenderTexture", "layerCullDistances", "scene",
        "layerCullSpherical",
        "worldToCameraMatrix", "nonJitteredProjectionMatrix", "opaqueSortMode",
        "overrideSceneCullingMask", "previousViewProjectionMatrix",
        "renderCloudsInSceneView", "renderingPath", "sceneViewFilterMode",
        "stereoActiveEye", "stereoConvergence", "stereoEnabled",
        "stereoMirrorMode", "stereoSeparation", "stereoTargetEye",
        "transparencySortAxis",
        "useJitteredProjectionMatrixForTransparentRendering", "useOcclusionCulling",
        "velocity", "CalculateProjectionMatrixFromPhysicalProperties",
        "FieldOfViewToFocalLength", "FocalLengthToFieldOfView",
        "GetGateFittedFieldOfView", "GetGateFittedLensShift",
        "anamorphism", "aperture", "barrelClipping", "bladeCount", "curvature",
        "focalLength", "focusDistance", "gateFit", "iso",
        "shutterSpeed", "kMaxAperture",
        "kMaxBladeCount", "kMinAperture", "kMinBladeCount",
        "CalculateObliqueMatrix", "ResetCullingMatrix",
        "ResetTransparencySortSettings", "ResetWorldToCameraMatrix",
        "cullingMatrix", "isProcessingRenderRequest",
    ];

    private static readonly HashSet<string> RendererBoundMaterialMembers = [
        "ApplyPropertyOverride", "ComputeCRC", "Create", "FindPass",
        "GetPassName", "GetShaderPassEnabled", "GetTag", "SetOverrideTag",
        "SetPass", "SetShaderPassEnabled", "passCount",
        "GetBuffer", "SetBuffer", "HasBuffer", "GetConstantBuffer",
        "SetConstantBuffer", "HasConstantBuffer",
        "GetColorArray", "GetFloatArray", "GetVectorArray", "GetMatrix",
        "GetMatrixArray", "SetColorArray", "SetFloatArray", "SetVectorArray",
        "SetMatrix", "SetMatrixArray",
        "GetPropertyNames", "GetTexturePropertyNameIDs", "GetTexturePropertyNames",
        "IsChildOf", "IsPropertyLocked", "IsPropertyLockedByAncestor",
        "IsPropertyOverriden", "RevertAllPropertyOverrides",
        "RevertPropertyOverride", "SetPropertyLock", "parent", "isVariant",
        "doubleSidedGI", "enableInstancing", "enabledKeywords",
        "globalIlluminationFlags", "rawRenderQueue", "shaderKeywords",
        "SetKeyword",
    ];

    private static readonly HashSet<string> RendererBoundTextureMembers = [
        "GenerateAllMips", "GetNativeTextureID", "GetNativeTexturePtr",
        "IncrementUpdateCount", "SetGlobalAnisotropicFilteringLimits",
        "SetStreamingTextureMaterialDebugProperties",
        "allowThreadedTextureCreation", "anisotropicFiltering",
        "currentTextureMemory", "desiredTextureMemory",
        "globalMipmapLimit", "graphicsTexture", "graphicsFormat", "mipMapBias",
        "masterTextureLimit",
        "nonStreamingTextureCount", "nonStreamingTextureMemory",
        "streamingMipmapUploadCount", "streamingRendererCount",
        "streamingTextureCount", "streamingTextureDiscardUnusedMips",
        "streamingTextureForceLoadAll", "streamingTextureLoadingCount",
        "streamingTexturePendingLoadCount", "targetTextureMemory",
        "totalTextureMemory", "updateCount",
    ];

    private static readonly HashSet<string> RendererBoundTexture2DMembers = [
        "Compress", "CreateExternalTexture", "UpdateExternalTexture",
    ];

    private static readonly HashSet<string> HostProfilerMembers = [
        "AddFramesFromFile", "BeginThreadProfiling", "EmitFrameMetaData",
        "EmitSessionMetaData", "EndThreadProfiling", "GetAllCategories",
        "GetAllocatedMemoryForGraphicsDriver", "GetAreaEnabled", "GetCategoriesCount",
        "GetMonoHeapSize", "GetMonoHeapSizeLong", "GetMonoUsedSize",
        "GetMonoUsedSizeLong", "GetRuntimeMemorySize", "GetRuntimeMemorySizeLong",
        "GetTempAllocatorSize", "GetTotalAllocatedMemory", "GetTotalAllocatedMemoryLong",
        "GetTotalFragmentationInfo", "GetTotalReservedMemory",
        "GetTotalReservedMemoryLong", "GetTotalUnusedReservedMemory",
        "GetTotalUnusedReservedMemoryLong", "IsCategoryEnabled", "SetAreaEnabled",
        "SetCategoryEnabled", "SetScreenshotCaptureFrameInterval",
        "SetTempAllocatorRequestedSize", "areaCount", "enableAllocationCallstacks",
        "enableBinaryLog", "enabled", "logFile", "maxNumberOfSamplesPerFrame",
        "maxUsedMemory", "supported", "usedHeapSize", "usedHeapSizeLong",
    ];

    private static readonly HashSet<string> HostSystemInfoMembers = [
        "batteryLevel", "batteryStatus", "deviceName", "deviceUniqueIdentifier",
        "graphicsDeviceID", "graphicsDeviceVendorID", "graphicsMemorySize",
        "graphicsPixelFillrate", "operatingSystem", "operatingSystemFamily",
        "processorFrequency", "processorManufacturer", "processorModel",
        "processorType", "systemMemorySize", "processorCount",
        "supportsAccelerometer", "supportsAudio", "supportsGyroscope",
        "supportsLocationService", "supportsVibration",
    ];

    private static readonly HashSet<string> RendererSystemInfoMembers = [
        "computeSubGroupSize", "constantBufferOffsetAlignment", "foveatedRenderingCaps",
        "maxComputeBufferInputsCompute", "maxComputeBufferInputsDomain",
        "maxComputeBufferInputsFragment", "maxComputeBufferInputsGeometry",
        "maxComputeBufferInputsHull", "maxComputeBufferInputsVertex",
        "maxComputeWorkGroupSize", "maxComputeWorkGroupSizeX",
        "maxComputeWorkGroupSizeY", "maxComputeWorkGroupSizeZ",
        "maxConstantBufferSize", "maxGraphicsBufferSize", "supportedRandomWriteTargetCount",
        "supportsAsyncCompute", "supportsComputeShaders", "supportsConservativeRaster",
        "supportsDepthFetchInRenderPass", "supportsGeometryShaders", "supportsGPUFence",
        "supportsGraphicsFence", "supportsIndirectArgumentsBuffer",
        "supportsIndirectDispatchRays", "supportsInlineRayTracing",
        "supportsParallelPSOCreation", "supportsRayTracing", "supportsRayTracingShaders",
        "supportsRenderTargetArrayIndexFromVertexShader", "supportsSetConstantBuffer",
        "supportsTessellationShaders", "supportsVariableRateShading",
        "supportsMachineLearning", "supportsRendering",
        "copyTextureSupport", "GetRenderTextureSupportedMSAASampleCount",
        "graphicsDeviceName", "graphicsDeviceType", "graphicsDeviceVendor",
        "graphicsDeviceVersion", "graphicsMultiThreaded", "graphicsShaderLevel",
        "graphicsUVStartsAtTop", "hasDynamicUniformArrayIndexingInFragmentShaders",
        "hasHiddenSurfaceRemovalOnGPU", "hasMipMaxLevel", "hasTiledGPU",
        "hdrDisplaySupportFlags", "maxAnisotropyLevel", "maxCubemapSize",
        "maxTexture3DSize", "maxTextureArraySlices", "maxTextureSize",
        "maxTiledPixelStorageSize", "minConstantBufferOffsetAlignment",
        "npotSupport", "renderingThreadingMode", "supportedRenderTargetCount",
        "supports2DArrayTextures", "supports32bitsIndexBuffer",
        "supports3DRenderTextures", "supports3DTextures", "supportsAnisotropicFilter",
        "supportsAsyncGPUReadback", "SupportsBlendingOnRenderTextureFormat",
        "supportsCompressed3DTextures", "supportsCubemapArrayTextures",
        "supportsDynamicResolution", "supportsGpuRecorder",
        "supportsHardwareQuadTopology", "supportsImageEffects", "supportsInstancing",
        "supportsMemorylessTextures", "supportsMipStreaming", "supportsMotionVectors",
        "supportsMultisampleAutoResolve", "supportsMultisampled2DArrayTextures",
        "supportsMultisampledBackBuffer", "supportsMultisampledShaderResolve",
        "supportsMultisampledTextures", "supportsMultisampleResolveDepth",
        "supportsMultisampleResolveStencil", "supportsMultiview",
        "SupportsRandomWriteOnRenderTextureFormat", "supportsRawShadowDepthSampling",
        "SupportsRenderTextureFormat", "supportsRenderTextures", "supportsRenderToCubemap",
        "supportsSeparatedRenderTargetsBlend", "supportsShadows", "supportsSparseTextures",
        "supportsStencil", "supportsStoreAndResolveAction", "SupportsTextureFormat",
        "supportsTextureWrapMirrorOnce", "SupportsVertexAttributeFormat",
        "supportsVertexPrograms", "usesLoadStoreActions", "usesReversedZBuffer",
    ];

    private static readonly HashSet<string> HostScreenMembers = [
        "GetDisplayLayout", "MoveMainWindowTo", "mainWindowDisplayInfo",
        "mainWindowPosition", "brightness", "sleepTimeout", "dpi", "lockCursor",
        "SetMSAASamples", "msaaSamples",
        "orientation", "autorotateToLandscapeLeft", "autorotateToLandscapeRight",
        "autorotateToPortrait", "autorotateToPortraitUpsideDown",
    ];
    // Native licensing, authorization, install provenance and process crash controls.
    private static readonly HashSet<string> HostApplicationMembers = [
        "ForceCrash", "HasProLicense", "HasUserAuthorization",
        "RequestUserAuthorization", "RequestAdvertisingIdentifierAsync",
        "installMode", "installerName", "genuine", "genuineCheckAvailable",
        "CaptureScreenshot", "ExternalCall", "ExternalEval", "Unload",
        "consoleLogPath", "buildGUID", "cloudProjectId",
        "exitCancellationToken", "sandboxType", "webSecurityEnabled",
        "backgroundLoadingPriority",
        "deepLinkActivated", "lowMemory", "memoryUsageChanged",
        "onBeforeRender", "unloading",
    ];
    private static readonly HashSet<string> HostResourcesMembers = [
        "GetBuiltinResource", "UnloadUnusedAssets",
    ];

    private static readonly HashSet<string> HostDisplayMembers = [
        "onDisplaysUpdated", "Activate", "MultiDisplayLicense", "RelativeMouseAt",
        "SetParams", "SetRenderingResolution", "activeEditorGameViewTarget",
        "colorBuffer", "depthBuffer", "requiresBlitToBackbuffer",
        "requiresSrgbBlitToBackbuffer",
    ];

    private static readonly HashSet<string> RendererQualityPipelineMembers = [
        "GetAllRenderPipelineAssetsForPlatform", "GetRenderPipelineAssetAt",
        "GetRenderPipelineAssetsForPlatform", "renderPipeline",
    ];

    private static readonly HashSet<string> RendererQualityGpuMembers = [
        "GetTextureMipmapLimitSettings", "SetLODSettings",
        "SetTextureMipmapLimitSettings", "activeColorSpace",
        "anisotropicFiltering", "antiAliasing", "asyncUploadBufferSize",
        "asyncUploadPersistentBuffer", "asyncUploadTimeSlice",
        "billboardsFaceCameraPosition", "blendWeights", "desiredColorSpace",
        "enableLODCrossFade", "globalTextureMipmapLimit", "lodBias",
        "masterTextureLimit", "maxQueuedFrames", "maximumLODLevel",
        "meshLodThreshold", "particleRaycastBudget", "pixelLightCount",
        "realtimeGICPUUsage", "realtimeReflectionProbes",
        "resolutionScalingFixedDPIFactor", "shadowCascade2Split",
        "shadowCascade4Split", "shadowCascades", "shadowDistance",
        "shadowNearPlaneOffset", "shadowProjection", "shadowResolution",
        "shadowmaskMode", "shadows", "skinWeights", "softParticles",
        "softVegetation", "streamingMipmapsActive",
        "streamingMipmapsAddAllCameras", "streamingMipmapsMaxFileIORequests",
        "streamingMipmapsMaxLevelReduction", "streamingMipmapsMemoryBudget",
        "streamingMipmapsRenderersPerFrame", "terrainBasemapDistance",
        "terrainBillboardStart", "terrainDetailDensityScale",
        "terrainDetailDistance", "terrainFadeLength", "terrainMaxTrees",
        "terrainPixelError", "terrainQualityOverrides",
        "terrainTreeDistance", "useLegacyDetailDistribution",
    ];

    private static readonly HashSet<string> RendererEnvironmentMembers = [
        "ambientEquatorColor", "ambientGroundColor", "ambientIntensity",
        "ambientLight", "ambientMode", "ambientProbe", "ambientSkyColor",
        "ambientSkyboxAmount", "customReflection", "customReflectionTexture",
        "defaultReflectionMode", "defaultReflectionResolution",
        "flareFadeSpeed", "flareStrength", "haloStrength",
        "reflectionBounces", "reflectionIntensity", "skybox",
        "subtractiveShadowColor", "sun",
    ];

    private static readonly HashSet<string> HostQualityProfileMembers = [
        "activeQualityLevelRenamed", "GetActiveQualityLevelsForPlatform",
        "GetActiveQualityLevelsForPlatformCount", "GetQualitySettings",
        "IsPlatformIncluded", "TryExcludePlatformAt", "TryIncludePlatformAt",
    ];

    private static bool IsGraphicsBufferEnum(SyntaxNode node) =>
        node.ToString().Replace("global::", "", StringComparison.Ordinal) is var name
        && (name.StartsWith("UnityEngine.GraphicsBuffer.Target", StringComparison.Ordinal)
            || name.StartsWith("UnityEngine.GraphicsBuffer.UsageFlags", StringComparison.Ordinal)
            || name.StartsWith("GraphicsBuffer.Target", StringComparison.Ordinal)
            || name.StartsWith("GraphicsBuffer.UsageFlags", StringComparison.Ordinal)
            || (node.Parent is QualifiedNameSyntax parent && parent.Left == node
                && parent.Right.Identifier.ValueText is "Target" or "UsageFlags"
                && (name == "GraphicsBuffer" || name == "UnityEngine.GraphicsBuffer"))
            || (node.Parent is MemberAccessExpressionSyntax member && member.Expression == node
                && member.Name.Identifier.ValueText is "Target" or "UsageFlags"
                && (name == "GraphicsBuffer" || name == "UnityEngine.GraphicsBuffer"))
            || (name == "GraphicsBuffer"
                && node.Parent is MemberAccessExpressionSyntax qualified
                && qualified.Name == node && qualified.Expression.ToString() == "UnityEngine"
                && qualified.Parent is MemberAccessExpressionSyntax nested
                && nested.Expression == qualified
                && nested.Name.Identifier.ValueText is "Target" or "UsageFlags"));

    private static readonly string[] PreservedRendererNestedEnums = [
        "UnityEngine.Scripting.GarbageCollector.Mode",
        "UnityEngine.Rendering.SplashScreen.StopBehavior",
        "UnityEngine.TouchScreenKeyboard.InputFieldAppearance",
        "UnityEngine.TouchScreenKeyboard.Status",
        "UnityEngine.LightingSettings.DenoiserType",
        "UnityEngine.LightingSettings.FilterMode",
        "UnityEngine.LightingSettings.FilterType",
        "UnityEngine.LightingSettings.LightmapPackingMethod",
        "UnityEngine.LightingSettings.LightmapPackingMode",
        "UnityEngine.LightingSettings.Lightmapper",
        "UnityEngine.LightingSettings.Sampling",
        "UnityEngine.Rendering.SupportedRenderingFeatures.LightmapMixedBakeModes",
        "UnityEngine.Rendering.SupportedRenderingFeatures.ReflectionProbeModes",
        "UnityEngine.Rendering.RayTracingAccelerationStructure.ManagementMode",
        "UnityEngine.Rendering.RayTracingAccelerationStructure.RayTracingModeMask",
        "UnityEngine.LightProbeProxyVolume.BoundingBoxMode",
        "UnityEngine.LightProbeProxyVolume.DataFormat",
        "UnityEngine.LightProbeProxyVolume.ProbePositionMode",
        "UnityEngine.LightProbeProxyVolume.QualityMode",
        "UnityEngine.LightProbeProxyVolume.RefreshMode",
        "UnityEngine.LightProbeProxyVolume.ResolutionMode",
        "UnityEngine.ReflectionProbe.ReflectionProbeEvent",
        "UnityEngine.Rendering.SupportedOnRenderPipelineAttribute.SupportedMode",
    ];

    private static bool IsPreservedRendererEnum(SyntaxNode node)
    {
        if (IsGraphicsBufferEnum(node)) return true;
        static bool IsEnumField(MemberAccessExpressionSyntax access) =>
            access.Name.Identifier.ValueText == "LensFlare"
                && access.Expression.ToString().EndsWith("BuiltinShaderType", StringComparison.Ordinal)
            || access.Name.Identifier.ValueText == "ComputeBuffer"
                && access.Expression.ToString().EndsWith("MaterialPropertyType", StringComparison.Ordinal)
            || access.Name.Identifier.ValueText == "LightProbes"
                && access.Expression.ToString().EndsWith("ReceiveGI", StringComparison.Ordinal);
        if (node is MemberAccessExpressionSyntax enumAccess && IsEnumField(enumAccess)) return true;
        if (node is IdentifierNameSyntax enumField
            && enumField.Parent is MemberAccessExpressionSyntax access
            && access.Name == enumField && IsEnumField(access)) return true;
        var name = node.ToString().Replace("global::", "", StringComparison.Ordinal);
        bool MatchesEnum(string value) => PreservedRendererNestedEnums.Any(full =>
            Matches(value, full) || Matches(value, full["UnityEngine.".Length..])
            || Matches(value, full[(full.LastIndexOf('.', full.LastIndexOf('.') - 1) + 1)..]));
        if (MatchesEnum(name)) return true;
        if (node.Parent is MemberAccessExpressionSyntax qualified
            && qualified.Name == node
            && qualified.Parent is MemberAccessExpressionSyntax nested
            && nested.Expression == qualified && MatchesEnum(nested.ToString())) return true;
        var child = node.Parent switch
        {
            QualifiedNameSyntax parent when parent.Left == node => parent.Right.Identifier.ValueText,
            MemberAccessExpressionSyntax parent when parent.Expression == node => parent.Name.Identifier.ValueText,
            _ => null,
        };
        return child != null && MatchesEnum(name + "." + child);
    }

    public static void RejectSyntax(SyntaxTree tree)
    {
        var root = tree.GetRoot();
        var imports = root.DescendantNodes().OfType<UsingDirectiveSyntax>()
            .Where(usingDirective => usingDirective.Alias == null
                && usingDirective.StaticKeyword == default)
            .Select(usingDirective => usingDirective.Name?.ToString().Replace("global::", "", StringComparison.Ordinal))
            .Where(name => name != null).ToArray();
        var localTypes = root.DescendantNodes().OfType<BaseTypeDeclarationSyntax>()
            .Select(declaration => declaration.Identifier.ValueText).ToHashSet(StringComparer.Ordinal);
        var aliases = root.DescendantNodes().OfType<UsingDirectiveSyntax>()
            .Where(usingDirective => usingDirective.Alias != null)
            .Select(usingDirective => usingDirective.Alias!.Name.Identifier.ValueText)
            .ToHashSet(StringComparer.Ordinal);
        var hostTypes = new Dictionary<string, (string full, string import, string reason)>(StringComparer.Ordinal) {
            ["Handheld"] = ("UnityEngine.Handheld", "UnityEngine",
                "native handheld controls are outside browser Preview"),
            ["PlayerConnection"] = ("UnityEngine.Networking.PlayerConnection.PlayerConnection",
                "UnityEngine.Networking.PlayerConnection",
                "Unity Editor-to-Player messaging is unavailable in browser Preview"),
            ["IEditorPlayerConnection"] = ("UnityEngine.Networking.PlayerConnection.IEditorPlayerConnection",
                "UnityEngine.Networking.PlayerConnection",
                "Unity Editor-to-Player messaging is unavailable in browser Preview"),
            ["SearchContext"] = ("UnityEngine.Search.SearchContextAttribute", "UnityEngine.Search",
                "Unity Inspector search metadata has no browser runtime effect"),
            ["SearchContextAttribute"] = ("UnityEngine.Search.SearchContextAttribute", "UnityEngine.Search",
                "Unity Inspector search metadata has no browser runtime effect"),
            ["CrashReport"] = ("UnityEngine.CrashReport", "UnityEngine",
                "native Unity crash reports are unavailable in browser Preview"),
            ["DisplayInfo"] = ("UnityEngine.DisplayInfo", "UnityEngine",
                "native display layout and physical DPI are outside the browser canvas contract"),
            ["Utils"] = ("UnityEngine.Diagnostics.Utils", "UnityEngine.Diagnostics",
                "native crash, heap, and diagnostic controls are outside browser Preview"),
            ["Arguments"] = ("UnityEngine.DedicatedServer.Arguments", "UnityEngine.DedicatedServer",
                "Unity dedicated-server command-line settings are unavailable in browser Preview"),
            ["LazyLoadReference"] = ("UnityEngine.LazyLoadReference", "UnityEngine",
                "Unity asset-import lazy loading is outside the browser asset catalog"),
        };
        var hostAliases = root.DescendantNodes().OfType<UsingDirectiveSyntax>()
            .Where(usingDirective => usingDirective.Alias != null)
            .Select(usingDirective => (name: usingDirective.Alias!.Name.Identifier.ValueText,
                target: usingDirective.Name?.ToString().Replace("global::", "", StringComparison.Ordinal)))
            .Where(item => hostTypes.Values.Any(host => host.full == item.target
                || host.full == "UnityEngine.LazyLoadReference"
                    && item.target?.StartsWith("UnityEngine.LazyLoadReference<", StringComparison.Ordinal) == true))
            .ToDictionary(item => item.name, item => hostTypes.Values.First(host => host.full == item.target
                || host.full == "UnityEngine.LazyLoadReference"
                    && item.target?.StartsWith("UnityEngine.LazyLoadReference<", StringComparison.Ordinal) == true),
                StringComparer.Ordinal);
        foreach (var usingDirective in root.DescendantNodes().OfType<UsingDirectiveSyntax>()) {
            if (usingDirective.StaticKeyword == default) continue;
            var target = usingDirective.Name?.ToString().Replace("global::", "", StringComparison.Ordinal);
            if (!hostTypes.Values.Any(host => host.full == target)) continue;
            var host = hostTypes.Values.First(candidate => candidate.full == target);
            var location = usingDirective.GetLocation().GetLineSpan();
            throw new InvalidOperationException($"{location.Path}:{location.StartLinePosition.Line + 1}:"
                + $"{location.StartLinePosition.Character + 1}: Preview defers {host.full}: {host.reason}");
        }
        foreach (var identifier in root.DescendantNodes().OfType<IdentifierNameSyntax>()) {
            if (identifier.Ancestors().OfType<UsingDirectiveSyntax>().Any()
                || identifier.Ancestors().OfType<MethodDeclarationSyntax>()
                    .Any(EditorCodePolicy.IsEditorOnlyMember)) continue;
            var name = identifier.Identifier.ValueText;
            var aliasMatch = hostAliases.TryGetValue(name, out var aliasHost);
            if (!aliasMatch && !hostTypes.TryGetValue(name, out aliasHost)) continue;
            // The nested enum is pure value data even though its declaring
            // class exposes native dedicated-server command-line settings.
            if (aliasHost.full == "UnityEngine.DedicatedServer.Arguments"
                && (identifier.Parent is MemberAccessExpressionSyntax direct
                    && direct.Expression == identifier
                    && direct.Name.Identifier.ValueText == "ArgumentErrorPolicy"
                    || identifier.Parent is MemberAccessExpressionSyntax enumOwner
                    && enumOwner.Name == identifier
                    && enumOwner.Parent is MemberAccessExpressionSyntax nested
                    && nested.Expression == enumOwner
                    && nested.Name.Identifier.ValueText == "ArgumentErrorPolicy")) continue;
            var receiver = identifier.Parent switch {
                QualifiedNameSyntax qualified when qualified.Right == identifier => qualified.Left.ToString(),
                MemberAccessExpressionSyntax member when member.Name == identifier => member.Expression.ToString(),
                _ => "",
            };
            receiver = receiver.Replace("global::", "", StringComparison.Ordinal);
            if (aliasMatch && receiver.Length != 0) continue;
            if (!aliasMatch && receiver != aliasHost.import
                && !(receiver.Length == 0 && imports.Contains(aliasHost.import)
                    && !localTypes.Contains(name) && !aliases.Contains(name))) continue;
            var location = identifier.GetLocation().GetLineSpan();
            throw new InvalidOperationException($"{location.Path}:{location.StartLinePosition.Line + 1}:"
                + $"{location.StartLinePosition.Character + 1}: Preview defers {aliasHost.full}: {aliasHost.reason}");
        }
        foreach (var generic in root.DescendantNodes().OfType<GenericNameSyntax>()) {
            if (generic.Identifier.ValueText != "LazyLoadReference"
                || generic.Ancestors().OfType<UsingDirectiveSyntax>().Any()
                || generic.Ancestors().OfType<MethodDeclarationSyntax>()
                    .Any(EditorCodePolicy.IsEditorOnlyMember)) continue;
            var receiver = generic.Parent switch {
                QualifiedNameSyntax qualified when qualified.Right == generic => qualified.Left.ToString(),
                MemberAccessExpressionSyntax member when member.Name == generic => member.Expression.ToString(),
                _ => "",
            };
            receiver = receiver.Replace("global::", "", StringComparison.Ordinal);
            if (receiver != "UnityEngine"
                && !(receiver.Length == 0 && imports.Contains("UnityEngine")
                    && !localTypes.Contains("LazyLoadReference")
                    && !aliases.Contains("LazyLoadReference"))) continue;
            var location = generic.GetLocation().GetLineSpan();
            throw new InvalidOperationException($"{location.Path}:{location.StartLinePosition.Line + 1}:"
                + $"{location.StartLinePosition.Character + 1}: Preview defers UnityEngine.LazyLoadReference<T>:"
                + " Unity asset-import lazy loading is outside the browser asset catalog");
        }
        var meshAliases = root.DescendantNodes().OfType<UsingDirectiveSyntax>()
            .Where(usingDirective => usingDirective.Alias != null
                && usingDirective.Name?.ToString().Replace("global::", "", StringComparison.Ordinal)
                    == "UnityEngine.Mesh")
            .Select(usingDirective => usingDirective.Alias!.Name.Identifier.ValueText)
            .ToHashSet(StringComparer.Ordinal);
        var meshDataAliases = root.DescendantNodes().OfType<UsingDirectiveSyntax>()
            .Where(usingDirective => usingDirective.Alias != null
                && usingDirective.Name?.ToString().Replace("global::", "", StringComparison.Ordinal)
                    is "UnityEngine.Mesh.MeshData" or "UnityEngine.Mesh.MeshDataArray")
            .ToDictionary(usingDirective => usingDirective.Alias!.Name.Identifier.ValueText,
                usingDirective => usingDirective.Name!.ToString().Replace("global::", "", StringComparison.Ordinal),
                StringComparer.Ordinal);
        var staticMeshImport = root.DescendantNodes().OfType<UsingDirectiveSyntax>()
            .Any(usingDirective => usingDirective.StaticKeyword != default
                && usingDirective.Name?.ToString().Replace("global::", "", StringComparison.Ordinal)
                    == "UnityEngine.Mesh");
        foreach (var identifier in root.DescendantNodes().OfType<IdentifierNameSyntax>())
        {
            if (identifier.Ancestors().OfType<UsingDirectiveSyntax>().Any()
                || identifier.Ancestors().OfType<MethodDeclarationSyntax>()
                    .Any(EditorCodePolicy.IsEditorOnlyMember)) continue;
            var name = identifier.Identifier.ValueText;
            var isAlias = meshDataAliases.TryGetValue(name, out var aliasType);
            if (!isAlias && name is not ("MeshData" or "MeshDataArray")) continue;
            var receiver = identifier.Parent switch
            {
                QualifiedNameSyntax qualified when qualified.Right == identifier => qualified.Left.ToString(),
                MemberAccessExpressionSyntax member when member.Name == identifier => member.Expression.ToString(),
                _ => "",
            };
            receiver = receiver.Replace("global::", "", StringComparison.Ordinal);
            var isMeshType = receiver == "UnityEngine.Mesh"
                || receiver == "Mesh" && imports.Contains("UnityEngine")
                    && !localTypes.Contains("Mesh") && !aliases.Contains("Mesh")
                || meshAliases.Contains(receiver);
            if (!isAlias && !isMeshType
                && !(receiver.Length == 0 && staticMeshImport && !localTypes.Contains(name)))
                continue;
            var typeName = isAlias ? aliasType : $"UnityEngine.Mesh.{name}";
            var location = identifier.GetLocation().GetLineSpan();
            throw new InvalidOperationException($"{location.Path}:{location.StartLinePosition.Line + 1}:"
                + $"{location.StartLinePosition.Character + 1}: Preview defers {typeName}:"
                + " native Unity MeshData and buffer layout are outside the Three.js mesh bridge");
        }
        var shaderAliases = root.DescendantNodes().OfType<UsingDirectiveSyntax>()
            .Where(usingDirective => usingDirective.Alias != null
                && usingDirective.Name?.ToString().Replace("global::", "", StringComparison.Ordinal)
                    == "UnityEngine.Shader")
            .Select(usingDirective => usingDirective.Alias!.Name.Identifier.ValueText)
            .ToHashSet(StringComparer.Ordinal);
        var profilerAliases = root.DescendantNodes().OfType<UsingDirectiveSyntax>()
            .Where(usingDirective => usingDirective.Alias != null
                && usingDirective.Name?.ToString().Replace("global::", "", StringComparison.Ordinal)
                    == "UnityEngine.Profiling.Profiler")
            .Select(usingDirective => usingDirective.Alias!.Name.Identifier.ValueText)
            .ToHashSet(StringComparer.Ordinal);
        var systemInfoAliases = root.DescendantNodes().OfType<UsingDirectiveSyntax>()
            .Where(usingDirective => usingDirective.Alias != null
                && usingDirective.Name?.ToString().Replace("global::", "", StringComparison.Ordinal)
                    is "UnityEngine.SystemInfo" or "UnityEngine.Device.SystemInfo")
            .Select(usingDirective => usingDirective.Alias!.Name.Identifier.ValueText)
            .ToHashSet(StringComparer.Ordinal);
        var screenAliases = root.DescendantNodes().OfType<UsingDirectiveSyntax>()
            .Where(usingDirective => usingDirective.Alias != null
                && usingDirective.Name?.ToString().Replace("global::", "", StringComparison.Ordinal)
                    is "UnityEngine.Screen" or "UnityEngine.Device.Screen")
            .Select(usingDirective => usingDirective.Alias!.Name.Identifier.ValueText)
            .ToHashSet(StringComparer.Ordinal);
        var applicationAliases = root.DescendantNodes().OfType<UsingDirectiveSyntax>()
            .Where(usingDirective => usingDirective.Alias != null
                && usingDirective.Name?.ToString().Replace("global::", "", StringComparison.Ordinal)
                    is "UnityEngine.Application" or "UnityEngine.Device.Application")
            .Select(usingDirective => usingDirective.Alias!.Name.Identifier.ValueText)
            .ToHashSet(StringComparer.Ordinal);
        var resourcesAliases = root.DescendantNodes().OfType<UsingDirectiveSyntax>()
            .Where(usingDirective => usingDirective.Alias != null
                && usingDirective.Name?.ToString().Replace("global::", "", StringComparison.Ordinal)
                    == "UnityEngine.Resources")
            .Select(usingDirective => usingDirective.Alias!.Name.Identifier.ValueText)
            .ToHashSet(StringComparer.Ordinal);
        var displayAliases = root.DescendantNodes().OfType<UsingDirectiveSyntax>()
            .Where(usingDirective => usingDirective.Alias != null
                && usingDirective.Name?.ToString().Replace("global::", "", StringComparison.Ordinal)
                    == "UnityEngine.Display")
            .Select(usingDirective => usingDirective.Alias!.Name.Identifier.ValueText)
            .ToHashSet(StringComparer.Ordinal);
        var staticScreenImport = root.DescendantNodes().OfType<UsingDirectiveSyntax>()
            .Any(usingDirective => usingDirective.StaticKeyword != default
                && usingDirective.Name?.ToString().Replace("global::", "", StringComparison.Ordinal)
                    is "UnityEngine.Screen" or "UnityEngine.Device.Screen");
        var staticApplicationImport = root.DescendantNodes().OfType<UsingDirectiveSyntax>()
            .Any(usingDirective => usingDirective.StaticKeyword != default
                && usingDirective.Name?.ToString().Replace("global::", "", StringComparison.Ordinal)
                    is "UnityEngine.Application" or "UnityEngine.Device.Application");
        var staticResourcesImport = root.DescendantNodes().OfType<UsingDirectiveSyntax>()
            .Any(usingDirective => usingDirective.StaticKeyword != default
                && usingDirective.Name?.ToString().Replace("global::", "", StringComparison.Ordinal)
                    == "UnityEngine.Resources");
        var staticDisplayImport = root.DescendantNodes().OfType<UsingDirectiveSyntax>()
            .Any(usingDirective => usingDirective.StaticKeyword != default
                && usingDirective.Name?.ToString().Replace("global::", "", StringComparison.Ordinal)
                    == "UnityEngine.Display");
        var qualityAliases = root.DescendantNodes().OfType<UsingDirectiveSyntax>()
            .Where(usingDirective => usingDirective.Alias != null
                && usingDirective.Name?.ToString().Replace("global::", "", StringComparison.Ordinal)
                    == "UnityEngine.QualitySettings")
            .Select(usingDirective => usingDirective.Alias!.Name.Identifier.ValueText)
            .ToHashSet(StringComparer.Ordinal);
        var environmentAliases = root.DescendantNodes().OfType<UsingDirectiveSyntax>()
            .Where(usingDirective => usingDirective.Alias != null
                && usingDirective.Name?.ToString().Replace("global::", "", StringComparison.Ordinal)
                    == "UnityEngine.RenderSettings")
            .Select(usingDirective => usingDirective.Alias!.Name.Identifier.ValueText)
            .ToHashSet(StringComparer.Ordinal);
        var staticEnvironmentImport = root.DescendantNodes().OfType<UsingDirectiveSyntax>()
            .Any(usingDirective => usingDirective.StaticKeyword != default
                && usingDirective.Name?.ToString().Replace("global::", "", StringComparison.Ordinal)
                    == "UnityEngine.RenderSettings");
        var staticQualityImport = root.DescendantNodes().OfType<UsingDirectiveSyntax>()
            .Any(usingDirective => usingDirective.StaticKeyword != default
                && usingDirective.Name?.ToString().Replace("global::", "", StringComparison.Ordinal)
                    == "UnityEngine.QualitySettings");
        var sortingLayerAliases = root.DescendantNodes().OfType<UsingDirectiveSyntax>()
            .Where(usingDirective => usingDirective.Alias != null
                && usingDirective.Name?.ToString().Replace("global::", "", StringComparison.Ordinal)
                    == "UnityEngine.SortingLayer")
            .Select(usingDirective => usingDirective.Alias!.Name.Identifier.ValueText)
            .ToHashSet(StringComparer.Ordinal);
        var staticSortingLayerImport = root.DescendantNodes().OfType<UsingDirectiveSyntax>()
            .Any(usingDirective => usingDirective.StaticKeyword != default
                && usingDirective.Name?.ToString().Replace("global::", "", StringComparison.Ordinal)
                    == "UnityEngine.SortingLayer");
        var staticSystemInfoImport = root.DescendantNodes().OfType<UsingDirectiveSyntax>()
            .Any(usingDirective => usingDirective.StaticKeyword != default
                && usingDirective.Name?.ToString().Replace("global::", "", StringComparison.Ordinal)
                    is "UnityEngine.SystemInfo" or "UnityEngine.Device.SystemInfo");
        var staticProfilerImport = root.DescendantNodes().OfType<UsingDirectiveSyntax>()
            .Any(usingDirective => usingDirective.StaticKeyword != default
                && usingDirective.Name?.ToString().Replace("global::", "", StringComparison.Ordinal)
                    == "UnityEngine.Profiling.Profiler");
        var staticShaderImport = root.DescendantNodes().OfType<UsingDirectiveSyntax>()
            .Any(usingDirective => usingDirective.StaticKeyword != default
                && usingDirective.Name?.ToString().Replace("global::", "", StringComparison.Ordinal)
                    == "UnityEngine.Shader");
        foreach (var access in root.DescendantNodes().OfType<MemberAccessExpressionSyntax>())
        {
            if (!IsRendererBoundShaderMember(access.Name.Identifier.ValueText)
                || access.Ancestors().OfType<MethodDeclarationSyntax>()
                    .Any(EditorCodePolicy.IsEditorOnlyMember)) continue;
            var receiver = access.Expression.ToString().Replace("global::", "", StringComparison.Ordinal);
            var isShader = receiver == "UnityEngine.Shader"
                || (receiver == "Shader" && imports.Contains("UnityEngine")
                    && !localTypes.Contains("Shader") && !aliases.Contains("Shader"))
                || shaderAliases.Contains(receiver)
                || (receiver.EndsWith(".Shader", StringComparison.Ordinal)
                    && aliases.Any(alias => receiver == alias + ".Shader"
                        && root.DescendantNodes().OfType<UsingDirectiveSyntax>().Any(usingDirective =>
                            usingDirective.Alias?.Name.Identifier.ValueText == alias
                            && usingDirective.Name?.ToString() == "UnityEngine")));
            if (!isShader) continue;
            var location = access.GetLocation().GetLineSpan();
            throw new InvalidOperationException($"{location.Path}:{location.StartLinePosition.Line + 1}:"
                + $"{location.StartLinePosition.Character + 1}: Preview graylists UnityEngine.Shader."
                + $"{access.Name.Identifier.ValueText} API: Unity shader globals and keywords are outside"
                + " the Three.js material mapping");
        }
        if (staticShaderImport)
        {
            foreach (var invocation in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
            {
                if (invocation.Expression is not IdentifierNameSyntax name
                    || !IsRendererBoundShaderMember(name.Identifier.ValueText)
                    || invocation.Ancestors().OfType<MethodDeclarationSyntax>()
                        .Any(EditorCodePolicy.IsEditorOnlyMember)) continue;
                var location = invocation.GetLocation().GetLineSpan();
                throw new InvalidOperationException($"{location.Path}:{location.StartLinePosition.Line + 1}:"
                    + $"{location.StartLinePosition.Character + 1}: Preview graylists UnityEngine.Shader."
                    + $"{name.Identifier.ValueText} API: Unity shader globals and keywords are outside"
                    + " the Three.js material mapping");
            }
            foreach (var identifier in root.DescendantNodes().OfType<IdentifierNameSyntax>())
            {
                if (identifier.Parent is MemberAccessExpressionSyntax or UsingDirectiveSyntax
                    || identifier.Ancestors().OfType<MethodDeclarationSyntax>()
                        .Any(EditorCodePolicy.IsEditorOnlyMember)
                    || identifier.Identifier.ValueText is not ("enabledGlobalKeywords"
                        or "globalKeywords" or "globalMaximumLOD" or "globalRenderPipeline"
                        or "globalShaderHardwareTier" or "maximumChunksOverride")) continue;
                var location = identifier.GetLocation().GetLineSpan();
                throw new InvalidOperationException($"{location.Path}:{location.StartLinePosition.Line + 1}:"
                    + $"{location.StartLinePosition.Character + 1}: Preview graylists UnityEngine.Shader."
                    + $"{identifier.Identifier.ValueText} API: Unity shader globals and keywords are outside"
                    + " the Three.js material mapping");
            }
        }
        foreach (var access in root.DescendantNodes().OfType<MemberAccessExpressionSyntax>())
        {
            if (!HostProfilerMembers.Contains(access.Name.Identifier.ValueText)
                || access.Ancestors().OfType<MethodDeclarationSyntax>()
                    .Any(EditorCodePolicy.IsEditorOnlyMember)) continue;
            var receiver = access.Expression.ToString().Replace("global::", "", StringComparison.Ordinal);
            var isProfiler = receiver == "UnityEngine.Profiling.Profiler"
                || (receiver == "Profiler" && imports.Contains("UnityEngine.Profiling")
                    && !localTypes.Contains("Profiler") && !aliases.Contains("Profiler"))
                || (receiver == "Profiling.Profiler" && imports.Contains("UnityEngine"))
                || profilerAliases.Contains(receiver);
            if (!isProfiler) continue;
            var location = access.GetLocation().GetLineSpan();
            throw new InvalidOperationException($"{location.Path}:{location.StartLinePosition.Line + 1}:"
                + $"{location.StartLinePosition.Character + 1}: Preview defers UnityEngine.Profiling.Profiler."
                + $"{access.Name.Identifier.ValueText}: Unity host profiler state is unavailable in the browser");
        }
        if (staticProfilerImport)
            foreach (var name in root.DescendantNodes().OfType<IdentifierNameSyntax>())
            {
                if (!HostProfilerMembers.Contains(name.Identifier.ValueText)
                    || name.Parent is MemberAccessExpressionSyntax
                    || name.Ancestors().OfType<MethodDeclarationSyntax>()
                        .Any(EditorCodePolicy.IsEditorOnlyMember)) continue;
                var location = name.GetLocation().GetLineSpan();
                throw new InvalidOperationException($"{location.Path}:{location.StartLinePosition.Line + 1}:"
                    + $"{location.StartLinePosition.Character + 1}: Preview defers UnityEngine.Profiling.Profiler."
                    + $"{name.Identifier.ValueText}: Unity host profiler state is unavailable in the browser");
            }
        foreach (var access in root.DescendantNodes().OfType<MemberAccessExpressionSyntax>())
        {
            if (!HostSystemInfoMembers.Contains(access.Name.Identifier.ValueText)
                && !RendererSystemInfoMembers.Contains(access.Name.Identifier.ValueText)
                || access.Ancestors().OfType<MethodDeclarationSyntax>()
                    .Any(EditorCodePolicy.IsEditorOnlyMember)) continue;
            var receiver = access.Expression.ToString().Replace("global::", "", StringComparison.Ordinal);
            var isSystemInfo = receiver is "UnityEngine.SystemInfo"
                or "UnityEngine.Device.SystemInfo"
                || (receiver == "SystemInfo" && (imports.Contains("UnityEngine")
                    || imports.Contains("UnityEngine.Device"))
                    && !localTypes.Contains("SystemInfo") && !aliases.Contains("SystemInfo"))
                || (receiver == "Device.SystemInfo" && imports.Contains("UnityEngine"))
                || systemInfoAliases.Contains(receiver);
            if (!isSystemInfo) continue;
            var location = access.GetLocation().GetLineSpan();
            if (RendererSystemInfoMembers.Contains(access.Name.Identifier.ValueText))
                throw new InvalidOperationException($"{location.Path}:{location.StartLinePosition.Line + 1}:"
                    + $"{location.StartLinePosition.Character + 1}: Preview defers Unity GPU SystemInfo."
                    + $"{access.Name.Identifier.ValueText}: Unity GPU capability does not describe the Three.js renderer");
            throw new InvalidOperationException($"{location.Path}:{location.StartLinePosition.Line + 1}:"
                + $"{location.StartLinePosition.Character + 1}: Preview defers Unity host SystemInfo."
                + $"{access.Name.Identifier.ValueText}: exact host identity or memory is unavailable in the browser");
        }
        if (staticSystemInfoImport)
            foreach (var name in root.DescendantNodes().OfType<IdentifierNameSyntax>())
            {
                if (!HostSystemInfoMembers.Contains(name.Identifier.ValueText)
                    && !RendererSystemInfoMembers.Contains(name.Identifier.ValueText)
                    || name.Parent is MemberAccessExpressionSyntax
                    || name.Ancestors().OfType<MethodDeclarationSyntax>()
                        .Any(EditorCodePolicy.IsEditorOnlyMember)) continue;
                var location = name.GetLocation().GetLineSpan();
                if (RendererSystemInfoMembers.Contains(name.Identifier.ValueText))
                    throw new InvalidOperationException($"{location.Path}:{location.StartLinePosition.Line + 1}:"
                        + $"{location.StartLinePosition.Character + 1}: Preview defers Unity GPU SystemInfo."
                        + $"{name.Identifier.ValueText}: Unity GPU capability does not describe the Three.js renderer");
                throw new InvalidOperationException($"{location.Path}:{location.StartLinePosition.Line + 1}:"
                    + $"{location.StartLinePosition.Character + 1}: Preview defers Unity host SystemInfo."
                    + $"{name.Identifier.ValueText}: exact host identity or memory is unavailable in the browser");
            }
        foreach (var access in root.DescendantNodes().OfType<MemberAccessExpressionSyntax>())
        {
            if (!HostDisplayMembers.Contains(access.Name.Identifier.ValueText)
                || access.Ancestors().OfType<MethodDeclarationSyntax>()
                    .Any(EditorCodePolicy.IsEditorOnlyMember)) continue;
            var receiver = access.Expression.ToString().Replace("global::", "", StringComparison.Ordinal);
            var isDisplay = receiver is "UnityEngine.Display" or "UnityEngine.Display.main"
                || (receiver is "Display" or "Display.main"
                    && imports.Contains("UnityEngine") && !localTypes.Contains("Display")
                    && !aliases.Contains("Display"))
                || displayAliases.Contains(receiver)
                || displayAliases.Any(alias => receiver == alias + ".main");
            if (!isDisplay) continue;
            var location = access.GetLocation().GetLineSpan();
            throw new InvalidOperationException($"{location.Path}:{location.StartLinePosition.Line + 1}:"
                + $"{location.StartLinePosition.Character + 1}: Preview defers Unity host Display."
                + $"{access.Name.Identifier.ValueText}: native display or framebuffer control is unavailable in Preview");
        }
        foreach (var access in root.DescendantNodes().OfType<MemberAccessExpressionSyntax>())
        {
            if (!HostScreenMembers.Contains(access.Name.Identifier.ValueText)
                || access.Ancestors().OfType<MethodDeclarationSyntax>()
                    .Any(EditorCodePolicy.IsEditorOnlyMember)) continue;
            var receiver = access.Expression.ToString().Replace("global::", "", StringComparison.Ordinal);
            var isScreen = receiver is "UnityEngine.Screen" or "UnityEngine.Device.Screen"
                || (receiver == "Screen" && (imports.Contains("UnityEngine")
                    || imports.Contains("UnityEngine.Device"))
                    && !localTypes.Contains("Screen") && !aliases.Contains("Screen"))
                || (receiver == "Device.Screen" && imports.Contains("UnityEngine"))
                || screenAliases.Contains(receiver);
            if (!isScreen) continue;
            var location = access.GetLocation().GetLineSpan();
            throw new InvalidOperationException($"{location.Path}:{location.StartLinePosition.Line + 1}:"
                + $"{location.StartLinePosition.Character + 1}: Preview defers Unity host Screen."
                + $"{access.Name.Identifier.ValueText}: native display, orientation, or power control is unavailable in the browser");
        }
        foreach (var access in root.DescendantNodes().OfType<MemberAccessExpressionSyntax>())
        {
            if (!HostApplicationMembers.Contains(access.Name.Identifier.ValueText)
                || access.Ancestors().OfType<MethodDeclarationSyntax>()
                    .Any(EditorCodePolicy.IsEditorOnlyMember)) continue;
            var receiver = access.Expression.ToString().Replace("global::", "", StringComparison.Ordinal);
            var isApplication = receiver is "UnityEngine.Application" or "UnityEngine.Device.Application"
                || (receiver == "Application" && (imports.Contains("UnityEngine")
                    || imports.Contains("UnityEngine.Device"))
                    && !localTypes.Contains("Application") && !aliases.Contains("Application"))
                || (receiver == "Device.Application" && imports.Contains("UnityEngine"))
                || applicationAliases.Contains(receiver);
            if (!isApplication) continue;
            var location = access.GetLocation().GetLineSpan();
            throw new InvalidOperationException($"{location.Path}:{location.StartLinePosition.Line + 1}:"
                + $"{location.StartLinePosition.Character + 1}: Preview defers Unity host Application."
                + $"{access.Name.Identifier.ValueText}: native host state or control is unavailable in the browser");
        }
        foreach (var access in root.DescendantNodes().OfType<MemberAccessExpressionSyntax>())
        {
            if (!HostResourcesMembers.Contains(access.Name.Identifier.ValueText)
                || access.Ancestors().OfType<MethodDeclarationSyntax>()
                    .Any(EditorCodePolicy.IsEditorOnlyMember)) continue;
            var receiver = access.Expression.ToString().Replace("global::", "", StringComparison.Ordinal);
            var isResources = receiver == "UnityEngine.Resources"
                || (receiver == "Resources" && imports.Contains("UnityEngine")
                    && !localTypes.Contains("Resources") && !aliases.Contains("Resources"))
                || resourcesAliases.Contains(receiver);
            if (!isResources) continue;
            var location = access.GetLocation().GetLineSpan();
            throw new InvalidOperationException($"{location.Path}:{location.StartLinePosition.Line + 1}:"
                + $"{location.StartLinePosition.Character + 1}: Preview defers Unity host Resources."
                + $"{access.Name.Identifier.ValueText}: native built-in assets or unused-asset collection"
                + " are outside the exported asset catalog");
        }
        if (staticScreenImport)
            foreach (var name in root.DescendantNodes().OfType<IdentifierNameSyntax>())
            {
                if (!HostScreenMembers.Contains(name.Identifier.ValueText)
                    || name.Parent is MemberAccessExpressionSyntax
                    || name.Ancestors().OfType<MethodDeclarationSyntax>()
                        .Any(EditorCodePolicy.IsEditorOnlyMember)) continue;
                var location = name.GetLocation().GetLineSpan();
                throw new InvalidOperationException($"{location.Path}:{location.StartLinePosition.Line + 1}:"
                    + $"{location.StartLinePosition.Character + 1}: Preview defers Unity host Screen."
                    + $"{name.Identifier.ValueText}: native display, orientation, or power control is unavailable in the browser");
            }
        if (staticApplicationImport)
            foreach (var name in root.DescendantNodes().OfType<IdentifierNameSyntax>())
            {
                if (!HostApplicationMembers.Contains(name.Identifier.ValueText)
                    || name.Parent is MemberAccessExpressionSyntax
                    || name.Ancestors().OfType<MethodDeclarationSyntax>()
                        .Any(EditorCodePolicy.IsEditorOnlyMember)) continue;
                var location = name.GetLocation().GetLineSpan();
                throw new InvalidOperationException($"{location.Path}:{location.StartLinePosition.Line + 1}:"
                    + $"{location.StartLinePosition.Character + 1}: Preview defers Unity host Application."
                    + $"{name.Identifier.ValueText}: native host state or control is unavailable in the browser");
            }
        if (staticResourcesImport)
            foreach (var name in root.DescendantNodes().OfType<SimpleNameSyntax>())
            {
                if (!HostResourcesMembers.Contains(name.Identifier.ValueText)
                    || name.Parent is MemberAccessExpressionSyntax
                    || name.Ancestors().OfType<MethodDeclarationSyntax>()
                        .Any(EditorCodePolicy.IsEditorOnlyMember)) continue;
                var location = name.GetLocation().GetLineSpan();
                throw new InvalidOperationException($"{location.Path}:{location.StartLinePosition.Line + 1}:"
                    + $"{location.StartLinePosition.Character + 1}: Preview defers Unity host Resources."
                    + $"{name.Identifier.ValueText}: native built-in assets or unused-asset collection"
                    + " are outside the exported asset catalog");
            }
        if (staticDisplayImport)
            foreach (var name in root.DescendantNodes().OfType<IdentifierNameSyntax>())
            {
                if (!HostDisplayMembers.Contains(name.Identifier.ValueText)
                    || name.Parent is MemberAccessExpressionSyntax
                    || name.Ancestors().OfType<MethodDeclarationSyntax>()
                        .Any(EditorCodePolicy.IsEditorOnlyMember)) continue;
                var location = name.GetLocation().GetLineSpan();
                throw new InvalidOperationException($"{location.Path}:{location.StartLinePosition.Line + 1}:"
                    + $"{location.StartLinePosition.Character + 1}: Preview defers Unity host Display."
                    + $"{name.Identifier.ValueText}: native display or framebuffer control is unavailable in Preview");
            }
        foreach (var access in root.DescendantNodes().OfType<MemberAccessExpressionSyntax>())
        {
            if (!RendererEnvironmentMembers.Contains(access.Name.Identifier.ValueText)
                || access.Ancestors().OfType<MethodDeclarationSyntax>()
                    .Any(EditorCodePolicy.IsEditorOnlyMember)) continue;
            var receiver = access.Expression.ToString().Replace("global::", "", StringComparison.Ordinal);
            if (receiver != "UnityEngine.RenderSettings"
                && !(receiver == "RenderSettings" && imports.Contains("UnityEngine")
                    && !localTypes.Contains("RenderSettings") && !aliases.Contains("RenderSettings"))
                && !environmentAliases.Contains(receiver)) continue;
            var location = access.GetLocation().GetLineSpan();
            throw new InvalidOperationException($"{location.Path}:{location.StartLinePosition.Line + 1}:"
                + $"{location.StartLinePosition.Character + 1}: Preview defers UnityEngine.RenderSettings."
                + $"{access.Name.Identifier.ValueText}: Unity environment lighting and skybox effects are outside the single Three.js scene mapping");
        }
        if (staticEnvironmentImport)
            foreach (var name in root.DescendantNodes().OfType<IdentifierNameSyntax>())
            {
                if (!RendererEnvironmentMembers.Contains(name.Identifier.ValueText)
                    || name.Parent is MemberAccessExpressionSyntax
                    || name.Ancestors().OfType<MethodDeclarationSyntax>()
                        .Any(EditorCodePolicy.IsEditorOnlyMember)) continue;
                var location = name.GetLocation().GetLineSpan();
                throw new InvalidOperationException($"{location.Path}:{location.StartLinePosition.Line + 1}:"
                    + $"{location.StartLinePosition.Character + 1}: Preview defers UnityEngine.RenderSettings."
                    + $"{name.Identifier.ValueText}: Unity environment lighting and skybox effects are outside the single Three.js scene mapping");
            }
        foreach (var access in root.DescendantNodes().OfType<MemberAccessExpressionSyntax>())
        {
            if (!(RendererQualityPipelineMembers.Contains(access.Name.Identifier.ValueText)
                || RendererQualityGpuMembers.Contains(access.Name.Identifier.ValueText)
                || HostQualityProfileMembers.Contains(access.Name.Identifier.ValueText))
                || access.Ancestors().OfType<MethodDeclarationSyntax>()
                    .Any(EditorCodePolicy.IsEditorOnlyMember)) continue;
            var receiver = access.Expression.ToString().Replace("global::", "", StringComparison.Ordinal);
            var isQuality = receiver == "UnityEngine.QualitySettings"
                || (receiver == "QualitySettings" && imports.Contains("UnityEngine")
                    && !localTypes.Contains("QualitySettings") && !aliases.Contains("QualitySettings"))
                || qualityAliases.Contains(receiver);
            if (!isQuality) continue;
            var location = access.GetLocation().GetLineSpan();
            if (HostQualityProfileMembers.Contains(access.Name.Identifier.ValueText))
                throw new InvalidOperationException($"{location.Path}:{location.StartLinePosition.Line + 1}:"
                    + $"{location.StartLinePosition.Character + 1}: Preview defers Unity host QualitySettings."
                    + $"{access.Name.Identifier.ValueText}: Unity build quality profiles are unavailable in the browser");
            throw new InvalidOperationException($"{location.Path}:{location.StartLinePosition.Line + 1}:"
                + $"{location.StartLinePosition.Character + 1}: Preview defers UnityEngine.QualitySettings."
                + $"{access.Name.Identifier.ValueText}: Unity native renderer quality controls are outside the Three.js mapping");
        }
        foreach (var access in root.DescendantNodes().OfType<MemberAccessExpressionSyntax>())
        {
            if (access.Name.Identifier.ValueText is not ("onLayerAdded" or "onLayerRemoved")
                || access.Ancestors().OfType<MethodDeclarationSyntax>()
                    .Any(EditorCodePolicy.IsEditorOnlyMember)) continue;
            var receiver = access.Expression.ToString().Replace("global::", "", StringComparison.Ordinal);
            if (receiver != "UnityEngine.SortingLayer"
                && !(receiver == "SortingLayer" && imports.Contains("UnityEngine")
                    && !localTypes.Contains("SortingLayer") && !aliases.Contains("SortingLayer"))
                && !sortingLayerAliases.Contains(receiver)) continue;
            var location = access.GetLocation().GetLineSpan();
            throw new InvalidOperationException($"{location.Path}:{location.StartLinePosition.Line + 1}:"
                + $"{location.StartLinePosition.Character + 1}: Preview defers Unity host SortingLayer."
                + $"{access.Name.Identifier.ValueText}: project sorting-layer edits do not occur in browser Preview");
        }
        if (staticSortingLayerImport)
            foreach (var name in root.DescendantNodes().OfType<IdentifierNameSyntax>())
            {
                if (name.Identifier.ValueText is not ("onLayerAdded" or "onLayerRemoved")
                    || name.Parent is MemberAccessExpressionSyntax
                    || name.Ancestors().OfType<MethodDeclarationSyntax>()
                        .Any(EditorCodePolicy.IsEditorOnlyMember)) continue;
                var location = name.GetLocation().GetLineSpan();
                throw new InvalidOperationException($"{location.Path}:{location.StartLinePosition.Line + 1}:"
                    + $"{location.StartLinePosition.Character + 1}: Preview defers Unity host SortingLayer."
                    + $"{name.Identifier.ValueText}: project sorting-layer edits do not occur in browser Preview");
            }
        if (staticQualityImport)
            foreach (var name in root.DescendantNodes().OfType<IdentifierNameSyntax>())
            {
                if (!(RendererQualityPipelineMembers.Contains(name.Identifier.ValueText)
                    || RendererQualityGpuMembers.Contains(name.Identifier.ValueText)
                    || HostQualityProfileMembers.Contains(name.Identifier.ValueText))
                    || name.Parent is MemberAccessExpressionSyntax
                    || name.Ancestors().OfType<MethodDeclarationSyntax>()
                        .Any(EditorCodePolicy.IsEditorOnlyMember)) continue;
                var location = name.GetLocation().GetLineSpan();
                if (HostQualityProfileMembers.Contains(name.Identifier.ValueText))
                    throw new InvalidOperationException($"{location.Path}:{location.StartLinePosition.Line + 1}:"
                        + $"{location.StartLinePosition.Character + 1}: Preview defers Unity host QualitySettings."
                        + $"{name.Identifier.ValueText}: Unity build quality profiles are unavailable in the browser");
                throw new InvalidOperationException($"{location.Path}:{location.StartLinePosition.Line + 1}:"
                    + $"{location.StartLinePosition.Character + 1}: Preview defers UnityEngine.QualitySettings."
                    + $"{name.Identifier.ValueText}: Unity native renderer quality controls are outside the Three.js mapping");
            }
        foreach (var access in root.DescendantNodes().OfType<MemberAccessExpressionSyntax>())
        {
            if (access.Ancestors().OfType<MethodDeclarationSyntax>()
                .Any(EditorCodePolicy.IsEditorOnlyMember) || IsPreservedRendererEnum(access)) continue;
            var name = access.ToString().Replace("global::", "", StringComparison.Ordinal);
            if (!Matches(name, "UnityEngine.TouchScreenKeyboard")) continue;
            var location = access.GetLocation().GetLineSpan();
            throw new InvalidOperationException($"{location.Path}:{location.StartLinePosition.Line + 1}:"
                + $"{location.StartLinePosition.Character + 1}: Preview graylists UnityEngine.TouchScreenKeyboard"
                + " API: native on-screen keyboard is unavailable in browser Preview");
        }
        foreach (var node in root.DescendantNodes().OfType<NameSyntax>())
        {
            if (node.Ancestors().OfType<MethodDeclarationSyntax>()
                .Any(EditorCodePolicy.IsEditorOnlyMember)) continue;
            if (IsPreservedRendererEnum(node)) continue;
            var name = node.ToString().Replace("global::", "", StringComparison.Ordinal);
            var excluded = Namespaces.FirstOrDefault(prefix => Matches(name, prefix));
            if (excluded == null && name.StartsWith("UnityEngine.Procedural", StringComparison.Ordinal))
                excluded = "Substance";
            if (excluded == null && (name.StartsWith("UnityEngine.Subsystem", StringComparison.Ordinal)
                || name.StartsWith("UnityEngine.ISubsystem", StringComparison.Ordinal)
                || name.StartsWith("UnityEngine.IntegratedSubsystem", StringComparison.Ordinal)))
                excluded = "Subsystems";
            if (excluded == null && name.StartsWith("UnityEngine.iPhone", StringComparison.Ordinal))
                excluded = "iPhone";
            if (excluded == null && name.StartsWith("UnityEngine.", StringComparison.Ordinal)
                && name.Contains("Android", StringComparison.OrdinalIgnoreCase))
                excluded = "Android";
            if (excluded == null && (name.StartsWith("UnityEngine.", StringComparison.Ordinal)
                || name.StartsWith("Unity.", StringComparison.Ordinal))
                && name.Contains("Experimental", StringComparison.OrdinalIgnoreCase))
                excluded = "Experimental";
            var graylisted = GraylistedNamespaces.FirstOrDefault(prefix => Matches(name, prefix));
            if (graylisted == null && !node.Ancestors().OfType<UsingDirectiveSyntax>().Any()
                && !localTypes.Contains(name.Split('.')[0]) && !aliases.Contains(name.Split('.')[0]))
                graylisted = GraylistedNamespaces.FirstOrDefault(prefix => imports.Any(imported =>
                    Matches(imported + "." + name, prefix)));
            if (graylisted == null && !localTypes.Contains(name.Split('.')[0]))
            {
                var fullName = name.StartsWith("UnityEngine.UIElements.", StringComparison.Ordinal)
                    ? name : imports.Contains("UnityEngine.UIElements")
                        ? "UnityEngine.UIElements." + name : name;
                if (fullName.StartsWith("UnityEngine.UIElements.Uxml", StringComparison.Ordinal)
                    || fullName.StartsWith("UnityEngine.UIElements.IUxml", StringComparison.Ordinal)
                    || fullName == "UnityEngine.UIElements.CreationContext")
                    graylisted = "UnityEngine.UIElements.UxmlAuthoring";
            }
            if (graylisted == null && !localTypes.Contains(name.Split('.')[0])
                && (name.StartsWith("UnityEngine.UIElements.", StringComparison.Ordinal)
                    || imports.Contains("UnityEngine.UIElements"))
                && name.Split('.').Any(part => part is "UxmlTraits" or "UxmlFactory"
                    or "UxmlSerializedData"))
                graylisted = "UnityEngine.UIElements.UxmlAuthoring";
            if (excluded == null && graylisted == null) continue;
            var location = node.GetLocation().GetLineSpan();
            var message = excluded != null ? $"Preview excludes {excluded} API"
                : GraylistMessage(graylisted!);
            throw new InvalidOperationException($"{location.Path}:{location.StartLinePosition.Line + 1}:"
                + $"{location.StartLinePosition.Character + 1}: {message}");
        }
    }

    public static void RejectAdvancedRenderTexture(BaseTypeDeclarationSyntax declaration, SemanticModel model)
    {
        foreach (var access in declaration.DescendantNodes().OfType<MemberAccessExpressionSyntax>())
        {
            if (access.Ancestors().OfType<MethodDeclarationSyntax>()
                .Any(EditorCodePolicy.IsEditorOnlyMember)) continue;
            var receiverType = model.GetTypeInfo(access.Expression).Type?.ToDisplayString();
            var receiverSymbol = model.GetSymbolInfo(access.Expression).Symbol?.ToDisplayString();
            if (receiverType != "UnityEngine.RenderTexture"
                && receiverSymbol != "UnityEngine.RenderTexture") continue;
            var name = access.Name.Identifier.ValueText;
            var advancedTemporary = name == "GetTemporary"
                && access.Parent is InvocationExpressionSyntax invocation
                && invocation.ArgumentList.Arguments.Count > 4;
            if (!RendererBoundRenderTextureMembers.Contains(name) && !advancedTemporary)
                continue;
            var location = access.GetLocation().GetLineSpan();
            throw new InvalidOperationException($"{location.Path}:{location.StartLinePosition.Line + 1}:"
                + $"{location.StartLinePosition.Character + 1}: Preview defers UnityEngine.RenderTexture."
                + $"{name}: advanced Unity GPU target controls are outside the single Three.js render path");
        }
        foreach (var creation in declaration.DescendantNodes().OfType<ObjectCreationExpressionSyntax>())
        {
            if (creation.Ancestors().OfType<MethodDeclarationSyntax>()
                .Any(EditorCodePolicy.IsEditorOnlyMember)
                || model.GetTypeInfo(creation).Type?.ToDisplayString() != "UnityEngine.RenderTexture")
                continue;
            var arguments = creation.ArgumentList?.Arguments;
            if (arguments == null || arguments.Value.Count == 0) continue;
            var descriptor = model.GetTypeInfo(arguments.Value[0].Expression).Type?.ToDisplayString()
                == "UnityEngine.RenderTextureDescriptor";
            var advancedFormat = arguments.Value.Count == 5
                && model.GetTypeInfo(arguments.Value[4].Expression).Type?.ToDisplayString()
                    != "UnityEngine.RenderTextureReadWrite";
            if (!descriptor && !advancedFormat) continue;
            var location = creation.GetLocation().GetLineSpan();
            throw new InvalidOperationException($"{location.Path}:{location.StartLinePosition.Line + 1}:"
                + $"{location.StartLinePosition.Character + 1}: Preview defers UnityEngine.RenderTexture constructor:"
                + " descriptor and advanced GPU target options are outside the single Three.js render path");
        }
    }

    public static void RejectAdvancedLight(BaseTypeDeclarationSyntax declaration, SemanticModel model)
    {
        foreach (var access in declaration.DescendantNodes().OfType<MemberAccessExpressionSyntax>())
        {
            if (access.Ancestors().OfType<MethodDeclarationSyntax>()
                .Any(EditorCodePolicy.IsEditorOnlyMember)
                || !RendererBoundLightMembers.Contains(access.Name.Identifier.ValueText)) continue;
            var receiverType = model.GetTypeInfo(access.Expression).Type?.ToDisplayString();
            var receiverSymbol = model.GetSymbolInfo(access.Expression).Symbol?.ToDisplayString();
            if (receiverType != "UnityEngine.Light" && receiverSymbol != "UnityEngine.Light")
                continue;
            var location = access.GetLocation().GetLineSpan();
            throw new InvalidOperationException($"{location.Path}:{location.StartLinePosition.Line + 1}:"
                + $"{location.StartLinePosition.Character + 1}: Preview defers UnityEngine.Light."
                + $"{access.Name.Identifier.ValueText}: outside the supported Three.js scene-light mapping");
        }
    }

    public static void RejectAdvancedMesh(BaseTypeDeclarationSyntax declaration, SemanticModel model)
    {
        foreach (var access in declaration.DescendantNodes().OfType<MemberAccessExpressionSyntax>())
        {
            var name = access.Name.Identifier.ValueText;
            var called = access.Parent is InvocationExpressionSyntax invocation
                ? model.GetSymbolInfo(invocation).Symbol as IMethodSymbol : null;
            var lodOverload = called?.Parameters.Any(parameter => parameter.Name == "lod") == true;
            if (access.Ancestors().OfType<MethodDeclarationSyntax>()
                .Any(EditorCodePolicy.IsEditorOnlyMember)
                || !RendererBoundMeshMembers.Contains(name) && !lodOverload) continue;
            var receiverType = model.GetTypeInfo(access.Expression).Type?.ToDisplayString();
            var receiverSymbol = model.GetSymbolInfo(access.Expression).Symbol?.ToDisplayString();
            if (receiverType != "UnityEngine.Mesh" && receiverSymbol != "UnityEngine.Mesh")
                continue;
            var location = access.GetLocation().GetLineSpan();
            throw new InvalidOperationException($"{location.Path}:{location.StartLinePosition.Line + 1}:"
                + $"{location.StartLinePosition.Character + 1}: Preview defers UnityEngine.Mesh."
                + $"{name}: native GPU buffers or Unity mesh LOD submission"
                + " are outside the Three.js mesh data bridge");
        }
    }

    public static void RejectAdvancedLineRenderer(BaseTypeDeclarationSyntax declaration,
        SemanticModel model)
    {
        foreach (var access in declaration.DescendantNodes().OfType<MemberAccessExpressionSyntax>())
        {
            if (access.Ancestors().OfType<MethodDeclarationSyntax>()
                .Any(EditorCodePolicy.IsEditorOnlyMember)
                || !RendererBoundLineRendererMembers.Contains(access.Name.Identifier.ValueText)) continue;
            var receiverType = model.GetTypeInfo(access.Expression).Type?.ToDisplayString();
            if (receiverType != "UnityEngine.LineRenderer") continue;
            var location = access.GetLocation().GetLineSpan();
            throw new InvalidOperationException($"{location.Path}:{location.StartLinePosition.Line + 1}:"
                + $"{location.StartLinePosition.Character + 1}: Preview defers UnityEngine.LineRenderer."
                + $"{access.Name.Identifier.ValueText}: Unity line tessellation, shader texture,"
                + " or bake controls are outside the single Three.js line mesh mapping");
        }
    }

    public static void RejectAdvancedTrailRenderer(BaseTypeDeclarationSyntax declaration,
        SemanticModel model)
    {
        foreach (var access in declaration.DescendantNodes().OfType<MemberAccessExpressionSyntax>())
        {
            if (access.Ancestors().OfType<MethodDeclarationSyntax>()
                .Any(EditorCodePolicy.IsEditorOnlyMember)
                || !RendererBoundTrailRendererMembers.Contains(access.Name.Identifier.ValueText)) continue;
            if (model.GetTypeInfo(access.Expression).Type?.ToDisplayString()
                != "UnityEngine.TrailRenderer") continue;
            var location = access.GetLocation().GetLineSpan();
            throw new InvalidOperationException($"{location.Path}:{location.StartLinePosition.Line + 1}:"
                + $"{location.StartLinePosition.Character + 1}: Preview defers UnityEngine.TrailRenderer."
                + $"{access.Name.Identifier.ValueText}: Unity trail tessellation, shader controls,"
                + " or baking are outside the single Three.js line mesh mapping");
        }
    }

    public static void RejectAdvancedMeshRenderer(BaseTypeDeclarationSyntax declaration,
        SemanticModel model)
    {
        foreach (var access in declaration.DescendantNodes().OfType<MemberAccessExpressionSyntax>())
        {
            if (access.Ancestors().OfType<MethodDeclarationSyntax>()
                .Any(EditorCodePolicy.IsEditorOnlyMember)
                || !RendererBoundMeshRendererMembers.Contains(access.Name.Identifier.ValueText)) continue;
            if (model.GetTypeInfo(access.Expression).Type?.ToDisplayString()
                != "UnityEngine.MeshRenderer") continue;
            var location = access.GetLocation().GetLineSpan();
            throw new InvalidOperationException($"{location.Path}:{location.StartLinePosition.Line + 1}:"
                + $"{location.StartLinePosition.Character + 1}: Preview defers UnityEngine.MeshRenderer."
                + $"{access.Name.Identifier.ValueText}: Unity shader user values, vertex streams,"
                + " static batching, and baked lighting are outside the single Three.js mesh mapping");
        }
    }

    public static void RejectHostEditorAttributes(BaseTypeDeclarationSyntax declaration,
        SemanticModel model)
    {
        static bool IsHostAttribute(ITypeSymbol? type) => type?.ToDisplayString()
            is "UnityEngine.AddComponentMenu" or "UnityEngine.ColorUsageAttribute"
            or "UnityEngine.ContextMenu" or "UnityEngine.ContextMenuItemAttribute"
            or "UnityEngine.CreateAssetMenuAttribute" or "UnityEngine.DelayedAttribute"
            or "UnityEngine.EnumButtonsAttribute" or "UnityEngine.HelpURLAttribute"
            or "UnityEngine.IconAttribute" or "UnityEngine.InspectorNameAttribute"
            or "UnityEngine.NonReorderableAttribute" or "UnityEngine.SelectionBaseAttribute"
            or "UnityEngine.GradientUsageAttribute" or "UnityEngine.HeaderAttribute"
            or "UnityEngine.MinAttribute" or "UnityEngine.MultilineAttribute"
            or "UnityEngine.RangeAttribute" or "UnityEngine.SpaceAttribute"
            or "UnityEngine.TextAreaAttribute" or "UnityEngine.TooltipAttribute";
        static bool IsAdditionalHostAttribute(ITypeSymbol? type) => type?.ToDisplayString()
            is "UnityEngine.AssemblyIsEditorAssembly"
            or "UnityEngine.Categorization.CategoryInfoAttribute"
            or "UnityEngine.Categorization.ElementInfoAttribute"
            or "UnityEngine.ExcludeFromPresetAttribute"
            or "UnityEngine.ExecuteAlways" or "UnityEngine.ExecuteInEditMode"
            or "UnityEngine.InspectorOrderAttribute"
            or "UnityEngine.Internal.ExcludeFromDocsAttribute"
            or "UnityEngine.SearchService.ObjectSelectorHandlerWithLabelsAttribute"
            or "UnityEngine.SearchService.ObjectSelectorHandlerWithTagsAttribute"
            or "UnityEngine.Scripting.AlwaysLinkAssemblyAttribute"
            or "UnityEngine.Scripting.PreserveAttribute"
            or "UnityEngine.Scripting.RequireAttributeUsagesAttribute"
            or "UnityEngine.Scripting.RequireDerivedAttribute"
            or "UnityEngine.Scripting.RequireImplementorsAttribute"
            or "UnityEngine.Scripting.RequiredInterfaceAttribute"
            or "UnityEngine.Scripting.RequiredMemberAttribute"
            or "UnityEngine.UnityAPICompatibilityVersionAttribute";
        static bool IsAnyHostAttribute(ITypeSymbol? type) =>
            IsHostAttribute(type) || IsAdditionalHostAttribute(type);
        foreach (var creation in declaration.DescendantNodes().OfType<ObjectCreationExpressionSyntax>())
        {
            if (creation.Ancestors().OfType<MethodDeclarationSyntax>()
                .Any(EditorCodePolicy.IsEditorOnlyMember)
                || !IsAnyHostAttribute(model.GetTypeInfo(creation).Type)) continue;
            var location = creation.GetLocation().GetLineSpan();
            throw new InvalidOperationException($"{location.Path}:{location.StartLinePosition.Line + 1}:"
                + $"{location.StartLinePosition.Character + 1}: Preview defers "
                + $"{model.GetTypeInfo(creation).Type}: Unity inspector presentation belongs to the editor host");
        }
        foreach (var access in declaration.DescendantNodes().OfType<MemberAccessExpressionSyntax>())
        {
            if (access.Ancestors().OfType<MethodDeclarationSyntax>()
                .Any(EditorCodePolicy.IsEditorOnlyMember)
                || !IsAnyHostAttribute(model.GetTypeInfo(access.Expression).Type)) continue;
            var location = access.GetLocation().GetLineSpan();
            throw new InvalidOperationException($"{location.Path}:{location.StartLinePosition.Line + 1}:"
                + $"{location.StartLinePosition.Character + 1}: Preview defers "
                + $"{model.GetTypeInfo(access.Expression).Type}.{access.Name.Identifier.ValueText}:"
                + " Unity inspector presentation belongs to the editor host");
        }
        foreach (var typeOf in declaration.DescendantNodes().OfType<TypeOfExpressionSyntax>())
        {
            if (typeOf.Ancestors().OfType<MethodDeclarationSyntax>()
                .Any(EditorCodePolicy.IsEditorOnlyMember)
                || !IsAnyHostAttribute(model.GetTypeInfo(typeOf.Type).Type)) continue;
            var location = typeOf.GetLocation().GetLineSpan();
            throw new InvalidOperationException($"{location.Path}:{location.StartLinePosition.Line + 1}:"
                + $"{location.StartLinePosition.Character + 1}: Preview defers "
                + $"{model.GetTypeInfo(typeOf.Type).Type}: Unity inspector presentation belongs to the editor host");
        }
    }

    public static void RejectAdvancedCamera(BaseTypeDeclarationSyntax declaration, SemanticModel model)
    {
        foreach (var access in declaration.DescendantNodes().OfType<MemberAccessExpressionSyntax>())
        {
            if (access.Ancestors().OfType<MethodDeclarationSyntax>()
                .Any(EditorCodePolicy.IsEditorOnlyMember)
                || !RendererBoundCameraMembers.Contains(access.Name.Identifier.ValueText)) continue;
            var receiverType = model.GetTypeInfo(access.Expression).Type?.ToDisplayString();
            var receiverSymbol = model.GetSymbolInfo(access.Expression).Symbol?.ToDisplayString();
            if (receiverType != "UnityEngine.Camera" && receiverSymbol != "UnityEngine.Camera")
                continue;
            var location = access.GetLocation().GetLineSpan();
            throw new InvalidOperationException($"{location.Path}:{location.StartLinePosition.Line + 1}:"
                + $"{location.StartLinePosition.Character + 1}: Preview defers UnityEngine.Camera."
                + $"{access.Name.Identifier.ValueText}: Unity render submission, stereo, physical"
                + " camera, or native culling controls are outside the Three.js camera bridge");
        }
    }

    public static void RejectAdvancedRenderer(BaseTypeDeclarationSyntax declaration, SemanticModel model)
    {
        foreach (var access in declaration.DescendantNodes().OfType<MemberAccessExpressionSyntax>())
        {
            if (access.Ancestors().OfType<MethodDeclarationSyntax>()
                .Any(EditorCodePolicy.IsEditorOnlyMember)) continue;
            var member = access.Name.Identifier.ValueText;
            if (RendererBoundSkinnedMembers.Contains(member))
            {
                var skinnedType = model.GetTypeInfo(access.Expression).Type as INamedTypeSymbol;
                while (skinnedType != null
                    && skinnedType.ToDisplayString() != "UnityEngine.SkinnedMeshRenderer")
                    skinnedType = skinnedType.BaseType;
                if (skinnedType != null)
                {
                    var span = access.GetLocation().GetLineSpan();
                    throw new InvalidOperationException($"{span.Path}:{span.StartLinePosition.Line + 1}:"
                        + $"{span.StartLinePosition.Character + 1}: Preview defers"
                        + $" UnityEngine.SkinnedMeshRenderer.{member}: native vertex buffers and"
                        + " motion vectors are outside the single Three.js renderer bridge");
                }
            }
            if (!RendererBoundRendererMembers.Contains(member)) continue;
            var type = model.GetTypeInfo(access.Expression).Type as INamedTypeSymbol;
            while (type != null && type.ToDisplayString() != "UnityEngine.Renderer")
                type = type.BaseType;
            if (type == null) continue;
            var location = access.GetLocation().GetLineSpan();
            throw new InvalidOperationException($"{location.Path}:{location.StartLinePosition.Line + 1}:"
                + $"{location.StartLinePosition.Character + 1}: Preview defers UnityEngine.Renderer."
                + $"{access.Name.Identifier.ValueText}: Unity culling, LOD, lightmap, probe,"
                + " ray tracing, or pipeline state is outside the single Three.js renderer bridge");
        }
    }

    public static void RejectAdvancedMaterial(BaseTypeDeclarationSyntax declaration, SemanticModel model)
    {
        foreach (var access in declaration.DescendantNodes().OfType<MemberAccessExpressionSyntax>())
        {
            if (access.Ancestors().OfType<MethodDeclarationSyntax>()
                .Any(EditorCodePolicy.IsEditorOnlyMember)) continue;
            var receiverType = model.GetTypeInfo(access.Expression).Type?.ToDisplayString();
            var receiverSymbol = model.GetSymbolInfo(access.Expression).Symbol?.ToDisplayString();
            if (receiverType != "UnityEngine.Material" && receiverSymbol != "UnityEngine.Material")
                continue;
            var name = access.Name.Identifier.ValueText;
            var textureSubelement = name == "SetTexture"
                && access.Parent is InvocationExpressionSyntax invocation
                && invocation.ArgumentList.Arguments.Count == 3;
            if (!RendererBoundMaterialMembers.Contains(name) && !textureSubelement) continue;
            var location = access.GetLocation().GetLineSpan();
            throw new InvalidOperationException($"{location.Path}:{location.StartLinePosition.Line + 1}:"
                + $"{location.StartLinePosition.Character + 1}: Preview defers UnityEngine.Material."
                + $"{name}: shader pass, GPU buffer, uniform array, or material variant"
                + " controls are outside the Three.js material mapping");
        }
    }

    public static void RejectAdvancedTexture(BaseTypeDeclarationSyntax declaration, SemanticModel model)
    {
        foreach (var access in declaration.DescendantNodes().OfType<MemberAccessExpressionSyntax>())
        {
            if (access.Ancestors().OfType<MethodDeclarationSyntax>()
                .Any(EditorCodePolicy.IsEditorOnlyMember)) continue;
            var type = model.GetTypeInfo(access.Expression).Type as INamedTypeSymbol;
            var symbol = model.GetSymbolInfo(access.Expression).Symbol?.ToDisplayString();
            var isTexture2D = type?.ToDisplayString() == "UnityEngine.Texture2D"
                || symbol == "UnityEngine.Texture2D";
            var isTexture = symbol == "UnityEngine.Texture" || symbol == "UnityEngine.Texture2D"
                || type?.ToDisplayString() == "UnityEngine.Texture";
            for (var baseType = type?.BaseType; baseType != null; baseType = baseType.BaseType)
                if (baseType.ToDisplayString() == "UnityEngine.Texture") isTexture = true;
            var name = access.Name.Identifier.ValueText;
            var nativeRawData = isTexture2D && name == "LoadRawTextureData"
                && access.Parent is InvocationExpressionSyntax rawDataCall
                && rawDataCall.ArgumentList.Arguments.Count == 2;
            if (!(isTexture && RendererBoundTextureMembers.Contains(name)
                || isTexture2D && RendererBoundTexture2DMembers.Contains(name)
                || nativeRawData)) continue;
            var location = access.GetLocation().GetLineSpan();
            var owner = isTexture2D && (RendererBoundTexture2DMembers.Contains(name) || nativeRawData)
                ? "Texture2D" : "Texture";
            throw new InvalidOperationException($"{location.Path}:{location.StartLinePosition.Line + 1}:"
                + $"{location.StartLinePosition.Character + 1}: Preview defers UnityEngine.{owner}."
                + $"{name}: native texture handles, compression, or GPU residency"
                + " controls are outside the Three.js texture data bridge");
        }
        foreach (var creation in declaration.DescendantNodes().OfType<ObjectCreationExpressionSyntax>())
        {
            if (creation.Ancestors().OfType<MethodDeclarationSyntax>()
                .Any(EditorCodePolicy.IsEditorOnlyMember)
                || creation.ArgumentList?.Arguments.Count < 6) continue;
            var type = model.GetTypeInfo(creation).Type?.ToDisplayString()
                ?? model.GetSymbolInfo(creation.Type).Symbol?.ToDisplayString();
            if (type != "UnityEngine.Texture2D") continue;
            var location = creation.GetLocation().GetLineSpan();
            throw new InvalidOperationException($"{location.Path}:{location.StartLinePosition.Line + 1}:"
                + $"{location.StartLinePosition.Character + 1}: Preview defers UnityEngine.Texture2D constructor:"
                + " advanced native texture creation options are outside the Three.js texture data bridge");
        }
    }

    public static void RejectNativeToolkitUssNames(SyntaxTree tree, SemanticModel model)
    {
        foreach (var access in tree.GetRoot().DescendantNodes()
            .OfType<MemberAccessExpressionSyntax>())
        {
            if (!access.Name.Identifier.ValueText.Contains("ussClassName",
                    StringComparison.OrdinalIgnoreCase)) continue;
            var owner = model.GetSymbolInfo(access).Symbol?.ContainingType?.ToDisplayString()
                ?? model.GetTypeInfo(access.Expression).Type?.ToDisplayString()
                ?? model.GetSymbolInfo(access.Expression).Symbol?.ToDisplayString();
            if (owner?.StartsWith("UnityEngine.UIElements.",
                    StringComparison.Ordinal) != true
                || owner is "UnityEngine.UIElements.ScrollView"
                    or "UnityEngine.UIElements.Tab"
                    or "UnityEngine.UIElements.GenericDropdownMenu"
                    or "UnityEngine.UIElements.VisualElement") continue;
            var location = access.GetLocation().GetLineSpan();
            throw new InvalidOperationException($"{location.Path}:"
                + $"{location.StartLinePosition.Line + 1}:"
                + $"{location.StartLinePosition.Character + 1}: Preview defers "
                + $"{owner}.{access.Name.Identifier.ValueText}: native control USS"
                + " class names are outside the DOM control styling contract;"
                + " see ui-browser-boundary.md");
        }
    }

    public static void Reject(BaseTypeDeclarationSyntax declaration, SemanticModel model)
    {
        foreach (var creation in declaration.DescendantNodes().OfType<ObjectCreationExpressionSyntax>())
            if (model.GetTypeInfo(creation).Type?.ToDisplayString()
                == "UnityEngine.UIElements.FocusController")
            {
                var focusLocation = creation.GetLocation().GetLineSpan();
                throw new InvalidOperationException($"{focusLocation.Path}:"
                    + $"{focusLocation.StartLinePosition.Line + 1}:"
                    + $"{focusLocation.StartLinePosition.Character + 1}: Preview defers"
                    + " FocusController(IFocusRing): native focus-ring construction is outside"
                    + " the browser DOM focus route; see ui-browser-boundary.md");
            }
        foreach (var access in declaration.DescendantNodes().OfType<MemberAccessExpressionSyntax>())
        {
            var backgroundOwner = model.GetSymbolInfo(access).Symbol?.ContainingType?.ToDisplayString()
                ?? model.GetTypeInfo(access.Expression).Type?.ToDisplayString();
            if (backgroundOwner == "UnityEngine.UIElements.VisualElementExtensions"
                && access.Name.Identifier.ValueText is "AddManipulator" or "RemoveManipulator")
            {
                var manipulatorLocation = access.GetLocation().GetLineSpan();
                throw new InvalidOperationException($"{manipulatorLocation.Path}:"
                    + $"{manipulatorLocation.StartLinePosition.Line + 1}:"
                    + $"{manipulatorLocation.StartLinePosition.Character + 1}: Preview defers"
                    + $" VisualElementExtensions.{access.Name.Identifier.ValueText}: custom"
                    + " native manipulators are outside the DOM event route;"
                    + " see ui-browser-boundary.md");
            }
            if (backgroundOwner == "UnityEngine.UIElements.ToggleButtonGroup"
                && access.Name.Identifier.ValueText.EndsWith("ClassName", StringComparison.Ordinal))
            {
                var classLocation = access.GetLocation().GetLineSpan();
                throw new InvalidOperationException($"{classLocation.Path}:"
                    + $"{classLocation.StartLinePosition.Line + 1}:"
                    + $"{classLocation.StartLinePosition.Character + 1}: Preview defers ToggleButtonGroup."
                    + $"{access.Name.Identifier.ValueText}: Unity's internal USS class names"
                    + " are outside the browser control styling contract; see ui-browser-boundary.md");
            }
            if (backgroundOwner == "UnityEngine.UIElements.TabView"
                && access.Name.Identifier.ValueText == "headerContainerClassName")
            {
                var classLocation = access.GetLocation().GetLineSpan();
                throw new InvalidOperationException($"{classLocation.Path}:"
                    + $"{classLocation.StartLinePosition.Line + 1}:"
                    + $"{classLocation.StartLinePosition.Character + 1}: Preview defers"
                    + " TabView.headerContainerClassName: Unity's internal tab header"
                    + " USS structure is outside the browser DOM control;"
                    + " see ui-browser-boundary.md");
            }
            if (backgroundOwner == "UnityEngine.UIElements.FocusController"
                && access.Name.Identifier.ValueText == "IgnoreEvent")
            {
                var focusLocation = access.GetLocation().GetLineSpan();
                throw new InvalidOperationException($"{focusLocation.Path}:"
                    + $"{focusLocation.StartLinePosition.Line + 1}:"
                    + $"{focusLocation.StartLinePosition.Character + 1}: Preview defers FocusController."
                    + "IgnoreEvent: native focus-ring suppression is outside the browser DOM focus route;"
                    + " see ui-browser-boundary.md");
            }
            if (backgroundOwner?.StartsWith("UnityEngine.UIElements.", StringComparison.Ordinal) == true
                && access.Name.Identifier.ValueText == "GetPooled"
                && access.Parent is InvocationExpressionSyntax pooledCall
                && pooledCall.ArgumentList.Arguments.Any(argument =>
                    model.GetTypeInfo(argument.Expression).Type?.ToDisplayString()
                    is "UnityEngine.Event" or "UnityEngine.PenData" or "UnityEngine.Touch"))
            {
                var pooledLocation = access.GetLocation().GetLineSpan();
                throw new InvalidOperationException($"{pooledLocation.Path}:"
                    + $"{pooledLocation.StartLinePosition.Line + 1}:"
                    + $"{pooledLocation.StartLinePosition.Character + 1}: Preview defers native UI Toolkit"
                    + " GetPooled(UnityEngine.Event/PenData/Touch): browser DOM events are"
                    + " constructed by the shared event route; see ui-browser-boundary.md");
            }
            if (backgroundOwner == "UnityEngine.GUI.WindowFunction"
                && access.Name.Identifier.ValueText is "BeginInvoke" or "EndInvoke")
            {
                var imguiDelegateLocation = access.GetLocation().GetLineSpan();
                throw new InvalidOperationException($"{imguiDelegateLocation.Path}:"
                    + $"{imguiDelegateLocation.StartLinePosition.Line + 1}:"
                    + $"{imguiDelegateLocation.StartLinePosition.Character + 1}: Preview defers GUI.WindowFunction."
                    + $"{access.Name.Identifier.ValueText}: native asynchronous delegate invocation"
                    + " is outside browser OnGUI; see ui-browser-boundary.md");
            }
            if (backgroundOwner?.StartsWith("UnityEngine.UIElements.EventCallback<",
                    StringComparison.Ordinal) == true
                && access.Name.Identifier.ValueText is "BeginInvoke" or "EndInvoke")
            {
                var callbackLocation = access.GetLocation().GetLineSpan();
                throw new InvalidOperationException($"{callbackLocation.Path}:"
                    + $"{callbackLocation.StartLinePosition.Line + 1}:"
                    + $"{callbackLocation.StartLinePosition.Character + 1}: Preview defers"
                    + $" UI Toolkit EventCallback.{access.Name.Identifier.ValueText}: native"
                    + " asynchronous delegate invocation is outside the browser event route;"
                    + " see ui-browser-boundary.md");
            }
            if (backgroundOwner == "UnityEngine.UIElements.PanelRaycaster"
                && access.Name.Identifier.ValueText == "panel")
            {
                var panelRaycasterLocation = access.GetLocation().GetLineSpan();
                throw new InvalidOperationException($"{panelRaycasterLocation.Path}:"
                    + $"{panelRaycasterLocation.StartLinePosition.Line + 1}:"
                    + $"{panelRaycasterLocation.StartLinePosition.Character + 1}: Preview defers PanelRaycaster.panel:"
                    + " Unity's native panel handle is outside the DOM screen UI route;"
                    + " see ui-browser-boundary.md");
            }
            if (backgroundOwner?.StartsWith(
                    "UnityEngine.EventSystems.ExecuteEvents.EventFunction<",
                    StringComparison.Ordinal) == true
                    && access.Name.Identifier.ValueText is "BeginInvoke" or "EndInvoke")
            {
                var nativeEventLocation = access.GetLocation().GetLineSpan();
                throw new InvalidOperationException($"{nativeEventLocation.Path}:"
                    + $"{nativeEventLocation.StartLinePosition.Line + 1}:"
                    + $"{nativeEventLocation.StartLinePosition.Character + 1}: Preview defers "
                    + $"{backgroundOwner}.{access.Name.Identifier.ValueText}: native delegate"
                    + " asynchronous invocation is outside browser event dispatch;"
                    + " see ui-browser-boundary.md");
            }
            if (backgroundOwner == "UnityEngine.EventSystems.EventSystem"
                && access.Name.Identifier.ValueText == "SetUITookitEventSystemOverride"
                || backgroundOwner == "UnityEngine.EventSystems.BaseInputModule"
                    && access.Name.Identifier.ValueText == "Process"
                || backgroundOwner == "UnityEngine.EventSystems.StandaloneInputModule"
                    && access.Name.Identifier.ValueText is "Process" or "ActivateModule"
                        or "DeactivateModule" or "ShouldActivateModule" or "UpdateModule")
            {
                var eventLocation = access.GetLocation().GetLineSpan();
                throw new InvalidOperationException($"{eventLocation.Path}:"
                    + $"{eventLocation.StartLinePosition.Line + 1}:"
                    + $"{eventLocation.StartLinePosition.Character + 1}: Preview defers "
                    + $"{backgroundOwner}.{access.Name.Identifier.ValueText}: Unity input-module"
                    + " lifecycle and native panel override are handled by the shared browser"
                    + " event route; see ui-browser-boundary.md");
            }
            if (backgroundOwner == "UnityEngine.UIElements.Background"
                && access.Name.Identifier.ValueText is "FromRenderTexture" or "FromVectorImage"
                    or "renderTexture" or "vectorImage")
            {
                var backgroundLocation = access.GetLocation().GetLineSpan();
                throw new InvalidOperationException($"{backgroundLocation.Path}:"
                    + $"{backgroundLocation.StartLinePosition.Line + 1}:"
                    + $"{backgroundLocation.StartLinePosition.Character + 1}: Preview defers Background."
                    + $"{access.Name.Identifier.ValueText}: render-target and vector-image backgrounds"
                    + " are outside the DOM image route; see ui-browser-boundary.md");
            }
            if (backgroundOwner == "UnityEngine.UIElements.BackgroundPropertyHelper"
                && access.Name.Identifier.ValueText == "ResolveUnityBackgroundScaleMode")
            {
                var styleLocation = access.GetLocation().GetLineSpan();
                throw new InvalidOperationException($"{styleLocation.Path}:"
                    + $"{styleLocation.StartLinePosition.Line + 1}:"
                    + $"{styleLocation.StartLinePosition.Character + 1}: Preview defers BackgroundPropertyHelper."
                    + "ResolveUnityBackgroundScaleMode: native out-parameter style reverse mapping"
                    + " is outside the DOM image route; see ui-browser-boundary.md");
            }
            if (backgroundOwner == "UnityEngine.UIElements.StyleSheet"
                && access.Name.Identifier.ValueText is "contentHash" or "importedWithErrors"
                    or "importedWithWarnings")
            {
                var styleLocation = access.GetLocation().GetLineSpan();
                throw new InvalidOperationException($"{styleLocation.Path}:"
                    + $"{styleLocation.StartLinePosition.Line + 1}:"
                    + $"{styleLocation.StartLinePosition.Character + 1}: Preview defers StyleSheet."
                    + $"{access.Name.Identifier.ValueText}: Unity asset-import metadata"
                    + " is not present in browser USS assets; see ui-browser-boundary.md");
            }
            if (BrowserUiNativeStyleMembers.Contains(access.Name.Identifier.ValueText)
                && (backgroundOwner is "UnityEngine.UIElements.IStyle"
                    or "UnityEngine.UIElements.IResolvedStyle"
                    || access.Expression is MemberAccessExpressionSyntax styleOwner
                        && styleOwner.Name.Identifier.ValueText is "style" or "resolvedStyle"))
            {
                var styleLocation = access.GetLocation().GetLineSpan();
                throw new InvalidOperationException($"{styleLocation.Path}:"
                    + $"{styleLocation.StartLinePosition.Line + 1}:"
                    + $"{styleLocation.StartLinePosition.Character + 1}: Preview defers UI Toolkit style."
                    + $"{access.Name.Identifier.ValueText}: native filter, text layout, or editor"
                    + " renderer state is outside the DOM style bridge; see ui-browser-boundary.md");
            }
            var textInputOwner = model.GetSymbolInfo(access).Symbol?.ContainingType;
            if (textInputOwner?.ToDisplayString() == "UnityEngine.UIElements.TextElement"
                && access.Name.Identifier.ValueText == "emojiFallbackSupport")
            {
                var emojiLocation = access.GetLocation().GetLineSpan();
                throw new InvalidOperationException($"{emojiLocation.Path}:"
                    + $"{emojiLocation.StartLinePosition.Line + 1}:"
                    + $"{emojiLocation.StartLinePosition.Character + 1}: Preview defers"
                    + " TextElement.emojiFallbackSupport: browser font fallback is owned"
                    + " by the DOM renderer; see ui-browser-boundary.md");
            }
            if (BrowserTextInputHostMembers.Contains(access.Name.Identifier.ValueText)
                && textInputOwner?.Name is "TextInputBaseField" or "ITextEdition"
                && textInputOwner.ContainingNamespace.ToDisplayString()
                    == "UnityEngine.UIElements")
            {
                var textLocation = access.GetLocation().GetLineSpan();
                throw new InvalidOperationException($"{textLocation.Path}:"
                    + $"{textLocation.StartLinePosition.Line + 1}:"
                    + $"{textLocation.StartLinePosition.Character + 1}: Preview defers TextInputBaseField."
                    + $"{access.Name.Identifier.ValueText}: native caret geometry, keyboard host,"
                    + " text measurement, and USS internals are outside DOM text editing;"
                    + " see ui-browser-boundary.md");
            }
            if (BrowserTextSelectionNativeMembers.Contains(access.Name.Identifier.ValueText)
                && textInputOwner?.Name == "ITextSelection"
                || BrowserTextValueNativeMembers.Contains(access.Name.Identifier.ValueText)
                    && textInputOwner?.Name is "TextValueField" or "IValueField"
                    && textInputOwner.ContainingNamespace.ToDisplayString()
                        == "UnityEngine.UIElements")
            {
                var textLocation = access.GetLocation().GetLineSpan();
                throw new InvalidOperationException($"{textLocation.Path}:"
                    + $"{textLocation.StartLinePosition.Line + 1}:"
                    + $"{textLocation.StartLinePosition.Character + 1}: Preview defers"
                    + $" {textInputOwner?.Name}.{access.Name.Identifier.ValueText}:"
                    + " native glyph metrics, selection callbacks, or drag-expression"
                    + " editing are outside browser text inputs; see ui-browser-boundary.md");
            }
            var sliderOwner = model.GetSymbolInfo(access).Symbol?.ContainingType;
            if (BrowserBaseSliderNativeMembers.Contains(access.Name.Identifier.ValueText)
                && sliderOwner?.Name == "BaseSlider"
                && sliderOwner.ContainingNamespace.ToDisplayString() == "UnityEngine.UIElements")
            {
                var sliderLocation = access.GetLocation().GetLineSpan();
                throw new InvalidOperationException($"{sliderLocation.Path}:"
                    + $"{sliderLocation.StartLinePosition.Line + 1}:"
                    + $"{sliderLocation.StartLinePosition.Character + 1}: Preview defers BaseSlider."
                    + $"{access.Name.Identifier.ValueText}: native drag presentation and USS"
                    + " internals are outside the DOM range control; see ui-browser-boundary.md");
            }
            if (access.Name.Identifier.ValueText.Contains("ussClassName",
                    StringComparison.OrdinalIgnoreCase)
                && backgroundOwner?.StartsWith("UnityEngine.UIElements.",
                    StringComparison.Ordinal) == true
                && backgroundOwner is not "UnityEngine.UIElements.ScrollView"
                    and not "UnityEngine.UIElements.Tab"
                    and not "UnityEngine.UIElements.GenericDropdownMenu"
                    and not "UnityEngine.UIElements.VisualElement")
            {
                var ussLocation = access.GetLocation().GetLineSpan();
                throw new InvalidOperationException($"{ussLocation.Path}:"
                    + $"{ussLocation.StartLinePosition.Line + 1}:"
                    + $"{ussLocation.StartLinePosition.Character + 1}: Preview defers "
                    + $"{backgroundOwner}.{access.Name.Identifier.ValueText}: native control USS"
                    + " class names are outside the DOM control styling contract;"
                    + " see ui-browser-boundary.md");
            }
            if (backgroundOwner is "UnityEngine.UI.Graphic"
                    or "UnityEngine.UI.MaskableGraphic" or "UnityEngine.UI.Mask"
                && access.Name.Identifier.ValueText is "defaultGraphicMaterial"
                    or "defaultMaterial" or "depth" or "GetModifiedMaterial"
                    or "SetClipRect" or "SetClipSoftness")
            {
                var graphicLocation = access.GetLocation().GetLineSpan();
                throw new InvalidOperationException($"{graphicLocation.Path}:"
                    + $"{graphicLocation.StartLinePosition.Line + 1}:"
                    + $"{graphicLocation.StartLinePosition.Character + 1}: Preview defers "
                    + $"{backgroundOwner}.{access.Name.Identifier.ValueText}: native uGUI"
                    + " material, absolute-depth, and clip state are outside the DOM"
                    + " Canvas renderer; see ui-browser-boundary.md");
            }
            if (backgroundOwner == "UnityEngine.UI.IClippable"
                && access.Name.Identifier.ValueText is "SetClipRect" or "SetClipSoftness"
                || backgroundOwner == "UnityEngine.UI.Image"
                && access.Name.Identifier.ValueText is "defaultETC1GraphicMaterial"
                    or "material" or "useSpriteMesh"
                || backgroundOwner == "UnityEngine.UI.Text"
                && access.Name.Identifier.ValueText is "cachedTextGenerator"
                    or "cachedTextGeneratorForLayout" or "GetGenerationSettings"
                    or "mainTexture"
                || backgroundOwner == "UnityEngine.UI.InputField.OnValidateInput"
                && access.Name.Identifier.ValueText is "BeginInvoke" or "EndInvoke")
            {
                var nativeLocation = access.GetLocation().GetLineSpan();
                throw new InvalidOperationException($"{nativeLocation.Path}:"
                    + $"{nativeLocation.StartLinePosition.Line + 1}:"
                    + $"{nativeLocation.StartLinePosition.Character + 1}: Preview defers "
                    + $"{backgroundOwner}.{access.Name.Identifier.ValueText}: native uGUI"
                    + " clip, material, sprite mesh, font-atlas, or delegate-async"
                    + " state is outside the browser Canvas renderer;"
                    + " see ui-browser-boundary.md");
            }
            if (BrowserCanvasRendererNativeMembers.Contains(access.Name.Identifier.ValueText)
                && model.GetTypeInfo(access.Expression).Type?.ToDisplayString()
                    == "UnityEngine.CanvasRenderer")
            {
                var rendererLocation = access.GetLocation().GetLineSpan();
                throw new InvalidOperationException($"{rendererLocation.Path}:"
                    + $"{rendererLocation.StartLinePosition.Line + 1}:"
                    + $"{rendererLocation.StartLinePosition.Character + 1}: Preview defers CanvasRenderer."
                    + $"{access.Name.Identifier.ValueText}: native uGUI mesh, clipping, and"
                    + " rebuild state are outside the DOM overlay; see ui-browser-boundary.md");
            }
            if (access.Name.Identifier.ValueText == "ModifyMesh"
                && model.GetTypeInfo(access.Expression).Type?.ToDisplayString()
                    is "UnityEngine.UI.Shadow" or "UnityEngine.UI.Outline")
            {
                var meshLocation = access.GetLocation().GetLineSpan();
                throw new InvalidOperationException($"{meshLocation.Path}:"
                    + $"{meshLocation.StartLinePosition.Line + 1}:"
                    + $"{meshLocation.StartLinePosition.Character + 1}: Preview defers "
                    + "uGUI ModifyMesh: native vertex edits are outside the DOM Canvas renderer;"
                    + " see ui-browser-boundary.md");
            }
            if (BrowserCanvasNativeMembers.Contains(access.Name.Identifier.ValueText)
                && model.GetTypeInfo(access.Expression).Type?.ToDisplayString()
                    == "UnityEngine.Canvas")
            {
                var canvasLocation = access.GetLocation().GetLineSpan();
                throw new InvalidOperationException($"{canvasLocation.Path}:"
                    + $"{canvasLocation.StartLinePosition.Line + 1}:"
                    + $"{canvasLocation.StartLinePosition.Character + 1}: Preview defers Canvas."
                    + $"{access.Name.Identifier.ValueText}: native uGUI material, mesh, and"
                    + " render-order controls are outside browser screen overlay;"
                    + " see ui-browser-boundary.md");
            }
            if (BrowserPanelNativeMembers.Contains(access.Name.Identifier.ValueText)
                && model.GetTypeInfo(access.Expression).Type?.ToDisplayString()
                    == "UnityEngine.UIElements.PanelSettings")
            {
                var panelLocation = access.GetLocation().GetLineSpan();
                throw new InvalidOperationException($"{panelLocation.Path}:"
                    + $"{panelLocation.StartLinePosition.Line + 1}:"
                    + $"{panelLocation.StartLinePosition.Character + 1}: Preview defers PanelSettings."
                    + $"{access.Name.Identifier.ValueText}: native panel rendering and custom coordinate mapping"
                    + " are outside browser screen UI; see ui-browser-boundary.md");
            }
            var toolkitPanelOwner = model.GetSymbolInfo(access).Symbol?.ContainingType?.ToDisplayString();
            toolkitPanelOwner ??= model.GetTypeInfo(access.Expression).Type?.ToDisplayString();
            if (BrowserVisualElementNativeMembers.Contains(access.Name.Identifier.ValueText)
                && toolkitPanelOwner is "UnityEngine.UIElements.VisualElement"
                    or "UnityEngine.UIElements.TextElement")
            {
                var nativeLocation = access.GetLocation().GetLineSpan();
                throw new InvalidOperationException($"{nativeLocation.Path}:"
                    + $"{nativeLocation.StartLinePosition.Line + 1}:"
                    + $"{nativeLocation.StartLinePosition.Character + 1}: Preview defers"
                    + $" {toolkitPanelOwner}.{access.Name.Identifier.ValueText}: native UI Toolkit"
                    + " transform, glyph, or renderer metadata is outside the DOM UI renderer;"
                    + " see ui-browser-boundary.md");
            }
            if (toolkitPanelOwner == "UnityEngine.UIElements.Image"
                && access.Name.Identifier.ValueText == "vectorImage")
            {
                var nativeLocation = access.GetLocation().GetLineSpan();
                throw new InvalidOperationException($"{nativeLocation.Path}:"
                    + $"{nativeLocation.StartLinePosition.Line + 1}:"
                    + $"{nativeLocation.StartLinePosition.Character + 1}: Preview defers"
                    + " Image.vectorImage: Unity vector-image meshes are outside the DOM"
                    + " image route; see ui-browser-boundary.md");
            }
            if (BrowserToolkitPanelNativeMembers.Contains(access.Name.Identifier.ValueText)
                && toolkitPanelOwner is "UnityEngine.UIElements.IPanel"
                    or "UnityEngine.UIElements.IRuntimePanel"
                    or "UnityEngine.UIElements.RuntimePanelUtils"
                    or "UnityEngine.UIElements.UIDocument"
                    or "UnityEngine.UIElements.VisualTreeAsset")
            {
                var nativeLocation = access.GetLocation().GetLineSpan();
                throw new InvalidOperationException($"{nativeLocation.Path}:"
                    + $"{nativeLocation.StartLinePosition.Line + 1}:"
                    + $"{nativeLocation.StartLinePosition.Character + 1}: Preview defers"
                    + $" {toolkitPanelOwner}.{access.Name.Identifier.ValueText}:"
                    + " native panel, world document, or import metadata is outside"
                    + " browser screen UI; see ui-browser-boundary.md");
            }
            var collectionMember = access.Name.Identifier.ValueText;
            var collectionOwner = model.GetSymbolInfo(access).Symbol?.ContainingType?.ToDisplayString();
            collectionOwner ??= model.GetTypeInfo(access.Expression).Type?.ToDisplayString();
            if ((collectionOwner is "UnityEngine.UIElements.BaseListView"
                or "UnityEngine.UIElements.BaseVerticalCollectionView"
                or "UnityEngine.UIElements.ListView"
                or "UnityEngine.UIElements.TreeView"
                or "UnityEngine.UIElements.MultiColumnListView"
                or "UnityEngine.UIElements.MultiColumnTreeView")
                && (collectionMember.EndsWith("ClassName", StringComparison.Ordinal)
                    || collectionMember is "footerAddButtonName" or "footerRemoveButtonName"
                        or "viewController" or "SetViewController" or "reorderMode"
                        or "overridingAddButtonBehavior" or "bindingSourceSelectionMode"))
            {
                var collectionLocation = access.GetLocation().GetLineSpan();
                throw new InvalidOperationException($"{collectionLocation.Path}:"
                    + $"{collectionLocation.StartLinePosition.Line + 1}:"
                    + $"{collectionLocation.StartLinePosition.Character + 1}: Preview defers "
                    + $"{collectionOwner}.{collectionMember}: native collection styling, "
                    + "controller, and binding internals are outside the DOM collection bridge; "
                    + "see ui-browser-boundary.md");
            }
            if (BrowserInputFieldHostMembers.Contains(access.Name.Identifier.ValueText)
                && model.GetSymbolInfo(access).Symbol?.ContainingType?.ToDisplayString()
                    == "UnityEngine.UI.InputField")
            {
                var inputLocation = access.GetLocation().GetLineSpan();
                throw new InvalidOperationException($"{inputLocation.Path}:{inputLocation.StartLinePosition.Line + 1}:"
                    + $"{inputLocation.StartLinePosition.Character + 1}: Preview defers InputField."
                    + $"{access.Name.Identifier.ValueText}: native browser text editing owns this host"
                    + " event path; see ui-browser-boundary.md");
            }
            if (!BrowserUiRendererStyleMembers.Contains(access.Name.Identifier.ValueText)
                || access.Ancestors().OfType<MethodDeclarationSyntax>()
                    .Any(EditorCodePolicy.IsEditorOnlyMember)) continue;
            var receiver = model.GetTypeInfo(access.Expression).Type?.ToDisplayString();
            var directStyle = access.Expression is MemberAccessExpressionSyntax owner
                && owner.Name.Identifier.ValueText is "style" or "resolvedStyle";
            if (receiver is not ("UnityEngine.UIElements.IStyle"
                or "UnityEngine.UIElements.IResolvedStyle") && !directStyle) continue;
            var location = access.GetLocation().GetLineSpan();
            throw new InvalidOperationException($"{location.Path}:{location.StartLinePosition.Line + 1}:"
                + $"{location.StartLinePosition.Character + 1}: Preview defers UI Toolkit style."
                + $"{access.Name.Identifier.ValueText}: Unity material, text generator, and nine-slice"
                + " rendering are outside the single DOM UI renderer; see ui-browser-boundary.md");
        }
        foreach (var creation in declaration.DescendantNodes().OfType<ObjectCreationExpressionSyntax>())
        {
            if (model.GetSymbolInfo(creation).Symbol is not IMethodSymbol constructor
                || constructor.ContainingType.ToDisplayString() != "UnityEngine.UIElements.StyleBackground"
                || constructor.Parameters.Length != 1
                || constructor.Parameters[0].Type.ToDisplayString() != "UnityEngine.UIElements.VectorImage")
                continue;
            var backgroundLocation = creation.GetLocation().GetLineSpan();
            throw new InvalidOperationException($"{backgroundLocation.Path}:"
                + $"{backgroundLocation.StartLinePosition.Line + 1}:"
                + $"{backgroundLocation.StartLinePosition.Character + 1}: Preview defers StyleBackground(VectorImage):"
                + " vector-image drawing is outside the DOM image route; see ui-browser-boundary.md");
        }
        foreach (var invocation in declaration.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            if (invocation.Ancestors().OfType<MethodDeclarationSyntax>()
                .Any(EditorCodePolicy.IsEditorOnlyMember)) continue;
            if (model.GetSymbolInfo(invocation).Symbol is IMethodSymbol customStyleMethod
                && customStyleMethod.ContainingType.ToDisplayString()
                    == "UnityEngine.UIElements.ICustomStyle"
                && customStyleMethod.Name == "TryGetValue"
                && customStyleMethod.Parameters.Length > 0
                && customStyleMethod.Parameters[0].Type.ToDisplayString() is string propertyType
                && (propertyType.Contains("UnityEngine.Sprite", StringComparison.Ordinal)
                    || propertyType.Contains("UnityEngine.Texture2D", StringComparison.Ordinal)
                    || propertyType.Contains("UnityEngine.UIElements.VectorImage", StringComparison.Ordinal)))
            {
                var styleLocation = invocation.GetLocation().GetLineSpan();
                throw new InvalidOperationException($"{styleLocation.Path}:"
                    + $"{styleLocation.StartLinePosition.Line + 1}:"
                    + $"{styleLocation.StartLinePosition.Character + 1}: Preview defers"
                    + " ICustomStyle.TryGetValue for image assets: custom USS image assets"
                    + " lack a browser object conversion; see ui-browser-boundary.md");
            }
            if (model.GetSymbolInfo(invocation).Symbol is IMethodSymbol cloneMethod
                && cloneMethod.ContainingType.ToDisplayString()
                    == "UnityEngine.UIElements.VisualTreeAsset"
                && cloneMethod.Name == "CloneTree" && cloneMethod.Parameters.Length == 3)
            {
                var cloneLocation = invocation.GetLocation().GetLineSpan();
                throw new InvalidOperationException($"{cloneLocation.Path}:"
                    + $"{cloneLocation.StartLinePosition.Line + 1}:"
                    + $"{cloneLocation.StartLinePosition.Character + 1}: Preview defers"
                    + " VisualTreeAsset.CloneTree out indices: exported UXML element IDs"
                    + " are authoring metadata; use CloneTree(target); see ui-browser-boundary.md");
            }
            if (model.GetSymbolInfo(invocation).Symbol is IMethodSymbol menuMethod
                && menuMethod.ContainingType.ToDisplayString()
                    == "UnityEngine.UIElements.GenericDropdownMenu"
                && menuMethod.Name == "DropDown" && menuMethod.Parameters.Length == 1)
            {
                var menuLocation = invocation.GetLocation().GetLineSpan();
                throw new InvalidOperationException($"{menuLocation.Path}:"
                    + $"{menuLocation.StartLinePosition.Line + 1}:"
                    + $"{menuLocation.StartLinePosition.Character + 1}: Preview defers"
                    + " GenericDropdownMenu.DropDown(Rect): the browser menu needs a"
                    + " VisualElement target; use DropDown(position, target);"
                    + " see ui-browser-boundary.md");
            }
            if (model.GetSymbolInfo(invocation).Symbol is not IMethodSymbol method
                || method.ContainingType.ToDisplayString() != "UnityEngine.Awaitable"
                || method.Name != "BackgroundThreadAsync") continue;
            var location = invocation.GetLocation().GetLineSpan();
            throw new InvalidOperationException($"{location.Path}:{location.StartLinePosition.Line + 1}:"
                + $"{location.StartLinePosition.Character + 1}: Preview defers UnityEngine.Awaitable.BackgroundThreadAsync:"
                + " browser game code has no Unity worker-thread context");
        }
        RejectAdvancedLight(declaration, model);
        RejectAdvancedMesh(declaration, model);
        RejectAdvancedLineRenderer(declaration, model);
        RejectAdvancedTrailRenderer(declaration, model);
        RejectAdvancedMeshRenderer(declaration, model);
        RejectHostEditorAttributes(declaration, model);
        RejectAdvancedCamera(declaration, model);
        RejectAdvancedRenderer(declaration, model);
        RejectAdvancedMaterial(declaration, model);
        foreach (var access in declaration.DescendantNodes().OfType<MemberAccessExpressionSyntax>())
        {
            if (access.Ancestors().OfType<MethodDeclarationSyntax>()
                .Any(EditorCodePolicy.IsEditorOnlyMember)) continue;
            var receiver = model.GetTypeInfo(access.Expression).Type?.ToDisplayString()
                ?? model.GetSymbolInfo(access.Expression).Symbol?.ToDisplayString();
            var isCallback = receiver == "UnityEngine.RectTransform"
                && access.Name.Identifier.ValueText == "reapplyDrivenProperties";
            var isUndo = receiver == "UnityEngine.DrivenRectTransformTracker"
                && access.Name.Identifier.ValueText is "StartRecordingUndo" or "StopRecordingUndo";
            if (!isCallback && !isUndo) continue;
            var location = access.GetLocation().GetLineSpan();
            throw new InvalidOperationException($"{location.Path}:{location.StartLinePosition.Line + 1}:"
                + $"{location.StartLinePosition.Character + 1}: Preview defers {receiver}."
                + $"{access.Name.Identifier.ValueText}: Unity UGUI layout callbacks and editor undo"
                + " are outside the current browser UI layout bridge");
        }
        var root = declaration.SyntaxTree.GetRoot();
        foreach (var call in declaration.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            if (call.Ancestors().OfType<MethodDeclarationSyntax>()
                .Any(EditorCodePolicy.IsEditorOnlyMember)) continue;
            if (model.GetSymbolInfo(call).Symbol is not IMethodSymbol method
                || method.ContainingType.ToDisplayString() != "UnityEngine.GUIStyle"
                || method.Name is not ("DrawCursor" or "DrawWithTextSelection")) continue;
            var location = call.GetLocation().GetLineSpan();
            throw new InvalidOperationException($"{location.Path}:{location.StartLinePosition.Line + 1}:"
                + $"{location.StartLinePosition.Character + 1}: Preview defers UnityEngine.GUIStyle."
                + $"{method.Name}: native browser text inputs own caret and selection painting;"
                + " see ui-browser-boundary.md");
        }
        foreach (var call in declaration.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            if (call.Ancestors().OfType<MethodDeclarationSyntax>()
                .Any(EditorCodePolicy.IsEditorOnlyMember)) continue;
            if (model.GetSymbolInfo(call).Symbol is not IMethodSymbol method
                || method.ContainingType.ToDisplayString() != "UnityEngine.GUI") continue;
            var borderTexture = method.Name == "DrawTexture" && method.Parameters.Length >= 8;
            var offsetClip = method.Name == "BeginClip" && method.Parameters.Length == 4;
            if (!borderTexture && !offsetClip) continue;
            var location = call.GetLocation().GetLineSpan();
            var reason = borderTexture
                ? "patterned texture borders and rounded shader corners are outside the shared browser UI renderer"
                : "the offset clip matrix is outside the shared browser UI renderer";
            throw new InvalidOperationException($"{location.Path}:{location.StartLinePosition.Line + 1}:"
                + $"{location.StartLinePosition.Character + 1}: Preview defers UnityEngine.GUI."
                + $"{method.Name}: {reason}; see ui-browser-boundary.md");
        }
        foreach (var call in declaration.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            if (call.Ancestors().OfType<MethodDeclarationSyntax>()
                .Any(EditorCodePolicy.IsEditorOnlyMember)) continue;
            if (model.GetSymbolInfo(call).Symbol is not IMethodSymbol method
                || method.ContainingType.ToDisplayString() != "UnityEngine.GUIUtility") continue;
            var outPixels = method.Name == "AlignRectToDevice" && method.Parameters.Length == 3;
            if (!outPixels) continue;
            var location = call.GetLocation().GetLineSpan();
            var reason = "device-pixel out parameters are outside the browser C# bridge";
            throw new InvalidOperationException($"{location.Path}:{location.StartLinePosition.Line + 1}:"
                + $"{location.StartLinePosition.Character + 1}: Preview defers UnityEngine.GUIUtility."
                + $"{method.Name}: {reason}; see ui-browser-boundary.md");
        }
        foreach (var access in declaration.DescendantNodes().OfType<MemberAccessExpressionSyntax>())
        {
            if (access.Ancestors().OfType<MethodDeclarationSyntax>()
                .Any(EditorCodePolicy.IsEditorOnlyMember)) continue;
            if (model.GetSymbolInfo(access).Symbol is not IPropertySymbol property) continue;
            var clipboard = property.ContainingType.ToDisplayString() == "UnityEngine.GUIUtility"
                && property.Name == "systemCopyBuffer";
            var mouseRay = property.ContainingType.ToDisplayString() == "UnityEngine.Event"
                && property.Name == "mouseRay";
            if (!clipboard && !mouseRay) continue;
            var location = access.GetLocation().GetLineSpan();
            var reason = clipboard ? "synchronous native clipboard access is unavailable to browser scripts"
                : "Editor camera handle rays are outside runtime browser IMGUI";
            throw new InvalidOperationException($"{location.Path}:{location.StartLinePosition.Line + 1}:"
                + $"{location.StartLinePosition.Character + 1}: Preview defers UnityEngine."
                + $"{(clipboard ? "GUIUtility.systemCopyBuffer" : "Event.mouseRay")}: {reason}; see ui-browser-boundary.md");
        }
        foreach (var access in declaration.DescendantNodes().OfType<MemberAccessExpressionSyntax>())
        {
            if (access.Ancestors().OfType<MethodDeclarationSyntax>()
                .Any(EditorCodePolicy.IsEditorOnlyMember)) continue;
            if (model.GetSymbolInfo(access).Symbol is not IMethodSymbol method
                || method.ContainingType.ToDisplayString() != "UnityEngine.MaterialPropertyBlock"
                || (!RendererBoundPropertyBlockMembers.Contains(method.Name)
                    && !(method.Name == "SetTexture" && method.Parameters.Length == 3))) continue;
            var location = access.GetLocation().GetLineSpan();
            throw new InvalidOperationException($"{location.Path}:{location.StartLinePosition.Line + 1}:"
                + $"{location.StartLinePosition.Character + 1}: Preview defers UnityEngine.MaterialPropertyBlock."
                + $"{method.Name}: shader uniform arrays, matrices, GPU buffers, probe data, and"
                + " RenderTexture subelements are outside the Three.js material mapping");
        }
        foreach (var access in declaration.DescendantNodes().OfType<MemberAccessExpressionSyntax>())
        {
            if (!HostDisplayMembers.Contains(access.Name.Identifier.ValueText)
                || access.Ancestors().OfType<MethodDeclarationSyntax>()
                    .Any(EditorCodePolicy.IsEditorOnlyMember)) continue;
            var receiverType = model.GetTypeInfo(access.Expression).Type?.ToDisplayString();
            var receiverSymbol = model.GetSymbolInfo(access.Expression).Symbol?.ToDisplayString();
            if (receiverType != "UnityEngine.Display" && receiverSymbol != "UnityEngine.Display")
                continue;
            var location = access.GetLocation().GetLineSpan();
            throw new InvalidOperationException($"{location.Path}:{location.StartLinePosition.Line + 1}:"
                + $"{location.StartLinePosition.Character + 1}: Preview defers Unity host Display."
                + $"{access.Name.Identifier.ValueText}: native display or framebuffer control is unavailable in Preview");
        }
        foreach (var node in declaration.DescendantNodesAndSelf())
        {
            if (node.Ancestors().OfType<MethodDeclarationSyntax>()
                .Any(EditorCodePolicy.IsEditorOnlyMember)) continue;
            if (IsPreservedRendererEnum(node)) continue;
            if (node is not (NameSyntax or MemberAccessExpressionSyntax
                or ObjectCreationExpressionSyntax)) continue;
            var text = node.ToString().Replace("global::", "", StringComparison.Ordinal);
            var symbol = model.GetSymbolInfo(node).Symbol;
            if (PreservedRendererNestedEnums.Any(full =>
                    Matches(symbol?.ToDisplayString(), full)
                    || Matches(model.GetTypeInfo(node).Type?.ToDisplayString(), full))
                || symbol?.ToDisplayString().StartsWith("UnityEngine.GraphicsBuffer.Target", StringComparison.Ordinal) == true
                || symbol?.ToDisplayString().StartsWith("UnityEngine.GraphicsBuffer.UsageFlags", StringComparison.Ordinal) == true
                || model.GetTypeInfo(node).Type?.ToDisplayString().StartsWith(
                    "UnityEngine.GraphicsBuffer.Target", StringComparison.Ordinal) == true
                || model.GetTypeInfo(node).Type?.ToDisplayString().StartsWith(
                    "UnityEngine.GraphicsBuffer.UsageFlags", StringComparison.Ordinal) == true)
                continue;
            var namespaceName = symbol switch
            {
                INamespaceSymbol space => space.ToDisplayString(),
                ITypeSymbol type => type.ContainingNamespace?.ToDisplayString(),
                _ => symbol?.ContainingNamespace?.ToDisplayString(),
            };
            var typeNamespace = model.GetTypeInfo(node).Type?.ContainingNamespace?.ToDisplayString();
            var excluded = Namespaces.FirstOrDefault(prefix => Matches(text, prefix)
                || Matches(namespaceName, prefix) || Matches(typeNamespace, prefix)
                || Matches(model.GetTypeInfo(node).Type?.ToDisplayString(), prefix)
                || Matches(symbol?.ToDisplayString(), prefix)
                || Matches(symbol?.ContainingType?.ToDisplayString(), prefix));
            if (excluded == null && (text.StartsWith("UnityEngine.iPhone", StringComparison.Ordinal)
                || symbol?.ToDisplayString().StartsWith("UnityEngine.iPhone", StringComparison.Ordinal) == true
                || symbol?.ContainingType?.ToDisplayString().StartsWith("UnityEngine.iPhone", StringComparison.Ordinal) == true))
                excluded = "iPhone";
            if (excluded == null && symbol?.Name.Contains("SpriteMask", StringComparison.Ordinal) == true
                && symbol.ContainingAssembly?.Name.StartsWith("Unity", StringComparison.Ordinal) == true)
                excluded = "SpriteMask";
            if (excluded == null && symbol?.ContainingAssembly?.Name.StartsWith("UnityEngine.Cluster", StringComparison.Ordinal) == true)
                excluded = "UnityEngine.Cluster";
            if (excluded == null && symbol?.ContainingAssembly?.Name == "UnityEngine.SpriteShapeModule")
                excluded = "UnityEngine.SpriteShapeModule";
            if (excluded == null && symbol?.ContainingAssembly?.Name == "UnityEngine.SubstanceModule")
                excluded = "Substance";
            if (excluded == null && symbol?.ContainingAssembly?.Name == "UnityEngine.SubsystemsModule")
                excluded = "Subsystems";
            if (excluded == null && (symbol?.ContainingAssembly?.Name == "UnityEngine.TextCoreFontEngineModule"
                || symbol?.ContainingAssembly?.Name == "UnityEngine.TextCoreTextEngineModule"))
                excluded = symbol.ContainingAssembly.Name;
            if (excluded == null && symbol?.ContainingAssembly?.Name == "UnityEngine.TilemapModule")
                excluded = "UnityEngine.TilemapModule";
            if (excluded == null && (symbol?.ContainingAssembly?.Name == "UnityEngine.UnityAnalyticsCommonModule"
                || symbol?.ContainingAssembly?.Name == "UnityEngine.UnityAnalyticsModule"
                || symbol?.ContainingAssembly?.Name == "UnityEngine.PerformanceReportingModule"))
                excluded = symbol.ContainingAssembly.Name;
            if (excluded == null && (symbol?.ContainingAssembly?.Name == "UnityEngine.VFXModule"
                || symbol?.ContainingAssembly?.Name == "UnityEngine.VRModule"))
                excluded = symbol.ContainingAssembly.Name;
            if (excluded == null && (symbol?.ContainingAssembly?.Name == "UnityEngine.VirtualTexturingModule"
                || symbol?.ContainingAssembly?.Name == "UnityEngine.WindModule"))
                excluded = symbol.ContainingAssembly.Name;
            if (excluded == null && symbol?.ContainingAssembly?.Name == "UnityEngine.XRModule")
                excluded = "UnityEngine.XRModule";
            if (excluded == null && symbol?.ContainingAssembly?.Name.StartsWith("UnityEngine", StringComparison.Ordinal) == true
                && (symbol.Name.Contains("Android", StringComparison.OrdinalIgnoreCase)
                    || symbol.ContainingType?.Name.Contains("Android", StringComparison.OrdinalIgnoreCase) == true
                    || symbol.ToDisplayString().Contains("Android", StringComparison.OrdinalIgnoreCase)))
                excluded = "Android";
            if (excluded == null && symbol != null
                && (symbol.ContainingNamespace?.ToDisplayString().StartsWith("UnityEngine.", StringComparison.Ordinal) == true
                    || symbol.ContainingNamespace?.ToDisplayString().StartsWith("Unity.", StringComparison.Ordinal) == true
                    || symbol.ContainingType?.ToDisplayString().StartsWith("UnityEngine.", StringComparison.Ordinal) == true
                    || symbol.ContainingType?.ToDisplayString().StartsWith("Unity.", StringComparison.Ordinal) == true)
                && symbol.ToDisplayString().Contains("Experimental", StringComparison.OrdinalIgnoreCase))
                excluded = "Experimental";
            var graylisted = GraylistedNamespaces.FirstOrDefault(prefix => Matches(text, prefix)
                || Matches(namespaceName, prefix) || Matches(typeNamespace, prefix)
                || Matches(model.GetTypeInfo(node).Type?.ToDisplayString(), prefix)
                || Matches(symbol?.ToDisplayString(), prefix)
                || Matches(symbol?.ContainingType?.ToDisplayString(), prefix));
            if (graylisted == null && symbol?.ContainingAssembly?.Name == "UnityEngine.VideoModule")
                graylisted = "UnityEngine.VideoModule";
            if (excluded == null && graylisted == null) continue;
            var location = node.GetLocation().GetLineSpan();
            var message = excluded != null ? $"Preview excludes {excluded} API"
                : GraylistMessage(graylisted!);
            throw new InvalidOperationException($"{location.Path}:{location.StartLinePosition.Line + 1}:"
                + $"{location.StartLinePosition.Character + 1}: {message}");
        }
    }

    private static bool Matches(string? value, string prefix) => value == prefix
        || value?.StartsWith(prefix + "<", StringComparison.Ordinal) == true
        || value?.StartsWith(prefix + ".", StringComparison.Ordinal) == true
        || value?.StartsWith(prefix + "<", StringComparison.Ordinal) == true;
}
