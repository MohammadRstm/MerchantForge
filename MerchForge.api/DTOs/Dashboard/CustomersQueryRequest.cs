using MerchForge.api.DTOs.Common;
using MerchForge.api.Enums;

namespace MerchForge.api.DTOs.Dashboard;

public class CustomersQueryRequest : PagedQuery
{
    public string? Search { get; set; }

    /// <summary>When set, only customers with at least one order for this business.</summary>
    public Guid? BusinessId { get; set; }

    public CustomerSortField SortBy { get; set; } = CustomerSortField.CreatedAt;

    public bool SortDescending { get; set; } = true;
}
