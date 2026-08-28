using FluentValidation;
using MerchForge.api.DTOs.Common;
using MerchForge.api.DTOs.Storefront;
using MerchForge.api.Services.Storefront;
using MerchForge.api.Services.Storefront.interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace MerchForge.api.Controllers
{
    /// <summary>
    /// Public read-only catalog API consumed by independent storefronts through the
    /// MerchForge Storefront SDK.
    ///
    /// Anonymous by design: this is the public face of a store. businessId identifies
    /// which catalog to read, it does not authorize anything — so every response here
    /// must contain only data that is safe for anyone to see. Nothing owner-, member-,
    /// subscription- or draft-related may ever be added to these DTOs.
    ///
    /// businessId is read from the query string rather than the route so that
    /// hostname-based resolution can replace it later without changing any route
    /// shape, SDK function, or storefront. This controller is the only place the
    /// business context is established; the services below take a plain Guid.
    /// </summary>
    [Route("api/storefront")]
    [ApiController]
    [AllowAnonymous]
    [EnableCors("Storefront")]
    public class StorefrontController : ControllerBase
    {
        private readonly IStorefrontService _storefrontService;
        private readonly IValidator<StorefrontProductsQueryRequest> _productsQueryValidator;
        private readonly IValidator<CreateOrderRequest> _createOrderValidator;

        public StorefrontController(
            IStorefrontService storefrontService,
            IValidator<StorefrontProductsQueryRequest> productsQueryValidator,
            IValidator<CreateOrderRequest> createOrderValidator)
        {
            _storefrontService = storefrontService;
            _productsQueryValidator = productsQueryValidator;
            _createOrderValidator = createOrderValidator;
        }

        /// <summary>Store identity, presentation, and formatting configuration.</summary>
        [HttpGet("business")]
        public async Task<ActionResult<StorefrontBusinessResponse>> GetBusiness(
            [FromQuery] Guid businessId,
            CancellationToken cancellationToken)
        {
            var response = await _storefrontService.GetBusinessAsync(businessId, cancellationToken);

            return Ok(response);
        }

        /// <summary>
        /// Active categories of this business's domain, with per-business product
        /// counts. Empty for a business that has not selected a domain.
        /// </summary>
        [HttpGet("categories")]
        public async Task<ActionResult<List<StorefrontCategoryResponse>>> GetCategories(
            [FromQuery] Guid businessId,
            CancellationToken cancellationToken)
        {
            var response = await _storefrontService.GetCategoriesAsync(businessId, cancellationToken);

            return Ok(response);
        }

        /// <summary>
        /// Paginated catalog with search, category filtering, price range, and
        /// sorting. How the pagination is presented is entirely the storefront's
        /// choice; this returns the data for any strategy.
        /// </summary>
        [HttpGet("products")]
        public async Task<ActionResult<PagedResult<StorefrontProductResponse>>> GetProducts(
            [FromQuery] Guid businessId,
            [FromQuery] StorefrontProductsQueryRequest query,
            CancellationToken cancellationToken)
        {
            await _productsQueryValidator.ValidateAndThrowAsync(query, cancellationToken);

            var response = await _storefrontService.GetProductsAsync(businessId, query, cancellationToken);

            return Ok(response);
        }

        /// <summary>A single product, including its description and metadata.</summary>
        [HttpGet("products/{productId:guid}")]
        public async Task<ActionResult<StorefrontProductDetailResponse>> GetProduct(
            Guid productId,
            [FromQuery] Guid businessId,
            CancellationToken cancellationToken)
        {
            var response = await _storefrontService.GetProductAsync(businessId, productId, cancellationToken);

            return Ok(response);
        }

        /// <summary>
        /// Other products in the same category, excluding this one. Deliberately the
        /// simplest rule the schema supports rather than a recommendation engine.
        /// </summary>
        [HttpGet("products/{productId:guid}/related")]
        public async Task<ActionResult<List<StorefrontProductResponse>>> GetRelatedProducts(
            Guid productId,
            [FromQuery] Guid businessId,
            CancellationToken cancellationToken,
            [FromQuery] int limit = 4)
        {
            var response = await _storefrontService.GetRelatedProductsAsync(
                businessId,
                productId,
                limit,
                cancellationToken);

            return Ok(response);
        }

        // ---- orders ----

        /// <summary>
        /// Places an order from the storefront's cart. No price is trusted from the
        /// client — every line's price and every item's stock are resolved and
        /// checked server-side. No payment is collected or verified here; see
        /// PaymentStatus's own doc comment.
        /// </summary>
        [HttpPost("orders")]
        public async Task<ActionResult<StorefrontOrderResponse>> CreateOrder(
            [FromQuery] Guid businessId,
            [FromBody] CreateOrderRequest request,
            CancellationToken cancellationToken)
        {
            await _createOrderValidator.ValidateAndThrowAsync(request, cancellationToken);

            var response = await _storefrontService.CreateOrderAsync(businessId, request, cancellationToken);

            return Ok(response);
        }

        /// <summary>
        /// Looks up a single order for a confirmation/tracking page. The order id
        /// itself is the only credential — see StorefrontOrderResponse's doc comment.
        /// </summary>
        [HttpGet("orders/{orderId:guid}")]
        public async Task<ActionResult<StorefrontOrderResponse>> GetOrder(
            Guid orderId,
            [FromQuery] Guid businessId,
            CancellationToken cancellationToken)
        {
            var response = await _storefrontService.GetOrderAsync(businessId, orderId, cancellationToken);

            return Ok(response);
        }
    }
}
