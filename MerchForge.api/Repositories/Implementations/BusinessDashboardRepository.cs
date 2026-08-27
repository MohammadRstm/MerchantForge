using System.Text.Json;
using MerchForge.api.Data;
using MerchForge.api.DTOs.BusinessDashboard;
using MerchForge.api.DTOs.Common;
using MerchForge.api.Enums;
using MerchForge.api.Models;
using MerchForge.api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MerchForge.api.Repositories.Implementations
{
    public class BusinessDashboardRepository : IBusinessDashboardRepository
    {
        private readonly MerchForgeDbContext _db;

        public BusinessDashboardRepository(MerchForgeDbContext db)
        {
            _db = db;
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
            return await _db.Products
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
                    CreatedAt = p.CreatedAt,
                })
                .ToListAsync(cancellationToken);
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

                baseQuery = baseQuery.Where(p => EF.Functions.Like(p.Title, pattern));
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
                CreatedAt = p.CreatedAt,
            });

            projected = query.SortBy switch
            {
                ProductSortField.Title => query.SortDescending
                    ? projected.OrderByDescending(x => x.Title)
                    : projected.OrderBy(x => x.Title),

                ProductSortField.Price => query.SortDescending
                    ? projected.OrderByDescending(x => x.Price)
                    : projected.OrderBy(x => x.Price),

                _ => query.SortDescending
                    ? projected.OrderByDescending(x => x.CreatedAt)
                    : projected.OrderBy(x => x.CreatedAt),
            };

            var items = await projected
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync(cancellationToken);

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
            return await _db.Products
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
                    CreatedAt = p.CreatedAt,
                    UpdatedAt = p.UpdatedAt,
                })
                .FirstOrDefaultAsync(cancellationToken);
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
            CancellationToken cancellationToken = default)
        {
            return await _db.StockMovements
                .AsNoTracking()
                .Where(m => m.BusinessId == businessId)
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
    }
}
