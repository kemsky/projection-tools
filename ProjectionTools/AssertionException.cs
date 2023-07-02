namespace ProjectionTools;

internal sealed class AssertionException : Exception
{
    public AssertionException(string message) : base(message)
    {
    }
}