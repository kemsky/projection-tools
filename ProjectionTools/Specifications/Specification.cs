using System.Linq.Expressions;
using DelegateDecompiler;
using ProjectionTools.Expressions;

namespace ProjectionTools.Specifications;

internal interface ISpecificationExpressionAccessor
{
    LambdaExpression GetExpression();
}

public sealed class Specification<TSource> : ISpecificationExpressionAccessor
{
    public Expression<Func<TSource, bool>> IsSatisfiedByExpression => _lazyExpression.Value;

    public Func<TSource, bool> IsSatisfiedBy => _lazyDelegate.Value;

    private readonly Lazy<Func<TSource, bool>> _lazyDelegate;

    private readonly Lazy<Expression<Func<TSource, bool>>> _lazyExpression;

    public Specification(Expression<Func<TSource, bool>> expressionFunc)
    {
        Defensive.Contract.ArgumentNotNull(expressionFunc);

        _lazyExpression = new Lazy<Expression<Func<TSource, bool>>>(() => expressionFunc.Rewrite(), LazyThreadSafetyMode.PublicationOnly);

        _lazyDelegate = new Lazy<Func<TSource, bool>>(() =>
        {
            var compiled = expressionFunc.Compile();

            return compiled;
        }, LazyThreadSafetyMode.PublicationOnly);
    }

    public Specification(Expression<Func<TSource, bool>> expressionFunc, Func<TSource, bool> delegateFunc)
    {
        Defensive.Contract.ArgumentNotNull(delegateFunc);

        _lazyExpression = expressionFunc == null
            ? new Lazy<Expression<Func<TSource, bool>>>(() => ((Expression<Func<TSource, bool>>)delegateFunc.Decompile()).Rewrite(), LazyThreadSafetyMode.PublicationOnly)
            : new Lazy<Expression<Func<TSource, bool>>>(() => expressionFunc.Rewrite(), LazyThreadSafetyMode.PublicationOnly);

        _lazyDelegate = new Lazy<Func<TSource, bool>>(() => delegateFunc, LazyThreadSafetyMode.PublicationOnly);
    }

    internal Specification(Lazy<Expression<Func<TSource, bool>>> lazyExpression, Lazy<Func<TSource, bool>> lazyDelegate)
    {
        Defensive.Contract.ArgumentNotNull(lazyExpression);
        Defensive.Contract.ArgumentNotNull(lazyDelegate);

        _lazyExpression = lazyExpression;

        _lazyDelegate = lazyDelegate;
    }

    public static Specification<TSource> operator &(Specification<TSource> spec1, Specification<TSource> spec2)
    {
        Defensive.Contract.ArgumentNotNull(spec1);
        Defensive.Contract.ArgumentNotNull(spec2);

        return new Specification<TSource>(
            new Lazy<Expression<Func<TSource, bool>>>(() => spec1.IsSatisfiedByExpression.And(spec2.IsSatisfiedByExpression), LazyThreadSafetyMode.PublicationOnly),
            new Lazy<Func<TSource, bool>>(() => x => spec1.IsSatisfiedBy(x) && spec2.IsSatisfiedBy(x), LazyThreadSafetyMode.PublicationOnly)
        );
    }

    public static Specification<TSource> operator |(Specification<TSource> spec1, Specification<TSource> spec2)
    {
        Defensive.Contract.ArgumentNotNull(spec1);
        Defensive.Contract.ArgumentNotNull(spec2);

        return new Specification<TSource>(
            new Lazy<Expression<Func<TSource, bool>>>(() => spec1.IsSatisfiedByExpression.Or(spec2.IsSatisfiedByExpression), LazyThreadSafetyMode.PublicationOnly),
            new Lazy<Func<TSource, bool>>(() => x => spec1.IsSatisfiedBy(x) || spec2.IsSatisfiedBy(x), LazyThreadSafetyMode.PublicationOnly)
        );
    }

    public static Specification<TSource> operator !(Specification<TSource> specification)
    {
        Defensive.Contract.ArgumentNotNull(specification);

        return new Specification<TSource>(
            new Lazy<Expression<Func<TSource, bool>>>(() => specification.IsSatisfiedByExpression.Not(), LazyThreadSafetyMode.PublicationOnly),
            new Lazy<Func<TSource, bool>>(() => x => !specification.IsSatisfiedBy(x), LazyThreadSafetyMode.PublicationOnly)
        );
    }

    // false operator required to prevent short-circuit and force evaluate both specs: spec1 && spec2
    public static bool operator false(Specification<TSource> _) => false;

    // true operator used by if, while etc. required as pair to the false operator
    public static bool operator true(Specification<TSource> _) => false;

    public Specification<TProjection> ApplyTo<TProjection>(Expression<Func<TProjection, TSource>> expression)
    {
        Defensive.Contract.ArgumentNotNull(expression);

        var currentDelegate = IsSatisfiedBy;
        var currentExpression = IsSatisfiedByExpression;

        return new Specification<TProjection>(
            new Lazy<Expression<Func<TProjection, bool>>>(() => currentExpression.ApplyTo(expression), LazyThreadSafetyMode.PublicationOnly),
            new Lazy<Func<TProjection, bool>>(() =>
            {
                var compiled = expression.Compile();

                return x => currentDelegate(compiled.Invoke(x));
            }, LazyThreadSafetyMode.PublicationOnly)
        );
    }

    public Specification<TProjection> ApplyTo<TProjection>(Expression<Func<TProjection, TSource>> expressionFunc, Func<TProjection, TSource> delegateFunc)
    {
        Defensive.Contract.ArgumentNotNull(delegateFunc);

        var currentDelegate = IsSatisfiedBy;
        var currentExpression = IsSatisfiedByExpression;

        var lazyExpression = expressionFunc == null
            ? new Lazy<Expression<Func<TProjection, bool>>>(() => currentExpression.ApplyTo((Expression<Func<TProjection, TSource>>)delegateFunc.Decompile()), LazyThreadSafetyMode.PublicationOnly)
            : new Lazy<Expression<Func<TProjection, bool>>>(() => currentExpression.ApplyTo(expressionFunc), LazyThreadSafetyMode.PublicationOnly);

        var lazyDelegate = new Lazy<Func<TProjection, bool>>(() => x => currentDelegate(delegateFunc(x)), LazyThreadSafetyMode.PublicationOnly);

        return new Specification<TProjection>(
            lazyExpression,
            lazyDelegate
        );
    }

    public static implicit operator Func<TSource, bool>(Specification<TSource> f)
    {
        return f.IsSatisfiedBy;
    }

    public static implicit operator Expression<Func<TSource, bool>>(Specification<TSource> f)
    {
        return f.IsSatisfiedByExpression;
    }

    LambdaExpression ISpecificationExpressionAccessor.GetExpression()
    {
        return IsSatisfiedByExpression;
    }
}

internal static class SpecificationExpressionExtensions
{
    private static readonly ProjectionVisitor ProjectionVisitor = new();

    private static readonly SpecificationVisitor SpecificationVisitor = new();

    public static Expression<Func<TSource, bool>> Rewrite<TSource>(this Expression<Func<TSource, bool>> expression)
    {
        return (Expression<Func<TSource, bool>>)SpecificationVisitor.Visit(ProjectionVisitor.Visit(expression));
    }
}