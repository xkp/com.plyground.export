namespace UnityEngine.Scripting
{
    [System.AttributeUsage(System.AttributeTargets.Assembly)]
    public sealed class AlwaysLinkAssemblyAttribute : System.Attribute { }
    [System.AttributeUsage(System.AttributeTargets.All)]
    public sealed class PreserveAttribute : System.Attribute { }
    [System.AttributeUsage(System.AttributeTargets.Class)]
    public sealed class RequireAttributeUsagesAttribute : System.Attribute { }
    [System.AttributeUsage(System.AttributeTargets.Class)]
    public sealed class RequireDerivedAttribute : System.Attribute { }
    [System.AttributeUsage(System.AttributeTargets.Class)]
    public sealed class RequireImplementorsAttribute : System.Attribute { }
    [System.AttributeUsage(System.AttributeTargets.Class)]
    public sealed class RequiredInterfaceAttribute : System.Attribute
    {
        public RequiredInterfaceAttribute(System.Type type) { }
    }
    [System.AttributeUsage(System.AttributeTargets.Class)]
    public sealed class RequiredMemberAttribute : System.Attribute { }
}

namespace UnityEngine
{
    [System.AttributeUsage(System.AttributeTargets.Assembly)]
    public sealed class UnityAPICompatibilityVersionAttribute : System.Attribute
    {
        public string version { get; }
        public UnityAPICompatibilityVersionAttribute(string version) { this.version = version; }
        public UnityAPICompatibilityVersionAttribute(string version, bool _flag)
            { this.version = version; }
        public UnityAPICompatibilityVersionAttribute(string version, string[] _assemblies)
            { this.version = version; }
    }
}
