using MerchForge.api.Enums;

namespace MerchForge.api.DTOs.Subscriptions;

public class SubscriptionResponse
{
    public Guid Id { get; set; }

    public Guid BusinessId { get; set; }

    public Guid PlanId { get; set; }

    public string PlanName { get; set; } = null!;

    public SubscriptionStatus Status { get; set; }

    public DateTime CurrentPeriodStart { get; set; }

    public DateTime CurrentPeriodEnd { get; set; }
}