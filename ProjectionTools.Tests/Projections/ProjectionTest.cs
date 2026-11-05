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

    [Test]
    [TestCase(true, true, false)]
    [TestCase(true, false, false)]
    [TestCase(false, true, false)]
    [TestCase(false, false, false)]
    [TestCase(true, false, true)]
    [TestCase(false, true, true)]
    [TestCase(false, false, true)]
    public void ApplyTo_nested_projection__Expression(bool createFromExpression, bool applyToExpression, bool decompile)
    {
        var projection = Create<ProjectedClass, string>(
            createFromExpression,
            x => x.Name == "A" ? x.Name : "B",
            x => x.Name == "A" ? x.Name : "B",
            decompile
        );

        var nestedProjection = Create<ProjectedClassContainer, ProjectedClass>(
            createFromExpression,
            x => x.ProjectedClass,
            x => x.ProjectedClass,
            decompile
        );

        var result = ApplyTo<ProjectedClass, string, ProjectedClassContainer>(
            applyToExpression,
            projection,
            x => nestedProjection.Project(x),
            x => nestedProjection.Project(x),
            decompile
        );

        Assert.That(result.ProjectExpression.ToString(), Is.EqualTo("x => IIF((x.ProjectedClass.Name == \"A\"), x.ProjectedClass.Name, \"B\")"));
    }

    [Test]
    [TestCase(true, true, false)]
    [TestCase(true, false, false)]
    [TestCase(false, true, false)]
    [TestCase(false, false, false)]
    [TestCase(true, false, true)]
    [TestCase(false, true, true)]
    [TestCase(false, false, true)]
    public void ApplyTo_nested_projection__Delegate(bool createFromExpression, bool applyToExpression, bool decompile)
    {
        var projection = Create<ProjectedClass, string>(
            createFromExpression,
            x => x.Name == "A" ? x.Name : "B",
            x => x.Name == "A" ? x.Name : "B",
            decompile
        );

        var nestedProjection = Create<ProjectedClassContainer, ProjectedClass>(
            createFromExpression,
            x => x.ProjectedClass,
            x => x.ProjectedClass,
            decompile
        );

        var result = ApplyTo<ProjectedClass, string, ProjectedClassContainer>(
            applyToExpression,
            projection,
            x => nestedProjection.Project(x),
            x => nestedProjection.Project(x),
            decompile
        );

        Assert.That(result.Project(new ProjectedClassContainer(new ProjectedClass("A"))), Is.EqualTo("A"));
    }

    private Projection<TProjection, TResult> ApplyTo<TSource, TResult, TProjection>(
        bool applyToExpression,
        Projection<TSource, TResult> projection,
        Expression<Func<TProjection, TSource>> sourceExpression,
        Func<TProjection, TSource> sourceDelegate,
        bool decompile = false
    )
    {
        if (applyToExpression)
        {
            return projection.ApplyTo(sourceExpression);
        }
        else
        {
            return projection.ApplyTo(decompile ? null : sourceExpression, sourceDelegate);
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

    [Test]
    [TestCase(true, true, false)]
    [TestCase(true, false, false)]
    [TestCase(false, true, false)]
    [TestCase(false, false, false)]
    [TestCase(true, false, true)]
    [TestCase(false, true, true)]
    [TestCase(false, false, true)]
    public void To_nested_projection__Expression(bool createFromExpression, bool toExpression, bool decompile)
    {
        var projection = Create<ProjectedClass, string>(
            createFromExpression,
            x => x.Name == "A" ? x.Name : "B",
            x => x.Name == "A" ? x.Name : "B",
            decompile
        );

        var nestedProjection = Create<string, string>(
            createFromExpression,
            x => x.ToLower(),
            x => x.ToLower(),
            decompile
        );

        var result = To(
            toExpression,
            projection,
            x => nestedProjection.Project(x),
            x => nestedProjection.Project(x),
            decompile
        );

        Assert.That(result.ProjectExpression.ToString(), Is.EqualTo("x => IIF((x.Name == \"A\"), x.Name, \"B\").ToLower()"));
    }

    [Test]
    [TestCase(true, true, false)]
    [TestCase(true, false, false)]
    [TestCase(false, true, false)]
    [TestCase(false, false, false)]
    [TestCase(true, false, true)]
    [TestCase(false, true, true)]
    [TestCase(false, false, true)]
    public void To_nested_projection__Delegate(bool createFromExpression, bool toExpression, bool decompile)
    {
        var projection = Create<ProjectedClass, string>(
            createFromExpression,
            x => x.Name == "A" ? x.Name : "B",
            x => x.Name == "A" ? x.Name : "B",
            decompile
        );

        var nestedProjection = Create<string, string>(
            createFromExpression,
            x => x.ToLower(),
            x => x.ToLower(),
            decompile
        );

        var result = To(
            toExpression,
            projection,
            x => nestedProjection.Project(x),
            x => nestedProjection.Project(x),
            decompile
        );

        Assert.That(result.Project(new ProjectedClass("A")), Is.EqualTo("a"));
    }

    private Projection<TSource, TProjection> To<TSource, TResult, TProjection>(
        bool applyToExpression,
        Projection<TSource, TResult> projection,
        Expression<Func<TResult, TProjection>> projectionExpression,
        Func<TResult, TProjection> projectionDelegate,
        bool decompile = false
    )
    {
        if (applyToExpression)
        {
            return projection.To(projectionExpression);
        }
        else
        {
            return projection.To(decompile ? null : projectionExpression, projectionDelegate);
        }
    }

    #endregion

    #region Rewiter

    [Test]
    [TestCase(true, true, false)]
    [TestCase(true, false, false)]
    [TestCase(false, true, false)]
    [TestCase(false, false, false)]
    [TestCase(true, false, true)]
    [TestCase(false, true, true)]
    [TestCase(false, false, true)]
    public void Projection__Expression__Rewriter(bool createInnerFromExpression, bool createFromExpression, bool decompile)
    {
        var lengthPlusOne = Create<string, int>(
            createInnerFromExpression,
            x => x.Length + 1,
            x => x.Length + 1,
            decompile
        );

        var lengthPlusOnePlusOne = Create<string, int>(
            createFromExpression,
            x => lengthPlusOne.Project(x) + 1,
            x => lengthPlusOne.Project(x) + 1,
            decompile
        );

        Assert.That(lengthPlusOnePlusOne.ProjectExpression.ToString(), Is.EqualTo("x => ((x.Length + 1) + 1)"));
    }

    [Test]
    [TestCase(true, true, false)]
    [TestCase(true, false, false)]
    [TestCase(false, true, false)]
    [TestCase(false, false, false)]
    [TestCase(true, false, true)]
    [TestCase(false, true, true)]
    [TestCase(false, false, true)]
    public void Projection__Delegate__Rewriter(bool createInnerFromExpression, bool createFromExpression, bool decompile)
    {
        var lengthPlusOne = Create<string, int>(
            createInnerFromExpression,
            x => x.Length + 1,
            x => x.Length + 1,
            decompile
        );

        var lengthPlusOnePlusOne = Create<string, int>(
            createFromExpression,
            x => lengthPlusOne.Project(x) + 1,
            x => lengthPlusOne.Project(x) + 1,
            decompile
        );

        Assert.That(lengthPlusOnePlusOne.Project("1"), Is.EqualTo(3));
    }

    #endregion

    private Projection<TSource, TResult> Create<TSource, TResult>(
        bool createFromExpression,
        Expression<Func<TSource, TResult>> projectionExpression,
        Func<TSource, TResult> projectionDelegate,
        bool decompile = false
    )
    {
        if (createFromExpression)
        {
            return new Projection<TSource, TResult>(projectionExpression);
        }
        else
        {
            return new Projection<TSource, TResult>(decompile ? null : projectionExpression, projectionDelegate);
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