using MerchForge.api.Data;
using MerchForge.api.Models;
using MerchForge.api.Repositories.Interfaces;

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

        public async Task<BusinessUserResponse> GetUserBusinessAsync(Guid userId, CancellationToken cancellationToken)
        {
            var businessUser = await _db.BusinessUsers.FindAsync(userId, cancellationToken);

            if (businessUser == null) throw new Exception("Business user not found");

            var businessRole = await _userRepository.GetBusinessRoleById(businessUser.RoleId, cancellationToken);

            var business = await _db.Businesses.FindAsync(businessUser.BusinessId, cancellationToken);

            return new BusinessUserResponse
            {
                Business = business!,
                BusinessRole = businessRole!
            };
        }
    }
}
