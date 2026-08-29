namespace MerchForge.api.Enums;

public enum FeatureCreditTransactionType
{
    Purchase,
    Consumption,

    /// <summary>
    /// The balance was set to a plan's per-period credit allotment (initial grant
    /// on subscribe, or a billing-period rollover) rather than topped up by a
    /// purchase — Amount is the delta versus the prior balance, which can be
    /// negative.
    /// </summary>
    Reset
}
