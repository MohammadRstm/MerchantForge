using System.Text.Json;
using MerchForge.api.Enums;

namespace MerchForge.api.Models;

/// <summary>
/// One structural customization slot a specific WebsiteTemplate actually supports —
/// e.g. "heroImage" for fashion-template-01. This is the platform's catalogue of what
/// that template's own React code has been wired to read, not per-business
/// configuration; a business's saved values (Business.WebsiteCustomizationValues) are
/// only ever a values-for-these-keys map, never a place to invent a new key.
///
/// Deliberately never assumed uniform across templates: a brand-new WebsiteTemplate
/// starts with zero rows here until a developer inspects its actual components and
/// registers what it supports. Unlike ProductAttributeDefinition/Business.MetadataShape,
/// there is no per-business "opt in" snapshot step — every active row for a business's
/// current WebsiteTemplateId simply IS that business's customization form, looked up
/// live, because there is no "opt out of the hero slot" concept the way a business can
/// opt out of an optional product field.
/// </summary>
public class WebsiteTemplateCustomizableComponent
{
    public Guid Id { get; set; }

    public Guid WebsiteTemplateId { get; set; }

    /// <summary>
    /// The JSON key this slot occupies in Business.WebsiteCustomizationValues (under
    /// that template's own namespace). Stable and machine-facing — same policy as
    /// ProductAttributeDefinition.Key: never renamed or reused once live, retire via
    /// IsActive = false instead.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Display label on the customization form. Safe to change.</summary>
    public string Label { get; set; } = string.Empty;

    public WebsiteCustomizableValueType ValueType { get; set; }

    public bool IsRequired { get; set; }

    /// <summary>Closed set of permitted values, as a JSON array of strings — only meaningful for Select.</summary>
    public JsonDocument? AllowedValues { get; set; }

    /// <summary>Optional guidance shown next to the control, e.g. "Recommended size 1920x600px".</summary>
    public string? HelpText { get; set; }

    public int DisplayOrder { get; set; }

    /// <summary>Lets a slot be retired without deleting it, so businesses that already saved a value under its key keep working (the value is simply dropped on the next publish — see WebsiteCustomizationService).</summary>
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties

    public WebsiteTemplate WebsiteTemplate { get; set; } = null!;
}
