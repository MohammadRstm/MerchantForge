namespace MerchForge.api.Repositories.Interfaces
{
    public interface IBusinessRepository
    {
        Task<BusinessUserResponse> GetUserBusinessAsync(Guid userId, CancellationToken cancellationToken);
    }
}
