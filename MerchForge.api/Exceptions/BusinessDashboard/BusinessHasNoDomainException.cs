using MerchForge.api.Exceptions.Base;

namespace MerchForge.api.Exceptions.BusinessDashboard
{
    public class BusinessHasNoDomainException : AppException
    {
        public BusinessHasNoDomainException() : base(
            Enums.ErrorType.Validation,
            "BUSINESS_HAS_NO_DOMAIN",
            "This business has no domain set, so no website templates are available yet")
        {
        }
    }
}
