using System.Text.Json;
using MerchForge.api.DTOs.Common;

namespace MerchForge.api.DTOs.BusinessDashboard;

/// <summary>
/// A full replacement of the draft — matches the "always a complete snapshot, never
/// a partial diff" contract on BusinessWebsiteDraft. LogoUrl/FaviconUrl and any
/// Image-typed template field are plain URL strings here, already uploaded via the
/// separate image-upload endpoint before this is submitted (same two-step pattern
/// as the existing product image flow).
/// </summary>
public class SaveWebsiteCustomizationDraftRequest
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

    public SocialLinksDto? SocialLinks { get; set; }

    public BusinessHoursDto? BusinessHours { get; set; }

    public string? PrimaryColor { get; set; }

    /// <summary>Keyed by the current template's own customizable-component keys — validated against that template's catalogue by WebsiteCustomizationValuesBuilder, not by FluentValidation, same as Product.Metadata's validation living in ProductMetadataBuilder rather than a validator.</summary>
    public Dictionary<string, JsonElement>? TemplateFields { get; set; }
}
