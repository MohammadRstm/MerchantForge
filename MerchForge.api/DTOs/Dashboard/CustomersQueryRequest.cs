using MerchForge.api.DTOs.Common;
using MerchForge.api.Enums;

namespace MerchForge.api.DTOs.Dashboard;

public class CustomersQueryRequest : PagedQuery
{
    public string? Search { get; set; }

    /// <summary>When set, only customers with at least one order for this business.</summary>
    public Guid? BusinessId { get; set; }

    /// <summary>null = all, true = has at least one non-cancelled order, false = none.</summary>
    public bool? HasOrders { get; set; }

    public DateTime? RegisteredFrom { get; set; }

    public DateTime? RegisteredTo { get; set; }

    public CustomerSortField SortBy { get; set; } = CustomerSortField.CreatedAt;

    public bool SortDescending { get; set; } = true;
}
