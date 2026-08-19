using MerchForge.api.Exceptions.Base;

namespace MerchForge.api.Exceptions.Auth
{
    public class SuperAdminAlreadyExistsException : AppException
    {
        public SuperAdminAlreadyExistsException() : base(
            Enums.ErrorType.Conflict,
            "SUPER_ADMIN_ALREADY_EXISTS",
            "A super admin account already exists")
        {

        }
    }
}
