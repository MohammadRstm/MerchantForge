using MerchForge.api.DTOs.BusinessDashboard;
using MerchForge.api.DTOs.ProductAi;

namespace MerchForge.api.Services.ProductAi.Interfaces
{
    /// <summary>
    /// Orchestrates the AI product-creation workflow: draft lifecycle, state
    /// transitions, validation, and turning a confirmed draft into a product.
    ///
    /// Carries no provider types — it talks to the AI through
    /// IProductAiConversationClient — so the workflow can be read and tested without
    /// knowing which provider is configured.
    /// </summary>
    public interface IProductAiService
    {
        Task<ProductDraftResponse> StartAsync(
            Guid businessId,
            Guid userId,
            CancellationToken cancellationToken = default);

        Task<ProductDraftResponse> GetAsync(
            Guid businessId,
            Guid draftId,
            CancellationToken cancellationToken = default);

        /// <summary>Transcribes the audio, then continues the conversation with the text.</summary>
        Task<ProductDraftResponse> SendVoiceMessageAsync(
            Guid businessId,
            Guid userId,
            Guid draftId,
            IFormFile audio,
            CancellationToken cancellationToken = default);

        /// <summary>Attaches an image and lets the agent react to now having one.</summary>
        Task<ProductDraftResponse> AttachImageAsync(
            Guid businessId,
            Guid userId,
            Guid draftId,
            IFormFile image,
            CancellationToken cancellationToken = default);

        Task<ProductDraftResponse> ResolveImageModificationAsync(
            Guid businessId,
            Guid userId,
            Guid draftId,
            bool approved,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates the product from the draft. The only path that writes to the
        /// products table, and only reachable by explicit owner action.
        /// </summary>
        Task<BusinessProductDetailResponse> ConfirmAsync(
            Guid businessId,
            Guid userId,
            Guid draftId,
            CancellationToken cancellationToken = default);

        Task<ProductDraftResponse> CancelAsync(
            Guid businessId,
            Guid userId,
            Guid draftId,
            CancellationToken cancellationToken = default);
    }
}
