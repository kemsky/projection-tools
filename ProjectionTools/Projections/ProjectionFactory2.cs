using System.Linq.Expressions;
using DelegateDecompiler;
using ProjectionTools.Assertions;
using ProjectionTools.Expressions;

namespace ProjectionTools.Projections;

public sealed class ProjectionFactory<TSource, TResult, TParam1, TParam2>
{
    internal Func<TParam1, TParam2, Expression<Func<TSource, TResult>>> ExpressionFactory => _lazyExpression.Value;

    internal Func<TParam1, TParam2, Func<TSource, TResult>> DelegateFactory => _lazyDelegate.Value;

    private readonly Lazy<Func<TParam1, TParam2, Expression<Func<TSource, TResult>>>> _lazyExpression;

    private readonly Lazy<Func<TParam1, TParam2, Func<TSource, TResult>>> _lazyDelegate;

    public ProjectionFactory(Func<TParam1, TParam2, Expression<Func<TSource, TResult>>> expressionFactory)
    {
        Defensive.Contract.ArgumentNotNull(expressionFactory);

        _lazyExpression = new Lazy<Func<TParam1, TParam2, Expression<Func<TSource, TResult>>>>(() => expressionFactory, LazyThreadSafetyMode.PublicationOnly);

        _lazyDelegate = new Lazy<Func<TParam1, TParam2, Func<TSource, TResult>>>(() => (x, y) => expressionFactory(x, y).Compile(), LazyThreadSafetyMode.PublicationOnly);
    }

    public ProjectionFactory(Func<TParam1, TParam2, Expression<Func<TSource, TResult>>>? expressionFactory, Func<TParam1, TParam2, Func<TSource, TResult>> delegateFactory)
    {
        Defensive.Contract.ArgumentNotNull(delegateFactory);

        _lazyExpression = expressionFactory == null
            ? new Lazy<Func<TParam1, TParam2, Expression<Func<TSource, TResult>>>>(() => (x, y) => (Expression<Func<TSource, TResult>>)delegateFactory(x, y).Decompile(), LazyThreadSafetyMode.PublicationOnly)
            : new Lazy<Func<TParam1, TParam2, Expression<Func<TSource, TResult>>>>(() => expressionFactory, LazyThreadSafetyMode.PublicationOnly);

        _lazyDelegate = new Lazy<Func<TParam1, TParam2, Func<TSource, TResult>>>(() => delegateFactory, LazyThreadSafetyMode.PublicationOnly);
    }

    public ProjectionFactory<TSource, TResult, TParam2> For(TParam1 param1)
    {
        var expressionFactory = ExpressionFactory;
        var delegateFactory = DelegateFactory;

        return new(param2 => expressionFactory(param1, param2), param2 => delegateFactory(param1, param2));
    }

    public Projection<TSource, TResult> For(TParam1 param1, TParam2 param2)
    {
        return new(ExpressionFactory(param1, param2), DelegateFactory(param1, param2));
    }

    // experimental
    public Projection<TSource, TResult> For(Func<TParam1> param1, Func<TParam2> param2)
    {
        var expressionFactory = ExpressionFactory;
        var delegateFactory = DelegateFactory;

        // todo: not consistent behavior expression vs delegate when factories contain IIF
        return new(
            new Lazy<Expression<Func<TSource, TResult>>>(() => expressionFactory(param1(), param2()).BindArguments((Expression<Func<TParam1>>)param1.Decompile(), (Expression<Func<TParam2>>)param2.Decompile(), expressionFactory.Method.GetParameters()[0], expressionFactory.Method.GetParameters()[1]), LazyThreadSafetyMode.PublicationOnly),
            new Lazy<Func<TSource, TResult>>(() => source => delegateFactory(param1(), param2())(source), LazyThreadSafetyMode.PublicationOnly)
        );
    }

    // experimental
    public ProjectionFactory<TSource, TResult, TParam2> For(Func<TParam1> param1)
    {
        var expressionFactory = ExpressionFactory;
        var delegateFactory = DelegateFactory;

        // todo: not consistent behavior expression vs delegate when factories contain IIF
        return new(
            new Lazy<Func<TParam2, Expression<Func<TSource, TResult>>>>(() => param2 => expressionFactory(param1(), param2).BindArgument((Expression<Func<TParam1>>)param1.Decompile(), expressionFactory.Method.GetParameters()[0]), LazyThreadSafetyMode.PublicationOnly),
            new Lazy<Func<TParam2, Func<TSource, TResult>>>(() => param2 => source => delegateFactory(param1(), param2)(source), LazyThreadSafetyMode.PublicationOnly)
        );
    }
}