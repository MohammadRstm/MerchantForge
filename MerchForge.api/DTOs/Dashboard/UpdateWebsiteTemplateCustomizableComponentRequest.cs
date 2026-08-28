namespace MerchForge.api.DTOs.Dashboard;

/// <summary>WebsiteTemplateId and Key are immutable once created -- not part of this request.</summary>
public class UpdateWebsiteTemplateCustomizableComponentRequest
{
    public string Label { get; set; } = string.Empty;

    public string ValueType { get; set; } = string.Empty;

    public bool IsRequired { get; set; }

    public List<string> AllowedValues { get; set; } = new();

    public string? HelpText { get; set; }

    public int DisplayOrder { get; set; }
}
