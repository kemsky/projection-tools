using System.Linq.Expressions;

namespace ProjectionTools.Specifications;

internal interface ISpecificationFactory2Internal
{
    ISpecificationInternal For(object? arg1, object? arg2);

    ISpecificationInternal For(LambdaExpression arg1, LambdaExpression arg2);
}