using MerchForge.api.Data;
using MerchForge.api.Models;
using MerchForge.api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MerchForge.api.Repositories.Implementations
{
    public class ImageEditJobRepository : IImageEditJobRepository
    {
        private readonly MerchForgeDbContext _db;

        public ImageEditJobRepository(MerchForgeDbContext db)
        {
            _db = db;
        }

        public async Task<ImageEditJob> CreateAsync(
            ImageEditJob job,
            CancellationToken cancellationToken = default)
        {
            await _db.ImageEditJobs.AddAsync(job, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);

            return job;
        }

        public async Task<ImageEditJob?> GetForBusinessAsync(
            Guid businessId,
            Guid jobId,
            CancellationToken cancellationToken = default)
        {
            return await _db.ImageEditJobs
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    j => j.Id == jobId && j.BusinessId == businessId,
                    cancellationToken);
        }
    }
}
