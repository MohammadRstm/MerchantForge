using MerchForge.api.Exceptions.Base;

namespace MerchForge.api.Exceptions.Dashboard
{
    public class DomainHasNoActiveTemplateException : AppException
    {
        public DomainHasNoActiveTemplateException() : base(
            Enums.ErrorType.Conflict,
            "DOMAIN_HAS_NO_ACTIVE_TEMPLATE",
            "This domain has no active website template to showcase yet")
        {
        }
    }
}
