using System.Text.Json;
using System.Text.Json.Nodes;
using MerchForge.api.DTOs.BusinessDashboard;
using MerchForge.api.DTOs.Common;
using MerchForge.api.DTOs.Dashboard;
using MerchForge.api.Enums;
using MerchForge.api.Exceptions.BusinessDashboard;
using MerchForge.api.Models;
using MerchForge.api.Repositories.Interfaces;
using MerchForge.api.Services.BusinessDashboard.interfaces;
using MerchForge.api.Services.Common;
using MerchForge.api.Services.Subscription.interfaces;

namespace MerchForge.api.Services.BusinessDashboard;

public class WebsiteCustomizationService : IWebsiteCustomizationService
{
    private readonly IWebsiteCustomizationRepository _websiteCustomizationRepository;
    private readonly IDashboardRepository _dashboardRepository;
    private readonly ISubscriptionService _subscriptionService;

    public WebsiteCustomizationService(
        IWebsiteCustomizationRepository websiteCustomizationRepository,
        IDashboardRepository dashboardRepository,
        ISubscriptionService subscriptionService)
    {
        _websiteCustomizationRepository = websiteCustomizationRepository;
        _dashboardRepository = dashboardRepository;
        _subscriptionService = subscriptionService;
    }

    public async Task<List<WebsiteTemplateCustomizableComponentResponse>> GetCatalogueAsync(
        Guid businessId,
        CancellationToken cancellationToken = default)
    {
        var business = await _websiteCustomizationRepository.GetTrackedBusinessAsync(businessId, cancellationToken)
            ?? throw new BusinessNotFoundException();

        if (business.WebsiteTemplateId is not Guid templateId)
        {
            return [];
        }

        var components = await _dashboardRepository.GetActiveCustomizableComponentsForTemplateAsync(templateId, cancellationToken);

        return components.Select(MapComponent).ToList();
    }

    public async Task<WebsiteCustomizationDraftResponse> GetOrCreateDraftAsync(
        Guid businessId,
        CancellationToken cancellationToken = default)
    {
        var business = await _websiteCustomizationRepository.GetTrackedBusinessAsync(businessId, cancellationToken)
            ?? throw new BusinessNotFoundException();

        var draft = await EnsureDraftExistsAsync(business, cancellationToken);

        return MapDraft(draft);
    }

    public async Task<WebsiteCustomizationDraftResponse> SaveDraftAsync(
        Guid businessId,
        SaveWebsiteCustomizationDraftRequest request,
        CancellationToken cancellationToken = default)
    {
        var business = await _websiteCustomizationRepository.GetTrackedBusinessAsync(businessId, cancellationToken)
            ?? throw new BusinessNotFoundException();

        var draft = await EnsureDraftExistsAsync(business, cancellationToken);

        var rules = business.WebsiteTemplateId is Guid templateId
            ? WebsiteCustomizationValuesBuilder.BuildRules(
                await _dashboardRepository.GetActiveCustomizableComponentsForTemplateAsync(templateId, cancellationToken))
            : new Dictionary<string, WebsiteCustomizationValuesBuilder.FieldRule>();

        var newTemplateFields = WebsiteCustomizationValuesBuilder.Build(rules, request.TemplateFields);
        var newSocialLinks = WriteSocialLinks(request.SocialLinks);
        var newBusinessHours = WriteBusinessHours(request.BusinessHours);

        var hasAdvancedAccess = await _subscriptionService.HasFeatureAsync(
            businessId, FeatureKeys.WebsiteCustomizationAdvanced);

        if (hasAdvancedAccess)
        {
            draft.SocialLinks = newSocialLinks;
            draft.BusinessHours = newBusinessHours;
            draft.TemplateFieldsDraft = newTemplateFields;
            draft.TemplateFieldsWebsiteTemplateId = business.WebsiteTemplateId;
        }
        else if (!JsonDocumentsEqual(newSocialLinks, draft.SocialLinks)
            || !JsonDocumentsEqual(newBusinessHours, draft.BusinessHours)
            || !JsonDocumentsEqual(newTemplateFields, draft.TemplateFieldsDraft))
        {
            // Not entitled to website_customization.advanced. A downgraded business
            // must still be able to keep re-saving the rest of its draft (basic
            // fields) even though the "complete snapshot" payload still carries its
            // old advanced-field values unchanged -- only reject when the incoming
            // advanced values actually differ from what's already stored.
            throw new WebsiteCustomizationAdvancedFeatureRequiredException();
        }

        draft.Tagline = Clean(request.Tagline);
        draft.Description = Clean(request.Description);
        draft.LogoUrl = Clean(request.LogoUrl);
        draft.FaviconUrl = Clean(request.FaviconUrl);
        draft.ContactEmail = Clean(request.ContactEmail);
        draft.ContactPhone = Clean(request.ContactPhone);
        draft.WhatsAppNumber = Clean(request.WhatsAppNumber);
        draft.AddressLine1 = Clean(request.AddressLine1);
        draft.AddressLine2 = Clean(request.AddressLine2);
        draft.City = Clean(request.City);
        draft.State = Clean(request.State);
        draft.PostalCode = Clean(request.PostalCode);
        draft.Country = Clean(request.Country);
        draft.PrimaryColor = request.PrimaryColor?.Trim().ToUpperInvariant() is { Length: > 0 } color ? color : null;
        draft.UpdatedAt = DateTime.UtcNow;

        await _websiteCustomizationRepository.SaveChangesAsync(cancellationToken);

        return MapDraft(draft);
    }

