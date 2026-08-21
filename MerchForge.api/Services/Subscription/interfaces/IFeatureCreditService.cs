using MerchForge.api.DTOs.Subscriptions;

namespace MerchForge.api.Services.Subscription.interfaces;

/// <summary>
/// Buying and spending credits for a feature bought independently of a subscription
/// plan.
/// </summary>
public interface IFeatureCreditService
{
    /// <summary>Every purchasable feature, its packages, and this business's balance for each.</summary>
    Task<List<FeatureCreditOverviewResponse>> GetOverviewAsync(
        Guid businessId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Stub payment: always succeeds. Real charging goes here later - this is the
    /// seam for it, matching how Subscription already carries an unused
    /// Provider/ExternalSubscriptionId pair for the same reason.
    /// </summary>
    Task<BusinessFeatureCreditResponse> PurchaseAsync(
        Guid businessId,
        Guid packageId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Spends one credit for one use of featureKey. A no-op success when the
    /// business's plan already bundles the feature - that usage is unlimited, not
    /// metered. Returns false only when the feature is credit-based and the balance
    /// is exhausted.
    /// </summary>
    Task<bool> TryConsumeAsync(
        Guid businessId,
        string featureKey,
        string? reference,
        CancellationToken cancellationToken = default);
}
