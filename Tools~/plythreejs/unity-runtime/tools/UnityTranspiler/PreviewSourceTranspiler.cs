using System.Text;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

static class PreviewSourceTranspiler
{
    public static void Run(string sourceDirectory, string outputPath, string runtimeImport,
        IReadOnlyList<string>? moduleSolutions = null)
    {
        string root = Path.GetFullPath(sourceDirectory);
        if (!Directory.Exists(root)) throw new DirectoryNotFoundException(root);
        var paths = Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories)
            .OrderBy(value => value, StringComparer.Ordinal).ToArray();
        if (paths.Length == 0) throw new InvalidOperationException("Preview source directory has no C# files");
        string stubPath = Path.Combine(AppContext.BaseDirectory, "PreviewUnityStubs.cs");
        if (!File.Exists(stubPath)) throw new FileNotFoundException("Preview Unity API stubs are missing", stubPath);
        var gameTrees = paths.Select(path => CSharpSyntaxTree.ParseText(File.ReadAllText(path),
            path: Path.GetRelativePath(root, path).Replace('\\', '/'))).ToArray();
        var referenceTrees = new List<SyntaxTree>();
        foreach (string manifestPath in moduleSolutions ?? [])
        {
            string manifestFile = Path.GetFullPath(manifestPath);
            string solutionRoot = Path.GetDirectoryName(manifestFile)!;
            using var manifest = JsonDocument.Parse(File.ReadAllText(manifestFile));
            if (!manifest.RootElement.TryGetProperty("sources", out var sourceEntries)) continue;
            if (sourceEntries.ValueKind != JsonValueKind.Array)
                throw new InvalidOperationException($"Preview module solution has invalid sources: {manifestFile}");
            foreach (var source in sourceEntries.EnumerateArray())
            {
                string relative = source.GetString() ?? throw new InvalidOperationException("Invalid module source path");
                string sourcePath = Path.GetFullPath(Path.Combine(solutionRoot, relative));
                if (!sourcePath.StartsWith(solutionRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
                    || !sourcePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException($"Module source escapes its solution: {relative}");
                referenceTrees.Add(CSharpSyntaxTree.ParseText(File.ReadAllText(sourcePath),
                    path: $"module/{Path.GetFileName(Path.GetDirectoryName(solutionRoot))}/{relative.Replace('\\', '/')}"));
            }
        }
        var trees = gameTrees.Concat(referenceTrees)
            .Append(CSharpSyntaxTree.ParseText(File.ReadAllText(stubPath), path: "PreviewUnityStubs.cs"))
            .ToArray();
        var platform = ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES"))
            ?.Split(Path.PathSeparator).Where(path => !string.IsNullOrWhiteSpace(path) && !Path.GetFileName(path).Equals("UnityTranspiler.dll", StringComparison.OrdinalIgnoreCase)).ToArray()
            ?? throw new InvalidOperationException("No .NET reference assemblies found");
        var references = platform.Select(file => MetadataReference.CreateFromFile(file));
        var compilation = CSharpCompilation.Create("PreviewVariationCode", trees, references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        // Module source is a declaration reference only. Its Unity package
        // dependencies are already transpiled; compile errors inside that
        // source do not invalidate the game script being compiled here.
        var gameTreeSet = gameTrees.ToHashSet();
        var errors = compilation.GetDiagnostics().Where(item => item.Severity == DiagnosticSeverity.Error
            && (item.Location.SourceTree == null || gameTreeSet.Contains(item.Location.SourceTree))).ToArray();
        if (errors.Length > 0) throw new InvalidOperationException(string.Join(Environment.NewLine, errors.Take(40)));
        var selected = gameTrees.SelectMany(tree => tree.GetRoot().DescendantNodes()
            .OfType<BaseTypeDeclarationSyntax>()).ToArray();
        if (selected.Length == 0) throw new InvalidOperationException("Preview C# files declare no types");
        var emitter = new Emitter(compilation.GetSemanticModel(gameTrees[0]), runtimeImport,
            allowExternalTypes: true);
        string output = emitter.EmitTypes(selected, tree => compilation.GetSemanticModel(tree));
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputPath))!);
        File.WriteAllText(outputPath, output, new UTF8Encoding(false));
        Console.WriteLine($"Transpiled {paths.Length} Preview variation C# files -> {outputPath}");
    }
}
