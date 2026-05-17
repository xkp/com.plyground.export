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
    public string version = "2.0";
    public string moduleId = "";
    public List<string> targetRoles = new List<string>();
    public List<string> semanticCapabilities = new List<string>();
    public List<PlySemanticFeatureDefinition> features = new List<PlySemanticFeatureDefinition>();
    public List<PlyFeatureImplementation> implementations = new List<PlyFeatureImplementation>();
}

[Serializable]
public class PlySemanticFeatureDefinition
{
    public string id = "";
    public string name = "";
    public string description = "";
    public string origin = "catalog";
    public List<string> intentExamples = new List<string>();
    public List<string> targetRoles = new List<string>();
    public string category = "";
    public List<string> tags = new List<string>();
    public List<PlySemanticFeatureParameter> parameters = new List<PlySemanticFeatureParameter>();
    public List<PlySemanticFeaturePort> inputs = new List<PlySemanticFeaturePort>();
    public List<PlySemanticFeaturePort> outputs = new List<PlySemanticFeaturePort>();
    public List<string> provides = new List<string>();
    public List<string> requires = new List<string>();
}

[Serializable]
public class PlySemanticFeatureParameter
{
    public string name = "";
    public PlyFeatureDataType type = PlyFeatureDataType.Any;
    public bool required;
    public string defaultValue = "";
}

[Serializable]
public class PlySemanticFeaturePort
{
    public string name = "";
    public PlyFeaturePortKind kind = PlyFeaturePortKind.Action;
    public PlyFeatureDataType dataType = PlyFeatureDataType.Void;
    public bool required = true;
}

[Serializable]
public class PlyFeatureImplementation
{
    public string id = "";
    public string featureId = "";
    public string name = "";
    public string description = "";
    public List<string> targetRoles = new List<string>();
    public List<string> tags = new List<string>();
    public string integrationMode = "bindings";
    public PlyFeatureImplementationSource source = new PlyFeatureImplementationSource();
    public PlyFeatureCapabilitySet capabilities = new PlyFeatureCapabilitySet();
    public List<PlyFeatureParameterBinding> parameterBindings = new List<PlyFeatureParameterBinding>();
    public List<PlyFeaturePortBinding> inputBindings = new List<PlyFeaturePortBinding>();
    public List<PlyFeaturePortBinding> outputBindings = new List<PlyFeaturePortBinding>();
    public PlyFeatureAdapterReference adapter = new PlyFeatureAdapterReference();
}

[Serializable]
public class PlyFeatureImplementationSource
{
    public string kind = "module";
    public string system = "";
    public string moduleId = "";
}

[Serializable]
public class PlyFeatureCapabilitySet
{
    public List<string> provides = new List<string>();
    public List<string> requires = new List<string>();
}

[Serializable]
public class PlyFeaturePortBinding
{
    public string featureInput = "";
    public string featureOutput = "";
    public PlyFeatureBinding binding = new PlyFeatureBinding();
}

[Serializable]
public class PlyFeatureParameterBinding
{
    public string featureParameter = "";
    public PlyFeatureBinding binding = new PlyFeatureBinding();
}

[Serializable]
public class PlyFeatureAdapterReference
{
    public string adapterId = "";
    public string setupAdapter = "";
    public string factoryId = "";
}

[Serializable]
public class PlyFeatureBinding
{
    public string bindingKind = "";
    public string componentType = "";
    public PlyFeatureMemberKind memberKind = PlyFeatureMemberKind.Method;
    public string memberName = "";
    public string memberSignature = "";
    public string conversion = "";
    public bool isStatic;
    public PlyFeatureParameterAccess access = PlyFeatureParameterAccess.ReadWrite;
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
    public bool required;
    public PlyFeatureParameterAccess accessMode = PlyFeatureParameterAccess.ReadWrite;
    public PlyFeatureBinding binding = new PlyFeatureBinding();
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
        manifest.version = string.IsNullOrWhiteSpace(manifest.version) ? "2.0" : manifest.version.Trim();
        manifest.moduleId = manifest.moduleId ?? "";
        manifest.targetRoles = NormalizeStrings(manifest.targetRoles);
        manifest.semanticCapabilities = NormalizeStrings(manifest.semanticCapabilities);
        manifest.features = manifest.features ?? new List<PlySemanticFeatureDefinition>();
        manifest.implementations = manifest.implementations ?? new List<PlyFeatureImplementation>();

        foreach (PlySemanticFeatureDefinition feature in manifest.features)
        {
            NormalizeFeatureDefinition(feature);
        }

