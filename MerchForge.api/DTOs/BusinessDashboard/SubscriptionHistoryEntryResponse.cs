namespace MerchForge.api.DTOs.BusinessDashboard;

/// <summary>
/// One row from the business's own Subscription history — every plan it has ever
/// been on, oldest replaced by newest (SubscribeToPlanAsync marks the prior row
/// Cancelled rather than deleting it). Purely a read of already-persisted rows, not
/// a new tracking mechanism.
/// </summary>
public class SubscriptionHistoryEntryResponse
{
    public Guid Id { get; set; }

    public string PlanName { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public string Currency { get; set; } = string.Empty;

    public string BillingInterval { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTime CurrentPeriodStart { get; set; }

    public DateTime CurrentPeriodEnd { get; set; }

    public bool CancelAtPeriodEnd { get; set; }

    public DateTime CreatedAt { get; set; }
}
