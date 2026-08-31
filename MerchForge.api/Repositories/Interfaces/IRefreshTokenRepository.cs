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

        Task<int> RevokeAllForBusinessAsync(
            Guid businessId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Revokes every currently-active refresh token platform-wide except the
        /// acting Super Admin's own - the same self-lockout guard as
        /// RevokeAllForUserAsync's CannotRevokeOwnSessionException, just applied by
        /// exclusion instead of by rejecting the whole call.
        /// </summary>
        Task<int> RevokeAllAsync(Guid excludeUserId, CancellationToken cancellationToken = default);
    }
}
