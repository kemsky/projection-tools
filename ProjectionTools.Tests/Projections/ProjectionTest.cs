using System.Linq.Expressions;
using NUnit.Framework;
using ProjectionTools.Projections;

namespace ProjectionTools.Tests.Projections;

[Parallelizable(ParallelScope.All)]
[TestFixture]
public class ProjectionTest
{
    #region Create

    [Test]
    [TestCase(true)]
    [TestCase(false)]
    public void Projection__Expression(bool createFromExpression)
    {
        var projection = Create<ProjectedClass, string>(
            createFromExpression,
            x => x.Name == "A" ? x.Name : "B",
            x => x.Name == "A" ? x.Name : "B"
        );

        var result = projection.ProjectExpression;

        Assert.That(result.ToString(), Is.EqualTo("x => IIF((x.Name == \"A\"), x.Name, \"B\")"));
    }

    [Test]
    [TestCase(true)]
    [TestCase(false)]
    public void Projection__Delegate(bool createFromExpression)
    {
        var projection = Create<ProjectedClass, string>(
            createFromExpression,
            x => x.Name == "A" ? x.Name : "B",
            x => x.Name == "A" ? x.Name : "B"
        );

        var result = projection.Project(new ProjectedClass("A"));

        Assert.That(result, Is.EqualTo("A"));
    }

    #endregion

    #region Implicit

    [Test]
    [TestCase(true)]
    [TestCase(true)]
    public void Projection__Implicit__Expression(bool createFromExpression)
    {
        var projection = Create<ProjectedClass, string>(
            createFromExpression,
            x => x.Name == "A" ? x.Name : "B",
            x => x.Name == "A" ? x.Name : "B"
        );

        Expression<Func<ProjectedClass, string>> result = projection;

        Assert.That(result.ToString(), Is.EqualTo("x => IIF((x.Name == \"A\"), x.Name, \"B\")"));
    }

    [Test]
    [TestCase(true)]
    [TestCase(true)]
    public void Projection__Implicit__Delegate(bool createFromExpression)
    {
        var projection = Create<ProjectedClass, string>(
            createFromExpression,
            x => x.Name == "A" ? x.Name : "B",
            x => x.Name == "A" ? x.Name : "B"
        );

        Func<ProjectedClass, string> func = projection;

        var result = func(new ProjectedClass("A"));

        Assert.That(result, Is.EqualTo("A"));
    }

    #endregion

    #region ApplyTo

    [Test]
    [TestCase(true, true)]
    [TestCase(true, false)]
    [TestCase(false, true)]
    [TestCase(false, false)]
    public void ApplyTo__Expression(bool createFromExpression, bool applyToExpression)
    {
        var projection = Create<ProjectedClass, string>(
            createFromExpression,
            x => x.Name == "A" ? x.Name : "B",
            x => x.Name == "A" ? x.Name : "B"
        );

        var result = ApplyTo<ProjectedClass, string, ProjectedClassContainer>(
            applyToExpression,
            projection,
            x => x.ProjectedClass,
            x => x.ProjectedClass
        );

        Assert.That(result.ProjectExpression.ToString(), Is.EqualTo("x => IIF((x.ProjectedClass.Name == \"A\"), x.ProjectedClass.Name, \"B\")"));
    }

    [Test]
    [TestCase(true, true)]
    [TestCase(true, false)]
    [TestCase(false, true)]
    [TestCase(false, false)]
    public void ApplyTo__Delegate(bool createFromExpression, bool applyToExpression)
    {
        var projection = Create<ProjectedClass, string>(
            createFromExpression,
            x => x.Name == "A" ? x.Name : "B",
            x => x.Name == "A" ? x.Name : "B"
        );

        var result = ApplyTo<ProjectedClass, string, ProjectedClassContainer>(
            applyToExpression,
            projection,
            x => x.ProjectedClass,
            x => x.ProjectedClass
        );

        Assert.That(result.Project(new ProjectedClassContainer(new ProjectedClass("A"))), Is.EqualTo("A"));
    }

