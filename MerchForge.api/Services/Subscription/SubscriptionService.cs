
namespace MerchForge.api.Services.Subscription
{
    using MerchForge.api.Data;
    using MerchForge.api.Enums;
    using MerchForge.api.Models;
    using MerchForge.api.Repositories.Interfaces;
    using MerchForge.api.Services.Subscription.interfaces;
    using Microsoft.EntityFrameworkCore;

    public class SubscriptionService : ISubscriptionService
    {
        private readonly ISubscriptionRepository _subscriptionRepository;
        private readonly IFeatureCreditRepository _featureCreditRepository;

        public SubscriptionService(
            ISubscriptionRepository subscriptionRepository,
            IFeatureCreditRepository featureCreditRepository)
        {
            _subscriptionRepository = subscriptionRepository;
            _featureCreditRepository = featureCreditRepository;
        }

        public async Task<bool> HasFeatureAsync(Guid businessId, string featureKey)
        {
            if (await HasPlanFeatureAsync(businessId, featureKey))
            {
                return true;
            }

            // Not bundled in the plan - the other route in is an independent credit
            // purchase, which grants access exactly as long as a balance remains.
            return await _featureCreditRepository.HasCreditsAsync(businessId, featureKey);
        }

        public async Task<bool> HasPlanFeatureAsync(Guid businessId, string featureKey)
        {
            var subscription = await _subscriptionRepository
                .GetSubscriptionWithPlanFeaturesAsync(businessId);

            if (subscription == null)
            {
                return false;
            }

            // Limit == null is what "unlimited, bundled in the plan" actually means
            // (see this method's own doc comment) - a feature can be on the plan with
            // a numeric Limit (e.g. ai.image_editing's 40/150/400 credits/period),
            // in which case it's still metered against the credit ledger like any
            // other credit-based feature, just pre-funded by ResetImageEditingCreditsForPeriodAsync
            // instead of a manual purchase. Treating plan-membership alone as
            // "unlimited" here made every metered plan feature un-meterable.
            return subscription.SubscriptionPlan.PlanFeatures
                .Any(pf =>
                    pf.Feature.IsActive &&
                    pf.Feature.Key == featureKey &&
                    pf.Limit == null);
        }

    }
}
