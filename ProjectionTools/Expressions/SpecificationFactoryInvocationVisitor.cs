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

            if (node.Arguments[0].TryEvaluate(out var arg))
            {
                var spec = ((ISpecificationFactoryInternal)factory).For(arg);

                return VisitBase(Expression.Constant(spec));
            }
            else
            {
                var spec = ((ISpecificationFactoryInternal)factory).For(node.Arguments[0]);

                return VisitBase(Expression.Constant(spec));
            }
        }

        if (node.Method.DeclaringType.IsSpecificationFactory2Type() && node.Method.HasName("For") && node.Method.ReturnType.IsSpecificationType())
        {
            Defensive.Contract.NotNull(node.Object);

            var factory = node.Object.EvaluateNotNull();

            if (node.Method.GetParameters().Length == 1)
            {
                if (node.Arguments[0].TryEvaluate(out var arg))
                {
                    var spec = ((ISpecificationFactory2Internal)factory).For(arg);

                    return VisitBase(Expression.Constant(spec));
                }
                else
                {
                    var spec = ((ISpecificationFactory2Internal)factory).For(node.Arguments[0]);

                    return VisitBase(Expression.Constant(spec));
                }
            }
            else
            {
                if (node.Arguments[0].TryEvaluate(out var arg1))
                {
                    var specFactory = ((ISpecificationFactory2Internal)factory).For(arg1);

                    if (node.Arguments[1].TryEvaluate(out var arg2))
                    {
                        var spec = specFactory.For(arg2);

                        return VisitBase(Expression.Constant(spec));
                    }
                    else
                    {
                        var spec = specFactory.For(node.Arguments[1]);

                        return VisitBase(Expression.Constant(spec));
                    }
                }
                else
                {
                    var specFactory = ((ISpecificationFactory2Internal)factory).For(node.Arguments[0]);

                    if (node.Arguments[1].TryEvaluate(out var arg2))
                    {
                        var spec = specFactory.For(arg2);

                        return VisitBase(Expression.Constant(spec));
                    }
                    else
                    {
                        var spec = specFactory.For(node.Arguments[1]);

                        return VisitBase(Expression.Constant(spec));
                    }
                }
            }
        }

        return base.VisitMethodCall(node);
    }
}