    public async Task<PublishWebsiteCustomizationResponse> PublishAsync(
        Guid businessId,
        CancellationToken cancellationToken = default)
    {
        var (droppedKeys, publishedAt) = await _websiteCustomizationRepository.PublishAsync(businessId, cancellationToken);

        return new PublishWebsiteCustomizationResponse
        {
            DroppedTemplateFieldKeys = droppedKeys,
            PublishedAt = publishedAt,
        };
    }

    public async Task<RegeneratePreviewTokenResponse> RegeneratePreviewTokenAsync(
        Guid businessId,
        CancellationToken cancellationToken = default)
    {
        var business = await _websiteCustomizationRepository.GetTrackedBusinessAsync(businessId, cancellationToken)
            ?? throw new BusinessNotFoundException();

        var draft = await EnsureDraftExistsAsync(business, cancellationToken);

        draft.PreviewToken = GeneratePreviewToken();
        draft.UpdatedAt = DateTime.UtcNow;

        await _websiteCustomizationRepository.SaveChangesAsync(cancellationToken);

        return new RegeneratePreviewTokenResponse { PreviewToken = draft.PreviewToken };
    }

    private async Task<BusinessWebsiteDraft> EnsureDraftExistsAsync(Business business, CancellationToken cancellationToken)
    {
        var draft = await _websiteCustomizationRepository.GetTrackedDraftAsync(business.Id, cancellationToken);

        if (draft is not null)
        {
            // A draft is only ever created once (below), but the business can switch
            // templates afterward via the website-template-request workflow. Without
            // this check, a stale TemplateFieldsDraft captured under the old template
            // would sit unnoticed and, if the two templates happen to share a key name,
            // get saved as if it were the new template's own value on the next save —
            // exactly the cross-template leak the namespaced storage exists to prevent.
            // Re-sync it here, the same way a fresh draft is seeded below, whenever it's
            // out of date — everything else on the draft (global fields) stays as-is,
            // since those aren't template-specific.
            if (draft.TemplateFieldsWebsiteTemplateId != business.WebsiteTemplateId)
            {
                draft.TemplateFieldsDraft = WebsiteCustomizationValuesReader.ReadForTemplate(
                    business.WebsiteCustomizationValues, business.WebsiteTemplateId);
                draft.TemplateFieldsWebsiteTemplateId = business.WebsiteTemplateId;
                draft.UpdatedAt = DateTime.UtcNow;

                await _websiteCustomizationRepository.SaveChangesAsync(cancellationToken);
            }

            return draft;
        }

        draft = new BusinessWebsiteDraft
        {
            BusinessId = business.Id,
            Tagline = business.Tagline,
            Description = business.Description,
            LogoUrl = business.LogoUrl,
            FaviconUrl = business.FaviconUrl,
            ContactEmail = business.ContactEmail,
            ContactPhone = business.ContactPhone,
            WhatsAppNumber = business.WhatsAppNumber,
            AddressLine1 = business.AddressLine1,
            AddressLine2 = business.AddressLine2,
            City = business.City,
            State = business.State,
            PostalCode = business.PostalCode,
            Country = business.Country,
            SocialLinks = business.SocialLinks,
            BusinessHours = business.BusinessHours,
            PrimaryColor = business.PrimaryColor,
            TemplateFieldsDraft = WebsiteCustomizationValuesReader.ReadForTemplate(
                business.WebsiteCustomizationValues, business.WebsiteTemplateId),
            TemplateFieldsWebsiteTemplateId = business.WebsiteTemplateId,
            PreviewToken = GeneratePreviewToken(),
            UpdatedAt = DateTime.UtcNow,
        };

        await _websiteCustomizationRepository.CreateDraftAsync(draft, cancellationToken);

        return draft;
    }

