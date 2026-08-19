using MerchForge.api.Models;

namespace MerchForge.api.Repositories.Interfaces
{
    public interface IRefreshTokenRepository
    {

        Task AddAsync(
            RefreshToken token, CancellationToken cancellationToken = default);

        Task<RefreshToken?> GetAsync(
            string tokenHash, CancellationToken cancellationToken = default);
        Task UpdateAsync(
            RefreshToken refreshToken,
            CancellationToken cancellationToken = default);

        Task<int> RevokeAllForUserAsync(
            Guid userId,
            CancellationToken cancellationToken = default);
    }
}
