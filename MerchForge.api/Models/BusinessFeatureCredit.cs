namespace MerchForge.api.Models;

/// <summary>
/// A business's running credit balance for one feature bought independently of a
/// subscription plan. One row per (Business, Feature) — created on first purchase,
/// topped up on every purchase after that.
/// </summary>
public class BusinessFeatureCredit
{
    public Guid Id { get; set; }

    public Guid BusinessId { get; set; }

    public Guid FeatureId { get; set; }

    public int CreditsRemaining { get; set; }

    /// <summary>Lifetime credits ever granted, for display — CreditsRemaining alone can't show how much was used.</summary>
    public int CreditsGrantedTotal { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Business Business { get; set; } = null!;

    public Feature Feature { get; set; } = null!;

    public ICollection<FeatureCreditTransaction> Transactions { get; set; }
        = new List<FeatureCreditTransaction>();
}
