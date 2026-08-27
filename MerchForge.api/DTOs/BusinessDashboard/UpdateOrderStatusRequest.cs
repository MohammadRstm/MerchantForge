using MerchForge.api.Enums;

namespace MerchForge.api.DTOs.BusinessDashboard;

public class UpdateOrderStatusRequest
{
    public OrderStatus Status { get; set; }
}
