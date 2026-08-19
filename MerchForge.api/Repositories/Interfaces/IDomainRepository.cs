using MerchForge.api.Models;

namespace MerchForge.api.Repositories.Interfaces
{
    public interface IDomainRepository
    {
        Task<List<BusinessDomain>> GetActiveDomainsAsync(
            CancellationToken cancellationToken = default);

        Task<bool> DomainExistsAndIsActiveAsync(
            Guid domainId,
            CancellationToken cancellationToken = default);

        /// <summary>Platform (BusinessId == null) categories only.</summary>
        Task<List<Category>> GetGlobalCategoriesAsync(
            Guid domainId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Case-insensitive slug lookup among a domain's platform categories, used to
        /// stop a new business from creating a custom category that duplicates one
        /// that already exists for everyone.
        /// </summary>
        Task<HashSet<string>> GetGlobalCategorySlugsAsync(
            Guid domainId,
            CancellationToken cancellationToken = default);
    }
}
