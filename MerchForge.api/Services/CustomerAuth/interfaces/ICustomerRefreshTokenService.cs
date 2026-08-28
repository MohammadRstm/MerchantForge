using MerchForge.api.Models;

namespace MerchForge.api.Services.CustomerAuth.interfaces
{
    public interface ICustomerRefreshTokenService
    {
        Task<(string Token, CustomerRefreshToken Entity)> CreateAsync(
            Customer customer,
            CancellationToken cancellationToken = default);

        Task<CustomerRefreshToken?> GetValidTokenAsync(
            string token,
            CancellationToken cancellationToken = default);

        Task<(string Token, CustomerRefreshToken Entity)> RotateAsync(
            CustomerRefreshToken currentToken,
            CancellationToken cancellationToken);

        Task RevokeAsync(
            CustomerRefreshToken refreshToken,
            CancellationToken cancellationToken = default);
    }
}
