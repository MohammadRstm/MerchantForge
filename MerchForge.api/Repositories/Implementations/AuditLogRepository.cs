using MerchForge.api.Data;
using MerchForge.api.DTOs.Audit;
using MerchForge.api.Enums;
using MerchForge.api.Models;
using MerchForge.api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MerchForge.api.Repositories.Implementations
{
    public class AuditLogRepository : IAuditLogRepository
    {
        private const string LoginSucceededAction = "LoginSucceeded";
        private const string LoginFailedAction = "LoginFailed";
        private const string UserDisabledAction = "UserDisabled";

        private readonly MerchForgeDbContext _db;

        public AuditLogRepository(MerchForgeDbContext db)
        {
            _db = db;
        }

        public async Task AddAsync(AuditLog entry, CancellationToken cancellationToken = default)
        {
            _db.AuditLogs.Add(entry);
            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task<(string FirstName, string LastName)?> GetUserNameAsync(
            Guid userId, CancellationToken cancellationToken = default)
        {
            var user = await _db.Users
                .AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(u => new { u.FirstName, u.LastName })
                .FirstOrDefaultAsync(cancellationToken);

            return user is null ? null : (user.FirstName, user.LastName);
        }

        public async Task<(List<AuditLogResponse> Items, int TotalCount)> GetLogsAsync(
            AuditLogQueryRequest query, CancellationToken cancellationToken = default)
        {
            var baseQuery = _db.AuditLogs.AsQueryable();

            if (query.EventType.HasValue)
            {
                baseQuery = baseQuery.Where(a => a.EventType == query.EventType.Value);
            }

            if (!string.IsNullOrWhiteSpace(query.Actor))
            {
                var pattern = $"%{query.Actor.Trim()}%";
                baseQuery = baseQuery.Where(a => EF.Functions.Like(a.ActorDisplayName, pattern));
            }

            if (query.Success.HasValue)
            {
                baseQuery = baseQuery.Where(a => a.Success == query.Success.Value);
            }

            if (query.From.HasValue)
            {
                baseQuery = baseQuery.Where(a => a.CreatedAt >= query.From.Value);
            }

            if (query.To.HasValue)
            {
                baseQuery = baseQuery.Where(a => a.CreatedAt <= query.To.Value);
            }

            if (query.BusinessId.HasValue)
            {
                baseQuery = baseQuery.Where(a => a.BusinessId == query.BusinessId.Value);
            }

            var totalCount = await baseQuery.CountAsync(cancellationToken);

            var items = await baseQuery
                .OrderByDescending(a => a.CreatedAt)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(a => new AuditLogResponse
                {
                    Id = a.Id,
                    ActorUserId = a.ActorUserId,
                    ActorDisplayName = a.ActorDisplayName,
                    EventType = a.EventType.ToString(),
                    Action = a.Action,
                    EntityType = a.EntityType,
                    EntityId = a.EntityId,
                    BusinessId = a.BusinessId,
                    BusinessName = a.Business != null ? a.Business.Name : null,
                    Description = a.Description,
                    Success = a.Success,
                    CreatedAt = a.CreatedAt,
                })
                .ToListAsync(cancellationToken);

            return (items, totalCount);
        }

        public async Task<(int SuccessfulLogins, int FailedLogins, int AdminActions, List<AuthActivityPointResponse> ActivityOverTime)> GetSecurityOverviewAsync(
            DateTime since, CancellationToken cancellationToken = default)
        {
            var authEvents = await _db.AuditLogs
                .Where(a =>
                    a.EventType == AuditEventType.Authentication &&
                    (a.Action == LoginSucceededAction || a.Action == LoginFailedAction) &&
                    a.CreatedAt >= since)
                .Select(a => new { a.Success, a.CreatedAt })
                .ToListAsync(cancellationToken);

            var adminActions = await _db.AuditLogs
                .CountAsync(a => a.EventType != AuditEventType.Authentication && a.CreatedAt >= since, cancellationToken);

            // Grouped in memory over an already-bounded (30-day) result set, not in SQL -
            // the day bucket is a DateOnly-style truncation that doesn't need to be.
            var activityOverTime = authEvents
                .GroupBy(a => a.CreatedAt.Date)
                .OrderBy(g => g.Key)
                .Select(g => new AuthActivityPointResponse
                {
                    Date = g.Key,
                    SuccessfulLogins = g.Count(a => a.Success),
                    FailedLogins = g.Count(a => !a.Success),
                })
                .ToList();

            return (
                authEvents.Count(a => a.Success),
                authEvents.Count(a => !a.Success),
                adminActions,
                activityOverTime);
        }

        public async Task<FailedLoginStatsResponse> GetFailedLoginStatsAsync(
            int recentTake, CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;
            var todayStart = now.Date;
            var last7 = now.AddDays(-7);
            var last30 = now.AddDays(-30);

            var failedLoginsQuery = _db.AuditLogs
                .Where(a => a.EventType == AuditEventType.Authentication && a.Action == LoginFailedAction);

            var today = await failedLoginsQuery.CountAsync(a => a.CreatedAt >= todayStart, cancellationToken);
            var last7Days = await failedLoginsQuery.CountAsync(a => a.CreatedAt >= last7, cancellationToken);
            var last30Days = await failedLoginsQuery.CountAsync(a => a.CreatedAt >= last30, cancellationToken);

            var recent = await failedLoginsQuery
                .OrderByDescending(a => a.CreatedAt)
                .Take(recentTake)
                .Select(a => new RecentFailedLoginResponse
                {
                    AttemptedEmail = a.ActorDisplayName,
                    CreatedAt = a.CreatedAt,
                })
                .ToListAsync(cancellationToken);

            return new FailedLoginStatsResponse
            {
                Today = today,
                Last7Days = last7Days,
                Last30Days = last30Days,
                Recent = recent,
            };
        }

        public async Task<List<(string AttemptedEmail, int Count, DateTime LastAttemptAt)>> GetRepeatedFailedLoginsAsync(
            DateTime since, int threshold, CancellationToken cancellationToken = default)
        {
            var grouped = await _db.AuditLogs
                .Where(a => a.EventType == AuditEventType.Authentication && a.Action == LoginFailedAction && a.CreatedAt >= since)
                .GroupBy(a => a.ActorDisplayName)
                .Select(g => new { Email = g.Key, Count = g.Count(), LastAttemptAt = g.Max(a => a.CreatedAt) })
                .Where(g => g.Count >= threshold)
                .OrderByDescending(g => g.LastAttemptAt)
                .ToListAsync(cancellationToken);

            return grouped.Select(g => (g.Email, g.Count, g.LastAttemptAt)).ToList();
        }

        public async Task<List<AuditLogResponse>> GetRecentlyDisabledAccountsAsync(
            DateTime since, CancellationToken cancellationToken = default)
        {
            return await _db.AuditLogs
                .Where(a => a.Action == UserDisabledAction && a.CreatedAt >= since)
                .OrderByDescending(a => a.CreatedAt)
                .Select(a => new AuditLogResponse
                {
                    Id = a.Id,
                    ActorUserId = a.ActorUserId,
                    ActorDisplayName = a.ActorDisplayName,
                    EventType = a.EventType.ToString(),
                    Action = a.Action,
                    EntityType = a.EntityType,
                    EntityId = a.EntityId,
                    BusinessId = a.BusinessId,
                    Description = a.Description,
                    Success = a.Success,
                    CreatedAt = a.CreatedAt,
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<List<AuditLogResponse>> GetUserActivityAsync(
            Guid userId, int take, CancellationToken cancellationToken = default)
        {
            return await _db.AuditLogs
                .Where(a => a.ActorUserId == userId || (a.EntityType == "User" && a.EntityId == userId))
                .OrderByDescending(a => a.CreatedAt)
                .Take(take)
                .Select(a => new AuditLogResponse
                {
                    Id = a.Id,
                    ActorUserId = a.ActorUserId,
                    ActorDisplayName = a.ActorDisplayName,
                    EventType = a.EventType.ToString(),
                    Action = a.Action,
                    EntityType = a.EntityType,
                    EntityId = a.EntityId,
                    BusinessId = a.BusinessId,
                    BusinessName = a.Business != null ? a.Business.Name : null,
                    Description = a.Description,
                    Success = a.Success,
                    CreatedAt = a.CreatedAt,
                })
                .ToListAsync(cancellationToken);
        }
    }
}