    private static WebsiteTemplateCustomizableComponentResponse MapComponent(WebsiteTemplateCustomizableComponent component)
    {
        return new WebsiteTemplateCustomizableComponentResponse
        {
            Id = component.Id,
            WebsiteTemplateId = component.WebsiteTemplateId,
            // Not needed by the owner-facing catalogue view -- the owner already knows which template they're on.
            TemplateName = string.Empty,
            Key = component.Key,
            Label = component.Label,
            ValueType = component.ValueType.ToString(),
            IsRequired = component.IsRequired,
            AllowedValues = ReadAllowedValuesList(component.AllowedValues),
            HelpText = component.HelpText,
            DisplayOrder = component.DisplayOrder,
            IsActive = component.IsActive,
            CreatedAt = component.CreatedAt,
        };
    }

    private static WebsiteCustomizationDraftResponse MapDraft(BusinessWebsiteDraft draft)
    {
        return new WebsiteCustomizationDraftResponse
        {
            Tagline = draft.Tagline,
            Description = draft.Description,
            LogoUrl = draft.LogoUrl,
            FaviconUrl = draft.FaviconUrl,
            ContactEmail = draft.ContactEmail,
            ContactPhone = draft.ContactPhone,
            WhatsAppNumber = draft.WhatsAppNumber,
            AddressLine1 = draft.AddressLine1,
            AddressLine2 = draft.AddressLine2,
            City = draft.City,
            State = draft.State,
            PostalCode = draft.PostalCode,
            Country = draft.Country,
            SocialLinks = ReadSocialLinks(draft.SocialLinks),
            BusinessHours = ReadBusinessHours(draft.BusinessHours),
            PrimaryColor = draft.PrimaryColor,
            TemplateFields = ReadTemplateFields(draft.TemplateFieldsDraft),
            UpdatedAt = draft.UpdatedAt,
            LastPublishedAt = draft.LastPublishedAt,
            PreviewToken = draft.PreviewToken,
        };
    }

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static bool JsonDocumentsEqual(JsonDocument? a, JsonDocument? b)
    {
        if (a is null && b is null)
        {
            return true;
        }

        if (a is null || b is null)
        {
            return false;
        }

        return JsonNode.DeepEquals(
            JsonNode.Parse(a.RootElement.GetRawText()),
            JsonNode.Parse(b.RootElement.GetRawText()));
    }

    private static JsonDocument? WriteSocialLinks(SocialLinksDto? dto)
    {
        if (dto is null || (dto.Facebook is null && dto.Instagram is null && dto.Twitter is null
            && dto.TikTok is null && dto.YouTube is null && dto.LinkedIn is null))
        {
            return null;
        }

        return JsonSerializer.SerializeToDocument(dto);
    }

    private static JsonDocument? WriteBusinessHours(BusinessHoursDto? dto)
    {
        if (dto is null || (dto.Monday is null && dto.Tuesday is null && dto.Wednesday is null
            && dto.Thursday is null && dto.Friday is null && dto.Saturday is null && dto.Sunday is null))
        {
            return null;
        }

        return JsonSerializer.SerializeToDocument(dto);
    }

    private static SocialLinksDto ReadSocialLinks(JsonDocument? document) =>
        document is null
            ? new SocialLinksDto()
            : JsonSerializer.Deserialize<SocialLinksDto>(document.RootElement.GetRawText()) ?? new SocialLinksDto();

    private static BusinessHoursDto ReadBusinessHours(JsonDocument? document) =>
        document is null
            ? new BusinessHoursDto()
            : JsonSerializer.Deserialize<BusinessHoursDto>(document.RootElement.GetRawText()) ?? new BusinessHoursDto();

    private static Dictionary<string, JsonElement> ReadTemplateFields(JsonDocument? document)
    {
        var result = new Dictionary<string, JsonElement>();

        if (document is null || document.RootElement.ValueKind != JsonValueKind.Object)
        {
            return result;
        }

        foreach (var property in document.RootElement.EnumerateObject())
        {
            result[property.Name] = property.Value.Clone();
        }

        return result;
    }

    private static List<string> ReadAllowedValuesList(JsonDocument? allowedValues)
    {
        if (allowedValues is null || allowedValues.RootElement.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return allowedValues.RootElement
            .EnumerateArray()
            .Where(v => v.ValueKind == JsonValueKind.String)
            .Select(v => v.GetString()!)
            .ToList();
    }

    private static string GeneratePreviewToken() =>
        Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
}
