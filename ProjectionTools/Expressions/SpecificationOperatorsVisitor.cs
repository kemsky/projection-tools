using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using ProjectionTools.Assertions;
using ProjectionTools.Specifications;

namespace ProjectionTools.Expressions;

internal sealed class SpecificationOperatorsVisitor : ExpressionVisitor
{
    [return: NotNullIfNotNull(nameof(node))]
    private Expression? VisitBase(Expression? node)
    {
        return Visit(node);
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
            ISpecificationInternal leftSpec;
            ISpecificationInternal rightSpec;

            if (binaryExpression.Left.TryEvaluate(out var leftValue))
            {
                Defensive.Contract.NotNull(leftValue);

                leftSpec = (ISpecificationInternal)leftValue;
            }
            else
            {
                leftSpec = (ISpecificationInternal)VisitBinary((BinaryExpression)binaryExpression.Left).EvaluateNotNull();
            }

            if (binaryExpression.Right.TryEvaluate(out var rightValue))
            {
                Defensive.Contract.NotNull(rightValue);

                rightSpec = (ISpecificationInternal)rightValue;
            }
            else
            {

                rightSpec = (ISpecificationInternal)VisitBinary((BinaryExpression)binaryExpression.Right).EvaluateNotNull();
            }

            return VisitBase(Expression.Constant(leftSpec.Or(rightSpec)));
        }

        // replace And
        if (
            (binaryExpression.NodeType == ExpressionType.AndAlso || binaryExpression.NodeType == ExpressionType.And)
            && binaryExpression.Method.IsSpecificationMember()
            && binaryExpression.Method.HasName("op_BitwiseAnd")
        )
        {
            ISpecificationInternal leftSpec;
            ISpecificationInternal rightSpec;

            if (binaryExpression.Left.TryEvaluate(out var leftValue))
            {
                Defensive.Contract.NotNull(leftValue);

                leftSpec = (ISpecificationInternal)leftValue;
            }
            else
            {
                leftSpec = (ISpecificationInternal)VisitBinary((BinaryExpression)binaryExpression.Left).EvaluateNotNull();
            }

            if (binaryExpression.Right.TryEvaluate(out var rightValue))
            {
                Defensive.Contract.NotNull(rightValue);

                rightSpec = (ISpecificationInternal)rightValue;
            }
            else
            {

                rightSpec = (ISpecificationInternal)VisitBinary((BinaryExpression)binaryExpression.Right).EvaluateNotNull();
            }

            return VisitBase(Expression.Constant(leftSpec.And(rightSpec)));
        }

        return base.VisitBinary(binaryExpression);
    }

    protected override Expression VisitUnary(UnaryExpression unaryExpression)
    {
        // replace Not operator
        if (
            unaryExpression.NodeType == ExpressionType.Not
            && unaryExpression.Method.IsSpecificationMember()
            && unaryExpression.Method.HasName("op_LogicalNot")
        )
        {
            var spec = (ISpecificationInternal)unaryExpression.Operand.EvaluateNotNull();

            return VisitBase(Expression.Constant(spec.Not()));
        }

        return base.VisitUnary(unaryExpression);
    }
}