using System.Runtime.CompilerServices;

namespace ProjectionTools;

internal sealed class Defensive
{
    public static readonly Defensive Contract = new();

    private Defensive()
    {
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Throw(string message)
    {
        throw new AssertionException(message);
    }
}