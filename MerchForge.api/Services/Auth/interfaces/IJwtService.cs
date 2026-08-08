using MerchForge.api.Models;

namespace MerchForge.api.Services.Auth.interfaces
{
    public interface IJwtService
    {
        string GenerateAccessToken(User user);

        DateTime GetExpirationTime();
    }
}
