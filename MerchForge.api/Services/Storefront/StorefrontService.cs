using MerchForge.api.DTOs.Common;
using MerchForge.api.DTOs.Storefront;
using MerchForge.api.Exceptions.BusinessDashboard;
using MerchForge.api.Exceptions.Orders;
using MerchForge.api.Exceptions.Storefront;
using MerchForge.api.Repositories.Interfaces;
using MerchForge.api.Services.Storefront.interfaces;

namespace MerchForge.api.Services.Storefront
{
    public class StorefrontService : IStorefrontService
    {
        /// <summary>
        /// Upper bound for the related-products endpoint, so a caller cannot turn it
        /// into an unbounded catalog dump.
        /// </summary>
        public const int MaxRelatedProducts = 20;

        private readonly IStorefrontRepository _storefrontRepository;
        private readonly IOrderRepository _orderRepository;

        public StorefrontService(IStorefrontRepository storefrontRepository, IOrderRepository orderRepository)
        {
            _storefrontRepository = storefrontRepository;
            _orderRepository = orderRepository;
        }

        public async Task<StorefrontBusinessResponse> GetBusinessAsync(
            Guid businessId,
            CancellationToken cancellationToken = default)
        {
            return await _storefrontRepository.GetBusinessAsync(businessId, cancellationToken)
                ?? throw new BusinessNotFoundException();
        }

        public async Task<List<StorefrontCategoryResponse>> GetCategoriesAsync(
            Guid businessId,
            CancellationToken cancellationToken = default)
        {
            // An unknown business must 404 rather than return an empty list, so a
            // storefront can tell "misconfigured businessId" apart from "no categories
            // yet". A business with no domain legitimately has no categories.
            await EnsureBusinessExistsAsync(businessId, cancellationToken);

            return await _storefrontRepository.GetCategoriesAsync(businessId, cancellationToken);
        }

        public async Task<PagedResult<StorefrontProductResponse>> GetProductsAsync(
            Guid businessId,
            StorefrontProductsQueryRequest query,
            CancellationToken cancellationToken = default)
        {
            await EnsureBusinessExistsAsync(businessId, cancellationToken);

            var (items, totalCount) = await _storefrontRepository.GetProductsAsync(
                businessId,
                query,
                cancellationToken);

            // Reuses the platform-wide PagedResult shape rather than a storefront-only
            // pagination format, so the SDK parses one envelope everywhere.
            return new PagedResult<StorefrontProductResponse>
            {
                Items = items,
                Page = query.Page,
                PageSize = query.PageSize,
                TotalCount = totalCount,
            };
        }

        public async Task<StorefrontProductDetailResponse> GetProductAsync(
            Guid businessId,
            Guid productId,
            CancellationToken cancellationToken = default)
        {
            return await _storefrontRepository.GetProductAsync(businessId, productId, cancellationToken)
                ?? throw new ProductNotFoundException();
        }

        public async Task<List<StorefrontProductResponse>> GetRelatedProductsAsync(
            Guid businessId,
            Guid productId,
            int limit,
            CancellationToken cancellationToken = default)
        {
            // 404 for an unknown product, rather than an empty list, so a storefront
            // can tell a bad product id apart from a product that genuinely has no
            // siblings in its category.
            var exists = await _storefrontRepository.ProductExistsAsync(
                businessId,
                productId,
                cancellationToken);

            if (!exists)
            {
                throw new ProductNotFoundException();
            }

            var effectiveLimit = Math.Clamp(limit, 1, MaxRelatedProducts);

            return await _storefrontRepository.GetRelatedProductsAsync(
                businessId,
                productId,
                effectiveLimit,
                cancellationToken);
        }

        // ---- orders ----

        public async Task<StorefrontOrderResponse> CreateOrderAsync(
            Guid businessId,
            CreateOrderRequest request,
            CancellationToken cancellationToken = default)
        {
            await EnsureBusinessExistsAsync(businessId, cancellationToken);

            var order = await _orderRepository.CreateOrderAsync(businessId, request, cancellationToken);

            // Re-read through the same projection GetOrderAsync uses, rather than
            // hand-mapping the just-created entity a second way — one shape, one place
            // it's built.
            return await GetOrderAsync(businessId, order.Id, cancellationToken);
        }

        public async Task<StorefrontOrderResponse> GetOrderAsync(
            Guid businessId,
            Guid orderId,
            CancellationToken cancellationToken = default)
        {
            return await _orderRepository.GetOrderForStorefrontAsync(businessId, orderId, cancellationToken)
                ?? throw new OrderNotFoundException();
        }

        private async Task EnsureBusinessExistsAsync(
            Guid businessId,
            CancellationToken cancellationToken)
        {
            var exists = await _storefrontRepository.BusinessExistsAsync(businessId, cancellationToken);

            if (!exists)
            {
                throw new BusinessNotFoundException();
            }
        }
    }
}
