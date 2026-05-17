using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

public static class PlyFeatureJson
{
    public static PlyFeatureManifest ImportFromFile(string filePath)
    {
        return Import(File.ReadAllText(filePath));
    }

    public static PlyFeatureManifest Import(string json)
    {
        object parsed = MiniJson.Deserialize(json);
        Dictionary<string, object> root = parsed as Dictionary<string, object>;
        if (root == null)
        {
            throw new InvalidDataException("Feature JSON root must be an object.");
        }

        bool isV2 = root.ContainsKey("version") || root.ContainsKey("implementations");
        return isV2 ? ImportV2(root) : ImportLegacy(root);
    }

    public static void ExportToFile(PlyFeatureManifest manifest, string filePath)
    {
        File.WriteAllText(filePath, Export(manifest));
    }

    public static string Export(PlyFeatureManifest manifest)
    {
        manifest = PlyFeatureSchemaUtility.NormalizeManifest(manifest);
        StringBuilder builder = new StringBuilder(4096);
        WriteManifest(builder, manifest, 0);
        return builder.ToString();
    }

    private static PlyFeatureManifest ImportV2(Dictionary<string, object> root)
    {
        PlyFeatureManifest manifest = new PlyFeatureManifest
        {
            version = ReadString(root, "version", "2.0"),
            moduleId = ReadString(root, "moduleId", ""),
            targetRoles = ReadStringList(root, "targetRoles"),
            semanticCapabilities = ReadStringList(root, "semanticCapabilities"),
            features = new List<PlySemanticFeatureDefinition>(),
            implementations = new List<PlyFeatureImplementation>()
        };

        foreach (Dictionary<string, object> featureObject in ReadObjectList(root, "features"))
        {
            manifest.features.Add(ReadSemanticFeature(featureObject));
        }

        foreach (Dictionary<string, object> implementationObject in ReadObjectList(root, "implementations"))
        {
            manifest.implementations.Add(ReadImplementation(implementationObject));
        }

        return PlyFeatureSchemaUtility.NormalizeManifest(manifest);
    }

    private static PlyFeatureManifest ImportLegacy(Dictionary<string, object> root)
    {
        PlyFeatureManifest manifest = new PlyFeatureManifest
        {
            version = "2.0",
            moduleId = ReadString(root, "moduleId", ""),
            features = new List<PlySemanticFeatureDefinition>(),
            implementations = new List<PlyFeatureImplementation>()
        };

        foreach (Dictionary<string, object> featureObject in ReadObjectList(root, "features"))
        {
            PlySemanticFeatureDefinition feature = ReadLegacySemanticFeature(featureObject);
            manifest.features.Add(feature);
            manifest.implementations.Add(ReadLegacyImplementation(featureObject, feature));
        }

        return PlyFeatureSchemaUtility.NormalizeManifest(manifest);
    }

    private static PlySemanticFeatureDefinition ReadSemanticFeature(Dictionary<string, object> featureObject)
    {
        PlySemanticFeatureDefinition feature = new PlySemanticFeatureDefinition
        {
            id = ReadString(featureObject, "id", ""),
            name = ReadString(featureObject, "name", ""),
            description = ReadString(featureObject, "description", ""),
            origin = ReadString(featureObject, "origin", "catalog"),
            intentExamples = ReadStringList(featureObject, "intentExamples"),
            targetRoles = ReadStringList(featureObject, "targetRoles"),
            category = ReadString(featureObject, "category", ""),
            tags = ReadStringList(featureObject, "tags"),
            parameters = new List<PlySemanticFeatureParameter>(),
            inputs = new List<PlySemanticFeaturePort>(),
            outputs = new List<PlySemanticFeaturePort>(),
            provides = ReadStringList(featureObject, "provides"),
            requires = ReadStringList(featureObject, "requires")
        };

        foreach (Dictionary<string, object> parameterObject in ReadObjectList(featureObject, "parameters"))
        {
            feature.parameters.Add(new PlySemanticFeatureParameter
            {
                name = ReadString(parameterObject, "name", ""),
                type = ReadDataType(ReadString(parameterObject, "type", "any")),
                required = ReadBool(parameterObject, "required", false),
                defaultValue = ReadScalarAsString(parameterObject, "defaultValue")
            });
        }

        foreach (Dictionary<string, object> portObject in ReadObjectList(featureObject, "inputs"))
        {
            feature.inputs.Add(ReadSemanticPort(portObject, PlyFeaturePortDirection.Input));
        }

        foreach (Dictionary<string, object> portObject in ReadObjectList(featureObject, "outputs"))
        {
            feature.outputs.Add(ReadSemanticPort(portObject, PlyFeaturePortDirection.Output));
        }

        return PlyFeatureSchemaUtility.NormalizeFeatureDefinition(feature);
    }

