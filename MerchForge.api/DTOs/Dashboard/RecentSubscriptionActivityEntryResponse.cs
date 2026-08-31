namespace MerchForge.api.DTOs.Dashboard;

/// <summary>
/// One Subscription row's creation - either a business's very first subscription,
/// or a plan switch (the prior row was marked Cancelled and this one inserted).
/// A routine period renewal never appears here, since it advances the existing
/// row's period in place rather than creating a new one.
/// </summary>
public class RecentSubscriptionActivityEntryResponse
{
    public Guid BusinessId { get; set; }

    public string BusinessName { get; set; } = string.Empty;

    public string PlanName { get; set; } = string.Empty;

    public string BillingInterval { get; set; } = string.Empty;

    public bool IsNewSubscription { get; set; }

    public DateTime CreatedAt { get; set; }
}
