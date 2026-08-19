namespace MerchForge.api.DTOs.ProductAi;

public class SendDraftMessageRequest
{
    public string Message { get; set; } = string.Empty;
}

public class ImageApprovalRequest
{
    /// <summary>
    /// True keeps the edited image, false discards it and restores the original.
    /// Explicit rather than two endpoints so the client sends one shape either way.
    /// </summary>
    public bool Approved { get; set; }
}
