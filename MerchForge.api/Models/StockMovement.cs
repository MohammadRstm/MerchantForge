namespace MerchForge.api.Models;

/// <summary>
/// One entry in a product's stock history: an Add (positive Amount) or a Remove
/// (negative Amount), never zero. Stock is money-adjacent state, so every change is
/// recorded here rather than only ever mutating Product.StockQuantity in place —
/// same reasoning as FeatureCreditTransaction/BalanceAfter for credit balances.
/// </summary>
public class StockMovement
{
    public Guid Id { get; set; }

    public Guid ProductId { get; set; }

    /// <summary>Denormalized from Product.BusinessId so business-scoped queries (the
    /// recent-activity list, the inventory summary) don't need to join through
    /// Product every time.</summary>
    public Guid BusinessId { get; set; }

    public int Amount { get; set; }

    /// <summary>Product.StockQuantity immediately after this movement was applied.</summary>
    public int BalanceAfter { get; set; }

    public string? Reason { get; set; }

    /// <summary>Plain id, no enforced FK — same precedent as
    /// WebsiteTemplateRequest.RequestedByUserId.</summary>
    public Guid CreatedByUserId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
