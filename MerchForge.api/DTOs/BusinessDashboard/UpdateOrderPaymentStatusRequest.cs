using MerchForge.api.Enums;

namespace MerchForge.api.DTOs.BusinessDashboard;

/// <summary>
/// Manually flips PaymentStatus — a placeholder for a future payment gateway
/// webhook. See PaymentStatus's own doc comment.
/// </summary>
public class UpdateOrderPaymentStatusRequest
{
    public PaymentStatus PaymentStatus { get; set; }
}
