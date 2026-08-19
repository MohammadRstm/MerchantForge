using System.Text.Json;
using System.Text.Json.Serialization;
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

        public async Task<List<OnboardingProductAttributeResponse>> GetProductAttributesAsync(
            Guid domainId,
            CancellationToken cancellationToken = default)
        {
            await EnsureDomainExistsAsync(domainId, cancellationToken);

            var definitions = await _domainRepository.GetProductAttributeDefinitionsAsync(
                domainId,
                cancellationToken);

            return definitions
                .Select(d => new OnboardingProductAttributeResponse
                {
                    Key = d.Key,
                    Label = d.Label,
                    ValueType = d.ValueType.ToString(),
                    DisplayOrder = d.DisplayOrder,
                })
                .ToList();
        }

        public async Task<JsonDocument?> BuildMetadataShapeAsync(
            Guid domainId,
            IReadOnlyList<string> selectedKeys,
            CancellationToken cancellationToken = default)
        {
            var distinctKeys = selectedKeys
                .Select(k => k.Trim())
                .Where(k => k.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (distinctKeys.Count == 0)
            {
                // Null rather than an empty fields array: both mean "fixed fields
                // only", and one representation is easier to check than two.
                return null;
            }

            var definitions = await _domainRepository.GetProductAttributeDefinitionsAsync(
                domainId,
                cancellationToken);

            var byKey = definitions.ToDictionary(d => d.Key, StringComparer.OrdinalIgnoreCase);

            var unknown = distinctKeys.Where(k => !byKey.ContainsKey(k)).ToList();

            if (unknown.Count > 0)
            {
                throw new UnknownProductAttributeException(unknown);
            }

            // Emitted in the domain's own display order rather than whatever order
            // the client happened to send, so every business's product form is laid
            // out consistently.
            var fields = distinctKeys
                .Select(k => byKey[k])
                .OrderBy(d => d.DisplayOrder)
                .ThenBy(d => d.Label)
                .Select(d => new MetadataShapeField
                {
                    Key = d.Key,
                    Label = d.Label,
                    ValueType = d.ValueType.ToString(),
                })
                .ToList();

            return JsonSerializer.SerializeToDocument(new MetadataShape { Fields = fields });
        }

        /// <summary>
        /// Serialization shape for Business.MetadataShape. An object with a "fields"
        /// array rather than a bare array, so the format can gain siblings later
        /// without breaking readers.
        /// </summary>
        private sealed class MetadataShape
        {
            [JsonPropertyName("fields")]
            public List<MetadataShapeField> Fields { get; set; } = [];
        }

        private sealed class MetadataShapeField
        {
            [JsonPropertyName("key")]
            public string Key { get; set; } = string.Empty;

            [JsonPropertyName("label")]
            public string Label { get; set; } = string.Empty;

            [JsonPropertyName("valueType")]
            public string ValueType { get; set; } = string.Empty;
        }
    }
}
