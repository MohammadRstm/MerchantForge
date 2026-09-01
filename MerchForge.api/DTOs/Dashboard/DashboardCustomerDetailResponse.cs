using MerchForge.api.DTOs.Audit;

namespace MerchForge.api.DTOs.Dashboard;

public class DashboardCustomerDetailResponse
{
    public Guid Id { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string? Phone { get; set; }

    public string? AddressLine1 { get; set; }

    public string? AddressLine2 { get; set; }

    public string? City { get; set; }

    public string? State { get; set; }

    public string? PostalCode { get; set; }

    public string? Country { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public List<CustomerBusinessOrderSummaryResponse> Businesses { get; set; } = [];

    public bool HasActiveSession { get; set; }

    public List<AuditLogResponse> RecentActivity { get; set; } = [];
}
