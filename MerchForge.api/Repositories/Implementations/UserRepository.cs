using MerchForge.api.Data;
using MerchForge.api.DTOs.Auth;
using MerchForge.api.Enums;
using MerchForge.api.Models;
using MerchForge.api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

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

        public async Task FinishBusinessOwnerRegistration(User user , Business business, BusinessUser businessUser, Guid invitationId, CancellationToken cancellationToken = default)
        {
            await using var transaction =
               await _db.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                // Atomically claim the invitation: only succeeds if it is still
                // unaccepted, unrevoked and unexpired, closing the race window where
                // two concurrent requests for the same token could both pass
                // validation and create two businesses.
                var claimed = await _db.Invitations
                    .Where(i =>
                        i.Id == invitationId &&
                        i.AcceptedAt == null &&
                        i.RevokedAt == null &&
                        i.ExpiresAt > DateTime.UtcNow)
                    .ExecuteUpdateAsync(
                        setters => setters.SetProperty(i => i.AcceptedAt, DateTime.UtcNow),
                        cancellationToken);

                if (claimed == 0)
                {
                    throw new Exceptions.Invitation.InvitationAlreadyUsedException();
                }

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
        }

        public async Task<bool> SuperAdminExistsAsync(CancellationToken cancellationToken = default)
        {
            var superAdminRoleId = await GetSystemRoleId(SystemRole.SuperAdmin, cancellationToken);
            return await _db.Users.AnyAsync(u => u.SystemRoleId == superAdminRoleId, cancellationToken);
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

        public async Task<Guid> GetBusinessRoleId(BusinessRole role, CancellationToken cancellationToken = default)
        {
            var businessRole = await _db.BusinessUserRoles.FirstOrDefaultAsync(bur => bur.Role == role);
            if (businessRole == null) throw new Exception("Business Role not found");
            return businessRole.Id;
        }

        public async Task<BusinessRole> GetBusinessRoleById(Guid Id, CancellationToken cancellationToken = default)
        {
            var businessRole = await _db.BusinessUserRoles.FindAsync(Id);
            if (businessRole == null) throw new Exception("Invalid Business role id");
            return businessRole.Role;
        }
    }
}
