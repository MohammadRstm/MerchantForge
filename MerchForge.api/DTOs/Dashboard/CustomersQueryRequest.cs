using MerchForge.api.DTOs.Common;
using MerchForge.api.Enums;

namespace MerchForge.api.DTOs.Dashboard;

public class CustomersQueryRequest : PagedQuery
{
    public string? Search { get; set; }

    public CustomerSortField SortBy { get; set; } = CustomerSortField.CreatedAt;

    public bool SortDescending { get; set; } = true;
}
