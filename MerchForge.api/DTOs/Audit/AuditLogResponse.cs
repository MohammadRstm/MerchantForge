namespace MerchForge.api.DTOs.Audit;

public class AuditLogResponse
{
    public Guid Id { get; set; }

    public Guid? ActorUserId { get; set; }

    public string ActorDisplayName { get; set; } = string.Empty;

    public string EventType { get; set; } = string.Empty;

    public string Action { get; set; } = string.Empty;

    public string? EntityType { get; set; }

    public Guid? EntityId { get; set; }

    public Guid? BusinessId { get; set; }

    public string? BusinessName { get; set; }

    public string Description { get; set; } = string.Empty;

    public bool Success { get; set; }

    public DateTime CreatedAt { get; set; }
}
