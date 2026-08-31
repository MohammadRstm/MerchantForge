namespace MerchForge.api.DTOs.Dashboard;

/// <summary>
/// A recorded-order total for one currency. Platform-wide revenue is grouped by
/// currency rather than summed into one number, since Order.Currency is a per-order
/// snapshot of Business.Currency and different businesses can use different
/// currencies — summing across them would produce a meaningless figure.
/// </summary>
public class CurrencyTotalResponse
{
    public string Currency { get; set; } = string.Empty;

    /// <summary>Sum of Order.Total, excluding Cancelled orders — recorded order totals, not money actually collected (no payment gateway exists).</summary>
    public decimal Total { get; set; }

    public int OrderCount { get; set; }
}
