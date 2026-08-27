using MerchForge.api.DTOs.Common;
using MerchForge.api.Enums;

namespace MerchForge.api.DTOs.BusinessDashboard;

public class OrdersQueryRequest : PagedQuery
{
    public OrderStatus? Status { get; set; }

    /// <summary>Matches against customer name or email.</summary>
    public string? Search { get; set; }
}
