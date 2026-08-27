namespace MerchForge.api.Enums;

/// <summary>
/// Query-only filter for the products list — not persisted. Buckets are mutually
/// exclusive: Untracked = null StockQuantity, OutOfStock = 0, LowStock = 0 &lt;
/// quantity &lt;= the business's LowStockThreshold, InStock = above the threshold.
/// Tracked is the three non-null buckets combined.
/// </summary>
public enum ProductStockStatus
{
    All,
    Tracked,
    Untracked,
    InStock,
    LowStock,
    OutOfStock,
}
