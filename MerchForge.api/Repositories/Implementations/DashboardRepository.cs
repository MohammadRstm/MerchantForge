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

        public async Task<List<KeyCountResponse>> GetBusinessCountsByDomainAsync(CancellationToken cancellationToken = default)
        {
            var grouped = await _db.Businesses
                .GroupBy(b => b.BusinessDomain != null ? b.BusinessDomain.Name : null)
                .Select(g => new { DomainName = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);

            return grouped
                .Select(x => new KeyCountResponse { Key = x.DomainName ?? "Unassigned", Count = x.Count })
                .ToList();
        }

        public async Task<List<KeyCountResponse>> GetSubscriptionStatusCountsAsync(CancellationToken cancellationToken = default)
        {
            var grouped = await _db.Subscriptions
                .GroupBy(s => s.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);

            return grouped
                .Select(x => new KeyCountResponse { Key = x.Status.ToString(), Count = x.Count })
                .ToList();
        }

        public async Task<int> CountActiveSessionsAsync(CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;

            return await _db.RefreshTokens
                .Where(rt => rt.RevokedAt == null && rt.ExpiresAt > now)
                .Select(rt => rt.UserId)
                .Distinct()
                .CountAsync(cancellationToken);
        }

        public async Task<int> CountOrdersAsync(CancellationToken cancellationToken = default)
        {
            return await _db.Orders.CountAsync(o => o.Status != OrderStatus.Cancelled, cancellationToken);
        }

        public async Task<int> CountBusinessesCreatedSinceAsync(DateTime since, CancellationToken cancellationToken = default)
        {
            return await _db.Businesses.CountAsync(b => b.CreatedAt >= since, cancellationToken);
        }

        public async Task<List<CurrencyTotalResponse>> GetRecordedOrderRevenueByCurrencyAsync(CancellationToken cancellationToken = default)
        {
            return await _db.Orders
                .Where(o => o.Status != OrderStatus.Cancelled)
                .GroupBy(o => o.Currency)
                .Select(g => new CurrencyTotalResponse
                {
                    Currency = g.Key,
                    Total = g.Sum(o => o.Total),
                    OrderCount = g.Count(),
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<List<DashboardBusinessResponse>> GetRecentBusinessesAsync(int take, CancellationToken cancellationToken = default)
        {
            return await _db.Businesses
                .OrderByDescending(b => b.CreatedAt)
                .Take(take)
                .Select(b => new DashboardBusinessResponse
                {
                    Id = b.Id,
                    Name = b.Name,
                    OwnerFullName = b.Owner.FirstName + " " + b.Owner.LastName,
                    OwnerEmail = b.Owner.Email,
                    MemberCount = b.Members.Count,
                    ProductCount = b.Products.Count,
                    CreatedAt = b.CreatedAt,
                })
                .ToListAsync(cancellationToken);
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
            var now = DateTime.UtcNow;

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
                    EF.Functions.Like(x.User.Email, pattern) ||
                    _db.BusinessUsers.Any(bu => bu.UserId == x.User.Id && EF.Functions.Like(bu.Business.Name, pattern)));
            }

            if (query.SystemRole.HasValue)
            {
                baseQuery = baseQuery.Where(x => x.Role == query.SystemRole.Value);
            }

            if (query.BusinessRole.HasValue)
            {
                // Each BusinessRole is seeded as exactly one BusinessUserRole row, so
                // resolving its id once and matching on RoleId avoids a nested EXISTS
                // with its own enum join.
                var roleId = await _db.BusinessUserRoles
                    .Where(r => r.Role == query.BusinessRole.Value)
                    .Select(r => r.Id)
                    .FirstOrDefaultAsync(cancellationToken);

                baseQuery = baseQuery.Where(x =>
                    _db.BusinessUsers.Any(bu => bu.UserId == x.User.Id && bu.RoleId == roleId));
            }

            if (query.HasActiveSession.HasValue)
            {
                var hasSession = query.HasActiveSession.Value;

                baseQuery = baseQuery.Where(x =>
                    _db.RefreshTokens.Any(rt => rt.UserId == x.User.Id && rt.RevokedAt == null && rt.ExpiresAt > now) == hasSession);
            }

            if (query.IsDisabled.HasValue)
            {
                var isDisabled = query.IsDisabled.Value;

                baseQuery = baseQuery.Where(x => (x.User.DisabledAt != null) == isDisabled);
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

                // Alphabetical by the persisted role string ("Admin" < "SuperAdmin" <
                // "User"), not by privilege level - a simple, predictable sort rather
                // than a bespoke severity ordering for a rarely-sorted column.
                UserSortField.SystemRole => query.SortDescending
                    ? baseQuery.OrderByDescending(x => x.Role)
                    : baseQuery.OrderBy(x => x.Role),

                UserSortField.HasActiveSession => query.SortDescending
                    ? baseQuery.OrderByDescending(x => _db.RefreshTokens.Any(rt => rt.UserId == x.User.Id && rt.RevokedAt == null && rt.ExpiresAt > now))
                    : baseQuery.OrderBy(x => _db.RefreshTokens.Any(rt => rt.UserId == x.User.Id && rt.RevokedAt == null && rt.ExpiresAt > now)),

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
                    x.User.DisabledAt,
                    SystemRole = x.Role
                })
                .ToListAsync(cancellationToken);

            var userIds = page.Select(x => x.Id).ToList();

            // Grouped, not ToDictionary-per-user - a user can belong to more than one
            // business (composite key on BusinessUser allows it), so a naive
            // single-row-per-user dictionary would throw the moment that happens.
            var membershipsByUser = (await (
                from bu in _db.BusinessUsers
                join b in _db.Businesses on bu.BusinessId equals b.Id
                join bur in _db.BusinessUserRoles on bu.RoleId equals bur.Id
                where userIds.Contains(bu.UserId)
                select new { bu.UserId, BusinessName = b.Name, BusinessRole = bur.Role, bu.CreatedAt }
            ).ToListAsync(cancellationToken))
                .GroupBy(m => m.UserId)
                .ToDictionary(g => g.Key, g => g.OrderBy(m => m.CreatedAt).ToList());

            var activeSessionUserIds = (await _db.RefreshTokens
                .Where(rt => userIds.Contains(rt.UserId) && rt.RevokedAt == null && rt.ExpiresAt > now)
                .Select(rt => rt.UserId)
                .Distinct()
                .ToListAsync(cancellationToken))
                .ToHashSet();

            var items = page
                .Select(u =>
                {
                    var memberships = membershipsByUser.TryGetValue(u.Id, out var list) ? list : null;
                    var primary = memberships?.FirstOrDefault();

                    return new DashboardUserResponse
                    {
                        Id = u.Id,
                        FirstName = u.FirstName,
                        LastName = u.LastName,
                        Email = u.Email,
                        SystemRole = u.SystemRole.ToString(),
                        BusinessName = primary?.BusinessName,
                        BusinessRole = primary?.BusinessRole.ToString(),
                        AdditionalMembershipCount = memberships is null ? 0 : Math.Max(0, memberships.Count - 1),
                        HasActiveSession = activeSessionUserIds.Contains(u.Id),
                        IsDisabled = u.DisabledAt != null,
                        CreatedAt = u.CreatedAt,
                    };
                })
                .ToList();

            return (items, totalCount);
        }

        public async Task<bool> UserExistsAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return await _db.Users.AnyAsync(u => u.Id == userId, cancellationToken);
        }

        public async Task<DashboardUserDetailResponse?> GetUserDetailAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var user = await (
                from u in _db.Users
                join r in _db.SystemRoles on u.SystemRoleId equals r.Id
                where u.Id == userId
                select new { User = u, Role = r.Role }
            ).AsNoTracking().FirstOrDefaultAsync(cancellationToken);

            if (user is null)
            {
                return null;
            }

            string? disabledByName = null;

            if (user.User.DisabledByUserId.HasValue)
            {
                disabledByName = await _db.Users
                    .Where(u => u.Id == user.User.DisabledByUserId.Value)
                    .Select(u => u.FirstName + " " + u.LastName)
                    .FirstOrDefaultAsync(cancellationToken);
            }

            var rawMemberships = await (
                from bu in _db.BusinessUsers
                join b in _db.Businesses on bu.BusinessId equals b.Id
                join bur in _db.BusinessUserRoles on bu.RoleId equals bur.Id
                where bu.UserId == userId
                orderby bu.CreatedAt
                select new { b.Id, b.Name, Role = bur.Role, bu.CreatedAt }
            ).ToListAsync(cancellationToken);

            var memberships = rawMemberships
                .Select(m => new UserMembershipResponse
                {
                    BusinessId = m.Id,
                    BusinessName = m.Name,
                    BusinessRole = m.Role.ToString(),
                    JoinedAt = m.CreatedAt,
                })
                .ToList();

            var now = DateTime.UtcNow;

            var activeSessionExpirations = await _db.RefreshTokens
                .Where(rt => rt.UserId == userId && rt.RevokedAt == null && rt.ExpiresAt > now)
                .Select(rt => rt.ExpiresAt)
                .ToListAsync(cancellationToken);

            return new DashboardUserDetailResponse
            {
                Id = user.User.Id,
                FirstName = user.User.FirstName,
                LastName = user.User.LastName,
                Email = user.User.Email,
                SystemRole = user.Role.ToString(),
                IsDisabled = user.User.DisabledAt != null,
                DisabledAt = user.User.DisabledAt,
                DisabledByName = disabledByName,
                CreatedAt = user.User.CreatedAt,
                UpdatedAt = user.User.UpdatedAt,
                Memberships = memberships,
                HasActiveSession = activeSessionExpirations.Count > 0,
                ActiveSessionCount = activeSessionExpirations.Count,
                NextSessionExpiresAt = activeSessionExpirations.Count > 0 ? activeSessionExpirations.Min() : null,
            };
        }

        public async Task<User?> GetTrackedUserAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
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

            var projected = baseQuery.Select(b => new
            {
                b.Id,
                b.Name,
                OwnerFullName = b.Owner.FirstName + " " + b.Owner.LastName,
                OwnerEmail = b.Owner.Email,
                DomainName = b.BusinessDomain != null ? b.BusinessDomain.Name : null,
                MemberCount = b.Members.Count,
                ProductCount = b.Products.Count,
                b.Currency,
                b.CreatedAt,
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

            var page = await projected
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync(cancellationToken);

            // Order/revenue and plan info are batch-fetched for just this page's
            // businesses (2 extra bounded queries, not one per row) - the same
            // pattern GetUsersAsync already uses for memberships/sessions below.
            var businessIds = page.Select(b => b.Id).ToList();

            var orderAggregates = (await _db.Orders
                .Where(o => businessIds.Contains(o.BusinessId) && o.Status != OrderStatus.Cancelled)
                .GroupBy(o => o.BusinessId)
                .Select(g => new
                {
                    BusinessId = g.Key,
                    Count = g.Count(),
                    Revenue = g.Sum(o => o.Total),
                    LastOrderAt = g.Max(o => o.CreatedAt),
                })
                .ToListAsync(cancellationToken))
                .ToDictionary(x => x.BusinessId);

            // Fetched, not translated to SQL as "latest per business" - grouped and
            // taken first (already CreatedAt-descending) in memory instead, since
            // enum properties like Status/BillingInterval can't be .ToString()'d
            // inside a query that still needs to execute in SQL.
            var latestSubscriptionByBusiness = (await _db.Subscriptions
                .Where(s => businessIds.Contains(s.BusinessId))
                .Include(s => s.SubscriptionPlan)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync(cancellationToken))
                .GroupBy(s => s.BusinessId)
                .ToDictionary(g => g.Key, g => g.First());

            var items = page
                .Select(b =>
                {
                    var hasOrders = orderAggregates.TryGetValue(b.Id, out var orders);
                    var hasSubscription = latestSubscriptionByBusiness.TryGetValue(b.Id, out var subscription);

                    return new DashboardBusinessResponse
                    {
                        Id = b.Id,
                        Name = b.Name,
                        OwnerFullName = b.OwnerFullName,
                        OwnerEmail = b.OwnerEmail,
                        DomainName = b.DomainName,
                        MemberCount = b.MemberCount,
                        ProductCount = b.ProductCount,
                        OrderCount = hasOrders ? orders!.Count : 0,
                        RecordedRevenue = hasOrders ? orders!.Revenue : 0m,
                        RevenueCurrency = b.Currency,
                        LastOrderAt = hasOrders ? orders!.LastOrderAt : null,
                        PlanName = hasSubscription ? subscription!.SubscriptionPlan.Name : null,
                        BillingInterval = hasSubscription ? subscription!.SubscriptionPlan.BillingInterval.ToString() : null,
                        SubscriptionStatus = hasSubscription ? subscription!.Status.ToString() : null,
                        CreatedAt = b.CreatedAt,
                    };
                })
                .ToList();

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

        public async Task<List<ProductAttributeDefinition>> GetAttributeDefinitionsAsync(
            Guid? businessDomainId,
            CancellationToken cancellationToken = default)
        {
            var query = _db.ProductAttributeDefinitions
                .AsNoTracking()
                .Include(d => d.BusinessDomain)
                .AsQueryable();

            if (businessDomainId.HasValue)
            {
                query = query.Where(d => d.BusinessDomainId == businessDomainId.Value);
            }

            return await query
                .OrderBy(d => d.BusinessDomain.Name)
                .ThenBy(d => d.DisplayOrder)
                .ToListAsync(cancellationToken);
        }

        public async Task<bool> AttributeDefinitionKeyExistsAsync(
            Guid businessDomainId,
            string key,
            CancellationToken cancellationToken = default)
        {
            return await _db.ProductAttributeDefinitions
                .AnyAsync(d => d.BusinessDomainId == businessDomainId && d.Key == key, cancellationToken);
        }

        public async Task CreateAttributeDefinitionAsync(
            ProductAttributeDefinition definition,
            CancellationToken cancellationToken = default)
        {
            _db.ProductAttributeDefinitions.Add(definition);
            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task<ProductAttributeDefinition?> GetTrackedAttributeDefinitionAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            return await _db.ProductAttributeDefinitions
                .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
        }

        public async Task<ProductAttributeDefinition?> GetAttributeDefinitionWithDomainAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            return await _db.ProductAttributeDefinitions
                .AsNoTracking()
                .Include(d => d.BusinessDomain)
                .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
        }

        public async Task<List<WebsiteTemplateCustomizableComponent>> GetActiveCustomizableComponentsForTemplateAsync(
            Guid websiteTemplateId,
            CancellationToken cancellationToken = default)
        {
            return await _db.WebsiteTemplateCustomizableComponents
                .AsNoTracking()
                .Where(c => c.WebsiteTemplateId == websiteTemplateId && c.IsActive)
                .OrderBy(c => c.DisplayOrder)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<WebsiteTemplateCustomizableComponent>> GetCustomizableComponentsAsync(
            Guid? websiteTemplateId,
            CancellationToken cancellationToken = default)
        {
            var query = _db.WebsiteTemplateCustomizableComponents
                .AsNoTracking()
                .Include(c => c.WebsiteTemplate)
                .AsQueryable();

            if (websiteTemplateId.HasValue)
            {
                query = query.Where(c => c.WebsiteTemplateId == websiteTemplateId.Value);
            }

            return await query
                .OrderBy(c => c.WebsiteTemplate.Name)
                .ThenBy(c => c.DisplayOrder)
                .ToListAsync(cancellationToken);
        }

        public async Task<bool> CustomizableComponentKeyExistsAsync(
            Guid websiteTemplateId,
            string key,
            CancellationToken cancellationToken = default)
        {
            return await _db.WebsiteTemplateCustomizableComponents
                .AnyAsync(c => c.WebsiteTemplateId == websiteTemplateId && c.Key == key, cancellationToken);
        }

        public async Task CreateCustomizableComponentAsync(
            WebsiteTemplateCustomizableComponent component,
            CancellationToken cancellationToken = default)
        {
            _db.WebsiteTemplateCustomizableComponents.Add(component);
            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task<WebsiteTemplateCustomizableComponent?> GetTrackedCustomizableComponentAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            return await _db.WebsiteTemplateCustomizableComponents
                .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        }

        public async Task<WebsiteTemplateCustomizableComponent?> GetCustomizableComponentWithTemplateAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            return await _db.WebsiteTemplateCustomizableComponents
                .AsNoTracking()
                .Include(c => c.WebsiteTemplate)
                .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
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
                    PreviewImageUrl = t.PreviewImageUrl,
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
                    PreviewImageUrl = t.PreviewImageUrl,
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

        // ---- customers ----

        public async Task<(List<DashboardCustomerResponse> Items, int TotalCount)> GetCustomersAsync(
            CustomersQueryRequest query,
            CancellationToken cancellationToken = default)
        {
            var baseQuery = _db.Customers.AsQueryable();

            if (query.BusinessId.HasValue)
            {
                var customerIdsForBusiness = _db.Orders
                    .Where(o => o.BusinessId == query.BusinessId.Value && o.CustomerId != null)
                    .Select(o => o.CustomerId!.Value)
                    .Distinct();

                baseQuery = baseQuery.Where(c => customerIdsForBusiness.Contains(c.Id));
            }

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var pattern = $"%{query.Search.Trim()}%";

                baseQuery = baseQuery.Where(c =>
                    EF.Functions.Like(c.FirstName, pattern) ||
                    EF.Functions.Like(c.LastName, pattern) ||
                    EF.Functions.Like(c.Email, pattern) ||
                    (c.Phone != null && EF.Functions.Like(c.Phone, pattern)));
            }

            if (query.HasOrders.HasValue)
            {
                var hasOrders = query.HasOrders.Value;

                baseQuery = baseQuery.Where(c =>
                    _db.Orders.Any(o => o.CustomerId == c.Id && o.Status != OrderStatus.Cancelled) == hasOrders);
            }

            if (query.RegisteredFrom.HasValue)
            {
                baseQuery = baseQuery.Where(c => c.CreatedAt >= query.RegisteredFrom.Value);
            }

            if (query.RegisteredTo.HasValue)
            {
                baseQuery = baseQuery.Where(c => c.CreatedAt <= query.RegisteredTo.Value);
            }

            var totalCount = await baseQuery.CountAsync(cancellationToken);

            baseQuery = query.SortBy switch
            {
                CustomerSortField.Name => query.SortDescending
                    ? baseQuery.OrderByDescending(c => c.FirstName).ThenByDescending(c => c.LastName)
                    : baseQuery.OrderBy(c => c.FirstName).ThenBy(c => c.LastName),

                CustomerSortField.Email => query.SortDescending
                    ? baseQuery.OrderByDescending(c => c.Email)
                    : baseQuery.OrderBy(c => c.Email),

                CustomerSortField.OrderCount => query.SortDescending
                    ? baseQuery.OrderByDescending(c => _db.Orders.Count(o => o.CustomerId == c.Id && o.Status != OrderStatus.Cancelled))
                    : baseQuery.OrderBy(c => _db.Orders.Count(o => o.CustomerId == c.Id && o.Status != OrderStatus.Cancelled)),

                // Sums across whatever currencies a customer happens to have ordered in -
                // correct for a *sort key* (it just needs a consistent ordering), even
                // though the same figure is never displayed as one collapsed total.
                CustomerSortField.TotalSpent => query.SortDescending
                    ? baseQuery.OrderByDescending(c => _db.Orders.Where(o => o.CustomerId == c.Id && o.Status != OrderStatus.Cancelled).Sum(o => (decimal?)o.Total) ?? 0)
                    : baseQuery.OrderBy(c => _db.Orders.Where(o => o.CustomerId == c.Id && o.Status != OrderStatus.Cancelled).Sum(o => (decimal?)o.Total) ?? 0),

                CustomerSortField.LastOrderAt => query.SortDescending
                    ? baseQuery.OrderByDescending(c => _db.Orders.Where(o => o.CustomerId == c.Id && o.Status != OrderStatus.Cancelled).Max(o => (DateTime?)o.CreatedAt))
                    : baseQuery.OrderBy(c => _db.Orders.Where(o => o.CustomerId == c.Id && o.Status != OrderStatus.Cancelled).Max(o => (DateTime?)o.CreatedAt)),

                _ => query.SortDescending
                    ? baseQuery.OrderByDescending(c => c.CreatedAt)
                    : baseQuery.OrderBy(c => c.CreatedAt),
            };

            var page = await baseQuery
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(c => new
                {
                    c.Id,
                    c.FirstName,
                    c.LastName,
                    c.Email,
                    c.CreatedAt,
                })
                .ToListAsync(cancellationToken);

            var customerIds = page.Select(c => c.Id).ToList();

            var orderCounts = (await _db.Orders
                .Where(o => o.CustomerId != null && customerIds.Contains(o.CustomerId.Value) && o.Status != OrderStatus.Cancelled)
                .GroupBy(o => o.CustomerId!.Value)
                .Select(g => new { CustomerId = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken))
                .ToDictionary(x => x.CustomerId, x => x.Count);

            // One row per (customer, currency) they've ordered in - grouped again in
            // memory below to pick each customer's highest-value currency as their
            // displayed "primary" total, same reasoning as GetCustomersAsync's spend
            // figures elsewhere: never summed across currencies.
            var spendRows = await _db.Orders
                .Where(o => o.CustomerId != null && customerIds.Contains(o.CustomerId.Value) && o.Status != OrderStatus.Cancelled)
                .GroupBy(o => new { CustomerId = o.CustomerId!.Value, o.Currency })
                .Select(g => new { g.Key.CustomerId, g.Key.Currency, Total = g.Sum(o => o.Total), LastOrderAt = g.Max(o => o.CreatedAt) })
                .ToListAsync(cancellationToken);

            var primarySpendByCustomer = spendRows
                .GroupBy(x => x.CustomerId)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.Total).First());

            var lastOrderByCustomer = spendRows
                .GroupBy(x => x.CustomerId)
                .ToDictionary(g => g.Key, g => g.Max(x => x.LastOrderAt));

            // One row per (customer, business) - the businesses preview shown in the
            // table row ("Acme Coffee, Fresh Market, +2 more"), most-recently-ordered-
            // from first. The full per-business breakdown lives on the detail endpoint;
            // this is only ever the first couple, for a compact table cell.
            var businessRows = await _db.Orders
                .Where(o => o.CustomerId != null && customerIds.Contains(o.CustomerId.Value) && o.Status != OrderStatus.Cancelled)
                .GroupBy(o => new { CustomerId = o.CustomerId!.Value, o.BusinessId, o.Business.Name })
                .Select(g => new { g.Key.CustomerId, g.Key.Name, LastOrderAt = g.Max(o => o.CreatedAt) })
                .ToListAsync(cancellationToken);

            var businessesByCustomer = businessRows
                .GroupBy(x => x.CustomerId)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.LastOrderAt).Select(x => x.Name).ToList());

            var now = DateTime.UtcNow;

            var activeSessionCustomerIds = (await _db.CustomerRefreshTokens
                .Where(rt => customerIds.Contains(rt.CustomerId) && rt.RevokedAt == null && rt.ExpiresAt > now)
                .Select(rt => rt.CustomerId)
                .Distinct()
                .ToListAsync(cancellationToken))
                .ToHashSet();

            var items = page
                .Select(c =>
                {
                    var businessNames = businessesByCustomer.TryGetValue(c.Id, out var names) ? names : [];

                    return new DashboardCustomerResponse
                    {
                        Id = c.Id,
                        FirstName = c.FirstName,
                        LastName = c.LastName,
                        Email = c.Email,
                        OrderCount = orderCounts.TryGetValue(c.Id, out var count) ? count : 0,
                        TotalSpent = primarySpendByCustomer.TryGetValue(c.Id, out var spend) ? spend.Total : 0,
                        SpentCurrency = primarySpendByCustomer.TryGetValue(c.Id, out var spendCurrency) ? spendCurrency.Currency : null,
                        LastOrderAt = lastOrderByCustomer.TryGetValue(c.Id, out var lastOrder) ? lastOrder : null,
                        RecentBusinessNames = businessNames.Take(2).ToList(),
                        AdditionalBusinessCount = Math.Max(0, businessNames.Count - 2),
                        HasActiveSession = activeSessionCustomerIds.Contains(c.Id),
                        CreatedAt = c.CreatedAt,
                    };
                })
                .ToList();

            return (items, totalCount);
        }

        public async Task<DashboardCustomerDetailResponse?> GetCustomerDetailAsync(
            Guid customerId,
            CancellationToken cancellationToken = default)
        {
            var customer = await _db.Customers
                .FirstOrDefaultAsync(c => c.Id == customerId, cancellationToken);

            if (customer is null)
            {
                return null;
            }

            var businesses = await _db.Orders
                .Where(o => o.CustomerId == customerId && o.Status != OrderStatus.Cancelled)
                .GroupBy(o => new { o.BusinessId, o.Business.Name, o.Currency })
                .Select(g => new CustomerBusinessOrderSummaryResponse
                {
                    BusinessId = g.Key.BusinessId,
                    BusinessName = g.Key.Name,
                    OrderCount = g.Count(),
                    TotalSpent = g.Sum(o => o.Total),
                    Currency = g.Key.Currency,
                    LastOrderAt = g.Max(o => o.CreatedAt),
                })
                .ToListAsync(cancellationToken);

            var now = DateTime.UtcNow;

            var hasActiveSession = await _db.CustomerRefreshTokens
                .AnyAsync(rt => rt.CustomerId == customerId && rt.RevokedAt == null && rt.ExpiresAt > now, cancellationToken);

            return new DashboardCustomerDetailResponse
            {
                Id = customer.Id,
                FirstName = customer.FirstName,
                LastName = customer.LastName,
                Email = customer.Email,
                Phone = customer.Phone,
                AddressLine1 = customer.AddressLine1,
                AddressLine2 = customer.AddressLine2,
                City = customer.City,
                State = customer.State,
                PostalCode = customer.PostalCode,
                Country = customer.Country,
                CreatedAt = customer.CreatedAt,
                UpdatedAt = customer.UpdatedAt,
                Businesses = businesses,
                HasActiveSession = hasActiveSession,
            };
        }

        public async Task<Customer?> GetTrackedCustomerAsync(Guid customerId, CancellationToken cancellationToken = default)
        {
            return await _db.Customers.FirstOrDefaultAsync(c => c.Id == customerId, cancellationToken);
        }

        public async Task<CustomerStatsResponse> GetCustomerStatsAsync(
            int newCustomersPeriodDays, CancellationToken cancellationToken = default)
        {
            var totalCustomers = await _db.Customers.CountAsync(cancellationToken);

            var since = DateTime.UtcNow.AddDays(-newCustomersPeriodDays);
            var newCustomers = await _db.Customers.CountAsync(c => c.CreatedAt >= since, cancellationToken);

            var orderCountsByCustomer = await _db.Orders
                .Where(o => o.CustomerId != null && o.Status != OrderStatus.Cancelled)
                .GroupBy(o => o.CustomerId!.Value)
                .Select(g => new { CustomerId = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);

            var customersWithOrders = orderCountsByCustomer.Count;
            var totalCustomerOrders = orderCountsByCustomer.Sum(x => x.Count);
            var repeatCustomers = orderCountsByCustomer.Count(x => x.Count >= 2);

            var revenueByCurrency = await _db.Orders
                .Where(o => o.CustomerId != null && o.Status != OrderStatus.Cancelled)
                .GroupBy(o => o.Currency)
                .Select(g => new CustomerCurrencyTotalResponse
                {
                    Currency = g.Key,
                    TotalSpent = g.Sum(o => o.Total),
                    CustomerCount = g.Select(o => o.CustomerId).Distinct().Count(),
                })
                .ToListAsync(cancellationToken);

            return new CustomerStatsResponse
            {
                TotalCustomers = totalCustomers,
                NewCustomers = newCustomers,
                CustomersWithOrders = customersWithOrders,
                CustomersWithoutOrders = totalCustomers - customersWithOrders,
                TotalCustomerOrders = totalCustomerOrders,
                RepeatCustomers = repeatCustomers,
                RepeatCustomerRate = customersWithOrders > 0 ? (double)repeatCustomers / customersWithOrders : null,
                AverageOrdersPerCustomer = customersWithOrders > 0 ? (double)totalCustomerOrders / customersWithOrders : 0,
                RevenueByCurrency = revenueByCurrency,
            };
        }

        public async Task<List<DateTime>> GetCustomerCreationDatesSinceAsync(DateTime since, CancellationToken cancellationToken = default)
        {
            return await _db.Customers
                .Where(c => c.CreatedAt >= since)
                .Select(c => c.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<TopCustomerResponse>> GetTopCustomersAsync(
            TopCustomersRankBy rankBy, string currency, int take, CancellationToken cancellationToken = default)
        {
            var grouped = _db.Orders
                .Where(o => o.CustomerId != null && o.Status != OrderStatus.Cancelled && o.Currency == currency)
                .GroupBy(o => new { CustomerId = o.CustomerId!.Value, o.Customer!.FirstName, o.Customer.LastName, o.Customer.Email })
                .Select(g => new
                {
                    g.Key.CustomerId,
                    g.Key.FirstName,
                    g.Key.LastName,
                    g.Key.Email,
                    OrderCount = g.Count(),
                    TotalSpent = g.Sum(o => o.Total),
                });

            grouped = rankBy == TopCustomersRankBy.Orders
                ? grouped.OrderByDescending(x => x.OrderCount)
                : grouped.OrderByDescending(x => x.TotalSpent);

            var top = await grouped.Take(take).ToListAsync(cancellationToken);

            return top
                .Select(x => new TopCustomerResponse
                {
                    CustomerId = x.CustomerId,
                    FirstName = x.FirstName,
                    LastName = x.LastName,
                    Email = x.Email,
                    OrderCount = x.OrderCount,
                    TotalSpent = x.TotalSpent,
                    Currency = currency,
                })
                .ToList();
        }

        public async Task<List<KeyCountResponse>> GetCustomerDistributionByBusinessAsync(CancellationToken cancellationToken = default)
        {
            var grouped = await _db.Orders
                .Where(o => o.CustomerId != null && o.Status != OrderStatus.Cancelled)
                .GroupBy(o => new { o.BusinessId, o.Business.Name })
                .Select(g => new { g.Key.Name, CustomerCount = g.Select(o => o.CustomerId).Distinct().Count() })
                .ToListAsync(cancellationToken);

            return grouped
                .Select(x => new KeyCountResponse { Key = x.Name, Count = x.CustomerCount })
                .ToList();
        }

        public async Task<List<DashboardCustomerResponse>> GetRecentCustomersAsync(int take, CancellationToken cancellationToken = default)
        {
            return await _db.Customers
                .OrderByDescending(c => c.CreatedAt)
                .Take(take)
                .Select(c => new DashboardCustomerResponse
                {
                    Id = c.Id,
                    FirstName = c.FirstName,
                    LastName = c.LastName,
                    Email = c.Email,
                    CreatedAt = c.CreatedAt,
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<List<BusinessOptionResponse>> GetBusinessOptionsAsync(CancellationToken cancellationToken = default)
        {
            return await _db.Businesses
                .OrderBy(b => b.Name)
                .Select(b => new BusinessOptionResponse { Id = b.Id, Name = b.Name })
                .ToListAsync(cancellationToken);
        }

        public async Task<(List<CustomerOrderResponse> Items, int TotalCount)> GetCustomerOrdersAsync(
            Guid customerId, Guid? businessId, int page, int pageSize, CancellationToken cancellationToken = default)
        {
            var baseQuery = _db.Orders.Where(o => o.CustomerId == customerId);

            if (businessId.HasValue)
            {
                baseQuery = baseQuery.Where(o => o.BusinessId == businessId.Value);
            }

            var totalCount = await baseQuery.CountAsync(cancellationToken);

            // Every status, including Cancelled, shown here on purpose - this is a
            // factual history, not an aggregate; aggregates elsewhere exclude Cancelled.
            var rawItems = await baseQuery
                .OrderByDescending(o => o.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(o => new { o.Id, o.BusinessId, BusinessName = o.Business.Name, o.Status, o.Total, o.Currency, o.CreatedAt })
                .ToListAsync(cancellationToken);

            var items = rawItems
                .Select(o => new CustomerOrderResponse
                {
                    Id = o.Id,
                    BusinessId = o.BusinessId,
                    BusinessName = o.BusinessName,
                    Status = o.Status.ToString(),
                    Total = o.Total,
                    Currency = o.Currency,
                    CreatedAt = o.CreatedAt,
                })
                .ToList();

            return (items, totalCount);
        }

        public async Task<List<CustomerSpendPointResponse>> GetCustomerSpendOverTimeAsync(
            Guid customerId, CancellationToken cancellationToken = default)
        {
            var grouped = await _db.Orders
                .Where(o => o.CustomerId == customerId && o.Status != OrderStatus.Cancelled)
                .GroupBy(o => new { Year = o.CreatedAt.Year, Month = o.CreatedAt.Month, o.Currency })
                .Select(g => new { g.Key.Year, g.Key.Month, g.Key.Currency, Total = g.Sum(o => o.Total) })
                .ToListAsync(cancellationToken);

            return grouped
                .OrderBy(x => x.Year).ThenBy(x => x.Month)
                .Select(x => new CustomerSpendPointResponse
                {
                    Period = $"{x.Year:D4}-{x.Month:D2}",
                    Total = x.Total,
                    Currency = x.Currency,
                })
                .ToList();
        }

        // ---- subscriptions (platform-wide, Subscriptions tab) ----

        public async Task<(List<AdminSubscriptionListItemResponse> Items, int TotalCount)> GetSubscriptionsAsync(
            SubscriptionsQueryRequest query,
            CancellationToken cancellationToken = default)
        {
            var baseQuery = _db.Subscriptions.AsQueryable();

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var pattern = $"%{query.Search.Trim()}%";

                baseQuery = baseQuery.Where(s =>
                    EF.Functions.Like(s.Business.Name, pattern) ||
                    EF.Functions.Like(s.Business.Owner.FirstName, pattern) ||
                    EF.Functions.Like(s.Business.Owner.LastName, pattern) ||
                    EF.Functions.Like(s.Business.Owner.Email, pattern));
            }

            if (query.PlanId.HasValue)
            {
                baseQuery = baseQuery.Where(s => s.SubscriptionPlanId == query.PlanId.Value);
            }

            if (!string.IsNullOrWhiteSpace(query.PlanName))
            {
                baseQuery = baseQuery.Where(s => s.SubscriptionPlan.Name == query.PlanName);
            }

            if (query.BillingInterval.HasValue)
            {
                baseQuery = baseQuery.Where(s => s.SubscriptionPlan.BillingInterval == query.BillingInterval.Value);
            }

            if (query.Status.HasValue)
            {
                baseQuery = baseQuery.Where(s => s.Status == query.Status.Value);
            }

            var totalCount = await baseQuery.CountAsync(cancellationToken);

            var projected = baseQuery.Select(s => new
            {
                s.Id,
                s.BusinessId,
                BusinessName = s.Business.Name,
                OwnerFullName = s.Business.Owner.FirstName + " " + s.Business.Owner.LastName,
                OwnerEmail = s.Business.Owner.Email,
                DomainName = s.Business.BusinessDomain != null ? s.Business.BusinessDomain.Name : null,
                s.SubscriptionPlanId,
                PlanName = s.SubscriptionPlan.Name,
                PlanIsActive = s.SubscriptionPlan.IsActive,
                BillingInterval = s.SubscriptionPlan.BillingInterval,
                s.Status,
                s.CurrentPeriodStart,
                s.CurrentPeriodEnd,
                s.CancelAtPeriodEnd,
                s.CreatedAt,
            });

            projected = query.SortBy switch
            {
                SubscriptionSortField.BusinessName => query.SortDescending
                    ? projected.OrderByDescending(x => x.BusinessName)
                    : projected.OrderBy(x => x.BusinessName),

                SubscriptionSortField.PlanName => query.SortDescending
                    ? projected.OrderByDescending(x => x.PlanName)
                    : projected.OrderBy(x => x.PlanName),

                SubscriptionSortField.CurrentPeriodEnd => query.SortDescending
                    ? projected.OrderByDescending(x => x.CurrentPeriodEnd)
                    : projected.OrderBy(x => x.CurrentPeriodEnd),

                _ => query.SortDescending
                    ? projected.OrderByDescending(x => x.CreatedAt)
                    : projected.OrderBy(x => x.CreatedAt),
            };

            var page = await projected
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync(cancellationToken);

            // Enum -> string conversion happens here, in memory, not inside the
            // SQL-translated Select above - same reasoning as GetBusinessesAsync.
            var items = page
                .Select(x => new AdminSubscriptionListItemResponse
                {
                    SubscriptionId = x.Id,
                    BusinessId = x.BusinessId,
                    BusinessName = x.BusinessName,
                    OwnerFullName = x.OwnerFullName,
                    OwnerEmail = x.OwnerEmail,
                    DomainName = x.DomainName,
                    PlanId = x.SubscriptionPlanId,
                    PlanName = x.PlanName,
                    PlanIsActive = x.PlanIsActive,
                    BillingInterval = x.BillingInterval.ToString(),
                    Status = x.Status.ToString(),
                    CurrentPeriodStart = x.CurrentPeriodStart,
                    CurrentPeriodEnd = x.CurrentPeriodEnd,
                    CancelAtPeriodEnd = x.CancelAtPeriodEnd,
                    CreatedAt = x.CreatedAt,
                })
                .ToList();

            return (items, totalCount);
        }

        public async Task<List<RecentSubscriptionActivityEntryResponse>> GetRecentSubscriptionActivityAsync(
            int take,
            CancellationToken cancellationToken = default)
        {
            var recent = await _db.Subscriptions
                .OrderByDescending(s => s.CreatedAt)
                .Take(take)
                .Select(s => new
                {
                    s.BusinessId,
                    BusinessName = s.Business.Name,
                    PlanName = s.SubscriptionPlan.Name,
                    BillingInterval = s.SubscriptionPlan.BillingInterval,
                    s.CreatedAt,
                })
                .ToListAsync(cancellationToken);

            if (recent.Count == 0)
            {
                return [];
            }

            var businessIds = recent.Select(r => r.BusinessId).Distinct().ToList();

            // One more grouped query for just the involved businesses' earliest
            // Subscription row - two queries total regardless of `take`, not N+1.
            var earliestByBusiness = (await _db.Subscriptions
                .Where(s => businessIds.Contains(s.BusinessId))
                .GroupBy(s => s.BusinessId)
                .Select(g => new { BusinessId = g.Key, Earliest = g.Min(s => s.CreatedAt) })
                .ToListAsync(cancellationToken))
                .ToDictionary(x => x.BusinessId, x => x.Earliest);

            return recent
                .Select(r => new RecentSubscriptionActivityEntryResponse
                {
                    BusinessId = r.BusinessId,
                    BusinessName = r.BusinessName,
                    PlanName = r.PlanName,
                    BillingInterval = r.BillingInterval.ToString(),
                    IsNewSubscription = earliestByBusiness.TryGetValue(r.BusinessId, out var earliest) && earliest == r.CreatedAt,
                    CreatedAt = r.CreatedAt,
                })
                .ToList();
        }
    }
}
