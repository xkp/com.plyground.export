namespace UnityEngine
{
    public struct SortingLayer
    {
        public int id { get; }
        public string name { get; }
        public int value { get; }
        public static SortingLayer[] layers => [];
        public static int NameToID(string name) => 0;
        public static string IDToName(int id) => "Default";
        public static bool IsValid(int id) => id == 0;
        public static int GetLayerValueFromID(int id) => 0;
        public static int GetLayerValueFromName(string name) => 0;
    }
    public static class RenderSettings
    {
        public static bool fog { get; set; }
        public static FogMode fogMode { get; set; }
        public static Color fogColor { get; set; }
        public static float fogDensity { get; set; }
        public static float fogStartDistance { get; set; }
        public static float fogEndDistance { get; set; }
    }
}

namespace UnityEngine.Rendering
{
    public struct SortingLayerRange : System.IEquatable<SortingLayerRange>
    {
        public short lowerBound { get; set; }
        public short upperBound { get; set; }
        public SortingLayerRange(short lowerBound, short upperBound)
        {
            this.lowerBound = lowerBound;
            this.upperBound = upperBound;
        }
        public static SortingLayerRange all => new(short.MinValue, short.MaxValue);
        public bool Equals(SortingLayerRange other) => lowerBound == other.lowerBound
            && upperBound == other.upperBound;
        public override bool Equals(object? other) => other is SortingLayerRange value && Equals(value);
        public override int GetHashCode() => (lowerBound & 65535) | (upperBound << 16);
        public static bool operator ==(SortingLayerRange left, SortingLayerRange right) => left.Equals(right);
        public static bool operator !=(SortingLayerRange left, SortingLayerRange right) => !left.Equals(right);
    }
}