    [Test]
    [TestCase(true, true)]
    [TestCase(true, false)]
    [TestCase(false, true)]
    [TestCase(false, false)]
    public void ApplyTo_projection__Expression(bool createFromExpression, bool applyToExpression)
    {
        var projection = Create<ProjectedClass, string>(
            createFromExpression,
            x => x.Name == "A" ? x.Name : "B",
            x => x.Name == "A" ? x.Name : "B"
        );

        var projectionApplyTo = Create<ProjectedClassContainer, ProjectedClass>(
            applyToExpression,
            x => x.ProjectedClass,
            x => x.ProjectedClass
        );

        var result = projection.ApplyTo(projectionApplyTo);

        Assert.That(result.ProjectExpression.ToString(), Is.EqualTo("x => IIF((x.ProjectedClass.Name == \"A\"), x.ProjectedClass.Name, \"B\")"));
    }

    [Test]
    [TestCase(true, true)]
    [TestCase(true, false)]
    [TestCase(false, true)]
    [TestCase(false, false)]
    public void ApplyTo_projection__Delegate(bool createFromExpression, bool applyToExpression)
    {
        var projection = Create<ProjectedClass, string>(
            createFromExpression,
            x => x.Name == "A" ? x.Name : "B",
            x => x.Name == "A" ? x.Name : "B"
        );

        var projectionApplyTo = Create<ProjectedClassContainer, ProjectedClass>(
            applyToExpression,
            x => x.ProjectedClass,
            x => x.ProjectedClass
        );

        var result = projection.ApplyTo(projectionApplyTo);

        Assert.That(result.Project(new ProjectedClassContainer(new ProjectedClass("A"))), Is.EqualTo("A"));
    }

    private Projection<TProjection, TResult> ApplyTo<TSource, TResult, TProjection>(
        bool applyToExpression,
        Projection<TSource, TResult> projection,
        Expression<Func<TProjection, TSource>> sourceExpression,
        Func<TProjection, TSource> sourceDelegate
    )
    {
        if (applyToExpression)
        {
            return projection.ApplyTo(sourceExpression);
        }
        else
        {
            return projection.ApplyTo(sourceExpression, sourceDelegate);
        }
    }

    #endregion

    #region To

    [Test]
    [TestCase(true, true)]
    [TestCase(true, false)]
    [TestCase(false, true)]
    [TestCase(false, false)]
    public void To__Expression(bool createFromExpression, bool toExpression)
    {
        var projection = Create<ProjectedClass, string>(
            createFromExpression,
            x => x.Name == "A" ? x.Name : "B",
            x => x.Name == "A" ? x.Name : "B"
        );

        var result = To(
            toExpression,
            projection,
            x => x.ToLower(),
            x => x.ToLower()
        );

        Assert.That(result.ProjectExpression.ToString(), Is.EqualTo("x => IIF((x.Name == \"A\"), x.Name, \"B\").ToLower()"));
    }

    [Test]
    [TestCase(true, true)]
    [TestCase(true, false)]
    [TestCase(false, true)]
    [TestCase(false, false)]
    public void To__Delegate(bool createFromExpression, bool toExpression)
    {
        var projection = Create<ProjectedClass, string>(
            createFromExpression,
            x => x.Name == "A" ? x.Name : "B",
            x => x.Name == "A" ? x.Name : "B"
        );

        var result = To(
            toExpression,
            projection,
            x => x.ToLower(),
            x => x.ToLower()
        );

        Assert.That(result.Project(new ProjectedClass("A")), Is.EqualTo("a"));
    }

