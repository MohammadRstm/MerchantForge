using MerchForge.api.Enums;

namespace MerchForge.api.Models;

// A persistent, append-only record of security and administrative events -
// there is no edit/delete path anywhere in the codebase for this table by
// design (the point of an audit log is accountability, not a mutable log).
public class AuditLog
{
    public Guid Id { get; set; }

    // Null for events with no resolved platform-user actor (e.g. a failed
    // login attempt against an email with no account).
    public Guid? ActorUserId { get; set; }

    // A snapshot of the actor's name/email at write time, so the log still
    // reads correctly even for actors that can't be resolved to a User row
    // (an attempted email on a failed login) or if a name changes later.
    public string ActorDisplayName { get; set; } = string.Empty;

    public AuditEventType EventType { get; set; }

    // Free-form, e.g. "LoginSucceeded", "UserDisabled", "SubscriptionPlanUpdated" -
    // open-ended on purpose so new actions don't need enum/migration churn.
    public string Action { get; set; } = string.Empty;

    public string? EntityType { get; set; }

    public Guid? EntityId { get; set; }

    public Guid? BusinessId { get; set; }

    public string Description { get; set; } = string.Empty;

    public bool Success { get; set; }

    // Structured extra detail (e.g. an old/new value diff), stored as a JSON
    // string rather than rigid columns so new event types don't need new
    // migrations. Never used for anything sensitive - see LogAsync's callers.
    public string? Metadata { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User? ActorUser { get; set; }

    public Business? Business { get; set; }
}
