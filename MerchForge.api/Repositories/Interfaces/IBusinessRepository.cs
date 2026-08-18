namespace MerchForge.api.Repositories.Interfaces
{
    public interface IBusinessRepository
    {
        Task<BusinessContextResponse?> GetUserBusinessAsync(Guid userId, CancellationToken cancellationToken);
    }
}
