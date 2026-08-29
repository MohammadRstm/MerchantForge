namespace MerchForge.api.DTOs.BusinessDashboard;

public class OrderNoteResponse
{
    public Guid Id { get; set; }

    public string Content { get; set; } = string.Empty;

    public string CreatedByUserName { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}
