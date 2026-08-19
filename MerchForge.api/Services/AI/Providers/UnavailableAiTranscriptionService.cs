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
        throw new AiConversationException(
            "Voice messages aren't configured on this server. Please type your message instead.");
    }
}
