namespace MerchForge.api.DTOs.Dashboard;

public class UpdateMetadataShapeRequest
{
    public List<UpdateMetadataShapeFieldRequest> Fields { get; set; } = new();
}

public class UpdateMetadataShapeFieldRequest
{
    public string Key { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;

    /// <summary>Text | Number | Boolean | TextList.</summary>
    public string ValueType { get; set; } = string.Empty;

    public bool IsRequired { get; set; }

    public List<string> AllowedValues { get; set; } = new();

    public int DisplayOrder { get; set; }
}
