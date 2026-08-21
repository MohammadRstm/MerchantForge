using System.Security.Claims;
using MerchForge.api.Authorization;
using MerchForge.api.DTOs.ImageEditing;
using MerchForge.api.Services.ImageEditing.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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

        [HttpPost]
        [RequestSizeLimit(26 * 1024 * 1024)]
        public async Task<ActionResult<ImageEditJobResponse>> Edit(
            Guid businessId,
            [FromForm] List<IFormFile> images,
            [FromForm] string prompt,
            CancellationToken cancellationToken)
        {
            var response = await _imageEditingService.EditAsync(
                businessId, CurrentUserId, images, prompt, cancellationToken);

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
