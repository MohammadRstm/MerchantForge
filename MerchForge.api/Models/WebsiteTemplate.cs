namespace MerchForge.api.Models;

/// <summary>
/// One storefront template a business can choose for its public website, scoped to a
/// single business vertical (a Fashion business is only ever offered Fashion
/// templates). SuperAdmin-managed: a template exists here once its project has been
/// built, independently of whether its preview image is ready yet.
///
/// <see cref="Name"/> is the identifier that actually matters -- it is expected to
/// match the deployed template project's own name (e.g. "fashion-template-01"), which
/// is how a later deployment step knows which storefront to stand up for a business
/// that has chosen it.
/// </summary>
public class WebsiteTemplate
{
    public Guid Id { get; set; }

    public Guid BusinessDomainId { get; set; }

    /// <summary>
    /// Globally unique technical identifier, e.g. "fashion-template-01". Distinct
    /// from <see cref="Label"/>: this is what ties a choice back to a physical
    /// template project, so it must never collide across domains either.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Human-facing display name shown to a business owner choosing a template.</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Preview screenshot shown on the template's card. Required on every template so
    /// the choose-a-template screen is never missing a preview outright; a SuperAdmin
    /// who doesn't have the real screenshot yet points this at a placeholder until
    /// they do.
    /// </summary>
    public string PreviewImageUrl { get; set; } = string.Empty;

    /// <summary>
    /// Link to a live demo of the template, opened in a new tab from the "Preview"
    /// button on the choose-a-template screen. Nullable for the same reason
    /// <see cref="PreviewImageUrl"/> tolerates a placeholder: a template can exist in
    /// the catalogue before a demo deployment exists for it.
    /// </summary>
    public string? PreviewWebsiteUrl { get; set; }

    /// <summary>Lets SuperAdmins retire a template without deleting it out from under a business that already chose it.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Display ordering within a domain's template list.</summary>
    public int DisplayOrder { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties

    public BusinessDomain BusinessDomain { get; set; } = null!;

    public ICollection<Business> Businesses { get; set; }
        = new List<Business>();

    public ICollection<WebsiteTemplateCustomizableComponent> CustomizableComponents { get; set; }
        = new List<WebsiteTemplateCustomizableComponent>();
}
