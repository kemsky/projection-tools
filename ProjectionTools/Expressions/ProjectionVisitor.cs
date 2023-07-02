using System.Linq.Expressions;
using ProjectionTools.Projections;

namespace ProjectionTools.Expressions;

internal sealed class ProjectionVisitor : ExpressionVisitor
{
    protected override Expression VisitInvocation(InvocationExpression node)
    {
        // replace Delegate call with projection expression body
        if (
            node.Expression is MemberExpression memberExpression
            && memberExpression.Member.DeclaringType?.IsGenericType == true
            && memberExpression.Member.DeclaringType.GetGenericTypeDefinition() == typeof(Projection<,>)
            && string.Equals(memberExpression.Member.Name, nameof(Projection<object, object>.Project), StringComparison.Ordinal)
            && memberExpression.Expression.TryEvaluate(out var projectionValue)
        )
        {
            var projection = (IProjectionExpressionAccessor)projectionValue;

            var lambda = projection.GetExpression();

            var body = lambda.Body;

            var rebindParameter = new ReplaceParameterVisitor(lambda.Parameters[0], node.Arguments[0]);

            var expression = rebindParameter.Visit(body);

            return Visit(expression)!;
        }

        return base.VisitInvocation(node);
    }

    protected override Expression VisitMember(MemberExpression memberExpression)
    {
        // replace Delegate property with projection expression
        if (
            memberExpression.Member.DeclaringType?.IsGenericType == true
            && memberExpression.Member.DeclaringType.GetGenericTypeDefinition() == typeof(Projection<,>)
            && string.Equals(memberExpression.Member.Name, nameof(Projection<object, object>.Project), StringComparison.Ordinal)
        )
        {
            return Visit(memberExpression.Expression)!;
        }

        // convert projection members to constants
        if (
            memberExpression.Type.IsGenericType
            && memberExpression.Type.GetGenericTypeDefinition() == typeof(Projection<,>)
            && memberExpression.TryEvaluate(out var memberValue)
        )
        {
            return Visit(Expression.Constant(memberValue, memberExpression.Type))!;
        }

        return base.VisitMember(memberExpression);
    }

    protected override Expression VisitConstant(ConstantExpression constantExpression)
    {
        // evaluate constant spec members and replace with spec expression
        if (
            constantExpression.Type.IsGenericType
            && constantExpression.Type.GetGenericTypeDefinition() == typeof(Projection<,>)
            && constantExpression.TryEvaluate(out var projectionValue)
        )
        {
            var projection = (IProjectionExpressionAccessor)projectionValue;

            var expression = projection.GetExpression();

            return Visit(expression)!;
        }

        return base.VisitConstant(constantExpression);
    }

    protected override Expression VisitUnary(UnaryExpression unaryExpression)
    {
        // replace implicit conversion with projection expression
        if (
            unaryExpression.NodeType == ExpressionType.Convert
            && unaryExpression.Method?.DeclaringType?.IsGenericType == true
            && unaryExpression.Method.DeclaringType.GetGenericTypeDefinition() == typeof(Projection<,>)
        )
        {
            return Visit(unaryExpression.Operand)!;
        }

        return base.VisitUnary(unaryExpression);
    }
}