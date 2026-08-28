using MerchForge.api.Exceptions.Base;

namespace MerchForge.api.Exceptions.CustomerAuth
{
    public class CustomerNotFoundException : AppException
    {
        public CustomerNotFoundException() : base(
            Enums.ErrorType.NotFound,
            "CUSTOMER_NOT_FOUND",
            "Customer not found")
        {
        }
    }
}
