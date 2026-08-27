namespace MerchForge.api.DTOs.Dashboard;

/// <summary>
/// Name and BusinessDomainId are deliberately not editable here: Name is the
/// technical identifier a deployment step matches against a physical template
/// project, and BusinessDomainId governs which businesses were already offered this
/// template — changing either after the fact would silently break that link.
/// </summary>
public class UpdateWebsiteTemplateRequest
{
    public string Label { get; set; } = string.Empty;

    public string PreviewImageUrl { get; set; } = string.Empty;

    public string? PreviewWebsiteUrl { get; set; }

    public int DisplayOrder { get; set; }
}
