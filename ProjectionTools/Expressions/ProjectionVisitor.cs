using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using ProjectionTools.Assertions;
using ProjectionTools.Projections;

namespace ProjectionTools.Expressions;

internal sealed class ProjectionVisitor : ExpressionVisitor
{
    protected override Expression VisitInvocation(InvocationExpression node)
    {
        if (TryInlineProject(node.Expression, node.Arguments, out var expression))
        {
            return expression;
        }

        return base.VisitInvocation(node);
    }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (node.Method.HasName(nameof(Func<object, object>.Invoke)) && TryInlineProject(node.Object, node.Arguments, out var expression))
        {
            return expression;
        }

        // do not rewrite projection instances passed as arguments, e.g. Helper(projection, x)
        if (node.Method.GetParameters().Any(x => x.ParameterType.IsProjectionType()))
        {
            return node.Update(Visit(node.Object), this.VisitArguments(node, x => x.IsProjectionType()));
        }

        return base.VisitMethodCall(node);
    }

    private bool TryInlineProject(Expression? target, IReadOnlyList<Expression> arguments, [NotNullWhen(true)] out Expression? expression)
    {
        if (
            arguments.Count == 1
            && target is MemberExpression memberExpression
            && memberExpression.Expression != null
            && memberExpression.Member.IsProjectionMember()
            && memberExpression.Member.HasName(nameof(Projection<object, object>.Project))
            && memberExpression.Expression.TryEvaluate(out var memberValue)
        )
        {
            Defensive.Contract.NotNull(memberValue);

            var projection = (IProjectionExpressionAccessor)memberValue;

            var lambda = projection.GetExpression();

            var visitor = new ReplaceParameterVisitor(lambda.Parameters[0], arguments[0]);

            expression = Visit(visitor.Visit(lambda.Body));

            return true;
        }

        expression = null;

        return false;
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