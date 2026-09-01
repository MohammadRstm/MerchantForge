namespace MerchForge.api.DTOs.Audit;

public class SecurityOverviewResponse
{
    // All counts are windowed to the last 30 days (matches the rest of the
    // Super Admin dashboard's "recently" window) - a running platform total
    // for logins would only ever grow and stop being a useful "activity" signal.
    public int SuccessfulLogins { get; set; }

    public int FailedLogins { get; set; }

    public int ActiveSessions { get; set; }

    public int AdminActions { get; set; }

    public List<AuthActivityPointResponse> ActivityOverTime { get; set; } = new();
}

public class AuthActivityPointResponse
{
    public DateTime Date { get; set; }

    public int SuccessfulLogins { get; set; }

    public int FailedLogins { get; set; }
}
