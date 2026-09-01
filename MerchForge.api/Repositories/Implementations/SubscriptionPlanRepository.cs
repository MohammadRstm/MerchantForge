using MerchForge.api.Data;
using MerchForge.api.DTOs.Common;
using MerchForge.api.Enums;
using MerchForge.api.Models;
using MerchForge.api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MerchForge.api.Repositories.Implementations
{
    public class SubscriptionPlanRepository : ISubscriptionPlanRepository
    {
        private readonly MerchForgeDbContext _db;

        public SubscriptionPlanRepository(MerchForgeDbContext db)
        {
            _db = db;
        }

        public async Task<List<SubscriptionPlan>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _db.SubscriptionPlans
                .AsNoTracking()
                .Include(p => p.PlanFeatures)
                    .ThenInclude(pf => pf.Feature)
                .OrderBy(p => p.Price)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<SubscriptionPlan>> GetActiveAsync(CancellationToken cancellationToken = default)
        {
            return await _db.SubscriptionPlans
                .AsNoTracking()
                .Include(p => p.PlanFeatures)
                    .ThenInclude(pf => pf.Feature)
                .Where(p => p.IsActive)
                .OrderBy(p => p.Price)
                .ToListAsync(cancellationToken);
        }

        public async Task<SubscriptionPlan?> GetByIdWithFeaturesAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _db.SubscriptionPlans
                .AsNoTracking()
                .Include(p => p.PlanFeatures)
                    .ThenInclude(pf => pf.Feature)
                .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        }

        public async Task<int> CountActiveSubscribersAsync(Guid subscriptionPlanId, CancellationToken cancellationToken = default)
        {
            return await _db.Subscriptions
                .CountAsync(s => s.SubscriptionPlanId == subscriptionPlanId && s.Status == SubscriptionStatus.Active, cancellationToken);
        }

        public async Task<Dictionary<Guid, int>> GetActiveSubscriberCountsByPlanIdAsync(CancellationToken cancellationToken = default)
        {
            var grouped = await _db.Subscriptions
                .Where(s => s.Status == SubscriptionStatus.Active)
                .GroupBy(s => s.SubscriptionPlanId)
                .Select(g => new { PlanId = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);

            return grouped.ToDictionary(x => x.PlanId, x => x.Count);
        }

        public async Task<List<KeyCountResponse>> GetActiveSubscriberCountsByPlanNameAsync(CancellationToken cancellationToken = default)
        {
            var grouped = await _db.Subscriptions
                .Where(s => s.Status == SubscriptionStatus.Active)
                .GroupBy(s => s.SubscriptionPlan.Name)
                .Select(g => new { Name = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);

            return grouped
                .Select(x => new KeyCountResponse { Key = x.Name, Count = x.Count })
                .ToList();
        }

        public async Task<Dictionary<BillingInterval, int>> GetActiveSubscriptionCountsByBillingIntervalAsync(CancellationToken cancellationToken = default)
        {
            // Grouped by the raw enum column, not .ToString() - the latter can't be
            // translated to SQL by this provider (same reasoning as
            // GetSubscriptionStatusCountsAsync elsewhere in DashboardRepository).
            var grouped = await _db.Subscriptions
                .Where(s => s.Status == SubscriptionStatus.Active)
                .GroupBy(s => s.SubscriptionPlan.BillingInterval)
                .Select(g => new { Interval = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);

            return grouped.ToDictionary(x => x.Interval, x => x.Count);
        }

        public async Task<List<Feature>> GetAllFeaturesAsync(CancellationToken cancellationToken = default)
        {
            return await _db.Features
                .AsNoTracking()
                .OrderBy(f => f.Name)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<Guid>> GetExistingFeatureIdsAsync(List<Guid> featureIds, CancellationToken cancellationToken = default)
        {
            return await _db.Features
                .Where(f => featureIds.Contains(f.Id))
                .Select(f => f.Id)
                .ToListAsync(cancellationToken);
        }

        public async Task<SubscriptionPlan> CreateAsync(SubscriptionPlan plan, CancellationToken cancellationToken = default)
        {
            await _db.SubscriptionPlans.AddAsync(plan, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);

            return plan;
        }

        public async Task<SubscriptionPlan?> UpdateAsync(
            Guid id,
            string name,
            string? description,
            decimal price,
            string currency,
            BillingInterval billingInterval,
            bool isActive,
            List<PlanFeature> features,
            CancellationToken cancellationToken = default)
        {
            var plan = await _db.SubscriptionPlans
                .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

            if (plan is null)
            {
                return null;
            }

            plan.Name = name;
            plan.Description = description;
            plan.Price = price;
            plan.Currency = currency;
            plan.BillingInterval = billingInterval;
            plan.IsActive = isActive;
            plan.UpdatedAt = DateTime.UtcNow;

            // Full replace, same "the request always sends the complete snapshot"
            // convention used elsewhere in this codebase (e.g. website customization
            // drafts) - simplest correct approach given CreateSubscriptionPlanRequest/
            // UpdateSubscriptionPlanRequest already send the whole feature list.
            await _db.PlanFeatures
                .Where(pf => pf.SubscriptionPlanId == id)
                .ExecuteDeleteAsync(cancellationToken);

            foreach (var feature in features)
            {
                feature.SubscriptionPlanId = id;
            }

            await _db.PlanFeatures.AddRangeAsync(features, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);

            return plan;
        }

        public async Task<SubscriptionPlan?> SetActiveAsync(
            Guid id,
            bool isActive,
            CancellationToken cancellationToken = default)
        {
            var plan = await _db.SubscriptionPlans
                .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

            if (plan is null)
            {
                return null;
            }

            plan.IsActive = isActive;
            plan.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(cancellationToken);

            return plan;
        }
    }
}
