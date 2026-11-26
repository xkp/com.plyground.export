namespace UnityEngine.Rendering
{
    public struct VertexAttributeDescriptor
    {
        public VertexAttribute attribute;
        public VertexAttributeFormat format;
        public int dimension;
        public int stream;

        public VertexAttributeDescriptor(VertexAttribute attribute,
            VertexAttributeFormat format = VertexAttributeFormat.Float32,
            int dimension = 3, int stream = 0)
        {
            this.attribute = attribute;
            this.format = format;
            this.dimension = dimension;
            this.stream = stream;
        }
        public bool Equals(VertexAttributeDescriptor other) => false;
        public override bool Equals(object other) => false;
        public override string ToString() => "";
        public static bool operator ==(VertexAttributeDescriptor left, VertexAttributeDescriptor right) => false;
        public static bool operator !=(VertexAttributeDescriptor left, VertexAttributeDescriptor right) => true;
        public override int GetHashCode() => 0;
    }
    public struct SubMeshDescriptor
    {
        public int indexStart;
        public int indexCount;
        public UnityEngine.MeshTopology topology;
        public int baseVertex;
        public int firstVertex;
        public int vertexCount;
        public UnityEngine.Bounds bounds;

        public SubMeshDescriptor(int indexStart, int indexCount,
            UnityEngine.MeshTopology topology = UnityEngine.MeshTopology.Triangles)
        {
            this.indexStart = indexStart;
            this.indexCount = indexCount;
            this.topology = topology;
            baseVertex = 0;
            firstVertex = 0;
            vertexCount = 0;
            bounds = default;
        }
        public override string ToString() => "";
    }
}
