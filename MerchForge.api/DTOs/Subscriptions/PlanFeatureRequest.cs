namespace MerchForge.api.DTOs.Subscriptions;

public class PlanFeatureRequest
{
    public Guid FeatureId { get; set; }

    public int? Limit { get; set; }
}