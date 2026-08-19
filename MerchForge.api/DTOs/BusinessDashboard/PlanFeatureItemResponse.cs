namespace MerchForge.api.DTOs.BusinessDashboard;

public class PlanFeatureItemResponse
{
    public string FeatureKey { get; set; } = string.Empty;

    public string FeatureName { get; set; } = string.Empty;

    public int? Limit { get; set; }
}
