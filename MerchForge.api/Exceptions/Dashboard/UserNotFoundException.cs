using MerchForge.api.Exceptions.Base;

namespace MerchForge.api.Exceptions.Dashboard
{
    public class UserNotFoundException : AppException
    {
        public UserNotFoundException() : base(
            Enums.ErrorType.NotFound,
            "USER_NOT_FOUND",
            "User was not found")
        {

        }
    }
}
