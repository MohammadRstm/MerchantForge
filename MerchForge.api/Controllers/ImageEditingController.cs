using System.Security.Claims;
using MerchForge.api.Authorization;
using MerchForge.api.DTOs.ImageEditing;
using MerchForge.api.Services.ImageEditing.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace MerchForge.api.Controllers
{
    /// <summary>
    /// AI-assisted product photo editing — the second independent AI feature,
    /// metered exactly like AI product creation (ProductDraftsController) but with
    /// no conversation: one request in, one edited image out.
    ///
    /// Holds no AI logic: it resolves the caller, delegates, and returns.
    /// </summary>
    [Route("api/businesses/{businessId:guid}/dashboard/image-edits")]
    [ApiController]
    [Authorize(Policy = AuthorizationPolicies.BusinessOwner)]
    [Authorize(Policy = AuthorizationPolicies.AiImageEditing)]
    [EnableRateLimiting("ai")]
    public class ImageEditingController : ControllerBase
    {
        private readonly IImageEditingService _imageEditingService;

        public ImageEditingController(IImageEditingService imageEditingService)
        {
            _imageEditingService = imageEditingService;
        }

        private Guid CurrentUserId =>
            Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
                ? id
                : Guid.Empty;

        /// <summary>
        /// imageUrls reference images already uploaded via the same product-image
        /// upload endpoint the manual form uses, and productId is the product they
        /// belong to, so the edited result is stored beside them - this never accepts raw file bytes
        /// for the images themselves, only which of the owner's own uploads to edit.
        /// The instruction is either prompt (typed) or audioPrompt (spoken); send
        /// exactly one.
        /// </summary>
        [HttpPost]
        [RequestSizeLimit(26 * 1024 * 1024)]
        public async Task<ActionResult<ImageEditJobResponse>> Edit(
            Guid businessId,
            [FromForm] Guid productId,
            [FromForm] List<string> imageUrls,
            [FromForm] string? prompt,
            IFormFile? audioPrompt,
            CancellationToken cancellationToken)
        {
            var response = await _imageEditingService.EditAsync(
                businessId, CurrentUserId, productId, imageUrls, prompt, audioPrompt, cancellationToken);

            return CreatedAtAction(nameof(Get), new { businessId, jobId = response.Id }, response);
        }

        [HttpGet("{jobId:guid}")]
        public async Task<ActionResult<ImageEditJobResponse>> Get(
            Guid businessId,
            Guid jobId,
            CancellationToken cancellationToken)
        {
            var response = await _imageEditingService.GetAsync(businessId, jobId, cancellationToken);

            return Ok(response);
        }
    }
}
