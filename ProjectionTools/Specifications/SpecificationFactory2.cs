using System.Linq.Expressions;
using DelegateDecompiler;
using ProjectionTools.Assertions;
using ProjectionTools.Expressions;

namespace ProjectionTools.Specifications;

public sealed class SpecificationFactory<TSource, TParam1, TParam2> : ISpecificationFactory2Internal
{
    internal Func<TParam1, TParam2, Expression<Func<TSource, bool>>> ExpressionFactory => _lazyExpressionFactory.Value;

    internal Func<TParam1, TParam2, Func<TSource, bool>> DelegateFactory => _lazyDelegateFactory.Value;

    private readonly Lazy<Func<TParam1, TParam2, Expression<Func<TSource, bool>>>> _lazyExpressionFactory;

    private readonly Lazy<Func<TParam1, TParam2, Func<TSource, bool>>> _lazyDelegateFactory;

    public SpecificationFactory(Func<TParam1, TParam2, Expression<Func<TSource, bool>>> expressionFactory)
    {
        Defensive.Contract.ArgumentNotNull(expressionFactory);

        _lazyExpressionFactory = new Lazy<Func<TParam1, TParam2, Expression<Func<TSource, bool>>>>(() => expressionFactory, LazyThreadSafetyMode.PublicationOnly);

        _lazyDelegateFactory = new Lazy<Func<TParam1, TParam2, Func<TSource, bool>>>(() => (x, y) => expressionFactory(x, y).Compile(), LazyThreadSafetyMode.PublicationOnly);
    }

    public SpecificationFactory(Func<TParam1, TParam2, Expression<Func<TSource, bool>>>? expressionFactory, Func<TParam1, TParam2, Func<TSource, bool>> delegateFactory)
    {
        Defensive.Contract.ArgumentNotNull(delegateFactory);

        _lazyExpressionFactory = expressionFactory == null
            ? new Lazy<Func<TParam1, TParam2, Expression<Func<TSource, bool>>>>(() => (x, y) => (Expression<Func<TSource, bool>>)delegateFactory(x, y).Decompile(), LazyThreadSafetyMode.PublicationOnly)
            : new Lazy<Func<TParam1, TParam2, Expression<Func<TSource, bool>>>>(() => expressionFactory, LazyThreadSafetyMode.PublicationOnly);

        _lazyDelegateFactory = new Lazy<Func<TParam1, TParam2, Func<TSource, bool>>>(() => delegateFactory, LazyThreadSafetyMode.PublicationOnly);
    }

    public SpecificationFactory<TSource, TParam2> For(TParam1 param1)
    {
        var lazyExpressionFactory = _lazyExpressionFactory;
        var lazyDelegateFactory = _lazyDelegateFactory;

        return new(
            new Lazy<Func<TParam2, Expression<Func<TSource, bool>>>>(() => param2 => lazyExpressionFactory.Value(param1, param2), LazyThreadSafetyMode.PublicationOnly),
            new Lazy<Func<TParam2, Func<TSource, bool>>>(() => param2 => lazyDelegateFactory.Value(param1, param2), LazyThreadSafetyMode.PublicationOnly)
        );
    }

    public Specification<TSource> For(TParam1 param1, TParam2 param2)
    {
        var lazyExpressionFactory = _lazyExpressionFactory;
        var lazyDelegateFactory = _lazyDelegateFactory;

        return new(
            new Lazy<Expression<Func<TSource, bool>>>(() => lazyExpressionFactory.Value(param1, param2), LazyThreadSafetyMode.PublicationOnly),
            new Lazy<Func<TSource, bool>>(() => lazyDelegateFactory.Value(param1, param2), LazyThreadSafetyMode.PublicationOnly)
        );
    }

    /// <summary>
    /// Experimental. Can be used in nested queries.
    /// <br/>
    /// (!) Inconsistent behavior expression vs delegate when factories contain IIF.
    /// </summary>
    /// <param name="param1"></param>
    /// <returns></returns>
    public SpecificationFactory<TSource, TParam2> For(Func<TParam1> param1)
    {
        var lazyExpressionFactory = _lazyExpressionFactory;
        var lazyDelegateFactory = _lazyDelegateFactory;

        // todo: not consistent behavior expression vs delegate when factories contain IIF
        return new(
            new Lazy<Func<TParam2, Expression<Func<TSource, bool>>>>(() => param2 => lazyExpressionFactory.Value(param1(), param2).BindArgument((Expression<Func<TParam1>>)param1.Decompile(), lazyExpressionFactory.Value.Method.GetParameters()[0]), LazyThreadSafetyMode.PublicationOnly),
            new Lazy<Func<TParam2, Func<TSource, bool>>>(() => param2 => lazyDelegateFactory.Value(param1(), param2), LazyThreadSafetyMode.PublicationOnly)
        );
    }

    /// <summary>
    /// Experimental. Can be used in nested queries.
    /// <br/>
    /// (!) Inconsistent behavior expression vs delegate when factories contain IIF.
    /// </summary>
    /// <param name="param1"></param>
    /// <param name="param2"></param>
    /// <returns></returns>
    public Specification<TSource> For(Func<TParam1> param1, Func<TParam2> param2)
    {
        var expressionFactory = _lazyExpressionFactory;
        var delegateFactory = _lazyDelegateFactory;

        // todo: not consistent behavior expression vs delegate when factories contain IIF
        return new(
            new Lazy<Expression<Func<TSource, bool>>>(() => expressionFactory.Value(param1(), param2()).BindArgument((Expression<Func<TParam1>>)param1.Decompile(), expressionFactory.Value.Method.GetParameters()[0]).BindArgument((Expression<Func<TParam2>>)param2.Decompile(), expressionFactory.Value.Method.GetParameters()[1]), LazyThreadSafetyMode.PublicationOnly),
            new Lazy<Func<TSource, bool>>(() => source => delegateFactory.Value(param1(), param2())(source), LazyThreadSafetyMode.PublicationOnly)
        );
    }

    ISpecificationInternal ISpecificationFactory2Internal.For(object? arg1, object? arg2)
    {
        return For((TParam1)arg1!, (TParam2)arg2!);
    }

    ISpecificationInternal ISpecificationFactory2Internal.For(LambdaExpression arg1, LambdaExpression arg2)
    {
        var expressionFactory = _lazyExpressionFactory;
        var delegateFactory = _lazyDelegateFactory;

        var param1 = (Expression<Func<TParam1>>)arg1;
        var param2 = (Expression<Func<TParam2>>)arg2;

        TParam1 value1 = default!;
        TParam2 value2 = default!;

        // todo: not consistent behavior expression vs delegate when factories contain IIF
        return new Specification<TSource>(
            new Lazy<Expression<Func<TSource, bool>>>(() => expressionFactory.Value(value1, value2).BindArgument(param1, expressionFactory.Value.Method.GetParameters()[0]).BindArgument(param2, expressionFactory.Value.Method.GetParameters()[1])),
            new Lazy<Func<TSource, bool>>(() => source => delegateFactory.Value(param1.Compile()(), param2.Compile()())(source), LazyThreadSafetyMode.PublicationOnly)
        );
    }
}