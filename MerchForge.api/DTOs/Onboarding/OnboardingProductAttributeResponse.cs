namespace MerchForge.api.DTOs.Onboarding;

/// <summary>
/// An optional product field a business in this domain can opt into during
/// registration, rendered as a checkbox.
/// </summary>
public class OnboardingProductAttributeResponse
{
    /// <summary>
    /// Stable machine key. This is what the client sends back as a selection, not
    /// the id — the key is what ends up in the metadata shape and in product
    /// metadata, so it's the meaningful identifier.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;

    /// <summary>Text | Number | Boolean | TextList — tells the form which input to use.</summary>
    public string ValueType { get; set; } = string.Empty;

    public int DisplayOrder { get; set; }
}
