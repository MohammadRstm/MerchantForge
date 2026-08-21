namespace MerchForge.api.Enums;

/// <summary>
/// What kind of value a product metadata field holds. Deliberately a small closed
/// set: it exists so a product form can pick the right input and so metadata can
/// eventually be validated, not to model arbitrary types.
/// </summary>
public enum ProductAttributeValueType
{
    /// <summary>Single free-text value. JSON string. e.g. material: "Cotton"</summary>
    Text,

    /// <summary>Single numeric value. JSON number. e.g. calories: 900</summary>
    Number,

    /// <summary>Yes/no. JSON boolean. e.g. spicy: true</summary>
    Boolean,

    /// <summary>Multiple text values. JSON array of strings. e.g. sizes: ["S","M","L"]</summary>
    TextList,

    /// <summary>Multiple hex colors, picked via a color wheel. JSON array of "#RRGGBB" strings. e.g. colors: ["#FF0000","#000000"]</summary>
    ColorList
}
