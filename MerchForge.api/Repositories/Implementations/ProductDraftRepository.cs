using MerchForge.api.Data;
using MerchForge.api.Enums;
using MerchForge.api.Models;
using MerchForge.api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MerchForge.api.Repositories.Implementations
{
    public class ProductDraftRepository : IProductDraftRepository
    {
        private readonly MerchForgeDbContext _db;

        public ProductDraftRepository(MerchForgeDbContext db)
        {
            _db = db;
        }

        public async Task<ProductDraft> CreateAsync(
            ProductDraft draft,
            CancellationToken cancellationToken = default)
        {
            await _db.ProductDrafts.AddAsync(draft, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);

            return draft;
        }

        public async Task<ProductDraft?> GetForBusinessAsync(
            Guid businessId,
            Guid draftId,
            CancellationToken cancellationToken = default)
        {
            // Both predicates together: matching on draftId alone would let one
            // business drive another's conversation.
            return await _db.ProductDrafts
                .FirstOrDefaultAsync(
                    d => d.Id == draftId && d.BusinessId == businessId,
                    cancellationToken);
        }

        public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task<bool> TryClaimForConfirmationAsync(
            Guid businessId,
            Guid draftId,
            CancellationToken cancellationToken = default)
        {
            // One statement, so the database decides the winner. The status condition
            // is part of the UPDATE rather than a preceding SELECT, which is what
            // makes concurrent confirmations mutually exclusive.
            var claimed = await _db.ProductDrafts
                .Where(d =>
                    d.Id == draftId &&
                    d.BusinessId == businessId &&
                    d.Status != ProductDraftStatus.Completed &&
                    d.Status != ProductDraftStatus.Cancelled &&
                    d.Status != ProductDraftStatus.Failed)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(d => d.Status, ProductDraftStatus.Completed)
                        .SetProperty(d => d.UpdatedAt, DateTime.UtcNow),
                    cancellationToken);

            return claimed > 0;
        }

        public async Task ReleaseConfirmationClaimAsync(
            Guid draftId,
            CancellationToken cancellationToken = default)
        {
            // Only releases a claim that never produced a product, so a genuinely
            // completed draft is never reopened.
            await _db.ProductDrafts
                .Where(d =>
                    d.Id == draftId &&
                    d.Status == ProductDraftStatus.Completed &&
                    d.ProductId == null)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(d => d.Status, ProductDraftStatus.WaitingForProductApproval)
                        .SetProperty(d => d.UpdatedAt, DateTime.UtcNow),
                    cancellationToken);
        }
    }
}
