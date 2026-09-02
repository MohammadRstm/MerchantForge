using System.Text.Json;
using Hangfire;
using Microsoft.AspNetCore.Identity;
using MerchForge.api.DTOs.Audit;
using MerchForge.api.DTOs.BusinessDashboard;
using MerchForge.api.DTOs.Common;
using MerchForge.api.DTOs.Dashboard;
using MerchForge.api.DTOs.Subscriptions;
using MerchForge.api.DTOs.WebsiteTemplateRequests;
using MerchForge.api.Enums;
using MerchForge.api.Exceptions.BusinessDashboard;
using MerchForge.api.Exceptions.Dashboard;
using MerchForge.api.Exceptions.WebsiteTemplateRequests;
using MerchForge.api.Jobs.Email;
using MerchForge.api.Models;
using MerchForge.api.Repositories.Interfaces;
using MerchForge.api.Services.Audit.interfaces;
using MerchForge.api.Services.Common;
using MerchForge.api.Services.BusinessDashboard.interfaces;
using MerchForge.api.Services.Dashboard.interfaces;
using MerchForge.api.Services.Onboarding.interfaces;
using MerchForge.api.Services.Subscription.interfaces;

namespace MerchForge.api.Services.Dashboard
{
    public class DashboardService : IDashboardService
    {
        private const int StatsTimeSeriesMonths = 6;
        private const int BusinessesAddedRecentlyWindowDays = 30;
        private const int UserRecentActivityTake = 20;
        private const int CustomerRecentActivityTake = 20;
        private const string DefaultTopCustomerCurrency = "USD";

        private readonly IDashboardRepository _dashboardRepository;
        private readonly IBusinessDashboardRepository _businessDashboardRepository;
        private readonly ISubscriptionRepository _subscriptionRepository;
        private readonly ISubscriptionPlanRepository _subscriptionPlanRepository;
        private readonly IOrderRepository _orderRepository;
        private readonly IFeatureCreditService _featureCreditService;
        private readonly IBusinessDashboardService _businessDashboardService;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly ICustomerRefreshTokenRepository _customerRefreshTokenRepository;
        private readonly IWebsiteTemplateRequestRepository _websiteTemplateRequestRepository;
        private readonly IWebsiteTemplateImageService _websiteTemplateImageService;
        private readonly IDomainService _domainService;
        private readonly IBackgroundJobClient _backgroundJobClient;
        private readonly IAuditLogService _auditLogService;
        private readonly ICurrentUserAccessor _currentUserAccessor;
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasher<User> _passwordHasher;

        public DashboardService(
            IDashboardRepository dashboardRepository,
            IBusinessDashboardRepository businessDashboardRepository,
            ISubscriptionRepository subscriptionRepository,
            ISubscriptionPlanRepository subscriptionPlanRepository,
            IOrderRepository orderRepository,
            IFeatureCreditService featureCreditService,
            IBusinessDashboardService businessDashboardService,
            IRefreshTokenRepository refreshTokenRepository,
            ICustomerRefreshTokenRepository customerRefreshTokenRepository,
            IWebsiteTemplateRequestRepository websiteTemplateRequestRepository,
            IWebsiteTemplateImageService websiteTemplateImageService,
            IDomainService domainService,
            IBackgroundJobClient backgroundJobClient,
            IAuditLogService auditLogService,
            ICurrentUserAccessor currentUserAccessor,
            IUserRepository userRepository,
            IPasswordHasher<User> passwordHasher)
        {
            _dashboardRepository = dashboardRepository;
            _businessDashboardRepository = businessDashboardRepository;
            _subscriptionRepository = subscriptionRepository;
            _subscriptionPlanRepository = subscriptionPlanRepository;
            _orderRepository = orderRepository;
            _featureCreditService = featureCreditService;
            _businessDashboardService = businessDashboardService;
            _refreshTokenRepository = refreshTokenRepository;
            _customerRefreshTokenRepository = customerRefreshTokenRepository;
            _websiteTemplateRequestRepository = websiteTemplateRequestRepository;
            _websiteTemplateImageService = websiteTemplateImageService;
            _domainService = domainService;
            _backgroundJobClient = backgroundJobClient;
            _auditLogService = auditLogService;
            _currentUserAccessor = currentUserAccessor;
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
        }

        public async Task<DashboardStatsResponse> GetPlatformStatsAsync(CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;

            var seriesStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc)
                .AddMonths(-(StatsTimeSeriesMonths - 1));

            var totalUsers = await _dashboardRepository.CountUsersAsync(cancellationToken);
            var totalBusinesses = await _dashboardRepository.CountBusinessesAsync(cancellationToken);
            var totalProducts = await _dashboardRepository.CountProductsAsync(cancellationToken);
            var totalProductDrafts = await _dashboardRepository.CountProductDraftsAsync(cancellationToken);
            var pendingInvitations = await _dashboardRepository.CountPendingInvitationsAsync(cancellationToken);
            var (pendingRequests, completedRequests) =
                await _dashboardRepository.GetWebsiteTemplateRequestStatusCountsAsync(cancellationToken);
            var activeSessionCount = await _dashboardRepository.CountActiveSessionsAsync(cancellationToken);
            var totalOrders = await _dashboardRepository.CountOrdersAsync(cancellationToken);
            var businessesAddedRecently = await _dashboardRepository.CountBusinessesCreatedSinceAsync(
                now.AddDays(-BusinessesAddedRecentlyWindowDays), cancellationToken);
            var recordedOrderRevenue = await _dashboardRepository.GetRecordedOrderRevenueByCurrencyAsync(cancellationToken);

            var usersBySystemRole = await _dashboardRepository.GetUserCountsBySystemRoleAsync(cancellationToken);
            var businessUsersByRole = await _dashboardRepository.GetBusinessUserCountsByRoleAsync(cancellationToken);
            var businessesByDomain = await _dashboardRepository.GetBusinessCountsByDomainAsync(cancellationToken);
            var subscriptionsByStatus = await _dashboardRepository.GetSubscriptionStatusCountsAsync(cancellationToken);
            var recentBusinesses = await _dashboardRepository.GetRecentBusinessesAsync(5, cancellationToken);

            var businessDates = await _dashboardRepository.GetBusinessCreationDatesSinceAsync(seriesStart, cancellationToken);
            var productDates = await _dashboardRepository.GetProductCreationDatesSinceAsync(seriesStart, cancellationToken);

            return new DashboardStatsResponse
            {
                TotalUsers = totalUsers,
                TotalBusinesses = totalBusinesses,
                TotalProducts = totalProducts,
                TotalProductDrafts = totalProductDrafts,
                PendingInvitations = pendingInvitations,
                PendingWebsiteTemplateRequests = pendingRequests,
                CompletedWebsiteTemplateRequests = completedRequests,
                ActiveSessionCount = activeSessionCount,
                TotalOrders = totalOrders,
                BusinessesAddedRecently = businessesAddedRecently,
                RecordedOrderRevenue = recordedOrderRevenue,

                UsersBySystemRole = usersBySystemRole,
                BusinessUsersByRole = businessUsersByRole,
                BusinessesByDomain = businessesByDomain,
                SubscriptionsByStatus = subscriptionsByStatus,
                RecentBusinesses = recentBusinesses,

                BusinessesOverTime = TimeSeriesBuilder.BuildMonthlySeries(businessDates, seriesStart, now),
                ProductsOverTime = TimeSeriesBuilder.BuildMonthlySeries(productDates, seriesStart, now),
            };
        }

        public async Task<PagedResult<DashboardUserResponse>> GetUsersAsync(
            UsersQueryRequest query,
            CancellationToken cancellationToken = default)
        {
            var (items, totalCount) = await _dashboardRepository.GetUsersAsync(query, cancellationToken);

            return new PagedResult<DashboardUserResponse>
            {
                Items = items,
                Page = query.Page,
                PageSize = query.PageSize,
                TotalCount = totalCount,
            };
        }

