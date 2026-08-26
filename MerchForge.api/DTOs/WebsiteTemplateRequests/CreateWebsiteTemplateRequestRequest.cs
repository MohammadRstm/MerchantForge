namespace MerchForge.api.DTOs.WebsiteTemplateRequests;

public class CreateWebsiteTemplateRequestRequest
{
    public Guid WebsiteTemplateId { get; set; }

    public string CustomizationNotes { get; set; } = string.Empty;
}
