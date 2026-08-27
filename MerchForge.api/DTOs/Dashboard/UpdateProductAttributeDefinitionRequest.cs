namespace MerchForge.api.DTOs.Dashboard;

/// <summary>BusinessDomainId and Key are immutable once created -- not part of this request.</summary>
public class UpdateProductAttributeDefinitionRequest
{
    public string Label { get; set; } = string.Empty;

    public string ValueType { get; set; } = string.Empty;

    public bool IsRequired { get; set; }

    public List<string> AllowedValues { get; set; } = new();

    public int DisplayOrder { get; set; }
}
