using MerchForge.api.DTOs.Dashboard;

namespace MerchForge.api.Repositories.Interfaces
{
    public interface IDashboardRepository
    {
        Task<int> CountUsersAsync(CancellationToken cancellationToken = default);

        Task<int> CountBusinessesAsync(CancellationToken cancellationToken = default);

        Task<int> CountProductsAsync(CancellationToken cancellationToken = default);

        Task<int> CountProductDraftsAsync(CancellationToken cancellationToken = default);

        Task<int> CountPendingInvitationsAsync(CancellationToken cancellationToken = default);

        Task<List<RoleCountResponse>> GetUserCountsBySystemRoleAsync(CancellationToken cancellationToken = default);

        Task<List<RoleCountResponse>> GetBusinessUserCountsByRoleAsync(CancellationToken cancellationToken = default);

        Task<List<DateTime>> GetBusinessCreationDatesSinceAsync(DateTime since, CancellationToken cancellationToken = default);

        Task<List<DateTime>> GetProductCreationDatesSinceAsync(DateTime since, CancellationToken cancellationToken = default);
    }
}
