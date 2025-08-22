using System.Diagnostics;
using System.Linq.Expressions;
using DelegateDecompiler;
using ProjectionTools.Assertions;
using ProjectionTools.Expressions;

namespace ProjectionTools.Projections;

[DebuggerDisplay("{ProjectExpression}")]
public sealed class Projection<TSource, TResult> : IProjectionExpressionAccessor
{
    public Expression<Func<TSource, TResult>> ProjectExpression => _lazyExpression.Value;

    public Func<TSource, TResult> Project => _lazyDelegate.Value;

    private readonly Lazy<Func<TSource, TResult>> _lazyDelegate;

    private readonly Lazy<Expression<Func<TSource, TResult>>> _lazyExpression;

    public Projection(Expression<Func<TSource, TResult>> projectionExpression)
    {
        Defensive.Contract.ArgumentNotNull(projectionExpression);

        _lazyExpression = new Lazy<Expression<Func<TSource, TResult>>>(() => projectionExpression.Rewrite(), LazyThreadSafetyMode.PublicationOnly);

        _lazyDelegate = new Lazy<Func<TSource, TResult>>(() => projectionExpression.Rewrite().Compile(), LazyThreadSafetyMode.PublicationOnly);
    }

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

    public static implicit operator Func<TSource, TResult>(Projection<TSource, TResult> f)
    {
        return f.Project;
    }

    public static implicit operator Expression<Func<TSource, TResult>>(Projection<TSource, TResult> f)
    {
        return f.ProjectExpression;
    }

    LambdaExpression IProjectionExpressionAccessor.GetExpression()
    {
        return ProjectExpression;
    }
}