namespace MerchForge.api.DTOs.Subscriptions;

public class PlanFeatureResponse
{
    public Guid FeatureId { get; set; }

    public string FeatureKey { get; set; } = null!;

    public string FeatureName { get; set; } = null!;

    public int? Limit { get; set; }
}