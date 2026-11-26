using System.Globalization;
using System.Numerics;
using System.Text;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

if (args.Length >= 4 && args[0] == "--preview-sources")
{
    try
    {
        var solutions = new List<string>();
        for (int index = 4; index < args.Length; index += 2)
        {
            if (index + 1 >= args.Length || args[index] != "--module-solution")
                throw new ArgumentException("Preview module references must be supplied as --module-solution <manifest.json>");
            solutions.Add(args[index + 1]);
        }
        PreviewSourceTranspiler.Run(args[1], args[2], args[3], solutions);
        return 0;
    }
    catch (Exception error)
    {
        Console.Error.WriteLine(error);
        return 1;
    }
}

if (args.Length == 3 && args[0] == "--audit")
{
    try
    {
        UnityAudit.Write(args[1], args[2]);
        return 0;
    }
    catch (Exception error)
    {
        Console.Error.WriteLine(error.Message);
        return 1;
    }
}

if (args.Length >= 5 && args[0] is "--project" or "--scan-project" or "--editor-project" or "--scan-editor-project")
{
    try
    {
        var roots = new List<string>();
        for (int index = 5; index < args.Length; index += 2)
        {
            if (index + 1 >= args.Length || args[index] != "--root")
                throw new ArgumentException("Project roots must be supplied as --root <full C# type name>");
            roots.Add(args[index + 1]);
        }
        UnityProjectTranspiler.Run(args[1], args[2], args[3], args[4], roots,
            scan: args[0] is "--scan-project" or "--scan-editor-project",
            editorMode: args[0] is "--editor-project" or "--scan-editor-project");
        return 0;
    }
    catch (Exception error)
    {
        Console.Error.WriteLine(error);
        return 1;
    }
}

if (args.Length >= 4 && args[0] is "--runtime-default-project" or "--scan-runtime-default-project"
    or "--runtime-project-assemblies" or "--scan-runtime-project-assemblies")
{
    try
    {
        var roots = new List<string>();
        for (int index = 4; index < args.Length; index += 2)
        {
            if (index + 1 >= args.Length || args[index] != "--root")
                throw new ArgumentException("Default project roots must be supplied as --root <full C# type name>");
            roots.Add(args[index + 1]);
        }
        UnityDefaultProjectTranspiler.Run(args[1], args[2], args[3], roots,
            scan: args[0] is "--scan-runtime-default-project" or "--scan-runtime-project-assemblies",
            includeProjectAssemblies: args[0] is "--runtime-project-assemblies" or "--scan-runtime-project-assemblies");
        return 0;
    }
    catch (Exception error)
    {
        Console.Error.WriteLine(error);
        return 1;
    }
}

if (args.Length == 5 && args[0] is "--editor-source" or "--scan-editor-source"
    or "--editor-closure" or "--scan-editor-closure")
{
    try
    {
        UnityEditorSourceTranspiler.Run(args[1], args[2], args[3], args[4],
            scan: args[0] is "--scan-editor-source" or "--scan-editor-closure",
            closure: args[0] is "--editor-closure" or "--scan-editor-closure");
        return 0;
    }
    catch (Exception error)
    {
        Console.Error.WriteLine(error.Message);
        return 1;
    }
}

if (args.Length != 4)
{
    Console.Error.WriteLine("Usage: UnityTranspiler <source.cs> <output.js> <UnityEngine managed directory> <runtime import specifier>\n"
        + "       UnityTranspiler --preview-sources <source directory> <output.js> <runtime import specifier>\n"
        + "       UnityTranspiler --audit <Unity Assets directory> <report.json>\n"
        + "       UnityTranspiler --project <Unity project directory> <asmdef path> <output.js> <runtime import specifier> [--root <full C# type name>]...\n"
        + "       UnityTranspiler --scan-project <Unity project directory> <asmdef path> <report.json> <runtime import specifier> --root <full C# type name>...\n"
        + "       UnityTranspiler --editor-project <Unity project directory> <asmdef path> <output.js> <runtime import specifier> --root <full C# controller type>...\n"
        + "       UnityTranspiler --scan-editor-project <Unity project directory> <asmdef path> <report.json> <runtime import specifier> --root <full C# controller type>...\n"
        + "       UnityTranspiler --editor-source <Unity project directory> <controller.cs> <output.js> <runtime import specifier>\n"
        + "       UnityTranspiler --scan-editor-source <Unity project directory> <controller.cs> <report.json> <runtime import specifier>\n"
        + "       UnityTranspiler --editor-closure <Unity project directory> <controller.cs> <output.js> <runtime import specifier>\n"
        + "       UnityTranspiler --scan-editor-closure <Unity project directory> <controller.cs> <report.json> <runtime import specifier>\n"
        + "       UnityTranspiler --runtime-default-project <Unity project directory> <output.js> <runtime import specifier> --root <full C# type name>...\n"
        + "       UnityTranspiler --scan-runtime-default-project <Unity project directory> <report.json> <runtime import specifier> --root <full C# type name>...\n"
        + "       UnityTranspiler --runtime-project-assemblies <Unity project directory> <output.js> <runtime import specifier> --root <full C# type name>...\n"
        + "       UnityTranspiler --scan-runtime-project-assemblies <Unity project directory> <report.json> <runtime import specifier> --root <full C# type name>...");
    return 2;
}

