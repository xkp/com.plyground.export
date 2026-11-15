using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

static class EditorCodePolicy
{
    private static readonly HashSet<string> EditorCallbacks = new(StringComparer.Ordinal)
    {
        "OnDrawGizmos", "OnDrawGizmosSelected", "OnValidate",
    };

    // JU3 and other Unity packages commonly keep editor visualization in this partial file.
    public static bool IsEditorOnlySource(SyntaxTree tree) =>
        tree.FilePath.EndsWith(".Gizmos.cs", StringComparison.OrdinalIgnoreCase);

    public static bool IsEditorOnlyMember(MemberDeclarationSyntax member) =>
        member is MethodDeclarationSyntax method
            && ((method.Identifier.ValueText == "Reset" && !IsMonoBehaviourReset(method))
                || EditorCallbacks.Contains(method.Identifier.ValueText)
                || method.AttributeLists.SelectMany(list => list.Attributes)
                    .Any(attribute => attribute.Name.ToString().EndsWith("MenuItem", StringComparison.Ordinal))
                || IsEditorOnlySource(method.SyntaxTree));

    // Unity calls Reset when the editor importer adds a component. Emit it for
    // MonoBehaviours so their serialized defaults are established. Other Reset
    // methods (notably input asset setup) belong to editor-only dependencies.
    private static bool IsMonoBehaviourReset(MethodDeclarationSyntax method) =>
        method.Parent is ClassDeclarationSyntax declaration
        && declaration.BaseList?.Types.Any(type => type.Type.ToString() == "MonoBehaviour") == true;

    public static bool IsEditorOnlySymbol(ISymbol symbol) =>
        symbol.DeclaringSyntaxReferences.Length > 0
        && symbol.DeclaringSyntaxReferences.All(reference =>
        {
            var node = reference.GetSyntax();
            return node is MemberDeclarationSyntax member && IsEditorOnlyMember(member);
        });
}
