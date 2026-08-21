using MerchForge.api.Enums;

namespace MerchForge.api.Models;

/// <summary>
/// One entry in a business's credit history for a feature: a purchase (positive
/// Amount, FeatureCreditPackageId set) or a consumption (Amount -1, Reference set to
/// whatever was being paid for — e.g. a product draft id). Credit balances are
/// money-adjacent state, so every change is recorded here rather than only ever
/// mutating BusinessFeatureCredit.CreditsRemaining in place.
/// </summary>
public class FeatureCreditTransaction
{
    public Guid Id { get; set; }

    public Guid BusinessFeatureCreditId { get; set; }

    public FeatureCreditTransactionType Type { get; set; }

    public int Amount { get; set; }

    public int BalanceAfter { get; set; }

    public Guid? FeatureCreditPackageId { get; set; }

    public string? Reference { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public BusinessFeatureCredit BusinessFeatureCredit { get; set; } = null!;
}
