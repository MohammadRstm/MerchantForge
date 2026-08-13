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
    }
}