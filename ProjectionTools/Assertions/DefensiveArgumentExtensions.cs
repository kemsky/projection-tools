using System.Runtime.CompilerServices;

namespace ProjectionTools.Assertions;

internal static class DefensiveArgumentExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ArgumentNotNull<T>(this Defensive defensive, T? value, string argumentName)
    {
        if (!(value is object))
        {
            defensive.Throw($"Argument \"{argumentName}\" must be not null");
        }
    }
}