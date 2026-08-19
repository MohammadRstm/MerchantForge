using MerchForge.api.Data;
using MerchForge.api.DTOs.Dashboard;
using MerchForge.api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MerchForge.api.Repositories.Implementations
{
    public class DashboardRepository : IDashboardRepository
    {
        private readonly MerchForgeDbContext _db;

        public DashboardRepository(MerchForgeDbContext db)
        {
            _db = db;
        }

        public async Task<int> CountUsersAsync(CancellationToken cancellationToken = default)
        {
            return await _db.Users.CountAsync(cancellationToken);
        }

        public async Task<int> CountBusinessesAsync(CancellationToken cancellationToken = default)
        {
            return await _db.Businesses.CountAsync(cancellationToken);
        }

        public async Task<int> CountProductsAsync(CancellationToken cancellationToken = default)
        {
            return await _db.Products.CountAsync(cancellationToken);
        }

        public async Task<int> CountProductDraftsAsync(CancellationToken cancellationToken = default)
        {
            return await _db.ProductDrafts.CountAsync(cancellationToken);
        }

        public async Task<int> CountPendingInvitationsAsync(CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;

            return await _db.Invitations.CountAsync(
                i => i.AcceptedAt == null && i.RevokedAt == null && i.ExpiresAt > now,
                cancellationToken);
        }

        public async Task<List<RoleCountResponse>> GetUserCountsBySystemRoleAsync(CancellationToken cancellationToken = default)
        {
            var grouped = await (
                from u in _db.Users
                join r in _db.SystemRoles on u.SystemRoleId equals r.Id
                group u by r.Role into g
                select new { Role = g.Key, Count = g.Count() }
            ).ToListAsync(cancellationToken);

            return grouped
                .Select(x => new RoleCountResponse { Role = x.Role.ToString(), Count = x.Count })
                .ToList();
        }

        public async Task<List<RoleCountResponse>> GetBusinessUserCountsByRoleAsync(CancellationToken cancellationToken = default)
        {
            var grouped = await (
                from bu in _db.BusinessUsers
                join r in _db.BusinessUserRoles on bu.RoleId equals r.Id
                group bu by r.Role into g
                select new { Role = g.Key, Count = g.Count() }
            ).ToListAsync(cancellationToken);

            return grouped
                .Select(x => new RoleCountResponse { Role = x.Role.ToString(), Count = x.Count })
                .ToList();
        }

        public async Task<List<DateTime>> GetBusinessCreationDatesSinceAsync(DateTime since, CancellationToken cancellationToken = default)
        {
            return await _db.Businesses
                .Where(b => b.CreatedAt >= since)
                .Select(b => b.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<DateTime>> GetProductCreationDatesSinceAsync(DateTime since, CancellationToken cancellationToken = default)
        {
            return await _db.Products
                .Where(p => p.CreatedAt >= since)
                .Select(p => p.CreatedAt)
                .ToListAsync(cancellationToken);
        }
    }
}
