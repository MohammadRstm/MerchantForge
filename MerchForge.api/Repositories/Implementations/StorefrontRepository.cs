using System.Text.Json;
using MerchForge.api.Data;
using MerchForge.api.DTOs.Common;
using MerchForge.api.DTOs.Storefront;
using MerchForge.api.Enums;
using MerchForge.api.Models;
using MerchForge.api.Repositories.Interfaces;
using MerchForge.api.Services.Storage.interfaces;
using MerchForge.api.Services.Common;
using Microsoft.EntityFrameworkCore;

namespace MerchForge.api.Repositories.Implementations
{
    public class StorefrontRepository : IStorefrontRepository
    {
        private readonly MerchForgeDbContext _db;
        private readonly IStoredImageUrlResolver _productImageUrlResolver;

        public StorefrontRepository(
            MerchForgeDbContext db,
            IStoredImageUrlResolver productImageUrlResolver)
        {
            _db = db;
            _productImageUrlResolver = productImageUrlResolver;
        }

        public async Task<StorefrontBusinessResponse?> GetBusinessAsync(
            Guid businessId,
            CancellationToken cancellationToken = default)
        {
            // Fetched as the full entity (not a LINQ Select projection) specifically so
            // the JSON columns (SocialLinks/BusinessHours/WebsiteCustomizationValues)
            // materialize normally and can be converted to their public DTO shapes in
            // C# below -- a Select projection can't call into
            // WebsiteCustomizationValuesReader/JsonSerializer for that conversion.
            var business = await _db.Businesses
                .AsNoTracking()
                .Include(b => b.BusinessDomain)
                .FirstOrDefaultAsync(b => b.Id == businessId, cancellationToken);

            return business is null ? null : MapToStorefrontResponse(business);
        }

        public async Task<StorefrontBusinessResponse?> GetPreviewAsync(
            Guid businessId,
            string previewToken,
            CancellationToken cancellationToken = default)
        {
            var business = await _db.Businesses
                .AsNoTracking()
                .Include(b => b.BusinessDomain)
                .Include(b => b.WebsiteDraft)
                .FirstOrDefaultAsync(b => b.Id == businessId, cancellationToken);

            if (business?.WebsiteDraft is not { } draft || draft.PreviewToken != previewToken)
            {
                return null;
            }

            // Baseline for the fields never part of customization (Id/Name/Currency/
            // Locale/Domain), then every customization field is overwritten from the
            // draft instead of what's published.
            var response = MapToStorefrontResponse(business);

            response.Description = draft.Description;
            response.Tagline = draft.Tagline;
            response.LogoUrl = draft.LogoUrl;
            response.FaviconUrl = draft.FaviconUrl;
            response.ContactEmail = draft.ContactEmail;
            response.ContactPhone = draft.ContactPhone;
            response.WhatsAppNumber = draft.WhatsAppNumber;
            response.AddressLine1 = draft.AddressLine1;
            response.AddressLine2 = draft.AddressLine2;
            response.City = draft.City;
            response.State = draft.State;
            response.PostalCode = draft.PostalCode;
            response.Country = draft.Country;
            response.SocialLinks = ReadSocialLinks(draft.SocialLinks);
            response.BusinessHours = ReadBusinessHours(draft.BusinessHours);
            response.PrimaryColor = draft.PrimaryColor;
            // TemplateFieldsDraft is already just the current template's own flat
            // object (see BusinessWebsiteDraft's doc comment) -- no namespace lookup
            // needed here, unlike the published WebsiteCustomizationValues column.
            response.TemplateFields = ReadTemplateFields(draft.TemplateFieldsDraft);

            return response;
        }

        internal static StorefrontBusinessResponse MapToStorefrontResponse(Business business)
        {
            return new StorefrontBusinessResponse
            {
                Id = business.Id,
                Name = business.Name,
                Description = business.Description,
                Tagline = business.Tagline,
                LogoUrl = business.LogoUrl,
                FaviconUrl = business.FaviconUrl,
                Currency = business.Currency,
                Locale = business.Locale,
                ContactEmail = business.ContactEmail,
                ContactPhone = business.ContactPhone,
                WhatsAppNumber = business.WhatsAppNumber,
                AddressLine1 = business.AddressLine1,
                AddressLine2 = business.AddressLine2,
                City = business.City,
                State = business.State,
                PostalCode = business.PostalCode,
                Country = business.Country,
                SocialLinks = ReadSocialLinks(business.SocialLinks),
                BusinessHours = ReadBusinessHours(business.BusinessHours),
                PrimaryColor = business.PrimaryColor,
                TemplateFields = ReadTemplateFields(
                    WebsiteCustomizationValuesReader.ReadForTemplate(business.WebsiteCustomizationValues, business.WebsiteTemplateId)),
                Domain = business.BusinessDomain is null
                    ? null
                    : new StorefrontDomainResponse
                    {
                        Id = business.BusinessDomain.Id,
                        Name = business.BusinessDomain.Name,
                        Slug = business.BusinessDomain.Slug,
                    },
            };
        }

