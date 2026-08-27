namespace MerchForge.api.DTOs.Dashboard;

public class CreateProductAttributeDefinitionRequest
{
    public Guid BusinessDomainId { get; set; }

    /// <summary>The JSON key this field will occupy in Product.Metadata. Immutable after creation.</summary>
    public string Key { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;

    public string ValueType { get; set; } = string.Empty;

    public bool IsRequired { get; set; }

    public List<string> AllowedValues { get; set; } = new();

    public int DisplayOrder { get; set; }
}
