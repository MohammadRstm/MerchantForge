namespace MerchForge.api.DTOs.Audit;

public class FailedLoginStatsResponse
{
    public int Today { get; set; }

    public int Last7Days { get; set; }

    public int Last30Days { get; set; }

    public List<RecentFailedLoginResponse> Recent { get; set; } = new();
}

public class RecentFailedLoginResponse
{
    /// <summary>The email that was attempted - ActorDisplayName on a failed-login AuditLog row.</summary>
    public string AttemptedEmail { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}
