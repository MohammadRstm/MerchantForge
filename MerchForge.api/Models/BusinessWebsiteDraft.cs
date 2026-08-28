using System.Text.Json;

namespace MerchForge.api.Models;

/// <summary>
/// A business's pending, unpublished storefront customization edits — 1:1 with
/// Business. Always a complete, self-contained snapshot, never a partial diff:
/// created the first time an owner opens the customization page as a full copy of
/// live Business (+ its published WebsiteCustomizationValues for the current
/// template), and every edit from then on overwrites this row wholesale. This
/// deliberately avoids a "does null mean cleared, or never touched?" merge
/// ambiguity — there is no per-field fallback logic anywhere in this feature.
/// Preview returns this row as-is; Publish copies it onto Business wholesale.
///
/// Typed columns mirror Business's own new columns exactly (NOT one opaque JSON
/// blob) for the global half — it's a small, fixed, known field set, so a typo in
/// the publish copy step is a compile error, not a silent bug. Only the genuinely
/// dynamic half (which keys exist depends on which template the business uses)
/// stays JSON.
/// </summary>
public class BusinessWebsiteDraft
{
    public Guid BusinessId { get; set; }

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

    public JsonDocument? SocialLinks { get; set; }

    public JsonDocument? BusinessHours { get; set; }

    public string? PrimaryColor { get; set; }

    /// <summary>Same namespaced-by-WebsiteTemplateId shape as Business.WebsiteCustomizationValues, but only ever holds the current template's own sub-object — a draft is scoped to "editing the current template's values right now", unlike the published column which keeps every previously-used template's leftover values around.</summary>
    public JsonDocument? TemplateFieldsDraft { get; set; }

    /// <summary>
    /// Which template TemplateFieldsDraft was captured for. A business can switch
    /// templates after a draft already exists (the draft is only ever created once,
    /// on first access) — without this, a stale TemplateFieldsDraft from the old
    /// template would sit unnoticed and, if two templates happen to share a key name
    /// (as fashion-template-01 and electronic-template-01's seeded slots do), get
    /// re-saved as if it were the new template's own value on the next save. Compared
    /// against Business.WebsiteTemplateId whenever the draft is loaded; a mismatch
    /// re-syncs TemplateFieldsDraft from the live published values for the new
    /// template, same as if the draft were being created fresh.
    /// </summary>
    public Guid? TemplateFieldsWebsiteTemplateId { get; set; }

    /// <summary>Opaque random, generated once when the row is first created. Regenerable via a dedicated endpoint if a link is shared/leaked.</summary>
    public string PreviewToken { get; set; } = string.Empty;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastPublishedAt { get; set; }

    // Navigation properties

    public Business Business { get; set; } = null!;
}
