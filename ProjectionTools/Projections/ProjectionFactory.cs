using System.Linq.Expressions;
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

    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectionFactory{TSource, TResult, TParam}"/> class with an expression factory.
    /// </summary>
    /// <param name="expressionFactory">The factory for creating the projection expression. The delegate factory will be created by compiling the expression.</param>
    public ProjectionFactory(Func<TParam, Expression<Func<TSource, TResult>>> expressionFactory)
    {
        Defensive.Contract.ArgumentNotNull(expressionFactory);

        _lazyExpression = new Lazy<Func<TParam, Expression<Func<TSource, TResult>>>>(() => expressionFactory, LazyThreadSafetyMode.PublicationOnly);

        _lazyDelegate = new Lazy<Func<TParam, Func<TSource, TResult>>>(() => x => expressionFactory(x).Compile(), LazyThreadSafetyMode.PublicationOnly);
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
    }

    internal ProjectionFactory(
        Lazy<Func<TParam, Expression<Func<TSource, TResult>>>> expressionFactory,
        Lazy<Func<TParam, Func<TSource, TResult>>> delegateFactory
    )
    {
        Defensive.Contract.ArgumentNotNull(expressionFactory);
        Defensive.Contract.ArgumentNotNull(delegateFactory);

        _lazyExpression = expressionFactory;
        _lazyDelegate = delegateFactory;
    }

    /// <summary>
    /// Creates a <see cref="Projection{TSource, TResult}"/> instance for the specified parameter.
    /// </summary>
    /// <param name="param">The parameter to be used by the projection factories.</param>
    /// <returns>A new <see cref="Projection{TSource, TResult}"/>.</returns>
    public Projection<TSource, TResult> For(TParam param)
    {
        return new(ExpressionFactory(param), DelegateFactory(param));
    }

    /// <summary>
    /// Creates a <see cref="Projection{TSource, TResult}"/> instance from a parameter factory.
    /// This overload is experimental and allows the parameter to be provided as a delegate, which can be useful in nested queries.
    /// <b>Warning:</b> This may lead to inconsistent behavior between the expression and delegate forms if the factories contain conditional logic (e.g., `?:`).
    /// </summary>
    /// <param name="param">A delegate that provides the parameter for the projection factories.</param>
    /// <returns>A new <see cref="Projection{TSource, TResult}"/>.</returns>
    public Projection<TSource, TResult> For(Func<TParam> param)
    {
        var expressionFactory = ExpressionFactory;
        var delegateFactory = DelegateFactory;

        // todo: not consistent behavior expression vs delegate when factories contain IIF
        return new(
            new Lazy<Expression<Func<TSource, TResult>>>(() => expressionFactory(param()).BindArgument((Expression<Func<TParam>>)param.Decompile(), expressionFactory.Method.GetParameters()[0]), LazyThreadSafetyMode.PublicationOnly),
            new Lazy<Func<TSource, TResult>>(() => source => delegateFactory(param())(source), LazyThreadSafetyMode.PublicationOnly)
        );
    }
}