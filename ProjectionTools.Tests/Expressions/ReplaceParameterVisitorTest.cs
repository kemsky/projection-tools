using System.Linq.Expressions;
using AgileObjects.ReadableExpressions;
using NUnit.Framework;
using ProjectionTools.Expressions;

namespace ProjectionTools.Tests.Expressions;

[Parallelizable(ParallelScope.All)]
[TestFixture]
public class ReplaceParameterVisitorTest
{
    [Test]
    public void Visit__Existing()
    {
        Expression<Func<string, string>> expression = x => x + "A";

        var visitor = new ReplaceParameterVisitor(expression.Parameters[0], Expression.Parameter(typeof(string), "p"));

        var result = visitor.Visit(expression);

        Assert.That(result.ToReadableString(), Is.EqualTo("p => p + \"A\""));
    }
    
    [Test]
    public void Visit__No_Existing()
    {
        Expression<Func<string, string, string>> expression = (x, y) => x + "A";

        var visitor = new ReplaceParameterVisitor(expression.Parameters[1], Expression.Parameter(typeof(string), "p"));

        var result = visitor.Visit(expression.Body);

        Assert.That(result.ToReadableString(), Is.EqualTo("x + \"A\""));
    }
}