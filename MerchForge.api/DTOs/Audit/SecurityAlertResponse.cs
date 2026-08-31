namespace MerchForge.api.DTOs.Audit;

// Deliberately simple - a threshold count and a recent-disable feed, not
// detection infrastructure. See SecurityAlertSeverity for the two levels used.
public class SecurityAlertResponse
{
    public string Severity { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}