        private static SocialLinksDto ReadSocialLinks(JsonDocument? document) =>
            document is null
                ? new SocialLinksDto()
                : JsonSerializer.Deserialize<SocialLinksDto>(document.RootElement.GetRawText()) ?? new SocialLinksDto();

        private static BusinessHoursDto ReadBusinessHours(JsonDocument? document) =>
            document is null
                ? new BusinessHoursDto()
                : JsonSerializer.Deserialize<BusinessHoursDto>(document.RootElement.GetRawText()) ?? new BusinessHoursDto();

        private static Dictionary<string, JsonElement> ReadTemplateFields(JsonDocument? document)
        {
            var result = new Dictionary<string, JsonElement>();

            if (document is null || document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return result;
            }

            foreach (var property in document.RootElement.EnumerateObject())
            {
                result[property.Name] = property.Value.Clone();
            }

            return result;
        }

        public async Task<bool> BusinessExistsAsync(
            Guid businessId,
            CancellationToken cancellationToken = default)
        {
            return await _db.Businesses
                .AsNoTracking()
                .AnyAsync(b => b.Id == businessId, cancellationToken);
        }

        public async Task<List<StorefrontCategoryResponse>> GetCategoriesAsync(
            Guid businessId,
            CancellationToken cancellationToken = default)
        {
            // "This storefront's categories" = the platform categories of its domain
            // (BusinessId null) UNION its own custom categories (BusinessId ==
            // businessId). Explicitly excludes every OTHER business's custom
            // categories, even ones sharing this domain — those are private to
            // whoever created them.
            return await _db.Categories
                .AsNoTracking()
                .Where(c =>
                    c.IsActive &&
                    (c.BusinessId == null || c.BusinessId == businessId) &&
                    _db.Businesses.Any(b =>
                        b.Id == businessId &&
                        b.BusinessDomainId == c.BusinessDomainId))
                .OrderBy(c => c.DisplayOrder)
                .ThenBy(c => c.Name)
                .Select(c => new StorefrontCategoryResponse
                {
                    Id = c.Id,
                    Name = c.Name,
                    Slug = c.Slug,
                    DisplayOrder = c.DisplayOrder,
                    ProductCount = c.Products.Count(p => p.BusinessId == businessId),
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<(List<StorefrontProductResponse> Items, int TotalCount)> GetProductsAsync(
            Guid businessId,
            StorefrontProductsQueryRequest query,
            CancellationToken cancellationToken = default)
        {
            // The business filter is applied first and unconditionally — every other
            // filter narrows within it, so no combination of query parameters can
            // widen the result beyond this business.
            var baseQuery = _db.Products
                .AsNoTracking()
                .Where(p => p.BusinessId == businessId);

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var pattern = $"%{query.Search.Trim()}%";

                baseQuery = baseQuery.Where(p => EF.Functions.Like(p.Title, pattern));
            }

            if (query.CategoryId.HasValue)
            {
                baseQuery = baseQuery.Where(p => p.CategoryId == query.CategoryId.Value);
            }

            if (query.MinPrice.HasValue)
            {
                baseQuery = baseQuery.Where(p => p.Price >= query.MinPrice.Value);
            }

            if (query.MaxPrice.HasValue)
            {
                baseQuery = baseQuery.Where(p => p.Price <= query.MaxPrice.Value);
            }

            var totalCount = await baseQuery.CountAsync(cancellationToken);

            var sorted = ApplySort(baseQuery, query.SortBy, query.SortDescending);

            var items = await sorted
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(p => new StorefrontProductResponse
                {
                    Id = p.Id,
                    Title = p.Title,
                    Price = p.Price,
                    CompareAtPrice = p.CompareAtPrice,
                    ImageUrl = p.ImageUrl,
                    Images = p.Images
                        .OrderBy(i => i.DisplayOrder)
                        .Select(i => new StorefrontProductImageResponse
                        {
                            Id = i.Id,
                            Url = i.Url,
                            IsMain = i.IsMain,
                            Width = i.Width,
                            Height = i.Height,
                            AltText = i.AltText,
                            DisplayOrder = i.DisplayOrder,
                        })
                        .ToList(),
                    Sku = p.Sku,
                    StockQuantity = p.StockQuantity,
                    Tags = p.Tags,
                    SaleEndsAt = p.SaleEndsAt,
                    Category = new StorefrontProductCategoryResponse
                    {
                        Id = p.Category.Id,
                        Name = p.Category.Name,
                        Slug = p.Category.Slug,
                    },
                    Metadata = p.Metadata,
                    // Correlated subqueries rather than denormalized columns on Product,
                    // matching how StorefrontCategoryResponse.ProductCount is computed.
                    // The (decimal?) cast is what makes an unreviewed product come back
                    // as null instead of 0.
                    AverageRating = _db.ProductReviews
                        .Where(r => r.ProductId == p.Id && !r.IsHidden)
                        .Average(r => (decimal?)r.Rating),
                    ReviewCount = _db.ProductReviews
                        .Count(r => r.ProductId == p.Id && !r.IsHidden),
                    CreatedAt = p.CreatedAt,
                })
                .ToListAsync(cancellationToken);

            RoundAverageRatings(items);
            ResolveProductImageUrls(items);

            return (items, totalCount);
        }

        public async Task<StorefrontProductDetailResponse?> GetProductAsync(
            Guid businessId,
            Guid productId,
            CancellationToken cancellationToken = default)
        {
            // Both predicates matter: matching on productId alone would let one
            // storefront read another business's product by id.
            var product = await _db.Products
                .AsNoTracking()
                .Where(p => p.Id == productId && p.BusinessId == businessId)
                .Select(p => new StorefrontProductDetailResponse
                {
                    Id = p.Id,
                    Title = p.Title,
                    Description = p.Description,
                    Price = p.Price,
                    CompareAtPrice = p.CompareAtPrice,
                    ImageUrl = p.ImageUrl,
                    Images = p.Images
                        .OrderBy(i => i.DisplayOrder)
                        .Select(i => new StorefrontProductImageResponse
                        {
                            Id = i.Id,
                            Url = i.Url,
                            IsMain = i.IsMain,
                            Width = i.Width,
                            Height = i.Height,
                            AltText = i.AltText,
                            DisplayOrder = i.DisplayOrder,
                        })
                        .ToList(),
                    Sku = p.Sku,
                    StockQuantity = p.StockQuantity,
                    Tags = p.Tags,
                    SaleEndsAt = p.SaleEndsAt,
                    Category = new StorefrontProductCategoryResponse
                    {
                        Id = p.Category.Id,
                        Name = p.Category.Name,
                        Slug = p.Category.Slug,
                    },
                    Metadata = p.Metadata,
                    // Correlated subqueries rather than denormalized columns on Product,
                    // matching how StorefrontCategoryResponse.ProductCount is computed.
                    // The (decimal?) cast is what makes an unreviewed product come back
                    // as null instead of 0.
                    AverageRating = _db.ProductReviews
                        .Where(r => r.ProductId == p.Id && !r.IsHidden)
                        .Average(r => (decimal?)r.Rating),
                    ReviewCount = _db.ProductReviews
                        .Count(r => r.ProductId == p.Id && !r.IsHidden),
                    CreatedAt = p.CreatedAt,
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (product is not null)
            {
                product.AverageRating = RoundRating(product.AverageRating);

                product.ImageUrl = _productImageUrlResolver.ToPublicUrl(product.ImageUrl);

                foreach (var image in product.Images)
                {
                    image.Url = _productImageUrlResolver.ToPublicUrl(image.Url);
                }
            }

            return product;
        }

        public async Task<bool> ProductExistsAsync(
            Guid businessId,
            Guid productId,
            CancellationToken cancellationToken = default)
        {
            return await _db.Products
                .AsNoTracking()
                .AnyAsync(
                    p => p.Id == productId && p.BusinessId == businessId,
                    cancellationToken);
        }

        public async Task<List<StorefrontProductResponse>> GetRelatedProductsAsync(
            Guid businessId,
            Guid productId,
            int limit,
            CancellationToken cancellationToken = default)
        {
            // "Related" is deliberately the simplest rule the schema supports: same
            // business, same category, excluding the product itself. No recommendation
            // engine, no invented relevance scoring.
            var categoryId = await _db.Products
                .AsNoTracking()
                .Where(p => p.Id == productId && p.BusinessId == businessId)
                .Select(p => (Guid?)p.CategoryId)
                .FirstOrDefaultAsync(cancellationToken);

            if (categoryId is null)
            {
                return [];
            }

            var related = await _db.Products
                .AsNoTracking()
                .Where(p =>
                    p.BusinessId == businessId &&
                    p.CategoryId == categoryId.Value &&
                    p.Id != productId)
                .OrderByDescending(p => p.CreatedAt)
                .Take(limit)
                .Select(p => new StorefrontProductResponse
                {
                    Id = p.Id,
                    Title = p.Title,
                    Price = p.Price,
                    CompareAtPrice = p.CompareAtPrice,
                    ImageUrl = p.ImageUrl,
                    Images = p.Images
                        .OrderBy(i => i.DisplayOrder)
                        .Select(i => new StorefrontProductImageResponse
                        {
                            Id = i.Id,
                            Url = i.Url,
                            IsMain = i.IsMain,
                            Width = i.Width,
                            Height = i.Height,
                            AltText = i.AltText,
                            DisplayOrder = i.DisplayOrder,
                        })
                        .ToList(),
                    Sku = p.Sku,
                    StockQuantity = p.StockQuantity,
                    Tags = p.Tags,
                    SaleEndsAt = p.SaleEndsAt,
                    Category = new StorefrontProductCategoryResponse
                    {
                        Id = p.Category.Id,
                        Name = p.Category.Name,
                        Slug = p.Category.Slug,
                    },
                    Metadata = p.Metadata,
                    // Correlated subqueries rather than denormalized columns on Product,
                    // matching how StorefrontCategoryResponse.ProductCount is computed.
                    // The (decimal?) cast is what makes an unreviewed product come back
                    // as null instead of 0.
                    AverageRating = _db.ProductReviews
                        .Where(r => r.ProductId == p.Id && !r.IsHidden)
                        .Average(r => (decimal?)r.Rating),
                    ReviewCount = _db.ProductReviews
                        .Count(r => r.ProductId == p.Id && !r.IsHidden),
                    CreatedAt = p.CreatedAt,
                })
                .ToListAsync(cancellationToken);

            RoundAverageRatings(related);
            ResolveProductImageUrls(related);

            return related;
        }

        /// <summary>
        /// Rounds the review average to two places after the query rather than inside
        /// it. SQL's AVG over an integer column returns more precision than any
        /// storefront wants to render, but rounding in the projection would mean either
        /// running the aggregate twice or relying on Math.Round translating — this is
        /// cheaper and can't fail at query time. Matches what
        /// ProductReviewRepository.GetSummaryAsync does for the same figure.
        /// </summary>
        /// <summary>
        /// Product images are persisted as object keys, so a storefront gets a URL only
        /// once this has run. After the query for the same reason the rounding above is:
        /// EF has no translation for it.
        /// </summary>
        private void ResolveProductImageUrls(List<StorefrontProductResponse> products)
        {
            foreach (var product in products)
            {
                product.ImageUrl = _productImageUrlResolver.ToPublicUrl(product.ImageUrl);

                foreach (var image in product.Images)
                {
                    image.Url = _productImageUrlResolver.ToPublicUrl(image.Url);
                }
            }
        }

        private static void RoundAverageRatings(List<StorefrontProductResponse> products)
        {
            foreach (var product in products)
            {
                product.AverageRating = RoundRating(product.AverageRating);
            }
        }

        private static decimal? RoundRating(decimal? averageRating)
        {
            return averageRating is null
                ? null
                : Math.Round(averageRating.Value, 2, MidpointRounding.AwayFromZero);
        }

        private static IQueryable<Product> ApplySort(
            IQueryable<Product> query,
            ProductSortField sortBy,
            bool descending)
        {
            return sortBy switch
            {
                ProductSortField.Title => descending
                    ? query.OrderByDescending(p => p.Title)
                    : query.OrderBy(p => p.Title),

                ProductSortField.Price => descending
                    ? query.OrderByDescending(p => p.Price)
                    : query.OrderBy(p => p.Price),

                // Id is a stable tie-breaker: without it, paging over products sharing
                // a CreatedAt can repeat or skip rows between pages.
                _ => descending
                    ? query.OrderByDescending(p => p.CreatedAt).ThenBy(p => p.Id)
                    : query.OrderBy(p => p.CreatedAt).ThenBy(p => p.Id),
            };
        }
    }
}
