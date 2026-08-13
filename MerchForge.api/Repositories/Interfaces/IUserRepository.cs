using MerchForge.api.Models;

namespace MerchForge.api.Repositories.Interfaces
{
    public interface IUserRepository
    {
        Task<User?> GetByEmailAsync(
          string email,
          CancellationToken cancellationToken = default);

        Task AddAsync(
          User user,
          CancellationToken cancellationToken = default);
    }
}
