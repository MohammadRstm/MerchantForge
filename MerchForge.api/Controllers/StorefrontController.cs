using FluentValidation;
using MerchForge.api.DTOs.Common;
using MerchForge.api.DTOs.Storefront;
using MerchForge.api.Services.ProductReviews.interfaces;
using MerchForge.api.Services.Storefront;
using MerchForge.api.Services.Storefront.interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

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
    [EnableRateLimiting("storefront")]
    public class StorefrontController : ControllerBase
    {
        private readonly IStorefrontService _storefrontService;
        private readonly IProductReviewService _productReviewService;
        private readonly IValidator<StorefrontProductsQueryRequest> _productsQueryValidator;
        private readonly IValidator<CreateOrderRequest> _createOrderValidator;
        private readonly IValidator<ProductReviewsQueryRequest> _reviewsQueryValidator;
        private readonly IValidator<CreateProductReviewRequest> _createReviewValidator;

        public StorefrontController(
            IStorefrontService storefrontService,
            IProductReviewService productReviewService,
            IValidator<StorefrontProductsQueryRequest> productsQueryValidator,
            IValidator<CreateOrderRequest> createOrderValidator,
            IValidator<ProductReviewsQueryRequest> reviewsQueryValidator,
            IValidator<CreateProductReviewRequest> createReviewValidator)
        {
            _storefrontService = storefrontService;
            _productReviewService = productReviewService;
            _productsQueryValidator = productsQueryValidator;
            _createOrderValidator = createOrderValidator;
            _reviewsQueryValidator = reviewsQueryValidator;
            _createReviewValidator = createReviewValidator;
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

        /// <summary>
        /// The draft overlaid on published, for the dashboard's "Preview" button —
        /// never cached, since its whole purpose is reflecting the very latest draft
        /// edit on every reload. token is public-but-obscure (same posture as an order
        /// lookup by id, not real authentication) and gates this to businesses that
        /// have actually started customizing.
        /// </summary>
        [HttpGet("preview")]
        public async Task<ActionResult<StorefrontBusinessResponse>> GetPreview(
            [FromQuery] Guid businessId,
            [FromQuery] string token,
            CancellationToken cancellationToken)
        {
            Response.Headers.CacheControl = "no-store";

            var response = await _storefrontService.GetPreviewAsync(businessId, token, cancellationToken);

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

        // ---- reviews ----

        /// <summary>
        /// A product's published reviews, newest first. Anonymous like the rest of the
        /// catalog — reviews are part of a store's public face. Reviews the owner has
        /// hidden are excluded here and from the summary below.
        /// </summary>
        [HttpGet("products/{productId:guid}/reviews")]
        public async Task<ActionResult<PagedResult<StorefrontProductReviewResponse>>> GetProductReviews(
            Guid productId,
            [FromQuery] Guid businessId,
            [FromQuery] ProductReviewsQueryRequest query,
            CancellationToken cancellationToken)
        {
            await _reviewsQueryValidator.ValidateAndThrowAsync(query, cancellationToken);

            var response = await _productReviewService.GetVisibleReviewsAsync(
                businessId,
                productId,
                query,
                cancellationToken);

            return Ok(response);
        }

        /// <summary>
        /// Average rating, total count, and the per-star breakdown behind a ratings
        /// histogram. Separate from the list so a storefront can show the summary on a
        /// product card without paging through reviews it isn't going to render.
        /// </summary>
        [HttpGet("products/{productId:guid}/reviews/summary")]
        public async Task<ActionResult<ProductReviewSummaryResponse>> GetProductReviewSummary(
            Guid productId,
            [FromQuery] Guid businessId,
            CancellationToken cancellationToken)
        {
            var response = await _productReviewService.GetSummaryAsync(businessId, productId, cancellationToken);

            return Ok(response);
        }

        /// <summary>
        /// Whether the signed-in customer may review this product, plus their existing
        /// review if they have one — one request that answers everything the storefront
        /// needs to decide between the form, a "sign in" prompt, and a "buyers only"
        /// notice.
        ///
        /// Unlike the two endpoints above, this one is about the caller, so it requires
        /// a customer token. The class is [AllowAnonymous], so that's enforced by hand
        /// below rather than by an attribute.
        /// </summary>
        [HttpGet("products/{productId:guid}/reviews/me")]
        public async Task<ActionResult<ProductReviewEligibilityResponse>> GetMyProductReview(
            Guid productId,
            [FromQuery] Guid businessId,
            CancellationToken cancellationToken)
        {
            var customerId = await TryGetAuthenticatedCustomerIdAsync();

            if (customerId is null)
            {
                return Unauthorized();
            }

            var response = await _productReviewService.GetEligibilityAsync(
                businessId,
                productId,
                customerId.Value,
                cancellationToken);

            return Ok(response);
        }

        /// <summary>
        /// Submits the signed-in customer's review of this product, or updates it if
        /// they already wrote one — a customer has at most one review per product, so
        /// this is an upsert rather than a create. That also keeps the whole feature
        /// within the "Storefront" CORS policy, which allows GET/POST/PUT but no DELETE.
        ///
        /// Requires a customer token, and requires that customer to have actually
        /// ordered the product.
        /// </summary>
        [HttpPost("products/{productId:guid}/reviews")]
        public async Task<ActionResult<MyProductReviewResponse>> SubmitProductReview(
            Guid productId,
            [FromQuery] Guid businessId,
            [FromBody] CreateProductReviewRequest request,
            CancellationToken cancellationToken)
        {
            var customerId = await TryGetAuthenticatedCustomerIdAsync();

            if (customerId is null)
            {
                return Unauthorized();
            }

            await _createReviewValidator.ValidateAndThrowAsync(request, cancellationToken);

            var response = await _productReviewService.SubmitReviewAsync(
                businessId,
                productId,
                customerId.Value,
                request,
                cancellationToken);

            return Ok(response);
        }

        // ---- orders ----

        /// <summary>
        /// Places an order from the storefront's cart. No price is trusted from the
        /// client — every line's price and every item's stock are resolved and
        /// checked server-side. No payment is collected or verified here; see
        /// PaymentStatus's own doc comment. Guest checkout stays fully anonymous —
        /// this endpoint only ever *optionally* attaches a customer, never requires
        /// one: if the caller carries a valid "Customer" Bearer token, the order links
        /// to that customer; otherwise it's a guest order exactly as before.
        /// </summary>
        [HttpPost("orders")]
        public async Task<ActionResult<StorefrontOrderResponse>> CreateOrder(
            [FromQuery] Guid businessId,
            [FromBody] CreateOrderRequest request,
            CancellationToken cancellationToken)
        {
            await _createOrderValidator.ValidateAndThrowAsync(request, cancellationToken);

            var customerId = await TryGetAuthenticatedCustomerIdAsync();

            var response = await _storefrontService.CreateOrderAsync(businessId, request, customerId, cancellationToken);

            return Ok(response);
        }

        /// <summary>
        /// This endpoint is [AllowAnonymous], so nothing requires a customer token —
        /// but a signed-in customer's storefront calls still send one as a plain
        /// Bearer header (see the SDK's customerApiClient). Authenticating it by hand
        /// against the "Customer" scheme, rather than an [Authorize] attribute, is
        /// what makes attaching it optional instead of mandatory.
        /// </summary>
        private async Task<Guid?> TryGetAuthenticatedCustomerIdAsync()
        {
            var result = await HttpContext.AuthenticateAsync("Customer");

            if (!result.Succeeded || result.Principal is null)
            {
                return null;
            }

            var id = result.Principal.FindFirstValue(ClaimTypes.NameIdentifier);

            return Guid.TryParse(id, out var customerId) ? customerId : null;
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
