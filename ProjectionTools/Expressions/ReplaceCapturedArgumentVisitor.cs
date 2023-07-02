using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace ProjectionTools.Expressions;

internal sealed class ReplaceCapturedArgumentVisitor : ExpressionVisitor
{
    private readonly ParameterInfo _parameter;

    private readonly Expression _replacement;

    public ReplaceCapturedArgumentVisitor(ParameterInfo parameter, Expression replacement)
    {
        _parameter = parameter;
        _replacement = replacement;
    }

    protected override Expression VisitMember(MemberExpression memberExpression)
    {
        // func argument gets *copied* to the field of compiler generated class,
        // this class belongs to method (also generated class) 

        // memberInfo has the same the name and type...
        if (string.Equals(memberExpression.Member.Name, _parameter.Name, StringComparison.Ordinal) && memberExpression.Type == _parameter.ParameterType)
        {
            // memberInfo is property/field of ConstantExpression
            // memberInfo has DeclaringType compiler generated
            if (memberExpression.Expression is ConstantExpression && memberExpression.Member.DeclaringType?.GetCustomAttribute<CompilerGeneratedAttribute>(inherit: false) != null)
            {
                return _replacement;
            }
        }

        return base.VisitMember(memberExpression);
    }
}