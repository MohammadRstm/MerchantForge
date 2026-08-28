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

        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
