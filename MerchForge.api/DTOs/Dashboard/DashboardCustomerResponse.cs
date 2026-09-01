namespace MerchForge.api.DTOs.Dashboard;

public class DashboardCustomerResponse
{
    public Guid Id { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public int OrderCount { get; set; }

    /// <summary>Recorded total in this customer's highest-value currency (usually their only one) - never summed across currencies. Zero/null when they have no non-cancelled orders.</summary>
    public decimal TotalSpent { get; set; }

    public string? SpentCurrency { get; set; }

    public DateTime? LastOrderAt { get; set; }

    /// <summary>The (up to 2) businesses this customer has most recently ordered from - "Acme Coffee, Fresh Market, +2 more" style preview, not the full list (see the detail endpoint for that).</summary>
    public List<string> RecentBusinessNames { get; set; } = [];

    public int AdditionalBusinessCount { get; set; }

    public bool HasActiveSession { get; set; }

    public DateTime CreatedAt { get; set; }
}
