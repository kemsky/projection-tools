using System.Linq.Expressions;

namespace ProjectionTools.Specifications;

internal interface ISpecificationExpressionAccessor
{
    LambdaExpression GetExpression();
}