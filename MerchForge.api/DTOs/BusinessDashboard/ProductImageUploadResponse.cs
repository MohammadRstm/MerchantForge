namespace MerchForge.api.DTOs.BusinessDashboard;

public class ProductImageUploadResponse
{
    /// <summary>
    /// Relative URL of the stored image, to be sent back when saving the product.
    /// Relative rather than absolute so it stays correct across environments.
    /// </summary>
    public string ImageUrl { get; set; } = string.Empty;
}
