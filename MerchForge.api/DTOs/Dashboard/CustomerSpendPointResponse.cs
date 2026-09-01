namespace MerchForge.api.DTOs.Dashboard;

/// <summary>One month's recorded spend for one customer, in one currency - kept separate per currency for the same reason every other spend figure in this codebase is.</summary>
public class CustomerSpendPointResponse
{
    public string Period { get; set; } = string.Empty;

    public decimal Total { get; set; }

    public string Currency { get; set; } = string.Empty;
}
