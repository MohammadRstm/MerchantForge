using MerchForge.api.Models;

namespace MerchForge.api.Repositories.Interfaces
{
    /// <summary>
    /// Draft persistence. Every lookup takes businessId alongside the draft id: a
    /// draft is only ever reachable through the business that owns it, so there is no
    /// method that can accidentally return another business's conversation.
    /// </summary>
    public interface IProductDraftRepository
    {
        Task<ProductDraft> CreateAsync(
            ProductDraft draft,
            CancellationToken cancellationToken = default);

        /// <summary>Tracked, for mutation. Null when the draft doesn't exist or belongs elsewhere.</summary>
        Task<ProductDraft?> GetForBusinessAsync(
            Guid businessId,
            Guid draftId,
            CancellationToken cancellationToken = default);

        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
