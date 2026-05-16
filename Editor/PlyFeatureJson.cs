using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
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

        PlyFeatureManifest manifest = new PlyFeatureManifest();
        manifest.schemaVersion = ReadString(root, "schemaVersion", "1.0");
        manifest.moduleId = ReadString(root, "moduleId", "");
        manifest.features = new List<PlyFeatureProfile>();

        foreach (Dictionary<string, object> featureObject in ReadObjectList(root, "features"))
        {
            PlyFeatureProfile feature = new PlyFeatureProfile();
            feature.id = ReadString(featureObject, "id", "");
            feature.featureId = ReadString(featureObject, "featureId", "");
            feature.name = ReadString(featureObject, "name", "");
            feature.description = ReadString(featureObject, "description", "");
            feature.aiMatchDescription = ReadString(featureObject, "aiMatchDescription", "");
            feature.tags = ReadStringList(featureObject, "tags");
            feature.categories = ReadStringList(featureObject, "categories");
            feature.implements = ReadStringList(featureObject, "implements");
            feature.provides = ReadStringList(featureObject, "provides");
            feature.consumes = ReadStringList(featureObject, "consumes");
            feature.targetRoles = ReadStringList(featureObject, "targetRoles");
            feature.useAdapterComponent = ReadBool(featureObject, "useAdapterComponent", false);
            feature.adapterComponentType = ReadString(featureObject, "adapterComponentType", "");
            feature.componentRequirements = new List<PlyFeatureComponentRequirement>();
            feature.ports = new List<PlyFeaturePortMapping>();
            feature.parameters = new List<PlyFeatureParameterMapping>();

            foreach (Dictionary<string, object> requirementObject in ReadObjectList(featureObject, "componentRequirements"))
            {
                feature.componentRequirements.Add(new PlyFeatureComponentRequirement
                {
                    typeName = ReadString(requirementObject, "typeName", ""),
                    assemblyQualifiedName = ReadString(requirementObject, "assemblyQualifiedName", ""),
                    required = ReadBool(requirementObject, "required", true)
                });
            }

            foreach (Dictionary<string, object> portObject in ReadObjectList(featureObject, "ports"))
            {
                feature.ports.Add(new PlyFeaturePortMapping
                {
                    name = ReadString(portObject, "name", ""),
                    direction = ReadEnum(ReadString(portObject, "direction", "input"), PlyFeaturePortDirection.Input),
                    kind = ReadEnum(ReadString(portObject, "kind", "action"), PlyFeaturePortKind.Action),
                    dataType = ReadEnum(ReadString(portObject, "dataType", "any"), PlyFeatureDataType.Any),
                    binding = ReadBinding(portObject)
                });
            }

            foreach (Dictionary<string, object> parameterObject in ReadObjectList(featureObject, "parameters"))
            {
                PlyFeatureParameterMapping parameter = new PlyFeatureParameterMapping
                {
                    name = ReadString(parameterObject, "name", ""),
                    direction = ReadString(parameterObject, "direction", "parameter"),
                    type = ReadEnum(ReadString(parameterObject, "type", "any"), PlyFeatureDataType.Any),
                    defaultValue = ReadScalarAsString(parameterObject, "defaultValue"),
                    accessMode = ReadEnum(ReadString(parameterObject, "accessMode", "readWrite"), PlyFeatureParameterAccess.ReadWrite),
                    binding = ReadBinding(parameterObject)
                };
                feature.parameters.Add(parameter);
            }

            manifest.features.Add(PlyFeatureSchemaUtility.NormalizeFeature(feature));
        }

        return PlyFeatureSchemaUtility.NormalizeManifest(manifest);
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

    private static PlyFeatureBinding ReadBinding(Dictionary<string, object> owner)
    {
        Dictionary<string, object> bindingObject = null;
        if (owner != null && owner.TryGetValue("binding", out object bindingValue))
        {
            bindingObject = bindingValue as Dictionary<string, object>;
        }

        if (bindingObject == null)
        {
            return new PlyFeatureBinding();
        }

        return new PlyFeatureBinding
        {
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
        WriteProperty(builder, indent + 1, "schemaVersion", manifest.schemaVersion, true);
        WriteProperty(builder, indent + 1, "moduleId", manifest.moduleId, true);
        Indent(builder, indent + 1);
        builder.Append("\"features\": [");
        if (manifest.features.Count > 0)
        {
            builder.AppendLine();
            for (int i = 0; i < manifest.features.Count; i++)
            {
                WriteFeature(builder, manifest.features[i], indent + 2);
                if (i < manifest.features.Count - 1)
                {
                    builder.Append(",");
                }

                builder.AppendLine();
            }

            Indent(builder, indent + 1);
        }

        builder.AppendLine("]");
        Indent(builder, indent);
        builder.Append("}");
    }

    private static void WriteFeature(StringBuilder builder, PlyFeatureProfile feature, int indent)
    {
        builder.AppendLine("{");
        WriteProperty(builder, indent, "id", feature.id, true);
        WriteProperty(builder, indent, "featureId", feature.featureId, true);
        WriteProperty(builder, indent, "name", feature.name, true);
        WriteProperty(builder, indent, "description", feature.description, true);
        WriteProperty(builder, indent, "aiMatchDescription", feature.aiMatchDescription, true);
        WriteStringArray(builder, indent, "tags", feature.tags, true);
        WriteStringArray(builder, indent, "categories", feature.categories, true);
        WriteStringArray(builder, indent, "implements", feature.implements, true);
        WriteStringArray(builder, indent, "provides", feature.provides, true);
        WriteStringArray(builder, indent, "consumes", feature.consumes, true);
        WriteStringArray(builder, indent, "targetRoles", feature.targetRoles, true);
        WriteBoolProperty(builder, indent, "useAdapterComponent", feature.useAdapterComponent, true);
        WriteProperty(builder, indent, "adapterComponentType", feature.adapterComponentType, true);
        WriteComponentRequirements(builder, indent, feature.componentRequirements, true);
        WritePorts(builder, indent, feature.ports, true);
        WriteParameters(builder, indent, feature.parameters, false);
        Indent(builder, indent - 1);
        builder.Append("}");
    }

    private static void WriteComponentRequirements(StringBuilder builder, int indent, List<PlyFeatureComponentRequirement> requirements, bool trailingComma)
    {
        Indent(builder, indent);
        builder.Append("\"componentRequirements\": [");
        if (requirements != null && requirements.Count > 0)
        {
            builder.AppendLine();
            for (int i = 0; i < requirements.Count; i++)
            {
                PlyFeatureComponentRequirement requirement = requirements[i] ?? new PlyFeatureComponentRequirement();
                Indent(builder, indent + 1);
                builder.AppendLine("{");
                WriteProperty(builder, indent + 2, "typeName", requirement.typeName, true);
                WriteProperty(builder, indent + 2, "assemblyQualifiedName", requirement.assemblyQualifiedName, true);
                WriteBoolProperty(builder, indent + 2, "required", requirement.required, false);
                Indent(builder, indent + 1);
                builder.Append("}");
                if (i < requirements.Count - 1)
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

    private static void WritePorts(StringBuilder builder, int indent, List<PlyFeaturePortMapping> ports, bool trailingComma)
    {
        Indent(builder, indent);
        builder.Append("\"ports\": [");
        if (ports != null && ports.Count > 0)
        {
            builder.AppendLine();
            for (int i = 0; i < ports.Count; i++)
            {
                PlyFeaturePortMapping port = ports[i] ?? new PlyFeaturePortMapping();
                Indent(builder, indent + 1);
                builder.AppendLine("{");
                WriteProperty(builder, indent + 2, "name", port.name, true);
                WriteProperty(builder, indent + 2, "direction", ToCamelCase(port.direction), true);
                WriteProperty(builder, indent + 2, "kind", ToCamelCase(port.kind), true);
                WriteProperty(builder, indent + 2, "dataType", ToCamelCase(port.dataType), true);
                WriteBinding(builder, indent + 2, port.binding, false);
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

    private static void WriteParameters(StringBuilder builder, int indent, List<PlyFeatureParameterMapping> parameters, bool trailingComma)
    {
        Indent(builder, indent);
        builder.Append("\"parameters\": [");
        if (parameters != null && parameters.Count > 0)
        {
            builder.AppendLine();
            for (int i = 0; i < parameters.Count; i++)
            {
                PlyFeatureParameterMapping parameter = parameters[i] ?? new PlyFeatureParameterMapping();
                Indent(builder, indent + 1);
                builder.AppendLine("{");
                WriteProperty(builder, indent + 2, "name", parameter.name, true);
                WriteProperty(builder, indent + 2, "direction", parameter.direction, true);
                WriteProperty(builder, indent + 2, "type", ToCamelCase(parameter.type), true);
                WriteScalarProperty(builder, indent + 2, "defaultValue", parameter.defaultValue, true);
                WriteProperty(builder, indent + 2, "accessMode", ToCamelCase(parameter.accessMode), true);
                WriteBinding(builder, indent + 2, parameter.binding, false);
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

    private static void WriteBinding(StringBuilder builder, int indent, PlyFeatureBinding binding, bool trailingComma)
    {
        binding = PlyFeatureSchemaUtility.NormalizeBinding(binding);
        Indent(builder, indent);
        builder.AppendLine("\"binding\": {");
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
