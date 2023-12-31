using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using ProjectionTools.Assertions;

namespace ProjectionTools.Expressions;

internal static class ExpressionExtensions
{
    public static bool TryEvaluate(this Expression expression, [MaybeNullWhen(false)] out object result)
    {
        if (expression is MemberExpression memberExpression)
        {
            if (memberExpression.Expression == null)
            {
                if (memberExpression.Member is PropertyInfo staticProperty)
                {
                    result = staticProperty.GetValue(null);

                    return true;
                }
                else if (memberExpression.Member is FieldInfo staticField)
                {
                    result = staticField.GetValue(null);

                    return true;
                }
                else
                {
                    Defensive.Contract.Throw($"Unexpected MemberInfo: {memberExpression}");
                }
            }

            var stack = new Stack<MemberExpression>();

            var e = memberExpression;

            while (e != null)
            {
                stack.Push(e);

                e = e.Expression as MemberExpression;
            }

            if (stack.Peek().Expression is ConstantExpression constantExpression)
            {
                var value = constantExpression.Value;

                while (stack.TryPop(out var stackExpression))
                {
                    if (stackExpression.Member is PropertyInfo propertyInfo)
                    {
                        value = propertyInfo.GetValue(value);
                    }
                    else if (stackExpression.Member is FieldInfo fieldInfo)
                    {
                        value = fieldInfo.GetValue(value);
                    }
                    else
                    {
                        Defensive.Contract.Throw($"Unexpected MemberInfo: {memberExpression}");
                    }
                }

                result = value;

                return true;
            }
            else
            {
                result = null;

                return false;
            }
        }
        else if (expression is ConstantExpression constantExpression)
        {
            result = constantExpression.Value;

            return true;
        }

        result = null;

        return false;
    }

    public static Expression<Func<T, bool>> And<T>(this Expression<Func<T, bool>> predicate1, Expression<Func<T, bool>> predicate2)
    {
        return predicate1.Compose(predicate2, Expression.AndAlso);
    }

    public static Expression<Func<T, bool>> Or<T>(this Expression<Func<T, bool>> predicate1, Expression<Func<T, bool>> predicate2)
    {
        return predicate1.Compose(predicate2, Expression.OrElse);
    }

    private static Expression<Func<T, bool>> Compose<T>(
        this Expression<Func<T, bool>> predicate1,
        Expression<Func<T, bool>> predicate2,
        Func<Expression, Expression, BinaryExpression> compose
    )
    {
        var firstBody = predicate1.Body;
        var secondBody = new ReplaceParameterVisitor(predicate2.Parameters[0], predicate1.Parameters[0]).Visit(predicate2.Body);

        return Expression.Lambda<Func<T, bool>>(compose(firstBody, secondBody), predicate1.Parameters);
    }

    public static Expression<Func<T, bool>> Not<T>(this Expression<Func<T, bool>> predicate)
    {
        return Expression.Lambda<Func<T, bool>>(Expression.Not(predicate.Body), predicate.Parameters);
    }

    public static Expression<Func<TParam, TReturn>> ApplyTo<TSource, TReturn, TParam>(
        this Expression<Func<TSource, TReturn>> expression,
        Expression<Func<TParam, TSource>> parameterExpression
    )
    {
        var body = new ReplaceParameterVisitor(expression.Parameters[0], parameterExpression.Body).Visit(expression.Body)!;

        return Expression.Lambda<Func<TParam, TReturn>>(body, parameterExpression.Parameters);
    }

    public static Expression<Func<TSource, TDestination>> ProjectTo<TSource, TResult, TDestination>(
        this Expression<Func<TSource, TResult>> expression,
        Expression<Func<TResult, TDestination>> projectionExpression
    )
    {
        var body = new ReplaceParameterVisitor(projectionExpression.Parameters[0], expression.Body).Visit(projectionExpression.Body)!;

        return Expression.Lambda<Func<TSource, TDestination>>(body, expression.Parameters);
    }

    public static TExpression BindArgument<TExpression, TArg>(
        this TExpression expression,
        Expression<Func<TArg>> argumentExpression,
        ParameterInfo parameterInfo
    ) where TExpression : Expression
    {
        var parameterBody = argumentExpression.Body;

        return (TExpression)new ReplaceCapturedArgumentVisitor(parameterInfo, parameterBody).Visit(expression)!;
    }

    public static TExpression BindArguments<TExpression, TArg1, TArg2>(
        this TExpression expression,
        Expression<Func<TArg1>> argument1Expression,
        Expression<Func<TArg2>> argument2Expression,
        ParameterInfo parameterInfo1,
        ParameterInfo parameterInfo2
    ) where TExpression : Expression
    {
        var argument1Body = argument1Expression.Body;
        var argument2Body = argument2Expression.Body;

        return (TExpression)new ReplaceCapturedArgumentVisitor(parameterInfo2, argument2Body).Visit(new ReplaceCapturedArgumentVisitor(parameterInfo1, argument1Body).Visit(expression))!;
    }
}