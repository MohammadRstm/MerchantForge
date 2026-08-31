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

        public async Task ReplaceActiveSubscriptionAsync(
            Guid businessId,
            Subscription newSubscription,
            Func<CancellationToken, Task> onSubscriptionReplaced,
            CancellationToken cancellationToken = default)
        {
            await using var transaction =
                await _db.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var currentActive = await _db.Subscriptions
                    .FirstOrDefaultAsync(
                        s => s.BusinessId == businessId && s.Status == SubscriptionStatus.Active,
                        cancellationToken);

                if (currentActive is not null)
                {
                    currentActive.Status = SubscriptionStatus.Cancelled;
                    currentActive.UpdatedAt = DateTime.UtcNow;
                }

                await _db.Subscriptions.AddAsync(newSubscription, cancellationToken);
                await _db.SaveChangesAsync(cancellationToken);

                // Runs inside the same transaction/DbContext, so its own
                // SaveChangesAsync call (FeatureCreditRepository.ResetToLimitAsync)
                // participates in this commit rather than landing separately.
                await onSubscriptionReplaced(cancellationToken);

                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        public async Task<bool> TryAdvanceSubscriptionPeriodAsync(
            Guid subscriptionId,
            DateTime expectedCurrentPeriodEnd,
            DateTime newPeriodStart,
            DateTime newPeriodEnd,
            DateTime now,
            CancellationToken cancellationToken = default)
        {
            var advanced = await _db.Subscriptions
                .Where(s =>
                    s.Id == subscriptionId &&
                    s.Status == SubscriptionStatus.Active &&
                    s.CurrentPeriodEnd == expectedCurrentPeriodEnd)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(s => s.CurrentPeriodStart, newPeriodStart)
                        .SetProperty(s => s.CurrentPeriodEnd, newPeriodEnd)
                        .SetProperty(s => s.UpdatedAt, now),
                    cancellationToken);

            return advanced == 1;
        }

        public async Task<bool> TryEndSubscriptionAsync(
            Guid subscriptionId,
            DateTime now,
            CancellationToken cancellationToken = default)
        {
            var ended = await _db.Subscriptions
                .Where(s => s.Id == subscriptionId && s.Status == SubscriptionStatus.Active)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(s => s.Status, SubscriptionStatus.Cancelled)
                        .SetProperty(s => s.UpdatedAt, now),
                    cancellationToken);

            return ended == 1;
        }
    }
}
