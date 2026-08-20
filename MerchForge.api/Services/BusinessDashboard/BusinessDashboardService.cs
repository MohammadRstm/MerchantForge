using System.Text.Json;
using MerchForge.api.DTOs.BusinessDashboard;
using MerchForge.api.DTOs.Common;
using MerchForge.api.Exceptions.BusinessDashboard;
using MerchForge.api.Exceptions.Storefront;
using MerchForge.api.Repositories.Interfaces;
using MerchForge.api.Services.BusinessDashboard.interfaces;
using MerchForge.api.Services.Common;

namespace MerchForge.api.Services.BusinessDashboard
{
    public class BusinessDashboardService : IBusinessDashboardService
    {
        private const int StatsTimeSeriesMonths = 6;

        private readonly IBusinessDashboardRepository _businessDashboardRepository;
        private readonly ISubscriptionRepository _subscriptionRepository;

        public BusinessDashboardService(
            IBusinessDashboardRepository businessDashboardRepository,
            ISubscriptionRepository subscriptionRepository)
        {
            _businessDashboardRepository = businessDashboardRepository;
            _subscriptionRepository = subscriptionRepository;
        }

        public async Task<BusinessDashboardStatsResponse> GetStatsAsync(
            Guid businessId,
            CancellationToken cancellationToken = default)
        {
            var summary = await _businessDashboardRepository.GetBusinessSummaryAsync(businessId, cancellationToken);

            if (summary is null)
            {
                throw new BusinessNotFoundException();
            }

            var now = DateTime.UtcNow;

            var seriesStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc)
                .AddMonths(-(StatsTimeSeriesMonths - 1));

            var memberCount = await _businessDashboardRepository.CountMembersAsync(businessId, cancellationToken);
            var productCount = await _businessDashboardRepository.CountProductsAsync(businessId, cancellationToken);
            var draftCount = await _businessDashboardRepository.CountProductDraftsAsync(businessId, cancellationToken);

            var priceStats = await _businessDashboardRepository.GetProductPriceStatsAsync(businessId, cancellationToken);

            var productsByCategory = await _businessDashboardRepository.GetProductsByCategoryAsync(businessId, cancellationToken);
            var draftsByStatus = await _businessDashboardRepository.GetProductDraftsByStatusAsync(businessId, cancellationToken);
            var membersByRole = await _businessDashboardRepository.GetMembersByRoleAsync(businessId, cancellationToken);

            var productDates = await _businessDashboardRepository.GetProductCreationDatesSinceAsync(
                businessId,
                seriesStart,
                cancellationToken);

            return new BusinessDashboardStatsResponse
            {
                BusinessId = businessId,
                BusinessName = summary.Value.Name,
                CreatedAt = summary.Value.CreatedAt,

                MemberCount = memberCount,
                ProductCount = productCount,
                ProductDraftCount = draftCount,

                AverageProductPrice = priceStats.Average,
                MinProductPrice = priceStats.Min,
                MaxProductPrice = priceStats.Max,

                ProductsByCategory = productsByCategory,
                ProductDraftsByStatus = draftsByStatus,
                MembersByRole = membersByRole,

                ProductsOverTime = TimeSeriesBuilder.BuildMonthlySeries(productDates, seriesStart, now),
            };
        }

        public async Task<PagedResult<BusinessProductResponse>> GetProductsAsync(
            Guid businessId,
            ProductsQueryRequest query,
            CancellationToken cancellationToken = default)
        {
            var (items, totalCount) = await _businessDashboardRepository.GetProductsAsync(businessId, query, cancellationToken);

            return new PagedResult<BusinessProductResponse>
            {
                Items = items,
                Page = query.Page,
                PageSize = query.PageSize,
                TotalCount = totalCount,
            };
        }

        public async Task<List<BusinessMemberResponse>> GetMembersAsync(
            Guid businessId,
            CancellationToken cancellationToken = default)
        {
            return await _businessDashboardRepository.GetMembersAsync(businessId, cancellationToken);
        }

        public async Task<BusinessSubscriptionResponse?> GetSubscriptionAsync(
            Guid businessId,
            CancellationToken cancellationToken = default)
        {
            var subscription = await _subscriptionRepository.GetLatestSubscriptionWithPlanFeaturesAsync(
                businessId,
                cancellationToken);

            if (subscription is null)
            {
                return null;
            }

            return new BusinessSubscriptionResponse
            {
                Id = subscription.Id,
                PlanName = subscription.SubscriptionPlan.Name,
                Price = subscription.SubscriptionPlan.Price,
                Currency = subscription.SubscriptionPlan.Currency,
                BillingInterval = subscription.SubscriptionPlan.BillingInterval.ToString(),
                Status = subscription.Status.ToString(),
                CurrentPeriodStart = subscription.CurrentPeriodStart,
                CurrentPeriodEnd = subscription.CurrentPeriodEnd,
                Features = subscription.SubscriptionPlan.PlanFeatures
                    .Where(pf => pf.Feature.IsActive)
                    .Select(pf => new PlanFeatureItemResponse
                    {
                        FeatureKey = pf.Feature.Key,
                        FeatureName = pf.Feature.Name,
                        Limit = pf.Limit,
                    })
                    .ToList(),
            };
        }

        // ---- product CRUD ----

