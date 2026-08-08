using MerchForge.api.Exceptions.Base;

namespace MerchForge.api.Exceptions.Auth
{
    public class EmailAlreadyExistsException : AppException
    {
        public EmailAlreadyExistsException(): base("A user already exists with this email")
        {

        }
    }
}
