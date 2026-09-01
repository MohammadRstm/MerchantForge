namespace MerchForge.api.DTOs.Dashboard;

/// <summary>Recorded spend for one currency, plus how many distinct customers spent in it — never collapsed across currencies, same reasoning as CurrencyTotalResponse.</summary>
public class CustomerCurrencyTotalResponse
{
    public string Currency { get; set; } = string.Empty;

    public decimal TotalSpent { get; set; }

    public int CustomerCount { get; set; }
}
