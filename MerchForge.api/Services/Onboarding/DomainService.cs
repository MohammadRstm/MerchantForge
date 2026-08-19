using MerchForge.api.DTOs.Onboarding;
using MerchForge.api.Exceptions.Onboarding;
using MerchForge.api.Models;
using MerchForge.api.Repositories.Interfaces;
using MerchForge.api.Services.Common;
using MerchForge.api.Services.Onboarding.interfaces;

namespace MerchForge.api.Services.Onboarding
{
    public class DomainService : IDomainService
    {
        private readonly IDomainRepository _domainRepository;

        public DomainService(IDomainRepository domainRepository)
        {
            _domainRepository = domainRepository;
        }

        public async Task<List<OnboardingDomainResponse>> GetDomainsAsync(
            CancellationToken cancellationToken = default)
        {
            var domains = await _domainRepository.GetActiveDomainsAsync(cancellationToken);

            return domains
                .Select(d => new OnboardingDomainResponse { Id = d.Id, Name = d.Name, Slug = d.Slug })
                .ToList();
        }

        public async Task<List<OnboardingCategoryResponse>> GetCategoriesAsync(
            Guid domainId,
            CancellationToken cancellationToken = default)
        {
            await EnsureDomainExistsAsync(domainId, cancellationToken);

            var categories = await _domainRepository.GetGlobalCategoriesAsync(domainId, cancellationToken);

            return categories
                .Select(c => new OnboardingCategoryResponse { Id = c.Id, Name = c.Name, Slug = c.Slug })
                .ToList();
        }

        public async Task EnsureDomainExistsAsync(
            Guid domainId,
            CancellationToken cancellationToken = default)
        {
            var exists = await _domainRepository.DomainExistsAndIsActiveAsync(domainId, cancellationToken);

            if (!exists)
            {
                throw new BusinessDomainNotFoundException();
            }
        }

        public async Task<List<Category>> BuildCustomCategoriesAsync(
            Guid businessId,
            Guid domainId,
            IReadOnlyList<string> categoryNames,
            CancellationToken cancellationToken = default)
        {
            if (categoryNames.Count == 0)
            {
                return [];
            }

            // De-duplicate the submitted names against each other by slug — "Vintage"
            // and "vintage" would otherwise create two rows that render identically.
            var distinctNames = categoryNames
                .Select(n => n.Trim())
                .Where(n => n.Length > 0)
                .GroupBy(n => Slug.From(n), StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .ToList();

            var existingGlobalSlugs = await _domainRepository.GetGlobalCategorySlugsAsync(domainId, cancellationToken);

            var duplicates = distinctNames
                .Where(n => existingGlobalSlugs.Contains(Slug.From(n)))
                .ToList();

            if (duplicates.Count > 0)
            {
                throw new DuplicateCategoryNameException(duplicates);
            }

            var now = DateTime.UtcNow;

            return distinctNames
                .Select((name, index) => new Category
                {
                    Id = Guid.NewGuid(),
                    BusinessDomainId = domainId,
                    BusinessId = businessId,
                    Name = name,
                    Slug = Slug.From(name),
                    // Sorts after the platform categories, which start at 1.
                    DisplayOrder = 100 + index,
                    IsActive = true,
                    CreatedAt = now,
                    UpdatedAt = now,
                })
                .ToList();
        }
    }
}
