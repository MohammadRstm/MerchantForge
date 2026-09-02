namespace MerchForge.api.DTOs.Dashboard;

/// <summary>
/// SuperAdmin-supplied inputs for a showcase business. No invitation email is sent —
/// there's no real person to email — so the SuperAdmin sets real login credentials
/// directly, letting them log in later and show the live dashboard to a prospect.
/// </summary>
public class CreateDemoBusinessRequest
{
    /// <summary>
    /// Which template this business showcases -- its domain is derived from the
    /// template's own BusinessDomainId. Keyed on the template, not the domain: a
    /// domain can have more than one active template (e.g. two different fashion
    /// storefronts), each deserving its own showcase business.
    /// </summary>
    public Guid WebsiteTemplateId { get; set; }

    public string BusinessName { get; set; } = string.Empty;

    public string OwnerFirstName { get; set; } = string.Empty;

    public string OwnerLastName { get; set; } = string.Empty;

    public string OwnerEmail { get; set; } = string.Empty;

    public string OwnerPassword { get; set; } = string.Empty;
}
