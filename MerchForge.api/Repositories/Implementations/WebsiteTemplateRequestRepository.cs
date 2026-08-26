using MerchForge.api.Data;
using MerchForge.api.DTOs.Common;
using MerchForge.api.DTOs.WebsiteTemplateRequests;
using MerchForge.api.Enums;
using MerchForge.api.Models;
using MerchForge.api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MerchForge.api.Repositories.Implementations
{
    public class WebsiteTemplateRequestRepository : IWebsiteTemplateRequestRepository
    {
        private static readonly WebsiteTemplateRequestStatus[] OpenStatuses =
        [
            WebsiteTemplateRequestStatus.Pending,
            WebsiteTemplateRequestStatus.InProgress,
        ];

        private readonly MerchForgeDbContext _db;

        public WebsiteTemplateRequestRepository(MerchForgeDbContext db)
        {
            _db = db;
        }

        public async Task<bool> HasOpenRequestAsync(Guid businessId, CancellationToken cancellationToken = default)
        {
            return await _db.WebsiteTemplateRequests
                .AnyAsync(r => r.BusinessId == businessId && OpenStatuses.Contains(r.Status), cancellationToken);
        }

        public async Task CreateAsync(WebsiteTemplateRequest request, CancellationToken cancellationToken = default)
        {
            _db.WebsiteTemplateRequests.Add(request);
            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task<List<WebsiteTemplateRequestResponse>> GetForBusinessAsync(
            Guid businessId,
            CancellationToken cancellationToken = default)
        {
            return await _db.WebsiteTemplateRequests
                .AsNoTracking()
                .Where(r => r.BusinessId == businessId)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new WebsiteTemplateRequestResponse
                {
                    Id = r.Id,
                    WebsiteTemplateId = r.WebsiteTemplateId,
                    TemplateName = r.WebsiteTemplate.Name,
                    TemplateLabel = r.WebsiteTemplate.Label,
                    DomainName = r.Business.BusinessDomain != null ? r.Business.BusinessDomain.Name : string.Empty,
                    CustomizationNotes = r.CustomizationNotes,
                    Status = r.Status,
                    CreatedAt = r.CreatedAt,
                    BuildStartedAt = r.BuildStartedAt,
                    ClosedAt = r.ClosedAt,
                    FinalWebsiteUrl = r.FinalWebsiteUrl,
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<WebsiteTemplateRequest?> GetTrackedByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            return await _db.WebsiteTemplateRequests
                .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        }

        public async Task<WebsiteTemplateRequestDetailResponse?> GetDetailByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            return await _db.WebsiteTemplateRequests
                .AsNoTracking()
                .Where(r => r.Id == id)
                .Select(r => new WebsiteTemplateRequestDetailResponse
                {
                    Id = r.Id,
                    BusinessId = r.BusinessId,
                    BusinessName = r.Business.Name,
                    OwnerFullName = r.Business.Owner.FirstName + " " + r.Business.Owner.LastName,
                    OwnerEmail = r.Business.Owner.Email,
                    WebsiteTemplateId = r.WebsiteTemplateId,
                    TemplateName = r.WebsiteTemplate.Name,
                    TemplateLabel = r.WebsiteTemplate.Label,
                    DomainName = r.Business.BusinessDomain != null ? r.Business.BusinessDomain.Name : string.Empty,
                    CustomizationNotes = r.CustomizationNotes,
                    Status = r.Status,
                    CreatedAt = r.CreatedAt,
                    BuildStartedAt = r.BuildStartedAt,
                    ClosedAt = r.ClosedAt,
                    ClosedByFullName = r.ClosedByUserId == null
                        ? null
                        : _db.Users.Where(u => u.Id == r.ClosedByUserId).Select(u => u.FirstName + " " + u.LastName).FirstOrDefault(),
                    FinalWebsiteUrl = r.FinalWebsiteUrl,
                })
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<(List<WebsiteTemplateRequestSummaryResponse> Items, int TotalCount)> GetPagedAsync(
            WebsiteTemplateRequestsQueryRequest query,
            CancellationToken cancellationToken = default)
        {
            var baseQuery = _db.WebsiteTemplateRequests.AsQueryable();

            if (query.Status is not null)
            {
                baseQuery = baseQuery.Where(r => r.Status == query.Status);
            }

            var totalCount = await baseQuery.CountAsync(cancellationToken);

            var projected = baseQuery.Select(r => new WebsiteTemplateRequestSummaryResponse
            {
                Id = r.Id,
                BusinessId = r.BusinessId,
                BusinessName = r.Business.Name,
                OwnerFullName = r.Business.Owner.FirstName + " " + r.Business.Owner.LastName,
                OwnerEmail = r.Business.Owner.Email,
                TemplateLabel = r.WebsiteTemplate.Label,
                DomainName = r.Business.BusinessDomain != null ? r.Business.BusinessDomain.Name : string.Empty,
                Status = r.Status,
                CreatedAt = r.CreatedAt,
                FinalWebsiteUrl = r.FinalWebsiteUrl,
            });

            projected = query.SortDescending
                ? projected.OrderByDescending(x => x.CreatedAt)
                : projected.OrderBy(x => x.CreatedAt);

            var items = await projected
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync(cancellationToken);

            return (items, totalCount);
        }

        public async Task SetBusinessActiveWebsiteTemplateAsync(
            Guid businessId,
            Guid websiteTemplateId,
            string websiteUrl,
            CancellationToken cancellationToken = default)
        {
            await _db.Businesses
                .Where(b => b.Id == businessId)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(b => b.WebsiteTemplateId, websiteTemplateId)
                        .SetProperty(b => b.WebsiteTemplateChosenAt, DateTime.UtcNow)
                        .SetProperty(b => b.WebsiteUrl, websiteUrl)
                        .SetProperty(b => b.UpdatedAt, DateTime.UtcNow),
                    cancellationToken);
        }

        public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}
