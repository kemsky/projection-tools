using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace ProjectionTools.Assertions;

internal sealed class Defensive
{
    public static readonly Defensive Contract = new();

    private Defensive()
    {
    }

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Throw(string message)
    {
        throw new InvalidOperationException(message);
    }
}