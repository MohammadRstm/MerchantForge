using MerchForge.api.Exceptions.Base;

namespace MerchForge.api.Exceptions.AI
{
    /// <summary>
    /// The AI provider failed, or returned something that doesn't match the decision
    /// schema. Surfaced as Unexpected rather than Validation: nothing the owner typed
    /// caused it, so telling them to fix their input would be wrong.
    /// </summary>
    public class AiConversationException : AppException
    {
        public AiConversationException(string message, Exception? inner = null) : base(
            Enums.ErrorType.Unexpected,
            "AI_CONVERSATION_FAILED",
            message)
        {
            InnerFailure = inner;
        }

        /// <summary>Kept for logging; never serialized to the client.</summary>
        public Exception? InnerFailure { get; }
    }
}
