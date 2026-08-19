using MerchForge.api.Exceptions.Base;

namespace MerchForge.api.Exceptions.Onboarding
{
    public class BusinessDomainNotFoundException : AppException
    {
        public BusinessDomainNotFoundException() : base(
            Enums.ErrorType.NotFound,
            "BUSINESS_DOMAIN_NOT_FOUND",
            "Business domain was not found")
        {
        }
    }
}
