using MerchForge.api.Configurations;
using MerchForge.api.Data;
using MerchForge.api.Models;
using MerchForge.api.Repositories.Interfaces;
using MerchForge.api.Services.Auth.interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

namespace MerchForge.api.Services.Auth
{
    public class RefreshTokenService : IRefreshTokenService
    {
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly int _refreshTokenExpirationDays;

        public RefreshTokenService(
            IRefreshTokenRepository refreshTokenRepository,
            IOptions<RefreshTokenOptions> options)
        {
            _refreshTokenRepository = refreshTokenRepository;
            _refreshTokenExpirationDays = options.Value.ExpirationDays;
        }

        public async Task<(string Token, RefreshToken Entity)> CreateAsync(
           User user,
           CancellationToken cancellationToken = default)
        {
            var rawToken = GenerateToken();

            var tokenHash = HashToken(rawToken);

            var refreshToken = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                TokenHash = tokenHash,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(_refreshTokenExpirationDays)
            };

            await _refreshTokenRepository.AddAsync(refreshToken, cancellationToken);

            return (rawToken, refreshToken);
        }

        public async Task<RefreshToken?> GetValidTokenAsync(
           string token,
           CancellationToken cancellationToken = default)
        {
            var tokenHash = HashToken(token);

            var refreshToken = await _refreshTokenRepository.GetAsync(tokenHash , cancellationToken);

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

        public async Task<(string Token ,RefreshToken Entity)> RotateAsync(
            RefreshToken currentToken,
            CancellationToken cancellationToken
            )
        {
            // revoke token
            currentToken.RevokedAt = DateTime.UtcNow;

            // generate new refresh token
            var rawToken = GenerateToken();

            var tokenHash = HashToken(rawToken);

            var newToken = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = currentToken.UserId,
                TokenHash = tokenHash,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(_refreshTokenExpirationDays)
            };

            await _refreshTokenRepository.AddAsync(newToken, cancellationToken);// the _db.save on add_token will also save the revoke_token

            return (rawToken , newToken);
        }

        public async Task RevokeAsync(
           RefreshToken refreshToken,
           CancellationToken cancellationToken = default)
        {
            if (refreshToken.RevokedAt.HasValue)
            {
                return;
            }

            refreshToken.RevokedAt = DateTime.UtcNow;

            await _refreshTokenRepository.UpdateAsync(
              refreshToken,
              cancellationToken);
        }

        private static string GenerateToken()
        {
            var randomBytes = RandomNumberGenerator.GetBytes(64);

            return Convert.ToBase64String(randomBytes);
        }

        private static string HashToken(string token)
        {
            var bytes = SHA256.HashData(
                Encoding.UTF8.GetBytes(token));

            return Convert.ToHexString(bytes);
        }
    }
}
