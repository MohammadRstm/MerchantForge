using MerchForge.api.Data;
using MerchForge.api.Models;
using MerchForge.api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MerchForge.api.Repositories.Implementations
{
    public class BusinessRepository : IBusinessRepository
    {
        private readonly MerchForgeDbContext _db;
        private readonly IUserRepository _userRepository;

        public BusinessRepository(MerchForgeDbContext db, IUserRepository userRepository)
        {
            _db = db;
            _userRepository = userRepository;
        }

        public async Task<BusinessContextResponse?> GetUserBusinessAsync(
            Guid userId,
            CancellationToken cancellationToken)
        {
            var businessUser = await _db.BusinessUsers
                .FirstOrDefaultAsync(
                    bu => bu.UserId == userId,
                    cancellationToken);

            if (businessUser == null)
                return null;

            var businessRole = await _userRepository
                .GetBusinessRoleById(
                    businessUser.RoleId,
                    cancellationToken);

            var business = await _db.Businesses
                .FindAsync(
                    [businessUser.BusinessId],
                    cancellationToken);

            if (business == null)
                throw new Exception("Business not found");

            return new BusinessContextResponse
            {
                Id = business.Id,
                Name = business.Name,
                Role = businessRole.ToString()
            };
        }
    }
}
