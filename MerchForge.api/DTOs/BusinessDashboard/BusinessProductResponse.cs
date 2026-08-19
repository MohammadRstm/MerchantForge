namespace MerchForge.api.DTOs.BusinessDashboard;

public class BusinessProductResponse
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public string? ImageUrl { get; set; }

    public DateTime CreatedAt { get; set; }
}
