using System.Linq.Expressions;

namespace ProjectionTools.Specifications;

public interface ISpecificationExpressionAccessor
{
    LambdaExpression GetExpression();
}