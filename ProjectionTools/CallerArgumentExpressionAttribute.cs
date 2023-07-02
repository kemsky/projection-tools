namespace ProjectionTools;

#if NETSTANDARD2_0

[AttributeUsage(AttributeTargets.Parameter)]
public sealed class CallerArgumentExpressionAttribute : Attribute
{
    public string Name { get; }

    public CallerArgumentExpressionAttribute(string name)
    {
        Name = name;
    }
}

#endif