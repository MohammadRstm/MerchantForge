namespace MerchForge.api.Models;

using MerchForge.api.Enums;
public class Subscription
{
    public Guid Id { get; set; }

    public Guid BusinessId { get; set; }

    public Guid SubscriptionPlanId { get; set; }

    public SubscriptionStatus Status { get; set; }

    public DateTime CurrentPeriodStart { get; set; }

    public DateTime CurrentPeriodEnd { get; set; }

    /// <summary>
    /// The owner cancelled, but the business keeps full access through the end of
    /// the already-paid-for period - RenewSubscriptionPeriodsJob checks this
    /// instead of renewing once CurrentPeriodEnd passes, terminating the
    /// subscription (Status -> Cancelled) and notifying SuperAdmins to take the
    /// website down rather than rolling the period forward.
    /// </summary>
    public bool CancelAtPeriodEnd { get; set; }

    public string? Provider { get; set; }

    public string? ExternalSubscriptionId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Business Business { get; set; } = null!;

    public SubscriptionPlan SubscriptionPlan { get; set; } = null!;
}