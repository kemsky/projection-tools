using System.Linq.Expressions;
using AgileObjects.ReadableExpressions;
using NUnit.Framework;
using ProjectionTools.Expressions;

namespace ProjectionTools.Tests.Expressions;

[Parallelizable(ParallelScope.All)]
[TestFixture]
public class CompositeVisitorTest
{
    [Test]
    public void Visit__Empty()
    {
        var visitor = new CompositeVisitor();

        Expression<Func<string, string>> expression = x => x + "A";

        var result = visitor.Visit(expression);

        Assert.That(result.ToReadableString(), Is.EqualTo("x => x + \"A\""));
    }
    
    [Test]
    public void Visit__Not_Empty()
    {
        Expression<Func<string, string>> expression = x => x + "A";

        var visitor = new CompositeVisitor(new ReplaceParameterVisitor(expression.Parameters[0], Expression.Parameter(typeof(string), "p")));

        var result = visitor.Visit(expression);

        Assert.That(result.ToReadableString(), Is.EqualTo("p => p + \"A\""));
    }
}