using System.Linq.Expressions;
using DelegateDecompiler;
using ProjectionTools.Assertions;

namespace ProjectionTools.Specifications;

public sealed class SpecificationFactory<TSource, TParam>
{
    internal Func<TParam, Expression<Func<TSource, bool>>> ExpressionFactory => _lazyExpressionFactory.Value;

    internal Func<TParam, Func<TSource, bool>> DelegateFactory => _lazyDelegateFactory.Value;

    private readonly Lazy<Func<TParam, Expression<Func<TSource, bool>>>> _lazyExpressionFactory;

    private readonly Lazy<Func<TParam, Func<TSource, bool>>> _lazyDelegateFactory;

    public SpecificationFactory(Func<TParam, Expression<Func<TSource, bool>>> expressionFactory)
    {
        Defensive.Contract.ArgumentNotNull(expressionFactory, nameof(expressionFactory));

        _lazyExpressionFactory = new Lazy<Func<TParam, Expression<Func<TSource, bool>>>>(() => expressionFactory, LazyThreadSafetyMode.PublicationOnly);

        _lazyDelegateFactory = new Lazy<Func<TParam, Func<TSource, bool>>>(() => x => expressionFactory(x).Compile(), LazyThreadSafetyMode.PublicationOnly);
    }

    public SpecificationFactory(Func<TParam, Expression<Func<TSource, bool>>>? expressionFactory, Func<TParam, Func<TSource, bool>> delegateFactory)
    {
        Defensive.Contract.ArgumentNotNull(delegateFactory, nameof(delegateFactory));

        _lazyExpressionFactory = expressionFactory == null
            ? new Lazy<Func<TParam, Expression<Func<TSource, bool>>>>(() => x => (Expression<Func<TSource, bool>>)delegateFactory(x).Decompile(), LazyThreadSafetyMode.None)
            : new Lazy<Func<TParam, Expression<Func<TSource, bool>>>>(() => expressionFactory, LazyThreadSafetyMode.PublicationOnly);

        _lazyDelegateFactory = new Lazy<Func<TParam, Func<TSource, bool>>>(() => delegateFactory, LazyThreadSafetyMode.PublicationOnly);
    }

    internal SpecificationFactory(Lazy<Func<TParam, Expression<Func<TSource, bool>>>> expressionFactory, Lazy<Func<TParam, Func<TSource, bool>>> delegateFactory)
    {
        Defensive.Contract.ArgumentNotNull(expressionFactory, nameof(expressionFactory));
        Defensive.Contract.ArgumentNotNull(delegateFactory, nameof(delegateFactory));

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