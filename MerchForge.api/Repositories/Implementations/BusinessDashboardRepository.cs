using System.Text.Json;
using MerchForge.api.Data;
using MerchForge.api.DTOs.BusinessDashboard;
using MerchForge.api.DTOs.Common;
using MerchForge.api.Enums;
using MerchForge.api.Models;
using MerchForge.api.Repositories.Interfaces;
using MerchForge.api.Services.Storage.interfaces;
using Microsoft.EntityFrameworkCore;

namespace MerchForge.api.Repositories.Implementations
{
    public class BusinessDashboardRepository : IBusinessDashboardRepository
    {
        private readonly MerchForgeDbContext _db;
        private readonly IProductImageUrlResolver _productImageUrlResolver;

        public BusinessDashboardRepository(
            MerchForgeDbContext db,
            IProductImageUrlResolver productImageUrlResolver)
        {
            _db = db;
            _productImageUrlResolver = productImageUrlResolver;
        }

        /// <summary>
        /// Product images are persisted as object keys, not URLs, so every projection
        /// that hands one to a client has to turn it back into something loadable.
        ///
        /// Applied after materialization rather than inside the Select: EF cannot
        /// translate the resolver into SQL. Resolution is idempotent, so a value that
        /// somehow passes through twice still comes out right.
        /// </summary>
        private void ResolveProductImageUrls(IEnumerable<BusinessProductResponse> products)
        {
            foreach (var product in products)
            {
                product.ImageUrl = _productImageUrlResolver.ToPublicUrl(product.ImageUrl);
            }
        }

        public async Task<(string Name, DateTime CreatedAt, string? WebsiteUrl)?> GetBusinessSummaryAsync(
            Guid businessId,
            CancellationToken cancellationToken = default)
        {
            var business = await _db.Businesses
                .Where(b => b.Id == businessId)
                .Select(b => new { b.Name, b.CreatedAt, b.WebsiteUrl })
                .FirstOrDefaultAsync(cancellationToken);

            return business is null ? null : (business.Name, business.CreatedAt, business.WebsiteUrl);
        }

        public async Task<int> CountMembersAsync(Guid businessId, CancellationToken cancellationToken = default)
        {
            return await _db.BusinessUsers.CountAsync(bu => bu.BusinessId == businessId, cancellationToken);
        }

        public async Task<int> CountProductsAsync(Guid businessId, CancellationToken cancellationToken = default)
        {
            return await _db.Products.CountAsync(p => p.BusinessId == businessId, cancellationToken);
        }

        public async Task<int> CountProductDraftsAsync(Guid businessId, CancellationToken cancellationToken = default)
        {
            return await _db.ProductDrafts.CountAsync(d => d.BusinessId == businessId, cancellationToken);
        }

        public async Task<(decimal? Average, decimal? Min, decimal? Max)> GetProductPriceStatsAsync(
            Guid businessId,
            CancellationToken cancellationToken = default)
        {
            var stats = await _db.Products
                .Where(p => p.BusinessId == businessId)
                .GroupBy(p => 1)
                .Select(g => new
                {
                    Average = g.Average(p => p.Price),
                    Min = g.Min(p => p.Price),
                    Max = g.Max(p => p.Price),
                })
                .FirstOrDefaultAsync(cancellationToken);

            return stats is null ? (null, null, null) : (stats.Average, stats.Min, stats.Max);
        }

