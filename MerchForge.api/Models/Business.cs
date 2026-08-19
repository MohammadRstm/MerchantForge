namespace MerchForge.api.Models;

public class Business
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public Guid OwnerUserId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties

    public User Owner { get; set; } = null!;

    public ICollection<BusinessUser> Members { get; set; }
        = new List<BusinessUser>();

    public ICollection<Product> Products { get; set; }
        = new List<Product>();

    public ICollection<ProductDraft> ProductDrafts { get; set; }
        = new List<ProductDraft>();
}