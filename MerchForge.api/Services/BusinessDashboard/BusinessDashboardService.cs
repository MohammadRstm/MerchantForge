using MerchForge.api.DTOs.BusinessDashboard;
using MerchForge.api.DTOs.Common;
using MerchForge.api.Exceptions.BusinessDashboard;
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

        public BusinessDashboardService(
            IBusinessDashboardRepository businessDashboardRepository,
            ISubscriptionRepository subscriptionRepository)
        {
            _businessDashboardRepository = businessDashboardRepository;
            _subscriptionRepository = subscriptionRepository;
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
    }
}
