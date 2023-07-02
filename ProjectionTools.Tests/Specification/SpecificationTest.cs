using System.Linq.Expressions;
using NUnit.Framework;
using ProjectionTools.Specifications;

namespace ProjectionTools.Tests.Specification;

[Parallelizable(ParallelScope.All)]
[TestFixture]
public class SpecificationTest
{
    #region Create

    [Test]
    [TestCase(true)]
    [TestCase(false)]
    public void Specification__Expression__ok(bool createFromExpression)
    {
        var specification = Create<string>(
            createFromExpression,
            x => x == "A",
            x => x == "A"
        );

        var result = specification.IsSatisfiedByExpression;

        Assert.That(result.ToString(), Is.EqualTo("x => (x == \"A\")"));
    }

    [Test]
    [TestCase(true)]
    [TestCase(false)]
    public void Specification__Delegate__ok(bool createFromExpression)
    {
        var specification = Create<string>(
            createFromExpression,
            x => x == "A",
            x => x == "A"
        );

        var result = specification.IsSatisfiedBy("A");

        Assert.That(result, Is.True);
    }

    #endregion

    #region Implicit

    [Test]
    [TestCase(true)]
    [TestCase(false)]
    public void Specification__Implicit__Expression__ok(bool createFromExpression)
    {
        var specification = Create<string>(
            createFromExpression,
            x => x == "A",
            x => x == "A"
        );

        Expression<Func<string, bool>> result = specification;

        Assert.That(result.ToString(), Is.EqualTo("x => (x == \"A\")"));
    }

    [Test]
    [TestCase(true)]
    [TestCase(false)]
    public void Specification__Implicit__Delegate__ok(bool createFromExpression)
    {
        var specification = Create<string>(
            createFromExpression,
            x => x == "A",
            x => x == "A"
        );

        Func<string, bool> func = specification;

        var result = func("A");

        Assert.That(result, Is.True);
    }

    #endregion

    #region ApplyTo

    [Test]
    [TestCase(true, true)]
    [TestCase(true, false)]
    [TestCase(false, true)]
    [TestCase(false, false)]
    public void Specification_ApplyTo__Expression__ok(bool createFromExpression, bool applyToExpression)
    {
        var specification = Create<string>(
            createFromExpression,
            x => x == "A",
            x => x == "A"
        );

        var specification1 = ApplyTo<string, ProjectedClass>(
            applyToExpression,
            specification,
            x => x.Name,
            x => x.Name
        );

        var result = specification1.IsSatisfiedByExpression;

        Assert.That(result.ToString(), Is.EqualTo("x => (x.Name == \"A\")"));
    }

    [Test]
    [TestCase(true, true)]
    [TestCase(true, false)]
    [TestCase(false, true)]
    [TestCase(false, false)]
    public void Specification_ApplyTo__Delegate__ok(bool createFromExpression, bool applyToExpression)
    {
        var specification = Create<string>(
            createFromExpression,
            x => x == "A",
            x => x == "A"
        );

        var specification1 = ApplyTo<string, ProjectedClass>(
            applyToExpression,
            specification,
            x => x.Name,
            x => x.Name
        );

        var result = specification1.IsSatisfiedBy(new ProjectedClass("A"));

        Assert.That(result, Is.True);
    }

    private Specification<TProjection> ApplyTo<TSource, TProjection>(
        bool applyToExpression,
        Specification<TSource> spec,
        Expression<Func<TProjection, TSource>> expression,
        Func<TProjection, TSource> func
    )
    {
        if (applyToExpression)
        {
            return spec.ApplyTo(expression);
        }
        else
        {
            return spec.ApplyTo(expression, func);
        }
    }

    #endregion

    #region Operator true/false

    [Test]
    [TestCase(true)]
    [TestCase(false)]
    public void Specification__Operator_true__ok(bool createFromExpression)
    {
        var specification = Create<string>(
            createFromExpression,
            x => x == "A",
            x => x == "A"
        );

        bool result;

        if (specification)
        {
            result = true;
        }
        else
        {
            result = false;
        }

        Assert.That(result, Is.False);
    }

    [Test]
    [TestCase(true)]
    [TestCase(false)]
    public void Specification__Operator_false__ok(bool createFromExpression)
    {
        var specification1 = Create<string>(
            createFromExpression,
            x => x == "A",
            x => x == "A"
        );

        var specification2 = Create<string>(
            createFromExpression,
            x => x.Length == 1,
            x => x.Length == 1
        );

        var specification = specification1 && specification2;

        var result = specification.IsSatisfiedBy("A");

        Assert.That(result, Is.True);
        Assert.That(specification.IsSatisfiedByExpression.ToString(), Is.EqualTo("x => ((x == \"A\") AndAlso (x.Length == 1))"));
    }

    #endregion

    #region Operator !

