using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

// Syntax-only inventory. This runs on an Assets directory without compiling or changing the Unity project.
static class UnityAudit
{
    private static readonly HashSet<string> UnityTypes = new(StringComparer.Ordinal)
    {
        "Animator", "Animation", "Avatar", "Rigidbody", "Collider", "CapsuleCollider",
        "CharacterController", "Camera", "Physics", "RaycastHit", "AudioSource",
        "ParticleSystem", "NavMeshAgent", "ScriptableObject", "InputAction", "UnityEvent",
        "Canvas", "RectTransform", "SkinnedMeshRenderer", "Resources", "Terrain",
    };

    public static void Write(string assetsPath, string outputPath)
    {
        string root = Path.GetFullPath(assetsPath);
        if (!Directory.Exists(root)) throw new DirectoryNotFoundException(root);
        string[] allFiles = Directory.GetFiles(root, "*", SearchOption.AllDirectories);
        string[] sources = allFiles.Where(path => path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            .Order(StringComparer.Ordinal).ToArray();
        var extensions = allFiles.Where(path => !path.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
            .GroupBy(path => Path.GetExtension(path).ToLowerInvariant())
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Count());
        var syntax = new Dictionary<string, List<Occurrence>>(StringComparer.Ordinal);
        var api = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        var namespaces = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        var classes = new List<ClassInfo>();
        int editorScripts = 0;

        foreach (string path in sources)
        {
            string relative = Relative(root, path);
            if (relative.Split('/').Contains("Editor", StringComparer.OrdinalIgnoreCase)) editorScripts++;
            var tree = CSharpSyntaxTree.ParseText(File.ReadAllText(path), path: relative);
            var unit = tree.GetCompilationUnitRoot();
            foreach (var directive in unit.DescendantNodes().OfType<UsingDirectiveSyntax>())
            {
                string name = directive.Name?.ToString() ?? "";
                if (name.StartsWith("UnityEngine", StringComparison.Ordinal)) Add(namespaces, name, relative);
            }
            foreach (var node in unit.DescendantNodes())
            {
                string? blocker = Blocker(node);
                if (blocker != null)
                {
                    if (!syntax.TryGetValue(blocker, out var occurrences)) syntax[blocker] = occurrences = [];
                    var line = node.GetLocation().GetLineSpan().StartLinePosition;
                    occurrences.Add(new Occurrence(relative, line.Line + 1, line.Character + 1));
                }
                if (node is IdentifierNameSyntax identifier && UnityTypes.Contains(identifier.Identifier.ValueText))
                    Add(api, identifier.Identifier.ValueText, relative);
                if (node is ClassDeclarationSyntax declaration)
                {
                    string? baseType = declaration.BaseList?.Types.FirstOrDefault()?.Type.ToString();
                    classes.Add(new ClassInfo(relative, declaration.Identifier.ValueText, baseType));
                }
            }
        }

        var scriptsByGuid = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var guidPattern = new Regex(@"(?m)^guid:\s*([0-9a-f]{32})\s*$", RegexOptions.IgnoreCase);
        foreach (string path in sources)
        {
            string meta = path + ".meta";
            if (!File.Exists(meta)) continue;
            var match = guidPattern.Match(File.ReadAllText(meta));
            if (match.Success) scriptsByGuid[match.Groups[1].Value] = Relative(root, path);
        }
        var scriptPattern = new Regex(@"m_Script:\s*\{fileID:\s*11500000,\s*guid:\s*([0-9a-f]{32})",
            RegexOptions.IgnoreCase);
        var prefabs = allFiles.Where(path => path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
            .Select(path =>
            {
                var guids = scriptPattern.Matches(File.ReadAllText(path)).Select(match => match.Groups[1].Value)
                    .Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
                string[] scripts = guids.Where(scriptsByGuid.ContainsKey).Select(guid => scriptsByGuid[guid])
                    .Order(StringComparer.Ordinal).ToArray();
                var scriptSet = scripts.ToHashSet(StringComparer.Ordinal);
                return new PrefabInfo(Relative(root, path), guids.Length, scripts,
                    guids.Where(guid => !scriptsByGuid.ContainsKey(guid)).Order(StringComparer.Ordinal).ToArray(),
                    syntax.Where(entry => entry.Value.Any(item => scriptSet.Contains(item.Path)))
                        .OrderBy(entry => entry.Key, StringComparer.Ordinal)
                        .ToDictionary(entry => entry.Key,
                            entry => entry.Value.Count(item => scriptSet.Contains(item.Path))),
                    api.Where(entry => entry.Value.Overlaps(scriptSet))
                        .OrderBy(entry => entry.Key, StringComparer.Ordinal)
                        .ToDictionary(entry => entry.Key,
                            entry => entry.Value.Count(scriptSet.Contains)));
            })
            .OrderByDescending(prefab => prefab.ScriptTypes)
            .ThenBy(prefab => prefab.Path, StringComparer.Ordinal).ToArray();
        var report = new
        {
            format = "unity-bridge-audit",
            version = 1,
            assetsName = Path.GetFileName(root),
            fileCount = allFiles.Length,
            assetKinds = extensions,
            csharp = new
            {
                files = sources.Length,
                editorFiles = editorScripts,
                classes = classes.Count,
                directMonoBehaviours = classes.Count(item => item.BaseType is "MonoBehaviour" or "UnityEngine.MonoBehaviour"),
                otherBaseTypes = classes.Where(item => item.BaseType != null
                    && item.BaseType is not ("MonoBehaviour" or "UnityEngine.MonoBehaviour"))
                    .OrderBy(item => item.Path, StringComparer.Ordinal).ToArray(),
                knownUnsupportedSyntax = syntax.OrderBy(entry => entry.Key, StringComparer.Ordinal)
                    .ToDictionary(entry => entry.Key, entry => new
                    {
                        occurrences = entry.Value.Count,
                        files = entry.Value.Select(item => item.Path).Distinct().Count(),
                        examples = entry.Value.Take(5).ToArray(),
                    }),
                unityNamespaceImports = Summarize(namespaces),
                unityApiTouchpoints = Summarize(api),
            },
            prefabs = new { count = prefabs.Length, mostScriptTypes = prefabs.Take(15).ToArray() },
        };
        string output = Path.GetFullPath(outputPath);
        Directory.CreateDirectory(Path.GetDirectoryName(output)!);
        File.WriteAllText(output, JsonSerializer.Serialize(report, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        }));
        Console.WriteLine($"Audited {sources.Length} C# files and {prefabs.Length} prefabs -> {output}");
    }

    private static string? Blocker(SyntaxNode node) => node switch
    {
        ConstructorDeclarationSyntax => "constructor",
        PropertyDeclarationSyntax => "property",
        IndexerDeclarationSyntax => "indexer",
        EventDeclarationSyntax or EventFieldDeclarationSyntax => "event",
        OperatorDeclarationSyntax or ConversionOperatorDeclarationSyntax => "operator declaration",
        StructDeclarationSyntax => "struct",
        InterfaceDeclarationSyntax => "interface",
        EnumDeclarationSyntax => "enum",
        LocalFunctionStatementSyntax => "local function",
        ForStatementSyntax or ForEachStatementSyntax or WhileStatementSyntax or DoStatementSyntax => "loop",
        SwitchStatementSyntax or SwitchExpressionSyntax => "switch",
        TryStatementSyntax => "try/catch/finally",
        UsingStatementSyntax => "using statement",
        LockStatementSyntax => "lock statement",
        YieldStatementSyntax => "yield/coroutine",
        SimpleLambdaExpressionSyntax or ParenthesizedLambdaExpressionSyntax or AnonymousMethodExpressionSyntax => "lambda/delegate",
        AwaitExpressionSyntax => "await",
        InterpolatedStringExpressionSyntax => "interpolated string",
        ObjectCreationExpressionSyntax creation when creation.Initializer != null => "object/collection initializer",
        MethodDeclarationSyntax method when method.Modifiers.Any(SyntaxKind.StaticKeyword) => "static method",
        MethodDeclarationSyntax method when method.TypeParameterList != null => "generic method",
        MethodDeclarationSyntax method when method.ExpressionBody != null => "expression-bodied method",
        ParameterSyntax parameter when parameter.Default != null || parameter.Modifiers.Count > 0 => "optional/ref/out/params parameter",
        InvocationExpressionSyntax call when call.Expression is GenericNameSyntax generic
            && generic.Identifier.ValueText != "GetComponent" => "generic call other than GetComponent",
        InvocationExpressionSyntax call when call.Expression is MemberAccessExpressionSyntax member
            && member.Name is GenericNameSyntax generic && generic.Identifier.ValueText != "GetComponent"
            => "generic call other than GetComponent",
        _ => null,
    };

    private static void Add(Dictionary<string, HashSet<string>> map, string key, string file)
    {
        if (!map.TryGetValue(key, out var files)) map[key] = files = new(StringComparer.Ordinal);
        files.Add(file);
    }

    private static Dictionary<string, int> Summarize(Dictionary<string, HashSet<string>> map) =>
        map.OrderBy(entry => entry.Key, StringComparer.Ordinal)
            .ToDictionary(entry => entry.Key, entry => entry.Value.Count);

    private static string Relative(string root, string path) => Path.GetRelativePath(root, path).Replace('\\', '/');
    private sealed record Occurrence(string Path, int Line, int Column);
    private sealed record ClassInfo(string Path, string Name, string? BaseType);
    private sealed record PrefabInfo(string Path, int ScriptTypes, string[] Scripts,
        string[] ExternalScriptGuids, Dictionary<string, int> KnownUnsupportedSyntax,
        Dictionary<string, int> UnityApiTouchpoints);
}
