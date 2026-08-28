using System.Security.Claims;
using FluentValidation;
using MerchForge.api.Authorization;
using MerchForge.api.DTOs.BusinessDashboard;
using MerchForge.api.DTOs.Common;
using MerchForge.api.DTOs.WebsiteTemplateRequests;
using MerchForge.api.Services.BusinessDashboard.interfaces;
using MerchForge.api.DTOs.Dashboard;
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
        private readonly IWebsiteCustomizationService _websiteCustomizationService;
        private readonly IValidator<ProductsQueryRequest> _productsQueryValidator;
        private readonly IValidator<SaveProductRequest> _saveProductValidator;
        private readonly IValidator<CreateBusinessMemberRequest> _createMemberValidator;
        private readonly IValidator<CreateWebsiteTemplateRequestRequest> _createWebsiteTemplateRequestValidator;
        private readonly IValidator<StockAdjustmentRequest> _stockAdjustmentValidator;
        private readonly IValidator<UpdateLowStockThresholdRequest> _updateLowStockThresholdValidator;
        private readonly IValidator<OrdersQueryRequest> _ordersQueryValidator;
        private readonly IValidator<UpdateOrderStatusRequest> _updateOrderStatusValidator;
        private readonly IValidator<UpdateOrderPaymentStatusRequest> _updateOrderPaymentStatusValidator;
        private readonly IValidator<SaveWebsiteCustomizationDraftRequest> _saveWebsiteCustomizationDraftValidator;

        public BusinessDashboardController(
            IBusinessDashboardService businessDashboardService,
            IBusinessMemberService businessMemberService,
            IProductImageService productImageService,
            IWebsiteCustomizationService websiteCustomizationService,
            IValidator<ProductsQueryRequest> productsQueryValidator,
            IValidator<SaveProductRequest> saveProductValidator,
            IValidator<CreateBusinessMemberRequest> createMemberValidator,
            IValidator<CreateWebsiteTemplateRequestRequest> createWebsiteTemplateRequestValidator,
            IValidator<StockAdjustmentRequest> stockAdjustmentValidator,
            IValidator<UpdateLowStockThresholdRequest> updateLowStockThresholdValidator,
            IValidator<OrdersQueryRequest> ordersQueryValidator,
            IValidator<UpdateOrderStatusRequest> updateOrderStatusValidator,
            IValidator<UpdateOrderPaymentStatusRequest> updateOrderPaymentStatusValidator,
            IValidator<SaveWebsiteCustomizationDraftRequest> saveWebsiteCustomizationDraftValidator)
        {
            _businessDashboardService = businessDashboardService;
            _businessMemberService = businessMemberService;
            _productImageService = productImageService;
            _websiteCustomizationService = websiteCustomizationService;
            _productsQueryValidator = productsQueryValidator;
            _saveProductValidator = saveProductValidator;
            _createMemberValidator = createMemberValidator;
            _createWebsiteTemplateRequestValidator = createWebsiteTemplateRequestValidator;
            _stockAdjustmentValidator = stockAdjustmentValidator;
            _updateLowStockThresholdValidator = updateLowStockThresholdValidator;
            _ordersQueryValidator = ordersQueryValidator;
            _updateOrderStatusValidator = updateOrderStatusValidator;
            _updateOrderPaymentStatusValidator = updateOrderPaymentStatusValidator;
            _saveWebsiteCustomizationDraftValidator = saveWebsiteCustomizationDraftValidator;
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

        // ---- website template requests ----

        /// <summary>
        /// What the template-selection page needs: the business's domain, whether it
        /// already has an open request, and the domain's available templates otherwise.
        /// </summary>
        [HttpGet("website-template-options")]
        public async Task<ActionResult<WebsiteTemplateOptionsResponse>> GetWebsiteTemplateOptions(
            Guid businessId,
            CancellationToken cancellationToken)
        {
            var response = await _businessDashboardService.GetWebsiteTemplateOptionsAsync(businessId, cancellationToken);

            return Ok(response);
        }

        [HttpGet("website-template-requests")]
        public async Task<ActionResult<List<WebsiteTemplateRequestResponse>>> GetWebsiteTemplateRequests(
            Guid businessId,
            CancellationToken cancellationToken)
        {
            var response = await _businessDashboardService.GetWebsiteTemplateRequestsAsync(businessId, cancellationToken);

            return Ok(response);
        }

        [HttpPost("website-template-requests")]
        public async Task<ActionResult<WebsiteTemplateRequestResponse>> CreateWebsiteTemplateRequest(
            Guid businessId,
            [FromBody] CreateWebsiteTemplateRequestRequest request,
            CancellationToken cancellationToken)
        {
            await _createWebsiteTemplateRequestValidator.ValidateAndThrowAsync(request, cancellationToken);

            var requestedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!Guid.TryParse(requestedByUserId, out var parsedRequestedByUserId))
            {
                return Unauthorized();
            }

            var response = await _businessDashboardService.CreateWebsiteTemplateRequestAsync(
                businessId,
                parsedRequestedByUserId,
                request,
                cancellationToken);

            return Ok(response);
        }

        // ---- inventory ----

        [HttpPost("products/{productId:guid}/stock-adjustments")]
        public async Task<ActionResult<StockAdjustmentResponse>> AdjustStock(
            Guid businessId,
            Guid productId,
            [FromBody] StockAdjustmentRequest request,
            CancellationToken cancellationToken)
        {
            await _stockAdjustmentValidator.ValidateAndThrowAsync(request, cancellationToken);

            var actingUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!Guid.TryParse(actingUserId, out var parsedActingUserId))
            {
                return Unauthorized();
            }

            var response = await _businessDashboardService.AdjustStockAsync(
                businessId,
                productId,
                request,
                parsedActingUserId,
                cancellationToken);

            return Ok(response);
        }

        [HttpGet("inventory/summary")]
        public async Task<ActionResult<InventorySummaryResponse>> GetInventorySummary(
            Guid businessId,
            CancellationToken cancellationToken)
        {
            var response = await _businessDashboardService.GetInventorySummaryAsync(businessId, cancellationToken);

            return Ok(response);
        }

        [HttpGet("inventory/movements")]
        public async Task<ActionResult<List<StockMovementResponse>>> GetRecentStockMovements(
            Guid businessId,
            [FromQuery] int take,
            CancellationToken cancellationToken)
        {
            // Same "clamp rather than validate" treatment as an unset paging size
            // elsewhere — this is a fixed-shape activity feed, not a paged list.
            var boundedTake = take is < 1 or > 100 ? 20 : take;

            var response = await _businessDashboardService.GetRecentStockMovementsAsync(
                businessId, boundedTake, cancellationToken);

            return Ok(response);
        }

        [HttpPut("inventory/low-stock-threshold")]
        public async Task<IActionResult> UpdateLowStockThreshold(
            Guid businessId,
            [FromBody] UpdateLowStockThresholdRequest request,
            CancellationToken cancellationToken)
        {
            await _updateLowStockThresholdValidator.ValidateAndThrowAsync(request, cancellationToken);

            await _businessDashboardService.UpdateLowStockThresholdAsync(
                businessId, request.LowStockThreshold, cancellationToken);

            return NoContent();
        }

        // ---- orders ----

        [HttpGet("orders")]
        public async Task<ActionResult<PagedResult<BusinessOrderResponse>>> GetOrders(
            Guid businessId,
            [FromQuery] OrdersQueryRequest query,
            CancellationToken cancellationToken)
        {
            await _ordersQueryValidator.ValidateAndThrowAsync(query, cancellationToken);

            var response = await _businessDashboardService.GetOrdersAsync(businessId, query, cancellationToken);

            return Ok(response);
        }

        [HttpGet("orders/{orderId:guid}")]
        public async Task<ActionResult<BusinessOrderDetailResponse>> GetOrder(
            Guid businessId,
            Guid orderId,
            CancellationToken cancellationToken)
        {
            var response = await _businessDashboardService.GetOrderAsync(businessId, orderId, cancellationToken);

            return Ok(response);
        }

        [HttpPut("orders/{orderId:guid}/status")]
        public async Task<ActionResult<BusinessOrderDetailResponse>> UpdateOrderStatus(
            Guid businessId,
            Guid orderId,
            [FromBody] UpdateOrderStatusRequest request,
            CancellationToken cancellationToken)
        {
            await _updateOrderStatusValidator.ValidateAndThrowAsync(request, cancellationToken);

            var response = await _businessDashboardService.UpdateOrderStatusAsync(
                businessId, orderId, request.Status, cancellationToken);

            return Ok(response);
        }

        [HttpPut("orders/{orderId:guid}/payment-status")]
        public async Task<ActionResult<BusinessOrderDetailResponse>> UpdateOrderPaymentStatus(
            Guid businessId,
            Guid orderId,
            [FromBody] UpdateOrderPaymentStatusRequest request,
            CancellationToken cancellationToken)
        {
            await _updateOrderPaymentStatusValidator.ValidateAndThrowAsync(request, cancellationToken);

            var response = await _businessDashboardService.UpdateOrderPaymentStatusAsync(
                businessId, orderId, request.PaymentStatus, cancellationToken);

            return Ok(response);
        }

        // ---- website customization ----

        [HttpGet("website-customization/catalogue")]
        public async Task<ActionResult<List<WebsiteTemplateCustomizableComponentResponse>>> GetWebsiteCustomizationCatalogue(
            Guid businessId,
            CancellationToken cancellationToken)
        {
            var response = await _websiteCustomizationService.GetCatalogueAsync(businessId, cancellationToken);

            return Ok(response);
        }

        [HttpGet("website-customization/draft")]
        public async Task<ActionResult<WebsiteCustomizationDraftResponse>> GetWebsiteCustomizationDraft(
            Guid businessId,
            CancellationToken cancellationToken)
        {
            var response = await _websiteCustomizationService.GetOrCreateDraftAsync(businessId, cancellationToken);

            return Ok(response);
        }

        [HttpPut("website-customization/draft")]
        public async Task<ActionResult<WebsiteCustomizationDraftResponse>> SaveWebsiteCustomizationDraft(
            Guid businessId,
            [FromBody] SaveWebsiteCustomizationDraftRequest request,
            CancellationToken cancellationToken)
        {
            await _saveWebsiteCustomizationDraftValidator.ValidateAndThrowAsync(request, cancellationToken);

            var response = await _websiteCustomizationService.SaveDraftAsync(businessId, request, cancellationToken);

            return Ok(response);
        }
    }
}
