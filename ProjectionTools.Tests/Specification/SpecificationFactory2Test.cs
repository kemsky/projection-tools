using System.Linq.Expressions;
using AgileObjects.ReadableExpressions;
using NUnit.Framework;
using ProjectionTools.Specifications;

namespace ProjectionTools.Tests.Specification;

[Parallelizable(ParallelScope.All)]
[TestFixture]
public class SpecificationFactory2Test
{
    #region Create

    [Test]
    [TestCase(true)]
    [TestCase(false)]
    public void SpecificationFactory__Create__Expression(bool createFromExpression)
    {
        var specificationFactory = Create<string, string, string>(
            createFromExpression,
            (param1, param2) => x => x == param1 || x == param2,
            (param1, param2) => x => x == param1 || x == param2
        );

        var result = specificationFactory.ExpressionFactory("A", "B");

        Assert.That(result.ToReadableString(), Is.EqualTo("x => (x == param1) || (x == param2)"));
    }

    [Test]
    [TestCase(true)]
    [TestCase(false)]
    public void SpecificationFactory__Create__Delegate(bool createFromExpression)
    {
        var specificationFactory = Create<string, string, string>(
            createFromExpression,
            (param1, param2) => x => x == param1 || x == param2,
            (param1, param2) => x => x == param1 || x == param2
        );

        var result = specificationFactory.DelegateFactory("A", "B")("A");

        Assert.That(result, Is.True);
    }

    #endregion

    #region For

    [Test]
    [TestCase(true)]
    [TestCase(false)]
    public void SpecificationFactory__For__Expression(bool createFromExpression)
    {
        var specificationFactory = Create<string, string, string>(
            createFromExpression,
            (param1, param2) => x => x == param1 || x == param2,
            (param1, param2) => x => x == param1 || x == param2
        );

        var specificationFactory1 = specificationFactory.For("A", "B");

        var result = specificationFactory1.IsSatisfiedByExpression;

        Assert.That(result.ToReadableString(), Is.EqualTo("x => (x == param1) || (x == param2)"));
    }

    [Test]
    [TestCase(true)]
    [TestCase(false)]
    public void SpecificationFactory__For__Delegate(bool createFromExpression)
    {
        var specificationFactory = Create<string, string, string>(
            createFromExpression,
            (param1, param2) => x => x == param1 || x == param2,
            (param1, param2) => x => x == param1 || x == param2
        );

        var specificationFactory1 = specificationFactory.For("A", "B");

        var result = specificationFactory1.IsSatisfiedBy("A");

        Assert.That(result, Is.True);
    }

    #endregion

    #region For Partial

    [Test]
    [TestCase(true)]
    [TestCase(false)]
    public void SpecificationFactory__For_Partial__Expression(bool createFromExpression)
    {
        var specificationFactory = Create<string, string, string>(
            createFromExpression,
            (param1, param2) => x => x == param1 || x == param2,
            (param1, param2) => x => x == param1 || x == param2
        );

        var specificationFactory1 = specificationFactory.For("A").For("B");

        var result = specificationFactory1.IsSatisfiedByExpression;

        Assert.That(result.ToReadableString(), Is.EqualTo("x => (x == param1) || (x == param2)"));
    }

    [Test]
    [TestCase(true)]
    [TestCase(false)]
    public void SpecificationFactory__For_Partial__Delegate(bool createFromExpression)
    {
        var specificationFactory = Create<string, string, string>(
            createFromExpression,
            (param1, param2) => x => x == param1 || x == param2,
            (param1, param2) => x => x == param1 || x == param2
        );

        var specificationFactory1 = specificationFactory.For("A").For("B");

        var result = specificationFactory1.IsSatisfiedBy("A");

        Assert.That(result, Is.True);
    }

    #endregion

    private SpecificationFactory<TSource, TParam1, TParam2> Create<TSource, TParam1, TParam2>(
        bool createFromExpression,
        Func<TParam1, TParam2, Expression<Func<TSource, bool>>> expressionFactory,
        Func<TParam1, TParam2, Func<TSource, bool>> delegateFactory
    )
    {
        if (createFromExpression)
        {
            return new SpecificationFactory<TSource, TParam1, TParam2>(expressionFactory);
        }
        else
        {
            return new SpecificationFactory<TSource, TParam1, TParam2>(expressionFactory, delegateFactory);
        }
    }
}