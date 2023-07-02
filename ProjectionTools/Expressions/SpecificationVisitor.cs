using System.Linq.Expressions;
using ProjectionTools.Specifications;

namespace ProjectionTools.Expressions;

internal sealed class SpecificationVisitor : ExpressionVisitor
{
    protected override Expression VisitInvocation(InvocationExpression invocationExpression)
    {
        // replace IsSatisfiedBy invocation with spec expression body
        if (
            invocationExpression.Expression is MemberExpression memberExpression
            && memberExpression.Member.DeclaringType?.IsGenericType == true
            && memberExpression.Member.DeclaringType.GetGenericTypeDefinition() == typeof(Specification<>)
            && string.Equals(memberExpression.Member.Name, nameof(Specification<object>.IsSatisfiedBy), StringComparison.Ordinal)
            && memberExpression.Expression.TryEvaluate(out var specificationValue)
        )
        {
            var specification = (ISpecificationExpressionAccessor)specificationValue;

            var lambda = specification.GetExpression();

            var replaceParameterVisitor = new ReplaceParameterVisitor(lambda.Parameters[0], invocationExpression.Arguments[0]);

            var expression = replaceParameterVisitor.Visit(lambda.Body);

            return Visit(expression)!;
        }

        return base.VisitInvocation(invocationExpression);
    }

    protected override Expression VisitMember(MemberExpression memberExpression)
    {
        // replace IsSatisfiedBy property with spec
        if (
            memberExpression.Member.DeclaringType?.IsGenericType == true
            && memberExpression.Member.DeclaringType.GetGenericTypeDefinition() == typeof(Specification<>)
            && string.Equals(memberExpression.Member.Name, nameof(Specification<object>.IsSatisfiedBy), StringComparison.Ordinal)
        )
        {
            return Visit(memberExpression.Expression)!;
        }

        // convert spec members to constants
        if (
            memberExpression.Type.IsGenericType
            && memberExpression.Type.GetGenericTypeDefinition() == typeof(Specification<>)
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
            && constantExpression.Type.GetGenericTypeDefinition() == typeof(Specification<>)
            && constantExpression.TryEvaluate(out var memberValue)
        )
        {
            var specification = (ISpecificationExpressionAccessor)memberValue;

            if (specification == null)
            {
                throw new InvalidOperationException("Specification can not be null");
            }

            var expression = specification.GetExpression();

            return Visit(expression)!;
        }

        return base.VisitConstant(constantExpression);
    }

    protected override Expression VisitBinary(BinaryExpression binaryExpression)
    {
        // replace Or
        if (
            (binaryExpression.NodeType == ExpressionType.OrElse || binaryExpression.NodeType == ExpressionType.Or)
            && binaryExpression.Method?.DeclaringType?.IsGenericType == true
            && binaryExpression.Method.DeclaringType.GetGenericTypeDefinition() == typeof(Specification<>)
            && string.Equals(binaryExpression.Method.Name, "op_BitwiseOr", StringComparison.Ordinal)
        )
        {
            var arg1 = (LambdaExpression)Visit(binaryExpression.Left)!;
            var arg2 = (LambdaExpression)Visit(binaryExpression.Right)!;

            var replaceParameterVisitor = new ReplaceParameterVisitor(arg2.Parameters[0], arg1.Parameters[0]);

            var body = replaceParameterVisitor.Visit(arg2.Body)!;

            return Visit(Expression.Lambda(Expression.Or(arg1.Body, body), arg1.Parameters))!;
        }

        // replace And
        if (
            (binaryExpression.NodeType == ExpressionType.AndAlso || binaryExpression.NodeType == ExpressionType.And)
            && binaryExpression.Method?.DeclaringType?.IsGenericType == true
            && binaryExpression.Method.DeclaringType.GetGenericTypeDefinition() == typeof(Specification<>)
            && string.Equals(binaryExpression.Method.Name, "op_BitwiseAnd", StringComparison.Ordinal)
        )
        {
            var arg1 = (LambdaExpression)Visit(binaryExpression.Left)!;
            var arg2 = (LambdaExpression)Visit(binaryExpression.Right)!;

            var replaceParameterVisitor = new ReplaceParameterVisitor(arg2.Parameters[0], arg1.Parameters[0]);

            var body = replaceParameterVisitor.Visit(arg2.Body)!;

            return Visit(Expression.Lambda(Expression.And(arg1.Body, body), arg1.Parameters))!;
        }

        return base.VisitBinary(binaryExpression);
    }

    protected override Expression VisitUnary(UnaryExpression unaryExpression)
    {
        // drop implicit conversion
        if (
            unaryExpression.NodeType == ExpressionType.Convert
            && unaryExpression.Method?.DeclaringType?.IsGenericType == true
            && unaryExpression.Method.DeclaringType.GetGenericTypeDefinition() == typeof(Specification<>)
            && string.Equals(unaryExpression.Method.Name, "op_Implicit", StringComparison.Ordinal)
        )
        {
            return Visit(unaryExpression.Operand)!;
        }

        // replace Not operator
        if (
            unaryExpression.NodeType == ExpressionType.Not
            && unaryExpression.Method?.DeclaringType?.IsGenericType == true
            && unaryExpression.Method.DeclaringType.GetGenericTypeDefinition() == typeof(Specification<>)
            && string.Equals(unaryExpression.Method.Name, "op_LogicalNot", StringComparison.Ordinal)
        )
        {
            var expression = (LambdaExpression)Visit(unaryExpression.Operand)!;

            return Visit(Expression.Lambda(Expression.Not(expression.Body), expression.Parameters))!;
        }

        return base.VisitUnary(unaryExpression);
    }
}