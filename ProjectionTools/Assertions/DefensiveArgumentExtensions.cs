using System.Runtime.CompilerServices;
using System.Diagnostics.CodeAnalysis;

namespace ProjectionTools.Assertions;

internal static class DefensiveArgumentExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ArgumentNotNull<T>(this Defensive defensive, [NotNull] T value, [CallerArgumentExpression(nameof(value)), AllowNull] string argumentName = null)
    {
        if (!(value is object))
        {
            defensive.Throw($"Argument \"{argumentName}\" must be not null");
        }
    }
}

internal static class DefensiveExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void NotNull<T>(this Defensive defensive, [NotNull] T value, [CallerArgumentExpression(nameof(value)), AllowNull] string argumentName = null)
    {
        if (!(value is object))
        {
            defensive.Throw($"{argumentName} can not be null");
        }
    }
}