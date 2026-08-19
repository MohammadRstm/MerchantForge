using MerchForge.api.Exceptions.Base;

namespace MerchForge.api.Exceptions.AI
{
    /// <summary>
    /// The requested operation isn't valid for the draft's current state — confirming
    /// a draft that was already completed or cancelled, approving an image when none
    /// is pending, or continuing a finished conversation.
    ///
    /// Conflict rather than Validation: the request is well-formed, it just arrived
    /// against a state that no longer accepts it, typically a stale tab or a double
    /// click.
    /// </summary>
    public class ProductDraftStateException : AppException
    {
        public ProductDraftStateException(string message) : base(
            Enums.ErrorType.Conflict,
            "PRODUCT_DRAFT_INVALID_STATE",
            message)
        {
        }
    }
}
