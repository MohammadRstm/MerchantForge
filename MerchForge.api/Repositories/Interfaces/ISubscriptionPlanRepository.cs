using MerchForge.api.DTOs.Common;
using MerchForge.api.Models;

namespace MerchForge.api.Repositories.Interfaces
{
    /// <summary>SuperAdmin CRUD over SubscriptionPlan/PlanFeature/Feature — the plan catalogue itself, not a business's own subscription (see ISubscriptionRepository for that).</summary>
    public interface ISubscriptionPlanRepository
    {
        Task<List<SubscriptionPlan>> GetAllAsync(CancellationToken cancellationToken = default);

        Task<List<SubscriptionPlan>> GetActiveAsync(CancellationToken cancellationToken = default);

        Task<SubscriptionPlan?> GetByIdWithFeaturesAsync(Guid id, CancellationToken cancellationToken = default);

        Task<int> CountActiveSubscribersAsync(Guid subscriptionPlanId, CancellationToken cancellationToken = default);

        /// <summary>Active-subscriber counts for every plan row, in one grouped query rather than N calls to CountActiveSubscribersAsync.</summary>
        Task<Dictionary<Guid, int>> GetActiveSubscriberCountsByPlanIdAsync(CancellationToken cancellationToken = default);

        /// <summary>Active-subscriber counts grouped by plan tier Name (Monthly + Yearly rows combined) — backs the Subscription Distribution chart.</summary>
        Task<List<KeyCountResponse>> GetActiveSubscriberCountsByPlanNameAsync(CancellationToken cancellationToken = default);

        /// <summary>Active subscription counts grouped by billing interval — backs the platform KPIs and the Billing Period distribution.</summary>
        Task<Dictionary<Enums.BillingInterval, int>> GetActiveSubscriptionCountsByBillingIntervalAsync(CancellationToken cancellationToken = default);

        Task<List<Feature>> GetAllFeaturesAsync(CancellationToken cancellationToken = default);

        Task<List<Guid>> GetExistingFeatureIdsAsync(List<Guid> featureIds, CancellationToken cancellationToken = default);

        Task<SubscriptionPlan> CreateAsync(SubscriptionPlan plan, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates the plan's own fields and replaces its PlanFeature rows wholesale
        /// from `features` — matches this codebase's established "always a full
        /// snapshot" convention for a request that already sends the complete list
        /// each time.
        /// </summary>
        Task<SubscriptionPlan?> UpdateAsync(
            Guid id,
            string name,
            string? description,
            decimal price,
            string currency,
            Enums.BillingInterval billingInterval,
            bool isActive,
            List<PlanFeature> features,
            CancellationToken cancellationToken = default);

        Task<SubscriptionPlan?> SetActiveAsync(
            Guid id,
            bool isActive,
            CancellationToken cancellationToken = default);
    }
}
