using MerchForge.api.Models;

namespace MerchForge.api.Services.Auth.interfaces
{
    public interface IRefreshTokenService
    {
        Task<(string Token, RefreshToken Entity)> CreateAsync(
               User user,
               CancellationToken cancellationToken = default);

        Task<RefreshToken?> GetValidTokenAsync(
            string token,
            CancellationToken cancellationToken = default);

        Task RevokeAsync(
            RefreshToken refreshToken,
            CancellationToken cancellationToken = default);

        Task<(string Token, RefreshToken Entity)> RotateAsync(
            RefreshToken currentToken,
            CancellationToken cancellationToken = default);
    }
}
