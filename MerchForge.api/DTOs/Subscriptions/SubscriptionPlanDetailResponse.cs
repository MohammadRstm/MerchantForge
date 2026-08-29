using MerchForge.api.DTOs.BusinessDashboard;

namespace MerchForge.api.DTOs.Subscriptions;

public class SubscriptionPlanDetailResponse
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public decimal Price { get; set; }

    public string Currency { get; set; } = string.Empty;

    public string BillingInterval { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public bool IsCustom { get; set; }

    /// <summary>Businesses currently on this plan with an Active subscription — informational, so an admin can see the impact before deactivating it.</summary>
    public int ActiveSubscriberCount { get; set; }

    public List<PlanFeatureItemResponse> Features { get; set; } = new();
}
