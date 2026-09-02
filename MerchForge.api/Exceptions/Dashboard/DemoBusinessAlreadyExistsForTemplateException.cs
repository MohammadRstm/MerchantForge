using MerchForge.api.Exceptions.Base;

namespace MerchForge.api.Exceptions.Dashboard
{
    public class DemoBusinessAlreadyExistsForTemplateException : AppException
    {
        public DemoBusinessAlreadyExistsForTemplateException() : base(
            Enums.ErrorType.Conflict,
            "DEMO_BUSINESS_ALREADY_EXISTS_FOR_TEMPLATE",
            "This template already has a demo business")
        {
        }
    }
}
