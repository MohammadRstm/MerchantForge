using MerchForge.api.DTOs.Common;
using MerchForge.api.Enums;

namespace MerchForge.api.DTOs.WebsiteTemplateRequests;

public class WebsiteTemplateRequestsQueryRequest : PagedQuery
{
    /// <summary>Filters to one status when set; otherwise every request is included.</summary>
    public WebsiteTemplateRequestStatus? Status { get; set; }

    /// <summary>Sorted by CreatedAt — the only column that matters for triage order.</summary>
    public bool SortDescending { get; set; } = true;
}
