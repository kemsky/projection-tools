using System.Linq.Expressions;
using ProjectionTools.Assertions;
using ProjectionTools.Projections;

namespace ProjectionTools.Expressions;

internal sealed class ProjectionVisitor : ExpressionVisitor
{
    protected override Expression VisitInvocation(InvocationExpression node)
    {
        // replace Delegate call with projection expression body
        if (
            node.Expression is MemberExpression memberExpression
            && memberExpression.Expression != null
            && memberExpression.Member.IsProjectionMember()
            && string.Equals(memberExpression.Member.Name, nameof(Projection<object, object>.Project), StringComparison.Ordinal)
            && memberExpression.Expression.TryEvaluate(out var memberValue)
        )
        {
            Defensive.Contract.NotNull(memberValue);

            var projection = (IProjectionExpressionAccessor)memberValue;

            var lambda = projection.GetExpression();

            var body = lambda.Body;

            var visitor = new ReplaceParameterVisitor(lambda.Parameters[0], node.Arguments[0]);

            var expression = visitor.Visit(body);

            return Visit(expression);
        }

        return base.VisitInvocation(node);
    }

    protected override Expression VisitMember(MemberExpression memberExpression)
    {
        // replace Delegate property with projection expression
        if (
            memberExpression.Expression != null
            && memberExpression.Member.IsProjectionMember()
            && memberExpression.Member.HasName(nameof(Projection<object, object>.Project))
        )
        {
            return Visit(memberExpression.Expression);
        }

        // convert projection members to constants
        if (
            memberExpression.Type.IsProjectionType()
            && memberExpression.TryEvaluate(out var memberValue)
        )
        {
            return Visit(Expression.Constant(memberValue, memberExpression.Type));
        }

        return base.VisitMember(memberExpression);
    }

    protected override Expression VisitConstant(ConstantExpression constantExpression)
    {
        // evaluate constant spec members and replace with spec expression
        if (
            constantExpression.Type.IsProjectionType()
            && constantExpression.TryEvaluate(out var constantValue)
        )
        {
            Defensive.Contract.NotNull(constantValue);

            var projection = (IProjectionExpressionAccessor)constantValue;

            var expression = projection.GetExpression();

            return Visit(expression);
        }

        return base.VisitConstant(constantExpression);
    }

    protected override Expression VisitUnary(UnaryExpression unaryExpression)
    {
        // replace implicit conversion with projection expression
        if (
            unaryExpression.NodeType == ExpressionType.Convert
            && unaryExpression.Method.IsProjectionMember()
        )
        {
            return Visit(unaryExpression.Operand);
        }

        return base.VisitUnary(unaryExpression);
    }
}