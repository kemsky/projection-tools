using System.Linq.Expressions;
using DelegateDecompiler;

namespace ProjectionTools.Specifications;

public readonly struct SpecificationFactory<TSource, TParam>
{
    internal Func<TParam, Expression<Func<TSource, bool>>> ExpressionFactory => _lazyExpressionFactory.Value;

    internal Func<TParam, Func<TSource, bool>> DelegateFactory => _lazyDelegateFactory.Value;

    private readonly Lazy<Func<TParam, Expression<Func<TSource, bool>>>> _lazyExpressionFactory;

    private readonly Lazy<Func<TParam, Func<TSource, bool>>> _lazyDelegateFactory;

    public SpecificationFactory(Func<TParam, Expression<Func<TSource, bool>>> expressionFactory)
    {
        Defensive.Contract.ArgumentNotNull(expressionFactory);

        _lazyExpressionFactory = new Lazy<Func<TParam, Expression<Func<TSource, bool>>>>(() => expressionFactory, LazyThreadSafetyMode.PublicationOnly);

        _lazyDelegateFactory = new Lazy<Func<TParam, Func<TSource, bool>>>(() => x => expressionFactory(x).Compile(), LazyThreadSafetyMode.PublicationOnly);
    }

    public SpecificationFactory(Func<TParam, Expression<Func<TSource, bool>>> expressionFactory, Func<TParam, Func<TSource, bool>> delegateFactory)
    {
        Defensive.Contract.ArgumentNotNull(delegateFactory);

        _lazyExpressionFactory = expressionFactory == null
            ? new Lazy<Func<TParam, Expression<Func<TSource, bool>>>>(() => x => (Expression<Func<TSource, bool>>)delegateFactory(x).Decompile(), LazyThreadSafetyMode.None)
            : new Lazy<Func<TParam, Expression<Func<TSource, bool>>>>(() => expressionFactory, LazyThreadSafetyMode.PublicationOnly);

        _lazyDelegateFactory = new Lazy<Func<TParam, Func<TSource, bool>>>(() => delegateFactory, LazyThreadSafetyMode.PublicationOnly);
    }

    internal SpecificationFactory(Lazy<Func<TParam, Expression<Func<TSource, bool>>>> expressionFactory, Lazy<Func<TParam, Func<TSource, bool>>> delegateFactory)
    {
        Defensive.Contract.ArgumentNotNull(expressionFactory);
        Defensive.Contract.ArgumentNotNull(delegateFactory);

        _lazyExpressionFactory = expressionFactory;

        _lazyDelegateFactory = delegateFactory;
    }

    public Specification<TSource> For(TParam param)
    {
        var lazyExpressionFactory = _lazyExpressionFactory;
        var lazyDelegateFactory = _lazyDelegateFactory;

        return new(
            new Lazy<Expression<Func<TSource, bool>>>(() => lazyExpressionFactory.Value(param), LazyThreadSafetyMode.PublicationOnly),
            new Lazy<Func<TSource, bool>>(() => lazyDelegateFactory.Value(param), LazyThreadSafetyMode.PublicationOnly)
        );
    }
}

public readonly struct SpecificationFactory<TSource, TParam1, TParam2>
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

    public SpecificationFactory(Func<TParam1, TParam2, Expression<Func<TSource, bool>>> expressionFactory, Func<TParam1, TParam2, Func<TSource, bool>> delegateFactory)
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
}