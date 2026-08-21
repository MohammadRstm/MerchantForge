namespace MerchForge.api.DTOs.BusinessDashboard;

/// <summary>
/// Drives the dashboard's "choose a website template" section. The frontend shows
/// the choose-a-template button/grid exactly when <see cref="Chosen"/> is null --
/// there is deliberately no separate flag for that, so the two can never disagree.
/// </summary>
public class BusinessWebsiteTemplateStatusResponse
{
    public Guid BusinessDomainId { get; set; }

    public string DomainName { get; set; } = string.Empty;

    public ChosenWebsiteTemplateResponse? Chosen { get; set; }

    /// <summary>Active templates in this business's domain. Empty once a template is already chosen -- the frontend has no use for them at that point.</summary>
    public List<WebsiteTemplateOptionResponse> Available { get; set; } = [];
}
