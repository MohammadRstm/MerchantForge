using MerchForge.api.DTOs.Common;
using MerchForge.api.Enums;

namespace MerchForge.api.DTOs.BusinessDashboard;

public class ProductsQueryRequest : PagedQuery
{
    public string? Search { get; set; }

    public string? Category { get; set; }

    public ProductStockStatus StockStatus { get; set; } = ProductStockStatus.All;

    public ProductSortField SortBy { get; set; } = ProductSortField.CreatedAt;

    public bool SortDescending { get; set; } = true;
}
