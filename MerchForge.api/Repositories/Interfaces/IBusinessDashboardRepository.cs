using MerchForge.api.DTOs.BusinessDashboard;
using MerchForge.api.DTOs.Common;

namespace MerchForge.api.Repositories.Interfaces
{
    public interface IBusinessDashboardRepository
    {
        Task<(string Name, DateTime CreatedAt)?> GetBusinessSummaryAsync(
            Guid businessId,
            CancellationToken cancellationToken = default);

        Task<int> CountMembersAsync(Guid businessId, CancellationToken cancellationToken = default);

        Task<int> CountProductsAsync(Guid businessId, CancellationToken cancellationToken = default);

        Task<int> CountProductDraftsAsync(Guid businessId, CancellationToken cancellationToken = default);

        Task<(decimal? Average, decimal? Min, decimal? Max)> GetProductPriceStatsAsync(
            Guid businessId,
            CancellationToken cancellationToken = default);

        Task<List<KeyCountResponse>> GetProductsByCategoryAsync(Guid businessId, CancellationToken cancellationToken = default);

        Task<List<KeyCountResponse>> GetProductDraftsByStatusAsync(Guid businessId, CancellationToken cancellationToken = default);

        Task<List<KeyCountResponse>> GetMembersByRoleAsync(Guid businessId, CancellationToken cancellationToken = default);

        Task<List<DateTime>> GetProductCreationDatesSinceAsync(
            Guid businessId,
            DateTime since,
            CancellationToken cancellationToken = default);

        Task<(List<BusinessProductResponse> Items, int TotalCount)> GetProductsAsync(
            Guid businessId,
            ProductsQueryRequest query,
            CancellationToken cancellationToken = default);

        Task<List<BusinessMemberResponse>> GetMembersAsync(Guid businessId, CancellationToken cancellationToken = default);
    }
}
