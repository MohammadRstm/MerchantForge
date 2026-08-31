using MerchForge.api.DTOs.Common;
using MerchForge.api.Enums;

namespace MerchForge.api.DTOs.Dashboard;

public class SubscriptionsQueryRequest : PagedQuery
{
    /// <summary>Matches business name, owner name, or owner email.</summary>
    public string? Search { get; set; }

    public Guid? PlanId { get; set; }

    public BillingInterval? BillingInterval { get; set; }

    public SubscriptionStatus? Status { get; set; }

    public SubscriptionSortField SortBy { get; set; } = SubscriptionSortField.CreatedAt;

    public bool SortDescending { get; set; } = true;
}
