using System.Linq.Expressions;
using AgileObjects.ReadableExpressions;
using NUnit.Framework;
using ProjectionTools.Specifications;

namespace ProjectionTools.Tests.Specification;

[Parallelizable(ParallelScope.All)]
[TestFixture]
public class SpecificationFactory1Test
{
    private int A { get; } = 1;

    #region Create

    [Test]
    [TestCase(true)]
    [TestCase(false)]
    public void SpecificationFactory__Create__Expression(bool createFromExpression)
    {
        var specificationFactory = Create<string, string>(
            createFromExpression,
            param1 => x => x == param1,
            param1 => x => x == param1
        );

        var result = specificationFactory.ExpressionFactory("A");

        Assert.That(result.ToReadableString(), Is.EqualTo("x => x == param1"));
    }

    [Test]
    [TestCase(true)]
    [TestCase(false)]
    public void SpecificationFactory__Create__Delegate(bool createFromExpression)
    {
        var specificationFactory = Create<string, string>(
            createFromExpression,
            param1 => x => x == param1,
            param1 => x => x == param1
        );

        var result = specificationFactory.DelegateFactory("A")("A");

        Assert.That(result, Is.True);
    }

    #endregion

    #region For

    [Test]
    [TestCase(true)]
    [TestCase(false)]
    public void SpecificationFactory__For__Expression(bool createFromExpression)
    {
        var specificationFactory = Create<string, string>(
            createFromExpression,
            param1 => x => x == param1,
            param1 => x => x == param1
        );

        var specificationFactory1 = specificationFactory.For("A");

        var result = specificationFactory1.IsSatisfiedByExpression;

        Assert.That(result.ToReadableString(), Is.EqualTo("x => x == param1"));
    }

    [Test]
    [TestCase(true)]
    [TestCase(false)]
    public void SpecificationFactory__For__Delegate__ok(bool createFromExpression)
    {
        var specificationFactory = Create<string, string>(
            createFromExpression,
            param1 => x => x == param1,
            param1 => x => x == param1
        );

        var specificationFactory1 = specificationFactory.For("A");

        var result = specificationFactory1.IsSatisfiedBy("A");

        Assert.That(result, Is.True);
    }

    [Test]
    [TestCase(true)]
    [TestCase(false)]
    public void SpecificationFactory__For__Nested_Expression__ok(bool createFromExpression)
    {
        var specificationFactory = Create<string, int>(
            createFromExpression,
            param1 => x => x.Length == param1,
            param1 => x => x.Length == param1
        );

        var specification = specificationFactory.For(() => A);

        Assert.That(specification.IsSatisfiedByExpression.ToReadableString(), Is.EqualTo(@"x => x.Length == A"));
        Assert.That(specification.IsSatisfiedBy("A"), Is.True);
    }

    #endregion

    private SpecificationFactory<TSource, TParam> Create<TSource, TParam>(
        bool createFromExpression,
        Func<TParam, Expression<Func<TSource, bool>>> expressionFactory,
        Func<TParam, Func<TSource, bool>> delegateFactory
    )
    {
        if (createFromExpression)
        {
            return new SpecificationFactory<TSource, TParam>(expressionFactory);
        }
        else
        {
            return new SpecificationFactory<TSource, TParam>(expressionFactory, delegateFactory);
        }
    }
}