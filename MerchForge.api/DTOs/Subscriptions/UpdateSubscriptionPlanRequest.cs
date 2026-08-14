using MerchForge.api.Enums;

namespace MerchForge.api.DTOs.Subscriptions;

public class UpdateSubscriptionPlanRequest
{
    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public decimal Price { get; set; }

    public string Currency { get; set; } = "USD";

    public BillingInterval BillingInterval { get; set; }

    public bool IsActive { get; set; }

    public List<PlanFeatureRequest> Features { get; set; }
        = new();
}