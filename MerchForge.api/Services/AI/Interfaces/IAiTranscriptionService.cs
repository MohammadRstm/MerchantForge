namespace MerchForge.api.Services.AI.Interfaces;

/// <summary>
/// Turns a voice message into text. Separated from the conversation client so the
/// orchestration never deals with audio: by the time a message reaches the agent it
/// is text, regardless of how it arrived.
/// </summary>
public interface IAiTranscriptionService
{
    Task<string> TranscribeAsync(
        Stream audio,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default);
}
