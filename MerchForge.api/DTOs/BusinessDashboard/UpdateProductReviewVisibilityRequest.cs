namespace MerchForge.api.DTOs.BusinessDashboard;

/// <summary>
/// Hides or unhides one review. A single endpoint taking the desired state rather
/// than separate hide/unhide routes, so re-sending the state a review is already in
/// is a harmless no-op instead of an error.
/// </summary>
public class UpdateProductReviewVisibilityRequest
{
    public bool IsHidden { get; set; }
}
