using System.Text.Json;
using MerchForge.api.Data;
using MerchForge.api.Exceptions.BusinessDashboard;
using MerchForge.api.Models;
using MerchForge.api.Repositories.Interfaces;
using MerchForge.api.Services.BusinessDashboard;
using MerchForge.api.Services.Common;
using Microsoft.EntityFrameworkCore;

namespace MerchForge.api.Repositories.Implementations
{
    public class WebsiteCustomizationRepository : IWebsiteCustomizationRepository
    {
        private readonly MerchForgeDbContext _db;

        public WebsiteCustomizationRepository(MerchForgeDbContext db)
        {
            _db = db;
        }

        public async Task<Business?> GetTrackedBusinessAsync(
            Guid businessId,
            CancellationToken cancellationToken = default)
        {
            return await _db.Businesses
                .FirstOrDefaultAsync(b => b.Id == businessId, cancellationToken);
        }

        public async Task<BusinessWebsiteDraft?> GetTrackedDraftAsync(
            Guid businessId,
            CancellationToken cancellationToken = default)
        {
            return await _db.BusinessWebsiteDrafts
                .FirstOrDefaultAsync(d => d.BusinessId == businessId, cancellationToken);
        }

        public async Task CreateDraftAsync(
            BusinessWebsiteDraft draft,
            CancellationToken cancellationToken = default)
        {
            _db.BusinessWebsiteDrafts.Add(draft);
            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task<(List<string> DroppedTemplateFieldKeys, DateTime PublishedAt)> PublishAsync(
            Guid businessId,
            CancellationToken cancellationToken = default)
        {
            await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var business = await _db.Businesses
                    .FirstOrDefaultAsync(b => b.Id == businessId, cancellationToken)
                    ?? throw new BusinessNotFoundException();

                var draft = await _db.BusinessWebsiteDrafts
                    .FirstOrDefaultAsync(d => d.BusinessId == businessId, cancellationToken)
                    ?? throw new WebsiteCustomizationDraftNotFoundException();

                var droppedKeys = new List<string>();
                JsonDocument? validatedTemplateFields = null;

                if (business.WebsiteTemplateId is Guid templateId)
                {
                    var components = await _db.WebsiteTemplateCustomizableComponents
                        .Where(c => c.WebsiteTemplateId == templateId && c.IsActive)
                        .ToListAsync(cancellationToken);

                    var rules = WebsiteCustomizationValuesBuilder.BuildRules(components);

                    (validatedTemplateFields, droppedKeys) =
                        WebsiteCustomizationValuesBuilder.DropUnknownKeys(draft.TemplateFieldsDraft, rules);
                }
                else if (draft.TemplateFieldsDraft is not null
                    && draft.TemplateFieldsDraft.RootElement.ValueKind == JsonValueKind.Object)
                {
                    // No template chosen at all -- any saved template-field values are
                    // meaningless, so everything the draft had is dropped.
                    droppedKeys.AddRange(draft.TemplateFieldsDraft.RootElement.EnumerateObject().Select(p => p.Name));
                }

                business.Tagline = draft.Tagline;
                business.Description = draft.Description;
                business.LogoUrl = draft.LogoUrl;
                business.FaviconUrl = draft.FaviconUrl;
                business.ContactEmail = draft.ContactEmail;
                business.ContactPhone = draft.ContactPhone;
                business.WhatsAppNumber = draft.WhatsAppNumber;
                business.AddressLine1 = draft.AddressLine1;
                business.AddressLine2 = draft.AddressLine2;
                business.City = draft.City;
                business.State = draft.State;
                business.PostalCode = draft.PostalCode;
                business.Country = draft.Country;
                business.SocialLinks = draft.SocialLinks;
                business.BusinessHours = draft.BusinessHours;
                business.PrimaryColor = draft.PrimaryColor;

                if (business.WebsiteTemplateId is Guid currentTemplateId)
                {
                    business.WebsiteCustomizationValues = WebsiteCustomizationValuesReader.WriteForTemplate(
                        business.WebsiteCustomizationValues, currentTemplateId, validatedTemplateFields);
                }

                var publishedAt = DateTime.UtcNow;

                business.UpdatedAt = publishedAt;
                draft.LastPublishedAt = publishedAt;
                draft.UpdatedAt = publishedAt;

                await _db.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return (droppedKeys, publishedAt);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }
    }
}