    [Test]
    [TestCase(true, true)]
    [TestCase(true, false)]
    [TestCase(false, true)]
    [TestCase(false, false)]
    public void To_projection__Expression(bool createFromExpression, bool toExpression)
    {
        var projection = Create<ProjectedClass, string>(
            createFromExpression,
            x => x.Name == "A" ? x.Name : "B",
            x => x.Name == "A" ? x.Name : "B"
        );

        var projectionTo = Create<string, string>(
            toExpression,
            x => x.ToLower(),
            x => x.ToLower()
        );

        var result = projection.To(projectionTo);

        Assert.That(result.ProjectExpression.ToString(), Is.EqualTo("x => IIF((x.Name == \"A\"), x.Name, \"B\").ToLower()"));
    }

    [Test]
    [TestCase(true, true)]
    [TestCase(true, false)]
    [TestCase(false, true)]
    [TestCase(false, false)]
    public void To_projection__Delegate(bool createFromExpression, bool toExpression)
    {
        var projection = Create<ProjectedClass, string>(
            createFromExpression,
            x => x.Name == "A" ? x.Name : "B",
            x => x.Name == "A" ? x.Name : "B"
        );

        var projectionTo = Create<string, string>(
            toExpression,
            x => x.ToLower(),
            x => x.ToLower()
        );

        var result = projection.To(projectionTo);

        Assert.That(result.Project(new ProjectedClass("A")), Is.EqualTo("a"));
    }

    private Projection<TSource, TProjection> To<TSource, TResult, TProjection>(
        bool applyToExpression,
        Projection<TSource, TResult> projection,
        Expression<Func<TResult, TProjection>> projectionExpression,
        Func<TResult, TProjection> projectionDelegate
    )
    {
        if (applyToExpression)
        {
            return projection.To(projectionExpression);
        }
        else
        {
            return projection.To(projectionExpression, projectionDelegate);
        }
    }

    #endregion

    #region Rewiter

    [Test]
    [TestCase(true, true)]
    [TestCase(true, false)]
    [TestCase(false, true)]
    [TestCase(false, false)]
    public void Projection__Expression__Rewriter(bool createInnerFromExpression, bool createFromExpression)
    {
        var lengthPlusOne = Create<string, int>(
            createInnerFromExpression,
            x => x.Length + 1,
            x => x.Length + 1
        );

        var lengthPlusOnePlusOne = Create<string, int>(
            createFromExpression,
            x => lengthPlusOne.Project(x) + 1,
            x => lengthPlusOne.Project(x) + 1
        );

        Assert.That(lengthPlusOnePlusOne.ProjectExpression.ToString(), Is.EqualTo("x => ((x.Length + 1) + 1)"));
    }

    [Test]
    [TestCase(true, true)]
    [TestCase(true, false)]
    [TestCase(false, true)]
    [TestCase(false, false)]
    public void Projection__Delegate__Rewriter(bool createInnerFromExpression, bool createFromExpression)
    {
        var lengthPlusOne = Create<string, int>(
            createInnerFromExpression,
            x => x.Length + 1,
            x => x.Length + 1
        );

        var lengthPlusOnePlusOne = Create<string, int>(
            createFromExpression,
            x => lengthPlusOne.Project(x) + 1,
            x => lengthPlusOne.Project(x) + 1
        );

        Assert.That(lengthPlusOnePlusOne.Project("1"), Is.EqualTo(3));
    }

    #endregion

    private Projection<TSource, TResult> Create<TSource, TResult>(
        bool createFromExpression,
        Expression<Func<TSource, TResult>> projectionExpression,
        Func<TSource, TResult> projectionDelegate
    )
    {
        if (createFromExpression)
        {
            return new Projection<TSource, TResult>(projectionExpression);
        }
        else
        {
            return new Projection<TSource, TResult>(projectionExpression, projectionDelegate);
        }
    }

    private class ProjectedClass
    {
        public string Name { get; }

        public ProjectedClass(string name)
        {
            Name = name;
        }
    }

    private class ProjectedClassContainer
    {
        public ProjectedClass ProjectedClass { get; }

        public ProjectedClassContainer(ProjectedClass projectedClass)
        {
            ProjectedClass = projectedClass;
        }
    }
}