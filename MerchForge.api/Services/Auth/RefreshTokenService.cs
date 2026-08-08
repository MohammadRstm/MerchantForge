using MerchForge.api.Data;
using MerchForge.api.Models;
using MerchForge.api.Services.Auth.interfaces;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace MerchForge.api.Services.Auth
{
    public class RefreshTokenService : IRefreshTokenService
    {
        private readonly MerchForgeDbContext _db;

        private int RefreshTokenExpirationDays = 30;

        public RefreshTokenService(MerchForgeDbContext db)
        {
            _db = db;
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
                ExpiresAt = DateTime.UtcNow.AddDays(RefreshTokenExpirationDays)
            };

            _db.RefreshTokens.Add(refreshToken);

            await _db.SaveChangesAsync(cancellationToken);

            return (rawToken, refreshToken);
        }

        public async Task<RefreshToken?> GetValidTokenAsync(
           string token,
           CancellationToken cancellationToken = default)
        {
            var tokenHash = HashToken(token);

            var refreshToken = await _db.RefreshTokens
                .Include(x => x.User)
                .FirstOrDefaultAsync(
                    x => x.TokenHash == tokenHash,
                    cancellationToken);

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
            await using var transaction =
                await _db.Database.BeginTransactionAsync(cancellationToken);

            try
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
                    ExpiresAt = DateTime.UtcNow.AddDays(RefreshTokenExpirationDays)
                };

                await _db.RefreshTokens.AddAsync(newToken, cancellationToken);

                await _db.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return (rawToken , newToken);
            }catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }

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

            await _db.SaveChangesAsync(cancellationToken);
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
