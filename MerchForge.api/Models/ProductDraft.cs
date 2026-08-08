namespace MerchForge.api.Models;

public class ProductDraft
{
    public Guid Id { get; set; }

    public Guid BusinessId { get; set; }

    public string Provider { get; set; } = string.Empty;

    public string ConversationId { get; set; } = string.Empty;

    public string? OriginalImagePath { get; set; }

    public string? ProcessedImagePath { get; set; }

    public string? PendingDetails { get; set; }

    public string? StructuredData { get; set; }

    public string Status { get; set; } = "WaitingForDetails";

    public DateTime LastMessageAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    // Navigation

    public Business Business { get; set; } = null!;
}