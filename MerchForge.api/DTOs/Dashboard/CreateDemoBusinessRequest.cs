namespace MerchForge.api.DTOs.Dashboard;

/// <summary>
/// SuperAdmin-supplied inputs for a showcase business. No invitation email is sent —
/// there's no real person to email — so the SuperAdmin sets real login credentials
/// directly, letting them log in later and show the live dashboard to a prospect.
/// </summary>
public class CreateDemoBusinessRequest
{
    public Guid BusinessDomainId { get; set; }

    public string BusinessName { get; set; } = string.Empty;

    public string OwnerFirstName { get; set; } = string.Empty;

    public string OwnerLastName { get; set; } = string.Empty;

    public string OwnerEmail { get; set; } = string.Empty;

    public string OwnerPassword { get; set; } = string.Empty;
}