    private static PlyFeatureImplementation ReadImplementation(Dictionary<string, object> implementationObject)
    {
        PlyFeatureImplementation implementation = new PlyFeatureImplementation
        {
            id = ReadString(implementationObject, "id", ""),
            featureId = ReadString(implementationObject, "featureId", ""),
            name = ReadString(implementationObject, "name", ""),
            description = ReadString(implementationObject, "description", ""),
            targetRoles = ReadStringList(implementationObject, "targetRoles"),
            tags = ReadStringList(implementationObject, "tags"),
            integrationMode = ReadString(implementationObject, "integrationMode", "bindings"),
            source = ReadSource(implementationObject),
            capabilities = ReadCapabilitySet(implementationObject),
            parameterBindings = new List<PlyFeatureParameterBinding>(),
            inputBindings = new List<PlyFeaturePortBinding>(),
            outputBindings = new List<PlyFeaturePortBinding>(),
            adapter = ReadAdapter(implementationObject)
        };

        foreach (Dictionary<string, object> bindingObject in ReadObjectList(implementationObject, "parameterBindings"))
        {
            implementation.parameterBindings.Add(new PlyFeatureParameterBinding
            {
                featureParameter = ReadString(bindingObject, "featureParameter", ""),
                binding = ReadBindingContainer(bindingObject)
            });
        }

        foreach (Dictionary<string, object> bindingObject in ReadObjectList(implementationObject, "inputBindings"))
        {
            implementation.inputBindings.Add(new PlyFeaturePortBinding
            {
                featureInput = ReadString(bindingObject, "featureInput", ""),
                binding = ReadBindingContainer(bindingObject)
            });
        }

        foreach (Dictionary<string, object> bindingObject in ReadObjectList(implementationObject, "outputBindings"))
        {
            implementation.outputBindings.Add(new PlyFeaturePortBinding
            {
                featureOutput = ReadString(bindingObject, "featureOutput", ""),
                binding = ReadBindingContainer(bindingObject)
            });
        }

        return PlyFeatureSchemaUtility.NormalizeImplementation(implementation);
    }

    private static PlySemanticFeatureDefinition ReadLegacySemanticFeature(Dictionary<string, object> featureObject)
    {
        PlySemanticFeatureDefinition feature = new PlySemanticFeatureDefinition
        {
            id = ReadString(featureObject, "featureId", ReadString(featureObject, "id", "")),
            name = ReadString(featureObject, "name", ""),
            description = ReadString(featureObject, "description", ""),
            origin = "catalog",
            intentExamples = ReadLegacyIntentExamples(featureObject),
            targetRoles = ReadStringList(featureObject, "targetRoles"),
            category = ReadFirstString(featureObject, "categories"),
            tags = ReadStringList(featureObject, "tags"),
            parameters = new List<PlySemanticFeatureParameter>(),
            inputs = new List<PlySemanticFeaturePort>(),
            outputs = new List<PlySemanticFeaturePort>(),
            provides = ReadStringList(featureObject, "provides"),
            requires = ReadStringList(featureObject, "consumes")
        };

        foreach (Dictionary<string, object> parameterObject in ReadObjectList(featureObject, "parameters"))
        {
            feature.parameters.Add(new PlySemanticFeatureParameter
            {
                name = ReadString(parameterObject, "name", ""),
                type = ReadDataType(ReadString(parameterObject, "type", "any")),
                required = false,
                defaultValue = ReadScalarAsString(parameterObject, "defaultValue")
            });
        }

        foreach (Dictionary<string, object> portObject in ReadObjectList(featureObject, "inputs"))
        {
            feature.inputs.Add(ReadLegacySemanticPort(portObject, PlyFeaturePortDirection.Input));
        }

        foreach (Dictionary<string, object> portObject in ReadObjectList(featureObject, "outputs"))
        {
            feature.outputs.Add(ReadLegacySemanticPort(portObject, PlyFeaturePortDirection.Output));
        }

        foreach (Dictionary<string, object> portObject in ReadObjectList(featureObject, "ports"))
        {
            PlyFeaturePortDirection direction = ReadEnum(ReadString(portObject, "direction", "input"), PlyFeaturePortDirection.Input);
            PlySemanticFeaturePort port = ReadLegacySemanticPort(portObject, direction);
            if (direction == PlyFeaturePortDirection.Output)
            {
                feature.outputs.Add(port);
            }
            else
            {
                feature.inputs.Add(port);
            }
        }

        return PlyFeatureSchemaUtility.NormalizeFeatureDefinition(feature);
    }

    private static PlyFeatureImplementation ReadLegacyImplementation(Dictionary<string, object> featureObject, PlySemanticFeatureDefinition feature)
    {
        bool useAdapter = ReadBool(featureObject, "useAdapterComponent", false);
        string adapterComponentType = ReadString(featureObject, "adapterComponentType", "");

        PlyFeatureImplementation implementation = new PlyFeatureImplementation
        {
            id = ReadString(featureObject, "id", ""),
            featureId = feature != null ? feature.id : ReadString(featureObject, "featureId", ""),
            name = ReadString(featureObject, "name", ""),
            description = ReadString(featureObject, "description", ""),
            targetRoles = ReadStringList(featureObject, "targetRoles"),
            tags = ReadStringList(featureObject, "tags"),
            integrationMode = useAdapter || !string.IsNullOrWhiteSpace(adapterComponentType) ? "adapter" : "bindings",
            source = new PlyFeatureImplementationSource
            {
                kind = "module",
                moduleId = ReadString(featureObject, "moduleId", "")
            },
            capabilities = new PlyFeatureCapabilitySet
            {
                provides = ReadStringList(featureObject, "provides"),
                requires = ReadStringList(featureObject, "consumes")
            },
            adapter = new PlyFeatureAdapterReference
            {
                adapterId = adapterComponentType,
                setupAdapter = "",
                factoryId = ""
            },
            parameterBindings = new List<PlyFeatureParameterBinding>(),
            inputBindings = new List<PlyFeaturePortBinding>(),
            outputBindings = new List<PlyFeaturePortBinding>()
        };

        foreach (Dictionary<string, object> portObject in ReadObjectList(featureObject, "inputs"))
        {
            implementation.inputBindings.Add(new PlyFeaturePortBinding
            {
                featureInput = ReadString(portObject, "name", ""),
                binding = ReadBindingContainer(portObject)
            });
        }

        foreach (Dictionary<string, object> portObject in ReadObjectList(featureObject, "outputs"))
        {
            implementation.outputBindings.Add(new PlyFeaturePortBinding
            {
                featureOutput = ReadString(portObject, "name", ""),
                binding = ReadBindingContainer(portObject)
            });
        }

        foreach (Dictionary<string, object> parameterObject in ReadObjectList(featureObject, "parameters"))
        {
            implementation.parameterBindings.Add(new PlyFeatureParameterBinding
            {
                featureParameter = ReadString(parameterObject, "name", ""),
                binding = ReadBindingContainer(parameterObject)
            });
        }

        return PlyFeatureSchemaUtility.NormalizeImplementation(implementation);
    }

