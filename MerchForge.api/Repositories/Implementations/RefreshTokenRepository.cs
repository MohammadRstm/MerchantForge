using MerchForge.api.Data;
using MerchForge.api.Models;
using MerchForge.api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;



namespace MerchForge.api.Repositories.Implementations
{
    public class RefreshTokenRepository : IRefreshTokenRepository
    {
       private readonly MerchForgeDbContext _db;

        public RefreshTokenRepository(MerchForgeDbContext db)
        {
            _db = db;
        }

        public async Task AddAsync(
          RefreshToken token, CancellationToken cancellationToken = default)
        {
            _db.RefreshTokens.Add(token);

            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task<RefreshToken?> GetAsync(
            string tokenHash, CancellationToken cancellationToken = default)
        {
            return await _db.RefreshTokens
                .Include(x => x.User)
                .FirstOrDefaultAsync(
                    x => x.TokenHash == tokenHash,
                    cancellationToken);
        }
        public async Task UpdateAsync(
            RefreshToken refreshToken,
            CancellationToken cancellationToken = default)
        {
            _db.RefreshTokens.Update(refreshToken);

            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task<int> RevokeAllForUserAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;

            return await _db.RefreshTokens
                .Where(rt =>
                    rt.UserId == userId &&
                    rt.RevokedAt == null &&
                    rt.ExpiresAt > now)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(rt => rt.RevokedAt, now),
                    cancellationToken);
        }

        public async Task<int> RevokeAllForBusinessAsync(
            Guid businessId,
            CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;

            var memberUserIds = _db.BusinessUsers
                .Where(bu => bu.BusinessId == businessId)
                .Select(bu => bu.UserId);

            return await _db.RefreshTokens
                .Where(rt =>
                    memberUserIds.Contains(rt.UserId) &&
                    rt.RevokedAt == null &&
                    rt.ExpiresAt > now)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(rt => rt.RevokedAt, now),
                    cancellationToken);
        }

        public async Task<int> RevokeAllAsync(Guid excludeUserId, CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;

            return await _db.RefreshTokens
                .Where(rt =>
                    rt.UserId != excludeUserId &&
                    rt.RevokedAt == null &&
                    rt.ExpiresAt > now)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(rt => rt.RevokedAt, now),
                    cancellationToken);
        }
    }
}