        public async Task<RevokeUserSessionsResponse> RevokeUserSessionsAsync(
            Guid targetUserId,
            Guid actingUserId,
            CancellationToken cancellationToken = default)
        {
            if (targetUserId == actingUserId)
            {
                throw new CannotRevokeOwnSessionException();
            }

            var userExists = await _dashboardRepository.UserExistsAsync(targetUserId, cancellationToken);

            if (!userExists)
            {
                throw new UserNotFoundException();
            }

            var revokedCount = await _refreshTokenRepository.RevokeAllForUserAsync(targetUserId, cancellationToken);

            await _auditLogService.LogAsync(
                AuditEventType.UserManagement, "UserSessionRevoked",
                $"Revoked {revokedCount} session(s) for a user.",
                success: true, actorUserId: actingUserId,
                entityType: "User", entityId: targetUserId,
                cancellationToken: cancellationToken);

            return new RevokeUserSessionsResponse
            {
                RevokedSessionsCount = revokedCount
            };
        }

        public async Task<DashboardUserDetailResponse> GetUserDetailAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            var detail = await _dashboardRepository.GetUserDetailAsync(userId, cancellationToken)
                ?? throw new UserNotFoundException();

            detail.RecentActivity = await _auditLogService.GetUserActivityAsync(userId, UserRecentActivityTake, cancellationToken);