        public async Task<List<KeyCountResponse>> GetProductsByCategoryAsync(
            Guid businessId,
            CancellationToken cancellationToken = default)
        {
            return await _db.Products
                .Where(p => p.BusinessId == businessId)
                .GroupBy(p => p.Category.Name)
                .Select(g => new KeyCountResponse { Key = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);
        }

        public async Task<int> CountOutOfStockProductsAsync(Guid businessId, CancellationToken cancellationToken = default)
        {
            return await _db.Products
                .CountAsync(p => p.BusinessId == businessId && p.StockQuantity == 0, cancellationToken);
        }

        public async Task<List<BusinessProductResponse>> GetRecentProductsAsync(
            Guid businessId,
            int take,
            CancellationToken cancellationToken = default)
        {
            var products = await _db.Products
                .Where(p => p.BusinessId == businessId)
                .OrderByDescending(p => p.CreatedAt)
                .Take(take)
                .Select(p => new BusinessProductResponse
                {
                    Id = p.Id,
                    Title = p.Title,
                    Category = p.Category.Name,
                    Price = p.Price,
                    CompareAtPrice = p.CompareAtPrice,
                    ImageUrl = p.ImageUrl,
                    StockQuantity = p.StockQuantity,
                    Sku = p.Sku,
                    // Visible reviews only, so the owner sees the same rating a
                    // shopper does. Correlated subqueries rather than denormalized
                    // columns, matching the storefront projections.
                    AverageRating = _db.ProductReviews
                        .Where(r => r.ProductId == p.Id && !r.IsHidden)
                        .Average(r => (decimal?)r.Rating),
                    ReviewCount = _db.ProductReviews
                        .Count(r => r.ProductId == p.Id && !r.IsHidden),
                    CreatedAt = p.CreatedAt,
                    UpdatedAt = p.UpdatedAt,
                })
                .ToListAsync(cancellationToken);

            ResolveProductImageUrls(products);

            return products;
        }

        public async Task<List<KeyCountResponse>> GetProductDraftsByStatusAsync(
            Guid businessId,
            CancellationToken cancellationToken = default)
        {
            // Grouped in the database, named in memory: Status is now an enum, and
            // ToString() on it has no SQL translation. Same shape as
            // GetMembersByRoleAsync, which handles its enum the same way.
            var grouped = await _db.ProductDrafts
                .Where(d => d.BusinessId == businessId)
                .GroupBy(d => d.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);

            return grouped
                .Select(x => new KeyCountResponse { Key = x.Status.ToString(), Count = x.Count })
                .ToList();
        }

        public async Task<List<KeyCountResponse>> GetMembersByRoleAsync(
            Guid businessId,
            CancellationToken cancellationToken = default)
        {
            var grouped = await (
                from bu in _db.BusinessUsers
                join r in _db.BusinessUserRoles on bu.RoleId equals r.Id
                where bu.BusinessId == businessId
                group bu by r.Role into g
                select new { Role = g.Key, Count = g.Count() }
            ).ToListAsync(cancellationToken);

            return grouped
                .Select(x => new KeyCountResponse { Key = x.Role.ToString(), Count = x.Count })
                .ToList();
        }

        public async Task<List<DateTime>> GetProductCreationDatesSinceAsync(
            Guid businessId,
            DateTime since,
            CancellationToken cancellationToken = default)
        {
            return await _db.Products
                .Where(p => p.BusinessId == businessId && p.CreatedAt >= since)
                .Select(p => p.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<(List<BusinessProductResponse> Items, int TotalCount)> GetProductsAsync(
            Guid businessId,
            ProductsQueryRequest query,
            int lowStockThreshold,
            CancellationToken cancellationToken = default)
        {
            var baseQuery = _db.Products.Where(p => p.BusinessId == businessId);

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var pattern = $"%{query.Search.Trim()}%";

                baseQuery = baseQuery.Where(p =>
                    EF.Functions.Like(p.Title, pattern) || (p.Sku != null && EF.Functions.Like(p.Sku, pattern)));
            }

            if (!string.IsNullOrWhiteSpace(query.Category))
            {
                // The dashboard filters by category name (its dropdown is populated
                // from the ProductsByCategory stat, which is name-keyed). Kept as-is
                // so the existing merchant UI keeps working; the storefront API uses
                // categoryId instead.
                baseQuery = baseQuery.Where(p => p.Category.Name == query.Category);
            }

            // Buckets are mutually exclusive — see ProductStockStatus's own doc comment.
            baseQuery = query.StockStatus switch
            {
                ProductStockStatus.Tracked => baseQuery.Where(p => p.StockQuantity != null),
                ProductStockStatus.Untracked => baseQuery.Where(p => p.StockQuantity == null),
                ProductStockStatus.OutOfStock => baseQuery.Where(p => p.StockQuantity == 0),
                ProductStockStatus.LowStock => baseQuery.Where(
                    p => p.StockQuantity != null && p.StockQuantity > 0 && p.StockQuantity <= lowStockThreshold),
                ProductStockStatus.InStock => baseQuery.Where(p => p.StockQuantity > lowStockThreshold),
                _ => baseQuery,
            };

            var totalCount = await baseQuery.CountAsync(cancellationToken);

            var projected = baseQuery.Select(p => new BusinessProductResponse
            {
                Id = p.Id,
                Title = p.Title,
                Category = p.Category.Name,
                Price = p.Price,
                CompareAtPrice = p.CompareAtPrice,
                ImageUrl = p.ImageUrl,
                StockQuantity = p.StockQuantity,
                Sku = p.Sku,
                // Visible reviews only, so the owner sees the same rating a
                // shopper does. Correlated subqueries rather than denormalized
                // columns, matching the storefront projections.
                AverageRating = _db.ProductReviews
                    .Where(r => r.ProductId == p.Id && !r.IsHidden)
                    .Average(r => (decimal?)r.Rating),
                ReviewCount = _db.ProductReviews
                    .Count(r => r.ProductId == p.Id && !r.IsHidden),
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt,
            });

            projected = query.SortBy switch
            {
                ProductSortField.Title => query.SortDescending
                    ? projected.OrderByDescending(x => x.Title)
                    : projected.OrderBy(x => x.Title),

                ProductSortField.Price => query.SortDescending
                    ? projected.OrderByDescending(x => x.Price)
                    : projected.OrderBy(x => x.Price),

                // Nulls (untracked) sort last regardless of direction - an untracked
                // product has no stock level to rank by, so it shouldn't jump to the
                // front of a "lowest stock first" sort just because EF/SQL treats
                // NULL as smallest by default.
                ProductSortField.StockQuantity => query.SortDescending
                    ? projected.OrderByDescending(x => x.StockQuantity.HasValue).ThenByDescending(x => x.StockQuantity)
                    : projected.OrderByDescending(x => x.StockQuantity.HasValue).ThenBy(x => x.StockQuantity),

                ProductSortField.UpdatedAt => query.SortDescending
                    ? projected.OrderByDescending(x => x.UpdatedAt)
                    : projected.OrderBy(x => x.UpdatedAt),

                _ => query.SortDescending
                    ? projected.OrderByDescending(x => x.CreatedAt)
                    : projected.OrderBy(x => x.CreatedAt),
            };

            var items = await projected
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync(cancellationToken);

            ResolveProductImageUrls(items);

            return (items, totalCount);
        }

        public async Task<List<BusinessMemberResponse>> GetMembersAsync(
            Guid businessId,
            CancellationToken cancellationToken = default)
        {
            var members = await (
                from bu in _db.BusinessUsers
                join u in _db.Users on bu.UserId equals u.Id
                join r in _db.BusinessUserRoles on bu.RoleId equals r.Id
                where bu.BusinessId == businessId
                select new { u.Id, u.FirstName, u.LastName, u.Email, Role = r.Role, bu.CreatedAt }
            ).ToListAsync(cancellationToken);

            return members
                .OrderBy(m => m.Role)
                .ThenBy(m => m.FirstName)
                .Select(m => new BusinessMemberResponse
                {
                    UserId = m.Id,
                    FirstName = m.FirstName,
                    LastName = m.LastName,
                    Email = m.Email,
                    Role = m.Role.ToString(),
                    JoinedAt = m.CreatedAt,
                })
                .ToList();
        }

        public async Task CreateMemberAsync(
            User user,
            BusinessUser businessUser,
            CancellationToken cancellationToken = default)
        {
            await using var transaction =
                await _db.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                await _db.Users.AddAsync(user, cancellationToken);
                await _db.BusinessUsers.AddAsync(businessUser, cancellationToken);

                await _db.SaveChangesAsync(cancellationToken);

                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        // ---- product CRUD ----

        public async Task<BusinessProductDetailResponse?> GetProductAsync(
            Guid businessId,
            Guid productId,
            CancellationToken cancellationToken = default)
        {
            // Both predicates: matching on productId alone would let one business read
            // another's product.
            var product = await _db.Products
                .AsNoTracking()
                .Where(p => p.Id == productId && p.BusinessId == businessId)
                .Select(p => new BusinessProductDetailResponse
                {
                    Id = p.Id,
                    Title = p.Title,
                    Description = p.Description,
                    Price = p.Price,
                    CompareAtPrice = p.CompareAtPrice,
                    CategoryId = p.CategoryId,
                    CategoryName = p.Category.Name,
                    ImageUrl = p.ImageUrl,
                    Images = p.Images
                        .OrderBy(i => i.DisplayOrder)
                        .Select(i => new ProductImageResponse
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
                    Metadata = p.Metadata,
                    // Visible reviews only, matching the storefront's own rating.
                    AverageRating = _db.ProductReviews
                        .Where(r => r.ProductId == p.Id && !r.IsHidden)
                        .Average(r => (decimal?)r.Rating),
                    ReviewCount = _db.ProductReviews
                        .Count(r => r.ProductId == p.Id && !r.IsHidden),
                    CreatedAt = p.CreatedAt,
                    UpdatedAt = p.UpdatedAt,
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (product is not null)
            {
                product.ImageUrl = _productImageUrlResolver.ToPublicUrl(product.ImageUrl);

                foreach (var image in product.Images)
                {
                    image.Url = _productImageUrlResolver.ToPublicUrl(image.Url);
                }
            }

            return product;
        }

        public async Task<(JsonDocument? MetadataShape, List<ProductFormCategoryResponse> Categories)?> GetProductFormDataAsync(
            Guid businessId,
            CancellationToken cancellationToken = default)
        {
            var business = await _db.Businesses
                .AsNoTracking()
                .Where(b => b.Id == businessId)
                .Select(b => new { b.BusinessDomainId, b.MetadataShape })
                .FirstOrDefaultAsync(cancellationToken);

            if (business is null)
            {
                return null;
            }

            // A business with no domain has no categories, so it cannot have products
            // yet — an empty list is the honest answer rather than an error.
            var categories = business.BusinessDomainId is null
                ? []
                : await _db.Categories
                    .AsNoTracking()
                    .Where(c =>
                        c.IsActive &&
                        c.BusinessDomainId == business.BusinessDomainId &&
                        (c.BusinessId == null || c.BusinessId == businessId))
                    .OrderBy(c => c.DisplayOrder)
                    .ThenBy(c => c.Name)
                    .Select(c => new ProductFormCategoryResponse { Id = c.Id, Name = c.Name })
                    .ToListAsync(cancellationToken);

            return (business.MetadataShape, categories);
        }

        public async Task<bool> CanUseCategoryAsync(
            Guid businessId,
            Guid categoryId,
            CancellationToken cancellationToken = default)
        {
            // Enforces in one query what the schema cannot: the category must be in
            // this business's domain, and must be either shared platform data or this
            // business's own private category.
            return await _db.Categories
                .AsNoTracking()
                .AnyAsync(
                    c => c.Id == categoryId
                        && c.IsActive
                        && (c.BusinessId == null || c.BusinessId == businessId)
                        && _db.Businesses.Any(b =>
                            b.Id == businessId &&
                            b.BusinessDomainId == c.BusinessDomainId),
                    cancellationToken);
        }

        public async Task<Product> CreateProductAsync(
            Product product,
            CancellationToken cancellationToken = default)
        {
            await _db.Products.AddAsync(product, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);

            return product;
        }

        public async Task<Guid?> GetProductOwnerBusinessIdAsync(
            Guid productId,
            CancellationToken cancellationToken = default)
        {
            return await _db.Products
                .AsNoTracking()
                .Where(p => p.Id == productId)
                .Select(p => (Guid?)p.BusinessId)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<Product?> GetTrackedProductAsync(
            Guid businessId,
            Guid productId,
            CancellationToken cancellationToken = default)
        {
            // Images included and tracked: UpdateProductAsync does a full
            // Images.Clear()-then-rebuild, which needs the existing rows loaded to
            // know what to delete. Category included too: AdjustStockAsync's response
            // mapping reads product.Category.Name and there's no lazy-loading here.
            return await _db.Products
                .Include(p => p.Images)
                .Include(p => p.Category)
                .FirstOrDefaultAsync(
                    p => p.Id == productId && p.BusinessId == businessId,
                    cancellationToken);
        }

        public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task ReplaceProductImagesAsync(
            Product product,
            List<ProductImage> newImages,
            CancellationToken cancellationToken = default)
        {
            // Explicit Remove/Add rather than mutating product.Images in place
            // (Clear() + Add()). A freshly-constructed ProductImage already carries a
            // real, client-generated Guid — attaching it purely via navigation fixup
            // leaves EF's change tracker to guess whether that's a new row or an
            // existing one it just hasn't seen yet, and it guesses existing, producing
            // an UPDATE for a row that was never inserted. Going through the DbSet
            // directly is unambiguous: Added means INSERT, Removed means DELETE.
            _db.ProductImages.RemoveRange(product.Images);

            foreach (var image in newImages)
            {
                image.ProductId = product.Id;
                _db.ProductImages.Add(image);
            }

            // Also flushes whatever scalar changes the caller already made to
            // `product` before calling this — one transaction, one round trip.
            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task DeleteProductAsync(
            Product product,
            CancellationToken cancellationToken = default)
        {
            _db.Products.Remove(product);
            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task<ProductPerformanceResponse> GetProductPerformanceAsync(
            Guid businessId,
            DateTime from,
            DateTime to,
            CancellationToken cancellationToken = default)
        {
            var products = await _db.Products
                .Where(p => p.BusinessId == businessId)
                .Select(p => new
                {
                    p.Id,
                    p.Title,
                    p.ImageUrl,
                    CategoryName = p.Category.Name,
                    p.Price,
                    p.CreatedAt,
                })
                .ToListAsync(cancellationToken);

            var span = to - from;
            var previousTo = from.AddTicks(-1);
            var previousFrom = previousTo - span;

            var currentSales = await GetProductSalesByProductAsync(businessId, from, to, cancellationToken);
            var previousSales = await GetProductSalesByProductAsync(businessId, previousFrom, previousTo, cancellationToken);

            var entries = products
                .Select(p =>
                {
                    var current = currentSales.GetValueOrDefault(p.Id);
                    var previous = previousSales.GetValueOrDefault(p.Id);

                    return new ProductPerformanceEntryResponse
                    {
                        ProductId = p.Id,
                        Title = p.Title,
                        ImageUrl = _productImageUrlResolver.ToPublicUrl(p.ImageUrl),
                        CategoryName = p.CategoryName,
                        Price = p.Price,
                        UnitsSold = current.UnitsSold,
                        Revenue = current.Revenue,
                        OrderCount = current.OrderCount,
                        PreviousUnitsSold = previous.UnitsSold,
                        PreviousRevenue = previous.Revenue,
                        UnitsSoldChangePercent = previous.UnitsSold > 0
                            ? Math.Round((decimal)(current.UnitsSold - previous.UnitsSold) / previous.UnitsSold * 100, 1)
                            : null,
                        RevenueChangePercent = previous.Revenue > 0
                            ? Math.Round((current.Revenue - previous.Revenue) / previous.Revenue * 100, 1)
                            : null,
                        CreatedAt = p.CreatedAt,
                    };
                })
                .ToList();

            var categories = entries
                .GroupBy(e => e.CategoryName)
                .Select(g => new CategoryPerformanceEntryResponse
                {
                    CategoryName = g.Key,
                    ProductCount = g.Count(),
                    UnitsSold = g.Sum(e => e.UnitsSold),
                    Revenue = g.Sum(e => e.Revenue),
                })
                .OrderByDescending(c => c.Revenue)
                .ToList();

            return new ProductPerformanceResponse
            {
                Products = entries,
                Categories = categories,
                TotalRevenue = entries.Sum(e => e.Revenue),
            };
        }

        private async Task<Dictionary<Guid, (int UnitsSold, decimal Revenue, int OrderCount)>> GetProductSalesByProductAsync(
            Guid businessId,
            DateTime from,
            DateTime to,
            CancellationToken cancellationToken)
        {
            var rows = await _db.OrderItems
                .Where(i =>
                    i.Order.BusinessId == businessId &&
                    i.Order.Status != OrderStatus.Cancelled &&
                    i.Order.CreatedAt >= from &&
                    i.Order.CreatedAt <= to)
                .GroupBy(i => i.ProductId)
                .Select(g => new
                {
                    ProductId = g.Key,
                    UnitsSold = g.Sum(i => i.Quantity),
                    Revenue = g.Sum(i => i.LineTotal),
                    OrderCount = g.Select(i => i.OrderId).Distinct().Count(),
                })
                .ToListAsync(cancellationToken);

            return rows.ToDictionary(r => r.ProductId, r => (r.UnitsSold, r.Revenue, r.OrderCount));
        }

        // ---- website template ----

        public async Task<(Guid? BusinessDomainId, string? DomainName, Guid? WebsiteTemplateId, string? WebsiteTemplateName,
            string? WebsiteTemplateLabel, string? WebsiteTemplatePreviewImageUrl, DateTime? WebsiteTemplateChosenAt)?>
            GetBusinessWebsiteTemplateInfoAsync(Guid businessId, CancellationToken cancellationToken = default)
        {
            var info = await _db.Businesses
                .Where(b => b.Id == businessId)
                .Select(b => new
                {
                    b.BusinessDomainId,
                    DomainName = b.BusinessDomain != null ? b.BusinessDomain.Name : null,
                    b.WebsiteTemplateId,
                    WebsiteTemplateName = b.WebsiteTemplate != null ? b.WebsiteTemplate.Name : null,
                    WebsiteTemplateLabel = b.WebsiteTemplate != null ? b.WebsiteTemplate.Label : null,
                    WebsiteTemplatePreviewImageUrl = b.WebsiteTemplate != null ? b.WebsiteTemplate.PreviewImageUrl : null,
                    b.WebsiteTemplateChosenAt,
                })
                .FirstOrDefaultAsync(cancellationToken);

            return info is null
                ? null
                : (info.BusinessDomainId, info.DomainName, info.WebsiteTemplateId, info.WebsiteTemplateName,
                    info.WebsiteTemplateLabel, info.WebsiteTemplatePreviewImageUrl, info.WebsiteTemplateChosenAt);
        }

        public async Task<List<WebsiteTemplateOptionResponse>> GetActiveWebsiteTemplatesByDomainAsync(
            Guid businessDomainId,
            CancellationToken cancellationToken = default)
        {
            return await _db.WebsiteTemplates
                .AsNoTracking()
                .Where(t => t.BusinessDomainId == businessDomainId && t.IsActive)
                .OrderBy(t => t.DisplayOrder)
                .ThenBy(t => t.Label)
                .Select(t => new WebsiteTemplateOptionResponse
                {
                    Id = t.Id,
                    Name = t.Name,
                    Label = t.Label,
                    PreviewImageUrl = t.PreviewImageUrl,
                    PreviewWebsiteUrl = t.PreviewWebsiteUrl,
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<WebsiteTemplateOptionResponse?> GetActiveWebsiteTemplateInDomainAsync(
            Guid websiteTemplateId,
            Guid businessDomainId,
            CancellationToken cancellationToken = default)
        {
            return await _db.WebsiteTemplates
                .AsNoTracking()
                .Where(t => t.Id == websiteTemplateId && t.BusinessDomainId == businessDomainId && t.IsActive)
                .Select(t => new WebsiteTemplateOptionResponse
                {
                    Id = t.Id,
                    Name = t.Name,
                    Label = t.Label,
                    PreviewImageUrl = t.PreviewImageUrl,
                    PreviewWebsiteUrl = t.PreviewWebsiteUrl,
                })
                .FirstOrDefaultAsync(cancellationToken);
        }

        // ---- inventory ----

        public async Task<int?> GetLowStockThresholdAsync(Guid businessId, CancellationToken cancellationToken = default)
        {
            return await _db.Businesses
                .AsNoTracking()
                .Where(b => b.Id == businessId)
                .Select(b => (int?)b.LowStockThreshold)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<bool> UpdateLowStockThresholdAsync(
            Guid businessId,
            int lowStockThreshold,
            CancellationToken cancellationToken = default)
        {
            var business = await _db.Businesses
                .FirstOrDefaultAsync(b => b.Id == businessId, cancellationToken);

            if (business is null)
            {
                return false;
            }

            business.LowStockThreshold = lowStockThreshold;
            business.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(cancellationToken);

            return true;
        }

        public async Task<StockMovement?> AdjustStockAsync(
            Product product,
            int amount,
            string? reason,
            Guid createdByUserId,
            CancellationToken cancellationToken = default)
        {
            var newQuantity = (product.StockQuantity ?? 0) + amount;

            if (newQuantity < 0)
            {
                return null;
            }

            var now = DateTime.UtcNow;

            product.StockQuantity = newQuantity;
            product.UpdatedAt = now;

            var movement = new StockMovement
            {
                Id = Guid.NewGuid(),
                ProductId = product.Id,
                BusinessId = product.BusinessId,
                Amount = amount,
                BalanceAfter = newQuantity,
                Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim(),
                CreatedByUserId = createdByUserId,
                CreatedAt = now,
            };

            await _db.StockMovements.AddAsync(movement, cancellationToken);

            // One SaveChangesAsync call, so the stock update and its ledger entry land
            // in the same implicit transaction — same reasoning as
            // FeatureCreditRepository.GrantCreditsAsync.
            await _db.SaveChangesAsync(cancellationToken);

            return movement;
        }

        public async Task<InventorySummaryResponse> GetInventorySummaryAsync(
            Guid businessId,
            int lowStockThreshold,
            CancellationToken cancellationToken = default)
        {
            var stats = await _db.Products
                .Where(p => p.BusinessId == businessId)
                .GroupBy(p => 1)
                .Select(g => new
                {
                    Tracked = g.Count(p => p.StockQuantity != null),
                    Untracked = g.Count(p => p.StockQuantity == null),
                    OutOfStock = g.Count(p => p.StockQuantity == 0),
                    LowStock = g.Count(p => p.StockQuantity != null && p.StockQuantity > 0 && p.StockQuantity <= lowStockThreshold),
                    TotalUnits = g.Sum(p => p.StockQuantity ?? 0),
                })
                .FirstOrDefaultAsync(cancellationToken);

            return new InventorySummaryResponse
            {
                TrackedProductCount = stats?.Tracked ?? 0,
                UntrackedProductCount = stats?.Untracked ?? 0,
                TotalUnitsInStock = stats?.TotalUnits ?? 0,
                OutOfStockCount = stats?.OutOfStock ?? 0,
                LowStockCount = stats?.LowStock ?? 0,
                LowStockThreshold = lowStockThreshold,
            };
        }

        public async Task<List<StockMovementResponse>> GetRecentStockMovementsAsync(
            Guid businessId,
            int take,
            Guid? productId = null,
            CancellationToken cancellationToken = default)
        {
            return await _db.StockMovements
                .AsNoTracking()
                .Where(m => m.BusinessId == businessId && (productId == null || m.ProductId == productId))
                .OrderByDescending(m => m.CreatedAt)
                .Take(take)
                .Join(
                    _db.Products,
                    m => m.ProductId,
                    p => p.Id,
                    (m, p) => new StockMovementResponse
                    {
                        Id = m.Id,
                        ProductId = m.ProductId,
                        ProductTitle = p.Title,
                        Amount = m.Amount,
                        BalanceAfter = m.BalanceAfter,
                        Reason = m.Reason,
                        CreatedAt = m.CreatedAt,
                    })
                .ToListAsync(cancellationToken);
        }

        public async Task<InventoryAnalyticsResponse> GetInventoryAnalyticsAsync(
            Guid businessId,
            DateTime from,
            DateTime to,
            CancellationToken cancellationToken = default)
        {
            var granularity = (to - from).TotalDays <= 31
                ? OrderAnalyticsGranularity.Daily
                : OrderAnalyticsGranularity.Monthly;

            var unitsSoldByPeriod = await GetUnitsSoldByPeriodAsync(businessId, from, to, granularity, cancellationToken);
            var movementsByPeriod = await GetStockMovementsByPeriodAsync(businessId, from, to, granularity, cancellationToken);

            // Union of both sources' periods - a bucket can have sales with no manual
            // stock movement that day, or vice versa (a restock with no sales yet).
            var periods = unitsSoldByPeriod.Keys.Union(movementsByPeriod.Keys).OrderBy(p => p);

            var points = periods
                .Select(period =>
                {
                    var (added, removed) = movementsByPeriod.GetValueOrDefault(period);
                    return new InventoryAnalyticsPointResponse
                    {
                        Period = period,
                        UnitsSold = unitsSoldByPeriod.GetValueOrDefault(period),
                        StockAdded = added,
                        StockRemoved = removed,
                    };
                })
                .ToList();

            var currentTotals = new InventoryAnalyticsPeriodTotalsResponse
            {
                UnitsSold = points.Sum(p => p.UnitsSold),
                StockAdded = points.Sum(p => p.StockAdded),
                StockRemoved = points.Sum(p => p.StockRemoved),
            };

            var span = to - from;
            var previousTo = from.AddTicks(-1);
            var previousFrom = previousTo - span;

            var previousTotals = await GetInventoryPeriodTotalsAsync(businessId, previousFrom, previousTo, cancellationToken);

            return new InventoryAnalyticsResponse
            {
                Granularity = granularity,
                Points = points,
                CurrentPeriod = currentTotals,
                PreviousPeriod = previousTotals,
                UnitsSoldChangePercent = previousTotals.UnitsSold > 0
                    ? Math.Round((decimal)(currentTotals.UnitsSold - previousTotals.UnitsSold) / previousTotals.UnitsSold * 100, 1)
                    : null,
            };
        }

        private async Task<Dictionary<DateTime, int>> GetUnitsSoldByPeriodAsync(
            Guid businessId,
            DateTime from,
            DateTime to,
            OrderAnalyticsGranularity granularity,
            CancellationToken cancellationToken)
        {
            var baseQuery = _db.OrderItems.Where(i =>
                i.Order.BusinessId == businessId &&
                i.Order.Status != OrderStatus.Cancelled &&
                i.Order.CreatedAt >= from &&
                i.Order.CreatedAt <= to);

            if (granularity == OrderAnalyticsGranularity.Daily)
            {
                var rows = await baseQuery
                    .GroupBy(i => i.Order.CreatedAt.Date)
                    .Select(g => new { Period = g.Key, UnitsSold = g.Sum(i => i.Quantity) })
                    .ToListAsync(cancellationToken);

                return rows.ToDictionary(r => r.Period, r => r.UnitsSold);
            }

            // Reconstructing a DateTime from g.Key.Year/g.Key.Month inside the
            // server-evaluated Select isn't translatable by the MySQL provider - see
            // the identical fix in OrderRepository.GetProductAnalyticsAsync.
            var monthlyRows = await baseQuery
                .GroupBy(i => new { i.Order.CreatedAt.Year, i.Order.CreatedAt.Month })
                .Select(g => new { g.Key.Year, g.Key.Month, UnitsSold = g.Sum(i => i.Quantity) })
                .ToListAsync(cancellationToken);

            return monthlyRows.ToDictionary(r => new DateTime(r.Year, r.Month, 1), r => r.UnitsSold);
        }

        private async Task<Dictionary<DateTime, (int Added, int Removed)>> GetStockMovementsByPeriodAsync(
            Guid businessId,
            DateTime from,
            DateTime to,
            OrderAnalyticsGranularity granularity,
            CancellationToken cancellationToken)
        {
            var baseQuery = _db.StockMovements.Where(m =>
                m.BusinessId == businessId && m.CreatedAt >= from && m.CreatedAt <= to);

            if (granularity == OrderAnalyticsGranularity.Daily)
            {
                var rows = await baseQuery
                    .GroupBy(m => m.CreatedAt.Date)
                    .Select(g => new
                    {
                        Period = g.Key,
                        Added = g.Where(m => m.Amount > 0).Sum(m => (int?)m.Amount) ?? 0,
                        Removed = g.Where(m => m.Amount < 0).Sum(m => (int?)m.Amount) ?? 0,
                    })
                    .ToListAsync(cancellationToken);

                return rows.ToDictionary(r => r.Period, r => (r.Added, -r.Removed));
            }

            var monthlyRows = await baseQuery
                .GroupBy(m => new { m.CreatedAt.Year, m.CreatedAt.Month })
                .Select(g => new
                {
                    g.Key.Year,
                    g.Key.Month,
                    Added = g.Where(m => m.Amount > 0).Sum(m => (int?)m.Amount) ?? 0,
                    Removed = g.Where(m => m.Amount < 0).Sum(m => (int?)m.Amount) ?? 0,
                })
                .ToListAsync(cancellationToken);

            return monthlyRows.ToDictionary(r => new DateTime(r.Year, r.Month, 1), r => (r.Added, -r.Removed));
        }

        private async Task<InventoryAnalyticsPeriodTotalsResponse> GetInventoryPeriodTotalsAsync(
            Guid businessId,
            DateTime from,
            DateTime to,
            CancellationToken cancellationToken)
        {
            var unitsSold = await _db.OrderItems
                .Where(i =>
                    i.Order.BusinessId == businessId &&
                    i.Order.Status != OrderStatus.Cancelled &&
                    i.Order.CreatedAt >= from &&
                    i.Order.CreatedAt <= to)
                .SumAsync(i => (int?)i.Quantity, cancellationToken) ?? 0;

            var movements = await _db.StockMovements
                .Where(m => m.BusinessId == businessId && m.CreatedAt >= from && m.CreatedAt <= to)
                .GroupBy(m => 1)
                .Select(g => new
                {
                    Added = g.Where(m => m.Amount > 0).Sum(m => (int?)m.Amount) ?? 0,
                    Removed = g.Where(m => m.Amount < 0).Sum(m => (int?)m.Amount) ?? 0,
                })
                .FirstOrDefaultAsync(cancellationToken);

            return new InventoryAnalyticsPeriodTotalsResponse
            {
                UnitsSold = unitsSold,
                StockAdded = movements?.Added ?? 0,
                StockRemoved = -(movements?.Removed ?? 0),
            };
        }

        public async Task<InventoryPerformanceResponse> GetInventoryPerformanceAsync(
            Guid businessId,
            DateTime from,
            DateTime to,
            int lowStockThreshold,
            CancellationToken cancellationToken = default)
        {
            var products = await _db.Products
                .Where(p => p.BusinessId == businessId)
                .Select(p => new
                {
                    p.Id,
                    p.Title,
                    p.ImageUrl,
                    CategoryName = p.Category.Name,
                    p.StockQuantity,
                    p.CreatedAt,
                })
                .ToListAsync(cancellationToken);

            var sales = await GetProductSalesByProductAsync(businessId, from, to, cancellationToken);

            // All-time last-sale date per product, independent of the selected period -
            // "dead stock" framing shouldn't flip depending on which range is picked.
            var lastSaleByProduct = await _db.OrderItems
                .Where(i => i.Order.BusinessId == businessId && i.Order.Status != OrderStatus.Cancelled)
                .GroupBy(i => i.ProductId)
                .Select(g => new { ProductId = g.Key, LastSaleAt = g.Max(i => i.Order.CreatedAt) })
                .ToDictionaryAsync(r => r.ProductId, r => (DateTime?)r.LastSaleAt, cancellationToken);

            var entries = products
                .Select(p =>
                {
                    var sale = sales.GetValueOrDefault(p.Id);

                    return new InventoryProductPerformanceEntryResponse
                    {
                        ProductId = p.Id,
                        Title = p.Title,
                        ImageUrl = _productImageUrlResolver.ToPublicUrl(p.ImageUrl),
                        CategoryName = p.CategoryName,
                        StockQuantity = p.StockQuantity,
                        UnitsSold = sale.UnitsSold,
                        Revenue = sale.Revenue,
                        LastSaleAt = lastSaleByProduct.GetValueOrDefault(p.Id),
                        CreatedAt = p.CreatedAt,
                    };
                })
                .ToList();

            var categories = entries
                .GroupBy(e => e.CategoryName)
                .Select(g => new InventoryCategoryPerformanceEntryResponse
                {
                    CategoryName = g.Key,
                    TrackedProductCount = g.Count(e => e.StockQuantity != null),
                    UntrackedProductCount = g.Count(e => e.StockQuantity == null),
                    UnitsInStock = g.Sum(e => e.StockQuantity ?? 0),
                    UnitsSold = g.Sum(e => e.UnitsSold),
                    Revenue = g.Sum(e => e.Revenue),
                    LowStockCount = g.Count(e => e.StockQuantity is int q && q > 0 && q <= lowStockThreshold),
                    OutOfStockCount = g.Count(e => e.StockQuantity == 0),
                })
                .OrderByDescending(c => c.UnitsInStock)
                .ToList();

            return new InventoryPerformanceResponse
            {
                Products = entries,
                Categories = categories,
            };
        }
    }
}
