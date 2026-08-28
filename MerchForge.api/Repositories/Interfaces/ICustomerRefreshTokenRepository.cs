using MerchForge.api.Models;

namespace MerchForge.api.Repositories.Interfaces
{
    public interface ICustomerRefreshTokenRepository
    {
        Task AddAsync(
            CustomerRefreshToken token,
            CancellationToken cancellationToken = default);

        Task<CustomerRefreshToken?> GetAsync(
            string tokenHash,
            CancellationToken cancellationToken = default);

        Task UpdateAsync(
            CustomerRefreshToken token,
            CancellationToken cancellationToken = default);

        Task<int> RevokeAllForCustomerAsync(
            Guid customerId,
            CancellationToken cancellationToken = default);
    }
}
