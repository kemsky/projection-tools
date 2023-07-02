using System.Linq.Expressions;
using AgileObjects.ReadableExpressions;
using NUnit.Framework;
using ProjectionTools.Projections;

namespace ProjectionTools.Tests.Projections;

[Parallelizable(ParallelScope.All)]
[TestFixture]
public class ProjectionFactory1Test
{
    private string A { get; } = "A";

    #region Create

    [Test]
    [TestCase(true)]
    [TestCase(false)]
    public void ProjectionFactory__Expression__ok(bool createFromExpression)
    {
        var projectionFactory = Create<ProjectedClass, string, string>(
            createFromExpression,
            param1 => y => y.Name == param1 ? "C" : "B",
            param1 => y => y.Name == param1 ? "C" : "B"
        );

        var result = projectionFactory.ExpressionFactory("A");

        Assert.That(result.ToReadableString(), Is.EqualTo("y => (y.Name == param1) ? \"C\" : \"B\""));
    }

    [Test]
    [TestCase(true)]
    [TestCase(false)]
    public void ProjectionFactory__Delegate__ok(bool createFromExpression)
    {
        var projectionFactory = Create<ProjectedClass, string, string>(
            createFromExpression,
            param1 => y => y.Name == param1 ? "C" : "B",
            param1 => y => y.Name == param1 ? "C" : "B"
        );

        var delegateFactory = projectionFactory.DelegateFactory("A");

        var result = delegateFactory(new ProjectedClass("A"));

        Assert.That(result, Is.EqualTo("C"));
    }

    #endregion

    #region For

    [Test]
    [TestCase(true)]
    [TestCase(false)]
    public void ProjectionFactory__For__Param__ok(bool createFromExpression)
    {
        var projectionFactory = Create<ProjectedClass, string, string>(
            createFromExpression,
            param1 => y => y.Name == param1 ? "C" : "B",
            param1 => y => y.Name == param1 ? "C" : "B"
        );

        var projection = projectionFactory.For("A");

        Assert.That(projection.ProjectExpression.ToReadableString(), Is.EqualTo("y => (y.Name == param1) ? \"C\" : \"B\""));
        Assert.That(projection.Project(new ProjectedClass("A")), Is.EqualTo("C"));
    }

    [Test]
    [TestCase(true)]
    [TestCase(false)]
    public void ProjectionFactory__For__BoundParam__ok(bool createFromExpression)
    {
        var projectionFactory = Create<ProjectedClass, string, string>(
            createFromExpression,
            param1 => y => y.Name == param1 ? "C" : "B",
            param1 => y => y.Name == param1 ? "C" : "B"
        );

        var projection = projectionFactory.For(() => A);

        Assert.That(projection.ProjectExpression.ToReadableString(), Is.EqualTo("y => (y.Name == A) ? \"C\" : \"B\""));
        Assert.That(projection.Project(new ProjectedClass("A")), Is.EqualTo("C"));
    }

    #endregion

    private ProjectionFactory<TSource, TResult, TParam> Create<TSource, TResult, TParam>(
        bool createFromExpression,
        Func<TParam, Expression<Func<TSource, TResult>>> expressionFactory,
        Func<TParam, Func<TSource, TResult>> delegateFactory
    )
    {
        if (createFromExpression)
        {
            return new ProjectionFactory<TSource, TResult, TParam>(expressionFactory);
        }
        else
        {
            return new ProjectionFactory<TSource, TResult, TParam>(expressionFactory, delegateFactory);
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