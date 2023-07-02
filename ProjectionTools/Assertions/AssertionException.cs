namespace ProjectionTools.Assertions;

internal sealed class AssertionException : Exception
{
    public AssertionException(string message) : base(message)
    {
    }
}