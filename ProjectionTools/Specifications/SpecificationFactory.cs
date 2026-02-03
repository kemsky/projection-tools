using System.Linq.Expressions;
using DelegateDecompiler;
using ProjectionTools.Assertions;
using ProjectionTools.Expressions;

namespace ProjectionTools.Specifications;

/// <summary>
/// A factory for creating <see cref="Specification{TSource}"/> instances that depend on a parameter.
/// This is useful for specifications that are not constant and need to be configured at runtime.
/// </summary>
/// <typeparam name="TSource">The type of the object to which the specification is applied.</typeparam>
/// <typeparam name="TParam">The type of the parameter used to create the specification.</typeparam>
public sealed class SpecificationFactory<TSource, TParam> : ISpecificationFactoryInternal
{
    internal Func<TParam, Expression<Func<TSource, bool>>> ExpressionFactory => _lazyExpressionFactory.Value;

    internal Func<TParam, Func<TSource, bool>> DelegateFactory => _lazyDelegateFactory.Value;

    private readonly Lazy<Func<TParam, Expression<Func<TSource, bool>>>> _lazyExpressionFactory;

    private readonly Lazy<Func<TParam, Func<TSource, bool>>> _lazyDelegateFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="SpecificationFactory{TSource, TParam}"/> class with an expression factory.
    /// </summary>
    /// <param name="expressionFactory">The factory for creating the specification expression. The delegate factory will be created by compiling the expression.</param>
    public SpecificationFactory(Func<TParam, Expression<Func<TSource, bool>>> expressionFactory)
    {
        Defensive.Contract.ArgumentNotNull(expressionFactory);

        _lazyExpressionFactory = new Lazy<Func<TParam, Expression<Func<TSource, bool>>>>(() => expressionFactory, LazyThreadSafetyMode.PublicationOnly);

        _lazyDelegateFactory = new Lazy<Func<TParam, Func<TSource, bool>>>(() => x => expressionFactory(x).Compile(), LazyThreadSafetyMode.PublicationOnly);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SpecificationFactory{TSource, TParam}"/> class with optional expression and delegate factories.
    /// </summary>
    /// <param name="expressionFactory">The factory for creating the specification expression. If null, it will be created by decompiling the <paramref name="delegateFactory"/>.</param>
    /// <param name="delegateFactory">The factory for creating the specification delegate.</param>
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

    /// <summary>
    /// Creates a <see cref="Specification{TSource}"/> instance for the specified parameter.
    /// <br/>
    /// Warning: can be used in expressions if expression factory does not contain conditional access.
    /// </summary>
    /// <param name="param">The parameter to be used by the specification factories.</param>
    /// <returns>A new <see cref="Specification{TSource}"/>.</returns>
    public Specification<TSource> For(TParam param)
    {
        var lazyExpressionFactory = _lazyExpressionFactory;
        var lazyDelegateFactory = _lazyDelegateFactory;

        return new(
            new Lazy<Expression<Func<TSource, bool>>>(() => lazyExpressionFactory.Value(param), LazyThreadSafetyMode.PublicationOnly),
            new Lazy<Func<TSource, bool>>(() => lazyDelegateFactory.Value(param), LazyThreadSafetyMode.PublicationOnly)
        );
    }

    ISpecificationInternal ISpecificationFactoryInternal.For(object? arg)
    {
        return For((TParam)arg!);
    }

    ISpecificationInternal ISpecificationFactoryInternal.For(Expression arg)
    {
        var lazyExpressionFactory = _lazyExpressionFactory;

        TParam value = default!;

        // todo: not consistent behavior expression vs delegate when factories contain IIF
        return new Specification<TSource>(
            new Lazy<Expression<Func<TSource, bool>>>(() => lazyExpressionFactory.Value(value).BindArgument(arg, lazyExpressionFactory.Value.Method.GetParameters()[0])),
            new Lazy<Func<TSource, bool>>(() => throw new NotSupportedException($"Parameter expression: {arg}"), LazyThreadSafetyMode.PublicationOnly)
        );
    }
}