
namespace MerchForge.api.Services.Subscription
{
    using MerchForge.api.Data;
    using MerchForge.api.Enums;
    using MerchForge.api.Models;
    using MerchForge.api.Services.Subscription.interfaces;
    using Microsoft.EntityFrameworkCore;

    public class SubscriptionService : ISubscriptionService
    {
        private readonly MerchForgeDbContext _db;
        public SubscriptionService(MerchForgeDbContext db)
        {
            _db = db;
        }

        public async Task<bool> HasFeatureAsync(Guid businessId,string featureKey)
        {
            var subscription = await _db.Subscriptions
                .Include(s => s.SubscriptionPlan)
                    .ThenInclude(p => p.PlanFeatures)
                        .ThenInclude(pf => pf.Feature)
                .FirstOrDefaultAsync(s =>
                    s.BusinessId == businessId &&
                    s.Status == SubscriptionStatus.Active);

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
