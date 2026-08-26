using MerchForge.api.DTOs.Common;
using MerchForge.api.DTOs.WebsiteTemplateRequests;
using MerchForge.api.Models;

namespace MerchForge.api.Repositories.Interfaces
{
    public interface IWebsiteTemplateRequestRepository
    {
        Task<bool> HasOpenRequestAsync(Guid businessId, CancellationToken cancellationToken = default);

        Task CreateAsync(WebsiteTemplateRequest request, CancellationToken cancellationToken = default);

        Task<List<WebsiteTemplateRequestResponse>> GetForBusinessAsync(
            Guid businessId,
            CancellationToken cancellationToken = default);

        /// <summary>Loads a tracked entity for a status-transition mutation (Start Build / Close).</summary>
        Task<WebsiteTemplateRequest?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken = default);

        Task<WebsiteTemplateRequestDetailResponse?> GetDetailByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default);

        Task<(List<WebsiteTemplateRequestSummaryResponse> Items, int TotalCount)> GetPagedAsync(
            WebsiteTemplateRequestsQueryRequest query,
            CancellationToken cancellationToken = default);

        /// <summary>Unconditionally sets the business's "currently live on" template and site URL — used when a request is closed, so a later close (e.g. after a redesign) is free to overwrite an earlier one.</summary>
        Task SetBusinessActiveWebsiteTemplateAsync(
            Guid businessId,
            Guid websiteTemplateId,
            string websiteUrl,
            CancellationToken cancellationToken = default);

        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
