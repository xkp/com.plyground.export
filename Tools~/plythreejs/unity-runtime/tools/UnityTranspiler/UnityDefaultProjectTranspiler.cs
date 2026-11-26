using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

// Compile scene roots from Unity's generated runtime projects. The default
// assembly remains the fast path; selected asmdef project sources can join it.
static class UnityDefaultProjectTranspiler
{
    public static void Run(string projectPath, string outputPath, string runtimeImport,
        IReadOnlyList<string> roots, bool scan = false, bool includeProjectAssemblies = false)
    {
        if (roots.Count == 0) throw new ArgumentException("At least one --root type is required");
        string project = Path.GetFullPath(projectPath);
        string projectFile = Path.Combine(project, "Assembly-CSharp.csproj");
        if (!File.Exists(projectFile)) throw new FileNotFoundException("Unity default runtime project is required", projectFile);
        var csproj = XDocument.Load(projectFile);
        // Timeline's PlayableDirector and tracks run in Unity's native player.
        // Keep the package's optional Timeline branches out of browser code;
        // scene Timeline components remain notified stubs.
        var symbols = csproj.Descendants().Where(item => item.Name.LocalName == "DefineConstants")
            .SelectMany(item => item.Value.Split(';', StringSplitOptions.RemoveEmptyEntries))
            .Select(item => item.Trim()).Where(item => item.Length > 0
                && item != "UNITY_EDITOR" && item != "TimelineIsPresent")
            .Distinct(StringComparer.Ordinal).ToArray();
        var paths = csproj.Descendants().Where(item => item.Name.LocalName == "Compile")
            .Select(item => item.Attribute("Include")?.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => Path.GetFullPath(Path.Combine(project, value!)))
            .Where(File.Exists).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        if (paths.Length == 0) throw new InvalidOperationException("Default runtime project has no C# sources");
        var trees = paths.Select(path => CSharpSyntaxTree.ParseText(PreviewSource(path),
            CSharpParseOptions.Default.WithPreprocessorSymbols(symbols),
            path: Path.GetRelativePath(project, path).Replace('\\', '/'))).ToArray();
        // The generated default assembly includes unrelated package helpers.
        // Validate the selected type closure during emission instead of
        // rejecting Editor-only names elsewhere in the project.
        var platform = ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES"))
            ?.Split(Path.PathSeparator).Where(path => !string.IsNullOrWhiteSpace(path) && !Path.GetFileName(path).Equals("UnityTranspiler.dll", StringComparison.OrdinalIgnoreCase)).ToArray()
            ?? throw new InvalidOperationException("No .NET reference assemblies found");
        var hintPaths = csproj.Descendants().Where(item => item.Name.LocalName == "HintPath")
            .Select(item => Path.IsPathRooted(item.Value) ? item.Value
                : Path.GetFullPath(Path.Combine(project, item.Value))).Where(File.Exists);
        string scriptAssemblies = Path.Combine(project, "Library", "ScriptAssemblies");
        var compiled = Directory.Exists(scriptAssemblies)
            ? Directory.GetFiles(scriptAssemblies, "*.dll")
                .Where(path => Path.GetFileNameWithoutExtension(path) != "Assembly-CSharp") : [];
        var metadata = platform.Concat(hintPaths).Concat(compiled)
            .GroupBy(Path.GetFileNameWithoutExtension, StringComparer.OrdinalIgnoreCase)
            .Select(group => MetadataReference.CreateFromFile(group.First())).ToArray();
        var compilation = CSharpCompilation.Create("PreviewRuntimeDefault", trees, metadata,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        if (includeProjectAssemblies && roots.Any(root =>
            compilation.Assembly.GetTypeByMetadataName(root) == null))
        {
            var selectedAssemblies = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var sourcePaths = new HashSet<string>(paths, StringComparer.OrdinalIgnoreCase);
            var additionalTrees = new List<SyntaxTree>();
            foreach (string file in Directory.GetFiles(project, "*.csproj")
                .Where(file => !Path.GetFileNameWithoutExtension(file).Equals("Assembly-CSharp", StringComparison.OrdinalIgnoreCase)
                    && !Path.GetFileNameWithoutExtension(file).Contains("Editor", StringComparison.OrdinalIgnoreCase)
                    && !Path.GetFileNameWithoutExtension(file).EndsWith(".Player", StringComparison.OrdinalIgnoreCase)))
            {
                var document = XDocument.Load(file);
                var defines = document.Descendants().Where(item => item.Name.LocalName == "DefineConstants")
                    .SelectMany(item => item.Value.Split(';', StringSplitOptions.RemoveEmptyEntries))
                    .Select(item => item.Trim()).Where(item => item.Length > 0
                        && item != "UNITY_EDITOR" && item != "TimelineIsPresent")
                    .Distinct(StringComparer.Ordinal).ToArray();
                var projectTrees = document.Descendants().Where(item => item.Name.LocalName == "Compile")
                    .Select(item => item.Attribute("Include")?.Value)
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .Select(value => Path.GetFullPath(Path.Combine(project, value!)))
                    .Where(File.Exists).Distinct(StringComparer.OrdinalIgnoreCase)
                    .Select(path => CSharpSyntaxTree.ParseText(PreviewSource(path),
                        CSharpParseOptions.Default.WithPreprocessorSymbols(defines),
                        path: Path.GetRelativePath(project, path).Replace('\\', '/'))).ToArray();
                if (!projectTrees.Any(tree => tree.GetRoot().DescendantNodes()
                    .OfType<BaseTypeDeclarationSyntax>().Any(declaration =>
                        roots.Contains(DeclaredTypeName(declaration), StringComparer.Ordinal)))) continue;
                selectedAssemblies.Add(document.Descendants().FirstOrDefault(item =>
                    item.Name.LocalName == "AssemblyName")?.Value ?? Path.GetFileNameWithoutExtension(file));
                foreach (var tree in projectTrees)
                    if (sourcePaths.Add(Path.GetFullPath(Path.Combine(project, tree.FilePath))))
                        additionalTrees.Add(tree);
            }
            trees = trees.Concat(additionalTrees).ToArray();
            metadata = platform.Concat(hintPaths).Concat(compiled)
                .Where(path => !selectedAssemblies.Contains(Path.GetFileNameWithoutExtension(path)))
                .GroupBy(Path.GetFileNameWithoutExtension, StringComparer.OrdinalIgnoreCase)
                .Select(group => MetadataReference.CreateFromFile(group.First())).ToArray();
            compilation = CSharpCompilation.Create("PreviewRuntimeDefault", trees, metadata,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        }
        var errors = compilation.GetDiagnostics().Where(item => item.Severity == DiagnosticSeverity.Error).ToArray();
        string destination = Path.GetFullPath(outputPath);
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        if (errors.Length > 0)
        {
            if (!scan) throw new InvalidOperationException(string.Join(Environment.NewLine, errors.Take(40)));
            File.WriteAllText(destination, JsonSerializer.Serialize(new
            {
                format = "unity-runtime-default-scan", version = 1, roots,
                findingCount = errors.Length,
                findings = errors.Select(error => new { phase = "compile", message = error.ToString() })
            }, new JsonSerializerOptions { WriteIndented = true }), new UTF8Encoding(false));
            Console.WriteLine($"Scanned default runtime assembly: {errors.Length} compile findings -> {destination}");
            return;
        }
        var selected = ProjectTypeClosure.Resolve(compilation, roots);
        var emitter = new Emitter(compilation.GetSemanticModel(trees[0]), runtimeImport, project,
            allowExternalTypes: true, annotateSource: true);
        if (scan)
        {
            var findings = emitter.ScanTypes(selected, tree => compilation.GetSemanticModel(tree));
            File.WriteAllText(destination, JsonSerializer.Serialize(new
            {
                format = "unity-runtime-default-scan", version = 1, roots,
                selectedTypes = selected.Count, findingCount = findings.Count, findings
            }, new JsonSerializerOptions { WriteIndented = true }), new UTF8Encoding(false));
            Console.WriteLine($"Scanned default runtime assembly: {selected.Count} types, {findings.Count} findings -> {destination}");
            return;
        }
        File.WriteAllText(destination, emitter.EmitTypes(selected,
            tree => compilation.GetSemanticModel(tree)), new UTF8Encoding(false));
        Console.WriteLine($"Transpiled default runtime assembly: {selected.Count} types -> {destination}");
    }

    private static string DeclaredTypeName(BaseTypeDeclarationSyntax declaration)
    {
        var parts = new Stack<string>();
        for (SyntaxNode? node = declaration; node != null; node = node.Parent)
        {
            if (node is BaseTypeDeclarationSyntax type) parts.Push(type.Identifier.ValueText);
            else if (node is BaseNamespaceDeclarationSyntax space) parts.Push(space.Name.ToString());
        }
        return string.Join(".", parts);
    }

    private static string PreviewSource(string path)
    {
        string source = File.ReadAllText(path);
        // Adventure Creator enables UnityEngine.Video in individual files,
        // independently of project symbols. Its native video path cannot run
        // in the browser, so retain the source and omit that optional branch
        // only in the preview compilation.
        if (path.Replace('\\', '/').Contains("/Assets/AdventureCreator/", StringComparison.OrdinalIgnoreCase))
            source = source.Replace("#define ALLOW_VIDEO", "// Browser Preview: native video unavailable");
        if (path.EndsWith("/ActionMovie.cs", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith("\\ActionMovie.cs", StringComparison.OrdinalIgnoreCase))
        {
            // This optional callback sits outside AC's ALLOW_VIDEO guard even
            // though its parameter and state are declared only inside it.
            source = source.Replace("private void Prepared (VideoPlayer videoPlayer)",
                "#if ALLOW_VIDEO\n\t\tprivate void Prepared (VideoPlayer videoPlayer)");
            source = source.Replace("public override void Skip ()",
                "#endif\n\t\tpublic override void Skip ()");
        }
        return source;
    }
}
