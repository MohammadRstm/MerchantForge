namespace MerchForge.api.Models;

public class PlanFeature
{
    public Guid SubscriptionPlanId { get; set; }

    public Guid FeatureId { get; set; }

    public int? Limit { get; set; }

    public SubscriptionPlan SubscriptionPlan { get; set; } = null!;

    public Feature Feature { get; set; } = null!;
}