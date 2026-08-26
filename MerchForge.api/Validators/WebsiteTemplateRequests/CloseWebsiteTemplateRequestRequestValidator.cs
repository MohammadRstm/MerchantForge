using FluentValidation;
using MerchForge.api.DTOs.WebsiteTemplateRequests;

namespace MerchForge.api.Validators.WebsiteTemplateRequests;

public class CloseWebsiteTemplateRequestRequestValidator : AbstractValidator<CloseWebsiteTemplateRequestRequest>
{
    public CloseWebsiteTemplateRequestRequestValidator()
    {
        RuleFor(x => x.FinalWebsiteUrl)
            .NotEmpty()
            .WithMessage("Enter the final website URL.")
            .Must(BeAValidAbsoluteUrl)
            .WithMessage("Enter a valid URL, including https://.");
    }

    private static bool BeAValidAbsoluteUrl(string url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var parsed)
        && (parsed.Scheme == Uri.UriSchemeHttp || parsed.Scheme == Uri.UriSchemeHttps);
}
