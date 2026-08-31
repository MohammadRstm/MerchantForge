using MerchForge.api.DTOs.Audit;
using MerchForge.api.DTOs.Common;
using MerchForge.api.Enums;

namespace MerchForge.api.Services.Audit.interfaces
{
    public interface IAuditLogService
    {
        /// <summary>
        /// Writes one audit entry. Never throws - a failed audit write must not break
        /// the primary action it's recording, so failures are swallowed and logged.
        /// actorUserId null + no override resolves to "System"; actorUserId set with no
        /// override resolves the actor's current name from the Users table.
        /// </summary>
        Task LogAsync(
            AuditEventType eventType,
            string action,
            string description,
            bool success,
            Guid? actorUserId,
            string? actorDisplayNameOverride = null,
            string? entityType = null,
            Guid? entityId = null,
            Guid? businessId = null,
            CancellationToken cancellationToken = default);

        Task<PagedResult<AuditLogResponse>> GetLogsAsync(
            AuditLogQueryRequest query, CancellationToken cancellationToken = default);

        /// <summary>ActiveSessions is left at 0 - callers combining this with session data (DashboardService) fill it in.</summary>
        Task<SecurityOverviewResponse> GetSecurityOverviewAsync(CancellationToken cancellationToken = default);

        Task<FailedLoginStatsResponse> GetFailedLoginStatsAsync(CancellationToken cancellationToken = default);

        Task<List<SecurityAlertResponse>> GetSecurityAlertsAsync(CancellationToken cancellationToken = default);

        Task<List<AuditLogResponse>> GetUserActivityAsync(
            Guid userId, int take, CancellationToken cancellationToken = default);
    }
}
