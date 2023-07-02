using System.Linq.Expressions;
using ProjectionTools.Expressions;

namespace ProjectionTools.Specifications;

internal static class SpecificationExpressionExtensions
{
    private static readonly ProjectionVisitor ProjectionVisitor = new();

    private static readonly SpecificationVisitor SpecificationVisitor = new();

    public static Expression<Func<TSource, bool>> Rewrite<TSource>(this Expression<Func<TSource, bool>> expression)
    {
        return (Expression<Func<TSource, bool>>)SpecificationVisitor.Visit(ProjectionVisitor.Visit(expression))!;
    }
}