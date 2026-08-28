namespace MerchForge.api.DTOs.Dashboard;

public class WebsiteTemplateCustomizableComponentResponse
{
    public Guid Id { get; set; }

    public Guid WebsiteTemplateId { get; set; }

    public string TemplateName { get; set; } = string.Empty;

    public string Key { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;

    /// <summary>Text | Textarea | Image | Color | Url | Boolean | Number | Select | Link.</summary>
    public string ValueType { get; set; } = string.Empty;

    public bool IsRequired { get; set; }

    public List<string> AllowedValues { get; set; } = new();

    public string? HelpText { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }
}
