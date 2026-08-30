using MerchForge.api.DTOs.Common;
using MerchForge.api.Enums;

namespace MerchForge.api.DTOs.BusinessDashboard;

public class OrdersQueryRequest : PagedQuery
{
    public OrderStatus? Status { get; set; }

    /// <summary>Matches against customer name, email, phone, or an item's product title.</summary>
    public string? Search { get; set; }

    /// <summary>Inclusive lower bound on CreatedAt, in UTC.</summary>
    public DateTime? From { get; set; }

    /// <summary>Inclusive upper bound on CreatedAt, in UTC.</summary>
    public DateTime? To { get; set; }
}
