using MerchForge.api.Services.AI.Contracts;
using MerchForge.api.Services.AI.Interfaces;
using MerchForge.api.Services.BusinessDashboard.interfaces;
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

/// <summary>Skips disk entirely; the real upload path is covered by the product CRUD tests.</summary>
public class FakeProductImageService : IProductImageService
{
    public string SavedUrl { get; set; } = "/uploads/products/uploaded.png";

    public Task<string> SaveAsync(
        Guid businessId,
        IFormFile file,
        CancellationToken cancellationToken = default)
        => Task.FromResult(SavedUrl);

    public Task<string> SaveAsync(
        Guid businessId,
        byte[] bytes,
        string contentType,
        CancellationToken cancellationToken = default)
        => Task.FromResult(SavedUrl);

    public byte[] ReadBytes { get; set; } = [1, 2, 3, 4];

    public string ReadContentType { get; set; } = "image/png";

    public Task<(byte[] Bytes, string ContentType)> ReadAsync(
        Guid businessId,
        string url,
        CancellationToken cancellationToken = default)
        => Task.FromResult((ReadBytes, ReadContentType));
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
