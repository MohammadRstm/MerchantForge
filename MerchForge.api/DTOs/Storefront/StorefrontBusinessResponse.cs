using System.Text.Json;
using MerchForge.api.DTOs.Common;

namespace MerchForge.api.DTOs.Storefront;

/// <summary>
/// Everything a storefront needs to boot: identity, presentation, and the
/// formatting configuration prices/dates depend on.
///
/// Deliberately narrower than the Business entity. Owner, members, roles,
/// subscriptions, product drafts, and UpdatedAt are all internal and never appear
/// here. Unlike Business.MetadataShape, the customization fields below ARE meant to
/// be public — they exist specifically to drive storefront rendering.
/// </summary>
public class StorefrontBusinessResponse
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? Tagline { get; set; }

    public string? LogoUrl { get; set; }

    public string? FaviconUrl { get; set; }

    /// <summary>ISO 4217. Required — prices cannot be rendered without it.</summary>
    public string Currency { get; set; } = string.Empty;

    /// <summary>BCP 47 tag, for price/date formatting.</summary>
    public string Locale { get; set; } = string.Empty;

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

    /// <summary>
    /// This business's saved values for its *current* WebsiteTemplateId's own
    /// customizable-component catalogue — already unwrapped from the namespaced
    /// storage shape, so a template's own code just reads keys directly (e.g.
    /// templateFields.heroImage) without knowing anything about namespacing. Opaque
    /// by design: only the currently-selected template's own code should read this.
    /// Empty when no template is chosen, or none of its fields have a saved value yet.
    /// </summary>
    public Dictionary<string, JsonElement> TemplateFields { get; set; } = new();

    /// <summary>
    /// Null when the business has not selected a domain yet. Such a business has no
    /// categories and therefore cannot have products.
    /// </summary>
    public StorefrontDomainResponse? Domain { get; set; }
}
