using System.Linq.Expressions;
using DelegateDecompiler;
using ProjectionTools.Assertions;
using ProjectionTools.Expressions;

namespace ProjectionTools.Projections;

public sealed class ProjectionFactory<TSource, TResult, TParam>
{
    internal Func<TParam, Expression<Func<TSource, TResult>>> ExpressionFactory => _lazyExpression.Value;

    internal Func<TParam, Func<TSource, TResult>> DelegateFactory => _lazyDelegate.Value;

    private readonly Lazy<Func<TParam, Expression<Func<TSource, TResult>>>> _lazyExpression;

    private readonly Lazy<Func<TParam, Func<TSource, TResult>>> _lazyDelegate;

    public ProjectionFactory(Func<TParam, Expression<Func<TSource, TResult>>> expressionFactory)
    {
        Defensive.Contract.ArgumentNotNull(expressionFactory);

        _lazyExpression = new Lazy<Func<TParam, Expression<Func<TSource, TResult>>>>(() => expressionFactory, LazyThreadSafetyMode.PublicationOnly);

        _lazyDelegate = new Lazy<Func<TParam, Func<TSource, TResult>>>(() => x => expressionFactory(x).Compile(), LazyThreadSafetyMode.PublicationOnly);
    }

    public ProjectionFactory(
        Func<TParam, Expression<Func<TSource, TResult>>>? expressionFactory,
        Func<TParam, Func<TSource, TResult>> delegateFactory
    )
    {
        Defensive.Contract.ArgumentNotNull(delegateFactory);

        _lazyExpression = expressionFactory == null
            ? new Lazy<Func<TParam, Expression<Func<TSource, TResult>>>>(() => x => (Expression<Func<TSource, TResult>>)delegateFactory(x).Decompile(), LazyThreadSafetyMode.PublicationOnly)
            : new Lazy<Func<TParam, Expression<Func<TSource, TResult>>>>(() => expressionFactory, LazyThreadSafetyMode.PublicationOnly);

        _lazyDelegate = new Lazy<Func<TParam, Func<TSource, TResult>>>(() => delegateFactory, LazyThreadSafetyMode.PublicationOnly);
    }

    internal ProjectionFactory(
        Lazy<Func<TParam, Expression<Func<TSource, TResult>>>> expressionFactory,
        Lazy<Func<TParam, Func<TSource, TResult>>> delegateFactory
    )
    {
        Defensive.Contract.ArgumentNotNull(expressionFactory);
        Defensive.Contract.ArgumentNotNull(delegateFactory);

        _lazyExpression = expressionFactory;
        _lazyDelegate = delegateFactory;
    }

    public Projection<TSource, TResult> For(TParam param)
    {
        return new(ExpressionFactory(param), DelegateFactory(param));
    }

    // experimental
    public Projection<TSource, TResult> For(Func<TParam> param)
    {
        var expressionFactory = ExpressionFactory;
        var delegateFactory = DelegateFactory;

        // todo: not consistent behavior expression vs delegate when factories contain IIF
        return new(
            new Lazy<Expression<Func<TSource, TResult>>>(() => expressionFactory(param()).BindArgument((Expression<Func<TParam>>)param.Decompile(), expressionFactory.Method.GetParameters()[0]), LazyThreadSafetyMode.PublicationOnly),
            new Lazy<Func<TSource, TResult>>(() => source => delegateFactory(param())(source), LazyThreadSafetyMode.PublicationOnly)
        );
    }
}