    [Test]
    [TestCase(true, true)]
    [TestCase(true, false)]
    [TestCase(false, true)]
    [TestCase(false, false)]
    public void Specification__Operator_Not__Expression__ok(bool createFromExpression, bool applyToExpression)
    {
        var specification = Create<string>(
            createFromExpression,
            x => x == "A",
            x => x == "A"
        );

        var specification1 = !specification;

        var result = specification1.IsSatisfiedByExpression;

        Assert.That(result.ToString(), Is.EqualTo("x => Not((x == \"A\"))"));
    }

    [Test]
    [TestCase(true, true)]
    [TestCase(true, false)]
    [TestCase(false, true)]
    [TestCase(false, false)]
    public void Specification__Operator_Not__Delegate__ok(bool createFromExpression, bool applyToExpression)
    {
        var specification = Create<string>(
            createFromExpression,
            x => x == "A",
            x => x == "A"
        );

        var specification1 = !specification;

        var result = specification1.IsSatisfiedBy("B");

        Assert.That(result, Is.True);
    }

    #endregion

    #region Operator &

    [Test]
    [TestCase(true, true)]
    [TestCase(true, false)]
    [TestCase(false, true)]
    [TestCase(false, false)]
    public void Specification__Operator_And__Expression__ok(bool createFromExpression, bool applyToExpression)
    {
        var specification1 = Create<string>(
            createFromExpression,
            x => x == "A",
            x => x == "A"
        );

        var specification2 = Create<string>(
            createFromExpression,
            x => x == "B",
            x => x == "B"
        );

        var specification = specification1 && specification2;

        var result = specification.IsSatisfiedByExpression;

        Assert.That(result.ToString(), Is.EqualTo("x => ((x == \"A\") AndAlso (x == \"B\"))"));
    }

    [Test]
    [TestCase(true, true)]
    [TestCase(true, false)]
    [TestCase(false, true)]
    [TestCase(false, false)]
    public void Specification__Operator_And__Delegate__ok(bool createFromExpression, bool applyToExpression)
    {
        var specification1 = Create<string>(
            createFromExpression,
            x => x == "A",
            x => x == "A"
        );

        var specification2 = Create<string>(
            createFromExpression,
            x => x.Length == 1,
            x => x.Length == 1
        );

        var specification = specification1 && specification2;

        var result = specification.IsSatisfiedBy("A");

        Assert.That(result, Is.True);
    }

    #endregion

    #region Operator |

    [Test]
    [TestCase(true, true)]
    [TestCase(true, false)]
    [TestCase(false, true)]
    [TestCase(false, false)]
    public void Specification__Operator_Or__Expression__ok(bool createFromExpression, bool applyToExpression)
    {
        var specification1 = Create<string>(
            createFromExpression,
            x => x == "A",
            x => x == "A"
        );

        var specification2 = Create<string>(
            createFromExpression,
            x => x == "B",
            x => x == "B"
        );

        var specification = specification1 || specification2;

        var result = specification.IsSatisfiedByExpression;

        Assert.That(result.ToString(), Is.EqualTo("x => ((x == \"A\") OrElse (x == \"B\"))"));
    }

    [Test]
    [TestCase(true, true)]
    [TestCase(true, false)]
    [TestCase(false, true)]
    [TestCase(false, false)]
    public void Specification__Operator_Or__Delegate__ok(bool createFromExpression, bool applyToExpression)
    {
        var specification1 = Create<string>(
            createFromExpression,
            x => x == "A",
            x => x == "A"
        );

        var specification2 = Create<string>(
            createFromExpression,
            x => x == "B",
            x => x == "B"
        );

        var specification = specification1 || specification2;

        var result = specification.IsSatisfiedBy("B");

        Assert.That(result, Is.True);
    }

    #endregion

    [Test]
    [TestCase(true, true)]
    [TestCase(true, false)]
    [TestCase(false, true)]
    [TestCase(false, false)]
    public void Specification__nested__ok(bool createFromExpression, bool applyToExpression)
    {
        var specification1 = Create<ProjectedClass>(
            createFromExpression,
            x => x.Name == "A",
            x => x.Name == "A"
        );

        var specification = Create<ProjectedClass>(
            createFromExpression,
            x => x.Name == "B" || specification1.IsSatisfiedBy(x),
            x => x.Name == "B" || specification1.IsSatisfiedBy(x)
        );

        var result = specification.IsSatisfiedBy(new ProjectedClass("A"));

        Assert.That(result, Is.True);
    }

    private Specification<TSource> Create<TSource>(
        bool createFromExpression,
        Expression<Func<TSource, bool>> expressionFunc,
        Func<TSource, bool> delegateFunc
    )
    {
        if (createFromExpression)
        {
            return new Specification<TSource>(expressionFunc);
        }
        else
        {
            return new Specification<TSource>(expressionFunc, delegateFunc);
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
}