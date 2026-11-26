namespace UnityEngine
{
    [System.AttributeUsage(System.AttributeTargets.Assembly)]
    public sealed class AssemblyIsEditorAssembly : System.Attribute { }
    [System.AttributeUsage(System.AttributeTargets.Class)]
    public sealed class ExcludeFromPresetAttribute : System.Attribute { }
    [System.AttributeUsage(System.AttributeTargets.Class)]
    public sealed class ExecuteAlways : System.Attribute { }
    [System.AttributeUsage(System.AttributeTargets.Class)]
    public sealed class ExecuteInEditMode : System.Attribute { }
    [System.AttributeUsage(System.AttributeTargets.Class)]
    public sealed class InspectorOrderAttribute : System.Attribute
    {
        public InspectorOrderAttribute(InspectorSort sort, InspectorSortDirection direction) { }
    }
}

namespace UnityEngine.Categorization
{
    [System.AttributeUsage(System.AttributeTargets.All)]
    public sealed class CategoryInfoAttribute : System.Attribute
    {
        public string Name { get; set; } = "";
        public int Order { get; set; }
    }
    [System.AttributeUsage(System.AttributeTargets.All)]
    public sealed class ElementInfoAttribute : System.Attribute
    {
        public string Name { get; set; } = "";
        public int Order { get; set; }
    }
}

namespace UnityEngine.Internal
{
    [System.AttributeUsage(System.AttributeTargets.All)]
    public sealed class ExcludeFromDocsAttribute : System.Attribute { }
}

namespace UnityEngine.SearchService
{
    [System.AttributeUsage(System.AttributeTargets.All)]
    public sealed class ObjectSelectorHandlerWithLabelsAttribute : System.Attribute
    {
        public string[] labels { get; }
        public bool matchAll { get; }
        public ObjectSelectorHandlerWithLabelsAttribute(params string[] labels)
            { this.labels = labels; }
        public ObjectSelectorHandlerWithLabelsAttribute(bool matchAll, params string[] labels)
            { this.matchAll = matchAll; this.labels = labels; }
    }
    [System.AttributeUsage(System.AttributeTargets.All)]
    public sealed class ObjectSelectorHandlerWithTagsAttribute : System.Attribute
    {
        public string[] tags { get; }
        public ObjectSelectorHandlerWithTagsAttribute(params string[] tags)
            { this.tags = tags; }
    }
}
