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

        /// <summary>
        /// Persists the new customer and their LegalAcceptance row together in one
        /// SaveChanges call, so a customer can never exist without a recorded
        /// acceptance.
        /// </summary>
        Task AddAsync(
            Customer customer,
            LegalAcceptance legalAcceptance,
            CancellationToken cancellationToken = default);

        Task UpdateAsync(
            Customer customer,
            CancellationToken cancellationToken = default);
    }
}
