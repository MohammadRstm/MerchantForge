using MerchForge.api.Exceptions.AI;
using MerchForge.api.Services.AI.Interfaces;

namespace MerchForge.api.Services.AI.Providers;

public class UnavailableAiTranscriptionService : IAiTranscriptionService
{
    public Task<string> TranscribeAsync(
        Stream audio,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        // Deliberately generic: whether the provider is unconfigured or genuinely
        // down is an operational detail the merchant has no use for and no way to
        // act on either way — "try again later" is the only honest, actionable
        // thing to tell them.
        throw new AiConversationException(
            "A server error occurred. Please try again later.");
    }
}
