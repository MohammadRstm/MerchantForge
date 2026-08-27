using MerchForge.api.Data;
using MerchForge.api.DTOs.Common;
using MerchForge.api.DTOs.Dashboard;
using MerchForge.api.Enums;
using MerchForge.api.Models;
using MerchForge.api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MerchForge.api.Repositories.Implementations
{
    public class DashboardRepository : IDashboardRepository
    {
        private readonly MerchForgeDbContext _db;

        public DashboardRepository(MerchForgeDbContext db)
        {
            _db = db;
        }

        public async Task<int> CountUsersAsync(CancellationToken cancellationToken = default)
        {
            return await _db.Users.CountAsync(cancellationToken);
        }

        public async Task<int> CountBusinessesAsync(CancellationToken cancellationToken = default)
        {
            return await _db.Businesses.CountAsync(cancellationToken);
        }

        public async Task<int> CountProductsAsync(CancellationToken cancellationToken = default)
        {
            return await _db.Products.CountAsync(cancellationToken);
        }

        public async Task<int> CountProductDraftsAsync(CancellationToken cancellationToken = default)
        {
            return await _db.ProductDrafts.CountAsync(cancellationToken);
        }

        public async Task<int> CountPendingInvitationsAsync(CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;

            return await _db.Invitations.CountAsync(
                i => i.AcceptedAt == null && i.RevokedAt == null && i.ExpiresAt > now,
                cancellationToken);
        }

        public async Task<(int Pending, int Completed)> GetWebsiteTemplateRequestStatusCountsAsync(CancellationToken cancellationToken = default)
        {
            var pending = await _db.WebsiteTemplateRequests
                .CountAsync(r =>
                    r.Status == WebsiteTemplateRequestStatus.Pending ||
                    r.Status == WebsiteTemplateRequestStatus.InProgress,
                    cancellationToken);

            var completed = await _db.WebsiteTemplateRequests
                .CountAsync(r => r.Status == WebsiteTemplateRequestStatus.Closed, cancellationToken);

            return (pending, completed);
        }

        public async Task<List<KeyCountResponse>> GetUserCountsBySystemRoleAsync(CancellationToken cancellationToken = default)
        {
            var grouped = await (
                from u in _db.Users
                join r in _db.SystemRoles on u.SystemRoleId equals r.Id
                group u by r.Role into g
                select new { Role = g.Key, Count = g.Count() }
            ).ToListAsync(cancellationToken);

            return grouped
                .Select(x => new KeyCountResponse { Key = x.Role.ToString(), Count = x.Count })
                .ToList();
        }

        public async Task<List<KeyCountResponse>> GetBusinessUserCountsByRoleAsync(CancellationToken cancellationToken = default)
        {
            var grouped = await (
                from bu in _db.BusinessUsers
                join r in _db.BusinessUserRoles on bu.RoleId equals r.Id
                group bu by r.Role into g
                select new { Role = g.Key, Count = g.Count() }
            ).ToListAsync(cancellationToken);

            return grouped
                .Select(x => new KeyCountResponse { Key = x.Role.ToString(), Count = x.Count })
                .ToList();
        }

        public async Task<List<DateTime>> GetBusinessCreationDatesSinceAsync(DateTime since, CancellationToken cancellationToken = default)
        {
            return await _db.Businesses
                .Where(b => b.CreatedAt >= since)
                .Select(b => b.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<DateTime>> GetProductCreationDatesSinceAsync(DateTime since, CancellationToken cancellationToken = default)
        {
            return await _db.Products
                .Where(p => p.CreatedAt >= since)
                .Select(p => p.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<(List<DashboardUserResponse> Items, int TotalCount)> GetUsersAsync(
            UsersQueryRequest query,
            CancellationToken cancellationToken = default)
        {
            var baseQuery =
                from u in _db.Users
                join r in _db.SystemRoles on u.SystemRoleId equals r.Id
                select new { User = u, Role = r.Role };

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var pattern = $"%{query.Search.Trim()}%";

                baseQuery = baseQuery.Where(x =>
                    EF.Functions.Like(x.User.FirstName, pattern) ||
                    EF.Functions.Like(x.User.LastName, pattern) ||
                    EF.Functions.Like(x.User.Email, pattern));
            }

            if (query.SystemRole.HasValue)
            {
                baseQuery = baseQuery.Where(x => x.Role == query.SystemRole.Value);
            }

            var totalCount = await baseQuery.CountAsync(cancellationToken);

            baseQuery = query.SortBy switch
            {
                UserSortField.Name => query.SortDescending
                    ? baseQuery.OrderByDescending(x => x.User.FirstName).ThenByDescending(x => x.User.LastName)
                    : baseQuery.OrderBy(x => x.User.FirstName).ThenBy(x => x.User.LastName),

                UserSortField.Email => query.SortDescending
                    ? baseQuery.OrderByDescending(x => x.User.Email)
                    : baseQuery.OrderBy(x => x.User.Email),

                _ => query.SortDescending
                    ? baseQuery.OrderByDescending(x => x.User.CreatedAt)
                    : baseQuery.OrderBy(x => x.User.CreatedAt),
            };

            var page = await baseQuery
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(x => new
                {
                    x.User.Id,
                    x.User.FirstName,
                    x.User.LastName,
                    x.User.Email,
                    x.User.CreatedAt,
                    SystemRole = x.Role
                })
                .ToListAsync(cancellationToken);

            var userIds = page.Select(x => x.Id).ToList();

            var memberships = await (
                from bu in _db.BusinessUsers
                join b in _db.Businesses on bu.BusinessId equals b.Id
                join bur in _db.BusinessUserRoles on bu.RoleId equals bur.Id
                where userIds.Contains(bu.UserId)
                select new { bu.UserId, BusinessName = b.Name, BusinessRole = bur.Role }
            ).ToListAsync(cancellationToken);

            var membershipLookup = memberships.ToDictionary(m => m.UserId);

            var now = DateTime.UtcNow;

            var activeSessionUserIds = (await _db.RefreshTokens
                .Where(rt => userIds.Contains(rt.UserId) && rt.RevokedAt == null && rt.ExpiresAt > now)
                .Select(rt => rt.UserId)
                .Distinct()
                .ToListAsync(cancellationToken))
                .ToHashSet();

            var items = page
                .Select(u => new DashboardUserResponse
                {
                    Id = u.Id,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    Email = u.Email,
                    SystemRole = u.SystemRole.ToString(),
                    BusinessName = membershipLookup.TryGetValue(u.Id, out var membership) ? membership.BusinessName : null,
                    BusinessRole = membershipLookup.TryGetValue(u.Id, out var membershipRole) ? membershipRole.BusinessRole.ToString() : null,
                    HasActiveSession = activeSessionUserIds.Contains(u.Id),
                    CreatedAt = u.CreatedAt,
                })
                .ToList();

            return (items, totalCount);
        }

        public async Task<bool> UserExistsAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return await _db.Users.AnyAsync(u => u.Id == userId, cancellationToken);
        }

        public async Task<(List<DashboardBusinessResponse> Items, int TotalCount)> GetBusinessesAsync(
            BusinessesQueryRequest query,
            CancellationToken cancellationToken = default)
        {
            var baseQuery = _db.Businesses.AsQueryable();

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var pattern = $"%{query.Search.Trim()}%";

                baseQuery = baseQuery.Where(b => EF.Functions.Like(b.Name, pattern));
            }

            var totalCount = await baseQuery.CountAsync(cancellationToken);

            var projected = baseQuery.Select(b => new DashboardBusinessResponse
            {
                Id = b.Id,
                Name = b.Name,
                OwnerFullName = b.Owner.FirstName + " " + b.Owner.LastName,
                OwnerEmail = b.Owner.Email,
                MemberCount = b.Members.Count,
                ProductCount = b.Products.Count,
                CreatedAt = b.CreatedAt,
            });

            projected = query.SortBy switch
            {
                BusinessSortField.Name => query.SortDescending
                    ? projected.OrderByDescending(x => x.Name)
                    : projected.OrderBy(x => x.Name),

                BusinessSortField.MemberCount => query.SortDescending
                    ? projected.OrderByDescending(x => x.MemberCount)
                    : projected.OrderBy(x => x.MemberCount),

                BusinessSortField.ProductCount => query.SortDescending
                    ? projected.OrderByDescending(x => x.ProductCount)
                    : projected.OrderBy(x => x.ProductCount),

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

        public async Task<Business?> GetBusinessDetailCoreAsync(Guid businessId, CancellationToken cancellationToken = default)
        {
            return await _db.Businesses
                .AsNoTracking()
                .Include(b => b.Owner)
                .Include(b => b.BusinessDomain)
                .Include(b => b.WebsiteTemplate)
                .FirstOrDefaultAsync(b => b.Id == businessId, cancellationToken);
        }

        public async Task<Business?> GetTrackedBusinessAsync(Guid businessId, CancellationToken cancellationToken = default)
        {
            return await _db.Businesses
                .FirstOrDefaultAsync(b => b.Id == businessId, cancellationToken);
        }

        public async Task<List<ProductAttributeDefinition>> GetActiveAttributeDefinitionsForDomainAsync(
            Guid businessDomainId,
            CancellationToken cancellationToken = default)
        {
            return await _db.ProductAttributeDefinitions
                .AsNoTracking()
                .Where(d => d.BusinessDomainId == businessDomainId && d.IsActive)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<BusinessFeatureCreditResponse>> GetBusinessFeatureCreditsAsync(
            Guid businessId,
            CancellationToken cancellationToken = default)
        {
            return await _db.BusinessFeatureCredits
                .AsNoTracking()
                .Where(fc => fc.BusinessId == businessId)
                .Select(fc => new BusinessFeatureCreditResponse
                {
                    FeatureKey = fc.Feature.Key,
                    FeatureName = fc.Feature.Name,
                    CreditsRemaining = fc.CreditsRemaining,
                    CreditsGrantedTotal = fc.CreditsGrantedTotal,
                })
                .ToListAsync(cancellationToken);
        }

        // ---- website templates ----

        public async Task<List<WebsiteTemplateResponse>> GetWebsiteTemplatesAsync(CancellationToken cancellationToken = default)
        {
            return await _db.WebsiteTemplates
                .AsNoTracking()
                .OrderBy(t => t.BusinessDomain.Name)
                .ThenBy(t => t.DisplayOrder)
                .Select(t => new WebsiteTemplateResponse
                {
                    Id = t.Id,
                    BusinessDomainId = t.BusinessDomainId,
                    DomainName = t.BusinessDomain.Name,
                    Name = t.Name,
                    Label = t.Label,
                    VideoPreviewUrl = t.VideoPreviewUrl,
                    PreviewWebsiteUrl = t.PreviewWebsiteUrl,
                    IsActive = t.IsActive,
                    DisplayOrder = t.DisplayOrder,
                    BusinessesUsingIt = t.Businesses.Count,
                    CreatedAt = t.CreatedAt,
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<bool> WebsiteTemplateNameExistsAsync(string name, CancellationToken cancellationToken = default)
        {
            return await _db.WebsiteTemplates.AnyAsync(t => t.Name == name, cancellationToken);
        }

        public async Task<WebsiteTemplate> CreateWebsiteTemplateAsync(
            WebsiteTemplate template,
            CancellationToken cancellationToken = default)
        {
            _db.WebsiteTemplates.Add(template);
            await _db.SaveChangesAsync(cancellationToken);

            return template;
        }

        public async Task<WebsiteTemplateDetailResponse?> GetWebsiteTemplateDetailAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            return await _db.WebsiteTemplates
                .AsNoTracking()
                .Where(t => t.Id == id)
                .Select(t => new WebsiteTemplateDetailResponse
                {
                    Id = t.Id,
                    BusinessDomainId = t.BusinessDomainId,
                    DomainName = t.BusinessDomain.Name,
                    Name = t.Name,
                    Label = t.Label,
                    VideoPreviewUrl = t.VideoPreviewUrl,
                    PreviewWebsiteUrl = t.PreviewWebsiteUrl,
                    IsActive = t.IsActive,
                    DisplayOrder = t.DisplayOrder,
                    CreatedAt = t.CreatedAt,
                    Businesses = t.Businesses
                        .Select(b => new WebsiteTemplateBusinessResponse { Id = b.Id, Name = b.Name })
                        .ToList(),
                })
                .FirstOrDefaultAsync(cancellationToken);
        }

        /// <summary>Loads a tracked entity for an update/deactivate mutation.</summary>
        public async Task<WebsiteTemplate?> GetTrackedWebsiteTemplateAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            return await _db.WebsiteTemplates
                .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        }

        public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}
