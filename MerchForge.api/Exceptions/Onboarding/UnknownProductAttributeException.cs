using MerchForge.api.Exceptions.Base;

namespace MerchForge.api.Exceptions.Onboarding
{
    /// <summary>
    /// Raised when a submitted product field key isn't one this domain offers.
    /// Constraining metadata keys to the platform catalogue is the whole point of
    /// ProductAttributeDefinition, so an unrecognised key is rejected rather than
    /// quietly dropped — silently ignoring it would leave the business thinking they
    /// had a field they don't.
    /// </summary>
    public class UnknownProductAttributeException : AppException
    {
        public UnknownProductAttributeException(IEnumerable<string> keys) : base(
            Enums.ErrorType.Validation,
            "UNKNOWN_PRODUCT_ATTRIBUTE",
            $"These product fields aren't available for the selected business domain: {string.Join(", ", keys)}.")
        {
        }
    }
}
