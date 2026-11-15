// Compile-time Unity surface for variation scripts. This file is never emitted to JavaScript.
// Extend it only for APIs that the runtime implements and the transpiler can lower.
namespace UnityEngine
{
    public class Object
    {
        public string name { get; set; } = "";
        public static implicit operator bool(Object? value) => value is not null;
        public static T Instantiate<T>(T original) where T : Object => original;
        public static T Instantiate<T>(T original, Transform parent) where T : Object => original;
        public static T Instantiate<T>(T original, Vector3 position, Quaternion rotation) where T : Object => original;
        public static T Instantiate<T>(T original, Vector3 position, Quaternion rotation, Transform parent) where T : Object => original;
        public static void Destroy(Object target) { }
        public static void Destroy(Object target, float delay) { }
        public static void DestroyImmediate(Object target) { }
        public static void DontDestroyOnLoad(Object target) { }
        public static T[] FindObjectsOfType<T>(bool includeInactive = false) where T : Object => [];
    }
    public class Component : Object
    {
        public GameObject gameObject { get; set; } = null!;
        public Transform transform { get; set; } = null!;
        public T GetComponent<T>() where T : Component => null!;
        public T GetComponentInChildren<T>(bool includeInactive = false) where T : Component => null!;
        public T[] GetComponentsInChildren<T>(bool includeInactive = false) where T : Component => [];
    }
    public class Behaviour : Component { public bool enabled { get; set; } = true; }
    public class MonoBehaviour : Behaviour { }
    public class GameObject : Object
    {
        public static GameObject CreatePrimitive(PrimitiveType type) => new GameObject();
        public Transform transform { get; set; } = null!;
        public T AddComponent<T>() where T : Component, new() => new T();
        public T GetComponent<T>() where T : Component => null!;
        public T GetComponentInChildren<T>(bool includeInactive = false) where T : Component => null!;
        public T[] GetComponentsInChildren<T>(bool includeInactive = false) where T : Component => [];
        public void SetActive(bool active) { }
    }
    public enum PrimitiveType { Sphere, Capsule, Cylinder, Cube, Plane, Quad }
    public class Collider : Component { }
    public class Transform : Component
    {
        public Vector3 position { get; set; }
        public Vector3 localPosition { get; set; }
        public Vector3 localScale { get; set; }
        public Quaternion rotation { get; set; }
        public Quaternion localRotation { get; set; }
    }
    public struct Vector2
    {
        public float x, y;
        public float this[int index] { get => 0; set { } }
        public Vector2(float x, float y) { this.x = x; this.y = y; }
        public float magnitude => 0; public float sqrMagnitude => 0; public Vector2 normalized => this;
        public static Vector2 zero => new(); public static Vector2 one => new(1, 1);
        public static Vector2 positiveInfinity => new(float.PositiveInfinity, float.PositiveInfinity);
        public static Vector2 negativeInfinity => new(float.NegativeInfinity, float.NegativeInfinity);
        public static Vector2 up => new(0, 1); public static Vector2 down => new(0, -1);
        public static Vector2 left => new(-1, 0); public static Vector2 right => new(1, 0);
        public void Normalize() { } public void Set(float x, float y) { }
        public static float Distance(Vector2 a, Vector2 b) => 0;
        public static float Dot(Vector2 a, Vector2 b) => 0;
        public static Vector2 Normalize(Vector2 v) => v;
        public static float SqrMagnitude(Vector2 v) => 0;
        public static float Angle(Vector2 a, Vector2 b) => 0;
        public static float SignedAngle(Vector2 a, Vector2 b) => 0;
        public static Vector2 Lerp(Vector2 a, Vector2 b, float t) => a;
        public static Vector2 LerpUnclamped(Vector2 a, Vector2 b, float t) => a;
        public static Vector2 MoveTowards(Vector2 a, Vector2 b, float maxDistanceDelta) => a;
        public static Vector2 ClampMagnitude(Vector2 v, float maxLength) => v;
        public static Vector2 Scale(Vector2 a, Vector2 b) => a;
        public static Vector2 Min(Vector2 a, Vector2 b) => a;
        public static Vector2 Max(Vector2 a, Vector2 b) => a;
        public static Vector2 Perpendicular(Vector2 v) => v;
        public static Vector2 Reflect(Vector2 v, Vector2 normal) => v;
        public static Vector2 SmoothDamp(Vector2 current, Vector2 target, ref Vector2 currentVelocity,
            float smoothTime, float maxSpeed = float.PositiveInfinity, float deltaTime = 0.02f) => current;
        public static Vector2 operator +(Vector2 a, Vector2 b) => a;
        public static Vector2 operator -(Vector2 a, Vector2 b) => a;
        public static Vector2 operator *(Vector2 a, float b) => a;
        public static Vector2 operator /(Vector2 a, float b) => a;
    }
    public struct Vector3
    {
        public float x, y, z;
        public float this[int index] { get => 0; set { } }
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
        public static Vector3 back => new(0, 0, -1);
        public float magnitude => 0; public float sqrMagnitude => 0; public Vector3 normalized => this;
        public void Normalize() { } public void Set(float x, float y, float z) { }
        public static float Dot(Vector3 left, Vector3 right) => left.x * right.x + left.y * right.y + left.z * right.z;
        public static Vector3 Normalize(Vector3 v) => v;
        public static float SqrMagnitude(Vector3 v) => 0;
        public static float Distance(Vector3 a, Vector3 b) => 0;
        public static float Angle(Vector3 a, Vector3 b) => 0;
        public static float SignedAngle(Vector3 a, Vector3 b, Vector3 axis) => 0;
        public static Vector3 Cross(Vector3 a, Vector3 b) => a;
        public static Vector3 Lerp(Vector3 a, Vector3 b, float t) => a;
        public static Vector3 LerpUnclamped(Vector3 a, Vector3 b, float t) => a;
        public static Vector3 Slerp(Vector3 a, Vector3 b, float t) => a;
        public static Vector3 SlerpUnclamped(Vector3 a, Vector3 b, float t) => a;
        public static Vector3 MoveTowards(Vector3 a, Vector3 b, float maxDistanceDelta) => a;
        public static Vector3 RotateTowards(Vector3 a, Vector3 b, float maxRadiansDelta, float maxMagnitudeDelta) => a;
        public static Vector3 ClampMagnitude(Vector3 v, float maxLength) => v;
        public static Vector3 Scale(Vector3 a, Vector3 b) => a;
        public static Vector3 Min(Vector3 a, Vector3 b) => a;
        public static Vector3 Max(Vector3 a, Vector3 b) => a;
        public static Vector3 Project(Vector3 v, Vector3 onNormal) => v;
        public static Vector3 ProjectOnPlane(Vector3 v, Vector3 normal) => v;
        public static Vector3 Reflect(Vector3 v, Vector3 normal) => v;
        public static Vector3 SmoothDamp(Vector3 current, Vector3 target, ref Vector3 currentVelocity,
            float smoothTime, float maxSpeed = float.PositiveInfinity, float deltaTime = 0.02f) => current;
        public static void OrthoNormalize(ref Vector3 normal, ref Vector3 tangent) { }
        public static void OrthoNormalize(ref Vector3 normal, ref Vector3 tangent, ref Vector3 binormal) { }
        public static Vector3 operator +(Vector3 left, Vector3 right) => new(left.x + right.x, left.y + right.y, left.z + right.z);
        public static Vector3 operator -(Vector3 left, Vector3 right) => new(left.x - right.x, left.y - right.y, left.z - right.z);
        public static Vector3 operator *(Vector3 value, float scalar) => new(value.x * scalar, value.y * scalar, value.z * scalar);
        public static Vector3 operator /(Vector3 value, float scalar) => new(value.x / scalar, value.y / scalar, value.z / scalar);
    }
    public struct Vector4
    {
        public float x, y, z, w;
        public float this[int index] { get => 0; set { } }
        public Vector4(float x, float y, float z, float w)
        { this.x = x; this.y = y; this.z = z; this.w = w; }
        public float magnitude => 0; public float sqrMagnitude => 0; public Vector4 normalized => this;
        public static Vector4 zero => new(); public static Vector4 one => new(1, 1, 1, 1);
        public static Vector4 positiveInfinity => new(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
        public static Vector4 negativeInfinity => new(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
        public void Normalize() { } public void Set(float x, float y, float z, float w) { }
        public static float Distance(Vector4 a, Vector4 b) => 0;
        public static float Dot(Vector4 a, Vector4 b) => 0;
        public static Vector4 Normalize(Vector4 v) => v;
        public static float SqrMagnitude(Vector4 v) => 0;
        public static Vector4 Lerp(Vector4 a, Vector4 b, float t) => a;
        public static Vector4 LerpUnclamped(Vector4 a, Vector4 b, float t) => a;
        public static Vector4 MoveTowards(Vector4 a, Vector4 b, float maxDistanceDelta) => a;
        public static Vector4 Scale(Vector4 a, Vector4 b) => a;
        public static Vector4 Min(Vector4 a, Vector4 b) => a;
        public static Vector4 Max(Vector4 a, Vector4 b) => a;
        public static Vector4 operator +(Vector4 a, Vector4 b) => a;
        public static Vector4 operator -(Vector4 a, Vector4 b) => a;
        public static Vector4 operator *(Vector4 a, float b) => a;
        public static Vector4 operator /(Vector4 a, float b) => a;
    }
    public struct Quaternion
    {
        public float x, y, z, w;
        public float this[int index] { get => 0; set { } }
        public Quaternion(float x, float y, float z, float w) { this.x = x; this.y = y; this.z = z; this.w = w; }
        public static Quaternion identity => new(0, 0, 0, 1);
        public Quaternion normalized => this;
        public Vector3 eulerAngles { get; set; }
        public void Normalize() { } public void Set(float x, float y, float z, float w) { }
        public void SetFromToRotation(Vector3 from, Vector3 to) { }
        public void SetLookRotation(Vector3 forward) { }
        public void SetLookRotation(Vector3 forward, Vector3 up) { }
        public void ToAngleAxis(out float angle, out Vector3 axis) { angle = 0; axis = default; }
        public static Quaternion Euler(float x, float y, float z) => identity;
        public static Quaternion Euler(Vector3 angles) => identity;
        public static Quaternion AngleAxis(float angle, Vector3 axis) => identity;
        public static Quaternion FromToRotation(Vector3 from, Vector3 to) => identity;
        public static Quaternion LookRotation(Vector3 forward, Vector3 up) => identity;
        public static Quaternion LookRotation(Vector3 forward) => identity;
        public static Quaternion Inverse(Quaternion q) => q;
        public static Quaternion Normalize(Quaternion q) => q;
        public static Quaternion Lerp(Quaternion a, Quaternion b, float t) => a;
        public static Quaternion LerpUnclamped(Quaternion a, Quaternion b, float t) => a;
        public static Quaternion Slerp(Quaternion a, Quaternion b, float t) => a;
        public static Quaternion SlerpUnclamped(Quaternion a, Quaternion b, float t) => a;
        public static Quaternion RotateTowards(Quaternion a, Quaternion b, float maxDegreesDelta) => a;
        public static float Angle(Quaternion a, Quaternion b) => 0;
        public static float Dot(Quaternion a, Quaternion b) => 0;
        public static Quaternion operator *(Quaternion a, Quaternion b) => a;
        public static Vector3 operator *(Quaternion a, Vector3 b) => b;
    }
    public struct Ray
    {
        public Vector3 origin { get; set; } public Vector3 direction { get; set; }
        public Ray(Vector3 origin, Vector3 direction) { this.origin = origin; this.direction = direction; }
        public Vector3 GetPoint(float distance) => origin;
    }
    public struct Plane
    {
        public Vector3 normal { get; set; }
        public float distance { get; set; }
        public Plane flipped => this;
        public Plane(Vector3 normal, float distance) { this.normal = normal; this.distance = distance; }
        public Plane(Vector3 normal, Vector3 point) { this.normal = normal; distance = 0; }
        public Plane(Vector3 a, Vector3 b, Vector3 c) { normal = default; distance = 0; }
        public void Flip() { }
        public void Set3Points(Vector3 a, Vector3 b, Vector3 c) { }
        public void SetNormalAndPosition(Vector3 normal, Vector3 point) { }
        public float GetDistanceToPoint(Vector3 point) => 0;
        public Vector3 ClosestPointOnPlane(Vector3 point) => point;
        public bool GetSide(Vector3 point) => false;
        public bool SameSide(Vector3 a, Vector3 b) => false;
        public bool Raycast(Ray ray, out float enter) { enter = 0; return false; }
        public void Translate(Vector3 translation) { }
        public static Plane Translate(Plane plane, Vector3 translation) => plane;
    }
    public struct Bounds
    {
        public Vector3 center { get; set; }
        public Vector3 size { get; set; }
        public Vector3 extents { get; set; }
        public Vector3 min { get; set; }
        public Vector3 max { get; set; }
        public Bounds(Vector3 center, Vector3 size) { this.center = center; this.size = size; extents = min = max = default; }
        public Vector3 ClosestPoint(Vector3 point) => point;
        public bool Contains(Vector3 point) => false;
        public void Encapsulate(Vector3 point) { }
        public void Encapsulate(Bounds bounds) { }
        public void Expand(float amount) { }
        public void Expand(Vector3 amount) { }
        public bool Intersects(Bounds other) => false;
        public bool IntersectRay(Ray ray) => false;
        public bool IntersectRay(Ray ray, out float distance) { distance = 0; return false; }
        public void SetMinMax(Vector3 min, Vector3 max) { }
        public float SqrDistance(Vector3 point) => 0;
    }
    public struct Matrix4x4
    {
        public Matrix4x4(Vector4 column0, Vector4 column1, Vector4 column2, Vector4 column3) { }
        public static Matrix4x4 identity => default;
        public static Matrix4x4 zero => default;
        public Matrix4x4 inverse => this;
        public Matrix4x4 transpose => this;
        public float determinant => 0;
        public bool isIdentity => false;
        public Vector3 lossyScale => default;
        public Quaternion rotation => default;
        public float this[int row, int column] { get => 0; set { } }
        public float this[int index] { get => 0; set { } }
        public Vector4 GetColumn(int index) => default;
        public Vector4 GetRow(int index) => default;
        public void SetColumn(int index, Vector4 value) { }
        public void SetRow(int index, Vector4 value) { }
        public Vector3 GetPosition() => default;
        public void SetTRS(Vector3 position, Quaternion rotation, Vector3 scale) { }
        public Vector3 MultiplyPoint(Vector3 point) => point;
        public Vector3 MultiplyPoint3x4(Vector3 point) => point;
        public Vector3 MultiplyVector(Vector3 vector) => vector;
        public Plane TransformPlane(Plane plane) => plane;
        public bool ValidTRS() => false;
        public static Matrix4x4 TRS(Vector3 position, Quaternion rotation, Vector3 scale) => default;
        public static Matrix4x4 LookAt(Vector3 from, Vector3 to, Vector3 up) => default;
        public static bool Inverse3DAffine(Matrix4x4 input, ref Matrix4x4 result) => false;
        public static Matrix4x4 Translate(Vector3 position) => default;
        public static Matrix4x4 Rotate(Quaternion rotation) => default;
        public static Matrix4x4 Scale(Vector3 scale) => default;
        public static Matrix4x4 Ortho(float left, float right, float bottom, float top, float near, float far) => default;
        public static Matrix4x4 Perspective(float fov, float aspect, float near, float far) => default;
        public static Matrix4x4 Frustum(float left, float right, float bottom, float top, float near, float far) => default;
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
    public class Rigidbody : Component { }
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
    public struct Color
    {
        public float r, g, b, a;
        public Color(float r, float g, float b, float a = 1) { this.r = r; this.g = g; this.b = b; this.a = a; }
    }
    public struct Rect
    {
        public float x, y, width, height;
        public Rect(float x, float y, float width, float height)
        { this.x = x; this.y = y; this.width = width; this.height = height; }
    }
    public static class GUI
    {
        public static void Label(Rect position, string text) { }
        public static void Box(Rect position, string text) { }
    }
    public static class Screen { public static int width => 0; public static int height => 0; }
    public enum CursorLockMode { None, Locked, Confined }
    public static class Cursor
    {
        public static CursorLockMode lockState { get; set; }
        public static bool visible { get; set; }
    }
    public static class Time { public static float deltaTime { get; } public static float time { get; } }
    public static class Input
    {
        public static float GetAxis(string name) => 0;
        public static bool GetKey(string name) => false;
        public static bool GetKeyDown(KeyCode key) => false;
    }
    public enum KeyCode { Return = 13, T = 116 }
    public static class Debug { public static void Log(object message) { } public static void LogError(object message) { } }
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
    public sealed class HeaderAttribute : System.Attribute { public HeaderAttribute(string title) { } }
    public sealed class TooltipAttribute : System.Attribute { public TooltipAttribute(string text) { } }
}
namespace UnityEngine.SceneManagement
{
    public struct Scene { public int buildIndex { get; } }
    public static class SceneManager
    {
        public static Scene GetActiveScene() => default;
        public static void LoadScene(int buildIndex) { }
    }
}
