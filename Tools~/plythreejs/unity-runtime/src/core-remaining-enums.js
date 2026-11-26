// Generated from audits/core-remaining-enums-unity-reference.json.
// Regenerate with node tools/UnityApiInventory/generate-core-remaining-enums.mjs.

export const ApplicationMemoryUsage = Object.freeze({
  Unknown: 0,
  Low: 1,
  Medium: 2,
  High: 3,
  Critical: 4,
});

export const ApplicationSandboxType = Object.freeze({
  Unknown: 0,
  NotSandboxed: 1,
  Sandboxed: 2,
  SandboxBroken: 3,
});

export const BlendWeights = Object.freeze({
  OneBone: 1,
  TwoBones: 2,
  FourBones: 4,
});

export const CameraClearFlags = Object.freeze({
  Skybox: 1,
  Color: 2,
  SolidColor: 2,
  Depth: 3,
  Nothing: 4,
});

export const CameraType = Object.freeze({
  Game: 1,
  SceneView: 2,
  Preview: 4,
  VR: 8,
  Reflection: 16,
});

export const ColorGamut = Object.freeze({
  sRGB: 0,
  Rec709: 1,
  Rec2020: 2,
  DisplayP3: 3,
  HDR10: 4,
  DolbyHDR: 5,
  P3D65G22: 6,
});

export const ColorPrimaries = Object.freeze({
  Rec709: 0,
  Rec2020: 1,
  P3: 2,
  Unknown: -1,
});

export const CompressionType = Object.freeze({
  None: 0,
  Lzma: 1,
  Lz4: 2,
  Lz4HC: 3,
});

export const ComputeBufferMode = Object.freeze({
  Immutable: 0,
  Dynamic: 1,
  Circular: 2,
  StreamOut: 3,
  SubUpdates: 4,
});

export const ComputeBufferType = Object.freeze({
  Default: 0,
  Raw: 1,
  Append: 2,
  Counter: 4,
  Constant: 8,
  Structured: 16,
  DrawIndirect: 256,
  IndirectArguments: 256,
  GPUMemory: 512,
});

export const CursorLockMode = Object.freeze({
  None: 0,
  Locked: 1,
  Confined: 2,
});

export const CursorMode = Object.freeze({
  Auto: 0,
  ForceSoftware: 1,
});

export const CustomRenderTextureInitializationSource = Object.freeze({
  TextureAndColor: 0,
  Material: 1,
});

export const CustomRenderTextureUpdateMode = Object.freeze({
  OnLoad: 0,
  Realtime: 1,
  OnDemand: 2,
});

export const CustomRenderTextureUpdateZoneSpace = Object.freeze({
  Normalized: 0,
  Pixel: 1,
});

export const D3D12Comparator = Object.freeze({
  EqualTo: 0,
  NotEqualTo: 1,
  LessThan: 2,
  LessThanOrEqualTo: 3,
  GreaterThan: 4,
  GreaterThanOrEqualTo: 5,
});

export const D3D12GraphicsDeviceType = Object.freeze({
  DoNotCare: 0,
  Discrete: 1,
  Integrated: 2,
});

export const D3DHDRDisplayBitDepth = Object.freeze({
  D3DHDRDisplayBitDepth10: 0,
  D3DHDRDisplayBitDepth16: 1,
});

export const DepthTextureMode = Object.freeze({
  None: 0,
  Depth: 1,
  DepthNormals: 2,
  MotionVectors: 4,
});

export const DeviceType = Object.freeze({
  Unknown: 0,
  Handheld: 1,
  Console: 2,
  Desktop: 3,
});

export const DrivenTransformProperties = Object.freeze({
  None: 0,
  AnchoredPositionX: 2,
  AnchoredPositionY: 4,
  AnchoredPosition: 6,
  AnchoredPositionZ: 8,
  AnchoredPosition3D: 14,
  Rotation: 16,
  ScaleX: 32,
  ScaleY: 64,
  ScaleZ: 128,
  Scale: 224,
  AnchorMinX: 256,
  AnchorMinY: 512,
  AnchorMin: 768,
  AnchorMaxX: 1024,
  AnchorMaxY: 2048,
  AnchorMax: 3072,
  Anchors: 3840,
  SizeDeltaX: 4096,
  SizeDeltaY: 8192,
  SizeDelta: 12288,
  PivotX: 16384,
  PivotY: 32768,
  Pivot: 49152,
  All: -1,
});

