using System.Linq.Expressions;
using AgileObjects.ReadableExpressions;
using NUnit.Framework;
using ProjectionTools.Projections;

namespace ProjectionTools.Tests.Projections;

[Parallelizable(ParallelScope.All)]
[TestFixture]
public class ProjectionFactory2Test
{
    private string A { get; } = nameof(A);

    private string B { get; } = nameof(B);

    private string C { get; } = nameof(C);

    private string D { get; } = nameof(D);

    #region Create

    [Test]
    [TestCase(true)]
    [TestCase(false)]
    public void ProjectionFactory__Expression(bool createFromExpression)
    {
        var projectionFactory = Create<ProjectedClass, string, string, string>(
            createFromExpression,
            (param1, param2) => y => y.Name == param1 ? "C" : y.Name == param2 ? "D" : "B",
            (param1, param2) => y => y.Name == param1 ? "C" : y.Name == param2 ? "D" : "B"
        );

        var result = projectionFactory.ExpressionFactory("A", "D");

        Assert.That(result.ToReadableString(), Is.EqualTo("y => (y.Name == param1) ? \"C\" : (y.Name == param2) ? \"D\" : \"B\""));
    }

    [Test]
    [TestCase(true)]
    [TestCase(false)]
    public void ProjectionFactory__Delegate(bool createFromExpression)
    {
        var projectionFactory = Create<ProjectedClass, string, string, string>(
            createFromExpression,
            (param1, param2) => y => y.Name == param1 ? "C" : y.Name == param2 ? "D" : "B",
            (param1, param2) => y => y.Name == param1 ? "C" : y.Name == param2 ? "D" : "B"
        );

        var delegateFactory = projectionFactory.DelegateFactory("A", "D");

        var result = delegateFactory(new ProjectedClass("A"));

        Assert.That(result, Is.EqualTo("C"));
    }

    #endregion

    #region For

    [Test]
    [TestCase(true)]
    [TestCase(false)]
    public void ProjectionFactory__For__Param(bool createFromExpression)
    {
        var projectionFactory = Create<ProjectedClass, string, string, string>(
            createFromExpression,
            (param1, param2) => y => y.Name == param1 ? "C" : y.Name == param2 ? "D" : "B",
            (param1, param2) => y => y.Name == param1 ? "C" : y.Name == param2 ? "D" : "B"
        );

        var projection = projectionFactory.For("A", "D");

        Assert.That(projection.ProjectExpression.ToReadableString(), Is.EqualTo("y => (y.Name == param1) ? \"C\" : (y.Name == param2) ? \"D\" : \"B\""));
        Assert.That(projection.Project(new ProjectedClass("A")), Is.EqualTo("C"));
    }

    [Test]
    [TestCase(true)]
    [TestCase(false)]
    public void ProjectionFactory__For__BoundParam(bool createFromExpression)
    {
        var projectionFactory = Create<ProjectedClass, string, string, string>(
            createFromExpression,
            (param1, param2) => y => y.Name == param1 ? "C" : y.Name == param2 ? "D" : "B",
            (param1, param2) => y => y.Name == param1 ? "C" : y.Name == param2 ? "D" : "B"
        );

        var projection = projectionFactory.For(() => A, () => B);

        Assert.That(projection.ProjectExpression.ToReadableString(), Is.EqualTo("y => (y.Name == A) ? \"C\" : (y.Name == B) ? \"D\" : \"B\""));
        Assert.That(projection.Project(new ProjectedClass("A")), Is.EqualTo("C"));
    }

    #endregion

    #region ForPartial

    [Test]
    [TestCase(true)]
    [TestCase(false)]
    public void ProjectionFactory__For__Param__Partial(bool createFromExpression)
    {
        var projectionFactory = Create<ProjectedClass, string, string, string>(
            createFromExpression,
            (param1, param2) => y => y.Name == param1 ? "C" : y.Name == param2 ? "D" : "B",
            (param1, param2) => y => y.Name == param1 ? "C" : y.Name == param2 ? "D" : "B"
        );

        var projectionFactory1 = projectionFactory.For("D");

        var projection = projectionFactory1.For("A");

        Assert.That(projection.ProjectExpression.ToReadableString(), Is.EqualTo("y => (y.Name == param1) ? \"C\" : (y.Name == param2) ? \"D\" : \"B\""));
        Assert.That(projection.Project(new ProjectedClass("A")), Is.EqualTo("D"));
    }

    [Test]
    [TestCase(true)]
    [TestCase(false)]
    public void ProjectionFactory__For__BoundParam__Partial(bool createFromExpression)
    {
        var projectionFactory = Create<ProjectedClass, string, string, string>(
            createFromExpression,
            (param1, param2) => y => y.Name == param1 ? "C" : y.Name == param2 ? "D" : "B",
            (param1, param2) => y => y.Name == param1 ? "C" : y.Name == param2 ? "D" : "B"
        );

        var projectionFactory1 = projectionFactory.For(() => D);

        var projection = projectionFactory1.For(() => A);

        Assert.That(projection.ProjectExpression.ToReadableString(), Is.EqualTo("y => (y.Name == D) ? \"C\" : (y.Name == A) ? \"D\" : \"B\""));
        Assert.That(projection.Project(new ProjectedClass("A")), Is.EqualTo("D"));
    }

    #endregion

    private ProjectionFactory<TSource, TResult, TParam1, TParam2> Create<TSource, TResult, TParam1, TParam2>(
        bool createFromExpression,
        Func<TParam1, TParam2, Expression<Func<TSource, TResult>>> expressionFactory,
        Func<TParam1, TParam2, Func<TSource, TResult>> delegateFactory
    )
    {
        if (createFromExpression)
        {
            return new ProjectionFactory<TSource, TResult, TParam1, TParam2>(expressionFactory);
        }
        else
        {
            return new ProjectionFactory<TSource, TResult, TParam1, TParam2>(expressionFactory, delegateFactory);
        }
    }

    private class ProjectedClass
    {
        public string Name { get; set; }

        public ProjectedClass(string name)
        {
            Name = name;
        }
    }
}