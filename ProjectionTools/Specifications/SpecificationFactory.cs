using System.Linq.Expressions;
using DelegateDecompiler;
using ProjectionTools.Assertions;
using ProjectionTools.Expressions;

namespace ProjectionTools.Specifications;

public sealed class SpecificationFactory<TSource, TParam> : ISpecificationFactoryInternal
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

    public SpecificationFactory(Func<TParam, Expression<Func<TSource, bool>>>? expressionFactory, Func<TParam, Func<TSource, bool>> delegateFactory)
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

    /// <summary>
    /// Experimental. Can be used in nested queries.
    /// <br/>
    /// (!) Inconsistent behavior expression vs delegate when factories contain IIF.
    /// </summary>
    /// <param name="param"></param>
    /// <returns></returns>
    public Specification<TSource> For(Func<TParam> param)
    {
        var expressionFactory = ExpressionFactory;
        var delegateFactory = DelegateFactory;

        // todo: not consistent behavior expression vs delegate when factories contain IIF
        return new(
            new Lazy<Expression<Func<TSource, bool>>>(() => expressionFactory(param()).BindArgument((Expression<Func<TParam>>)param.Decompile(), expressionFactory.Method.GetParameters()[0]), LazyThreadSafetyMode.PublicationOnly),
            new Lazy<Func<TSource, bool>>(() => source => delegateFactory(param())(source), LazyThreadSafetyMode.PublicationOnly)
        );
    }

    ISpecificationInternal ISpecificationFactoryInternal.For(object? arg)
    {
        return For((TParam)arg!);
    }

    ISpecificationInternal ISpecificationFactoryInternal.For(LambdaExpression arg)
    {
        var expressionFactory = ExpressionFactory;
        var delegateFactory = DelegateFactory;

        var param = (Expression<Func<TParam>>)arg;

        TParam value = default!;

        // todo: not consistent behavior expression vs delegate when factories contain IIF
        return new Specification<TSource>(
            new Lazy<Expression<Func<TSource, bool>>>(() => expressionFactory(value).BindArgument(param, expressionFactory.Method.GetParameters()[0])),
            new Lazy<Func<TSource, bool>>(() => source => delegateFactory(param.Compile()())(source), LazyThreadSafetyMode.PublicationOnly)
        );
    }
}