export const FindObjectsInactive = Object.freeze({
  Exclude: 0,
  Include: 1,
});

export const FindObjectsSortMode = Object.freeze({
  None: 0,
  InstanceID: 1,
});

export const FogMode = Object.freeze({
  Linear: 1,
  Exponential: 2,
  ExponentialSquared: 3,
});

export const FullScreenMovieControlMode = Object.freeze({
  Full: 0,
  Minimal: 1,
  CancelOnInput: 2,
  Hidden: 3,
});

export const FullScreenMovieScalingMode = Object.freeze({
  None: 0,
  AspectFit: 1,
  AspectFill: 2,
  Fill: 3,
});

export const GraphicsJobsFilterMode = Object.freeze({
  Off: 0,
  Native: 1,
  Legacy: 2,
  Split: 3,
});

export const HDRDisplayBitDepth = Object.freeze({
  BitDepth10: 0,
  BitDepth16: 1,
});

export const HDRDisplaySupportFlags = Object.freeze({
  None: 0,
  Supported: 1,
  RuntimeSwitchable: 2,
  AutomaticTonemapping: 4,
});

export const InspectorSort = Object.freeze({
  ByName: 0,
  ByValue: 1,
});

export const InspectorSortDirection = Object.freeze({
  Ascending: 0,
  Descending: 1,
});

export const LightmapBakeType = Object.freeze({
  Mixed: 1,
  Baked: 2,
  Realtime: 4,
});

export const LightmapCompression = Object.freeze({
  None: 0,
  LowQuality: 1,
  NormalQuality: 2,
  HighQuality: 3,
});

export const LightmappingMode = Object.freeze({
  Mixed: 1,
  Baked: 2,
  Realtime: 4,
});

export const LightmapsMode = Object.freeze({
  NonDirectional: 0,
  Single: 0,
  CombinedDirectional: 1,
  Dual: 1,
  SeparateDirectional: 2,
  Directional: 2,
});

export const LightmapsModeLegacy = Object.freeze({
  Single: 0,
  Dual: 1,
  Directional: 2,
});

export const LightRenderMode = Object.freeze({
  Auto: 0,
  ForcePixel: 1,
  ForceVertex: 2,
});

export const LightShadowCasterMode = Object.freeze({
  Default: 0,
  NonLightmappedOnly: 1,
  Everything: 2,
});

export const LightShadows = Object.freeze({
  None: 0,
  Hard: 1,
  Soft: 2,
});

export const LightShape = Object.freeze({
  Cone: 0,
  Pyramid: 1,
  Box: 2,
});

export const LightType = Object.freeze({
  Spot: 0,
  Directional: 1,
  Point: 2,
  Area: 3,
  Rectangle: 3,
  Disc: 4,
  Pyramid: 5,
  Box: 6,
  Tube: 7,
});

export const LineAlignment = Object.freeze({
  View: 0,
  Local: 1,
  TransformZ: 1,
});

export const LineTextureMode = Object.freeze({
  Stretch: 0,
  Tile: 1,
  DistributePerSegment: 2,
  RepeatPerSegment: 3,
  Static: 4,
});

export const LODFadeMode = Object.freeze({
  None: 0,
  CrossFade: 1,
  SpeedTree: 2,
});

export const MaterialGlobalIlluminationFlags = Object.freeze({
  None: 0,
  RealtimeEmissive: 1,
  BakedEmissive: 2,
  AnyEmissive: 3,
  EmissiveIsBlack: 4,
});

export const MaterialPropertyType = Object.freeze({
  Float: 0,
  Int: 1,
  Vector: 2,
  Matrix: 3,
  Texture: 4,
  ConstantBuffer: 5,
  ComputeBuffer: 6,
});

export const MeshTopology = Object.freeze({
  Triangles: 0,
  Quads: 2,
  Lines: 3,
  LineStrip: 4,
  Points: 5,
});

export const MixedLightingMode = Object.freeze({
  IndirectOnly: 0,
  Subtractive: 1,
  Shadowmask: 2,
});

export const MotionVectorGenerationMode = Object.freeze({
  Camera: 0,
  Object: 1,
  ForceNoMotion: 2,
});

