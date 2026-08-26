using MerchForge.api.DTOs.BusinessDashboard;

namespace MerchForge.api.DTOs.WebsiteTemplateRequests;

/// <summary>What the template-selection page needs: the business's domain, whether it already has an open request, and that domain's templates.</summary>
public class WebsiteTemplateOptionsResponse
{
    public Guid BusinessDomainId { get; set; }

    public string DomainName { get; set; } = string.Empty;

    /// <summary>True while the business has a Pending or InProgress request — the page shows that state instead of the picker.</summary>
    public bool HasOpenRequest { get; set; }

    public List<WebsiteTemplateOptionResponse> Templates { get; set; } = [];
}
