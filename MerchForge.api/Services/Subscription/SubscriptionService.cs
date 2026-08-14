
namespace MerchForge.api.Services.Subscription
{
    using MerchForge.api.Data;
    using MerchForge.api.Enums;
    using MerchForge.api.Models;
    using MerchForge.api.Repositories.Implementations;
    using MerchForge.api.Services.Subscription.interfaces;
    using Microsoft.EntityFrameworkCore;

    public class SubscriptionService : ISubscriptionService
    {
        private readonly SubscriptionRepository _subscriptionRepository;
        public SubscriptionService(SubscriptionRepository subscriptionRepository)
        {
            _subscriptionRepository = subscriptionRepository;
        }

        public async Task<bool> HasFeatureAsync(Guid businessId,string featureKey)
        {
            var subscription = await _subscriptionRepository
                .GetSubscriptionWithPlanFeaturesAsync(businessId);

            if (subscription == null)
            {
                return false;
            }

            return subscription.SubscriptionPlan.PlanFeatures
                .Any(pf =>
                    pf.Feature.IsActive &&
                    pf.Feature.Key == featureKey);
        }

    }
}
