namespace MerchForge.api.Models;

public class Feature
{
    public Guid Id { get; set; }

    public string Key { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Whether this feature can be bought independently of a subscription plan, as a
    /// credit package. Explicit rather than inferred from "has packages", so a
    /// feature's purchasability doesn't silently change because its packages were
    /// temporarily deactivated.
    /// </summary>
    public bool SupportsCreditPurchase { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<PlanFeature> PlanFeatures { get; set; }
        = new List<PlanFeature>();

    public ICollection<FeatureCreditPackage> CreditPackages { get; set; }
        = new List<FeatureCreditPackage>();
}