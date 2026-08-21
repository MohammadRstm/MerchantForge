using System.Text.Json;
using MerchForge.api.Enums;

namespace MerchForge.api.Models;

/// <summary>
/// One request to edit a product photo with AI: the input images, the owner's
/// instruction, and the outcome. Unlike ProductDraft this is single-shot, not a
/// conversation - there is nothing to resume, so a job is written once, in its final
/// state, after the edit has already succeeded or failed. Kept for audit (what was
/// asked for, what came back) and as the reference a credit spend points at.
/// </summary>
public class ImageEditJob
{
    public Guid Id { get; set; }

    public Guid BusinessId { get; set; }

    /// <summary>Recorded for audit; authorization is by business membership, not this field.</summary>
    public Guid CreatedByUserId { get; set; }

    public string Prompt { get; set; } = string.Empty;

    /// <summary>
    /// The input images as stored (relative URLs), in the order they were sent to the
    /// model: { "urls": ["/uploads/...", ...] }. A JSON array rather than a related
    /// table - there is nothing about an input image worth querying independently of
    /// its job.
    /// </summary>
    public JsonDocument InputImageUrls { get; set; } = null!;

    /// <summary>Null until the edit succeeds; stays null on a Failed job.</summary>
    public string? OutputImageUrl { get; set; }

    public ImageEditJobStatus Status { get; set; }

    /// <summary>Set only on a Failed job, shown back to the owner.</summary>
    public string? ErrorMessage { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Business Business { get; set; } = null!;
}
