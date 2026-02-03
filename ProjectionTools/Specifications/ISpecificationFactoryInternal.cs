using System.Linq.Expressions;

namespace ProjectionTools.Specifications;

internal interface ISpecificationFactoryInternal
{
    ISpecificationInternal For(object? arg);

    ISpecificationInternal For(Expression arg);
}