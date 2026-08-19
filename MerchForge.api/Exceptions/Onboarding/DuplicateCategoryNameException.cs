using MerchForge.api.Exceptions.Base;

namespace MerchForge.api.Exceptions.Onboarding
{
    public class DuplicateCategoryNameException : AppException
    {
        public DuplicateCategoryNameException(IEnumerable<string> names) : base(
            Enums.ErrorType.Validation,
            "DUPLICATE_CATEGORY_NAME",
            $"These categories already exist: {string.Join(", ", names)}. Select them instead of adding them again.")
        {
        }
    }
}
