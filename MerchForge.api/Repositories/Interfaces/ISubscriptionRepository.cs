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

        /// <summary>
        /// Cancels this business's current Active subscription (if any) and inserts
        /// the new one in a single transaction, running onSubscriptionReplaced (the
        /// initial credit grant) before committing — so a failure at any point
        /// leaves neither a dangling cancellation nor an ungranted credit reset.
        /// Callers are responsible for the idempotency check (is the business
        /// already on this exact plan?) before calling this, same as
        /// BusinessDashboardService.SubscribeToPlanAsync does.
        /// </summary>
        Task ReplaceActiveSubscriptionAsync(
            Guid businessId,
            Subscription newSubscription,
            Func<CancellationToken, Task> onSubscriptionReplaced,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Atomically rolls a subscription's billing period forward, but only if its
        /// CurrentPeriodEnd still matches expectedCurrentPeriodEnd - the same
        /// claim-based pattern FeatureCreditRepository.TryConsumeCreditAsync uses.
        /// Returns false (a safe no-op) when a prior attempt already advanced it,
        /// which is what makes RenewSubscriptionPeriodsJob safe to retry.
        /// </summary>
        Task<bool> TryAdvanceSubscriptionPeriodAsync(
            Guid subscriptionId,
            DateTime expectedCurrentPeriodEnd,
            DateTime newPeriodStart,
            DateTime newPeriodEnd,
            DateTime now,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Atomically marks a subscription Cancelled, but only if it's still Active -
        /// same reasoning as TryAdvanceSubscriptionPeriodAsync, for the
        /// cancel-at-period-end branch of the renewal job.
        /// </summary>
        Task<bool> TryEndSubscriptionAsync(
            Guid subscriptionId,
            DateTime now,
            CancellationToken cancellationToken = default);
    }
}
