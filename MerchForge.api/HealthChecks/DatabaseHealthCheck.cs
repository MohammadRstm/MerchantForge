using MerchForge.api.Data;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace MerchForge.api.HealthChecks;

/// <summary>
/// The one thing worth checking for a first production deploy: can the app
/// actually reach its database. A process that's up but can't reach MySQL is not
/// meaningfully healthy - every real request would fail - so a load
/// balancer/orchestrator needs to see that as unhealthy, not as a 200.
/// </summary>
public class DatabaseHealthCheck : IHealthCheck
{
    private readonly MerchForgeDbContext _db;

    public DatabaseHealthCheck(MerchForgeDbContext db)
    {
        _db = db;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var canConnect = await _db.Database.CanConnectAsync(cancellationToken);

            return canConnect
                ? HealthCheckResult.Healthy("Database reachable.")
                : HealthCheckResult.Unhealthy("Database not reachable.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Database health check threw an exception.", ex);
        }
    }
}
