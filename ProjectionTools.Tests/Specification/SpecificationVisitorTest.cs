using System.Linq.Expressions;
using AgileObjects.ReadableExpressions;
using NUnit.Framework;
using ProjectionTools.Expressions;
using ProjectionTools.Specifications;

namespace ProjectionTools.Tests.Specification;

[Parallelizable(ParallelScope.All)]
[TestFixture]
public class SpecificationVisitorTest
{
    private static readonly SpecificationVisitor Visitor = new();

    private static Specification<string> CreateSpec => new(s => s == "A");

    private static readonly Specification<string> StaticFieldSpec = CreateSpec;

    private static Specification<string> StaticPropertySpec { get; } = CreateSpec;

    private readonly Specification<string> _fieldSpec = CreateSpec;

    private Specification<string> PropertySpec { get; } = CreateSpec;

    [Test]
    [TestCase(SpecSource.Local)]
    [TestCase(SpecSource.Field)]
    [TestCase(SpecSource.Property)]
    [TestCase(SpecSource.StaticField)]
    [TestCase(SpecSource.StaticProperty)]
    [TestCase(SpecSource.Parameter)]
    public void Specification__IsSatisfiedBy_Invocation(SpecSource source)
    {
        var expression = CreateCallExpression(source);

        var result = (LambdaExpression)Visitor.Visit(expression);

        Assert.That(result.ToReadableString(), Is.EqualTo(@"x => x == ""A"""));

        result.Compile();
    }

    [Test]
    [TestCase(SpecSource.Local)]
    [TestCase(SpecSource.Field)]
    [TestCase(SpecSource.Property)]
    [TestCase(SpecSource.StaticField)]
    [TestCase(SpecSource.StaticProperty)]
    [TestCase(SpecSource.Parameter)]
    public void Specification__IsSatisfiedBy_Negated_Invocation(SpecSource source)
    {
        var expression = CreateNegatedCallExpression(source);

        var result = (LambdaExpression)Visitor.Visit(expression);

        Assert.That(result.ToReadableString(), Is.EqualTo(@"x => !(x == ""A"")"));

        result.Compile();
    }

    [Test]
    [TestCase(SpecSource.Local)]
    [TestCase(SpecSource.Field)]
    [TestCase(SpecSource.Property)]
    [TestCase(SpecSource.StaticField)]
    [TestCase(SpecSource.StaticProperty)]
    [TestCase(SpecSource.Parameter)]
    public void Specification__IsSatisfiedBy_Reference(SpecSource source)
    {
        var expression = CreateReferenceExpression(source);

        var result = (LambdaExpression)Visitor.Visit(expression);

        Assert.That(result.ToReadableString(), Is.EqualTo(@"x => x.Any(s => s == ""A"")"));

        result.Compile();
    }

    [Test]
    [TestCase(SpecSource.Local)]
    [TestCase(SpecSource.Field)]
    [TestCase(SpecSource.Property)]
    [TestCase(SpecSource.StaticField)]
    [TestCase(SpecSource.StaticProperty)]
    [TestCase(SpecSource.Parameter)]
    public void Specification__Value(SpecSource source)
    {
        var expression = CreateValueExpression(source);

        var result = (LambdaExpression)Visitor.Visit(expression);

        Assert.That(result.ToReadableString(), Is.EqualTo(@"x => x.Any(s => s == ""A"")"));

        result.Compile();
    }

    [Test]
    [TestCase(SpecSource.Local)]
    [TestCase(SpecSource.Field)]
    [TestCase(SpecSource.Property)]
    [TestCase(SpecSource.StaticField)]
    [TestCase(SpecSource.StaticProperty)]
    [TestCase(SpecSource.Parameter)]
    public void Specification__Negated_Value(SpecSource source)
    {
        var expression = CreateNegatedValueExpression(source);

        var result = (LambdaExpression)Visitor.Visit(expression);

        Assert.That(result.ToReadableString(), Is.EqualTo(@"x => x.Any(s => !(s == ""A""))"));

        result.Compile();
    }

