using MerchForge.api.DTOs.ProductAi;
using MerchForge.api.Services.AI.Contracts;
using MerchForge.api.Services.AI.Interfaces;
using MerchForge.api.Services.BusinessDashboard.interfaces;
using MerchForge.api.Services.ProductAi;
using Microsoft.AspNetCore.Http;

namespace MerchForge.IntegrationTests.Fakes;

/// <summary>
/// Scripted stand-in for the AI provider.
///
/// Every product-AI test drives the workflow through this rather than a real model:
/// the behaviour worth protecting is what the orchestration does with a decision -
/// state updates, corrections, validation, transitions - and that is deterministic
/// only if the decision is. It also means the suite makes no paid API calls.
/// </summary>
public class FakeProductAiConversationClient : IProductAiConversationClient
{
    private readonly Queue<ProductAiDecision> _scripted = new();

    public string ModelName => "fake-model";

    /// <summary>Contexts received, so tests can assert what the agent was actually told.</summary>
    public List<ProductAiContext> ReceivedContexts { get; } = [];

    /// <summary>Set to make the next call fail, for the provider-outage path.</summary>
    public Exception? NextFailure { get; set; }

    public FakeProductAiConversationClient Enqueue(ProductAiDecision decision)
    {
        _scripted.Enqueue(decision);
        return this;
    }

    public Task<ProductAiTurnResult> ContinueConversationAsync(
        ProductAiContext context,
        CancellationToken cancellationToken = default)
    {
        ReceivedContexts.Add(context);

        if (NextFailure is not null)
        {
            var failure = NextFailure;
            NextFailure = null;
            throw failure;
        }

        if (_scripted.Count == 0)
        {
            throw new InvalidOperationException(
                "FakeProductAiConversationClient ran out of scripted decisions - the test made an unexpected extra turn.");
        }

        return Task.FromResult(new ProductAiTurnResult
        {
            Decision = _scripted.Dequeue(),
            PromptTokens = 100,
            CompletionTokens = 20,
        });
    }
}

public class FakeAiTranscriptionService : IAiTranscriptionService
{
    public string Transcript { get; set; } = "transcribed text";

    public int CallCount { get; private set; }

    public Task<string> TranscribeAsync(
        Stream audio,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        CallCount++;
        return Task.FromResult(Transcript);
    }
}

/// <summary>
/// Skips storage entirely. Validation, key construction and the ownership checks in
/// the real service are covered by ProductImageServiceTests in the unit suite.
/// </summary>
public class FakeProductImageService : IProductImageService
{
    public string SavedUrl { get; set; } = "/uploads/products/uploaded.png";

    /// <summary>
    /// The product ids images were stored against, so a test can assert the draft flow
    /// nests them under the product it will actually create.
    /// </summary>
    public List<Guid> SavedProductIds { get; } = [];

    public Task<string> SaveAsync(
        Guid businessId,
        Guid productId,
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        SavedProductIds.Add(productId);
        return Task.FromResult(SavedUrl);
    }

    public Task<string> SaveAsync(
        Guid businessId,
        Guid productId,
        byte[] bytes,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        SavedProductIds.Add(productId);
        return Task.FromResult(SavedUrl);
    }

    public byte[] ReadBytes { get; set; } = [1, 2, 3, 4];

    public string ReadContentType { get; set; } = "image/png";

    public Task<(byte[] Bytes, string ContentType)> ReadAsync(
        Guid businessId,
        string storedValue,
        CancellationToken cancellationToken = default)
        => Task.FromResult((ReadBytes, ReadContentType));

    public List<string> DeletedValues { get; } = [];

    public Task DeleteManyAsync(
        Guid businessId,
        IReadOnlyCollection<string> storedValues,
        CancellationToken cancellationToken = default)
    {
        DeletedValues.AddRange(storedValues);
        return Task.CompletedTask;
    }
}

/// <summary>Records what was logged so tests can assert on it without a logging framework.</summary>
public class RecordingAiInteractionLogger : IAiInteractionLogger
{
    public List<string> Events { get; } = [];

    public void LogTurnStarted(AiInteractionScope scope, string trigger)
        => Events.Add($"started:{trigger}");

    public void LogTurnSucceeded(
        AiInteractionScope scope,
        ProductAiAction action,
        int missingFieldCount,
        long elapsedMs,
        int? promptTokens,
        int? completionTokens)
        => Events.Add($"succeeded:{action}");

    public void LogTurnFailed(AiInteractionScope scope, long elapsedMs, Exception exception)
        => Events.Add("failed");

    public void LogValidationRejected(AiInteractionScope scope, string reason)
        => Events.Add($"rejected:{reason}");
}

/// <summary>
/// Test-only convenience preserving the old text-message call shape.
///
/// The production text-message endpoint/method was removed — the AI
/// product-creation feature is voice-only now — but most of these tests exist to
/// exercise ProductAiService's orchestration (state updates, corrections,
/// validation, transitions), not the transport a message arrived through. This
/// routes a plain string through the one remaining entry point,
/// SendVoiceMessageAsync, via a transcription fake that echoes it back verbatim,
/// so the exact same orchestration is exercised as before.
/// </summary>
internal static class ProductAiServiceTestExtensions
{
    private static IFormFile FakeVoiceFile() =>
        new FormFile(new MemoryStream([1, 2, 3, 4]), 0, 4, "file", "voice.webm")
        {
            Headers = new HeaderDictionary(),
            ContentType = "audio/webm",
        };

    public static Task<ProductDraftResponse> SendMessageAsync(
        this ProductAiService service,
        FakeAiTranscriptionService transcription,
        Guid businessId,
        Guid userId,
        Guid draftId,
        string message,
        CancellationToken cancellationToken = default)
    {
        transcription.Transcript = message;
        return service.SendVoiceMessageAsync(businessId, userId, draftId, FakeVoiceFile(), cancellationToken);
    }
}
