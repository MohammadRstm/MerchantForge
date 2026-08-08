using MerchForge.api.Models;
using MerchForge.api.DTOs.Auth;

namespace MerchForge.api.Factory
{
    public interface IRegistrationFactory
    {
       (User User, Business Business, BusinessUser BusinessUser) Create(RegisterRequest request);
    
     }
    
}
