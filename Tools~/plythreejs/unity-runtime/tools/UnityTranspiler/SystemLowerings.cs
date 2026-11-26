using System.Text.Json;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

interface ISystemLowering
{
    string? TryLower(IMethodSymbol symbol, InvocationExpressionSyntax invocation,
        SemanticModel model, Func<ExpressionSyntax, string> emitExpression);
    string EmitPrelude();
    IEnumerable<string> RuntimeImports { get; }
}

sealed class SystemLoweringRegistry
{
    private readonly List<ISystemLowering> handlers = [];

    public SystemLoweringRegistry(string? unityProjectRoot)
    {
        handlers.Add(new UnityEventListenerLowering());
        if (unityProjectRoot != null)
        {
            handlers.Add(new AssetDatabaseLowering(unityProjectRoot));
            handlers.Add(new PrefabUtilityLowering());
            handlers.Add(new AssetImporterLowering());
        }
    }

    public string? TryLower(InvocationExpressionSyntax invocation, SemanticModel model,
        Func<ExpressionSyntax, string> emitExpression)
    {
        if (model.GetSymbolInfo(invocation).Symbol is not IMethodSymbol symbol) return null;
        foreach (var handler in handlers)
        {
            string? replacement = handler.TryLower(symbol, invocation, model, emitExpression);
            if (replacement != null) return replacement;
        }
        return null;
    }

    public string EmitPrelude() => string.Join("", handlers.Select(handler => handler.EmitPrelude()));
    public IEnumerable<string> RuntimeImports => handlers.SelectMany(handler => handler.RuntimeImports).Distinct();
}

sealed class AssetDatabaseLowering(string projectRoot) : ISystemLowering
{
    private readonly Dictionary<string, PreparedTextAsset> assets = new(StringComparer.OrdinalIgnoreCase);
    private bool runtimeLookup;
    public IEnumerable<string> RuntimeImports => (assets.Count > 0 ? new[] { "TextAsset" } : Array.Empty<string>())
        .Concat(runtimeLookup ? new[] { "AssetDatabase", "GameObject" } : Array.Empty<string>());

