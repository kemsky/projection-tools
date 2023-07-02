using System.Linq.Expressions;
using ProjectionTools.Expressions;

namespace ProjectionTools.Projections;

internal static class ProjectionExpressionExtensions
{
    private static readonly ProjectionVisitor ProjectionVisitor = new();

    private static readonly SpecificationVisitor SpecificationVisitor = new();

    public static Expression<Func<TSource, TResult>> Rewrite<TSource, TResult>(this Expression<Func<TSource, TResult>> expression)
    {
        return (Expression<Func<TSource, TResult>>)ProjectionVisitor.Visit(SpecificationVisitor.Visit(expression))!;
    }
}