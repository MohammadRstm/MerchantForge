using MerchForge.api.Data;
using MerchForge.api.Models;
using MerchForge.api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MerchForge.api.Repositories.Implementations
{
    public class WebsiteCustomizationRepository : IWebsiteCustomizationRepository
    {
        private readonly MerchForgeDbContext _db;

        public WebsiteCustomizationRepository(MerchForgeDbContext db)
        {
            _db = db;
        }

        public async Task<Business?> GetTrackedBusinessAsync(
            Guid businessId,
            CancellationToken cancellationToken = default)
        {
            return await _db.Businesses
                .FirstOrDefaultAsync(b => b.Id == businessId, cancellationToken);
        }

        public async Task<BusinessWebsiteDraft?> GetTrackedDraftAsync(
            Guid businessId,
            CancellationToken cancellationToken = default)
        {
            return await _db.BusinessWebsiteDrafts
                .FirstOrDefaultAsync(d => d.BusinessId == businessId, cancellationToken);
        }

        public async Task CreateDraftAsync(
            BusinessWebsiteDraft draft,
            CancellationToken cancellationToken = default)
        {
            _db.BusinessWebsiteDrafts.Add(draft);
            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}
