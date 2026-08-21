namespace MerchForge.api.DTOs.Subscriptions;

/// <summary>One purchasable feature, its packages, and this business's current balance for it.</summary>
public class FeatureCreditOverviewResponse
{
    public string FeatureKey { get; set; } = null!;

    public string FeatureName { get; set; } = null!;

    public string? FeatureDescription { get; set; }

    /// <summary>Whether the plan already grants this feature - if so it's unlimited, and buying credits would be redundant.</summary>
    public bool IncludedInPlan { get; set; }

    public int CreditsRemaining { get; set; }

    public int CreditsGrantedTotal { get; set; }

    public List<FeatureCreditPackageResponse> Packages { get; set; } = [];
}
