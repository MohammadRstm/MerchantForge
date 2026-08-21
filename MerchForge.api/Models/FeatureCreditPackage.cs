namespace MerchForge.api.Models;

/// <summary>
/// A purchasable SKU for a feature bought independently of a subscription plan —
/// e.g. "50 credits for $5". Buying one grants its Credits to the business's
/// BusinessFeatureCredit balance for that feature.
/// </summary>
public class FeatureCreditPackage
{
    public Guid Id { get; set; }

    public Guid FeatureId { get; set; }

    public string Name { get; set; } = null!;

    public int Credits { get; set; }

    public decimal Price { get; set; }

    public string Currency { get; set; } = "USD";

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Feature Feature { get; set; } = null!;
}
