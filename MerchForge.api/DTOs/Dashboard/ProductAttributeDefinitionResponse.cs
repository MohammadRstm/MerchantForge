namespace MerchForge.api.DTOs.Dashboard;

public class ProductAttributeDefinitionResponse
{
    public Guid Id { get; set; }

    public Guid BusinessDomainId { get; set; }

    public string DomainName { get; set; } = string.Empty;

    public string Key { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;

    /// <summary>Text | Number | Boolean | TextList | ColorList.</summary>
    public string ValueType { get; set; } = string.Empty;

    public bool IsRequired { get; set; }

    public List<string> AllowedValues { get; set; } = new();

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }
}
