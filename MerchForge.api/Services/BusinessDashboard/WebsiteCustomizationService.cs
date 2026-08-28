using System.Text.Json;
using MerchForge.api.DTOs.BusinessDashboard;
using MerchForge.api.DTOs.Common;
using MerchForge.api.DTOs.Dashboard;
using MerchForge.api.Exceptions.BusinessDashboard;
using MerchForge.api.Models;
using MerchForge.api.Repositories.Interfaces;
using MerchForge.api.Services.BusinessDashboard.interfaces;
using MerchForge.api.Services.Common;

namespace MerchForge.api.Services.BusinessDashboard;

public class WebsiteCustomizationService : IWebsiteCustomizationService
{
    private readonly IWebsiteCustomizationRepository _websiteCustomizationRepository;
    private readonly IDashboardRepository _dashboardRepository;

    public WebsiteCustomizationService(
        IWebsiteCustomizationRepository websiteCustomizationRepository,
        IDashboardRepository dashboardRepository)
    {
        _websiteCustomizationRepository = websiteCustomizationRepository;
        _dashboardRepository = dashboardRepository;
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

        var templateFields = WebsiteCustomizationValuesBuilder.Build(rules, request.TemplateFields);

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
        draft.SocialLinks = WriteSocialLinks(request.SocialLinks);
        draft.BusinessHours = WriteBusinessHours(request.BusinessHours);
        draft.PrimaryColor = request.PrimaryColor?.Trim().ToUpperInvariant() is { Length: > 0 } color ? color : null;
        draft.TemplateFieldsDraft = templateFields;
        draft.UpdatedAt = DateTime.UtcNow;

        await _websiteCustomizationRepository.SaveChangesAsync(cancellationToken);

        return MapDraft(draft);
    }

    private async Task<BusinessWebsiteDraft> EnsureDraftExistsAsync(Business business, CancellationToken cancellationToken)
    {
        var draft = await _websiteCustomizationRepository.GetTrackedDraftAsync(business.Id, cancellationToken);

        if (draft is not null)
        {
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
