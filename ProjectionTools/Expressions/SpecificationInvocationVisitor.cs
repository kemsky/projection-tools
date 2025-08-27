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
        // replace IsSatisfiedBy invocation with spec expression body
        if (
            invocationExpression.Expression is MemberExpression memberExpression
            && memberExpression.Expression != null
            && memberExpression.Member.IsSpecificationMember()
            && memberExpression.Member.HasName(nameof(Specification<object>.IsSatisfiedBy))
        )
        {
            var spec = (ISpecificationExpressionAccessor)memberExpression.Expression.EvaluateNotNull();

            var lambda = spec.GetExpression();

            var replaceParameterVisitor = new ReplaceParameterVisitor(lambda.Parameters[0], invocationExpression.Arguments[0]);

            var expression = replaceParameterVisitor.Visit(lambda.Body);

            return VisitBase(expression);
        }

        return base.VisitInvocation(invocationExpression);
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