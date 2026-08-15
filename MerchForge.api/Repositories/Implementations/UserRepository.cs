using MerchForge.api.Data;
using MerchForge.api.DTOs.Auth;
using MerchForge.api.Enums;
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

        public async Task<User> RegisterUser(User user,Business business ,BusinessUser businessUser , CancellationToken cancellationToken = default)
        {
            await using var transaction =
                await _db.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                await _db.Users.AddAsync(user, cancellationToken);
                await _db.Businesses.AddAsync(business, cancellationToken);
                await _db.BusinessUsers.AddAsync(businessUser, cancellationToken);

                await _db.SaveChangesAsync(cancellationToken);

                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }

            return user;
        }

        public async Task<User> CreateSuperAdmin(User superAdmin , CancellationToken cancellationToken = default)
        {
            await _db.Users.AddAsync(superAdmin , cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
            return superAdmin;
        }

        public async Task<Guid> GetSystemRoleId(SystemRole role, CancellationToken cancellationToken = default) 
        {
            var systemRole = await _db.SystemRoles.FirstAsync(s => s.Role == role);
            return systemRole.Id;
        }

        public async Task<SystemRole> GetSystemRoleById(Guid Id, CancellationToken cancellationToken = default)
        {
            var systemRole = await _db.SystemRoles.FindAsync(Id);
            if (systemRole == null) throw new Exception("System Role not found");

            return systemRole.Role;
        }
    }
}
