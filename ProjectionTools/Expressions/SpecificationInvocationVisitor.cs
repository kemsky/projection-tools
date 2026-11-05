using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using ProjectionTools.Specifications;

namespace ProjectionTools.Expressions;

internal sealed class SpecificationInvocationVisitor : ExpressionVisitor
{
    [return: NotNullIfNotNull(nameof(node))]
    private Expression? VisitBase(Expression? node)
    {
        return Visit(node);
    }

    protected override Expression VisitInvocation(InvocationExpression invocationExpression)
    {
        if (TryInlineIsSatisfiedBy(invocationExpression.Expression, invocationExpression.Arguments, out var expression))
        {
            return expression;
        }

        return base.VisitInvocation(invocationExpression);
    }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (node.Method.HasName(nameof(Func<object, bool>.Invoke)) && TryInlineIsSatisfiedBy(node.Object, node.Arguments, out var expression))
        {
            return expression;
        }

        // do not rewrite specification instances passed as arguments, e.g. Helper(specification, x)
        if (node.Method.GetParameters().Any(x => x.ParameterType.IsSpecificationType()))
        {
            return node.Update(VisitBase(node.Object), this.VisitArguments(node, x => x.IsSpecificationType()));
        }

        return base.VisitMethodCall(node);
    }

    private bool TryInlineIsSatisfiedBy(Expression? target, IReadOnlyList<Expression> arguments, [NotNullWhen(true)] out Expression? expression)
    {
        if (
            arguments.Count == 1
            && target is MemberExpression memberExpression
            && memberExpression.Expression != null
            && memberExpression.Member.IsSpecificationMember()
            && memberExpression.Member.HasName(nameof(Specification<object>.IsSatisfiedBy))
        )
        {
            var spec = (ISpecificationExpressionAccessor)memberExpression.Expression.EvaluateNotNull();

            var lambda = spec.GetExpression();

            var replaceParameterVisitor = new ReplaceParameterVisitor(lambda.Parameters[0], arguments[0]);

            expression = VisitBase(replaceParameterVisitor.Visit(lambda.Body));

            return true;
        }

        expression = null;

        return false;
    }

    protected override Expression VisitMember(MemberExpression memberExpression)
    {
        // replace IsSatisfiedBy property with spec e.g. Where(spec.IsSatisfiedBy)
        if (
            memberExpression.Expression != null
            && memberExpression.Member.IsSpecificationMember()
            && memberExpression.Member.HasName(nameof(Specification<object>.IsSatisfiedBy))
        )
        {
            var spec = (ISpecificationExpressionAccessor)memberExpression.Expression.EvaluateNotNull();

            return VisitBase(spec.GetExpression());
        }

        // convert spec fields, properties values to constants
        if (memberExpression.Type.IsSpecificationType())
        {
            var spec = (ISpecificationExpressionAccessor)memberExpression.EvaluateNotNull();

            return VisitBase(Expression.Constant(spec));
        }

        return base.VisitMember(memberExpression);
    }

    protected override Expression VisitConstant(ConstantExpression constantExpression)
    {
        // evaluate constant spec members and replace with spec expression
        if (constantExpression.Type.IsSpecificationType())
        {
            var spec = (ISpecificationExpressionAccessor)constantExpression.EvaluateNotNull();

            var expression = spec.GetExpression();

            return VisitBase(expression);
        }

        return base.VisitConstant(constantExpression);
    }

    protected override Expression VisitUnary(UnaryExpression unaryExpression)
    {
        // drop implicit conversion
        if (
            unaryExpression.NodeType == ExpressionType.Convert
            && unaryExpression.Method.IsSpecificationMember()
            && unaryExpression.Method.HasName("op_Implicit")
        )
        {
            return VisitBase(unaryExpression.Operand);
        }

        return base.VisitUnary(unaryExpression);
    }
}