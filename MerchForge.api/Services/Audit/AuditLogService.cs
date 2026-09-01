using MerchForge.api.DTOs.Audit;
using MerchForge.api.DTOs.Common;
using MerchForge.api.Enums;
using MerchForge.api.Models;
using MerchForge.api.Repositories.Interfaces;
using MerchForge.api.Services.Audit.interfaces;

namespace MerchForge.api.Services.Audit
{
    public class AuditLogService : IAuditLogService
    {
        private const int RecentFailedLoginTake = 20;
        private const int RepeatedFailedLoginThreshold = 5;
        private const int OverviewWindowDays = 30;
        private static readonly TimeSpan AlertWindow = TimeSpan.FromHours(24);

        private readonly IAuditLogRepository _repository;
        private readonly ILogger<AuditLogService> _logger;

        public AuditLogService(IAuditLogRepository repository, ILogger<AuditLogService> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task LogAsync(
            AuditEventType eventType,
            string action,
            string description,
            bool success,
            Guid? actorUserId,
            string? actorDisplayNameOverride = null,
            string? entityType = null,
            Guid? entityId = null,
            Guid? businessId = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var displayName = actorDisplayNameOverride;

                if (displayName is null && actorUserId.HasValue)
                {
                    var name = await _repository.GetUserNameAsync(actorUserId.Value, cancellationToken);
                    displayName = name.HasValue ? $"{name.Value.FirstName} {name.Value.LastName}" : "Unknown";
                }

                displayName ??= "System";

                var entry = new AuditLog
                {
                    Id = Guid.NewGuid(),
                    ActorUserId = actorUserId,
                    ActorDisplayName = displayName,
                    EventType = eventType,
                    Action = action,
                    EntityType = entityType,
                    EntityId = entityId,
                    BusinessId = businessId,
                    Description = description,
                    Success = success,
                    CreatedAt = DateTime.UtcNow,
                };

                await _repository.AddAsync(entry, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write audit log entry for action {Action}.", action);
            }
        }

        public async Task<PagedResult<AuditLogResponse>> GetLogsAsync(
            AuditLogQueryRequest query, CancellationToken cancellationToken = default)
        {
            var (items, totalCount) = await _repository.GetLogsAsync(query, cancellationToken);

            return new PagedResult<AuditLogResponse>
            {
                Items = items,
                Page = query.Page,
                PageSize = query.PageSize,
                TotalCount = totalCount,
            };
        }

        public async Task<SecurityOverviewResponse> GetSecurityOverviewAsync(CancellationToken cancellationToken = default)
        {
            var since = DateTime.UtcNow.AddDays(-OverviewWindowDays);

            var (successfulLogins, failedLogins, adminActions, activityOverTime) =
                await _repository.GetSecurityOverviewAsync(since, cancellationToken);

            return new SecurityOverviewResponse
            {
                SuccessfulLogins = successfulLogins,
                FailedLogins = failedLogins,
                AdminActions = adminActions,
                ActivityOverTime = activityOverTime,
                ActiveSessions = 0,
            };
        }

        public Task<FailedLoginStatsResponse> GetFailedLoginStatsAsync(CancellationToken cancellationToken = default)
        {
            return _repository.GetFailedLoginStatsAsync(RecentFailedLoginTake, cancellationToken);
        }

        public async Task<List<SecurityAlertResponse>> GetSecurityAlertsAsync(CancellationToken cancellationToken = default)
        {
            var since = DateTime.UtcNow.Subtract(AlertWindow);
            var alerts = new List<SecurityAlertResponse>();

            var repeatedFailedLogins = await _repository.GetRepeatedFailedLoginsAsync(
                since, RepeatedFailedLoginThreshold, cancellationToken);

            alerts.AddRange(repeatedFailedLogins.Select(r => new SecurityAlertResponse
            {
                Severity = "Warning",
                Title = "Repeated failed logins",
                Description = $"{r.Count} failed login attempts for {r.AttemptedEmail} in the last 24 hours.",
                CreatedAt = r.LastAttemptAt,
            }));

            var recentlyDisabled = await _repository.GetRecentlyDisabledAccountsAsync(since, cancellationToken);

            alerts.AddRange(recentlyDisabled.Select(d => new SecurityAlertResponse
            {
                Severity = "Critical",
                Title = "Account disabled",
                Description = d.Description,
                CreatedAt = d.CreatedAt,
            }));

            return alerts.OrderByDescending(a => a.CreatedAt).ToList();
        }

        public Task<List<AuditLogResponse>> GetUserActivityAsync(
            Guid userId, int take, CancellationToken cancellationToken = default)
        {
            return _repository.GetUserActivityAsync(userId, take, cancellationToken);
        }

        public Task<List<AuditLogResponse>> GetCustomerActivityAsync(
            Guid customerId, int take, CancellationToken cancellationToken = default)
        {
            return _repository.GetCustomerActivityAsync(customerId, take, cancellationToken);
        }
    }
}
