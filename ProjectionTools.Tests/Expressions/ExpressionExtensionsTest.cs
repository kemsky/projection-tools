using System.Linq.Expressions;
using System.Reflection;
using AgileObjects.ReadableExpressions;
using NUnit.Framework;
using ProjectionTools.Expressions;
using ProjectionTools.Projections;
using ProjectionTools.Specifications;

namespace ProjectionTools.Tests.Expressions;

[Parallelizable(ParallelScope.All)]
[TestFixture]
public class ExpressionExtensionsTest
{
    [Test]
    public void TryEvaluate__Binary()
    {
        Expression expression1 = Expression.MakeMemberAccess(null, typeof(TryEvaluateTest).GetField("Instance", BindingFlags.Static | BindingFlags.Public)!);
        Expression expression = Expression.MakeMemberAccess(expression1, typeof(TryEvaluateTest).GetProperty("A")!);

        Assert.That(expression.TryEvaluate(out _), Is.False);
    }

    [Test]
    public void TryEvaluate__Constant()
    {
        Expression expression = Expression.Constant(true);

        Assert.That(expression.TryEvaluate(out var result), Is.True);
        Assert.That(result, Is.True);
    }

    [Test]
    public void Evaluate_Impossible()
    {
        Expression<Func<string, bool>> expression = x => x.Length == 1;

        Assert.Throws<InvalidOperationException>(() =>
        {
            expression.Evaluate();
        });
    }

    [Test]
    public void EvaluateNotNull_Null()
    {
        Expression expression = Expression.Constant(null, typeof(string));

        Assert.Throws<InvalidOperationException>(() =>
        {
            expression.EvaluateNotNull();
        });
    }

    [Test]
    public void EvaluateNotNull_Impossible()
    {
        Expression<Func<string, bool>> expression = x => x.Length == 1;

        Assert.Throws<InvalidOperationException>(() =>
        {
            expression.EvaluateNotNull();
        });
    }

    [Test]
    public void And()
    {
        Expression<Func<string, bool>> expression1 = x => x == "A";
        Expression<Func<string, bool>> expression2 = x => x.Length == 1;

        var result = expression1.And(expression2);

        Assert.That(result.ToReadableString(), Is.EqualTo("x => (x == \"A\") && (x.Length == 1)"));
    }

    [Test]
    public void Or()
    {
        Expression<Func<string, bool>> expression1 = x => x == "A";
        Expression<Func<string, bool>> expression2 = x => x.Length == 1;

        var result = expression1.Or(expression2);

        Assert.That(result.ToReadableString(), Is.EqualTo("x => (x == \"A\") || (x.Length == 1)"));
    }

    [Test]
    public void Not()
    {
        Expression<Func<string, bool>> expression1 = x => x == "A";

        var result = expression1.Not();

        Assert.That(result.ToReadableString(), Is.EqualTo("x => !(x == \"A\")"));
    }

    [Test]
    public void ApplyTo()
    {
        Expression<Func<string, bool>> expression1 = x => x == "A";
        Expression<Func<string, string>> expression2 = x => x.Length + "B";

        var result = expression1.ApplyTo(expression2);

        Assert.That(result.ToReadableString(), Is.EqualTo("x => (x.Length + \"B\") == \"A\""));
    }

    [Test]
    public void ProjectTo()
    {
        Expression<Func<string, bool>> source = x => x == "A";
        Expression<Func<bool, string>> projection = x => x.ToString();

        var result = source.ProjectTo(projection);

        Assert.That(result.ToReadableString(), Is.EqualTo("x => (x == \"A\").ToString()"));
    }

    [Test]
    public void BindArgument()
    {
        var instance = new BindArgumentTest();

        // captures A property
        Expression<Func<string>> lambda = () => instance.A;

        // captures x parameter
        Func<string, long, Expression<Func<string, string>>> factory = (x, y) => z => z + x;

        var expression = factory("TEMP", 0L);

        // replace captured x with A by name, type
        var result = expression.BindArgument(lambda, factory.Method.GetParameters()[0]);

        Assert.That(result.ToReadableString(), Is.EqualTo("z => z + A"));

        var compiled = result.Compile();

        Assert.That(compiled("B"), Is.EqualTo("BA"));
    }

    [Test]
    public void BindArguments()
    {
        var instance = new BindArgumentTest();

        // captures A property
        Expression<Func<string>> lambda1 = () => instance.A;
        // captures B property
        Expression<Func<long>> lambda2 = () => instance.B;

        // captures x, y parameters
        Func<string, long, Expression<Func<string, string>>> factory = (x, y) => z => z + x + y;

        var expression = factory("TEMP", 0L);

        // replace captured x with A by name, type
        var result = expression.BindArguments(lambda1, lambda2, factory.Method.GetParameters()[0], factory.Method.GetParameters()[1]);

        Assert.That(result.ToReadableString(), Is.EqualTo("z => z + A + B"));

        var compiled = result.Compile();

        Assert.That(compiled("B"), Is.EqualTo("BA1"));
    }

    [Test]
    [TestCase("HasName", "HasName", true)]
    [TestCase("HasName", "HasName1", false)]
    [TestCase("HasName1", "HasName1", false)]
    public void HasName(string method, string name, bool result)
    {
        MethodInfo? methodInfo = typeof(ExpressionExtensionsTest).GetMethod(method);

        Assert.That(methodInfo.HasName(name), Is.EqualTo(result));
    }

    [Test]
    public void IsProjectionType()
    {
        var type = typeof(Projection<string, string>);

        Assert.That(type.IsProjectionType(), Is.True);
    }

    [Test]
    public void IsProjectionMember()
    {
        var type = typeof(Projection<string, string>);

        var member = type.GetMember(nameof(Projection<string, string>.Project)).Single();

        Assert.That(member.IsProjectionMember(), Is.True);
    }

    [Test]
    public void IsSpecificationType()
    {
        var type = typeof(Specification<string>);

        Assert.That(type.IsSpecificationType(), Is.True);
    }

    [Test]
    public void IsSpecificationMember()
    {
        var type = typeof(Specification<string>);

        var member = type.GetMember(nameof(Specification<string>.IsSatisfiedBy)).Single();

        Assert.That(member.IsSpecificationMember(), Is.True);
    }

    [Test]
    public void IsSpecificationFactoryType()
    {
        var type = typeof(SpecificationFactory<string, string>);

        Assert.That(type.IsSpecificationFactoryType(), Is.True);
    }

    [Test]
    public void IsSpecificationFactory2Type()
    {
        var type = typeof(SpecificationFactory<string, string, string>);

        Assert.That(type.IsSpecificationFactory2Type(), Is.True);
    }

    private class BindArgumentTest
    {
        public string A { get; } = "A";

        public long B { get; } = 1L;
    }

    private class TryEvaluateTest
    {
        public static readonly TryEvaluateTest Instance = new TryEvaluateTest();

        public string A { get; } = "A";
    }
}