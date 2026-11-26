using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

sealed class UnityEventListenerLowering : ISystemLowering
{
    private bool used;
    public IEnumerable<string> RuntimeImports => used ? ["UnityEvent"] : [];
    public string EmitPrelude() => "";

    public string? TryLower(IMethodSymbol symbol, InvocationExpressionSyntax invocation,
        SemanticModel model, Func<ExpressionSyntax, string> emitExpression)
    {
        if (symbol.Name is not ("AddListener" or "RemoveListener")
            || symbol.ContainingType.ContainingNamespace.ToDisplayString() != "UnityEngine.Events"
            || symbol.ContainingType.Name != "UnityEvent"
            || invocation.Expression is not MemberAccessExpressionSyntax eventMethod
            || invocation.ArgumentList.Arguments.Count != 1)
            return null;
        ExpressionSyntax callback = invocation.ArgumentList.Arguments[0].Expression;
        if (callback is LambdaExpressionSyntax or AnonymousMethodExpressionSyntax) return null;
        if (model.GetSymbolInfo(callback).Symbol is not IMethodSymbol method) return null;
        ExpressionSyntax? receiver = callback switch
        {
            MemberAccessExpressionSyntax member => member.Expression,
            IdentifierNameSyntax => null,
            _ => throw Unsupported(callback, "UnityEvent method group form is not supported"),
        };
        used = true;
        string target = method.IsStatic ? receiver == null ? method.ContainingType.Name
            : emitExpression(receiver) : receiver == null ? "this" : emitExpression(receiver);
        return $"{emitExpression(eventMethod.Expression)}.{symbol.Name}(UnityEvent.Bind({target}, "
            + $"{JsonSerializer.Serialize(method.Name)}))";
    }

    private static Exception Unsupported(SyntaxNode node, string message)
    {
        var span = node.GetLocation().GetLineSpan();
        return new InvalidOperationException($"{span.Path}:{span.StartLinePosition.Line + 1}:"
            + $"{span.StartLinePosition.Character + 1}: {message}");
    }
}
