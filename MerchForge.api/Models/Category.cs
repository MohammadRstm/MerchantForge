namespace MerchForge.api.Models;

/// <summary>
/// A product category belonging to a single <see cref="BusinessDomain"/>. Categories
/// are platform-level (seeded and eventually SuperAdmin-managed), not per-business,
/// so every Fashion business shares the same "Shoes" category.
///
/// Because a category belongs to exactly one domain, and a business belongs to
/// exactly one domain, "a Fashion business must not use a Restaurant category" is
/// expressible as: product.Category.BusinessDomainId == product.Business.BusinessDomainId.
/// MariaDB cannot express that as a CHECK constraint (no subqueries), so it is
/// enforced in the application layer wherever products are written.
/// </summary>
public class Category
{
    public Guid Id { get; set; }

    public Guid BusinessDomainId { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Unique within a domain. Lets storefronts build stable category URLs
    /// (/category/shoes) that survive a display-name change.
    /// </summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>
    /// Storefront navigation ordering. The SDK exposes it as data; how (or whether)
    /// a storefront honours it is a UI decision.
    /// </summary>
    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties

    public BusinessDomain BusinessDomain { get; set; } = null!;

    public ICollection<Product> Products { get; set; }
        = new List<Product>();
}
