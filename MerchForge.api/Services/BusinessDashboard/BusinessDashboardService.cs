using System.Text.Json;
using Hangfire;
using MerchForge.api.DTOs.BusinessDashboard;
using MerchForge.api.DTOs.Common;
using MerchForge.api.Exceptions.BusinessDashboard;
using MerchForge.api.Exceptions.Storefront;
using MerchForge.api.Jobs.Email;
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
        private readonly IBackgroundJobClient _backgroundJobClient;

        public BusinessDashboardService(
            IBusinessDashboardRepository businessDashboardRepository,
            ISubscriptionRepository subscriptionRepository,
            IBackgroundJobClient backgroundJobClient)
        {
            _businessDashboardRepository = businessDashboardRepository;
            _subscriptionRepository = subscriptionRepository;
            _backgroundJobClient = backgroundJobClient;
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
            var images = BuildProductImages(request.Images);

            var product = new Models.Product
            {
                Id = Guid.NewGuid(),
                BusinessId = businessId,
                CategoryId = request.CategoryId,
                Title = request.Title.Trim(),
                Description = request.Description.Trim(),
                Price = request.Price,
                CompareAtPrice = request.CompareAtPrice,
                // Kept in sync with Images so consumers that only read ImageUrl (the
                // dashboard list, existing storefront card rendering) keep working
                // without needing to switch to the gallery first.
                ImageUrl = images.First(i => i.IsMain).Url,
                Images = images,
                Sku = NormalizeSku(request.Sku),
                StockQuantity = request.StockQuantity,
                Tags = NormalizeTags(request.Tags),
                SaleEndsAt = request.SaleEndsAt,
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

            var images = BuildProductImages(request.Images);

            product.Title = request.Title.Trim();
            product.Description = request.Description.Trim();
            product.Price = request.Price;
            product.CompareAtPrice = request.CompareAtPrice;
            product.CategoryId = request.CategoryId;
            product.ImageUrl = images.First(i => i.IsMain).Url;
            product.Sku = NormalizeSku(request.Sku);
            product.StockQuantity = request.StockQuantity;
            product.Tags = NormalizeTags(request.Tags);
            product.SaleEndsAt = request.SaleEndsAt;
            product.Metadata = ProductMetadataBuilder.Build(formData.MetadataShape, request.Metadata);
            product.UpdatedAt = DateTime.UtcNow;

            // Full replace, not a merge — same "one DTO, no partial patch" contract as
            // every other field here. This also persists every other change made to
            // `product` above, in the same SaveChanges call.
            await _businessDashboardRepository.ReplaceProductImagesAsync(product, images, cancellationToken);

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

        // ---- website template ----

        public async Task<BusinessWebsiteTemplateStatusResponse> GetWebsiteTemplateStatusAsync(
            Guid businessId,
            CancellationToken cancellationToken = default)
        {
            var info = await _businessDashboardRepository.GetBusinessWebsiteTemplateInfoAsync(businessId, cancellationToken)
                ?? throw new BusinessNotFoundException();

            if (info.BusinessDomainId is null)
            {
                throw new BusinessHasNoDomainException();
            }

            var chosen = info.WebsiteTemplateId is null
                ? null
                : new ChosenWebsiteTemplateResponse
                {
                    Id = info.WebsiteTemplateId.Value,
                    Name = info.WebsiteTemplateName!,
                    Label = info.WebsiteTemplateLabel!,
                    VideoPreviewUrl = info.WebsiteTemplateVideoPreviewUrl!,
                    ChosenAt = info.WebsiteTemplateChosenAt!.Value,
                };

            // No point fetching the available list once a template is already chosen
            // — the dashboard has nothing to render it for.
            var available = chosen is null
                ? await _businessDashboardRepository.GetActiveWebsiteTemplatesByDomainAsync(info.BusinessDomainId.Value, cancellationToken)
                : [];

            return new BusinessWebsiteTemplateStatusResponse
            {
                BusinessDomainId = info.BusinessDomainId.Value,
                DomainName = info.DomainName!,
                Chosen = chosen,
                Available = available,
            };
        }

        public async Task<ChosenWebsiteTemplateResponse> ChooseWebsiteTemplateAsync(
            Guid businessId,
            ChooseWebsiteTemplateRequest request,
            CancellationToken cancellationToken = default)
        {
            var info = await _businessDashboardRepository.GetBusinessWebsiteTemplateInfoAsync(businessId, cancellationToken)
                ?? throw new BusinessNotFoundException();

            if (info.BusinessDomainId is null)
            {
                throw new BusinessHasNoDomainException();
            }

            if (info.WebsiteTemplateId is not null)
            {
                throw new WebsiteTemplateAlreadyChosenException();
            }

            var template = await _businessDashboardRepository.GetActiveWebsiteTemplateInDomainAsync(
                request.WebsiteTemplateId, info.BusinessDomainId.Value, cancellationToken)
                ?? throw new WebsiteTemplateWrongDomainException();

            var chosen = await _businessDashboardRepository.ChooseWebsiteTemplateAsync(
                businessId, template.Id, cancellationToken);

            if (!chosen)
            {
                // Lost a race with another request choosing at the same instant.
                throw new WebsiteTemplateAlreadyChosenException();
            }

            _backgroundJobClient.Enqueue<NotifyAdminOfWebsiteTemplateChoiceJob>(
                job => job.ExecuteAsync(businessId, template.Id));

            return new ChosenWebsiteTemplateResponse
            {
                Id = template.Id,
                Name = template.Name,
                Label = template.Label,
                VideoPreviewUrl = template.VideoPreviewUrl,
                ChosenAt = DateTime.UtcNow,
            };
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
        /// Turns the submitted image list into real ProductImage rows. The validator
        /// already guarantees 1-5 images with exactly one IsMain, so First(IsMain) at
        /// the call site is always safe — no separate null-checking needed there.
        /// DisplayOrder is just submission order: the merchant controls gallery order
        /// by however they arrange images in the form, not by a separate field.
        /// </summary>
        private static List<Models.ProductImage> BuildProductImages(List<ProductImageRequest> images)
        {
            var now = DateTime.UtcNow;

            return images
                .Select((image, index) => new Models.ProductImage
                {
                    Id = Guid.NewGuid(),
                    Url = image.Url.Trim(),
                    IsMain = image.IsMain,
                    Width = image.Width,
                    Height = image.Height,
                    AltText = string.IsNullOrWhiteSpace(image.AltText) ? null : image.AltText.Trim(),
                    DisplayOrder = index,
                    CreatedAt = now,
                })
                .ToList();
        }

        /// <summary>
        /// Blank and whitespace-only SKUs are stored as null, same reasoning as
        /// NormalizeImageUrl — and it matters more here, since the unique index treats
        /// every null as distinct but would happily reject a second empty string.
        /// </summary>
        private static string? NormalizeSku(string? sku) =>
            string.IsNullOrWhiteSpace(sku) ? null : sku.Trim();

        /// <summary>
        /// Trims each tag and drops blanks/duplicates. A null request means "no tags
        /// submitted", which normalizes to the same empty list as an explicitly empty
        /// one — Product.Tags is never null.
        /// </summary>
        private static List<string> NormalizeTags(List<string>? tags) =>
            tags is null
                ? []
                : tags
                    .Select(t => t.Trim())
                    .Where(t => t.Length > 0)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

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