    private static PlySemanticFeaturePort ReadSemanticPort(Dictionary<string, object> portObject, PlyFeaturePortDirection direction)
    {
        string rawType = ReadString(portObject, "type", direction == PlyFeaturePortDirection.Output ? "Event" : "Any");
        bool isEvent = string.Equals(rawType, "Event", StringComparison.OrdinalIgnoreCase);
        return new PlySemanticFeaturePort
        {
            name = ReadString(portObject, "name", ""),
            kind = isEvent ? PlyFeaturePortKind.Event : direction == PlyFeaturePortDirection.Input ? PlyFeaturePortKind.Value : PlyFeaturePortKind.Value,
            dataType = isEvent ? PlyFeatureDataType.Void : ReadDataType(rawType),
            required = ReadBool(portObject, "required", true)
        };
    }

    private static PlySemanticFeaturePort ReadLegacySemanticPort(Dictionary<string, object> portObject, PlyFeaturePortDirection direction)
    {
        return new PlySemanticFeaturePort
        {
            name = ReadString(portObject, "name", ""),
            kind = ReadEnum(ReadString(portObject, "kind", direction == PlyFeaturePortDirection.Output ? "event" : "action"),
                direction == PlyFeaturePortDirection.Output ? PlyFeaturePortKind.Event : PlyFeaturePortKind.Action),
            dataType = ReadDataType(ReadString(portObject, "dataType", "any")),
            required = true
        };
    }

    private static PlyFeatureImplementationSource ReadSource(Dictionary<string, object> implementationObject)
    {
        Dictionary<string, object> sourceObject = ReadObject(implementationObject, "source");
        if (sourceObject == null)
        {
            return new PlyFeatureImplementationSource();
        }

        return new PlyFeatureImplementationSource
        {
            kind = ReadString(sourceObject, "kind", "module"),
            system = ReadString(sourceObject, "system", ""),
            moduleId = ReadString(sourceObject, "moduleId", "")
        };
    }

    private static PlyFeatureCapabilitySet ReadCapabilitySet(Dictionary<string, object> implementationObject)
    {
        Dictionary<string, object> capabilitiesObject = ReadObject(implementationObject, "capabilities");
        if (capabilitiesObject == null)
        {
            return new PlyFeatureCapabilitySet();
        }

        return new PlyFeatureCapabilitySet
        {
            provides = ReadStringList(capabilitiesObject, "provides"),
            requires = ReadStringList(capabilitiesObject, "requires")
        };
    }

    private static PlyFeatureAdapterReference ReadAdapter(Dictionary<string, object> implementationObject)
    {
        Dictionary<string, object> adapterObject = ReadObject(implementationObject, "adapter");
        if (adapterObject == null)
        {
            return new PlyFeatureAdapterReference();
        }

        return new PlyFeatureAdapterReference
        {
            adapterId = ReadString(adapterObject, "adapterId", ""),
            setupAdapter = ReadString(adapterObject, "setupAdapter", ""),
            factoryId = ReadString(adapterObject, "factoryId", "")
        };
    }

    private static PlyFeatureBinding ReadBindingContainer(Dictionary<string, object> owner)
    {
        Dictionary<string, object> bindingObject = ReadObject(owner, "binding");
        if (bindingObject == null)
        {
            return new PlyFeatureBinding();
        }

        return new PlyFeatureBinding
        {
            bindingKind = ReadString(bindingObject, "bindingKind", ""),
            componentType = ReadString(bindingObject, "componentType", ""),
            memberKind = ReadEnum(ReadString(bindingObject, "memberKind", "method"), PlyFeatureMemberKind.Method),
            memberName = ReadString(bindingObject, "memberName", ""),
            memberSignature = ReadString(bindingObject, "memberSignature", ""),
            conversion = ReadString(bindingObject, "conversion", ""),
            isStatic = ReadBool(bindingObject, "isStatic", false),
            access = ReadEnum(ReadString(bindingObject, "access", "readWrite"), PlyFeatureParameterAccess.ReadWrite)
        };
    }

    private static string ReadString(Dictionary<string, object> owner, string key, string fallback)
    {
        if (owner == null || !owner.TryGetValue(key, out object value) || value == null)
        {
            return fallback;
        }

        return value.ToString();
    }

