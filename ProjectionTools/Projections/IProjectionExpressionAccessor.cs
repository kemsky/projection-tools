using System.Linq.Expressions;

namespace ProjectionTools.Projections;

/// <summary>
/// Provides a mechanism to access the underlying <see cref="LambdaExpression"/> of a projection.
/// This is primarily used for internal operations where the generic type of the projection is not known.
/// </summary>
public interface IProjectionExpressionAccessor
{
    /// <summary>
    /// Gets the underlying lambda expression of the projection.
    /// </summary>
    /// <returns>The <see cref="LambdaExpression"/> that represents the projection's logic.</returns>
    LambdaExpression GetExpression();
}