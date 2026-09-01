using MerchForge.api.DTOs.Common;
using MerchForge.api.Enums;

namespace MerchForge.api.DTOs.Audit;

public class AuditLogQueryRequest : PagedQuery
{
    public AuditEventType? EventType { get; set; }

    /// <summary>Matches against ActorDisplayName (name or attempted-email search).</summary>
    public string? Actor { get; set; }

    public bool? Success { get; set; }

    public DateTime? From { get; set; }

    public DateTime? To { get; set; }

    public Guid? BusinessId { get; set; }

    /// <summary>Scopes results to one entity (e.g. one WebsiteTemplate's own activity feed) when set.</summary>
    public Guid? EntityId { get; set; }
}
