using MerchForge.api.Data;
using MerchForge.api.Models;
using MerchForge.api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MerchForge.api.Repositories.Implementations
{
    public class CustomerRefreshTokenRepository : ICustomerRefreshTokenRepository
    {
        private readonly MerchForgeDbContext _db;

        public CustomerRefreshTokenRepository(MerchForgeDbContext db)
        {
            _db = db;
        }

        public async Task AddAsync(
            CustomerRefreshToken token,
            CancellationToken cancellationToken = default)
        {
            _db.CustomerRefreshTokens.Add(token);
            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task<CustomerRefreshToken?> GetAsync(
            string tokenHash,
            CancellationToken cancellationToken = default)
        {
            return await _db.CustomerRefreshTokens
                .Include(x => x.Customer)
                .FirstOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);
        }

        public async Task UpdateAsync(
            CustomerRefreshToken token,
            CancellationToken cancellationToken = default)
        {
            _db.CustomerRefreshTokens.Update(token);
            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task<int> RevokeAllForCustomerAsync(
            Guid customerId,
            CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;

            return await _db.CustomerRefreshTokens
                .Where(rt =>
                    rt.CustomerId == customerId &&
                    rt.RevokedAt == null &&
                    rt.ExpiresAt > now)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(rt => rt.RevokedAt, now),
                    cancellationToken);
        }
    }
}