    [Test]
    [TestCase(SpecSource.Local)]
    [TestCase(SpecSource.Field)]
    [TestCase(SpecSource.Property)]
    [TestCase(SpecSource.StaticField)]
    [TestCase(SpecSource.StaticProperty)]
    [TestCase(SpecSource.Parameter)]
    public void Specification__Combined_Or_Value(SpecSource source)
    {
        var expression = CreateCombinedOrExpression(source);

        var result = (LambdaExpression)Visitor.Visit(expression);

        Assert.That(result.ToReadableString(), Is.EqualTo(@"x => x.Any(s => ((s == ""A"") || (s == ""A"")) || (s == ""A""))"));

        result.Compile();
    }

    [Test]
    [TestCase(SpecSource.Local)]
    [TestCase(SpecSource.Field)]
    [TestCase(SpecSource.Property)]
    [TestCase(SpecSource.StaticField)]
    [TestCase(SpecSource.StaticProperty)]
    [TestCase(SpecSource.Parameter)]
    public void Specification__Combined_Bit_Or_Value(SpecSource source)
    {
        var expression = CreateCombinedBitOrExpression(source);

        var result = (LambdaExpression)Visitor.Visit(expression);

        Assert.That(result.ToReadableString(), Is.EqualTo(@"x => x.Any(s => ((s == ""A"") || (s == ""A"")) || (s == ""A""))"));

        result.Compile();
    }

    [Test]
    [TestCase(SpecSource.Local)]
    [TestCase(SpecSource.Field)]
    [TestCase(SpecSource.Property)]
    [TestCase(SpecSource.StaticField)]
    [TestCase(SpecSource.StaticProperty)]
    [TestCase(SpecSource.Parameter)]
    public void Specification__Combined_And_Value(SpecSource source)
    {
        var expression = CreateCombinedAndExpression(source);

        var result = (LambdaExpression)Visitor.Visit(expression);

        Assert.That(result.ToReadableString(), Is.EqualTo(@"x => x.Any(s => ((s == ""A"") && (s == ""A"")) && (s == ""A""))"));

        result.Compile();
    }

    [Test]
    [TestCase(SpecSource.Local)]
    [TestCase(SpecSource.Field)]
    [TestCase(SpecSource.Property)]
    [TestCase(SpecSource.StaticField)]
    [TestCase(SpecSource.StaticProperty)]
    [TestCase(SpecSource.Parameter)]
    public void Specification__Combined_Bit_And_Value(SpecSource source)
    {
        var expression = CreateCombinedBitAndExpression(source);

        var result = (LambdaExpression)Visitor.Visit(expression);

        Assert.That(result.ToReadableString(), Is.EqualTo(@"x => x.Any(s => ((s == ""A"") && (s == ""A"")) && (s == ""A""))"));

        result.Compile();
    }

    private Expression<Func<string, bool>> CreateCallExpression(SpecSource source)
    {
        var parameter = CreateSpec;

        return CreateCallExpression(source, parameter);
    }

    private Expression<Func<string, bool>> CreateCallExpression(SpecSource source, Specification<string> parameter)
    {
        var local = CreateSpec;

        switch (source)
        {
            case SpecSource.Local:
                return x => local.IsSatisfiedBy(x);
            case SpecSource.Field:
                return x => _fieldSpec.IsSatisfiedBy(x);
            case SpecSource.Property:
                return x => PropertySpec.IsSatisfiedBy(x);
            case SpecSource.StaticField:
                return x => StaticFieldSpec.IsSatisfiedBy(x);
            case SpecSource.StaticProperty:
                return x => StaticPropertySpec.IsSatisfiedBy(x);
            case SpecSource.Parameter:
                return x => parameter.IsSatisfiedBy(x);
            default:
                throw new Exception(source.ToString());
        }
    }

    private Expression<Func<string, bool>> CreateNegatedCallExpression(SpecSource source)
    {
        var parameter = CreateSpec;

        return CreateNegatedCallExpression(source, parameter);
    }

