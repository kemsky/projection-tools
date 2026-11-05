using System.Linq.Expressions;
using System.Reflection;
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

    // used to bind captured arguments in For(Func<...>)
    private readonly ParameterInfo _parameter1;

    // used to bind captured arguments in For(Func<...>)
    private readonly ParameterInfo _parameter2;

    // true when delegates are compiled from expressions (no delegate factory was provided)
    private readonly bool _compileDelegate;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectionFactory{TSource, TResult, TParam1, TParam2}"/> class with an expression factory.
    /// </summary>
    /// <param name="expressionFactory">The factory for creating the projection expression. The delegate factory will be created by compiling the expression.</param>
    public ProjectionFactory(Func<TParam1, TParam2, Expression<Func<TSource, TResult>>> expressionFactory)
    {
        Defensive.Contract.ArgumentNotNull(expressionFactory);

        _lazyExpression = new Lazy<Func<TParam1, TParam2, Expression<Func<TSource, TResult>>>>(() => expressionFactory, LazyThreadSafetyMode.PublicationOnly);

        _lazyDelegate = new Lazy<Func<TParam1, TParam2, Func<TSource, TResult>>>(() => (x, y) => expressionFactory(x, y).Compile(), LazyThreadSafetyMode.PublicationOnly);

        var parameters = expressionFactory.Method.GetParameters();

        _parameter1 = parameters[0];
        _parameter2 = parameters[1];

        _compileDelegate = true;
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


        var parameters = ((Delegate?)expressionFactory ?? delegateFactory).Method.GetParameters();

        _parameter1 = parameters[0];
        _parameter2 = parameters[1];
    }

    /// <summary>
    /// Partially applies the factory by providing the first parameter, returning a new factory that only requires the second parameter.
    /// </summary>
    /// <remarks>
    /// Calling this method inside an expression is not supported. Call it outside the expression and use the result inside it.
    /// </remarks>
    /// <param name="param1">The first parameter to apply.</param>
    /// <returns>A new <see cref="ProjectionFactory{TSource, TResult, TParam2}"/> that takes the remaining parameter.</returns>
    public ProjectionFactory<TSource, TResult, TParam2> For(TParam1 param1)
    {
        var expressionFactory = ExpressionFactory;
        var delegateFactory = DelegateFactory;

        return new(
            new Lazy<Func<TParam2, Expression<Func<TSource, TResult>>>>(() => param2 => expressionFactory(param1, param2), LazyThreadSafetyMode.PublicationOnly),
            new Lazy<Func<TParam2, Func<TSource, TResult>>>(() => param2 => delegateFactory(param1, param2), LazyThreadSafetyMode.PublicationOnly),
            _parameter2,
            _compileDelegate
        );
    }

    /// <summary>
    /// Creates a <see cref="Projection{TSource, TResult}"/> instance by providing both parameters to the factory.
    /// </summary>
    /// <remarks>
    /// Calling this method inside an expression is not supported. Call it outside the expression and use the result inside it.
    /// </remarks>
    /// <param name="param1">The first parameter to be used by the projection factories.</param>
    /// <param name="param2">The second parameter to be used by the projection factories.</param>
    /// <returns>A new <see cref="Projection{TSource, TResult}"/>.</returns>
    public Projection<TSource, TResult> For(TParam1 param1, TParam2 param2)
    {
        var lazyExpressionFactory = _lazyExpression;
        var lazyDelegateFactory = _lazyDelegate;

        var lazyExpression = new Lazy<Expression<Func<TSource, TResult>>>(() => lazyExpressionFactory.Value(param1, param2).Rewrite(), LazyThreadSafetyMode.PublicationOnly);

        var lazyDelegate = _compileDelegate
            ? new Lazy<Func<TSource, TResult>>(() => lazyExpression.Value.Compile(), LazyThreadSafetyMode.PublicationOnly)
            : new Lazy<Func<TSource, TResult>>(() => lazyDelegateFactory.Value(param1, param2), LazyThreadSafetyMode.PublicationOnly);

        return new(lazyExpression, lazyDelegate);
    }

    /// <summary>
    /// Creates a <see cref="Projection{TSource, TResult}"/> instance from parameter factories.
    /// This overload is experimental and allows parameters to be provided as delegates, which can be useful in nested queries.
    /// <br/>
    /// <b>Warning:</b> This may lead to inconsistent behavior between the expression and delegate forms if the factories contain conditional logic (e.g., `?:`).
    /// </summary>
    /// <remarks>
    /// Calling this method inside an expression is not supported. Call it outside the expression and use the result inside it.
    /// </remarks>
    /// <param name="param1">A delegate that provides the first parameter for the projection factories.</param>
    /// <param name="param2">A delegate that provides the second parameter for the projection factories.</param>
    /// <returns>A new <see cref="Projection{TSource, TResult}"/>.</returns>
    public Projection<TSource, TResult> For(Func<TParam1> param1, Func<TParam2> param2)
    {
        var expressionFactory = ExpressionFactory;
        var delegateFactory = DelegateFactory;
        var parameter1 = _parameter1;
        var parameter2 = _parameter2;

        // todo: not consistent behavior expression vs delegate when factories contain IIF
        var lazyExpression = new Lazy<Expression<Func<TSource, TResult>>>(() => expressionFactory(param1(), param2()).BindArguments((Expression<Func<TParam1>>)param1.Decompile(), (Expression<Func<TParam2>>)param2.Decompile(), parameter1, parameter2).Rewrite(), LazyThreadSafetyMode.PublicationOnly);

        // bound expression evaluates param1() and param2() on each call, so it is compiled only once
        var lazyDelegate = _compileDelegate
            ? new Lazy<Func<TSource, TResult>>(() => lazyExpression.Value.Compile(), LazyThreadSafetyMode.PublicationOnly)
            : new Lazy<Func<TSource, TResult>>(() => source => delegateFactory(param1(), param2())(source), LazyThreadSafetyMode.PublicationOnly);

        return new(lazyExpression, lazyDelegate);
    }

    /// <summary>
    /// Partially applies the factory by providing the first parameter as a delegate, returning a new factory that only requires the second parameter.
    /// This overload is experimental and allows the parameter to be provided as a delegate, which can be useful in nested queries.
    /// <br/>
    /// <b>Warning:</b> This may lead to inconsistent behavior between the expression and delegate forms if the factories contain conditional logic (e.g., `?:`).
    /// </summary>
    /// <remarks>
    /// Calling this method inside an expression is not supported. Call it outside the expression and use the result inside it.
    /// </remarks>
    /// <param name="param1">A delegate that provides the first parameter to apply.</param>
    /// <returns>A new <see cref="ProjectionFactory{TSource, TResult, TParam2}"/> that takes the remaining parameter.</returns>
    public ProjectionFactory<TSource, TResult, TParam2> For(Func<TParam1> param1)
    {
        var expressionFactory = ExpressionFactory;
        var delegateFactory = DelegateFactory;
        var parameter1 = _parameter1;

        // todo: not consistent behavior expression vs delegate when factories contain IIF
        return new(
            new Lazy<Func<TParam2, Expression<Func<TSource, TResult>>>>(() => param2 => expressionFactory(param1(), param2).BindArgument((Expression<Func<TParam1>>)param1.Decompile(), parameter1), LazyThreadSafetyMode.PublicationOnly),
            new Lazy<Func<TParam2, Func<TSource, TResult>>>(() => param2 => source => delegateFactory(param1(), param2)(source), LazyThreadSafetyMode.PublicationOnly),
            _parameter2,
            _compileDelegate
        );
    }
}