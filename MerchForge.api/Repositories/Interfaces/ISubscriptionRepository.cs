using MerchForge.api.Models;

namespace MerchForge.api.Repositories.Interfaces
{
    public interface ISubscriptionRepository
    {
        Task<Subscription?> GetSubscriptionWithPlanFeaturesAsync(Guid businessId);

        Task<Subscription?> GetLatestSubscriptionWithPlanFeaturesAsync(Guid businessId, CancellationToken cancellationToken = default);

        /// <summary>
        /// A plan's per-billing-period credit allotment for one feature, or null if
        /// the plan doesn't grant the feature at all, or grants it unlimited
        /// (Limit is only ever meaningful for metered features like
        /// ai.image_editing - every other PlanFeature row leaves it null).
        /// </summary>
        Task<int?> GetPlanFeatureLimitAsync(
            Guid subscriptionPlanId,
            string featureKey,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Active subscriptions whose current billing period has already ended -
        /// tracked entities, ready for the renewal job to roll their period forward
        /// and save. Includes SubscriptionPlan.PlanFeatures so the job can resolve
        /// each plan's credit limits without a second query per subscription.
        /// </summary>
        Task<List<Subscription>> GetSubscriptionsDueForRenewalAsync(
            DateTime now,
            CancellationToken cancellationToken = default);

        /// <summary>Every Subscription row the business has ever had, newest first — the prior row on a plan switch is marked Cancelled rather than deleted, so this is a real (if coarse) change history with no new persistence needed.</summary>
        Task<List<Subscription>> GetSubscriptionHistoryAsync(
            Guid businessId,
            CancellationToken cancellationToken = default);

        Task<SubscriptionPlan?> GetPlanAsync(
            Guid subscriptionPlanId,
            CancellationToken cancellationToken = default);

        /// <summary>Tracked-add only — caller commits via SaveChangesAsync, so this can land in the same transaction as cancelling a prior Active subscription.</summary>
        Task AddSubscriptionAsync(
            Subscription subscription,
            CancellationToken cancellationToken = default);

        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
