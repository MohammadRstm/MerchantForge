namespace MerchForge.api.DTOs.CustomerAuth;

/// <summary>
/// Email is deliberately not editable here — changing it would need its own
/// re-verification flow, out of scope for v1.
/// </summary>
public class UpdateCustomerProfileRequest
{
    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string? Phone { get; set; }

    public string? AddressLine1 { get; set; }

    public string? AddressLine2 { get; set; }

    public string? City { get; set; }

    public string? State { get; set; }

    public string? PostalCode { get; set; }

    public string? Country { get; set; }
}
