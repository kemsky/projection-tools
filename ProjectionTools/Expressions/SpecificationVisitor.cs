using System.Linq.Expressions;
using ProjectionTools.Assertions;
using ProjectionTools.Specifications;

namespace ProjectionTools.Expressions;

internal sealed class SpecificationVisitor : ExpressionVisitor
{
    protected override Expression VisitInvocation(InvocationExpression invocationExpression)
    {
        // replace IsSatisfiedBy invocation with spec expression body
        if (
            invocationExpression.Expression is MemberExpression memberExpression
            && memberExpression.Expression != null
            && memberExpression.Member.IsSpecificationMember()
            && memberExpression.Member.HasName(nameof(Specification<object>.IsSatisfiedBy))
            && memberExpression.Expression.TryEvaluate(out var memberValue)
        )
        {
            Defensive.Contract.NotNull(memberValue);

            var specification = (ISpecificationExpressionAccessor)memberValue;

            var lambda = specification.GetExpression();

            var replaceParameterVisitor = new ReplaceParameterVisitor(lambda.Parameters[0], invocationExpression.Arguments[0]);

            var expression = replaceParameterVisitor.Visit(lambda.Body);

            return Visit(expression);
        }

        return base.VisitInvocation(invocationExpression);
    }

    protected override Expression VisitMember(MemberExpression memberExpression)
    {
        // replace IsSatisfiedBy property with spec
        if (
            memberExpression.Expression != null
            && memberExpression.Member.IsSpecificationMember()
            && memberExpression.Member.HasName(nameof(Specification<object>.IsSatisfiedBy))
        )
        {
            return Visit(memberExpression.Expression);
        }

        // convert spec members to constants
        if (
            memberExpression.Type.IsSpecificationType()
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
            constantExpression.Type.IsSpecificationType()
            && constantExpression.TryEvaluate(out var constantValue)
        )
        {
            Defensive.Contract.NotNull(constantValue);

            var specification = (ISpecificationExpressionAccessor)constantValue;

            var expression = specification.GetExpression();

            return Visit(expression);
        }

        return base.VisitConstant(constantExpression);
    }

    protected override Expression VisitBinary(BinaryExpression binaryExpression)
    {
        // replace Or
        if (
            (binaryExpression.NodeType == ExpressionType.OrElse || binaryExpression.NodeType == ExpressionType.Or)
            && binaryExpression.Method.IsSpecificationMember()
            && binaryExpression.Method.HasName("op_BitwiseOr")
        )
        {
            var left = Visit(binaryExpression.Left);
            var right = Visit(binaryExpression.Right);

            LambdaExpression leftLambda;
            LambdaExpression rightLambda;

            if (left.TryEvaluate(out var evaluated1) && evaluated1 is LambdaExpression)
            {
                leftLambda = (LambdaExpression)evaluated1;
            }
            else
            {
                leftLambda = (LambdaExpression)left;
            }

            if (right.TryEvaluate(out var evaluated2) && evaluated2 is LambdaExpression)
            {
                rightLambda = (LambdaExpression)evaluated2;
            }
            else
            {
                rightLambda = (LambdaExpression)right;
            }

            var replaceParameterVisitor = new ReplaceParameterVisitor(rightLambda.Parameters[0], leftLambda.Parameters[0]);

            var body = replaceParameterVisitor.Visit(rightLambda.Body);

            return Visit(Expression.Lambda(Expression.Or(leftLambda.Body, body), leftLambda.Parameters));
        }

        // replace And
        if (
            (binaryExpression.NodeType == ExpressionType.AndAlso || binaryExpression.NodeType == ExpressionType.And)
            && binaryExpression.Method.IsSpecificationMember()
            && binaryExpression.Method.HasName("op_BitwiseAnd")
        )
        {
            var left = Visit(binaryExpression.Left);
            var right = Visit(binaryExpression.Right);

            LambdaExpression leftLambda;
            LambdaExpression rightLambda;

            if (left.TryEvaluate(out var evaluated1) && evaluated1 is LambdaExpression)
            {
                leftLambda = (LambdaExpression)evaluated1;
            }
            else
            {
                leftLambda = (LambdaExpression)left;
            }

            if (right.TryEvaluate(out var evaluated2) && evaluated2 is LambdaExpression)
            {
                rightLambda = (LambdaExpression)evaluated2;
            }
            else
            {
                rightLambda = (LambdaExpression)right;
            }

            var replaceParameterVisitor = new ReplaceParameterVisitor(rightLambda.Parameters[0], leftLambda.Parameters[0]);

            var body = replaceParameterVisitor.Visit(rightLambda.Body);

            return Visit(Expression.Lambda(Expression.And(leftLambda.Body, body), leftLambda.Parameters));
        }

        return base.VisitBinary(binaryExpression);
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
            return Visit(unaryExpression.Operand)!;
        }

        // replace Not operator
        if (
            unaryExpression.NodeType == ExpressionType.Not
            && unaryExpression.Method.IsSpecificationMember()
            && unaryExpression.Method.HasName("op_LogicalNot")
        )
        {
            var expression = (LambdaExpression)Visit(unaryExpression.Operand);

            return Visit(Expression.Lambda(Expression.Not(expression.Body), expression.Parameters));
        }

        return base.VisitUnary(unaryExpression);
    }
}