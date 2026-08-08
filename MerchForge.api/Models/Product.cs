namespace MerchForge.api.Models;

public class Product
{
    public Guid Id { get; set; }

    public Guid BusinessId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public string Category { get; set; } = string.Empty;

    public string? ImageUrl { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    // Navigation property

    public Business Business { get; set; } = null!;
}