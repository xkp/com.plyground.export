using System;
using System.Collections.Generic;
using System.Linq;

[Serializable]
public enum PlyFeaturePortDirection
{
    Input,
    Output
}

[Serializable]
public enum PlyFeaturePortKind
{
    Action,
    Event,
    Value
}

[Serializable]
public enum PlyFeatureDataType
{
    Void,
    Bool,
    Float,
    Int,
    String,
    GameObject,
    Vector3,
    Any
}

[Serializable]
public enum PlyFeatureMemberKind
{
    Method,
    UnityEvent,
    CSharpEvent,
    Field,
    Property
}

[Serializable]
public enum PlyFeatureParameterAccess
{
    ReadOnly,
    WriteOnly,
    ReadWrite
}

[Serializable]
public class PlyFeatureManifest
{
    public string schemaVersion = "1.0";
    public string moduleId = "";
    public List<PlyFeatureProfile> features = new List<PlyFeatureProfile>();
}

[Serializable]
public class PlyFeatureProfile
{
    public string id = "";
    public string featureId = "";
    public string name = "";
    public string description = "";
    public string aiMatchDescription = "";
    public List<string> tags = new List<string>();
    public List<string> categories = new List<string>();
    public List<string> implements = new List<string>();
    public List<string> provides = new List<string>();
    public List<string> consumes = new List<string>();
    public List<string> targetRoles = new List<string>();
    public bool useAdapterComponent;
    public string adapterComponentType = "";
    public List<PlyFeatureComponentRequirement> componentRequirements = new List<PlyFeatureComponentRequirement>();
    public List<PlyFeaturePortMapping> inputs = new List<PlyFeaturePortMapping>();
    public List<PlyFeaturePortMapping> outputs = new List<PlyFeaturePortMapping>();
    [NonSerialized]
    public List<PlyFeaturePortMapping> ports = new List<PlyFeaturePortMapping>();
    public List<PlyFeatureParameterMapping> parameters = new List<PlyFeatureParameterMapping>();
}

[Serializable]
public class PlyFeatureComponentRequirement
{
    public string typeName = "";
    public string assemblyQualifiedName = "";
    public bool required = true;
}

[Serializable]
public class PlyFeaturePortMapping
{
    public string name = "";
    public PlyFeaturePortDirection direction = PlyFeaturePortDirection.Input;
    public PlyFeaturePortKind kind = PlyFeaturePortKind.Action;
    public PlyFeatureDataType dataType = PlyFeatureDataType.Void;
    public PlyFeatureBinding binding = new PlyFeatureBinding();
}

[Serializable]
public class PlyFeatureParameterMapping
{
    public string name = "";
    public string direction = "parameter";
    public PlyFeatureDataType type = PlyFeatureDataType.Any;
    public string defaultValue = "";
    public PlyFeatureParameterAccess accessMode = PlyFeatureParameterAccess.ReadWrite;
    public PlyFeatureBinding binding = new PlyFeatureBinding();
}

[Serializable]
public class PlyFeatureBinding
{
    public string componentType = "";
    public PlyFeatureMemberKind memberKind = PlyFeatureMemberKind.Method;
    public string memberName = "";
    public string memberSignature = "";
    public string conversion = "";
    public bool isStatic;
    public PlyFeatureParameterAccess access = PlyFeatureParameterAccess.ReadWrite;
}

[Serializable]
public class PlyFeatureMemberDescriptor
{
    public string componentTypeName = "";
    public string componentAssemblyQualifiedName = "";
    public string memberName = "";
    public string displayName = "";
    public PlyFeatureMemberKind memberKind = PlyFeatureMemberKind.Method;
    public PlyFeatureDataType dataType = PlyFeatureDataType.Any;
    public PlyFeatureParameterAccess access = PlyFeatureParameterAccess.ReadWrite;
    public bool isStatic;
    public int parameterCount;
    public bool isLifecycleMethod;
}

[Serializable]
public class PlyFeatureComponentDescriptor
{
    public string typeName = "";
    public string fullName = "";
    public string assemblyQualifiedName = "";
    public string namespaceName = "";
    public string baseTypeName = "";
    public bool isMonoBehaviour;
    public bool isScriptableObject;
    public List<PlyFeatureMemberDescriptor> members = new List<PlyFeatureMemberDescriptor>();
}

[Serializable]
public class PlyFeatureTypeCacheSnapshot
{
    public string generatedAtUtc = "";
    public List<PlyFeatureComponentDescriptor> components = new List<PlyFeatureComponentDescriptor>();
}

[Serializable]
public class PlyFeatureValidationIssue
{
    public string severity = "warning";
    public string path = "";
    public string message = "";
}

public static class PlyFeatureSchemaUtility
{
    public static PlyFeatureManifest NormalizeManifest(PlyFeatureManifest manifest)
    {
        manifest = manifest ?? new PlyFeatureManifest();
        manifest.schemaVersion = string.IsNullOrWhiteSpace(manifest.schemaVersion) ? "1.0" : manifest.schemaVersion.Trim();
        manifest.moduleId = manifest.moduleId ?? "";
        manifest.features = manifest.features ?? new List<PlyFeatureProfile>();

        foreach (PlyFeatureProfile feature in manifest.features)
        {
            NormalizeFeature(feature);
        }

        return manifest;
    }