    private Expression<Func<string, bool>> CreateNegatedCallExpression(SpecSource source, Specification<string> parameter)
    {
        var local = CreateSpec;

        switch (source)
        {
            case SpecSource.Local:
                return x => !local.IsSatisfiedBy(x);
            case SpecSource.Field:
                return x => !_fieldSpec.IsSatisfiedBy(x);
            case SpecSource.Property:
                return x => !PropertySpec.IsSatisfiedBy(x);
            case SpecSource.StaticField:
                return x => !StaticFieldSpec.IsSatisfiedBy(x);
            case SpecSource.StaticProperty:
                return x => !StaticPropertySpec.IsSatisfiedBy(x);
            case SpecSource.Parameter:
                return x => !parameter.IsSatisfiedBy(x);
            default:
                throw new Exception(source.ToString());
        }
    }

    private Expression<Func<List<string>, bool>> CreateReferenceExpression(SpecSource source)
    {
        var parameter = CreateSpec;

        return CreateReferenceExpression(source, parameter);
    }

    private Expression<Func<List<string>, bool>> CreateReferenceExpression(SpecSource source, Specification<string> parameter)
    {
        var local = CreateSpec;

        switch (source)
        {
            case SpecSource.Local:
                return x => x.Any(local.IsSatisfiedBy);
            case SpecSource.Field:
                return x => x.Any(_fieldSpec.IsSatisfiedBy);
            case SpecSource.Property:
                return x => x.Any(PropertySpec.IsSatisfiedBy);
            case SpecSource.StaticField:
                return x => x.Any(StaticFieldSpec.IsSatisfiedBy);
            case SpecSource.StaticProperty:
                return x => x.Any(StaticPropertySpec.IsSatisfiedBy);
            case SpecSource.Parameter:
                return x => x.Any(parameter.IsSatisfiedBy);
            default:
                throw new Exception(source.ToString());
        }
    }

    private Expression<Func<List<string>, bool>> CreateValueExpression(SpecSource source)
    {
        var parameter = CreateSpec;

        return CreateValueExpression(source, parameter);
    }

    private Expression<Func<List<string>, bool>> CreateValueExpression(SpecSource source, Specification<string> parameter)
    {
        var spec = CreateSpec;

        switch (source)
        {
            case SpecSource.Local:
                return x => x.Any(spec);
            case SpecSource.Field:
                return x => x.Any(_fieldSpec);
            case SpecSource.Property:
                return x => x.Any(PropertySpec);
            case SpecSource.StaticField:
                return x => x.Any(StaticFieldSpec);
            case SpecSource.StaticProperty:
                return x => x.Any(StaticPropertySpec);
            case SpecSource.Parameter:
                return x => x.Any(parameter);
            default:
                throw new Exception(source.ToString());
        }
    }

    private Expression<Func<List<string>, bool>> CreateNegatedValueExpression(SpecSource source)
    {
        var parameter = CreateSpec;

        return CreateNegatedValueExpression(source, parameter);
    }

    private Expression<Func<List<string>, bool>> CreateNegatedValueExpression(SpecSource source, Specification<string> parameter)
    {
        var local = CreateSpec;

        switch (source)
        {
            case SpecSource.Local:
                return x => x.Any(!local);
            case SpecSource.Field:
                return x => x.Any(!_fieldSpec);
            case SpecSource.Property:
                return x => x.Any(!PropertySpec);
            case SpecSource.StaticField:
                return x => x.Any(!StaticFieldSpec);
            case SpecSource.StaticProperty:
                return x => x.Any(!StaticPropertySpec);
            case SpecSource.Parameter:
                return x => x.Any(!parameter);
            default:
                throw new Exception(source.ToString());
        }
    }

    private Expression<Func<List<string>, bool>> CreateCombinedOrExpression(SpecSource source)
    {
        var parameter = CreateSpec;

        return CreateCombinedOrExpression(source, parameter);
    }

