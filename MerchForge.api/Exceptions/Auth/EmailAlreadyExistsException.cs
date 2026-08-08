using MerchForge.api.Exceptions.Base;

namespace MerchForge.api.Exceptions.Auth
{
    public class EmailAlreadyExistsException : AppException
    {
        public EmailAlreadyExistsException(): base(
            Enums.ErrorType.Conflict,
            "EMAIL_ALREADY_EXISTS",
            "A user already exists with this email")
        {

        }
    }
}
