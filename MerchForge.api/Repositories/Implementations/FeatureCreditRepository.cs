using MerchForge.api.Data;
using MerchForge.api.Enums;
using MerchForge.api.Models;
using MerchForge.api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MerchForge.api.Repositories.Implementations
{
    public class FeatureCreditRepository : IFeatureCreditRepository
    {
        private readonly MerchForgeDbContext _db;

        public FeatureCreditRepository(MerchForgeDbContext db)
        {
            _db = db;
        }

        public async Task<List<Feature>> GetPurchasableFeaturesAsync(
            CancellationToken cancellationToken = default)
        {
            return await _db.Features
                .AsNoTracking()
                .Where(f => f.IsActive && f.SupportsCreditPurchase)
                .Include(f => f.CreditPackages.Where(p => p.IsActive))
                .ToListAsync(cancellationToken);
        }

        public async Task<List<BusinessFeatureCredit>> GetBalancesForBusinessAsync(
            Guid businessId,
            CancellationToken cancellationToken = default)
        {
            return await _db.BusinessFeatureCredits
                .AsNoTracking()
                .Include(b => b.Feature)
                .Where(b => b.BusinessId == businessId)
                .ToListAsync(cancellationToken);
        }

        public async Task<FeatureCreditPackage?> GetPackageAsync(
            Guid packageId,
            CancellationToken cancellationToken = default)
        {
            return await _db.FeatureCreditPackages
                .AsNoTracking()
                .Include(p => p.Feature)
                .FirstOrDefaultAsync(p => p.Id == packageId, cancellationToken);
        }

        public async Task<bool> HasCreditsAsync(
            Guid businessId,
            string featureKey,
            CancellationToken cancellationToken = default)
        {
            return await _db.BusinessFeatureCredits
                .AsNoTracking()
                .AnyAsync(
                    b => b.BusinessId == businessId
                        && b.Feature.Key == featureKey
                        && b.Feature.IsActive
                        && b.CreditsRemaining > 0,
                    cancellationToken);
        }

        public async Task<BusinessFeatureCredit> GrantCreditsAsync(
            Guid businessId,
            Guid featureId,
            Guid packageId,
            int credits,
            CancellationToken cancellationToken = default)
        {
            var balance = await _db.BusinessFeatureCredits
                .FirstOrDefaultAsync(
                    b => b.BusinessId == businessId && b.FeatureId == featureId,
                    cancellationToken);

            if (balance is null)
            {
                balance = new BusinessFeatureCredit
                {
                    Id = Guid.NewGuid(),
                    BusinessId = businessId,
                    FeatureId = featureId,
                    CreditsRemaining = 0,
                    CreditsGrantedTotal = 0,
                };

                await _db.BusinessFeatureCredits.AddAsync(balance, cancellationToken);
            }

            balance.CreditsRemaining += credits;
            balance.CreditsGrantedTotal += credits;
            balance.UpdatedAt = DateTime.UtcNow;

            await _db.FeatureCreditTransactions.AddAsync(
                new FeatureCreditTransaction
                {
                    Id = Guid.NewGuid(),
                    BusinessFeatureCreditId = balance.Id,
                    Type = FeatureCreditTransactionType.Purchase,
                    Amount = credits,
                    BalanceAfter = balance.CreditsRemaining,
                    FeatureCreditPackageId = packageId,
                },
                cancellationToken);

            // One SaveChangesAsync call, so the balance update and its ledger entry
            // land in the same implicit transaction - a purchase either fully lands
            // or not at all.
            await _db.SaveChangesAsync(cancellationToken);

            return balance;
        }

        public async Task<bool> TryConsumeCreditAsync(
            Guid businessId,
            string featureKey,
            string? reference,
            CancellationToken cancellationToken = default)
        {
            // ExecuteUpdateAsync runs immediately against the database rather than
            // through the change tracker, so it and the ledger insert that follows
            // need an explicit transaction to stay atomic together - the same
            // requirement ProductDraftRepository's confirmation claim has, just
            // spanning two statements instead of one.
            await using var transaction =
                await _db.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var consumed = await _db.BusinessFeatureCredits
                    .Where(b =>
                        b.BusinessId == businessId &&
                        b.Feature.Key == featureKey &&
                        b.CreditsRemaining > 0)
                    .ExecuteUpdateAsync(
                        setters => setters
                            .SetProperty(b => b.CreditsRemaining, b => b.CreditsRemaining - 1)
                            .SetProperty(b => b.UpdatedAt, DateTime.UtcNow),
                        cancellationToken);

                if (consumed == 0)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return false;
                }

                var balance = await _db.BusinessFeatureCredits
                    .AsNoTracking()
                    .FirstAsync(
                        b => b.BusinessId == businessId && b.Feature.Key == featureKey,
                        cancellationToken);

                await _db.FeatureCreditTransactions.AddAsync(
                    new FeatureCreditTransaction
                    {
                        Id = Guid.NewGuid(),
                        BusinessFeatureCreditId = balance.Id,
                        Type = FeatureCreditTransactionType.Consumption,
                        Amount = -1,
                        BalanceAfter = balance.CreditsRemaining,
                        Reference = reference,
                    },
                    cancellationToken);

                await _db.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return true;
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }
    }
}