try
{
    string sourcePath = Path.GetFullPath(args[0]);
    string outputPath = Path.GetFullPath(args[1]);
    string managedPath = Path.GetFullPath(args[2]);
    var tree = CSharpSyntaxTree.ParseText(File.ReadAllText(sourcePath), path: sourcePath);
    PreviewExcludedApis.RejectSyntax(tree);
    var platform = ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES"))
        ?.Split(Path.PathSeparator).Where(path => !string.IsNullOrWhiteSpace(path) && !Path.GetFileName(path).Equals("UnityTranspiler.dll", StringComparison.OrdinalIgnoreCase)).ToArray()
        ?? throw new InvalidOperationException("No .NET reference assemblies found");
    var references = platform.Concat(Directory.EnumerateFiles(managedPath, "UnityEngine*.dll"))
        .Select(path => MetadataReference.CreateFromFile(path));
    var compilation = CSharpCompilation.Create("UnityBridgeTranspile", new[] { tree }, references,
        new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    var errors = compilation.GetDiagnostics().Where(item => item.Severity == DiagnosticSeverity.Error).ToList();
    if (errors.Count > 0) throw new InvalidOperationException(string.Join(Environment.NewLine, errors));
    var emitter = new Emitter(compilation.GetSemanticModel(tree), args[3]);
    string output = emitter.Emit(tree.GetCompilationUnitRoot());
    Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
    File.WriteAllText(outputPath, output, new UTF8Encoding(false));
    Console.WriteLine($"Transpiled {sourcePath} -> {outputPath}");
    return 0;
}
catch (Exception error)
{
    Console.Error.WriteLine(error.Message);
    return 1;
}

sealed class Emitter(SemanticModel model, string runtimeImport, string? unityProjectRoot = null,
    bool editorMode = false, bool allowExternalTypes = false, bool annotateSource = false)
{
    private readonly HashSet<string> imports = new(StringComparer.Ordinal);
    private StringBuilder body = new();
    private readonly SystemLoweringRegistry lowerings = new(unityProjectRoot);
    private HashSet<string> emittedTypes = new(StringComparer.Ordinal);
    private Dictionary<string, string> emittedNames = new(StringComparer.Ordinal);
    private HashSet<string> structTypes = new(StringComparer.Ordinal);
    private Func<SyntaxTree, SemanticModel> models = _ => model;
    private int foreachCounter;
    private int refCounter;
    private bool diagnosticMode;
    private readonly List<string> diagnostics = [];
    private readonly Dictionary<string, string> requiredTypes = new(StringComparer.Ordinal);
    public IReadOnlyDictionary<string, string> RequiredTypes => requiredTypes;

    private void SourceMarker(SyntaxNode node, int depth)
    {
        if (!annotateSource) return;
        var location = node.GetLocation().GetLineSpan();
        string path = location.Path.Replace('\\', '/');
        if (!path.StartsWith("Assets/", StringComparison.Ordinal)) path = "Assets/" + path;
        body.AppendLine($"{new string(' ', depth * 2)}// @unity-source {path}:"
            + $"{location.StartLinePosition.Line + 1}:{location.StartLinePosition.Character + 1}");
    }

    public IReadOnlyList<string> ScanTypes(IEnumerable<BaseTypeDeclarationSyntax> selected,
        Func<SyntaxTree, SemanticModel> modelForTree)
    {
        diagnosticMode = true;
        EmitTypes(selected, modelForTree);
        return diagnostics.Distinct(StringComparer.Ordinal).ToArray();
    }

    private void TryEmit(Action action)
    {
        if (!diagnosticMode) { action(); return; }
        try { action(); }
        catch (InvalidOperationException error) { diagnostics.Add(error.Message); }
    }

    public string Emit(CompilationUnitSyntax root) => Emit([root], _ => model);

    public string Emit(IEnumerable<CompilationUnitSyntax> roots, Func<SyntaxTree, SemanticModel> modelForTree)
    {
        var units = roots.ToList();
        foreach (var declaration in units.SelectMany(root => root.DescendantNodes())
            .OfType<BaseTypeDeclarationSyntax>().Where(declaration => declaration is not
                (ClassDeclarationSyntax or StructDeclarationSyntax or EnumDeclarationSyntax
                    or InterfaceDeclarationSyntax)))
            throw Unsupported(declaration, $"Unsupported type declaration {declaration.Kind()}");
        var classes = units.SelectMany(root => root.DescendantNodes().OfType<BaseTypeDeclarationSyntax>()).ToList();
        return EmitTypes(classes, modelForTree);
    }

    public string EmitTypes(IEnumerable<BaseTypeDeclarationSyntax> selected,
        Func<SyntaxTree, SemanticModel> modelForTree)
    {
        models = modelForTree;
        var classes = selected.ToList();
        if (classes.Count == 0) throw new InvalidOperationException("No C# class was found");
        var groups = classes.GroupBy(declaration => modelForTree(declaration.SyntaxTree)
            .GetDeclaredSymbol(declaration)?.ToDisplayString()
            ?? throw Unsupported(declaration, "Class symbol could not be resolved"))
            .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.Ordinal);
        emittedTypes = groups.Keys.ToHashSet(StringComparer.Ordinal);
        structTypes = groups.Where(group => group.Value[0] is StructDeclarationSyntax)
            .Select(group => group.Key).ToHashSet(StringComparer.Ordinal);
        var counts = groups.Keys.GroupBy(EmittableTypeName, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
        emittedNames = groups.Keys.ToDictionary(name => name,
            name => counts[EmittableTypeName(name)] == 1
                    && EmittableTypeName(name) is not ("Button" or "MouseState" or "SceneManager"
                        or "PlayerInput")
                ? EmittableTypeName(name)
                : "Unity_" + string.Concat(name.Select(character => char.IsLetterOrDigit(character)
                    ? character : '_')), StringComparer.Ordinal);
        foreach (var group in groups.Values.OrderBy(group => InheritanceDepth(group[0], modelForTree)))
        {
            model = modelForTree(group[0].SyntaxTree);
            TryEmit(() =>
            {
                foreach (var declaration in group)
                    PreviewExcludedApis.Reject(declaration, modelForTree(declaration.SyntaxTree));
                if (group[0] is EnumDeclarationSyntax enumeration) EmitEnum(enumeration);
                else if (group[0] is InterfaceDeclarationSyntax contract) EmitInterface(contract);
                else EmitType(group.Cast<TypeDeclarationSyntax>().ToList());
            });
        }
        foreach (var group in groups.Values)
        {
            var symbol = modelForTree(group[0].SyntaxTree).GetDeclaredSymbol(group[0]) as INamedTypeSymbol;
            if (symbol?.TypeKind != TypeKind.Class) continue;
            var contracts = symbol.AllInterfaces.Where(contract => emittedTypes.Contains(contract.ToDisplayString()))
                .Select(EmittedName).Distinct(StringComparer.Ordinal).ToList();
            if (symbol.AllInterfaces.Any(contract =>
                contract.ToDisplayString() == "UnityEngine.ISerializationCallbackReceiver"))
            {
                imports.Add("ISerializationCallbackReceiver");
                contracts.Add("ISerializationCallbackReceiver");
            }
            if (contracts.Count > 0)
                body.AppendLine($"{EmittedName(symbol)}.__unityInterfaces = [{string.Join(", ", contracts)}];");
        }
        foreach (var group in groups.Values.Where(group => group[0].Parent is TypeDeclarationSyntax))
        {
            var symbol = modelForTree(group[0].SyntaxTree).GetDeclaredSymbol(group[0])!;
            if (symbol.ContainingType != null && emittedTypes.Contains(symbol.ContainingType.ToDisplayString()))
                body.AppendLine($"{EmittedName(symbol.ContainingType)}.{symbol.Name} = {EmittedName(symbol)};");
        }
        imports.UnionWith(lowerings.RuntimeImports);
        string names = string.Join(", ", imports.Order(StringComparer.Ordinal));
        return $"// Generated from C# by UnityTranspiler. Edit the source .cs file.\n"
            + (names.Length > 0 ? $"import {{ {names} }} from {JsonSerializer.Serialize(runtimeImport)};\n\n" : "")
            + lowerings.EmitPrelude()
            + body
            + (requiredTypes.Count > 0 ? "\nexport const requiredUnityTypes = "
                + JsonSerializer.Serialize(requiredTypes.Keys.Order(StringComparer.Ordinal)) + ";\n" : "")
            + (groups.Count > 0 ? "\nexport const unityTypes = {\n"
                + string.Join("", groups.Select(group => $"  {JsonSerializer.Serialize(group.Key)}: {emittedNames[group.Key]},\n"))
                + "};\n" : "");
    }

    private static string EmittableTypeName(string qualifiedName)
    {
        string leaf = qualifiedName.Split('.').Last();
        int generic = leaf.IndexOf('<');
        return generic < 0 ? leaf : leaf[..generic];
    }

    private string EmittedName(ITypeSymbol type) => emittedNames.GetValueOrDefault(type.ToDisplayString(), type.Name);

    private static int InheritanceDepth(BaseTypeDeclarationSyntax declaration,
        Func<SyntaxTree, SemanticModel> modelForTree)
    {
        var symbol = modelForTree(declaration.SyntaxTree).GetDeclaredSymbol(declaration);
        int depth = 0;
        for (var current = symbol?.BaseType; current != null; current = current.BaseType) depth++;
        return depth;
    }

    private void EmitEnum(EnumDeclarationSyntax declaration)
    {
        var symbol = model.GetDeclaredSymbol(declaration)
            ?? throw Unsupported(declaration, "Enum symbol could not be resolved");
        body.AppendLine($"export const {EmittedName(symbol)} = globalThis.Object.freeze({{");
        foreach (var member in declaration.Members)
        {
            if (model.GetDeclaredSymbol(member) is not IFieldSymbol field || field.ConstantValue == null)
                throw Unsupported(member, "Enum member value could not be resolved");
            BigInteger number = BigInteger.Parse(Convert.ToString(field.ConstantValue, CultureInfo.InvariantCulture)!,
                CultureInfo.InvariantCulture);
            if (number > 9007199254740991L || number < -9007199254740991L)
                throw Unsupported(member, "Enum value exceeds JavaScript's safe integer range");
            body.AppendLine($"  {JsonSerializer.Serialize(member.Identifier.ValueText)}: {number},");
        }
        body.AppendLine("});");
    }

    private void EmitInterface(InterfaceDeclarationSyntax declaration)
    {
        var symbol = model.GetDeclaredSymbol(declaration)
            ?? throw Unsupported(declaration, "Interface symbol could not be resolved");
        imports.Add("UnityInterface");
        body.AppendLine($"export const {EmittedName(symbol)} = UnityInterface({JsonSerializer.Serialize(symbol.ToDisplayString())});");
        foreach (var field in declaration.Members.OfType<FieldDeclarationSyntax>())
        {
            if (!field.Modifiers.Any(SyntaxKind.StaticKeyword)) continue;
            foreach (var variable in field.Declaration.Variables)
            {
                if (model.GetDeclaredSymbol(variable) is IFieldSymbol { Type.TypeKind: TypeKind.Delegate })
                {
                    imports.Add("UnityEvent");
                    body.AppendLine($"{EmittedName(symbol)}.{variable.Identifier.ValueText} = new UnityEvent();");
                }
            }
        }
    }

    private void EmitType(List<TypeDeclarationSyntax> declarations)
    {
        var declaration = declarations[0];
        bool isStruct = declaration is StructDeclarationSyntax;
        var symbol = model.GetDeclaredSymbol(declaration)
            ?? throw Unsupported(declaration, "Class symbol could not be resolved");
        var baseType = symbol.BaseType;
        string baseClause = "";
        if (isStruct) { }
        else if (baseType?.ToDisplayString() == "UnityEngine.MonoBehaviour")
        {
            imports.Add("MonoBehaviour");
            baseClause = " extends MonoBehaviour";
        }
        else if (baseType?.ToDisplayString() == "UnityEngine.ScriptableObject")
        {
            imports.Add("ScriptableObject");
            baseClause = " extends ScriptableObject";
        }
        else if (baseType?.ToDisplayString() == "UnityEngine.Playables.PlayableAsset")
        {
            imports.Add("PlayableAsset");
            baseClause = " extends PlayableAsset";
        }
        else if (baseType?.ToDisplayString() == "UnityEngine.StateMachineBehaviour")
        {
            imports.Add("StateMachineBehaviour");
            baseClause = " extends StateMachineBehaviour";
        }
        else if (baseType?.ToDisplayString() == "UnityEngine.Playables.PlayableBehaviour")
        {
            imports.Add("PlayableBehaviour");
            baseClause = " extends PlayableBehaviour";
        }
        else if (baseType?.ToDisplayString() == "UnityEngine.PropertyAttribute")
        {
            imports.Add("PropertyAttribute");
            baseClause = " extends PropertyAttribute";
        }
        else if (baseType?.ToDisplayString() == "UnityEngine.UI.Image")
        {
            imports.Add("Image");
            baseClause = " extends Image";
        }
        else if (baseType?.ToDisplayString() == "UnityEngine.InputSystem.OnScreen.OnScreenControl")
        {
            imports.Add("OnScreenControl");
            baseClause = " extends OnScreenControl";
        }
        else if (baseType is INamedTypeSymbol jsonConverter
            && jsonConverter.OriginalDefinition.ToDisplayString()
                == "Newtonsoft.Json.Converters.CustomCreationConverter<T>")
        {
            imports.Add("PreviewCustomCreationConverter");
            baseClause = " extends PreviewCustomCreationConverter";
        }
        else if (editorMode && baseType?.ToDisplayString() == "UnityEditor.EditorWindow")
        {
            imports.Add("EditorWindow");
            baseClause = " extends EditorWindow";
        }
        else if (baseType?.ToDisplayString() == "Unity.Netcode.NetworkBehaviour")
        {
            imports.Add("NetworkBehaviour");
            baseClause = " extends NetworkBehaviour";
        }
        else if (baseType?.Name == "UnityEvent"
            && baseType.ContainingNamespace.ToDisplayString() == "UnityEngine.Events")
        {
            imports.Add("UnityEvent");
            baseClause = " extends UnityEvent";
        }
        else if (baseType != null && emittedTypes.Contains(baseType.ToDisplayString()))
            baseClause = $" extends {EmittedName(baseType)}";
        else if (allowExternalTypes && !isStruct && baseType != null
            && baseType.SpecialType != SpecialType.System_Object)
            baseClause = $" extends {RuntimeTypeName(baseType, declaration)}";
        else if (editorMode && baseType?.ToDisplayString() is ("BaseGameModule" or "BaseCharacterModule" or "BasePlygroundModule"))
        {
            baseClause = $" extends {ExternalTypeName(baseType)}";
        }
        else if (baseType?.SpecialType != SpecialType.System_Object)
            TryEmit(() => throw Unsupported(declaration, $"Base type {baseType} is not mapped to the runtime"));
        body.AppendLine($"export class {EmittedName(symbol)}{baseClause} {{");
        body.AppendLine($"  static __unityFullName = {JsonSerializer.Serialize(symbol.ToDisplayString())};");
        if (symbol.IsAbstract)
            body.AppendLine("  static __unityAbstract = true;");
        if (symbol.GetAttributes().Any(attribute => attribute.AttributeClass?.ToDisplayString()
                == "UnityEngine.DisallowMultipleComponent"))
            body.AppendLine("  static __unityDisallowMultipleComponent = true;");
        var requiredComponentTypes = symbol.GetAttributes()
            .Where(attribute => attribute.AttributeClass?.ToDisplayString()
                == "UnityEngine.RequireComponent")
            .SelectMany(attribute => attribute.ConstructorArguments)
            .Select(argument => argument.Value as ITypeSymbol)
            .Where(type => type != null)
            .Select(type => RuntimeTypeName(type!, declaration))
            .ToArray();
        if (requiredComponentTypes.Length > 0)
            body.AppendLine($"  static __unityRequiredComponents = () => [{string.Join(", ", requiredComponentTypes)}];");
        var methodSignatures = declarations.SelectMany(part =>
        {
            var semanticModel = models(part.SyntaxTree);
            return part.Members.OfType<MethodDeclarationSyntax>()
                .Where(method => editorMode || !EditorCodePolicy.IsEditorOnlyMember(method))
                .Select(method => semanticModel.GetDeclaredSymbol(method) as IMethodSymbol)
                .Where(method => method != null)
                .Select(method => new {
                    name = method!.Name,
                    emittedName = MethodName(method),
                    parameters = method.Parameters.Select(parameter => parameter.Type.ToDisplayString()).ToArray(),
                });
        }).ToArray();
        if (methodSignatures.Length > 0)
            body.AppendLine($"  static __unityMethodSignatures = {JsonSerializer.Serialize(methodSignatures)};");
        var serializedPrivateFields = declarations.SelectMany(part =>
        {
            var semanticModel = models(part.SyntaxTree);
            return part.Members.OfType<FieldDeclarationSyntax>()
                .Where(field => !field.Modifiers.Any(SyntaxKind.StaticKeyword)
                    && !field.Modifiers.Any(SyntaxKind.ConstKeyword))
                .SelectMany(field => field.Declaration.Variables)
                .Select(variable => semanticModel.GetDeclaredSymbol(variable) as IFieldSymbol)
                .Where(field => field != null && field.DeclaredAccessibility != Accessibility.Public
                    && field.GetAttributes().Any(attribute => attribute.AttributeClass?.ToDisplayString()
                        is "UnityEngine.SerializeField" or "UnityEngine.SerializeReference"))
                .Select(field => field!.Name);
        }).Distinct().ToArray();
        if (serializedPrivateFields.Length > 0)
            body.AppendLine($"  static __unitySerializedFields = {JsonSerializer.Serialize(serializedPrivateFields)};");
        var jsonFields = declarations.SelectMany(part =>
        {
            var semanticModel = models(part.SyntaxTree);
            return part.Members.OfType<FieldDeclarationSyntax>()
                .Where(field => !field.Modifiers.Any(SyntaxKind.StaticKeyword)
                    && !field.Modifiers.Any(SyntaxKind.ConstKeyword))
                .SelectMany(field => field.Declaration.Variables)
                .Select(variable => semanticModel.GetDeclaredSymbol(variable) as IFieldSymbol)
                .Where(field => field != null && (field.DeclaredAccessibility == Accessibility.Public
                    || field.GetAttributes().Any(attribute => attribute.AttributeClass?.ToDisplayString()
                        is "UnityEngine.SerializeField" or "UnityEngine.SerializeReference"))
                    && !field.GetAttributes().Any(attribute => attribute.AttributeClass?.ToDisplayString()
                        == "System.NonSerializedAttribute"))
                .Select(field => field!.Name);
        }).Distinct().ToArray();
        body.AppendLine($"  static __unityJsonFields = {JsonSerializer.Serialize(jsonFields)};");
        var managedReferenceFields = declarations.SelectMany(part =>
        {
            var semanticModel = models(part.SyntaxTree);
            return part.Members.OfType<FieldDeclarationSyntax>()
                .Where(field => !field.Modifiers.Any(SyntaxKind.StaticKeyword)
                    && !field.Modifiers.Any(SyntaxKind.ConstKeyword))
                .SelectMany(field => field.Declaration.Variables)
                .Select(variable => semanticModel.GetDeclaredSymbol(variable) as IFieldSymbol)
                .Where(field => field?.GetAttributes().Any(attribute =>
                    attribute.AttributeClass?.ToDisplayString() == "UnityEngine.SerializeReference") == true)
                .Select(field => field!.Name);
        }).Distinct().ToArray();
        body.AppendLine($"  static __unityManagedReferenceFields = {JsonSerializer.Serialize(managedReferenceFields)};");
        var formerNames = new Dictionary<string, string>();
        foreach (var part in declarations)
        {
            var semanticModel = models(part.SyntaxTree);
            foreach (var variable in part.Members.OfType<FieldDeclarationSyntax>()
                .Where(field => !field.Modifiers.Any(SyntaxKind.StaticKeyword)
                    && !field.Modifiers.Any(SyntaxKind.ConstKeyword))
                .SelectMany(field => field.Declaration.Variables))
            {
                if (semanticModel.GetDeclaredSymbol(variable) is not IFieldSymbol field) continue;
                foreach (var attribute in field.GetAttributes().Where(attribute =>
                    attribute.AttributeClass?.ToDisplayString()
                        == "UnityEngine.Serialization.FormerlySerializedAsAttribute"))
                    if (attribute.ConstructorArguments.Length == 1
                        && attribute.ConstructorArguments[0].Value is string oldName)
                        formerNames[oldName] = field.Name;
            }
        }
        if (formerNames.Count > 0)
            body.AppendLine($"  static __unityFormerlySerializedFields = {JsonSerializer.Serialize(formerNames)};");
        var nonSerializedFields = declarations.SelectMany(part =>
        {
            var semanticModel = models(part.SyntaxTree);
            return part.Members.OfType<FieldDeclarationSyntax>()
                .Where(field => !field.Modifiers.Any(SyntaxKind.StaticKeyword)
                    && !field.Modifiers.Any(SyntaxKind.ConstKeyword))
                .SelectMany(field => field.Declaration.Variables)
                .Select(variable => semanticModel.GetDeclaredSymbol(variable) as IFieldSymbol)
                .Where(field => field?.GetAttributes().Any(attribute =>
                    attribute.AttributeClass?.ToDisplayString() == "System.NonSerializedAttribute") == true)
                .Select(field => field!.Name);
        }).Distinct().ToArray();
        if (nonSerializedFields.Length > 0)
            body.AppendLine($"  static __unityNonSerializedFields = {JsonSerializer.Serialize(nonSerializedFields)};");
        if (!isStruct && symbol.GetAttributes().Any(attribute =>
            attribute.AttributeClass?.ToDisplayString() == "System.SerializableAttribute"))
            body.AppendLine("  static __unitySerializableType = true;");
        var executionOrder = symbol.GetAttributes().FirstOrDefault(attribute =>
            attribute.AttributeClass?.ToDisplayString() == "UnityEngine.DefaultExecutionOrder");
        if (executionOrder?.ConstructorArguments.Length == 1
            && executionOrder.ConstructorArguments[0].Value is int order)
            body.AppendLine($"  static __unityExecutionOrder = {order};");
        var runtimeInitializers = symbol.GetMembers().OfType<IMethodSymbol>()
            .Where(method => method.GetAttributes().Any(attribute =>
                attribute.AttributeClass?.ToDisplayString()
                    == "UnityEngine.RuntimeInitializeOnLoadMethodAttribute"))
            .ToArray();
        if (runtimeInitializers.Length > 0)
        {
            var entries = new List<string>();
            foreach (var method in runtimeInitializers)
            {
                if (!method.IsStatic || method.Parameters.Length != 0)
                    throw new InvalidOperationException($"{method.Locations.FirstOrDefault()?.GetLineSpan().Path}:"
                        + " RuntimeInitializeOnLoadMethod requires a static parameterless method");
                var attribute = method.GetAttributes().First(value =>
                    value.AttributeClass?.ToDisplayString()
                        == "UnityEngine.RuntimeInitializeOnLoadMethodAttribute");
                var loadType = attribute.ConstructorArguments.Length == 0 ? 0
                    : (int?)attribute.ConstructorArguments[0].Value ?? 0;
                entries.Add($"{{ method: {JsonSerializer.Serialize(method.Name)}, loadType: {loadType} }}");
            }
            body.AppendLine($"  static __unityRuntimeInitializers = [{string.Join(", ", entries)}];");
        }
        var listFields = declarations.SelectMany(part =>
        {
            var semanticModel = models(part.SyntaxTree);
            return part.Members.OfType<FieldDeclarationSyntax>()
                .Where(field => !field.Modifiers.Any(SyntaxKind.StaticKeyword)
                    && !field.Modifiers.Any(SyntaxKind.ConstKeyword))
                .SelectMany(field => field.Declaration.Variables)
                .Select(variable => semanticModel.GetDeclaredSymbol(variable) as IFieldSymbol)
                .Where(field => field != null && IsList(field.Type))
                .Select(field => field!.Name);
        }).Distinct().ToArray();
        if (listFields.Length > 0)
            body.AppendLine($"  static __unityListFields = {JsonSerializer.Serialize(listFields)};");
        var constructors = declarations.SelectMany(part => part.Members.OfType<ConstructorDeclarationSyntax>()
            .Where(member => !member.Modifiers.Any(SyntaxKind.StaticKeyword)
                && (editorMode || !EditorCodePolicy.IsEditorOnlyMember(member)))).ToList();
        var staticConstructors = declarations.SelectMany(part => part.Members.OfType<ConstructorDeclarationSyntax>()
            .Where(member => member.Modifiers.Any(SyntaxKind.StaticKeyword)
                && (editorMode || !EditorCodePolicy.IsEditorOnlyMember(member)))).ToList();
        bool overloadConstructors = constructors.Count > 1;
        if (overloadConstructors)
        {
            body.AppendLine("  constructor(__unityCtorId, ...__unityCtorArgs) {");
            body.AppendLine("    switch (__unityCtorId) {");
        }
        foreach (var constructor in constructors)
        {
            model = models(constructor.SyntaxTree);
            TryEmit(() => EmitConstructor(constructor, baseClause.Length > 0, overloadConstructors));
        }
        if (overloadConstructors)
        {
            if (isStruct) body.AppendLine("      case -1: break; // implicit struct default constructor");
            body.AppendLine("      default: throw new Error('Unresolved C# constructor overload');");
            body.AppendLine("    }");
            body.AppendLine("  }");
        }
        foreach (var part in declarations)
        {
            model = models(part.SyntaxTree);
            foreach (var member in part.Members)
            {
                if (!editorMode && EditorCodePolicy.IsEditorOnlyMember(member)) continue;
                TryEmit(() =>
                {
                    switch (member)
                    {
                        case FieldDeclarationSyntax field: EmitField(field); break;
                        case EventFieldDeclarationSyntax events: EmitEventField(events); break;
                        case PropertyDeclarationSyntax property: EmitProperty(property); break;
                        case IndexerDeclarationSyntax indexer: EmitIndexer(indexer); break;
                        case ConversionOperatorDeclarationSyntax conversion: EmitConversionOperator(conversion); break;
                        case ConstructorDeclarationSyntax: break;
                        case MethodDeclarationSyntax method: EmitMethod(method); break;
                        case DelegateDeclarationSyntax: break; // Type-only C# signature; JS callbacks are functions.
                        case BaseTypeDeclarationSyntax: break; // Emitted separately if reachable.
                        default: throw Unsupported(member, $"Unsupported class member {member.Kind()}");
                    }
                });
            }
        }
        foreach (var constructor in staticConstructors)
        {
            model = models(constructor.SyntaxTree);
            TryEmit(() =>
            {
                if (constructor.Body == null)
                    throw Unsupported(constructor, "Static constructor body is missing");
                body.AppendLine("  static {");
                StatementSequence(constructor.Body.Statements, 2);
                body.AppendLine("  }");
            });
        }
        if (isStruct)
        {
            body.AppendLine("  clone() {");
            body.AppendLine($"    const copy = globalThis.Object.assign(globalThis.Object.create({EmittedName(symbol)}.prototype), this);");
            foreach (var field in symbol.GetMembers().OfType<IFieldSymbol>()
                .Where(field => !field.IsStatic && IsCopiedValueType(field.Type)))
            {
                string name = JsonSerializer.Serialize(field.Name);
                body.AppendLine($"    copy[{name}] = this[{name}]?.clone();");
            }
            body.AppendLine("    return copy;");
            body.AppendLine("  }");
        }
        body.AppendLine("}");
        if (isStruct)
            body.AppendLine($"{EmittedName(symbol)}.__unityValueType = true;");
        if (!isStruct && constructors.Count > 0
            && constructors.All(constructor => constructor.ParameterList.Parameters.Count > 0)
            && symbol.GetAttributes().Any(attribute => attribute.AttributeClass?.ToDisplayString()
                == "System.SerializableAttribute"))
            body.AppendLine($"{EmittedName(symbol)}.__unitySerializationBypassConstructor = true;");
        if (overloadConstructors)
        {
            var defaultConstructor = ConstructorOverloads(symbol).FirstOrDefault(candidate => candidate.Parameters.Length == 0)
                ?? ConstructorOverloads(symbol).FirstOrDefault(candidate => candidate.Parameters.All(parameter => parameter.IsOptional));
            if (defaultConstructor != null)
                body.AppendLine($"{EmittedName(symbol)}.__unityDefaultConstructorId = {ConstructorId(defaultConstructor)};");
            else if (isStruct)
                body.AppendLine($"{EmittedName(symbol)}.__unityDefaultConstructorId = -1;");
        }
        if (emittedTypes.Count == 1)
            body.AppendLine($"export const unityTypeName = {JsonSerializer.Serialize(symbol.ToDisplayString())};");
    }

    private void EmitField(FieldDeclarationSyntax field)
    {
        bool isStatic = field.Modifiers.Any(SyntaxKind.StaticKeyword)
            || field.Modifiers.Any(SyntaxKind.ConstKeyword);
        foreach (var variable in field.Declaration.Variables)
        {
            SourceMarker(variable, 1);
            var symbol = model.GetDeclaredSymbol(variable) as IFieldSymbol;
            bool componentField = false;
            for (var owner = symbol?.ContainingType; owner != null; owner = owner.BaseType)
                if (owner.ToDisplayString() == "UnityEngine.MonoBehaviour") componentField = true;
            bool serializedField = symbol?.DeclaredAccessibility == Accessibility.Public
                || symbol?.GetAttributes().Any(attribute => attribute.AttributeClass?.ToDisplayString()
                    == "UnityEngine.SerializeField") == true;
            var serializableType = symbol?.Type as INamedTypeSymbol;
            bool unitySerializableDefault = variable.Initializer == null && componentField && serializedField
                && serializableType?.TypeKind == TypeKind.Class
                && serializableType.GetAttributes().Any(attribute => attribute.AttributeClass?.ToDisplayString()
                    == "System.SerializableAttribute")
                && serializableType.InstanceConstructors.Any(constructor => constructor.Parameters.Length == 0
                    && constructor.DeclaredAccessibility == Accessibility.Public)
                && emittedTypes.Contains(serializableType.ToDisplayString());
            string value = unitySerializableDefault
                ? $"new {EmittedName(serializableType!)}("
                    + (ConstructorOverloads(serializableType!).Count > 1
                        ? ConstructorId(serializableType!.InstanceConstructors.First(constructor =>
                            constructor.Parameters.Length == 0 && constructor.DeclaredAccessibility == Accessibility.Public))
                            .ToString(CultureInfo.InvariantCulture) : "") + ")"
                : variable.Initializer == null ? DefaultValue(symbol?.Type)
                : ConvertImplicitValue(variable.Initializer.Value,
                    CopyStruct(variable.Initializer.Value, Expression(variable.Initializer.Value)), symbol?.Type);
            if (isStatic && symbol?.ContainingType.IsValueType == true
                && variable.Initializer?.Value is ObjectCreationExpressionSyntax
                    or ImplicitObjectCreationExpressionSyntax)
            {
                string fieldName = variable.Identifier.ValueText;
                string descriptor = $"{{ value: __unityValue, writable: true, configurable: true }}";
                body.AppendLine($"  static get {fieldName}() {{ const __unityValue = {value}; "
                    + $"globalThis.Object.defineProperty(this, {JsonSerializer.Serialize(fieldName)}, {descriptor}); "
                    + "return __unityValue; }");
                body.AppendLine($"  static set {fieldName}(__unityValue) {{ "
                    + $"globalThis.Object.defineProperty(this, {JsonSerializer.Serialize(fieldName)}, {descriptor}); }}");
            }
            else body.AppendLine($"  {(isStatic ? "static " : "")}{variable.Identifier.ValueText} = {value};");
        }
    }

    private void EmitEventField(EventFieldDeclarationSyntax events)
    {
        imports.Add("UnityEvent");
        bool isStatic = events.Modifiers.Any(SyntaxKind.StaticKeyword);
        foreach (var variable in events.Declaration.Variables)
            body.AppendLine($"  {(isStatic ? "static " : "")}{variable.Identifier.ValueText} = new UnityEvent();");
    }

    private void EmitProperty(PropertyDeclarationSyntax property)
    {
        var symbol = model.GetDeclaredSymbol(property)
            ?? throw Unsupported(property, "Property symbol could not be resolved");
        string name = property.Identifier.ValueText;
        string prefix = symbol.IsStatic ? "static " : "";
        string backing = $"__unityBacking_{name}";
        bool auto = property.AccessorList?.Accessors.Any(accessor =>
            accessor.Body == null && accessor.ExpressionBody == null) == true;
        if (auto)
        {
            string value = property.Initializer == null ? DefaultValue(symbol.Type)
                : ConvertImplicitValue(property.Initializer.Value,
                    CopyStruct(property.Initializer.Value, Expression(property.Initializer.Value)), symbol.Type);
            body.AppendLine($"  {prefix}{backing} = {value};");
        }
        if (property.ExpressionBody != null)
        {
            SourceMarker(property.ExpressionBody.Expression, 1);
            string value = ConvertImplicitValue(property.ExpressionBody.Expression,
                Expression(property.ExpressionBody.Expression), symbol.Type);
            body.AppendLine($"  {prefix}get {name}() {{ return {value}; }}");
            return;
        }
        if (property.AccessorList == null)
            throw Unsupported(property, "Property has no supported accessor body");
        // A JavaScript accessor on a subclass shadows both accessors on the
        // prototype. C# permits overriding only the setter while inheriting
        // the getter, as MShootable.IsAiming does.
        if (symbol.IsOverride && symbol.OverriddenProperty?.GetMethod != null
            && !property.AccessorList.Accessors.Any(accessor =>
                accessor.IsKind(SyntaxKind.GetAccessorDeclaration)))
            body.AppendLine($"  {prefix}get {name}() {{ return super.{name}; }}");
        foreach (var accessor in property.AccessorList.Accessors)
        {
            string kind = accessor.IsKind(SyntaxKind.GetAccessorDeclaration) ? "get"
                : accessor.IsKind(SyntaxKind.SetAccessorDeclaration) ? "set"
                : throw Unsupported(accessor, "Property accessor is not mapped");
            body.AppendLine($"  {prefix}{kind} {name}({(kind == "set" ? "value" : "")}) {{");
            if (accessor.ExpressionBody != null)
            {
                SourceMarker(accessor.ExpressionBody.Expression, 2);
                string expression = Expression(accessor.ExpressionBody.Expression);
                if (kind == "get") expression = ConvertImplicitValue(accessor.ExpressionBody.Expression,
                    expression, symbol.Type);
                body.AppendLine(kind == "get" ? $"    return {expression};" : $"    {expression};");
            }
            else if (accessor.Body != null)
            {
                StatementSequence(accessor.Body.Statements, 2);
            }
            else if (auto)
            {
                bool copyValue = IsCopiedValueType(symbol.Type);
                body.AppendLine(kind == "get"
                    ? $"    return this.{backing}{(copyValue ? "?.clone()" : "")};"
                    : $"    this.{backing} = value{(copyValue ? "?.clone()" : "")};");
            }
            body.AppendLine("  }");
        }
        if (auto && !property.AccessorList.Accessors.Any(accessor =>
            accessor.IsKind(SyntaxKind.SetAccessorDeclaration)))
            body.AppendLine($"  {prefix}set {name}(value) {{ this.{backing} = value"
                + $"{(IsCopiedValueType(symbol.Type) ? "?.clone()" : "")}; }}");
    }

    private static string IndexerMethodName(IPropertySymbol symbol, string action)
    {
        if (symbol.Parameters.Length != 1)
            throw new InvalidOperationException("Only single-argument custom indexers are supported");
        int overloads = symbol.ContainingType.GetMembers().OfType<IPropertySymbol>()
            .Count(property => property.IsIndexer);
        if (overloads == 1) return action + "Item";
        return symbol.Parameters[0].Type.SpecialType switch
        {
            SpecialType.System_Int32 => action + "ItemInt32",
            SpecialType.System_String => action + "ItemString",
            _ => throw new InvalidOperationException("Custom indexer overload type needs lowering"),
        };
    }

    private void EmitIndexer(IndexerDeclarationSyntax indexer)
    {
        if (model.GetDeclaredSymbol(indexer) is not IPropertySymbol symbol
            || indexer.ParameterList.Parameters.Count != 1)
            throw Unsupported(indexer, "Custom indexer needs one resolved parameter");
        string parameter = indexer.ParameterList.Parameters[0].Identifier.ValueText;
        if (indexer.ExpressionBody != null)
        {
            SourceMarker(indexer.ExpressionBody.Expression, 1);
            body.AppendLine($"  {IndexerMethodName(symbol, "Get")}({parameter}) {{ "
                + $"return {Expression(indexer.ExpressionBody.Expression)}; }}");
            return;
        }
        if (indexer.AccessorList == null)
            throw Unsupported(indexer, "Custom indexer accessor body is missing");
        foreach (var accessor in indexer.AccessorList.Accessors)
        {
            string name = accessor.IsKind(SyntaxKind.GetAccessorDeclaration) ? IndexerMethodName(symbol, "Get")
                : accessor.IsKind(SyntaxKind.SetAccessorDeclaration) ? IndexerMethodName(symbol, "Set")
                : throw Unsupported(accessor, "Custom indexer accessor needs lowering");
            body.AppendLine($"  {name}({parameter}{(name.StartsWith("Set", StringComparison.Ordinal) ? ", value" : "")}) {{");
            if (accessor.ExpressionBody != null)
                body.AppendLine($"    {(name.StartsWith("Get", StringComparison.Ordinal) ? "return " : "")}{Expression(accessor.ExpressionBody.Expression)};");
            else if (accessor.Body != null)
                StatementSequence(accessor.Body.Statements, 2);
            else
                throw Unsupported(accessor, "Auto custom indexer needs lowering");
            body.AppendLine("  }");
        }
    }

    private string DefaultValue(ITypeSymbol? type)
    {
        if (type is INamedTypeSymbol { IsTupleType: true } tuple)
            return "{ " + string.Join(", ", tuple.TupleElements.Select(element =>
                $"{JsonSerializer.Serialize(element.Name)}: {DefaultValue(element.Type)}")) + " }";
        for (var current = type as INamedTypeSymbol; current != null; current = current.BaseType)
            if (current.ToDisplayString().StartsWith("UnityEngine.Events.UnityEvent", StringComparison.Ordinal))
            {
                imports.Add("UnityEvent");
                return "new UnityEvent()";
            }
        if (type?.SpecialType == SpecialType.System_Boolean) return "false";
        if (type?.SpecialType == SpecialType.System_UInt64) return "0n";
        if (type?.SpecialType is SpecialType.System_Int32 or SpecialType.System_Single
            or SpecialType.System_Double) return "0";
        if (type?.TypeKind == TypeKind.Enum) return "0";
        if (type?.ToDisplayString() == "UnityEngine.AnimatorStateInfo")
            return "{ normalizedTime: 0, length: 0, shortNameHash: 0, fullPathHash: 0, "
                + "tagHash: 0, speed: 0, loop: false, IsName: () => false, IsTag: () => false }";
        if (type?.ToDisplayString() == "UnityEngine.RaycastHit")
        {
            imports.Add("RaycastHit");
            return "new RaycastHit()";
        }
        if (type?.ToDisplayString() is "UnityEngine.Matrix4x4" or "UnityEngine.Plane"
            or "UnityEngine.Bounds" or "UnityEngine.Rect" or "UnityEngine.Ray" or "UnityEngine.Ray2D")
        {
            imports.Add(type.Name);
            return $"new {type.Name}()";
        }
        if (type?.ToDisplayString() == "UnityEngine.LayerMask")
        {
            imports.Add("LayerMask");
            return "new LayerMask()";
        }
        if (type?.ToDisplayString() == "UnityEngine.Playables.PlayableGraph")
        {
            imports.Add("PlayableGraph");
            return "new PlayableGraph()";
        }
        if (type is INamedTypeSymbol exposedReference
            && exposedReference.OriginalDefinition.ToDisplayString() == "UnityEngine.ExposedReference<T>")
        {
            imports.Add("ExposedReference");
            return "new ExposedReference()";
        }
        if (type?.ToDisplayString() == "UnityEngine.RenderingLayerMask")
        {
            imports.Add("RenderingLayerMask");
            return "new RenderingLayerMask()";
        }
        if (type?.ToDisplayString() is "UnityEngine.EntityId" or "UnityEngine.InstanceID" or "UnityEngine.PropertyName")
        {
            imports.Add(type.Name);
            return $"new {type.Name}()";
        }
        if (type?.ToDisplayString() == "UnityEngine.Color")
        {
            imports.Add("Color");
            return "new Color(0, 0, 0, 0)";
        }
        if (type?.ToDisplayString() == "System.Guid")
        {
            imports.Add("CSharpGuid");
            return "CSharpGuid.Empty";
        }
        if (type?.ContainingNamespace?.ToDisplayString() == "UnityEngine"
            && type.Name is "Vector2" or "Vector3" or "Vector4" or "Quaternion")
        {
            imports.Add(type.Name);
            return $"new {type.Name}()";
        }
        if (type is INamedTypeSymbol valueType
            && structTypes.Contains(valueType.OriginalDefinition.ToDisplayString()))
            return $"new {EmittedName(type)}({(ConstructorOverloads(valueType).Count > 1 ? "-1" : "")})";
        return "null";
    }

    private static List<IMethodSymbol> ConstructorOverloads(INamedTypeSymbol type) =>
        type.InstanceConstructors.Where(candidate => !candidate.IsImplicitlyDeclared)
            .OrderBy(candidate => candidate.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                StringComparer.Ordinal).ToList();

    private static int ConstructorId(IMethodSymbol constructor)
    {
        if (constructor.IsImplicitlyDeclared && constructor.ContainingType.TypeKind == TypeKind.Struct
            && constructor.Parameters.Length == 0)
            return -1;
        var overloads = ConstructorOverloads(constructor.ContainingType);
        int index = overloads.FindIndex(candidate =>
            SymbolEqualityComparer.Default.Equals(candidate.OriginalDefinition, constructor.OriginalDefinition));
        if (index < 0) throw new InvalidOperationException($"Constructor overload could not be resolved: {constructor}");
        return index;
    }

    private void EmitConstructor(ConstructorDeclarationSyntax constructor, bool hasBase, bool dispatch)
    {
        if (constructor.Modifiers.Any(SyntaxKind.StaticKeyword))
            throw Unsupported(constructor, "Static constructors need initialization lowering");
        if (constructor.Body == null && constructor.ExpressionBody == null)
            throw Unsupported(constructor, "Constructor body is missing");
        var parameters = constructor.ParameterList.Parameters;
        if (parameters.Any(parameter => parameter.Modifiers.Count > 0))
            throw Unsupported(constructor, "ref, out, and params constructor parameters need lowering");
        var arguments = parameters.Select(ParameterDeclaration).ToList();
        if (dispatch)
        {
            var symbol = model.GetDeclaredSymbol(constructor)
                ?? throw Unsupported(constructor, "Constructor symbol could not be resolved");
            body.AppendLine($"      case {ConstructorId(symbol)}: {{");
            body.AppendLine($"        const [{string.Join(", ", arguments)}] = __unityCtorArgs;");
        }
        else body.AppendLine($"  constructor({string.Join(", ", arguments)}) {{");
        if (!dispatch && constructor.Parent is StructDeclarationSyntax && parameters.Count > 0)
            body.AppendLine("    if (arguments.length === 0) return; // implicit struct default constructor");
        EmitConstructorBody(constructor, hasBase, dispatch ? 4 : 2, 0);
        if (dispatch) body.AppendLine("        break;\n      }");
        else body.AppendLine("  }");
    }

    private void EmitConstructorBody(ConstructorDeclarationSyntax constructor, bool hasBase,
        int depth, int chainDepth)
    {
        var indent = new string(' ', depth * 2);
        if (constructor.Initializer?.IsKind(SyntaxKind.ThisConstructorInitializer) == true)
        {
            var target = model.GetSymbolInfo(constructor.Initializer).Symbol as IMethodSymbol;
            var targetSyntax = target?.DeclaringSyntaxReferences
                .Select(reference => reference.GetSyntax()).OfType<ConstructorDeclarationSyntax>()
                .FirstOrDefault();
            if (targetSyntax == null)
                throw Unsupported(constructor.Initializer,
                    "Chained constructor target could not be resolved");
            var args = constructor.Initializer.ArgumentList.Arguments
                .Select(argument => Expression(argument.Expression));
            body.AppendLine($"{indent}const __unityChainArgs{chainDepth} = [{string.Join(", ", args)}];");
            body.AppendLine($"{indent}{{");
            var previousModel = model;
            try
            {
                model = models(targetSyntax.SyntaxTree);
                var parameters = targetSyntax.ParameterList.Parameters;
                if (parameters.Any(parameter => parameter.Modifiers.Count > 0))
                    throw Unsupported(targetSyntax,
                        "ref, out, and params chained constructor parameters need lowering");
                body.AppendLine($"{indent}  const [{string.Join(", ", parameters.Select(ParameterDeclaration))}] = __unityChainArgs{chainDepth};");
                EmitConstructorBody(targetSyntax, hasBase, depth + 1, chainDepth + 1);
            }
            finally { model = previousModel; }
            body.AppendLine($"{indent}}}");
        }
        else if (hasBase)
        {
            var baseArguments = constructor.Initializer?.ArgumentList.Arguments
                .Select(argument => Expression(argument.Expression)) ?? [];
            var baseSymbol = model.GetDeclaredSymbol(constructor)?.ContainingType.BaseType;
            if (baseSymbol != null && emittedTypes.Contains(baseSymbol.ToDisplayString())
                && ConstructorOverloads(baseSymbol).Count > 1)
            {
                var target = constructor.Initializer == null
                    ? baseSymbol.InstanceConstructors.SingleOrDefault(candidate => candidate.Parameters.Length == 0)
                    : model.GetSymbolInfo(constructor.Initializer).Symbol as IMethodSymbol;
                if (target == null)
                    throw Unsupported(constructor, "Base constructor overload could not be resolved");
                baseArguments = new[] { ConstructorId(target).ToString(CultureInfo.InvariantCulture) }
                    .Concat(baseArguments);
            }
            body.AppendLine($"{indent}super({string.Join(", ", baseArguments)});");
        }
        else if (constructor.Initializer != null)
            throw Unsupported(constructor.Initializer, "Constructor initializer has no emitted base type");
        if (constructor.Body != null)
        {
            EmitPatternVariables(constructor.Body, depth);
            StatementSequence(constructor.Body.Statements, depth);
        }
        else if (constructor.ExpressionBody != null)
        {
            SourceMarker(constructor.ExpressionBody.Expression, depth);
            body.AppendLine($"{indent}{Expression(constructor.ExpressionBody.Expression)};");
        }
    }

    private static string ConstantValue(object? value, SyntaxNode source) => value switch
    {
        null => "null",
        bool boolean => boolean ? "true" : "false",
        string text => JsonSerializer.Serialize(text),
        char character => JsonSerializer.Serialize(character.ToString()),
        byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal
            => Convert.ToString(value, CultureInfo.InvariantCulture)!,
        _ => throw Unsupported(source, $"Optional constructor default {value} cannot be emitted"),
    };

    private string ParameterDeclaration(ParameterSyntax parameter)
    {
        string value = JsLocalName(parameter.Identifier.ValueText);
        if (parameter.Default == null) return value;
        var symbol = model.GetDeclaredSymbol(parameter) as IParameterSymbol;
        if (symbol?.HasExplicitDefaultValue != true)
            throw Unsupported(parameter, "Optional parameter default is not a compile-time constant");
        string defaultValue = parameter.Default.Value is DefaultExpressionSyntax
            || parameter.Default.Value.IsKind(SyntaxKind.DefaultLiteralExpression)
            ? DefaultValue(symbol.Type) : ConstantValue(symbol.ExplicitDefaultValue, parameter);
        return $"{value} = {defaultValue}";
    }

    private static string JsLocalName(string name) => name switch
    {
        "PlayerInput" or "await" or "break" or "case" or "catch" or "class" or "const" or "continue"
            or "debugger" or "default" or "delete" or "do" or "else" or "enum"
            or "export" or "extends" or "false" or "finally" or "for" or "function"
            or "if" or "import" or "in" or "instanceof" or "let" or "new" or "null"
            or "return" or "static" or "super" or "switch" or "this" or "throw"
            or "true" or "try" or "typeof" or "var" or "void" or "while" or "with"
            or "yield" or "implements" or "interface" or "package" or "private"
            or "protected" or "public" or "arguments" or "eval" => $"__unityLocal_{name}",
        _ => name,
    };

    private void EmitMethod(MethodDeclarationSyntax method)
    {
        var parameters = method.ParameterList.Parameters;
        if (parameters.Select((parameter, index) => (parameter, index)).Any(item =>
            item.parameter.Modifiers.Any(modifier => !modifier.IsKind(SyntaxKind.ThisKeyword)
                && !modifier.IsKind(SyntaxKind.RefKeyword) && !modifier.IsKind(SyntaxKind.OutKeyword)
                && !modifier.IsKind(SyntaxKind.ParamsKeyword)
                || modifier.IsKind(SyntaxKind.ThisKeyword) && item.index != 0)))
            throw Unsupported(method, "params and unsupported parameter modifiers need lowering");
        var symbol = model.GetDeclaredSymbol(method) ?? throw Unsupported(method, "Method symbol could not be resolved");
        string prefix = symbol.IsStatic ? "static " : "";
        bool iterator = method.Body?.DescendantNodes().OfType<YieldStatementSyntax>()
            .Any(item => !item.Ancestors().OfType<LocalFunctionStatementSyntax>().Any()) == true;
        var runtimeParameters = (method.TypeParameterList?.Parameters.Select(parameter => parameter.Identifier.ValueText)
            ?? Enumerable.Empty<string>()).Concat(parameters.Select(parameter =>
                (parameter.Modifiers.Any(SyntaxKind.ParamsKeyword) ? "..." : "") + ParameterDeclaration(parameter)));
        body.AppendLine($"  {prefix}{(symbol.IsAsync ? "async " : "")}{(iterator ? "*" : "")}{MethodName(symbol)}({string.Join(", ", runtimeParameters)}) {{");
        if (method.Body != null)
        {
            EmitPatternVariables(method.Body, 2);
            StatementSequence(method.Body.Statements, 2);
        }
        else if (method.ExpressionBody != null)
        {
            EmitPatternVariables(method.ExpressionBody, 2);
            SourceMarker(method.ExpressionBody.Expression, 2);
            string value = Expression(method.ExpressionBody.Expression);
            body.AppendLine(symbol.ReturnsVoid ? $"    {value};"
                : $"    return {CopyStruct(method.ExpressionBody.Expression, value)};");
        }
        else body.AppendLine($"    throw new Error({JsonSerializer.Serialize($"Abstract method {symbol.Name} must be overridden")});");
        body.AppendLine("  }");
    }

    private static string ConversionMethodName(IMethodSymbol method)
    {
        var overloads = method.ContainingType.GetMembers().OfType<IMethodSymbol>()
            .Where(candidate => candidate.MethodKind == MethodKind.Conversion
                && candidate.Name == method.Name)
            .OrderBy(candidate => candidate.Parameters[0].Type.ToDisplayString()
                + "->" + candidate.ReturnType.ToDisplayString(), StringComparer.Ordinal)
            .ToList();
        int index = overloads.FindIndex(candidate =>
            SymbolEqualityComparer.Default.Equals(candidate, method.OriginalDefinition));
        if (index < 0) throw new InvalidOperationException($"Conversion operator could not be resolved: {method}");
        return overloads.Count == 1 ? method.Name : $"{method.Name}__{index}";
    }

    private void EmitConversionOperator(ConversionOperatorDeclarationSyntax conversion)
    {
        if (model.GetDeclaredSymbol(conversion) is not IMethodSymbol symbol
            || conversion.ParameterList.Parameters.Count != 1)
            throw Unsupported(conversion, "Conversion operator needs one resolved parameter");
        body.AppendLine($"  static {ConversionMethodName(symbol)}("
            + $"{ParameterDeclaration(conversion.ParameterList.Parameters[0])}) {{");
        if (conversion.Body != null)
            StatementSequence(conversion.Body.Statements, 2);
        else if (conversion.ExpressionBody != null)
        {
            SourceMarker(conversion.ExpressionBody.Expression, 2);
            body.AppendLine($"    return {ConvertImplicitValue(conversion.ExpressionBody.Expression,
                CopyStruct(conversion.ExpressionBody.Expression, Expression(conversion.ExpressionBody.Expression)),
                symbol.ReturnType)};");
        }
        else throw Unsupported(conversion, "Conversion operator body is missing");
        body.AppendLine("  }");
    }

    private void EmitPatternVariables(SyntaxNode block, int depth)
    {
        string pad = new(' ', depth * 2);
        var names = block.DescendantNodes().OfType<SingleVariableDesignationSyntax>()
            .Select(item => item.Identifier.ValueText).Distinct(StringComparer.Ordinal);
        foreach (var name in names) body.AppendLine($"{pad}let {name};");
    }

    private string MethodName(IMethodSymbol method)
    {
        method = method.ReducedFrom ?? method;
        if (method.IsOverride && method.OverriddenMethod != null)
            return MethodName(method.OverriddenMethod);
        var containingType = method.ContainingType.OriginalDefinition;
        if (!emittedTypes.Contains(containingType.ToDisplayString())) return method.Name;
        var overloads = containingType.GetMembers(method.Name).OfType<IMethodSymbol>()
            .Where(candidate => candidate.MethodKind == MethodKind.Ordinary)
            // Interface and implementation declarations can list the same overloads
            // in different orders. Their emitted names must still agree.
            .OrderBy(candidate => string.Join(",", candidate.Parameters.Select(parameter =>
                parameter.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))), StringComparer.Ordinal)
            .ToList();
        if (overloads.Count <= 1) return method.Name;
        int index = overloads.FindIndex(candidate => SymbolEqualityComparer.Default.Equals(candidate, method.OriginalDefinition));
        if (index < 0) throw new InvalidOperationException($"Method overload could not be resolved: {method}");
        return $"{method.Name}__{index}";
    }

    private void StatementSequence(IEnumerable<StatementSyntax> statements, int depth)
    {
        var sequence = statements.ToList();
        string pad = new(' ', depth * 2);
        // C# local functions are in scope throughout their block, including
        // calls that appear before the declaration. Bound JS expressions are not.
        foreach (var localFunction in sequence.OfType<LocalFunctionStatementSyntax>())
            TryEmit(() => Statement(localFunction, depth));
        for (int index = 0; index < sequence.Count; index++)
        {
            if (sequence[index] is LocalFunctionStatementSyntax) continue;
            if (sequence[index] is LocalDeclarationStatementSyntax local
                && !local.UsingKeyword.IsKind(SyntaxKind.None))
            {
                TryEmit(() => Statement(local, depth));
                body.AppendLine($"{pad}try {{");
                StatementSequence(sequence.Skip(index + 1), depth + 1);
                body.AppendLine($"{pad}}} finally {{");
                foreach (var variable in local.Declaration.Variables.Reverse())
                    body.AppendLine($"{pad}  {variable.Identifier.ValueText}?.Dispose?.();");
                body.AppendLine($"{pad}}}");
                return;
            }
            TryEmit(() => Statement(sequence[index], depth));
        }
    }

    private void Statement(StatementSyntax statement, int depth)
    {
        string pad = new(' ', depth * 2);
        if (statement is not BlockSyntax) SourceMarker(statement, depth);
        switch (statement)
        {
            case EmptyStatementSyntax:
                break;
            case BlockSyntax block:
                StatementSequence(block.Statements, depth);
                break;
            case LocalDeclarationStatementSyntax local:
                foreach (var variable in local.Declaration.Variables)
                {
                    body.AppendLine(variable.Initializer == null
                        ? $"{pad}let {JsLocalName(variable.Identifier.ValueText)};"
                        : $"{pad}let {JsLocalName(variable.Identifier.ValueText)} = "
                            + $"{ConvertImplicitValue(variable.Initializer.Value,
                                CopyStruct(variable.Initializer.Value, Expression(variable.Initializer.Value)),
                                (model.GetDeclaredSymbol(variable) as ILocalSymbol)?.Type)};");
                }
                break;
            case ExpressionStatementSyntax expression:
                body.AppendLine($"{pad}{Expression(expression.Expression)};");
                break;
            case LocalFunctionStatementSyntax localFunction:
                bool iterator = localFunction.Body?.DescendantNodes().OfType<YieldStatementSyntax>().Any() == true;
                body.AppendLine($"{pad}const {localFunction.Identifier.ValueText} = function{(iterator ? "*" : "")}("
                    + string.Join(", ", localFunction.ParameterList.Parameters.Select(ParameterDeclaration)) + ") {");
                if (localFunction.Body != null)
                {
                    EmitPatternVariables(localFunction.Body, depth + 1);
                    StatementSequence(localFunction.Body.Statements, depth + 1);
                }
                else if (localFunction.ExpressionBody != null)
                    body.AppendLine($"{pad}  return {Expression(localFunction.ExpressionBody.Expression)};");
                body.AppendLine($"{pad}}}.bind(this);");
                break;
            case YieldStatementSyntax yielded:
                body.AppendLine(yielded.IsKind(SyntaxKind.YieldBreakStatement)
                    ? $"{pad}return;" : $"{pad}yield {Expression(yielded.Expression!)};");
                break;
            case ThrowStatementSyntax thrown when thrown.Expression is ObjectCreationExpressionSyntax creation
                && model.GetTypeInfo(creation).Type?.ToDisplayString() == "System.NotImplementedException":
                body.AppendLine($"{pad}throw new Error(\"Not implemented\");");
                break;
            case ThrowStatementSyntax thrown:
                body.AppendLine(thrown.Expression == null ? $"{pad}throw __unityCaughtError;"
                    : $"{pad}throw {Expression(thrown.Expression)};");
                break;
            case TryStatementSyntax attempt:
                body.AppendLine($"{pad}try {{");
                Statement(attempt.Block, depth + 1);
                body.AppendLine($"{pad}}}");
                if (attempt.Catches.Count > 0)
                {
                    body.AppendLine($"{pad}catch (__unityCaughtError) {{");
                    foreach (var clause in attempt.Catches)
                    {
                        if (clause.Filter != null)
                            throw Unsupported(clause, "Catch filters need lowering");
                        string catchCondition = clause.Declaration == null ? "true"
                            : model.GetTypeInfo(clause.Declaration.Type).Type?.ToDisplayString()
                                is "UnityEngine.PlayerPrefsException" or "UnityEngine.Assertions.AssertionException"
                                    or "UnityEngine.UnityException" or "UnityEngine.MissingComponentException"
                                    or "UnityEngine.MissingReferenceException" or "UnityEngine.UnassignedReferenceException"
                                ? $"__unityCaughtError instanceof {TypeName(clause.Declaration.Type)}"
                            : IsExceptionType(model.GetTypeInfo(clause.Declaration.Type).Type!) ? "true"
                            : $"__unityCaughtError instanceof {TypeName(clause.Declaration.Type)}";
                        body.AppendLine($"{pad}  if ({catchCondition}) {{");
                        if (clause.Declaration?.Identifier.ValueText is { Length: > 0 } name)
                            body.AppendLine($"{pad}    const {name} = __unityCaughtError;");
                        Statement(clause.Block, depth + 2);
                        body.AppendLine($"{pad}  }} else ");
                    }
                    body.AppendLine($"{pad}  {{ throw __unityCaughtError; }}");
                    body.AppendLine($"{pad}}}");
                }
                if (attempt.Finally != null)
                {
                    body.AppendLine($"{pad}finally {{");
                    Statement(attempt.Finally.Block, depth + 1);
                    body.AppendLine($"{pad}}}");
                }
                break;
            case UsingStatementSyntax usingStatement:
                body.AppendLine($"{pad}{{");
                var usingNames = new List<string>();
                if (usingStatement.Declaration != null)
                {
                    foreach (var variable in usingStatement.Declaration.Variables)
                    {
                        body.AppendLine($"{pad}  const {variable.Identifier.ValueText} = {Expression(variable.Initializer!.Value)};");
                        usingNames.Add(variable.Identifier.ValueText);
                    }
                }
                else if (usingStatement.Expression != null)
                {
                    string name = $"__unityUsing{usingStatement.SpanStart}";
                    body.AppendLine($"{pad}  const {name} = {Expression(usingStatement.Expression)};");
                    usingNames.Add(name);
                }
                body.AppendLine($"{pad}  try {{");
                Statement(usingStatement.Statement, depth + 2);
                body.AppendLine($"{pad}  }} finally {{");
                foreach (var name in usingNames.AsEnumerable().Reverse())
                    body.AppendLine($"{pad}    {name}?.Dispose?.();");
                body.AppendLine($"{pad}  }}");
                body.AppendLine($"{pad}}}");
                break;
            case IfStatementSyntax conditional:
                body.AppendLine($"{pad}if ({BooleanCondition(conditional.Condition)}) {{");
                Statement(conditional.Statement, depth + 1);
                body.AppendLine($"{pad}}}");
                if (conditional.Else != null)
                {
                    body.AppendLine($"{pad}else {{");
                    Statement(conditional.Else.Statement, depth + 1);
                    body.AppendLine($"{pad}}}");
                }
                break;
            case ForEachStatementSyntax loop:
                string item = $"__unityForeachItem{foreachCounter++}";
                bool dictionaryLoop = IsDictionary(model.GetTypeInfo(loop.Expression).Type);
                string collection = Expression(loop.Expression);
                body.AppendLine($"{pad}for (const {item} of {collection}{(dictionaryLoop ? ".entries()" : "")}) {{");
                string value = dictionaryLoop ? $"{{ Key: {item}[0], Value: {item}[1] }}"
                    : IsCopiedValueType(model.GetTypeInfo(loop.Type).Type) ? $"{item}.clone()" : item;
                body.AppendLine($"{pad}  const {JsLocalName(loop.Identifier.ValueText)} = {value};");
                Statement(loop.Statement, depth + 1);
                body.AppendLine($"{pad}}}");
                break;
            case ForStatementSyntax loop:
                string initializer = loop.Declaration == null
                    ? string.Join(", ", loop.Initializers.Select(Expression))
                    : "let " + string.Join(", ", loop.Declaration.Variables.Select(variable =>
                        JsLocalName(variable.Identifier.ValueText) + (variable.Initializer == null ? ""
                            : " = " + CopyStruct(variable.Initializer.Value, Expression(variable.Initializer.Value)))));
                string condition = loop.Condition == null ? "" : BooleanCondition(loop.Condition);
                string increment = string.Join(", ", loop.Incrementors.Select(Expression));
                body.AppendLine($"{pad}for ({initializer}; {condition}; {increment}) {{");
                Statement(loop.Statement, depth + 1);
                body.AppendLine($"{pad}}}");
                break;
            case WhileStatementSyntax loop:
                body.AppendLine($"{pad}while ({BooleanCondition(loop.Condition)}) {{");
                Statement(loop.Statement, depth + 1);
                body.AppendLine($"{pad}}}");
                break;
            case DoStatementSyntax loop:
                body.AppendLine($"{pad}do {{");
                Statement(loop.Statement, depth + 1);
                body.AppendLine($"{pad}}} while ({BooleanCondition(loop.Condition)});");
                break;
            case SwitchStatementSyntax selection:
                body.AppendLine($"{pad}switch ({Expression(selection.Expression)}) {{");
                foreach (var section in selection.Sections)
                {
                    foreach (var label in section.Labels)
                    {
                        if (label is CaseSwitchLabelSyntax option)
                            body.AppendLine($"{pad}  case {Expression(option.Value)}:");
                        else if (label is DefaultSwitchLabelSyntax)
                            body.AppendLine($"{pad}  default:");
                        else throw Unsupported(label, "Switch pattern labels need lowering");
                    }
                    body.AppendLine($"{pad}  {{");
                    foreach (var child in section.Statements)
                        TryEmit(() => Statement(child, depth + 2));
                    body.AppendLine($"{pad}  }}");
                }
                body.AppendLine($"{pad}}}");
                break;
            case BreakStatementSyntax:
                body.AppendLine($"{pad}break;");
                break;
            case ContinueStatementSyntax:
                body.AppendLine($"{pad}continue;");
                break;
            case ReturnStatementSyntax returned:
                var returnedValue = returned.Expression == null ? null
                    : CopyStruct(returned.Expression, Expression(returned.Expression));
                if (returned.Expression != null && model.GetEnclosingSymbol(returned.SpanStart) is IMethodSymbol method)
                    returnedValue = ConvertImplicitValue(returned.Expression, returnedValue!, method.ReturnType);
                body.AppendLine(returned.Expression == null ? $"{pad}return;"
                    : $"{pad}return {returnedValue};");
                break;
            default: throw Unsupported(statement, $"Unsupported statement {statement.Kind()}");
        }
    }

    private string Expression(ExpressionSyntax expression) => expression switch
    {
        LiteralExpressionSyntax literal when literal.IsKind(SyntaxKind.DefaultLiteralExpression)
            => DefaultValue(model.GetTypeInfo(literal).Type),
        LiteralExpressionSyntax literal => Literal(literal),
        AwaitExpressionSyntax awaited => AwaitExpression(awaited),
        IdentifierNameSyntax identifier => Identifier(identifier),
        GenericNameSyntax genericName => TypeName(genericName),
        ThisExpressionSyntax => "this",
        BaseExpressionSyntax => "super",
        MemberAccessExpressionSyntax member => MemberAccess(member),
        PredefinedTypeSyntax predefined => PredefinedType(predefined),
        ElementAccessExpressionSyntax element => ElementAccess(element),
        ArrayCreationExpressionSyntax array => ArrayCreation(array),
        StackAllocArrayCreationExpressionSyntax stack => StackAllocArray(stack),
        ImplicitArrayCreationExpressionSyntax array => TypeArrayInitializer(
            array.Initializer.Expressions, model.GetTypeInfo(array).Type)
            ?? ObjectArrayInitializer(array.Initializer.Expressions, model.GetTypeInfo(array).Type)
            ?? "[" + string.Join(", ",
            array.Initializer.Expressions.Select(item => CopyStruct(item, Expression(item)))) + "]",
        InitializerExpressionSyntax initializer when initializer.IsKind(SyntaxKind.ArrayInitializerExpression)
            => TypeArrayInitializer(initializer.Expressions, ArrayInitializerType(initializer))
                ?? ObjectArrayInitializer(initializer.Expressions, ArrayInitializerType(initializer))
                ?? "[" + string.Join(", ", initializer.Expressions.Select(item =>
                    CopyStruct(item, Expression(item)))) + "]",
        DefaultExpressionSyntax defaultExpression => DefaultValue(model.GetTypeInfo(defaultExpression.Type).Type),
        CastExpressionSyntax cast => Cast(cast),
        RefExpressionSyntax reference => ReadOnlyRefLocal(reference),
        ConditionalAccessExpressionSyntax access => ConditionalAccess(access),
        InterpolatedStringExpressionSyntax interpolated => InterpolatedString(interpolated),
        InvocationExpressionSyntax invocation => Invocation(invocation),
        ObjectCreationExpressionSyntax creation => Creation(creation),
        ImplicitObjectCreationExpressionSyntax creation => ImplicitCreation(creation),
        BinaryExpressionSyntax binary => Binary(binary),
        SwitchExpressionSyntax selection => SwitchExpression(selection),
        IsPatternExpressionSyntax pattern => IsPattern(pattern),
        TypeOfExpressionSyntax typeOf => TypeOf(typeOf),
        AssignmentExpressionSyntax assignment => Assignment(assignment),
        TupleExpressionSyntax tuple => "{ " + string.Join(", ", tuple.Arguments.Select((argument, index) =>
            $"Item{index + 1}: {Expression(argument.Expression)}")) + " }",
        PrefixUnaryExpressionSyntax unary => PrefixUnary(unary),
        PostfixUnaryExpressionSyntax unary => $"({Expression(unary.Operand)}{unary.OperatorToken.Text})",
        ParenthesizedExpressionSyntax parenthesized => $"({Expression(parenthesized.Expression)})",
        ConditionalExpressionSyntax conditional => ConditionalExpression(conditional),
        ParenthesizedLambdaExpressionSyntax lambda => Lambda(
            lambda.ParameterList.Parameters.Select(parameter => JsLocalName(parameter.Identifier.ValueText)), lambda.Body),
        SimpleLambdaExpressionSyntax lambda => Lambda([JsLocalName(lambda.Parameter.Identifier.ValueText)], lambda.Body),
        AnonymousMethodExpressionSyntax anonymous => Lambda(
            anonymous.ParameterList?.Parameters.Select(parameter => JsLocalName(parameter.Identifier.ValueText)) ?? [],
            anonymous.Block),
        _ => throw Unsupported(expression, $"Unsupported expression {expression.Kind()}"),
    };

    private string ConditionalExpression(ConditionalExpressionSyntax conditional)
    {
        var target = model.GetTypeInfo(conditional).Type;
        return $"({BooleanCondition(conditional.Condition)} ? "
            + $"{ConvertImplicitValue(conditional.WhenTrue, Expression(conditional.WhenTrue), target)} : "
            + $"{ConvertImplicitValue(conditional.WhenFalse, Expression(conditional.WhenFalse), target)})";
    }

    private string SwitchExpression(SwitchExpressionSyntax selection)
    {
        var arms = new List<string>();
        var resultType = model.GetTypeInfo(selection).Type;
        foreach (var arm in selection.Arms)
        {
            if (arm.WhenClause != null)
                throw Unsupported(arm, "Switch expression guard needs lowering");
            string result = ConvertImplicitValue(arm.Expression,
                CopyStruct(arm.Expression, Expression(arm.Expression)), resultType);
            switch (arm.Pattern)
            {
                case ConstantPatternSyntax constant:
                    arms.Add($"if (__unitySwitchValue === {Expression(constant.Expression)}) return {result};");
                    break;
                case DiscardPatternSyntax:
                    arms.Add($"return {result};");
                    break;
                default:
                    throw Unsupported(arm.Pattern,
                        $"Switch expression pattern {arm.Pattern.Kind()} needs lowering");
            }
        }
        arms.Add("throw new Error(\"Non-exhaustive switch expression\");");
        return $"((__unitySwitchValue) => {{ {string.Join(" ", arms)} }})"
            + $"({Expression(selection.GoverningExpression)})";
    }

    private string AwaitExpression(AwaitExpressionSyntax awaited)
    {
        var type = model.GetTypeInfo(awaited.Expression).Type as INamedTypeSymbol;
        if (type?.Name == "UnityEvent"
            && type.ContainingNamespace.ToDisplayString() == "UnityEngine.Events")
            return $"await {Expression(awaited.Expression)}.ToPromise({type.TypeArguments.Length})";
        return $"await {Expression(awaited.Expression)}";
    }

    private string ReadOnlyRefLocal(RefExpressionSyntax reference)
    {
        if (reference.Parent is not EqualsValueClauseSyntax
            { Parent: VariableDeclaratorSyntax variable }
            || reference.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault() is not { } method)
            throw Unsupported(reference, "ref expression needs alias lowering");
        string name = variable.Identifier.ValueText;
        bool written = method.DescendantNodes().OfType<IdentifierNameSyntax>()
            .Where(identifier => identifier.Identifier.ValueText == name)
            .Any(identifier => identifier.Ancestors().Any(ancestor =>
                ancestor is AssignmentExpressionSyntax assignment
                    && assignment.Left.Span.Contains(identifier.Span)
                || ancestor is ArgumentSyntax argument
                    && argument.RefKindKeyword.IsKind(SyntaxKind.RefKeyword)
                || ancestor is PrefixUnaryExpressionSyntax prefix
                    && prefix.IsKind(SyntaxKind.PreIncrementExpression)
                        || ancestor is PostfixUnaryExpressionSyntax postfix
                            && postfix.IsKind(SyntaxKind.PostIncrementExpression)));
        if (written) throw Unsupported(reference, "Writable ref local needs alias lowering");
        return Expression(reference.Expression);
    }

    private string BooleanCondition(ExpressionSyntax condition)
    {
        if (IsUnityObject(model.GetTypeInfo(condition).Type))
        {
            imports.Add("Object");
            return $"Object.op_Implicit({Expression(condition)})";
        }
        return ConvertImplicitValue(condition, Expression(condition), model.GetTypeInfo(condition).ConvertedType);
    }

    private string PrefixUnary(PrefixUnaryExpressionSyntax unary)
    {
        var type = model.GetTypeInfo(unary.Operand).Type?.ToDisplayString();
        if (unary.IsKind(SyntaxKind.LogicalNotExpression)
            && IsUnityObject(model.GetTypeInfo(unary.Operand).Type))
            return $"(!{BooleanCondition(unary.Operand)})";
        if (unary.IsKind(SyntaxKind.UnaryMinusExpression)
            && type is "UnityEngine.Vector2" or "UnityEngine.Vector3" or "UnityEngine.Vector4"
                or "UnityEngine.Vector2Int" or "UnityEngine.Vector3Int")
            return $"{Expression(unary.Operand)}.clone().negate()";
        return $"({unary.OperatorToken.Text}{Expression(unary.Operand)})";
    }

    private string Literal(LiteralExpressionSyntax literal)
    {
        if (literal.IsKind(SyntaxKind.NullLiteralExpression)) return "null";
        if (literal.IsKind(SyntaxKind.TrueLiteralExpression)) return "true";
        if (literal.IsKind(SyntaxKind.FalseLiteralExpression)) return "false";
        if (literal.Token.Value is string text) return JsonSerializer.Serialize(text);
        if (literal.Token.Value is char character) return JsonSerializer.Serialize(character.ToString());
        if (literal.Token.Value is ulong unsigned64)
            return $"{unsigned64.ToString(CultureInfo.InvariantCulture)}n";
        if (literal.Token.Value is long signed
            && (signed > 9007199254740991L || signed < -9007199254740991L))
            return $"{signed.ToString(CultureInfo.InvariantCulture)}n";
        if (literal.Token.Value is ulong unsigned && unsigned > 9007199254740991UL)
            return $"{unsigned.ToString(CultureInfo.InvariantCulture)}n";
        return Convert.ToString(literal.Token.Value, CultureInfo.InvariantCulture)
            ?? throw Unsupported(literal, "Literal value could not be emitted");
    }

    private string Identifier(IdentifierNameSyntax identifier)
    {
        var symbol = model.GetSymbolInfo(identifier).Symbol;
        if (symbol is INamedTypeSymbol type)
        {
            if (type.ContainingNamespace.ToDisplayString() is "UnityEngine" or "UnityEngine.Events" or "UnityEngine.AI"
                or "UnityEngine.Serialization"
                or "UnityEngine.UI" or "UnityEngine.EventSystems" or "TMPro"
                || type.ToDisplayString() == "UnityEngine.UIElements.DropdownMenuSizeMode"
                || type.ToDisplayString() is "UnityEngine.InputSystem.InputAction"
                    or "UnityEngine.InputSystem.InputActionMap"
                    or "UnityEngine.InputSystem.InputActionAsset"
                    or "UnityEngine.InputSystem.InputActionReference"
                    or "UnityEngine.InputSystem.InputBinding"
                    or "UnityEngine.InputSystem.InputActionType" or "UnityEngine.InputSystem.InputActionPhase"
                    or "UnityEngine.InputSystem.InputActionChange"
                    or "UnityEngine.InputSystem.InputDeviceChange")
                imports.Add(type.Name);
            return RuntimeTypeName(type, identifier);
        }
        if (symbol is IFieldSymbol field)
            return field.IsStatic ? $"{EmittedName(field.ContainingType)}.{field.Name}" : $"this.{field.Name}";
        if (symbol is IEventSymbol eventSymbol)
            return eventSymbol.IsStatic ? $"{EmittedName(eventSymbol.ContainingType)}.{eventSymbol.Name}"
                : $"this.{eventSymbol.Name}";
        if (symbol is IPropertySymbol property)
            return property.IsStatic ? $"{EmittedName(property.ContainingType)}.{property.Name}"
                : $"this.{property.Name}";
        if (symbol is IMethodSymbol method)
        {
            if (method.ContainingType.ToDisplayString() == "UnityEditor.EditorWindow") imports.Add("EditorWindow");
            return method.MethodKind == MethodKind.LocalFunction ? method.Name
                : method.IsStatic ? $"{RuntimeTypeName(method.ContainingType, identifier)}.{MethodName(method)}"
                : $"this.{MethodName(method)}";
        }
        if (symbol is IParameterSymbol parameter && parameter.RefKind is RefKind.Ref or RefKind.Out)
            return JsLocalName(identifier.Identifier.ValueText) + ".value";
        if (symbol is ILocalSymbol or IParameterSymbol) return JsLocalName(identifier.Identifier.ValueText);
        throw Unsupported(identifier, $"Unresolved or unsupported identifier {identifier.Identifier.ValueText}");
    }

    private string PredefinedType(PredefinedTypeSyntax syntax) => model.GetTypeInfo(syntax).Type?.SpecialType switch
    {
        SpecialType.System_String => ImportedCSharpString(),
        SpecialType.System_Boolean => "Boolean",
        SpecialType.System_Int32 or SpecialType.System_Single or SpecialType.System_Double => "Number",
        SpecialType.System_Object => "Object",
        _ => throw Unsupported(syntax, $"Predefined type {syntax.Keyword.ValueText} needs lowering"),
    };

    private string ImportedCSharpString() { imports.Add("CSharpString"); return "CSharpString"; }

    private string MemberAccess(MemberAccessExpressionSyntax member)
    {
        var symbol = model.GetSymbolInfo(member).Symbol;
        if (symbol is IPropertySymbol { Name: "color" } materialColor
            && materialColor.ContainingType.ToDisplayString() == "UnityEngine.Material")
            return $"{Expression(member.Expression)}.GetColor(\"_Color\")";
        if (member.Expression is TypeOfExpressionSyntax directType
            && member.Name.Identifier.ValueText is "Name" or "IsPrimitive")
        {
            imports.Add("UnityType");
            return $"UnityType.{member.Name.Identifier.ValueText}({NamedTypeExpression(directType)})";
        }
        if (symbol is IPropertySymbol { Name: "Length" }
            && model.GetTypeInfo(member.Expression).Type is IArrayTypeSymbol
                or { SpecialType: SpecialType.System_String })
            return $"{Expression(member.Expression)}.length";
        if (symbol is IFieldSymbol { IsConst: true } systemField
            && systemField.ContainingType.ContainingNamespace.ToDisplayString() == "System")
            return ConstantValue(systemField.ConstantValue, member);
        if (symbol is IPropertySymbol { Name: "Name" } property
            && property.ContainingType.ToDisplayString() == "System.Type")
        {
            imports.Add("UnityType");
            return $"UnityType.Name({Expression(member.Expression)})";
        }
        if (symbol is IPropertySymbol { Name: "IsPrimitive" } primitive
            && primitive.ContainingType.ToDisplayString() == "System.Type")
        {
            imports.Add("UnityType");
            return $"UnityType.IsPrimitive({Expression(member.Expression)})";
        }
        if (symbol is IFieldSymbol field && field.ContainingType.TypeKind == TypeKind.Enum
            && emittedTypes.Contains(field.ContainingType.ToDisplayString()))
            return $"{EmittedName(field.ContainingType)}.{field.Name}";
        if (symbol is IFieldSymbol externalEnum && externalEnum.ContainingType.TypeKind == TypeKind.Enum
            && externalEnum.ConstantValue != null)
        {
            if (externalEnum.ContainingType.ToDisplayString() == "UnityEngine.UIElements.StyleKeyword")
                return JsonSerializer.Serialize(externalEnum.Name);
            return ConstantValue(externalEnum.ConstantValue, member);
        }
        if (symbol is INamedTypeSymbol type)
            return RuntimeTypeName(type, member);
        if (symbol is IMethodSymbol method
            && emittedTypes.Contains(method.ContainingType.OriginalDefinition.ToDisplayString()))
            return $"{Expression(member.Expression)}.{MethodName(method)}";
        if (symbol is IMethodSymbol { IsStatic: true } externalMethod
            && (editorMode || allowExternalTypes)
            && !emittedTypes.Contains(externalMethod.ContainingType.ToDisplayString()))
            return $"{RuntimeTypeName(externalMethod.ContainingType, member)}.{MethodName(externalMethod)}";
        return $"{Expression(member.Expression)}.{member.Name.Identifier.ValueText}";
    }

    private string Lambda(IEnumerable<string> parameters, CSharpSyntaxNode lambdaBody)
    {
        string names = string.Join(", ", parameters);
        if (lambdaBody is ExpressionSyntax expression)
            return $"({names}) => {Expression(expression)}";
        if (lambdaBody is not BlockSyntax block)
            throw Unsupported(lambdaBody, "Lambda body needs lowering");
        var outer = body;
        body = new StringBuilder();
        try
        {
            foreach (var statement in block.Statements) Statement(statement, 1);
            return $"({names}) => {{\n{body}  }}";
        }
        finally { body = outer; }
    }

    private string TypeOf(TypeOfExpressionSyntax typeOf)
    {
        var type = model.GetTypeInfo(typeOf.Type).Type;
        return type?.SpecialType switch
        {
            SpecialType.System_Int32 or SpecialType.System_Single or SpecialType.System_Double => "Number",
            SpecialType.System_String or SpecialType.System_Char => "String",
            SpecialType.System_Boolean => "Boolean",
            _ => TypeName(typeOf.Type),
        };
    }

    private static bool IsList(ITypeSymbol? type) => type is INamedTypeSymbol named
        && named.OriginalDefinition.ToDisplayString() == "System.Collections.Generic.List<T>";

    private static bool IsDictionary(ITypeSymbol? type) => type is INamedTypeSymbol named
        && named.OriginalDefinition.ToDisplayString() == "System.Collections.Generic.Dictionary<TKey, TValue>";

    private static bool IsReadOnlyArray(ITypeSymbol? type) => type is INamedTypeSymbol named
        && named.Name == "ReadOnlyArray"
        && named.ContainingNamespace.ToDisplayString() == "UnityEngine.InputSystem.Utilities";

    private string ConvertImplicitValue(ExpressionSyntax source, string value, ITypeSymbol? target)
    {
        if (target?.TypeKind == TypeKind.Delegate
            && source is AnonymousMethodExpressionSyntax { Block.Statements.Count: 0 })
        {
            imports.Add("UnityEvent");
            return "new UnityEvent()";
        }
        var sourceType = model.GetTypeInfo(source).Type;
        var targetName = target?.ToDisplayString();
        var sourceName = sourceType?.ToDisplayString();
        if (sourceType is INamedTypeSymbol queryBuilder
            && queryBuilder.OriginalDefinition.ToDisplayString()
                == "UnityEngine.UIElements.UQueryBuilder<T>"
            && SymbolEqualityComparer.Default.Equals(queryBuilder.TypeArguments[0], target))
            return $"{value}.First()";
        if (targetName == "float" && sourceName == "UnityEngine.UIElements.Ratio")
            return $"{value}.value";
        if (targetName == "float" && sourceName == "UnityEngine.UIElements.StyleRatio")
            return $"{value}.value.value";
        if (targetName == "UnityEngine.UIElements.Ratio"
            && sourceName == "UnityEngine.UIElements.StyleRatio")
            return $"{value}.value";
        if (targetName == "UnityEngine.UIElements.StyleKeyword"
            && sourceName == "UnityEngine.UIElements.StyleRatio")
            return $"{value}.keyword";
        if (targetName != sourceType?.ToDisplayString()
            && (targetName is "UnityEngine.UIElements.Length"
                or "UnityEngine.UIElements.StyleLength"
                or "UnityEngine.UIElements.StyleFloat"
                or "UnityEngine.UIElements.StyleColor"
                or "UnityEngine.UIElements.StyleBackground"
                or "UnityEngine.UIElements.Ratio"
                or "UnityEngine.UIElements.StyleRatio"
                or "UnityEngine.UIElements.StyleBackgroundPosition"
                or "UnityEngine.UIElements.StyleBackgroundRepeat"
                or "UnityEngine.UIElements.StyleBackgroundSize"
                || targetName?.StartsWith("UnityEngine.UIElements.StyleEnum<",
                    StringComparison.Ordinal) == true))
        {
            var constructor = targetName!.StartsWith("UnityEngine.UIElements.StyleEnum<",
                StringComparison.Ordinal) ? "StyleEnum" : targetName.Split('.').Last();
            imports.Add(constructor);
            return $"new {constructor}({value})";
        }
        if (target?.TypeKind == TypeKind.Delegate
            && source is IdentifierNameSyntax or MemberAccessExpressionSyntax
            && model.GetSymbolInfo(source).Symbol is IMethodSymbol method)
        {
            if (method.MethodKind == MethodKind.LocalFunction)
                return Expression(source);
            string receiver = source is MemberAccessExpressionSyntax member
                ? Expression(member.Expression) : method.IsStatic ? method.ContainingType.Name : "this";
            imports.Add("UnityEvent");
            return $"UnityEvent.Bind({receiver}, {JsonSerializer.Serialize(MethodName(method))})";
        }
        if (target?.ToDisplayString() == "UnityEngine.PropertyName"
            && sourceType?.SpecialType is SpecialType.System_String or SpecialType.System_Int32)
        {
            imports.Add("PropertyName");
            return $"new PropertyName({value})";
        }
        if (target?.ToDisplayString() == "System.Type" && source is TypeOfExpressionSyntax typeOf)
            return NamedTypeExpression(typeOf);
        if (target?.SpecialType == SpecialType.System_Object
            && (sourceType?.SpecialType is SpecialType.System_Boolean or SpecialType.System_Byte
                or SpecialType.System_SByte or SpecialType.System_Int16 or SpecialType.System_UInt16
                or SpecialType.System_Int32 or SpecialType.System_UInt32 or SpecialType.System_Int64
                or SpecialType.System_UInt64 or SpecialType.System_Char or SpecialType.System_Decimal
                or SpecialType.System_Single or SpecialType.System_Double
                || sourceType?.TypeKind == TypeKind.Enum))
        {
            imports.Add("UnityType");
            return $"UnityType.Box({value}, {JsonSerializer.Serialize(sourceType!.ToDisplayString())})";
        }
        if (target?.SpecialType == SpecialType.System_Boolean && IsUnityObject(sourceType))
        {
            imports.Add("Object");
            return $"Object.op_Implicit({value})";
        }
        if (target?.ToDisplayString() == "UnityEngine.Color32"
            && sourceType?.ToDisplayString() == "UnityEngine.Color")
        {
            imports.Add("Color32");
            return $"Color32.FromColor({value})";
        }
        if (target?.ToDisplayString() == "UnityEngine.Color"
            && sourceType?.ToDisplayString() == "UnityEngine.Color32")
            return $"({value}).toColor()";
        if (target?.ToDisplayString() == "UnityEngine.Color"
            && sourceType?.ToDisplayString() == "UnityEngine.Vector4")
        {
            imports.Add("Color");
            return $"Color.FromVector4({value})";
        }
        if (target?.ToDisplayString() == "UnityEngine.Vector4"
            && sourceType?.ToDisplayString() == "UnityEngine.Color")
            return $"({value}).toVector4()";
        if (target?.ToDisplayString() == "UnityEngine.Vector2"
            && sourceType?.ToDisplayString() == "UnityEngine.Vector2Int")
            return $"({value}).toVector2()";
        if (target?.ToDisplayString() == "UnityEngine.Vector2"
            && sourceType?.ToDisplayString() == "UnityEngine.Vector3")
            return $"({value}).toVector2()";
        if (target?.ToDisplayString() == "UnityEngine.Vector3"
            && sourceType?.ToDisplayString() == "UnityEngine.Vector2")
            return $"({value}).toVector3()";
        if (target?.ToDisplayString() == "UnityEngine.Vector2"
            && sourceType?.ToDisplayString() == "UnityEngine.Vector4")
            return $"({value}).toVector2()";
        if (target?.ToDisplayString() == "UnityEngine.Vector3"
            && sourceType?.ToDisplayString() == "UnityEngine.Vector4")
            return $"({value}).toVector3()";
        if (target?.ToDisplayString() == "UnityEngine.Vector4"
            && sourceType?.ToDisplayString() is "UnityEngine.Vector2" or "UnityEngine.Vector3")
            return $"({value}).toVector4()";
        if (target?.ToDisplayString() == "UnityEngine.Vector3"
            && sourceType?.ToDisplayString() == "UnityEngine.Vector3Int")
            return $"({value}).toVector3()";
        if (target?.ToDisplayString() == "UnityEngine.LayerMask"
            && sourceType?.SpecialType == SpecialType.System_Int32)
        {
            imports.Add("LayerMask");
            return $"new LayerMask({value})";
        }
        if (target?.SpecialType == SpecialType.System_Int32
            && sourceType?.ToDisplayString() == "UnityEngine.LayerMask")
            return $"({value}).value";
        if (target?.ToDisplayString() == "UnityEngine.RenderingLayerMask"
            && sourceType?.SpecialType is SpecialType.System_Int32 or SpecialType.System_UInt32)
        {
            imports.Add("RenderingLayerMask");
            return $"new RenderingLayerMask({value})";
        }
        if (sourceType?.ToDisplayString() == "UnityEngine.RenderingLayerMask"
            && target?.SpecialType == SpecialType.System_Int32)
            return $"(({value}).value | 0)";
        if (sourceType?.ToDisplayString() == "UnityEngine.RenderingLayerMask"
            && target?.SpecialType == SpecialType.System_UInt32)
            return $"(({value}).value >>> 0)";
        if (target?.ToDisplayString() is "UnityEngine.EntityId" or "UnityEngine.InstanceID"
            && (sourceType?.SpecialType == SpecialType.System_Int32
                || sourceType?.ToDisplayString() is "UnityEngine.EntityId" or "UnityEngine.InstanceID"
                    && !SymbolEqualityComparer.Default.Equals(target, sourceType)))
        {
            imports.Add(target.Name);
            return $"new {target.Name}({value})";
        }
        if (target?.SpecialType == SpecialType.System_Int32
            && sourceType?.ToDisplayString() is "UnityEngine.EntityId" or "UnityEngine.InstanceID")
            return $"({value}).value";
        if (target?.ToDisplayString() == "UnityEngine.SceneManagement.SceneHandle"
            && sourceType?.SpecialType is SpecialType.System_Int32 or SpecialType.System_UInt32)
        {
            imports.Add("SceneHandle");
            return $"new SceneHandle({value})";
        }
        if (target?.SpecialType is SpecialType.System_Int32 or SpecialType.System_UInt32
            && sourceType?.ToDisplayString() == "UnityEngine.SceneManagement.SceneHandle")
            return target.SpecialType == SpecialType.System_UInt32
                ? $"(({value}).value >>> 0)" : $"({value}).value";
        if (IsReadOnlyArray(target) && model.GetTypeInfo(source).Type is IArrayTypeSymbol)
        {
            imports.Add("ReadOnlyArray");
            return $"new ReadOnlyArray({value})";
        }
        return ApplyUserDefinedConversion(source, value, target);
    }

    private string ApplyUserDefinedConversion(ExpressionSyntax source, string value, ITypeSymbol? target)
    {
        var conversion = target == null ? default : model.ClassifyConversion(source, target);
        if (conversion.IsUserDefined && conversion.MethodSymbol is IMethodSymbol method
            && SymbolEqualityComparer.Default.Equals(method.ContainingAssembly, model.Compilation.Assembly)
            && emittedTypes.Contains(method.ContainingType.ToDisplayString()))
            return $"{EmittedName(method.ContainingType)}.{ConversionMethodName(method)}({value})";
        return value;
    }

    private string ElementAccess(ElementAccessExpressionSyntax element)
    {
        var type = model.GetTypeInfo(element.Expression).Type;
        if (element.ArgumentList.Arguments.Count == 1
            && element.ArgumentList.Arguments[0].Expression is PrefixUnaryExpressionSyntax fromEnd
            && fromEnd.IsKind(SyntaxKind.IndexExpression))
        {
            if (IsList(type))
                return $"((__unityIndexed) => __unityIndexed.GetItem(__unityIndexed.Count - {Expression(fromEnd.Operand)}))"
                    + $"({Expression(element.Expression)})";
            if (type is not IArrayTypeSymbol && type?.SpecialType != SpecialType.System_String)
                throw Unsupported(element, "From-end indexing needs an array, string, or List<T>");
            return $"((__unityIndexed) => __unityIndexed[__unityIndexed.length - {Expression(fromEnd.Operand)}])"
                + $"({Expression(element.Expression)})";
        }
        if ((type?.SpecialType == SpecialType.System_String || type is IArrayTypeSymbol)
            && element.ArgumentList.Arguments.Count == 1
            && element.ArgumentList.Arguments[0].Expression is RangeExpressionSyntax range)
        {
            string Bound(ExpressionSyntax? bound) => bound is PrefixUnaryExpressionSyntax fromEnd
                && fromEnd.IsKind(SyntaxKind.IndexExpression)
                ? $"(__unityRangeTarget.length - {Expression(fromEnd.Operand)})"
                : bound == null ? "0" : Expression(bound);
            string start = Bound(range.LeftOperand);
            string end = range.RightOperand == null ? "" : ", " + Bound(range.RightOperand);
            return $"((__unityRangeTarget) => __unityRangeTarget.slice({start}{end}))"
                + $"({Expression(element.Expression)})";
        }
        if (type?.ToDisplayString() == "UnityEngine.AnimationCurve")
        {
            if (element.ArgumentList.Arguments.Count != 1)
                throw Unsupported(element, "AnimationCurve index needs one integer");
            return $"{Expression(element.Expression)}.GetItem({Expression(element.ArgumentList.Arguments[0].Expression)})";
        }
        if (type?.ToDisplayString() == "UnityEngine.Matrix4x4")
        {
            if (element.ArgumentList.Arguments.Count is not (1 or 2))
                throw Unsupported(element, "Matrix4x4 index needs one or two integers");
            return $"{Expression(element.Expression)}.GetItem("
                + string.Join(", ", element.ArgumentList.Arguments.Select(argument => Expression(argument.Expression))) + ")";
        }
        if (type?.ToDisplayString() is "UnityEngine.Vector2" or "UnityEngine.Vector2Int" or "UnityEngine.Vector3" or "UnityEngine.Vector3Int"
            or "UnityEngine.Vector4" or "UnityEngine.Quaternion" or "UnityEngine.Color" or "UnityEngine.Color32")
        {
            if (element.ArgumentList.Arguments.Count != 1)
                throw Unsupported(element, "Vector or quaternion index needs one integer");
            return $"{Expression(element.Expression)}.GetItem({Expression(element.ArgumentList.Arguments[0].Expression)})";
        }
        if (element.ArgumentList.Arguments.Count == 2
            && type is IArrayTypeSymbol { Rank: 2 })
            return $"{Expression(element.Expression)}[{Expression(element.ArgumentList.Arguments[0].Expression)}]"
                + $"[{Expression(element.ArgumentList.Arguments[1].Expression)}]";
        if (element.ArgumentList.Arguments.Count != 1)
            throw Unsupported(element, "Only single-index element access is supported");
        string target = Expression(element.Expression);
        var indexExpression = element.ArgumentList.Arguments[0].Expression;
        var indexParameter = (model.GetSymbolInfo(element).Symbol as IPropertySymbol)?
            .Parameters.FirstOrDefault()?.Type
            ?? (type is IArrayTypeSymbol
                ? model.Compilation.GetSpecialType(SpecialType.System_Int32) : null);
        string index = ConvertImplicitValue(indexExpression, Expression(indexExpression), indexParameter);
        if (type is INamedTypeSymbol span
            && span.OriginalDefinition.ToDisplayString() is "System.Span<T>" or "System.ReadOnlySpan<T>")
            return $"{target}[{index}]";
        if (type is INamedTypeSymbol listInterface
            && listInterface.OriginalDefinition.ToDisplayString() == "System.Collections.Generic.IList<T>")
            return $"{target}[{index}]";
        if (model.GetSymbolInfo(element).Symbol is IPropertySymbol { IsIndexer: true } customIndexer
            && SymbolEqualityComparer.Default.Equals(customIndexer.ContainingAssembly, model.Compilation.Assembly))
            return $"{target}.{IndexerMethodName(customIndexer, "Get")}({index})";
        if (type?.ToDisplayString() == "UnityEngine.UIElements.ToggleButtonGroupState")
            return $"{target}.get_Item({index})";
        if (type?.ToDisplayString() == "UnityEngine.Animation")
            return $"{target}.GetItem({index})";
        if (type?.ToDisplayString() == "UnityEngine.Animation")
            return $"{target}.GetItem({index})";
        if (type?.ToDisplayString() is "UnityEngine.UIElements.Columns"
            or "UnityEngine.UIElements.SortColumnDescriptions")
            return $"{target}.GetItem({index})";
        if (type?.ToDisplayString() == "UnityEngine.UIElements.VisualElement.Hierarchy")
            return $"{target}.GetItem({index})";
        if (type?.ToDisplayString() == "UnityEngine.UIElements.VisualElement"
            || model.GetSymbolInfo(element).Symbol is IPropertySymbol indexer
                && indexer.ContainingType.ToDisplayString()
                    == "UnityEngine.UIElements.VisualElement")
            return $"{target}.GetItem({index})";
        if (type?.ToDisplayString() is "Newtonsoft.Json.Linq.JObject" or "Newtonsoft.Json.Linq.JArray")
            return $"{target}.GetItem({index})";
        if (IsList(type) || IsDictionary(type) || IsReadOnlyArray(type))
            return $"{target}.GetItem({index})";
        if (type is IArrayTypeSymbol || type?.SpecialType == SpecialType.System_String
            || type?.ToDisplayString() == "UnityEngine.UIElements.VisualElementStyleSheetSet"
            || type is INamedTypeSymbol nativeArray
                && nativeArray.OriginalDefinition.ToDisplayString() is
                    "Unity.Collections.NativeArray<T>" or "Unity.Collections.NativeSlice<T>")
            return $"{target}[{index}]";
        throw Unsupported(element, $"Indexer on {type} has no JavaScript lowering");
    }

    private string StackAllocArray(StackAllocArrayCreationExpressionSyntax stack)
    {
        if (stack.Type is not ArrayTypeSyntax arrayType
            || arrayType.RankSpecifiers.Count != 1
            || arrayType.RankSpecifiers[0].Sizes.Count != 1
            || stack.Initializer != null)
            throw Unsupported(stack, "Stack allocation shape needs lowering");
        var sizeSyntax = arrayType.RankSpecifiers[0].Sizes[0];
        string size = ApplyUserDefinedConversion(sizeSyntax, Expression(sizeSyntax),
            model.Compilation.GetSpecialType(SpecialType.System_Int32));
        string initial = DefaultValue(model.GetTypeInfo(arrayType.ElementType).Type);
        return $"Array.from({{ length: {size} }}, () => {initial})";
    }

    private string ArrayCreation(ArrayCreationExpressionSyntax array)
    {
        if (array.Type.RankSpecifiers.Count == 1
            && array.Type.RankSpecifiers[0].Sizes.Count == 2 && array.Initializer == null)
        {
            var sizes = array.Type.RankSpecifiers[0].Sizes;
            var nestedElementType = (model.GetTypeInfo(array.Type).Type as IArrayTypeSymbol)?.ElementType;
            return $"Array.from({{ length: {Expression(sizes[0])} }}, () => "
                + $"Array.from({{ length: {Expression(sizes[1])} }}, () => {DefaultValue(nestedElementType)}))";
        }
        if (array.Type.RankSpecifiers.Count != 1 || array.Type.RankSpecifiers[0].Sizes.Count != 1)
            throw Unsupported(array, "Multidimensional arrays need lowering");
        if (array.Initializer != null)
            return TypeArrayInitializer(array.Initializer.Expressions, model.GetTypeInfo(array).Type)
                ?? ObjectArrayInitializer(array.Initializer.Expressions, model.GetTypeInfo(array).Type)
                ?? "[" + string.Join(", ", array.Initializer.Expressions.Select(item =>
                    CopyStruct(item, Expression(item)))) + "]";
        var size = array.Type.RankSpecifiers[0].Sizes[0];
        if (size is OmittedArraySizeExpressionSyntax)
            throw Unsupported(array, "Array size or initializer is required");
        var elementType = (model.GetTypeInfo(array.Type).Type as IArrayTypeSymbol)?.ElementType;
        string defaultValue = DefaultValue(elementType);
        string length = ApplyUserDefinedConversion(size, Expression(size),
            model.Compilation.GetSpecialType(SpecialType.System_Int32));
        return $"Array.from({{ length: {length} }}, () => {defaultValue})";
    }

    private string? TypeArrayInitializer(IEnumerable<ExpressionSyntax> expressions, ITypeSymbol? arrayType)
    {
        if (arrayType is not IArrayTypeSymbol array
            || array.ElementType.ToDisplayString() != "System.Type") return null;
        return "[" + string.Join(", ", expressions.Select(item =>
            item is TypeOfExpressionSyntax typeOf ? NamedTypeExpression(typeOf) : Expression(item))) + "]";
    }

    private string? ObjectArrayInitializer(IEnumerable<ExpressionSyntax> expressions, ITypeSymbol? arrayType)
    {
        if (arrayType is not IArrayTypeSymbol array
            || array.ElementType.SpecialType != SpecialType.System_Object) return null;
        return "[" + string.Join(", ", expressions.Select(item =>
            ConvertImplicitValue(item, CopyStruct(item, Expression(item)), array.ElementType))) + "]";
    }

    private string NamedTypeExpression(TypeOfExpressionSyntax typeOf)
    {
        var type = model.GetTypeInfo(typeOf.Type).Type
            ?? throw Unsupported(typeOf, "Reflection argument type is unresolved");
        if (type is ITypeParameterSymbol) return Expression(typeOf);
        var name = type.ToDisplayString();
        imports.Add("UnityType");
        return $"UnityType.Named({JsonSerializer.Serialize(name)}, {Expression(typeOf)})";
    }

    private string TypeValueExpression(ExpressionSyntax value) =>
        value is TypeOfExpressionSyntax typeOf ? NamedTypeExpression(typeOf) : Expression(value);

    private ITypeSymbol? ArrayInitializerType(InitializerExpressionSyntax initializer)
    {
        if (initializer.Parent is EqualsValueClauseSyntax { Parent: VariableDeclaratorSyntax variable })
            return model.GetDeclaredSymbol(variable) switch
            {
                ILocalSymbol local => local.Type,
                IFieldSymbol field => field.Type,
                _ => model.GetTypeInfo(initializer).Type,
            };
        return model.GetTypeInfo(initializer).Type;
    }

    private string Cast(CastExpressionSyntax cast)
    {
        var target = model.GetTypeInfo(cast.Type).Type;
        var source = model.GetTypeInfo(cast.Expression).Type;
        string value = Expression(cast.Expression);
        if (target?.SpecialType == SpecialType.System_Boolean && IsUnityObject(source))
        {
            imports.Add("Object");
            return $"Object.op_Implicit({value})";
        }
        if (target?.SpecialType == SpecialType.System_Byte
            && source?.SpecialType is SpecialType.System_Int32 or SpecialType.System_Single
                or SpecialType.System_Double)
            return $"(Math.trunc({value}) & 255)";
        if (target?.SpecialType == SpecialType.System_Byte && source?.TypeKind == TypeKind.Enum)
            return $"({value} & 255)";
        if (target?.SpecialType == SpecialType.System_Int32
            && source?.ToDisplayString() == "MalbersAnimations.Scriptables.FloatReference")
            return $"Math.trunc({ConvertImplicitValue(cast.Expression, value,
                model.Compilation.GetSpecialType(SpecialType.System_Single))})";
        if (target?.ToDisplayString() == "UnityEngine.Vector2"
            && source?.ToDisplayString() == "UnityEngine.Vector2Int")
            return ConvertImplicitValue(cast.Expression, value, target);
        if (target?.ToDisplayString() == "UnityEngine.Quaternion"
            && source?.ToDisplayString() == "Unity.Mathematics.quaternion")
        {
            imports.Add("Quaternion");
            return $"new Quaternion({value}.value.x, {value}.value.y, {value}.value.z, {value}.value.w)";
        }
        if (target?.ToDisplayString() == "UnityEngine.Vector2"
            && source?.ToDisplayString() == "UnityEngine.Vector3"
            || target?.ToDisplayString() == "UnityEngine.Vector3"
            && source?.ToDisplayString() == "UnityEngine.Vector2")
            return ConvertImplicitValue(cast.Expression, value, target);
        if (target?.ToDisplayString() == "UnityEngine.Vector2"
            && source?.ToDisplayString() == "UnityEngine.Vector4"
            || target?.ToDisplayString() == "UnityEngine.Vector3"
            && source?.ToDisplayString() == "UnityEngine.Vector4"
            || target?.ToDisplayString() == "UnityEngine.Vector4"
            && source?.ToDisplayString() is "UnityEngine.Vector2" or "UnityEngine.Vector3")
            return ConvertImplicitValue(cast.Expression, value, target);
        if (target?.ToDisplayString() == "UnityEngine.Vector3"
            && source?.ToDisplayString() == "UnityEngine.Vector3Int")
            return ConvertImplicitValue(cast.Expression, value, target);
        if (target?.ToDisplayString() == "UnityEngine.Vector3Int"
            && source?.ToDisplayString() == "UnityEngine.Vector2Int")
            return $"({value}).toVector3Int()";
        if (target?.ToDisplayString() == "UnityEngine.Vector2Int"
            && source?.ToDisplayString() == "UnityEngine.Vector3Int")
            return $"({value}).toVector2Int()";
        if (target?.ToDisplayString() == "UnityEngine.Color32"
            && source?.ToDisplayString() == "UnityEngine.Color"
            || target?.ToDisplayString() == "UnityEngine.Color"
            && source?.ToDisplayString() == "UnityEngine.Color32")
            return ConvertImplicitValue(cast.Expression, value, target);
        if (target?.ToDisplayString() == "UnityEngine.Color"
            && source?.ToDisplayString() == "UnityEngine.Vector4"
            || target?.ToDisplayString() == "UnityEngine.Vector4"
            && source?.ToDisplayString() == "UnityEngine.Color")
            return ConvertImplicitValue(cast.Expression, value, target);
        if (target?.ToDisplayString() == "UnityEngine.LayerMask"
            && source?.SpecialType == SpecialType.System_Int32)
            return ConvertImplicitValue(cast.Expression, value, target);
        if (target?.SpecialType == SpecialType.System_Int32
            && source?.ToDisplayString() == "UnityEngine.LayerMask")
            return ConvertImplicitValue(cast.Expression, value, target);
        if (target?.ToDisplayString() == "UnityEngine.RenderingLayerMask"
            && source?.SpecialType is SpecialType.System_Int32 or SpecialType.System_UInt32
            || source?.ToDisplayString() == "UnityEngine.RenderingLayerMask"
                && target?.SpecialType is SpecialType.System_Int32 or SpecialType.System_UInt32)
            return ConvertImplicitValue(cast.Expression, value, target);
        if (target?.ToDisplayString() is "UnityEngine.EntityId" or "UnityEngine.InstanceID"
            || target?.SpecialType == SpecialType.System_Int32
                && source?.ToDisplayString() is "UnityEngine.EntityId" or "UnityEngine.InstanceID")
            return ConvertImplicitValue(cast.Expression, value, target);
        if (target?.ToDisplayString() == "UnityEngine.SceneManagement.SceneHandle"
            && source?.SpecialType is SpecialType.System_Int32 or SpecialType.System_UInt32
            || target?.SpecialType is SpecialType.System_Int32 or SpecialType.System_UInt32
            && source?.ToDisplayString() == "UnityEngine.SceneManagement.SceneHandle")
            return ConvertImplicitValue(cast.Expression, value, target);
        if (target?.SpecialType == SpecialType.System_Int32
            && source?.SpecialType is SpecialType.System_Single or SpecialType.System_Double)
            return $"Math.trunc({value})";
        if (target?.SpecialType == SpecialType.System_UInt64
            && (source?.SpecialType is SpecialType.System_Int32 or SpecialType.System_UInt32
                or SpecialType.System_Int64 or SpecialType.System_UInt64
                || source?.TypeKind == TypeKind.Enum))
            return $"BigInt.asUintN(64, BigInt({value}))";
        if (source?.SpecialType == SpecialType.System_Int32)
        {
            string? narrowed = target?.SpecialType switch
            {
                SpecialType.System_SByte => "(__unityValue << 24) >> 24",
                SpecialType.System_Byte => "__unityValue & 255",
                SpecialType.System_Int16 => "(__unityValue << 16) >> 16",
                SpecialType.System_UInt16 => "__unityValue & 65535",
                SpecialType.System_UInt32 => "__unityValue >>> 0",
                _ => null,
            };
            if (narrowed != null)
                return $"((__unityValue) => {narrowed})({value})";
        }
        if (target?.SpecialType is SpecialType.System_Single or SpecialType.System_Double
            && source?.SpecialType is SpecialType.System_Int32 or SpecialType.System_Single
                or SpecialType.System_Double)
            return value;
        if (target?.SpecialType == SpecialType.System_Int32 && source?.TypeKind == TypeKind.Enum)
            return value;
        if (target?.TypeKind == TypeKind.Enum && source?.SpecialType == SpecialType.System_Int32)
            return value;
        if (target?.TypeKind == TypeKind.Enum && source?.TypeKind == TypeKind.Enum)
            return value;
        if (target?.ToDisplayString() is "UnityEngine.Playables.IPlayable"
                or "UnityEngine.Playables.IPlayableOutput"
            && source is INamedTypeSymbol playableInterfaceSource
            && playableInterfaceSource.AllInterfaces.Any(candidate =>
                candidate.ToDisplayString() == target.ToDisplayString()))
            return value;
        if (source?.ToDisplayString() == "UnityEngine.Playables.Playable"
            && target is INamedTypeSymbol scriptPlayableCast
            && scriptPlayableCast.OriginalDefinition.ToDisplayString() ==
                "UnityEngine.Playables.ScriptPlayable<T>")
        {
            imports.Add("ScriptPlayable");
            return $"ScriptPlayable.FromPlayable({value}, {RuntimeTypeName(scriptPlayableCast.TypeArguments[0], cast)})";
        }
        if (source?.ToDisplayString() == "UnityEngine.Playables.PlayableOutput"
            && target?.ToDisplayString() == "UnityEngine.Playables.ScriptPlayableOutput")
        {
            imports.Add("ScriptPlayableOutput");
            return $"ScriptPlayableOutput.FromPlayableOutput({value})";
        }
        if (source?.SpecialType == SpecialType.System_Object
            && (target?.SpecialType is SpecialType.System_Boolean or SpecialType.System_Byte
                or SpecialType.System_SByte or SpecialType.System_Int16 or SpecialType.System_UInt16
                or SpecialType.System_Int32 or SpecialType.System_UInt32 or SpecialType.System_Int64
                or SpecialType.System_UInt64 or SpecialType.System_Char or SpecialType.System_Decimal
                or SpecialType.System_Single or SpecialType.System_Double
                || target?.TypeKind == TypeKind.Enum))
        {
            imports.Add("UnityType");
            return $"UnityType.Unbox({value}, {JsonSerializer.Serialize(target!.ToDisplayString())})";
        }
        if (source?.SpecialType == SpecialType.System_Object
            && target?.ToDisplayString() is "UnityEngine.Vector2" or "UnityEngine.Vector3"
                or "UnityEngine.Vector4" or "UnityEngine.Quaternion" or "UnityEngine.Color")
        {
            string type = TypeName(cast.Type);
            return $"((__unityCastValue) => {{ if (!(__unityCastValue instanceof {type})) "
                + $"{{ const error = new TypeError({JsonSerializer.Serialize($"Cannot cast object to {target}")}); "
                + "error.name = \"InvalidCastException\"; throw error; } "
                + "return __unityCastValue.clone?.() ?? __unityCastValue; })"
                + $"({value})";
        }
        if (source?.SpecialType == SpecialType.System_Object
            && target?.TypeKind == TypeKind.Struct
            && emittedTypes.Contains(target.ToDisplayString()))
        {
            string type = EmittedName(target);
            return $"((__unityCastValue) => {{ if (!(__unityCastValue instanceof {type})) "
                + $"{{ const error = new TypeError({JsonSerializer.Serialize($"Cannot cast object to {target}")}); "
                + "error.name = \"InvalidCastException\"; throw error; } "
                + "return __unityCastValue.clone(); })"
                + $"({value})";
        }
        if (target is ITypeParameterSymbol && source?.SpecialType == SpecialType.System_Object)
            return value;
        if (target != null && source != null && SymbolEqualityComparer.Default.Equals(target, source))
            return value;
        if (target?.SpecialType == SpecialType.System_Object)
            return ConvertImplicitValue(cast.Expression, CopyStruct(cast.Expression, value), target);
        if (cast.Expression.IsKind(SyntaxKind.NullLiteralExpression) && target?.IsReferenceType == true)
            return "null";
        if (model.GetOperation(cast) is IConversionOperation { OperatorMethod: { } method }
            && SymbolEqualityComparer.Default.Equals(method.ContainingAssembly, model.Compilation.Assembly)
            && emittedTypes.Contains(method.ContainingType.ToDisplayString()))
            return $"{EmittedName(method.ContainingType)}.{ConversionMethodName(method)}({value})";
        if (target?.IsReferenceType == true && source?.IsReferenceType == true)
        {
            string type = TypeName(cast.Type);
            return $"((__unityCastValue) => {{ if (__unityCastValue == null || __unityCastValue instanceof {type}) "
                + $"return __unityCastValue; throw new TypeError({JsonSerializer.Serialize($"Invalid cast to {target}")}); }})({value})";
        }
        throw Unsupported(cast, $"Cast from {source} to {target} needs lowering");
    }

    private string ConditionalAccess(ConditionalAccessExpressionSyntax access)
    {
        return ConditionalTail(Expression(access.Expression), model.GetTypeInfo(access.Expression).Type,
            access.WhenNotNull);
    }

    private string ConditionalTail(string receiver, ITypeSymbol? receiverType, ExpressionSyntax tail)
    {
        if (tail is ConditionalAccessExpressionSyntax nested)
            return ConditionalTail(ConditionalTail(receiver, receiverType, nested.Expression),
                model.GetTypeInfo(nested.Expression).Type, nested.WhenNotNull);
        if (tail is ElementBindingExpressionSyntax element
            && element.ArgumentList.Arguments.Count == 1)
        {
            string index = Expression(element.ArgumentList.Arguments[0].Expression);
            return receiverType?.ToDisplayString() is "Newtonsoft.Json.Linq.JObject" or "Newtonsoft.Json.Linq.JArray"
                || IsList(receiverType) || IsDictionary(receiverType)
                ? $"{receiver}?.GetItem({index})" : $"{receiver}?.[{index}]";
        }
        if (tail is MemberBindingExpressionSyntax member)
            return $"{receiver}?.{member.Name.Identifier.ValueText}";
        if (tail is MemberAccessExpressionSyntax
            && ConditionalMemberChain(tail) is string memberChain)
            return $"{receiver}?.{memberChain}";
        if (tail is InvocationExpressionSyntax interfaceInvocation
            && interfaceInvocation.Expression is MemberBindingExpressionSyntax
            && receiverType?.TypeKind == TypeKind.Interface
            && model.GetSymbolInfo(interfaceInvocation).Symbol is IMethodSymbol interfaceMethod)
            return InterfaceCall(receiver, interfaceMethod, interfaceInvocation, true);
        if (tail is InvocationExpressionSyntax delegateInvocation
            && delegateInvocation.Expression is MemberBindingExpressionSyntax delegateCall
            && delegateCall.Name.Identifier.ValueText == "Invoke"
            && receiverType?.TypeKind == TypeKind.Delegate)
        {
            imports.Add("UnityType");
            return $"({receiver} == null ? undefined : UnityType.InvokeDelegate({receiver}, ["
                + string.Join(", ", delegateInvocation.ArgumentList.Arguments.Select(argument => Expression(argument.Expression)))
                + "]))";
        }
        if (tail is InvocationExpressionSyntax chainedInvocation
            && ConditionalMemberChain(chainedInvocation.Expression) is string chain)
            return $"{receiver}?.{chain}("
                + string.Join(", ", chainedInvocation.ArgumentList.Arguments.Select(argument => Expression(argument.Expression)))
                + ")";
        if (tail is InvocationExpressionSyntax invocation
            && invocation.Expression is MemberBindingExpressionSyntax called)
        {
            if (called.Name.Identifier.ValueText == "GetType"
                && invocation.ArgumentList.Arguments.Count == 0
                && model.GetSymbolInfo(invocation).Symbol is IMethodSymbol method
                && method.ContainingType.SpecialType == SpecialType.System_Object)
            {
                imports.Add("UnityType");
                string? staticType = receiverType is INamedTypeSymbol nullable
                    && nullable.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T
                    ? nullable.TypeArguments[0].ToDisplayString() : null;
                return $"((__unityReceiver) => __unityReceiver == null ? null : UnityType.Of(__unityReceiver"
                    + (staticType == null ? "" : $", {JsonSerializer.Serialize(staticType)}")
                    + $"))({receiver})";
            }
            if (model.GetSymbolInfo(invocation).Symbol is IMethodSymbol { ReducedFrom: { } extension }
                && extension.ContainingType.ToDisplayString() == "System.Linq.Enumerable")
            {
                imports.Add("PreviewEnumerable");
                var arguments = invocation.ArgumentList.Arguments.Select(arg => Expression(arg.Expression));
                return $"((__unityReceiver) => __unityReceiver == null ? null : PreviewEnumerable.{extension.Name}(__unityReceiver"
                    + (invocation.ArgumentList.Arguments.Count > 0 ? ", " + string.Join(", ", arguments) : "")
                    + $"))({receiver})";
            }
            if (called.Name is GenericNameSyntax generic
                && generic.Identifier.ValueText == "ToObject"
                && generic.TypeArgumentList.Arguments.Count == 1)
                return $"{receiver}?.ToObject({JsonSerializer.Serialize(model.GetTypeInfo(generic.TypeArgumentList.Arguments[0]).Type?.ToDisplayString())})";
            if (called.Name is GenericNameSyntax genericValue
                && genericValue.Identifier.ValueText == "Value"
                && genericValue.TypeArgumentList.Arguments.Count == 1)
                return $"{receiver}?.Value({JsonSerializer.Serialize(model.GetTypeInfo(genericValue.TypeArgumentList.Arguments[0]).Type?.ToDisplayString())}"
                    + (invocation.ArgumentList.Arguments.Count > 0 ? ", " + string.Join(", ", invocation.ArgumentList.Arguments.Select(arg => Expression(arg.Expression))) : "") + ")";
            return $"{receiver}?.{called.Name.Identifier.ValueText}("
                + string.Join(", ", invocation.ArgumentList.Arguments.Select(arg => Expression(arg.Expression))) + ")";
        }
        throw Unsupported(tail, "Conditional access form needs lowering");
    }

    private static string? ConditionalMemberChain(ExpressionSyntax expression) => expression switch
    {
        MemberBindingExpressionSyntax member => member.Name.Identifier.ValueText,
        MemberAccessExpressionSyntax member when ConditionalMemberChain(member.Expression) is string root
            => root + "." + member.Name.Identifier.ValueText,
        _ => null,
    };

    private string InterpolatedString(InterpolatedStringExpressionSyntax interpolated)
    {
        var pieces = new List<string>();
        foreach (var part in interpolated.Contents)
        {
            if (part is InterpolatedStringTextSyntax text)
                pieces.Add(JsonSerializer.Serialize(text.TextToken.ValueText));
            else if (part is InterpolationSyntax value && value.AlignmentClause == null)
            {
                if (value.FormatClause == null)
                {
                    imports.Add("CSharpString");
                    pieces.Add($"CSharpString({Expression(value.Expression)})");
                }
                else
                {
                    string format = value.FormatClause.FormatStringToken.ValueText;
                    if (format.Length < 2 || format[0] is not ('F' or 'f')
                        || !int.TryParse(format[1..], out int digits) || digits is < 0 or > 99)
                        throw Unsupported(part, $"Interpolation format {format} needs lowering");
                    pieces.Add($"Number({Expression(value.Expression)}).toFixed({digits})");
                }
            }
            else throw Unsupported(part, "Formatted interpolation needs lowering");
        }
        return pieces.Count == 0 ? "\"\"" : "(" + string.Join(" + ", pieces) + ")";
    }

    private string Invocation(InvocationExpressionSyntax invocation)
    {
        if (invocation.Expression is IdentifierNameSyntax identifier
            && identifier.Identifier.ValueText == "nameof")
        {
            var constant = model.GetConstantValue(invocation);
            if (!constant.HasValue || constant.Value is not string name)
                throw Unsupported(invocation, "nameof could not be evaluated at build time");
            return JsonSerializer.Serialize(name);
        }
        string? lowered = lowerings.TryLower(invocation, model, Expression);
        if (lowered != null) return lowered;
        var called = model.GetSymbolInfo(invocation).Symbol as IMethodSymbol;
        if (called?.Name == "CompareTo"
            && called.ContainingType.SpecialType is SpecialType.System_SByte or SpecialType.System_Byte
                or SpecialType.System_Int16 or SpecialType.System_UInt16
                or SpecialType.System_Int32 or SpecialType.System_UInt32
                or SpecialType.System_Int64 or SpecialType.System_UInt64
                or SpecialType.System_Single or SpecialType.System_Double
                or SpecialType.System_Decimal or SpecialType.System_Boolean
                or SpecialType.System_String
            && invocation.ArgumentList.Arguments.Count == 1
            && invocation.Expression is MemberAccessExpressionSyntax primitiveMember)
        {
            imports.Add("CSharpPrimitive");
            return $"CSharpPrimitive.CompareTo({Expression(primitiveMember.Expression)}, "
                + $"{Expression(invocation.ArgumentList.Arguments[0].Expression)})";
        }
        if (called?.IsStatic == true && called.Name == "IsNaN"
            && called.ContainingType.SpecialType is SpecialType.System_Single or SpecialType.System_Double
            && invocation.ArgumentList.Arguments.Count == 1)
            return $"Number.isNaN({Expression(invocation.ArgumentList.Arguments[0].Expression)})";
        if (called?.ContainingType.TypeKind == TypeKind.Delegate && called.Name == "Invoke"
            && invocation.Expression is MemberAccessExpressionSyntax delegateInvoke)
        {
            imports.Add("UnityType");
            return $"UnityType.InvokeDelegate({Expression(delegateInvoke.Expression)}, ["
                + string.Join(", ", invocation.ArgumentList.Arguments.Select(argument => Expression(argument.Expression)))
                + "])";
        }
        if (model.GetTypeInfo(invocation.Expression).Type?.TypeKind == TypeKind.Delegate)
        {
            imports.Add("UnityType");
            return $"UnityType.InvokeDelegate({Expression(invocation.Expression)}, ["
                + string.Join(", ", invocation.ArgumentList.Arguments.Select(argument => Expression(argument.Expression)))
                + "])";
        }
        if (called?.ContainingType.SpecialType == SpecialType.System_String
            && called.Name is "ToLower" or "ToUpper"
            && invocation.ArgumentList.Arguments.Count == 0
            && invocation.Expression is MemberAccessExpressionSyntax stringMember)
            return $"{Expression(stringMember.Expression)}."
                + (called.Name == "ToLower" ? "toLowerCase()" : "toUpperCase()");
        if (called?.ContainingType.ToDisplayString() == "UnityEngine.UIElements.EnumField"
            && called.Name == "Init" && invocation.ArgumentList.Arguments.Count > 0
            && model.GetTypeInfo(invocation.ArgumentList.Arguments[0].Expression).Type
                is INamedTypeSymbol { TypeKind: TypeKind.Enum } enumType
            && emittedTypes.Contains(enumType.ToDisplayString()))
        {
            var args = invocation.ArgumentList.Arguments.Select(argument => Expression(argument.Expression));
            if (invocation.ArgumentList.Arguments.Count == 1) args = args.Append("false");
            return $"{Expression(invocation.Expression)}({string.Join(", ", args.Append(EmittedName(enumType)))})";
        }
        if (called?.Name == "AsSpan" && invocation.ArgumentList.Arguments.Count == 0
            && called.ReducedFrom?.ContainingType.ToDisplayString() == "System.MemoryExtensions"
            && invocation.Expression is MemberAccessExpressionSyntax spanMember
            && model.GetTypeInfo(spanMember.Expression).Type is IArrayTypeSymbol)
            return Expression(spanMember.Expression);
        if (called?.ContainingType.ToDisplayString() == "UnityEngine.Application"
            && called.Name is "RegisterLogCallback" or "RegisterLogCallbackThreaded"
            && invocation.ArgumentList.Arguments.Count == 1
            && model.GetSymbolInfo(invocation.ArgumentList.Arguments[0].Expression).Symbol
                is IMethodSymbol logMethod)
        {
            var callback = invocation.ArgumentList.Arguments[0].Expression;
            string receiver = callback is MemberAccessExpressionSyntax member
                ? Expression(member.Expression)
                : logMethod.IsStatic ? logMethod.ContainingType.Name : "this";
            imports.Add("UnityEvent");
            return $"{Expression(invocation.Expression)}(UnityEvent.Bind({receiver}, "
                + $"{JsonSerializer.Serialize(MethodName(logMethod))}))";
        }
        if (called?.ReducedFrom?.ContainingType.ToDisplayString() is
                "UnityEngine.Playables.PlayableExtensions"
                or "UnityEngine.Playables.PlayableOutputExtensions"
            && invocation.Expression is MemberAccessExpressionSyntax playableMember)
            return $"{Expression(playableMember.Expression)}.{called.Name}({string.Join(", ",
                invocation.ArgumentList.Arguments.Select(argument => Expression(argument.Expression)))})";
        if (called?.ContainingType.ToDisplayString() is
                "UnityEngine.Playables.PlayableExtensions"
                or "UnityEngine.Playables.PlayableOutputExtensions"
            && called.IsStatic && invocation.ArgumentList.Arguments.Count > 0)
            return $"{Expression(invocation.ArgumentList.Arguments[0].Expression)}.{called.Name}({string.Join(", ",
                invocation.ArgumentList.Arguments.Skip(1).Select(argument => Expression(argument.Expression)))})";
        if (called?.ContainingType.ToDisplayString() == "UnityEngine.Playables.PlayableGraph"
            && called.IsStatic && called.Name == "Create")
        {
            imports.Add("PlayableGraph");
            return $"PlayableGraph.Create({string.Join(", ", invocation.ArgumentList.Arguments.Select(
                argument => Expression(argument.Expression)))})";
        }
        if (called?.ContainingType.ToDisplayString() == "UnityEngine.LineUtility"
            && called.IsStatic && called.Name == "Simplify")
        {
            imports.Add("LineUtility");
            var resultType = (called.Parameters[2].Type as INamedTypeSymbol)?.TypeArguments[0];
            var method = resultType?.SpecialType == SpecialType.System_Int32
                ? "SimplifyIndices" : "SimplifyPoints";
            return $"LineUtility.{method}({string.Join(", ", invocation.ArgumentList.Arguments.Select(
                argument => Expression(argument.Expression)))})";
        }
        if (called?.ContainingType.ToDisplayString() is
                "UnityEngine.Animations.AnimationClipPlayable"
                or "UnityEngine.Animations.AnimationPlayableOutput"
                or "UnityEngine.Playables.ScriptPlayableOutput"
            && called.IsStatic && called.Name == "Create")
        {
            imports.Add(called.ContainingType.Name);
            return $"{called.ContainingType.Name}.Create({string.Join(", ",
                invocation.ArgumentList.Arguments.Select(argument => Expression(argument.Expression)))})";
        }
        if (called?.ContainingType.OriginalDefinition.ToDisplayString() ==
                "UnityEngine.Playables.ScriptPlayable<T>"
            && called.IsStatic && called.Name == "Create")
        {
            imports.Add("ScriptPlayable");
            var behaviourType = RuntimeTypeName(called.ContainingType.TypeArguments[0], invocation);
            var args = new[] { Expression(invocation.ArgumentList.Arguments[0].Expression), behaviourType }
                .Concat(invocation.ArgumentList.Arguments.Skip(1)
                    .Select(argument => Expression(argument.Expression)));
            return $"ScriptPlayable.Create({string.Join(", ", args)})";
        }
        if (called?.IsStatic == true
            && called.ContainingType.ContainingNamespace.ToDisplayString() == "UnityEngine.Pool"
            && called.ContainingType.Name is ("CollectionPool" or "ListPool" or "HashSetPool"
                or "DictionaryPool" or "GenericPool" or "UnsafeGenericPool")
            && called.Name is "Get" or "Release")
        {
            var poolType = called.ContainingType;
            var poolName = poolType.Name;
            static bool HasOpenTypeParameter(ITypeSymbol type) => type is ITypeParameterSymbol
                || type is INamedTypeSymbol named && named.TypeArguments.Any(HasOpenTypeParameter);
            if (poolType.TypeArguments.Any(HasOpenTypeParameter))
                throw Unsupported(invocation,
                    $"{poolName} with an open generic type needs runtime type-specific pool identity");
            // Inherited CollectionPool methods retain their constructed base type.
            var collectionType = poolName == "CollectionPool" ? poolType.TypeArguments[0] : null;
            var key = poolName switch {
                "ListPool" => $"System.Collections.Generic.List<{poolType.TypeArguments[0]}>",
                "HashSetPool" => $"System.Collections.Generic.HashSet<{poolType.TypeArguments[0]}>",
                "DictionaryPool" => $"System.Collections.Generic.Dictionary<{poolType.TypeArguments[0]}, {poolType.TypeArguments[1]}>",
                "CollectionPool" => collectionType!.ToDisplayString(),
                _ => poolType.TypeArguments[0].ToDisplayString(),
            };
            imports.Add(poolName);
            var args = new List<string>();
            string? outTarget = null;
            foreach (var argument in invocation.ArgumentList.Arguments)
            {
                if (argument.RefKindKeyword.IsKind(SyntaxKind.OutKeyword))
                {
                    outTarget = argument.Expression is DeclarationExpressionSyntax declaration
                        && declaration.Designation is SingleVariableDesignationSyntax designation
                        ? designation.Identifier.ValueText
                        : argument.Expression is IdentifierNameSyntax outName
                            ? outName.Identifier.ValueText
                            : throw Unsupported(argument, "Pool Get(out) target is not supported");
                    args.Add("__unityPoolOut");
                }
                else args.Add(Expression(argument.Expression));
            }
            if (poolName is "CollectionPool" or "GenericPool" or "UnsafeGenericPool")
            {
                var itemType = collectionType ?? poolType.TypeArguments[0];
                var constructor = itemType is INamedTypeSymbol namedCollection
                    && namedCollection.OriginalDefinition.ToDisplayString() == "System.Collections.Generic.List<T>"
                    ? "List"
                    : itemType is INamedTypeSymbol namedDictionary
                        && namedDictionary.OriginalDefinition.ToDisplayString() == "System.Collections.Generic.Dictionary<TKey, TValue>"
                        ? "CSharpDictionary"
                        : itemType is INamedTypeSymbol namedSet
                            && namedSet.OriginalDefinition.ToDisplayString() == "System.Collections.Generic.HashSet<T>"
                            ? "CSharpHashSet"
                            : RuntimeTypeName(itemType, invocation);
                if (constructor is "List" or "CSharpDictionary" or "CSharpHashSet") imports.Add(constructor);
                args.Insert(0, constructor);
            }
            args.Insert(0, JsonSerializer.Serialize(key));
            var call = $"{poolName}.{called.Name}({string.Join(", ", args)})";
            return outTarget == null ? call
                : $"(() => {{ const __unityPoolOut = {{ value: null }}; const __unityPoolResult = {call}; "
                    + $"{outTarget} = __unityPoolOut.value; return __unityPoolResult; }})()";
        }
        if (called?.ContainingType.ToDisplayString() == "UnityEngine.Mesh"
            && called.Name == "GetUVs" && called.Parameters.Length == 2
            && called.Parameters[1].Type is INamedTypeSymbol uvList
            && uvList.TypeArguments.Length == 1
            && uvList.TypeArguments[0].ToDisplayString() is "UnityEngine.Vector3" or "UnityEngine.Vector4")
        {
            var dimension = uvList.TypeArguments[0].ToDisplayString() == "UnityEngine.Vector3" ? 3 : 4;
            return $"{Expression(invocation.Expression)}("
                + $"{string.Join(", ", invocation.ArgumentList.Arguments.Select(arg => Expression(arg.Expression)))}, "
                + $"{dimension})";
        }
        if (called?.ContainingType.ToDisplayString() == "UnityEngine.Mesh"
            && called.Name == "GetColors" && called.Parameters.Length == 1
            && called.Parameters[0].Type is INamedTypeSymbol colorList
            && colorList.TypeArguments.Length == 1
            && colorList.TypeArguments[0].ToDisplayString() == "UnityEngine.Color32")
            return $"{Expression(invocation.Expression)}({Expression(invocation.ArgumentList.Arguments[0].Expression)}, true)";
        if (called?.ContainingType.ToDisplayString() == "UnityEngine.Hash128"
            && called.Name is "Compute" or "Append" && called.IsGenericMethod)
        {
            var element = called.TypeArguments[0].SpecialType switch {
                SpecialType.System_Byte => "byte",
                SpecialType.System_Int32 => "int",
                SpecialType.System_Single => "float",
                _ => throw Unsupported(invocation,
                    $"Hash128 generic element type {called.TypeArguments[0]} is not supported"),
            };
            var hashArguments = invocation.ArgumentList.Arguments;
            var value = Expression(hashArguments[0].Expression);
            if (hashArguments[0].RefKindKeyword.IsKind(SyntaxKind.RefKeyword)) value = $"[{value}]";
            var start = hashArguments.Count == 3 ? Expression(hashArguments[1].Expression) : "undefined";
            var count = hashArguments.Count == 3 ? Expression(hashArguments[2].Expression) : "undefined";
            var hashTarget = called.IsStatic ? "Hash128" : invocation.Expression
                is MemberAccessExpressionSyntax member ? Expression(member.Expression)
                : throw Unsupported(invocation, "Hash128.Append receiver is missing");
            if (called.IsStatic) imports.Add("Hash128");
            return $"{hashTarget}.{(called.IsStatic ? "ComputeGeneric" : "AppendGeneric")}"
                + $"({value}, {start}, {count}, {JsonSerializer.Serialize(element)})";
        }
        if (called?.ContainingType.ToDisplayString() == "UnityEngine.Hash128"
            && called.Name is "Compute" or "Append"
            && called.Parameters.Length == 1
            && called.Parameters[0].Type.SpecialType == SpecialType.System_Single
            && invocation.ArgumentList.Arguments.Count == 1)
        {
            var value = Expression(invocation.ArgumentList.Arguments[0].Expression);
            if (called.IsStatic)
            {
                imports.Add("Hash128");
                return $"Hash128.ComputeFloat({value})";
            }
            if (invocation.Expression is MemberAccessExpressionSyntax member)
                return $"{Expression(member.Expression)}.AppendFloat({value})";
        }
        if (called?.ContainingType.SpecialType == SpecialType.System_Object
            && called.Name == "Equals"
            && invocation.Expression is MemberAccessExpressionSyntax objectEquals)
        {
            imports.Add("UnityType");
            if (called.IsStatic && invocation.ArgumentList.Arguments.Count == 2)
            {
                var values = invocation.ArgumentList.Arguments.Select((argument, index) =>
                    ConvertImplicitValue(argument.Expression, Expression(argument.Expression),
                        called.Parameters[index].Type));
                return $"UnityType.BoxedEquals({string.Join(", ", values)})";
            }
            if (!called.IsStatic && invocation.ArgumentList.Arguments.Count == 1)
            {
                var argument = invocation.ArgumentList.Arguments[0].Expression;
                return $"UnityType.BoxedEquals({Expression(objectEquals.Expression)}, "
                    + $"{ConvertImplicitValue(argument, Expression(argument), called.Parameters[0].Type)})";
            }
        }
        if (called?.ContainingType.ToDisplayString() == "UnityEngine.Events.UnityEventBase"
            && called.Name == "GetValidMethodInfo"
            && invocation.ArgumentList.Arguments.Count == 3)
        {
            imports.Add("UnityEventBase");
            var reflectionArguments = invocation.ArgumentList.Arguments;
            var typesExpression = reflectionArguments[2].Expression;
            var renderedTypes = typesExpression is MemberAccessExpressionSyntax empty
                && empty.ToString() == "Type.EmptyTypes" ? "[]" : Expression(typesExpression);
            return $"UnityEventBase.GetValidMethodInfo({Expression(reflectionArguments[0].Expression)}, "
                + $"{Expression(reflectionArguments[1].Expression)}, {renderedTypes})";
        }
        if (called?.ContainingType.TypeKind == TypeKind.Delegate
            && called.ContainingType.Name == "UnityAction"
            && called.ContainingType.ContainingNamespace.ToDisplayString() == "UnityEngine.Events"
            && called.Name is "BeginInvoke" or "EndInvoke"
            && invocation.Expression is MemberAccessExpressionSyntax delegateCall)
        {
            imports.Add("UnityAction");
            var values = invocation.ArgumentList.Arguments.Select(arg => Expression(arg.Expression))
                .Prepend(Expression(delegateCall.Expression));
            return $"UnityAction.{called.Name}({string.Join(", ", values)})";
        }
        if (called?.ContainingType.ToDisplayString() == "UnityEngine.Transform"
            && called.Name == "Rotate" && called.Parameters.Length is 2 or 3
            && called.Parameters[0].Type.ToDisplayString() == "UnityEngine.Vector3"
            && called.Parameters[1].Type.SpecialType == SpecialType.System_Single)
        {
            string receiver = invocation.Expression is MemberAccessExpressionSyntax member
                ? Expression(member.Expression) : "this";
            return $"{receiver}.RotateAxis({string.Join(", ", invocation.ArgumentList.Arguments.Select(arg => Expression(arg.Expression)))})";
        }
        if (called?.ContainingType.ToDisplayString() == "UnityEngine.Random"
            && called.Name is "Range" or "RandomRange" && called.Parameters.Length == 2
            && called.Parameters[0].Type.SpecialType == SpecialType.System_Single)
        {
            imports.Add("Random");
            return $"Random.RangeFloat({string.Join(", ", invocation.ArgumentList.Arguments.Select(
                (arg, index) => ConvertImplicitValue(arg.Expression, Expression(arg.Expression),
                    called.Parameters[index].Type)))})";
        }
        if (called?.Name == "GetType" && called.Parameters.Length == 0
            && invocation.Expression is MemberAccessExpressionSyntax getType
            && called.ContainingType.SpecialType == SpecialType.System_Object)
        {
            imports.Add("UnityType");
            var receiverType = model.GetTypeInfo(getType.Expression).Type;
            bool primitiveReceiver = receiverType?.SpecialType is
                SpecialType.System_Boolean or SpecialType.System_Byte or SpecialType.System_SByte
                or SpecialType.System_Int16 or SpecialType.System_UInt16
                or SpecialType.System_Int32 or SpecialType.System_UInt32
                or SpecialType.System_Int64 or SpecialType.System_UInt64
                or SpecialType.System_Char or SpecialType.System_Decimal
                or SpecialType.System_Single or SpecialType.System_Double or SpecialType.System_String;
            return primitiveReceiver
                ? $"UnityType.Of({Expression(getType.Expression)}, {JsonSerializer.Serialize(receiverType!.ToDisplayString())})"
                : $"UnityType.Of({Expression(getType.Expression)})";
        }
        if (called?.ContainingType.ToDisplayString() == "System.Type"
            && called.Name == "IsSubclassOf" && invocation.Expression is MemberAccessExpressionSyntax subclass
            && invocation.ArgumentList.Arguments.Count == 1)
        {
            imports.Add("UnityType");
            return $"UnityType.IsSubclassOf({TypeValueExpression(subclass.Expression)}, "
                + $"{TypeValueExpression(invocation.ArgumentList.Arguments[0].Expression)})";
        }
        if (called?.Name is "IsAssignableFrom" or "IsInstanceOfType" or "Equals"
            && invocation.Expression is MemberAccessExpressionSyntax typeCall
            && model.GetTypeInfo(typeCall.Expression).Type?.ToDisplayString() == "System.Type"
            && invocation.ArgumentList.Arguments.Count == 1)
        {
            imports.Add("UnityType");
            var value = invocation.ArgumentList.Arguments[0].Expression;
            if (called.Name == "IsInstanceOfType")
            {
                var valueType = model.GetTypeInfo(value).Type;
                bool primitiveValue = valueType?.SpecialType is
                    SpecialType.System_Boolean or SpecialType.System_Byte or SpecialType.System_SByte
                    or SpecialType.System_Int16 or SpecialType.System_UInt16
                    or SpecialType.System_Int32 or SpecialType.System_UInt32
                    or SpecialType.System_Int64 or SpecialType.System_UInt64
                    or SpecialType.System_Char or SpecialType.System_Decimal
                    or SpecialType.System_Single or SpecialType.System_Double or SpecialType.System_String;
                return $"UnityType.IsInstanceOfType({TypeValueExpression(typeCall.Expression)}, "
                    + $"{Expression(value)}{(primitiveValue ? ", " + JsonSerializer.Serialize(valueType!.ToDisplayString()) : "")})";
            }
            return $"UnityType.{called.Name}({TypeValueExpression(typeCall.Expression)}, "
                + $"{TypeValueExpression(value)})";
        }
        if (called?.ContainingType.SpecialType == SpecialType.System_String
            && called.Name == "Contains" && invocation.Expression is MemberAccessExpressionSyntax contains
            && invocation.ArgumentList.Arguments.Count == 1)
            return $"{Expression(contains.Expression)}.includes({Expression(invocation.ArgumentList.Arguments[0].Expression)})";
        if (called?.ContainingType.SpecialType == SpecialType.System_String
            && called.Name is ("StartsWith" or "EndsWith")
            && invocation.Expression is MemberAccessExpressionSyntax stringMethod
            && invocation.ArgumentList.Arguments.Count is 1 or 2)
        {
            imports.Add("CSharpString");
            var values = invocation.ArgumentList.Arguments.Select(argument => Expression(argument.Expression))
                .Prepend(Expression(stringMethod.Expression));
            return $"CSharpString.{called.Name}({string.Join(", ", values)})";
        }
        if (called?.ContainingType.SpecialType == SpecialType.System_String
            && called.Name == "Equals" && invocation.Expression is MemberAccessExpressionSyntax equals
            && invocation.ArgumentList.Arguments.Count is 1 or 2)
        {
            imports.Add("CSharpString");
            var values = invocation.ArgumentList.Arguments.Select(argument => Expression(argument.Expression));
            if (!called.IsStatic) values = values.Prepend(Expression(equals.Expression));
            return $"CSharpString.Equals({string.Join(", ", values)})";
        }
        if (called?.Name == "ToString" && called.Parameters.Length == 0
            && invocation.Expression is MemberAccessExpressionSyntax toString)
        {
            if (model.GetTypeInfo(toString.Expression).Type is { TypeKind: TypeKind.Enum } toStringEnumType)
            {
                imports.Add("CSharpEnum");
                return $"CSharpEnum.ToString({RuntimeTypeName(toStringEnumType, invocation)}, "
                    + $"{Expression(toString.Expression)})";
            }
            imports.Add("CSharpString");
            return $"CSharpString({Expression(toString.Expression)})";
        }
        if (called?.Name == "TryGetComponent"
            && (called.ContainingType.ToDisplayString() is "UnityEngine.GameObject" or "UnityEngine.Component")
            && invocation.ArgumentList.Arguments.Count is 1 or 2
            && invocation.ArgumentList.Arguments[^1].RefKindKeyword.IsKind(SyntaxKind.OutKeyword))
        {
            var output = invocation.ArgumentList.Arguments[^1].Expression;
            bool discard = output is DeclarationExpressionSyntax
                { Designation: DiscardDesignationSyntax };
            string name = output switch
            {
                DeclarationExpressionSyntax declaration when declaration.Designation is SingleVariableDesignationSyntax designation
                    => designation.Identifier.ValueText,
                IdentifierNameSyntax outIdentifier => Expression(outIdentifier),
                DeclarationExpressionSyntax { Designation: DiscardDesignationSyntax } => "",
                _ => throw Unsupported(output, "TryGetComponent out target is not supported"),
            };
            string type = invocation.ArgumentList.Arguments.Count == 2
                ? Expression(invocation.ArgumentList.Arguments[0].Expression)
                : called.TypeArguments.Length == 1
                    ? RuntimeTypeName(called.TypeArguments[0], output)
                    : throw Unsupported(invocation, "TryGetComponent type is unresolved");
            string receiver = invocation.Expression is MemberAccessExpressionSyntax member
                ? Expression(member.Expression) : "this";
            return discard
                ? $"({receiver}.GetComponent({type}) !== null)"
                : $"(({name} = {receiver}.GetComponent({type})) !== null)";
        }
        if (invocation.ArgumentList.Arguments.Any(argument => argument.RefKindKeyword.IsKind(SyntaxKind.RefKeyword)
            || argument.RefKindKeyword.IsKind(SyntaxKind.OutKeyword)))
        {
            if (called == null) throw Unsupported(invocation, "ref/out call target is unresolved");
            int id = refCounter++;
            var setup = new List<string>();
            var copyBack = new List<string>();
            var values = new List<string>();
            foreach (var argument in invocation.ArgumentList.Arguments)
            {
                if (!argument.RefKindKeyword.IsKind(SyntaxKind.RefKeyword)
                    && !argument.RefKindKeyword.IsKind(SyntaxKind.OutKeyword))
                {
                    values.Add(Expression(argument.Expression));
                    continue;
                }
                if (argument.Expression is IdentifierNameSyntax passed
                    && model.GetSymbolInfo(passed).Symbol is IParameterSymbol parameter
                    && parameter.RefKind is RefKind.Ref or RefKind.Out)
                {
                    values.Add(passed.Identifier.ValueText);
                    continue;
                }
                string name = $"__unityRef{id}_{values.Count}";
                string? copyTarget = argument.Expression is DeclarationExpressionSyntax declaration
                    ? declaration.Designation is SingleVariableDesignationSyntax outDesignation
                        ? outDesignation.Identifier.ValueText : declaration.Designation is DiscardDesignationSyntax
                            ? null : throw Unsupported(declaration, "Unsupported out designation")
                    : argument.Expression is IdentifierNameSyntax discard
                        && discard.Identifier.ValueText == "_"
                        && model.GetSymbolInfo(discard).Symbol is not (ILocalSymbol or IParameterSymbol or IFieldSymbol)
                        ? null
                    : Expression(argument.Expression);
                string initial = argument.RefKindKeyword.IsKind(SyntaxKind.RefKeyword) ? copyTarget! : "null";
                setup.Add($"const {name} = {{ value: {initial} }};");
                values.Add(name);
                if (copyTarget != null) copyBack.Add($"{copyTarget} = {name}.value;");
            }
            string call;
            if (called.ContainingType.ToDisplayString() == "System.Array" && called.Name == "Resize"
                && called.TypeArguments.Length == 1)
            {
                imports.Add("CSharpArray");
                call = $"CSharpArray.Resize({string.Join(", ", values)}, {DefaultValue(called.TypeArguments[0])})";
            }
            else if (called.ContainingType.ToDisplayString() == "System.Array" && called.Name == "Sort"
                && values.Count is 1 or 2)
            {
                imports.Add("CSharpArray");
                call = $"CSharpArray.Sort({string.Join(", ", values)})";
            }
            else if (called.ContainingType.ToDisplayString() == "System.Enum" && called.Name == "TryParse"
                && called.TypeArguments.Length == 1)
            {
                imports.Add("CSharpEnum");
                call = $"CSharpEnum.TryParse({RuntimeTypeName(called.TypeArguments[0], invocation)}, {string.Join(", ", values)})";
            }
            else if (called.ReducedFrom is IMethodSymbol refExtension
                && invocation.Expression is MemberAccessExpressionSyntax member)
            {
                if (!emittedTypes.Contains(refExtension.ContainingType.ToDisplayString()))
                    throw Unsupported(invocation, $"Extension container {refExtension.ContainingType} is not emitted");
                call = $"{EmittedName(refExtension.ContainingType)}.{MethodName(refExtension)}("
                    + $"{string.Join(", ", values.Prepend(Expression(member.Expression)))})";
            }
            else if (invocation.Expression is GenericNameSyntax)
                call = $"{(called.IsStatic ? EmittedName(called.ContainingType) : "this")}.{MethodName(called)}"
                    + $"({string.Join(", ", values)})";
            else call = $"{Expression(invocation.Expression)}({string.Join(", ", values)})";
            if (setup.Count == 0) return call;
            return $"(() => {{ {string.Join(" ", setup)} const __unityResult{id} = {call}; "
                + $"{string.Join(" ", copyBack)} return __unityResult{id}; }})()";
        }
        if (called?.ReducedFrom is IMethodSymbol extension)
        {
            if (invocation.Expression is not MemberAccessExpressionSyntax member)
                throw Unsupported(invocation, "Extension receiver could not be resolved");
            string source = Expression(member.Expression);
            if (extension.ContainingType.ToDisplayString() == "UnityEngine.UnityEventAwaitableExtensions"
                && extension.Name == "GetAwaiter")
                return $"{source}.GetAwaiter({extension.TypeParameters.Length})";
            if (extension.ContainingType.ToDisplayString() == "UnityEngine.AsyncOperationAwaitableExtensions"
                && extension.Name == "GetAwaiter")
                return $"{source}.GetAwaiter()";
            if (extension.ContainingType.ToDisplayString() == "System.MemoryExtensions"
                && extension.Name == "Contains" && invocation.ArgumentList.Arguments.Count == 1)
                return $"{source}.includes({Expression(invocation.ArgumentList.Arguments[0].Expression)})";
            if (extension.ContainingType.ToDisplayString() == "System.Linq.Enumerable"
                && invocation.ArgumentList.Arguments.Count == 0)
            {
                if (extension.Name == "ToArray") return $"Array.from({source})";
                if (extension.Name == "ToList")
                {
                    imports.Add("List");
                    return $"new List({source})";
                }
            }
            if (extension.ContainingType.ToDisplayString() == "System.Linq.Enumerable")
            {
                imports.Add("PreviewEnumerable");
                var linqArguments = invocation.ArgumentList.Arguments.Select(argument => Expression(argument.Expression));
                if (extension.Name == "OfType" && called.TypeArguments.Length == 1)
                    linqArguments = linqArguments.Prepend(RuntimeTypeName(called.TypeArguments[0], invocation));
                string result = $"PreviewEnumerable.{extension.Name}({string.Join(", ", linqArguments.Prepend(source))})";
                if (extension.Name == "FirstOrDefault" && called.TypeArguments.Length == 1
                    && IsGeneratedStruct(called.TypeArguments[0]))
                    return $"({result} ?? new {EmittedName(called.TypeArguments[0])}())";
                return result;
            }
            if (extension.ContainingType.ToDisplayString() == "UnityEngine.PhysicsSceneExtensions"
                && extension.Name == "GetPhysicsScene")
            {
                imports.Add("Physics");
                return "{ Raycast: (origin, direction, results, distance) => "
                    + "Physics.RaycastNonAlloc(origin, direction, results, distance) }";
            }
            if (extension.ContainingType.ToDisplayString() == "UnityEngine.ImageConversion"
                && extension.Name is "EncodeToJPG" or "EncodeToPNG" or "EncodeToTGA" or "EncodeToEXR" or "LoadImage")
                return $"{source}.{extension.Name}("
                    + string.Join(", ", invocation.ArgumentList.Arguments.Select(argument => Expression(argument.Expression)))
                    + ")";
            if (extension.ContainingType.ToDisplayString() == "UnityEngine.Assertions.Must.MustExtensions")
            {
                imports.Add("MustExtensions");
                var mustArguments = invocation.ArgumentList.Arguments.Select(argument => Expression(argument.Expression));
                return $"MustExtensions.{extension.Name}({string.Join(", ", mustArguments.Prepend(source))})";
            }
            if (extension.ContainingType.ToDisplayString() == "System.Collections.Generic.CollectionExtensions"
                && extension.Name == "GetValueOrDefault"
                && invocation.ArgumentList.Arguments.Count is 1 or 2)
                return $"{source}.GetValueOrDefault("
                    + string.Join(", ", invocation.ArgumentList.Arguments.Select(argument => Expression(argument.Expression)))
                    + ")";
            if (extension.ContainingType.ToDisplayString()
                == "UnityEngine.UIElements.INotifyValueChangedExtensions")
            {
                imports.Add("INotifyValueChangedExtensions");
                var args = invocation.ArgumentList.Arguments.Select(argument => Expression(argument.Expression));
                return $"INotifyValueChangedExtensions.{extension.Name}({string.Join(", ", args.Prepend(source))})";
            }
            if (extension.ContainingType.ToDisplayString()
                == "UnityEngine.InputSystem.InputActionRebindingExtensions")
                return $"{source}.{extension.Name}("
                    + string.Join(", ", invocation.ArgumentList.Arguments.Select(argument => Expression(argument.Expression)))
                    + ")";
            if (extension.ContainingType.ToDisplayString()
                == "UnityEngine.InputSystem.Utilities.Observable" && extension.Name == "Call")
            {
                var callback = invocation.ArgumentList.Arguments[0].Expression;
                string listener;
                if (model.GetSymbolInfo(callback).Symbol is IMethodSymbol method)
                {
                    imports.Add("UnityEvent");
                    string receiver = callback is MemberAccessExpressionSyntax callbackMember
                        ? Expression(callbackMember.Expression) : method.IsStatic
                            ? RuntimeTypeName(method.ContainingType, invocation) : "this";
                    listener = $"UnityEvent.Bind({receiver}, {JsonSerializer.Serialize(MethodName(method))})";
                }
                else listener = Expression(callback);
                return $"{source}.Call({listener})";
            }
            if (!emittedTypes.Contains(extension.ContainingType.ToDisplayString()))
                throw Unsupported(invocation, $"Extension container {extension.ContainingType} is not emitted");
            var values = (called.IsGenericMethod
                ? called.TypeArguments.Select(type => RuntimeTypeName(type, invocation))
                : Enumerable.Empty<string>()).Concat(new[] { Expression(member.Expression) })
                .Concat(invocation.ArgumentList.Arguments.Select((argument, index) =>
                {
                    string value = Expression(argument.Expression);
                    if (ParamsArraySpread(called, invocation, index).Length != 0) return "..." + value;
                    var parameter = called.Parameters[Math.Min(index, called.Parameters.Length - 1)];
                    var targetType = parameter.IsParams && parameter.Type is IArrayTypeSymbol paramsArray
                        ? paramsArray.ElementType : parameter.Type;
                    return ConvertImplicitValue(argument.Expression, value, targetType);
                }));
            return $"{EmittedName(extension.ContainingType)}.{MethodName(extension)}({string.Join(", ", values)})";
        }
        string target;
        var generic = invocation.Expression switch
        {
            GenericNameSyntax direct => direct,
            MemberAccessExpressionSyntax member when member.Name is GenericNameSyntax named => named,
            _ => null,
        };
        if (generic != null)
        {
            var symbol = model.GetSymbolInfo(invocation).Symbol as IMethodSymbol;
            if (symbol?.ContainingType.ToDisplayString() == "UnityEngine.JsonUtility"
                && symbol.Name == "FromJson" && symbol.TypeArguments.Length == 1)
            {
                imports.Add("JsonUtility");
                return $"JsonUtility.FromJson({Expression(invocation.ArgumentList.Arguments[0].Expression)}, "
                    + $"{RuntimeTypeName(symbol.TypeArguments[0], invocation)})";
            }
            if (symbol?.ContainingType.ToDisplayString() == "UnityEngine.EventSystems.ExecuteEvents"
                && symbol.TypeArguments.Length == 1
                && symbol.Name is "ValidateEventData" or "Execute" or "ExecuteHierarchy"
                    or "CanHandleEvent" or "GetEventHandler")
            {
                imports.Add("ExecuteEvents");
                var executeArguments = new[] { JsonSerializer.Serialize(symbol.TypeArguments[0].Name) }
                    .Concat(invocation.ArgumentList.Arguments.Select(argument => Expression(argument.Expression)));
                return $"ExecuteEvents.{symbol.Name}({string.Join(", ", executeArguments)})";
            }
            if (symbol?.ContainingType.ToDisplayString() == "UnityEngine.Object"
                && symbol.Name == "Instantiate" && symbol.TypeArguments.Length == 1)
            {
                imports.Add("Object");
                return $"Object.Instantiate({string.Join(", ", invocation.ArgumentList.Arguments.Select(arg => Expression(arg.Expression)))})";
            }
            if (symbol?.ContainingType.ToDisplayString() == "UnityEngine.ScriptableObject"
                && symbol.Name == "CreateInstance" && symbol.TypeArguments.Length == 1)
            {
                imports.Add("ScriptableObject");
                return $"ScriptableObject.CreateInstance({RuntimeTypeName(symbol.TypeArguments[0], invocation)})";
            }
            if (symbol?.ContainingType.ToDisplayString() == "UnityEngine.Resources"
                && symbol.Name is "Load" or "LoadAll" or "LoadAsync" && symbol.TypeArguments.Length == 1
                && invocation.ArgumentList.Arguments.Count == 1)
            {
                imports.Add("Resources");
                return $"Resources.{symbol.Name}({Expression(invocation.ArgumentList.Arguments[0].Expression)}, "
                    + $"{RuntimeTypeName(symbol.TypeArguments[0], invocation)})";
            }
            if (symbol?.ContainingType.ToDisplayString() == "UnityEngine.Resources"
                && symbol.Name == "FindObjectsOfTypeAll" && symbol.TypeArguments.Length == 1
                && invocation.ArgumentList.Arguments.Count == 0)
            {
                imports.Add("Resources");
                return $"Resources.FindObjectsOfTypeAll({RuntimeTypeName(symbol.TypeArguments[0], invocation)})";
            }
            if (symbol?.ContainingType.ToDisplayString() == "System.Array" && symbol.Name == "Empty") return "[]";
            if (symbol?.ContainingType.ToDisplayString() == "System.Linq.Enumerable" && symbol.Name == "Empty") return "[]";
            if (symbol?.ContainingType.ToDisplayString() == "Newtonsoft.Json.JsonConvert"
                && symbol.Name == "DeserializeObject" && symbol.TypeArguments.Length == 1)
            {
                imports.Add("PreviewJsonConvert");
                var values = invocation.ArgumentList.Arguments.Select(argument => Expression(argument.Expression));
                return $"PreviewJsonConvert.DeserializeObject({string.Join(", ", values)}, "
                    + $"{RuntimeTypeName(symbol.TypeArguments[0], invocation)})";
            }
            if (generic.Identifier.ValueText == "ToObject"
                && generic.TypeArgumentList.Arguments.Count == 1
                && invocation.Expression is MemberAccessExpressionSyntax objectMemberSyntax
                && model.GetTypeInfo(objectMemberSyntax.Expression).Type?.ToDisplayString()
                    is ("Newtonsoft.Json.Linq.JToken" or "Newtonsoft.Json.Linq.JObject"
                        or "Newtonsoft.Json.Linq.JArray"))
            {
                string receiver = invocation.Expression is MemberAccessExpressionSyntax objectMember
                    ? Expression(objectMember.Expression) : "this";
                return $"{receiver}.ToObject({JsonSerializer.Serialize(model.GetTypeInfo(generic.TypeArgumentList.Arguments[0]).Type?.ToDisplayString())})";
            }            if (symbol?.ContainingType.ToDisplayString() == "Newtonsoft.Json.Linq.JToken"
                && symbol.Name == "Value" && symbol.TypeArguments.Length == 1)
            {
                string receiver = invocation.Expression is MemberAccessExpressionSyntax valueMember
                    ? Expression(valueMember.Expression) : "this";
                var values = new[] { JsonSerializer.Serialize(symbol.TypeArguments[0].ToDisplayString()) }
                    .Concat(invocation.ArgumentList.Arguments.Select(argument => Expression(argument.Expression)));
                return $"{receiver}.Value({string.Join(", ", values)})";
            }
            if (symbol?.ContainingType.ToDisplayString() is "UnityEngine.InputSystem.InputAction"
                or "UnityEngine.InputSystem.InputAction.CallbackContext"
                && symbol.Name == "ReadValue" && generic.TypeArgumentList.Arguments.Count == 1)
            {
                var valueType = symbol.TypeArguments[0];
                string label = valueType.ToDisplayString() switch
                {
                    "UnityEngine.Vector2" => "Vector2",
                    "float" => "float",
                    "int" => "int",
                    _ => throw Unsupported(invocation, $"InputAction.ReadValue<{valueType}> is not mapped"),
                };
                string receiver = invocation.Expression is MemberAccessExpressionSyntax readMember
                    ? Expression(readMember.Expression) : "this";
                return $"{receiver}.ReadValue({JsonSerializer.Serialize(label)})";
            }
            if (symbol?.Name == "GetComponentAtIndex"
                && symbol.ContainingType.ToDisplayString() == "UnityEngine.GameObject"
                && invocation.ArgumentList.Arguments.Count == 1)
            {
                string receiver = invocation.Expression is MemberAccessExpressionSyntax indexedMember
                    ? Expression(indexedMember.Expression) : "this";
                string expectedType = symbol.IsGenericMethod
                    ? $", {RuntimeTypeName(symbol.TypeArguments[0], invocation)}" : "";
                return $"{receiver}.GetComponentAtIndex({Expression(invocation.ArgumentList.Arguments[0].Expression)}{expectedType})";
            }
            if (symbol?.ContainingType.ToDisplayString() == "UnityEngine.Animator"
                && symbol.Name is "GetBehaviours" or "GetBehaviour"
                && symbol.TypeArguments.Length == 1
                && invocation.ArgumentList.Arguments.Count == 0)
            {
                string receiver = invocation.Expression is MemberAccessExpressionSyntax animatorMember
                    ? Expression(animatorMember.Expression) : "this";
                return $"{receiver}.{symbol.Name}({RuntimeTypeName(symbol.TypeArguments[0], invocation)})";
            }
            if (symbol?.ContainingType.ToDisplayString() == "UnityEngine.TextAsset"
                && symbol.Name == "GetData" && symbol.TypeArguments.Length == 1
                && invocation.ArgumentList.Arguments.Count == 0)
            {
                string type = symbol.TypeArguments[0].SpecialType switch
                {
                    SpecialType.System_Byte => "byte",
                    SpecialType.System_UInt16 => "ushort",
                    SpecialType.System_Int32 => "int",
                    _ => throw Unsupported(invocation,
                        $"TextAsset.GetData<{symbol.TypeArguments[0]}> has no byte-layout mapping"),
                };
                string receiver = invocation.Expression is MemberAccessExpressionSyntax dataMember
                    ? Expression(dataMember.Expression) : "this";
                return $"{receiver}.GetData(\"{type}\")";
            }
            if (symbol?.ContainingType.ToDisplayString() == "UnityEngine.Playables.PlayableGraph"
                && symbol.Name is "GetOutputByType" or "GetOutputCountByType"
                && symbol.TypeArguments.Length == 1)
            {
                string receiver = invocation.Expression is MemberAccessExpressionSyntax graphMember
                    ? Expression(graphMember.Expression) : "this";
                var graphArguments = new[] { RuntimeTypeName(symbol.TypeArguments[0], invocation) }
                    .Concat(invocation.ArgumentList.Arguments.Select(argument => Expression(argument.Expression)));
                return $"{receiver}.{symbol.Name}({string.Join(", ", graphArguments)})";
            }
            if (symbol?.ContainingType.ToDisplayString() is "UnityEngine.Playables.Playable"
                    or "UnityEngine.Playables.PlayableOutput"
                && symbol.Name is "IsPlayableOfType" or "IsPlayableOutputOfType"
                && symbol.TypeArguments.Length == 1)
            {
                string receiver = invocation.Expression is MemberAccessExpressionSyntax genericPlayableMember
                    ? Expression(genericPlayableMember.Expression) : "this";
                return $"{receiver}.{symbol.Name}({RuntimeTypeName(symbol.TypeArguments[0], invocation)})";
            }
            if (symbol?.ContainingType.ToDisplayString() == "UnityEngine.UIElements.VisualElement"
                && symbol.Name is "Q" or "Query" && symbol.TypeArguments.Length == 1)
            {
                string receiver = invocation.Expression is MemberAccessExpressionSyntax queryMember
                    ? Expression(queryMember.Expression) : "this";
                var queryArguments = new[] { RuntimeTypeName(symbol.TypeArguments[0], invocation) }
                    .Concat(invocation.ArgumentList.Arguments.Select(argument => Expression(argument.Expression)));
                return $"{receiver}.{symbol.Name}({string.Join(", ", queryArguments)})";
            }
            if (symbol?.ContainingType.ToDisplayString() == "UnityEngine.UIElements.UQueryExtensions"
                && symbol.Name is "Q" or "Query" && symbol.TypeArguments.Length == 1)
            {
                imports.Add("UQueryExtensions");
                var queryArguments = invocation.ArgumentList.Arguments
                    .Select((argument, index) => index == 0
                        ? Expression(argument.Expression) + ", "
                            + RuntimeTypeName(symbol.TypeArguments[0], invocation)
                        : Expression(argument.Expression));
                return $"UQueryExtensions.{symbol.Name}({string.Join(", ", queryArguments)})";
            }
            if (symbol?.ContainingType.OriginalDefinition.Name == "UQueryBuilder"
                && symbol.ContainingType.ContainingNamespace.ToDisplayString() == "UnityEngine.UIElements"
                && symbol.Name is "OfType" or "Children" or "Descendents"
                && symbol.TypeArguments.Length == 1)
            {
                string receiver = invocation.Expression is MemberAccessExpressionSyntax queryMember
                    ? Expression(queryMember.Expression) : "this";
                var queryArguments = new[] { RuntimeTypeName(symbol.TypeArguments[0], invocation) }
                    .Concat(invocation.ArgumentList.Arguments.Select(argument => Expression(argument.Expression)));
                return $"{receiver}.{symbol.Name}({string.Join(", ", queryArguments)})";
            }
            if (symbol?.ContainingType.OriginalDefinition.Name is "UQueryBuilder" or "UQueryState"
                && symbol.ContainingType.ContainingNamespace.ToDisplayString() == "UnityEngine.UIElements"
                && symbol.Name == "ForEach" && symbol.TypeArguments.Length == 1
                && invocation.ArgumentList.Arguments.Count == 1)
            {
                string receiver = invocation.Expression is MemberAccessExpressionSyntax queryMember
                    ? Expression(queryMember.Expression) : "this";
                return $"{receiver}.ForEachMap({Expression(invocation.ArgumentList.Arguments[0].Expression)})";
            }
            if (symbol?.ContainingType.ToDisplayString() == "UnityEngine.UIElements.VisualElement"
                && symbol.Name is "GetFirstAncestorOfType" or "GetFirstOfType"
                && symbol.TypeArguments.Length == 1)
            {
                string receiver = invocation.Expression is MemberAccessExpressionSyntax ancestorMember
                    ? Expression(ancestorMember.Expression) : "this";
                return $"{receiver}.{symbol.Name}({RuntimeTypeName(symbol.TypeArguments[0], invocation)})";
            }
            if (symbol?.ContainingType.ToDisplayString() is "UnityEngine.UIElements.VisualElement"
                    or "UnityEngine.UIElements.CallbackEventHandler"
                && symbol.Name is "RegisterCallback" or "RegisterCallbackOnce" or "UnregisterCallback"
                && symbol.TypeArguments.Length is 1 or 2
                && symbol.TypeArguments[0] is INamedTypeSymbol eventType
                && eventType.ContainingNamespace.ToDisplayString() == "UnityEngine.UIElements"
                && eventType.Name.EndsWith("Event", StringComparison.Ordinal))
            {
                string receiver = invocation.Expression is MemberAccessExpressionSyntax callbackMember
                    ? Expression(callbackMember.Expression) : "this";
                var callbackArguments = new[] { JsonSerializer.Serialize(eventType.Name) }
                    .Concat(invocation.ArgumentList.Arguments.Select((argument, index) =>
                    {
                        if (index != 0 || model.GetSymbolInfo(argument.Expression).Symbol is not IMethodSymbol method)
                            return Expression(argument.Expression);
                        string target = argument.Expression is MemberAccessExpressionSyntax callbackTarget
                            ? Expression(callbackTarget.Expression)
                            : method.IsStatic ? RuntimeTypeName(method.ContainingType, invocation) : "this";
                        imports.Add("UnityEvent");
                        return $"UnityEvent.Bind({target}, {JsonSerializer.Serialize(MethodName(method))})";
                    }))
                    .Concat(symbol.TypeArguments.Length == 2 && invocation.ArgumentList.Arguments.Count == 2
                        ? new[] { "0" } : Array.Empty<string>());
                return $"{receiver}.{symbol.Name}({string.Join(", ", callbackArguments)})";
            }
            bool componentLookup = symbol?.Name is "GetComponent" or "GetComponents"
                or "GetComponentInChildren" or "GetComponentsInChildren"
                or "GetComponentInParent" or "GetComponentsInParent" or "AddComponent";
            bool findObject = symbol?.ContainingType.ToDisplayString() == "UnityEngine.Object"
                && symbol.Name is "FindObjectOfType" or "FindObjectsOfType" or "FindObjectsByType"
                    or "FindFirstObjectByType" or "FindAnyObjectByType";
            bool emittedGeneric = symbol?.IsGenericMethod == true
                && emittedTypes.Contains(symbol.ContainingType.ToDisplayString());
            bool editorWindow = symbol?.ContainingType.ToDisplayString() == "UnityEditor.EditorWindow"
                && symbol.Name == "GetWindow";
            if (symbol?.ContainingType.ToDisplayString() == "UnityEngine.AssetBundle"
                && symbol.Name is "LoadAsset" or "LoadAssetAsync" or "LoadAllAssets"
                    or "LoadAllAssetsAsync" or "LoadAssetWithSubAssets"
                    or "LoadAssetWithSubAssetsAsync"
                && invocation.Expression is MemberAccessExpressionSyntax bundleMember)
            {
                var bundleArguments = invocation.ArgumentList.Arguments.Select(arg => Expression(arg.Expression))
                    .Concat(generic.TypeArgumentList.Arguments.Select(TypeName));
                return $"{Expression(bundleMember.Expression)}.{symbol.Name}({string.Join(", ", bundleArguments)})";
            }
            if (symbol?.ContainingType.ToDisplayString() == "UnityEngine.Assertions.Assert"
                && symbol.Name is "AreEqual" or "AreNotEqual" or "IsNull" or "IsNotNull")
            {
                imports.Add("UnityType");
                return $"UnityType.Resolve(\"UnityEngine.Assertions.Assert\").{symbol.Name}("
                    + string.Join(", ", invocation.ArgumentList.Arguments.Select(arg => Expression(arg.Expression))) + ")";
            }
            if (symbol?.ContainingType.ToDisplayString() == "UnityEngine.UIElements.BaseTreeView"
                && symbol.Name is "GetItemDataForId" or "GetItemDataForIndex" or "GetSelectedItems")
            {
                var receiver = invocation.Expression is MemberAccessExpressionSyntax treeMember
                    ? Expression(treeMember.Expression) : "this";
                return $"{receiver}.{symbol.Name}("
                    + string.Join(", ", invocation.ArgumentList.Arguments.Select(arg => Expression(arg.Expression))) + ")";
            }
            if (symbol?.ContainingType.ToDisplayString()
                == "UnityEngine.UIElements.ToggleButtonGroupState"
                && symbol.Name is "FromEnumFlags" or "ToEnumFlags" or "Compare")
            {
                imports.Add("ToggleButtonGroupState");
                return $"ToggleButtonGroupState.{symbol.Name}("
                    + string.Join(", ", invocation.ArgumentList.Arguments.Select(arg => Expression(arg.Expression))) + ")";
            }
            if (symbol?.ContainingType.ToDisplayString() is "UnityEngine.Texture2D"
                or "UnityEngine.Texture3D" or "UnityEngine.Texture2DArray"
                or "UnityEngine.Cubemap" or "UnityEngine.CubemapArray"
                && symbol.Name is "GetPixelData" or "GetRawTextureData" or "SetPixelData")
            {
                var receiver = invocation.Expression is MemberAccessExpressionSyntax textureMember
                    ? Expression(textureMember.Expression) : "this";
                var method = symbol.Name == "GetRawTextureData"
                    ? "_GetRawTextureDataGeneric" : symbol.Name;
                return $"{receiver}.{method}("
                    + string.Join(", ", invocation.ArgumentList.Arguments.Select(arg => Expression(arg.Expression))) + ")";
            }
            if ((!componentLookup && !findObject && !emittedGeneric && !editorWindow)
                || (componentLookup || findObject) && generic.TypeArgumentList.Arguments.Count != 1)
                throw Unsupported(invocation, $"Generic method {symbol?.ContainingType}.{symbol?.Name} needs lowering");
            target = findObject ? $"Object.{symbol!.Name}"
                : editorWindow ? "EditorWindow.GetWindow"
                : invocation.Expression is MemberAccessExpressionSyntax member
                ? $"{Expression(member.Expression)}.{(emittedGeneric ? MethodName(symbol!) : generic.Identifier.ValueText)}"
                : symbol?.IsStatic == true ? $"{EmittedName(symbol.ContainingType)}.{MethodName(symbol)}"
                : $"this.{(emittedGeneric ? MethodName(symbol!) : generic.Identifier.ValueText)}";
            if (findObject) imports.Add("Object");
            if (editorWindow) imports.Add("EditorWindow");
            var args = generic.TypeArgumentList.Arguments.Select(TypeName)
                .Concat(invocation.ArgumentList.Arguments.Select(arg => Expression(arg.Expression)));
            return $"{target}({string.Join(", ", args)})";
        }
        if (called?.IsGenericMethod == true
            && (called.Name is "GetComponents" or "GetComponentsInChildren" or "GetComponentsInParent")
            && (called.ContainingType.ToDisplayString() is "UnityEngine.GameObject" or "UnityEngine.Component")
            && called.TypeArguments.Length == 1
            && invocation.ArgumentList.Arguments.Count == called.Parameters.Length
            && called.Parameters.LastOrDefault()?.Type.OriginalDefinition.ToDisplayString()
                == "System.Collections.Generic.List<T>")
        {
            string receiver = invocation.Expression is MemberAccessExpressionSyntax componentsMember
                ? Expression(componentsMember.Expression) : "this";
            var args = invocation.ArgumentList.Arguments.Select(argument => Expression(argument.Expression));
            return $"{receiver}.{called.Name}({RuntimeTypeName(called.TypeArguments[0], invocation)}, "
                + $"{string.Join(", ", args)})";
        }
        if (called?.ContainingType.ToDisplayString() == "System.Array" && called.Name == "Sort"
            && invocation.ArgumentList.Arguments.Count is 1 or 2)
        {
            imports.Add("CSharpArray");
            return $"CSharpArray.Sort({string.Join(", ", invocation.ArgumentList.Arguments.Select(arg => Expression(arg.Expression)))})";
        }
        if (called?.ContainingType.ToDisplayString() == "System.Array" && called.Name == "Reverse"
            && invocation.ArgumentList.Arguments.Count == 1)
        {
            imports.Add("CSharpArray");
            return $"CSharpArray.Reverse({Expression(invocation.ArgumentList.Arguments[0].Expression)})";
        }
        if (called?.ContainingType is INamedTypeSymbol listType && IsList(listType)
            && listType.TypeArguments.Length == 1 && IsCopiedValueType(listType.TypeArguments[0])
            && invocation.Expression is MemberAccessExpressionSyntax listCall)
        {
            string receiver = Expression(listCall.Expression);
            if (called.Name == "AddRange" && invocation.ArgumentList.Arguments.Count == 1)
                return $"{receiver}.AddRange(Array.from({Expression(invocation.ArgumentList.Arguments[0].Expression)}, "
                    + "__item => __item.clone()))";
            if (called.Name == "ToArray" && invocation.ArgumentList.Arguments.Count == 0)
                return $"{receiver}.ToArray().map(__item => __item.clone())";
        }
        if (called?.Name is "SendMessage" or "SendMessageUpwards" or "BroadcastMessage"
            && called.ContainingType.ToDisplayString() is "UnityEngine.GameObject" or "UnityEngine.Component"
            && invocation.ArgumentList.Arguments.Count == 2
            && called.Parameters[1].Type.SpecialType == SpecialType.System_Object)
        {
            // An integer object payload can equal a SendMessageOptions enum value.
            // The third argument keeps that overload distinction in JavaScript.
            var payload = invocation.ArgumentList.Arguments[1].Expression;
            return $"{Expression(invocation.Expression)}({Expression(invocation.ArgumentList.Arguments[0].Expression)}, "
                + $"{ConvertImplicitValue(payload, Expression(payload), called.Parameters[1].Type)}, 0)";
        }
        if (called?.ContainingType.TypeKind == TypeKind.Interface
            && invocation.Expression is MemberAccessExpressionSyntax interfaceMember)
            return InterfaceCall(Expression(interfaceMember.Expression), called, invocation, false);
        target = Expression(invocation.Expression);
        var arguments = invocation.ArgumentList.Arguments.Select((arg, index) =>
        {
            string value = Expression(arg.Expression);
            if (called == null || called.Parameters.Length == 0) return value;
            var parameter = called.Parameters[Math.Min(index, called.Parameters.Length - 1)];
            if (index >= called.Parameters.Length && !parameter.IsParams) return value;
            if (ParamsArraySpread(called, invocation, index).Length != 0) return $"...{value}";
            var targetType = parameter.IsParams && parameter.Type is IArrayTypeSymbol paramsArray
                ? paramsArray.ElementType : parameter.Type;
            if (called.Name == "GetComponent"
                && called.ContainingType.ToDisplayString() is "UnityEngine.GameObject" or "UnityEngine.Component"
                && targetType.SpecialType == SpecialType.System_String)
                value = $"({value} ?? \"\")";
            if (IsCopiedValueType(targetType)) value = CopyStruct(arg.Expression, value);
            return ConvertImplicitValue(arg.Expression, value, targetType);
        }).ToList();
        // C# can select an inherited overload even when a derived class declares
        // another overload with the same name. JavaScript shadows that base name.
        // Call the selected base implementation directly in that case.
        if (called is { IsStatic: false, MethodKind: MethodKind.Ordinary }
            && emittedTypes.Contains(called.ContainingType.OriginalDefinition.ToDisplayString()))
        {
            // C# binds a non-virtual overload to its declaring class. A derived
            // class can use the same emitted __N name for another signature,
            // which would otherwise hijack an internal base-class call.
            if (!called.IsVirtual && !called.IsOverride
                && called.ContainingType.GetMembers(called.Name).OfType<IMethodSymbol>()
                    .Count(candidate => candidate.MethodKind == MethodKind.Ordinary) > 1)
            {
                var receiver = invocation.Expression is MemberAccessExpressionSyntax member
                    ? Expression(member.Expression) : "this";
                return $"{EmittedName(called.ContainingType.OriginalDefinition)}.prototype.{MethodName(called)}"
                    + $".call({receiver}{(arguments.Count > 0 ? ", " : "")}{string.Join(", ", arguments)})";
            }
            var receiverType = invocation.Expression is MemberAccessExpressionSyntax receiverMember
                ? model.GetTypeInfo(receiverMember.Expression).Type as INamedTypeSymbol
                : model.GetEnclosingSymbol(invocation.SpanStart)?.ContainingType;
            if (receiverType != null
                && !SymbolEqualityComparer.Default.Equals(receiverType.OriginalDefinition,
                    called.ContainingType.OriginalDefinition)
                && receiverType.GetMembers(called.Name).OfType<IMethodSymbol>().Any(candidate =>
                    !candidate.IsOverride && MethodName(candidate) == MethodName(called)))
            {
                var receiver = invocation.Expression is MemberAccessExpressionSyntax member
                    ? Expression(member.Expression) : "this";
                return $"{EmittedName(called.ContainingType.OriginalDefinition)}.prototype.{MethodName(called)}"
                    + $".call({receiver}{(arguments.Count > 0 ? ", " : "")}{string.Join(", ", arguments)})";
            }
        }
        return $"{target}({string.Join(", ", arguments)})";
    }

    private string InterfaceCall(string receiver, IMethodSymbol method,
        InvocationExpressionSyntax invocation, bool conditional)
    {
        imports.Add("UnityType");
        var args = invocation.ArgumentList.Arguments.Select((argument, index) =>
            index < method.Parameters.Length
                ? ConvertImplicitValue(argument.Expression,
                    CopyStruct(argument.Expression, Expression(argument.Expression)),
                    method.Parameters[index].Type)
                : Expression(argument.Expression));
        string signature = JsonSerializer.Serialize(method.Parameters.Select(parameter =>
            parameter.Type.ToDisplayString()).ToArray());
        string target = conditional ? "__unityReceiver" : receiver;
        string call = $"UnityType.InvokeInterface({target}, {JsonSerializer.Serialize(method.Name)}, "
            + $"[{string.Join(", ", args)}], {signature})";
        return conditional ? $"((__unityReceiver) => __unityReceiver == null ? null : {call})({receiver})"
            : call;
    }

    private string ParamsArraySpread(IMethodSymbol called, InvocationExpressionSyntax invocation, int index)
    {
        var parameters = called.Parameters;
        return parameters.Length > 0 && parameters[^1].IsParams
            && index == parameters.Length - 1
            && invocation.ArgumentList.Arguments.Count == parameters.Length
            && model.GetTypeInfo(invocation.ArgumentList.Arguments[index].Expression).Type is IArrayTypeSymbol
            ? "..." : "";
    }

    private string Creation(ObjectCreationExpressionSyntax creation)
    {
        if (model.GetTypeInfo(creation).Type?.TypeKind == TypeKind.Delegate
            && creation.ArgumentList?.Arguments.Count == 1)
        {
            var argument = creation.ArgumentList.Arguments[0].Expression;
            return ConvertImplicitValue(argument, Expression(argument), model.GetTypeInfo(creation).Type);
        }
        if (model.GetTypeInfo(creation).Type is INamedTypeSymbol span
            && span.OriginalDefinition.ToDisplayString() is "System.ReadOnlySpan<T>" or "System.Span<T>"
            && creation.ArgumentList?.Arguments.Count == 1
            && model.GetTypeInfo(creation.ArgumentList.Arguments[0].Expression).Type is IArrayTypeSymbol)
            return Expression(creation.ArgumentList.Arguments[0].Expression);
        string type = TypeName(creation.Type);
        var resolvedConstructor = model.GetSymbolInfo(creation).Symbol as IMethodSymbol;
        IEnumerable<string> arguments = creation.ArgumentList?.Arguments.Select((arg, index) =>
            index < resolvedConstructor?.Parameters.Length
                ? ConvertImplicitValue(arg.Expression, Expression(arg.Expression),
                    resolvedConstructor.Parameters[index].Type)
                : Expression(arg.Expression)) ?? [];
        var createdType = model.GetTypeInfo(creation).Type;
        if (createdType is INamedTypeSymbol customStyleProperty
            && customStyleProperty.OriginalDefinition.ToDisplayString()
                == "UnityEngine.UIElements.CustomStyleProperty<T>")
            arguments = arguments.Append(JsonSerializer.Serialize(
                customStyleProperty.TypeArguments[0].ToDisplayString()));
        if (createdType?.ToDisplayString() == "UnityEngine.UIElements.EnumField"
            && resolvedConstructor != null && creation.ArgumentList != null)
        {
            int enumIndex = resolvedConstructor.Parameters.IndexOf(resolvedConstructor.Parameters
                .FirstOrDefault(parameter => parameter.Type.SpecialType == SpecialType.System_Enum));
            if (enumIndex >= 0 && enumIndex < creation.ArgumentList.Arguments.Count
                && model.GetTypeInfo(creation.ArgumentList.Arguments[enumIndex].Expression).Type
                    is INamedTypeSymbol { TypeKind: TypeKind.Enum } enumType
                && emittedTypes.Contains(enumType.ToDisplayString()))
                arguments = arguments.Append(EmittedName(enumType));
        }
        if (createdType is INamedTypeSymbol listType && IsList(listType)
            && listType.TypeArguments.Length == 1 && IsCopiedValueType(listType.TypeArguments[0])
            && resolvedConstructor?.Parameters.Length == 1
            && resolvedConstructor.Parameters[0].Type is INamedTypeSymbol enumerableType
            && enumerableType.OriginalDefinition.ToDisplayString() == "System.Collections.Generic.IEnumerable<T>"
            && creation.ArgumentList?.Arguments.Count == 1)
            arguments = [$"Array.from({Expression(creation.ArgumentList.Arguments[0].Expression)}, "
                + "__item => __item.clone())"];
        if (EventGenericArgumentNames(createdType) is string[] eventTypeNames)
            arguments = [JsonSerializer.Serialize(eventTypeNames)];
        if (model.GetSymbolInfo(creation).Symbol is IMethodSymbol inputConstructor
            && inputConstructor.ContainingType.ToDisplayString() == "UnityEngine.InputSystem.InputAction"
            && creation.ArgumentList != null)
        {
            var slots = new string[inputConstructor.Parameters.Length];
            int positional = 0;
            foreach (var argument in creation.ArgumentList.Arguments)
            {
                int index = argument.NameColon == null ? positional++
                    : inputConstructor.Parameters.IndexOf(inputConstructor.Parameters.First(parameter =>
                        parameter.Name == argument.NameColon.Name.Identifier.ValueText));
                slots[index] = Expression(argument.Expression);
            }
            arguments = slots.Take(Array.FindLastIndex(slots, item => item != null) + 1)
                .Select(item => item ?? "null");
        }
        if (model.GetSymbolInfo(creation).Symbol is IMethodSymbol constructor
            && emittedTypes.Contains(constructor.ContainingType.OriginalDefinition.ToDisplayString())
            && ConstructorOverloads(constructor.ContainingType).Count > 1)
            arguments = new[] { ConstructorId(constructor).ToString(CultureInfo.InvariantCulture) }
                .Concat(arguments);
        else if (createdType is INamedTypeSymbol { TypeKind: TypeKind.Struct } defaultStruct
            && emittedTypes.Contains(defaultStruct.OriginalDefinition.ToDisplayString())
            && ConstructorOverloads(defaultStruct).Count > 1
            && creation.ArgumentList?.Arguments.Count == 0)
            arguments = new[] { "-1" };
        if (createdType?.ToDisplayString() == "UnityEngine.Event"
            && resolvedConstructor?.Parameters.Length == 1
            && resolvedConstructor.Parameters[0].Type.SpecialType == SpecialType.System_Int32)
            return $"Event.ForDisplay({string.Join(", ", arguments)})";
        string constructed = model.GetTypeInfo(creation).Type?.ToDisplayString() == "UnityEngine.GameObject"
            ? $"{type}.Create({string.Join(", ", arguments)})"
            : $"new {(type.StartsWith("UnityType.Resolve(", StringComparison.Ordinal) ? $"({type})" : type)}({string.Join(", ", arguments)})";
        if (createdType != null && IsExceptionType(createdType)
            && createdType.ToDisplayString() != "UnityEngine.Assertions.AssertionException")
            constructed = $"globalThis.Object.assign({constructed}, {{ name: {JsonSerializer.Serialize(createdType.Name)} }})";
        if (creation.Initializer == null) return constructed;
        if (creation.Initializer.IsKind(SyntaxKind.CollectionInitializerExpression))
        {
            var additions = creation.Initializer.Expressions.Select(item => item is InitializerExpressionSyntax complex
                && complex.IsKind(SyntaxKind.ComplexElementInitializerExpression)
                ? $"__unityCollection.Add({string.Join(", ", complex.Expressions.Select(Expression))});"
                : $"__unityCollection.Add({CopyStruct(item, Expression(item))});");
            return $"(() => {{ const __unityCollection = {constructed}; {string.Join(" ", additions)} return __unityCollection; }})()";
        }
        var assignments = new List<string>();
        foreach (var item in creation.Initializer.Expressions)
        {
            if (item is not AssignmentExpressionSyntax assignment
                || !assignment.IsKind(SyntaxKind.SimpleAssignmentExpression)
                || assignment.Left is not IdentifierNameSyntax name)
                throw Unsupported(item, "Object initializer entry needs lowering");
            assignments.Add($"__unityObject.{name.Identifier.ValueText} = "
                + ConvertImplicitValue(assignment.Right,
                    CopyStruct(assignment.Right, Expression(assignment.Right)),
                    model.GetTypeInfo(assignment.Left).Type) + ";");
        }
        return $"(() => {{ const __unityObject = {constructed}; {string.Join(" ", assignments)} "
            + "return __unityObject; })()";
    }

    private string ImplicitCreation(ImplicitObjectCreationExpressionSyntax creation)
    {
        var type = model.GetTypeInfo(creation).Type
            ?? throw Unsupported(creation, "Target type for implicit object creation could not be resolved");
        if (type.TypeKind == TypeKind.Delegate && creation.ArgumentList.Arguments.Count == 1)
        {
            var argument = creation.ArgumentList.Arguments[0].Expression;
            return ConvertImplicitValue(argument, Expression(argument), type);
        }
        string target = RuntimeTypeName(type, creation);
        var resolvedConstructor = model.GetSymbolInfo(creation).Symbol as IMethodSymbol;
        var arguments = creation.ArgumentList.Arguments.Select((argument, index) =>
            index < resolvedConstructor?.Parameters.Length
                ? ConvertImplicitValue(argument.Expression, Expression(argument.Expression),
                    resolvedConstructor.Parameters[index].Type)
                : Expression(argument.Expression));
        if (type is INamedTypeSymbol listType && IsList(listType)
            && listType.TypeArguments.Length == 1 && IsCopiedValueType(listType.TypeArguments[0])
            && resolvedConstructor?.Parameters.Length == 1
            && resolvedConstructor.Parameters[0].Type is INamedTypeSymbol enumerableType
            && enumerableType.OriginalDefinition.ToDisplayString() == "System.Collections.Generic.IEnumerable<T>"
            && creation.ArgumentList.Arguments.Count == 1)
            arguments = [$"Array.from({Expression(creation.ArgumentList.Arguments[0].Expression)}, "
                + "__item => __item.clone())"];
        if (EventGenericArgumentNames(type) is string[] eventTypeNames)
            return $"new {target}({JsonSerializer.Serialize(eventTypeNames)})";
        if (type is INamedTypeSymbol customStyleProperty
            && customStyleProperty.OriginalDefinition.ToDisplayString()
                == "UnityEngine.UIElements.CustomStyleProperty<T>")
            arguments = arguments.Append(JsonSerializer.Serialize(
                customStyleProperty.TypeArguments[0].ToDisplayString()));
        if (type.ToDisplayString() == "UnityEngine.GameObject")
            return $"{target}.Create({string.Join(", ", arguments)})";
        if (resolvedConstructor != null && type is INamedTypeSymbol namedType
            && emittedTypes.Contains(namedType.OriginalDefinition.ToDisplayString())
            && ConstructorOverloads(namedType).Count > 1)
            arguments = new[] { ConstructorId(resolvedConstructor).ToString(CultureInfo.InvariantCulture) }
                .Concat(arguments);
        else if (type is INamedTypeSymbol { TypeKind: TypeKind.Struct } defaultStruct
            && emittedTypes.Contains(defaultStruct.OriginalDefinition.ToDisplayString())
            && ConstructorOverloads(defaultStruct).Count > 1
            && creation.ArgumentList.Arguments.Count == 0)
            arguments = new[] { "-1" };
        return $"new {(target.StartsWith("UnityType.Resolve(", StringComparison.Ordinal) ? $"({target})" : target)}({string.Join(", ", arguments)})";
    }

    private static string[]? EventGenericArgumentNames(ITypeSymbol? type)
    {
        if (type is not INamedTypeSymbol named || named.Name != "UnityEvent"
            || named.ContainingNamespace.ToDisplayString() != "UnityEngine.Events"
            || named.TypeArguments.Length == 0)
            return null;
        return named.TypeArguments.Select(argument => argument.ToDisplayString()).ToArray();
    }

    private string TypeName(TypeSyntax syntax)
    {
        var symbol = model.GetTypeInfo(syntax).Type;
        if (symbol == null) throw Unsupported(syntax, "Type could not be resolved");
        if (symbol is ITypeParameterSymbol) return symbol.Name;
        if (IsBrowserWebRequestType(symbol))
        { imports.Add(symbol.Name); return symbol.Name; }
        if (symbol.ToDisplayString() is "UnityEngine.EventSystems.EventTrigger"
            or "UnityEngine.EventSystems.ExecuteEvents"
            or "UnityEngine.EventSystems.BaseInput"
            or "UnityEngine.EventSystems.BaseInputModule"
            or "UnityEngine.EventSystems.PointerInputModule")
        { imports.Add(symbol.Name); return symbol.Name; }
        if (symbol.ToDisplayString() is "UnityEngine.EventSystems.EventTrigger.Entry"
            or "UnityEngine.EventSystems.EventTrigger.TriggerEvent")
        { imports.Add("EventTrigger"); return $"EventTrigger.{symbol.Name}"; }
        if (symbol.ToDisplayString() == "UnityEngine.UIElements.NavigationMoveEvent.Direction")
        { imports.Add("NavigationMoveEvent"); return "NavigationMoveEvent.Direction"; }
        var toolkitType = ToolkitRuntimeName(symbol);
        if (toolkitType != null) { imports.Add(toolkitType); return toolkitType; }
        if (symbol is INamedTypeSymbol awaiterType && awaiterType.Name == "Awaiter"
            && awaiterType.ContainingType?.OriginalDefinition.ToDisplayString() == "UnityEngine.Awaitable<T>")
        { imports.Add("AwaitableAwaiter"); return "AwaitableAwaiter"; }
        if (symbol is INamedTypeSymbol nativeArray
            && nativeArray.OriginalDefinition.ToDisplayString() == "Unity.Collections.NativeArray<T>")
        { imports.Add("PreviewNativeArray"); return "PreviewNativeArray"; }
        if (symbol is INamedTypeSymbol nativeSlice
            && nativeSlice.OriginalDefinition.ToDisplayString() == "Unity.Collections.NativeSlice<T>")
        { imports.Add("PreviewNativeSlice"); return "PreviewNativeSlice"; }
        if (symbol is INamedTypeSymbol poolType
            && poolType.ContainingNamespace.ToDisplayString() == "UnityEngine.Pool"
            && poolType.Name is ("IObjectPool" or "ObjectPool" or "LinkedPool" or "PooledObject"
                or "CollectionPool" or "ListPool" or "HashSetPool" or "DictionaryPool"
                or "GenericPool" or "UnsafeGenericPool"))
        { imports.Add(poolType.Name); return poolType.Name; }
        if (symbol.ToDisplayString() == "UnityEngine.Playables.PlayableGraph")
        { imports.Add("PlayableGraph"); return "PlayableGraph"; }
        if (symbol.ToDisplayString() == "UnityEngine.Sprites.DataUtility")
        { imports.Add("DataUtility"); return "DataUtility"; }
        if (symbol.ToDisplayString() == "UnityEngine.LineUtility")
        { imports.Add("LineUtility"); return "LineUtility"; }
        if (symbol.ToDisplayString() is "UnityEngine.RectInt.PositionEnumerator"
            or "UnityEngine.BoundsInt.PositionEnumerator")
        {
            var name = symbol.ContainingType.Name + "PositionEnumerator";
            imports.Add(name);
            return name;
        }
        if (symbol.ToDisplayString() == "UnityEngine.Playables.Notification")
        { imports.Add("Notification"); return "Notification"; }
        if (symbol is INamedTypeSymbol scriptPlayableType
            && scriptPlayableType.OriginalDefinition.ToDisplayString() ==
                "UnityEngine.Playables.ScriptPlayable<T>")
        { imports.Add("ScriptPlayable"); return "ScriptPlayable"; }
        if (symbol.ToDisplayString() is "UnityEngine.Playables.PlayableBehaviour"
            or "UnityEngine.Playables.ScriptPlayableOutput"
            or "UnityEngine.Playables.FrameData"
            or "UnityEngine.Playables.PlayableAsset")
        { imports.Add(symbol.Name); return symbol.Name; }
        if (symbol.ToDisplayString() is "UnityEngine.Playables.PlayableBinding"
            or "UnityEngine.Playables.ScriptPlayableBinding")
        { imports.Add(symbol.Name); return symbol.Name; }
        if (symbol.ToDisplayString() is "UnityEngine.Playables.Playable"
            or "UnityEngine.Playables.PlayableHandle"
            or "UnityEngine.Playables.PlayableOutput"
            or "UnityEngine.Playables.PlayableOutputHandle")
        { imports.Add(symbol.Name); return symbol.Name; }
        if (symbol.ToDisplayString() is "UnityEngine.Animations.AnimationClipPlayable"
            or "UnityEngine.Animations.AnimationPlayableOutput")
        { imports.Add(symbol.Name); return symbol.Name; }
        if (symbol.ToDisplayString() == "System.Type") { imports.Add("UnityType"); return "UnityType"; }
        if (symbol.ToDisplayString() == "System.Globalization.CultureInfo")
        { imports.Add("PreviewCultureInfo"); return "PreviewCultureInfo"; }
        if (symbol.ToDisplayString() == "UnityEngine.Rect") { imports.Add("Rect"); return "Rect"; }
        if (symbol.ToDisplayString() == "System.IO.Directory") { imports.Add("PreviewDirectory"); return "PreviewDirectory"; }
        if (symbol.ToDisplayString() == "System.IO.Path") { imports.Add("PreviewPath"); return "PreviewPath"; }
        if (symbol.ToDisplayString() == "System.IO.File") { imports.Add("PreviewFile"); return "PreviewFile"; }
        if (symbol.ToDisplayString() == "System.Text.Encoding") { imports.Add("PreviewEncoding"); return "PreviewEncoding"; }
        if (symbol.ToDisplayString() == "System.Array") { imports.Add("PreviewArray"); return "PreviewArray"; }
        if (symbol.ToDisplayString() == "System.Convert") { imports.Add("PreviewConvert"); return "PreviewConvert"; }
        if (symbol.ToDisplayString() == "System.Math") { imports.Add("PreviewMath"); return "PreviewMath"; }
        if (symbol.ToDisplayString() == "Newtonsoft.Json.JsonConvert")
        { imports.Add("PreviewJsonConvert"); return "PreviewJsonConvert"; }
        if (symbol.ToDisplayString() == "System.Security.Cryptography.AesManaged")
        { imports.Add("PreviewAesManaged"); return "PreviewAesManaged"; }
        if (symbol.ToDisplayString() == "System.Security.Cryptography.CryptoStream")
        { imports.Add("PreviewCryptoStream"); return "PreviewCryptoStream"; }
        if (symbol.ToDisplayString() == "System.IO.MemoryStream")
        { imports.Add("PreviewMemoryStream"); return "PreviewMemoryStream"; }
        if (symbol.ToDisplayString() == "System.IO.StreamReader")
        { imports.Add("PreviewStreamReader"); return "PreviewStreamReader"; }
        if (symbol.ToDisplayString() == "Newtonsoft.Json.JsonSerializerSettings")
        { imports.Add("PreviewJsonSerializerSettings"); return "PreviewJsonSerializerSettings"; }
        if (symbol.ToDisplayString() == "UnityEditor.EditorWindow") { imports.Add("EditorWindow"); return "EditorWindow"; }
        if (symbol.ToDisplayString() is "UnityEngine.InputSystem.InputSystem"
            or "UnityEngine.InputSystem.Gamepad" or "UnityEngine.InputSystem.Pointer"
            or "UnityEngine.InputSystem.Touchscreen"
            or "UnityEngine.InputSystem.InputActionMap"
            or "UnityEngine.InputSystem.InputActionAsset"
            or "UnityEngine.InputSystem.InputActionReference"
            or "UnityEngine.InputSystem.InputBinding"
            or "UnityEngine.InputSystem.InputActionChange"
            or "UnityEngine.InputSystem.InputDeviceChange"
            or "UnityEngine.InputSystem.PlayerInput"
            or "UnityEngine.InputSystem.DualShock.DualShockGamepad")
        { imports.Add(symbol.Name); return symbol.Name; }
        if (symbol.ToDisplayString() is "Newtonsoft.Json.Linq.JToken" or "Newtonsoft.Json.Linq.JObject"
            or "Newtonsoft.Json.Linq.JArray" or "System.Text.StringBuilder"
            or "UnityEngine.InputSystem.Keyboard" or "UnityEngine.InputSystem.Mouse")
        {
            imports.Add(symbol.Name);
            return symbol.Name;
        }
        if (symbol.SpecialType is SpecialType.System_Boolean) return "Boolean";
        if (symbol.SpecialType is SpecialType.System_String) return "String";
        if (symbol.SpecialType is SpecialType.System_Int32 or SpecialType.System_Single or SpecialType.System_Double)
            return "Number";
        if (symbol.ToDisplayString() == "UnityEngine.PlayerPrefsException")
        { imports.Add("PlayerPrefsException"); return "PlayerPrefsException"; }
        if (symbol.ToDisplayString() == "UnityEngine.Assertions.AssertionException")
        { imports.Add("AssertionException"); return "AssertionException"; }
        if (symbol.ToDisplayString() is "UnityEngine.UnityException" or "UnityEngine.MissingComponentException"
            or "UnityEngine.MissingReferenceException" or "UnityEngine.UnassignedReferenceException")
        { imports.Add(symbol.Name); return symbol.Name; }
        if (IsExceptionType(symbol)) return "Error";
        if (symbol is INamedTypeSymbol named && named.OriginalDefinition.ToDisplayString() == "System.Collections.Generic.List<T>")
        {
            imports.Add("List");
            return "List";
        }
        if (symbol is INamedTypeSymbol queue && queue.OriginalDefinition.ToDisplayString() == "System.Collections.Generic.Queue<T>")
        {
            imports.Add("CSharpQueue");
            return "CSharpQueue";
        }
        if (symbol.ToDisplayString() == "System.Guid") { imports.Add("CSharpGuid"); return "CSharpGuid"; }
        if (symbol.ToDisplayString() == "System.DateTime") { imports.Add("CSharpDateTime"); return "CSharpDateTime"; }
        if (symbol.ToDisplayString() == "System.DateTimeKind") { imports.Add("DateTimeKind"); return "DateTimeKind"; }
        if (symbol.ToDisplayString() == "System.TimeSpan") { imports.Add("CSharpTimeSpan"); return "CSharpTimeSpan"; }
        if (symbol.ToDisplayString() == "System.Threading.Thread") { imports.Add("PreviewThread"); return "PreviewThread"; }
        if (symbol.ToDisplayString() == "System.Threading.CancellationTokenSource") { imports.Add("PreviewCancellationTokenSource"); return "PreviewCancellationTokenSource"; }
        if (symbol.ToDisplayString() == "UnityEngine.Profiling.Profiler") { imports.Add("Profiler"); return "Profiler"; }
        if (IsCoreSimpleRenderingStruct(symbol)) { imports.Add(symbol.Name); return symbol.Name; }
        if (symbol.ToDisplayString() == "Unity.Netcode.NetworkBehaviour") { imports.Add("NetworkBehaviour"); return "NetworkBehaviour"; }
        if (IsDictionary(symbol)) { imports.Add("CSharpDictionary"); return "CSharpDictionary"; }
        if (IsReadOnlyArray(symbol)) { imports.Add("ReadOnlyArray"); return "ReadOnlyArray"; }
        if (symbol is INamedTypeSymbol set && set.OriginalDefinition.ToDisplayString() == "System.Collections.Generic.HashSet<T>")
        { imports.Add("CSharpHashSet"); return "CSharpHashSet"; }
        if (symbol.ContainingNamespace.ToDisplayString() is "UnityEngine" or "UnityEngine.Events" or "UnityEngine.AI" or "UnityEngine.UI"
            or "UnityEngine.Serialization"
            or "UnityEngine.SceneManagement" or "TMPro"
            or "UnityEngine.UI" or "UnityEngine.EventSystems"
            || symbol.ToDisplayString() is "UnityEngine.InputSystem.InputAction"
                or "UnityEngine.InputSystem.InputActionType" or "UnityEngine.InputSystem.InputActionPhase")
            imports.Add(symbol.Name);
        else if (!emittedTypes.Contains(symbol.ToDisplayString()))
        {
            if (editorMode || allowExternalTypes) return ExternalTypeName(symbol);
            throw Unsupported(syntax, $"Type {symbol} is not mapped to the runtime");
        }
        return EmittedName(symbol);
    }

    private string RuntimeTypeName(ITypeSymbol symbol, SyntaxNode source)
    {
        if (symbol is ITypeParameterSymbol) return symbol.Name;
        if (symbol.SpecialType is SpecialType.System_Int32 or SpecialType.System_UInt32
            or SpecialType.System_Int64 or SpecialType.System_UInt64
            or SpecialType.System_Int16 or SpecialType.System_UInt16
            or SpecialType.System_Byte or SpecialType.System_SByte
            or SpecialType.System_Single or SpecialType.System_Double
            or SpecialType.System_Decimal) return "Number";
        if (symbol.SpecialType == SpecialType.System_Boolean) return "Boolean";
        if (symbol is INamedTypeSymbol constructed
            && emittedNames.TryGetValue(constructed.OriginalDefinition.ToDisplayString(), out string? genericName))
            return genericName;
        if (symbol is INamedTypeSymbol collection)
        {
            string? runtimeCollection = collection.OriginalDefinition.ToDisplayString() switch
            {
                "System.Collections.Generic.List<T>" => "List",
                "System.Collections.Generic.Dictionary<TKey, TValue>" => "CSharpDictionary",
                "System.Collections.Generic.HashSet<T>" => "CSharpHashSet",
                _ => null,
            };
            if (runtimeCollection != null)
            {
                imports.Add(runtimeCollection);
                return runtimeCollection;
            }
        }
        if (IsBrowserWebRequestType(symbol))
        { imports.Add(symbol.Name); return symbol.Name; }
        if (symbol.ToDisplayString() is "UnityEngine.EventSystems.EventTrigger"
            or "UnityEngine.EventSystems.ExecuteEvents"
            or "UnityEngine.EventSystems.BaseInput"
            or "UnityEngine.EventSystems.BaseInputModule"
            or "UnityEngine.EventSystems.PointerInputModule")
        { imports.Add(symbol.Name); return symbol.Name; }
        if (symbol.ToDisplayString() is "UnityEngine.EventSystems.EventTrigger.Entry"
            or "UnityEngine.EventSystems.EventTrigger.TriggerEvent")
        { imports.Add("EventTrigger"); return $"EventTrigger.{symbol.Name}"; }
        if (symbol.ToDisplayString() == "UnityEngine.UIElements.NavigationMoveEvent.Direction")
        { imports.Add("NavigationMoveEvent"); return "NavigationMoveEvent.Direction"; }
        var toolkitType = ToolkitRuntimeName(symbol);
        if (toolkitType != null) { imports.Add(toolkitType); return toolkitType; }
        var enhancedTouchType = symbol.ToDisplayString() switch
        {
            "UnityEngine.InputSystem.TouchPhase" => "InputSystemTouchPhase",
            "UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport" => "EnhancedTouchSupport",
            "UnityEngine.InputSystem.EnhancedTouch.Touch" => "EnhancedTouchTouch",
            "UnityEngine.InputSystem.EnhancedTouch.Finger" => "EnhancedTouchFinger",
            _ => null
        };
        if (enhancedTouchType != null)
        { imports.Add(enhancedTouchType); return enhancedTouchType; }
        if (symbol is INamedTypeSymbol awaiterType && awaiterType.Name == "Awaiter"
            && awaiterType.ContainingType?.OriginalDefinition.ToDisplayString() == "UnityEngine.Awaitable<T>")
        { imports.Add("AwaitableAwaiter"); return "AwaitableAwaiter"; }
        if (symbol.ToDisplayString() == "UnityEngine.Sprites.DataUtility")
        { imports.Add("DataUtility"); return "DataUtility"; }
        if (symbol.ToDisplayString() == "UnityEngine.LineUtility")
        { imports.Add("LineUtility"); return "LineUtility"; }
        if (symbol.ToDisplayString() is "UnityEngine.RectInt.PositionEnumerator"
            or "UnityEngine.BoundsInt.PositionEnumerator")
        {
            var name = symbol.ContainingType.Name + "PositionEnumerator";
            imports.Add(name);
            return name;
        }
        if (symbol is INamedTypeSymbol scriptPlayableRuntimeType
            && scriptPlayableRuntimeType.OriginalDefinition.ToDisplayString() ==
                "UnityEngine.Playables.ScriptPlayable<T>")
        { imports.Add("ScriptPlayable"); return "ScriptPlayable"; }
        if (symbol.ToDisplayString() is "UnityEngine.Playables.ScriptPlayableOutput"
            or "UnityEngine.Playables.PlayableBehaviour"
            or "UnityEngine.Playables.FrameData")
        { imports.Add(symbol.Name); return symbol.Name; }
        if (symbol.ToDisplayString() == "UnityEngine.Playables.Playable")
        { imports.Add("Playable"); return "Playable"; }
        if (symbol.ToDisplayString() == "UnityEngine.Animations.AnimationPlayableOutput")
        { imports.Add("AnimationPlayableOutput"); return "AnimationPlayableOutput"; }
        if (symbol.ToDisplayString() == "UnityEngine.Animations.AnimationClipPlayable")
        { imports.Add("AnimationClipPlayable"); return "AnimationClipPlayable"; }
        if (symbol.ToDisplayString() == "UnityEngine.Device.Application")
        { imports.Add("DeviceApplication"); return "DeviceApplication"; }
        if (symbol.ToDisplayString() == "UnityEngine.Device.Screen")
        { imports.Add("DeviceScreen"); return "DeviceScreen"; }
        if (symbol.ToDisplayString() == "UnityEngine.Device.SystemInfo")
        { imports.Add("DeviceSystemInfo"); return "DeviceSystemInfo"; }
        if (symbol is INamedTypeSymbol nativeArray
            && nativeArray.OriginalDefinition.ToDisplayString() == "Unity.Collections.NativeArray<T>")
        { imports.Add("PreviewNativeArray"); return "PreviewNativeArray"; }
        if (symbol is INamedTypeSymbol nativeSlice
            && nativeSlice.OriginalDefinition.ToDisplayString() == "Unity.Collections.NativeSlice<T>")
        { imports.Add("PreviewNativeSlice"); return "PreviewNativeSlice"; }
        if (symbol.ToDisplayString() == "UnityEngine.PlayerPrefsException")
        { imports.Add("PlayerPrefsException"); return "PlayerPrefsException"; }
        if (symbol.ToDisplayString() == "UnityEngine.Assertions.AssertionException")
        { imports.Add("AssertionException"); return "AssertionException"; }
        if (symbol.ToDisplayString() is "UnityEngine.UnityException" or "UnityEngine.MissingComponentException"
            or "UnityEngine.MissingReferenceException" or "UnityEngine.UnassignedReferenceException")
        { imports.Add(symbol.Name); return symbol.Name; }
        if (IsExceptionType(symbol)) return "Error";
        if (symbol is INamedTypeSymbol queue && queue.OriginalDefinition.ToDisplayString() == "System.Collections.Generic.Queue<T>")
        {
            imports.Add("CSharpQueue");
            return "CSharpQueue";
        }
        if (symbol.ToDisplayString() == "System.Guid") { imports.Add("CSharpGuid"); return "CSharpGuid"; }
        if (symbol.ToDisplayString() == "System.DateTime") { imports.Add("CSharpDateTime"); return "CSharpDateTime"; }
        if (symbol.ToDisplayString() == "System.DateTimeKind") { imports.Add("DateTimeKind"); return "DateTimeKind"; }
        if (symbol.ToDisplayString() == "System.TimeSpan") { imports.Add("CSharpTimeSpan"); return "CSharpTimeSpan"; }
        if (symbol.ToDisplayString() == "System.Threading.Thread") { imports.Add("PreviewThread"); return "PreviewThread"; }
        if (symbol.ToDisplayString() == "System.Threading.CancellationTokenSource") { imports.Add("PreviewCancellationTokenSource"); return "PreviewCancellationTokenSource"; }
        if (symbol.ToDisplayString() == "UnityEngine.Profiling.Profiler") { imports.Add("Profiler"); return "Profiler"; }
        if (IsCoreSimpleRenderingStruct(symbol)) { imports.Add(symbol.Name); return symbol.Name; }
        if (symbol.ToDisplayString() == "Unity.Netcode.NetworkBehaviour") { imports.Add("NetworkBehaviour"); return "NetworkBehaviour"; }
        if (symbol.ToDisplayString() == "System.Type") { imports.Add("UnityType"); return "UnityType"; }
        if (symbol.ToDisplayString() == "System.Globalization.CultureInfo")
        { imports.Add("PreviewCultureInfo"); return "PreviewCultureInfo"; }
        if (symbol.ToDisplayString() == "System.IO.Directory") { imports.Add("PreviewDirectory"); return "PreviewDirectory"; }
        if (symbol.ToDisplayString() == "System.IO.Path") { imports.Add("PreviewPath"); return "PreviewPath"; }
        if (symbol.ToDisplayString() == "System.IO.File") { imports.Add("PreviewFile"); return "PreviewFile"; }
        if (symbol.ToDisplayString() == "System.Text.Encoding") { imports.Add("PreviewEncoding"); return "PreviewEncoding"; }
        if (symbol.ToDisplayString() == "System.Array") { imports.Add("PreviewArray"); return "PreviewArray"; }
        if (symbol.ToDisplayString() == "System.Convert") { imports.Add("PreviewConvert"); return "PreviewConvert"; }
        if (symbol.ToDisplayString() == "System.Math") { imports.Add("PreviewMath"); return "PreviewMath"; }
        if (symbol.ToDisplayString() == "Newtonsoft.Json.JsonConvert")
        { imports.Add("PreviewJsonConvert"); return "PreviewJsonConvert"; }
        if (symbol.ToDisplayString() == "System.Security.Cryptography.AesManaged")
        { imports.Add("PreviewAesManaged"); return "PreviewAesManaged"; }
        if (symbol.ToDisplayString() == "System.Security.Cryptography.CryptoStream")
        { imports.Add("PreviewCryptoStream"); return "PreviewCryptoStream"; }
        if (symbol.ToDisplayString() == "System.IO.MemoryStream")
        { imports.Add("PreviewMemoryStream"); return "PreviewMemoryStream"; }
        if (symbol.ToDisplayString() == "System.IO.StreamReader")
        { imports.Add("PreviewStreamReader"); return "PreviewStreamReader"; }
        if (symbol.ToDisplayString() == "Newtonsoft.Json.JsonSerializerSettings")
        { imports.Add("PreviewJsonSerializerSettings"); return "PreviewJsonSerializerSettings"; }
        if (symbol.ToDisplayString() == "UnityEditor.EditorWindow") { imports.Add("EditorWindow"); return "EditorWindow"; }
        if (symbol.ToDisplayString() is "UnityEngine.InputSystem.InputSystem"
            or "UnityEngine.InputSystem.Gamepad" or "UnityEngine.InputSystem.Pointer"
            or "UnityEngine.InputSystem.Touchscreen"
            or "UnityEngine.InputSystem.DualShock.DualShockGamepad")
        { imports.Add(symbol.Name); return symbol.Name; }
        if (symbol.SpecialType == SpecialType.System_String) return ImportedCSharpString();
        if (symbol.ToDisplayString() is "Newtonsoft.Json.Linq.JToken" or "Newtonsoft.Json.Linq.JObject"
            or "Newtonsoft.Json.Linq.JArray" or "System.Text.StringBuilder"
            or "UnityEngine.InputSystem.Keyboard" or "UnityEngine.InputSystem.Mouse")
        {
            imports.Add(symbol.Name);
            return symbol.Name;
        }
        if (symbol.ContainingNamespace.ToDisplayString() is "UnityEngine" or "UnityEngine.Events" or "UnityEngine.AI" or "UnityEngine.UI"
            or "UnityEngine.Serialization"
            or "UnityEngine.SceneManagement"
            || symbol.ToDisplayString() is "UnityEngine.InputSystem.InputAction")
            imports.Add(symbol.Name);
        else if (!emittedTypes.Contains(symbol.ToDisplayString()))
        {
            if (editorMode || allowExternalTypes) return ExternalTypeName(symbol);
            throw Unsupported(source, $"Type {symbol} is not mapped to the runtime");
        }
        return EmittedName(symbol);
    }

    private static bool IsBrowserWebRequestType(ITypeSymbol symbol) =>
        symbol.ToDisplayString() is "UnityEngine.Networking.UnityWebRequest"
            or "UnityEngine.Networking.UnityWebRequestAsyncOperation"
            or "UnityEngine.Networking.DownloadHandler"
            or "UnityEngine.Networking.DownloadHandlerBuffer"
            or "UnityEngine.Networking.DownloadHandlerTexture"
            or "UnityEngine.Networking.DownloadHandlerAudioClip"
            or "UnityEngine.Networking.DownloadHandlerAssetBundle"
            or "UnityEngine.Networking.UnityWebRequestTexture"
            or "UnityEngine.Networking.UnityWebRequestMultimedia"
            or "UnityEngine.Networking.UnityWebRequestAssetBundle"
            or "UnityEngine.Networking.UploadHandler"
            or "UnityEngine.Networking.UploadHandlerRaw";

    private static string? ToolkitRuntimeName(ITypeSymbol symbol) =>
        symbol is INamedTypeSymbol treeItem
            && treeItem.OriginalDefinition.ToDisplayString() == "UnityEngine.UIElements.TreeViewItemData<T>"
            ? "TreeViewItemData"
            : symbol is INamedTypeSymbol customStyleProperty
                && customStyleProperty.OriginalDefinition.ToDisplayString()
                    == "UnityEngine.UIElements.CustomStyleProperty<T>"
                ? "CustomStyleProperty"
            : symbol is INamedTypeSymbol baseField
                && baseField.OriginalDefinition.ToDisplayString()
                    == "UnityEngine.UIElements.BaseField<T>"
                ? "BaseField"
            : symbol is INamedTypeSymbol queryEnumerator
                && queryEnumerator.OriginalDefinition.ToDisplayString()
                    == "UnityEngine.UIElements.UQueryState<T>.Enumerator"
                ? "UQueryStateEnumerator"
            : symbol is INamedTypeSymbol compositeField
                && compositeField.OriginalDefinition.ToDisplayString()
                    == "UnityEngine.UIElements.BaseCompositeField<TValueType, TField, TFieldValue>"
                ? "BaseCompositeField"
            : symbol is INamedTypeSymbol popupField
                && popupField.OriginalDefinition.ToDisplayString()
                    == "UnityEngine.UIElements.PopupField<T>"
                ? "PopupField"
            : symbol is INamedTypeSymbol basePopupField
                && basePopupField.OriginalDefinition.ToDisplayString()
                    == "UnityEngine.UIElements.BasePopupField<TValueType, TValueChoice>"
                ? "BasePopupField"
            : symbol is INamedTypeSymbol eventBase
                && eventBase.OriginalDefinition.ToDisplayString() == "UnityEngine.UIElements.EventBase<T>"
                ? ToolkitRuntimeName(eventBase.TypeArguments[0])
            : symbol is INamedTypeSymbol navigationEventBase
                && navigationEventBase.OriginalDefinition.ToDisplayString()
                    == "UnityEngine.UIElements.NavigationEventBase<T>"
                ? "NavigationEventBase"
            : symbol is INamedTypeSymbol panelEventBase
                && panelEventBase.OriginalDefinition.ToDisplayString()
                    == "UnityEngine.UIElements.PanelChangedEventBase<T>"
                ? "PanelChangedEventBase"
            : symbol is INamedTypeSymbol commandEventBase
                && commandEventBase.OriginalDefinition.ToDisplayString()
                    == "UnityEngine.UIElements.CommandEventBase<T>"
                ? "CommandEventBase"
            : symbol is INamedTypeSymbol dragEventBase
                && dragEventBase.OriginalDefinition.ToDisplayString()
                    == "UnityEngine.UIElements.DragAndDropEventBase<T>"
                ? "DragAndDropEventBase"
            : symbol is INamedTypeSymbol transitionEventBase
                && transitionEventBase.OriginalDefinition.ToDisplayString()
                    == "UnityEngine.UIElements.TransitionEventBase<T>"
                ? "TransitionEventBase"
            : symbol is INamedTypeSymbol changeEvent
                && changeEvent.OriginalDefinition.ToDisplayString() == "UnityEngine.UIElements.ChangeEvent<T>"
                ? "ChangeEvent"
            : symbol is INamedTypeSymbol styleEnum
                && styleEnum.OriginalDefinition.ToDisplayString() == "UnityEngine.UIElements.StyleEnum<T>"
                ? "StyleEnum" : symbol.ToDisplayString() switch
    {
        "UnityEngine.UIElements.Length" => "Length",
        "UnityEngine.UIElements.PointerId" => "PointerId",
        "UnityEngine.UIElements.PointerType" => "UIElementsPointerType",
        "UnityEngine.UIElements.InputWrapper" => "InputWrapper",
        "UnityEngine.UIElements.DropdownMenuSizeMode" => "DropdownMenuSizeMode",
        "UnityEngine.UIElements.StyleLength" => "StyleLength",
        "UnityEngine.UIElements.StyleFloat" => "StyleFloat",
        "UnityEngine.UIElements.StyleColor" => "StyleColor",
        "UnityEngine.UIElements.Ratio" => "Ratio",
        "UnityEngine.UIElements.StyleRatio" => "StyleRatio",
        "UnityEngine.UIElements.BackgroundPosition" => "BackgroundPosition",
        "UnityEngine.UIElements.BackgroundRepeat" => "BackgroundRepeat",
        "UnityEngine.UIElements.BackgroundSize" => "BackgroundSize",
        "UnityEngine.UIElements.Background" => "Background",
        "UnityEngine.UIElements.StyleBackgroundPosition" => "StyleBackgroundPosition",
        "UnityEngine.UIElements.StyleBackgroundRepeat" => "StyleBackgroundRepeat",
        "UnityEngine.UIElements.StyleBackgroundSize" => "StyleBackgroundSize",
        "UnityEngine.UIElements.EventBase" => "UIElementsEventBase",
        "UnityEngine.UIElements.Focusable" => "Focusable",
        "UnityEngine.UIElements.FocusController" => "FocusController",
        "UnityEngine.UIElements.ClickEvent" => "ClickEvent",
        "UnityEngine.UIElements.NavigationSubmitEvent" => "NavigationSubmitEvent",
        "UnityEngine.UIElements.NavigationCancelEvent" => "NavigationCancelEvent",
        "UnityEngine.UIElements.NavigationMoveEvent" => "NavigationMoveEvent",
        "UnityEngine.UIElements.GeometryChangedEvent" => "GeometryChangedEvent",
        "UnityEngine.UIElements.InputEvent" => "InputEvent",
        "UnityEngine.UIElements.TooltipEvent" => "TooltipEvent",
        "UnityEngine.UIElements.CustomStyleResolvedEvent" => "CustomStyleResolvedEvent",
        "UnityEngine.UIElements.AttachToPanelEvent" => "AttachToPanelEvent",
        "UnityEngine.UIElements.DetachFromPanelEvent" => "DetachFromPanelEvent",
        "UnityEngine.UIElements.ExecuteCommandEvent" => "ExecuteCommandEvent",
        "UnityEngine.UIElements.ValidateCommandEvent" => "ValidateCommandEvent",
        "UnityEngine.UIElements.ContextClickEvent" => "ContextClickEvent",
        "UnityEngine.UIElements.DragEnterEvent" => "DragEnterEvent",
        "UnityEngine.UIElements.DragLeaveEvent" => "DragLeaveEvent",
        "UnityEngine.UIElements.DragUpdatedEvent" => "DragUpdatedEvent",
        "UnityEngine.UIElements.DragPerformEvent" => "DragPerformEvent",
        "UnityEngine.UIElements.DragExitedEvent" => "DragExitedEvent",
        "UnityEngine.UIElements.TransitionRunEvent" => "TransitionRunEvent",
        "UnityEngine.UIElements.TransitionStartEvent" => "TransitionStartEvent",
        "UnityEngine.UIElements.TransitionEndEvent" => "TransitionEndEvent",
        "UnityEngine.UIElements.TransitionCancelEvent" => "TransitionCancelEvent",
        "UnityEngine.UIElements.PointerDownEvent" => "PointerDownEvent",
        "UnityEngine.UIElements.PointerUpEvent" => "PointerUpEvent",
        "UnityEngine.UIElements.PointerMoveEvent" => "PointerMoveEvent",
        "UnityEngine.UIElements.PointerCancelEvent" => "PointerCancelEvent",
        "UnityEngine.UIElements.PointerOverEvent" => "PointerOverEvent",
        "UnityEngine.UIElements.PointerOutEvent" => "PointerOutEvent",
        "UnityEngine.UIElements.PointerEnterEvent" => "PointerEnterEvent",
        "UnityEngine.UIElements.PointerLeaveEvent" => "PointerLeaveEvent",
        "UnityEngine.UIElements.PointerStationaryEvent" => "PointerStationaryEvent",
        "UnityEngine.UIElements.WheelEvent" => "WheelEvent",
        "UnityEngine.UIElements.MouseDownEvent" => "MouseDownEvent",
        "UnityEngine.UIElements.MouseUpEvent" => "MouseUpEvent",
        "UnityEngine.UIElements.MouseMoveEvent" => "MouseMoveEvent",
        "UnityEngine.UIElements.MouseOverEvent" => "MouseOverEvent",
        "UnityEngine.UIElements.MouseOutEvent" => "MouseOutEvent",
        "UnityEngine.UIElements.MouseEnterEvent" => "MouseEnterEvent",
        "UnityEngine.UIElements.MouseLeaveEvent" => "MouseLeaveEvent",
        "UnityEngine.UIElements.MouseEnterWindowEvent" => "MouseEnterWindowEvent",
        "UnityEngine.UIElements.MouseLeaveWindowEvent" => "MouseLeaveWindowEvent",
        "UnityEngine.UIElements.FocusInEvent" => "FocusInEvent",
        "UnityEngine.UIElements.FocusOutEvent" => "FocusOutEvent",
        "UnityEngine.UIElements.FocusEvent" => "FocusEvent",
        "UnityEngine.UIElements.BlurEvent" => "BlurEvent",
        "UnityEngine.UIElements.KeyDownEvent" => "KeyDownEvent",
        "UnityEngine.UIElements.KeyUpEvent" => "KeyUpEvent",
        "UnityEngine.UIElements.PointerCaptureEvent" => "PointerCaptureEvent",
        "UnityEngine.UIElements.PointerCaptureOutEvent" => "PointerCaptureOutEvent",
        "UnityEngine.UIElements.MouseCaptureEvent" => "MouseCaptureEvent",
        "UnityEngine.UIElements.MouseCaptureOutEvent" => "MouseCaptureOutEvent",
        "UnityEngine.UIElements.CallbackEventHandler" => "CallbackEventHandler",
        "UnityEngine.UIElements.DropdownMenu" => "DropdownMenu",
        "UnityEngine.UIElements.DropdownMenuItem" => "DropdownMenuItem",
        "UnityEngine.UIElements.DropdownMenuSeparator" => "DropdownMenuSeparator",
        "UnityEngine.UIElements.DropdownMenuAction" => "DropdownMenuAction",
        "UnityEngine.UIElements.DropdownMenuEventInfo" => "DropdownMenuEventInfo",
        "UnityEngine.UIElements.AbstractProgressBar" => "AbstractProgressBar",
        "UnityEngine.UI.FontData" => "FontData",
        "UnityEngine.UI.ColorBlock" => "ColorBlock",
        "UnityEngine.UI.AnimationTriggers" => "AnimationTriggers",
        "UnityEngine.UI.AspectRatioFitter" => "AspectRatioFitter",
        "UnityEngine.UI.MaskUtilities" => "MaskUtilities",
        "UnityEngine.UI.CanvasUpdate" => "CanvasUpdate",
        "UnityEngine.UI.Navigation" => "Navigation",
        "UnityEngine.UI.ToggleGroup" => "ToggleGroup",
        "UnityEngine.UIElements.StyleSheet" => "StyleSheet",
        "UnityEngine.UIElements.BindableElement" => "BindableElement",
        "UnityEngine.UIElements.UQuery" => "UQuery",
        "UnityEngine.UIElements.BaseBoolField" => "BaseBoolField",
        "UnityEngine.UIElements.INotifyValueChangedExtensions" => "INotifyValueChangedExtensions",
        "UnityEngine.UIElements.TimerState" => "TimerState",
        "UnityEngine.UIElements.ICustomStyle" => "ICustomStyle",
        "UnityEngine.UIElements.VisualElementStyleSheetSet" => "VisualElementStyleSheetSet",
        "UnityEngine.UI.LayoutUtility" => "LayoutUtility",
        "UnityEngine.UI.LayoutRebuilder" => "LayoutRebuilder",
        "UnityEngine.GUI.Scope" => "GUIScope",
        "UnityEngine.GUI.GroupScope" => "GUIGroupScope",
        "UnityEngine.GUI.ClipScope" => "GUIClipScope",
        "UnityEngine.GUI.ScrollViewScope" => "GUIScrollViewScope",
        "UnityEngine.GUILayout.HorizontalScope" => "GUILayoutHorizontalScope",
        "UnityEngine.GUILayout.VerticalScope" => "GUILayoutVerticalScope",
        "UnityEngine.GUILayout.AreaScope" => "GUILayoutAreaScope",
        "UnityEngine.GUILayout.ScrollViewScope" => "GUILayoutScrollViewScope",
        "UnityEngine.UIElements.Button" => "UIElementsButton",
        "UnityEngine.UIElements.RepeatButton" => "RepeatButton",
        "UnityEngine.UIElements.EnumField" => "UIElementsEnumField",
        "UnityEngine.UIElements.Hash128Field" => "UIElementsHash128Field",
        "UnityEngine.UIElements.TextField" => "UIElementsTextField",
        "UnityEngine.UIElements.Toggle" => "UIElementsToggle",
        "UnityEngine.UIElements.RadioButton" => "UIElementsRadioButton",
        "UnityEngine.UIElements.RadioButtonGroup" => "UIElementsRadioButtonGroup",
        "UnityEngine.UIElements.ToggleButtonGroup" => "ToggleButtonGroup",
        "UnityEngine.UIElements.ToggleButtonGroupState" => "ToggleButtonGroupState",
        "UnityEngine.UIElements.TwoPaneSplitView" => "TwoPaneSplitView",
        "UnityEngine.UIElements.TwoPaneSplitViewOrientation" => "TwoPaneSplitViewOrientation",
        "UnityEngine.UIElements.GenericDropdownMenu" => "GenericDropdownMenu",
        "UnityEngine.UIElements.Foldout" => "UIElementsFoldout",
        "UnityEngine.UIElements.Slider" => "UIElementsSlider",
        "UnityEngine.UIElements.SliderInt" => "UIElementsSliderInt",
        "UnityEngine.UIElements.Vector2Field" => "UIElementsVector2Field",
        "UnityEngine.UIElements.Vector3Field" => "UIElementsVector3Field",
        "UnityEngine.UIElements.Vector4Field" => "UIElementsVector4Field",
        "UnityEngine.UIElements.Vector2IntField" => "UIElementsVector2IntField",
        "UnityEngine.UIElements.Vector3IntField" => "UIElementsVector3IntField",
        "UnityEngine.UIElements.RectField" => "UIElementsRectField",
        "UnityEngine.UIElements.RectIntField" => "UIElementsRectIntField",
        "UnityEngine.UIElements.BoundsField" => "UIElementsBoundsField",
        "UnityEngine.UIElements.BoundsIntField" => "UIElementsBoundsIntField",
        "UnityEngine.UIElements.MinMaxSlider" => "UIElementsMinMaxSlider",
        "UnityEngine.UIElements.IntegerField" => "UIElementsIntegerField",
        "UnityEngine.UIElements.FloatField" => "UIElementsFloatField",
        "UnityEngine.UIElements.DoubleField" => "UIElementsDoubleField",
        "UnityEngine.UIElements.LongField" => "UIElementsLongField",
        "UnityEngine.UIElements.UnsignedIntegerField" => "UIElementsUnsignedIntegerField",
        "UnityEngine.UIElements.UnsignedLongField" => "UIElementsUnsignedLongField",
        "UnityEngine.UIElements.DropdownField" => "UIElementsDropdownField",
        "UnityEngine.UIElements.ProgressBar" => "UIElementsProgressBar",
        "UnityEngine.UIElements.ScrollView" => "ScrollView",
        "UnityEngine.UIElements.Scroller" => "Scroller",
        "UnityEngine.UIElements.ScrollerVisibility" => "ScrollerVisibility",
        "UnityEngine.UIElements.SliderDirection" => "SliderDirection",
        "UnityEngine.UIElements.Box" => "Box",
        "UnityEngine.UIElements.GroupBox" => "GroupBox",
        "UnityEngine.UIElements.PopupWindow" => "PopupWindow",
        "UnityEngine.UIElements.HelpBox" => "HelpBox",
        "UnityEngine.UIElements.Image" => "UIElementsImage",
        "UnityEngine.UIElements.HelpBoxMessageType" => "HelpBoxMessageType",
        "UnityEngine.UIElements.Tab" => "Tab",
        "UnityEngine.UIElements.TabView" => "TabView",
        "UnityEngine.UIElements.ScrollViewMode" => "ScrollViewMode",
        "UnityEngine.UIElements.SelectionType" => "SelectionType",
        "UnityEngine.UIElements.CollectionVirtualizationMethod" => "CollectionVirtualizationMethod",
        "UnityEngine.UIElements.DragVisualMode" => "DragVisualMode",
        "UnityEngine.UIElements.ListViewReorderMode" => "ListViewReorderMode",
        "UnityEngine.UIElements.DragAndDropPosition" => "DragAndDropPosition",
        "UnityEngine.UIElements.StartDragArgs" => "StartDragArgs",
        "UnityEngine.UIElements.DragAndDropData" => "DragAndDropData",
        "UnityEngine.UIElements.TreeViewExpansionChangedArgs" => "TreeViewExpansionChangedArgs",
        "UnityEngine.UIElements.BaseVerticalCollectionView" => "BaseVerticalCollectionView",
        "UnityEngine.UIElements.BaseListView" => "BaseListView",
        "UnityEngine.UIElements.ListView" => "ListView",
        "UnityEngine.UIElements.Column" => "Column",
        "UnityEngine.UIElements.Columns" => "Columns",
        "UnityEngine.UIElements.ColumnSortingMode" => "ColumnSortingMode",
        "UnityEngine.UIElements.SortDirection" => "SortDirection",
        "UnityEngine.UIElements.SortColumnDescription" => "SortColumnDescription",
        "UnityEngine.UIElements.SortColumnDescriptions" => "SortColumnDescriptions",
        "UnityEngine.UIElements.MultiColumnListView" => "MultiColumnListView",
        "UnityEngine.UIElements.BaseTreeView" => "BaseTreeView",
        "UnityEngine.UIElements.TreeView" => "TreeView",
        "UnityEngine.UIElements.MultiColumnTreeView" => "MultiColumnTreeView",
        "UnityEngine.UIElements.VisualElement" => "VisualElement",
        "UnityEngine.UIElements.TextElement" => "TextElement",
        "UnityEngine.UIElements.TemplateContainer" => "TemplateContainer",
        "UnityEngine.UIElements.Label" => "Label",
        "UnityEngine.UIElements.VisualTreeAsset" => "VisualTreeAsset",
        "UnityEngine.UIElements.PanelSettings" => "PanelSettings",
        "UnityEngine.UIElements.PanelRaycaster" => "PanelRaycaster",
        "UnityEngine.EventSystems.BaseRaycaster" => "BaseRaycaster",
        "UnityEngine.EventSystems.RaycastResult" => "RaycastResult",
        "UnityEngine.UIElements.UIDocument" => "UIDocument",
        _ => null,
    };

    private static bool IsCoreSimpleRenderingStruct(ITypeSymbol symbol) =>
        symbol.ToDisplayString() is "UnityEngine.Rendering.SubMeshDescriptor"
            or "UnityEngine.Rendering.VertexAttributeDescriptor"
            or "UnityEngine.Rendering.MetadataValue"
            or "UnityEngine.Rendering.RayTracingInstanceCullingTest"
            or "UnityEngine.Rendering.RayTracingInstanceMaterialCRC"
            or "UnityEngine.Rendering.RayTracingInstanceTriangleCullingConfig";

    private static bool IsExceptionType(ITypeSymbol symbol)
    {
        for (var type = symbol as INamedTypeSymbol; type != null; type = type.BaseType)
            if (type.ToDisplayString() == "System.Exception") return true;
        return false;
    }

    private string ExternalTypeName(ITypeSymbol symbol)
    {
        requiredTypes[symbol.ToDisplayString()] = symbol.ContainingAssembly?.Name ?? "";
        imports.Add("UnityType");
        return $"UnityType.Resolve({JsonSerializer.Serialize(symbol.ToDisplayString())})";
    }

    private string IsPattern(IsPatternExpressionSyntax pattern)
    {
        if (pattern.Pattern is UnaryPatternSyntax { Pattern: TypePatternSyntax excluded }
            && pattern.Pattern.IsKind(SyntaxKind.NotPattern))
            return $"(!({Expression(pattern.Expression)} instanceof {TypeName(excluded.Type)}))";
        if (pattern.Pattern is UnaryPatternSyntax { Pattern: DeclarationPatternSyntax excludedDeclaration }
            && excludedDeclaration.Designation is not SingleVariableDesignationSyntax
            && pattern.Pattern.IsKind(SyntaxKind.NotPattern))
            return $"(!({Expression(pattern.Expression)} instanceof {TypeName(excludedDeclaration.Type)}))";
        if (pattern.Pattern is UnaryPatternSyntax { Pattern: ConstantPatternSyntax excludedConstant }
            && pattern.Pattern.IsKind(SyntaxKind.NotPattern))
        {
            if (model.GetSymbolInfo(excludedConstant.Expression).Symbol is INamedTypeSymbol excludedType)
                return $"(!({Expression(pattern.Expression)} instanceof {RuntimeTypeName(excludedType, pattern)}))";
            return excludedConstant.Expression.IsKind(SyntaxKind.NullLiteralExpression)
                ? $"({Expression(pattern.Expression)} != null)"
                : $"({Expression(pattern.Expression)} !== {Expression(excludedConstant.Expression)})";
        }
        if (pattern.Pattern is TypePatternSyntax matching)
            return $"({Expression(pattern.Expression)} instanceof {TypeName(matching.Type)})";
        if (pattern.Pattern is RecursivePatternSyntax
            or UnaryPatternSyntax { Pattern: RecursivePatternSyntax })
            return $"((__unityPatternValue) => {PropertyPatternPredicate(pattern.Pattern, "__unityPatternValue")})"
                + $"({Expression(pattern.Expression)})";
        if (pattern.Pattern is UnaryPatternSyntax unary && unary.Pattern is DeclarationPatternSyntax negative
            && negative.Designation is SingleVariableDesignationSyntax bound)
        {
            string type = TypeName(negative.Type);
            return $"(!(({bound.Identifier.ValueText} = {Expression(pattern.Expression)}) instanceof {type}))";
        }
        if (pattern.Pattern is DeclarationPatternSyntax declaration
            && declaration.Designation is SingleVariableDesignationSyntax variable)
        {
            string type = TypeName(declaration.Type);
            return $"(({variable.Identifier.ValueText} = {Expression(pattern.Expression)}) instanceof {type})";
        }
        if (pattern.Pattern is ConstantPatternSyntax constant
            && constant.Expression.IsKind(SyntaxKind.NullLiteralExpression))
            return $"({Expression(pattern.Expression)} == null)";
        throw Unsupported(pattern, "Pattern form needs lowering");
    }

    private string PropertyPatternPredicate(PatternSyntax pattern, string value)
    {
        if (pattern is UnaryPatternSyntax unary && unary.IsKind(SyntaxKind.NotPattern))
            return $"!({PropertyPatternPredicate(unary.Pattern, value)})";
        if (pattern is ConstantPatternSyntax constant)
            return $"{value} === {Expression(constant.Expression)}";
        if (pattern is RecursivePatternSyntax recursive
            && recursive.PositionalPatternClause == null
            && recursive.Designation == null
            && recursive.PropertyPatternClause != null)
        {
            var checks = new List<string> { $"{value} != null" };
            if (recursive.Type != null)
                checks.Add($"{value} instanceof {TypeName(recursive.Type)}");
            foreach (var subpattern in recursive.PropertyPatternClause.Subpatterns)
            {
                string? member = subpattern.NameColon?.Name.ToString()
                    ?? subpattern.ExpressionColon?.Expression.ToString();
                if (string.IsNullOrWhiteSpace(member))
                    throw Unsupported(subpattern, "Unnamed property pattern needs lowering");
                checks.Add(PropertyPatternPredicate(subpattern.Pattern, $"{value}.{member}"));
            }
            return "(" + string.Join(" && ", checks) + ")";
        }
        throw Unsupported(pattern, $"Property pattern {pattern.Kind()} needs lowering");
    }

    private string Binary(BinaryExpressionSyntax binary)
    {
        if (binary.IsKind(SyntaxKind.IsExpression) || binary.IsKind(SyntaxKind.AsExpression))
        {
            if (binary.Right is not TypeSyntax checkedType)
                throw Unsupported(binary, "Type test has no resolved type syntax");
            var targetType = model.GetTypeInfo(checkedType).Type;
            if (targetType is IArrayTypeSymbol)
            {
                string arraySource = Expression(binary.Left);
                return binary.IsKind(SyntaxKind.IsExpression)
                    ? $"Array.isArray({arraySource})"
                    : $"((__unityCastValue) => Array.isArray(__unityCastValue) ? __unityCastValue : null)({arraySource})";
            }
            string target = TypeName(checkedType);
            string source = Expression(binary.Left);
            if (binary.IsKind(SyntaxKind.AsExpression)
                && targetType?.ToDisplayString() is ("Newtonsoft.Json.Linq.JObject" or "Newtonsoft.Json.Linq.JArray"))
            {
                imports.Add("JToken");
                return $"JToken.As({source}, {target})";
            }
            return binary.IsKind(SyntaxKind.IsExpression) ? $"({source} instanceof {target})"
                : $"((__unityCastValue) => __unityCastValue instanceof {target} ? __unityCastValue : null)({source})";
        }
        // UnityEngine.Object converts implicitly to bool. C# often writes
        // GetComponent<T>() == false to test for a missing or destroyed object.
        // JavaScript returns null for a missing component, so strict equality
        // with false would silently skip the Unity branch.
        if (binary.IsKind(SyntaxKind.EqualsExpression) || binary.IsKind(SyntaxKind.NotEqualsExpression))
        {
            var objectSide = binary.Left;
            var boolSide = binary.Right;
            if (boolSide.IsKind(SyntaxKind.TrueLiteralExpression)
                || boolSide.IsKind(SyntaxKind.FalseLiteralExpression))
            {
                if (IsUnityObject(model.GetTypeInfo(objectSide).Type))
                {
                    imports.Add("Object");
                    bool wantLive = boolSide.IsKind(SyntaxKind.TrueLiteralExpression)
                        == binary.IsKind(SyntaxKind.EqualsExpression);
                    return $"Object.{(wantLive ? "op_Inequality" : "op_Equality")}({Expression(objectSide)}, null)";
                }
            }
            objectSide = binary.Right;
            boolSide = binary.Left;
            if ((boolSide.IsKind(SyntaxKind.TrueLiteralExpression)
                || boolSide.IsKind(SyntaxKind.FalseLiteralExpression))
                && IsUnityObject(model.GetTypeInfo(objectSide).Type))
            {
                imports.Add("Object");
                bool wantLive = boolSide.IsKind(SyntaxKind.TrueLiteralExpression)
                    == binary.IsKind(SyntaxKind.EqualsExpression);
                return $"Object.{(wantLive ? "op_Inequality" : "op_Equality")}({Expression(objectSide)}, null)";
            }
        }
        if (binary.IsKind(SyntaxKind.LogicalAndExpression)
            || binary.IsKind(SyntaxKind.LogicalOrExpression))
        {
            if (IsUnityObject(model.GetTypeInfo(binary.Left).Type)
                || IsUnityObject(model.GetTypeInfo(binary.Right).Type))
                return $"({BooleanCondition(binary.Left)} {binary.OperatorToken.Text} "
                    + $"{BooleanCondition(binary.Right)})";
        }
        string left = ApplyUserDefinedConversion(binary.Left, Expression(binary.Left),
            model.GetTypeInfo(binary.Left).ConvertedType);
        string right = ApplyUserDefinedConversion(binary.Right, Expression(binary.Right),
            model.GetTypeInfo(binary.Right).ConvertedType);
        if (binary.IsKind(SyntaxKind.AddExpression)
            && model.GetTypeInfo(binary).Type?.SpecialType == SpecialType.System_String)
        {
            if (model.GetTypeInfo(binary.Left).Type is { TypeKind: TypeKind.Enum } leftEnum)
            {
                imports.Add("CSharpEnum");
                left = $"CSharpEnum.ToString({RuntimeTypeName(leftEnum, binary)}, {left})";
            }
            if (model.GetTypeInfo(binary.Right).Type is { TypeKind: TypeKind.Enum } rightEnum)
            {
                imports.Add("CSharpEnum");
                right = $"CSharpEnum.ToString({RuntimeTypeName(rightEnum, binary)}, {right})";
            }
        }
        if ((binary.IsKind(SyntaxKind.EqualsExpression)
                || binary.IsKind(SyntaxKind.NotEqualsExpression))
            && (model.GetTypeInfo(binary.Left).Type?.ToDisplayString() == "UnityEngine.LayerMask"
                || model.GetTypeInfo(binary.Right).Type?.ToDisplayString() == "UnityEngine.LayerMask"))
            return $"(Number({left}) {(binary.IsKind(SyntaxKind.EqualsExpression) ? "===" : "!==")} Number({right}))";
        var operation = model.GetOperation(binary) as IBinaryOperation;
        var method = operation?.OperatorMethod;
        if (method != null)
        {
            if (method.ContainingType.ToDisplayString() == "System.DateTime"
                && method.Name == "op_Subtraction"
                && operation?.RightOperand.Type?.ToDisplayString() == "System.DateTime")
            {
                imports.Add("CSharpTimeSpan");
                return $"CSharpTimeSpan.FromMilliseconds({left}.getTime() - {right}.getTime())";
            }
            if (method.ContainingType.ToDisplayString() is "UnityEngine.EntityId" or "UnityEngine.InstanceID" or "UnityEngine.Hash128" or "UnityEngine.PropertyName" or "UnityEngine.TransformHandle" or "UnityEngine.Playables.PlayableHandle" or "UnityEngine.Playables.PlayableOutputHandle" or "UnityEngine.Rendering.VertexAttributeDescriptor" or "UnityEngine.Rendering.SortingLayerRange"
                && method.Name is "op_Equality" or "op_Inequality" or "op_LessThan"
                    or "op_LessThanOrEqual" or "op_GreaterThan" or "op_GreaterThanOrEqual")
            {
                imports.Add(method.ContainingType.Name);
                return $"{method.ContainingType.Name}.{method.Name}({left}, {right})";
            }
            if (method.ContainingType.ToDisplayString() == "System.Type"
                && method.Name is ("op_Equality" or "op_Inequality"))
            {
                imports.Add("UnityType");
                string typedLeft = binary.Left is TypeOfExpressionSyntax leftTypeOf
                    ? NamedTypeExpression(leftTypeOf) : left;
                string typedRight = binary.Right is TypeOfExpressionSyntax rightTypeOf
                    ? NamedTypeExpression(rightTypeOf) : right;
                string equals = $"UnityType.Equals({typedLeft}, {typedRight})";
                return method.Name == "op_Equality" ? equals : $"(!{equals})";
            }
            if (method.ContainingType.ToDisplayString() == "System.Guid"
                && method.Name is ("op_Equality" or "op_Inequality"))
                return method.Name == "op_Equality"
                    ? $"{left}.Equals({right})" : $"(!{left}.Equals({right}))";
            if (method.ContainingType.ToDisplayString() == "UnityEngine.SceneManagement.Scene"
                && method.Name is ("op_Equality" or "op_Inequality"))
            {
                imports.Add("Scene");
                return $"Scene.{method.Name}({left}, {right})";
            }
            if (method.ContainingType.ToDisplayString() == "UnityEngine.SceneManagement.SceneHandle"
                && method.Name is ("op_Equality" or "op_Inequality"))
            {
                imports.Add("SceneHandle");
                return $"SceneHandle.{method.Name}({left}, {right})";
            }
            if (method.ContainingType.ToDisplayString() == "UnityEngine.Quaternion")
            {
                imports.Add("Quaternion");
                if (method.Name is "op_Equality" or "op_Inequality")
                    return $"Quaternion.{method.Name}({left}, {right})";
                if (method.Name != "op_Multiply")
                    throw Unsupported(binary, $"Quaternion operator {method.Name} is not mapped");
                return operation!.RightOperand.Type?.ToDisplayString() switch
                {
                    "UnityEngine.Quaternion" => $"Quaternion.Multiply({left}, {right})",
                    "UnityEngine.Vector3" => $"Quaternion.RotateVector({left}, {right})",
                    _ => throw Unsupported(binary, $"Quaternion operator {method} is not mapped"),
                };
            }
            if (method.ContainingType.ToDisplayString() == "UnityEngine.Matrix4x4"
                && method.Name == "op_Multiply")
            {
                imports.Add("Matrix4x4");
                return operation!.RightOperand.Type?.ToDisplayString() == "UnityEngine.Vector4"
                    ? $"Matrix4x4.MultiplyVector4({left}, {right})"
                    : $"Matrix4x4.Multiply({left}, {right})";
            }
            if (method.ContainingType.ToDisplayString() == "UnityEngine.Object"
                && method.Name is "op_Equality" or "op_Inequality")
            {
                imports.Add("Object");
                return $"Object.{method.Name}({left}, {right})";
            }
            if (method.ContainingType.ToDisplayString() == "UnityEngine.Color"
                && method.Name is "op_Equality" or "op_Inequality")
            {
                imports.Add("Color");
                return $"Color.{method.Name}({left}, {right})";
            }
            if (method.ContainingType.ToDisplayString() == "UnityEngine.Color")
            {
                string leftType = operation!.LeftOperand.Type?.ToDisplayString() ?? "";
                string rightType = operation.RightOperand.Type?.ToDisplayString() ?? "";
                return method.Name switch
                {
                    "op_Addition" => $"{left}.clone().add({right})",
                    "op_Subtraction" => $"{left}.clone().sub({right})",
                    "op_Multiply" when leftType == "UnityEngine.Color" && rightType == "UnityEngine.Color"
                        => $"{left}.clone().multiply({right})",
                    "op_Multiply" when rightType == "UnityEngine.Vector4"
                        => $"{left}.clone().multiplyVector({right})",
                    "op_Multiply" when leftType == "UnityEngine.Color"
                        => $"{left}.clone().multiplyScalar({right})",
                    "op_Multiply" => $"{right}.clone().multiplyScalar({left})",
                    "op_Division" => $"{left}.clone().divideScalar({right})",
                    _ => throw Unsupported(binary, $"Color operator {method.Name} is not mapped"),
                };
            }
            if (method.ContainingType.ToDisplayString() is "UnityEngine.Rect" or "UnityEngine.Bounds" or "UnityEngine.Plane" or "UnityEngine.RectInt" or "UnityEngine.BoundsInt" or "UnityEngine.Matrix4x4" or "UnityEngine.Pose" or "UnityEngine.RefreshRate" or "UnityEngine.Resolution" or "UnityEngine.BoneWeight" or "UnityEngine.BoneWeight1"
                && method.Name is "op_Equality" or "op_Inequality")
            {
                string typeName = method.ContainingType.Name;
                imports.Add(typeName);
                return $"{typeName}.{method.Name}({left}, {right})";
            }
            if (method.ContainingType.ToDisplayString() is "UnityEngine.Vector2Int" or "UnityEngine.Vector3Int")
            {
                string integerType = method.ContainingType.Name;
                imports.Add(integerType);
                return method.Name switch
                {
                    "op_Equality" or "op_Inequality" => $"{integerType}.{method.Name}({left}, {right})",
                    "op_Addition" => $"{left}.clone().add({right})",
                    "op_Subtraction" => $"{left}.clone().sub({right})",
                    "op_Multiply" when operation!.LeftOperand.Type?.ToDisplayString() == method.ContainingType.ToDisplayString()
                        && operation.RightOperand.Type?.ToDisplayString() == method.ContainingType.ToDisplayString()
                        => $"{left}.clone().multiply({right})",
                    "op_Multiply" when operation!.LeftOperand.Type?.ToDisplayString() == method.ContainingType.ToDisplayString()
                        => $"{left}.clone().multiplyScalar({right})",
                    "op_Multiply" => $"{right}.clone().multiplyScalar({left})",
                    "op_Division" => $"{left}.clone().divideScalar({right})",
                    _ => throw Unsupported(binary, $"{integerType} operator {method.Name} is not mapped"),
                };
            }
            if (method.ContainingType.ToDisplayString() == "UnityEngine.SecondarySpriteTexture"
                && method.Name is ("op_Equality" or "op_Inequality"))
            { imports.Add("SecondarySpriteTexture"); return $"SecondarySpriteTexture.{method.Name}({left}, {right})"; }
            if (method.ContainingType.ContainingNamespace.ToDisplayString() == "UnityEngine.UIElements"
                && method.ContainingType.Name is ("Length" or "StyleLength" or "StyleFloat"
                    or "StyleColor" or "StyleEnum" or "Ratio" or "StyleRatio"
                    or "BackgroundPosition" or "BackgroundRepeat" or "BackgroundSize"
                    or "StyleBackgroundPosition" or "StyleBackgroundRepeat"
                    or "StyleBackgroundSize")
                && method.Name is ("op_Equality" or "op_Inequality"))
            {
                string styleType = method.ContainingType.Name;
                imports.Add(styleType);
                return $"{styleType}.{method.Name}({left}, {right})";
            }
            if (method.ContainingType.ToDisplayString()
                == "UnityEngine.UIElements.ToggleButtonGroupState"
                && method.Name is "op_Equality" or "op_Inequality")
            {
                imports.Add("ToggleButtonGroupState");
                return $"ToggleButtonGroupState.{method.Name}({left}, {right})";
            }
            if (method.ContainingType.OriginalDefinition.ToDisplayString()
                == "UnityEngine.UIElements.CustomStyleProperty<T>"
                && method.Name is "op_Equality" or "op_Inequality")
            {
                imports.Add("CustomStyleProperty");
                return $"CustomStyleProperty.{method.Name}({left}, {right})";
            }
            if (method.ContainingType.ToDisplayString()
                == "UnityEngine.UIElements.VisualElementStyleSheetSet"
                && method.Name is "op_Equality" or "op_Inequality")
            {
                imports.Add("VisualElementStyleSheetSet");
                return $"VisualElementStyleSheetSet.{method.Name}({left}, {right})";
            }
            if (method.ContainingType.ToDisplayString()
                == "UnityEngine.UIElements.TimerState"
                && method.Name is "op_Equality" or "op_Inequality")
            {
                imports.Add("TimerState");
                return $"TimerState.{method.Name}({left}, {right})";
            }
            if (method.ContainingType.ToDisplayString()
                == "UnityEngine.UIElements.ManipulatorActivationFilter"
                && method.Name is "op_Equality" or "op_Inequality")
            {
                imports.Add("ManipulatorActivationFilter");
                return $"ManipulatorActivationFilter.{method.Name}({left}, {right})";
            }
            if (method.ContainingType.ToDisplayString()
                == "UnityEngine.UIElements.VisualElement.Hierarchy"
                && method.Name is "op_Equality" or "op_Inequality")
            {
                imports.Add("VisualElementHierarchy");
                return $"VisualElementHierarchy.{method.Name}({left}, {right})";
            }
            if (method.ContainingType.OriginalDefinition.ToDisplayString()
                == "UnityEngine.UIElements.UQueryState<T>"
                && method.Name is "op_Equality" or "op_Inequality")
            {
                imports.Add("UQueryState");
                return $"UQueryState.{method.Name}({left}, {right})";
            }
            if (method.ContainingType.OriginalDefinition.ToDisplayString()
                == "UnityEngine.UIElements.UQueryBuilder<T>"
                && method.Name is "op_Equality" or "op_Inequality")
            {
                imports.Add("UQueryBuilder");
                return $"UQueryBuilder.{method.Name}({left}, {right})";
            }
            if (method.ContainingType.ToDisplayString() is "System.Reflection.PropertyInfo"
                or "System.Reflection.MethodInfo" or "System.Reflection.FieldInfo"
                && method.Name is "op_Equality" or "op_Inequality")
                return $"({left} {(method.Name == "op_Equality" ? "===" : "!==")} {right})";
            if (method.ContainingType.ToDisplayString() == "UnityEngine.InputSystem.InputControlScheme"
                && method.Name is "op_Equality" or "op_Inequality"
                && (binary.Left.IsKind(SyntaxKind.NullLiteralExpression)
                    || binary.Right.IsKind(SyntaxKind.NullLiteralExpression)))
                return $"({left} {(method.Name == "op_Equality" ? "==" : "!=")} {right})";
            if (method.ContainingType.ToDisplayString() is not ("UnityEngine.Vector2" or "UnityEngine.Vector3" or "UnityEngine.Vector4"))
                throw Unsupported(binary, $"Operator {method} has no JavaScript lowering");
            if (method.Name is "op_Equality" or "op_Inequality")
            {
                string vectorType = method.ContainingType.Name;
                imports.Add(vectorType);
                return $"{vectorType}.{method.Name}({left}, {right})";
            }
            return method.Name switch
            {
                "op_Addition" => $"{left}.clone().add({right})",
                "op_Subtraction" => $"{left}.clone().sub({right})",
                "op_Multiply" when operation!.LeftOperand.Type?.ToDisplayString() == "UnityEngine.Vector2"
                    && operation.RightOperand.Type?.ToDisplayString() == "UnityEngine.Vector2"
                    => $"{left}.clone().multiply({right})",
                "op_Multiply" when operation!.LeftOperand.Type?.ToDisplayString() is "UnityEngine.Vector2" or "UnityEngine.Vector3" or "UnityEngine.Vector4"
                    => $"{left}.clone().multiplyScalar({right})",
                "op_Multiply" => $"{right}.clone().multiplyScalar({left})",
                "op_Division" when operation!.RightOperand.Type?.ToDisplayString() == "UnityEngine.Vector2"
                    => $"{left}.clone().divide({right})",
                "op_Division" => $"{left}.clone().divideScalar({right})",
                _ => throw Unsupported(binary, $"Vector operator {method.Name} is not mapped"),
            };
        }
        if (binary.IsKind(SyntaxKind.DivideExpression)
            && operation?.LeftOperand.Type?.SpecialType == SpecialType.System_Int32
            && operation.RightOperand.Type?.SpecialType == SpecialType.System_Int32)
            return $"Math.trunc({left} / {right})";
        if (binary.IsKind(SyntaxKind.EqualsExpression) || binary.IsKind(SyntaxKind.NotEqualsExpression))
        {
            if (IsUnityObject(operation?.LeftOperand.Type) || IsUnityObject(operation?.RightOperand.Type))
            {
                imports.Add("Object");
                return $"Object.{(binary.IsKind(SyntaxKind.EqualsExpression) ? "op_Equality" : "op_Inequality")}({left}, {right})";
            }
        }
        string op = binary.OperatorToken.Text;
        if (op is "|" or "&" or "^" or "<<" or ">>")
        {
            if (operation?.LeftOperand.Type?.SpecialType == SpecialType.System_Boolean
                && op is ("|" or "&" or "^"))
                return $"Boolean({left} {op} {right})";
            if (operation?.LeftOperand.Type?.SpecialType != SpecialType.System_Int32
                && operation?.LeftOperand.Type?.TypeKind != TypeKind.Enum)
                throw Unsupported(binary, $"Bitwise operator {op} requires an int or enum");
            return $"({left} {op} {right})";
        }
        if (op is not ("+" or "-" or "*" or "/" or "%" or ">" or "<" or ">="
            or "<=" or "==" or "!=" or "&&" or "||" or "??"))
            throw Unsupported(binary, $"Operator {op} is not mapped");
        if (op == "==") op = "===";
        if (op == "!=") op = "!==";
        return $"({left} {op} {right})";
    }

    private static bool IsUnityObject(ITypeSymbol? type)
    {
        for (var current = type as INamedTypeSymbol; current != null; current = current.BaseType)
            if (current.ToDisplayString() == "UnityEngine.Object") return true;
        return false;
    }

    private string Assignment(AssignmentExpressionSyntax assignment)
    {
        if (assignment.IsKind(SyntaxKind.SimpleAssignmentExpression)
            && assignment.Left is ThisExpressionSyntax
            && model.GetEnclosingSymbol(assignment.SpanStart)?.ContainingType?.IsValueType == true)
            return $"globalThis.Object.assign(this, {Expression(assignment.Right)})";
        if (assignment.IsKind(SyntaxKind.SimpleAssignmentExpression)
            && assignment.Left is ElementAccessExpressionSyntax fromEndElement
            && model.GetTypeInfo(fromEndElement.Expression).Type is IArrayTypeSymbol
            && fromEndElement.ArgumentList.Arguments is { Count: 1 }
            && fromEndElement.ArgumentList.Arguments[0].Expression is PrefixUnaryExpressionSyntax fromEnd
            && fromEnd.IsKind(SyntaxKind.IndexExpression))
            return $"((__unityIndexed) => (__unityIndexed[__unityIndexed.length - {Expression(fromEnd.Operand)}]"
                + $" = {Expression(assignment.Right)}))({Expression(fromEndElement.Expression)})";
        if (assignment.IsKind(SyntaxKind.SimpleAssignmentExpression)
            && assignment.Left is TupleExpressionSyntax leftTuple
            && assignment.Right is TupleExpressionSyntax rightTuple
            && leftTuple.Arguments.Count == rightTuple.Arguments.Count)
        {
            int id = refCounter++;
            var reads = rightTuple.Arguments.Select((argument, index) =>
                $"const __unityTuple{id}_{index} = {Expression(argument.Expression)};");
            var writes = leftTuple.Arguments.Select((argument, index) =>
                $"{Expression(argument.Expression)} = "
                + ConvertImplicitValue(rightTuple.Arguments[index].Expression,
                    CopyStruct(rightTuple.Arguments[index].Expression, $"__unityTuple{id}_{index}"),
                    model.GetTypeInfo(argument.Expression).Type) + ";");
            return $"(() => {{ {string.Join(" ", reads)} {string.Join(" ", writes)} "
                + $"return [{string.Join(", ", Enumerable.Range(0, leftTuple.Arguments.Count).Select(index => $"__unityTuple{id}_{index}"))}]; }})()";
        }
        if (assignment.IsKind(SyntaxKind.SimpleAssignmentExpression)
            && assignment.Left is MemberAccessExpressionSyntax materialColorAccess
            && model.GetSymbolInfo(materialColorAccess).Symbol is IPropertySymbol { Name: "color" } materialColor
            && materialColor.ContainingType.ToDisplayString() == "UnityEngine.Material")
            return $"{Expression(materialColorAccess.Expression)}.SetColor(\"_Color\", "
                + $"{CopyStruct(assignment.Right, Expression(assignment.Right))})";
        if (assignment.IsKind(SyntaxKind.CoalesceAssignmentExpression))
            return $"({Expression(assignment.Left)} ??= {Expression(assignment.Right)})";
        if (assignment.Left is ElementAccessExpressionSyntax jsonElement
            && model.GetTypeInfo(jsonElement.Expression).Type?.ToDisplayString() == "Newtonsoft.Json.Linq.JObject"
            && assignment.IsKind(SyntaxKind.SimpleAssignmentExpression)
            && jsonElement.ArgumentList.Arguments.Count == 1)
            return $"{Expression(jsonElement.Expression)}.SetItem({Expression(jsonElement.ArgumentList.Arguments[0].Expression)}, {Expression(assignment.Right)})";
        if (assignment.Left is ElementAccessExpressionSyntax toggleStateElement
            && model.GetTypeInfo(toggleStateElement.Expression).Type?.ToDisplayString() == "UnityEngine.UIElements.ToggleButtonGroupState"
            && assignment.IsKind(SyntaxKind.SimpleAssignmentExpression)
            && toggleStateElement.ArgumentList.Arguments.Count == 1)
            return $"{Expression(toggleStateElement.Expression)}.set_Item({Expression(toggleStateElement.ArgumentList.Arguments[0].Expression)}, {Expression(assignment.Right)})";
        if (!assignment.IsKind(SyntaxKind.SimpleAssignmentExpression))
        {
            string op = assignment.OperatorToken.Text;
            if (assignment.Left is MemberAccessExpressionSyntax inputEvent
                && model.GetTypeInfo(inputEvent.Expression).Type?.ToDisplayString()
                    == "UnityEngine.InputSystem.InputSystem"
                && inputEvent.Name.Identifier.ValueText == "onEvent" && op is "+=" or "-=")
            {
                string callback = Expression(assignment.Right);
                if (model.GetSymbolInfo(assignment.Right).Symbol is IMethodSymbol inputMethod)
                {
                    string receiver = assignment.Right is MemberAccessExpressionSyntax member
                        ? Expression(member.Expression) : inputMethod.IsStatic ? inputMethod.ContainingType.Name : "this";
                    imports.Add("UnityEvent");
                    callback = $"UnityEvent.Bind({receiver}, {JsonSerializer.Serialize(MethodName(inputMethod))})";
                }
                return $"{Expression(assignment.Left)}.{(op == "+=" ? "AddListener" : "RemoveListener")}({callback})";
            }
            var eventSymbol = model.GetSymbolInfo(assignment.Left).Symbol as IEventSymbol;
            if ((eventSymbol?.ContainingType.ToDisplayString() == "UnityEngine.InputSystem.InputAction"
                    && eventSymbol.Name is "started" or "performed" or "canceled"
                || eventSymbol?.ContainingType.ToDisplayString() == "UnityEngine.InputSystem.PlayerInput"
                    && eventSymbol.Name == "onActionTriggered")
                && op is "+=" or "-=")
            {
                string callback = Expression(assignment.Right);
                if (model.GetSymbolInfo(assignment.Right).Symbol is IMethodSymbol method)
                {
                    string receiver = assignment.Right is MemberAccessExpressionSyntax member
                        ? Expression(member.Expression) : method.IsStatic ? method.ContainingType.Name : "this";
                    imports.Add("UnityEvent");
                    callback = $"UnityEvent.Bind({receiver}, {JsonSerializer.Serialize(MethodName(method))})";
                }
                return $"{Expression(assignment.Left)}.{(op == "+=" ? "AddListener" : "RemoveListener")}({callback})";
            }
            var type = model.GetTypeInfo(assignment.Left).Type;
            if (type?.SpecialType == SpecialType.System_UInt64
                && op is ("^=" or "|=" or "&=" or "*=" or "+=" or "-="))
            {
                string target = Expression(assignment.Left);
                string value = Expression(assignment.Right);
                return $"({target} = BigInt.asUintN(64, BigInt({target}) {op[..1]} BigInt({value})))";
            }
            if (type?.SpecialType == SpecialType.System_Int32 && op is ("|=" or "&=" or "^="))
                return $"({Expression(assignment.Left)} {op} {Expression(assignment.Right)})";
            if (type?.ToDisplayString() == "UnityEngine.LayerMask" && op is ("|=" or "&=" or "^="))
            {
                imports.Add("LayerMask");
                string target = Expression(assignment.Left);
                return $"({target} = new LayerMask({target}.value {op[..1]} {Expression(assignment.Right)}))";
            }
            if (op is "+=" or "-=" && (eventSymbol != null || type?.TypeKind == TypeKind.Delegate))
            {
                string callback = Expression(assignment.Right);
                if (model.GetSymbolInfo(assignment.Right).Symbol is IMethodSymbol method)
                {
                    string receiver = assignment.Right is MemberAccessExpressionSyntax member
                        ? Expression(member.Expression) : method.IsStatic ? method.ContainingType.Name : "this";
                    imports.Add("UnityEvent");
                    callback = $"UnityEvent.Bind({receiver}, {JsonSerializer.Serialize(MethodName(method))})";
                }
                string eventTarget = Expression(assignment.Left);
                if (eventSymbol == null && type?.TypeKind == TypeKind.Delegate)
                {
                    imports.Add("UnityEvent");
                    if (assignment.Left is ElementAccessExpressionSyntax delegateElement
                        && delegateElement.ArgumentList.Arguments.Count == 1
                        && IsDictionary(model.GetTypeInfo(delegateElement.Expression).Type))
                    {
                        string receiver = Expression(delegateElement.Expression);
                        string key = Expression(delegateElement.ArgumentList.Arguments[0].Expression);
                        return op == "+="
                            ? $"((__unityEvents, __unityKey) => {{ let __unityEvent = __unityEvents.GetItem(__unityKey); "
                                + "if (__unityEvent == null) { __unityEvent = new UnityEvent(); "
                                + "__unityEvents.SetItem(__unityKey, __unityEvent); } "
                                + $"__unityEvent.AddListener({callback}); }} )({receiver}, {key})"
                            : $"((__unityEvents, __unityKey) => __unityEvents.GetItem(__unityKey)?.RemoveListener({callback}))"
                                + $"({receiver}, {key})";
                    }
                    return op == "+="
                        ? $"({eventTarget} ??= new UnityEvent()).AddListener({callback})"
                        : $"{eventTarget}?.RemoveListener({callback})";
                }
                return $"{eventTarget}.{(op == "+=" ? "AddListener" : "RemoveListener")}({callback})";
            }
            if (type?.ToDisplayString() is "UnityEngine.Vector2" or "UnityEngine.Vector3" or "UnityEngine.Vector4"
                && op is "+=" or "-=" or "*=" or "/=")
            {
                if (assignment.Left is not (IdentifierNameSyntax or MemberAccessExpressionSyntax))
                    throw Unsupported(assignment, "Vector compound assignment target needs single-evaluation lowering");
                string method = op switch { "+=" => "add", "-=" => "sub",
                    "*=" => "multiplyScalar", _ => "divideScalar" };
                string target = Expression(assignment.Left);
                return $"({target} = {target}.clone().{method}({Expression(assignment.Right)}))";
            }
            if (type?.ToDisplayString() == "UnityEngine.Quaternion" && op == "*=")
            {
                imports.Add("Quaternion");
                return $"({Expression(assignment.Left)} = Quaternion.Multiply({Expression(assignment.Left)}, {Expression(assignment.Right)}))";
            }
            if (type?.SpecialType == SpecialType.System_Boolean && op is "|=" or "&=" or "^=")
            {
                if (assignment.Left is not (IdentifierNameSyntax or MemberAccessExpressionSyntax))
                    throw Unsupported(assignment, "Boolean compound assignment target needs single-evaluation lowering");
                string target = Expression(assignment.Left);
                return $"({target} = Boolean({target} {op[..1]} {Expression(assignment.Right)}))";
            }
            bool numeric = type?.SpecialType is SpecialType.System_Int32 or SpecialType.System_Single
                or SpecialType.System_Double;
            bool stringAdd = type?.SpecialType == SpecialType.System_String && op == "+=";
            if ((!numeric && !stringAdd) || op is not ("+=" or "-=" or "*=" or "/=" or "%="))
                throw Unsupported(assignment, "Compound assignment needs operator lowering");
            string left = Expression(assignment.Left);
            string right = numeric
                ? ConvertImplicitValue(assignment.Right, Expression(assignment.Right), type)
                : Expression(assignment.Right);
            if (op == "/=" && type?.SpecialType == SpecialType.System_Int32)
            {
                if (assignment.Left is not (IdentifierNameSyntax or MemberAccessExpressionSyntax))
                    throw Unsupported(assignment, "Integer division assignment target needs single-evaluation lowering");
                return $"({left} = Math.trunc({left} / {right}))";
            }
            return $"({left} {op} {right})";
        }
        if (assignment.Left is ElementAccessExpressionSyntax element
            && model.GetTypeInfo(element.Expression).Type?.ToDisplayString() == "UnityEngine.Matrix4x4")
            return $"{Expression(element.Expression)}.SetItem("
                + string.Join(", ", element.ArgumentList.Arguments.Select(argument => Expression(argument.Expression)))
                + $", {Expression(assignment.Right)})";
        if (assignment.Left is ElementAccessExpressionSyntax vectorElement
            && model.GetTypeInfo(vectorElement.Expression).Type?.ToDisplayString() is "UnityEngine.Vector2"
                or "UnityEngine.Vector2Int" or "UnityEngine.Vector3" or "UnityEngine.Vector3Int"
                or "UnityEngine.Vector4" or "UnityEngine.Quaternion"
                or "UnityEngine.Color" or "UnityEngine.Color32")
            return $"{Expression(vectorElement.Expression)}.SetItem("
                + $"{Expression(vectorElement.ArgumentList.Arguments[0].Expression)}, {Expression(assignment.Right)})";
        if (assignment.Left is ElementAccessExpressionSyntax listElement
            && (IsList(model.GetTypeInfo(listElement.Expression).Type)
                || IsDictionary(model.GetTypeInfo(listElement.Expression).Type)))
        {
            if (listElement.ArgumentList.Arguments.Count != 1)
                throw Unsupported(listElement, "Only single-index list assignment is supported");
            var indexer = model.GetSymbolInfo(listElement).Symbol as IPropertySymbol;
            var indexExpression = listElement.ArgumentList.Arguments[0].Expression;
            string index = ConvertImplicitValue(indexExpression, Expression(indexExpression),
                indexer?.Parameters.FirstOrDefault()?.Type);
            string value = ConvertImplicitValue(assignment.Right,
                CopyStruct(assignment.Right, Expression(assignment.Right)), indexer?.Type);
            return $"{Expression(listElement.Expression)}.SetItem({index}, {value})";
        }
        if (assignment.Left is ElementAccessExpressionSyntax customElement
            && assignment.IsKind(SyntaxKind.SimpleAssignmentExpression)
            && model.GetSymbolInfo(customElement).Symbol is IPropertySymbol { IsIndexer: true } customIndexer
            && SymbolEqualityComparer.Default.Equals(customIndexer.ContainingAssembly, model.Compilation.Assembly)
            && customElement.ArgumentList.Arguments.Count == 1)
            return $"{Expression(customElement.Expression)}.{IndexerMethodName(customIndexer, "Set")}("
                + $"{Expression(customElement.ArgumentList.Arguments[0].Expression)}, "
                + $"{ConvertImplicitValue(assignment.Right, CopyStruct(assignment.Right, Expression(assignment.Right)), customIndexer.Type)})";
        if (model.GetTypeInfo(assignment.Left).Type?.ToDisplayString() == "System.Type"
            && assignment.Right is TypeOfExpressionSyntax namedType)
            return $"{Expression(assignment.Left)} = {NamedTypeExpression(namedType)}";
        return $"{Expression(assignment.Left)} = "
            + ConvertImplicitValue(assignment.Right,
                CopyStruct(assignment.Right, Expression(assignment.Right)),
                model.GetTypeInfo(assignment.Left).Type);
    }

    private bool IsGeneratedStruct(ITypeSymbol? type) =>
        type != null && structTypes.Contains(type.ToDisplayString());

    private bool IsCopiedValueType(ITypeSymbol? type) =>
        IsGeneratedStruct(type)
        || type is INamedTypeSymbol { OriginalDefinition: { } definition }
            && definition.ToDisplayString() == "UnityEngine.ExposedReference<T>"
        || type?.ToDisplayString() is
            "UnityEngine.Vector2" or "UnityEngine.Vector2Int" or "UnityEngine.Vector3" or "UnityEngine.Vector3Int" or "UnityEngine.Vector4"
            or "UnityEngine.Quaternion" or "UnityEngine.Pose" or "UnityEngine.Matrix4x4" or "UnityEngine.Color"
            or "UnityEngine.Ray" or "UnityEngine.Ray2D" or "UnityEngine.Plane" or "UnityEngine.Bounds"
            or "UnityEngine.LayerMask" or "UnityEngine.RenderingLayerMask" or "UnityEngine.Color32"
            or "UnityEngine.EntityId" or "UnityEngine.InstanceID" or "UnityEngine.Hash128" or "UnityEngine.PropertyName"
            or "UnityEngine.GradientColorKey" or "UnityEngine.GradientAlphaKey"
            or "UnityEngine.TransformHandle"
            or "UnityEngine.Keyframe"
            or "UnityEngine.RefreshRate" or "UnityEngine.Resolution" or "UnityEngine.BoneWeight" or "UnityEngine.BoneWeight1"
            or "UnityEngine.RangeInt" or "UnityEngine.BoundingSphere" or "UnityEngine.MeshLodRange"
            or "UnityEngine.MipmapLimitDescriptor"
            or "UnityEngine.InstantiateParameters"
            or "UnityEngine.Rect" or "UnityEngine.RectInt" or "UnityEngine.BoundsInt"
            or "UnityEngine.SceneManagement.SceneHandle"
            or "UnityEngine.SceneManagement.LoadSceneParameters"
            or "UnityEngine.SceneManagement.CreateSceneParameters";

    private string CopyStruct(ExpressionSyntax source, string expression) =>
        IsCopiedValueType(model.GetTypeInfo(source).Type)
            ? $"({expression}).clone()" : expression;

    private static Exception Unsupported(SyntaxNode node, string message)
    {
        var location = node.GetLocation().GetLineSpan();
        return new InvalidOperationException($"{location.Path}:{location.StartLinePosition.Line + 1}:"
            + $"{location.StartLinePosition.Character + 1}: {message}");
    }
}
