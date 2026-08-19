using MerchForge.api.Models;

namespace MerchForge.api.Services.Auth.interfaces
{
    public interface IJwtService
    {
        Task<string> GenerateAccessToken(User user);

        DateTime GetExpirationTime();
    }
}
