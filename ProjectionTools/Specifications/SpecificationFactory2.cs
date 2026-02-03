using System.Linq.Expressions;
using DelegateDecompiler;
using ProjectionTools.Assertions;
using ProjectionTools.Expressions;

namespace ProjectionTools.Specifications;

/// <summary>
/// A factory for creating <see cref="Specification{TSource}"/> instances that depend on two parameters.
/// This is useful for specifications that are not constant and need to be configured at runtime.
/// </summary>
/// <typeparam name="TSource">The type of the object to which the specification is applied.</typeparam>
/// <typeparam name="TParam1">The type of the first parameter used to create the specification.</typeparam>
/// <typeparam name="TParam2">The type of the second parameter used to create the specification.</typeparam>
public sealed class SpecificationFactory<TSource, TParam1, TParam2> : ISpecificationFactory2Internal
{
    internal Func<TParam1, TParam2, Expression<Func<TSource, bool>>> ExpressionFactory => _lazyExpressionFactory.Value;

    internal Func<TParam1, TParam2, Func<TSource, bool>> DelegateFactory => _lazyDelegateFactory.Value;

    private readonly Lazy<Func<TParam1, TParam2, Expression<Func<TSource, bool>>>> _lazyExpressionFactory;

    private readonly Lazy<Func<TParam1, TParam2, Func<TSource, bool>>> _lazyDelegateFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="SpecificationFactory{TSource, TParam1, TParam2}"/> class with an expression factory.
    /// </summary>
    /// <param name="expressionFactory">The factory for creating the specification expression. The delegate factory will be created by compiling the expression.</param>
    public SpecificationFactory(Func<TParam1, TParam2, Expression<Func<TSource, bool>>> expressionFactory)
    {
        Defensive.Contract.ArgumentNotNull(expressionFactory);

        _lazyExpressionFactory = new Lazy<Func<TParam1, TParam2, Expression<Func<TSource, bool>>>>(() => expressionFactory, LazyThreadSafetyMode.PublicationOnly);

        _lazyDelegateFactory = new Lazy<Func<TParam1, TParam2, Func<TSource, bool>>>(() => (x, y) => expressionFactory(x, y).Compile(), LazyThreadSafetyMode.PublicationOnly);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SpecificationFactory{TSource, TParam1, TParam2}"/> class with optional expression and delegate factories.
    /// </summary>
    /// <param name="expressionFactory">The factory for creating the specification expression. If null, it will be created by decompiling the <paramref name="delegateFactory"/>.</param>
    /// <param name="delegateFactory">The factory for creating the specification delegate.</param>
    public SpecificationFactory(Func<TParam1, TParam2, Expression<Func<TSource, bool>>>? expressionFactory, Func<TParam1, TParam2, Func<TSource, bool>> delegateFactory)
    {
        Defensive.Contract.ArgumentNotNull(delegateFactory);

        _lazyExpressionFactory = expressionFactory == null
            ? new Lazy<Func<TParam1, TParam2, Expression<Func<TSource, bool>>>>(() => (x, y) => (Expression<Func<TSource, bool>>)delegateFactory(x, y).Decompile(), LazyThreadSafetyMode.PublicationOnly)
            : new Lazy<Func<TParam1, TParam2, Expression<Func<TSource, bool>>>>(() => expressionFactory, LazyThreadSafetyMode.PublicationOnly);

        _lazyDelegateFactory = new Lazy<Func<TParam1, TParam2, Func<TSource, bool>>>(() => delegateFactory, LazyThreadSafetyMode.PublicationOnly);
    }

    /// <summary>
    /// Partially applies the factory by providing the first parameter, returning a new factory that only requires the second parameter.
    /// <br/>
    /// Warning: can be used in expressions if expression factory does not contain conditional access.
    /// </summary>
    /// <param name="param1">The first parameter to apply.</param>
    /// <returns>A new <see cref="SpecificationFactory{TSource, TParam2}"/> that takes the remaining parameter.</returns>
    public SpecificationFactory<TSource, TParam2> For(TParam1 param1)
    {
        var lazyExpressionFactory = _lazyExpressionFactory;
        var lazyDelegateFactory = _lazyDelegateFactory;

        return new(
            new Lazy<Func<TParam2, Expression<Func<TSource, bool>>>>(() => param2 => lazyExpressionFactory.Value(param1, param2), LazyThreadSafetyMode.PublicationOnly),
            new Lazy<Func<TParam2, Func<TSource, bool>>>(() => param2 => lazyDelegateFactory.Value(param1, param2), LazyThreadSafetyMode.PublicationOnly)
        );
    }

    /// <summary>
    /// Creates a <see cref="Specification{TSource}"/> instance by providing both parameters to the factory.
    /// <br/>
    /// Warning: can be used in expressions if expression factory does not contain conditional access.
    /// </summary>
    /// <param name="param1">The first parameter to be used by the specification factories.</param>
    /// <param name="param2">The second parameter to be used by the specification factories.</param>
    /// <returns>A new <see cref="Specification{TSource}"/>.</returns>
    public Specification<TSource> For(TParam1 param1, TParam2 param2)
    {
        var lazyExpressionFactory = _lazyExpressionFactory;
        var lazyDelegateFactory = _lazyDelegateFactory;

        return new(
            new Lazy<Expression<Func<TSource, bool>>>(() => lazyExpressionFactory.Value(param1, param2), LazyThreadSafetyMode.PublicationOnly),
            new Lazy<Func<TSource, bool>>(() => lazyDelegateFactory.Value(param1, param2), LazyThreadSafetyMode.PublicationOnly)
        );
    }

    ISpecificationFactoryInternal ISpecificationFactory2Internal.For(object? arg1)
    {
        return For((TParam1)arg1!);
    }

    ISpecificationInternal ISpecificationFactory2Internal.For(object? arg1, object? arg2)
    {
        return For((TParam1)arg1!, (TParam2)arg2!);
    }

    ISpecificationFactoryInternal ISpecificationFactory2Internal.For(Expression arg1)
    {
        var lazyExpressionFactory = _lazyExpressionFactory;

        TParam1 value1 = default!;

        // todo: not consistent behavior expression vs delegate when factories contain IIF
        return new SpecificationFactory<TSource, TParam2>(
            new Lazy<Func<TParam2, Expression<Func<TSource, bool>>>>(
                () => param2 => lazyExpressionFactory.Value(value1, param2).BindArgument(arg1, lazyExpressionFactory.Value.Method.GetParameters()[0])
                    .BindArgument(Expression.Constant(param2), lazyExpressionFactory.Value.Method.GetParameters()[1]), LazyThreadSafetyMode.PublicationOnly),
            new Lazy<Func<TParam2, Func<TSource, bool>>>(() => throw new NotSupportedException($"Parameter expressions: {arg1}"), LazyThreadSafetyMode.PublicationOnly)
        );
    }

    ISpecificationInternal ISpecificationFactory2Internal.For(Expression arg1, Expression arg2)
    {
        var lazyExpressionFactory = _lazyExpressionFactory;

        TParam1 value1 = default!;
        TParam2 value2 = default!;

        // todo: not consistent behavior expression vs delegate when factories contain IIF
        return new Specification<TSource>(
            new Lazy<Expression<Func<TSource, bool>>>(() =>
                lazyExpressionFactory.Value(value1, value2).BindArgument(arg1, lazyExpressionFactory.Value.Method.GetParameters()[0]).BindArgument(arg2, lazyExpressionFactory.Value.Method.GetParameters()[1])),
            new Lazy<Func<TSource, bool>>(() => throw new NotSupportedException($"Parameter expressions: {arg1}, {arg2}"), LazyThreadSafetyMode.PublicationOnly)
        );
    }
}