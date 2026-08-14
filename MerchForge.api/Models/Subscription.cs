namespace MerchForge.api.Models;

public class Subscription
{
    public Guid Id { get; set; }

    public Guid BusinessId { get; set; }

    public Guid SubscriptionPlanId { get; set; }

    public SubscriptionStatus Status { get; set; }

    public DateTime CurrentPeriodStart { get; set; }

    public DateTime CurrentPeriodEnd { get; set; }

    public string? Provider { get; set; }

    public string? ExternalSubscriptionId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public Business Business { get; set; } = null!;

    public SubscriptionPlan SubscriptionPlan { get; set; } = null!;
}