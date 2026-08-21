using MerchForge.api.Models;

namespace MerchForge.api.Repositories.Interfaces
{
    public interface IImageEditJobRepository
    {
        Task<ImageEditJob> CreateAsync(
            ImageEditJob job,
            CancellationToken cancellationToken = default);

        /// <summary>Null when the job doesn't exist or belongs to another business.</summary>
        Task<ImageEditJob?> GetForBusinessAsync(
            Guid businessId,
            Guid jobId,
            CancellationToken cancellationToken = default);
    }
}
