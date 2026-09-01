namespace MerchForge.api.DTOs.Dashboard;

/// <summary>
/// One customer's rank within a single currency - ranking mixes currencies together
/// no more than any other spend figure in this codebase does. OrderCount/TotalSpent
/// here are scoped to that one currency's orders, not the customer's global totals.
/// </summary>
public class TopCustomerResponse
{
    public Guid CustomerId { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public int OrderCount { get; set; }

    public decimal TotalSpent { get; set; }

    public string Currency { get; set; } = string.Empty;
}
