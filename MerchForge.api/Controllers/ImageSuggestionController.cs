using System.Security.Claims;
using MerchForge.api.Authorization;
using MerchForge.api.DTOs.ImageSuggestion;
using MerchForge.api.DTOs.ProductAi;
using MerchForge.api.Services.ImageSuggestion.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace MerchForge.api.Controllers
{
    /// <summary>
    /// AI-assisted "suggest product details from this photo" — the third
    /// independent AI image feature, metered exactly like AI image editing (same
    /// credit pool) but with no conversation and no persisted draft: one photo in,
    /// one best-effort field draft out.
    ///
    /// Holds no AI logic: it resolves the caller, delegates, and returns.
    /// </summary>
    [Route("api/businesses/{businessId:guid}/dashboard/image-suggestions")]
    [ApiController]
    [Authorize(Policy = AuthorizationPolicies.BusinessOwner)]
    [Authorize(Policy = AuthorizationPolicies.AiImageEditing)]
    [EnableRateLimiting("ai")]
    public class ImageSuggestionController : ControllerBase
    {
        private readonly IImageSuggestionService _imageSuggestionService;

        public ImageSuggestionController(IImageSuggestionService imageSuggestionService)
        {
            _imageSuggestionService = imageSuggestionService;
        }

        private Guid CurrentUserId =>
            Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
                ? id
                : Guid.Empty;

        /// <summary>
        /// imageUrl references an image already uploaded via the same product-image
        /// upload endpoint the manual form uses.
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<ProductDraftProductResponse>> Suggest(
            Guid businessId,
            [FromBody] SuggestFromImageRequest request,
            CancellationToken cancellationToken)
        {
            var response = await _imageSuggestionService.SuggestAsync(
                businessId, CurrentUserId, request.ImageUrl, cancellationToken);

            return Ok(response);
        }
    }
}
