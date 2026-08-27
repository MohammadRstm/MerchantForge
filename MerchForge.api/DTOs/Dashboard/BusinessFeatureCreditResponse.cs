namespace MerchForge.api.DTOs.Dashboard;

public class BusinessFeatureCreditResponse
{
    public string FeatureKey { get; set; } = string.Empty;

    public string FeatureName { get; set; } = string.Empty;

    public int CreditsRemaining { get; set; }

    public int CreditsGrantedTotal { get; set; }
}
