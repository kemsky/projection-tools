using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using ProjectionTools.Assertions;
using ProjectionTools.Specifications;

namespace ProjectionTools.Expressions;

internal sealed class SpecificationFactoryInvocationVisitor : ExpressionVisitor
{
    [return: NotNullIfNotNull(nameof(node))]
    private Expression? VisitBase(Expression? node)
    {
        return Visit(node);
    }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (node.Method.DeclaringType.IsSpecificationFactoryType() && node.Method.HasName("For") && node.Method.ReturnType.IsSpecificationType())
        {
            Defensive.Contract.NotNull(node.Object);

            var factory = node.Object.EvaluateNotNull();

            if (node.Arguments[0] is LambdaExpression lambda)
            {
                var spec = ((ISpecificationFactoryInternal)factory).For(lambda);

                return VisitBase(Expression.Constant(spec));
            }
            else
            {
                var arg = node.Arguments[0].Evaluate();

                var spec = ((ISpecificationFactoryInternal)factory).For(arg);

                return VisitBase(Expression.Constant(spec));
            }
        }

        if (node.Method.DeclaringType.IsSpecificationFactory2Type() && node.Method.HasName("For") && node.Method.ReturnType.IsSpecificationType())
        {
            Defensive.Contract.NotNull(node.Object);

            var factory = node.Object.EvaluateNotNull();

            if (node.Arguments[0] is LambdaExpression lambda1 && node.Arguments[1] is LambdaExpression lambda2)
            {
                var spec = ((ISpecificationFactory2Internal)factory).For(lambda1, lambda2);

                return VisitBase(Expression.Constant(spec));
            }
            else
            {
                var arg1 = node.Arguments[0].Evaluate();
                var arg2 = node.Arguments[1].Evaluate();

                var spec = ((ISpecificationFactory2Internal)factory).For(arg1, arg2);

                return VisitBase(Expression.Constant(spec));
            }
        }

        return base.VisitMethodCall(node);
    }
}