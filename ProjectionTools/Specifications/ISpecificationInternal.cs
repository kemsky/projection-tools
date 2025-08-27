namespace ProjectionTools.Specifications;

internal interface ISpecificationInternal
{
    internal ISpecificationInternal Or(ISpecificationInternal input);

    internal ISpecificationInternal And(ISpecificationInternal input);

    internal ISpecificationInternal Not();
}