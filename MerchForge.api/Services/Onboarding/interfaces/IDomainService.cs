using MerchForge.api.DTOs.Onboarding;
using MerchForge.api.Models;

namespace MerchForge.api.Services.Onboarding.interfaces
{
    public interface IDomainService
    {
        Task<List<OnboardingDomainResponse>> GetDomainsAsync(
            CancellationToken cancellationToken = default);

        Task<List<OnboardingCategoryResponse>> GetCategoriesAsync(
            Guid domainId,
            CancellationToken cancellationToken = default);

        Task EnsureDomainExistsAsync(
            Guid domainId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Builds (but does not persist) new custom Category entities for
        /// <paramref name="businessId"/> from free-text names, e.g. from
        /// registration. Trims and de-duplicates the names against each other and
        /// throws DuplicateCategoryNameException for any that already exist as a
        /// platform category in this domain — the caller should pick the existing
        /// one instead of creating a private duplicate of public data.
        /// </summary>
        Task<List<Category>> BuildCustomCategoriesAsync(
            Guid businessId,
            Guid domainId,
            IReadOnlyList<string> categoryNames,
            CancellationToken cancellationToken = default);
    }
}
