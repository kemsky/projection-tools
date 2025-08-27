using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace ProjectionTools.Expressions;

public sealed class SpecificationVisitor : ExpressionVisitor
{
    private readonly ExpressionVisitor _visitor = new CompositeVisitor(
        new SpecificationFactoryInvocationVisitor(),
        new SpecificationOperatorsVisitor(),
        new SpecificationInvocationVisitor()
    );

    [return: NotNullIfNotNull(nameof(node))]
    public override Expression? Visit(Expression? node)
    {
        return _visitor.Visit(node);
    }
}