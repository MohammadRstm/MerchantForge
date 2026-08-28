namespace MerchForge.api.DTOs.Dashboard;

public class CreateWebsiteTemplateCustomizableComponentRequest
{
    public Guid WebsiteTemplateId { get; set; }

    /// <summary>The key this slot will occupy in Business.WebsiteCustomizationValues. Immutable after creation.</summary>
    public string Key { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;

    public string ValueType { get; set; } = string.Empty;

    public bool IsRequired { get; set; }

    public List<string> AllowedValues { get; set; } = new();

    public string? HelpText { get; set; }

    public int DisplayOrder { get; set; }
}
