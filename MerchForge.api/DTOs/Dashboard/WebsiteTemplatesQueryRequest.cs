using MerchForge.api.DTOs.Common;
using MerchForge.api.Enums;

namespace MerchForge.api.DTOs.Dashboard;

public class WebsiteTemplatesQueryRequest : PagedQuery
{
    /// <summary>Matches Name, Label, or the template's domain name.</summary>
    public string? Search { get; set; }

    public Guid? BusinessDomainId { get; set; }

    public bool? IsActive { get; set; }

    /// <summary>null = all, true = at least one business currently uses it, false = unused.</summary>
    public bool? HasBusinesses { get; set; }

    /// <summary>null = all, true = has at least one active customizable component, false = none configured.</summary>
    public bool? IsCustomizable { get; set; }

    public WebsiteTemplateSortField SortBy { get; set; } = WebsiteTemplateSortField.DisplayOrder;

    public bool SortDescending { get; set; }
}
