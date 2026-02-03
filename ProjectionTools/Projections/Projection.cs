using System.Diagnostics;
using System.Linq.Expressions;
using DelegateDecompiler;
using ProjectionTools.Assertions;
using ProjectionTools.Expressions;

namespace ProjectionTools.Projections;

/// <summary>
/// Represents a reusable projection from a source type to a result type.
/// It encapsulates both an <see cref="Expression"/> for querying and a <see cref="Func{T, TResult}"/> for in-memory execution.
/// Projections can be chained and combined to build complex object mappings.
/// </summary>
/// <typeparam name="TSource">The source type of the projection.</typeparam>
/// <typeparam name="TResult">The result type of the projection.</typeparam>
[DebuggerDisplay("{ProjectExpression}")]
public sealed class Projection<TSource, TResult> : IProjectionExpressionAccessor
{
    /// <summary>
    /// Gets the expression that represents the projection.
    /// </summary>
    public Expression<Func<TSource, TResult>> ProjectExpression => _lazyExpression.Value;

    /// <summary>
    /// Gets the compiled delegate that represents the projection for in-memory execution.
    /// </summary>
    public Func<TSource, TResult> Project => _lazyDelegate.Value;

    private readonly Lazy<Func<TSource, TResult>> _lazyDelegate;

    private readonly Lazy<Expression<Func<TSource, TResult>>> _lazyExpression;

