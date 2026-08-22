namespace MerchForge.api.DTOs.ImageEditing;

public class ImageEditJobResponse
{
    public Guid Id { get; set; }

    /// <summary>Completed | Failed.</summary>
    public string Status { get; set; } = string.Empty;

    public string Prompt { get; set; } = string.Empty;

    public List<string> InputImageUrls { get; set; } = [];

    /// <summary>Null on a Failed job.</summary>
    public string? OutputImageUrl { get; set; }

    /// <summary>Set only on a Failed job.</summary>
    public string? ErrorMessage { get; set; }

    public DateTime CreatedAt { get; set; }
}
