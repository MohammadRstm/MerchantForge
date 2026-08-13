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
    }
}
