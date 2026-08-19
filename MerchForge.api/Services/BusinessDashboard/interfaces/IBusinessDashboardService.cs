using MerchForge.api.DTOs.BusinessDashboard;
using MerchForge.api.DTOs.Common;

namespace MerchForge.api.Services.BusinessDashboard.interfaces
{
    public interface IBusinessDashboardService
    {
        Task<BusinessDashboardStatsResponse> GetStatsAsync(
            Guid businessId,
            CancellationToken cancellationToken = default);

        Task<PagedResult<BusinessProductResponse>> GetProductsAsync(
            Guid businessId,
            ProductsQueryRequest query,
            CancellationToken cancellationToken = default);

        Task<List<BusinessMemberResponse>> GetMembersAsync(
            Guid businessId,
            CancellationToken cancellationToken = default);

        Task<BusinessSubscriptionResponse?> GetSubscriptionAsync(
            Guid businessId,
            CancellationToken cancellationToken = default);
    }
}