    private static string ReadScalarAsString(Dictionary<string, object> owner, string key)
    {
        if (owner == null || !owner.TryGetValue(key, out object value) || value == null)
        {
            return "";
        }

        if (value is bool boolValue)
        {
            return boolValue ? "true" : "false";
        }

        if (value is long longValue)
        {
            return longValue.ToString(CultureInfo.InvariantCulture);
        }

        if (value is double doubleValue)
        {
            return doubleValue.ToString(CultureInfo.InvariantCulture);
        }

        return value.ToString();
    }

    private static bool ReadBool(Dictionary<string, object> owner, string key, bool fallback)
    {
        if (owner == null || !owner.TryGetValue(key, out object value) || value == null)
        {
            return fallback;
        }

        if (value is bool boolValue)
        {
            return boolValue;
        }

        if (bool.TryParse(value.ToString(), out bool parsed))
        {
            return parsed;
        }

        return fallback;
    }

    private static TEnum ReadEnum<TEnum>(string rawValue, TEnum fallback) where TEnum : struct
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return fallback;
        }

        string normalized = rawValue.Replace("_", "").Replace("-", "").Trim();
        foreach (string enumName in Enum.GetNames(typeof(TEnum)))
        {
            if (string.Equals(enumName, normalized, StringComparison.OrdinalIgnoreCase))
            {
                if (Enum.TryParse(enumName, true, out TEnum value))
                {
                    return value;
                }
            }
        }

        return fallback;
    }

    private static PlyFeatureDataType ReadDataType(string rawValue)
    {
        if (string.Equals(rawValue, "Event", StringComparison.OrdinalIgnoreCase))
        {
            return PlyFeatureDataType.Void;
        }

        return ReadEnum(rawValue, PlyFeatureDataType.Any);
    }

    private static List<string> ReadStringList(Dictionary<string, object> owner, string key)
    {
        List<string> result = new List<string>();
        if (owner == null || !owner.TryGetValue(key, out object value))
        {
            return result;
        }

        IList rawList = value as IList;
        if (rawList == null)
        {
            return result;
        }

        foreach (object entry in rawList)
        {
            if (entry == null)
            {
                continue;
            }

            result.Add(entry.ToString());
        }

        return PlyFeatureSchemaUtility.NormalizeStrings(result);
    }

    private static string ReadFirstString(Dictionary<string, object> owner, string key)
    {
        return ReadStringList(owner, key).FirstOrDefault() ?? "";
    }

    private static List<string> ReadLegacyIntentExamples(Dictionary<string, object> featureObject)
    {
        List<string> examples = new List<string>();
        string aiMatchDescription = ReadString(featureObject, "aiMatchDescription", "");
        if (!string.IsNullOrWhiteSpace(aiMatchDescription))
        {
            examples.Add(aiMatchDescription);
        }

        return PlyFeatureSchemaUtility.NormalizeStrings(examples);
    }

    private static Dictionary<string, object> ReadObject(Dictionary<string, object> owner, string key)
    {
        if (owner == null || !owner.TryGetValue(key, out object value))
        {
            return null;
        }

        return value as Dictionary<string, object>;
    }

    private static List<Dictionary<string, object>> ReadObjectList(Dictionary<string, object> owner, string key)
    {
        List<Dictionary<string, object>> result = new List<Dictionary<string, object>>();
        if (owner == null || !owner.TryGetValue(key, out object value))
        {
            return result;
        }

        IList rawList = value as IList;
        if (rawList == null)
        {
            return result;
        }

        foreach (object entry in rawList)
        {
            Dictionary<string, object> objectEntry = entry as Dictionary<string, object>;
            if (objectEntry != null)
            {
                result.Add(objectEntry);
            }
        }

        return result;
    }

    private static void WriteManifest(StringBuilder builder, PlyFeatureManifest manifest, int indent)
    {
        builder.AppendLine("{");
        WriteProperty(builder, indent + 1, "version", manifest.version, true);
        WriteProperty(builder, indent + 1, "moduleId", manifest.moduleId, true);
        WriteStringArray(builder, indent + 1, "targetRoles", manifest.targetRoles, true);
        WriteStringArray(builder, indent + 1, "semanticCapabilities", manifest.semanticCapabilities, true);
        WriteSemanticFeatures(builder, indent + 1, manifest.features, true);
        WriteImplementations(builder, indent + 1, manifest.implementations, false);
        Indent(builder, indent);
        builder.Append("}");
    }

    private static void WriteSemanticFeatures(StringBuilder builder, int indent, List<PlySemanticFeatureDefinition> features, bool trailingComma)
    {
        Indent(builder, indent);
        builder.Append("\"features\": [");
        if (features != null && features.Count > 0)
        {
            builder.AppendLine();
            for (int i = 0; i < features.Count; i++)
            {
                WriteSemanticFeature(builder, features[i], indent + 1);
                if (i < features.Count - 1)
                {
                    builder.Append(",");
                }

                builder.AppendLine();
            }

            Indent(builder, indent);
        }

        builder.Append("]");
        if (trailingComma)
        {
            builder.Append(",");
        }

        builder.AppendLine();
    }

    private static void WriteSemanticFeature(StringBuilder builder, PlySemanticFeatureDefinition feature, int indent)
    {
        feature = PlyFeatureSchemaUtility.NormalizeFeatureDefinition(feature);
        builder.AppendLine("{");
        WriteProperty(builder, indent, "id", feature.id, true);
        WriteProperty(builder, indent, "name", feature.name, true);
        WriteProperty(builder, indent, "description", feature.description, true);
        WriteProperty(builder, indent, "origin", feature.origin, true);
        WriteStringArray(builder, indent, "intentExamples", feature.intentExamples, true);
        WriteStringArray(builder, indent, "targetRoles", feature.targetRoles, true);
        WriteProperty(builder, indent, "category", feature.category, true);
        WriteStringArray(builder, indent, "tags", feature.tags, true);
        WriteSemanticParameters(builder, indent, feature.parameters, true);
        WriteSemanticPorts(builder, indent, "inputs", feature.inputs, PlyFeaturePortDirection.Input, true);
        WriteSemanticPorts(builder, indent, "outputs", feature.outputs, PlyFeaturePortDirection.Output, true);
        WriteStringArray(builder, indent, "provides", feature.provides, true);
        WriteStringArray(builder, indent, "requires", feature.requires, false);
        Indent(builder, indent - 1);
        builder.Append("}");
    }

    private static void WriteImplementations(StringBuilder builder, int indent, List<PlyFeatureImplementation> implementations, bool trailingComma)
    {
        Indent(builder, indent);
        builder.Append("\"implementations\": [");
        if (implementations != null && implementations.Count > 0)
        {
            builder.AppendLine();
            for (int i = 0; i < implementations.Count; i++)
            {
                WriteImplementation(builder, implementations[i], indent + 1);
                if (i < implementations.Count - 1)
                {
                    builder.Append(",");
                }

                builder.AppendLine();
            }

            Indent(builder, indent);
        }

        builder.Append("]");
        if (trailingComma)
        {
            builder.Append(",");
        }

        builder.AppendLine();
    }

    private static void WriteImplementation(StringBuilder builder, PlyFeatureImplementation implementation, int indent)
    {
        implementation = PlyFeatureSchemaUtility.NormalizeImplementation(implementation);
        builder.AppendLine("{");
        WriteProperty(builder, indent, "id", implementation.id, true);
        WriteProperty(builder, indent, "featureId", implementation.featureId, true);
        WriteProperty(builder, indent, "name", implementation.name, true);
        WriteProperty(builder, indent, "description", implementation.description, true);
        WriteStringArray(builder, indent, "targetRoles", implementation.targetRoles, true);
        WriteStringArray(builder, indent, "tags", implementation.tags, true);
        WriteSource(builder, indent, implementation.source, true);
        WriteProperty(builder, indent, "integrationMode", implementation.integrationMode, true);
        WriteCapabilitySet(builder, indent, implementation.capabilities, true);
        if (string.Equals(implementation.integrationMode, "adapter", StringComparison.OrdinalIgnoreCase))
        {
            WriteAdapter(builder, indent, implementation.adapter, false);
        }
        else
        {
            WriteParameterBindings(builder, indent, implementation.parameterBindings, true);
            WritePortBindings(builder, indent, "inputBindings", implementation.inputBindings, true);
            WritePortBindings(builder, indent, "outputBindings", implementation.outputBindings, false);
        }

        Indent(builder, indent - 1);
        builder.Append("}");
    }

    private static void WriteSemanticParameters(StringBuilder builder, int indent, List<PlySemanticFeatureParameter> parameters, bool trailingComma)
    {
        Indent(builder, indent);
        builder.Append("\"parameters\": [");
        if (parameters != null && parameters.Count > 0)
        {
            builder.AppendLine();
            for (int i = 0; i < parameters.Count; i++)
            {
                PlySemanticFeatureParameter parameter = parameters[i] ?? new PlySemanticFeatureParameter();
                Indent(builder, indent + 1);
                builder.AppendLine("{");
                WriteProperty(builder, indent + 2, "name", parameter.name, true);
                WriteProperty(builder, indent + 2, "type", ToSchemaType(parameter.type), true);
                WriteBoolProperty(builder, indent + 2, "required", parameter.required, true);
                WriteScalarProperty(builder, indent + 2, "defaultValue", parameter.defaultValue, false);
                Indent(builder, indent + 1);
                builder.Append("}");
                if (i < parameters.Count - 1)
                {
                    builder.Append(",");
                }

                builder.AppendLine();
            }

            Indent(builder, indent);
        }

        builder.Append("]");
        if (trailingComma)
        {
            builder.Append(",");
        }

        builder.AppendLine();
    }

    private static void WriteSemanticPorts(StringBuilder builder, int indent, string propertyName, List<PlySemanticFeaturePort> ports, PlyFeaturePortDirection direction, bool trailingComma)
    {
        Indent(builder, indent);
        builder.Append("\"").Append(Escape(propertyName)).Append("\": [");
        if (ports != null && ports.Count > 0)
        {
            builder.AppendLine();
            for (int i = 0; i < ports.Count; i++)
            {
                PlySemanticFeaturePort port = ports[i] ?? new PlySemanticFeaturePort();
                Indent(builder, indent + 1);
                builder.AppendLine("{");
                WriteProperty(builder, indent + 2, "name", port.name, true);
                WriteProperty(builder, indent + 2, "type", ToSchemaPortType(port, direction), true);
                WriteProperty(builder, indent + 2, "direction", direction == PlyFeaturePortDirection.Input ? "input" : "output", true);
                WriteBoolProperty(builder, indent + 2, "required", port.required, false);
                Indent(builder, indent + 1);
                builder.Append("}");
                if (i < ports.Count - 1)
                {
                    builder.Append(",");
                }

                builder.AppendLine();
            }

            Indent(builder, indent);
        }

        builder.Append("]");
        if (trailingComma)
        {
            builder.Append(",");
        }

        builder.AppendLine();
    }

    private static void WriteSource(StringBuilder builder, int indent, PlyFeatureImplementationSource source, bool trailingComma)
    {
        source = source ?? new PlyFeatureImplementationSource();
        Indent(builder, indent);
        builder.AppendLine("\"source\": {");
        WriteProperty(builder, indent + 1, "kind", source.kind, true);
        WriteNullableStringProperty(builder, indent + 1, "system", source.system, true);
        WriteNullableStringProperty(builder, indent + 1, "moduleId", source.moduleId, false);
        Indent(builder, indent);
        builder.Append("}");
        if (trailingComma)
        {
            builder.Append(",");
        }

        builder.AppendLine();
    }

    private static void WriteCapabilitySet(StringBuilder builder, int indent, PlyFeatureCapabilitySet capabilities, bool trailingComma)
    {
        capabilities = capabilities ?? new PlyFeatureCapabilitySet();
        Indent(builder, indent);
        builder.AppendLine("\"capabilities\": {");
        WriteStringArray(builder, indent + 1, "provides", capabilities.provides, true);
        WriteStringArray(builder, indent + 1, "requires", capabilities.requires, false);
        Indent(builder, indent);
        builder.Append("}");
        if (trailingComma)
        {
            builder.Append(",");
        }

        builder.AppendLine();
    }

    private static void WriteAdapter(StringBuilder builder, int indent, PlyFeatureAdapterReference adapter, bool trailingComma)
    {
        adapter = adapter ?? new PlyFeatureAdapterReference();
        Indent(builder, indent);
        builder.AppendLine("\"adapter\": {");
        WriteNullableStringProperty(builder, indent + 1, "adapterId", adapter.adapterId, true);
        WriteNullableStringProperty(builder, indent + 1, "setupAdapter", adapter.setupAdapter, true);
        WriteNullableStringProperty(builder, indent + 1, "factoryId", adapter.factoryId, false);
        Indent(builder, indent);
        builder.Append("}");
        if (trailingComma)
        {
            builder.Append(",");
        }

        builder.AppendLine();
    }

    private static void WriteParameterBindings(StringBuilder builder, int indent, List<PlyFeatureParameterBinding> bindings, bool trailingComma)
    {
        Indent(builder, indent);
        builder.Append("\"parameterBindings\": [");
        if (bindings != null && bindings.Count > 0)
        {
            builder.AppendLine();
            for (int i = 0; i < bindings.Count; i++)
            {
                PlyFeatureParameterBinding binding = bindings[i] ?? new PlyFeatureParameterBinding();
                Indent(builder, indent + 1);
                builder.AppendLine("{");
                WriteProperty(builder, indent + 2, "featureParameter", binding.featureParameter, true);
                WriteBinding(builder, indent + 2, binding.binding, false);
                Indent(builder, indent + 1);
                builder.Append("}");
                if (i < bindings.Count - 1)
                {
                    builder.Append(",");
                }

                builder.AppendLine();
            }

            Indent(builder, indent);
        }

        builder.Append("]");
        if (trailingComma)
        {
            builder.Append(",");
        }

        builder.AppendLine();
    }

    private static void WritePortBindings(StringBuilder builder, int indent, string propertyName, List<PlyFeaturePortBinding> bindings, bool trailingComma)
    {
        bool isInput = string.Equals(propertyName, "inputBindings", StringComparison.Ordinal);
        Indent(builder, indent);
        builder.Append("\"").Append(Escape(propertyName)).Append("\": [");
        if (bindings != null && bindings.Count > 0)
        {
            builder.AppendLine();
            for (int i = 0; i < bindings.Count; i++)
            {
                PlyFeaturePortBinding binding = bindings[i] ?? new PlyFeaturePortBinding();
                Indent(builder, indent + 1);
                builder.AppendLine("{");
                WriteProperty(builder, indent + 2, isInput ? "featureInput" : "featureOutput", isInput ? binding.featureInput : binding.featureOutput, true);
                WriteBinding(builder, indent + 2, binding.binding, false);
                Indent(builder, indent + 1);
                builder.Append("}");
                if (i < bindings.Count - 1)
                {
                    builder.Append(",");
                }

                builder.AppendLine();
            }

            Indent(builder, indent);
        }

        builder.Append("]");
        if (trailingComma)
        {
            builder.Append(",");
        }

        builder.AppendLine();
    }

    private static void WriteBinding(StringBuilder builder, int indent, PlyFeatureBinding binding, bool trailingComma)
    {
        binding = PlyFeatureSchemaUtility.NormalizeBinding(binding);
        Indent(builder, indent);
        builder.AppendLine("\"binding\": {");
        WriteProperty(builder, indent + 1, "bindingKind", binding.bindingKind, true);
        WriteProperty(builder, indent + 1, "componentType", binding.componentType, true);
        WriteProperty(builder, indent + 1, "memberKind", ToCamelCase(binding.memberKind), true);
        WriteProperty(builder, indent + 1, "memberName", binding.memberName, true);
        WriteProperty(builder, indent + 1, "memberSignature", binding.memberSignature, true);
        WriteProperty(builder, indent + 1, "conversion", binding.conversion, true);
        WriteBoolProperty(builder, indent + 1, "isStatic", binding.isStatic, true);
        WriteProperty(builder, indent + 1, "access", ToCamelCase(binding.access), false);
        Indent(builder, indent);
        builder.Append("}");
        if (trailingComma)
        {
            builder.Append(",");
        }

        builder.AppendLine();
    }

    private static void WriteStringArray(StringBuilder builder, int indent, string name, List<string> values, bool trailingComma)
    {
        Indent(builder, indent);
        builder.Append("\"").Append(Escape(name)).Append("\": [");
        if (values != null && values.Count > 0)
        {
            builder.Append(string.Join(", ", values.ConvertAll(value => "\"" + Escape(value) + "\"").ToArray()));
        }

        builder.Append("]");
        if (trailingComma)
        {
            builder.Append(",");
        }

        builder.AppendLine();
    }

    private static void WriteProperty(StringBuilder builder, int indent, string name, string value, bool trailingComma)
    {
        Indent(builder, indent);
        builder.Append("\"").Append(Escape(name)).Append("\": \"").Append(Escape(value ?? "")).Append("\"");
        if (trailingComma)
        {
            builder.Append(",");
        }

        builder.AppendLine();
    }

    private static void WriteNullableStringProperty(StringBuilder builder, int indent, string name, string value, bool trailingComma)
    {
        Indent(builder, indent);
        builder.Append("\"").Append(Escape(name)).Append("\": ");
        if (string.IsNullOrWhiteSpace(value))
        {
            builder.Append("null");
        }
        else
        {
            builder.Append("\"").Append(Escape(value)).Append("\"");
        }

        if (trailingComma)
        {
            builder.Append(",");
        }

        builder.AppendLine();
    }

    private static void WriteBoolProperty(StringBuilder builder, int indent, string name, bool value, bool trailingComma)
    {
        Indent(builder, indent);
        builder.Append("\"").Append(Escape(name)).Append("\": ").Append(value ? "true" : "false");
        if (trailingComma)
        {
            builder.Append(",");
        }

        builder.AppendLine();
    }

    private static void WriteScalarProperty(StringBuilder builder, int indent, string name, string rawValue, bool trailingComma)
    {
        Indent(builder, indent);
        builder.Append("\"").Append(Escape(name)).Append("\": ");
        if (bool.TryParse(rawValue, out bool boolValue))
        {
            builder.Append(boolValue ? "true" : "false");
        }
        else if (long.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out long longValue))
        {
            builder.Append(longValue.ToString(CultureInfo.InvariantCulture));
        }
        else if (double.TryParse(rawValue, NumberStyles.Float, CultureInfo.InvariantCulture, out double doubleValue))
        {
            builder.Append(doubleValue.ToString(CultureInfo.InvariantCulture));
        }
        else
        {
            builder.Append("\"").Append(Escape(rawValue ?? "")).Append("\"");
        }

        if (trailingComma)
        {
            builder.Append(",");
        }

        builder.AppendLine();
    }

    private static string ToSchemaPortType(PlySemanticFeaturePort port, PlyFeaturePortDirection direction)
    {
        if (direction == PlyFeaturePortDirection.Output && port.kind == PlyFeaturePortKind.Event)
        {
            return "Event";
        }

        return ToSchemaType(port.dataType);
    }

    private static string ToSchemaType(PlyFeatureDataType dataType)
    {
        switch (dataType)
        {
            case PlyFeatureDataType.Bool: return "bool";
            case PlyFeatureDataType.Float: return "float";
            case PlyFeatureDataType.Int: return "int";
            case PlyFeatureDataType.String: return "string";
            case PlyFeatureDataType.GameObject: return "GameObject";
            case PlyFeatureDataType.Vector3: return "Vector3";
            case PlyFeatureDataType.Void: return "void";
            default: return "Any";
        }
    }

    private static string ToCamelCase<TEnum>(TEnum value) where TEnum : struct
    {
        string raw = value.ToString();
        return string.IsNullOrEmpty(raw) ? "" : char.ToLowerInvariant(raw[0]) + raw.Substring(1);
    }

    private static void Indent(StringBuilder builder, int indent)
    {
        for (int i = 0; i < indent; i++)
        {
            builder.Append("  ");
        }
    }

    private static string Escape(string value)
    {
        return (value ?? "")
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\r", "\\r")
            .Replace("\n", "\\n")
            .Replace("\t", "\\t");
    }

    private static class MiniJson
    {
        public static object Deserialize(string json)
        {
            if (json == null)
            {
                return null;
            }

            return Parser.Parse(json);
        }

        private sealed class Parser : IDisposable
        {
            private enum Token
            {
                None,
                CurlyOpen,
                CurlyClose,
                SquaredOpen,
                SquaredClose,
                Colon,
                Comma,
                String,
                Number,
                True,
                False,
                Null
            }

            private readonly StringReader reader;

            private Parser(string jsonString)
            {
                reader = new StringReader(jsonString);
            }

            public static object Parse(string jsonString)
            {
                using (Parser instance = new Parser(jsonString))
                {
                    return instance.ParseValue();
                }
            }

            public void Dispose()
            {
                reader.Dispose();
            }

            private Dictionary<string, object> ParseObject()
            {
                Dictionary<string, object> table = new Dictionary<string, object>();
                reader.Read();

                while (true)
                {
                    Token nextToken = NextToken;
                    if (nextToken == Token.None)
                    {
                        return null;
                    }

                    if (nextToken == Token.Comma)
                    {
                        continue;
                    }

                    if (nextToken == Token.CurlyClose)
                    {
                        return table;
                    }

                    string name = ParseString();
                    if (NextToken != Token.Colon)
                    {
                        return null;
                    }

                    reader.Read();
                    table[name] = ParseValue();
                }
            }

            private List<object> ParseArray()
            {
                List<object> array = new List<object>();
                reader.Read();

                bool parsing = true;
                while (parsing)
                {
                    Token nextToken = NextToken;
                    switch (nextToken)
                    {
                        case Token.None:
                            return null;
                        case Token.Comma:
                            continue;
                        case Token.SquaredClose:
                            parsing = false;
                            break;
                        default:
                            array.Add(ParseByToken(nextToken));
                            break;
                    }
                }

                return array;
            }

            private object ParseValue()
            {
                Token nextToken = NextToken;
                return ParseByToken(nextToken);
            }

            private object ParseByToken(Token token)
            {
                switch (token)
                {
                    case Token.String:
                        return ParseString();
                    case Token.Number:
                        return ParseNumber();
                    case Token.CurlyOpen:
                        return ParseObject();
                    case Token.SquaredOpen:
                        return ParseArray();
                    case Token.True:
                        return true;
                    case Token.False:
                        return false;
                    case Token.Null:
                        return null;
                }

                return null;
            }

            private string ParseString()
            {
                StringBuilder builder = new StringBuilder();
                char quote = Convert.ToChar(reader.Read());
                bool parsing = true;

                while (parsing)
                {
                    if (reader.Peek() == -1)
                    {
                        break;
                    }

                    char c = Convert.ToChar(reader.Read());
                    switch (c)
                    {
                        case '"':
                            if (quote == '"')
                            {
                                parsing = false;
                            }
                            else
                            {
                                builder.Append(c);
                            }
                            break;
                        case '\\':
                            if (reader.Peek() == -1)
                            {
                                parsing = false;
                                break;
                            }

                            c = Convert.ToChar(reader.Read());
                            switch (c)
                            {
                                case '"': builder.Append('"'); break;
                                case '\\': builder.Append('\\'); break;
                                case '/': builder.Append('/'); break;
                                case 'b': builder.Append('\b'); break;
                                case 'f': builder.Append('\f'); break;
                                case 'n': builder.Append('\n'); break;
                                case 'r': builder.Append('\r'); break;
                                case 't': builder.Append('\t'); break;
                                case 'u':
                                    char[] hex = new char[4];
                                    for (int i = 0; i < 4; i++)
                                    {
                                        hex[i] = Convert.ToChar(reader.Read());
                                    }

                                    builder.Append((char)Convert.ToInt32(new string(hex), 16));
                                    break;
                            }
                            break;
                        default:
                            builder.Append(c);
                            break;
                    }
                }

                return builder.ToString();
            }

            private object ParseNumber()
            {
                string number = NextWord;
                if (number.IndexOf('.') == -1 && number.IndexOf('e') == -1 && number.IndexOf('E') == -1)
                {
                    if (long.TryParse(number, NumberStyles.Integer, CultureInfo.InvariantCulture, out long parsedInt))
                    {
                        return parsedInt;
                    }
                }

                if (double.TryParse(number, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsedDouble))
                {
                    return parsedDouble;
                }

                return 0d;
            }

            private void EatWhitespace()
            {
                while (char.IsWhiteSpace(PeekChar))
                {
                    reader.Read();
                    if (reader.Peek() == -1)
                    {
                        break;
                    }
                }
            }

            private char PeekChar
            {
                get
                {
                    int peek = reader.Peek();
                    return peek == -1 ? '\0' : Convert.ToChar(peek);
                }
            }

            private string NextWord
            {
                get
                {
                    StringBuilder builder = new StringBuilder();
                    while (!IsWordBreak(PeekChar))
                    {
                        builder.Append(Convert.ToChar(reader.Read()));
                        if (reader.Peek() == -1)
                        {
                            break;
                        }
                    }

                    return builder.ToString();
                }
            }

            private Token NextToken
            {
                get
                {
                    EatWhitespace();
                    if (reader.Peek() == -1)
                    {
                        return Token.None;
                    }

                    switch (PeekChar)
                    {
                        case '{': return Token.CurlyOpen;
                        case '}':
                            reader.Read();
                            return Token.CurlyClose;
                        case '[': return Token.SquaredOpen;
                        case ']':
                            reader.Read();
                            return Token.SquaredClose;
                        case ',':
                            reader.Read();
                            return Token.Comma;
                        case '"': return Token.String;
                        case ':': return Token.Colon;
                        case '0':
                        case '1':
                        case '2':
                        case '3':
                        case '4':
                        case '5':
                        case '6':
                        case '7':
                        case '8':
                        case '9':
                        case '-': return Token.Number;
                        case 'f':
                            ConsumeWord("false");
                            return Token.False;
                        case 't':
                            ConsumeWord("true");
                            return Token.True;
                        case 'n':
                            ConsumeWord("null");
                            return Token.Null;
                    }

                    return Token.None;
                }
            }

            private void ConsumeWord(string word)
            {
                for (int i = 0; i < word.Length; i++)
                {
                    reader.Read();
                }
            }

            private static bool IsWordBreak(char c)
            {
                return char.IsWhiteSpace(c) || c == ',' || c == ':' || c == ']' || c == '}' || c == '[' || c == '{' || c == '"' || c == '\0';
            }
        }
    }
}
