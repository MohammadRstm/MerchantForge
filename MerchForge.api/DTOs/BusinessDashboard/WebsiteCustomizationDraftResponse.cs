using System.Text.Json;
using MerchForge.api.DTOs.Common;

namespace MerchForge.api.DTOs.BusinessDashboard;

public class WebsiteCustomizationDraftResponse
{
    public string? Tagline { get; set; }

    public string? Description { get; set; }

    public string? LogoUrl { get; set; }

    public string? FaviconUrl { get; set; }

    public string? ContactEmail { get; set; }

    public string? ContactPhone { get; set; }

    public string? WhatsAppNumber { get; set; }

    public string? AddressLine1 { get; set; }

    public string? AddressLine2 { get; set; }

    public string? City { get; set; }

    public string? State { get; set; }

    public string? PostalCode { get; set; }

    public string? Country { get; set; }

    public SocialLinksDto SocialLinks { get; set; } = new();

    public BusinessHoursDto BusinessHours { get; set; } = new();

    public string? PrimaryColor { get; set; }

    public Dictionary<string, JsonElement> TemplateFields { get; set; } = new();

    public DateTime UpdatedAt { get; set; }

    public DateTime? LastPublishedAt { get; set; }

    public string PreviewToken { get; set; } = string.Empty;
}
