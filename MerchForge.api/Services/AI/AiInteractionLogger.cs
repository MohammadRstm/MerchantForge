using MerchForge.api.Services.AI.Contracts;
using MerchForge.api.Services.AI.Interfaces;

namespace MerchForge.api.Services.AI;

/// <summary>
/// Structured logging for AI turns. Every call uses message templates with named
/// properties rather than interpolated strings, so the fields stay queryable.
///
/// Deliberately logs no message text, no draft contents and no credentials — only
/// ids, the decided action, timings and token counts. That is enough to reconstruct
/// what happened without retaining what was said.
/// </summary>
public class AiInteractionLogger : IAiInteractionLogger
{
    private readonly ILogger<AiInteractionLogger> _logger;

    public AiInteractionLogger(ILogger<AiInteractionLogger> logger)
    {
        _logger = logger;
    }

    public void LogTurnStarted(AiInteractionScope scope, string trigger)
    {
        _logger.LogInformation(
            "AI product turn started. BusinessId={BusinessId} UserId={UserId} DraftId={DraftId} Model={Model} Trigger={Trigger}",
            scope.BusinessId, scope.UserId, scope.DraftId, scope.Model, trigger);
    }

    public void LogTurnSucceeded(
        AiInteractionScope scope,
        ProductAiAction action,
        int missingFieldCount,
        long elapsedMs,
        int? promptTokens,
        int? completionTokens)
    {
        _logger.LogInformation(
            "AI product turn succeeded. BusinessId={BusinessId} UserId={UserId} DraftId={DraftId} " +
            "Model={Model} Action={Action} MissingFieldCount={MissingFieldCount} DurationMs={DurationMs} " +
            "PromptTokens={PromptTokens} CompletionTokens={CompletionTokens}",
            scope.BusinessId, scope.UserId, scope.DraftId, scope.Model, action,
            missingFieldCount, elapsedMs, promptTokens, completionTokens);
    }

    public void LogTurnFailed(AiInteractionScope scope, long elapsedMs, Exception exception)
    {
        _logger.LogError(
            exception,
            "AI product turn failed. BusinessId={BusinessId} UserId={UserId} DraftId={DraftId} " +
            "Model={Model} DurationMs={DurationMs}",
            scope.BusinessId, scope.UserId, scope.DraftId, scope.Model, elapsedMs);
    }

    public void LogValidationRejected(AiInteractionScope scope, string reason)
    {
        // Warning, not Error: the agent proposing something invalid is expected
        // occasionally and is handled by asking the owner again, not a fault.
        _logger.LogWarning(
            "AI product decision rejected by validation. BusinessId={BusinessId} UserId={UserId} " +
            "DraftId={DraftId} Model={Model} Reason={Reason}",
            scope.BusinessId, scope.UserId, scope.DraftId, scope.Model, reason);
    }
}