    /// <summary>
    /// Initializes a new instance of the <see cref="Projection{TSource, TResult}"/> class from an expression.
    /// The delegate for in-memory execution will be created by compiling the expression.
    /// </summary>
    /// <param name="projectionExpression">The expression representing the projection.</param>
    public Projection(Expression<Func<TSource, TResult>> projectionExpression)
    {
        Defensive.Contract.ArgumentNotNull(projectionExpression);

        _lazyExpression = new Lazy<Expression<Func<TSource, TResult>>>(() => projectionExpression.Rewrite(), LazyThreadSafetyMode.PublicationOnly);

        _lazyDelegate = new Lazy<Func<TSource, TResult>>(() => projectionExpression.Rewrite().Compile(), LazyThreadSafetyMode.PublicationOnly);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Projection{TSource, TResult}"/> class with an optional expression and a delegate.
    /// If the expression is not provided, it will be created by decompiling the delegate.
    /// </summary>
    /// <param name="projectionExpression">The expression representing the projection.</param>
    /// <param name="projectionDelegate">The delegate representing the projection for in-memory execution.</param>
    public Projection(Expression<Func<TSource, TResult>>? projectionExpression, Func<TSource, TResult> projectionDelegate)
    {
        Defensive.Contract.ArgumentNotNull(projectionDelegate);

        _lazyExpression = projectionExpression == null
            ? new Lazy<Expression<Func<TSource, TResult>>>(() => ((Expression<Func<TSource, TResult>>)projectionDelegate.Decompile()).Rewrite(), LazyThreadSafetyMode.PublicationOnly)
            : new Lazy<Expression<Func<TSource, TResult>>>(() => projectionExpression.Rewrite(), LazyThreadSafetyMode.PublicationOnly);

        _lazyDelegate = new Lazy<Func<TSource, TResult>>(() => projectionDelegate, LazyThreadSafetyMode.PublicationOnly);
    }

    internal Projection(Lazy<Expression<Func<TSource, TResult>>> projectionExpression, Lazy<Func<TSource, TResult>> projectionDelegate)
    {
        Defensive.Contract.ArgumentNotNull(projectionExpression);
        Defensive.Contract.ArgumentNotNull(projectionDelegate);

        _lazyExpression = projectionExpression;

        _lazyDelegate = projectionDelegate;
    }

    /// <summary>
    /// Chains a projection by applying it to a new source type.
    /// </summary>
    /// <typeparam name="TProjection">The new source type.</typeparam>
    /// <param name="sourceExpression">The expression to project from the new source type to the current source type.</param>
    /// <returns>A new <see cref="Projection{TSource, TResult}"/> from <typeparamref name="TProjection"/> to <typeparamref name="TResult"/>.</returns>
    public Projection<TProjection, TResult> ApplyTo<TProjection>(Expression<Func<TProjection, TSource>> sourceExpression)
    {
        Defensive.Contract.ArgumentNotNull(sourceExpression);

        var delegateLocal = Project;
        var expressionLocal = ProjectExpression;

        return new Projection<TProjection, TResult>(
            new Lazy<Expression<Func<TProjection, TResult>>>(() => expressionLocal.ApplyTo(sourceExpression), LazyThreadSafetyMode.PublicationOnly),
            new Lazy<Func<TProjection, TResult>>(() =>
            {
                var sourceDelegate = sourceExpression.Compile();

                return source => delegateLocal(sourceDelegate(source));
            }, LazyThreadSafetyMode.PublicationOnly)
        );
    }

    /// <summary>
    /// Chains a projection by applying it to a new source type, using both an expression and a delegate.
    /// </summary>
    /// <typeparam name="TProjection">The new source type.</typeparam>
    /// <param name="sourceExpression">The expression to project from the new source type to the current source type. If null, it will be decompiled from <paramref name="sourceDelegate"/>.</param>
    /// <param name="sourceDelegate">The delegate to project from the new source type to the current source type.</param>
    /// <returns>A new <see cref="Projection{TSource, TResult}"/> from <typeparamref name="TProjection"/> to <typeparamref name="TResult"/>.</returns>
    public Projection<TProjection, TResult> ApplyTo<TProjection>(Expression<Func<TProjection, TSource>>? sourceExpression, Func<TProjection, TSource> sourceDelegate)
    {
        Defensive.Contract.ArgumentNotNull(sourceDelegate);

        var delegateLocal = Project;
        var expressionLocal = ProjectExpression;

        var lazyExpression = sourceExpression == null
            ? new Lazy<Expression<Func<TProjection, TResult>>>(() => expressionLocal.ApplyTo((Expression<Func<TProjection, TSource>>)sourceDelegate.Decompile()), LazyThreadSafetyMode.PublicationOnly)
            : new Lazy<Expression<Func<TProjection, TResult>>>(() => expressionLocal.ApplyTo(sourceExpression), LazyThreadSafetyMode.PublicationOnly);

        var lazyDelegate = new Lazy<Func<TProjection, TResult>>(() => source => delegateLocal(sourceDelegate(source)), LazyThreadSafetyMode.PublicationOnly);

        return new Projection<TProjection, TResult>(
            lazyExpression,
            lazyDelegate
        );
    }

    /// <summary>
    /// Chains a projection by applying it to the result of another projection.
    /// </summary>
    /// <typeparam name="TProjection">The source type of the provided projection.</typeparam>
    /// <param name="sourceProjection">The projection to apply before this one.</param>
    /// <returns>A new <see cref="Projection{TSource, TResult}"/> from <typeparamref name="TProjection"/> to <typeparamref name="TResult"/>.</returns>
    public Projection<TProjection, TResult> ApplyTo<TProjection>(Projection<TProjection, TSource> sourceProjection)
    {
        Defensive.Contract.ArgumentNotNull(sourceProjection);

        var delegateLocal = Project;
        var expressionLocal = ProjectExpression;

        return new Projection<TProjection, TResult>(
            new Lazy<Expression<Func<TProjection, TResult>>>(() => expressionLocal.ApplyTo(sourceProjection.ProjectExpression), LazyThreadSafetyMode.PublicationOnly),
            new Lazy<Func<TProjection, TResult>>(() => source => delegateLocal(sourceProjection.Project(source)), LazyThreadSafetyMode.PublicationOnly)
        );
    }

    /// <summary>
    /// Chains this projection with another one, projecting the result of this projection to a new type.
    /// </summary>
    /// <typeparam name="TProjection">The new result type.</typeparam>
    /// <param name="projectionExpression">The expression to project from the current result type to the new result type.</param>
    /// <returns>A new <see cref="Projection{TSource, TResult}"/> from <typeparamref name="TSource"/> to <typeparamref name="TProjection"/>.</returns>
    public Projection<TSource, TProjection> To<TProjection>(Expression<Func<TResult, TProjection>> projectionExpression)
    {
        Defensive.Contract.ArgumentNotNull(projectionExpression);

        var delegateLocal = Project;
        var expressionLocal = ProjectExpression;

        return new Projection<TSource, TProjection>(
            new Lazy<Expression<Func<TSource, TProjection>>>(() => expressionLocal.ProjectTo(projectionExpression), LazyThreadSafetyMode.PublicationOnly),
            new Lazy<Func<TSource, TProjection>>(() =>
            {
                var projectionDelegate = projectionExpression.Compile();

                return source => projectionDelegate(delegateLocal(source));
            }, LazyThreadSafetyMode.PublicationOnly)
        );
    }

    /// <summary>
    /// Chains this projection with another one, projecting the result of this projection to a new type, using both an expression and a delegate.
    /// </summary>
    /// <typeparam name="TProjection">The new result type.</typeparam>
    /// <param name="projectionExpression">The expression to project from the current result type to the new result type. If null, it will be decompiled from <paramref name="projectionDelegate"/>.</param>
    /// <param name="projectionDelegate">The delegate to project from the current result type to the new result type.</param>
    /// <returns>A new <see cref="Projection{TSource, TResult}"/> from <typeparamref name="TSource"/> to <typeparamref name="TProjection"/>.</returns>
    public Projection<TSource, TProjection> To<TProjection>(Expression<Func<TResult, TProjection>>? projectionExpression, Func<TResult, TProjection> projectionDelegate)
    {
        Defensive.Contract.ArgumentNotNull(projectionDelegate);

        var delegateLocal = Project;
        var expressionLocal = ProjectExpression;

        var lazyExpression = projectionExpression == null
            ? new Lazy<Expression<Func<TSource, TProjection>>>(() => expressionLocal.ProjectTo((Expression<Func<TResult, TProjection>>)projectionDelegate.Decompile()), LazyThreadSafetyMode.PublicationOnly)
            : new Lazy<Expression<Func<TSource, TProjection>>>(() => expressionLocal.ProjectTo(projectionExpression), LazyThreadSafetyMode.PublicationOnly);

        var lazyDelegate = new Lazy<Func<TSource, TProjection>>(() => source => projectionDelegate(delegateLocal(source)), LazyThreadSafetyMode.PublicationOnly);

        return new Projection<TSource, TProjection>(
            lazyExpression,
            lazyDelegate
        );
    }

    /// <summary>
    /// Chains this projection with another one, projecting the result of this projection to a new type.
    /// </summary>
    /// <typeparam name="TProjection">The result type of the provided projection.</typeparam>
    /// <param name="projection">The projection to apply after this one.</param>
    /// <returns>A new <see cref="Projection{TSource, TResult}"/> from <typeparamref name="TSource"/> to <typeparamref name="TProjection"/>.</returns>
    public Projection<TSource, TProjection> To<TProjection>(Projection<TResult, TProjection> projection)
    {
        Defensive.Contract.ArgumentNotNull(projection);

        var delegateLocal = Project;
        var expressionLocal = ProjectExpression;

        return new Projection<TSource, TProjection>(
            new Lazy<Expression<Func<TSource, TProjection>>>(() => expressionLocal.ProjectTo(projection.ProjectExpression), LazyThreadSafetyMode.PublicationOnly),
            new Lazy<Func<TSource, TProjection>>(() => source => projection.Project(delegateLocal(source)), LazyThreadSafetyMode.PublicationOnly)
        );
    }

    /// <summary>
    /// Allows for implicit conversion of a <see cref="Projection{TSource, TResult}"/> to a <see cref="Func{TSource, TResult}"/>.
    /// </summary>
    /// <param name="f">The projection to convert.</param>
    public static implicit operator Func<TSource, TResult>(Projection<TSource, TResult> f)
    {
        return f.Project;
    }

    /// <summary>
    /// Allows for implicit conversion of a <see cref="Projection{TSource, TResult}"/> to an <see cref="Expression{TDelegate}"/>.
    /// </summary>
    /// <param name="f">The projection to convert.</param>
    public static implicit operator Expression<Func<TSource, TResult>>(Projection<TSource, TResult> f)
    {
        return f.ProjectExpression;
    }

    LambdaExpression IProjectionExpressionAccessor.GetExpression()
    {
        return ProjectExpression;
    }
}