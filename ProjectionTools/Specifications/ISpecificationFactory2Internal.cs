using System.Linq.Expressions;

namespace ProjectionTools.Specifications;

internal interface ISpecificationFactory2Internal
{
    ISpecificationFactoryInternal For(object? arg1);

    ISpecificationInternal For(object? arg1, object? arg2);

    ISpecificationFactoryInternal For(Expression arg1);

    ISpecificationInternal For(Expression arg1, Expression arg2);
}