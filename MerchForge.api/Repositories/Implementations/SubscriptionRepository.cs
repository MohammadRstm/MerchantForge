namespace MerchForge.api.Repositories.Implementations
{
    using MerchForge.api.Data;
    using MerchForge.api.Enums;
    using MerchForge.api.Models;
    using MerchForge.api.Repositories.Interfaces;
    using Microsoft.EntityFrameworkCore;

    public class SubscriptionRepository : ISubscriptionRepository
    {
        private readonly MerchForgeDbContext _db;
        public SubscriptionRepository(MerchForgeDbContext db)
        {
            _db = db;
        }

        public async Task<Subscription?> GetSubscriptionWithPlanFeaturesAsync(Guid businessId)
        {
            return await _db.Subscriptions
                .Include(s => s.SubscriptionPlan)
                    .ThenInclude(p => p.PlanFeatures)
                        .ThenInclude(pf => pf.Feature)
                .FirstOrDefaultAsync(s =>
                    s.BusinessId == businessId &&
                    s.Status == SubscriptionStatus.Active);
        }

        public async Task<Subscription?> GetLatestSubscriptionWithPlanFeaturesAsync(
            Guid businessId,
            CancellationToken cancellationToken = default)
        {
            return await _db.Subscriptions
                .Include(s => s.SubscriptionPlan)
                    .ThenInclude(p => p.PlanFeatures)
                        .ThenInclude(pf => pf.Feature)
                .Where(s => s.BusinessId == businessId)
                .OrderByDescending(s => s.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<int?> GetPlanFeatureLimitAsync(
            Guid subscriptionPlanId,
            string featureKey,
            CancellationToken cancellationToken = default)
        {
            return await _db.PlanFeatures
                .Where(pf => pf.SubscriptionPlanId == subscriptionPlanId && pf.Feature.Key == featureKey)
                .Select(pf => (int?)pf.Limit)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<List<Subscription>> GetSubscriptionsDueForRenewalAsync(
            DateTime now,
            CancellationToken cancellationToken = default)
        {
            return await _db.Subscriptions
                .Include(s => s.SubscriptionPlan)
                    .ThenInclude(p => p.PlanFeatures)
                        .ThenInclude(pf => pf.Feature)
                .Where(s => s.Status == SubscriptionStatus.Active && s.CurrentPeriodEnd <= now)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<Subscription>> GetSubscriptionHistoryAsync(
            Guid businessId,
            CancellationToken cancellationToken = default)
        {
            return await _db.Subscriptions
                .Include(s => s.SubscriptionPlan)
                .Where(s => s.BusinessId == businessId)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<SubscriptionPlan?> GetPlanAsync(
            Guid subscriptionPlanId,
            CancellationToken cancellationToken = default)
        {
            return await _db.SubscriptionPlans
                .FirstOrDefaultAsync(p => p.Id == subscriptionPlanId, cancellationToken);
        }

        public async Task AddSubscriptionAsync(
            Subscription subscription,
            CancellationToken cancellationToken = default)
        {
            await _db.Subscriptions.AddAsync(subscription, cancellationToken);
        }

        public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}
