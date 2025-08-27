using System.Linq.Expressions;
using AgileObjects.ReadableExpressions;
using NUnit.Framework;
using ProjectionTools.Expressions;

namespace ProjectionTools.Tests.Expressions;

[Parallelizable(ParallelScope.All)]
[TestFixture]
public class ReplaceCapturedArgumentVisitorTest
{
    private string A { get; } = "A";

    [Test]
    public void Visit()
    {
        // captures A property
        Expression<Func<string>> lambda = () => A;

        // captures x parameter
        Func<string, long, Expression<Func<string, string>>> factory = (x, y) => z => z + x;

        var visitor = new ReplaceCapturedArgumentVisitor(factory.Method.GetParameters()[0], lambda.Body);

        var expression = factory("TEMP", 0L);

        // replace captured x with A by name, type
        var result = (Expression<Func<string, string>>)visitor.Visit(expression);

        Assert.That(result.ToReadableString(), Is.EqualTo("z => z + A"));

        var compiled = result.Compile();

        Assert.That(compiled("B"), Is.EqualTo("BA"));
    }
}