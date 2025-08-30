using System.Linq.Expressions;
using NUnit.Framework;
using ProjectionTools.Expressions;

namespace ProjectionTools.Tests.Expressions;

public partial class ExpressionExtensionsTest
{
    [Test]
    public void TryEvaluate__Constant()
    {
        Expression<Func<string>> subject = () => "constant";

        var success = subject.Body.TryEvaluate(out var value);

        Assert.That(success, Is.True);
        Assert.That(value, Is.EqualTo("constant"));
    }

    [Test]
    public void TryEvaluate__Static_field()
    {
        Expression<Func<string>> subject = () => TryEvaluate.StringStaticField;

        var success = subject.Body.TryEvaluate(out var value);

        Assert.That(success, Is.True);
        Assert.That(value, Is.EqualTo("constant"));
    }

    [Test]
    public void TryEvaluate__Static_Property()
    {
        Expression<Func<string>> subject = () => TryEvaluate.StringStaticProperty;

        var success = subject.Body.TryEvaluate(out var value);

        Assert.That(success, Is.True);
        Assert.That(value, Is.EqualTo("constant"));
    }

    [Test]
    public void TryEvaluate__Nested_Static_Field()
    {
        Expression<Func<string>> subject = () => TryEvaluateTest.Instance.A;

        var success = subject.Body.TryEvaluate(out var result);

        Assert.That(success, Is.True);
        Assert.That(result, Is.EqualTo("A"));
    }

    [Test]
    public void TryEvaluate__Call_Expression__Not_Supported()
    {
        Expression<Func<string>> subject = () => TryEvaluate.StringStaticFunc();

        var success = subject.Body.TryEvaluate(out _);

        Assert.That(success, Is.False);
    }

    [Test]
    public void TryEvaluate__Static_Expression__Not_Supported()
    {
        Expression<Func<Func<string>>> subject = () => TryEvaluate.StringStaticFunc;

        var success = subject.Body.TryEvaluate(out _);

        Assert.That(success, Is.False);
    }

    [Test]
    public void TryEvaluate__Parameter_Expression__Not_Supported()
    {
        Expression<Func<string, string>> subject = x => x;

        var success = subject.Body.TryEvaluate(out _);

        Assert.That(success, Is.False);
    }

    [Test]
    public void TryEvaluate__Parameter_Property_Expression__Not_Supported()
    {
        Expression<Func<TryEvaluate, string>> subject = x => x.StringField;

        var success = subject.Body.TryEvaluate(out _);

        Assert.That(success, Is.False);
    }

    [Test]
    public void TryEvaluate__Field()
    {
        var instance = new TryEvaluate();

        Expression<Func<string>> subject = () => instance.StringField;

        var success = subject.Body.TryEvaluate(out var value);

        Assert.That(success, Is.True);
        Assert.That(value, Is.EqualTo("constant"));
    }

    [Test]
    public void TryEvaluate__Property()
    {
        var instance = new TryEvaluate();

        Expression<Func<string>> subject = () => instance.StringProperty;

        var success = subject.Body.TryEvaluate(out var value);

        Assert.That(success, Is.True);
        Assert.That(value, Is.EqualTo("constant"));
    }

    [Test]
    public void TryEvaluate__Nested_Property()
    {
        var value = new TestPropertyValue("123");

        Expression<Func<int>> expression = () => value.Value.Length;

        var memberExpression = (MemberExpression)expression.Body;

        Assert.That(memberExpression.TryEvaluate(out var result), Is.True);

        Assert.That(result, Is.EqualTo(3));
    }

    [Test]
    public void TryEvaluate__Local_var()
    {
        var local = "constant";

        Expression<Func<string>> subject = () => local;

        var success = subject.Body.TryEvaluate(out var value);

        Assert.That(success, Is.True);
        Assert.That(value, Is.EqualTo("constant"));
    }

    [Test]
    [TestCase("constant")]
    public void TryEvaluate__Local_Parameter(string parameter)
    {
        Expression<Func<string>> subject = () => parameter;

        var success = subject.Body.TryEvaluate(out var value);

        Assert.That(success, Is.True);
        Assert.That(value, Is.EqualTo("constant"));
    }

    private class TryEvaluate
    {
        public static readonly string StringStaticField = "constant";

        public static string StringStaticProperty { get; } = "constant";

        public static string StringStaticFunc() => "constant";

        // ReSharper disable once InconsistentNaming
        public readonly string StringField = "constant";

        public string StringProperty { get; } = "constant";
    }

    private class TryEvaluateTest
    {
        public static readonly TryEvaluateTest Instance = new TryEvaluateTest();

        public string A { get; } = "A";
    }

    private class TestPropertyValue
    {
        public string Value { get; }

        public TestPropertyValue(string value)
        {
            Value = value;
        }
    }
}