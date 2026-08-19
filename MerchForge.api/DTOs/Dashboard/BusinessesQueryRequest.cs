using MerchForge.api.DTOs.Common;
using MerchForge.api.Enums;

namespace MerchForge.api.DTOs.Dashboard;

public class BusinessesQueryRequest : PagedQuery
{
    public string? Search { get; set; }

    public BusinessSortField SortBy { get; set; } = BusinessSortField.CreatedAt;

    public bool SortDescending { get; set; } = true;
}
