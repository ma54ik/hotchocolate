namespace HotChocolate.ApolloFederation.CertificationSchema.Types;

public class ProductDimension
{
    public ProductDimension(string size, double weight)
    {
        Size = size;
        Weight = weight;
    }

    public string? Size { get; }

    public double? Weight { get; }
}
