using System.Security.Claims;
using MerchForge.api.Authorization;
using MerchForge.api.DTOs.BusinessDashboard;
using MerchForge.api.DTOs.ProductAi;
using MerchForge.api.Services.ProductAi.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MerchForge.api.Controllers
{
    /// <summary>
    /// AI-assisted product creation.
    ///
    /// Routed under api/businesses/{businessId}/... deliberately: the BusinessOwner
    /// policy resolves businessId from the route and checks the authenticated user's
    /// membership, so a route without it would leave these endpoints effectively
    /// unauthorized. businessId therefore is not "trusted input from the client" — a
    /// request naming someone else's business fails authorization before reaching
    /// here, and every draft lookup is scoped to it again in the repository.
    ///
    /// Holds no AI logic: it resolves the caller, delegates, and returns.
    /// </summary>
    [Route("api/businesses/{businessId:guid}/dashboard/product-drafts")]
    [ApiController]
    [Authorize(Policy = AuthorizationPolicies.BusinessOwner)]
    [Authorize(Policy = AuthorizationPolicies.AiProductGeneration)]
    public class ProductDraftsController : ControllerBase
    {
        private readonly IProductAiService _productAiService;

        public ProductDraftsController(
            IProductAiService productAiService)
        {
            _productAiService = productAiService;
        }

        /// <summary>
        /// The authenticated user, for audit and logging. Read from the token rather
        /// than the request body so it cannot be spoofed.
        /// </summary>
        private Guid CurrentUserId =>
            Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
                ? id
                : Guid.Empty;

        [HttpPost]
        public async Task<ActionResult<ProductDraftResponse>> Start(
            Guid businessId,
            CancellationToken cancellationToken)
        {
            var response = await _productAiService.StartAsync(businessId, CurrentUserId, cancellationToken);

            return CreatedAtAction(nameof(Get), new { businessId, draftId = response.Id }, response);
        }

        /// <summary>Resumes a conversation — the reason drafts are persisted.</summary>
        [HttpGet("{draftId:guid}")]
        public async Task<ActionResult<ProductDraftResponse>> Get(
            Guid businessId,
            Guid draftId,
            CancellationToken cancellationToken)
        {
            var response = await _productAiService.GetAsync(businessId, draftId, cancellationToken);

            return Ok(response);
        }

        [HttpPost("{draftId:guid}/voice")]
        [RequestSizeLimit(26 * 1024 * 1024)]
        public async Task<ActionResult<ProductDraftResponse>> SendVoiceMessage(
            Guid businessId,
            Guid draftId,
            IFormFile file,
            CancellationToken cancellationToken)
        {
            var response = await _productAiService.SendVoiceMessageAsync(
                businessId, CurrentUserId, draftId, file, cancellationToken);

            return Ok(response);
        }

        [HttpPost("{draftId:guid}/image")]
        [RequestSizeLimit(6 * 1024 * 1024)]
        public async Task<ActionResult<ProductDraftResponse>> AttachImage(
            Guid businessId,
            Guid draftId,
            IFormFile file,
            CancellationToken cancellationToken)
        {
            var response = await _productAiService.AttachImageAsync(
                businessId, CurrentUserId, draftId, file, cancellationToken);

            return Ok(response);
        }

        [HttpPost("{draftId:guid}/image-approval")]
        public async Task<ActionResult<ProductDraftResponse>> ResolveImageModification(
            Guid businessId,
            Guid draftId,
            [FromBody] ImageApprovalRequest request,
            CancellationToken cancellationToken)
        {
            var response = await _productAiService.ResolveImageModificationAsync(
                businessId, CurrentUserId, draftId, request.Approved, cancellationToken);

            return Ok(response);
        }

        /// <summary>
        /// Creates the product. The only endpoint that writes to the products table,
        /// and it exists so that creation is always an explicit owner action rather
        /// than something the agent can trigger by deciding it is finished.
        /// </summary>
        [HttpPost("{draftId:guid}/confirm")]
        public async Task<ActionResult<BusinessProductDetailResponse>> Confirm(
            Guid businessId,
            Guid draftId,
            CancellationToken cancellationToken)
        {
            var response = await _productAiService.ConfirmAsync(
                businessId, CurrentUserId, draftId, cancellationToken);

            return Ok(response);
        }

        [HttpPost("{draftId:guid}/cancel")]
        public async Task<ActionResult<ProductDraftResponse>> Cancel(
            Guid businessId,
            Guid draftId,
            CancellationToken cancellationToken)
        {
            var response = await _productAiService.CancelAsync(
                businessId, CurrentUserId, draftId, cancellationToken);

            return Ok(response);
        }
    }
}
