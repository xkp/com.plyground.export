using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

// The assembly is compiled in full; only types reachable from requested roots are emitted.
static class ProjectTypeClosure
{
    public static IReadOnlyList<BaseTypeDeclarationSyntax> Resolve(
        Compilation compilation, IReadOnlyList<string> roots, bool editorMode = false)
    {
        var declarations = new Dictionary<string, List<BaseTypeDeclarationSyntax>>(StringComparer.Ordinal);
        foreach (var tree in compilation.SyntaxTrees)
        {
            var model = compilation.GetSemanticModel(tree);
            foreach (var declaration in tree.GetRoot().DescendantNodes().OfType<BaseTypeDeclarationSyntax>())
            {
                if (model.GetDeclaredSymbol(declaration) is not INamedTypeSymbol symbol) continue;
                string key = symbol.OriginalDefinition.ToDisplayString();
                if (!declarations.TryGetValue(key, out var parts)) declarations[key] = parts = [];
                parts.Add(declaration);
            }
        }
        var pending = new Queue<string>(roots);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var selected = new List<BaseTypeDeclarationSyntax>();

        void IncludeType(ITypeSymbol? type)
        {
            if (type is IArrayTypeSymbol array) { IncludeType(array.ElementType); return; }
            if (type is not INamedTypeSymbol named) return;
            if (named.TypeKind == TypeKind.Delegate) return; // Represented by JavaScript functions.
            foreach (var argument in named.TypeArguments) IncludeType(argument);
            if (SymbolEqualityComparer.Default.Equals(named.ContainingAssembly, compilation.Assembly))
                pending.Enqueue(named.OriginalDefinition.ToDisplayString());
        }

        void IncludeSymbol(ISymbol? symbol)
        {
            switch (symbol)
            {
                case INamedTypeSymbol type: IncludeType(type); break;
                case IMethodSymbol method:
                    IncludeType(method.ContainingType);
                    IncludeType(method.ReturnType);
                    foreach (var parameter in method.Parameters) IncludeType(parameter.Type);
                    foreach (var argument in method.TypeArguments) IncludeType(argument);
                    break;
                case IPropertySymbol property:
                    IncludeType(property.ContainingType);
                    IncludeType(property.Type);
                    break;
                case IFieldSymbol field:
                    IncludeType(field.ContainingType);
                    IncludeType(field.Type);
                    break;
                case IEventSymbol eventSymbol:
                    IncludeType(eventSymbol.ContainingType);
                    IncludeType(eventSymbol.Type);
                    break;
                case ILocalSymbol local: IncludeType(local.Type); break;
                case IParameterSymbol parameter: IncludeType(parameter.Type); break;
            }
        }

        while (pending.Count > 0)
        {
            string key = pending.Dequeue();
            if (!seen.Add(key)) continue;
            if (!declarations.TryGetValue(key, out var parts))
                throw new InvalidOperationException($"Root or dependency type not found in assembly: {key}");
            if (parts.Any(part => part is not (ClassDeclarationSyntax or StructDeclarationSyntax
                or EnumDeclarationSyntax or InterfaceDeclarationSyntax)))
                throw Unsupported(parts[0], $"Selected dependency {key} has no JavaScript lowering ({parts[0].Kind()})");
            foreach (var part in parts)
            {
                selected.Add(part);
                var model = compilation.GetSemanticModel(part.SyntaxTree);
                var classSymbol = (INamedTypeSymbol)model.GetDeclaredSymbol(part)!;
                IncludeType(classSymbol.BaseType);
                foreach (var contract in classSymbol.Interfaces) IncludeType(contract);
                IEnumerable<SyntaxNode> members = part switch
                {
                    TypeDeclarationSyntax type => type.Members,
                    EnumDeclarationSyntax enumeration => enumeration.Members,
                    _ => [],
                };
                foreach (var member in members)
                {
                    if (!editorMode && member is MemberDeclarationSyntax declared
                        && EditorCodePolicy.IsEditorOnlyMember(declared)) continue;
                    foreach (var node in member.DescendantNodes())
                    {
                        // Unity attributes describe editor/serializer behavior and emit no runtime JS.
                        if (node.Ancestors().Any(parent => parent is AttributeSyntax)) continue;
                        if (node is SimpleNameSyntax name)
                        {
                            var referenced = model.GetSymbolInfo(name).Symbol;
                            if (!editorMode && referenced != null && EditorCodePolicy.IsEditorOnlySymbol(referenced))
                                throw Unsupported(name,
                                    $"Runtime code references editor-only symbol {referenced.ToDisplayString()}");
                            IncludeSymbol(referenced);
                        }
                        else if (node is InvocationExpressionSyntax or ObjectCreationExpressionSyntax)
                            IncludeType(model.GetTypeInfo(node).Type);
                    }
                }
            }
        }
        return selected;
    }

    private static Exception Unsupported(SyntaxNode node, string message)
    {
        var span = node.GetLocation().GetLineSpan();
        return new InvalidOperationException($"{span.Path}:{span.StartLinePosition.Line + 1}:"
            + $"{span.StartLinePosition.Character + 1}: {message}");
    }
}