        foreach (PlyFeatureImplementation implementation in manifest.implementations)
        {
            NormalizeImplementation(implementation);
        }

        if (manifest.targetRoles.Count == 0)
        {
            manifest.targetRoles = NormalizeStrings(
                manifest.features.SelectMany(feature => feature.targetRoles ?? new List<string>())
                    .Concat(manifest.implementations.SelectMany(implementation => implementation.targetRoles ?? new List<string>()))
                    .ToList());
        }

        if (manifest.semanticCapabilities.Count == 0)
        {
            manifest.semanticCapabilities = NormalizeStrings(
                manifest.features.SelectMany(feature => feature.provides ?? new List<string>())
                    .Concat(manifest.features.SelectMany(feature => feature.requires ?? new List<string>()))
                    .Concat(manifest.implementations.SelectMany(implementation => implementation.capabilities?.provides ?? new List<string>()))
                    .Concat(manifest.implementations.SelectMany(implementation => implementation.capabilities?.requires ?? new List<string>()))
                    .ToList());
        }

        return manifest;
    }

    public static PlySemanticFeatureDefinition NormalizeFeatureDefinition(PlySemanticFeatureDefinition feature)
    {
        feature = feature ?? new PlySemanticFeatureDefinition();
        feature.id = feature.id ?? "";
        feature.name = feature.name ?? "";
        feature.description = feature.description ?? "";
        feature.origin = NormalizeFeatureOrigin(feature.origin);
        feature.intentExamples = NormalizeStrings(feature.intentExamples);
        feature.targetRoles = NormalizeStrings(feature.targetRoles);
        feature.category = feature.category ?? "";
        feature.tags = NormalizeStrings(feature.tags);
        feature.provides = NormalizeStrings(feature.provides);
        feature.requires = NormalizeStrings(feature.requires);
        feature.parameters = NormalizeParameters(feature.parameters);
        feature.inputs = NormalizePorts(feature.inputs, PlyFeaturePortDirection.Input);
        feature.outputs = NormalizePorts(feature.outputs, PlyFeaturePortDirection.Output);
        return feature;
    }

    public static PlyFeatureImplementation NormalizeImplementation(PlyFeatureImplementation implementation)
    {
        implementation = implementation ?? new PlyFeatureImplementation();
        implementation.id = implementation.id ?? "";
        implementation.featureId = implementation.featureId ?? "";
        implementation.name = implementation.name ?? "";
        implementation.description = implementation.description ?? "";
        implementation.targetRoles = NormalizeStrings(implementation.targetRoles);
        implementation.tags = NormalizeStrings(implementation.tags);
        implementation.integrationMode = NormalizeIntegrationMode(implementation.integrationMode, implementation);
        implementation.source = NormalizeSource(implementation.source);
        implementation.capabilities = NormalizeCapabilitySet(implementation.capabilities);
        implementation.adapter = NormalizeAdapter(implementation.adapter);
        implementation.parameterBindings = NormalizeParameterBindings(implementation.parameterBindings);
        implementation.inputBindings = NormalizePortBindings(implementation.inputBindings, true);
        implementation.outputBindings = NormalizePortBindings(implementation.outputBindings, false);

        if (string.Equals(implementation.integrationMode, "adapter", StringComparison.OrdinalIgnoreCase))
        {
            implementation.parameterBindings = new List<PlyFeatureParameterBinding>();
            implementation.inputBindings = new List<PlyFeaturePortBinding>();
            implementation.outputBindings = new List<PlyFeaturePortBinding>();
        }
        else
        {
            implementation.adapter = new PlyFeatureAdapterReference();
        }

        return implementation;
    }

    public static PlyFeatureBinding NormalizeBinding(PlyFeatureBinding binding)
    {
        binding = binding ?? new PlyFeatureBinding();
        binding.bindingKind = binding.bindingKind ?? "";
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

    private static List<PlySemanticFeatureParameter> NormalizeParameters(List<PlySemanticFeatureParameter> parameters)
    {
        return (parameters ?? new List<PlySemanticFeatureParameter>())
            .Where(parameter => parameter != null)
            .Select(parameter =>
            {
                parameter.name = parameter.name ?? "";
                parameter.defaultValue = parameter.defaultValue ?? "";
                return parameter;
            })
            .GroupBy(parameter => parameter.name.Trim(), StringComparer.OrdinalIgnoreCase)
            .Where(group => !string.IsNullOrWhiteSpace(group.Key))
            .Select(group => group.First())
            .OrderBy(parameter => parameter.name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static List<PlySemanticFeaturePort> NormalizePorts(List<PlySemanticFeaturePort> ports, PlyFeaturePortDirection direction)
    {
        return (ports ?? new List<PlySemanticFeaturePort>())
            .Where(port => port != null)
            .Select(port =>
            {
                port.name = port.name ?? "";
                if (direction == PlyFeaturePortDirection.Output && port.kind == PlyFeaturePortKind.Action)
                {
                    port.kind = port.dataType == PlyFeatureDataType.Void ? PlyFeaturePortKind.Event : PlyFeaturePortKind.Value;
                }

                if (direction == PlyFeaturePortDirection.Input && port.kind == PlyFeaturePortKind.Event)
                {
                    port.kind = PlyFeaturePortKind.Action;
                }

                return port;
            })
            .GroupBy(port => port.name.Trim(), StringComparer.OrdinalIgnoreCase)
            .Where(group => !string.IsNullOrWhiteSpace(group.Key))
            .Select(group => group.First())
            .OrderBy(port => port.name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static PlyFeatureImplementationSource NormalizeSource(PlyFeatureImplementationSource source)
    {
        source = source ?? new PlyFeatureImplementationSource();
        source.kind = string.IsNullOrWhiteSpace(source.kind) ? "module" : source.kind.Trim();
        source.system = source.system ?? "";
        source.moduleId = source.moduleId ?? "";
        return source;
    }

    private static PlyFeatureCapabilitySet NormalizeCapabilitySet(PlyFeatureCapabilitySet capabilities)
    {
        capabilities = capabilities ?? new PlyFeatureCapabilitySet();
        capabilities.provides = NormalizeStrings(capabilities.provides);
        capabilities.requires = NormalizeStrings(capabilities.requires);
        return capabilities;
    }

    private static PlyFeatureAdapterReference NormalizeAdapter(PlyFeatureAdapterReference adapter)
    {
        adapter = adapter ?? new PlyFeatureAdapterReference();
        adapter.adapterId = adapter.adapterId ?? "";
        adapter.setupAdapter = adapter.setupAdapter ?? "";
        adapter.factoryId = adapter.factoryId ?? "";
        return adapter;
    }

    private static List<PlyFeatureParameterBinding> NormalizeParameterBindings(List<PlyFeatureParameterBinding> bindings)
    {
        return (bindings ?? new List<PlyFeatureParameterBinding>())
            .Where(binding => binding != null)
            .Select(binding =>
            {
                binding.featureParameter = binding.featureParameter ?? "";
                binding.binding = NormalizeBinding(binding.binding);
                return binding;
            })
            .Where(binding => !string.IsNullOrWhiteSpace(binding.featureParameter))
            .OrderBy(binding => binding.featureParameter, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static List<PlyFeaturePortBinding> NormalizePortBindings(List<PlyFeaturePortBinding> bindings, bool input)
    {
        return (bindings ?? new List<PlyFeaturePortBinding>())
            .Where(binding => binding != null)
            .Select(binding =>
            {
                binding.featureInput = binding.featureInput ?? "";
                binding.featureOutput = binding.featureOutput ?? "";
                binding.binding = NormalizeBinding(binding.binding);
                return binding;
            })
            .Where(binding => !string.IsNullOrWhiteSpace(input ? binding.featureInput : binding.featureOutput))
            .OrderBy(binding => input ? binding.featureInput : binding.featureOutput, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string NormalizeIntegrationMode(string rawMode, PlyFeatureImplementation implementation)
    {
        if (!string.IsNullOrWhiteSpace(rawMode))
        {
            string normalized = rawMode.Trim().ToLowerInvariant();
            if (normalized == "adapter" || normalized == "bindings")
            {
                return normalized;
            }
        }

        bool hasAdapter = implementation?.adapter != null &&
            (!string.IsNullOrWhiteSpace(implementation.adapter.adapterId) ||
             !string.IsNullOrWhiteSpace(implementation.adapter.setupAdapter) ||
             !string.IsNullOrWhiteSpace(implementation.adapter.factoryId));
        return hasAdapter ? "adapter" : "bindings";
    }

    private static string NormalizeFeatureOrigin(string origin)
    {
        if (string.IsNullOrWhiteSpace(origin))
        {
            return "catalog";
        }

        string normalized = origin.Trim().ToLowerInvariant();
        return normalized == "user" ? "user" : "catalog";
    }
}