    public static PlyFeatureProfile NormalizeFeature(PlyFeatureProfile feature)
    {
        feature = feature ?? new PlyFeatureProfile();
        feature.id = feature.id ?? "";
        feature.featureId = feature.featureId ?? "";
        feature.name = feature.name ?? "";
        feature.description = feature.description ?? "";
        feature.aiMatchDescription = feature.aiMatchDescription ?? "";
        feature.tags = NormalizeStrings(feature.tags);
        feature.categories = NormalizeStrings(feature.categories);
        feature.implements = NormalizeStrings(feature.implements);
        feature.provides = NormalizeStrings(feature.provides);
        feature.consumes = NormalizeStrings(feature.consumes);
        feature.targetRoles = NormalizeStrings(feature.targetRoles);
        feature.adapterComponentType = feature.adapterComponentType ?? "";
        feature.componentRequirements = feature.componentRequirements ?? new List<PlyFeatureComponentRequirement>();
        feature.inputs = NormalizePortList(feature.inputs, PlyFeaturePortDirection.Input);
        feature.outputs = NormalizePortList(feature.outputs, PlyFeaturePortDirection.Output);
        if (feature.ports != null && feature.ports.Count > 0)
        {
            foreach (PlyFeaturePortMapping port in feature.ports)
            {
                if (port == null)
                {
                    continue;
                }

                if (port.direction == PlyFeaturePortDirection.Output)
                {
                    feature.outputs.Add(NormalizePort(port, PlyFeaturePortDirection.Output));
                }
                else
                {
                    feature.inputs.Add(NormalizePort(port, PlyFeaturePortDirection.Input));
                }
            }
        }

        feature.inputs = DeduplicatePorts(feature.inputs, PlyFeaturePortDirection.Input);
        feature.outputs = DeduplicatePorts(feature.outputs, PlyFeaturePortDirection.Output);
        feature.ports = feature.inputs.Concat(feature.outputs).ToList();
        feature.parameters = feature.parameters ?? new List<PlyFeatureParameterMapping>();

        foreach (PlyFeatureComponentRequirement requirement in feature.componentRequirements)
        {
            if (requirement == null)
            {
                continue;
            }

            requirement.typeName = requirement.typeName ?? "";
            requirement.assemblyQualifiedName = requirement.assemblyQualifiedName ?? "";
        }

        foreach (PlyFeatureParameterMapping parameter in feature.parameters)
        {
            if (parameter == null)
            {
                continue;
            }

            parameter.name = parameter.name ?? "";
            parameter.direction = string.IsNullOrWhiteSpace(parameter.direction) ? "parameter" : parameter.direction;
            parameter.defaultValue = parameter.defaultValue ?? "";
            parameter.binding = NormalizeBinding(parameter.binding);
        }

        return feature;
    }

    private static List<PlyFeaturePortMapping> NormalizePortList(List<PlyFeaturePortMapping> ports, PlyFeaturePortDirection direction)
    {
        List<PlyFeaturePortMapping> normalized = new List<PlyFeaturePortMapping>();
        foreach (PlyFeaturePortMapping port in ports ?? new List<PlyFeaturePortMapping>())
        {
            if (port == null)
            {
                continue;
            }

            normalized.Add(NormalizePort(port, direction));
        }

        return normalized;
    }

    private static PlyFeaturePortMapping NormalizePort(PlyFeaturePortMapping port, PlyFeaturePortDirection direction)
    {
        port = port ?? new PlyFeaturePortMapping();
        port.name = port.name ?? "";
        port.direction = direction;
        port.binding = NormalizeBinding(port.binding);
        return port;
    }

    private static List<PlyFeaturePortMapping> DeduplicatePorts(List<PlyFeaturePortMapping> ports, PlyFeaturePortDirection direction)
    {
        return (ports ?? new List<PlyFeaturePortMapping>())
            .Where(port => port != null && !string.IsNullOrWhiteSpace(port.name))
            .GroupBy(port => port.name.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(group => NormalizePort(group.First(), direction))
            .OrderBy(port => port.name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static PlyFeatureBinding NormalizeBinding(PlyFeatureBinding binding)
    {
        binding = binding ?? new PlyFeatureBinding();
        binding.componentType = binding.componentType ?? "";
        binding.memberName = binding.memberName ?? "";
        binding.memberSignature = binding.memberSignature ?? "";
        binding.conversion = binding.conversion ?? "";
        return binding;
    }

    public static List<string> NormalizeStrings(List<string> values)
    {
        List<string> result = new List<string>();
        if (values == null)
        {
            return result;
        }

        HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (string value in values)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            string trimmed = value.Trim();
            if (seen.Add(trimmed))
            {
                result.Add(trimmed);
            }
        }

        result.Sort(StringComparer.OrdinalIgnoreCase);
        return result;
    }
}
