using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace ProjectionTools.Expressions;

/// <summary>
/// An <see cref="ExpressionVisitor"/> that rewrites expressions containing various <see cref="Specifications.Specification{TSource}"/> constructs.
/// </summary>
public sealed class SpecificationVisitor : ExpressionVisitor
{
    private readonly ExpressionVisitor _visitor = new CompositeVisitor(
        new SpecificationFactoryInvocationVisitor(),
        new SpecificationOperatorsVisitor(),
        new SpecificationInvocationVisitor()
    );

    /// <summary>
    /// Visits the specified expression node, dispatching it to the appropriate underlying visitor for processing.
    /// </summary>
    /// <param name="node">The expression to visit.</param>
    /// <returns>The modified expression, if it or any subexpression was modified; otherwise, returns the original expression.</returns>
    [return: NotNullIfNotNull(nameof(node))]
    public override Expression? Visit(Expression? node)
    {
        return _visitor.Visit(node);
    }
}