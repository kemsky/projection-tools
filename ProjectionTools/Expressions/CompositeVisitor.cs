using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace ProjectionTools.Expressions;

internal class CompositeVisitor : ExpressionVisitor
{
    private readonly ExpressionVisitor[] _visitors;

    internal CompositeVisitor(params ExpressionVisitor[] visitors)
    {
        _visitors = visitors;
    }

    [return: NotNullIfNotNull(nameof(node))]
    public override Expression? Visit(Expression? node)
    {
        var result = node;

        foreach (var visitor in _visitors)
        {
            result = visitor.Visit(result);
        }

        return result;
    }
}