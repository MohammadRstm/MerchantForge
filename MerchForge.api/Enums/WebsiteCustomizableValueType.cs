namespace MerchForge.api.Enums;

/// <summary>
/// Every value shape a WebsiteTemplateCustomizableComponent can declare. Link's value
/// is a labeled { text, url } pair (a CTA button); Url is a bare string (e.g. a social
/// profile link) — the two are deliberately distinct types.
/// </summary>
public enum WebsiteCustomizableValueType
{
    Text,
    Textarea,
    Image,
    Color,
    Url,
    Boolean,
    Number,
    Select,
    Link
}
