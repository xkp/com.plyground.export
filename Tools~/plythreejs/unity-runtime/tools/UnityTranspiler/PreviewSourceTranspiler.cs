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
        string uiStubPath = Path.Combine(AppContext.BaseDirectory, "PreviewUIStubs.cs");
        string webRequestStubPath = Path.Combine(AppContext.BaseDirectory, "PreviewWebRequestStubs.cs");
        string uiEnumStubPath = Path.Combine(AppContext.BaseDirectory, "PreviewUiEnums.cs");
        string uiNestedEnumStubPath = Path.Combine(AppContext.BaseDirectory, "PreviewUiNestedEnums.cs");
        if (!File.Exists(uiStubPath)) throw new FileNotFoundException("Preview UI API stubs are missing", uiStubPath);
        if (!File.Exists(uiEnumStubPath)) throw new FileNotFoundException("Preview UI enum stubs are missing", uiEnumStubPath);
        if (!File.Exists(uiNestedEnumStubPath)) throw new FileNotFoundException("Preview nested UI enum stubs are missing", uiNestedEnumStubPath);
        string paletteStubPath = Path.Combine(AppContext.BaseDirectory, "PreviewColorPaletteStubs.cs");
        if (!File.Exists(paletteStubPath)) throw new FileNotFoundException("Preview Color stubs are missing", paletteStubPath);
        string platformEnumStubPath = Path.Combine(AppContext.BaseDirectory, "PreviewPlatformEnums.cs");
        string keyCodeStubPath = Path.Combine(AppContext.BaseDirectory, "PreviewKeyCode.cs");
        string textureEnumStubPath = Path.Combine(AppContext.BaseDirectory, "PreviewTextureEnums.cs");
        string textureApiStubPath = Path.Combine(AppContext.BaseDirectory, "PreviewTextureApiStubs.cs");
        string imageConversionStubPath = Path.Combine(AppContext.BaseDirectory, "PreviewImageConversionStubs.cs");
        string particleApiStubPath = Path.Combine(AppContext.BaseDirectory, "PreviewParticleApiStubs.cs");
        string remainingEnumStubPath = Path.Combine(AppContext.BaseDirectory, "PreviewRemainingEnums.cs");
        string renderingEnumStubPath = Path.Combine(AppContext.BaseDirectory, "PreviewRenderingEnums.cs");
        string otherEnumStubPath = Path.Combine(AppContext.BaseDirectory, "PreviewOtherEnums.cs");
        string simpleStructStubPath = Path.Combine(AppContext.BaseDirectory, "PreviewSimpleStructs.cs");
        string meshLayoutStubPath = Path.Combine(AppContext.BaseDirectory, "PreviewMeshLayout.cs");
        string sortingMetadataStubPath = Path.Combine(AppContext.BaseDirectory, "PreviewSortingMetadata.cs");
        string editorMetadataStubPath = Path.Combine(AppContext.BaseDirectory, "PreviewEditorMetadataAttributes.cs");
        string linkerMetadataStubPath = Path.Combine(AppContext.BaseDirectory, "PreviewLinkerMetadataAttributes.cs");
        if (!File.Exists(platformEnumStubPath)) throw new FileNotFoundException("Preview platform enum stubs are missing", platformEnumStubPath);
        if (!File.Exists(keyCodeStubPath)) throw new FileNotFoundException("Preview KeyCode stubs are missing", keyCodeStubPath);
        if (!File.Exists(textureEnumStubPath)) throw new FileNotFoundException("Preview texture enum stubs are missing", textureEnumStubPath);
        if (!File.Exists(remainingEnumStubPath)) throw new FileNotFoundException("Preview remaining enum stubs are missing", remainingEnumStubPath);
        var gameTrees = paths.Select(path => CSharpSyntaxTree.ParseText(File.ReadAllText(path),
            path: Path.GetRelativePath(root, path).Replace('\\', '/'))).ToArray();
        foreach (var tree in gameTrees) PreviewExcludedApis.RejectSyntax(tree);
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
            .Append(CSharpSyntaxTree.ParseText(File.ReadAllText(uiStubPath), path: "PreviewUIStubs.cs"))
            .Append(CSharpSyntaxTree.ParseText(File.ReadAllText(webRequestStubPath), path: "PreviewWebRequestStubs.cs"))
            .Append(CSharpSyntaxTree.ParseText(File.ReadAllText(uiEnumStubPath), path: "PreviewUiEnums.cs"))
            .Append(CSharpSyntaxTree.ParseText(File.ReadAllText(uiNestedEnumStubPath), path: "PreviewUiNestedEnums.cs"))
            .Append(CSharpSyntaxTree.ParseText(File.ReadAllText(paletteStubPath), path: "PreviewColorPaletteStubs.cs"))
            .Append(CSharpSyntaxTree.ParseText(File.ReadAllText(platformEnumStubPath), path: "PreviewPlatformEnums.cs"))
            .Append(CSharpSyntaxTree.ParseText(File.ReadAllText(keyCodeStubPath), path: "PreviewKeyCode.cs"))
            .Append(CSharpSyntaxTree.ParseText(File.ReadAllText(textureEnumStubPath), path: "PreviewTextureEnums.cs"))
            .Append(CSharpSyntaxTree.ParseText(File.ReadAllText(textureApiStubPath), path: "PreviewTextureApiStubs.cs"))
            .Append(CSharpSyntaxTree.ParseText(File.ReadAllText(imageConversionStubPath), path: "PreviewImageConversionStubs.cs"))
            .Append(CSharpSyntaxTree.ParseText(File.ReadAllText(particleApiStubPath), path: "PreviewParticleApiStubs.cs"))
            .Append(CSharpSyntaxTree.ParseText(File.ReadAllText(remainingEnumStubPath), path: "PreviewRemainingEnums.cs"))
            .Append(CSharpSyntaxTree.ParseText(File.ReadAllText(renderingEnumStubPath), path: "PreviewRenderingEnums.cs"))
            .Append(CSharpSyntaxTree.ParseText(File.ReadAllText(otherEnumStubPath), path: "PreviewOtherEnums.cs"))
            .Append(CSharpSyntaxTree.ParseText(File.ReadAllText(simpleStructStubPath), path: "PreviewSimpleStructs.cs"))
            .Append(CSharpSyntaxTree.ParseText(File.ReadAllText(meshLayoutStubPath), path: "PreviewMeshLayout.cs"))
            .Append(CSharpSyntaxTree.ParseText(File.ReadAllText(sortingMetadataStubPath), path: "PreviewSortingMetadata.cs"))
            .Append(CSharpSyntaxTree.ParseText(File.ReadAllText(editorMetadataStubPath), path: "PreviewEditorMetadataAttributes.cs"))
            .Append(CSharpSyntaxTree.ParseText(File.ReadAllText(linkerMetadataStubPath), path: "PreviewLinkerMetadataAttributes.cs"))
            .ToArray();
        var platform = ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES"))
            ?.Split(Path.PathSeparator).Where(path => !string.IsNullOrWhiteSpace(path) && !Path.GetFileName(path).Equals("UnityTranspiler.dll", StringComparison.OrdinalIgnoreCase)).ToArray()
            ?? throw new InvalidOperationException("No .NET reference assemblies found");
        var references = platform.Select(file => MetadataReference.CreateFromFile(file));
        var compilation = CSharpCompilation.Create("PreviewVariationCode", trees, references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        foreach (var tree in gameTrees)
            PreviewExcludedApis.RejectNativeToolkitUssNames(tree,
                compilation.GetSemanticModel(tree));
        foreach (var tree in gameTrees)
            foreach (var declaration in tree.GetRoot().DescendantNodes()
                .OfType<BaseTypeDeclarationSyntax>())
                PreviewExcludedApis.RejectAdvancedLight(declaration,
                    compilation.GetSemanticModel(tree));
        foreach (var tree in gameTrees)
            foreach (var declaration in tree.GetRoot().DescendantNodes()
                .OfType<BaseTypeDeclarationSyntax>())
                PreviewExcludedApis.RejectAdvancedMesh(declaration,
                    compilation.GetSemanticModel(tree));
        foreach (var tree in gameTrees)
            foreach (var declaration in tree.GetRoot().DescendantNodes()
                .OfType<BaseTypeDeclarationSyntax>())
                PreviewExcludedApis.RejectAdvancedLineRenderer(declaration,
                    compilation.GetSemanticModel(tree));
        foreach (var tree in gameTrees)
            foreach (var declaration in tree.GetRoot().DescendantNodes()
                .OfType<BaseTypeDeclarationSyntax>())
                PreviewExcludedApis.RejectAdvancedCamera(declaration,
                    compilation.GetSemanticModel(tree));
        foreach (var tree in gameTrees)
            foreach (var declaration in tree.GetRoot().DescendantNodes()
                .OfType<BaseTypeDeclarationSyntax>())
                PreviewExcludedApis.RejectAdvancedMaterial(declaration,
                    compilation.GetSemanticModel(tree));
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
            allowExternalTypes: true, annotateSource: true);
        string output = emitter.EmitTypes(selected, tree => compilation.GetSemanticModel(tree));
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputPath))!);
        File.WriteAllText(outputPath, output, new UTF8Encoding(false));
        Console.WriteLine($"Transpiled {paths.Length} Preview variation C# files -> {outputPath}");
    }
}
