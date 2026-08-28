using MerchForge.api.Configurations;
using MerchForge.api.Models;
using MerchForge.api.Repositories.Interfaces;
using MerchForge.api.Services.CustomerAuth.interfaces;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

namespace MerchForge.api.Services.CustomerAuth
{
    /// <summary>Mirrors RefreshTokenService exactly, for CustomerRefreshToken instead of RefreshToken.</summary>
    public class CustomerRefreshTokenService : ICustomerRefreshTokenService
    {
        private readonly ICustomerRefreshTokenRepository _customerRefreshTokenRepository;
        private readonly int _refreshTokenExpirationDays;

        public CustomerRefreshTokenService(
            ICustomerRefreshTokenRepository customerRefreshTokenRepository,
            IOptions<CustomerRefreshTokenOptions> options)
        {
            _customerRefreshTokenRepository = customerRefreshTokenRepository;
            _refreshTokenExpirationDays = options.Value.ExpirationDays;
        }

        public async Task<(string Token, CustomerRefreshToken Entity)> CreateAsync(
            Customer customer,
            CancellationToken cancellationToken = default)
        {
            var rawToken = GenerateToken();
            var tokenHash = HashToken(rawToken);

            var refreshToken = new CustomerRefreshToken
            {
                Id = Guid.NewGuid(),
                CustomerId = customer.Id,
                TokenHash = tokenHash,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(_refreshTokenExpirationDays)
            };

            await _customerRefreshTokenRepository.AddAsync(refreshToken, cancellationToken);

            return (rawToken, refreshToken);
        }

        public async Task<CustomerRefreshToken?> GetValidTokenAsync(
            string token,
            CancellationToken cancellationToken = default)
        {
            var tokenHash = HashToken(token);

            var refreshToken = await _customerRefreshTokenRepository.GetAsync(tokenHash, cancellationToken);

            if (refreshToken is null)
            {
                return null;
            }

            if (refreshToken.RevokedAt.HasValue)
            {
                return null;
            }

            if (refreshToken.ExpiresAt <= DateTime.UtcNow)
            {
                return null;
            }

            return refreshToken;
        }

        public async Task<(string Token, CustomerRefreshToken Entity)> RotateAsync(
            CustomerRefreshToken currentToken,
            CancellationToken cancellationToken)
        {
            currentToken.RevokedAt = DateTime.UtcNow;

            var rawToken = GenerateToken();
            var tokenHash = HashToken(rawToken);

            var newToken = new CustomerRefreshToken
            {
                Id = Guid.NewGuid(),
                CustomerId = currentToken.CustomerId,
                TokenHash = tokenHash,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(_refreshTokenExpirationDays)
            };

            await _customerRefreshTokenRepository.AddAsync(newToken, cancellationToken);

            return (rawToken, newToken);
        }

        public async Task RevokeAsync(
            CustomerRefreshToken refreshToken,
            CancellationToken cancellationToken = default)
        {
            if (refreshToken.RevokedAt.HasValue)
            {
                return;
            }

            refreshToken.RevokedAt = DateTime.UtcNow;

            await _customerRefreshTokenRepository.UpdateAsync(refreshToken, cancellationToken);
        }

        private static string GenerateToken()
        {
            var randomBytes = RandomNumberGenerator.GetBytes(64);
            return Convert.ToBase64String(randomBytes);
        }

        private static string HashToken(string token)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
            return Convert.ToHexString(bytes);
        }
    }
}
