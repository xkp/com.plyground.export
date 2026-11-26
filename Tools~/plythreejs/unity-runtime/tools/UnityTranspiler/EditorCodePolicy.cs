using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

static class EditorCodePolicy
{
    private static readonly HashSet<string> EditorCallbacks = new(StringComparer.Ordinal)
    {
        "OnDrawGizmos", "OnDrawGizmosSelected", "DrawGizmos", "Gizmos",
        "DrawGizmosSelected", "OnValidate",
    };

    // JU3 and other Unity packages commonly keep editor visualization in this partial file.
    public static bool IsEditorOnlySource(SyntaxTree tree) =>
        tree.FilePath.EndsWith(".Gizmos.cs", StringComparison.OrdinalIgnoreCase);

    public static bool IsEditorOnlyMember(MemberDeclarationSyntax member) =>
        IsMalbersGizmoCacheField(member)
        || member is MethodDeclarationSyntax method
            && ((method.Identifier.ValueText == "Reset"
                    && method.ParameterList.Parameters.Count == 0
                    && !IsMonoBehaviourReset(method)
                    && !IsMalbersRuntimeReset(method) && !IsSplineArchitectRuntimeReset(method)
                    && !IsAdventureCreatorRuntimeReset(method))
                || EditorCallbacks.Contains(method.Identifier.ValueText)
                || ReturnsImmediatelyInPlayMode(method)
                || IsMalbersGizmoCacheHelper(method)
                || method.DescendantNodes().OfType<MemberAccessExpressionSyntax>()
                    .Any(access => access.Expression.ToString() is "Gizmos"
                        or "UnityEngine.Gizmos")
                || method.AttributeLists.SelectMany(list => list.Attributes)
                    .Any(attribute => attribute.Name.ToString().EndsWith("MenuItem", StringComparison.Ordinal))
                || IsEditorOnlySource(method.SyntaxTree));

    private static bool IsMalbersGizmoCacheField(MemberDeclarationSyntax member) =>
        member is FieldDeclarationSyntax field
        && field.Declaration.Variables.Count == 1
        && field.Declaration.Variables[0].Identifier.ValueText == "s_CapsuleMeshCache"
        && field.Parent is ClassDeclarationSyntax declaration
        && declaration.Identifier.ValueText == "MDebug"
        && field.Ancestors().OfType<BaseNamespaceDeclarationSyntax>()
            .Any(space => space.Name.ToString() == "MalbersAnimations");

    // Unity calls Reset when the editor importer adds a component. Emit it for
    // MonoBehaviours so their serialized defaults are established. Other Reset
    // methods (notably input asset setup) belong to editor-only dependencies.
    private static bool IsMonoBehaviourReset(MethodDeclarationSyntax method) =>
        method.Parent is ClassDeclarationSyntax declaration
        && declaration.BaseList?.Types.Any(type => type.Type.ToString() == "MonoBehaviour") == true;

    // Some packages call editor maintenance helpers from source that also compiles
    // for players. A leading play-mode return makes the entire method inert there.
    private static bool ReturnsImmediatelyInPlayMode(MethodDeclarationSyntax method) =>
        method.Body?.Statements.FirstOrDefault() is IfStatementSyntax statement
        && statement.Condition.ToString() == "Application.isPlaying"
        && statement.Statement is ReturnStatementSyntax
        && statement.Else == null;

    // These private mesh builders are reached only by MDebug's Gizmos calls.
    // Their tuple-keyed cache has no player-side behavior to preserve.
    private static bool IsMalbersGizmoCacheHelper(MethodDeclarationSyntax method) =>
        method.Identifier.ValueText is "CapsuleGetOrCreate" or "CapsuleBuildMesh"
        && method.Modifiers.Any(modifier => modifier.ValueText == "private")
        && method.Parent is ClassDeclarationSyntax declaration
        && declaration.Identifier.ValueText == "MDebug"
        && method.Ancestors().OfType<BaseNamespaceDeclarationSyntax>()
            .Any(space => space.Name.ToString() == "MalbersAnimations");

    // Malbers invokes these controller resets in play mode. Stance clears transient
    // input and queue state; Mode stops the active ability and restores state.
    private static bool IsMalbersRuntimeReset(MethodDeclarationSyntax method) =>
        method.Parent is ClassDeclarationSyntax declaration
        && declaration.Identifier.ValueText is "Stance" or "Mode"
        && method.Ancestors().OfType<BaseNamespaceDeclarationSyntax>()
            .Any(space => space.Name.ToString() == "MalbersAnimations.Controller");

    // SplineArchitect workers call their private Reset methods from Start and
    // other runtime lifecycle paths to clear native arrays and job state.
    private static bool IsSplineArchitectRuntimeReset(MethodDeclarationSyntax method) =>
        method.Parent is ClassDeclarationSyntax declaration
        && declaration.Identifier.ValueText.EndsWith("Worker", StringComparison.Ordinal)
        && method.Ancestors().OfType<BaseNamespaceDeclarationSyntax>()
            .Any(space => space.Name.ToString() == "SplineArchitect.Workers");

    // Adventure Creator's third-person camera calls this state reset each frame
    // when starting a new interpolation; it is not Unity's editor Reset callback.
    private static bool IsAdventureCreatorRuntimeReset(MethodDeclarationSyntax method) =>
        method.Parent is ClassDeclarationSyntax declaration
        && declaration.Identifier.ValueText == "FloatLerp"
        && method.Ancestors().OfType<BaseNamespaceDeclarationSyntax>()
            .Any(space => space.Name.ToString() == "AC");

    public static bool IsEditorOnlySymbol(ISymbol symbol) =>
        symbol.DeclaringSyntaxReferences.Length > 0
        && symbol.DeclaringSyntaxReferences.All(reference =>
        {
            var node = reference.GetSyntax();
            return node is MemberDeclarationSyntax member && IsEditorOnlyMember(member);
        });
}
