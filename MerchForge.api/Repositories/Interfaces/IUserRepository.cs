using MerchForge.api.Enums;
using MerchForge.api.Models;

namespace MerchForge.api.Repositories.Interfaces
{
    public interface IUserRepository
    {
        Task<User?> GetByEmailAsync(
          string email,
          CancellationToken cancellationToken = default);

        Task<User> RegisterUser(
            User user,
            Business business,
            BusinessUser businessUser,
            CancellationToken token = default);

        Task<User> CreateSuperAdmin(User superAdmin, CancellationToken cancellationToken);

        Task<bool> SuperAdminExistsAsync(CancellationToken cancellationToken = default);

        Task FinishBusinessOwnerRegistration(
            User user,
            Business business,
            BusinessUser businessUser,
            IReadOnlyList<Category> customCategories,
            Guid invitationId,
            CancellationToken cancellationToken = default);

        Task<Guid> GetSystemRoleId(SystemRole role, CancellationToken cancellationToken);
        Task<SystemRole> GetSystemRoleById(Guid Id, CancellationToken cancellationToken = default);
        Task<Guid> GetBusinessRoleId(BusinessRole role, CancellationToken cancellationToken = default);
        Task<BusinessRole> GetBusinessRoleById(Guid Id, CancellationToken cancellationToken = default);

    }
}
