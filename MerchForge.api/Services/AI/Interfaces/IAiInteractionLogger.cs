using MerchForge.api.Services.AI.Contracts;

namespace MerchForge.api.Services.AI.Interfaces;

/// <summary>
/// Records what the agent was asked and what it decided.
///
/// An abstraction rather than ILogger calls scattered through the orchestration so
/// these can later be persisted to a table for debugging real conversations, without
/// touching the call sites.
/// </summary>
public interface IAiInteractionLogger
{
    void LogTurnStarted(AiInteractionScope scope, string trigger);

    void LogTurnSucceeded(
        AiInteractionScope scope,
        ProductAiAction action,
        int missingFieldCount,
        long elapsedMs,
        int? promptTokens,
        int? completionTokens);

    void LogTurnFailed(AiInteractionScope scope, long elapsedMs, Exception exception);

    void LogValidationRejected(AiInteractionScope scope, string reason);
}

/// <summary>
/// Identifies which conversation a log line belongs to. Ids only — no message
/// content, so that logs stay safe to ship somewhere central even when owners
/// describe products in ways they would not want retained.
/// </summary>
public record AiInteractionScope(
    Guid BusinessId,
    Guid UserId,
    Guid DraftId,
    string Model);
