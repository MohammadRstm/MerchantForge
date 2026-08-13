using MerchForge.api.Data;
using MerchForge.api.Models;
using MerchForge.api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MerchForge.api.Repositories.Implementations
{
    public class UserRepository : IUserRepository
    {
        private readonly MerchForgeDbContext _db;

        public UserRepository(MerchForgeDbContext db)
        {
            _db = db;
        }

        public async Task<User?> GetByEmailAsync(
           string email,
           CancellationToken cancellationToken = default)
        {
            return await _db.Users
                .FirstOrDefaultAsync(
                    u => u.Email == email,
                    cancellationToken);
        }

        public async Task AddAsync(
            User user,
            CancellationToken cancellationToken = default)
        {
            await _db.Users.AddAsync(user, cancellationToken);
        }
    }
}
