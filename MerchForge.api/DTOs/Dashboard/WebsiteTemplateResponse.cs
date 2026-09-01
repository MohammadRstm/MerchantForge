namespace MerchForge.api.DTOs.Dashboard;

/// <summary>One row in the SuperAdmin's "manage website templates" list.</summary>
public class WebsiteTemplateResponse
{
    public Guid Id { get; set; }

    public Guid BusinessDomainId { get; set; }

    public string DomainName { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;

    public string PreviewImageUrl { get; set; } = string.Empty;

    public string? PreviewWebsiteUrl { get; set; }

    public bool IsActive { get; set; }

    public int DisplayOrder { get; set; }

    /// <summary>How many businesses have chosen this template — shown so a SuperAdmin can see the impact before retiring one.</summary>
    public int BusinessesUsingIt { get; set; }

    /// <summary>Total WebsiteTemplateRequest rows referencing this template, any status — a signal distinct from BusinessesUsingIt: what's being asked for, not what's live.</summary>
    public int RequestCount { get; set; }

    public int ActiveCustomizableComponentCount { get; set; }

    public DateTime CreatedAt { get; set; }
}
