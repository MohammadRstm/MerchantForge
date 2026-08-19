using MerchForge.api.Exceptions.Base;

namespace MerchForge.api.Exceptions.Email
{
    public class EmailDeliveryException : AppException
    {
        public EmailDeliveryException(
            string message = "Unable to send email.")
            : base(
                Enums.ErrorType.Unexpected,
                "EMAIL_DELIVERY_FAILED",
                message)
        {
        }
    }
}
