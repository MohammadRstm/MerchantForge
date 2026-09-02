namespace MerchForge.api.DTOs.Dashboard;

public class DashboardBusinessResponse
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string OwnerFullName { get; set; } = string.Empty;

    public string OwnerEmail { get; set; } = string.Empty;

    public string? DomainName { get; set; }

    public int MemberCount { get; set; }

    public int ProductCount { get; set; }

    /// <summary>All-time, excludes Cancelled orders.</summary>
    public int OrderCount { get; set; }

    /// <summary>All-time recorded order total, excludes Cancelled orders — this business's own currency, not a platform-wide sum.</summary>
    public decimal RecordedRevenue { get; set; }

    public string RevenueCurrency { get; set; } = string.Empty;

    /// <summary>Null when the business has never had an order.</summary>
    public DateTime? LastOrderAt { get; set; }

    /// <summary>Null when the business has no subscription of any kind.</summary>
    public string? PlanName { get; set; }

    public string? BillingInterval { get; set; }

    /// <summary>
    /// The business's subscription status (Active/Trialing/PastDue/Cancelled/Expired),
    /// not a business-suspension state — no such concept exists on Business itself.
    /// Null when the business has no subscription.
    /// </summary>
    public string? SubscriptionStatus { get; set; }

    public DateTime CreatedAt { get; set; }

    /// <summary>True for a SuperAdmin-created showcase business — see Business.IsDemo's own doc comment.</summary>
    public bool IsDemo { get; set; }
}
