using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

// Controllers in Unity's default Assembly-CSharp-Editor have no asmdef. Use the
// generated .csproj for symbols and references. Closure mode includes referenced
// types declared in the same default Editor assembly.
static class UnityEditorSourceTranspiler
{
    public static void Run(string projectPath, string sourcePath, string outputPath,
        string runtimeImport, bool scan = false, bool closure = false)
    {
        string project = Path.GetFullPath(projectPath);
        string source = Path.IsPathRooted(sourcePath) ? Path.GetFullPath(sourcePath)
            : Path.GetFullPath(Path.Combine(project, sourcePath));
        string relative = Path.GetRelativePath(project, source);
        if (relative == ".." || relative.StartsWith(".." + Path.DirectorySeparatorChar)
            || Path.IsPathRooted(relative))
            throw new InvalidOperationException("Editor source is outside the Unity project");
        if (!File.Exists(source)) throw new FileNotFoundException("Editor controller source not found", source);
        string projectFile = Path.Combine(project, "Assembly-CSharp-Editor.csproj");
        if (!File.Exists(projectFile))
            throw new FileNotFoundException("Unity-generated Assembly-CSharp-Editor.csproj is required", projectFile);

        var csproj = XDocument.Load(projectFile);
        var symbols = csproj.Descendants().Where(item => item.Name.LocalName == "DefineConstants")
            .SelectMany(item => (item.Value ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries))
            .Select(item => item.Trim()).Where(item => item.Length > 0).Append("UNITY_EDITOR")
            .Distinct(StringComparer.Ordinal).ToArray();
        var references = ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES"))
            ?.Split(Path.PathSeparator).Where(path => !string.IsNullOrWhiteSpace(path) && !Path.GetFileName(path).Equals("UnityTranspiler.dll", StringComparison.OrdinalIgnoreCase)).ToArray()
            ?? throw new InvalidOperationException("No .NET reference assemblies found");
        var hintPaths = csproj.Descendants().Where(item => item.Name.LocalName == "HintPath")
            .Select(item => Path.IsPathRooted(item.Value) ? item.Value
                : Path.GetFullPath(Path.Combine(project, item.Value)))
            .Where(File.Exists);
        var scriptAssemblies = Path.Combine(project, "Library", "ScriptAssemblies");
        // Unity's test assemblies are not references of the Editor assembly.
        // Including them can make runtime types ambiguous when tests contain
        // stand-in classes with the same fully qualified names.
        var compiled = Directory.Exists(scriptAssemblies)
            ? Directory.GetFiles(scriptAssemblies, "*.dll")
                .Where(file => !Path.GetFileNameWithoutExtension(file)
                    .EndsWith(".Tests", StringComparison.OrdinalIgnoreCase)).ToArray() : [];
        if (closure) compiled = compiled.Where(file => Path.GetFileNameWithoutExtension(file)
            != "Assembly-CSharp-Editor").ToArray();
        // Unity's generated csproj includes Mono BCL DLLs. Prefer the running
        // .NET reference set so Roslyn does not bind System.Object twice.
        var metadata = references.Concat(hintPaths).Concat(compiled)
            .GroupBy(Path.GetFileNameWithoutExtension, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .Select(file => MetadataReference.CreateFromFile(file)).ToArray();
        var paths = closure ? csproj.Descendants().Where(item => item.Name.LocalName == "Compile")
            .Select(item => item.Attribute("Include")?.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => Path.GetFullPath(Path.Combine(project, value!)))
            .Where(File.Exists).Append(source).Distinct(StringComparer.OrdinalIgnoreCase).ToArray()
            : [source];
        if (closure) paths = RefreshExporterPackageSources(project, paths);
        var trees = paths.Select(path => CSharpSyntaxTree.ParseText(File.ReadAllText(path),
            CSharpParseOptions.Default.WithPreprocessorSymbols(symbols),
            path: Path.GetRelativePath(project, path).Replace('\\', '/'))).ToArray();
        var tree = trees.First(item => string.Equals(item.FilePath, relative.Replace('\\', '/'),
            StringComparison.OrdinalIgnoreCase));
        // Closure files include unrelated Editor tools. Check the requested
        // controller's imports here; selected dependency members are checked
        // semantically by Emitter.EmitTypes below.
        PreviewExcludedApis.RejectSyntax(tree);
        var compilation = CSharpCompilation.Create("PreviewEditorSource", trees, metadata,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        var errors = compilation.GetDiagnostics().Where(item => item.Severity == DiagnosticSeverity.Error).ToArray();
        var model = compilation.GetSemanticModel(tree);
        var emitter = new Emitter(model, runtimeImport, project,
            editorMode: true, annotateSource: true);
        var root = tree.GetCompilationUnitRoot();
        IReadOnlyList<BaseTypeDeclarationSyntax> selected = closure && errors.Length == 0
            ? ProjectTypeClosure.Resolve(compilation, root.DescendantNodes()
                .OfType<BaseTypeDeclarationSyntax>().Where(type => type.Parent is not TypeDeclarationSyntax)
                .Select(type => model.GetDeclaredSymbol(type)?.ToDisplayString()
                    ?? throw new InvalidOperationException("Controller type could not be resolved"))
                .ToArray(), editorMode: true)
            : root.DescendantNodes().OfType<BaseTypeDeclarationSyntax>().ToArray();
        string destination = Path.GetFullPath(outputPath);
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        if (scan)
        {
            var findings = errors.Select(error => new { phase = "compile", message = error.ToString() })
                .Concat(errors.Length == 0
                    ? emitter.ScanTypes(selected, syntaxTree => compilation.GetSemanticModel(syntaxTree))
                        .Select(message => new { phase = "emit", message })
                    : []).ToArray();
            File.WriteAllText(destination, JsonSerializer.Serialize(new
            {
                format = "unity-editor-source-scan", version = 1,
                source = relative.Replace('\\', '/'), findingCount = findings.Length, findings
            }, new JsonSerializerOptions { WriteIndented = true }), new UTF8Encoding(false));
            Console.WriteLine($"Scanned Editor controller {relative}: {findings.Length} findings -> {destination}");
            return;
        }
        if (errors.Length > 0) throw new InvalidOperationException(string.Join(Environment.NewLine, errors.Take(40)));
        File.WriteAllText(destination, emitter.EmitTypes(selected,
            syntaxTree => compilation.GetSemanticModel(syntaxTree)), new UTF8Encoding(false));
        File.WriteAllText(destination + ".references.json", JsonSerializer.Serialize(new
        {
            references = emitter.RequiredTypes.OrderBy(item => item.Key, StringComparer.Ordinal)
                .Select(item => new { type = item.Key, assembly = item.Value }).ToArray()
        }), new UTF8Encoding(false));
        Console.WriteLine($"Transpiled Editor controller {relative} -> {destination}");
    }

    // Unity can update Packages/packages-lock.json before regenerating its IDE
    // .csproj. In that interval the project file points at an old PackageCache
    // revision and can omit newly added exporter scripts altogether.
    private static string[] RefreshExporterPackageSources(string project, string[] paths)
    {
        string lockFile = Path.Combine(project, "Packages", "packages-lock.json");
        if (!File.Exists(lockFile)) return paths;
        using var packageLock = JsonDocument.Parse(File.ReadAllText(lockFile));
        if (!packageLock.RootElement.TryGetProperty("dependencies", out var dependencies)
            || !dependencies.TryGetProperty("com.plyground.export", out var exporter)
            || !exporter.TryGetProperty("hash", out var hashValue)) return paths;
        string? hash = hashValue.GetString();
        if (string.IsNullOrWhiteSpace(hash) || hash.Length < 12) return paths;
        string packageCache = Path.Combine(project, "Library", "PackageCache");
        string currentPackage = Path.Combine(packageCache, "com.plyground.export@" + hash[..12]);
        string editorDirectory = Path.Combine(currentPackage, "Editor");
        if (!Directory.Exists(editorDirectory)) return paths;
        string exporterPrefix = Path.Combine(packageCache, "com.plyground.export@");
        return paths.Where(path => !path.StartsWith(exporterPrefix, StringComparison.OrdinalIgnoreCase))
            .Concat(Directory.GetFiles(editorDirectory, "*.cs", SearchOption.AllDirectories))
            .Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }
}
