using System.Runtime.CompilerServices;

namespace ProjectionTools;

internal static class DefensiveArgumentExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ArgumentNotNull<T>(this Defensive defensive, T value, [CallerArgumentExpression("value")] string argumentName = null)
    {
        if (!(value is object))
        {
            defensive.Throw($"Argument \"{argumentName}\" must be not null");
        }
    }
}