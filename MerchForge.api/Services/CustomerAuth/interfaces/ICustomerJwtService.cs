using MerchForge.api.Models;

namespace MerchForge.api.Services.CustomerAuth.interfaces
{
    public interface ICustomerJwtService
    {
        string GenerateAccessToken(Customer customer);

        DateTime GetExpirationTime();
    }
}
