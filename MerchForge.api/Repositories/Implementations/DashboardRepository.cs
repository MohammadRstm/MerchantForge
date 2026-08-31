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
                    EF.Functions.Like(c.Email, pattern));
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
                .Where(o => o.CustomerId != null && customerIds.Contains(o.CustomerId.Value))
                .GroupBy(o => o.CustomerId!.Value)
                .Select(g => new { CustomerId = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken))
                .ToDictionary(x => x.CustomerId, x => x.Count);

            var items = page
                .Select(c => new DashboardCustomerResponse
                {
                    Id = c.Id,
                    FirstName = c.FirstName,
                    LastName = c.LastName,
                    Email = c.Email,
                    OrderCount = orderCounts.TryGetValue(c.Id, out var count) ? count : 0,
                    CreatedAt = c.CreatedAt,
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
                .Where(o => o.CustomerId == customerId)
                .GroupBy(o => new { o.BusinessId, o.Business.Name, o.Currency })
                .Select(g => new CustomerBusinessOrderSummaryResponse
                {
                    BusinessId = g.Key.BusinessId,
                    BusinessName = g.Key.Name,
                    OrderCount = g.Count(),
                    TotalSpent = g.Sum(o => o.Total),
                    Currency = g.Key.Currency,
                })
                .ToListAsync(cancellationToken);

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
            };
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
