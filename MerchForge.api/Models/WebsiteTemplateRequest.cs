using MerchForge.api.Enums;

namespace MerchForge.api.Models;

/// <summary>
/// A business owner's request to have a chosen <see cref="WebsiteTemplate"/> custom-
/// built and deployed for their business. This is the bridge record between the
/// owner's choice and the eventual deployed site: once <see cref="Status"/> reaches
/// <see cref="WebsiteTemplateRequestStatus.Closed"/>, <see cref="FinalWebsiteUrl"/> is
/// the deployed site and <see cref="BusinessId"/> is what it was built for.
///
/// Unlike the retired one-shot "choose a template" flow, this is reviewed by a
/// SuperAdmin before anything is built - CustomizationNotes carries what the owner
/// actually asked for.
/// </summary>
public class WebsiteTemplateRequest
{
    public Guid Id { get; set; }

    public Guid BusinessId { get; set; }

    /// <summary>The business member who submitted the request. Recorded for audit and as the recipient of the build-started notification; authorization is by business membership, not this field.</summary>
    public Guid RequestedByUserId { get; set; }

    public Guid WebsiteTemplateId { get; set; }

    /// <summary>What the owner wants changed from the stock template - colors, sections, layout, business info, etc.</summary>
    public string CustomizationNotes { get; set; } = string.Empty;

    public WebsiteTemplateRequestStatus Status { get; set; } = WebsiteTemplateRequestStatus.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Set when a SuperAdmin marks the build as started. Null while Pending.</summary>
    public DateTime? BuildStartedAt { get; set; }

    /// <summary>Set once, when the request is closed. Null while open.</summary>
    public DateTime? ClosedAt { get; set; }

    /// <summary>Which SuperAdmin closed the request. Null while open.</summary>
    public Guid? ClosedByUserId { get; set; }

    /// <summary>The deployed site's URL. Null until the request is closed.</summary>
    public string? FinalWebsiteUrl { get; set; }

    // Navigation properties

    public Business Business { get; set; } = null!;

    public WebsiteTemplate WebsiteTemplate { get; set; } = null!;
}
