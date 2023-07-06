using System.Diagnostics.CodeAnalysis;

namespace ProjectionTools.Expressions;

#if NETSTANDARD2_0
internal static class StackExtensions
{
    public static bool TryPop<T>(this Stack<T> stack, [MaybeNullWhen(false)] out T value)
    {
        if (stack.Count > 0)
        {
            value = stack.Pop();

            return true;
        }

        value = default;

        return false;
    }
}
#endif