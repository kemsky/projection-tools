using System.Linq.Expressions;
using System.Reflection;
using DelegateDecompiler;
using ProjectionTools.Assertions;
using ProjectionTools.Expressions;

namespace ProjectionTools.Projections;

/// <summary>
/// A factory for creating <see cref="Projection{TSource, TResult}"/> instances that depend on a parameter.
/// This is useful for projections that are not constant and need to be configured at runtime.
/// </summary>
/// <typeparam name="TSource">The source type of the projection.</typeparam>
/// <typeparam name="TResult">The result type of the projection.</typeparam>
/// <typeparam name="TParam">The type of the parameter used to create the projection.</typeparam>
public sealed class ProjectionFactory<TSource, TResult, TParam>
{
    internal Func<TParam, Expression<Func<TSource, TResult>>> ExpressionFactory => _lazyExpression.Value;

    internal Func<TParam, Func<TSource, TResult>> DelegateFactory => _lazyDelegate.Value;

    private readonly Lazy<Func<TParam, Expression<Func<TSource, TResult>>>> _lazyExpression;

    private readonly Lazy<Func<TParam, Func<TSource, TResult>>> _lazyDelegate;

    // used to bind captured argument in For(Func<TParam>)
    private readonly ParameterInfo _parameter;

    // true when delegates are compiled from expressions (no delegate factory was provided)
    private readonly bool _compileDelegate;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectionFactory{TSource, TResult, TParam}"/> class with an expression factory.
    /// </summary>
    /// <param name="expressionFactory">The factory for creating the projection expression. The delegate factory will be created by compiling the expression.</param>
    public ProjectionFactory(Func<TParam, Expression<Func<TSource, TResult>>> expressionFactory)
    {
        Defensive.Contract.ArgumentNotNull(expressionFactory);

        _lazyExpression = new Lazy<Func<TParam, Expression<Func<TSource, TResult>>>>(() => expressionFactory, LazyThreadSafetyMode.PublicationOnly);

        _lazyDelegate = new Lazy<Func<TParam, Func<TSource, TResult>>>(() => x => expressionFactory(x).Compile(), LazyThreadSafetyMode.PublicationOnly);

        _parameter = expressionFactory.Method.GetParameters()[0];

        _compileDelegate = true;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectionFactory{TSource, TResult, TParam}"/> class with optional expression and delegate factories.
    /// </summary>
    /// <param name="expressionFactory">The factory for creating the projection expression. If null, it will be created by decompiling the <paramref name="delegateFactory"/>.</param>
    /// <param name="delegateFactory">The factory for creating the projection delegate.</param>
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

        _parameter = ((Delegate?)expressionFactory ?? delegateFactory).Method.GetParameters()[0];
    }

    internal ProjectionFactory(
        Lazy<Func<TParam, Expression<Func<TSource, TResult>>>> expressionFactory,
        Lazy<Func<TParam, Func<TSource, TResult>>> delegateFactory,
        ParameterInfo parameter,
        bool compileDelegate
    )
    {
        Defensive.Contract.ArgumentNotNull(expressionFactory);
        Defensive.Contract.ArgumentNotNull(delegateFactory);
        Defensive.Contract.ArgumentNotNull(parameter);

        _lazyExpression = expressionFactory;
        _lazyDelegate = delegateFactory;
        _parameter = parameter;
        _compileDelegate = compileDelegate;
    }

    /// <summary>
    /// Creates a <see cref="Projection{TSource, TResult}"/> instance for the specified parameter.
    /// </summary>
    /// <remarks>
    /// Calling this method inside an expression is not supported. Call it outside the expression and use the result inside it.
    /// </remarks>
    /// <param name="param">The parameter to be used by the projection factories.</param>
    /// <returns>A new <see cref="Projection{TSource, TResult}"/>.</returns>
    public Projection<TSource, TResult> For(TParam param)
    {
        var lazyExpressionFactory = _lazyExpression;
        var lazyDelegateFactory = _lazyDelegate;

        var lazyExpression = new Lazy<Expression<Func<TSource, TResult>>>(() => lazyExpressionFactory.Value(param).Rewrite(), LazyThreadSafetyMode.PublicationOnly);

        var lazyDelegate = _compileDelegate
            ? new Lazy<Func<TSource, TResult>>(() => lazyExpression.Value.Compile(), LazyThreadSafetyMode.PublicationOnly)
            : new Lazy<Func<TSource, TResult>>(() => lazyDelegateFactory.Value(param), LazyThreadSafetyMode.PublicationOnly);

        return new(lazyExpression, lazyDelegate);
    }

    /// <summary>
    /// Creates a <see cref="Projection{TSource, TResult}"/> instance from a parameter factory.
    /// This overload is experimental and allows the parameter to be provided as a delegate, which can be useful in nested queries.
    /// <b>Warning:</b> This may lead to inconsistent behavior between the expression and delegate forms if the factories contain conditional logic (e.g., `?:`).
    /// </summary>
    /// <remarks>
    /// Calling this method inside an expression is not supported. Call it outside the expression and use the result inside it.
    /// </remarks>
    /// <param name="param">A delegate that provides the parameter for the projection factories.</param>
    /// <returns>A new <see cref="Projection{TSource, TResult}"/>.</returns>
    public Projection<TSource, TResult> For(Func<TParam> param)
    {
        var expressionFactory = ExpressionFactory;
        var delegateFactory = DelegateFactory;
        var parameter = _parameter;

        // todo: not consistent behavior expression vs delegate when factories contain IIF
        var lazyExpression = new Lazy<Expression<Func<TSource, TResult>>>(() => expressionFactory(param()).BindArgument((Expression<Func<TParam>>)param.Decompile(), parameter).Rewrite(), LazyThreadSafetyMode.PublicationOnly);

        // bound expression evaluates param() on each call, so it is compiled only once
        var lazyDelegate = _compileDelegate
            ? new Lazy<Func<TSource, TResult>>(() => lazyExpression.Value.Compile(), LazyThreadSafetyMode.PublicationOnly)
            : new Lazy<Func<TSource, TResult>>(() => source => delegateFactory(param())(source), LazyThreadSafetyMode.PublicationOnly);

        return new(lazyExpression, lazyDelegate);
    }
}