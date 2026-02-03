using System.Diagnostics;
using System.Linq.Expressions;
using DelegateDecompiler;
using ProjectionTools.Assertions;
using ProjectionTools.Expressions;

namespace ProjectionTools.Specifications;

/// <summary>
/// Represents a specification that can be applied to an object of type <typeparamref name="TSource"/>.
/// A specification is a business rule that can be combined with other specifications.
/// It encapsulates both an <see cref="Expression"/> for querying and a <see cref="Func{T, TResult}"/> for in-memory evaluation.
/// </summary>
/// <typeparam name="TSource">The type of the object to which the specification is applied.</typeparam>
[DebuggerDisplay("{IsSatisfiedByExpression}")]
public sealed class Specification<TSource> : ISpecificationExpressionAccessor, ISpecificationInternal
{
    /// <summary>
    /// Gets the expression that represents the specification.
    /// </summary>
    public Expression<Func<TSource, bool>> IsSatisfiedByExpression => _lazyExpression.Value;

    /// <summary>
    /// Gets the compiled delegate that represents the specification for in-memory evaluation.
    /// </summary>
    public Func<TSource, bool> IsSatisfiedBy => _lazyDelegate.Value;

    private readonly Lazy<Func<TSource, bool>> _lazyDelegate;

    private readonly Lazy<Expression<Func<TSource, bool>>> _lazyExpression;

