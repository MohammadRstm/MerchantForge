using MerchForge.api.Data;
using MerchForge.api.Models;
using MerchForge.api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MerchForge.api.Repositories.Implementations
{
    public class DomainRepository : IDomainRepository
    {
        private readonly MerchForgeDbContext _db;

        public DomainRepository(MerchForgeDbContext db)
        {
            _db = db;
        }

        public async Task<List<BusinessDomain>> GetActiveDomainsAsync(
            CancellationToken cancellationToken = default)
        {
            return await _db.BusinessDomains
                .AsNoTracking()
                .Where(d => d.IsActive)
                .OrderBy(d => d.Name)
                .ToListAsync(cancellationToken);
        }

        public async Task<bool> DomainExistsAndIsActiveAsync(
            Guid domainId,
            CancellationToken cancellationToken = default)
        {
            return await _db.BusinessDomains
                .AsNoTracking()
                .AnyAsync(d => d.Id == domainId && d.IsActive, cancellationToken);
        }

        public async Task<List<Category>> GetGlobalCategoriesAsync(
            Guid domainId,
            CancellationToken cancellationToken = default)
        {
            return await _db.Categories
                .AsNoTracking()
                .Where(c => c.BusinessDomainId == domainId && c.BusinessId == null && c.IsActive)
                .OrderBy(c => c.DisplayOrder)
                .ThenBy(c => c.Name)
                .ToListAsync(cancellationToken);
        }

        public async Task<HashSet<string>> GetGlobalCategorySlugsAsync(
            Guid domainId,
            CancellationToken cancellationToken = default)
        {
            var slugs = await _db.Categories
                .AsNoTracking()
                .Where(c => c.BusinessDomainId == domainId && c.BusinessId == null)
                .Select(c => c.Slug)
                .ToListAsync(cancellationToken);

            return new HashSet<string>(slugs, StringComparer.OrdinalIgnoreCase);
        }
    }
}
