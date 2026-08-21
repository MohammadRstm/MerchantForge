using MerchForge.api.Data;
using MerchForge.api.DTOs.Storefront;
using MerchForge.api.Enums;
using MerchForge.api.Models;
using MerchForge.api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MerchForge.api.Repositories.Implementations
{
    public class StorefrontRepository : IStorefrontRepository
    {
        private readonly MerchForgeDbContext _db;

        public StorefrontRepository(MerchForgeDbContext db)
        {
            _db = db;
        }

        public async Task<StorefrontBusinessResponse?> GetBusinessAsync(
            Guid businessId,
            CancellationToken cancellationToken = default)
        {
            return await _db.Businesses
                .AsNoTracking()
                .Where(b => b.Id == businessId)
                .Select(b => new StorefrontBusinessResponse
                {
                    Id = b.Id,
                    Name = b.Name,
                    Description = b.Description,
                    LogoUrl = b.LogoUrl,
                    Currency = b.Currency,
                    Locale = b.Locale,
                    ContactEmail = b.ContactEmail,
                    ContactPhone = b.ContactPhone,
                    Domain = b.BusinessDomain == null
                        ? null
                        : new StorefrontDomainResponse
                        {
                            Id = b.BusinessDomain.Id,
                            Name = b.BusinessDomain.Name,
                            Slug = b.BusinessDomain.Slug,
                        },
                })
                .FirstOrDefaultAsync(cancellationToken);
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
                    CreatedAt = p.CreatedAt,
                })
                .ToListAsync(cancellationToken);

            return (items, totalCount);
        }

        public async Task<StorefrontProductDetailResponse?> GetProductAsync(
            Guid businessId,
            Guid productId,
            CancellationToken cancellationToken = default)
        {
            // Both predicates matter: matching on productId alone would let one
            // storefront read another business's product by id.
            return await _db.Products
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
                    CreatedAt = p.CreatedAt,
                })
                .FirstOrDefaultAsync(cancellationToken);
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

            return await _db.Products
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
                    CreatedAt = p.CreatedAt,
                })
                .ToListAsync(cancellationToken);
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
