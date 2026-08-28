using MerchForge.api.Models;

namespace MerchForge.api.Repositories.Interfaces
{
    public interface ICustomerRepository
    {
        Task<Customer?> GetByEmailAsync(
            string email,
            CancellationToken cancellationToken = default);

        Task<Customer?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default);

        Task AddAsync(
            Customer customer,
            CancellationToken cancellationToken = default);

        Task UpdateAsync(
            Customer customer,
            CancellationToken cancellationToken = default);
    }
}
