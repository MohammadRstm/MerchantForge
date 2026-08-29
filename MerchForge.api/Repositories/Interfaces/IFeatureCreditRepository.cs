using MerchForge.api.Models;

namespace MerchForge.api.Repositories.Interfaces
{
    /// <summary>
    /// Persistence for features bought independently of a subscription plan: the
    /// purchasable catalogue, a business's running balance per feature, and the
    /// ledger behind it.
    /// </summary>
    public interface IFeatureCreditRepository
    {
        /// <summary>Active features flagged SupportsCreditPurchase, with their active packages.</summary>
        Task<List<Feature>> GetPurchasableFeaturesAsync(
            CancellationToken cancellationToken = default);

        /// <summary>Every credit balance this business holds, across all features it has bought into.</summary>
        Task<List<BusinessFeatureCredit>> GetBalancesForBusinessAsync(
            Guid businessId,
            CancellationToken cancellationToken = default);

        /// <summary>Includes the owning Feature, so callers can validate it's still purchasable.</summary>
        Task<FeatureCreditPackage?> GetPackageAsync(
            Guid packageId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Read-only: does this business have a positive credit balance for this
        /// feature. Deliberately side-effect-free, since this is what the
        /// authorization policy pipeline calls on every request.
        /// </summary>
        Task<bool> HasCreditsAsync(
            Guid businessId,
            string featureKey,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Records a purchase: creates the business's balance row for this feature if
        /// this is its first purchase, otherwise tops up the existing one, and appends
        /// a Purchase ledger entry. Atomic - a purchase either fully lands or not at all.
        /// </summary>
        Task<BusinessFeatureCredit> GrantCreditsAsync(
            Guid businessId,
            Guid featureId,
            Guid packageId,
            int credits,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Atomically spends one credit and appends a Consumption ledger entry.
        /// Returns false without changing anything if the business has no balance row
        /// for this feature, or it's already at zero - conditioned in the same
        /// statement that decrements, so two concurrent calls can't both succeed past
        /// the last credit.
        /// </summary>
        Task<bool> TryConsumeCreditAsync(
            Guid businessId,
            string featureKey,
            string? reference,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Sets a business's balance for a feature to exactly `limit` (creating the
        /// balance row if this is the business's first grant for it) and appends a
        /// Reset ledger entry — used for a plan's per-billing-period credit
        /// allotment, which resets rather than accumulates. Unlike GrantCreditsAsync,
        /// this is not a top-up: the new balance replaces the old one.
        /// </summary>
        Task<BusinessFeatureCredit> ResetToLimitAsync(
            Guid businessId,
            string featureKey,
            int limit,
            CancellationToken cancellationToken = default);
    }
}
