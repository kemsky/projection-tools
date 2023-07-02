using System.Linq.Expressions;

namespace ProjectionTools.Projections;

internal interface IProjectionExpressionAccessor
{
    LambdaExpression GetExpression();
}