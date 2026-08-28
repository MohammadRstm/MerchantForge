namespace MerchForge.api.DTOs.BusinessDashboard;

public class PublishWebsiteCustomizationResponse
{
    /// <summary>
    /// Template-field keys the draft had saved values for that are no longer part of
    /// the current template's catalogue (a SuperAdmin retired them after the value was
    /// saved) -- dropped rather than blocking the whole publish, since the owner didn't
    /// cause this and can't fix a field that no longer exists.
    /// </summary>
    public List<string> DroppedTemplateFieldKeys { get; set; } = new();

    public DateTime PublishedAt { get; set; }
}
