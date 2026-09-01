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

    public DateTime CreatedAt { get; set; }
}
