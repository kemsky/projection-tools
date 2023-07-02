using System.Linq.Expressions;
using NUnit.Framework;
using ProjectionTools.Expressions;
using ProjectionTools.Projections;

namespace ProjectionTools.Tests.Projections;

[Parallelizable(ParallelScope.All)]
[TestFixture]
public class ProjectionVisitorTest
{
    private static readonly ProjectionVisitor Visitor = new();

    private static Projection<string, int> CreateProjection => new(s => s.Length);

    private static readonly Projection<string, int> StaticFieldProjection = CreateProjection;

    private static Projection<string, int> StaticPropertyProjection { get; } = CreateProjection;

    private readonly Projection<string, int> _fieldProjection = CreateProjection;

    private Projection<string, int> PropertyProjection { get; } = CreateProjection;

    [Test]
    [TestCase(ProjectionSource.Local)]
    [TestCase(ProjectionSource.Field)]
    [TestCase(ProjectionSource.Property)]
    [TestCase(ProjectionSource.StaticField)]
    [TestCase(ProjectionSource.StaticProperty)]
    [TestCase(ProjectionSource.Parameter)]
    public void Projection__Project_invocation(ProjectionSource source)
    {
        var expression = CreateCallExpression(source);

        var result = (LambdaExpression)Visitor.Visit(expression);

        Assert.That(result.ToString(), Is.EqualTo(@"x => x.Length"));

        result.Compile();
    }

    [Test]
    [TestCase(ProjectionSource.Local)]
    [TestCase(ProjectionSource.Field)]
    [TestCase(ProjectionSource.Property)]
    [TestCase(ProjectionSource.StaticField)]
    [TestCase(ProjectionSource.StaticProperty)]
    [TestCase(ProjectionSource.Parameter)]
    public void Projection__Project_reference(ProjectionSource source)
    {
        var expression = CreateReferenceExpression(source);

        var result = (LambdaExpression)Visitor.Visit(expression);

        Assert.That(result.ToString(), Is.EqualTo(@"x => x.Select(s => s.Length).First()"));

        result.Compile();
    }

    [Test]
    [TestCase(ProjectionSource.Local)]
    [TestCase(ProjectionSource.Field)]
    [TestCase(ProjectionSource.Property)]
    [TestCase(ProjectionSource.StaticField)]
    [TestCase(ProjectionSource.StaticProperty)]
    [TestCase(ProjectionSource.Parameter)]
    public void Projection__value(ProjectionSource source)
    {
        var expression = CreateValueExpression(source);

        var result = (LambdaExpression)Visitor.Visit(expression);

        Assert.That(result.ToString(), Is.EqualTo(@"x => x.Select(s => s.Length).First()"));

        result.Compile();
    }

    private Expression<Func<string, int>> CreateCallExpression(ProjectionSource source)
    {
        var parameter = CreateProjection;

        return CreateCallExpression(source, parameter);
    }

    private Expression<Func<string, int>> CreateCallExpression(ProjectionSource source, Projection<string, int> parameter)
    {
        var local = CreateProjection;

        switch (source)
        {
            case ProjectionSource.Local:
                return x => local.Project(x);
            case ProjectionSource.Field:
                return x => _fieldProjection.Project(x);
            case ProjectionSource.Property:
                return x => PropertyProjection.Project(x);
            case ProjectionSource.StaticField:
                return x => StaticFieldProjection.Project(x);
            case ProjectionSource.StaticProperty:
                return x => StaticPropertyProjection.Project(x);
            case ProjectionSource.Parameter:
                return x => parameter.Project(x);
            default:
                throw new Exception(source.ToString());
        }
    }

    private Expression<Func<List<string>, int>> CreateReferenceExpression(ProjectionSource source)
    {
        var parameter = CreateProjection;

        return CreateReferenceExpression(source, parameter);
    }

    private Expression<Func<List<string>, int>> CreateReferenceExpression(ProjectionSource source, Projection<string, int> parameter)
    {
        var local = CreateProjection;

        switch (source)
        {
            case ProjectionSource.Local:
                return x => x.Select(local.Project).First();
            case ProjectionSource.Field:
                return x => x.Select(_fieldProjection.Project).First();
            case ProjectionSource.Property:
                return x => x.Select(PropertyProjection.Project).First();
            case ProjectionSource.StaticField:
                return x => x.Select(StaticFieldProjection.Project).First();
            case ProjectionSource.StaticProperty:
                return x => x.Select(StaticPropertyProjection.Project).First();
            case ProjectionSource.Parameter:
                return x => x.Select(parameter.Project).First();
            default:
                throw new Exception(source.ToString());
        }
    }

    private Expression<Func<List<string>, int>> CreateValueExpression(ProjectionSource source)
    {
        var parameter = CreateProjection;

        return CreateValueExpression(source, parameter);
    }

    private Expression<Func<List<string>, int>> CreateValueExpression(ProjectionSource source, Projection<string, int> parameter)
    {
        var local = CreateProjection;

        switch (source)
        {
            case ProjectionSource.Local:
                return x => x.Select<string, int>(local).First();
            case ProjectionSource.Field:
                return x => x.Select<string, int>(_fieldProjection).First();
            case ProjectionSource.Property:
                return x => x.Select<string, int>(PropertyProjection).First();
            case ProjectionSource.StaticField:
                return x => x.Select<string, int>(StaticFieldProjection).First();
            case ProjectionSource.StaticProperty:
                return x => x.Select<string, int>(StaticPropertyProjection).First();
            case ProjectionSource.Parameter:
                return x => x.Select<string, int>(parameter).First();
            default:
                throw new Exception(source.ToString());
        }
    }

    public enum ProjectionSource
    {
        Local,

        Field,

        Property,

        StaticField,

        StaticProperty,

        Parameter
    }
}