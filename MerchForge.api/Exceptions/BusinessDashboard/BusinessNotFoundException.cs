using MerchForge.api.Exceptions.Base;

namespace MerchForge.api.Exceptions.BusinessDashboard
{
    public class BusinessNotFoundException : AppException
    {
        public BusinessNotFoundException() : base(
            Enums.ErrorType.NotFound,
            "BUSINESS_NOT_FOUND",
            "Business was not found")
        {

        }
    }
}
