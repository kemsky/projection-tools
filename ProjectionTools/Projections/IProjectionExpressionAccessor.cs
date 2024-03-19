using System.Linq.Expressions;

namespace ProjectionTools.Projections;

public interface IProjectionExpressionAccessor
{
    LambdaExpression GetExpression();
}