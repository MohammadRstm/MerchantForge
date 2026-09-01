using MerchForge.api.DTOs.Audit;
using MerchForge.api.Models;

namespace MerchForge.api.Repositories.Interfaces
{
    public interface IAuditLogRepository
    {
        Task AddAsync(AuditLog entry, CancellationToken cancellationToken = default);

        Task<(string FirstName, string LastName)?> GetUserNameAsync(
            Guid userId, CancellationToken cancellationToken = default);

        Task<(List<AuditLogResponse> Items, int TotalCount)> GetLogsAsync(
            AuditLogQueryRequest query, CancellationToken cancellationToken = default);

        /// <summary>Successful/failed login counts and non-Authentication ("admin action") event count, all since the given time; plus a per-day breakdown of successful vs failed logins over the same window. Active-session count is not this repository's concern - callers combine it with IDashboardRepository.CountActiveSessionsAsync.</summary>
        Task<(int SuccessfulLogins, int FailedLogins, int AdminActions, List<AuthActivityPointResponse> ActivityOverTime)> GetSecurityOverviewAsync(
            DateTime since, CancellationToken cancellationToken = default);

        Task<FailedLoginStatsResponse> GetFailedLoginStatsAsync(
            int recentTake, CancellationToken cancellationToken = default);

        /// <summary>Attempted-email/failed-login-count pairs meeting or exceeding the threshold within the given window.</summary>
        Task<List<(string AttemptedEmail, int Count, DateTime LastAttemptAt)>> GetRepeatedFailedLoginsAsync(
            DateTime since, int threshold, CancellationToken cancellationToken = default);

        Task<List<AuditLogResponse>> GetRecentlyDisabledAccountsAsync(
            DateTime since, CancellationToken cancellationToken = default);

        /// <summary>Events where the given user is either the actor or the target ("User" entity) - their own activity feed.</summary>
        Task<List<AuditLogResponse>> GetUserActivityAsync(
            Guid userId, int take, CancellationToken cancellationToken = default);

        /// <summary>Events targeting the given customer ("Customer" entity) - a customer is never ActorUserId (that FK only points at platform Users), so this only ever matches on EntityType/EntityId.</summary>
        Task<List<AuditLogResponse>> GetCustomerActivityAsync(
            Guid customerId, int take, CancellationToken cancellationToken = default);
    }
}
