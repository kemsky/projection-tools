using System.Linq.Expressions;

namespace ProjectionTools.Specifications;

/// <summary>
/// Provides a mechanism to access the underlying <see cref="LambdaExpression"/> of a specification.
/// This is primarily used for internal operations where the generic type of the specification is not known.
/// </summary>
public interface ISpecificationExpressionAccessor
{
    /// <summary>
    /// Gets the underlying lambda expression of the specification.
    /// </summary>
    /// <returns>The <see cref="LambdaExpression"/> that represents the specification's logic.</returns>
    LambdaExpression GetExpression();
}