            return detail;
        }

        public async Task<DashboardUserDetailResponse> DisableUserAsync(
            Guid targetUserId,
            Guid actingUserId,
            CancellationToken cancellationToken = default)
        {
            if (targetUserId == actingUserId)
            {
                throw new CannotDisableOwnAccountException();
            }

            var user = await _dashboardRepository.GetTrackedUserAsync(targetUserId, cancellationToken)
                ?? throw new UserNotFoundException();

            if (user.DisabledAt is null)
            {
                user.DisabledAt = DateTime.UtcNow;
                user.DisabledByUserId = actingUserId;
                user.UpdatedAt = DateTime.UtcNow;

                await _dashboardRepository.SaveChangesAsync(cancellationToken);

                // A disabled account can't authenticate again, but any session it
                // already holds must be cut off too - the same revoke path Force
                // Logout uses.
                await _refreshTokenRepository.RevokeAllForUserAsync(targetUserId, cancellationToken);

                await _auditLogService.LogAsync(
                    AuditEventType.UserManagement, "UserDisabled",
                    $"Disabled account for {user.Email}.",
                    success: true, actorUserId: actingUserId,
                    entityType: "User", entityId: targetUserId,
                    cancellationToken: cancellationToken);
            }

            return await GetUserDetailAsync(targetUserId, cancellationToken);
        }

        public async Task<DashboardUserDetailResponse> EnableUserAsync(
            Guid targetUserId,
            Guid actingUserId,
            CancellationToken cancellationToken = default)
        {
            var user = await _dashboardRepository.GetTrackedUserAsync(targetUserId, cancellationToken)
                ?? throw new UserNotFoundException();

            if (user.DisabledAt is not null)
            {
                user.DisabledAt = null;
                user.DisabledByUserId = null;
                user.UpdatedAt = DateTime.UtcNow;

                await _dashboardRepository.SaveChangesAsync(cancellationToken);

                await _auditLogService.LogAsync(
                    AuditEventType.UserManagement, "UserEnabled",
                    $"Re-enabled account for {user.Email}.",
                    success: true, actorUserId: actingUserId,
                    entityType: "User", entityId: targetUserId,
                    cancellationToken: cancellationToken);
            }

            return await GetUserDetailAsync(targetUserId, cancellationToken);
        }

        public async Task<RevokeUserSessionsResponse> RevokeAllSessionsAsync(
            Guid actingUserId,
            CancellationToken cancellationToken = default)
        {
            var revokedCount = await _refreshTokenRepository.RevokeAllAsync(actingUserId, cancellationToken);

            await _auditLogService.LogAsync(
                AuditEventType.Security, "AllSessionsRevoked",
                $"Revoked {revokedCount} session(s) platform-wide.",
                success: true, actorUserId: actingUserId,
                cancellationToken: cancellationToken);

            return new RevokeUserSessionsResponse
            {
                RevokedSessionsCount = revokedCount
            };
        }

        public async Task<PagedResult<DashboardBusinessResponse>> GetBusinessesAsync(
            BusinessesQueryRequest query,
            CancellationToken cancellationToken = default)
        {
            var (items, totalCount) = await _dashboardRepository.GetBusinessesAsync(query, cancellationToken);

            return new PagedResult<DashboardBusinessResponse>
            {
                Items = items,
                Page = query.Page,
                PageSize = query.PageSize,
                TotalCount = totalCount,
            };
        }

        public async Task<BusinessDetailResponse> GetBusinessDetailAsync(
            Guid businessId,
            CancellationToken cancellationToken = default)
        {
            var business = await _dashboardRepository.GetBusinessDetailCoreAsync(businessId, cancellationToken)
                ?? throw new BusinessNotFoundException();

            var members = await _businessDashboardRepository.GetMembersAsync(businessId, cancellationToken);
            var productCount = await _businessDashboardRepository.CountProductsAsync(businessId, cancellationToken);
            var priceStats = await _businessDashboardRepository.GetProductPriceStatsAsync(businessId, cancellationToken);
            var productsByCategory = await _businessDashboardRepository.GetProductsByCategoryAsync(businessId, cancellationToken);
            var draftCount = await _businessDashboardRepository.CountProductDraftsAsync(businessId, cancellationToken);
            var draftsByStatus = await _businessDashboardRepository.GetProductDraftsByStatusAsync(businessId, cancellationToken);
            var requests = await _websiteTemplateRequestRepository.GetForBusinessAsync(businessId, cancellationToken);
            var subscription = await _subscriptionRepository.GetLatestSubscriptionWithPlanFeaturesAsync(businessId, cancellationToken);
            var featureCredits = await _featureCreditService.GetOverviewAsync(businessId, cancellationToken);

            int? activeSubscriberCountForPlan = subscription is null
                ? null
                : await _subscriptionPlanRepository.CountActiveSubscribersAsync(subscription.SubscriptionPlanId, cancellationToken);

            return new BusinessDetailResponse
            {
                Id = business.Id,
                Name = business.Name,
                Description = business.Description,
                LogoUrl = business.LogoUrl,
                Currency = business.Currency,
                Locale = business.Locale,
                ContactEmail = business.ContactEmail,
                ContactPhone = business.ContactPhone,
                Tagline = business.Tagline,
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
                BusinessDomainId = business.BusinessDomainId,
                DomainName = business.BusinessDomain?.Name,
                CreatedAt = business.CreatedAt,
                IsDemo = business.IsDemo,

                OwnerUserId = business.OwnerUserId,
                OwnerFullName = $"{business.Owner.FirstName} {business.Owner.LastName}",
                OwnerEmail = business.Owner.Email,

                Members = members,

                ProductCount = productCount,
                AverageProductPrice = priceStats.Average,
                MinProductPrice = priceStats.Min,
                MaxProductPrice = priceStats.Max,
                ProductsByCategory = productsByCategory,

                ProductDraftCount = draftCount,
                ProductDraftsByStatus = draftsByStatus,

                WebsiteUrl = business.WebsiteUrl,
                WebsiteTemplateId = business.WebsiteTemplateId,
                WebsiteTemplateName = business.WebsiteTemplate?.Name,
                WebsiteTemplateLabel = business.WebsiteTemplate?.Label,
                WebsiteTemplateChosenAt = business.WebsiteTemplateChosenAt,
                WebsiteTemplateRequests = requests,

                Subscription = subscription is null ? null : MapSubscription(subscription),
                ActiveSubscriberCountForPlan = activeSubscriberCountForPlan,

                FeatureCredits = featureCredits,
            };
        }

        private const int DemoBusinessProductCount = 12;
        private const int DemoBusinessCustomerCount = 20;
        private const int DemoBusinessOrderCount = 30;
        private const int DemoBusinessOrderHistoryDays = 120;

        public async Task<DemoBusinessResponse> CreateDemoBusinessAsync(
            CreateDemoBusinessRequest request,
            CancellationToken cancellationToken = default)
        {
            await _domainService.EnsureDomainExistsAsync(request.BusinessDomainId, cancellationToken);

            if (await _dashboardRepository.DemoBusinessExistsForDomainAsync(request.BusinessDomainId, cancellationToken))
            {
                throw new DemoBusinessAlreadyExistsForDomainException();
            }

            var template = await _dashboardRepository.GetPrimaryActiveTemplateForDomainAsync(request.BusinessDomainId, cancellationToken)
                ?? throw new DomainHasNoActiveTemplateException();

            if (await _userRepository.GetByEmailAsync(request.OwnerEmail, cancellationToken) is not null)
            {
                throw new Exceptions.Auth.EmailAlreadyExistsException();
            }

            var proYearlyPlan = (await _subscriptionPlanRepository.GetActiveAsync(cancellationToken))
                .FirstOrDefault(p => p.Name == "Pro" && p.BillingInterval == BillingInterval.Yearly)
                ?? throw new InvalidOperationException("No active Pro/Yearly subscription plan found.");

            var systemRoleId = await _userRepository.GetSystemRoleId(SystemRole.User, cancellationToken);
            var businessRoleId = await _userRepository.GetBusinessRoleId(BusinessRole.Owner, cancellationToken);

            var now = DateTime.UtcNow;

            var owner = new User
            {
                Id = Guid.NewGuid(),
                FirstName = request.OwnerFirstName,
                LastName = request.OwnerLastName,
                Email = request.OwnerEmail,
                SystemRoleId = systemRoleId,
                CreatedAt = now,
                UpdatedAt = now,
            };
            owner.PasswordHash = _passwordHasher.HashPassword(owner, request.OwnerPassword);

            var business = new Business
            {
                Id = Guid.NewGuid(),
                Name = request.BusinessName,
                OwnerUserId = owner.Id,
                BusinessDomainId = request.BusinessDomainId,
                WebsiteTemplateId = template.Id,
                WebsiteTemplateChosenAt = now,
                IsDemo = true,
                CreatedAt = now,
                UpdatedAt = now,
            };

            var businessUser = new BusinessUser
            {
                UserId = owner.Id,
                BusinessId = business.Id,
                RoleId = businessRoleId,
                CreatedAt = now,
                UpdatedAt = now,
            };

            await _userRepository.RegisterUser(owner, business, businessUser, cancellationToken);

            await _businessDashboardService.SubscribeToPlanAsync(business.Id, proYearlyPlan.Id, cancellationToken);

            var categories = await _dashboardRepository.GetActivePlatformCategoriesForDomainAsync(request.BusinessDomainId, cancellationToken);
            var products = BuildDemoProducts(business.Id, categories);
            var customers = BuildDemoCustomers();
            var orders = BuildDemoOrders(business, customers, products);

            await _dashboardRepository.CreateDemoBusinessCatalogAsync(products, customers, orders, cancellationToken);

            await _auditLogService.LogAsync(
                AuditEventType.BusinessManagement, "DemoBusinessCreated",
                $"Created demo business \"{business.Name}\".",
                success: true, actorUserId: _currentUserAccessor.UserId,
                entityType: "Business", entityId: business.Id,
                cancellationToken: cancellationToken);

            return new DemoBusinessResponse
            {
                BusinessId = business.Id,
                BusinessName = business.Name,
                OwnerUserId = owner.Id,
                OwnerEmail = owner.Email,
                ProductCount = products.Count,
                CustomerCount = customers.Count,
                OrderCount = orders.Count,
            };
        }

        /// <summary>
        /// Curated, believable product names keyed by category name where one exists;
        /// any category without a curated list (a domain added after this was written)
        /// falls back to "{Category} Item N" rather than failing — demo data should
        /// degrade to bland, not break.
        /// </summary>
        private static readonly Dictionary<string, string[]> DemoProductNamesByCategory = new()
        {
            ["Shoes"] = ["Classic Leather Sneaker", "Suede Chelsea Boot", "Canvas Low-Top", "Running Trainer", "Woven Sandal"],
            ["Shirts"] = ["Oxford Button-Down", "Merino Wool Sweater", "Linen Short-Sleeve", "Graphic Tee", "Flannel Overshirt"],
            ["Accessories"] = ["Leather Belt", "Wool Scarf", "Canvas Tote Bag", "Aviator Sunglasses", "Woven Bracelet"],
            ["Phones"] = ["Aurora X12 Smartphone", "Nova Lite 5G", "Pulse Mini", "Aurora X12 Pro", "Vantage Flip"],
            ["Laptops"] = ["Slate 14 Ultrabook", "Forge 16 Workstation", "Aria Go 13", "Slate 14 Pro", "Titan Gaming 17"],
            ["Vegetables"] = ["Organic Roma Tomatoes", "Baby Spinach Bunch", "Heirloom Carrots", "Bell Pepper Mix", "Red Onions"],
            ["Fruits"] = ["Honeycrisp Apples", "Ripe Avocados", "Seedless Grapes", "Navel Oranges", "Alphonso Mangoes"],
            ["Dairy"] = ["Whole Milk, 1 Gal", "Aged Cheddar Block", "Greek Yogurt Tub", "Salted Butter", "Free-Range Eggs, Dozen"],
            ["Bakery"] = ["Sourdough Loaf", "Butter Croissants (4pk)", "Cinnamon Rolls (6pk)", "Baguette", "Blueberry Muffins (4pk)"],
            ["Beverages"] = ["Cold Brew Coffee", "Sparkling Water (6pk)", "Fresh-Pressed Orange Juice", "Herbal Tea Sampler", "Kombucha"],
        };

        private static List<Product> BuildDemoProducts(Guid businessId, List<Category> categories)
        {
            if (categories.Count == 0)
            {
                return [];
            }

            var random = new Random();
            var products = new List<Product>();
            var now = DateTime.UtcNow;
            var perCategory = Math.Max(1, DemoBusinessProductCount / categories.Count);

            foreach (var category in categories)
            {
                var names = DemoProductNamesByCategory.TryGetValue(category.Name, out var curated)
                    ? curated
                    : Enumerable.Range(1, perCategory).Select(i => $"{category.Name} Item {i}").ToArray();

                foreach (var name in names.Take(perCategory))
                {
                    if (products.Count >= DemoBusinessProductCount) break;

                    var price = Math.Round((decimal)(random.NextDouble() * 180 + 15), 2);
                    var onSale = random.Next(4) == 0;

                    products.Add(new Product
                    {
                        Id = Guid.NewGuid(),
                        BusinessId = businessId,
                        CategoryId = category.Id,
                        Title = name,
                        Price = price,
                        CompareAtPrice = onSale ? Math.Round(price * 1.25m, 2) : null,
                        StockQuantity = random.Next(5, 120),
                        CreatedAt = now.AddDays(-random.Next(1, DemoBusinessOrderHistoryDays)),
                        UpdatedAt = now,
                    });
                }
            }

            return products;
        }

        private static readonly string[] DemoCustomerFirstNames =
            ["Amara", "Liam", "Sofia", "Noah", "Elena", "Marcus", "Priya", "Ethan", "Yuki", "Daniel",
             "Grace", "Omar", "Chloe", "Lucas", "Mia", "Adrian", "Nadia", "Jack", "Zara", "Felix"];

        private static readonly string[] DemoCustomerLastNames =
            ["Bennett", "Torres", "Nakamura", "Osei", "Kowalski", "Rossi", "Larsen", "Petrov", "Hughes", "Alvarez",
             "Chen", "Meyer", "Novak", "Diallo", "Sato", "Reyes", "Fischer", "Hassan", "Moretti", "Blake"];

        private static List<Customer> BuildDemoCustomers()
        {
            var now = DateTime.UtcNow;
            var customers = new List<Customer>();

            for (var i = 0; i < DemoBusinessCustomerCount; i++)
            {
                var first = DemoCustomerFirstNames[i % DemoCustomerFirstNames.Length];
                var last = DemoCustomerLastNames[i % DemoCustomerLastNames.Length];
                var createdAt = now.AddDays(-Random.Shared.Next(1, DemoBusinessOrderHistoryDays));

                customers.Add(new Customer
                {
                    Id = Guid.NewGuid(),
                    Email = $"{first.ToLowerInvariant()}.{last.ToLowerInvariant()}.demo{i}@example.com",
                    PasswordHash = string.Empty,
                    FirstName = first,
                    LastName = last,
                    CreatedAt = createdAt,
                    UpdatedAt = createdAt,
                });
            }

            return customers;
        }

        private static readonly OrderStatus[] DemoOrderStatuses =
            [OrderStatus.Confirmed, OrderStatus.Confirmed, OrderStatus.Shipped, OrderStatus.Delivered,
             OrderStatus.Delivered, OrderStatus.Pending, OrderStatus.Cancelled];

        private static List<Order> BuildDemoOrders(Business business, List<Customer> customers, List<Product> products)
        {
            if (customers.Count == 0 || products.Count == 0)
            {
                return [];
            }

            var random = new Random();
            var now = DateTime.UtcNow;
            var orders = new List<Order>();

            for (var i = 0; i < DemoBusinessOrderCount; i++)
            {
                var customer = customers[random.Next(customers.Count)];
                var createdAt = now.AddDays(-random.Next(1, DemoBusinessOrderHistoryDays));
                var itemCount = random.Next(1, 4);
                var items = new List<OrderItem>();

                foreach (var product in products.OrderBy(_ => random.Next()).Take(itemCount))
                {
                    var quantity = random.Next(1, 3);
                    var lineTotal = product.Price * quantity;

                    items.Add(new OrderItem
                    {
                        Id = Guid.NewGuid(),
                        ProductId = product.Id,
                        ProductTitle = product.Title,
                        ProductImageUrl = product.ImageUrl,
                        UnitPrice = product.Price,
                        Quantity = quantity,
                        LineTotal = lineTotal,
                    });
                }

                var subtotal = items.Sum(i => i.LineTotal);

                orders.Add(new Order
                {
                    Id = Guid.NewGuid(),
                    BusinessId = business.Id,
                    CustomerId = customer.Id,
                    CustomerName = $"{customer.FirstName} {customer.LastName}",
                    CustomerEmail = customer.Email,
                    ShippingAddressLine1 = "123 Market Street",
                    ShippingCity = "Springfield",
                    ShippingPostalCode = "00000",
                    ShippingCountry = "US",
                    Status = DemoOrderStatuses[random.Next(DemoOrderStatuses.Length)],
                    Subtotal = subtotal,
                    Total = subtotal,
                    Currency = business.Currency,
                    CreatedAt = createdAt,
                    UpdatedAt = createdAt,
                    Items = items,
                });
            }

            return orders;
        }

        private static SocialLinksDto? ReadSocialLinks(JsonDocument? document) =>
            document is null
                ? null
                : JsonSerializer.Deserialize<SocialLinksDto>(document.RootElement.GetRawText());

        private static BusinessHoursDto? ReadBusinessHours(JsonDocument? document) =>
            document is null
                ? null
                : JsonSerializer.Deserialize<BusinessHoursDto>(document.RootElement.GetRawText());

        private static BusinessSubscriptionResponse MapSubscription(Models.Subscription subscription)
        {
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

        public async Task<RevokeUserSessionsResponse> RevokeBusinessSessionsAsync(
            Guid businessId,
            CancellationToken cancellationToken = default)
        {
            var business = await _dashboardRepository.GetBusinessDetailCoreAsync(businessId, cancellationToken)
                ?? throw new BusinessNotFoundException();

            var revokedCount = await _refreshTokenRepository.RevokeAllForBusinessAsync(business.Id, cancellationToken);

            await _auditLogService.LogAsync(
                AuditEventType.UserManagement, "UserSessionRevoked",
                $"Revoked {revokedCount} session(s) for {business.Name}'s members.",
                success: true, actorUserId: _currentUserAccessor.UserId,
                entityType: "Business", entityId: businessId, businessId: businessId,
                cancellationToken: cancellationToken);

            return new RevokeUserSessionsResponse
            {
                RevokedSessionsCount = revokedCount
            };
        }

        // ---- business analytics (reuses the same repository methods the Owner Dashboard calls) ----

        public async Task<OrderAnalyticsResponse> GetBusinessOrderAnalyticsAsync(
            Guid businessId,
            DateTime from,
            DateTime to,
            CancellationToken cancellationToken = default)
        {
            return await _orderRepository.GetOrderAnalyticsAsync(businessId, from, to, cancellationToken);
        }

        public async Task<List<BusinessOrderResponse>> GetBusinessRecentOrdersAsync(
            Guid businessId,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            var (items, _) = await _orderRepository.GetOrdersAsync(
                businessId,
                new OrdersQueryRequest { Page = 1, PageSize = pageSize },
                cancellationToken);

            return items;
        }

        public async Task<InventorySummaryResponse> GetBusinessInventorySummaryAsync(
            Guid businessId,
            CancellationToken cancellationToken = default)
        {
            var threshold = await _businessDashboardRepository.GetLowStockThresholdAsync(businessId, cancellationToken)
                ?? throw new BusinessNotFoundException();

            return await _businessDashboardRepository.GetInventorySummaryAsync(businessId, threshold, cancellationToken);
        }

        public async Task<ProductPerformanceResponse> GetBusinessProductPerformanceAsync(
            Guid businessId,
            DateTime from,
            DateTime to,
            CancellationToken cancellationToken = default)
        {
            return await _businessDashboardRepository.GetProductPerformanceAsync(businessId, from, to, cancellationToken);
        }

        public async Task<CustomerSnapshotResponse> GetBusinessCustomerSnapshotAsync(
            Guid businessId,
            DateTime from,
            DateTime to,
            CancellationToken cancellationToken = default)
        {
            return await _orderRepository.GetCustomerSnapshotAsync(businessId, from, to, cancellationToken);
        }

        public async Task<List<ProductFormFieldResponse>> GetBusinessMetadataShapeAsync(
            Guid businessId,
            CancellationToken cancellationToken = default)
        {
            var business = await _dashboardRepository.GetBusinessDetailCoreAsync(businessId, cancellationToken)
                ?? throw new BusinessNotFoundException();

            return MetadataShapeReader.Read(business.MetadataShape);
        }

        public async Task<List<ProductFormFieldResponse>> UpdateBusinessMetadataShapeAsync(
            Guid businessId,
            UpdateMetadataShapeRequest request,
            CancellationToken cancellationToken = default)
        {
            var business = await _dashboardRepository.GetTrackedBusinessAsync(businessId, cancellationToken)
                ?? throw new BusinessNotFoundException();

            if (business.BusinessDomainId is null)
            {
                throw new MetadataShapeDomainRequiredException();
            }

            var allowedDefinitions = await _dashboardRepository.GetActiveAttributeDefinitionsForDomainAsync(
                business.BusinessDomainId.Value, cancellationToken);

            var allowedKeys = allowedDefinitions.Select(d => d.Key).ToHashSet(StringComparer.Ordinal);

            if (request.Fields.Any(f => !allowedKeys.Contains(f.Key)))
            {
                throw new InvalidMetadataFieldKeyException();
            }

            // Full replace, matching the "snapshot, not live reference" contract on
            // Business.MetadataShape — existing Product.Metadata rows are untouched by
            // this, since nothing re-validates them against the shape retroactively.
            business.MetadataShape = BuildMetadataShapeJson(request.Fields);
            business.UpdatedAt = DateTime.UtcNow;

            await _dashboardRepository.SaveChangesAsync(cancellationToken);

            return MetadataShapeReader.Read(business.MetadataShape);
        }

        private static JsonDocument BuildMetadataShapeJson(List<UpdateMetadataShapeFieldRequest> fields)
        {
            var payload = new
            {
                fields = fields
                    .OrderBy(f => f.DisplayOrder)
                    .Select(f => new
                    {
                        key = f.Key,
                        label = f.Label,
                        valueType = f.ValueType,
                        isRequired = f.IsRequired,
                        allowedValues = f.AllowedValues,
                    }),
            };

            return JsonDocument.Parse(JsonSerializer.Serialize(payload));
        }

        // ---- product attribute definitions (domain field catalogue) ----

        public async Task<List<ProductAttributeDefinitionResponse>> GetAttributeDefinitionsAsync(
            Guid? businessDomainId,
            CancellationToken cancellationToken = default)
        {
            var definitions = await _dashboardRepository.GetAttributeDefinitionsAsync(businessDomainId, cancellationToken);

            return definitions.Select(MapAttributeDefinition).ToList();
        }

        public async Task<ProductAttributeDefinitionResponse> CreateAttributeDefinitionAsync(
            CreateProductAttributeDefinitionRequest request,
            CancellationToken cancellationToken = default)
        {
            await _domainService.EnsureDomainExistsAsync(request.BusinessDomainId, cancellationToken);

            if (!Enum.TryParse<ProductAttributeValueType>(request.ValueType, out var valueType))
            {
                throw new InvalidProductAttributeValueTypeException();
            }

            var key = request.Key.Trim();

            if (await _dashboardRepository.AttributeDefinitionKeyExistsAsync(request.BusinessDomainId, key, cancellationToken))
            {
                throw new ProductAttributeKeyAlreadyExistsException();
            }

            var definition = new ProductAttributeDefinition
            {
                Id = Guid.NewGuid(),
                BusinessDomainId = request.BusinessDomainId,
                Key = key,
                Label = request.Label.Trim(),
                ValueType = valueType,
                IsRequired = request.IsRequired,
                AllowedValues = SerializeAllowedValues(request.AllowedValues),
                DisplayOrder = request.DisplayOrder,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };

            await _dashboardRepository.CreateAttributeDefinitionAsync(definition, cancellationToken);

            return await MapAttributeDefinitionByIdAsync(definition.Id, cancellationToken);
        }

        public async Task<ProductAttributeDefinitionResponse> UpdateAttributeDefinitionAsync(
            Guid id,
            UpdateProductAttributeDefinitionRequest request,
            CancellationToken cancellationToken = default)
        {
            var definition = await _dashboardRepository.GetTrackedAttributeDefinitionAsync(id, cancellationToken)
                ?? throw new ProductAttributeDefinitionNotFoundException();

            if (!Enum.TryParse<ProductAttributeValueType>(request.ValueType, out var valueType))
            {
                throw new InvalidProductAttributeValueTypeException();
            }

            definition.Label = request.Label.Trim();
            definition.ValueType = valueType;
            definition.IsRequired = request.IsRequired;
            definition.AllowedValues = SerializeAllowedValues(request.AllowedValues);
            definition.DisplayOrder = request.DisplayOrder;
            definition.UpdatedAt = DateTime.UtcNow;

            await _dashboardRepository.SaveChangesAsync(cancellationToken);

            return await MapAttributeDefinitionByIdAsync(id, cancellationToken);
        }

        public async Task<ProductAttributeDefinitionResponse> SetAttributeDefinitionActiveAsync(
            Guid id,
            bool isActive,
            CancellationToken cancellationToken = default)
        {
            var definition = await _dashboardRepository.GetTrackedAttributeDefinitionAsync(id, cancellationToken)
                ?? throw new ProductAttributeDefinitionNotFoundException();

            definition.IsActive = isActive;
            definition.UpdatedAt = DateTime.UtcNow;

            await _dashboardRepository.SaveChangesAsync(cancellationToken);

            return await MapAttributeDefinitionByIdAsync(id, cancellationToken);
        }

        private async Task<ProductAttributeDefinitionResponse> MapAttributeDefinitionByIdAsync(
            Guid id,
            CancellationToken cancellationToken)
        {
            var definition = await _dashboardRepository.GetAttributeDefinitionWithDomainAsync(id, cancellationToken)
                ?? throw new ProductAttributeDefinitionNotFoundException();

            return MapAttributeDefinition(definition);
        }

        private static ProductAttributeDefinitionResponse MapAttributeDefinition(ProductAttributeDefinition definition)
        {
            return new ProductAttributeDefinitionResponse
            {
                Id = definition.Id,
                BusinessDomainId = definition.BusinessDomainId,
                DomainName = definition.BusinessDomain.Name,
                Key = definition.Key,
                Label = definition.Label,
                ValueType = definition.ValueType.ToString(),
                IsRequired = definition.IsRequired,
                AllowedValues = ReadAllowedValuesList(definition.AllowedValues),
                DisplayOrder = definition.DisplayOrder,
                IsActive = definition.IsActive,
                CreatedAt = definition.CreatedAt,
            };
        }

        private static JsonDocument? SerializeAllowedValues(List<string> allowedValues)
        {
            var cleaned = allowedValues
                .Select(v => v.Trim())
                .Where(v => v.Length > 0)
                .ToList();

            return cleaned.Count > 0 ? JsonDocument.Parse(JsonSerializer.Serialize(cleaned)) : null;
        }

        private static List<string> ReadAllowedValuesList(JsonDocument? allowedValues)
        {
            if (allowedValues is null || allowedValues.RootElement.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            return allowedValues.RootElement
                .EnumerateArray()
                .Where(v => v.ValueKind == JsonValueKind.String)
                .Select(v => v.GetString()!)
                .ToList();
        }

        // ---- website template customizable components (per-template capability catalogue) ----

        public async Task<List<WebsiteTemplateCustomizableComponentResponse>> GetCustomizableComponentsAsync(
            Guid? websiteTemplateId,
            CancellationToken cancellationToken = default)
        {
            var components = await _dashboardRepository.GetCustomizableComponentsAsync(websiteTemplateId, cancellationToken);

            return components.Select(MapCustomizableComponent).ToList();
        }

        public async Task<WebsiteTemplateCustomizableComponentResponse> CreateCustomizableComponentAsync(
            CreateWebsiteTemplateCustomizableComponentRequest request,
            CancellationToken cancellationToken = default)
        {
            if (await _dashboardRepository.GetTrackedWebsiteTemplateAsync(request.WebsiteTemplateId, cancellationToken) is null)
            {
                throw new WebsiteTemplateNotFoundException();
            }

            if (!Enum.TryParse<WebsiteCustomizableValueType>(request.ValueType, out var valueType))
            {
                throw new InvalidWebsiteCustomizableValueTypeException();
            }

            var key = request.Key.Trim();

            if (await _dashboardRepository.CustomizableComponentKeyExistsAsync(request.WebsiteTemplateId, key, cancellationToken))
            {
                throw new WebsiteTemplateCustomizableComponentKeyAlreadyExistsException();
            }

            var component = new WebsiteTemplateCustomizableComponent
            {
                Id = Guid.NewGuid(),
                WebsiteTemplateId = request.WebsiteTemplateId,
                Key = key,
                Label = request.Label.Trim(),
                ValueType = valueType,
                IsRequired = request.IsRequired,
                AllowedValues = SerializeAllowedValues(request.AllowedValues),
                HelpText = string.IsNullOrWhiteSpace(request.HelpText) ? null : request.HelpText.Trim(),
                DisplayOrder = request.DisplayOrder,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };

            await _dashboardRepository.CreateCustomizableComponentAsync(component, cancellationToken);

            return await MapCustomizableComponentByIdAsync(component.Id, cancellationToken);
        }

        public async Task<WebsiteTemplateCustomizableComponentResponse> UpdateCustomizableComponentAsync(
            Guid id,
            UpdateWebsiteTemplateCustomizableComponentRequest request,
            CancellationToken cancellationToken = default)
        {
            var component = await _dashboardRepository.GetTrackedCustomizableComponentAsync(id, cancellationToken)
                ?? throw new WebsiteTemplateCustomizableComponentNotFoundException();

            if (!Enum.TryParse<WebsiteCustomizableValueType>(request.ValueType, out var valueType))
            {
                throw new InvalidWebsiteCustomizableValueTypeException();
            }

            component.Label = request.Label.Trim();
            component.ValueType = valueType;
            component.IsRequired = request.IsRequired;
            component.AllowedValues = SerializeAllowedValues(request.AllowedValues);
            component.HelpText = string.IsNullOrWhiteSpace(request.HelpText) ? null : request.HelpText.Trim();
            component.DisplayOrder = request.DisplayOrder;
            component.UpdatedAt = DateTime.UtcNow;

            await _dashboardRepository.SaveChangesAsync(cancellationToken);

            return await MapCustomizableComponentByIdAsync(id, cancellationToken);
        }

        public async Task<WebsiteTemplateCustomizableComponentResponse> SetCustomizableComponentActiveAsync(
            Guid id,
            bool isActive,
            CancellationToken cancellationToken = default)
        {
            var component = await _dashboardRepository.GetTrackedCustomizableComponentAsync(id, cancellationToken)
                ?? throw new WebsiteTemplateCustomizableComponentNotFoundException();

            component.IsActive = isActive;
            component.UpdatedAt = DateTime.UtcNow;

            await _dashboardRepository.SaveChangesAsync(cancellationToken);

            return await MapCustomizableComponentByIdAsync(id, cancellationToken);
        }

        private async Task<WebsiteTemplateCustomizableComponentResponse> MapCustomizableComponentByIdAsync(
            Guid id,
            CancellationToken cancellationToken)
        {
            var component = await _dashboardRepository.GetCustomizableComponentWithTemplateAsync(id, cancellationToken)
                ?? throw new WebsiteTemplateCustomizableComponentNotFoundException();

            return MapCustomizableComponent(component);
        }

        private static WebsiteTemplateCustomizableComponentResponse MapCustomizableComponent(
            WebsiteTemplateCustomizableComponent component)
        {
            return new WebsiteTemplateCustomizableComponentResponse
            {
                Id = component.Id,
                WebsiteTemplateId = component.WebsiteTemplateId,
                TemplateName = component.WebsiteTemplate.Name,
                Key = component.Key,
                Label = component.Label,
                ValueType = component.ValueType.ToString(),
                IsRequired = component.IsRequired,
                AllowedValues = ReadAllowedValuesList(component.AllowedValues),
                HelpText = component.HelpText,
                DisplayOrder = component.DisplayOrder,
                IsActive = component.IsActive,
                CreatedAt = component.CreatedAt,
            };
        }

        // ---- website templates ----

        public async Task<PagedResult<WebsiteTemplateResponse>> GetWebsiteTemplatesAsync(
            WebsiteTemplatesQueryRequest query,
            CancellationToken cancellationToken = default)
        {
            var (items, totalCount) = await _dashboardRepository.GetWebsiteTemplatesAsync(query, cancellationToken);

            return new PagedResult<WebsiteTemplateResponse>
            {
                Items = items,
                Page = query.Page,
                PageSize = query.PageSize,
                TotalCount = totalCount,
            };
        }

        public Task<TemplateStatsResponse> GetTemplateStatsAsync(CancellationToken cancellationToken = default)
        {
            return _dashboardRepository.GetTemplateStatsAsync(cancellationToken);
        }

        public Task<List<DomainTemplateSummaryResponse>> GetDomainTemplateSummaryAsync(CancellationToken cancellationToken = default)
        {
            return _dashboardRepository.GetDomainTemplateSummaryAsync(cancellationToken);
        }

        public Task<List<KeyCountResponse>> GetRequestedTemplatesAsync(int take, CancellationToken cancellationToken = default)
        {
            return _dashboardRepository.GetRequestedTemplatesAsync(take, cancellationToken);
        }

        public async Task<List<TimeSeriesPointResponse>> GetTemplateRequestTrendAsync(
            int days, CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;
            var since = now.AddDays(-days);

            var dates = await _dashboardRepository.GetTemplateRequestCreationDatesSinceAsync(since, cancellationToken);

            return days <= 90
                ? TimeSeriesBuilder.BuildDailySeries(dates, since, now)
                : TimeSeriesBuilder.BuildMonthlySeries(dates, new DateTime(since.Year, since.Month, 1, 0, 0, 0, DateTimeKind.Utc), now);
        }

        public async Task<WebsiteTemplateResponse> CreateWebsiteTemplateAsync(
            CreateWebsiteTemplateRequest request,
            CancellationToken cancellationToken = default)
        {
            await _domainService.EnsureDomainExistsAsync(request.BusinessDomainId, cancellationToken);

            if (await _dashboardRepository.WebsiteTemplateNameExistsAsync(request.Name, cancellationToken))
            {
                throw new WebsiteTemplateNameAlreadyExistsException();
            }

            var template = new WebsiteTemplate
            {
                Id = Guid.NewGuid(),
                BusinessDomainId = request.BusinessDomainId,
                Name = request.Name,
                Label = request.Label,
                PreviewImageUrl = request.PreviewImageUrl,
                PreviewWebsiteUrl = string.IsNullOrWhiteSpace(request.PreviewWebsiteUrl) ? null : request.PreviewWebsiteUrl.Trim(),
                DisplayOrder = request.DisplayOrder,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };

            await _dashboardRepository.CreateWebsiteTemplateAsync(template, cancellationToken);

            var domains = await _domainService.GetDomainsAsync(cancellationToken);
            var domainName = domains.FirstOrDefault(d => d.Id == template.BusinessDomainId)?.Name ?? string.Empty;

            await _auditLogService.LogAsync(
                AuditEventType.Template, "WebsiteTemplateCreated",
                $"Created website template \"{template.Label}\".",
                success: true, actorUserId: _currentUserAccessor.UserId,
                entityType: "WebsiteTemplate", entityId: template.Id,
                cancellationToken: cancellationToken);

            return new WebsiteTemplateResponse
            {
                Id = template.Id,
                BusinessDomainId = template.BusinessDomainId,
                DomainName = domainName,
                Name = template.Name,
                Label = template.Label,
                PreviewImageUrl = template.PreviewImageUrl,
                PreviewWebsiteUrl = template.PreviewWebsiteUrl,
                IsActive = template.IsActive,
                DisplayOrder = template.DisplayOrder,
                BusinessesUsingIt = 0,
                RequestCount = 0,
                ActiveCustomizableComponentCount = 0,
                CreatedAt = template.CreatedAt,
            };
        }

        public async Task<string> UploadWebsiteTemplateImageAsync(
            IFormFile file,
            CancellationToken cancellationToken = default)
        {
            return await _websiteTemplateImageService.SaveAsync(file, cancellationToken);
        }

        public async Task<WebsiteTemplateDetailResponse> GetWebsiteTemplateDetailAsync(
            Guid websiteTemplateId,
            CancellationToken cancellationToken = default)
        {
            return await _dashboardRepository.GetWebsiteTemplateDetailAsync(websiteTemplateId, cancellationToken)
                ?? throw new WebsiteTemplateNotFoundException();
        }

        public async Task<WebsiteTemplateResponse> UpdateWebsiteTemplateAsync(
            Guid websiteTemplateId,
            UpdateWebsiteTemplateRequest request,
            CancellationToken cancellationToken = default)
        {
            var template = await _dashboardRepository.GetTrackedWebsiteTemplateAsync(websiteTemplateId, cancellationToken)
                ?? throw new WebsiteTemplateNotFoundException();

            template.Label = request.Label;
            template.PreviewImageUrl = request.PreviewImageUrl;
            template.PreviewWebsiteUrl = string.IsNullOrWhiteSpace(request.PreviewWebsiteUrl) ? null : request.PreviewWebsiteUrl.Trim();
            template.DisplayOrder = request.DisplayOrder;
            template.UpdatedAt = DateTime.UtcNow;

            await _dashboardRepository.SaveChangesAsync(cancellationToken);

            await _auditLogService.LogAsync(
                AuditEventType.Template, "WebsiteTemplateUpdated",
                $"Updated website template \"{template.Label}\".",
                success: true, actorUserId: _currentUserAccessor.UserId,
                entityType: "WebsiteTemplate", entityId: websiteTemplateId,
                cancellationToken: cancellationToken);

            return await MapToResponseAsync(websiteTemplateId, cancellationToken);
        }

        public async Task<WebsiteTemplateResponse> DeactivateWebsiteTemplateAsync(
            Guid websiteTemplateId,
            CancellationToken cancellationToken = default)
        {
            var template = await _dashboardRepository.GetTrackedWebsiteTemplateAsync(websiteTemplateId, cancellationToken)
                ?? throw new WebsiteTemplateNotFoundException();

            // A soft delete: IsActive already exists precisely so retiring a template
            // never removes it out from under a business that already lives on it.
            template.IsActive = false;
            template.UpdatedAt = DateTime.UtcNow;

            await _dashboardRepository.SaveChangesAsync(cancellationToken);

            await _auditLogService.LogAsync(
                AuditEventType.Template, "WebsiteTemplateDeactivated",
                $"Deactivated website template \"{template.Label}\".",
                success: true, actorUserId: _currentUserAccessor.UserId,
                entityType: "WebsiteTemplate", entityId: websiteTemplateId,
                cancellationToken: cancellationToken);

            return await MapToResponseAsync(websiteTemplateId, cancellationToken);
        }

        public async Task<WebsiteTemplateResponse> ReactivateWebsiteTemplateAsync(
            Guid websiteTemplateId,
            CancellationToken cancellationToken = default)
        {
            var template = await _dashboardRepository.GetTrackedWebsiteTemplateAsync(websiteTemplateId, cancellationToken)
                ?? throw new WebsiteTemplateNotFoundException();

            // Makes the template available for new selections again - existing
            // businesses were never affected by deactivation in the first place, so
            // there's nothing to restore for them here.
            template.IsActive = true;
            template.UpdatedAt = DateTime.UtcNow;

            await _dashboardRepository.SaveChangesAsync(cancellationToken);

            await _auditLogService.LogAsync(
                AuditEventType.Template, "WebsiteTemplateReactivated",
                $"Reactivated website template \"{template.Label}\".",
                success: true, actorUserId: _currentUserAccessor.UserId,
                entityType: "WebsiteTemplate", entityId: websiteTemplateId,
                cancellationToken: cancellationToken);

            return await MapToResponseAsync(websiteTemplateId, cancellationToken);
        }

        private async Task<WebsiteTemplateResponse> MapToResponseAsync(
            Guid websiteTemplateId,
            CancellationToken cancellationToken)
        {
            var detail = await _dashboardRepository.GetWebsiteTemplateDetailAsync(websiteTemplateId, cancellationToken)
                ?? throw new WebsiteTemplateNotFoundException();

            return new WebsiteTemplateResponse
            {
                Id = detail.Id,
                BusinessDomainId = detail.BusinessDomainId,
                DomainName = detail.DomainName,
                Name = detail.Name,
                Label = detail.Label,
                PreviewImageUrl = detail.PreviewImageUrl,
                PreviewWebsiteUrl = detail.PreviewWebsiteUrl,
                IsActive = detail.IsActive,
                DisplayOrder = detail.DisplayOrder,
                BusinessesUsingIt = detail.Businesses.Count,
                RequestCount = detail.RequestCount,
                ActiveCustomizableComponentCount = detail.ActiveCustomizableComponentCount,
                CreatedAt = detail.CreatedAt,
            };
        }

        // ---- customers ----

        public async Task<PagedResult<DashboardCustomerResponse>> GetCustomersAsync(
            CustomersQueryRequest query,
            CancellationToken cancellationToken = default)
        {
            var (items, totalCount) = await _dashboardRepository.GetCustomersAsync(query, cancellationToken);

            return new PagedResult<DashboardCustomerResponse>
            {
                Items = items,
                Page = query.Page,
                PageSize = query.PageSize,
                TotalCount = totalCount,
            };
        }

        public async Task<DashboardCustomerDetailResponse> GetCustomerDetailAsync(
            Guid customerId,
            CancellationToken cancellationToken = default)
        {
            var detail = await _dashboardRepository.GetCustomerDetailAsync(customerId, cancellationToken)
                ?? throw new Exceptions.CustomerAuth.CustomerNotFoundException();

            detail.RecentActivity = await _auditLogService.GetCustomerActivityAsync(
                customerId, CustomerRecentActivityTake, cancellationToken);

            return detail;
        }

        public async Task<DashboardCustomerDetailResponse> UpdateCustomerAsync(
            Guid customerId,
            UpdateCustomerRequest request,
            CancellationToken cancellationToken = default)
        {
            var customer = await _dashboardRepository.GetTrackedCustomerAsync(customerId, cancellationToken)
                ?? throw new Exceptions.CustomerAuth.CustomerNotFoundException();

            customer.FirstName = request.FirstName;
            customer.LastName = request.LastName;
            customer.Phone = request.Phone;
            customer.UpdatedAt = DateTime.UtcNow;

            await _dashboardRepository.SaveChangesAsync(cancellationToken);

            await _auditLogService.LogAsync(
                AuditEventType.UserManagement, "CustomerProfileUpdated", $"Updated profile for {customer.Email}.",
                success: true, actorUserId: _currentUserAccessor.UserId,
                entityType: "Customer", entityId: customerId, cancellationToken: cancellationToken);

            return await GetCustomerDetailAsync(customerId, cancellationToken);
        }

        public async Task<RevokeCustomerSessionsResponse> RevokeCustomerSessionsAsync(
            Guid customerId,
            CancellationToken cancellationToken = default)
        {
            var revokedCount = await _customerRefreshTokenRepository.RevokeAllForCustomerAsync(customerId, cancellationToken);

            await _auditLogService.LogAsync(
                AuditEventType.Security, "CustomerSessionRevoked", $"Revoked {revokedCount} session(s) for a customer.",
                success: true, actorUserId: _currentUserAccessor.UserId,
                entityType: "Customer", entityId: customerId, cancellationToken: cancellationToken);

            return new RevokeCustomerSessionsResponse { RevokedSessionsCount = revokedCount };
        }

        public Task<CustomerStatsResponse> GetCustomerStatsAsync(
            int newCustomersPeriodDays, CancellationToken cancellationToken = default)
        {
            return _dashboardRepository.GetCustomerStatsAsync(newCustomersPeriodDays, cancellationToken);
        }

        public async Task<List<TimeSeriesPointResponse>> GetCustomerGrowthAsync(
            int days, CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;
            var since = now.AddDays(-days);

            var dates = await _dashboardRepository.GetCustomerCreationDatesSinceAsync(since, cancellationToken);

            // Daily buckets stay readable up to ~90 days; beyond that, monthly - same
            // granularity switch a chart with a fixed-width x-axis needs regardless of
            // the window length.
            return days <= 90
                ? TimeSeriesBuilder.BuildDailySeries(dates, since, now)
                : TimeSeriesBuilder.BuildMonthlySeries(dates, new DateTime(since.Year, since.Month, 1, 0, 0, 0, DateTimeKind.Utc), now);
        }

        public Task<List<TopCustomerResponse>> GetTopCustomersAsync(
            TopCustomersRankBy rankBy, string? currency, int take, CancellationToken cancellationToken = default)
        {
            return _dashboardRepository.GetTopCustomersAsync(
                rankBy, string.IsNullOrWhiteSpace(currency) ? DefaultTopCustomerCurrency : currency, take, cancellationToken);
        }

        public Task<List<KeyCountResponse>> GetCustomerDistributionByBusinessAsync(CancellationToken cancellationToken = default)
        {
            return _dashboardRepository.GetCustomerDistributionByBusinessAsync(cancellationToken);
        }

        public Task<List<DashboardCustomerResponse>> GetRecentCustomersAsync(
            int take, CancellationToken cancellationToken = default)
        {
            return _dashboardRepository.GetRecentCustomersAsync(take, cancellationToken);
        }

        public Task<List<BusinessOptionResponse>> GetBusinessOptionsAsync(CancellationToken cancellationToken = default)
        {
            return _dashboardRepository.GetBusinessOptionsAsync(cancellationToken);
        }

        public async Task<PagedResult<CustomerOrderResponse>> GetCustomerOrdersAsync(
            Guid customerId, Guid? businessId, int page, int pageSize, CancellationToken cancellationToken = default)
        {
            var (items, totalCount) = await _dashboardRepository.GetCustomerOrdersAsync(
                customerId, businessId, page, pageSize, cancellationToken);

            return new PagedResult<CustomerOrderResponse>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
            };
        }

        public Task<List<CustomerSpendPointResponse>> GetCustomerSpendOverTimeAsync(
            Guid customerId, CancellationToken cancellationToken = default)
        {
            return _dashboardRepository.GetCustomerSpendOverTimeAsync(customerId, cancellationToken);
        }

        // ---- website template requests ----

        public async Task<PagedResult<WebsiteTemplateRequestSummaryResponse>> GetWebsiteTemplateRequestsAsync(
            WebsiteTemplateRequestsQueryRequest query,
            CancellationToken cancellationToken = default)
        {
            var (items, totalCount) = await _websiteTemplateRequestRepository.GetPagedAsync(query, cancellationToken);

            return new PagedResult<WebsiteTemplateRequestSummaryResponse>
            {
                Items = items,
                Page = query.Page,
                PageSize = query.PageSize,
                TotalCount = totalCount,
            };
        }

        public async Task<WebsiteTemplateRequestDetailResponse> GetWebsiteTemplateRequestAsync(
            Guid websiteTemplateRequestId,
            CancellationToken cancellationToken = default)
        {
            return await _websiteTemplateRequestRepository.GetDetailByIdAsync(websiteTemplateRequestId, cancellationToken)
                ?? throw new WebsiteTemplateRequestNotFoundException();
        }

        public async Task<WebsiteTemplateRequestDetailResponse> StartWebsiteTemplateRequestBuildAsync(
            Guid websiteTemplateRequestId,
            CancellationToken cancellationToken = default)
        {
            var request = await _websiteTemplateRequestRepository.GetTrackedByIdAsync(websiteTemplateRequestId, cancellationToken)
                ?? throw new WebsiteTemplateRequestNotFoundException();

            if (request.Status != WebsiteTemplateRequestStatus.Pending)
            {
                throw new WebsiteTemplateRequestInvalidStatusTransitionException();
            }

            request.Status = WebsiteTemplateRequestStatus.InProgress;
            request.BuildStartedAt = DateTime.UtcNow;

            await _websiteTemplateRequestRepository.SaveChangesAsync(cancellationToken);

            _backgroundJobClient.Enqueue<NotifyOwnerOfWebsiteBuildStartedJob>(
                job => job.ExecuteAsync(websiteTemplateRequestId));

            return await GetWebsiteTemplateRequestAsync(websiteTemplateRequestId, cancellationToken);
        }

        public async Task<WebsiteTemplateRequestDetailResponse> CloseWebsiteTemplateRequestAsync(
            Guid websiteTemplateRequestId,
            Guid closedByUserId,
            CloseWebsiteTemplateRequestRequest request,
            CancellationToken cancellationToken = default)
        {
            var websiteTemplateRequest = await _websiteTemplateRequestRepository.GetTrackedByIdAsync(
                websiteTemplateRequestId, cancellationToken)
                ?? throw new WebsiteTemplateRequestNotFoundException();

            if (websiteTemplateRequest.Status == WebsiteTemplateRequestStatus.Closed)
            {
                throw new WebsiteTemplateRequestInvalidStatusTransitionException();
            }

            websiteTemplateRequest.Status = WebsiteTemplateRequestStatus.Closed;
            websiteTemplateRequest.ClosedAt = DateTime.UtcNow;
            websiteTemplateRequest.ClosedByUserId = closedByUserId;
            websiteTemplateRequest.FinalWebsiteUrl = request.FinalWebsiteUrl.Trim();

            await _websiteTemplateRequestRepository.SaveChangesAsync(cancellationToken);

            // The template and URL this business is now actually running — set here,
            // on close, rather than when the request was merely submitted, and free
            // to overwrite an earlier value from a prior closed request.
            await _websiteTemplateRequestRepository.SetBusinessActiveWebsiteTemplateAsync(
                websiteTemplateRequest.BusinessId,
                websiteTemplateRequest.WebsiteTemplateId,
                websiteTemplateRequest.FinalWebsiteUrl,
                cancellationToken);

            _backgroundJobClient.Enqueue<NotifyOwnerOfWebsiteRequestClosedJob>(
                job => job.ExecuteAsync(websiteTemplateRequestId));

            return await GetWebsiteTemplateRequestAsync(websiteTemplateRequestId, cancellationToken);
        }

        // ---- subscriptions (platform-wide, Subscriptions tab) ----

        public async Task<PagedResult<AdminSubscriptionListItemResponse>> GetSubscriptionsAsync(
            SubscriptionsQueryRequest query,
            CancellationToken cancellationToken = default)
        {
            var (items, totalCount) = await _dashboardRepository.GetSubscriptionsAsync(query, cancellationToken);

            return new PagedResult<AdminSubscriptionListItemResponse>
            {
                Items = items,
                Page = query.Page,
                PageSize = query.PageSize,
                TotalCount = totalCount,
            };
        }

        public async Task<List<RecentSubscriptionActivityEntryResponse>> GetRecentSubscriptionActivityAsync(
            int take,
            CancellationToken cancellationToken = default)
        {
            return await _dashboardRepository.GetRecentSubscriptionActivityAsync(take, cancellationToken);
        }

        public async Task<List<SubscriptionHistoryEntryResponse>> GetBusinessSubscriptionHistoryAsync(
            Guid businessId,
            CancellationToken cancellationToken = default)
        {
            return await _businessDashboardService.GetSubscriptionHistoryAsync(businessId, cancellationToken);
        }

        public async Task<BusinessSubscriptionResponse> ChangeBusinessSubscriptionAsync(
            Guid businessId,
            Guid subscriptionPlanId,
            CancellationToken cancellationToken = default)
        {
            var result = await _businessDashboardService.SubscribeToPlanAsync(businessId, subscriptionPlanId, cancellationToken);

            await _auditLogService.LogAsync(
                AuditEventType.Subscription, "BusinessSubscriptionChanged",
                $"Changed a business's subscription to {result.PlanName} ({result.BillingInterval}).",
                success: true, actorUserId: _currentUserAccessor.UserId,
                entityType: "Business", entityId: businessId, businessId: businessId,
                cancellationToken: cancellationToken);

            return result;
        }

        public async Task<BusinessSubscriptionResponse> CancelBusinessSubscriptionAsync(
            Guid businessId,
            CancellationToken cancellationToken = default)
        {
            var result = await _businessDashboardService.CancelSubscriptionAsync(businessId, cancellationToken);

            await _auditLogService.LogAsync(
                AuditEventType.Subscription, "BusinessSubscriptionCancelled",
                "Cancelled a business's subscription.",
                success: true, actorUserId: _currentUserAccessor.UserId,
                entityType: "Business", entityId: businessId, businessId: businessId,
                cancellationToken: cancellationToken);

            return result;
        }

        // ---- audit / security ----

        public Task<PagedResult<AuditLogResponse>> GetAuditLogsAsync(
            AuditLogQueryRequest query, CancellationToken cancellationToken = default)
        {
            return _auditLogService.GetLogsAsync(query, cancellationToken);
        }

        public async Task<SecurityOverviewResponse> GetSecurityOverviewAsync(CancellationToken cancellationToken = default)
        {
            var overview = await _auditLogService.GetSecurityOverviewAsync(cancellationToken);
            overview.ActiveSessions = await _dashboardRepository.CountActiveSessionsAsync(cancellationToken);

            return overview;
        }

        public Task<FailedLoginStatsResponse> GetFailedLoginStatsAsync(CancellationToken cancellationToken = default)
        {
            return _auditLogService.GetFailedLoginStatsAsync(cancellationToken);
        }

        public Task<List<SecurityAlertResponse>> GetSecurityAlertsAsync(CancellationToken cancellationToken = default)
        {
            return _auditLogService.GetSecurityAlertsAsync(cancellationToken);
        }
    }
}