export const NetworkReachability = Object.freeze({
  NotReachable: 0,
  ReachableViaCarrierDataNetwork: 1,
  ReachableViaLocalAreaNetwork: 2,
});

export const NPOTSupport = Object.freeze({
  None: 0,
  Restricted: 1,
  Full: 2,
});

export const ReceiveGI = Object.freeze({
  Lightmaps: 1,
  LightProbes: 2,
});

export const RenderTextureReadWrite = Object.freeze({
  Default: 0,
  Linear: 1,
  sRGB: 2,
});

export const RuntimeInitializeLoadType = Object.freeze({
  AfterSceneLoad: 0,
  BeforeSceneLoad: 1,
  AfterAssembliesLoaded: 2,
  BeforeSplashScreen: 3,
  SubsystemRegistration: 4,
});

export const SendMessageOptions = Object.freeze({
  RequireReceiver: 0,
  DontRequireReceiver: 1,
});

export const ShadowmaskMode = Object.freeze({
  Shadowmask: 0,
  DistanceShadowmask: 1,
});

export const ShadowObjectsFilter = Object.freeze({
  AllObjects: 0,
  DynamicOnly: 1,
  StaticOnly: 2,
});

export const SkinQuality = Object.freeze({
  Auto: 0,
  Bone1: 1,
  Bone2: 2,
  Bone4: 4,
});

export const SnapAxis = Object.freeze({
  None: 0,
  X: 1,
  Y: 2,
  Z: 4,
  All: 7,
});

export const SpriteAlignment = Object.freeze({
  Center: 0,
  TopLeft: 1,
  TopCenter: 2,
  TopRight: 3,
  LeftCenter: 4,
  RightCenter: 5,
  BottomLeft: 6,
  BottomCenter: 7,
  BottomRight: 8,
  Custom: 9,
});

export const SpriteDrawMode = Object.freeze({
  Simple: 0,
  Sliced: 1,
  Tiled: 2,
});

export const SpriteMeshType = Object.freeze({
  FullRect: 0,
  Tight: 1,
});

export const SpritePackingMode = Object.freeze({
  Tight: 0,
  Rectangle: 1,
});

export const SpritePackingRotation = Object.freeze({
  None: 0,
  FlipHorizontal: 1,
  FlipVertical: 2,
  Rotate180: 3,
  Any: 15,
});

export const SpriteSortPoint = Object.freeze({
  Center: 0,
  Pivot: 1,
});

export const SpriteTileMode = Object.freeze({
  Continuous: 0,
  Adaptive: 1,
});

export const StackTraceLogType = Object.freeze({
  None: 0,
  ScriptOnly: 1,
  Full: 2,
});

export const StereoTargetEyeMask = Object.freeze({
  None: 0,
  Left: 1,
  Right: 2,
  Both: 3,
});

export const TexGenMode = Object.freeze({
  None: 0,
  SphereMap: 1,
  Object: 2,
  EyeLinear: 3,
  CubeReflect: 4,
  CubeNormal: 5,
});

export const TextureMipmapLimitBiasMode = Object.freeze({
  OffsetGlobalLimit: 0,
  OverrideGlobalLimit: 1,
});

export const TouchScreenKeyboardType = Object.freeze({
  Default: 0,
  ASCIICapable: 1,
  NumbersAndPunctuation: 2,
  URL: 3,
  NumberPad: 4,
  PhonePad: 5,
  NamePhonePad: 6,
  EmailAddress: 7,
  NintendoNetworkAccount: 8,
  Social: 9,
  Search: 10,
  DecimalPad: 11,
  OneTimeCode: 12,
});

export const TransferFunction = Object.freeze({
  sRGB: 0,
  BT1886: 1,
  PQ: 2,
  Linear: 3,
  Gamma22: 4,
  Unknown: -1,
});

export const TransparencySortMode = Object.freeze({
  Default: 0,
  Perspective: 1,
  Orthographic: 2,
  CustomAxis: 3,
});

export const UserAuthorization = Object.freeze({
  WebCam: 1,
  Microphone: 2,
});

export const WeightedMode = Object.freeze({
  None: 0,
  In: 1,
  Out: 2,
  Both: 3,
});

export const WhitePoint = Object.freeze({
  D65: 0,
  Unknown: -1,
});

