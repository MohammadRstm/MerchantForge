namespace MerchForge.api.DTOs.BusinessDashboard;

/// <summary>
/// Everything the product form needs to render: which categories this business may
/// use, and which optional fields it opted into during onboarding.
///
/// One endpoint rather than two because a modal needs both before it can show
/// anything, and two round trips would make it pop up half-built.
/// </summary>
public class ProductFormResponse
{
    public List<ProductFormCategoryResponse> Categories { get; set; } = [];

    /// <summary>
    /// Derived from the business's metadata shape. Empty when the business opted
    /// into nothing, in which case the form shows only the fixed fields.
    /// </summary>
    public List<ProductFormFieldResponse> MetadataFields { get; set; } = [];
}

public class ProductFormCategoryResponse
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;
}

public class ProductFormFieldResponse
{
    public string Key { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;

    /// <summary>Text | Number | Boolean | TextList — decides which input to render.</summary>
    public string ValueType { get; set; } = string.Empty;
}
