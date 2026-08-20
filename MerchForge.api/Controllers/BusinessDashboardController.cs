using FluentValidation;
using MerchForge.api.Authorization;
using MerchForge.api.DTOs.BusinessDashboard;
using MerchForge.api.DTOs.Common;
using MerchForge.api.Services.BusinessDashboard.interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MerchForge.api.Controllers
{
    [Route("api/businesses/{businessId:guid}/dashboard")]
    [ApiController]
    [Authorize(Policy = AuthorizationPolicies.BusinessOwner)]
    public class BusinessDashboardController : ControllerBase
    {
        private readonly IBusinessDashboardService _businessDashboardService;
        private readonly IBusinessMemberService _businessMemberService;
        private readonly IProductImageService _productImageService;
        private readonly IValidator<ProductsQueryRequest> _productsQueryValidator;
        private readonly IValidator<SaveProductRequest> _saveProductValidator;
        private readonly IValidator<CreateBusinessMemberRequest> _createMemberValidator;

        public BusinessDashboardController(
            IBusinessDashboardService businessDashboardService,
            IBusinessMemberService businessMemberService,
            IProductImageService productImageService,
            IValidator<ProductsQueryRequest> productsQueryValidator,
            IValidator<SaveProductRequest> saveProductValidator,
            IValidator<CreateBusinessMemberRequest> createMemberValidator)
        {
            _businessDashboardService = businessDashboardService;
            _businessMemberService = businessMemberService;
            _productImageService = productImageService;
            _productsQueryValidator = productsQueryValidator;
            _saveProductValidator = saveProductValidator;
            _createMemberValidator = createMemberValidator;
        }

        [HttpGet("stats")]
        public async Task<ActionResult<BusinessDashboardStatsResponse>> GetStats(
            Guid businessId,
            CancellationToken cancellationToken)
        {
            var response = await _businessDashboardService.GetStatsAsync(businessId, cancellationToken);

            return Ok(response);
        }

        [HttpGet("products")]
        public async Task<ActionResult<PagedResult<BusinessProductResponse>>> GetProducts(
            Guid businessId,
            [FromQuery] ProductsQueryRequest query,
            CancellationToken cancellationToken)
        {
            await _productsQueryValidator.ValidateAndThrowAsync(query, cancellationToken);

            var response = await _businessDashboardService.GetProductsAsync(businessId, query, cancellationToken);

            return Ok(response);
        }

        [HttpGet("members")]
        public async Task<ActionResult<List<BusinessMemberResponse>>> GetMembers(
            Guid businessId,
            CancellationToken cancellationToken)
        {
            var response = await _businessDashboardService.GetMembersAsync(businessId, cancellationToken);

            return Ok(response);
        }

        /// <summary>
        /// Creates a team member account and attaches it to this business.
        ///
        /// businessId comes from the route, which the BusinessOwner policy has
        /// already checked — never from the body.
        /// </summary>
        [HttpPost("members")]
        public async Task<ActionResult<CreateBusinessMemberResponse>> CreateMember(
            Guid businessId,
            [FromBody] CreateBusinessMemberRequest request,
            CancellationToken cancellationToken)
        {
            await _createMemberValidator.ValidateAndThrowAsync(request, cancellationToken);

            var response = await _businessMemberService.CreateMemberAsync(
                businessId,
                request,
                cancellationToken);

            return Ok(response);
        }

        [HttpGet("subscription")]
        public async Task<ActionResult<BusinessSubscriptionResponse?>> GetSubscription(
            Guid businessId,
            CancellationToken cancellationToken)
        {
            var response = await _businessDashboardService.GetSubscriptionAsync(businessId, cancellationToken);

            return Ok(response);
        }

        // ---- product CRUD ----

        /// <summary>
        /// What the product form needs to render: usable categories and the optional
        /// fields this business opted into at onboarding.
        /// </summary>
        [HttpGet("product-form")]
        public async Task<ActionResult<ProductFormResponse>> GetProductForm(
            Guid businessId,
            CancellationToken cancellationToken)
        {
            var response = await _businessDashboardService.GetProductFormAsync(businessId, cancellationToken);

            return Ok(response);
        }

        [HttpGet("products/{productId:guid}")]
        public async Task<ActionResult<BusinessProductDetailResponse>> GetProduct(
            Guid businessId,
            Guid productId,
            CancellationToken cancellationToken)
        {
            var response = await _businessDashboardService.GetProductAsync(businessId, productId, cancellationToken);

            return Ok(response);
        }

        [HttpPost("products")]
        public async Task<ActionResult<BusinessProductDetailResponse>> CreateProduct(
            Guid businessId,
            [FromBody] SaveProductRequest request,
            CancellationToken cancellationToken)
        {
            await _saveProductValidator.ValidateAndThrowAsync(request, cancellationToken);

            var response = await _businessDashboardService.CreateProductAsync(businessId, request, cancellationToken);

            return CreatedAtAction(
                nameof(GetProduct),
                new { businessId, productId = response.Id },
                response);
        }

        [HttpPut("products/{productId:guid}")]
        public async Task<ActionResult<BusinessProductDetailResponse>> UpdateProduct(
            Guid businessId,
            Guid productId,
            [FromBody] SaveProductRequest request,
            CancellationToken cancellationToken)
        {
            await _saveProductValidator.ValidateAndThrowAsync(request, cancellationToken);

            var response = await _businessDashboardService.UpdateProductAsync(
                businessId,
                productId,
                request,
                cancellationToken);

            return Ok(response);
        }

        [HttpDelete("products/{productId:guid}")]
        public async Task<IActionResult> DeleteProduct(
            Guid businessId,
            Guid productId,
            CancellationToken cancellationToken)
        {
            await _businessDashboardService.DeleteProductAsync(businessId, productId, cancellationToken);

            return NoContent();
        }

        /// <summary>
        /// Stores a product image and returns its URL. Separate from saving the
        /// product so the form can show a preview before anything is committed, and
        /// so an image can be replaced without re-sending the rest of the product.
        /// </summary>
        [HttpPost("products/image")]
        [RequestSizeLimit(6 * 1024 * 1024)]
        public async Task<ActionResult<ProductImageUploadResponse>> UploadProductImage(
            Guid businessId,
            IFormFile file,
            CancellationToken cancellationToken)
        {
            var imageUrl = await _productImageService.SaveAsync(businessId, file, cancellationToken);

            return Ok(new ProductImageUploadResponse { ImageUrl = imageUrl });
        }
    }
}
