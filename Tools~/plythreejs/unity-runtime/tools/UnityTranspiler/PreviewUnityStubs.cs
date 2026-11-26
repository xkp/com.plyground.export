// Compile-time Unity surface for variation scripts. This file is never emitted to JavaScript.
// Extend it only for APIs that the runtime implements and the transpiler can lower.
namespace UnityEngine
{
    public static class JsonUtility
    {
        public static string ToJson(object obj) => null!;
        public static string ToJson(object obj, bool prettyPrint) => null!;
        public static object FromJson(string json, System.Type type) => null!;
        public static T FromJson<T>(string json) => default!;
        public static void FromJsonOverwrite(string json, object objectToOverwrite) { }
    }
    [System.AttributeUsage(System.AttributeTargets.Field | System.AttributeTargets.Property)]
    public class PropertyAttribute : System.Attribute
    {
        public PropertyAttribute() { }
        public PropertyAttribute(bool applyToCollection) { this.applyToCollection = applyToCollection; }
        public int order { get; set; }
        public bool applyToCollection { get; set; }
    }
    [System.AttributeUsage(System.AttributeTargets.Method)]
    public sealed class HideInCallstackAttribute : System.Attribute { }
    public interface IExposedPropertyTable
    {
        void ClearReferenceValue(PropertyName id);
        Object GetReferenceValue(PropertyName id, out bool idValid);
        void SetReferenceValue(PropertyName id, Object value);
    }
    public struct ExposedReference<T> where T : Object
    {
        public PropertyName exposedName;
        public T defaultValue;
        public T Resolve(IExposedPropertyTable resolver) => defaultValue;
    }
    public class AnimationClip : Object { }
    public struct AnimatorStateInfo
    {
        public float normalizedTime { get; set; }
        public float length { get; set; }
        public int shortNameHash { get; set; }
        public int fullPathHash { get; set; }
        public int tagHash { get; set; }
        public float speed { get; set; }
        public bool loop { get; set; }
        public bool IsName(string name) => false;
        public bool IsTag(string tag) => false;
    }
    public struct AnimatorTransitionInfo
    {
        public float normalizedTime { get; set; }
        public float duration { get; set; }
    }
    public struct AnimatorClipInfo
    {
        public AnimationClip clip { get; set; }
        public float weight { get; set; }
    }
    public class Animator : Behaviour
    {
        public static int StringToHash(string name) => 0;
        public int layerCount { get; set; }
        public float speed { get; set; }
        public void Update(float deltaTime) { }
        public string GetLayerName(int layerIndex) => "";
        public int GetLayerIndex(string layerName) => -1;
        public bool HasState(int layerIndex, int stateID) => false;
        public void Play(string stateName, int layer, float normalizedTime) { }
        public void Play(int stateNameHash, int layer, float normalizedTime) { }
        public void CrossFade(string stateName, float normalizedTransitionDuration,
            int layer = -1, float normalizedTimeOffset = 0f,
            float normalizedTransitionTime = 0f) { }
        public void CrossFade(int stateNameHash, float normalizedTransitionDuration,
            int layer = -1, float normalizedTimeOffset = 0f,
            float normalizedTransitionTime = 0f) { }
        public void SetBool(string name, bool value) { }
        public void SetBool(int id, bool value) { }
        public bool GetBool(string name) => false;
        public bool GetBool(int id) => false;
        public void SetFloat(string name, float value) { }
        public void SetFloat(int id, float value) { }
        public float GetFloat(string name) => 0;
        public float GetFloat(int id) => 0;
        public void SetInteger(string name, int value) { }
        public void SetInteger(int id, int value) { }
        public int GetInteger(string name) => 0;
        public int GetInteger(int id) => 0;
        public void SetTrigger(string name) { }
        public void SetTrigger(int id) { }
        public void ResetTrigger(string name) { }
        public void ResetTrigger(int id) { }
        public AnimatorStateInfo GetCurrentAnimatorStateInfo(int layerIndex) => default;
        public AnimatorStateInfo GetNextAnimatorStateInfo(int layerIndex) => default;
        public bool IsInTransition(int layerIndex) => false;
        public AnimatorTransitionInfo GetAnimatorTransitionInfo(int layerIndex) => default;
        public AnimatorClipInfo[] GetCurrentAnimatorClipInfo(int layerIndex) => [];
        public AnimatorClipInfo[] GetNextAnimatorClipInfo(int layerIndex) => [];
        public void GetCurrentAnimatorClipInfo(int layerIndex,
            System.Collections.Generic.List<AnimatorClipInfo> clips) { }
        public void GetNextAnimatorClipInfo(int layerIndex,
            System.Collections.Generic.List<AnimatorClipInfo> clips) { }
        public int GetCurrentAnimatorClipInfoCount(int layerIndex) => 0;
        public int GetNextAnimatorClipInfoCount(int layerIndex) => 0;
    }
    public class TextAsset : Object
    {
        public TextAsset() { }
        public TextAsset(string text) { }
        public TextAsset(System.ReadOnlySpan<byte> data) { }
        public string text => "";
        public byte[] bytes => [];
        public long dataSize => 0;
        public Unity.Collections.NativeArray<T> GetData<T>() where T : struct => new(0, Unity.Collections.Allocator.Temp);
        public override string ToString() => "";
    }
    public class Resources
    {
        public static bool InstanceIDIsValid(int id) => false;
        public static Object? InstanceIDToObject(int id) => null;
        public static bool EntityIdIsValid(EntityId id) => false;
        public static Object? EntityIdToObject(EntityId id) => null;
        public static void InstanceIDToObjectList(Unity.Collections.NativeArray<int> ids,
            System.Collections.Generic.List<Object> results) { }
        public static void EntityIdsToObjectList(Unity.Collections.NativeArray<EntityId> ids,
            System.Collections.Generic.List<Object> results) { }
        public static void InstanceIDsToValidArray(Unity.Collections.NativeArray<int> ids,
            Unity.Collections.NativeArray<bool> results) { }
        public static void EntityIdsToValidArray(Unity.Collections.NativeArray<EntityId> ids,
            Unity.Collections.NativeArray<bool> results) { }
        public static void InstanceIDsToValidArray(System.ReadOnlySpan<int> ids,
            System.Span<bool> results) { }
        public static void EntityIdsToValidArray(System.ReadOnlySpan<EntityId> ids,
            System.Span<bool> results) { }
        public static Object[] FindObjectsOfTypeAll(System.Type type) => [];
        public static T[] FindObjectsOfTypeAll<T>() where T : Object => [];
        public static Object? Load(string path) => null;
        public static Object? Load(string path, System.Type type) => null;
        public static T? Load<T>(string path) where T : Object => null;
        public static Object[] LoadAll(string path) => [];
        public static Object[] LoadAll(string path, System.Type type) => [];
        public static T[] LoadAll<T>(string path) where T : Object => [];
        public static ResourceRequest LoadAsync(string path) => null!;
        public static ResourceRequest LoadAsync(string path, System.Type type) => null!;
        public static ResourceRequest LoadAsync<T>(string path) where T : Object => null!;
        public static void UnloadAsset(Object asset) { }
    }
    public class LineUtility
    {
        public LineUtility() { }
        public static void Simplify(System.Collections.Generic.List<Vector2> points, float tolerance,
            System.Collections.Generic.List<Vector2> results) { }
        public static void Simplify(System.Collections.Generic.List<Vector2> points, float tolerance,
            System.Collections.Generic.List<int> results) { }
        public static void Simplify(System.Collections.Generic.List<Vector3> points, float tolerance,
            System.Collections.Generic.List<Vector3> results) { }
        public static void Simplify(System.Collections.Generic.List<Vector3> points, float tolerance,
            System.Collections.Generic.List<int> results) { }
    }
    public class Object
    {
        public string name { get; set; } = "";
        public int GetInstanceID() => 0;
        public EntityId GetEntityId() => default;
        public int hideFlags { get; set; }
        public static implicit operator bool(Object? value) => value is not null;
        public static T Instantiate<T>(T original) where T : Object => original;
        public static T Instantiate<T>(T original, Transform parent) where T : Object => original;
        public static T Instantiate<T>(T original, Transform parent, bool instantiateInWorldSpace) where T : Object => original;
        public static T Instantiate<T>(T original, Vector3 position, Quaternion rotation) where T : Object => original;
        public static T Instantiate<T>(T original, Vector3 position, Quaternion rotation, Transform parent) where T : Object => original;
        public static T Instantiate<T>(T original, InstantiateParameters parameters) where T : Object => original;
        public static T Instantiate<T>(T original, Vector3 position, Quaternion rotation, InstantiateParameters parameters) where T : Object => original;
        public static AsyncInstantiateOperation<T> InstantiateAsync<T>(T original) where T : Object => new();
        public static AsyncInstantiateOperation<T> InstantiateAsync<T>(T original, InstantiateParameters parameters, System.Threading.CancellationToken token) where T : Object => new();
        public static AsyncInstantiateOperation<T> InstantiateAsync<T>(T original, Transform parent) where T : Object => new();
        public static AsyncInstantiateOperation<T> InstantiateAsync<T>(T original, Transform parent, Vector3 position, Quaternion rotation) where T : Object => new();
        public static AsyncInstantiateOperation<T> InstantiateAsync<T>(T original, Vector3 position, Quaternion rotation) where T : Object => new();
        public static AsyncInstantiateOperation<T> InstantiateAsync<T>(T original, Vector3 position, Quaternion rotation, InstantiateParameters parameters, System.Threading.CancellationToken token) where T : Object => new();
        public static AsyncInstantiateOperation<T> InstantiateAsync<T>(T original, int count) where T : Object => new();
        public static AsyncInstantiateOperation<T> InstantiateAsync<T>(T original, int count, System.ReadOnlySpan<Vector3> positions, System.ReadOnlySpan<Quaternion> rotations) where T : Object => new();
        public static AsyncInstantiateOperation<T> InstantiateAsync<T>(T original, int count, System.ReadOnlySpan<Vector3> positions, System.ReadOnlySpan<Quaternion> rotations, InstantiateParameters parameters, System.Threading.CancellationToken token) where T : Object => new();
        public static AsyncInstantiateOperation<T> InstantiateAsync<T>(T original, int count, InstantiateParameters parameters, System.Threading.CancellationToken token) where T : Object => new();
        public static AsyncInstantiateOperation<T> InstantiateAsync<T>(T original, int count, Transform parent) where T : Object => new();
        public static AsyncInstantiateOperation<T> InstantiateAsync<T>(T original, int count, Transform parent, System.ReadOnlySpan<Vector3> positions, System.ReadOnlySpan<Quaternion> rotations) where T : Object => new();
        public static AsyncInstantiateOperation<T> InstantiateAsync<T>(T original, int count, Transform parent, System.ReadOnlySpan<Vector3> positions, System.ReadOnlySpan<Quaternion> rotations, System.Threading.CancellationToken token) where T : Object => new();
        public static AsyncInstantiateOperation<T> InstantiateAsync<T>(T original, int count, Transform parent, Vector3 position, Quaternion rotation) where T : Object => new();
        public static AsyncInstantiateOperation<T> InstantiateAsync<T>(T original, int count, Transform parent, Vector3 position, Quaternion rotation, System.Threading.CancellationToken token) where T : Object => new();
        public static AsyncInstantiateOperation<T> InstantiateAsync<T>(T original, int count, Vector3 position, Quaternion rotation) where T : Object => new();
        public static AsyncInstantiateOperation<T> InstantiateAsync<T>(T original, int count, Vector3 position, Quaternion rotation, InstantiateParameters parameters, System.Threading.CancellationToken token) where T : Object => new();
        public static Object Instantiate(Object original) => original;
        public static Object Instantiate(Object original, Transform parent) => original;
        public static Object Instantiate(Object original, Transform parent, bool instantiateInWorldSpace) => original;
        public static Object Instantiate(Object original, Vector3 position, Quaternion rotation) => original;
        public static Object Instantiate(Object original, Vector3 position, Quaternion rotation, Transform parent) => original;
        public static Object Instantiate(Object original, UnityEngine.SceneManagement.Scene scene) => original;
        public static void Destroy(Object target) { }
        public static void Destroy(Object target, float delay) { }
        public static void DestroyImmediate(Object target) { }
        public static void DestroyImmediate(Object target, bool allowDestroyingAssets) { }
        public static void DestroyObject(Object target) { }
        public static void DestroyObject(Object target, float delay) { }
        public static void DontDestroyOnLoad(Object target) { }
        public static T[] FindObjectsOfType<T>(bool includeInactive = false) where T : Object => [];
        public static Object[] FindObjectsOfType(System.Type type, bool includeInactive = false) => [];
        public static T FindObjectOfType<T>(bool includeInactive = false) where T : Object => null!;
        public static Object FindObjectOfType(System.Type type, bool includeInactive = false) => null!;
        public static T FindFirstObjectByType<T>(FindObjectsInactive inactive = FindObjectsInactive.Exclude) where T : Object => null!;
        public static Object FindFirstObjectByType(System.Type type, FindObjectsInactive inactive = FindObjectsInactive.Exclude) => null!;
        public static T FindAnyObjectByType<T>(FindObjectsInactive inactive = FindObjectsInactive.Exclude) where T : Object => null!;
        public static Object FindAnyObjectByType(System.Type type, FindObjectsInactive inactive = FindObjectsInactive.Exclude) => null!;
        public static T[] FindObjectsByType<T>(FindObjectsSortMode sortMode) where T : Object => [];
        public static T[] FindObjectsByType<T>(FindObjectsInactive inactive, FindObjectsSortMode sortMode) where T : Object => [];
        public static Object[] FindObjectsByType(System.Type type, FindObjectsSortMode sortMode) => [];
        public static Object[] FindObjectsByType(System.Type type, FindObjectsInactive inactive, FindObjectsSortMode sortMode) => [];
        public static Object[] FindObjectsOfTypeAll(System.Type type) => [];
        public static Object[] FindObjectsOfTypeIncludingAssets(System.Type type) => [];
        public static Object[] FindSceneObjectsOfType(System.Type type) => [];
    }
    public partial class Texture : Object
    {
        public int width { get; }
        public int height { get; }
        public int mipmapCount { get; }
        public bool isReadable { get; }
        public Vector2 texelSize { get; }
        public FilterMode filterMode { get; set; }
        public TextureWrapMode wrapMode { get; set; }
        public TextureWrapMode wrapModeU { get; set; }
        public TextureWrapMode wrapModeV { get; set; }
        public TextureWrapMode wrapModeW { get; set; }
        public int anisoLevel { get; set; }
        public UnityEngine.Rendering.TextureDimension dimension { get; }
        public bool isDataSRGB { get; }
    }
    public partial class RenderTexture : Texture
    {
        public RenderTexture(RenderTexture source) { }
        public RenderTexture(int width, int height, int depth) { }
        public RenderTexture(int width, int height, int depth, RenderTextureFormat format) { }
        public RenderTexture(int width, int height, int depth, RenderTextureFormat format,
            RenderTextureReadWrite readWrite) { }
        public static RenderTexture active { get; set; } = null!;
        public int depth { get; set; }
        public RenderTextureFormat format { get; set; }
        public bool sRGB => false;
        public bool Create() => false;
        public bool IsCreated() => false;
        public void Release() { }
        public static RenderTexture GetTemporary(int width, int height) => null!;
        public static RenderTexture GetTemporary(int width, int height, int depth) => null!;
        public static RenderTexture GetTemporary(int width, int height, int depth,
            RenderTextureFormat format) => null!;
        public static void ReleaseTemporary(RenderTexture texture) { }
    }
    [System.AttributeUsage(System.AttributeTargets.Class)]
    public sealed class DefaultExecutionOrder : System.Attribute
    {
        public int order { get; }
        public DefaultExecutionOrder(int order) { this.order = order; }
    }
    [System.AttributeUsage(System.AttributeTargets.Method)]
    public sealed class RuntimeInitializeOnLoadMethodAttribute : System.Attribute
    {
        public RuntimeInitializeLoadType loadType { get; }
        public RuntimeInitializeOnLoadMethodAttribute()
            { loadType = RuntimeInitializeLoadType.AfterSceneLoad; }
        public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType loadType)
            { this.loadType = loadType; }
    }
    public class UnityException : System.Exception
    {
        public UnityException() : base("A Unity Runtime error occurred!") { }
        public UnityException(string message) : base(message) { }
        public UnityException(string message, System.Exception innerException) : base(message, innerException) { }
    }
    public class MissingComponentException : System.Exception
    {
        public MissingComponentException() : base("A Unity Runtime error occurred!") { }
        public MissingComponentException(string message) : base(message) { }
        public MissingComponentException(string message, System.Exception innerException) : base(message, innerException) { }
    }
    public class MissingReferenceException : System.Exception
    {
        public MissingReferenceException() : base("A Unity Runtime error occurred!") { }
        public MissingReferenceException(string message) : base(message) { }
        public MissingReferenceException(string message, System.Exception innerException) : base(message, innerException) { }
    }
    public class UnassignedReferenceException : System.Exception
    {
        public UnassignedReferenceException() : base("A Unity Runtime error occurred!") { }
        public UnassignedReferenceException(string message) : base(message) { }
        public UnassignedReferenceException(string message, System.Exception innerException) : base(message, innerException) { }
    }
    public partial class Texture2D : Texture
    {
        public static Texture2D whiteTexture => new(4, 4);
        public static Texture2D blackTexture => new(4, 4);
        public static Texture2D grayTexture => new(4, 4);
        public static Texture2D linearGrayTexture => new(4, 4);
        public static Texture2D redTexture => new(4, 4);
        public static Texture2D normalTexture => new(4, 4);
        public Texture2D(int width, int height) { }
        public Texture2D(int width, int height, TextureFormat format, bool mipChain) { }
        public Texture2D(int width, int height, TextureFormat format, bool mipChain, bool linear) { }
        public Texture2D(int width, int height, TextureFormat format, int mipCount, bool linear) { }
        public TextureFormat format { get; }
        public bool alphaIsTransparency { get; set; }
        public bool streamingMipmaps { get; }
        public int streamingMipmapsPriority { get; }
        public bool vtOnly { get; }
        public bool ignoreMipmapLimit { get; set; }
        public string mipmapLimitGroup { get; }
        public int activeMipmapLimit { get; }
        public int calculatedMipmapLevel { get; }
        public int desiredMipmapLevel { get; }
        public int loadedMipmapLevel { get; }
        public int loadingMipmapLevel { get; }
        public int minimumMipmapLevel { get; set; }
        public int requestedMipmapLevel { get; set; }
        public bool IsRequestedMipmapLevelLoaded() => true;
        public void ClearMinimumMipmapLevel() { }
        public void ClearRequestedMipmapLevel() { }
        public Color GetPixel(int x, int y) => default;
        public Color GetPixel(int x, int y, int mipLevel) => default;
        public Color GetPixelBilinear(float u, float v) => default;
        public Color GetPixelBilinear(float u, float v, int mipLevel) => default;
        public Color[] GetPixels() => [];
        public Color[] GetPixels(int mipLevel) => [];
        public Color[] GetPixels(int x, int y, int width, int height) => [];
        public Color[] GetPixels(int x, int y, int width, int height, int mipLevel) => [];
        public Color32[] GetPixels32() => [];
        public Color32[] GetPixels32(int mipLevel) => [];
        public void ReadPixels(Rect source, int destinationX, int destinationY) { }
        public void ReadPixels(Rect source, int destinationX, int destinationY,
            bool recalculateMipMaps) { }
        public byte[] EncodeToJPG(int quality = 75) => [];
        public byte[] EncodeToPNG() => [];
        public byte[] GetRawTextureData() => [];
        public void LoadRawTextureData(byte[] data) { }
        public void CopyPixels(Texture source) { }
        public void CopyPixels(Texture source, int sourceElement, int sourceMip, int destinationMip) { }
        public void CopyPixels(Texture source, int sourceElement, int sourceMip,
            int sourceX, int sourceY, int sourceWidth, int sourceHeight,
            int destinationMip, int destinationX, int destinationY) { }
        public void SetPixelData<T>(T[] data, int mipLevel, int sourceDataStartIndex = 0) where T : struct { }
        public void SetPixel(int x, int y, Color color) { }
        public void SetPixel(int x, int y, Color color, int mipLevel) { }
        public void SetPixels(Color[] colors) { }
        public void SetPixels(Color[] colors, int mipLevel) { }
        public void SetPixels(int x, int y, int width, int height, Color[] colors) { }
        public void SetPixels(int x, int y, int width, int height, Color[] colors, int mipLevel) { }
        public void SetPixels32(Color32[] colors) { }
        public void SetPixels32(Color32[] colors, int mipLevel) { }
        public void SetPixels32(int x, int y, int width, int height, Color32[] colors) { }
        public void SetPixels32(int x, int y, int width, int height, Color32[] colors, int mipLevel) { }
        public void Apply() { }
        public void Apply(bool updateMipmaps) { }
        public void Apply(bool updateMipmaps, bool makeNoLongerReadable) { }
        public bool Reinitialize(int width, int height) => true;
        public bool Reinitialize(int width, int height, TextureFormat format, bool mipChain) => true;
        public bool Resize(int width, int height) => true;
        public bool Resize(int width, int height, TextureFormat format, bool mipChain) => true;
    }
    public struct InstantiateParameters
    {
        public Transform parent;
        public UnityEngine.SceneManagement.Scene scene;
        public bool worldSpace;
        public bool originalImmutable;
    }
    public readonly struct EntityId : System.IEquatable<EntityId>, System.IComparable<EntityId>
    {
        public readonly int value;
        public EntityId(int value) { this.value = value; }
        public static EntityId None => default;
        public bool IsValid() => value != 0;
        public int CompareTo(EntityId other) => value.CompareTo(other.value);
        public bool Equals(EntityId other) => value == other.value;
        public bool Equals(int other) => value == other;
        public static implicit operator int(EntityId id) => id.value;
        public static implicit operator EntityId(int id) => new(id);
        public static implicit operator InstanceID(EntityId id) => new(id.value);
        public static implicit operator EntityId(InstanceID id) => new(id.value);
        public static bool operator ==(EntityId a, EntityId b) => a.value == b.value;
        public static bool operator !=(EntityId a, EntityId b) => a.value != b.value;
        public static bool operator <(EntityId a, EntityId b) => a.value < b.value;
        public static bool operator <=(EntityId a, EntityId b) => a.value <= b.value;
        public static bool operator >(EntityId a, EntityId b) => a.value > b.value;
        public static bool operator >=(EntityId a, EntityId b) => a.value >= b.value;
        public override bool Equals(object obj) => obj is EntityId other && Equals(other);
        public override int GetHashCode() => value;
        public override string ToString() => value.ToString();
        public string ToString(string format) => value.ToString(format);
    }
    public readonly struct InstanceID : System.IEquatable<InstanceID>, System.IComparable<InstanceID>
    {
        public readonly int value;
        public InstanceID(int value) { this.value = value; }
        public static InstanceID None => default;
        public bool IsValid() => value != 0;
        public int CompareTo(InstanceID other) => value.CompareTo(other.value);
        public bool Equals(InstanceID other) => value == other.value;
        public bool Equals(int other) => value == other;
        public static implicit operator int(InstanceID id) => id.value;
        public static implicit operator InstanceID(int id) => new(id);
        public static implicit operator EntityId(InstanceID id) => new(id.value);
        public static implicit operator InstanceID(EntityId id) => new(id.value);
        public static bool operator ==(InstanceID a, InstanceID b) => a.value == b.value;
        public static bool operator !=(InstanceID a, InstanceID b) => a.value != b.value;
        public static bool operator <(InstanceID a, InstanceID b) => a.value < b.value;
        public static bool operator <=(InstanceID a, InstanceID b) => a.value <= b.value;
        public static bool operator >(InstanceID a, InstanceID b) => a.value > b.value;
        public static bool operator >=(InstanceID a, InstanceID b) => a.value >= b.value;
        public override bool Equals(object obj) => obj is InstanceID other && Equals(other);
        public override int GetHashCode() => value;
        public override string ToString() => value.ToString();
        public string ToString(string format) => value.ToString(format);
    }
    public readonly struct TagHandle
    {
        public static TagHandle GetExistingTag(string name) => default;
    }
    public struct TransformHandle
    {
        public struct DirectChildrenEnumerator
        {
            public TransformHandle Current => default;
            public bool MoveNext() => false;
            public void Reset() { }
            public void Dispose() { }
        }
        public struct DirectChildrenEnumerable
        {
            public DirectChildrenEnumerable(TransformHandle handle) { }
            public DirectChildrenEnumerator GetEnumerator() => default;
        }
        public static TransformHandle None => default;
        public bool IsValid() => false;
        public bool HasParent() => false;
        public TransformHandle parent { get; set; }
        public TransformHandle root => default;
        public TransformHandle GetChild(int index) => default;
        public DirectChildrenEnumerable DirectChildren => default;
        public DirectChildrenEnumerator GetDirectChildrenEnumerator() => default;
        public bool IsChildOf(TransformHandle parent) => false;
        public void SetParent(TransformHandle parent) { }
        public void SetParent(TransformHandle parent, bool worldPositionStays) { }
        public void DetachChildren() { }
        public Vector3 position { get; set; }
        public Vector3 localPosition { get; set; }
        public Quaternion rotation { get; set; }
        public Quaternion localRotation { get; set; }
        public Vector3 localScale { get; set; }
        public Vector3 lossyScale => default;
        public Vector3 eulerAngles { get; set; }
        public Vector3 localEulerAngles { get; set; }
        public Vector3 forward { get; set; }
        public Vector3 up { get; set; }
        public Vector3 right { get; set; }
        public Matrix4x4 localToWorldMatrix => default;
        public Matrix4x4 worldToLocalMatrix => default;
        public int childCount { get; }
        public int hierarchyCount => 0;
        public int hierarchyCapacity { get; set; }
        public void GetPositionAndRotation(out Vector3 position, out Quaternion rotation)
            { position = default; rotation = default; }
        public void GetLocalPositionAndRotation(out Vector3 position, out Quaternion rotation)
            { position = default; rotation = default; }
        public void SetPositionAndRotation(Vector3 position, Quaternion rotation) { }
        public void SetLocalPositionAndRotation(Vector3 position, Quaternion rotation) { }
        public Vector3 TransformPoint(Vector3 point) => default;
        public Vector3 InverseTransformPoint(Vector3 point) => default;
        public Vector3 TransformDirection(Vector3 direction) => default;
        public Vector3 InverseTransformDirection(Vector3 direction) => default;
        public Vector3 TransformVector(Vector3 vector) => default;
        public Vector3 InverseTransformVector(Vector3 vector) => default;
        public Vector3 TransformPoint(float x, float y, float z) => default;
        public Vector3 InverseTransformPoint(float x, float y, float z) => default;
        public Vector3 TransformDirection(float x, float y, float z) => default;
        public Vector3 InverseTransformDirection(float x, float y, float z) => default;
        public Vector3 TransformVector(float x, float y, float z) => default;
        public Vector3 InverseTransformVector(float x, float y, float z) => default;
        public void TransformPoints(System.ReadOnlySpan<Vector3> input, System.Span<Vector3> output) { }
        public void TransformPoints(System.Span<Vector3> values) { }
        public void InverseTransformPoints(System.ReadOnlySpan<Vector3> input, System.Span<Vector3> output) { }
        public void InverseTransformPoints(System.Span<Vector3> values) { }
        public void TransformDirections(System.ReadOnlySpan<Vector3> input, System.Span<Vector3> output) { }
        public void TransformDirections(System.Span<Vector3> values) { }
        public void InverseTransformDirections(System.ReadOnlySpan<Vector3> input, System.Span<Vector3> output) { }
        public void InverseTransformDirections(System.Span<Vector3> values) { }
        public void TransformVectors(System.ReadOnlySpan<Vector3> input, System.Span<Vector3> output) { }
        public void TransformVectors(System.Span<Vector3> values) { }
        public void InverseTransformVectors(System.ReadOnlySpan<Vector3> input, System.Span<Vector3> output) { }
        public void InverseTransformVectors(System.Span<Vector3> values) { }
        public void LookAt(TransformHandle target) { }
        public void LookAt(TransformHandle target, Vector3 worldUp) { }
        public void LookAt(Vector3 target) { }
        public void LookAt(Vector3 target, Vector3 worldUp) { }
        public void Rotate(Vector3 eulers) { }
        public void Rotate(Vector3 eulers, Space relativeTo) { }
        public void Rotate(Vector3 axis, float angle) { }
        public void Rotate(Vector3 axis, float angle, Space relativeTo) { }
        public void Rotate(float x, float y, float z) { }
        public void Rotate(float x, float y, float z, Space relativeTo) { }
        public void RotateAround(Vector3 point, Vector3 axis, float angle) { }
        public void Translate(Vector3 translation) { }
        public void Translate(Vector3 translation, Space relativeTo) { }
        public void Translate(Vector3 translation, TransformHandle relativeTo) { }
        public void Translate(float x, float y, float z) { }
        public void Translate(float x, float y, float z, Space relativeTo) { }
        public void Translate(float x, float y, float z, TransformHandle relativeTo) { }
        public static bool operator ==(TransformHandle left, TransformHandle right) => false;
        public static bool operator !=(TransformHandle left, TransformHandle right) => true;
        public int CompareTo(TransformHandle other) => 0;
        public bool Equals(TransformHandle other) => false;
        public override bool Equals(object other) => false;
        public override int GetHashCode() => 0;
    }
    public enum FindObjectsInactive { Exclude = 0, Include = 1 }
    public enum FindObjectsSortMode { None = 0, InstanceID = 1 }
    public enum SendMessageOptions { RequireReceiver = 0, DontRequireReceiver = 1 }
    public class ScriptableObject : Object
    {
        public static T CreateInstance<T>() where T : ScriptableObject, new() => new();
        public void SetDirty() { }
    }
    public interface ISerializationCallbackReceiver
    {
        void OnBeforeSerialize();
        void OnAfterDeserialize();
    }
    public class Component : Object
    {
        public void SendMessage(string method) { }
        public void SendMessage(string method, SendMessageOptions options) { }
        public void SendMessage(string method, object value) { }
        public void SendMessage(string method, object value, SendMessageOptions options) { }
        public void SendMessageUpwards(string method) { }
        public void SendMessageUpwards(string method, SendMessageOptions options) { }
        public void SendMessageUpwards(string method, object value) { }
        public void SendMessageUpwards(string method, object value, SendMessageOptions options) { }
        public void BroadcastMessage(string method) { }
        public void BroadcastMessage(string method, SendMessageOptions options) { }
        public void BroadcastMessage(string method, object value) { }
        public void BroadcastMessage(string method, object value, SendMessageOptions options) { }
        public GameObject gameObject { get; set; } = null!;
        public Transform transform { get; set; } = null!;
        public TransformHandle transformHandle { get; }
        [System.Obsolete("animation property has been deprecated", true)] public Animation animation => throw new System.NotSupportedException();
        [System.Obsolete("audio property has been deprecated", true)] public AudioSource audio => throw new System.NotSupportedException();
        [System.Obsolete("camera property has been deprecated", true)] public Camera camera => throw new System.NotSupportedException();
        [System.Obsolete("collider property has been deprecated", true)] public Collider collider => throw new System.NotSupportedException();
        [System.Obsolete("collider2D property has been deprecated", true)] public Collider2D collider2D => throw new System.NotSupportedException();
        [System.Obsolete("constantForce property has been deprecated", true)] public ConstantForce constantForce => throw new System.NotSupportedException();
        [System.Obsolete("hingeJoint property has been deprecated", true)] public HingeJoint hingeJoint => throw new System.NotSupportedException();
        [System.Obsolete("light property has been deprecated", true)] public Light light => throw new System.NotSupportedException();
        [System.Obsolete("networkView property has been deprecated", true)] public NetworkView networkView => throw new System.NotSupportedException();
        [System.Obsolete("particleSystem property has been deprecated", true)] public ParticleSystem particleSystem => throw new System.NotSupportedException();
        [System.Obsolete("renderer property has been deprecated", true)] public Renderer renderer => throw new System.NotSupportedException();
        [System.Obsolete("rigidbody property has been deprecated", true)] public Rigidbody rigidbody => throw new System.NotSupportedException();
        [System.Obsolete("rigidbody2D property has been deprecated", true)] public Rigidbody2D rigidbody2D => throw new System.NotSupportedException();
        public string tag { get; set; } = "Untagged";
        public bool CompareTag(string tag) => default;
        public bool CompareTag(TagHandle tag) => default;
        public T GetComponent<T>() where T : Component => null!;
        public Component GetComponent(System.Type type) => null!;
        public Component GetComponent(string name) => null!;
        public T[] GetComponents<T>() where T : Component => [];
        public Component[] GetComponents(System.Type type) => [];
        public void GetComponents(System.Type type, System.Collections.Generic.List<Component> results) { }
        public bool TryGetComponent<T>(out T component) where T : Component { component = null!; return false; }
        public bool TryGetComponent(System.Type type, out Component component) { component = null!; return false; }
        public int GetComponentIndex() => default;
        public void GetComponents<T>(System.Collections.Generic.List<T> results) where T : Component { }
        public T GetComponentInChildren<T>(bool includeInactive = false) where T : Component => null!;
        public Component GetComponentInChildren(System.Type type, bool includeInactive = false) => null!;
        public T GetComponentInParent<T>(bool includeInactive = false) where T : Component => null!;
        public Component GetComponentInParent(System.Type type, bool includeInactive = false) => null!;
        public T[] GetComponentsInChildren<T>(bool includeInactive = false) where T : Component => [];
        public Component[] GetComponentsInChildren(System.Type type, bool includeInactive = false) => [];
        public void GetComponentsInChildren<T>(System.Collections.Generic.List<T> results) where T : Component { }
        public void GetComponentsInChildren<T>(bool includeInactive, System.Collections.Generic.List<T> results) where T : Component { }
        public T[] GetComponentsInParent<T>(bool includeInactive = false) where T : Component => [];
        public Component[] GetComponentsInParent(System.Type type, bool includeInactive = false) => [];
        public void GetComponentsInParent<T>(bool includeInactive, System.Collections.Generic.List<T> results) where T : Component { }
    }
    public class Behaviour : Component {
        public bool enabled { get; set; } = true;
        public bool isActiveAndEnabled => enabled && gameObject.activeInHierarchy;
    }
    public class MonoBehaviour : Behaviour
    {
        public bool runInEditMode { get; set; }
        public bool useGUILayout { get; set; } = true;
        public System.Threading.CancellationToken destroyCancellationToken => default;
        public bool didAwake => false;
        public bool didStart => false;
        public Coroutine StartCoroutine(System.Collections.IEnumerator routine) => null!;
        public void StopCoroutine(System.Collections.IEnumerator routine) { }
        public void StopCoroutine(Coroutine routine) { }
        public void StopAllCoroutines() { }
        public Coroutine StartCoroutine_Auto(System.Collections.IEnumerator routine) => null!;
        public static void print(object value) { }
    }
    public class Animation : Behaviour { }
    public class AudioClip : Object
    {
        public float length { get; }
        public int samples { get; }
        public int frequency { get; }
        public int channels { get; }
        public static AudioClip Create(string name, int lengthSamples, int channels,
            int frequency, bool stream = false) => null!;
        public bool GetData(float[] data, int offsetSamples) => false;
        public bool SetData(float[] data, int offsetSamples) => false;
        public bool LoadAudioData() => false;
        public bool UnloadAudioData() => false;
    }
    public class AudioListener : Behaviour { public static float volume { get; set; } = 1; }
    public static class AudioSettings { public static double dspTime => 0; }
    public enum AudioRolloffMode { Logarithmic, Linear, Custom }
    public class AudioSource : Behaviour
    {
        public AudioClip clip { get; set; } = null!;
        public float pitch { get; set; } = 1;
        public float volume { get; set; } = 1;
        public bool mute { get; set; }
        public int priority { get; set; } = 128;
        public bool loop { get; set; }
        public bool playOnAwake { get; set; }
        public float spatialBlend { get; set; }
        public float panStereo { get; set; }
        public AudioRolloffMode rolloffMode { get; set; }
        public float minDistance { get; set; } = 1;
        public float maxDistance { get; set; } = 500;
        public bool bypassEffects { get; set; }
        public bool bypassListenerEffects { get; set; }
        public bool bypassReverbZones { get; set; }
        public bool spatialize { get; set; }
        public bool spatializePostEffects { get; set; }
        public float spread { get; set; }
        public float dopplerLevel { get; set; } = 1;
        public float reverbZoneMix { get; set; } = 1;
        public UnityEngine.Audio.AudioMixerGroup outputAudioMixerGroup { get; set; } = null!;
        public float time { get; set; }
        public int timeSamples { get; set; }
        public bool isPlaying { get; }
        public void Play() { }
        public void PlayDelayed(float delay) { }
        public void PlayScheduled(double dspTime) { }
        public void PlayOneShot(AudioClip clip, float volumeScale = 1) { }
        public static void PlayClipAtPoint(AudioClip clip, Vector3 position, float volume = 1) { }
        public void SetScheduledStartTime(double time) { }
        public void SetScheduledEndTime(double time) { }
        public void Pause() { }
        public void UnPause() { }
        public void Stop() { }
    }
    public class AudioLowPassFilter : Behaviour
    {
        public float cutoffFrequency { get; set; } = 5000;
        public float lowpassResonanceQ { get; set; } = 1;
    }
    public class AudioHighPassFilter : Behaviour
    {
        public float cutoffFrequency { get; set; } = 10;
        public float highpassResonanceQ { get; set; } = 1;
    }
    public class AudioEchoFilter : Behaviour
    {
        public float delay { get; set; } = 500;
        public float decayRatio { get; set; } = 0.5f;
        public float dryMix { get; set; } = 1;
        public float wetMix { get; set; } = 1;
    }
    public class AudioDistortionFilter : Behaviour
    {
        public float distortionLevel { get; set; } = 0.5f;
    }
    public class AudioChorusFilter : Behaviour
    {
        public float dryMix { get; set; } = 0.5f;
        public float wetMix1 { get; set; } = 0.5f;
        public float wetMix2 { get; set; } = 0.5f;
        public float wetMix3 { get; set; } = 0.5f;
        public float delay { get; set; } = 40;
        public float rate { get; set; } = 0.8f;
        public float depth { get; set; } = 0.03f;
    }
    public class AudioReverbFilter : Behaviour { }
    public class AudioReverbZone : Behaviour
    {
        public float minDistance { get; set; } = 10;
        public float maxDistance { get; set; } = 15;
    }
    public partial class Camera : Behaviour
    {
        public static Camera main => null!;
        public static Camera current => null!;
        public static Camera[] allCameras => [];
        public static int allCamerasCount => 0;
        public static int GetAllCameras(Camera[] results) => 0;
        public RenderTexture targetTexture { get; set; } = null!;
        public int pixelWidth { get; }
        public int pixelHeight { get; }
        public int scaledPixelWidth { get; }
        public int scaledPixelHeight { get; }
        public float aspect { get; set; }
        public float fieldOfView { get; set; }
        public bool orthographic { get; set; }
        public float orthographicSize { get; set; }
        public float nearClipPlane { get; set; }
        public float farClipPlane { get; set; }
        public float near { get; set; }
        public float far { get; set; }
        public float fov { get; set; }
        public CameraClearFlags clearFlags { get; set; }
        public Color backgroundColor { get; set; }
        public float depth { get; set; }
        public int cullingMask { get; set; }
        public int targetDisplay { get; set; }
        public CameraType cameraType { get; set; }
        public UnityEngine.SceneManagement.Scene scene { get; set; }
        public Matrix4x4 cameraToWorldMatrix => default;
        public Matrix4x4 worldToCameraMatrix { get; set; }
        public Matrix4x4 projectionMatrix { get; set; }
        public Rect rect { get; set; }
        public Rect pixelRect { get; set; }
        public void CalculateFrustumCorners(Rect viewport, float distance,
            MonoOrStereoscopicEye eye, Vector3[] corners) { }
        public void CopyFrom(Camera other) { }
        public void Reset() { }
        public void ResetAspect() { }
        public void ResetFieldOfView() { }
        public static float HorizontalToVerticalFieldOfView(float horizontalFieldOfView, float aspect) => 0;
        public static float VerticalToHorizontalFieldOfView(float verticalFieldOfView, float aspect) => 0;
        public Ray ScreenPointToRay(Vector3 point) => default;
        public Ray ScreenPointToRay(Vector3 point, MonoOrStereoscopicEye eye) => default;
        public Ray ViewportPointToRay(Vector3 point) => default;
        public Ray ViewportPointToRay(Vector3 point, MonoOrStereoscopicEye eye) => default;
        public Vector3 ScreenToViewportPoint(Vector3 point) => default;
        public Vector3 ViewportToScreenPoint(Vector3 point) => default;
        public Vector3 ScreenToWorldPoint(Vector3 point) => default;
        public Vector3 ScreenToWorldPoint(Vector3 point, MonoOrStereoscopicEye eye) => default;
        public Vector3 ViewportToWorldPoint(Vector3 point) => default;
        public Vector3 ViewportToWorldPoint(Vector3 point, MonoOrStereoscopicEye eye) => default;
        public Vector3 WorldToScreenPoint(Vector3 point) => default;
        public Vector3 WorldToScreenPoint(Vector3 point, MonoOrStereoscopicEye eye) => default;
        public Vector3 WorldToViewportPoint(Vector3 point) => default;
        public Vector3 WorldToViewportPoint(Vector3 point, MonoOrStereoscopicEye eye) => default;
        public void Render() { }
    }
    public class Collider2D : Behaviour { }
    public class ConstantForce : Behaviour { }
    public class HingeJoint : Component { }
    public class Light : Behaviour
    {
        public LightType type { get; set; }
        public Color color { get; set; }
        public float intensity { get; set; }
        public float range { get; set; }
        public float spotAngle { get; set; }
        public float innerSpotAngle { get; set; }
    }
    public class NetworkView : Component { }
    public partial class ParticleSystem : Behaviour { }
    public class Renderer : Behaviour
    {
        public Renderer() { }
        public bool castShadows { get; set; }
        public int sortingLayerID { get; set; }
        public string sortingLayerName { get; set; } = "Default";
        public int sortingOrder { get; set; }
        public bool receiveShadows { get; set; }
        public bool forceRenderingOff { get; set; }
        public UnityEngine.Rendering.ShadowCastingMode shadowCastingMode { get; set; }
        public Bounds bounds { get; set; }
        public Bounds localBounds { get; set; }
        public Matrix4x4 localToWorldMatrix { get; }
        public Matrix4x4 worldToLocalMatrix { get; }
        public void ResetBounds() { }
        public void ResetLocalBounds() { }
        public Material sharedMaterial { get; set; } = null!;
        public Material material { get; set; } = null!;
        public Material[] sharedMaterials { get; set; } = System.Array.Empty<Material>();
        public Material[] materials { get; set; } = System.Array.Empty<Material>();
        public void GetSharedMaterials(System.Collections.Generic.List<Material> materials) { }
        public void GetMaterials(System.Collections.Generic.List<Material> materials) { }
        public void SetSharedMaterials(System.Collections.Generic.List<Material> materials) { }
        public void SetMaterials(System.Collections.Generic.List<Material> materials) { }
        public bool HasPropertyBlock() => false;
        public void SetPropertyBlock(MaterialPropertyBlock block) { }
        public void SetPropertyBlock(MaterialPropertyBlock block, int materialIndex) { }
        public void GetPropertyBlock(MaterialPropertyBlock block) { }
        public void GetPropertyBlock(MaterialPropertyBlock block, int materialIndex) { }
    }
    public struct LOD
    {
        public float screenRelativeTransitionHeight { get; set; }
        public float fadeTransitionWidth { get; set; }
        public Renderer[] renderers { get; set; }
        public LOD(float screenRelativeTransitionHeight, Renderer[] renderers)
        {
            this.screenRelativeTransitionHeight = screenRelativeTransitionHeight;
            this.renderers = renderers;
            fadeTransitionWidth = 0;
        }
    }
    public class LODGroup : Behaviour
    {
        public int lodCount => 0;
        public float size { get; set; }
        public Vector3 localReferencePoint { get; set; }
        public LODFadeMode fadeMode { get; set; }
        public bool animateCrossFading { get; set; }
        public bool lastLODBillboard { get; set; }
        public static float crossFadeAnimationDuration { get; set; }
        public LOD[] GetLODs() => [];
        public void SetLODs(LOD[] lods) { }
        public void SetLODS(LOD[] lods) { }
        public void ForceLOD(int index) { }
        public void RecalculateBounds() { }
    }
    public class SkinnedMeshRenderer : Renderer
    {
        public SkinnedMeshRenderer() { }
        public Mesh sharedMesh { get; set; } = null!;
        public Transform[] bones { get; set; } = [];
        public Transform rootBone { get; set; } = null!;
        public SkinQuality quality { get; set; }
        public bool updateWhenOffscreen { get; set; }
        public bool skinnedMotionVectors { get; set; } = true;
        public bool forceMatrixRecalculationPerRender { get; set; }
        public GraphicsBuffer.Target vertexBufferTarget { get; set; }
        public GraphicsBuffer GetVertexBuffer() => null!;
        public GraphicsBuffer GetPreviousVertexBuffer() => null!;
        public uint GetShaderUserValue() => 0;
        public void SetShaderUserValue(uint value) { }
        public void BakeMesh(Mesh mesh) { }
        public void BakeMesh(Mesh mesh, bool useScale) { }
        public float GetBlendShapeWeight(int index) => 0;
        public void SetBlendShapeWeight(int index, float weight) { }
    }
    public struct SecondarySpriteTexture
    {
        public string name;
        public Texture2D texture;
        public bool Equals(SecondarySpriteTexture other) => false;
        public override bool Equals(object other) => false;
        public override int GetHashCode() => 0;
        public static bool operator ==(SecondarySpriteTexture left, SecondarySpriteTexture right) => false;
        public static bool operator !=(SecondarySpriteTexture left, SecondarySpriteTexture right) => false;
    }
    public class Sprite : Object
    {
        public Texture2D texture { get; }
        public Rect rect { get; }
        public Rect textureRect { get; }
        public Vector2 textureRectOffset { get; }
        public Vector2 pivot { get; }
        public float pixelsPerUnit { get; }
        public Bounds bounds { get; }
        public Vector4 border { get; }
        public Vector2[] vertices { get; }
        public Vector2[] uv { get; }
        public ushort[] triangles { get; }
        public bool packed { get; }
        public SpritePackingMode packingMode { get; }
        public SpritePackingRotation packingRotation { get; }
        public float spriteAtlasTextureScale { get; }
        public Texture2D associatedAlphaSplitTexture { get; }
        public static Sprite Create(Texture2D texture, Rect rect, Vector2 pivot,
            float pixelsPerUnit = 100, uint extrude = 0,
            SpriteMeshType meshType = SpriteMeshType.Tight,
            Vector4 border = default, bool generateFallbackPhysicsShape = false) => null!;
        public static Sprite Create(Texture2D texture, Rect rect, Vector2 pivot,
            float pixelsPerUnit, uint extrude, SpriteMeshType meshType,
            Vector4 border, bool generateFallbackPhysicsShape,
            SecondarySpriteTexture[] secondaryTextures) => null!;
        public int GetPhysicsShapeCount() => 0;
        public int GetPhysicsShapePointCount(int index) => 0;
        public System.ReadOnlySpan<Vector2> GetPhysicsShape(int index) => default;
        public int GetPhysicsShape(int index, System.Collections.Generic.List<Vector2> results) => 0;
        public int GetSecondaryTextureCount() => 0;
        public int GetSecondaryTextures(SecondarySpriteTexture[] results) => 0;
        public void AddScriptableObject(ScriptableObject value) { }
        public uint GetScriptableObjectsCount() => 0;
        public uint GetScriptableObjects(ScriptableObject[] results) => 0;
        public void SetScriptableObjectAt(ScriptableObject value, uint index) { }
        public void RemoveScriptableObjectAt(uint index) { }
        public void OverrideGeometry(Vector2[] vertices, ushort[] triangles) { }
        public void OverridePhysicsShape(System.Collections.Generic.IList<Vector2[]> shapes) { }
    }
    public class SpriteRenderer : Renderer
    {
        public SpriteRenderer() { }
        public Sprite sprite { get; set; } = null!;
        public Color color { get; set; }
        public bool flipX { get; set; }
        public bool flipY { get; set; }
        public Vector2 size { get; set; }
        public SpriteDrawMode drawMode { get; set; }
        public SpriteTileMode tileMode { get; set; }
        public SpriteSortPoint spriteSortPoint { get; set; }
        public float adaptiveModeThreshold { get; set; }
        public void RegisterSpriteChangeCallback(UnityEngine.Events.UnityAction<SpriteRenderer> callback) { }
        public void UnregisterSpriteChangeCallback(UnityEngine.Events.UnityAction<SpriteRenderer> callback) { }
    }
    public class LineRenderer : Renderer
    {
        public int positionCount { get; set; }
        public int numPositions { get; set; }
        public float startWidth { get; set; }
        public float endWidth { get; set; }
        public Color startColor { get; set; }
        public Color endColor { get; set; }
        public Gradient colorGradient { get; set; } = null!;
        public bool useWorldSpace { get; set; }
        public Vector3 GetPosition(int index) => default;
        public int GetPositions(Vector3[] values) => 0;
        public int GetPositions(Unity.Collections.NativeArray<Vector3> values) => 0;
        public int GetPositions(Unity.Collections.NativeSlice<Vector3> values) => 0;
        public void SetPosition(int index, Vector3 value) { }
        public void SetPositions(Vector3[] values) { }
        public void SetPositions(Unity.Collections.NativeArray<Vector3> values) { }
        public void SetPositions(Unity.Collections.NativeSlice<Vector3> values) { }
        public void SetVertexCount(int count) { }
        public void SetWidth(float start, float end) { }
        public void SetColors(Color start, Color end) { }
        public void Simplify(float tolerance) { }
        public bool loop { get; set; }
        public float widthMultiplier { get; set; }
    }
    public class TrailRenderer : Renderer
    {
        public TrailRenderer() { }
        public object alignment { get; set; } = null!;
        public bool applyActiveColorSpace { get; set; }
        public int numCapVertices { get; set; }
        public int numCornerVertices { get; set; }
        public float shadowBias { get; set; }
        public object textureMode { get; set; } = null!;
        public AnimationCurve widthCurve { get; set; } = null!;
        public void BakeMesh(Mesh mesh, bool useTransform = false) { }
        public void BakeMesh(Mesh mesh, Camera camera, bool useTransform = false) { }
        public bool emitting { get; set; }
        public float time { get; set; }
        public float minVertexDistance { get; set; }
        public bool autodestruct { get; set; }
        public bool generateLightingData { get; set; }
        public Vector2 textureScale { get; set; }
        public float startWidth { get; set; }
        public float endWidth { get; set; }
        public float widthMultiplier { get; set; }
        public Color startColor { get; set; }
        public Color endColor { get; set; }
        public Gradient colorGradient { get; set; } = null!;
        public int positionCount => 0;
        public int numPositions => 0;
        public void AddPosition(Vector3 position) { }
        public void AddPositions(Vector3[] positions) { }
        public void AddPositions(Unity.Collections.NativeArray<Vector3> positions) { }
        public void AddPositions(Unity.Collections.NativeSlice<Vector3> positions) { }
        public void Clear() { }
        public Vector3 GetPosition(int index) => default;
        public int GetPositions(Vector3[] positions) => 0;
        public int GetPositions(Unity.Collections.NativeArray<Vector3> positions) => 0;
        public int GetPositions(Unity.Collections.NativeSlice<Vector3> positions) => 0;
        public int GetVisiblePositions(Vector3[] positions) => 0;
        public int GetVisiblePositions(Unity.Collections.NativeArray<Vector3> positions) => 0;
        public int GetVisiblePositions(Unity.Collections.NativeSlice<Vector3> positions) => 0;
        public void SetPosition(int index, Vector3 position) { }
        public void SetPositions(Vector3[] positions) { }
        public void SetPositions(Unity.Collections.NativeArray<Vector3> positions) { }
        public void SetPositions(Unity.Collections.NativeSlice<Vector3> positions) { }
    }
    public struct CombineInstance
    {
        public Mesh mesh;
        public int subMeshIndex;
        public Matrix4x4 transform;
        public Vector4 lightmapScaleOffset;
        public Vector4 realtimeLightmapScaleOffset;
    }

    public class Mesh : Object
    {
        public struct LodSelectionCurve
        {
            public LodSelectionCurve(float lodBias, float lodSlope) { }
            public float lodBias;
            public float lodSlope;
            public bool IsValid() => false;
        }
        public Mesh() { }
        public bool isReadable => true;
        public void MarkDynamic() { }
        public void UploadMeshData(bool markNoLongerReadable) { }
        public void CombineMeshes(CombineInstance[] combine, bool mergeSubMeshes = true,
            bool useMatrices = true, bool hasLightmapData = false) { }
        public Vector3[] vertices { get; set; } = [];
        public Vector3[] normals { get; set; } = [];
        public Vector2[] uv { get; set; } = [];
        public Vector2[] uv2 { get; set; } = [];
        public Vector2[] uv3 { get; set; } = [];
        public Vector2[] uv4 { get; set; } = [];
        public Vector2[] uv5 { get; set; } = [];
        public Vector2[] uv6 { get; set; } = [];
        public Vector2[] uv7 { get; set; } = [];
        public Vector2[] uv8 { get; set; } = [];
        public Color[] colors { get; set; } = [];
        public Color32[] colors32 { get; set; } = [];
        public Matrix4x4[] bindposes { get; set; } = [];
        public int bindposeCount => bindposes.Length;
        public BoneWeight[] boneWeights { get; set; } = [];
        public Unity.Collections.NativeArray<Matrix4x4> GetBindposes() => new(0, Unity.Collections.Allocator.Temp);
        public void SetBindposes(Unity.Collections.NativeArray<Matrix4x4> bindposes) { }
        public void GetBindposes(System.Collections.Generic.List<Matrix4x4> results) { }
        public void GetBoneWeights(System.Collections.Generic.List<BoneWeight> results) { }
        public Unity.Collections.NativeArray<byte> GetBonesPerVertex() => new(0, Unity.Collections.Allocator.Temp);
        public Unity.Collections.NativeArray<BoneWeight1> GetAllBoneWeights() => new(0, Unity.Collections.Allocator.Temp);
        public void SetBoneWeights(Unity.Collections.NativeArray<byte> bonesPerVertex,
            Unity.Collections.NativeArray<BoneWeight1> weights) { }
        public UnityEngine.Rendering.IndexFormat indexFormat { get; set; }
        public Vector4[] tangents { get; set; } = [];
        public int[] triangles { get; set; } = [];
        public Bounds bounds { get; set; }
        public int vertexCount => vertices.Length;
        public int blendShapeCount => 0;
        public void AddBlendShapeFrame(string name, float weight, Vector3[] deltaVertices,
            Vector3[] deltaNormals, Vector3[] deltaTangents) { }
        public void AddBlendShapeFrame(string name, float weight,
            System.ReadOnlySpan<Vector3> deltaVertices, System.ReadOnlySpan<Vector3> deltaNormals,
            System.ReadOnlySpan<Vector3> deltaTangents) { }
        public void ClearBlendShapes() { }
        public int GetBlendShapeFrameCount(int index) => 0;
        public void GetBlendShapeFrameVertices(int index, int frame,
            Vector3[] deltaVertices, Vector3[] deltaNormals, Vector3[] deltaTangents) { }
        public float GetBlendShapeFrameWeight(int index, int frame) => 0;
        public int GetBlendShapeIndex(string name) => -1;
        public string GetBlendShapeName(int index) => "";
        public int subMeshCount { get; set; } = 1;
        public void SetTriangles(int[] triangles, int submesh, bool calculateBounds = true) { }
        public void SetTriangles(int[] triangles, int submesh, bool calculateBounds, int baseVertex) { }
        public void SetTriangles(ushort[] triangles, int submesh, bool calculateBounds, int baseVertex) { }
        public void SetTriangles(System.Collections.Generic.List<int> triangles, int submesh, bool calculateBounds = true) { }
        public void SetTriangles(System.Collections.Generic.List<int> triangles, int submesh, bool calculateBounds, int baseVertex) { }
        public void SetTriangles(System.Collections.Generic.List<ushort> triangles, int submesh, bool calculateBounds, int baseVertex) { }
        public void SetTriangles(int[] triangles, int start, int length, int submesh, bool calculateBounds, int baseVertex) { }
        public void SetTriangles(ushort[] triangles, int start, int length, int submesh, bool calculateBounds, int baseVertex) { }
        public void SetTriangles(System.Collections.Generic.List<int> triangles, int start, int length, int submesh, bool calculateBounds, int baseVertex) { }
        public void SetTriangles(System.Collections.Generic.List<ushort> triangles, int start, int length, int submesh, bool calculateBounds, int baseVertex) { }
        public void SetTriangles(int[] triangles, int submesh, int lod, bool calculateBounds, int baseVertex) { }
        public void SetTriangles(ushort[] triangles, int submesh, int lod, bool calculateBounds, int baseVertex) { }
        public void SetTriangles(System.Collections.Generic.List<int> triangles, int submesh, int lod, bool calculateBounds, int baseVertex) { }
        public void SetTriangles(System.Collections.Generic.List<ushort> triangles, int submesh, int lod, bool calculateBounds, int baseVertex) { }
        public void SetTriangles(int[] triangles, int start, int length, int submesh, int lod, bool calculateBounds, int baseVertex) { }
        public void SetTriangles(ushort[] triangles, int start, int length, int submesh, int lod, bool calculateBounds, int baseVertex) { }
        public void SetTriangles(System.Collections.Generic.List<int> triangles, int start, int length, int submesh, int lod, bool calculateBounds, int baseVertex) { }
        public void SetTriangles(System.Collections.Generic.List<ushort> triangles, int start, int length, int submesh, int lod, bool calculateBounds, int baseVertex) { }
        public int[] GetTriangles(int submesh) => [];
        public int[] GetTriangles(int submesh, bool applyBaseVertex) => [];
        public void GetTriangles(System.Collections.Generic.List<int> triangles, int submesh) { }
        public void GetTriangles(System.Collections.Generic.List<int> triangles, int submesh, bool applyBaseVertex) { }
        public void GetTriangles(System.Collections.Generic.List<ushort> triangles, int submesh, bool applyBaseVertex) { }
        public int[] GetTriangles(int submesh, int lod, bool applyBaseVertex) => [];
        public void GetTriangles(System.Collections.Generic.List<int> triangles, int submesh, int lod, bool applyBaseVertex) { }
        public void GetTriangles(System.Collections.Generic.List<ushort> triangles, int submesh, int lod, bool applyBaseVertex) { }
        public int[] GetIndices(int submesh) => [];
        public int[] GetIndices(int submesh, bool applyBaseVertex) => [];
        public void GetIndices(System.Collections.Generic.List<int> indices, int submesh) { }
        public void GetIndices(System.Collections.Generic.List<int> indices, int submesh, bool applyBaseVertex) { }
        public void GetIndices(System.Collections.Generic.List<ushort> indices, int submesh, bool applyBaseVertex) { }
        public int[] GetIndices(int submesh, int lod, bool applyBaseVertex) => [];
        public void GetIndices(System.Collections.Generic.List<int> indices, int submesh, int lod, bool applyBaseVertex) { }
        public void GetIndices(System.Collections.Generic.List<ushort> indices, int submesh, int lod, bool applyBaseVertex) { }
        public void SetIndices(int[] indices, MeshTopology topology, int submesh) { }
        public void SetIndices<T>(Unity.Collections.NativeArray<T> indices,
            MeshTopology topology, int submesh, bool calculateBounds, int baseVertex) where T : struct { }
        public void SetIndices<T>(Unity.Collections.NativeArray<T> indices,
            int start, int length, MeshTopology topology, int submesh,
            bool calculateBounds, int baseVertex) where T : struct { }
        public void SetIndices(int[] indices, MeshTopology topology, int submesh, bool calculateBounds) { }
        public void SetIndices(int[] indices, MeshTopology topology, int submesh, bool calculateBounds, int baseVertex) { }
        public void SetIndices(ushort[] indices, MeshTopology topology, int submesh, bool calculateBounds, int baseVertex) { }
        public void SetIndices(System.Collections.Generic.List<int> indices, MeshTopology topology, int submesh, bool calculateBounds, int baseVertex) { }
        public void SetIndices(System.Collections.Generic.List<ushort> indices, MeshTopology topology, int submesh, bool calculateBounds, int baseVertex) { }
        public void SetIndices(int[] indices, int start, int length, MeshTopology topology, int submesh, bool calculateBounds, int baseVertex) { }
        public void SetIndices(ushort[] indices, int start, int length, MeshTopology topology, int submesh, bool calculateBounds, int baseVertex) { }
        public void SetIndices(System.Collections.Generic.List<int> indices, int start, int length, MeshTopology topology, int submesh, bool calculateBounds, int baseVertex) { }
        public void SetIndices(System.Collections.Generic.List<ushort> indices, int start, int length, MeshTopology topology, int submesh, bool calculateBounds, int baseVertex) { }
        public void SetIndices(int[] indices, MeshTopology topology, int submesh, int lod, bool calculateBounds, int baseVertex) { }
        public void SetIndices(ushort[] indices, MeshTopology topology, int submesh, int lod, bool calculateBounds, int baseVertex) { }
        public void SetIndices(System.Collections.Generic.List<int> indices, MeshTopology topology, int submesh, int lod, bool calculateBounds, int baseVertex) { }
        public void SetIndices(System.Collections.Generic.List<ushort> indices, MeshTopology topology, int submesh, int lod, bool calculateBounds, int baseVertex) { }
        public void SetIndices(int[] indices, int start, int length, MeshTopology topology, int submesh, int lod, bool calculateBounds, int baseVertex) { }
        public void SetIndices(ushort[] indices, int start, int length, MeshTopology topology, int submesh, int lod, bool calculateBounds, int baseVertex) { }
        public void SetIndices(System.Collections.Generic.List<int> indices, int start, int length, MeshTopology topology, int submesh, int lod, bool calculateBounds, int baseVertex) { }
        public void SetIndices(System.Collections.Generic.List<ushort> indices, int start, int length, MeshTopology topology, int submesh, int lod, bool calculateBounds, int baseVertex) { }
        public void SetIndices<T>(Unity.Collections.NativeArray<T> indices, MeshTopology topology, int submesh, int lod, bool calculateBounds, int baseVertex) where T : struct { }
        public void SetIndices<T>(Unity.Collections.NativeArray<T> indices, int start, int length, MeshTopology topology, int submesh, int lod, bool calculateBounds, int baseVertex) where T : struct { }
        public uint GetIndexCount(int submesh) => 0;
        public uint GetIndexStart(int submesh) => 0;
        public uint GetIndexCount(int submesh, int lod) => 0;
        public uint GetIndexStart(int submesh, int lod) => 0;
        public uint GetBaseVertex(int submesh) => 0;
        public MeshTopology GetTopology(int submesh) => MeshTopology.Triangles;
        public UnityEngine.Rendering.SubMeshDescriptor GetSubMesh(int submesh) => default;
        public void SetSubMesh(int submesh, UnityEngine.Rendering.SubMeshDescriptor descriptor,
            UnityEngine.Rendering.MeshUpdateFlags flags = 0) { }
        public void SetSubMeshes(UnityEngine.Rendering.SubMeshDescriptor[] descriptors,
            UnityEngine.Rendering.MeshUpdateFlags flags = 0) { }
        public void SetSubMeshes<T>(Unity.Collections.NativeArray<T> descriptors,
            UnityEngine.Rendering.MeshUpdateFlags flags) where T : struct { }
        public void SetSubMeshes<T>(Unity.Collections.NativeArray<T> descriptors,
            int start, int length, UnityEngine.Rendering.MeshUpdateFlags flags) where T : struct { }
        public void SetSubMeshes(UnityEngine.Rendering.SubMeshDescriptor[] descriptors, int start, int length,
            UnityEngine.Rendering.MeshUpdateFlags flags = 0) { }
        public void SetSubMeshes(System.Collections.Generic.List<UnityEngine.Rendering.SubMeshDescriptor> descriptors,
            UnityEngine.Rendering.MeshUpdateFlags flags = 0) { }
        public void SetSubMeshes(System.Collections.Generic.List<UnityEngine.Rendering.SubMeshDescriptor> descriptors,
            int start, int length, UnityEngine.Rendering.MeshUpdateFlags flags = 0) { }
        public int vertexAttributeCount => 0;
        public bool HasVertexAttribute(UnityEngine.Rendering.VertexAttribute attribute) => false;
        public int GetVertexAttributeDimension(UnityEngine.Rendering.VertexAttribute attribute) => 0;
        public int GetVertexAttributeStream(UnityEngine.Rendering.VertexAttribute attribute) => 0;
        public UnityEngine.Rendering.VertexAttributeDescriptor GetVertexAttribute(int index) => default;
        public UnityEngine.Rendering.VertexAttributeDescriptor[] GetVertexAttributes() => [];
        public int GetVertexAttributes(System.Collections.Generic.List<UnityEngine.Rendering.VertexAttributeDescriptor> results) => 0;
        public int GetVertexAttributes(UnityEngine.Rendering.VertexAttributeDescriptor[] results) => 0;
        public UnityEngine.Rendering.VertexAttributeFormat GetVertexAttributeFormat(UnityEngine.Rendering.VertexAttribute attribute) => default;
        public int GetVertexAttributeOffset(UnityEngine.Rendering.VertexAttribute attribute) => 0;
        public int GetVertexBufferStride(int stream) => 0;
        public void SetVertices(System.Collections.Generic.List<Vector3> vertices) { }
        public void SetVertices<T>(Unity.Collections.NativeArray<T> vertices) where T : struct { }
        public void SetVertices<T>(Unity.Collections.NativeArray<T> vertices, int start, int length) where T : struct { }
        public void SetVertices<T>(Unity.Collections.NativeArray<T> vertices, int start, int length,
            UnityEngine.Rendering.MeshUpdateFlags flags) where T : struct { }
        public void SetVertices(System.Collections.Generic.List<Vector3> vertices, int start, int length) { }
        public void SetVertices(System.Collections.Generic.List<Vector3> vertices, int start, int length,
            UnityEngine.Rendering.MeshUpdateFlags flags) { }
        public void SetVertices(Vector3[] vertices) { }
        public void SetVertices(Vector3[] vertices, int start, int length) { }
        public void SetVertices(Vector3[] vertices, int start, int length,
            UnityEngine.Rendering.MeshUpdateFlags flags) { }
        public void GetVertices(System.Collections.Generic.List<Vector3> vertices) { }
        public void SetNormals(System.Collections.Generic.List<Vector3> normals) { }
        public void SetNormals<T>(Unity.Collections.NativeArray<T> normals) where T : struct { }
        public void SetNormals<T>(Unity.Collections.NativeArray<T> normals, int start, int length) where T : struct { }
        public void SetNormals<T>(Unity.Collections.NativeArray<T> normals, int start, int length,
            UnityEngine.Rendering.MeshUpdateFlags flags) where T : struct { }
        public void SetNormals(System.Collections.Generic.List<Vector3> normals, int start, int length) { }
        public void SetNormals(System.Collections.Generic.List<Vector3> normals, int start, int length,
            UnityEngine.Rendering.MeshUpdateFlags flags) { }
        public void SetNormals(Vector3[] normals) { }
        public void SetNormals(Vector3[] normals, int start, int length) { }
        public void SetNormals(Vector3[] normals, int start, int length,
            UnityEngine.Rendering.MeshUpdateFlags flags) { }
        public void GetNormals(System.Collections.Generic.List<Vector3> normals) { }
        public void SetColors(System.Collections.Generic.List<Color> colors) { }
        public void SetColors<T>(Unity.Collections.NativeArray<T> colors) where T : struct { }
        public void SetColors<T>(Unity.Collections.NativeArray<T> colors, int start, int length) where T : struct { }
        public void SetColors<T>(Unity.Collections.NativeArray<T> colors, int start, int length,
            UnityEngine.Rendering.MeshUpdateFlags flags) where T : struct { }
        public void SetColors(System.Collections.Generic.List<Color> colors, int start, int length) { }
        public void SetColors(System.Collections.Generic.List<Color> colors, int start, int length,
            UnityEngine.Rendering.MeshUpdateFlags flags) { }
        public void SetColors(System.Collections.Generic.List<Color32> colors) { }
        public void SetColors(System.Collections.Generic.List<Color32> colors, int start, int length) { }
        public void SetColors(System.Collections.Generic.List<Color32> colors, int start, int length,
            UnityEngine.Rendering.MeshUpdateFlags flags) { }
        public void SetColors(Color[] colors) { }
        public void SetColors(Color[] colors, int start, int length) { }
        public void SetColors(Color[] colors, int start, int length,
            UnityEngine.Rendering.MeshUpdateFlags flags) { }
        public void SetColors(Color32[] colors) { }
        public void SetColors(Color32[] colors, int start, int length) { }
        public void SetColors(Color32[] colors, int start, int length,
            UnityEngine.Rendering.MeshUpdateFlags flags) { }
        public void GetColors(System.Collections.Generic.List<Color> colors) { }
        public void GetColors(System.Collections.Generic.List<Color32> colors) { }
        public void SetTangents(System.Collections.Generic.List<Vector4> tangents) { }
        public void SetTangents<T>(Unity.Collections.NativeArray<T> tangents) where T : struct { }
        public void SetTangents<T>(Unity.Collections.NativeArray<T> tangents, int start, int length) where T : struct { }
        public void SetTangents<T>(Unity.Collections.NativeArray<T> tangents, int start, int length,
            UnityEngine.Rendering.MeshUpdateFlags flags) where T : struct { }
        public void SetTangents(System.Collections.Generic.List<Vector4> tangents, int start, int length) { }
        public void SetTangents(System.Collections.Generic.List<Vector4> tangents, int start, int length,
            UnityEngine.Rendering.MeshUpdateFlags flags) { }
        public void SetTangents(Vector4[] tangents) { }
        public void SetTangents(Vector4[] tangents, int start, int length) { }
        public void SetTangents(Vector4[] tangents, int start, int length,
            UnityEngine.Rendering.MeshUpdateFlags flags) { }
        public void GetTangents(System.Collections.Generic.List<Vector4> tangents) { }
        public void SetUVs(int channel, System.Collections.Generic.List<Vector2> uvs) { }
        public void SetUVs<T>(int channel, Unity.Collections.NativeArray<T> uvs) where T : struct { }
        public void SetUVs<T>(int channel, Unity.Collections.NativeArray<T> uvs, int start, int length) where T : struct { }
        public void SetUVs<T>(int channel, Unity.Collections.NativeArray<T> uvs, int start, int length,
            UnityEngine.Rendering.MeshUpdateFlags flags) where T : struct { }
        public void SetUVs(int channel, System.Collections.Generic.List<Vector3> uvs) { }
        public void SetUVs(int channel, System.Collections.Generic.List<Vector4> uvs) { }
        public void SetUVs(int channel, System.Collections.Generic.List<Vector2> uvs, int start, int length) { }
        public void SetUVs(int channel, System.Collections.Generic.List<Vector2> uvs, int start, int length,
            UnityEngine.Rendering.MeshUpdateFlags flags) { }
        public void SetUVs(int channel, System.Collections.Generic.List<Vector3> uvs, int start, int length) { }
        public void SetUVs(int channel, System.Collections.Generic.List<Vector3> uvs, int start, int length,
            UnityEngine.Rendering.MeshUpdateFlags flags) { }
        public void SetUVs(int channel, System.Collections.Generic.List<Vector4> uvs, int start, int length) { }
        public void SetUVs(int channel, System.Collections.Generic.List<Vector4> uvs, int start, int length,
            UnityEngine.Rendering.MeshUpdateFlags flags) { }
        public void SetUVs(int channel, Vector2[] uvs) { }
        public void SetUVs(int channel, Vector3[] uvs) { }
        public void SetUVs(int channel, Vector4[] uvs) { }
        public void SetUVs(int channel, Vector2[] uvs, int start, int length) { }
        public void SetUVs(int channel, Vector2[] uvs, int start, int length,
            UnityEngine.Rendering.MeshUpdateFlags flags) { }
        public void SetUVs(int channel, Vector3[] uvs, int start, int length) { }
        public void SetUVs(int channel, Vector3[] uvs, int start, int length,
            UnityEngine.Rendering.MeshUpdateFlags flags) { }
        public void SetUVs(int channel, Vector4[] uvs, int start, int length) { }
        public void SetUVs(int channel, Vector4[] uvs, int start, int length,
            UnityEngine.Rendering.MeshUpdateFlags flags) { }
        public void GetUVs(int channel, System.Collections.Generic.List<Vector2> uvs) { }
        public void GetUVs(int channel, System.Collections.Generic.List<Vector3> uvs) { }
        public void GetUVs(int channel, System.Collections.Generic.List<Vector4> uvs) { }
        public void RecalculateBounds() { }
        public void RecalculateBounds(UnityEngine.Rendering.MeshUpdateFlags flags) { }
        public void RecalculateNormals() { }
        public void RecalculateNormals(UnityEngine.Rendering.MeshUpdateFlags flags) { }
        public void RecalculateTangents() { }
        public void RecalculateTangents(UnityEngine.Rendering.MeshUpdateFlags flags) { }
        public void Clear(bool keepVertexLayout = true) { }
    }
    public class MeshFilter : Component
    {
        public Mesh sharedMesh { get; set; } = null!;
        public Mesh mesh { get; set; } = null!;
    }
    public class MeshRenderer : Renderer
    {
        public Mesh additionalVertexStreams { get; set; } = null!;
        public Mesh enlightenVertexStream { get; set; } = null!;
        public int globalIlluminationMeshLod { get; set; }
        public object receiveGI { get; set; } = null!;
        public float scaleInLightmap { get; set; }
        public bool stitchLightmapSeams { get; set; }
        public int subMeshStartIndex => 0;
        public uint GetShaderUserValue() => 0;
        public void SetShaderUserValue(uint value) { }
    }
    public class Rigidbody2D : Component { }
    public class GameObject : Object
    {
        public static GameObject Find(string name) => null!;
        public void SendMessage(string method) { }
        public void SendMessage(string method, SendMessageOptions options) { }
        public void SendMessage(string method, object value) { }
        public void SendMessage(string method, object value, SendMessageOptions options) { }
        public void SendMessageUpwards(string method) { }
        public void SendMessageUpwards(string method, SendMessageOptions options) { }
        public void SendMessageUpwards(string method, object value) { }
        public void SendMessageUpwards(string method, object value, SendMessageOptions options) { }
        public void BroadcastMessage(string method) { }
        public void BroadcastMessage(string method, SendMessageOptions options) { }
        public void BroadcastMessage(string method, object value) { }
        public void BroadcastMessage(string method, object value, SendMessageOptions options) { }
        public GameObject() { }
        public GameObject(string name) { }
        public GameObject(string name, params System.Type[] components) { }
        public static UnityEngine.SceneManagement.Scene GetScene(int id) => default;
        public static UnityEngine.SceneManagement.Scene GetScene(EntityId id) => default;
        public static void SetGameObjectsActive(System.ReadOnlySpan<int> ids, bool active) { }
        public static void SetGameObjectsActive(System.ReadOnlySpan<EntityId> ids, bool active) { }
        public static void SetGameObjectsActive(Unity.Collections.NativeArray<int> ids, bool active) { }
        public static void SetGameObjectsActive(Unity.Collections.NativeArray<EntityId> ids, bool active) { }
        public static void InstantiateGameObjects(int sourceId, int count,
            Unity.Collections.NativeArray<int> objectIds, Unity.Collections.NativeArray<int> transformIds,
            UnityEngine.SceneManagement.Scene destinationScene) { }
        public static void InstantiateGameObjects(EntityId sourceId, int count,
            Unity.Collections.NativeArray<EntityId> objectIds,
            Unity.Collections.NativeArray<EntityId> transformIds,
            UnityEngine.SceneManagement.Scene destinationScene) { }
        public static GameObject CreatePrimitive(PrimitiveType type) => new GameObject();
        public UnityEngine.SceneManagement.Scene scene { get; }
        public Transform transform { get; set; } = null!;
        public TransformHandle transformHandle { get; }
        public ulong sceneCullingMask { get; }
        [System.Obsolete("animation property has been deprecated", true)] public Animation animation => throw new System.NotSupportedException();
        [System.Obsolete("audio property has been deprecated", true)] public AudioSource audio => throw new System.NotSupportedException();
        [System.Obsolete("camera property has been deprecated", true)] public Camera camera => throw new System.NotSupportedException();
        [System.Obsolete("collider property has been deprecated", true)] public Collider collider => throw new System.NotSupportedException();
        [System.Obsolete("collider2D property has been deprecated", true)] public Collider2D collider2D => throw new System.NotSupportedException();
        [System.Obsolete("constantForce property has been deprecated", true)] public ConstantForce constantForce => throw new System.NotSupportedException();
        [System.Obsolete("hingeJoint property has been deprecated", true)] public HingeJoint hingeJoint => throw new System.NotSupportedException();
        [System.Obsolete("light property has been deprecated", true)] public Light light => throw new System.NotSupportedException();
        [System.Obsolete("networkView property has been deprecated", true)] public NetworkView networkView => throw new System.NotSupportedException();
        [System.Obsolete("particleSystem property has been deprecated", true)] public ParticleSystem particleSystem => throw new System.NotSupportedException();
        [System.Obsolete("renderer property has been deprecated", true)] public Renderer renderer => throw new System.NotSupportedException();
        [System.Obsolete("rigidbody property has been deprecated", true)] public Rigidbody rigidbody => throw new System.NotSupportedException();
        [System.Obsolete("rigidbody2D property has been deprecated", true)] public Rigidbody2D rigidbody2D => throw new System.NotSupportedException();
        public string tag { get; set; } = "Untagged";
        public bool CompareTag(string tag) => default;
        public bool CompareTag(TagHandle tag) => default;
        public int layer { get; set; }
        public bool isStatic { get; set; }
        public bool activeSelf { get; }
        public bool active { get; set; }
        public bool activeInHierarchy { get; }
        public GameObject gameObject => this;
        public void SetActiveRecursively(bool value) { }
        public static GameObject FindWithTag(string tag) => null!;
        public static GameObject FindGameObjectWithTag(string tag) => null!;
        public static GameObject[] FindGameObjectsWithTag(string tag) => [];
        public static void FindGameObjectsWithTag(string tag, System.Collections.Generic.List<GameObject> results) { }
        public T AddComponent<T>() where T : Component, new() => new T();
        public Component AddComponent(System.Type type) => null!;
        [System.Obsolete("GameObject.AddComponent with string argument has been deprecated. Use GameObject.AddComponent<T>() instead. (UnityUpgradable).", true)]
        public Component AddComponent(string typeName) => null!;
        public T GetComponent<T>() where T : Component => null!;
        public Component GetComponent(System.Type type) => null!;
        public Component GetComponent(string name) => null!;
        public T[] GetComponents<T>() where T : Component => [];
        public Component[] GetComponents(System.Type type) => [];
        public bool TryGetComponent<T>(out T component) where T : Component { component = null!; return false; }
        public bool TryGetComponent(System.Type type, out Component component) { component = null!; return false; }
        public int GetComponentCount() => default;
        public Component GetComponentAtIndex(int index) => null!;
        public T GetComponentAtIndex<T>(int index) where T : Component => null!;
        public int GetComponentIndex(Component component) => default;
        public void GetComponents<T>(System.Collections.Generic.List<T> results) where T : Component { }
        public void GetComponents(System.Type type, System.Collections.Generic.List<Component> results) { }
        public T GetComponentInChildren<T>(bool includeInactive = false) where T : Component => null!;
        public Component GetComponentInChildren(System.Type type, bool includeInactive = false) => null!;
        public T GetComponentInParent<T>(bool includeInactive = false) where T : Component => null!;
        public Component GetComponentInParent(System.Type type, bool includeInactive = false) => null!;
        public T[] GetComponentsInChildren<T>(bool includeInactive = false) where T : Component => [];
        public Component[] GetComponentsInChildren(System.Type type, bool includeInactive = false) => [];
        public void GetComponentsInChildren<T>(System.Collections.Generic.List<T> results) where T : Component { }
        public void GetComponentsInChildren<T>(bool includeInactive, System.Collections.Generic.List<T> results) where T : Component { }
        public T[] GetComponentsInParent<T>(bool includeInactive = false) where T : Component => [];
        public Component[] GetComponentsInParent(System.Type type, bool includeInactive = false) => [];
        public void GetComponentsInParent<T>(bool includeInactive, System.Collections.Generic.List<T> results) where T : Component { }
        public void SetActive(bool active) { }
    }
    public enum PrimitiveType { Sphere, Capsule, Cylinder, Cube, Plane, Quad }
    public struct LayerMask
    {
        public int value;
        public static int GetMask(params string[] names) => 0;
        public static string LayerToName(int layer) => "";
        public static int NameToLayer(string layerName) => -1;
        public static implicit operator int(LayerMask mask) => mask.value;
        public static implicit operator LayerMask(int value) => new LayerMask { value = value };
    }
    public struct RenderingLayerMask
    {
        public uint value { get; set; }
        public static RenderingLayerMask defaultRenderingLayerMask => new() { value = 1 };
        public static int GetDefinedRenderingLayerCount() => 1;
        public static string[] GetDefinedRenderingLayerNames() => ["Default"];
        public static int[] GetDefinedRenderingLayerValues() => [1];
        public static uint GetDefinedRenderingLayersCombinedMaskValue() => 1;
        public static int GetLastDefinedRenderingLayerIndex() => 0;
        public static uint GetMask(params string[] names) => 0;
        public static uint GetMask(System.ReadOnlySpan<string> names) => 0;
        public static int GetRenderingLayerCount() => 1;
        public static int NameToRenderingLayer(string name) => -1;
        public static string RenderingLayerToName(int index) => "";
        public static implicit operator RenderingLayerMask(int value) => new() { value = unchecked((uint)value) };
        public static implicit operator RenderingLayerMask(uint value) => new() { value = value };
        public static implicit operator int(RenderingLayerMask mask) => unchecked((int)mask.value);
        public static implicit operator uint(RenderingLayerMask mask) => mask.value;
    }
    [System.AttributeUsage(System.AttributeTargets.Class, Inherited = true)]
    public sealed class DisallowMultipleComponent : System.Attribute { }
    [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = true)]
    public sealed class RequireComponent : System.Attribute {
        public RequireComponent(System.Type first) { }
        public RequireComponent(System.Type first, System.Type second) { }
        public RequireComponent(System.Type first, System.Type second, System.Type third) { }
    }
    [System.AttributeUsage(System.AttributeTargets.Class)]
    public sealed class AddComponentMenu : System.Attribute {
        public AddComponentMenu(string menuName) { componentMenu = menuName; }
        public AddComponentMenu(string menuName, int order) {
            componentMenu = menuName; componentOrder = order;
        }
        public string componentMenu { get; }
        public int componentOrder { get; }
    }
    [System.AttributeUsage(System.AttributeTargets.Field)]
    public sealed class ColorUsageAttribute : System.Attribute {
        public ColorUsageAttribute(bool showAlpha) { this.showAlpha = showAlpha; }
        public ColorUsageAttribute(bool showAlpha, bool hdr) {
            this.showAlpha = showAlpha; this.hdr = hdr;
        }
        public ColorUsageAttribute(bool showAlpha, bool hdr, float minBrightness,
            float maxBrightness, float minExposureValue, float maxExposureValue) {
            this.showAlpha = showAlpha; this.hdr = hdr;
            this.minBrightness = minBrightness; this.maxBrightness = maxBrightness;
            this.minExposureValue = minExposureValue; this.maxExposureValue = maxExposureValue;
        }
        public bool showAlpha { get; }
        public bool hdr { get; }
        public float minBrightness { get; }
        public float maxBrightness { get; }
        public float minExposureValue { get; }
        public float maxExposureValue { get; }
    }
    public class Collider : Component { }
    public class Transform : Component
    {
        public Transform parent { get; set; } = null!;
        public Transform root { get; } = null!;
        public int childCount { get; }
        public int hierarchyCount { get; }
        public int hierarchyCapacity { get; set; }
        public bool hasChanged { get; set; }
        public int GetChildCount() => default;
        public System.Collections.IEnumerator GetEnumerator() => null!;
        public Transform GetChild(int index) => null!;
        public Transform Find(string path) => null!;
        public Transform FindChild(string path) => null!;
        public bool IsChildOf(Transform parent) => default;
        public int GetSiblingIndex() => default;
        public void SetSiblingIndex(int index) { }
        public void SetAsFirstSibling() { }
        public void SetAsLastSibling() { }
        public void DetachChildren() { }
        public Vector3 position { get; set; }
        public Vector3 localPosition { get; set; }
        public Vector3 localScale { get; set; }
        public Vector3 lossyScale { get; }
        public Quaternion rotation { get; set; }
        public Quaternion localRotation { get; set; }
        public Vector3 eulerAngles { get; set; }
        public Vector3 localEulerAngles { get; set; }
        public Vector3 forward { get; set; }
        public Vector3 up { get; set; }
        public Vector3 right { get; set; }
        public void SetPositionAndRotation(Vector3 position, Quaternion rotation) { }
        public void SetLocalPositionAndRotation(Vector3 position, Quaternion rotation) { }
        public void GetPositionAndRotation(out Vector3 position, out Quaternion rotation) { position = default; rotation = default; }
        public void GetLocalPositionAndRotation(out Vector3 position, out Quaternion rotation) { position = default; rotation = default; }
        public Matrix4x4 localToWorldMatrix { get; }
        public Matrix4x4 worldToLocalMatrix { get; }
        public void SetParent(Transform parent, bool worldPositionStays = true) { }
        public Vector3 TransformPoint(Vector3 value) => default;
        public Vector3 TransformPoint(float x, float y, float z) => default;
        public Vector3 InverseTransformPoint(Vector3 value) => default;
        public Vector3 InverseTransformPoint(float x, float y, float z) => default;
        public Vector3 TransformDirection(Vector3 value) => default;
        public Vector3 TransformDirection(float x, float y, float z) => default;
        public Vector3 InverseTransformDirection(Vector3 value) => default;
        public Vector3 InverseTransformDirection(float x, float y, float z) => default;
        public Vector3 TransformVector(Vector3 value) => default;
        public Vector3 TransformVector(float x, float y, float z) => default;
        public Vector3 InverseTransformVector(Vector3 value) => default;
        public Vector3 InverseTransformVector(float x, float y, float z) => default;
        public void TransformPoints(System.ReadOnlySpan<Vector3> input, System.Span<Vector3> output) { }
        public void TransformPoints(System.Span<Vector3> values) { }
        public void InverseTransformPoints(System.ReadOnlySpan<Vector3> input, System.Span<Vector3> output) { }
        public void InverseTransformPoints(System.Span<Vector3> values) { }
        public void TransformVectors(System.ReadOnlySpan<Vector3> input, System.Span<Vector3> output) { }
        public void TransformVectors(System.Span<Vector3> values) { }
        public void InverseTransformVectors(System.ReadOnlySpan<Vector3> input, System.Span<Vector3> output) { }
        public void InverseTransformVectors(System.Span<Vector3> values) { }
        public void TransformDirections(System.ReadOnlySpan<Vector3> input, System.Span<Vector3> output) { }
        public void TransformDirections(System.Span<Vector3> values) { }
        public void InverseTransformDirections(System.ReadOnlySpan<Vector3> input, System.Span<Vector3> output) { }
        public void InverseTransformDirections(System.Span<Vector3> values) { }
        public void Translate(Vector3 translation) { }
        public void Translate(Vector3 translation, Space relativeTo) { }
        public void Translate(Vector3 translation, Transform relativeTo) { }
        public void Translate(float x, float y, float z) { }
        public void Translate(float x, float y, float z, Space relativeTo) { }
        public void Translate(float x, float y, float z, Transform relativeTo) { }
        public void Rotate(Vector3 eulers) { }
        public void Rotate(Vector3 eulers, Space relativeTo) { }
        public void Rotate(Vector3 axis, float angle) { }
        public void Rotate(Vector3 axis, float angle, Space relativeTo) { }
        public void Rotate(float x, float y, float z) { }
        public void Rotate(float x, float y, float z, Space relativeTo) { }
        public void RotateAround(Vector3 point, Vector3 axis, float angle) { }
        public void RotateAround(Vector3 axis, float angle) { }
        public void RotateAroundLocal(Vector3 axis, float angle) { }
        public void LookAt(Transform target) { }
        public void LookAt(Transform target, Vector3 worldUp) { }
        public void LookAt(Vector3 point) { }
        public void LookAt(Vector3 point, Vector3 worldUp) { }
    }
    public partial class RectTransform : Transform
    {
        public delegate void ReapplyDrivenProperties(RectTransform rect);
        public static event ReapplyDrivenProperties reapplyDrivenProperties;
        public Vector2 anchorMin { get; set; }
        public Vector2 anchorMax { get; set; }
        public Vector2 pivot { get; set; }
        public Vector2 sizeDelta { get; set; }
        public Vector2 anchoredPosition { get; set; }
        public Vector3 anchoredPosition3D { get; set; }
        public Vector2 offsetMin { get; set; }
        public Vector2 offsetMax { get; set; }
        public Rect rect => default;
        public Object drivenByObject => null;
        public bool sendChildDimensionsChange { get; set; }
        public void ForceUpdateRectTransforms() { }
        public void GetLocalCorners(Vector3[] corners) { }
        public void GetWorldCorners(Vector3[] corners) { }
        public void SetInsetAndSizeFromParentEdge(Edge edge, float inset, float size) { }
        public void SetSizeWithCurrentAnchors(Axis axis, float size) { }
    }
    public struct DrivenRectTransformTracker
    {
        public void Add(Object driver, RectTransform rectTransform, DrivenTransformProperties drivenProperties) { }
        public void Clear() { }
        public void Clear(bool undo) { }
        public static void StartRecordingUndo() { }
        public static void StopRecordingUndo() { }
    }
    public enum Space { World = 0, Self = 1 }
    public struct Vector2
    {
        public float x, y;
        public float this[int index] { get => 0; set { } }
        public Vector2(float x, float y) { this.x = x; this.y = y; }
        public float magnitude => 0; public float sqrMagnitude => 0; public Vector2 normalized => this;
        public static Vector2 zero => new(); public static Vector2 one => new(1, 1);
        public static Vector2 positiveInfinity => new(float.PositiveInfinity, float.PositiveInfinity);
        public static Vector2 negativeInfinity => new(float.NegativeInfinity, float.NegativeInfinity);
        public const float kEpsilon = 0.00001f;
        public const float kEpsilonNormalSqrt = 0.000000000000001f;
        public static Vector2 up => new(0, 1); public static Vector2 down => new(0, -1);
        public static Vector2 left => new(-1, 0); public static Vector2 right => new(1, 0);
        public void Normalize() { } public void Set(float x, float y) { }
        public void Scale(Vector2 scale) { }
        public void Scale(in Vector2 scale) { }
        public float SqrMagnitude() => 0;
        public bool Equals(Vector2 other) => false;
        public bool Equals(in Vector2 other) => false;
        public static float Distance(Vector2 a, Vector2 b) => 0;
        public static float Distance(in Vector2 a, in Vector2 b) => 0;
        public static float Dot(Vector2 a, Vector2 b) => 0;
        public static float Dot(in Vector2 a, in Vector2 b) => 0;
        public static Vector2 Normalize(Vector2 v) => v;
        public static Vector2 Normalize(in Vector2 v) => v;
        public static float SqrMagnitude(Vector2 v) => 0;
        public static float SqrMagnitude(in Vector2 v) => 0;
        public static float Angle(Vector2 a, Vector2 b) => 0;
        public static float Angle(in Vector2 a, in Vector2 b) => 0;
        public static float SignedAngle(Vector2 a, Vector2 b) => 0;
        public static float SignedAngle(in Vector2 a, in Vector2 b) => 0;
        public static Vector2 Lerp(Vector2 a, Vector2 b, float t) => a;
        public static Vector2 Lerp(in Vector2 a, in Vector2 b, float t) => a;
        public static Vector2 LerpUnclamped(Vector2 a, Vector2 b, float t) => a;
        public static Vector2 LerpUnclamped(in Vector2 a, in Vector2 b, float t) => a;
        public static Vector2 MoveTowards(Vector2 a, Vector2 b, float maxDistanceDelta) => a;
        public static Vector2 MoveTowards(in Vector2 a, in Vector2 b, float maxDistanceDelta) => a;
        public static Vector2 ClampMagnitude(Vector2 v, float maxLength) => v;
        public static Vector2 ClampMagnitude(in Vector2 v, float maxLength) => v;
        public static Vector2 Scale(Vector2 a, Vector2 b) => a;
        public static Vector2 Scale(in Vector2 a, in Vector2 b) => a;
        public static Vector2 Min(Vector2 a, Vector2 b) => a;
        public static Vector2 Min(in Vector2 a, in Vector2 b) => a;
        public static Vector2 Max(Vector2 a, Vector2 b) => a;
        public static Vector2 Max(in Vector2 a, in Vector2 b) => a;
        public static Vector2 Perpendicular(Vector2 v) => v;
        public static Vector2 Perpendicular(in Vector2 v) => v;
        public static Vector2 Reflect(Vector2 v, Vector2 normal) => v;
        public static Vector2 Reflect(in Vector2 v, in Vector2 normal) => v;
        public static Vector2 SmoothDamp(Vector2 current, Vector2 target, ref Vector2 currentVelocity,
            float smoothTime, float maxSpeed = float.PositiveInfinity, float deltaTime = 0.02f) => current;
        public static Vector2 SmoothDamp(in Vector2 current, in Vector2 target, ref Vector2 currentVelocity,
            float smoothTime) => current;
        public static Vector2 SmoothDamp(in Vector2 current, in Vector2 target, ref Vector2 currentVelocity,
            float smoothTime, float maxSpeed) => current;
        public static Vector2 SmoothDamp(in Vector2 current, in Vector2 target, ref Vector2 currentVelocity,
            float smoothTime, float maxSpeed, float deltaTime) => current;
        public static implicit operator Vector2(Vector3 value) => new(value.x, value.y);
        public static implicit operator Vector3(Vector2 value) => new(value.x, value.y, 0);
        public static Vector2 operator +(Vector2 a, Vector2 b) => a;
        public static Vector2 operator -(Vector2 a, Vector2 b) => a;
        public static Vector2 operator -(Vector2 value) => value;
        public static Vector2 operator *(Vector2 a, float b) => a;
        public static Vector2 operator *(float a, Vector2 b) => b;
        public static Vector2 operator *(Vector2 a, Vector2 b) => a;
        public static Vector2 operator /(Vector2 a, float b) => a;
        public static Vector2 operator /(Vector2 a, Vector2 b) => a;
        public static bool operator ==(Vector2 a, Vector2 b) => false;
        public static bool operator !=(Vector2 a, Vector2 b) => false;
        public override bool Equals(object? other) => false;
        public override int GetHashCode() => 0;
        public override string ToString() => "";
        public string ToString(string format) => "";
        public string ToString(string format, System.IFormatProvider provider) => "";
    }
    public struct Vector2Int
    {
        public int x, y;
        public int this[int index] { get => 0; set { } }
        public Vector2Int(int x, int y) { this.x = x; this.y = y; }
        public float magnitude => 0;
        public int sqrMagnitude => 0;
        public static Vector2Int zero => new(0, 0);
        public static Vector2Int one => new(1, 1);
        public static Vector2Int up => new(0, 1);
        public static Vector2Int down => new(0, -1);
        public static Vector2Int left => new(-1, 0);
        public static Vector2Int right => new(1, 0);
        public void Set(int x, int y) { }
        public void Scale(Vector2Int scale) { }
        public void Scale(in Vector2Int scale) { }
        public void Clamp(Vector2Int min, Vector2Int max) { }
        public void Clamp(in Vector2Int min, in Vector2Int max) { }
        public static Vector2Int Scale(Vector2Int a, Vector2Int b) => a;
        public static Vector2Int Scale(in Vector2Int a, in Vector2Int b) => a;
        public static Vector2Int Min(Vector2Int a, Vector2Int b) => a;
        public static Vector2Int Min(in Vector2Int a, in Vector2Int b) => a;
        public static Vector2Int Max(Vector2Int a, Vector2Int b) => a;
        public static Vector2Int Max(in Vector2Int a, in Vector2Int b) => a;
        public static float Distance(Vector2Int a, Vector2Int b) => 0;
        public static float Distance(in Vector2Int a, in Vector2Int b) => 0;
        public static Vector2Int FloorToInt(Vector2 value) => default;
        public static Vector2Int FloorToInt(in Vector2 value) => default;
        public static Vector2Int CeilToInt(Vector2 value) => default;
        public static Vector2Int CeilToInt(in Vector2 value) => default;
        public static Vector2Int RoundToInt(Vector2 value) => default;
        public static Vector2Int RoundToInt(in Vector2 value) => default;
        public bool Equals(in Vector2Int other) => false;
        public static implicit operator Vector2(Vector2Int value) => default;
        public static explicit operator Vector3Int(Vector2Int value) => default;
        public static Vector2Int operator +(Vector2Int a, Vector2Int b) => a;
        public static Vector2Int operator -(Vector2Int a, Vector2Int b) => a;
        public static Vector2Int operator -(Vector2Int value) => value;
        public static Vector2Int operator *(Vector2Int a, Vector2Int b) => a;
        public static Vector2Int operator *(Vector2Int a, int b) => a;
        public static Vector2Int operator *(int a, Vector2Int b) => b;
        public static Vector2Int operator /(Vector2Int a, int b) => a;
        public static bool operator ==(Vector2Int a, Vector2Int b) => false;
        public static bool operator !=(Vector2Int a, Vector2Int b) => false;
        public override bool Equals(object? other) => false;
        public override int GetHashCode() => 0;
        public override string ToString() => "";
        public string ToString(string format) => "";
        public string ToString(string format, System.IFormatProvider provider) => "";
    }
    public struct BoundsInt
    {
        public int x, y, z;
        public BoundsInt(int x, int y, int z, int sizeX, int sizeY, int sizeZ)
        { this.x = x; this.y = y; this.z = z; }
        public BoundsInt(Vector3Int position, Vector3Int size)
        { x = position.x; y = position.y; z = position.z; }
        public BoundsInt(in Vector3Int position, in Vector3Int size)
        { x = position.x; y = position.y; z = position.z; }
        public Vector3 center => default;
        public int xMin { get => 0; set { } }
        public int yMin { get => 0; set { } }
        public int zMin { get => 0; set { } }
        public int xMax { get => 0; set { } }
        public int yMax { get => 0; set { } }
        public int zMax { get => 0; set { } }
        public Vector3Int min { get => default; set { } }
        public Vector3Int max { get => default; set { } }
        public Vector3Int position { get => default; set { } }
        public Vector3Int size { get => default; set { } }
        public PositionEnumerator allPositionsWithin => default;
        public struct PositionEnumerator
        {
            public PositionEnumerator(in Vector3Int min, in Vector3Int max) { }
            public Vector3Int Current => default;
            public PositionEnumerator GetEnumerator() => this;
            public bool MoveNext() => false;
            public void Reset() { }
        }
        public void SetMinMax(Vector3Int min, Vector3Int max) { }
        public void SetMinMax(in Vector3Int min, in Vector3Int max) { }
        public void ClampToBounds(BoundsInt bounds) { }
        public void ClampToBounds(in BoundsInt bounds) { }
        public bool Contains(Vector3Int point) => false;
        public bool Contains(in Vector3Int point) => false;
        public bool Equals(BoundsInt other) => false;
        public bool Equals(in BoundsInt other) => false;
        public static bool operator ==(BoundsInt a, BoundsInt b) => false;
        public static bool operator !=(BoundsInt a, BoundsInt b) => false;
        public override bool Equals(object? other) => false;
        public override int GetHashCode() => 0;
        public override string ToString() => "";
        public string ToString(string format) => "";
        public string ToString(string format, System.IFormatProvider provider) => "";
    }
    public struct RectInt
    {
        public int x, y, width, height;
        public RectInt(int x, int y, int width, int height)
        { this.x = x; this.y = y; this.width = width; this.height = height; }
        public RectInt(Vector2Int position, Vector2Int size)
        { x = position.x; y = position.y; width = size.x; height = size.y; }
        public RectInt(in Vector2Int position, in Vector2Int size)
        { x = position.x; y = position.y; width = size.x; height = size.y; }
        public Vector2 center => default;
        public int xMin { get => 0; set { } }
        public int yMin { get => 0; set { } }
        public int xMax { get => 0; set { } }
        public int yMax { get => 0; set { } }
        public Vector2Int min { get => default; set { } }
        public Vector2Int max { get => default; set { } }
        public Vector2Int position { get => default; set { } }
        public Vector2Int size { get => default; set { } }
        public static RectInt zero => default;
        public PositionEnumerator allPositionsWithin => default;
        public struct PositionEnumerator
        {
            public PositionEnumerator(in Vector2Int min, in Vector2Int max) { }
            public Vector2Int Current => default;
            public PositionEnumerator GetEnumerator() => this;
            public bool MoveNext() => false;
            public void Reset() { }
        }
        public void SetMinMax(Vector2Int min, Vector2Int max) { }
        public void SetMinMax(in Vector2Int min, in Vector2Int max) { }
        public void ClampToBounds(RectInt bounds) { }
        public void ClampToBounds(in RectInt bounds) { }
        public bool Contains(Vector2Int point) => false;
        public bool Contains(in Vector2Int point) => false;
        public bool Overlaps(RectInt other) => false;
        public bool Overlaps(in RectInt other) => false;
        public bool Equals(RectInt other) => false;
        public bool Equals(in RectInt other) => false;
        public static bool operator ==(RectInt a, RectInt b) => false;
        public static bool operator !=(RectInt a, RectInt b) => false;
        public override bool Equals(object? other) => false;
        public override int GetHashCode() => 0;
        public string ToString(string format) => "";
        public string ToString(string format, System.IFormatProvider provider) => "";
    }
    public struct Vector3Int
    {
        public int x, y, z;
        public int this[int index] { get => 0; set { } }
        public Vector3Int(int x, int y) { this.x = x; this.y = y; this.z = 0; }
        public Vector3Int(int x, int y, int z) { this.x = x; this.y = y; this.z = z; }
        public float magnitude => 0;
        public int sqrMagnitude => 0;
        public static Vector3Int zero => new(0, 0, 0);
        public static Vector3Int one => new(1, 1, 1);
        public static Vector3Int up => new(0, 1, 0);
        public static Vector3Int down => new(0, -1, 0);
        public static Vector3Int left => new(-1, 0, 0);
        public static Vector3Int right => new(1, 0, 0);
        public static Vector3Int forward => new(0, 0, 1);
        public static Vector3Int back => new(0, 0, -1);
        public void Set(int x, int y, int z) { }
        public void Scale(Vector3Int scale) { }
        public void Scale(in Vector3Int scale) { }
        public void Clamp(Vector3Int min, Vector3Int max) { }
        public void Clamp(in Vector3Int min, in Vector3Int max) { }
        public static Vector3Int Scale(Vector3Int a, Vector3Int b) => a;
        public static Vector3Int Scale(in Vector3Int a, in Vector3Int b) => a;
        public static Vector3Int Min(Vector3Int a, Vector3Int b) => a;
        public static Vector3Int Min(in Vector3Int a, in Vector3Int b) => a;
        public static Vector3Int Max(Vector3Int a, Vector3Int b) => a;
        public static Vector3Int Max(in Vector3Int a, in Vector3Int b) => a;
        public static float Distance(Vector3Int a, Vector3Int b) => 0;
        public static float Distance(in Vector3Int a, in Vector3Int b) => 0;
        public static Vector3Int FloorToInt(Vector3 value) => default;
        public static Vector3Int FloorToInt(in Vector3 value) => default;
        public static Vector3Int CeilToInt(Vector3 value) => default;
        public static Vector3Int CeilToInt(in Vector3 value) => default;
        public static Vector3Int RoundToInt(Vector3 value) => default;
        public static Vector3Int RoundToInt(in Vector3 value) => default;
        public bool Equals(in Vector3Int other) => false;
        public static implicit operator Vector3(Vector3Int value) => default;
        public static explicit operator Vector2Int(Vector3Int value) => default;
        public static Vector3Int operator +(Vector3Int a, Vector3Int b) => a;
        public static Vector3Int operator -(Vector3Int a, Vector3Int b) => a;
        public static Vector3Int operator -(Vector3Int value) => value;
        public static Vector3Int operator *(Vector3Int a, Vector3Int b) => a;
        public static Vector3Int operator *(Vector3Int a, int b) => a;
        public static Vector3Int operator *(int a, Vector3Int b) => b;
        public static Vector3Int operator /(Vector3Int a, int b) => a;
        public static bool operator ==(Vector3Int a, Vector3Int b) => false;
        public static bool operator !=(Vector3Int a, Vector3Int b) => false;
        public override bool Equals(object? other) => false;
        public override int GetHashCode() => 0;
        public override string ToString() => "";
        public string ToString(string format) => "";
        public string ToString(string format, System.IFormatProvider provider) => "";
    }
    public struct Vector3
    {
        public float x, y, z;
        public float this[int index] { get => 0; set { } }
        public Vector3(float x, float y) { this.x = x; this.y = y; this.z = 0; }
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public static Vector3 zero => new(0, 0, 0);
        public static Vector3 one => new(1, 1, 1);
        public static Vector3 positiveInfinity => new(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
        public static Vector3 negativeInfinity => new(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
        public static Vector3 up => new(0, 1, 0);
        public static Vector3 down => new(0, -1, 0);
        public static Vector3 left => new(-1, 0, 0);
        public static Vector3 right => new(1, 0, 0);
        public static Vector3 forward => new(0, 0, 1);
        public static Vector3 fwd => forward;
        public static Vector3 back => new(0, 0, -1);
        public const float kEpsilon = 0.00001f;
        public const float kEpsilonNormalSqrt = 0.000000000000001f;
        public float magnitude => 0; public float sqrMagnitude => 0; public Vector3 normalized => this;
        public void Normalize() { } public void Set(float x, float y, float z) { }
        public void Scale(Vector3 scale) { }
        public void Scale(in Vector3 scale) { }
        public bool Equals(Vector3 other) => false;
        public bool Equals(in Vector3 other) => false;
        public static float Dot(Vector3 left, Vector3 right) => left.x * right.x + left.y * right.y + left.z * right.z;
        public static float Dot(in Vector3 left, in Vector3 right) => 0;
        public static Vector3 Normalize(Vector3 v) => v;
        public static Vector3 Normalize(in Vector3 v) => v;
        public static float SqrMagnitude(Vector3 v) => 0;
        public static float SqrMagnitude(in Vector3 v) => 0;
        public static float Magnitude(Vector3 v) => 0;
        public static float Magnitude(in Vector3 v) => 0;
        public static float Distance(Vector3 a, Vector3 b) => 0;
        public static float Distance(in Vector3 a, in Vector3 b) => 0;
        public static float Angle(Vector3 a, Vector3 b) => 0;
        public static float Angle(in Vector3 a, in Vector3 b) => 0;
        public static float AngleBetween(Vector3 a, Vector3 b) => 0;
        public static float SignedAngle(Vector3 a, Vector3 b, Vector3 axis) => 0;
        public static float SignedAngle(in Vector3 a, in Vector3 b, in Vector3 axis) => 0;
        public static Vector3 Cross(Vector3 a, Vector3 b) => a;
        public static Vector3 Cross(in Vector3 a, in Vector3 b) => a;
        public static Vector3 Lerp(Vector3 a, Vector3 b, float t) => a;
        public static Vector3 Lerp(in Vector3 a, in Vector3 b, float t) => a;
        public static Vector3 LerpUnclamped(Vector3 a, Vector3 b, float t) => a;
        public static Vector3 LerpUnclamped(in Vector3 a, in Vector3 b, float t) => a;
        public static Vector3 Slerp(Vector3 a, Vector3 b, float t) => a;
        public static Vector3 Slerp(in Vector3 a, in Vector3 b, float t) => a;
        public static Vector3 SlerpUnclamped(Vector3 a, Vector3 b, float t) => a;
        public static Vector3 SlerpUnclamped(in Vector3 a, in Vector3 b, float t) => a;
        public static Vector3 MoveTowards(Vector3 a, Vector3 b, float maxDistanceDelta) => a;
        public static Vector3 MoveTowards(in Vector3 a, in Vector3 b, float maxDistanceDelta) => a;
        public static Vector3 RotateTowards(Vector3 a, Vector3 b, float maxRadiansDelta, float maxMagnitudeDelta) => a;
        public static Vector3 RotateTowards(in Vector3 a, in Vector3 b, float maxRadiansDelta, float maxMagnitudeDelta) => a;
        public static Vector3 ClampMagnitude(Vector3 v, float maxLength) => v;
        public static Vector3 ClampMagnitude(in Vector3 v, float maxLength) => v;
        public static Vector3 Scale(Vector3 a, Vector3 b) => a;
        public static Vector3 Scale(in Vector3 a, in Vector3 b) => a;
        public static Vector3 Min(Vector3 a, Vector3 b) => a;
        public static Vector3 Min(in Vector3 a, in Vector3 b) => a;
        public static Vector3 Max(Vector3 a, Vector3 b) => a;
        public static Vector3 Max(in Vector3 a, in Vector3 b) => a;
        public static Vector3 Project(Vector3 v, Vector3 onNormal) => v;
        public static Vector3 Project(in Vector3 v, in Vector3 onNormal) => v;
        public static Vector3 ProjectOnPlane(Vector3 v, Vector3 normal) => v;
        public static Vector3 ProjectOnPlane(in Vector3 v, in Vector3 normal) => v;
        public static Vector3 Reflect(Vector3 v, Vector3 normal) => v;
        public static Vector3 Reflect(in Vector3 v, in Vector3 normal) => v;
        public static Vector3 Exclude(Vector3 excludeThis, Vector3 fromThat) => fromThat;
        public static Vector3 SmoothDamp(Vector3 current, Vector3 target, ref Vector3 currentVelocity,
            float smoothTime, float maxSpeed = float.PositiveInfinity, float deltaTime = 0.02f) => current;
        public static Vector3 SmoothDamp(in Vector3 current, in Vector3 target, ref Vector3 currentVelocity,
            float smoothTime) => current;
        public static Vector3 SmoothDamp(in Vector3 current, in Vector3 target, ref Vector3 currentVelocity,
            float smoothTime, float maxSpeed) => current;
        public static Vector3 SmoothDamp(in Vector3 current, in Vector3 target, ref Vector3 currentVelocity,
            float smoothTime, float maxSpeed, float deltaTime) => current;
        public static void OrthoNormalize(ref Vector3 normal, ref Vector3 tangent) { }
        public static void OrthoNormalize(ref Vector3 normal, ref Vector3 tangent, ref Vector3 binormal) { }
        public static Vector3 operator +(Vector3 left, Vector3 right) => new(left.x + right.x, left.y + right.y, left.z + right.z);
        public static Vector3 operator -(Vector3 left, Vector3 right) => new(left.x - right.x, left.y - right.y, left.z - right.z);
        public static Vector3 operator -(Vector3 value) => new(-value.x, -value.y, -value.z);
        public static Vector3 operator *(Vector3 value, float scalar) => new(value.x * scalar, value.y * scalar, value.z * scalar);
        public static Vector3 operator *(float scalar, Vector3 value) => new(value.x * scalar, value.y * scalar, value.z * scalar);
        public static Vector3 operator /(Vector3 value, float scalar) => new(value.x / scalar, value.y / scalar, value.z / scalar);
        public static bool operator ==(Vector3 left, Vector3 right) => false;
        public static bool operator !=(Vector3 left, Vector3 right) => false;
        public override bool Equals(object? other) => false;
        public override int GetHashCode() => 0;
        public override string ToString() => "";
        public string ToString(string format) => "";
        public string ToString(string format, System.IFormatProvider provider) => "";
    }
    public struct Vector4
    {
        public float x, y, z, w;
        public float this[int index] { get => 0; set { } }
        public Vector4(float x, float y) { this.x = x; this.y = y; this.z = 0; this.w = 0; }
        public Vector4(float x, float y, float z) { this.x = x; this.y = y; this.z = z; this.w = 0; }
        public Vector4(float x, float y, float z, float w)
        { this.x = x; this.y = y; this.z = z; this.w = w; }
        public float magnitude => 0; public float sqrMagnitude => 0; public Vector4 normalized => this;
        public static Vector4 zero => new(); public static Vector4 one => new(1, 1, 1, 1);
        public static Vector4 positiveInfinity => new(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
        public static Vector4 negativeInfinity => new(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
        public const float kEpsilon = 0.00001f;
        public void Normalize() { } public void Set(float x, float y, float z, float w) { }
        public void Scale(Vector4 scale) { }
        public void Scale(in Vector4 scale) { }
        public float SqrMagnitude() => 0;
        public bool Equals(Vector4 other) => false;
        public bool Equals(in Vector4 other) => false;
        public static float Distance(Vector4 a, Vector4 b) => 0;
        public static float Distance(in Vector4 a, in Vector4 b) => 0;
        public static float Dot(Vector4 a, Vector4 b) => 0;
        public static float Dot(in Vector4 a, in Vector4 b) => 0;
        public static Vector4 Normalize(Vector4 v) => v;
        public static Vector4 Normalize(in Vector4 v) => v;
        public static float SqrMagnitude(Vector4 v) => 0;
        public static float SqrMagnitude(in Vector4 v) => 0;
        public static float Magnitude(Vector4 v) => 0;
        public static float Magnitude(in Vector4 v) => 0;
        public static Vector4 Lerp(Vector4 a, Vector4 b, float t) => a;
        public static Vector4 Lerp(in Vector4 a, in Vector4 b, float t) => a;
        public static Vector4 LerpUnclamped(Vector4 a, Vector4 b, float t) => a;
        public static Vector4 LerpUnclamped(in Vector4 a, in Vector4 b, float t) => a;
        public static Vector4 MoveTowards(Vector4 a, Vector4 b, float maxDistanceDelta) => a;
        public static Vector4 MoveTowards(in Vector4 a, in Vector4 b, float maxDistanceDelta) => a;
        public static Vector4 Scale(Vector4 a, Vector4 b) => a;
        public static Vector4 Scale(in Vector4 a, in Vector4 b) => a;
        public static Vector4 Project(Vector4 a, Vector4 b) => a;
        public static Vector4 Project(in Vector4 a, in Vector4 b) => a;
        public static Vector4 Min(Vector4 a, Vector4 b) => a;
        public static Vector4 Min(in Vector4 a, in Vector4 b) => a;
        public static Vector4 Max(Vector4 a, Vector4 b) => a;
        public static Vector4 Max(in Vector4 a, in Vector4 b) => a;
        public static implicit operator Vector2(Vector4 value) => new(value.x, value.y);
        public static implicit operator Vector3(Vector4 value) => new(value.x, value.y, value.z);
        public static implicit operator Vector4(Vector2 value) => new(value.x, value.y, 0, 0);
        public static implicit operator Vector4(Vector3 value) => new(value.x, value.y, value.z, 0);
        public static Vector4 operator +(Vector4 a, Vector4 b) => a;
        public static Vector4 operator -(Vector4 a, Vector4 b) => a;
        public static Vector4 operator -(Vector4 value) => value;
        public static Vector4 operator *(Vector4 a, float b) => a;
        public static Vector4 operator *(float a, Vector4 b) => b;
        public static Vector4 operator /(Vector4 a, float b) => a;
        public static bool operator ==(Vector4 a, Vector4 b) => false;
        public static bool operator !=(Vector4 a, Vector4 b) => false;
        public override bool Equals(object? other) => false;
        public override int GetHashCode() => 0;
        public override string ToString() => "";
        public string ToString(string format) => "";
        public string ToString(string format, System.IFormatProvider provider) => "";
    }
    public struct Quaternion
    {
        public const float kEpsilon = 1e-6f;
        public float x, y, z, w;
        public float this[int index] { get => 0; set { } }
        public Quaternion(float x, float y, float z, float w) { this.x = x; this.y = y; this.z = z; this.w = w; }
        public static Quaternion identity => new(0, 0, 0, 1);
        public Quaternion normalized => this;
        public Vector3 eulerAngles { get; set; }
        public void Normalize() { } public void Set(float x, float y, float z, float w) { }
        public void SetFromToRotation(Vector3 from, Vector3 to) { }
        public void SetFromToRotation(in Vector3 from, in Vector3 to) { }
        public void SetLookRotation(Vector3 forward) { }
        public void SetLookRotation(Vector3 forward, Vector3 up) { }
        public void SetLookRotation(in Vector3 forward) { }
        public void SetLookRotation(in Vector3 forward, in Vector3 up) { }
        public void SetAxisAngle(Vector3 axis, float angle) { }
        public void SetEulerAngles(Vector3 angles) { }
        public void SetEulerAngles(float x, float y, float z) { }
        public void SetEulerRotation(Vector3 angles) { }
        public void SetEulerRotation(float x, float y, float z) { }
        public void ToAngleAxis(out float angle, out Vector3 axis) { angle = 0; axis = default; }
        public void ToAxisAngle(out Vector3 axis, out float angle) { axis = default; angle = 0; }
        public Vector3 ToEuler() => default;
        public Vector3 ToEulerAngles() => default;
        public static Vector3 ToEulerAngles(Quaternion q) => default;
        public static Quaternion Euler(float x, float y, float z) => identity;
        public static Quaternion Euler(Vector3 angles) => identity;
        public static Quaternion Euler(in Vector3 angles) => identity;
        public static Quaternion EulerAngles(Vector3 angles) => identity;
        public static Quaternion EulerAngles(float x, float y, float z) => identity;
        public static Quaternion EulerRotation(Vector3 angles) => identity;
        public static Quaternion EulerRotation(float x, float y, float z) => identity;
        public static Quaternion AxisAngle(Vector3 axis, float angle) => identity;
        public static Quaternion AngleAxis(float angle, Vector3 axis) => identity;
        public static Quaternion AngleAxis(float angle, in Vector3 axis) => identity;
        public static Quaternion FromToRotation(Vector3 from, Vector3 to) => identity;
        public static Quaternion FromToRotation(in Vector3 from, in Vector3 to) => identity;
        public static Quaternion LookRotation(Vector3 forward, Vector3 up) => identity;
        public static Quaternion LookRotation(Vector3 forward) => identity;
        public static Quaternion LookRotation(in Vector3 forward, in Vector3 up) => identity;
        public static Quaternion LookRotation(in Vector3 forward) => identity;
        public static Quaternion Inverse(Quaternion q) => q;
        public static Quaternion Inverse(in Quaternion q) => q;
        public static Quaternion Normalize(Quaternion q) => q;
        public static Quaternion Normalize(in Quaternion q) => q;
        public static Quaternion Lerp(Quaternion a, Quaternion b, float t) => a;
        public static Quaternion Lerp(in Quaternion a, in Quaternion b, float t) => a;
        public static Quaternion LerpUnclamped(Quaternion a, Quaternion b, float t) => a;
        public static Quaternion LerpUnclamped(in Quaternion a, in Quaternion b, float t) => a;
        public static Quaternion Slerp(Quaternion a, Quaternion b, float t) => a;
        public static Quaternion Slerp(in Quaternion a, in Quaternion b, float t) => a;
        public static Quaternion SlerpUnclamped(Quaternion a, Quaternion b, float t) => a;
        public static Quaternion SlerpUnclamped(in Quaternion a, in Quaternion b, float t) => a;
        public static Quaternion RotateTowards(Quaternion a, Quaternion b, float maxDegreesDelta) => a;
        public static Quaternion RotateTowards(in Quaternion a, in Quaternion b, float maxDegreesDelta) => a;
        public static float Angle(Quaternion a, Quaternion b) => 0;
        public static float Angle(in Quaternion a, in Quaternion b) => 0;
        public static float Dot(Quaternion a, Quaternion b) => 0;
        public static float Dot(in Quaternion a, in Quaternion b) => 0;
        public bool Equals(Quaternion other) => false;
        public bool Equals(in Quaternion other) => false;
        public static bool operator ==(Quaternion a, Quaternion b) => false;
        public static bool operator !=(Quaternion a, Quaternion b) => false;
        public override bool Equals(object? other) => false;
        public override int GetHashCode() => 0;
        public override string ToString() => "";
        public string ToString(string format) => "";
        public string ToString(string format, System.IFormatProvider provider) => "";
        public static Quaternion operator *(Quaternion a, Quaternion b) => a;
        public static Vector3 operator *(Quaternion a, Vector3 b) => b;
    }
    public struct Pose
    {
        public Vector3 position;
        public Quaternion rotation;
        public Pose(Vector3 position, Quaternion rotation)
        { this.position = position; this.rotation = rotation; }
        public static Pose identity => new(Vector3.zero, Quaternion.identity);
        public Vector3 forward => Vector3.forward;
        public Vector3 right => Vector3.right;
        public Vector3 up => Vector3.up;
        public bool Equals(Pose other) => false;
        public override bool Equals(object? other) => false;
        public override int GetHashCode() => 0;
        public Pose GetTransformedBy(Pose pose) => this;
        public Pose GetTransformedBy(Transform transform) => this;
        public override string ToString() => "";
        public string ToString(string format) => "";
        public static bool operator ==(Pose a, Pose b) => false;
        public static bool operator !=(Pose a, Pose b) => false;
    }
    public struct Ray
    {
        public Vector3 origin { get; set; } public Vector3 direction { get; set; }
        public Ray(Vector3 origin, Vector3 direction) { this.origin = origin; this.direction = direction; }
        public Ray(in Vector3 origin, in Vector3 direction) { this.origin = origin; this.direction = direction; }
        public Vector3 GetPoint(float distance) => origin;
        public override string ToString() => "";
        public string ToString(string format) => "";
        public string ToString(string format, System.IFormatProvider provider) => "";
    }
    public struct Ray2D
    {
        public Vector2 origin { get; set; } public Vector2 direction { get; set; }
        public Ray2D(Vector2 origin, Vector2 direction) { this.origin = origin; this.direction = direction; }
        public Ray2D(in Vector2 origin, in Vector2 direction) { this.origin = origin; this.direction = direction; }
        public Vector2 GetPoint(float distance) => origin;
        public override string ToString() => "";
        public string ToString(string format) => "";
        public string ToString(string format, System.IFormatProvider provider) => "";
    }
    public struct Plane
    {
        public Vector3 normal { get; set; }
        public float distance { get; set; }
        public Plane flipped => this;
        public Plane(Vector3 normal, float distance) { this.normal = normal; this.distance = distance; }
        public Plane(Vector3 normal, Vector3 point) { this.normal = normal; distance = 0; }
        public Plane(Vector3 a, Vector3 b, Vector3 c) { normal = default; distance = 0; }
        public Plane(in Vector3 normal, float distance) { this.normal = normal; this.distance = distance; }
        public Plane(in Vector3 normal, in Vector3 point) { this.normal = normal; distance = 0; }
        public Plane(in Vector3 a, in Vector3 b, in Vector3 c) { normal = default; distance = 0; }
        public void Flip() { }
        public void Set3Points(Vector3 a, Vector3 b, Vector3 c) { }
        public void Set3Points(in Vector3 a, in Vector3 b, in Vector3 c) { }
        public void SetNormalAndPosition(Vector3 normal, Vector3 point) { }
        public void SetNormalAndPosition(in Vector3 normal, in Vector3 point) { }
        public float GetDistanceToPoint(Vector3 point) => 0;
        public float GetDistanceToPoint(in Vector3 point) => 0;
        public Vector3 ClosestPointOnPlane(Vector3 point) => point;
        public Vector3 ClosestPointOnPlane(in Vector3 point) => point;
        public bool GetSide(Vector3 point) => false;
        public bool GetSide(in Vector3 point) => false;
        public bool SameSide(Vector3 a, Vector3 b) => false;
        public bool SameSide(in Vector3 a, in Vector3 b) => false;
        public bool Raycast(Ray ray, out float enter) { enter = 0; return false; }
        public bool Raycast(in Ray ray, out float enter) { enter = 0; return false; }
        public void Translate(Vector3 translation) { }
        public void Translate(in Vector3 translation) { }
        public static Plane Translate(Plane plane, Vector3 translation) => plane;
        public static Plane Translate(in Plane plane, in Vector3 translation) => plane;
        public bool Equals(Plane other) => false;
        public bool Equals(in Plane other) => false;
        public override bool Equals(object other) => false;
        public override int GetHashCode() => 0;
        public static bool operator ==(Plane left, Plane right) => false;
        public static bool operator !=(Plane left, Plane right) => false;
        public override string ToString() => "";
        public string ToString(string format) => "";
        public string ToString(string format, System.IFormatProvider provider) => "";
    }
    public struct Bounds
    {
        public Vector3 center { get; set; }
        public Vector3 size { get; set; }
        public Vector3 extents { get; set; }
        public Vector3 min { get; set; }
        public Vector3 max { get; set; }
        public Bounds(Vector3 center, Vector3 size) { this.center = center; this.size = size; extents = min = max = default; }
        public Bounds(in Vector3 center, in Vector3 size) { this.center = center; this.size = size; extents = min = max = default; }
        public Vector3 ClosestPoint(Vector3 point) => point;
        public Vector3 ClosestPoint(in Vector3 point) => point;
        public bool Contains(Vector3 point) => false;
        public bool Contains(in Vector3 point) => false;
        public void Encapsulate(Vector3 point) { }
        public void Encapsulate(Bounds bounds) { }
        public void Encapsulate(in Vector3 point) { }
        public void Encapsulate(in Bounds bounds) { }
        public void Expand(float amount) { }
        public void Expand(Vector3 amount) { }
        public void Expand(in Vector3 amount) { }
        public bool Intersects(Bounds other) => false;
        public bool Intersects(in Bounds other) => false;
        public bool IntersectRay(Ray ray) => false;
        public bool IntersectRay(Ray ray, out float distance) { distance = 0; return false; }
        public bool IntersectRay(in Ray ray) => false;
        public bool IntersectRay(in Ray ray, out float distance) { distance = 0; return false; }
        public void SetMinMax(Vector3 min, Vector3 max) { }
        public void SetMinMax(in Vector3 min, in Vector3 max) { }
        public float SqrDistance(Vector3 point) => 0;
        public float SqrDistance(in Vector3 point) => 0;
        public bool Equals(Bounds other) => false;
        public bool Equals(in Bounds other) => false;
        public override bool Equals(object other) => false;
        public override int GetHashCode() => 0;
        public static bool operator ==(Bounds left, Bounds right) => false;
        public static bool operator !=(Bounds left, Bounds right) => false;
        public override string ToString() => "";
        public string ToString(string format) => "";
        public string ToString(string format, System.IFormatProvider provider) => "";
    }
    public struct FrustumPlanes
    {
        public float left, right, bottom, top, zNear, zFar;
    }
    public class GeometryUtility
    {
        public static Bounds CalculateBounds(Vector3[] positions, Matrix4x4 transform) => default;
        public static Bounds CalculateBounds(Vector3[] positions, in Matrix4x4 transform) => default;
        public static Plane[] CalculateFrustumPlanes(Camera camera) => default!;
        public static void CalculateFrustumPlanes(Camera camera, Plane[] planes) { }
        public static void CalculateFrustumPlanes(Camera camera, System.Span<Plane> planes) { }
        public static Plane[] CalculateFrustumPlanes(Matrix4x4 matrix) => default!;
        public static Plane[] CalculateFrustumPlanes(in Matrix4x4 matrix) => default!;
        public static void CalculateFrustumPlanes(Matrix4x4 matrix, Plane[] planes) { }
        public static void CalculateFrustumPlanes(in Matrix4x4 matrix, Plane[] planes) { }
        public static void CalculateFrustumPlanes(Matrix4x4 matrix, System.Span<Plane> planes) { }
        public static void CalculateFrustumPlanes(in Matrix4x4 matrix, System.Span<Plane> planes) { }
        public static bool TestPlanesAABB(Plane[] planes, Bounds bounds) => false;
        public static bool TestPlanesAABB(Plane[] planes, in Bounds bounds) => false;
        public static bool TestPlanesAABB(System.ReadOnlySpan<Plane> planes, in Bounds bounds) => false;
        public static bool TryCreatePlaneFromPolygon(Vector3[] vertices, out Plane plane)
        { plane = default; return false; }
    }
    public struct Matrix4x4
    {
        public float m00, m01, m02, m03, m10, m11, m12, m13;
        public float m20, m21, m22, m23, m30, m31, m32, m33;
        public Matrix4x4(Vector4 column0, Vector4 column1, Vector4 column2, Vector4 column3) { this = default; }
        public Matrix4x4(in Vector4 column0, in Vector4 column1, in Vector4 column2, in Vector4 column3) { this = default; }
        public static Matrix4x4 identity => default;
        public static Matrix4x4 zero => default;
        public Matrix4x4 inverse => this;
        public Matrix4x4 transpose => this;
        public float determinant => 0;
        public FrustumPlanes decomposeProjection => default;
        public bool isIdentity => false;
        public Vector3 lossyScale => default;
        public Quaternion rotation => default;
        public float this[int row, int column] { get => 0; set { } }
        public float this[int index] { get => 0; set { } }
        public Vector4 GetColumn(int index) => default;
        public Vector4 GetRow(int index) => default;
        public void SetColumn(int index, Vector4 value) { }
        public void SetColumn(int index, in Vector4 value) { }
        public void SetRow(int index, Vector4 value) { }
        public void SetRow(int index, in Vector4 value) { }
        public Vector3 GetPosition() => default;
        public void SetTRS(Vector3 position, Quaternion rotation, Vector3 scale) { }
        public void SetTRS(in Vector3 position, in Quaternion rotation, in Vector3 scale) { }
        public Vector3 MultiplyPoint(Vector3 point) => point;
        public Vector3 MultiplyPoint(in Vector3 point) => point;
        public Vector3 MultiplyPoint3x4(Vector3 point) => point;
        public Vector3 MultiplyPoint3x4(in Vector3 point) => point;
        public Vector3 MultiplyVector(Vector3 vector) => vector;
        public Vector3 MultiplyVector(in Vector3 vector) => vector;
        public Plane TransformPlane(Plane plane) => plane;
        public Plane TransformPlane(in Plane plane) => plane;
        public bool ValidTRS() => false;
        public static Matrix4x4 TRS(Vector3 position, Quaternion rotation, Vector3 scale) => default;
        public static Matrix4x4 TRS(in Vector3 position, in Quaternion rotation, in Vector3 scale) => default;
        public static Matrix4x4 LookAt(Vector3 from, Vector3 to, Vector3 up) => default;
        public static Matrix4x4 LookAt(in Vector3 from, in Vector3 to, in Vector3 up) => default;
        public static bool Inverse3DAffine(Matrix4x4 input, ref Matrix4x4 result) => false;
        public static bool Inverse3DAffine(in Matrix4x4 input, ref Matrix4x4 result) => false;
        public static Matrix4x4 Inverse(Matrix4x4 matrix) => default;
        public static Matrix4x4 Inverse(in Matrix4x4 matrix) => default;
        public static Matrix4x4 Transpose(Matrix4x4 matrix) => default;
        public static Matrix4x4 Transpose(in Matrix4x4 matrix) => default;
        public static float Determinant(Matrix4x4 matrix) => default;
        public static float Determinant(in Matrix4x4 matrix) => default;
        public static Matrix4x4 Translate(Vector3 position) => default;
        public static Matrix4x4 Translate(in Vector3 position) => default;
        public static Matrix4x4 Rotate(Quaternion rotation) => default;
        public static Matrix4x4 Rotate(in Quaternion rotation) => default;
        public static Matrix4x4 Scale(Vector3 scale) => default;
        public static Matrix4x4 Scale(in Vector3 scale) => default;
        public static Matrix4x4 Ortho(float left, float right, float bottom, float top, float near, float far) => default;
        public static Matrix4x4 Perspective(float fov, float aspect, float near, float far) => default;
        public static Matrix4x4 Frustum(float left, float right, float bottom, float top, float near, float far) => default;
        public static Matrix4x4 Frustum(FrustumPlanes planes) => default;
        public static Matrix4x4 Frustum(in FrustumPlanes planes) => default;
        public bool Equals(Matrix4x4 other) => false;
        public bool Equals(in Matrix4x4 other) => false;
        public override bool Equals(object? other) => false;
        public override int GetHashCode() => 0;
        public override string ToString() => "";
        public string ToString(string format) => "";
        public string ToString(string format, System.IFormatProvider provider) => "";
        public static bool operator ==(Matrix4x4 a, Matrix4x4 b) => false;
        public static bool operator !=(Matrix4x4 a, Matrix4x4 b) => false;
        public static Matrix4x4 operator *(Matrix4x4 a, Matrix4x4 b) => a;
        public static Vector4 operator *(Matrix4x4 a, Vector4 b) => b;
    }
    public enum QueryTriggerInteraction { UseGlobal = 0, Ignore = 1, Collide = 2 }
    public struct RaycastHit
    {
        public Collider collider { get; }
        public Transform transform { get; }
        public Vector3 point { get; }
        public Vector3 normal { get; }
        public float distance { get; }
        public Rigidbody rigidbody { get; }
    }
    public class Rigidbody : Component
    {
        public bool isKinematic { get; set; }
        public Vector3 linearVelocity { get; set; }
        public Vector3 angularVelocity { get; set; }
    }
    public struct SoftJointLimit
    {
        public float limit, bounciness, contactDistance;
    }
    public struct SoftJointLimitSpring
    {
        public float spring, damper;
    }
    public class Joint : Component
    {
        public Rigidbody connectedBody { get; set; } = null!;
        public Vector3 anchor { get; set; }
        public Vector3 axis { get; set; }
        public bool autoConfigureConnectedAnchor { get; set; }
        public Vector3 connectedAnchor { get; set; }
        public bool enableCollision { get; set; }
        public float breakForce { get; set; }
        public float breakTorque { get; set; }
    }
    public class CharacterJoint : Joint
    {
        public Vector3 swingAxis { get; set; }
        public SoftJointLimit lowTwistLimit { get; set; }
        public SoftJointLimit highTwistLimit { get; set; }
        public SoftJointLimit swing1Limit { get; set; }
        public SoftJointLimit swing2Limit { get; set; }
        public SoftJointLimitSpring twistLimitSpring { get; set; }
        public SoftJointLimitSpring swingLimitSpring { get; set; }
    }
    public static class Physics
    {
        public const int IgnoreRaycastLayer = 4;
        public const int DefaultRaycastLayers = ~IgnoreRaycastLayer;
        public const int AllLayers = ~0;
        public static bool queriesHitTriggers { get; set; } = true;
        public static bool Raycast(Vector3 origin, Vector3 direction, out RaycastHit hitInfo,
            float maxDistance = float.PositiveInfinity, int layerMask = DefaultRaycastLayers,
            QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
        { hitInfo = default; return false; }
        public static bool Raycast(Vector3 origin, Vector3 direction,
            float maxDistance = float.PositiveInfinity, int layerMask = DefaultRaycastLayers,
            QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal) => false;
        public static bool Raycast(Ray ray, out RaycastHit hitInfo,
            float maxDistance = float.PositiveInfinity, int layerMask = DefaultRaycastLayers,
            QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
        { hitInfo = default; return false; }
        public static bool Raycast(Ray ray, float maxDistance = float.PositiveInfinity,
            int layerMask = DefaultRaycastLayers,
            QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal) => false;
        public static RaycastHit[] RaycastAll(Vector3 origin, Vector3 direction,
            float maxDistance = float.PositiveInfinity, int layerMask = DefaultRaycastLayers,
            QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal) => [];
        public static RaycastHit[] RaycastAll(Ray ray, float maxDistance = float.PositiveInfinity,
            int layerMask = DefaultRaycastLayers,
            QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal) => [];
        public static int RaycastNonAlloc(Vector3 origin, Vector3 direction, RaycastHit[] results,
            float maxDistance = float.PositiveInfinity, int layerMask = DefaultRaycastLayers,
            QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal) => 0;
        public static int RaycastNonAlloc(Ray ray, RaycastHit[] results,
            float maxDistance = float.PositiveInfinity, int layerMask = DefaultRaycastLayers,
            QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal) => 0;
        public static bool Linecast(Vector3 start, Vector3 end, out RaycastHit hitInfo,
            int layerMask = DefaultRaycastLayers,
            QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
        { hitInfo = default; return false; }
        public static bool Linecast(Vector3 start, Vector3 end,
            int layerMask = DefaultRaycastLayers,
            QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal) => false;
    }
    public partial struct Color
    {
        public float r, g, b, a;
        public Color gamma => default;
        public Color linear => default;
        public float grayscale => 0;
        public float maxColorComponent => 0;
        public float this[int index] { get => 0; set { } }
        public Color(float r, float g, float b) { this.r = r; this.g = g; this.b = b; this.a = 1; }
        public Color(float r, float g, float b, float a = 1) { this.r = r; this.g = g; this.b = b; this.a = a; }
        public bool Equals(Color other) => false;
        public bool Equals(in Color other) => false;
        public static Color Lerp(Color a, Color b, float t) => default;
        public static Color Lerp(in Color a, in Color b, float t) => default;
        public static Color LerpUnclamped(Color a, Color b, float t) => default;
        public static Color LerpUnclamped(in Color a, in Color b, float t) => default;
        public static Color HSVToRGB(float h, float s, float v) => default;
        public static Color HSVToRGB(float h, float s, float v, bool hdr) => default;
        public static void RGBToHSV(Color color, out float h, out float s, out float v)
        { h = 0; s = 0; v = 0; }
        public static void RGBToHSV(in Color color, out float h, out float s, out float v)
        { h = 0; s = 0; v = 0; }
        public static implicit operator Color(Vector4 value) => default;
        public static implicit operator Vector4(Color value) => default;
        public static Color operator +(Color a, Color b) => a;
        public static Color operator -(Color a, Color b) => a;
        public static Color operator *(Color a, Color b) => a;
        public static Color operator *(Color a, Vector4 b) => a;
        public static Color operator *(Color a, float b) => a;
        public static Color operator *(float a, Color b) => b;
        public static Color operator /(Color a, float b) => a;
        public static bool operator ==(Color a, Color b) => false;
        public static bool operator !=(Color a, Color b) => false;
        public override bool Equals(object? other) => false;
        public override int GetHashCode() => 0;
        public override string ToString() => "";
        public string ToString(string format) => "";
        public string ToString(string format, System.IFormatProvider provider) => "";
    }
    public enum GradientMode { Blend = 0, Fixed = 1, PerceptualBlend = 2 }
    public struct GradientColorKey
    {
        public Color color;
        public float time;
        public GradientColorKey(Color color, float time) { this.color = color; this.time = time; }
        public GradientColorKey(in Color color, float time) { this.color = color; this.time = time; }
    }
    public struct GradientAlphaKey
    {
        public float alpha;
        public float time;
        public GradientAlphaKey(float alpha, float time) { this.alpha = alpha; this.time = time; }
    }
    public class Gradient
    {
        public GradientMode mode { get; set; }
        public ColorSpace colorSpace { get; set; }
        public GradientColorKey[] colorKeys { get; set; } = Array.Empty<GradientColorKey>();
        public GradientAlphaKey[] alphaKeys { get; set; } = Array.Empty<GradientAlphaKey>();
        public int colorKeyCount => colorKeys.Length;
        public int alphaKeyCount => alphaKeys.Length;
        public void SetKeys(GradientColorKey[] colorKeys, GradientAlphaKey[] alphaKeys) { }
        public void SetKeys(System.ReadOnlySpan<GradientColorKey> colorKeys, System.ReadOnlySpan<GradientAlphaKey> alphaKeys) { }
        public void SetColorKeys(System.ReadOnlySpan<GradientColorKey> colorKeys) { }
        public void SetAlphaKeys(System.ReadOnlySpan<GradientAlphaKey> alphaKeys) { }
        public void GetColorKeys(System.Span<GradientColorKey> destination) { }
        public void GetAlphaKeys(System.Span<GradientAlphaKey> destination) { }
        public Color Evaluate(float time) => default;
    }
    public static class ColorUtility
    {
        public static string ToHtmlStringRGB(Color color) => "";
        public static string ToHtmlStringRGB(in Color color) => "";
        public static string ToHtmlStringRGBA(Color color) => "";
        public static string ToHtmlStringRGBA(in Color color) => "";
        public static bool TryParseHtmlString(string htmlString, out Color color)
        { color = default; return false; }
    }
    public struct Color32
    {
        public byte r, g, b, a;
        public Color32(byte r, byte g, byte b, byte a)
        { this.r = r; this.g = g; this.b = b; this.a = a; }
        public byte this[int index] { get => 0; set { } }
        public static implicit operator Color(Color32 value) => default;
        public static implicit operator Color32(Color value) => default;
        public static Color32 Lerp(Color32 a, Color32 b, float t) => default;
        public static Color32 Lerp(in Color32 a, in Color32 b, float t) => default;
        public static Color32 LerpUnclamped(Color32 a, Color32 b, float t) => default;
        public static Color32 LerpUnclamped(in Color32 a, in Color32 b, float t) => default;
        public bool Equals(in Color32 other) => false;
        public string ToString(string format) => "";
        public string ToString(string format, System.IFormatProvider provider) => "";
    }
    public struct Rect
    {
        public float x, y, width, height;
        public Rect(float x, float y, float width, float height)
        { this.x = x; this.y = y; this.width = width; this.height = height; }
        public Rect(Vector2 position, Vector2 size) : this(position.x, position.y, size.x, size.y) { }
        public Rect(Rect source) : this(source.x, source.y, source.width, source.height) { }
        public Rect(in Rect source) : this(source.x, source.y, source.width, source.height) { }
        public Rect(in Vector2 position, in Vector2 size) : this(position.x, position.y, size.x, size.y) { }
        public static Rect zero => default;
        public float xMin { get => x; set { } }
        public float xMax { get => x + width; set { } }
        public float yMin { get => y; set { } }
        public float yMax { get => y + height; set { } }
        public float left => xMin;
        public float right => xMax;
        public float top => yMin;
        public float bottom => yMax;
        public Vector2 position { get => default; set { } }
        public Vector2 size { get => default; set { } }
        public Vector2 center { get => default; set { } }
        public Vector2 min { get => default; set { } }
        public Vector2 max { get => default; set { } }
        public void Set(float x, float y, float width, float height) { }
        public bool Contains(Vector2 point) => false;
        public bool Contains(Vector3 point) => false;
        public bool Contains(Vector3 point, bool allowInverse) => false;
        public bool Contains(in Vector2 point) => false;
        public bool Contains(in Vector3 point) => false;
        public bool Contains(in Vector3 point, bool allowInverse) => false;
        public bool Overlaps(Rect other) => false;
        public bool Overlaps(Rect other, bool allowInverse) => false;
        public bool Overlaps(in Rect other) => false;
        public bool Overlaps(in Rect other, bool allowInverse) => false;
        public static Rect MinMaxRect(float xMin, float yMin, float xMax, float yMax) => default;
        public static Vector2 NormalizedToPoint(Rect rect, Vector2 normalized) => default;
        public static Vector2 NormalizedToPoint(in Rect rect, in Vector2 normalized) => default;
        public static Vector2 PointToNormalized(Rect rect, Vector2 point) => default;
        public static Vector2 PointToNormalized(in Rect rect, in Vector2 point) => default;
        public bool Equals(Rect other) => false;
        public bool Equals(in Rect other) => false;
        public static bool operator ==(Rect left, Rect right) => false;
        public static bool operator !=(Rect left, Rect right) => false;
        public override bool Equals(object other) => false;
        public override int GetHashCode() => 0;
        public override string ToString() => "";
        public string ToString(string format) => "";
        public string ToString(string format, System.IFormatProvider provider) => "";
    }
    public enum EventType { MouseDown = 0, MouseUp = 1, MouseMove = 2, MouseDrag = 3,
        KeyDown = 4, KeyUp = 5, ScrollWheel = 6, Repaint = 7, Layout = 8,
        DragUpdated = 9, DragPerform = 10, Ignore = 11, Used = 12,
        ValidateCommand = 13, ExecuteCommand = 14, DragExited = 15,
        ContextClick = 16, MouseEnterWindow = 20, MouseLeaveWindow = 21,
        TouchDown = 30, TouchUp = 31, TouchMove = 32, TouchEnter = 33,
        TouchLeave = 34, TouchStationary = 35,
        mouseDown = MouseDown, mouseUp = MouseUp, mouseMove = MouseMove,
        mouseDrag = MouseDrag, keyDown = KeyDown, keyUp = KeyUp,
        scrollWheel = ScrollWheel, repaint = Repaint, layout = Layout,
        dragUpdated = DragUpdated, dragPerform = DragPerform,
        ignore = Ignore, used = Used }
    [System.Flags]
    public enum EventModifiers { None = 0, Shift = 1, Control = 2, Alt = 4,
        Command = 8, Numeric = 16, CapsLock = 32, FunctionKey = 64 }
    public enum PointerType { Mouse = 0, Touch = 1, Pen = 2 }
    public enum FocusType { Native = 0, Keyboard = 1, Passive = 2 }
    public class Event
    {
        public Event() { }
        public Event(int displayIndex) { }
        public Event(Event other) { }
        public static Event current { get; }
        public EventType type { get; set; }
        public EventType rawType { get; }
        public EventModifiers modifiers { get; set; }
        public bool shift { get; }
        public bool control { get; }
        public bool alt { get; }
        public bool command { get; }
        public bool numeric { get; }
        public bool capsLock { get; }
        public bool functionKey { get; }
        public bool isKey { get; }
        public bool isMouse { get; }
        public bool isScrollWheel { get; }
        public Vector2 mousePosition { get; set; }
        public int button { get; set; }
        public int clickCount { get; set; }
        public KeyCode keyCode { get; set; }
        public char character { get; set; }
        public Vector2 delta { get; set; }
        public string commandName { get; set; }
        public int displayIndex { get; set; }
        public PointerType pointerType { get; set; }
        public float pressure { get; set; }
        public Vector2 tilt { get; set; }
        public float twist { get; set; }
        public PenStatus penStatus { get; set; }
        public Ray mouseRay { get; set; }
        public EventType GetTypeForControl(int controlID) => type;
        public static int GetEventCount() => 0;
        public static Event KeyboardEvent(string key) => new();
        public static bool PopEvent(Event outEvent) => false;
        public void Use() { }
        public override bool Equals(object other) => false;
        public override int GetHashCode() => 0;
        public override string ToString() => "";
    }
    public class GUIUtility
    {
        public GUIUtility() { }
        public static int hotControl { get; set; }
        public static int keyboardControl { get; set; }
        public static bool hasModalWindow { get; }
        public static int GetControlID(FocusType focus) => 0;
        public static int GetControlID(FocusType focus, Rect position) => 0;
        public static int GetControlID(GUIContent content, FocusType focus) => 0;
        public static int GetControlID(GUIContent content, FocusType focus, Rect position) => 0;
        public static int GetControlID(int hint, FocusType focus) => 0;
        public static int GetControlID(int hint, FocusType focus, Rect position) => 0;
        public static object GetStateObject(System.Type type, int controlID) => null;
        public static object QueryStateObject(System.Type type, int controlID) => null;
        public static Vector2 GUIToScreenPoint(Vector2 point) => point;
        public static Rect GUIToScreenRect(Rect rect) => rect;
        public static Vector2 ScreenToGUIPoint(Vector2 point) => point;
        public static Rect ScreenToGUIRect(Rect rect) => rect;
        public static Rect AlignRectToDevice(Rect rect) => rect;
        public static Rect AlignRectToDevice(Rect rect, out int widthInPixels,
            out int heightInPixels) { widthInPixels = 0; heightInPixels = 0; return rect; }
        public static void RotateAroundPivot(float angle, Vector2 pivotPoint) { }
        public static void ScaleAroundPivot(Vector2 scale, Vector2 pivotPoint) { }
        public static string systemCopyBuffer { get; set; }
        public static void ExitGUI() { }
    }
    public sealed class ExitGUIException : System.Exception { }
    public class GUIElement : Behaviour
    {
        public Rect GetScreenRect() => default;
        public Rect GetScreenRect(Camera camera) => default;
        public bool HitTest(Vector3 screenPosition) => false;
        public bool HitTest(Vector3 screenPosition, Camera camera) => false;
    }
    public class GUILayer : Behaviour
    {
        public GUIElement HitTest(Vector3 screenPosition) => null;
    }
    public class GUITexture : GUIElement
    {
        public RectOffset border { get; set; }
        public Color color { get; set; }
        public Rect pixelInset { get; set; }
        public Texture texture { get; set; }
    }
    [System.AttributeUsage(System.AttributeTargets.Method)]
    public sealed class GUITargetAttribute : System.Attribute
    {
        public GUITargetAttribute() { }
        public GUITargetAttribute(int displayIndex) { }
        public GUITargetAttribute(int displayIndex, int secondDisplayIndex) { }
        public GUITargetAttribute(int displayIndex, int secondDisplayIndex,
            params int[] remainingDisplayIndices) { }
    }
    public enum ScaleMode { StretchToFill = 0, ScaleAndCrop = 1, ScaleToFit = 2 }
    public class GUIContent
    {
        public GUIContent() { }
        public GUIContent(GUIContent other) { }
        public GUIContent(string text) { }
        public GUIContent(string text, string tooltip) { }
        public GUIContent(string text, Texture image) { }
        public GUIContent(string text, Texture image, string tooltip) { }
        public GUIContent(Texture image) { }
        public GUIContent(Texture image, string tooltip) { }
        public string text { get; set; } = "";
        public Texture image { get; set; }
        public string tooltip { get; set; } = "";
        public static GUIContent none => new();
        public override string ToString() => text;
    }
    public class GUI
    {
        public GUI() { }
        public enum ToolbarButtonSize { Fixed = 0, FitToContents = 1 }
        public delegate void WindowFunction(int id);
        public class Scope : System.IDisposable { public virtual void Dispose() { } }
        public class GroupScope : Scope
        {
            public GroupScope(Rect position) { }
            public GroupScope(Rect position, string text) { }
            public GroupScope(Rect position, string text, GUIStyle style) { }
            public GroupScope(Rect position, GUIContent content) { }
            public GroupScope(Rect position, GUIContent content, GUIStyle style) { }
            public GroupScope(Rect position, Texture image) { }
            public GroupScope(Rect position, Texture image, GUIStyle style) { }
            public GroupScope(Rect position, GUIStyle style) { }
        }
        public class ClipScope : Scope { public ClipScope(Rect position) { } }
        public class ScrollViewScope : Scope
        {
            public ScrollViewScope(Rect position, Vector2 scrollPosition, Rect viewRect) { }
            public ScrollViewScope(Rect position, Vector2 scrollPosition, Rect viewRect,
                GUIStyle horizontalScrollbar, GUIStyle verticalScrollbar) { }
            public ScrollViewScope(Rect position, Vector2 scrollPosition, Rect viewRect,
                bool alwaysShowHorizontal, bool alwaysShowVertical) { }
            public ScrollViewScope(Rect position, Vector2 scrollPosition, Rect viewRect,
                bool alwaysShowHorizontal, bool alwaysShowVertical,
                GUIStyle horizontalScrollbar, GUIStyle verticalScrollbar) { }
            public Vector2 scrollPosition { get; set; }
            public bool handleScrollWheel { get; set; } = true;
        }
        public static bool enabled { get; set; } = true;
        public static bool changed { get; set; }
        public static int depth { get; set; }
        public static string tooltip { get; set; } = "";
        public static Color color { get; set; }
        public static Color backgroundColor { get; set; }
        public static Color contentColor { get; set; }
        public static GUISkin skin { get; set; } = new();
        public static Matrix4x4 matrix { get; set; }
        public static void SetNextControlName(string name) { }
        public static string GetNameOfFocusedControl() => "";
        public static void FocusControl(string name) { }
        public static void Label(Rect position, string text) { }
        public static void Label(Rect position, string text, GUIStyle style) { }
        public static void Label(Rect position, GUIContent content) { }
        public static void Label(Rect position, GUIContent content, GUIStyle style) { }
        public static void Label(Rect position, Texture image) { }
        public static void Label(Rect position, Texture image, GUIStyle style) { }
        public static void Box(Rect position, string text) { }
        public static void Box(Rect position, string text, GUIStyle style) { }
        public static void Box(Rect position, GUIContent content) { }
        public static void Box(Rect position, GUIContent content, GUIStyle style) { }
        public static void Box(Rect position, Texture image) { }
        public static void Box(Rect position, Texture image, GUIStyle style) { }
        public static void DrawTexture(Rect position, Texture texture) { }
        public static void DrawTexture(Rect position, Texture texture, ScaleMode scaleMode) { }
        public static void DrawTexture(Rect position, Texture texture, ScaleMode scaleMode, bool alphaBlend) { }
        public static void DrawTexture(Rect position, Texture texture, ScaleMode scaleMode, bool alphaBlend, float imageAspect) { }
        public static void DrawTexture(Rect position, Texture texture, ScaleMode scaleMode, bool alphaBlend, float imageAspect, Color color, float borderWidth, float borderRadius) { }
        public static void DrawTexture(Rect position, Texture texture, ScaleMode scaleMode, bool alphaBlend, float imageAspect, Color color, Vector4 borderWidths, float borderRadius) { }
        public static void DrawTexture(Rect position, Texture texture, ScaleMode scaleMode, bool alphaBlend, float imageAspect, Color color, Vector4 borderWidths, Vector4 borderRadiuses) { }
        public static void DrawTextureWithTexCoords(Rect position, Texture texture, Rect texCoords) { }
        public static void DrawTextureWithTexCoords(Rect position, Texture texture, Rect texCoords, bool alphaBlend) { }
        public static bool Button(Rect position, string text) => false;
        public static bool Button(Rect position, string text, GUIStyle style) => false;
        public static bool Button(Rect position, GUIContent content) => false;
        public static bool Button(Rect position, GUIContent content, GUIStyle style) => false;
        public static bool Button(Rect position, Texture image) => false;
        public static bool Button(Rect position, Texture image, GUIStyle style) => false;
        public static bool RepeatButton(Rect position, string text) => false;
        public static bool RepeatButton(Rect position, string text, GUIStyle style) => false;
        public static bool RepeatButton(Rect position, GUIContent content) => false;
        public static bool RepeatButton(Rect position, GUIContent content, GUIStyle style) => false;
        public static bool RepeatButton(Rect position, Texture image) => false;
        public static bool RepeatButton(Rect position, Texture image, GUIStyle style) => false;
        public static int Toolbar(Rect position, int selected, string[] texts) => selected;
        public static int Toolbar(Rect position, int selected, string[] texts, GUIStyle style) => selected;
        public static int Toolbar(Rect position, int selected, GUIContent[] contents) => selected;
        public static int Toolbar(Rect position, int selected, GUIContent[] contents, GUIStyle style) => selected;
        public static int Toolbar(Rect position, int selected, GUIContent[] contents, GUIStyle style, ToolbarButtonSize buttonSize) => selected;
        public static int Toolbar(Rect position, int selected, Texture[] images) => selected;
        public static int Toolbar(Rect position, int selected, Texture[] images, GUIStyle style) => selected;
        public static int SelectionGrid(Rect position, int selected, string[] texts, int xCount) => selected;
        public static int SelectionGrid(Rect position, int selected, string[] texts, int xCount, GUIStyle style) => selected;
        public static int SelectionGrid(Rect position, int selected, GUIContent[] contents, int xCount) => selected;
        public static int SelectionGrid(Rect position, int selected, GUIContent[] contents, int xCount, GUIStyle style) => selected;
        public static int SelectionGrid(Rect position, int selected, Texture[] images, int xCount) => selected;
        public static int SelectionGrid(Rect position, int selected, Texture[] images, int xCount, GUIStyle style) => selected;
        public static bool Toggle(Rect position, bool value, string text) => value;
        public static bool Toggle(Rect position, bool value, string text, string style) => value;
        public static bool Toggle(Rect position, bool value, string text, GUIStyle style) => value;
        public static bool Toggle(Rect position, bool value, GUIContent content) => value;
        public static bool Toggle(Rect position, bool value, GUIContent content, GUIStyle style) => value;
        public static bool Toggle(Rect position, bool value, Texture image) => value;
        public static bool Toggle(Rect position, bool value, Texture image, GUIStyle style) => value;
        public static bool Toggle(Rect position, int id, bool value, GUIContent content, GUIStyle style) => value;
        public static string TextField(Rect position, string text) => text;
        public static string TextField(Rect position, string text, int maxLength) => text;
        public static string TextField(Rect position, string text, GUIStyle style) => text;
        public static string TextField(Rect position, string text, int maxLength, GUIStyle style) => text;
        public static string TextArea(Rect position, string text) => text;
        public static string TextArea(Rect position, string text, int maxLength) => text;
        public static string TextArea(Rect position, string text, GUIStyle style) => text;
        public static string TextArea(Rect position, string text, int maxLength, GUIStyle style) => text;
        public static string PasswordField(Rect position, string password, char maskChar) => password;
        public static string PasswordField(Rect position, string password, char maskChar, int maxLength) => password;
        public static string PasswordField(Rect position, string password, char maskChar, GUIStyle style) => password;
        public static string PasswordField(Rect position, string password, char maskChar, int maxLength, GUIStyle style) => password;
        public static float HorizontalSlider(Rect position, float value, float leftValue, float rightValue) => value;
        public static float HorizontalSlider(Rect position, float value, float leftValue, float rightValue, GUIStyle slider, GUIStyle thumb) => value;
        public static float HorizontalSlider(Rect position, float value, float leftValue, float rightValue, GUIStyle slider, GUIStyle thumb, GUIStyle thumbExtent) => value;
        public static float VerticalSlider(Rect position, float value, float topValue, float bottomValue) => value;
        public static float VerticalSlider(Rect position, float value, float topValue, float bottomValue, GUIStyle slider, GUIStyle thumb) => value;
        public static float VerticalSlider(Rect position, float value, float topValue, float bottomValue, GUIStyle slider, GUIStyle thumb, GUIStyle thumbExtent) => value;
        public static float HorizontalScrollbar(Rect position, float value, float size, float leftValue, float rightValue) => value;
        public static float HorizontalScrollbar(Rect position, float value, float size, float leftValue, float rightValue, GUIStyle style) => value;
        public static float VerticalScrollbar(Rect position, float value, float size, float topValue, float bottomValue) => value;
        public static float VerticalScrollbar(Rect position, float value, float size, float topValue, float bottomValue, GUIStyle style) => value;
        public static float Slider(Rect position, float value, float size, float start, float end, GUIStyle slider, GUIStyle thumb, bool horiz, int id, GUIStyle thumbExtent = null) => value;
        public static void BeginGroup(Rect position) { }
        public static void BeginGroup(Rect position, string text) { }
        public static void BeginGroup(Rect position, string text, GUIStyle style) { }
        public static void BeginGroup(Rect position, GUIContent content) { }
        public static void BeginGroup(Rect position, GUIContent content, GUIStyle style) { }
        public static void BeginGroup(Rect position, Texture image) { }
        public static void BeginGroup(Rect position, Texture image, GUIStyle style) { }
        public static void BeginGroup(Rect position, GUIStyle style) { }
        public static void EndGroup() { }
        public static void BeginClip(Rect position) { }
        public static void BeginClip(Rect position, Vector2 scrollOffset, Vector2 renderOffset, bool resetOffset) { }
        public static void EndClip() { }
        public static Vector2 BeginScrollView(Rect position, Vector2 scrollPosition, Rect viewRect) => scrollPosition;
        public static Vector2 BeginScrollView(Rect position, Vector2 scrollPosition, Rect viewRect, bool alwaysShowHorizontal, bool alwaysShowVertical) => scrollPosition;
        public static Vector2 BeginScrollView(Rect position, Vector2 scrollPosition, Rect viewRect, GUIStyle horizontalScrollbar, GUIStyle verticalScrollbar) => scrollPosition;
        public static Vector2 BeginScrollView(Rect position, Vector2 scrollPosition, Rect viewRect, bool alwaysShowHorizontal, bool alwaysShowVertical, GUIStyle horizontalScrollbar, GUIStyle verticalScrollbar) => scrollPosition;
        public static void EndScrollView() { }
        public static void EndScrollView(bool handleScrollWheel) { }
        public static void ScrollTo(Rect position) { }
        public static bool ScrollTowards(Rect position, float maxDelta) => false;
        public static Rect Window(int id, Rect position, WindowFunction callback, string title) => position;
        public static Rect Window(int id, Rect position, WindowFunction callback, GUIContent title) => position;
        public static Rect Window(int id, Rect position, WindowFunction callback, string title, GUIStyle style) => position;
        public static Rect Window(int id, Rect position, WindowFunction callback, GUIContent title, GUIStyle style) => position;
        public static Rect Window(int id, Rect position, WindowFunction callback, Texture title) => position;
        public static Rect Window(int id, Rect position, WindowFunction callback, Texture title, GUIStyle style) => position;
        public static Rect ModalWindow(int id, Rect position, WindowFunction callback, string title) => position;
        public static Rect ModalWindow(int id, Rect position, WindowFunction callback, GUIContent title) => position;
        public static Rect ModalWindow(int id, Rect position, WindowFunction callback, string title, GUIStyle style) => position;
        public static Rect ModalWindow(int id, Rect position, WindowFunction callback, GUIContent title, GUIStyle style) => position;
        public static Rect ModalWindow(int id, Rect position, WindowFunction callback, Texture title) => position;
        public static Rect ModalWindow(int id, Rect position, WindowFunction callback, Texture title, GUIStyle style) => position;
        public static void DragWindow() { }
        public static void DragWindow(Rect position) { }
        public static void BringWindowToFront(int id) { }
        public static void BringWindowToBack(int id) { }
        public static void FocusWindow(int id) { }
        public static void UnfocusWindow() { }
    }
    public class GUISkin
    {
        public GUISkin() { }
        public GUIStyle box { get; set; } = new();
        public GUIStyle label { get; set; } = new();
        public GUIStyle button { get; set; } = new();
        public GUIStyle toggle { get; set; } = new();
        public GUIStyle textField { get; set; } = new();
        public GUIStyle textArea { get; set; } = new();
        public GUIStyle window { get; set; } = new();
        public GUIStyle horizontalSlider { get; set; } = new();
        public GUIStyle verticalSlider { get; set; } = new();
        public GUIStyle horizontalSliderThumb { get; set; } = new();
        public GUIStyle verticalSliderThumb { get; set; } = new();
        public GUIStyle horizontalScrollbar { get; set; } = new();
        public GUIStyle verticalScrollbar { get; set; } = new();
        public GUIStyle horizontalScrollbarThumb { get; set; } = new();
        public GUIStyle verticalScrollbarThumb { get; set; } = new();
        public GUIStyle horizontalScrollbarLeftButton { get; set; } = new();
        public GUIStyle horizontalScrollbarRightButton { get; set; } = new();
        public GUIStyle verticalScrollbarUpButton { get; set; } = new();
        public GUIStyle verticalScrollbarDownButton { get; set; } = new();
        public GUIStyle scrollView { get; set; } = new();
        public GUIStyle[] customStyles { get; set; } = Array.Empty<GUIStyle>();
        public Font font { get; set; }
        public GUISettings settings { get; set; } = new();
        public GUIStyle FindStyle(string name) => null;
        public GUIStyle GetStyle(string name) => new();
        public System.Collections.IEnumerator GetEnumerator() => null;
    }
    public class GUIStyleState
    {
        public GUIStyleState() { }
        public Texture2D background { get; set; }
        public Texture2D[] scaledBackgrounds { get; set; } = Array.Empty<Texture2D>();
        public Color textColor { get; set; }
    }
    public class GUISettings
    {
        public Color cursorColor { get; set; }
        public Color selectionColor { get; set; }
        public float cursorFlashSpeed { get; set; }
        public bool doubleClickSelectsWord { get; set; }
        public bool tripleClickSelectsLine { get; set; }
    }
    public enum ImagePosition { ImageLeft, ImageAbove, ImageOnly, TextOnly }
    public enum TextClipping { Overflow, Clip, Ellipsis }
    public class GUIStyle
    {
        public GUIStyle() { }
        public GUIStyle(GUIStyle other) { }
        public static implicit operator GUIStyle(string name) => new();
        public static GUIStyle none => new();
        public TextAnchor alignment { get; set; }
        public RectOffset border { get; set; } = new();
        public Vector2 clipOffset { get; set; }
        public TextClipping clipping { get; set; }
        public Vector2 contentOffset { get; set; }
        public float fixedWidth { get; set; }
        public float fixedHeight { get; set; }
        public int fontSize { get; set; }
        public FontStyle fontStyle { get; set; }
        public Font font { get; set; }
        public ImagePosition imagePosition { get; set; }
        public bool isHeightDependantOnWidth => wordWrap && fixedHeight == 0;
        public float lineHeight => fontSize;
        public string name { get; set; }
        public RectOffset overflow { get; set; } = new();
        public bool richText { get; set; }
        public bool stretchHeight { get; set; }
        public bool stretchWidth { get; set; }
        public bool wordWrap { get; set; }
        public GUIStyleState normal { get; set; } = new();
        public GUIStyleState hover { get; set; } = new();
        public GUIStyleState active { get; set; } = new();
        public GUIStyleState focused { get; set; } = new();
        public GUIStyleState onNormal { get; set; } = new();
        public GUIStyleState onHover { get; set; } = new();
        public GUIStyleState onActive { get; set; } = new();
        public GUIStyleState onFocused { get; set; } = new();
        public RectOffset padding { get; set; } = new();
        public RectOffset margin { get; set; } = new();
        public Vector2 CalcSize(GUIContent content) => new();
        public Vector2 CalcScreenSize(Vector2 contentSize) => new();
        public float CalcHeight(GUIContent content, float width) => 0;
        public void CalcMinMaxWidth(GUIContent content, out float minWidth, out float maxWidth)
            { minWidth = maxWidth = 0; }
        public void Draw(Rect position, GUIContent content, int controlId = 0) { }
        public void Draw(Rect position, GUIContent content, bool isHover, bool isActive,
            bool on, bool hasKeyboardFocus) { }
        public void Draw(Rect position, GUIContent content, int controlId, bool on) { }
        public Vector2 GetCursorPixelPosition(Rect position, GUIContent content,
            int cursorStringIndex) => new();
        public int GetCursorStringIndex(Rect position, GUIContent content,
            Vector2 cursorPixelPosition) => 0;
        public void DrawCursor(Rect position, GUIContent content, int controlId,
            int character) { }
        public void DrawWithTextSelection(Rect position, GUIContent content,
            int controlId, int firstSelectedCharacter, int lastSelectedCharacter) { }
        public override string ToString() => name;
    }
    public sealed class GUILayoutOption { }
    public class GUILayout
    {
        public GUILayout() { }
        public class HorizontalScope : GUI.Scope
        {
            public HorizontalScope(params GUILayoutOption[] options) { }
            public HorizontalScope(GUIStyle style, params GUILayoutOption[] options) { }
            public HorizontalScope(string text, GUIStyle style, params GUILayoutOption[] options) { }
            public HorizontalScope(GUIContent content, GUIStyle style, params GUILayoutOption[] options) { }
            public HorizontalScope(Texture image, GUIStyle style, params GUILayoutOption[] options) { }
        }
        public class VerticalScope : GUI.Scope
        {
            public VerticalScope(params GUILayoutOption[] options) { }
            public VerticalScope(GUIStyle style, params GUILayoutOption[] options) { }
            public VerticalScope(string text, GUIStyle style, params GUILayoutOption[] options) { }
            public VerticalScope(GUIContent content, GUIStyle style, params GUILayoutOption[] options) { }
            public VerticalScope(Texture image, GUIStyle style, params GUILayoutOption[] options) { }
        }
        public class AreaScope : GUI.Scope
        {
            public AreaScope(Rect position) { }
            public AreaScope(Rect position, string text) { }
            public AreaScope(Rect position, string text, GUIStyle style) { }
            public AreaScope(Rect position, GUIContent content) { }
            public AreaScope(Rect position, GUIContent content, GUIStyle style) { }
            public AreaScope(Rect position, Texture image) { }
            public AreaScope(Rect position, Texture image, GUIStyle style) { }
        }
        public class ScrollViewScope : GUI.Scope
        {
            public ScrollViewScope(Vector2 scrollPosition, params GUILayoutOption[] options) { }
            public ScrollViewScope(Vector2 scrollPosition, GUIStyle style,
                params GUILayoutOption[] options) { }
            public ScrollViewScope(Vector2 scrollPosition, GUIStyle horizontalScrollbar,
                GUIStyle verticalScrollbar, params GUILayoutOption[] options) { }
            public ScrollViewScope(Vector2 scrollPosition, bool alwaysShowHorizontal,
                bool alwaysShowVertical, params GUILayoutOption[] options) { }
            public ScrollViewScope(Vector2 scrollPosition, bool alwaysShowHorizontal,
                bool alwaysShowVertical, GUIStyle horizontalScrollbar,
                GUIStyle verticalScrollbar, params GUILayoutOption[] options) { }
            public ScrollViewScope(Vector2 scrollPosition, bool alwaysShowHorizontal,
                bool alwaysShowVertical, GUIStyle horizontalScrollbar,
                GUIStyle verticalScrollbar, GUIStyle background,
                params GUILayoutOption[] options) { }
            public Vector2 scrollPosition { get; set; }
            public bool handleScrollWheel { get; set; } = true;
        }
        public static void BeginArea(Rect position) { }
        public static void BeginArea(Rect position, string text) { }
        public static void BeginArea(Rect position, string text, GUIStyle style) { }
        public static void BeginArea(Rect position, GUIContent content) { }
        public static void BeginArea(Rect position, GUIContent content, GUIStyle style) { }
        public static void BeginArea(Rect position, Texture image) { }
        public static void BeginArea(Rect position, Texture image, GUIStyle style) { }
        public static void BeginArea(Rect position, GUIStyle style) { }
        public static void EndArea() { }
        public static Vector2 BeginScrollView(Vector2 scrollPosition, params GUILayoutOption[] options) => scrollPosition;
        public static Vector2 BeginScrollView(Vector2 scrollPosition, GUIStyle style) => scrollPosition;
        public static Vector2 BeginScrollView(Vector2 scrollPosition, GUIStyle style,
            params GUILayoutOption[] options) => scrollPosition;
        public static Vector2 BeginScrollView(Vector2 scrollPosition, GUIStyle horizontalScrollbar,
            GUIStyle verticalScrollbar, params GUILayoutOption[] options) => scrollPosition;
        public static Vector2 BeginScrollView(Vector2 scrollPosition, bool alwaysShowHorizontal,
            bool alwaysShowVertical, params GUILayoutOption[] options) => scrollPosition;
        public static Vector2 BeginScrollView(Vector2 scrollPosition, bool alwaysShowHorizontal,
            bool alwaysShowVertical, GUIStyle horizontalScrollbar,
            GUIStyle verticalScrollbar, params GUILayoutOption[] options) => scrollPosition;
        public static Vector2 BeginScrollView(Vector2 scrollPosition, bool alwaysShowHorizontal,
            bool alwaysShowVertical, GUIStyle horizontalScrollbar,
            GUIStyle verticalScrollbar, GUIStyle background,
            params GUILayoutOption[] options) => scrollPosition;
        public static void EndScrollView() { }
        public static Rect Window(int id, Rect position, GUI.WindowFunction callback, string title, params GUILayoutOption[] options) => position;
        public static Rect Window(int id, Rect position, GUI.WindowFunction callback, string title, GUIStyle style, params GUILayoutOption[] options) => position;
        public static Rect Window(int id, Rect position, GUI.WindowFunction callback, GUIContent title, params GUILayoutOption[] options) => position;
        public static Rect Window(int id, Rect position, GUI.WindowFunction callback, GUIContent title, GUIStyle style, params GUILayoutOption[] options) => position;
        public static Rect Window(int id, Rect position, GUI.WindowFunction callback, Texture title, params GUILayoutOption[] options) => position;
        public static Rect Window(int id, Rect position, GUI.WindowFunction callback, Texture title, GUIStyle style, params GUILayoutOption[] options) => position;
        public static void BeginHorizontal(params GUILayoutOption[] options) { }
        public static void BeginHorizontal(GUIStyle style, params GUILayoutOption[] options) { }
        public static void BeginHorizontal(string text, GUIStyle style, params GUILayoutOption[] options) { }
        public static void BeginHorizontal(GUIContent content, GUIStyle style, params GUILayoutOption[] options) { }
        public static void BeginHorizontal(Texture image, GUIStyle style, params GUILayoutOption[] options) { }
        public static void EndHorizontal() { }
        public static void BeginVertical(params GUILayoutOption[] options) { }
        public static void BeginVertical(GUIStyle style, params GUILayoutOption[] options) { }
        public static void BeginVertical(string text, GUIStyle style, params GUILayoutOption[] options) { }
        public static void BeginVertical(GUIContent content, GUIStyle style, params GUILayoutOption[] options) { }
        public static void BeginVertical(Texture image, GUIStyle style, params GUILayoutOption[] options) { }
        public static void EndVertical() { }
        public static void Space(float pixels) { }
        public static void FlexibleSpace() { }
        public static void Label(string text, params GUILayoutOption[] options) { }
        public static void Label(string text, GUIStyle style, params GUILayoutOption[] options) { }
        public static void Label(GUIContent content, params GUILayoutOption[] options) { }
        public static void Label(GUIContent content, GUIStyle style, params GUILayoutOption[] options) { }
        public static void Label(Texture image, params GUILayoutOption[] options) { }
        public static void Label(Texture image, GUIStyle style, params GUILayoutOption[] options) { }
        public static void Box(string text, params GUILayoutOption[] options) { }
        public static void Box(string text, GUIStyle style, params GUILayoutOption[] options) { }
        public static void Box(GUIContent content, params GUILayoutOption[] options) { }
        public static void Box(GUIContent content, GUIStyle style, params GUILayoutOption[] options) { }
        public static void Box(Texture image, params GUILayoutOption[] options) { }
        public static void Box(Texture image, GUIStyle style, params GUILayoutOption[] options) { }
        public static bool Button(string text, params GUILayoutOption[] options) => false;
        public static bool Button(string text, GUIStyle style, params GUILayoutOption[] options) => false;
        public static bool Button(GUIContent content, params GUILayoutOption[] options) => false;
        public static bool Button(GUIContent content, GUIStyle style, params GUILayoutOption[] options) => false;
        public static bool Button(Texture image, params GUILayoutOption[] options) => false;
        public static bool Button(Texture image, GUIStyle style, params GUILayoutOption[] options) => false;
        public static bool RepeatButton(string text, params GUILayoutOption[] options) => false;
        public static bool RepeatButton(string text, GUIStyle style, params GUILayoutOption[] options) => false;
        public static bool RepeatButton(GUIContent content, params GUILayoutOption[] options) => false;
        public static bool RepeatButton(GUIContent content, GUIStyle style, params GUILayoutOption[] options) => false;
        public static bool RepeatButton(Texture image, params GUILayoutOption[] options) => false;
        public static bool RepeatButton(Texture image, GUIStyle style, params GUILayoutOption[] options) => false;
        public static int Toolbar(int selected, string[] texts, params GUILayoutOption[] options) => selected;
        public static int Toolbar(int selected, string[] texts, GUIStyle style, params GUILayoutOption[] options) => selected;
        public static int Toolbar(int selected, GUIContent[] contents, params GUILayoutOption[] options) => selected;
        public static int Toolbar(int selected, GUIContent[] contents, GUIStyle style, params GUILayoutOption[] options) => selected;
        public static int Toolbar(int selected, GUIContent[] contents, GUIStyle style, GUI.ToolbarButtonSize buttonSize, params GUILayoutOption[] options) => selected;
        public static int Toolbar(int selected, GUIContent[] contents, bool[] enabled, GUIStyle style, params GUILayoutOption[] options) => selected;
        public static int Toolbar(int selected, GUIContent[] contents, bool[] enabled, GUIStyle style, GUI.ToolbarButtonSize buttonSize, params GUILayoutOption[] options) => selected;
        public static int Toolbar(int selected, string[] texts, GUIStyle style, GUI.ToolbarButtonSize buttonSize, params GUILayoutOption[] options) => selected;
        public static int Toolbar(int selected, Texture[] images, GUIStyle style, GUI.ToolbarButtonSize buttonSize, params GUILayoutOption[] options) => selected;
        public static int Toolbar(int selected, Texture[] images, params GUILayoutOption[] options) => selected;
        public static int Toolbar(int selected, Texture[] images, GUIStyle style, params GUILayoutOption[] options) => selected;
        public static int SelectionGrid(int selected, string[] texts, int xCount, params GUILayoutOption[] options) => selected;
        public static int SelectionGrid(int selected, string[] texts, int xCount, GUIStyle style, params GUILayoutOption[] options) => selected;
        public static int SelectionGrid(int selected, GUIContent[] contents, int xCount, params GUILayoutOption[] options) => selected;
        public static int SelectionGrid(int selected, GUIContent[] contents, int xCount, GUIStyle style, params GUILayoutOption[] options) => selected;
        public static int SelectionGrid(int selected, Texture[] images, int xCount, params GUILayoutOption[] options) => selected;
        public static int SelectionGrid(int selected, Texture[] images, int xCount, GUIStyle style, params GUILayoutOption[] options) => selected;
        public static bool Toggle(bool value, string text, params GUILayoutOption[] options) => value;
        public static bool Toggle(bool value, string text, GUIStyle style, params GUILayoutOption[] options) => value;
        public static bool Toggle(bool value, GUIContent content, params GUILayoutOption[] options) => value;
        public static bool Toggle(bool value, GUIContent content, GUIStyle style, params GUILayoutOption[] options) => value;
        public static bool Toggle(bool value, Texture image, params GUILayoutOption[] options) => value;
        public static bool Toggle(bool value, Texture image, GUIStyle style, params GUILayoutOption[] options) => value;
        public static string TextField(string text, params GUILayoutOption[] options) => text;
        public static string TextField(string text, int maxLength, params GUILayoutOption[] options) => text;
        public static string TextField(string text, GUIStyle style, params GUILayoutOption[] options) => text;
        public static string TextField(string text, int maxLength, GUIStyle style, params GUILayoutOption[] options) => text;
        public static string TextArea(string text, params GUILayoutOption[] options) => text;
        public static string TextArea(string text, int maxLength, params GUILayoutOption[] options) => text;
        public static string TextArea(string text, GUIStyle style, params GUILayoutOption[] options) => text;
        public static string TextArea(string text, int maxLength, GUIStyle style, params GUILayoutOption[] options) => text;
        public static string PasswordField(string password, char maskChar, params GUILayoutOption[] options) => password;
        public static string PasswordField(string password, char maskChar, int maxLength, params GUILayoutOption[] options) => password;
        public static string PasswordField(string password, char maskChar, GUIStyle style, params GUILayoutOption[] options) => password;
        public static string PasswordField(string password, char maskChar, int maxLength, GUIStyle style, params GUILayoutOption[] options) => password;
        public static float HorizontalSlider(float value, float leftValue, float rightValue, params GUILayoutOption[] options) => value;
        public static float HorizontalSlider(float value, float leftValue, float rightValue, GUIStyle slider, GUIStyle thumb, params GUILayoutOption[] options) => value;
        public static float VerticalSlider(float value, float topValue, float bottomValue, params GUILayoutOption[] options) => value;
        public static float VerticalSlider(float value, float topValue, float bottomValue, GUIStyle slider, GUIStyle thumb, params GUILayoutOption[] options) => value;
        public static float HorizontalScrollbar(float value, float size, float leftValue, float rightValue, params GUILayoutOption[] options) => value;
        public static float HorizontalScrollbar(float value, float size, float leftValue, float rightValue, GUIStyle style, params GUILayoutOption[] options) => value;
        public static float VerticalScrollbar(float value, float size, float topValue, float bottomValue, params GUILayoutOption[] options) => value;
        public static float VerticalScrollbar(float value, float size, float topValue, float bottomValue, GUIStyle style, params GUILayoutOption[] options) => value;
        public static GUILayoutOption Width(float width) => new();
        public static GUILayoutOption Height(float height) => new();
        public static GUILayoutOption ExpandWidth(bool expand) => new();
        public static GUILayoutOption ExpandHeight(bool expand) => new();
        public static GUILayoutOption MinWidth(float width) => new();
        public static GUILayoutOption MaxWidth(float width) => new();
        public static GUILayoutOption MinHeight(float height) => new();
        public static GUILayoutOption MaxHeight(float height) => new();
    }
    public class GUILayoutUtility
    {
        public GUILayoutUtility() { }
        public static void BeginGroup(string name) { }
        public static void EndGroup(string name) { }
        public static Rect GetLastRect() => default;
        public static Rect GetAspectRect(float aspect) => default;
        public static Rect GetAspectRect(float aspect, GUIStyle style) => default;
        public static Rect GetAspectRect(float aspect, params GUILayoutOption[] options) => default;
        public static Rect GetAspectRect(float aspect, GUIStyle style,
            params GUILayoutOption[] options) => default;
        public static Rect GetRect(GUIContent content, GUIStyle style) => default;
        public static Rect GetRect(GUIContent content, GUIStyle style,
            params GUILayoutOption[] options) => default;
        public static Rect GetRect(float width, float height) => default;
        public static Rect GetRect(float width, float height, params GUILayoutOption[] options) => default;
        public static Rect GetRect(float width, float height, GUIStyle style) => default;
        public static Rect GetRect(float width, float height, GUIStyle style,
            params GUILayoutOption[] options) => default;
        public static Rect GetRect(float minWidth, float maxWidth, float minHeight,
            float maxHeight) => default;
        public static Rect GetRect(float minWidth, float maxWidth, float minHeight,
            float maxHeight, GUIStyle style) => default;
        public static Rect GetRect(float minWidth, float maxWidth, float minHeight,
            float maxHeight, params GUILayoutOption[] options) => default;
        public static Rect GetRect(float minWidth, float maxWidth, float minHeight,
            float maxHeight, GUIStyle style, params GUILayoutOption[] options) => default;
    }
    public struct RenderBuffer { }
    public class Display
    {
        public static Display main => new();
        public static Display[] displays => [];
        public bool active => true;
        public int systemWidth => 0;
        public int systemHeight => 0;
        public int renderingWidth => 0;
        public int renderingHeight => 0;
        public RenderBuffer colorBuffer => default;
        public RenderBuffer depthBuffer => default;
        public bool requiresBlitToBackbuffer => false;
        public bool requiresSrgbBlitToBackbuffer => false;
        public void Activate() { }
        public void Activate(int width, int height, int refreshRate) { }
        public void Activate(int width, int height, RefreshRate refreshRate) { }
        public void SetParams(int width, int height, int x, int y) { }
        public void SetRenderingResolution(int width, int height) { }
    }
    public class Screen
    {
        public static int width => 0;
        public static int height => 0;
        public static Resolution currentResolution => default;
        public static Resolution[] resolutions => [];
        public static bool fullScreen => false;
        public static FullScreenMode fullScreenMode => FullScreenMode.Windowed;
        public static Rect safeArea => default;
        public static Rect[] cutouts => [];
        public static void SetResolution(int width, int height, bool fullScreen) { }
        public static void SetResolution(int width, int height, bool fullScreen, int preferredRefreshRate) { }
        public static void SetResolution(int width, int height, FullScreenMode fullScreenMode) { }
        public static void SetResolution(int width, int height, FullScreenMode fullScreenMode,
            int preferredRefreshRate) { }
        public static void SetResolution(int width, int height, FullScreenMode fullScreenMode,
            RefreshRate preferredRefreshRate) { }
    }
    public enum CursorLockMode { None, Locked, Confined }
    public static class Cursor
    {
        public static CursorLockMode lockState { get; set; }
        public static bool visible { get; set; }
    }
    public class Time
    {
        public static float captureDeltaTime { get; set; }
        public static Unity.IntegerTime.RationalTime captureDeltaTimeRational { get; set; }
        public static int captureFramerate { get; set; }
        public static float maximumDeltaTime { get; set; }
        public static float maximumParticleDeltaTime { get; set; }
        public static int renderedFrameCount { get; }
        public static float smoothDeltaTime { get; }
        public static Unity.IntegerTime.RationalTime timeAsRational { get; }
        public static float deltaTime { get; }
        public static float fixedDeltaTime { get; set; }
        public static float timeScale { get; set; }
        public static int frameCount { get; }
        public static float time { get; }
        public static float fixedTime { get; }
        public static float unscaledTime { get; }
        public static float unscaledDeltaTime { get; }
        public static float fixedUnscaledTime { get; }
        public static float fixedUnscaledDeltaTime { get; }
        public static double timeAsDouble { get; }
        public static double fixedTimeAsDouble { get; }
        public static double unscaledTimeAsDouble { get; }
        public static double fixedUnscaledTimeAsDouble { get; }
        public static float realtimeSinceStartup { get; }
        public static double realtimeSinceStartupAsDouble { get; }
        public static float timeSinceLevelLoad { get; }
        public static double timeSinceLevelLoadAsDouble { get; }
        public static bool inFixedTimeStep { get; }
    }
    public enum TouchPhase { Began = 0, Moved = 1, Stationary = 2, Ended = 3, Canceled = 4 }
    public enum TouchType { Direct = 0, Indirect = 1, Stylus = 2 }
    public enum IMECompositionMode { Auto = 0, On = 1, Off = 2 }
    public enum PenEventType { NoContact = 0, PenDown = 1, PenUp = 2 }
    [System.Flags]
    public enum PenStatus { None = 0, Contact = 1, Barrel = 2, Inverted = 4, Eraser = 8 }
    public struct PenData
    {
        public Vector2 position, deltaPos, tilt;
        public float twist, pressure;
        public PenStatus penStatus;
        public PenEventType contactType;
    }
    public struct Touch
    {
        public int fingerId;
        public Vector2 position;
        public Vector2 rawPosition;
        public Vector2 deltaPosition;
        public float deltaTime;
        public TouchPhase phase;
        public TouchType type;
        public float pressure;
        public float radius;
        public int tapCount;
    }
    public static class Input
    {
        public static float GetAxis(string name) => 0;
        public static float GetAxisRaw(string name) => 0;
        public static bool GetButton(string name) => false;
        public static bool GetButtonDown(string name) => false;
        public static bool GetButtonUp(string name) => false;
        public static bool GetMouseButton(int button) => false;
        public static bool GetMouseButtonDown(int button) => false;
        public static bool GetMouseButtonUp(int button) => false;
        public static int touchCount => 0;
        public static Touch[] touches => default;
        public static Touch GetTouch(int index) => default;
        public static bool touchSupported => false;
        public static bool touchPressureSupported => false;
        public static bool stylusTouchSupported => false;
        public static int penEventCount => 0;
        public static PenData GetPenEvent(int index) => default;
        public static PenData GetLastPenContactEvent() => default;
        public static void ClearLastPenContactEvent() { }
        public static bool multiTouchEnabled { get; set; }
        public static bool mousePresent => false;
          public static string[] GetJoystickNames() => default;
        public static bool anyKey => false;
        public static bool anyKeyDown => false;
        public static Vector3 mousePosition => default;
        public static Vector3 mousePositionDelta => default;
        public static Vector2 mouseScrollDelta => default;
        public static string inputString => "";
        public static string compositionString => "";
        public static IMECompositionMode imeCompositionMode { get; set; }
        public static Vector2 compositionCursorPos { get; set; }
        public static void ResetInputAxes() { }
        public static bool GetKey(string name) => false;
        public static bool GetKey(KeyCode key) => false;
        public static bool GetKeyDown(string name) => false;
        public static bool GetKeyDown(KeyCode key) => false;
        public static bool GetKeyUp(string name) => false;
        public static bool GetKeyUp(KeyCode key) => false;
    }
    public static class Random
    {
        public struct State { public int s0, s1, s2, s3; }
        public static void InitState(int seed) { }
        public static State state { get; set; }
        public static int seed { get; set; }
        public static float value => 0;
        public static int Range(int minInclusive, int maxExclusive) => 0;
        public static float Range(float minInclusive, float maxInclusive) => 0;
        public static int RandomRange(int minInclusive, int maxExclusive) => 0;
        public static float RandomRange(float minInclusive, float maxInclusive) => 0;
        public static Vector2 insideUnitCircle => default;
        public static Vector2 onUnitCircle => default;
        public static Vector3 insideUnitSphere => default;
        public static Vector3 onUnitSphere => default;
        public static Quaternion rotation => default;
        public static Quaternion rotationUniform => default;
        public static Color ColorHSV() => default;
        public static Color ColorHSV(float hueMin, float hueMax) => default;
        public static Color ColorHSV(float hueMin, float hueMax, float saturationMin, float saturationMax) => default;
        public static Color ColorHSV(float hueMin, float hueMax, float saturationMin, float saturationMax,
            float valueMin, float valueMax) => default;
        public static Color ColorHSV(float hueMin, float hueMax, float saturationMin, float saturationMax,
            float valueMin, float valueMax, float alphaMin, float alphaMax) => default;
    }
    public enum IntegrityCheckLevel { Low = 1, Medium = 2, High = 3 }
    public enum ValidationLevel { None = 0, Low = 1, Medium = 2, High = 3 }
    // Gizmos calls in editor callbacks must bind during compilation, then are
    // removed from the browser runtime by EditorCodePolicy.
    public static class Gizmos
    {
        public static Color color { get; set; }
        public static Matrix4x4 matrix { get; set; }
        public static void DrawLine(Vector3 from, Vector3 to) { }
        public static void DrawCube(Vector3 center, Vector3 size) { }
        public static void DrawWireCube(Vector3 center, Vector3 size) { }
        public static void DrawSphere(Vector3 center, float radius) { }
        public static void DrawWireSphere(Vector3 center, float radius) { }
        public static void DrawRay(Vector3 from, Vector3 direction) { }
    }
    public class Debug
    {
        public Debug() { }
        public struct StartupLog
        {
            public long timestamp;
            public LogType logType;
            public string message;
        }
        public static Logger logger => new();
        public static Logger unityLogger => new();
        public static bool isDebugBuild => true;
        public static bool developerConsoleEnabled { get; set; }
        public static bool developerConsoleVisible { get; set; }
        public static string CheckIntegrity(IntegrityCheckLevel level) => string.Empty;
        public static bool IsValidationLevelEnabled(ValidationLevel level) => level == ValidationLevel.None;
        public static void ClearDeveloperConsole() { }
        public static void Break() { }
        public static StartupLog[] RetrieveStartupLogs() => [];
        public static void Log(object message) { }
        public static void Log(object message, Object context) { }
        public static void LogWarning(object message) { }
        public static void LogWarning(object message, Object context) { }
        public static void LogError(object message) { }
        public static void LogError(object message, Object context) { }
        public static void LogAssertion(object message) { }
        public static void LogAssertion(object message, Object context) { }
        public static void Assert(bool condition) { }
        public static void Assert(bool condition, Object context) { }
        public static void Assert(bool condition, object message) { }
        public static void Assert(bool condition, object message, Object context) { }
        public static void Assert(bool condition, string message) { }
        public static void Assert(bool condition, string message, Object context) { }
        public static void AssertFormat(bool condition, Object context, string format, params object[] args) { }
        public static void AssertFormat(bool condition, string format, params object[] args) { }
        public static void LogFormat(string format, params object[] args) { }
        public static void LogFormat(Object context, string format, params object[] args) { }
        public static void LogFormat(LogType type, LogOption option, Object context, string format, params object[] args) { }
        public static void LogWarningFormat(string format, params object[] args) { }
        public static void LogWarningFormat(Object context, string format, params object[] args) { }
        public static void LogErrorFormat(string format, params object[] args) { }
        public static void LogErrorFormat(Object context, string format, params object[] args) { }
        public static void LogAssertionFormat(string format, params object[] args) { }
        public static void LogAssertionFormat(Object context, string format, params object[] args) { }
        public static void LogException(System.Exception exception) { }
        public static void LogException(System.Exception exception, Object context) { }
        public static void DrawLine(Vector3 start, Vector3 end) { }
        public static void DrawLine(Vector3 start, Vector3 end, Color color) { }
        public static void DrawLine(Vector3 start, Vector3 end, Color color, float duration) { }
        public static void DrawLine(Vector3 start, Vector3 end, Color color, float duration, bool depthTest) { }
        public static void DrawRay(Vector3 start, Vector3 direction) { }
        public static void DrawRay(Vector3 start, Vector3 direction, Color color) { }
        public static void DrawRay(Vector3 start, Vector3 direction, Color color, float duration) { }
        public static void DrawRay(Vector3 start, Vector3 direction, Color color, float duration, bool depthTest) { }
    }
    public enum LogOption { None = 0, NoStacktrace = 1 }
    public interface ILogHandler
    {
        void LogFormat(LogType type, Object context, string format, params object[] args);
        void LogException(System.Exception exception, Object context);
    }
    public interface ILogger
    {
        ILogHandler logHandler { get; set; }
        bool logEnabled { get; set; }
        LogType filterLogType { get; set; }
        bool IsLogTypeAllowed(LogType type);
        void Log(object message);
        void Log(string tag, object message);
        void Log(string tag, object message, Object context);
        void Log(LogType type, object message);
        void Log(LogType type, object message, Object context);
        void Log(LogType type, string tag, object message);
        void Log(LogType type, string tag, object message, Object context);
        void LogError(string tag, object message);
        void LogError(string tag, object message, Object context);
        void LogWarning(string tag, object message);
        void LogWarning(string tag, object message, Object context);
        void LogException(System.Exception exception);
        void LogFormat(LogType type, string format, params object[] args);
    }
    public class Logger : ILogger
    {
        public Logger(ILogHandler handler) { logHandler = handler; }
        public Logger() { }
        public ILogHandler logHandler { get; set; }
        public bool logEnabled { get; set; }
        public LogType filterLogType { get; set; }
        public bool IsLogTypeAllowed(LogType type) => false;
        public void Log(object message) { }
        public void Log(object message, Object context) { }
        public void Log(string tag, object message) { }
        public void Log(string tag, object message, Object context) { }
        public void Log(LogType type, object message) { }
        public void Log(LogType type, object message, Object context) { }
        public void Log(LogType type, string tag, object message) { }
        public void Log(LogType type, string tag, object message, Object context) { }
        public void LogWarning(string tag, object message) { }
        public void LogWarning(string tag, object message, Object context) { }
        public void LogError(string tag, object message) { }
        public void LogError(string tag, object message, Object context) { }
        public void LogFormat(LogType type, string format, params object[] args) { }
        public void LogFormat(LogType type, Object context, string format, params object[] args) { }
        public void LogException(System.Exception exception) { }
        public void LogException(System.Exception exception, Object context) { }
    }
    public class Application
    {
        public Application() { }
        public static void DontDestroyOnLoad(Object target) { }
        public static bool isPlaying => false;
        public static bool IsPlaying(Object value) => false;
        public static bool isEditor => false;
        public static bool isBatchMode => false;
        public static bool isMobilePlatform => false;
        public static RuntimePlatform platform => RuntimePlatform.WebGLPlayer;
        public static string persistentDataPath => "";
        public static string absoluteURL => "";
        public static string dataPath => "";
        public static string streamingAssetsPath => "";
        public static string temporaryCachePath => "";
        public static NetworkReachability internetReachability => NetworkReachability.ReachableViaLocalAreaNetwork;
        public static bool isConsolePlatform => false;
        public static bool isFocused => false;
        public static bool isLoadingLevel => false;
        public static bool isPlayer => true;
        public static bool isShowingSplashScreen => false;
        public static SystemLanguage systemLanguage => SystemLanguage.English;
        public static bool runInBackground { get; set; }
        public static string companyName => "";
        public static string productName => "";
        public static string identifier => "";
        public static string version => "";
        public static string unityVersion => "";
        public static int streamedBytes => 0;
        public static float GetStreamProgressForLevel(int level) => 0;
        public static float GetStreamProgressForLevel(string level) => 1;
        public static int levelCount => 0;
        public static int loadedLevel => -1;
        public static string loadedLevelName => "";
        public static bool CanStreamedLevelBeLoaded(int index) => false;
        public static bool CanStreamedLevelBeLoaded(string name) => false;
        public static void LoadLevel(int index) { }
        public static void LoadLevel(string name) { }
        public static AsyncOperation LoadLevelAsync(int index) => null!;
        public static AsyncOperation LoadLevelAsync(string name) => null!;
        public static void LoadLevelAdditive(int index) { }
        public static void LoadLevelAdditive(string name) { }
        public static AsyncOperation LoadLevelAdditiveAsync(int index) => null!;
        public static AsyncOperation LoadLevelAdditiveAsync(string name) => null!;
        public static bool UnloadLevel(int index) => false;
        public static bool UnloadLevel(string name) => false;
        public static void Quit() { }
        public static void Quit(int exitCode) { }
        public static void CancelQuit() { }
        public static void OpenURL(string url) { }
        public static event System.Func<bool>? wantsToQuit;
        public static event System.Action? quitting;
        public delegate void LogCallback(string condition, string stackTrace, LogType type);
        public static event LogCallback? logMessageReceived;
        public static event LogCallback? logMessageReceivedThreaded;
        public static event System.Action<bool>? focusChanged;
        public static void RegisterLogCallback(LogCallback callback) { }
        public static void RegisterLogCallbackThreaded(LogCallback callback) { }
        public static int targetFrameRate { get; set; } = -1;
        public static string[] GetBuildTags() => System.Array.Empty<string>();
        public static void SetBuildTags(string[] tags) { }
        public static StackTraceLogType GetStackTraceLogType(LogType type) => StackTraceLogType.ScriptOnly;
        public static void SetStackTraceLogType(LogType type, StackTraceLogType stackTraceType) { }
        public static StackTraceLogType stackTraceLogType { get; set; }
    }
    public struct Keyframe
    {
        public Keyframe(float time, float value) : this(time, value, 0, 0) { }
        public Keyframe(float time, float value, float inTangent, float outTangent)
        {
            this.time = time; this.value = value;
            this.inTangent = inTangent; this.outTangent = outTangent;
            inWeight = 0; outWeight = 0; weightedMode = WeightedMode.None;
        }
        public Keyframe(float time, float value, float inTangent, float outTangent,
            float inWeight, float outWeight)
        {
            this.time = time; this.value = value;
            this.inTangent = inTangent; this.outTangent = outTangent;
            this.inWeight = inWeight; this.outWeight = outWeight;
            weightedMode = WeightedMode.Both;
        }
        public float time { get; set; }
        public float value { get; set; }
        public float inTangent { get; set; }
        public float outTangent { get; set; }
        public float inWeight { get; set; }
        public float outWeight { get; set; }
        public WeightedMode weightedMode { get; set; }
        public int tangentMode { get; set; }
    }
    public sealed class AnimationCurve
    {
        public AnimationCurve() { }
        public AnimationCurve(params Keyframe[] keys) { this.keys = keys; }
        public Keyframe[] keys { get; set; } = System.Array.Empty<Keyframe>();
        public int length => keys.Length;
        public Keyframe this[int index] => keys[index];
        public WrapMode preWrapMode { get; set; } = WrapMode.ClampForever;
        public WrapMode postWrapMode { get; set; } = WrapMode.ClampForever;
        public int AddKey(Keyframe key) => 0;
        public int AddKey(float time, float value) => 0;
        public float Evaluate(float time) => 0;
        public int MoveKey(int index, Keyframe key) => 0;
        public void RemoveKey(int index) { }
        public void ClearKeys() { }
        public void CopyFrom(AnimationCurve source) { }
        public void GetKeys(System.Span<Keyframe> destination) { }
        public void SetKeys(System.ReadOnlySpan<Keyframe> source) { }
        public void SmoothTangents(int index, float weight) { }
        public bool Equals(AnimationCurve other) => false;
        public override bool Equals(object? other) => false;
        public override int GetHashCode() => 0;
        public static AnimationCurve Constant(float startTime, float endTime,
            float value) => new AnimationCurve();
        public static AnimationCurve Linear(float startTime, float startValue,
            float endTime, float endValue) => new AnimationCurve();
        public static AnimationCurve EaseInOut(float startTime, float startValue,
            float endTime, float endValue) => new AnimationCurve();
    }
    public struct PropertyName
    {
        public PropertyName(PropertyName value) { }
        public PropertyName(int value) { }
        public PropertyName(string value) { }
        public static bool IsNullOrEmpty(PropertyName value) => false;
        public bool Equals(PropertyName other) => false;
        public override bool Equals(object? other) => false;
        public override int GetHashCode() => 0;
        public override string ToString() => "";
        public static implicit operator PropertyName(int value) => new PropertyName(value);
        public static implicit operator PropertyName(string value) => new PropertyName(value);
        public static bool operator ==(PropertyName left, PropertyName right) => false;
        public static bool operator !=(PropertyName left, PropertyName right) => false;
    }
    public struct Hash128
    {
        public Hash128(uint a, uint b, uint c, uint d) { }
        public Hash128(ulong a, ulong b) { }
        public bool isValid => false;
        public static Hash128 Parse(string value) => default;
        public static Hash128 Compute(string value) => default;
        public static Hash128 Compute(int value) => default;
        public static Hash128 Compute(float value) => default;
        public static Hash128 Compute(byte[] value) => default;
        public static Hash128 Compute<T>(T[] value) where T : struct => default;
        public static Hash128 Compute<T>(T[] value, int start, int count) where T : struct => default;
        public static Hash128 Compute<T>(System.Collections.Generic.List<T> value) where T : struct => default;
        public static Hash128 Compute<T>(System.Collections.Generic.List<T> value, int start, int count) where T : struct => default;
        public static Hash128 Compute<T>(Unity.Collections.NativeArray<T> value) where T : struct => default;
        public static Hash128 Compute<T>(Unity.Collections.NativeArray<T> value, int start, int count) where T : struct => default;
        public static Hash128 Compute<T>(ref T value) where T : struct => default;
        public void Append(string value) { }
        public void Append(int value) { }
        public void Append(float value) { }
        public void Append(byte[] value) { }
        public void Append<T>(T[] value) where T : struct { }
        public void Append<T>(T[] value, int start, int count) where T : struct { }
        public void Append<T>(System.Collections.Generic.List<T> value) where T : struct { }
        public void Append<T>(System.Collections.Generic.List<T> value, int start, int count) where T : struct { }
        public void Append<T>(Unity.Collections.NativeArray<T> value) where T : struct { }
        public void Append<T>(Unity.Collections.NativeArray<T> value, int start, int count) where T : struct { }
        public void Append<T>(ref T value) where T : struct { }
        public int CompareTo(Hash128 other) => 0;
        public int CompareTo(object other) => 0;
        public bool Equals(Hash128 other) => false;
        public override bool Equals(object other) => false;
        public override int GetHashCode() => 0;
        public override string ToString() => "";
        public static bool operator ==(Hash128 left, Hash128 right) => false;
        public static bool operator !=(Hash128 left, Hash128 right) => false;
        public static bool operator <(Hash128 left, Hash128 right) => false;
        public static bool operator >(Hash128 left, Hash128 right) => false;
    }
    public class Shader : Object
    {
        public static Shader Find(string name) => null!;
        public static int PropertyToID(string name) => 0;
    }
    public class Material : Object
    {
        public Material() { }
        public Material(Shader shader) { }
        public Material(Material material) { }
        public void CopyPropertiesFromMaterial(Material material) { }
        public void CopyMatchingPropertiesFromMaterial(Material material) { }
        public void Lerp(Material start, Material end, float t) { }
        public Shader shader { get; set; } = null!;
        public Color color { get; set; }
        public Texture mainTexture { get; set; } = null!;
        public Vector2 mainTextureScale { get; set; }
        public Vector2 mainTextureOffset { get; set; }
        public int renderQueue { get; set; }
        public void EnableKeyword(string keyword) { }
        public void DisableKeyword(string keyword) { }
        public bool IsKeywordEnabled(string keyword) => false;
        public void SetFloat(string name, float value) { }
        public void SetFloat(int nameID, float value) { }
        public void SetInt(string name, int value) { }
        public void SetInt(int nameID, int value) { }
        public int GetInt(string name) => 0;
        public int GetInt(int nameID) => 0;
        public void SetInteger(string name, int value) { }
        public void SetInteger(int nameID, int value) { }
        public int GetInteger(string name) => 0;
        public int GetInteger(int nameID) => 0;
        public void SetColor(string name, Color value) { }
        public void SetColor(int nameID, Color value) { }
        public void SetTexture(string name, Texture value) { }
        public void SetTexture(int nameID, Texture value) { }
        public float GetFloat(string name) => 0;
        public float GetFloat(int nameID) => 0;
        public Color GetColor(string name) => default;
        public Color GetColor(int nameID) => default;
        public Vector4 GetVector(string name) => default;
        public Vector4 GetVector(int nameID) => default;
        public void SetVector(string name, Vector4 value) { }
        public void SetVector(int nameID, Vector4 value) { }
        public bool HasColor(string name) => false;
        public bool HasColor(int nameID) => false;
        public bool HasFloat(string name) => false;
        public bool HasFloat(int nameID) => false;
        public bool HasInt(string name) => false;
        public bool HasInt(int nameID) => false;
        public bool HasInteger(string name) => false;
        public bool HasInteger(int nameID) => false;
        public bool HasVector(string name) => false;
        public bool HasVector(int nameID) => false;
        public bool HasTexture(string name) => false;
        public bool HasTexture(int nameID) => false;
        public bool HasMatrix(string name) => false;
        public bool HasMatrix(int nameID) => false;
        public Vector2 GetTextureScale(string name) => default;
        public Vector2 GetTextureScale(int nameID) => default;
        public Vector2 GetTextureOffset(string name) => default;
        public Vector2 GetTextureOffset(int nameID) => default;
        public void SetTextureScale(string name, Vector2 value) { }
        public void SetTextureScale(int nameID, Vector2 value) { }
        public void SetTextureOffset(string name, Vector2 value) { }
        public void SetTextureOffset(int nameID, Vector2 value) { }
        public Texture GetTexture(string name) => null!;
        public Texture GetTexture(int nameID) => null!;
        public bool HasProperty(string name) => false;
        public bool HasProperty(int nameID) => false;
    }
    public class MaterialPropertyBlock
    {
        public bool isEmpty => true;
        public void Clear() { }
        public void SetInt(string name, int value) { }
        public void SetInt(int nameID, int value) { }
        public int GetInt(string name) => 0;
        public int GetInt(int nameID) => 0;
        public bool HasInt(string name) => false;
        public bool HasInt(int nameID) => false;
        public void SetInteger(string name, int value) { }
        public void SetInteger(int nameID, int value) { }
        public int GetInteger(string name) => 0;
        public int GetInteger(int nameID) => 0;
        public bool HasInteger(string name) => false;
        public bool HasInteger(int nameID) => false;
        public void SetFloat(string name, float value) { }
        public void SetFloat(int nameID, float value) { }
        public float GetFloat(string name) => 0;
        public float GetFloat(int nameID) => 0;
        public bool HasFloat(string name) => false;
        public bool HasFloat(int nameID) => false;
        public void SetColor(string name, Color value) { }
        public void SetColor(int nameID, Color value) { }
        public Color GetColor(string name) => default;
        public Color GetColor(int nameID) => default;
        public bool HasColor(string name) => false;
        public bool HasColor(int nameID) => false;
        public bool HasProperty(string name) => false;
        public bool HasProperty(int nameID) => false;
        public void SetVector(string name, Vector4 value) { }
        public void SetVector(int nameID, Vector4 value) { }
        public Vector4 GetVector(string name) => default;
        public Vector4 GetVector(int nameID) => default;
        public bool HasVector(string name) => false;
        public bool HasVector(int nameID) => false;
        public void SetTexture(string name, Texture value) { }
        public void SetTexture(int nameID, Texture value) { }
        public Texture GetTexture(string name) => null!;
        public Texture GetTexture(int nameID) => null!;
        public bool HasTexture(string name) => false;
        public bool HasTexture(int nameID) => false;
    }
    public class SystemInfo
    {
        public SystemInfo() { }
        public static string unsupportedIdentifier => "n/a";
        public static string deviceModel => "";
        public static DeviceType deviceType => DeviceType.Desktop;
    }
    public struct RefreshRate
    {
        public uint numerator;
        public uint denominator;
        public double value => 0;
        public int CompareTo(RefreshRate other) => 0;
        public bool Equals(RefreshRate other) => false;
        public override bool Equals(object? other) => false;
        public override int GetHashCode() => 0;
        public override string ToString() => "";
        public static bool operator ==(RefreshRate left, RefreshRate right) => false;
        public static bool operator !=(RefreshRate left, RefreshRate right) => false;
    }
    public struct Resolution
    {
        public int width;
        public int height;
        public int refreshRate => 0;
        public RefreshRate refreshRateRatio { get; set; }
        public bool Equals(Resolution other) => false;
        public override bool Equals(object? other) => false;
        public override int GetHashCode() => 0;
        public override string ToString() => "";
        public static bool operator ==(Resolution left, Resolution right) => false;
        public static bool operator !=(Resolution left, Resolution right) => false;
    }
    public struct RangeInt
    {
        public int start;
        public int length;
        public int end => start + length;
        public RangeInt(int start, int length) { this.start = start; this.length = length; }
    }
    public struct BoundingSphere
    {
        public Vector3 position;
        public float radius;
        public BoundingSphere(Vector3 position, float radius) { this.position = position; this.radius = radius; }
        public BoundingSphere(Vector4 packed) { position = new Vector3(packed.x, packed.y, packed.z); radius = packed.w; }
    }
    public struct MeshLodRange
    {
        public uint indexStart { get; set; }
        public uint indexCount { get; set; }
        public MeshLodRange(uint indexStart, uint indexCount) { this.indexStart = indexStart; this.indexCount = indexCount; }
        public override string ToString() => "";
    }
    public struct MipmapLimitDescriptor
    {
        public bool useMipmapLimit { get; }
        public string groupName { get; }
        public MipmapLimitDescriptor(bool useMipmapLimit, string groupName)
        { this.useMipmapLimit = useMipmapLimit; this.groupName = groupName; }
    }
    public static class Snapping
    {
        public static float Snap(float value, float snap) => 0;
        public static Vector2 Snap(Vector2 value, Vector2 snap) => default;
        public static Vector3 Snap(Vector3 value, Vector3 snap, SnapAxis axis) => default;
    }
    public class RectOffset
    {
        public RectOffset() { }
        public RectOffset(int left, int right, int top, int bottom) { }
        public int left { get; set; }
        public int right { get; set; }
        public int top { get; set; }
        public int bottom { get; set; }
        public int horizontal => 0;
        public int vertical => 0;
        public Rect Add(Rect rect) => default;
        public Rect Remove(Rect rect) => default;
        public override string ToString() => "";
        public string ToString(string format) => "";
        public string ToString(string format, System.IFormatProvider provider) => "";
    }
    public struct BoneWeight
    {
        public int boneIndex0, boneIndex1, boneIndex2, boneIndex3;
        public float weight0, weight1, weight2, weight3;
        public bool Equals(BoneWeight other) => false;
        public override bool Equals(object? other) => false;
        public override int GetHashCode() => 0;
        public static bool operator ==(BoneWeight left, BoneWeight right) => false;
        public static bool operator !=(BoneWeight left, BoneWeight right) => false;
    }
    public struct BoneWeight1
    {
        public int boneIndex;
        public float weight;
        public bool Equals(BoneWeight1 other) => false;
        public override bool Equals(object? other) => false;
        public override int GetHashCode() => 0;
        public static bool operator ==(BoneWeight1 left, BoneWeight1 right) => false;
        public static bool operator !=(BoneWeight1 left, BoneWeight1 right) => false;
    }
    public class PlayerPrefsException : System.Exception
    {
        public PlayerPrefsException(string message) : base(message) { }
    }
    public class PlayerPrefs
    {
        public PlayerPrefs() { }
        public static void DeleteAll() { }
        public static void DeleteKey(string key) { }
        public static float GetFloat(string key) => 0;
        public static float GetFloat(string key, float defaultValue) => defaultValue;
        public static int GetInt(string key) => 0;
        public static int GetInt(string key, int defaultValue) => defaultValue;
        public static string GetString(string key) => "";
        public static string GetString(string key, string defaultValue) => defaultValue;
        public static bool HasKey(string key) => false;
        public static void Save() { }
        public static void SetFloat(string key, float value) { }
        public static void SetInt(string key, int value) { }
        public static void SetString(string key, string value) { }
    }
    public static class QualitySettings
    {
        public static event System.Action<int, int> activeQualityLevelChanged;
        public static string[] names => System.Array.Empty<string>();
        public static int count => 0;
        public static QualityLevel currentLevel => QualityLevel.Fastest;
        public static void ForEach(System.Action action) { }
        public static void ForEach(System.Action<int, string> action) { }
        public static int GetQualityLevel() => 0;
        public static void SetQualityLevel(int index) { }
        public static void SetQualityLevel(int index, bool applyExpensiveChanges) { }
        public static void IncreaseLevel() { }
        public static void IncreaseLevel(bool applyExpensiveChanges) { }
        public static void DecreaseLevel() { }
        public static void DecreaseLevel(bool applyExpensiveChanges) { }
    }
    public static class Mathf
    {
        public const float PI = 3.1415927f;
        public const float Deg2Rad = PI / 180f, Rad2Deg = 180f / PI;
        public const float Infinity = float.PositiveInfinity, NegativeInfinity = float.NegativeInfinity;
        public const float Epsilon = float.Epsilon;
        public static float Abs(float value) => value; public static int Abs(int value) => value;
        public static float Clamp(float value, float min, float max) => value;
        public static int Clamp(int value, int min, int max) => value;
        public static float Clamp01(float value) => value;
        public static float Min(float left, float right) => left;
        public static int Min(int left, int right) => left;
        public static float Min(params float[] values) => 0;
        public static int Min(params int[] values) => 0;
        public static float Max(float left, float right) => left;
        public static int Max(int left, int right) => left;
        public static float Max(params float[] values) => 0;
        public static int Max(params int[] values) => 0;
        public static float Sin(float value) => value;
        public static float Cos(float value) => value;
        public static float Tan(float value) => value;
        public static float Asin(float value) => value;
        public static float Acos(float value) => value;
        public static float Atan(float value) => value;
        public static float Atan2(float y, float x) => y;
        public static float Sqrt(float value) => value;
        public static float Pow(float value, float power) => value;
        public static float Exp(float value) => value;
        public static Color CorrelatedColorTemperatureToRGB(float kelvin) => default;
        public static float Gamma(float value, float absmax, float gamma) => value;
        public static float GammaToLinearSpace(float value) => value;
        public static float LinearToGammaSpace(float value) => value;
        public static ushort FloatToHalf(float value) => 0;
        public static float HalfToFloat(ushort value) => 0;
        public static float Log(float value) => value;
        public static float Log(float value, float baseValue) => value;
        public static float Log10(float value) => value;
        public static float Floor(float value) => value;
        public static float Ceil(float value) => value;
        public static float Round(float value) => value;
        public static int FloorToInt(float value) => 0;
        public static int CeilToInt(float value) => 0;
        public static int RoundToInt(float value) => 0;
        public static float Lerp(float a, float b, float t) => a;
        public static float LerpUnclamped(float a, float b, float t) => a;
        public static float LerpAngle(float a, float b, float t) => a;
        public static float InverseLerp(float a, float b, float value) => value;
        public static float MoveTowards(float current, float target, float maxDelta) => current;
        public static float MoveTowardsAngle(float current, float target, float maxDelta) => current;
        public static float DeltaAngle(float current, float target) => current;
        public static float Repeat(float t, float length) => t;
        public static float PingPong(float t, float length) => t;
        public static float SmoothStep(float from, float to, float t) => from;
        public static float Sign(float value) => value;
        public static bool Approximately(float a, float b) => false;
        public static bool IsPowerOfTwo(int value) => false;
        public static int NextPowerOfTwo(int value) => value;
        public static int ClosestPowerOfTwo(int value) => value;
        public static float PerlinNoise(float x, float y) => 0;
        public static float PerlinNoise1D(float x) => 0;
        public static float SmoothDamp(float current, float target, ref float currentVelocity,
            float smoothTime, float maxSpeed = float.PositiveInfinity, float deltaTime = 0.02f) => current;
        public static float SmoothDampAngle(float current, float target, ref float currentVelocity,
            float smoothTime, float maxSpeed = float.PositiveInfinity, float deltaTime = 0.02f) => current;
    }
    public sealed class SerializeField : System.Attribute { }
    public sealed class SerializeReference : System.Attribute { }
    public sealed class HideInInspector : System.Attribute { }
    public class YieldInstruction { }
    public class Coroutine : YieldInstruction { }
    public enum WaitTimeoutMode { Realtime = 0, InGameTime = 1 }
    public sealed class WaitUntil : CustomYieldInstruction
    {
        public WaitUntil(System.Func<bool> predicate) { }
        public WaitUntil(System.Func<bool> predicate, System.TimeSpan timeout,
            System.Action onTimeout, WaitTimeoutMode timeoutMode = WaitTimeoutMode.Realtime) { }
        public override bool keepWaiting => false;
    }
    public sealed class WaitWhile : CustomYieldInstruction
    {
        public WaitWhile(System.Func<bool> predicate) { }
        public WaitWhile(System.Func<bool> predicate, System.TimeSpan timeout,
            System.Action onTimeout, WaitTimeoutMode timeoutMode = WaitTimeoutMode.Realtime) { }
        public override bool keepWaiting => false;
    }
    public abstract class CustomYieldInstruction : System.Collections.IEnumerator
    {
        public abstract bool keepWaiting { get; }
        public object Current => null;
        public bool MoveNext() => keepWaiting;
        public virtual void Reset() { }
    }
    public class AsyncOperation
    {
        public event System.Action<AsyncOperation> completed;
        public bool isDone { get; }
        public float progress { get; }
        public bool allowSceneActivation { get; set; }
        public int priority { get; set; }
    }
    public static class AsyncOperationAwaitableExtensions
    {
        public static Awaitable.Awaiter GetAwaiter(this AsyncOperation operation) => default;
    }
    public static class UnityEventAwaitableExtensions
    {
        public static Awaitable.Awaiter GetAwaiter(this UnityEngine.Events.UnityEvent value) => default;
        public static Awaitable<T0>.Awaiter GetAwaiter<T0>(this UnityEngine.Events.UnityEvent<T0> value) => default;
        public static Awaitable<(T0, T1)>.Awaiter GetAwaiter<T0, T1>(
            this UnityEngine.Events.UnityEvent<T0, T1> value) => default;
        public static Awaitable<(T0, T1, T2)>.Awaiter GetAwaiter<T0, T1, T2>(
            this UnityEngine.Events.UnityEvent<T0, T1, T2> value) => default;
        public static Awaitable<(T0, T1, T2, T3)>.Awaiter GetAwaiter<T0, T1, T2, T3>(
            this UnityEngine.Events.UnityEvent<T0, T1, T2, T3> value) => default;
    }
    public class Awaitable
    {
        public bool IsCompleted => false;
        public void Cancel() { }
        public Awaiter GetAwaiter() => new();
        public static Awaitable NextFrameAsync(System.Threading.CancellationToken token = default) => new();
        public static Awaitable FixedUpdateAsync(System.Threading.CancellationToken token = default) => new();
        public static Awaitable EndOfFrameAsync(System.Threading.CancellationToken token = default) => new();
        public static Awaitable MainThreadAsync() => new();
        public static Awaitable BackgroundThreadAsync() => new();
        public static Awaitable WaitForSecondsAsync(float seconds,
            System.Threading.CancellationToken token = default) => new();
        public static Awaitable FromAsyncOperation(AsyncOperation operation,
            System.Threading.CancellationToken token = default) => new();
        public struct Awaiter : System.Runtime.CompilerServices.INotifyCompletion
        {
            public bool IsCompleted => false;
            public void OnCompleted(System.Action continuation) { }
            public void GetResult() { }
        }
    }
    public class Awaitable<T>
    {
        public void Cancel() { }
        public Awaiter GetAwaiter() => new();
        public struct Awaiter : System.Runtime.CompilerServices.INotifyCompletion
        {
            public Awaiter(Awaitable<T> awaitable) { }
            public bool IsCompleted => false;
            public void OnCompleted(System.Action continuation) { }
            public T GetResult() => default;
        }
    }
    public class AwaitableCompletionSource
    {
        public Awaitable Awaitable => new();
        public void Reset() { }
        public void SetCanceled() { }
        public void SetException(System.Exception error) { }
        public void SetResult() { }
        public bool TrySetCanceled() => false;
        public bool TrySetException(System.Exception error) => false;
        public bool TrySetResult() => false;
    }
    public class AwaitableCompletionSource<T>
    {
        public Awaitable<T> Awaitable => new();
        public void Reset() { }
        public void SetCanceled() { }
        public void SetException(System.Exception error) { }
        public void SetResult(in T value) { }
        public bool TrySetCanceled() => false;
        public bool TrySetException(System.Exception error) => false;
        public bool TrySetResult(in T value) => false;
    }
    public class ResourceRequest : AsyncOperation
    {
        private ResourceRequest() { }
        public Object asset { get; }
    }
    public class AssetBundle : Object
    {
        public static AssetBundle? LoadFromFile(string path, uint crc = 0, ulong offset = 0) => null;
        public static AssetBundleCreateRequest LoadFromFileAsync(string path, uint crc = 0, ulong offset = 0) => null!;
        public static AssetBundle? LoadFromMemory(byte[] binary) => null;
        public static AssetBundleCreateRequest LoadFromMemoryAsync(byte[] binary) => null!;
        public static AssetBundle? LoadFromStream(System.IO.Stream stream) => null;
        public static AssetBundleCreateRequest LoadFromStreamAsync(System.IO.Stream stream) => null!;
        public static System.Collections.Generic.IEnumerable<AssetBundle> GetAllLoadedAssetBundles() => [];
        public static void UnloadAllAssetBundles(bool unloadAllLoadedObjects) { }
        public bool Contains(string name) => false;
        public string[] GetAllAssetNames() => [];
        public string[] GetAllScenePaths() => [];
        public Object? LoadAsset(string name) => null;
        public Object? LoadAsset(string name, System.Type type) => null;
        public T? LoadAsset<T>(string name) where T : Object => null;
        public AssetBundleRequest LoadAssetAsync(string name) => null!;
        public AssetBundleRequest LoadAssetAsync(string name, System.Type type) => null!;
        public AssetBundleRequest LoadAssetAsync<T>(string name) where T : Object => null!;
        public Object[] LoadAssetWithSubAssets(string name) => [];
        public Object[] LoadAssetWithSubAssets(string name, System.Type type) => [];
        public T[] LoadAssetWithSubAssets<T>(string name) where T : Object => [];
        public AssetBundleRequest LoadAssetWithSubAssetsAsync(string name) => null!;
        public AssetBundleRequest LoadAssetWithSubAssetsAsync(string name, System.Type type) => null!;
        public AssetBundleRequest LoadAssetWithSubAssetsAsync<T>(string name) where T : Object => null!;
        public Object[] LoadAllAssets() => [];
        public Object[] LoadAllAssets(System.Type type) => [];
        public T[] LoadAllAssets<T>() where T : Object => [];
        public AssetBundleRequest LoadAllAssetsAsync() => null!;
        public AssetBundleRequest LoadAllAssetsAsync(System.Type type) => null!;
        public AssetBundleRequest LoadAllAssetsAsync<T>() where T : Object => null!;
        public void Unload(bool unloadAllLoadedObjects) { }
        public AssetBundleUnloadOperation UnloadAsync(bool unloadAllLoadedObjects) => null!;
    }
    public class AssetBundleManifest : Object
    {
        public string[] GetAllAssetBundles() => [];
        public string[] GetAllAssetBundlesWithVariant() => [];
        public string[] GetAllDependencies(string bundleName) => [];
        public string[] GetDirectDependencies(string bundleName) => [];
        public Hash128 GetAssetBundleHash(string bundleName) => default;
    }
    public class AssetBundleUnloadOperation : AsyncOperation
    {
        public void WaitForCompletion() { }
    }
    public class AssetBundleRequest : AsyncOperation
    {
        public Object asset { get; }
        public Object[] allAssets { get; }
    }
    public class AssetBundleCreateRequest : AsyncOperation
    {
        public AssetBundle assetBundle { get; }
    }
    public class AsyncInstantiateOperation : AsyncOperation
    {
        public Object[] Result => [];
        public void Cancel() { }
        public bool IsWaitingForSceneActivation() => false;
        public void WaitForCompletion() { }
        public static float GetIntegrationTimeMS() => 2f;
        public static void SetIntegrationTimeMS(float value) { }
    }
    public class AsyncInstantiateOperation<T> : AsyncInstantiateOperation where T : Object
    {
        public new T[] Result => [];
        public Awaiter GetAwaiter() => new(this);
        public struct Awaiter : System.Runtime.CompilerServices.INotifyCompletion
        {
            public Awaiter(AsyncInstantiateOperation<T> operation) { }
            public bool IsCompleted => false;
            public void OnCompleted(System.Action continuation) { }
            public T[] GetResult() => [];
        }
    }
    public sealed class ContextMenu : System.Attribute {
        public ContextMenu(string item) { menuItem = item; }
        public ContextMenu(string item, bool validate) { menuItem = item; this.validate = validate; }
        public ContextMenu(string item, bool validate, int priority) {
            menuItem = item; this.validate = validate; this.priority = priority;
        }
        public string menuItem { get; }
        public bool validate { get; }
        public int priority { get; }
    }
    public sealed class ContextMenuItemAttribute : System.Attribute {
        public ContextMenuItemAttribute(string name, string function) {
            this.name = name; this.function = function;
        }
        public string name { get; }
        public string function { get; }
    }
    public sealed class CreateAssetMenuAttribute : System.Attribute {
        public string fileName { get; set; } = "";
        public string menuName { get; set; } = "";
        public int order { get; set; }
    }
    public sealed class DelayedAttribute : System.Attribute { }
    public sealed class EnumButtonsAttribute : System.Attribute {
        public EnumButtonsAttribute(bool includeObsolete = false) {
            this.includeObsolete = includeObsolete;
        }
        public bool includeObsolete { get; }
    }
    public sealed class GradientUsageAttribute : System.Attribute {
        public GradientUsageAttribute(bool hdr) { this.hdr = hdr; }
        public GradientUsageAttribute(bool hdr, ColorSpace colorSpace) {
            this.hdr = hdr; this.colorSpace = colorSpace;
        }
        public bool hdr { get; }
        public ColorSpace colorSpace { get; }
    }
    public sealed class HeaderAttribute : System.Attribute {
        public HeaderAttribute(string title) { header = title; }
        public string header { get; }
    }
    public sealed class HelpURLAttribute : System.Attribute {
        public HelpURLAttribute(string url) { URL = url; }
        public string URL { get; }
    }
    public sealed class IconAttribute : System.Attribute {
        public IconAttribute(string path) { this.path = path; }
        public string path { get; }
    }
    public sealed class InspectorNameAttribute : System.Attribute {
        public InspectorNameAttribute(string displayName) { this.displayName = displayName; }
        public string displayName { get; }
    }
    public sealed class MinAttribute : System.Attribute {
        public MinAttribute(float min) { this.min = min; }
        public float min { get; }
    }
    public sealed class MultilineAttribute : System.Attribute {
        public MultilineAttribute() { }
        public MultilineAttribute(int lines) { this.lines = lines; }
        public int lines { get; }
    }
    public sealed class NonReorderableAttribute : System.Attribute { }
    public sealed class RangeAttribute : System.Attribute {
        public RangeAttribute(float min, float max) { this.min = min; this.max = max; }
        public float min { get; }
        public float max { get; }
    }
    public sealed class SpaceAttribute : System.Attribute {
        public SpaceAttribute() { }
        public SpaceAttribute(float height) { this.height = height; }
        public float height { get; }
    }
    public sealed class SelectionBaseAttribute : System.Attribute { }
    public sealed class TextAreaAttribute : System.Attribute {
        public TextAreaAttribute() { }
        public TextAreaAttribute(int minLines, int maxLines) {
            this.minLines = minLines; this.maxLines = maxLines;
        }
        public int minLines { get; }
        public int maxLines { get; }
    }
    public sealed class TooltipAttribute : System.Attribute {
        public TooltipAttribute(string text) { tooltip = text; }
        public string tooltip { get; }
    }
}
namespace UnityEngine.Audio
{
    public class AudioMixer : UnityEngine.Object
    {
        public bool SetFloat(string name, float value) => false;
        public bool GetFloat(string name, out float value) { value = 0; return false; }
        public bool ClearFloat(string name) => false;
        public AudioMixerGroup[] FindMatchingGroups(string name) => [];
        public AudioMixerSnapshot FindSnapshot(string name) => null!;
    }
    public class AudioMixerSnapshot : UnityEngine.Object
    {
        public void TransitionTo(float timeToReach) { }
    }
    public class AudioMixerGroup : UnityEngine.Object
    {
        public AudioMixer audioMixer { get; } = null!;
    }
}
namespace UnityEngine.Assertions
{
    public sealed class AssertionException : System.Exception
    {
        public AssertionException(string message, string userMessage) : base(message) { }
    }
    public static class Assert
    {
        public static bool raiseExceptions { get; set; } = true;
        public static void IsTrue(bool condition) { }
        public static void IsTrue(bool condition, string message) { }
        public static void IsFalse(bool condition) { }
        public static void IsFalse(bool condition, string message) { }
        public static void IsNull<T>(T value) { }
        public static void IsNull<T>(T value, string message) { }
        public static void IsNull(UnityEngine.Object value, string message) { }
        public static void IsNotNull<T>(T value) { }
        public static void IsNotNull<T>(T value, string message) { }
        public static void IsNotNull(UnityEngine.Object value, string message) { }
        public static void AreApproximatelyEqual(float expected, float actual) { }
        public static void AreApproximatelyEqual(float expected, float actual, float tolerance) { }
        public static void AreApproximatelyEqual(float expected, float actual, string message) { }
        public static void AreApproximatelyEqual(float expected, float actual, float tolerance, string message) { }
        public static void AreNotApproximatelyEqual(float expected, float actual) { }
        public static void AreNotApproximatelyEqual(float expected, float actual, float tolerance) { }
        public static void AreNotApproximatelyEqual(float expected, float actual, string message) { }
        public static void AreNotApproximatelyEqual(float expected, float actual, float tolerance, string message) { }
        public static void AreEqual<T>(T expected, T actual) { }
        public static void AreEqual<T>(T expected, T actual, string message) { }
        public static void AreEqual<T>(T expected, T actual, string message, System.Collections.Generic.IEqualityComparer<T> comparer) { }
        public static void AreNotEqual<T>(T expected, T actual) { }
        public static void AreNotEqual<T>(T expected, T actual, string message) { }
        public static void AreNotEqual<T>(T expected, T actual, string message, System.Collections.Generic.IEqualityComparer<T> comparer) { }
        public static void AreEqual(UnityEngine.Object expected, UnityEngine.Object actual, string message) { }
        public static void AreNotEqual(UnityEngine.Object expected, UnityEngine.Object actual, string message) { }
        public static void AreEqual(byte expected, byte actual) { }
        public static void AreEqual(byte expected, byte actual, string message) { }
        public static void AreEqual(char expected, char actual) { }
        public static void AreEqual(char expected, char actual, string message) { }
        public static void AreEqual(int expected, int actual) { }
        public static void AreEqual(int expected, int actual, string message) { }
        public static void AreEqual(long expected, long actual) { }
        public static void AreEqual(long expected, long actual, string message) { }
        public static void AreEqual(sbyte expected, sbyte actual) { }
        public static void AreEqual(sbyte expected, sbyte actual, string message) { }
        public static void AreEqual(short expected, short actual) { }
        public static void AreEqual(short expected, short actual, string message) { }
        public static void AreEqual(uint expected, uint actual) { }
        public static void AreEqual(uint expected, uint actual, string message) { }
        public static void AreEqual(ulong expected, ulong actual) { }
        public static void AreEqual(ulong expected, ulong actual, string message) { }
        public static void AreEqual(ushort expected, ushort actual) { }
        public static void AreEqual(ushort expected, ushort actual, string message) { }
        public static void AreNotEqual(byte expected, byte actual) { }
        public static void AreNotEqual(byte expected, byte actual, string message) { }
        public static void AreNotEqual(char expected, char actual) { }
        public static void AreNotEqual(char expected, char actual, string message) { }
        public static void AreNotEqual(int expected, int actual) { }
        public static void AreNotEqual(int expected, int actual, string message) { }
        public static void AreNotEqual(long expected, long actual) { }
        public static void AreNotEqual(long expected, long actual, string message) { }
        public static void AreNotEqual(sbyte expected, sbyte actual) { }
        public static void AreNotEqual(sbyte expected, sbyte actual, string message) { }
        public static void AreNotEqual(short expected, short actual) { }
        public static void AreNotEqual(short expected, short actual, string message) { }
        public static void AreNotEqual(uint expected, uint actual) { }
        public static void AreNotEqual(uint expected, uint actual, string message) { }
        public static void AreNotEqual(ulong expected, ulong actual) { }
        public static void AreNotEqual(ulong expected, ulong actual, string message) { }
        public static void AreNotEqual(ushort expected, ushort actual) { }
        public static void AreNotEqual(ushort expected, ushort actual, string message) { }
    }
}

namespace UnityEngine.Assertions.Must
{
    public static class MustExtensions
    {
        public static void MustBeTrue(this bool value) { }
        public static void MustBeTrue(this bool value, string message) { }
        public static void MustBeFalse(this bool value) { }
        public static void MustBeFalse(this bool value, string message) { }
        public static void MustBeEqual<T>(this T value, T expected) { }
        public static void MustBeEqual<T>(this T value, T expected, string message) { }
        public static void MustNotBeEqual<T>(this T value, T expected) { }
        public static void MustNotBeEqual<T>(this T value, T expected, string message) { }
        public static void MustBeNull<T>(this T value) { }
        public static void MustBeNull<T>(this T value, string message) { }
        public static void MustNotBeNull<T>(this T value) { }
        public static void MustNotBeNull<T>(this T value, string message) { }
        public static void MustBeApproximatelyEqual(this float value, float expected) { }
        public static void MustBeApproximatelyEqual(this float value, float expected, float tolerance) { }
        public static void MustBeApproximatelyEqual(this float value, float expected, string message) { }
        public static void MustBeApproximatelyEqual(this float value, float expected, float tolerance, string message) { }
        public static void MustNotBeApproximatelyEqual(this float value, float expected) { }
        public static void MustNotBeApproximatelyEqual(this float value, float expected, float tolerance) { }
        public static void MustNotBeApproximatelyEqual(this float value, float expected, string message) { }
        public static void MustNotBeApproximatelyEqual(this float value, float expected, float tolerance, string message) { }
    }
}

namespace UnityEngine.Assertions.Comparers
{
    public sealed class FloatComparer
    {
        public const float kEpsilon = 0.00001f;
        public static FloatComparer s_ComparerWithDefaultTolerance = new();
        public FloatComparer() { }
        public FloatComparer(bool relative) { }
        public FloatComparer(float tolerance) { }
        public FloatComparer(float tolerance, bool relative) { }
        public static bool AreEqual(float left, float right, float tolerance) => false;
        public static bool AreEqualRelative(float left, float right, float tolerance) => false;
        public bool Equals(float left, float right) => false;
        public int GetHashCode(float value) => 0;
    }
}

namespace UnityEngine.Serialization
{
    public class UnitySurrogateSelector : System.Runtime.Serialization.ISurrogateSelector
    {
        public System.Runtime.Serialization.ISerializationSurrogate GetSurrogate(System.Type type,
            System.Runtime.Serialization.StreamingContext context,
            out System.Runtime.Serialization.ISurrogateSelector selector)
        { selector = null!; return null!; }
        public void ChainSelector(System.Runtime.Serialization.ISurrogateSelector selector) { }
        public System.Runtime.Serialization.ISurrogateSelector GetNextSelector() => null!;
    }
    public class ManagedReferenceUtility
    {
        public const long RefIdNull = -2;
        public const long RefIdUnknown = -1;
        public static object GetManagedReference(UnityEngine.Object host, long referenceId) => null!;
        public static long GetManagedReferenceIdForObject(UnityEngine.Object host, object value) => -1;
        public static long[] GetManagedReferenceIds(UnityEngine.Object host) => [];
        public static void SetManagedReferenceIdForObject(UnityEngine.Object host, object value, long referenceId) { }
    }
    [System.AttributeUsage(System.AttributeTargets.Field, AllowMultiple = true)]
    public sealed class FormerlySerializedAsAttribute : System.Attribute
    {
        public string oldName { get; }
        public FormerlySerializedAsAttribute(string oldName) { this.oldName = oldName; }
    }
}
namespace UnityEngine.SceneManagement
{
    public class SceneManagerAPI
    {
        public static SceneManagerAPI overrideAPI { get; set; }
        protected SceneManagerAPI() { }
        protected virtual int GetNumScenesInBuildSettings() => default;
        protected virtual Scene GetSceneByBuildIndex(int index) => default;
        protected virtual UnityEngine.AsyncOperation LoadSceneAsyncByNameOrIndex(
            string sceneName, int sceneBuildIndex, LoadSceneParameters parameters, bool mustCompleteNextFrame) => default;
        protected virtual UnityEngine.AsyncOperation UnloadSceneAsyncByNameOrIndex(
            string sceneName, int sceneBuildIndex, bool immediately, UnloadSceneOptions options,
            out bool outSuccess) { outSuccess = false; return default; }
    }
    public static class SceneUtility
    {
        public static int GetBuildIndexByScenePath(string path) => default;
        public static string GetScenePathByBuildIndex(int index) => default;
    }
    public enum LoadSceneMode { Single = 0, Additive = 1 }
    public enum LocalPhysicsMode { None = 0, Physics2D = 1, Physics3D = 2 }
    public enum UnloadSceneOptions { None = 0, UnloadAllEmbeddedSceneObjects = 1 }
    public struct LoadSceneParameters
    {
        public LoadSceneMode loadSceneMode { get; set; }
        public LocalPhysicsMode localPhysicsMode { get; set; }
        public LoadSceneParameters(LoadSceneMode mode) { loadSceneMode = mode; localPhysicsMode = LocalPhysicsMode.None; }
        public LoadSceneParameters(LoadSceneMode mode, LocalPhysicsMode physicsMode)
        { loadSceneMode = mode; localPhysicsMode = physicsMode; }
    }
    public struct CreateSceneParameters
    {
        public LocalPhysicsMode localPhysicsMode { get; set; }
        public CreateSceneParameters(LocalPhysicsMode mode) { localPhysicsMode = mode; }
    }
    public struct Scene
    {
        public bool isDirty { get; }
        public int handle { get; }
        public int buildIndex { get; }
        public string path { get; }
        public bool isSubScene { get; }
        public string name { get; }
        public bool isLoaded { get; }
        public int rootCount { get; }
        public bool IsValid() => default;
        public UnityEngine.GameObject[] GetRootGameObjects() => default;
        public void GetRootGameObjects(System.Collections.Generic.List<UnityEngine.GameObject> roots) { }
        public static bool operator ==(Scene left, Scene right) => default;
        public static bool operator !=(Scene left, Scene right) => default;
        public override bool Equals(object? value) => default;
        public override int GetHashCode() => default;
    }
    public struct SceneHandle
    {
        public static SceneHandle None => default;
        public bool Equals(SceneHandle other) => false;
        public override bool Equals(object? other) => false;
        public override int GetHashCode() => 0;
        public override string ToString() => "";
        public string ToString(string format) => "";
        public static implicit operator SceneHandle(int value) => default;
        public static implicit operator SceneHandle(uint value) => default;
        public static implicit operator int(SceneHandle value) => default;
        public static implicit operator uint(SceneHandle value) => default;
        public static bool operator ==(SceneHandle left, SceneHandle right) => false;
        public static bool operator !=(SceneHandle left, SceneHandle right) => false;
    }
    public class SceneManager
    {
        public static event System.Action<Scene, LoadSceneMode>? sceneLoaded;
        public static event System.Action<Scene>? sceneUnloaded;
        public static event System.Action<Scene, Scene>? activeSceneChanged;
        public static int sceneCount { get; }
        public static int loadedSceneCount { get; }
        public static int sceneCountInBuildSettings { get; }
        public static Scene GetActiveScene() => default;
        public static Scene[] GetAllScenes() => default;
        public static Scene GetSceneAt(int index) => default;
        public static Scene GetSceneByName(string name) => default;
        public static Scene GetSceneByPath(string path) => default;
        public static Scene GetSceneByBuildIndex(int index) => default;
        public static void LoadScene(int buildIndex) { }
        public static void LoadScene(string sceneName) { }
        public static void LoadScene(int buildIndex, LoadSceneMode mode) { }
        public static void LoadScene(string sceneName, LoadSceneMode mode) { }
        public static Scene LoadScene(int buildIndex, LoadSceneParameters parameters) => default;
        public static Scene LoadScene(string sceneName, LoadSceneParameters parameters) => default;
        public static UnityEngine.AsyncOperation LoadSceneAsync(int buildIndex) => default;
        public static UnityEngine.AsyncOperation LoadSceneAsync(string sceneName) => default;
        public static UnityEngine.AsyncOperation LoadSceneAsync(int buildIndex, LoadSceneMode mode) => default;
        public static UnityEngine.AsyncOperation LoadSceneAsync(string sceneName, LoadSceneMode mode) => default;
        public static UnityEngine.AsyncOperation LoadSceneAsync(int buildIndex, LoadSceneParameters parameters) => default;
        public static UnityEngine.AsyncOperation LoadSceneAsync(string sceneName, LoadSceneParameters parameters) => default;
        public static Scene CreateScene(string name) => default;
        public static Scene CreateScene(string name, CreateSceneParameters parameters) => default;
        public static bool SetActiveScene(Scene scene) => false;
        public static void MoveGameObjectToScene(UnityEngine.GameObject gameObject, Scene scene) { }
        public static void MoveGameObjectsToScene(Unity.Collections.NativeArray<int> ids, Scene scene) { }
        public static void MoveGameObjectsToScene(Unity.Collections.NativeArray<UnityEngine.EntityId> ids, Scene scene) { }
        public static void MergeScenes(Scene source, Scene destination) { }
        public static bool UnloadScene(Scene scene) => false;
        public static bool UnloadScene(int buildIndex) => false;
        public static bool UnloadScene(string sceneName) => false;
        public static UnityEngine.AsyncOperation UnloadSceneAsync(Scene scene) => default;
        public static UnityEngine.AsyncOperation UnloadSceneAsync(int buildIndex) => default;
        public static UnityEngine.AsyncOperation UnloadSceneAsync(string sceneName) => default;
        public static UnityEngine.AsyncOperation UnloadSceneAsync(Scene scene, UnloadSceneOptions options) => default;
        public static UnityEngine.AsyncOperation UnloadSceneAsync(int buildIndex, UnloadSceneOptions options) => default;
        public static UnityEngine.AsyncOperation UnloadSceneAsync(string sceneName, UnloadSceneOptions options) => default;
    }
}

namespace Unity.IntegerTime
{
    public struct RationalTime
    {
        public long Count { get; set; }
        public TicksPerSecond Ticks { get; set; }
        public struct TicksPerSecond { }
        public RationalTime(long count, TicksPerSecond ticks) { Count = count; Ticks = ticks; }
    }
}

namespace UnityEngine.Events
{
    public abstract class UnityEventBase
    {
        public static System.Reflection.MethodInfo GetValidMethodInfo(System.Type type,
            string name, System.Type[] argumentTypes) => null;
        public static System.Reflection.MethodInfo GetValidMethodInfo(object target,
            string name, System.Type[] argumentTypes) => null;
        public override string ToString() => base.ToString();
        public int GetPersistentEventCount() => 0;
        public string GetPersistentMethodName(int index) => null;
        public UnityEngine.Object GetPersistentTarget(int index) => null;
        public PersistentListenerMode GetPersistentListenerState(int index) => PersistentListenerMode.EventDefined;
        public void SetPersistentListenerState(int index, UnityEventCallState state) { }
        public void RemoveAllListeners() { }
    }

    public delegate void UnityAction();
    public delegate void UnityAction<T0>(T0 arg0);
    public delegate void UnityAction<T0, T1>(T0 arg0, T1 arg1);
    public delegate void UnityAction<T0, T1, T2>(T0 arg0, T1 arg1, T2 arg2);
    public delegate void UnityAction<T0, T1, T2, T3>(T0 arg0, T1 arg1, T2 arg2, T3 arg3);

    public class UnityEvent : UnityEventBase
    {
        public void AddListener(UnityAction listener) { }
        public void RemoveListener(UnityAction listener) { }
        public void Invoke() { }
    }
    public class UnityEvent<T0> : UnityEventBase
    {
        public void AddListener(UnityAction<T0> listener) { }
        public void RemoveListener(UnityAction<T0> listener) { }
        public void Invoke(T0 arg0) { }
    }
    public class UnityEvent<T0, T1> : UnityEventBase
    {
        public void AddListener(UnityAction<T0, T1> listener) { }
        public void RemoveListener(UnityAction<T0, T1> listener) { }
        public void Invoke(T0 arg0, T1 arg1) { }
    }
    public class UnityEvent<T0, T1, T2> : UnityEventBase
    {
        public void AddListener(UnityAction<T0, T1, T2> listener) { }
        public void RemoveListener(UnityAction<T0, T1, T2> listener) { }
        public void Invoke(T0 arg0, T1 arg1, T2 arg2) { }
    }
    public class UnityEvent<T0, T1, T2, T3> : UnityEventBase
    {
        public void AddListener(UnityAction<T0, T1, T2, T3> listener) { }
        public void RemoveListener(UnityAction<T0, T1, T2, T3> listener) { }
        public void Invoke(T0 arg0, T1 arg1, T2 arg2, T3 arg3) { }
    }

    public enum PersistentListenerMode
    {
        EventDefined = 0, Void = 1, Object = 2, Int = 3, Float = 4, String = 5, Bool = 6
    }

    public enum UnityEventCallState { Off = 0, EditorAndRuntime = 1, RuntimeOnly = 2 }
}

namespace UnityEngine.Pool
{
    public interface IObjectPool<T> where T : class
    {
        int CountInactive { get; }
        T Get();
        PooledObject<T> Get(out T value);
        void Release(T value);
        void Clear();
    }
    public struct PooledObject<T> : System.IDisposable where T : class
    {
        public PooledObject(T value, IObjectPool<T> pool) { }
        public void Dispose() { }
    }
    public class ObjectPool<T> : IObjectPool<T>, System.IDisposable where T : class
    {
        public ObjectPool(System.Func<T> createFunc,
            System.Action<T>? actionOnGet = null,
            System.Action<T>? actionOnRelease = null,
            System.Action<T>? actionOnDestroy = null,
            bool collectionCheck = true, int defaultCapacity = 10, int maxSize = 10000) { }
        public int CountAll => 0;
        public int CountActive => 0;
        public int CountInactive => 0;
        public T Get() => default!;
        public PooledObject<T> Get(out T value) { value = default!; return default; }
        public void Release(T value) { }
        public void Clear() { }
        public void Dispose() { }
    }
    public class LinkedPool<T> : IObjectPool<T>, System.IDisposable where T : class
    {
        public LinkedPool(System.Func<T> createFunc,
            System.Action<T>? actionOnGet = null,
            System.Action<T>? actionOnRelease = null,
            System.Action<T>? actionOnDestroy = null,
            bool collectionCheck = true, int maxSize = 10000) { }
        public int CountInactive => 0;
        public T Get() => default!;
        public PooledObject<T> Get(out T value) { value = default!; return default; }
        public void Release(T value) { }
        public void Clear() { }
        public void Dispose() { }
    }
    public class CollectionPool<TCollection, TItem>
        where TCollection : class, System.Collections.Generic.ICollection<TItem>, new()
    {
        public static TCollection Get() => default!;
        public static PooledObject<TCollection> Get(out TCollection value) { value = default!; return default; }
        public static void Release(TCollection value) { }
    }
    public class ListPool<T> : CollectionPool<System.Collections.Generic.List<T>, T> { }
    public class HashSetPool<T> : CollectionPool<System.Collections.Generic.HashSet<T>, T> { }
    public class DictionaryPool<TKey, TValue> : CollectionPool<System.Collections.Generic.Dictionary<TKey, TValue>, System.Collections.Generic.KeyValuePair<TKey, TValue>> { }
    public class GenericPool<T> where T : class, new()
    {
        public static T Get() => default!;
        public static PooledObject<T> Get(out T value) { value = default!; return default; }
        public static void Release(T value) { }
    }
    public static class UnsafeGenericPool<T> where T : class, new()
    {
        public static T Get() => default!;
        public static PooledObject<T> Get(out T value) { value = default!; return default; }
        public static void Release(T value) { }
    }
}

namespace UnityEngine.Playables
{
    public interface IPlayable { PlayableHandle GetHandle(); }
    public interface IPlayableOutput { PlayableOutputHandle GetHandle(); }
    public interface INotification { UnityEngine.PropertyName id { get; } }
    public interface INotificationReceiver
    {
        void OnNotify(Playable origin, INotification notification, object context);
    }
    public class Notification : INotification
    {
        public Notification(string name) { }
        public UnityEngine.PropertyName id => default;
    }
    public struct PlayableBinding
    {
        public static double DefaultDuration => double.PositiveInfinity;
        public static PlayableBinding[] None => System.Array.Empty<PlayableBinding>();
        public string streamName => "";
        public UnityEngine.Object sourceObject => null;
        public System.Type outputTargetType => default!;
    }
    public static class ScriptPlayableBinding
    {
        public static PlayableBinding Create(string name, UnityEngine.Object source, System.Type type) => default;
    }
    public interface IPlayableAsset
    {
        Playable CreatePlayable(PlayableGraph graph, UnityEngine.GameObject owner);
        double duration { get; }
        System.Collections.Generic.IEnumerable<PlayableBinding> outputs { get; }
    }
    public abstract class PlayableAsset : UnityEngine.ScriptableObject, IPlayableAsset
    {
        public virtual double duration => double.PositiveInfinity;
        public virtual System.Collections.Generic.IEnumerable<PlayableBinding> outputs
            => System.Array.Empty<PlayableBinding>();
        public abstract Playable CreatePlayable(PlayableGraph graph, UnityEngine.GameObject owner);
    }
    public struct Playable : IPlayable
    {
        public static Playable Create(PlayableGraph graph, int inputCount = 0) => default;
        public static Playable Null => default;
        public bool Equals(Playable other) => false;
        public PlayableHandle GetHandle() => default;
        public System.Type GetPlayableType() => default!;
        public bool IsPlayableOfType<T>() where T : struct, IPlayable => false;
    }
    public struct PlayableHandle
    {
        public static PlayableHandle Null => default;
        public bool Equals(PlayableHandle other) => false;
        public override bool Equals(object other) => false;
        public override int GetHashCode() => 0;
        public static bool operator ==(PlayableHandle left, PlayableHandle right) => false;
        public static bool operator !=(PlayableHandle left, PlayableHandle right) => false;
    }
    public struct PlayableOutput : IPlayableOutput
    {
        public static PlayableOutput Null => default;
        public bool Equals(PlayableOutput other) => false;
        public PlayableOutputHandle GetHandle() => default;
        public System.Type GetPlayableOutputType() => default!;
        public bool IsPlayableOutputOfType<T>() where T : struct, IPlayableOutput => false;
    }
    public struct PlayableOutputHandle
    {
        public static PlayableOutputHandle Null => default;
        public bool Equals(PlayableOutputHandle other) => false;
        public override bool Equals(object other) => false;
        public override int GetHashCode() => 0;
        public static bool operator ==(PlayableOutputHandle left, PlayableOutputHandle right) => false;
        public static bool operator !=(PlayableOutputHandle left, PlayableOutputHandle right) => false;
    }
    public partial struct FrameData
    {
        public float deltaTime => 0;
        public double effectiveParentDelay => 0;
        public float effectiveParentSpeed => 1;
        public PlayState effectivePlayState => PlayState.Playing;
        public float effectiveSpeed => 1;
        public float effectiveWeight => 1;
        public EvaluationType evaluationType => EvaluationType.Playback;
        public ulong frameId => 0;
        public PlayableOutput output => default;
        public bool seekOccurred => false;
        public bool timeHeld => false;
        public bool timeLooped => false;
        public float weight => 1;
    }
    public interface IPlayableBehaviour
    {
        void OnBehaviourPause(Playable playable, FrameData info);
        void OnBehaviourPlay(Playable playable, FrameData info);
        void OnGraphStart(Playable playable);
        void OnGraphStop(Playable playable);
        void OnPlayableCreate(Playable playable);
        void OnPlayableDestroy(Playable playable);
        void PrepareFrame(Playable playable, FrameData info);
        void ProcessFrame(Playable playable, FrameData info, object playerData);
    }
    public class PlayableBehaviour : IPlayableBehaviour
    {
        public PlayableBehaviour() { }
        public virtual object Clone() => MemberwiseClone();
        public virtual void OnBehaviourDelay(Playable playable, FrameData info) { }
        public virtual void OnBehaviourPause(Playable playable, FrameData info) { }
        public virtual void OnBehaviourPlay(Playable playable, FrameData info) { }
        public virtual void OnGraphStart(Playable playable) { }
        public virtual void OnGraphStop(Playable playable) { }
        public virtual void OnPlayableCreate(Playable playable) { }
        public virtual void OnPlayableDestroy(Playable playable) { }
        public virtual void PrepareData(Playable playable, FrameData info) { }
        public virtual void PrepareFrame(Playable playable, FrameData info) { }
        public virtual void ProcessFrame(Playable playable, FrameData info, object playerData) { }
    }
    public struct ScriptPlayable<T> : IPlayable where T : class, IPlayableBehaviour, new()
    {
        public static ScriptPlayable<T> Create(PlayableGraph graph, int inputCount = 0) => default;
        public static ScriptPlayable<T> Create(PlayableGraph graph, T template, int inputCount = 0) => default;
        public static ScriptPlayable<T> Null => default;
        public bool Equals(ScriptPlayable<T> other) => false;
        public T GetBehaviour() => default!;
        public PlayableHandle GetHandle() => default;
        public static explicit operator ScriptPlayable<T>(Playable playable) => default;
        public static implicit operator Playable(ScriptPlayable<T> playable) => default;
    }
    public struct ScriptPlayableOutput : IPlayableOutput
    {
        public static ScriptPlayableOutput Create(PlayableGraph graph, string name) => default;
        public static ScriptPlayableOutput Null => default;
        public PlayableOutputHandle GetHandle() => default;
        public static explicit operator ScriptPlayableOutput(PlayableOutput output) => default;
        public static implicit operator PlayableOutput(ScriptPlayableOutput output) => default;
    }
    public static class PlayableExtensions
    {
        public static int AddInput<U, V>(this U playable, V input, int sourceOutputPort, float weight)
            where U : struct, IPlayable where V : struct, IPlayable => 0;
        public static void ConnectInput<U, V>(this U playable, int inputPort, V input, int sourceOutputPort)
            where U : struct, IPlayable where V : struct, IPlayable { }
        public static void ConnectInput<U, V>(this U playable, int inputPort, V input, int sourceOutputPort, float weight)
            where U : struct, IPlayable where V : struct, IPlayable { }
        public static void Destroy<U>(this U playable) where U : struct, IPlayable { }
        public static void DisconnectInput<U>(this U playable, int inputPort) where U : struct, IPlayable { }
        public static Playable GetInput<U>(this U playable, int inputPort) where U : struct, IPlayable => default;
        public static float GetInputWeight<U>(this U playable, int inputPort) where U : struct, IPlayable => 0;
        public static Playable GetOutput<U>(this U playable, int outputPort) where U : struct, IPlayable => default;
        public static void SetInputCount<U>(this U playable, int count) where U : struct, IPlayable { }
        public static void SetInputWeight<U>(this U playable, int inputPort, float weight) where U : struct, IPlayable { }
        public static void SetInputWeight<U, V>(this U playable, V input, float weight)
            where U : struct, IPlayable where V : struct, IPlayable { }
        public static void SetOutputCount<U>(this U playable, int count) where U : struct, IPlayable { }
        public static bool IsValid<U>(this U playable) where U : struct, IPlayable => false;
        public static bool IsNull<U>(this U playable) where U : struct, IPlayable => false;
        public static bool CanChangeInputs<U>(this U playable) where U : struct, IPlayable => false;
        public static bool CanDestroy<U>(this U playable) where U : struct, IPlayable => false;
        public static bool CanSetWeights<U>(this U playable) where U : struct, IPlayable => false;
        public static PlayableGraph GetGraph<U>(this U playable) where U : struct, IPlayable => default;
        public static int GetInputCount<U>(this U playable) where U : struct, IPlayable => 0;
        public static int GetOutputCount<U>(this U playable) where U : struct, IPlayable => 0;
        public static double GetTime<U>(this U playable) where U : struct, IPlayable => 0;
        public static void SetTime<U>(this U playable, double time) where U : struct, IPlayable { }
        public static double GetPreviousTime<U>(this U playable) where U : struct, IPlayable => 0;
        public static double GetSpeed<U>(this U playable) where U : struct, IPlayable => 1;
        public static void SetSpeed<U>(this U playable, double speed) where U : struct, IPlayable { }
        public static double GetDuration<U>(this U playable) where U : struct, IPlayable => 0;
        public static void SetDuration<U>(this U playable, double duration) where U : struct, IPlayable { }
        public static double GetDelay<U>(this U playable) where U : struct, IPlayable => 0;
        public static void SetDelay<U>(this U playable, double delay) where U : struct, IPlayable { }
        public static float GetLeadTime<U>(this U playable) where U : struct, IPlayable => 0;
        public static void SetLeadTime<U>(this U playable, float leadTime) where U : struct, IPlayable { }
        public static PlayState GetPlayState<U>(this U playable) where U : struct, IPlayable => PlayState.Playing;
        public static void SetPlayState<U>(this U playable, PlayState state) where U : struct, IPlayable { }
        public static void Play<U>(this U playable) where U : struct, IPlayable { }
        public static void Pause<U>(this U playable) where U : struct, IPlayable { }
        public static PlayableTraversalMode GetTraversalMode<U>(this U playable) where U : struct, IPlayable => PlayableTraversalMode.Mix;
        public static void SetTraversalMode<U>(this U playable, PlayableTraversalMode mode) where U : struct, IPlayable { }
        public static bool GetPropagateSetTime<U>(this U playable) where U : struct, IPlayable => false;
        public static void SetPropagateSetTime<U>(this U playable, bool value) where U : struct, IPlayable { }
        public static bool IsDelayed<U>(this U playable) where U : struct, IPlayable => false;
        public static bool IsDone<U>(this U playable) where U : struct, IPlayable => false;
        public static void SetDone<U>(this U playable, bool value) where U : struct, IPlayable { }
    }
    public static class PlayableOutputExtensions
    {
        public static void AddNotificationReceiver<U>(this U output, INotificationReceiver receiver)
            where U : struct, IPlayableOutput { }
        public static System.Collections.Generic.IEnumerable<INotificationReceiver> GetNotificationReceivers<U>(this U output)
            where U : struct, IPlayableOutput => default!;
        public static void PushNotification<U>(this U output, Playable origin, INotification notification, object context)
            where U : struct, IPlayableOutput { }
        public static void RemoveNotificationReceiver<U>(this U output, INotificationReceiver receiver)
            where U : struct, IPlayableOutput { }
        public static bool IsOutputValid<U>(this U output) where U : struct, IPlayableOutput => false;
        public static bool IsOutputNull<U>(this U output) where U : struct, IPlayableOutput => false;
        public static float GetWeight<U>(this U output) where U : struct, IPlayableOutput => 1;
        public static void SetWeight<U>(this U output, float weight) where U : struct, IPlayableOutput { }
        public static UnityEngine.Object GetReferenceObject<U>(this U output) where U : struct, IPlayableOutput => null;
        public static void SetReferenceObject<U>(this U output, UnityEngine.Object value) where U : struct, IPlayableOutput { }
        public static UnityEngine.Object GetUserData<U>(this U output) where U : struct, IPlayableOutput => null;
        public static void SetUserData<U>(this U output, UnityEngine.Object value) where U : struct, IPlayableOutput { }
        public static int GetSourceInputPort<U>(this U output) where U : struct, IPlayableOutput => 0;
        public static void SetSourceInputPort<U>(this U output, int port) where U : struct, IPlayableOutput { }
        public static int GetSourceOutputPort<U>(this U output) where U : struct, IPlayableOutput => 0;
        public static void SetSourceOutputPort<U>(this U output, int port) where U : struct, IPlayableOutput { }
        public static void SetSourcePlayable<U, V>(this U output, V playable)
            where U : struct, IPlayableOutput where V : struct, IPlayable { }
        public static void SetSourcePlayable<U, V>(this U output, V playable, int sourceOutputPort)
            where U : struct, IPlayableOutput where V : struct, IPlayable { }
        public static Playable GetSourcePlayable<U>(this U output)
            where U : struct, IPlayableOutput => default;
    }
    public struct PlayableGraph
    {
        public static PlayableGraph Create() => default;
        public static PlayableGraph Create(string name) => default;
        public bool IsValid() => false;
        public bool IsPlaying() => false;
        public bool IsDone() => false;
        public string GetEditorName() => "";
        public DirectorUpdateMode GetTimeUpdateMode() => DirectorUpdateMode.DSPClock;
        public void SetTimeUpdateMode(DirectorUpdateMode mode) { }
        public int GetPlayableCount() => 0;
        public int GetRootPlayableCount() => 0;
        public int GetOutputCount() => 0;
        public Playable GetRootPlayable(int index) => default;
        public PlayableOutput GetOutput(int index) => default;
        public T GetOutputByType<T>(int index) where T : struct, IPlayableOutput => default;
        public int GetOutputCountByType<T>() where T : struct, IPlayableOutput => 0;
        public bool Connect<U, V>(U source, int sourceOutputPort, V destination, int destinationInputPort)
            where U : struct, IPlayable where V : struct, IPlayable => false;
        public void Disconnect<U>(U destination, int destinationInputPort)
            where U : struct, IPlayable { }
        public void DestroyPlayable<U>(U playable) where U : struct, IPlayable { }
        public void DestroySubgraph<U>(U playable) where U : struct, IPlayable { }
        public void DestroyOutput<U>(U output) where U : struct, IPlayableOutput { }
        public UnityEngine.IExposedPropertyTable GetResolver() => null;
        public void SetResolver(UnityEngine.IExposedPropertyTable resolver) { }
        public void Play() { }
        public void Stop() { }
        public void Evaluate() { }
        public void Evaluate(float deltaTime) { }
        public void Destroy() { }
    }
}

namespace UnityEngine.Animations
{
    public struct AnimationClipPlayable : UnityEngine.Playables.IPlayable
    {
        public static AnimationClipPlayable Create(UnityEngine.Playables.PlayableGraph graph,
            UnityEngine.AnimationClip clip) => default;
        public void SetApplyFootIK(bool value) { }
        public UnityEngine.Playables.PlayableHandle GetHandle() => default;
    }
    public struct AnimationPlayableOutput : UnityEngine.Playables.IPlayableOutput
    {
        public static AnimationPlayableOutput Create(UnityEngine.Playables.PlayableGraph graph,
            string name, UnityEngine.Animator animator) => default;
        public UnityEngine.Playables.PlayableOutputHandle GetHandle() => default;
    }
}

namespace Unity.Collections
{
    public enum Allocator { Invalid = 0, None = 1, Temp = 2, TempJob = 3, Persistent = 4 }
    public sealed class NativeArray<T> : System.IDisposable
    {
        private readonly T[] values;
        public NativeArray(int length, Allocator allocator) { values = new T[length]; }
        public NativeArray(T[] source, Allocator allocator) { values = (T[])source.Clone(); }
        public int Length => values.Length;
        public T this[int index] { get => values[index]; set => values[index] = value; }
        public void Dispose() { }
    }
    public struct NativeSlice<T>
    {
        private readonly NativeArray<T> source;
        private readonly int start;
        public NativeSlice(NativeArray<T> source) { this.source = source; start = 0; Length = source.Length; }
        public NativeSlice(NativeArray<T> source, int start, int length)
        { this.source = source; this.start = start; Length = length; }
        public int Length { get; }
        public T this[int index] { get => source[start + index]; set => source[start + index] = value; }
    }
}

namespace UnityEngine.Device
{
    public static class Application
    {
        public static bool isPlaying => UnityEngine.Application.isPlaying;
        public static bool IsPlaying(UnityEngine.Object value) => UnityEngine.Application.IsPlaying(value);
        public static bool isEditor => UnityEngine.Application.isEditor;
        public static bool isBatchMode => UnityEngine.Application.isBatchMode;
        public static bool isMobilePlatform => UnityEngine.Application.isMobilePlatform;
        public static UnityEngine.RuntimePlatform platform => UnityEngine.Application.platform;
        public static string persistentDataPath => UnityEngine.Application.persistentDataPath;
        public static string absoluteURL => UnityEngine.Application.absoluteURL;
        public static string dataPath => UnityEngine.Application.dataPath;
        public static string streamingAssetsPath => UnityEngine.Application.streamingAssetsPath;
        public static string temporaryCachePath => UnityEngine.Application.temporaryCachePath;
        public static UnityEngine.NetworkReachability internetReachability => UnityEngine.Application.internetReachability;
        public static bool isConsolePlatform => UnityEngine.Application.isConsolePlatform;
        public static bool isFocused => UnityEngine.Application.isFocused;
        public static UnityEngine.SystemLanguage systemLanguage => UnityEngine.Application.systemLanguage;
        public static bool runInBackground { get => UnityEngine.Application.runInBackground;
            set => UnityEngine.Application.runInBackground = value; }
        public static string companyName => UnityEngine.Application.companyName;
        public static string productName => UnityEngine.Application.productName;
        public static string identifier => UnityEngine.Application.identifier;
        public static string version => UnityEngine.Application.version;
        public static string unityVersion => UnityEngine.Application.unityVersion;
        public static bool CanStreamedLevelBeLoaded(int index)
            => UnityEngine.Application.CanStreamedLevelBeLoaded(index);
        public static bool CanStreamedLevelBeLoaded(string name)
            => UnityEngine.Application.CanStreamedLevelBeLoaded(name);
        public static string[] GetBuildTags() => UnityEngine.Application.GetBuildTags();
        public static void SetBuildTags(string[] tags) => UnityEngine.Application.SetBuildTags(tags);
        public static UnityEngine.StackTraceLogType GetStackTraceLogType(UnityEngine.LogType type)
            => UnityEngine.Application.GetStackTraceLogType(type);
        public static void SetStackTraceLogType(UnityEngine.LogType type,
            UnityEngine.StackTraceLogType stackTraceType)
            => UnityEngine.Application.SetStackTraceLogType(type, stackTraceType);
        public static int targetFrameRate { get => UnityEngine.Application.targetFrameRate;
            set => UnityEngine.Application.targetFrameRate = value; }
        public static void Quit() => UnityEngine.Application.Quit();
        public static void Quit(int exitCode) => UnityEngine.Application.Quit(exitCode);
        public static void OpenURL(string url) => UnityEngine.Application.OpenURL(url);
        public static event System.Func<bool>? wantsToQuit;
        public static event System.Action? quitting;
        public static event UnityEngine.Application.LogCallback? logMessageReceived;
        public static event UnityEngine.Application.LogCallback? logMessageReceivedThreaded;
        public static event System.Action<bool>? focusChanged;
    }
    public static class Screen
    {
        public static int width => UnityEngine.Screen.width;
        public static int height => UnityEngine.Screen.height;
        public static UnityEngine.Resolution currentResolution => UnityEngine.Screen.currentResolution;
        public static UnityEngine.Resolution[] resolutions => UnityEngine.Screen.resolutions;
        public static bool fullScreen => UnityEngine.Screen.fullScreen;
        public static UnityEngine.FullScreenMode fullScreenMode => UnityEngine.Screen.fullScreenMode;
        public static UnityEngine.Rect safeArea => UnityEngine.Screen.safeArea;
        public static UnityEngine.Rect[] cutouts => UnityEngine.Screen.cutouts;
        public static void SetResolution(int width, int height, bool fullScreen) { }
        public static void SetResolution(int width, int height, bool fullScreen, int preferredRefreshRate) { }
        public static void SetResolution(int width, int height,
            UnityEngine.FullScreenMode fullScreenMode) { }
        public static void SetResolution(int width, int height,
            UnityEngine.FullScreenMode fullScreenMode, int preferredRefreshRate) { }
        public static void SetResolution(int width, int height,
            UnityEngine.FullScreenMode fullScreenMode,
            UnityEngine.RefreshRate preferredRefreshRate) { }
    }
    public static class SystemInfo
    {
        public static string unsupportedIdentifier => UnityEngine.SystemInfo.unsupportedIdentifier;
        public static string deviceModel => UnityEngine.SystemInfo.deviceModel;
        public static UnityEngine.DeviceType deviceType => UnityEngine.SystemInfo.deviceType;
    }
}

namespace UnityEngine.Sprites
{
    public class DataUtility
    {
        private DataUtility() { }
        public static UnityEngine.Vector4 GetInnerUV(UnityEngine.Sprite sprite) => default;
        public static UnityEngine.Vector4 GetOuterUV(UnityEngine.Sprite sprite) => default;
        public static UnityEngine.Vector4 GetPadding(UnityEngine.Sprite sprite) => default;
        public static UnityEngine.Vector2 GetMinSize(UnityEngine.Sprite sprite) => default;
    }
}

namespace UnityEngine.Profiling
{
    public static class Profiler
    {
        public static void BeginSample(string name) { }
        public static void BeginSample(string name, UnityEngine.Object targetObject) { }
        public static void EndSample() { }
    }
}

namespace UnityEngine.InputSystem.EnhancedTouch
{
    public static class EnhancedTouchSupport
    {
        public static bool enabled => false;
        public static void Enable() { }
        public static void Disable() { }
    }
    public class Touch
    {
        public static Touch[] activeTouches => default;
        public static Finger[] fingers => default;
        public static Finger[] activeFingers => default;
        public bool valid => false;
        public int touchId => 0;
        public UnityEngine.InputSystem.TouchPhase phase => default;
        public bool began => false;
        public bool ended => false;
        public bool inProgress => false;
        public UnityEngine.Vector2 screenPosition => default;
        public UnityEngine.Vector2 delta => default;
        public float pressure => 0;
        public Finger finger => default;
        public Touch[] history => default;
    }
    public class Finger
    {
        public int index => 0;
        public bool isActive => false;
        public Touch currentTouch => default;
        public Touch[] history => default;
    }
}

namespace UnityEngine.InputSystem
{
    public enum TouchPhase { None = 0, Began = 1, Moved = 2, Ended = 3, Canceled = 4, Stationary = 5 }
    public enum InputActionChange { ActionEnabled, ActionDisabled, ActionMapEnabled,
        ActionMapDisabled, ActionStarted, ActionPerformed, ActionCanceled }
    public enum InputDeviceChange { Added, Removed, Disconnected, Reconnected }
    public class InputDevice { }
    public class InputControl { public InputDevice device => default; }
    public static class InputSystem
    {
        public static event System.Action<object, InputActionChange> onActionChange;
        public static event System.Action<InputDevice, InputDeviceChange> onDeviceChange;
        public static event System.Action<LowLevel.InputEventPtr, InputDevice> onEvent;
    }
    public class PlayerInput : UnityEngine.MonoBehaviour
    {
        public InputActionAsset actions { get; set; }
        public string defaultActionMap { get; set; }
        public InputActionMap currentActionMap => default;
        public event System.Action<InputAction.CallbackContext> onActionTriggered;
        public void ActivateInput() { }
        public void DeactivateInput() { }
        public void SwitchCurrentActionMap(string name) { }
    }
    public enum InputActionType { Value, Button, PassThrough }
    public enum InputActionPhase { Disabled, Waiting, Started, Performed, Canceled }
    public class InputAction
    {
        public struct CallbackContext
        {
            public InputAction action => default;
            public InputActionPhase phase => default;
            public bool started => false;
            public bool performed => false;
            public bool canceled => false;
            public T ReadValue<T>() => default;
        }
        public InputAction(string name = "", InputActionType type = InputActionType.Value,
            string binding = null, string interactions = null, string processors = null,
            string expectedControlType = null) { }
        public string name => "";
        public InputControl activeControl => default;
        public bool enabled => false;
        public InputActionPhase phase => default;
        public bool triggered => false;
        public event System.Action<CallbackContext> started, performed, canceled;
        public void Enable() { }
        public void Disable() { }
        public void Dispose() { }
        public T ReadValue<T>() => default;
        public bool IsPressed() => false;
        public bool WasPressedThisFrame() => false;
        public bool WasReleasedThisFrame() => false;
        public bool WasPerformedThisFrame() => false;
        public InputBinding AddBinding(string path) => default;
        public InputBinding AddCompositeBinding(string path) => default;
        public void ApplyBindingOverride(int index, string path) { }
        public void RemoveBindingOverride(int index) { }
        public void RemoveAllBindingOverrides() { }
    }
    public class InputBinding
    {
        public string path, groups, overridePath;
        public static InputBinding MaskByGroup(params string[] groups) => default;
        public InputBinding With(string name, string path) => this;
        public InputBinding WithProcessor(string name) => this;
        public InputBinding WithInteraction(string name) => this;
    }
    public class InputActionMap
    {
        public string name => "";
        public bool enabled => false;
        public InputBinding bindingMask { get; set; }
        public InputAction AddAction(string name, InputActionType type = InputActionType.Value,
            string binding = null) => default;
        public InputAction FindAction(string name, bool throwIfNotFound = false) => default;
        public void Enable() { }
        public void Disable() { }
        public void Dispose() { }
    }
    public class InputActionAsset : UnityEngine.ScriptableObject
    {
        public InputBinding bindingMask { get; set; }
        public InputActionMap FindActionMap(string name, bool throwIfNotFound = false) => default;
        public InputAction FindAction(string name, bool throwIfNotFound = false) => default;
        public string SaveBindingOverridesAsJson() => "";
        public void LoadBindingOverridesFromJson(string json, bool removeExisting = true) { }
        public void Enable() { }
        public void Disable() { }
    }
    public class InputActionReference : UnityEngine.ScriptableObject
    {
        public InputAction action => default;
        public static InputActionReference Create(InputAction action) => default;
    }
}

namespace UnityEngine.InputSystem.LowLevel
{
    public struct InputEventPtr { }
}

namespace UnityEngine.AI
{
    // Compile-time bridge for the browser NavMeshAgent implementation.
    public class NavMeshAgent : UnityEngine.Behaviour
    {
        public UnityEngine.Vector3 velocity { get; set; }
        public float remainingDistance { get; set; }
        public bool isStopped { get; set; }
        public bool SetDestination(UnityEngine.Vector3 target) => false;
        public bool Warp(UnityEngine.Vector3 target) => false;
    }
}
