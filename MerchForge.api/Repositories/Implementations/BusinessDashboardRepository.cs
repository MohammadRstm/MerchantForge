using MerchForge.api.Data;
using MerchForge.api.DTOs.BusinessDashboard;
using MerchForge.api.DTOs.Common;
using MerchForge.api.Enums;
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

        public async Task<(string Name, DateTime CreatedAt)?> GetBusinessSummaryAsync(
            Guid businessId,
            CancellationToken cancellationToken = default)
        {
            var business = await _db.Businesses
                .Where(b => b.Id == businessId)
                .Select(b => new { b.Name, b.CreatedAt })
                .FirstOrDefaultAsync(cancellationToken);

            return business is null ? null : (business.Name, business.CreatedAt);
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

        public async Task<List<KeyCountResponse>> GetProductDraftsByStatusAsync(
            Guid businessId,
            CancellationToken cancellationToken = default)
        {
            return await _db.ProductDrafts
                .Where(d => d.BusinessId == businessId)
                .GroupBy(d => d.Status)
                .Select(g => new KeyCountResponse { Key = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);
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

            var totalCount = await baseQuery.CountAsync(cancellationToken);

            var projected = baseQuery.Select(p => new BusinessProductResponse
            {
                Id = p.Id,
                Title = p.Title,
                Category = p.Category.Name,
                Price = p.Price,
                ImageUrl = p.ImageUrl,
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
    }
}