        public async Task<ProductFormResponse> GetProductFormAsync(
            Guid businessId,
            CancellationToken cancellationToken = default)
        {
            var formData = await _businessDashboardRepository.GetProductFormDataAsync(businessId, cancellationToken)
                ?? throw new BusinessNotFoundException();

            return new ProductFormResponse
            {
                Categories = formData.Categories,
                MetadataFields = ReadMetadataFields(formData.MetadataShape),
            };
        }

        public async Task<BusinessProductDetailResponse> GetProductAsync(
            Guid businessId,
            Guid productId,
            CancellationToken cancellationToken = default)
        {
            return await _businessDashboardRepository.GetProductAsync(businessId, productId, cancellationToken)
                ?? throw new ProductNotFoundException();
        }

        public async Task<BusinessProductDetailResponse> CreateProductAsync(
            Guid businessId,
            SaveProductRequest request,
            CancellationToken cancellationToken = default)
        {
            var formData = await _businessDashboardRepository.GetProductFormDataAsync(businessId, cancellationToken)
                ?? throw new BusinessNotFoundException();

            await EnsureCategoryIsUsableAsync(businessId, request.CategoryId, cancellationToken);

            var now = DateTime.UtcNow;

            var product = new Models.Product
            {
                Id = Guid.NewGuid(),
                BusinessId = businessId,
                CategoryId = request.CategoryId,
                Title = request.Title.Trim(),
                Description = request.Description.Trim(),
                Price = request.Price,
                ImageUrl = NormalizeImageUrl(request.ImageUrl),
                Metadata = ProductMetadataBuilder.Build(formData.MetadataShape, request.Metadata),
                CreatedAt = now,
                UpdatedAt = now,
            };

            await _businessDashboardRepository.CreateProductAsync(product, cancellationToken);

            return await GetProductAsync(businessId, product.Id, cancellationToken);
        }

        public async Task<BusinessProductDetailResponse> UpdateProductAsync(
            Guid businessId,
            Guid productId,
            SaveProductRequest request,
            CancellationToken cancellationToken = default)
        {
            var product = await _businessDashboardRepository.GetTrackedProductAsync(businessId, productId, cancellationToken)
                ?? throw new ProductNotFoundException();

            var formData = await _businessDashboardRepository.GetProductFormDataAsync(businessId, cancellationToken)
                ?? throw new BusinessNotFoundException();

            await EnsureCategoryIsUsableAsync(businessId, request.CategoryId, cancellationToken);

            product.Title = request.Title.Trim();
            product.Description = request.Description.Trim();
            product.Price = request.Price;
            product.CategoryId = request.CategoryId;
            product.ImageUrl = NormalizeImageUrl(request.ImageUrl);
            product.Metadata = ProductMetadataBuilder.Build(formData.MetadataShape, request.Metadata);
            product.UpdatedAt = DateTime.UtcNow;

            await _businessDashboardRepository.SaveChangesAsync(cancellationToken);

            return await GetProductAsync(businessId, productId, cancellationToken);
        }

        public async Task DeleteProductAsync(
            Guid businessId,
            Guid productId,
            CancellationToken cancellationToken = default)
        {
            var product = await _businessDashboardRepository.GetTrackedProductAsync(businessId, productId, cancellationToken)
                ?? throw new ProductNotFoundException();

            // The image file is intentionally left on disk. Deleting it here would be
            // wrong if the same URL was ever reused, and orphaned files are a cleanup
            // concern rather than a correctness one.
            await _businessDashboardRepository.DeleteProductAsync(product, cancellationToken);
        }

        private async Task EnsureCategoryIsUsableAsync(
            Guid businessId,
            Guid categoryId,
            CancellationToken cancellationToken)
        {
            var canUse = await _businessDashboardRepository.CanUseCategoryAsync(
                businessId,
                categoryId,
                cancellationToken);

            if (!canUse)
            {
                throw new InvalidProductCategoryException();
            }
        }

        /// <summary>
        /// Blank and whitespace-only image URLs are stored as null so "no image" has
        /// one representation rather than three.
        /// </summary>
        private static string? NormalizeImageUrl(string? imageUrl) =>
            string.IsNullOrWhiteSpace(imageUrl) ? null : imageUrl.Trim();

        private static List<ProductFormFieldResponse> ReadMetadataFields(JsonDocument? metadataShape)
        {
            var fields = new List<ProductFormFieldResponse>();

            if (metadataShape is null
                || !metadataShape.RootElement.TryGetProperty("fields", out var fieldsElement)
                || fieldsElement.ValueKind != JsonValueKind.Array)
            {
                return fields;
            }

            foreach (var field in fieldsElement.EnumerateArray())
            {
                var key = field.TryGetProperty("key", out var k) ? k.GetString() : null;
                var label = field.TryGetProperty("label", out var l) ? l.GetString() : null;
                var valueType = field.TryGetProperty("valueType", out var v) ? v.GetString() : null;

                if (key is null || label is null || valueType is null)
                {
                    continue;
                }

                fields.Add(new ProductFormFieldResponse
                {
                    Key = key,
                    Label = label,
                    ValueType = valueType,
                    IsRequired = field.TryGetProperty("isRequired", out var req)
                        && req.ValueKind == JsonValueKind.True,
                    AllowedValues = field.TryGetProperty("allowedValues", out var allowed)
                        && allowed.ValueKind == JsonValueKind.Array
                            ? allowed.EnumerateArray()
                                .Where(v => v.ValueKind == JsonValueKind.String)
                                .Select(v => v.GetString()!)
                                .ToList()
                            : [],
                });
            }

            return fields;
        }
    }
}
