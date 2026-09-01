namespace MerchForge.api.DTOs.Dashboard;

public class AdminSubscriptionListItemResponse
{
    public Guid SubscriptionId { get; set; }

    public Guid BusinessId { get; set; }

    public string BusinessName { get; set; } = string.Empty;

    public string OwnerFullName { get; set; } = string.Empty;

    public string OwnerEmail { get; set; } = string.Empty;

    public string? DomainName { get; set; }

    public Guid PlanId { get; set; }

    public string PlanName { get; set; } = string.Empty;

    public bool PlanIsActive { get; set; }

    public string BillingInterval { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTime CurrentPeriodStart { get; set; }

    public DateTime CurrentPeriodEnd { get; set; }

    public bool CancelAtPeriodEnd { get; set; }

    public DateTime CreatedAt { get; set; }
}
