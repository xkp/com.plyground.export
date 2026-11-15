using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

// Compile Unity's default runtime assembly as a unit so custom module types
// can reference each other and types exported from asmdef assemblies.
static class UnityDefaultProjectTranspiler
{
    public static void Run(string projectPath, string outputPath, string runtimeImport,
        IReadOnlyList<string> roots, bool scan = false)
    {
        if (roots.Count == 0) throw new ArgumentException("At least one --root type is required");
        string project = Path.GetFullPath(projectPath);
        string projectFile = Path.Combine(project, "Assembly-CSharp.csproj");
        if (!File.Exists(projectFile)) throw new FileNotFoundException("Unity default runtime project is required", projectFile);
        var csproj = XDocument.Load(projectFile);
        var symbols = csproj.Descendants().Where(item => item.Name.LocalName == "DefineConstants")
            .SelectMany(item => item.Value.Split(';', StringSplitOptions.RemoveEmptyEntries))
            .Select(item => item.Trim()).Where(item => item.Length > 0)
            .Distinct(StringComparer.Ordinal).ToArray();
        var paths = csproj.Descendants().Where(item => item.Name.LocalName == "Compile")
            .Select(item => item.Attribute("Include")?.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => Path.GetFullPath(Path.Combine(project, value!)))
            .Where(File.Exists).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        if (paths.Length == 0) throw new InvalidOperationException("Default runtime project has no C# sources");
        var trees = paths.Select(path => CSharpSyntaxTree.ParseText(File.ReadAllText(path),
            CSharpParseOptions.Default.WithPreprocessorSymbols(symbols),
            path: Path.GetRelativePath(project, path).Replace('\\', '/'))).ToArray();
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
            allowExternalTypes: true);
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
}
