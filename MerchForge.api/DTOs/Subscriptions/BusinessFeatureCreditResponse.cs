namespace MerchForge.api.DTOs.Subscriptions;

public class BusinessFeatureCreditResponse
{
    public string FeatureKey { get; set; } = null!;

    public int CreditsRemaining { get; set; }

    public int CreditsGrantedTotal { get; set; }
}
