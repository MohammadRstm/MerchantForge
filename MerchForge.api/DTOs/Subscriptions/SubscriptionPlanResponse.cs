using MerchForge.api.Enums;

namespace MerchForge.api.DTOs.Subscriptions;

public class SubscriptionPlanResponse
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public decimal Price { get; set; }

    public string Currency { get; set; } = null!;

    public BillingInterval BillingInterval { get; set; }

    public bool IsActive { get; set; }

    public List<PlanFeatureResponse> Features { get; set; }
        = new();
}