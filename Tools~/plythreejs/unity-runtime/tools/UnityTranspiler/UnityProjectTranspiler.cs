using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

static class UnityProjectTranspiler
{
    public static void Run(string projectPath, string asmdefPath, string outputPath,
        string runtimeImport, IReadOnlyList<string> rootTypes, bool scan = false, bool editorMode = false)
    {
        string project = Path.GetFullPath(projectPath);
        string asmdef = Path.IsPathRooted(asmdefPath) ? Path.GetFullPath(asmdefPath)
            : Path.GetFullPath(Path.Combine(project, asmdefPath));
        if (!File.Exists(asmdef)) throw new FileNotFoundException("Assembly definition not found", asmdef);
        string assemblyRoot = Path.GetDirectoryName(asmdef)!;
        if (!Inside(project, assemblyRoot))
            throw new InvalidOperationException("Assembly definition is outside the Unity project");
        using var definition = JsonDocument.Parse(File.ReadAllText(asmdef));
        string assemblyName = definition.RootElement.GetProperty("name").GetString()
            ?? throw new InvalidOperationException("Assembly definition has no name");
        string[] dependencies = definition.RootElement.TryGetProperty("references", out var declaredReferences)
            ? declaredReferences.EnumerateArray().Select(reference => reference.GetString()!)
                .Where(reference => !string.IsNullOrWhiteSpace(reference)).ToArray()
            : Array.Empty<string>();
        bool editorAssembly = definition.RootElement.TryGetProperty("includePlatforms", out var platforms)
            && platforms.EnumerateArray().Any(platform => platform.GetString() == "Editor");
        string[] nestedRoots = Directory.GetFiles(assemblyRoot, "*.asmdef", SearchOption.AllDirectories)
            .Where(path => !Path.GetFullPath(path).Equals(asmdef, StringComparison.OrdinalIgnoreCase))
            .Select(path => Path.GetDirectoryName(path)!).ToArray();
        if (editorMode && rootTypes.Count == 0)
            throw new ArgumentException("Editor project mode requires at least one --root controller type");
        string[] sources = Directory.GetFiles(assemblyRoot, "*.cs", SearchOption.AllDirectories)
            .Where(path => !nestedRoots.Any(root => Inside(root, path)))
            .Where(path => editorMode || !Path.GetRelativePath(assemblyRoot, path).Split(Path.DirectorySeparatorChar)
                .Contains("Editor", StringComparer.OrdinalIgnoreCase))
            .Order(StringComparer.Ordinal).ToArray();
        if (sources.Length == 0) throw new InvalidOperationException("Assembly contains no runtime C# files");

        string versionPath = Path.Combine(project, "ProjectSettings", "ProjectVersion.txt");
        var versionMatch = Regex.Match(File.ReadAllText(versionPath), @"(?m)^m_EditorVersion:\s*(\S+)");
        if (!versionMatch.Success) throw new InvalidOperationException("Unity Editor version was not found");
        string managed = Environment.GetEnvironmentVariable("UNITY_EDITOR_MANAGED")
            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                "Unity", "Hub", "Editor", versionMatch.Groups[1].Value,
                "Editor", "Data", "Managed", "UnityEngine");
        if (!Directory.Exists(managed)) throw new DirectoryNotFoundException($"UnityEngine assemblies: {managed}");
        var platform = ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES"))
            ?.Split(Path.PathSeparator).Where(path => !string.IsNullOrWhiteSpace(path) && !Path.GetFileName(path).Equals("UnityTranspiler.dll", StringComparison.OrdinalIgnoreCase)).ToArray()
            ?? throw new InvalidOperationException("No .NET reference assemblies found");
        var referencePaths = platform.Concat(Directory.GetFiles(managed, "UnityEngine*.dll"))
            .ToList();
        if (editorAssembly || editorMode)
        {
            string editorManaged = Directory.GetParent(managed)!.FullName;
            referencePaths.AddRange(Directory.GetFiles(editorManaged, "UnityEditor*.dll"));
            referencePaths.AddRange(Directory.GetFiles(managed, "UnityEditor*.dll"));
        }
        foreach (string dependency in dependencies)
        {
            if (dependency.StartsWith("GUID:", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"GUID asmdef reference needs resolution: {dependency}");
            string dll = Path.Combine(project, "Library", "ScriptAssemblies", dependency + ".dll");
            if (!File.Exists(dll))
                throw new FileNotFoundException($"Assembly reference {dependency} is unavailable; open the project in Unity first", dll);
            referencePaths.Add(dll);
        }
        // Unity auto-references these package/runtime assemblies even when an asmdef only
        // names its explicit asmdef dependencies. Resolve the actual project copies.
        string scriptAssemblies = Path.Combine(project, "Library", "ScriptAssemblies");
        string ui = Path.Combine(scriptAssemblies, "UnityEngine.UI.dll");
        if (File.Exists(ui)) referencePaths.Add(ui);
        string packages = Path.Combine(project, "Library", "PackageCache");
        if (Directory.Exists(packages))
        {
            string? json = Directory.GetFiles(packages, "Newtonsoft.Json.dll", SearchOption.AllDirectories)
                .Where(path => path.Contains("com.unity.nuget.newtonsoft-json@", StringComparison.OrdinalIgnoreCase)
                    && !path.Contains($"{Path.DirectorySeparatorChar}AOT{Path.DirectorySeparatorChar}",
                        StringComparison.OrdinalIgnoreCase))
                .Order(StringComparer.Ordinal).FirstOrDefault();
            if (json != null) referencePaths.Add(json);
        }
        var references = referencePaths.Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(path => MetadataReference.CreateFromFile(path));
        var parseOptions = CSharpParseOptions.Default.WithPreprocessorSymbols(
            editorAssembly || editorMode ? new[] { "UNITY_EDITOR" } : Array.Empty<string>());
        var trees = sources.Select(path => CSharpSyntaxTree.ParseText(File.ReadAllText(path),
            parseOptions, path: Path.GetRelativePath(project, path).Replace('\\', '/'))).ToArray();
        // Editor assemblies can include unrelated host-only helpers and unused
        // imports. Emitter checks the selected Preview types semantically.
        if (!editorMode)
            foreach (var tree in trees) PreviewExcludedApis.RejectSyntax(tree);
        var compilation = CSharpCompilation.Create(assemblyName, trees, references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        var errors = compilation.GetDiagnostics().Where(item => item.Severity == DiagnosticSeverity.Error).ToArray();
        if (errors.Length > 0)
            throw new InvalidOperationException($"{assemblyName}: {errors.Length} C# compilation errors\n"
                + string.Join(Environment.NewLine, errors.Take(40))
                + (errors.Length > 40 ? $"\n... {errors.Length - 40} further errors" : ""));

        var emitter = new Emitter(compilation.GetSemanticModel(trees[0]), runtimeImport, project,
            editorMode, annotateSource: true);
        var roots = trees.Select(tree => tree.GetCompilationUnitRoot()).ToArray();
        if (scan)
        {
            if (rootTypes.Count == 0)
                throw new ArgumentException("Project scan requires at least one --root type");
            var selected = ProjectTypeClosure.Resolve(compilation, rootTypes, editorMode);
            var findings = emitter.ScanTypes(selected, tree => compilation.GetSemanticModel(tree))
                .Select(message =>
                {
                    var match = Regex.Match(message, @"^(.+?):(\d+):(\d+): (.+)$");
                    return new
                    {
                        path = match.Success ? match.Groups[1].Value : "",
                        line = match.Success ? int.Parse(match.Groups[2].Value) : 0,
                        column = match.Success ? int.Parse(match.Groups[3].Value) : 0,
                        message = match.Success ? match.Groups[4].Value : message,
                    };
                }).OrderBy(item => item.path, StringComparer.Ordinal)
                .ThenBy(item => item.line).ThenBy(item => item.column).ToArray();
            var report = new
            {
                format = "unity-transpiler-scan",
                version = 1,
                assembly = assemblyName,
                roots = rootTypes,
                selectedTypes = selected.Select(type => compilation.GetSemanticModel(type.SyntaxTree)
                    .GetDeclaredSymbol(type)?.ToDisplayString()).Distinct().Count(),
                findingCount = findings.Length,
                categories = findings.GroupBy(item => item.message, StringComparer.Ordinal)
                    .OrderByDescending(group => group.Count()).ThenBy(group => group.Key, StringComparer.Ordinal)
                    .Select(group => new { message = group.Key, count = group.Count(),
                        examples = group.Take(5).ToArray() }).ToArray(),
                findings,
            };
            string reportPath = Path.GetFullPath(outputPath);
            Directory.CreateDirectory(Path.GetDirectoryName(reportPath)!);
            File.WriteAllText(reportPath, JsonSerializer.Serialize(report,
                new JsonSerializerOptions { WriteIndented = true }), new UTF8Encoding(false));
            Console.WriteLine($"Scanned assembly {assemblyName}: {report.selectedTypes} selected types, "
                + $"{report.findingCount} transpiler findings -> {reportPath}");
            return;
        }
        string output = rootTypes.Count == 0
            ? emitter.Emit(roots, tree => compilation.GetSemanticModel(tree))
            : emitter.EmitTypes(ProjectTypeClosure.Resolve(compilation, rootTypes, editorMode),
                tree => compilation.GetSemanticModel(tree));
        string destination = Path.GetFullPath(outputPath);
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        File.WriteAllText(destination, output, new UTF8Encoding(false));
        if (editorMode)
            File.WriteAllText(destination + ".references.json", JsonSerializer.Serialize(new
            {
                references = emitter.RequiredTypes.OrderBy(item => item.Key, StringComparer.Ordinal)
                    .Select(item => new { type = item.Key, assembly = item.Value }).ToArray()
            }), new UTF8Encoding(false));
        Console.WriteLine($"Transpiled assembly {assemblyName}: {sources.Length} C# files"
            + (rootTypes.Count > 0 ? $", {rootTypes.Count} roots" : "") + $" -> {destination}");
    }

    private static bool Inside(string root, string path)
    {
        string relative = Path.GetRelativePath(root, path);
        return relative != ".." && !relative.StartsWith(".." + Path.DirectorySeparatorChar)
            && !Path.IsPathRooted(relative);
    }
}