    private Expression<Func<List<string>, bool>> CreateCombinedOrExpression(SpecSource source, Specification<string> parameter)
    {
        var local = CreateSpec;

        switch (source)
        {
            case SpecSource.Local:
                return x => x.Any(local || local || local);
            case SpecSource.Field:
                return x => x.Any(_fieldSpec || _fieldSpec || _fieldSpec);
            case SpecSource.Property:
                return x => x.Any(PropertySpec || PropertySpec || PropertySpec);
            case SpecSource.StaticField:
                return x => x.Any(StaticFieldSpec || StaticFieldSpec || StaticFieldSpec);
            case SpecSource.StaticProperty:
                return x => x.Any(StaticPropertySpec || StaticPropertySpec || StaticPropertySpec);
            case SpecSource.Parameter:
                return x => x.Any(parameter || parameter || parameter);
            default:
                throw new Exception(source.ToString());
        }
    }

    private Expression<Func<List<string>, bool>> CreateCombinedBitOrExpression(SpecSource source)
    {
        var parameter = CreateSpec;

        return CreateCombinedBitOrExpression(source, parameter);
    }

    private Expression<Func<List<string>, bool>> CreateCombinedBitOrExpression(SpecSource source, Specification<string> parameter)
    {
        var local = CreateSpec;

        switch (source)
        {
            case SpecSource.Local:
                return x => x.Any(local | local | local);
            case SpecSource.Field:
                return x => x.Any(_fieldSpec | _fieldSpec | _fieldSpec);
            case SpecSource.Property:
                return x => x.Any(PropertySpec | PropertySpec | PropertySpec);
            case SpecSource.StaticField:
                return x => x.Any(StaticFieldSpec | StaticFieldSpec | StaticFieldSpec);
            case SpecSource.StaticProperty:
                return x => x.Any(StaticPropertySpec | StaticPropertySpec | StaticPropertySpec);
            case SpecSource.Parameter:
                return x => x.Any(parameter | parameter | parameter);
            default:
                throw new Exception(source.ToString());
        }
    }

    private Expression<Func<List<string>, bool>> CreateCombinedAndExpression(SpecSource source)
    {
        var parameter = CreateSpec;

        return CreateCombinedAndExpression(source, parameter);
    }

    private Expression<Func<List<string>, bool>> CreateCombinedAndExpression(SpecSource source, Specification<string> parameter)
    {
        var local = CreateSpec;

        switch (source)
        {
            case SpecSource.Local:
                return x => x.Any(local && local && local);
            case SpecSource.Field:
                return x => x.Any(_fieldSpec && _fieldSpec && _fieldSpec);
            case SpecSource.Property:
                return x => x.Any(PropertySpec && PropertySpec && PropertySpec);
            case SpecSource.StaticField:
                return x => x.Any(StaticFieldSpec && StaticFieldSpec && StaticFieldSpec);
            case SpecSource.StaticProperty:
                return x => x.Any(StaticPropertySpec && StaticPropertySpec && StaticPropertySpec);
            case SpecSource.Parameter:
                return x => x.Any(parameter && parameter && parameter);
            default:
                throw new Exception(source.ToString());
        }
    }

    private Expression<Func<List<string>, bool>> CreateCombinedBitAndExpression(SpecSource source)
    {
        var parameter = CreateSpec;

        return CreateCombinedBitAndExpression(source, parameter);
    }

    private Expression<Func<List<string>, bool>> CreateCombinedBitAndExpression(SpecSource source, Specification<string> parameter)
    {
        var local = CreateSpec;

        switch (source)
        {
            case SpecSource.Local:
                return x => x.Any(local & local & local);
            case SpecSource.Field:
                return x => x.Any(_fieldSpec & _fieldSpec & _fieldSpec);
            case SpecSource.Property:
                return x => x.Any(PropertySpec & PropertySpec & PropertySpec);
            case SpecSource.StaticField:
                return x => x.Any(StaticFieldSpec & StaticFieldSpec & StaticFieldSpec);
            case SpecSource.StaticProperty:
                return x => x.Any(StaticPropertySpec & StaticPropertySpec & StaticPropertySpec);
            case SpecSource.Parameter:
                return x => x.Any(parameter & parameter & parameter);
            default:
                throw new Exception(source.ToString());
        }
    }

    public enum SpecSource
    {
        Local,

        Field,

        Property,

        StaticField,

        StaticProperty,

        Parameter
    }
}