using MerchForge.api.Exceptions.Base;

namespace MerchForge.api.Exceptions.Dashboard
{
    public class DemoBusinessAlreadyExistsForDomainException : AppException
    {
        public DemoBusinessAlreadyExistsForDomainException() : base(
            Enums.ErrorType.Conflict,
            "DEMO_BUSINESS_ALREADY_EXISTS_FOR_DOMAIN",
            "This domain already has a demo business")
        {
        }
    }
}
