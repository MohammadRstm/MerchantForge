namespace MerchForge.api.DTOs.Subscriptions;

public class FeatureCreditPackageResponse
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public int Credits { get; set; }

    public decimal Price { get; set; }

    public string Currency { get; set; } = null!;
}
