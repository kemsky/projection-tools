using System.Linq.Expressions;
using DelegateDecompiler;
using ProjectionTools.Assertions;
using ProjectionTools.Expressions;

namespace ProjectionTools.Projections;

/// <summary>
/// A factory for creating <see cref="Projection{TSource, TResult}"/> instances that depend on two parameters.
/// This is useful for projections that are not constant and need to be configured at runtime.
/// </summary>
/// <typeparam name="TSource">The source type of the projection.</typeparam>
/// <typeparam name="TResult">The result type of the projection.</typeparam>
/// <typeparam name="TParam1">The type of the first parameter used to create the projection.</typeparam>
/// <typeparam name="TParam2">The type of the second parameter used to create the projection.</typeparam>
public sealed class ProjectionFactory<TSource, TResult, TParam1, TParam2>
{
    internal Func<TParam1, TParam2, Expression<Func<TSource, TResult>>> ExpressionFactory => _lazyExpression.Value;

    internal Func<TParam1, TParam2, Func<TSource, TResult>> DelegateFactory => _lazyDelegate.Value;

    private readonly Lazy<Func<TParam1, TParam2, Expression<Func<TSource, TResult>>>> _lazyExpression;

    private readonly Lazy<Func<TParam1, TParam2, Func<TSource, TResult>>> _lazyDelegate;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectionFactory{TSource, TResult, TParam1, TParam2}"/> class with an expression factory.
    /// </summary>
    /// <param name="expressionFactory">The factory for creating the projection expression. The delegate factory will be created by compiling the expression.</param>
    public ProjectionFactory(Func<TParam1, TParam2, Expression<Func<TSource, TResult>>> expressionFactory)
    {
        Defensive.Contract.ArgumentNotNull(expressionFactory);

        _lazyExpression = new Lazy<Func<TParam1, TParam2, Expression<Func<TSource, TResult>>>>(() => expressionFactory, LazyThreadSafetyMode.PublicationOnly);

        _lazyDelegate = new Lazy<Func<TParam1, TParam2, Func<TSource, TResult>>>(() => (x, y) => expressionFactory(x, y).Compile(), LazyThreadSafetyMode.PublicationOnly);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectionFactory{TSource, TResult, TParam1, TParam2}"/> class with optional expression and delegate factories.
    /// </summary>
    /// <param name="expressionFactory">The factory for creating the projection expression. If null, it will be created by decompiling the <paramref name="delegateFactory"/>.</param>
    /// <param name="delegateFactory">The factory for creating the projection delegate.</param>
    public ProjectionFactory(Func<TParam1, TParam2, Expression<Func<TSource, TResult>>>? expressionFactory, Func<TParam1, TParam2, Func<TSource, TResult>> delegateFactory)
    {
        Defensive.Contract.ArgumentNotNull(delegateFactory);

        _lazyExpression = expressionFactory == null
            ? new Lazy<Func<TParam1, TParam2, Expression<Func<TSource, TResult>>>>(() => (x, y) => (Expression<Func<TSource, TResult>>)delegateFactory(x, y).Decompile(), LazyThreadSafetyMode.PublicationOnly)
            : new Lazy<Func<TParam1, TParam2, Expression<Func<TSource, TResult>>>>(() => expressionFactory, LazyThreadSafetyMode.PublicationOnly);

        _lazyDelegate = new Lazy<Func<TParam1, TParam2, Func<TSource, TResult>>>(() => delegateFactory, LazyThreadSafetyMode.PublicationOnly);
    }

    /// <summary>
    /// Partially applies the factory by providing the first parameter, returning a new factory that only requires the second parameter.
    /// </summary>
    /// <param name="param1">The first parameter to apply.</param>
    /// <returns>A new <see cref="ProjectionFactory{TSource, TResult, TParam2}"/> that takes the remaining parameter.</returns>
    public ProjectionFactory<TSource, TResult, TParam2> For(TParam1 param1)
    {
        var expressionFactory = ExpressionFactory;
        var delegateFactory = DelegateFactory;

        return new(param2 => expressionFactory(param1, param2), param2 => delegateFactory(param1, param2));
    }

    /// <summary>
    /// Creates a <see cref="Projection{TSource, TResult}"/> instance by providing both parameters to the factory.
    /// </summary>
    /// <param name="param1">The first parameter to be used by the projection factories.</param>
    /// <param name="param2">The second parameter to be used by the projection factories.</param>
    /// <returns>A new <see cref="Projection{TSource, TResult}"/>.</returns>
    public Projection<TSource, TResult> For(TParam1 param1, TParam2 param2)
    {
        return new(ExpressionFactory(param1, param2), DelegateFactory(param1, param2));
    }

    /// <summary>
    /// Creates a <see cref="Projection{TSource, TResult}"/> instance from parameter factories.
    /// This overload is experimental and allows parameters to be provided as delegates, which can be useful in nested queries.
    /// <br/>
    /// <b>Warning:</b> This may lead to inconsistent behavior between the expression and delegate forms if the factories contain conditional logic (e.g., `?:`).
    /// </summary>
    /// <param name="param1">A delegate that provides the first parameter for the projection factories.</param>
    /// <param name="param2">A delegate that provides the second parameter for the projection factories.</param>
    /// <returns>A new <see cref="Projection{TSource, TResult}"/>.</returns>
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

    /// <summary>
    /// Partially applies the factory by providing the first parameter as a delegate, returning a new factory that only requires the second parameter.
    /// This overload is experimental and allows the parameter to be provided as a delegate, which can be useful in nested queries.
    /// <br/>
    /// <b>Warning:</b> This may lead to inconsistent behavior between the expression and delegate forms if the factories contain conditional logic (e.g., `?:`).
    /// </summary>
    /// <param name="param1">A delegate that provides the first parameter to apply.</param>
    /// <returns>A new <see cref="ProjectionFactory{TSource, TResult, TParam2}"/> that takes the remaining parameter.</returns>
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