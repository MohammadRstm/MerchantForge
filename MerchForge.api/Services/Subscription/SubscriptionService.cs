
namespace MerchForge.api.Services.Subscription
{
    using MerchForge.api.Data;
    using MerchForge.api.Services.Subscription.interfaces;
    using MerchForge.api.Models;
    public class SubscriptionService : ISubscriptionService
    {
        private readonly MerchForgeDbContext _db;
        public SubscriptionService(MerchForgeDbContext db)
        {
            _db = db;
        }

        public async Task<Subscription> HasFeatureAsync(Guid businessId)
        {
            return await _db.Subscriptions
            .Include(s => s.SubscriptionPlan)
                .ThenInclude(p => p.PlanFeatures)
                    .ThenInclude(pf => pf.Feature)
            .Include(s => s.BusinessFeatureOverrides)
                .ThenInclude(o => o.Feature)
            .FirstOrDefaultAsync(
                s => s.BusinessId == businessId);
        }

    }
}