    /// <summary>
    /// Initializes a new instance of the <see cref="Specification{TSource}"/> class from an expression.
    /// The delegate for in-memory evaluation will be created by compiling the expression.
    /// </summary>
    /// <param name="expressionFunc">The expression representing the specification.</param>
    public Specification(Expression<Func<TSource, bool>> expressionFunc)
    {
        Defensive.Contract.ArgumentNotNull(expressionFunc);

        _lazyExpression = new Lazy<Expression<Func<TSource, bool>>>(() => expressionFunc.Rewrite(), LazyThreadSafetyMode.PublicationOnly);

        _lazyDelegate = new Lazy<Func<TSource, bool>>(() =>
        {
            var compiled = expressionFunc.Compile();

            return compiled;
        }, LazyThreadSafetyMode.PublicationOnly);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Specification{TSource}"/> class with an optional expression and a delegate.
    /// If the expression is not provided, it will be created by decompiling the delegate.
    /// </summary>
    /// <param name="expressionFunc">The expression representing the specification.</param>
    /// <param name="delegateFunc">The delegate representing the specification for in-memory evaluation.</param>
    public Specification(Expression<Func<TSource, bool>>? expressionFunc, Func<TSource, bool> delegateFunc)
    {
        Defensive.Contract.ArgumentNotNull(delegateFunc);

        _lazyExpression = expressionFunc == null
            ? new Lazy<Expression<Func<TSource, bool>>>(() => ((Expression<Func<TSource, bool>>)delegateFunc.Decompile()).Rewrite(), LazyThreadSafetyMode.PublicationOnly)
            : new Lazy<Expression<Func<TSource, bool>>>(() => expressionFunc.Rewrite(), LazyThreadSafetyMode.PublicationOnly);

        _lazyDelegate = new Lazy<Func<TSource, bool>>(() => delegateFunc, LazyThreadSafetyMode.PublicationOnly);
    }

    internal Specification(Lazy<Expression<Func<TSource, bool>>> lazyExpression, Lazy<Func<TSource, bool>> lazyDelegate)
    {
        Defensive.Contract.ArgumentNotNull(lazyExpression);
        Defensive.Contract.ArgumentNotNull(lazyDelegate);

        _lazyExpression = lazyExpression;

        _lazyDelegate = lazyDelegate;
    }

    /// <summary>
    /// Combines two specifications using a logical AND operation.
    /// </summary>
    /// <param name="spec1">The first specification.</param>
    /// <param name="spec2">The second specification.</param>
    /// <returns>A new specification that is the logical AND of the two specifications.</returns>
    public static Specification<TSource> operator &(Specification<TSource> spec1, Specification<TSource> spec2)
    {
        Defensive.Contract.ArgumentNotNull(spec1);
        Defensive.Contract.ArgumentNotNull(spec2);

        return new Specification<TSource>(
            new Lazy<Expression<Func<TSource, bool>>>(() => spec1.IsSatisfiedByExpression.And(spec2.IsSatisfiedByExpression), LazyThreadSafetyMode.PublicationOnly),
            new Lazy<Func<TSource, bool>>(() => x => spec1.IsSatisfiedBy(x) && spec2.IsSatisfiedBy(x), LazyThreadSafetyMode.PublicationOnly)
        );
    }

    /// <summary>
    /// Combines two specifications using a logical OR operation.
    /// </summary>
    /// <param name="spec1">The first specification.</param>
    /// <param name="spec2">The second specification.</param>
    /// <returns>A new specification that is the logical OR of the two specifications.</returns>
    public static Specification<TSource> operator |(Specification<TSource> spec1, Specification<TSource> spec2)
    {
        Defensive.Contract.ArgumentNotNull(spec1);
        Defensive.Contract.ArgumentNotNull(spec2);

        return new Specification<TSource>(
            new Lazy<Expression<Func<TSource, bool>>>(() => spec1.IsSatisfiedByExpression.Or(spec2.IsSatisfiedByExpression), LazyThreadSafetyMode.PublicationOnly),
            new Lazy<Func<TSource, bool>>(() => x => spec1.IsSatisfiedBy(x) || spec2.IsSatisfiedBy(x), LazyThreadSafetyMode.PublicationOnly)
        );
    }

    /// <summary>
    /// Negates a specification.
    /// </summary>
    /// <param name="specification">The specification to negate.</param>
    /// <returns>A new specification that is the logical negation of the original specification.</returns>
    public static Specification<TSource> operator !(Specification<TSource> specification)
    {
        Defensive.Contract.ArgumentNotNull(specification);

        return new Specification<TSource>(
            new Lazy<Expression<Func<TSource, bool>>>(() => specification.IsSatisfiedByExpression.Not(), LazyThreadSafetyMode.PublicationOnly),
            new Lazy<Func<TSource, bool>>(() => x => !specification.IsSatisfiedBy(x), LazyThreadSafetyMode.PublicationOnly)
        );
    }

    /// <summary>
    /// Defines the false operator for the specification. This is required to enable the use of `&amp;&amp;` and `||` operators,
    /// but it forces the evaluation of both operands by always returning false, thus preserving the behavior of `&amp;` and `|`.
    /// </summary>
    /// <param name="_">The specification instance.</param>
    /// <returns>Always <c>false</c>.</returns>
    public static bool operator false(Specification<TSource> _) => false;

    /// <summary>
    /// Defines the true operator for the specification. This is required as a pair to the false operator.
    /// </summary>
    /// <param name="_">The specification instance.</param>
    /// <returns>Always <c>false</c>.</returns>
    public static bool operator true(Specification<TSource> _) => false;

    /// <summary>
    /// Applies the specification to a different type by using a projection expression.
    /// </summary>
    /// <typeparam name="TProjection">The type to project from.</typeparam>
    /// <param name="expression">The projection expression from <typeparamref name="TProjection"/> to <typeparamref name="TSource"/>.</param>
    /// <returns>A new specification for the <typeparamref name="TProjection"/> type.</returns>
    public Specification<TProjection> ApplyTo<TProjection>(Expression<Func<TProjection, TSource>> expression)
    {
        Defensive.Contract.ArgumentNotNull(expression);

        var currentDelegate = IsSatisfiedBy;
        var currentExpression = IsSatisfiedByExpression;

        return new Specification<TProjection>(
            new Lazy<Expression<Func<TProjection, bool>>>(() => currentExpression.ApplyTo(expression), LazyThreadSafetyMode.PublicationOnly),
            new Lazy<Func<TProjection, bool>>(() =>
            {
                var compiled = expression.Compile();

                return x => currentDelegate(compiled.Invoke(x));
            }, LazyThreadSafetyMode.PublicationOnly)
        );
    }

    /// <summary>
    /// Applies the specification to a different type by using a projection expression and delegate.
    /// </summary>
    /// <typeparam name="TProjection">The type to project from.</typeparam>
    /// <param name="expressionFunc">The projection expression from <typeparamref name="TProjection"/> to <typeparamref name="TSource"/>.</param>
    /// <param name="delegateFunc">The projection delegate from <typeparamref name="TProjection"/> to <typeparamref name="TSource"/>.</param>
    /// <returns>A new specification for the <typeparamref name="TProjection"/> type.</returns>
    public Specification<TProjection> ApplyTo<TProjection>(Expression<Func<TProjection, TSource>>? expressionFunc, Func<TProjection, TSource> delegateFunc)
    {
        Defensive.Contract.ArgumentNotNull(delegateFunc);

        var currentDelegate = IsSatisfiedBy;
        var currentExpression = IsSatisfiedByExpression;

        var lazyExpression = expressionFunc == null
            ? new Lazy<Expression<Func<TProjection, bool>>>(() => currentExpression.ApplyTo((Expression<Func<TProjection, TSource>>)delegateFunc.Decompile()), LazyThreadSafetyMode.PublicationOnly)
            : new Lazy<Expression<Func<TProjection, bool>>>(() => currentExpression.ApplyTo(expressionFunc), LazyThreadSafetyMode.PublicationOnly);

        var lazyDelegate = new Lazy<Func<TProjection, bool>>(() => x => currentDelegate(delegateFunc(x)), LazyThreadSafetyMode.PublicationOnly);

        return new Specification<TProjection>(
            lazyExpression,
            lazyDelegate
        );
    }

    /// <summary>
    /// Allows for implicit conversion of a <see cref="Specification{TSource}"/> to a <see cref="Func{TSource, TResult}"/>.
    /// </summary>
    /// <param name="f">The specification to convert.</param>
    public static implicit operator Func<TSource, bool>(Specification<TSource> f)
    {
        return f.IsSatisfiedBy;
    }

    /// <summary>
    /// Allows for implicit conversion of a <see cref="Specification{TSource}"/> to an <see cref="Expression{TDelegate}"/>.
    /// </summary>
    /// <param name="f">The specification to convert.</param>
    public static implicit operator Expression<Func<TSource, bool>>(Specification<TSource> f)
    {
        return f.IsSatisfiedByExpression;
    }

    // public static implicit operator Specification<TSource>(Expression<Func<TSource, bool>> f)
    // {
    //     return new Specification<TSource>(f);
    // }
    //
    // public static implicit operator Specification<TSource>(Func<TSource, bool> f)
    // {
    //     return new Specification<TSource>(default, f);
    // }

    LambdaExpression ISpecificationExpressionAccessor.GetExpression()
    {
        return IsSatisfiedByExpression;
    }

    ISpecificationInternal ISpecificationInternal.Or(ISpecificationInternal input)
    {
        var inputSpec = (Specification<TSource>)input;
        var thisSpec = (Specification<TSource>)this;

        Specification<TSource> result = thisSpec || inputSpec;

        return result;
    }

    ISpecificationInternal ISpecificationInternal.And(ISpecificationInternal input)
    {
        var inputSpec = (Specification<TSource>)input;
        var thisSpec = (Specification<TSource>)this;

        Specification<TSource> result = thisSpec && inputSpec;

        return result;
    }

    ISpecificationInternal ISpecificationInternal.Not()
    {
        var thisSpec = (Specification<TSource>)this;

        Specification<TSource> result = !thisSpec;

        return result;
    }
}