    public string? TryLower(IMethodSymbol symbol, InvocationExpressionSyntax invocation,
        SemanticModel model, Func<ExpressionSyntax, string> emitExpression)
    {
        if (symbol.ContainingType.ToDisplayString() != "UnityEditor.AssetDatabase") return null;
        if (symbol.Name == "SaveAssets" && invocation.ArgumentList.Arguments.Count == 0)
        {
            runtimeLookup = true;
            return "AssetDatabase.SaveAssets()";
        }
        if (symbol.Name == "Refresh" && invocation.ArgumentList.Arguments.Count == 0)
        {
            runtimeLookup = true;
            return "AssetDatabase.Refresh()";
        }
        if (symbol.Name == "GetAssetPath" && invocation.ArgumentList.Arguments.Count == 1)
        {
            runtimeLookup = true;
            return $"AssetDatabase.GetAssetPath({emitExpression(invocation.ArgumentList.Arguments[0].Expression)})";
        }
        if (symbol.Name is "FindAssets" or "GUIDToAssetPath" or "LoadAllAssetsAtPath"
            && invocation.ArgumentList.Arguments.Count == 1)
        {
            runtimeLookup = true;
            return $"AssetDatabase.{symbol.Name}({emitExpression(invocation.ArgumentList.Arguments[0].Expression)})";
        }
        if (symbol.Name != "LoadAssetAtPath")
            throw Unsupported(invocation, $"No build-time substitution for AssetDatabase.{symbol.Name}");
        if (symbol.TypeArguments.Length == 1
            && symbol.TypeArguments[0].ToDisplayString() != "UnityEngine.TextAsset"
            && invocation.ArgumentList.Arguments.Count == 1)
        {
            runtimeLookup = true;
            string typeName = JsonSerializer.Serialize(symbol.TypeArguments[0].ToDisplayString());
            return $"AssetDatabase.LoadAssetAtPath({emitExpression(invocation.ArgumentList.Arguments[0].Expression)}, {typeName})";
        }
        if (symbol.TypeArguments.Length != 1
            || symbol.TypeArguments[0].ToDisplayString() != "UnityEngine.TextAsset"
            || invocation.ArgumentList.Arguments.Count != 1)
            throw Unsupported(invocation, "AssetDatabase.LoadAssetAtPath requires one path argument and a type argument");
        var constant = model.GetConstantValue(invocation.ArgumentList.Arguments[0].Expression);
        if (!constant.HasValue || constant.Value is not string unityPath)
            throw Unsupported(invocation, "AssetDatabase.LoadAssetAtPath requires a build-time constant path");
        string normalized = unityPath.Replace('\\', '/');
        if (!normalized.StartsWith("Assets/", StringComparison.Ordinal)
            || normalized.Split('/').Any(part => part is "" or "." or ".."))
            throw Unsupported(invocation, $"Asset path must be a normalized Assets/ path: {unityPath}");
        string fullPath = Path.GetFullPath(Path.Combine(projectRoot, normalized.Replace('/', Path.DirectorySeparatorChar)));
        string assetsRoot = Path.GetFullPath(Path.Combine(projectRoot, "Assets"));
        if (!fullPath.StartsWith(assetsRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw Unsupported(invocation, $"Asset path escapes the Unity project: {unityPath}");
        if (!File.Exists(fullPath)) throw Unsupported(invocation, $"Asset not found: {unityPath}");
        if (Path.GetExtension(fullPath).ToLowerInvariant() is not
            (".txt" or ".json" or ".csv" or ".xml" or ".yaml" or ".yml" or ".md"))
            throw Unsupported(invocation, $"TextAsset substitution does not support this asset format: {unityPath}");
        string meta = fullPath + ".meta";
        if (!File.Exists(meta)) throw Unsupported(invocation, $"Asset GUID metadata not found: {unityPath}.meta");
        var match = Regex.Match(File.ReadAllText(meta), @"(?m)^guid:\s*([0-9a-f]{32})\s*$", RegexOptions.IgnoreCase);
        if (!match.Success) throw Unsupported(invocation, $"Asset GUID is invalid: {unityPath}.meta");
        string guid = match.Groups[1].Value.ToLowerInvariant();
        if (assets.TryGetValue(guid, out var existing) && existing.Path != normalized)
            throw Unsupported(invocation, $"Asset GUID {guid} is shared by {existing.Path} and {normalized}");
        if (!assets.ContainsKey(guid))
        {
            byte[] bytes;
            try {
                File.ReadAllText(fullPath, new UTF8Encoding(false, true));
                bytes = File.ReadAllBytes(fullPath);
            }
            catch (DecoderFallbackException)
            {
                throw Unsupported(invocation, $"TextAsset is not valid UTF-8: {unityPath}");
            }
            assets.Add(guid, new PreparedTextAsset(guid, normalized, bytes));
        }
        return $"preparedAssets.get({JsonSerializer.Serialize(guid)})";
    }

    public string EmitPrelude()
    {
        if (assets.Count == 0) return "";
        var records = assets.Values.OrderBy(asset => asset.Id, StringComparer.Ordinal).ToArray();
        string manifest = JsonSerializer.Serialize(records.Select(asset => new
        {
            id = asset.Id,
            path = asset.Path,
            kind = "text",
        }));
        string entries = string.Join(",\n", records.Select(asset =>
            $"  [{JsonSerializer.Serialize(asset.Id)}, TextAsset.FromBase64({JsonSerializer.Serialize(Convert.ToBase64String(asset.Bytes))}, "
            + $"{JsonSerializer.Serialize(Path.GetFileNameWithoutExtension(asset.Path))})]"));
        return $"export const preparedAssetManifest = {manifest};\n"
            + $"export const preparedAssets = new Map([\n{entries}\n]);\n\n";
    }

    private static Exception Unsupported(SyntaxNode node, string message)
    {
        var span = node.GetLocation().GetLineSpan();
        return new InvalidOperationException($"{span.Path}:{span.StartLinePosition.Line + 1}:"
            + $"{span.StartLinePosition.Character + 1}: {message}");
    }

    private sealed record PreparedTextAsset(string Id, string Path, byte[] Bytes);
}

sealed class PrefabUtilityLowering : ISystemLowering
{
    private bool used;
    public IEnumerable<string> RuntimeImports => used ? ["PrefabUtility"] : [];
    public string EmitPrelude() => "";
    public string? TryLower(IMethodSymbol symbol, InvocationExpressionSyntax invocation,
        SemanticModel model, Func<ExpressionSyntax, string> emitExpression)
    {
        if (symbol.ContainingType.ToDisplayString() != "UnityEditor.PrefabUtility") return null;
        if (symbol.Name is not ("InstantiatePrefab" or "UnpackPrefabInstance" or "SaveAsPrefabAsset" or "GetCorrespondingObjectFromSource")
            || symbol.Name == "InstantiatePrefab" && invocation.ArgumentList.Arguments.Count is < 1 or > 2
            || symbol.Name == "UnpackPrefabInstance" && invocation.ArgumentList.Arguments.Count != 3
            || symbol.Name == "SaveAsPrefabAsset" && invocation.ArgumentList.Arguments.Count != 2
            || symbol.Name == "GetCorrespondingObjectFromSource" && invocation.ArgumentList.Arguments.Count != 1)
            throw new InvalidOperationException($"{invocation.GetLocation().GetLineSpan()}: PrefabUtility.{symbol.Name} is not supported by Preview");
        used = true;
        return $"PrefabUtility.{symbol.Name}({string.Join(", ", invocation.ArgumentList.Arguments.Select(argument => emitExpression(argument.Expression)))})";
    }
}

sealed class AssetImporterLowering : ISystemLowering
{
    private bool used;
    public IEnumerable<string> RuntimeImports => used ? ["ModelImporter"] : [];
    public string EmitPrelude() => "";
    public string? TryLower(IMethodSymbol symbol, InvocationExpressionSyntax invocation,
        SemanticModel model, Func<ExpressionSyntax, string> emitExpression)
    {
        if (symbol.ContainingType.ToDisplayString() != "UnityEditor.AssetImporter") return null;
        if (symbol.Name == "SaveAndReimport" && invocation.ArgumentList.Arguments.Count == 0)
            return null;
        if (symbol.Name != "GetAtPath" || invocation.ArgumentList.Arguments.Count != 1)
            throw new InvalidOperationException($"{invocation.GetLocation().GetLineSpan()}: AssetImporter.{symbol.Name} is not supported by Preview");
        used = true;
        return $"ModelImporter.GetAtPath({emitExpression(invocation.ArgumentList.Arguments[0].Expression)})";
    }
}
