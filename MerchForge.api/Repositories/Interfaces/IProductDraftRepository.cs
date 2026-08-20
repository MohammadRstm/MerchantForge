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

        /// <summary>
        /// Atomically moves a draft to Completed, but only from a state that still
        /// allows confirmation. Returns whether this caller won.
        ///
        /// A read-then-check-then-write cannot do this: two confirmations arriving
        /// together - a double click, or a retried request - would both see a
        /// non-terminal status and both create a product. Same reasoning, and same
        /// ExecuteUpdate approach, as claiming an invitation during registration.
        /// </summary>
        Task<bool> TryClaimForConfirmationAsync(
            Guid businessId,
            Guid draftId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Puts a claimed draft back for another attempt, used when product creation
        /// fails after the claim. Without it a validation failure at the last step
        /// would leave the draft Completed with no product and no way forward.
        /// </summary>
        Task ReleaseConfirmationClaimAsync(
            Guid draftId,
            CancellationToken cancellationToken = default);
    }
}
