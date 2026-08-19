namespace MerchForge.api.DTOs.Storefront;

/// <summary>
/// Everything a storefront needs to boot: identity, presentation, and the
/// formatting configuration prices/dates depend on.
///
/// Deliberately narrower than the Business entity. Owner, members, roles,
/// subscriptions, product drafts, and UpdatedAt are all internal and never appear
/// here.
/// </summary>
public class StorefrontBusinessResponse
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? LogoUrl { get; set; }

    /// <summary>ISO 4217. Required — prices cannot be rendered without it.</summary>
    public string Currency { get; set; } = string.Empty;

    /// <summary>BCP 47 tag, for price/date formatting.</summary>
    public string Locale { get; set; } = string.Empty;

    public string? ContactEmail { get; set; }

    public string? ContactPhone { get; set; }

    /// <summary>
    /// Null when the business has not selected a domain yet. Such a business has no
    /// categories and therefore cannot have products.
    /// </summary>
    public StorefrontDomainResponse? Domain { get; set; }
}
