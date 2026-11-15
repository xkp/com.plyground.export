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
        Console.Error.WriteLine(error.Message);
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

if (args.Length >= 4 && args[0] is "--runtime-default-project" or "--scan-runtime-default-project")
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
            scan: args[0] == "--scan-runtime-default-project");
        return 0;
    }
    catch (Exception error)
    {
        Console.Error.WriteLine(error.Message);
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
        + "       UnityTranspiler --scan-runtime-default-project <Unity project directory> <report.json> <runtime import specifier> --root <full C# type name>...");
    return 2;
}

try
{
    string sourcePath = Path.GetFullPath(args[0]);
    string outputPath = Path.GetFullPath(args[1]);
    string managedPath = Path.GetFullPath(args[2]);
    var tree = CSharpSyntaxTree.ParseText(File.ReadAllText(sourcePath), path: sourcePath);
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
    bool editorMode = false, bool allowExternalTypes = false)
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
        var counts = groups.Keys.GroupBy(name => name.Split('.').Last(), StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
        emittedNames = groups.Keys.ToDictionary(name => name,
            name => counts[name.Split('.').Last()] == 1 ? name.Split('.').Last()
                : "Unity_" + string.Concat(name.Select(character => char.IsLetterOrDigit(character)
                    ? character : '_')), StringComparer.Ordinal);
        foreach (var group in groups.Values.OrderBy(group => InheritanceDepth(group[0], modelForTree)))
        {
            model = modelForTree(group[0].SyntaxTree);
            TryEmit(() =>
            {
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
        else if (baseType?.ToDisplayString() == "UnityEngine.StateMachineBehaviour")
        {
            imports.Add("StateMachineBehaviour");
            baseClause = " extends StateMachineBehaviour";
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
                        == "UnityEngine.SerializeField"))
                .Select(field => field!.Name);
        }).Distinct().ToArray();
        if (serializedPrivateFields.Length > 0)
            body.AppendLine($"  static __unitySerializedFields = {JsonSerializer.Serialize(serializedPrivateFields)};");
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
            .Where(member => editorMode || !EditorCodePolicy.IsEditorOnlyMember(member))).ToList();
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
                        case ConstructorDeclarationSyntax: break;
                        case MethodDeclarationSyntax method: EmitMethod(method); break;
                        case BaseTypeDeclarationSyntax: break; // Emitted separately if reachable.
                        default: throw Unsupported(member, $"Unsupported class member {member.Kind()}");
                    }
                });
            }
        }
        if (isStruct)
            body.AppendLine($"  clone() {{ return globalThis.Object.assign(globalThis.Object.create({EmittedName(symbol)}.prototype), this); }}");
        body.AppendLine("}");
        if (overloadConstructors)
        {
            var defaultConstructor = ConstructorOverloads(symbol).FirstOrDefault(candidate => candidate.Parameters.Length == 0)
                ?? ConstructorOverloads(symbol).FirstOrDefault(candidate => candidate.Parameters.All(parameter => parameter.IsOptional));
            if (defaultConstructor != null)
                body.AppendLine($"{EmittedName(symbol)}.__unityDefaultConstructorId = {ConstructorId(defaultConstructor)};");
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
                ? $"new {EmittedName(serializableType!)}()"
                : variable.Initializer == null && symbol?.Type.TypeKind == TypeKind.Delegate
                ? "new UnityEvent()" : variable.Initializer == null ? DefaultValue(symbol?.Type)
                : CopyStruct(variable.Initializer.Value, Expression(variable.Initializer.Value));
            if (variable.Initializer == null && symbol?.Type.TypeKind == TypeKind.Delegate)
                imports.Add("UnityEvent");
            body.AppendLine($"  {(isStatic ? "static " : "")}{variable.Identifier.ValueText} = {value};");
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
                : CopyStruct(property.Initializer.Value, Expression(property.Initializer.Value));
            body.AppendLine($"  {prefix}{backing} = {value};");
        }
        if (property.ExpressionBody != null)
        {
            string value = ConvertImplicitValue(property.ExpressionBody.Expression,
                Expression(property.ExpressionBody.Expression), symbol.Type);
            body.AppendLine($"  {prefix}get {name}() {{ return {value}; }}");
            return;
        }
        if (property.AccessorList == null)
            throw Unsupported(property, "Property has no supported accessor body");
        foreach (var accessor in property.AccessorList.Accessors)
        {
            string kind = accessor.IsKind(SyntaxKind.GetAccessorDeclaration) ? "get"
                : accessor.IsKind(SyntaxKind.SetAccessorDeclaration) ? "set"
                : throw Unsupported(accessor, "Property accessor is not mapped");
            body.AppendLine($"  {prefix}{kind} {name}({(kind == "set" ? "value" : "")}) {{");
            if (accessor.ExpressionBody != null)
            {
                string expression = Expression(accessor.ExpressionBody.Expression);
                if (kind == "get") expression = ConvertImplicitValue(accessor.ExpressionBody.Expression,
                    expression, symbol.Type);
                body.AppendLine(kind == "get" ? $"    return {expression};" : $"    {expression};");
            }
            else if (accessor.Body != null)
            {
                foreach (var statement in accessor.Body.Statements) TryEmit(() => Statement(statement, 2));
            }
            else if (auto)
                body.AppendLine(kind == "get" ? $"    return this.{backing};"
                    : $"    this.{backing} = value;");
            body.AppendLine("  }");
        }
        if (auto && !property.AccessorList.Accessors.Any(accessor =>
            accessor.IsKind(SyntaxKind.SetAccessorDeclaration)))
            body.AppendLine($"  {prefix}set {name}(value) {{ this.{backing} = value; }}");
    }

    private string DefaultValue(ITypeSymbol? type)
    {
        for (var current = type as INamedTypeSymbol; current != null; current = current.BaseType)
            if (current.ToDisplayString().StartsWith("UnityEngine.Events.UnityEvent", StringComparison.Ordinal))
            {
                imports.Add("UnityEvent");
                return "new UnityEvent()";
            }
        if (type?.SpecialType == SpecialType.System_Boolean) return "false";
        if (type?.SpecialType is SpecialType.System_Int32 or SpecialType.System_Single
            or SpecialType.System_Double) return "0";
        if (type?.TypeKind == TypeKind.Enum) return "0";
        if (type?.ToDisplayString() == "UnityEngine.RaycastHit")
        {
            imports.Add("RaycastHit");
            return "new RaycastHit()";
        }
        if (type?.ToDisplayString() is "UnityEngine.Matrix4x4" or "UnityEngine.Plane" or "UnityEngine.Bounds" or "UnityEngine.Ray")
        {
            imports.Add(type.Name);
            return $"new {type.Name}()";
        }
        if (type?.ToDisplayString() == "UnityEngine.LayerMask")
        {
            imports.Add("LayerMask");
            return "new LayerMask()";
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
        if (type != null && structTypes.Contains(type.ToDisplayString())) return $"new {EmittedName(type)}()";
        return "null";
    }

    private static List<IMethodSymbol> ConstructorOverloads(INamedTypeSymbol type) =>
        type.InstanceConstructors.Where(candidate => !candidate.IsImplicitlyDeclared)
            .OrderBy(candidate => candidate.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                StringComparer.Ordinal).ToList();

    private static int ConstructorId(IMethodSymbol constructor)
    {
        var overloads = ConstructorOverloads(constructor.ContainingType);
        int index = overloads.FindIndex(candidate =>
            SymbolEqualityComparer.Default.Equals(candidate, constructor.OriginalDefinition));
        if (index < 0) throw new InvalidOperationException($"Constructor overload could not be resolved: {constructor}");
        return index;
    }

    private void EmitConstructor(ConstructorDeclarationSyntax constructor, bool hasBase, bool dispatch)
    {
        if (constructor.Modifiers.Any(SyntaxKind.StaticKeyword))
            throw Unsupported(constructor, "Static constructors need initialization lowering");
        if (constructor.Body == null)
            throw Unsupported(constructor, "Expression-bodied constructors are not supported");
        if (constructor.Initializer?.IsKind(SyntaxKind.ThisConstructorInitializer) == true)
            throw Unsupported(constructor.Initializer, "Chained this-constructor calls need dispatch lowering");
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
        if (hasBase)
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
            body.AppendLine($"{(dispatch ? "        " : "    ")}super({string.Join(", ", baseArguments)});");
        }
        else if (constructor.Initializer != null)
            throw Unsupported(constructor.Initializer, "Constructor initializer has no emitted base type");
        EmitPatternVariables(constructor.Body, dispatch ? 4 : 2);
        foreach (var statement in constructor.Body.Statements)
            TryEmit(() => Statement(statement, dispatch ? 4 : 2));
        if (dispatch) body.AppendLine("        break;\n      }");
        else body.AppendLine("  }");
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
        string value = parameter.Identifier.ValueText;
        if (parameter.Default == null) return value;
        var symbol = model.GetDeclaredSymbol(parameter) as IParameterSymbol;
        if (symbol?.HasExplicitDefaultValue != true)
            throw Unsupported(parameter, "Optional parameter default is not a compile-time constant");
        string defaultValue = parameter.Default.Value is DefaultExpressionSyntax
            || parameter.Default.Value.IsKind(SyntaxKind.DefaultLiteralExpression)
            ? DefaultValue(symbol.Type) : ConstantValue(symbol.ExplicitDefaultValue, parameter);
        return $"{value} = {defaultValue}";
    }

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
            foreach (var statement in method.Body.Statements) TryEmit(() => Statement(statement, 2));
        }
        else if (method.ExpressionBody != null)
        {
            string value = Expression(method.ExpressionBody.Expression);
            body.AppendLine(symbol.ReturnsVoid ? $"    {value};"
                : $"    return {CopyStruct(method.ExpressionBody.Expression, value)};");
        }
        else body.AppendLine($"    throw new Error({JsonSerializer.Serialize($"Abstract method {symbol.Name} must be overridden")});");
        body.AppendLine("  }");
    }

    private void EmitPatternVariables(BlockSyntax block, int depth)
    {
        string pad = new(' ', depth * 2);
        var names = block.DescendantNodes().OfType<SingleVariableDesignationSyntax>()
            .Select(item => item.Identifier.ValueText).Distinct(StringComparer.Ordinal);
        foreach (var name in names) body.AppendLine($"{pad}let {name};");
    }

    private string MethodName(IMethodSymbol method)
    {
        if (!emittedTypes.Contains(method.ContainingType.ToDisplayString())) return method.Name;
        var overloads = method.ContainingType.GetMembers(method.Name).OfType<IMethodSymbol>()
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

    private void Statement(StatementSyntax statement, int depth)
    {
        string pad = new(' ', depth * 2);
        switch (statement)
        {
            case BlockSyntax block:
                foreach (var child in block.Statements) TryEmit(() => Statement(child, depth));
                break;
            case LocalDeclarationStatementSyntax local:
                foreach (var variable in local.Declaration.Variables)
                {
                    body.AppendLine(variable.Initializer == null
                        ? $"{pad}let {variable.Identifier.ValueText};"
                        : $"{pad}let {variable.Identifier.ValueText} = "
                            + $"{CopyStruct(variable.Initializer.Value, Expression(variable.Initializer.Value))};");
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
                    foreach (var child in localFunction.Body.Statements) Statement(child, depth + 1);
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
                if (usingStatement.Declaration == null)
                    throw Unsupported(usingStatement, "Using expression needs lowering");
                body.AppendLine($"{pad}{{");
                foreach (var variable in usingStatement.Declaration.Variables)
                    body.AppendLine($"{pad}  const {variable.Identifier.ValueText} = {Expression(variable.Initializer!.Value)};");
                body.AppendLine($"{pad}  try {{");
                Statement(usingStatement.Statement, depth + 2);
                body.AppendLine($"{pad}  }} finally {{");
                foreach (var variable in usingStatement.Declaration.Variables.Reverse())
                    body.AppendLine($"{pad}    {variable.Identifier.ValueText}?.Dispose?.();");
                body.AppendLine($"{pad}  }}");
                body.AppendLine($"{pad}}}");
                break;
            case IfStatementSyntax conditional:
                body.AppendLine($"{pad}if ({Expression(conditional.Condition)}) {{");
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
                body.AppendLine($"{pad}for (const {item} of {Expression(loop.Expression)}) {{");
                string value = IsGeneratedStruct(model.GetTypeInfo(loop.Type).Type)
                    ? $"{item}.clone()" : item;
                body.AppendLine($"{pad}  const {loop.Identifier.ValueText} = {value};");
                Statement(loop.Statement, depth + 1);
                body.AppendLine($"{pad}}}");
                break;
            case ForStatementSyntax loop:
                string initializer = loop.Declaration == null
                    ? string.Join(", ", loop.Initializers.Select(Expression))
                    : "let " + string.Join(", ", loop.Declaration.Variables.Select(variable =>
                        variable.Identifier.ValueText + (variable.Initializer == null ? ""
                            : " = " + CopyStruct(variable.Initializer.Value, Expression(variable.Initializer.Value)))));
                string condition = loop.Condition == null ? "" : Expression(loop.Condition);
                string increment = string.Join(", ", loop.Incrementors.Select(Expression));
                body.AppendLine($"{pad}for ({initializer}; {condition}; {increment}) {{");
                Statement(loop.Statement, depth + 1);
                body.AppendLine($"{pad}}}");
                break;
            case WhileStatementSyntax loop:
                body.AppendLine($"{pad}while ({Expression(loop.Condition)}) {{");
                Statement(loop.Statement, depth + 1);
                body.AppendLine($"{pad}}}");
                break;
            case DoStatementSyntax loop:
                body.AppendLine($"{pad}do {{");
                Statement(loop.Statement, depth + 1);
                body.AppendLine($"{pad}}} while ({Expression(loop.Condition)});");
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
        AwaitExpressionSyntax awaited => $"await {Expression(awaited.Expression)}",
        IdentifierNameSyntax identifier => Identifier(identifier),
        ThisExpressionSyntax => "this",
        BaseExpressionSyntax => "super",
        MemberAccessExpressionSyntax member => MemberAccess(member),
        PredefinedTypeSyntax predefined => PredefinedType(predefined),
        ElementAccessExpressionSyntax element => ElementAccess(element),
        ArrayCreationExpressionSyntax array => ArrayCreation(array),
        ImplicitArrayCreationExpressionSyntax array => "[" + string.Join(", ",
            array.Initializer.Expressions.Select(item => CopyStruct(item, Expression(item)))) + "]",
        InitializerExpressionSyntax initializer when initializer.IsKind(SyntaxKind.ArrayInitializerExpression)
            => "[" + string.Join(", ", initializer.Expressions.Select(item =>
                CopyStruct(item, Expression(item)))) + "]",
        DefaultExpressionSyntax defaultExpression => DefaultValue(model.GetTypeInfo(defaultExpression.Type).Type),
        CastExpressionSyntax cast => Cast(cast),
        ConditionalAccessExpressionSyntax access => ConditionalAccess(access),
        InterpolatedStringExpressionSyntax interpolated => InterpolatedString(interpolated),
        InvocationExpressionSyntax invocation => Invocation(invocation),
        ObjectCreationExpressionSyntax creation => Creation(creation),
        ImplicitObjectCreationExpressionSyntax creation => ImplicitCreation(creation),
        BinaryExpressionSyntax binary => Binary(binary),
        IsPatternExpressionSyntax pattern => IsPattern(pattern),
        TypeOfExpressionSyntax typeOf => TypeOf(typeOf),
        AssignmentExpressionSyntax assignment => Assignment(assignment),
        PrefixUnaryExpressionSyntax unary => PrefixUnary(unary),
        PostfixUnaryExpressionSyntax unary => $"({Expression(unary.Operand)}{unary.OperatorToken.Text})",
        ParenthesizedExpressionSyntax parenthesized => $"({Expression(parenthesized.Expression)})",
        ConditionalExpressionSyntax conditional => $"({Expression(conditional.Condition)} ? "
            + $"{Expression(conditional.WhenTrue)} : {Expression(conditional.WhenFalse)})",
        ParenthesizedLambdaExpressionSyntax lambda => Lambda(
            lambda.ParameterList.Parameters.Select(parameter => parameter.Identifier.ValueText), lambda.Body),
        SimpleLambdaExpressionSyntax lambda => Lambda([lambda.Parameter.Identifier.ValueText], lambda.Body),
        _ => throw Unsupported(expression, $"Unsupported expression {expression.Kind()}"),
    };

    private string PrefixUnary(PrefixUnaryExpressionSyntax unary)
    {
        var type = model.GetTypeInfo(unary.Operand).Type?.ToDisplayString();
        if (unary.IsKind(SyntaxKind.UnaryMinusExpression)
            && type is "UnityEngine.Vector2" or "UnityEngine.Vector3" or "UnityEngine.Vector4")
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
        return Convert.ToString(literal.Token.Value, CultureInfo.InvariantCulture)
            ?? throw Unsupported(literal, "Literal value could not be emitted");
    }

    private string Identifier(IdentifierNameSyntax identifier)
    {
        var symbol = model.GetSymbolInfo(identifier).Symbol;
        if (symbol is INamedTypeSymbol type)
        {
            if (type.ContainingNamespace.ToDisplayString() is "UnityEngine" or "UnityEngine.Events" or "UnityEngine.AI"
                or "UnityEngine.UI"
                || type.ToDisplayString() is "UnityEngine.InputSystem.InputAction"
                    or "UnityEngine.InputSystem.InputActionType" or "UnityEngine.InputSystem.InputActionPhase")
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
            return identifier.Identifier.ValueText + ".value";
        if (symbol is ILocalSymbol or IParameterSymbol) return identifier.Identifier.ValueText;
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
        if (member.Expression is TypeOfExpressionSyntax && member.Name.Identifier.ValueText == "Name")
        {
            imports.Add("UnityType");
            return $"UnityType.Name({Expression(member.Expression)})";
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
            return ConstantValue(externalEnum.ConstantValue, member);
        if (symbol is INamedTypeSymbol type)
            return RuntimeTypeName(type, member);
        if (symbol is IMethodSymbol method && emittedTypes.Contains(method.ContainingType.ToDisplayString()))
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
            SpecialType.System_String => "String",
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
        if (IsReadOnlyArray(target) && model.GetTypeInfo(source).Type is IArrayTypeSymbol)
        {
            imports.Add("ReadOnlyArray");
            return $"new ReadOnlyArray({value})";
        }
        return value;
    }

    private string ElementAccess(ElementAccessExpressionSyntax element)
    {
        var type = model.GetTypeInfo(element.Expression).Type;
        if (type?.ToDisplayString() == "UnityEngine.Matrix4x4")
        {
            if (element.ArgumentList.Arguments.Count is not (1 or 2))
                throw Unsupported(element, "Matrix4x4 index needs one or two integers");
            return $"{Expression(element.Expression)}.GetItem("
                + string.Join(", ", element.ArgumentList.Arguments.Select(argument => Expression(argument.Expression))) + ")";
        }
        if (type?.ToDisplayString() is "UnityEngine.Vector2" or "UnityEngine.Vector3"
            or "UnityEngine.Vector4" or "UnityEngine.Quaternion")
        {
            if (element.ArgumentList.Arguments.Count != 1)
                throw Unsupported(element, "Vector or quaternion index needs one integer");
            return $"{Expression(element.Expression)}.GetItem({Expression(element.ArgumentList.Arguments[0].Expression)})";
        }
        if (element.ArgumentList.Arguments.Count != 1)
            throw Unsupported(element, "Only single-index element access is supported");
        string target = Expression(element.Expression);
        string index = Expression(element.ArgumentList.Arguments[0].Expression);
        if (type?.ToDisplayString() is "Newtonsoft.Json.Linq.JObject" or "Newtonsoft.Json.Linq.JArray")
            return $"{target}.GetItem({index})";
        if (IsList(type) || IsDictionary(type) || IsReadOnlyArray(type))
            return $"{target}.GetItem({index})";
        if (type is IArrayTypeSymbol || type?.SpecialType == SpecialType.System_String)
            return $"{target}[{index}]";
        throw Unsupported(element, $"Indexer on {type} has no JavaScript lowering");
    }

    private string ArrayCreation(ArrayCreationExpressionSyntax array)
    {
        if (array.Type.RankSpecifiers.Count != 1 || array.Type.RankSpecifiers[0].Sizes.Count != 1)
            throw Unsupported(array, "Multidimensional arrays need lowering");
        if (array.Initializer != null)
            return "[" + string.Join(", ", array.Initializer.Expressions.Select(item =>
                CopyStruct(item, Expression(item)))) + "]";
        var size = array.Type.RankSpecifiers[0].Sizes[0];
        if (size is OmittedArraySizeExpressionSyntax)
            throw Unsupported(array, "Array size or initializer is required");
        var elementType = (model.GetTypeInfo(array.Type).Type as IArrayTypeSymbol)?.ElementType;
        string defaultValue = DefaultValue(elementType);
        return $"Array.from({{ length: {Expression(size)} }}, () => {defaultValue})";
    }

    private string Cast(CastExpressionSyntax cast)
    {
        var target = model.GetTypeInfo(cast.Type).Type;
        var source = model.GetTypeInfo(cast.Expression).Type;
        string value = Expression(cast.Expression);
        if (target?.SpecialType == SpecialType.System_Int32
            && source?.SpecialType is SpecialType.System_Single or SpecialType.System_Double)
            return $"Math.trunc({value})";
        if (target?.SpecialType is SpecialType.System_Single or SpecialType.System_Double
            && source?.SpecialType is SpecialType.System_Int32 or SpecialType.System_Single
                or SpecialType.System_Double)
            return value;
        if (target?.SpecialType == SpecialType.System_Int32 && source?.TypeKind == TypeKind.Enum)
            return value;
        if (target?.TypeKind == TypeKind.Enum && source?.SpecialType == SpecialType.System_Int32)
            return value;
        if (target is ITypeParameterSymbol && source?.SpecialType == SpecialType.System_Object)
            return value;
        if (target != null && source != null && SymbolEqualityComparer.Default.Equals(target, source))
            return value;
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
        if (tail is InvocationExpressionSyntax invocation
            && invocation.Expression is MemberBindingExpressionSyntax called)
        {
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

    private string InterpolatedString(InterpolatedStringExpressionSyntax interpolated)
    {
        var pieces = new List<string>();
        foreach (var part in interpolated.Contents)
        {
            if (part is InterpolatedStringTextSyntax text)
                pieces.Add(JsonSerializer.Serialize(text.TextToken.ValueText));
            else if (part is InterpolationSyntax value && value.AlignmentClause == null
                && value.FormatClause == null)
            {
                imports.Add("CSharpString");
                pieces.Add($"CSharpString({Expression(value.Expression)})");
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
        if (called?.ContainingType.ToDisplayString() == "UnityEngine.Random"
            && called.Name == "Range" && called.Parameters.Length == 2
            && called.Parameters[0].Type.SpecialType == SpecialType.System_Single)
        {
            imports.Add("Random");
            return $"Random.RangeFloat({string.Join(", ", invocation.ArgumentList.Arguments.Select(arg => Expression(arg.Expression)))})";
        }
        if (called?.Name == "GetType" && called.Parameters.Length == 0
            && invocation.Expression is MemberAccessExpressionSyntax getType
            && called.ContainingType.SpecialType == SpecialType.System_Object)
        {
            imports.Add("UnityType");
            return $"UnityType.Of({Expression(getType.Expression)})";
        }
        if (called?.ContainingType.ToDisplayString() == "System.Type"
            && called.Name == "IsSubclassOf" && invocation.Expression is MemberAccessExpressionSyntax subclass
            && invocation.ArgumentList.Arguments.Count == 1)
        {
            imports.Add("UnityType");
            return $"UnityType.IsSubclassOf({Expression(subclass.Expression)}, "
                + $"{Expression(invocation.ArgumentList.Arguments[0].Expression)})";
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
            imports.Add("CSharpString");
            return $"CSharpString({Expression(toString.Expression)})";
        }
        if (called?.Name == "TryGetComponent"
            && invocation.ArgumentList.Arguments.Count == 1
            && invocation.ArgumentList.Arguments[0] is { RefKindKeyword.RawKind: (int)SyntaxKind.OutKeyword,
                Expression: DeclarationExpressionSyntax output }
            && output.Designation is SingleVariableDesignationSyntax designation)
        {
            var variable = model.GetDeclaredSymbol(designation) as ILocalSymbol;
            if (variable?.Type == null) throw Unsupported(output, "TryGetComponent out type is unresolved");
            string type = RuntimeTypeName(variable.Type, output);
            string receiver = invocation.Expression is MemberAccessExpressionSyntax member
                ? Expression(member.Expression) : "this";
            return $"(({designation.Identifier.ValueText} = {receiver}.GetComponent({type})) !== null)";
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
                return $"PreviewEnumerable.{extension.Name}({string.Join(", ", linqArguments.Prepend(source))})";
            }
            if (!emittedTypes.Contains(extension.ContainingType.ToDisplayString()))
                throw Unsupported(invocation, $"Extension container {extension.ContainingType} is not emitted");
            var values = new[] { Expression(member.Expression) }
                .Concat(invocation.ArgumentList.Arguments.Select(argument => Expression(argument.Expression)));
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
            if (symbol?.ContainingType.ToDisplayString() == "UnityEngine.Object"
                && symbol.Name == "Instantiate" && symbol.TypeArguments.Length == 1)
            {
                imports.Add("Object");
                return $"Object.Instantiate({string.Join(", ", invocation.ArgumentList.Arguments.Select(arg => Expression(arg.Expression)))})";
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
        target = Expression(invocation.Expression);
        var arguments = invocation.ArgumentList.Arguments.Select((arg, index) =>
        {
            string value = Expression(arg.Expression);
            return index < called?.Parameters.Length && IsGeneratedStruct(called.Parameters[index].Type)
                ? CopyStruct(arg.Expression, value) : value;
        });
        return $"{target}({string.Join(", ", arguments)})";
    }

    private string Creation(ObjectCreationExpressionSyntax creation)
    {
        string type = TypeName(creation.Type);
        IEnumerable<string> arguments = creation.ArgumentList?.Arguments.Select(arg => Expression(arg.Expression)) ?? [];
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
            && emittedTypes.Contains(constructor.ContainingType.ToDisplayString())
            && ConstructorOverloads(constructor.ContainingType).Count > 1)
            arguments = new[] { ConstructorId(constructor).ToString(CultureInfo.InvariantCulture) }
                .Concat(arguments);
        string constructed = $"new {(type.StartsWith("UnityType.Resolve(", StringComparison.Ordinal) ? $"({type})" : type)}({string.Join(", ", arguments)})";
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
                + CopyStruct(assignment.Right, Expression(assignment.Right)) + ";");
        }
        return $"(() => {{ const __unityObject = {constructed}; {string.Join(" ", assignments)} "
            + "return __unityObject; })()";
    }

    private string ImplicitCreation(ImplicitObjectCreationExpressionSyntax creation)
    {
        var type = model.GetTypeInfo(creation).Type
            ?? throw Unsupported(creation, "Target type for implicit object creation could not be resolved");
        string target = RuntimeTypeName(type, creation);
        var arguments = creation.ArgumentList.Arguments.Select(argument => Expression(argument.Expression));
        return $"new {(target.StartsWith("UnityType.Resolve(", StringComparison.Ordinal) ? $"({target})" : target)}({string.Join(", ", arguments)})";
    }

    private string TypeName(TypeSyntax syntax)
    {
        var symbol = model.GetTypeInfo(syntax).Type;
        if (symbol == null) throw Unsupported(syntax, "Type could not be resolved");
        if (symbol is ITypeParameterSymbol) return symbol.Name;
        if (symbol.ToDisplayString() == "System.Type") { imports.Add("UnityType"); return "UnityType"; }
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
        if (symbol.ToDisplayString() == "System.Threading.Thread") { imports.Add("PreviewThread"); return "PreviewThread"; }
        if (symbol.ToDisplayString() == "UnityEngine.Profiling.Profiler") { imports.Add("Profiler"); return "Profiler"; }
        if (symbol.ToDisplayString() == "Unity.Netcode.NetworkBehaviour") { imports.Add("NetworkBehaviour"); return "NetworkBehaviour"; }
        if (IsDictionary(symbol)) { imports.Add("CSharpDictionary"); return "CSharpDictionary"; }
        if (IsReadOnlyArray(symbol)) { imports.Add("ReadOnlyArray"); return "ReadOnlyArray"; }
        if (symbol is INamedTypeSymbol set && set.OriginalDefinition.ToDisplayString() == "System.Collections.Generic.HashSet<T>")
            return "Set";
        if (symbol.ContainingNamespace.ToDisplayString() is "UnityEngine" or "UnityEngine.Events" or "UnityEngine.AI"
            or "UnityEngine.SceneManagement"
            or "UnityEngine.UI"
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
        if (IsExceptionType(symbol)) return "Error";
        if (symbol is INamedTypeSymbol queue && queue.OriginalDefinition.ToDisplayString() == "System.Collections.Generic.Queue<T>")
        {
            imports.Add("CSharpQueue");
            return "CSharpQueue";
        }
        if (symbol.ToDisplayString() == "System.Guid") { imports.Add("CSharpGuid"); return "CSharpGuid"; }
        if (symbol.ToDisplayString() == "System.Threading.Thread") { imports.Add("PreviewThread"); return "PreviewThread"; }
        if (symbol.ToDisplayString() == "UnityEngine.Profiling.Profiler") { imports.Add("Profiler"); return "Profiler"; }
        if (symbol.ToDisplayString() == "Unity.Netcode.NetworkBehaviour") { imports.Add("NetworkBehaviour"); return "NetworkBehaviour"; }
        if (symbol.ToDisplayString() == "System.Type") { imports.Add("UnityType"); return "UnityType"; }
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
        if (symbol.ContainingNamespace.ToDisplayString() is "UnityEngine" or "UnityEngine.Events" or "UnityEngine.AI"
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

    private string Binary(BinaryExpressionSyntax binary)
    {
        if (binary.IsKind(SyntaxKind.IsExpression) || binary.IsKind(SyntaxKind.AsExpression))
        {
            if (binary.Right is not TypeSyntax checkedType)
                throw Unsupported(binary, "Type test has no resolved type syntax");
            var targetType = model.GetTypeInfo(checkedType).Type;
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
        string left = Expression(binary.Left);
        string right = Expression(binary.Right);
        var operation = model.GetOperation(binary) as IBinaryOperation;
        var method = operation?.OperatorMethod;
        if (method != null)
        {
            if (method.ContainingType.ToDisplayString() == "System.Type"
                && method.Name is ("op_Equality" or "op_Inequality"))
                return $"({left} {(method.Name == "op_Equality" ? "===" : "!==")} {right})";
            if (method.ContainingType.ToDisplayString() == "System.Guid"
                && method.Name is ("op_Equality" or "op_Inequality"))
                return method.Name == "op_Equality"
                    ? $"{left}.Equals({right})" : $"(!{left}.Equals({right}))";
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
            if (method.ContainingType.ToDisplayString() == "UnityEngine.Color"
                && method.Name == "op_Multiply")
                return operation!.LeftOperand.Type?.ToDisplayString() == "UnityEngine.Color"
                    ? $"{left}.clone().multiplyScalar({right})"
                    : $"{right}.clone().multiplyScalar({left})";
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
                "op_Multiply" when operation!.LeftOperand.Type?.ToDisplayString() is "UnityEngine.Vector2" or "UnityEngine.Vector3" or "UnityEngine.Vector4"
                    => $"{left}.clone().multiplyScalar({right})",
                "op_Multiply" => $"{right}.clone().multiplyScalar({left})",
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
        if (assignment.IsKind(SyntaxKind.CoalesceAssignmentExpression))
            return $"({Expression(assignment.Left)} ??= {Expression(assignment.Right)})";
        if (assignment.Left is ElementAccessExpressionSyntax jsonElement
            && model.GetTypeInfo(jsonElement.Expression).Type?.ToDisplayString() == "Newtonsoft.Json.Linq.JObject"
            && assignment.IsKind(SyntaxKind.SimpleAssignmentExpression)
            && jsonElement.ArgumentList.Arguments.Count == 1)
            return $"{Expression(jsonElement.Expression)}.SetItem({Expression(jsonElement.ArgumentList.Arguments[0].Expression)}, {Expression(assignment.Right)})";
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
                    callback = $"UnityEvent.Bind({receiver}, {JsonSerializer.Serialize(inputMethod.Name)})";
                }
                return $"{Expression(assignment.Left)}.{(op == "+=" ? "AddListener" : "RemoveListener")}({callback})";
            }
            var eventSymbol = model.GetSymbolInfo(assignment.Left).Symbol as IEventSymbol;
            if (eventSymbol?.ContainingType.ToDisplayString() == "UnityEngine.InputSystem.InputAction"
                && eventSymbol.Name is "started" or "performed" or "canceled"
                && op is "+=" or "-=")
            {
                string callback = Expression(assignment.Right);
                if (model.GetSymbolInfo(assignment.Right).Symbol is IMethodSymbol method)
                {
                    string receiver = assignment.Right is MemberAccessExpressionSyntax member
                        ? Expression(member.Expression) : method.IsStatic ? method.ContainingType.Name : "this";
                    imports.Add("UnityEvent");
                    callback = $"UnityEvent.Bind({receiver}, {JsonSerializer.Serialize(method.Name)})";
                }
                return $"{Expression(assignment.Left)}.{(op == "+=" ? "AddListener" : "RemoveListener")}({callback})";
            }
            var type = model.GetTypeInfo(assignment.Left).Type;
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
                    callback = $"UnityEvent.Bind({receiver}, {JsonSerializer.Serialize(method.Name)})";
                }
                return $"{Expression(assignment.Left)}.{(op == "+=" ? "AddListener" : "RemoveListener")}({callback})";
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
            string right = Expression(assignment.Right);
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
                or "UnityEngine.Vector3" or "UnityEngine.Vector4" or "UnityEngine.Quaternion")
            return $"{Expression(vectorElement.Expression)}.SetItem("
                + $"{Expression(vectorElement.ArgumentList.Arguments[0].Expression)}, {Expression(assignment.Right)})";
        if (assignment.Left is ElementAccessExpressionSyntax listElement
            && (IsList(model.GetTypeInfo(listElement.Expression).Type)
                || IsDictionary(model.GetTypeInfo(listElement.Expression).Type)))
        {
            if (listElement.ArgumentList.Arguments.Count != 1)
                throw Unsupported(listElement, "Only single-index list assignment is supported");
            return $"{Expression(listElement.Expression)}.SetItem({Expression(listElement.ArgumentList.Arguments[0].Expression)}, "
                + $"{CopyStruct(assignment.Right, Expression(assignment.Right))})";
        }
        return $"{Expression(assignment.Left)} = "
            + CopyStruct(assignment.Right, Expression(assignment.Right));
    }

    private bool IsGeneratedStruct(ITypeSymbol? type) =>
        type != null && structTypes.Contains(type.ToDisplayString());

    private string CopyStruct(ExpressionSyntax source, string expression) =>
        IsGeneratedStruct(model.GetTypeInfo(source).Type)
            || model.GetTypeInfo(source).Type?.ToDisplayString() == "UnityEngine.Color"
            ? $"({expression}).clone()" : expression;

    private static Exception Unsupported(SyntaxNode node, string message)
    {
        var location = node.GetLocation().GetLineSpan();
        return new InvalidOperationException($"{location.Path}:{location.StartLinePosition.Line + 1}:"
            + $"{location.StartLinePosition.Character + 1}: {message}");
    }
}
