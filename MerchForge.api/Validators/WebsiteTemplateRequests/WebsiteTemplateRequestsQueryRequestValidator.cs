using FluentValidation;
using MerchForge.api.DTOs.WebsiteTemplateRequests;
using MerchForge.api.Validators.Common;

namespace MerchForge.api.Validators.WebsiteTemplateRequests;

public class WebsiteTemplateRequestsQueryRequestValidator : PagedQueryValidator<WebsiteTemplateRequestsQueryRequest>
{
    public WebsiteTemplateRequestsQueryRequestValidator()
    {
        RuleFor(x => x.Status)
            .IsInEnum()
            .When(x => x.Status is not null);
    }
}
