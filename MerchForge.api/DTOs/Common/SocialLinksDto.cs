namespace MerchForge.api.DTOs.Common;

/// <summary>
/// Fixed key set, shared between the owner-side draft/business responses and the
/// public storefront response — a null/omitted field means "not set", and a
/// storefront should hide that icon rather than link to a placeholder.
/// </summary>
public class SocialLinksDto
{
    public string? Facebook { get; set; }

    public string? Instagram { get; set; }

    public string? Twitter { get; set; }

    public string? TikTok { get; set; }

    public string? YouTube { get; set; }

    public string? LinkedIn { get; set; }
}
