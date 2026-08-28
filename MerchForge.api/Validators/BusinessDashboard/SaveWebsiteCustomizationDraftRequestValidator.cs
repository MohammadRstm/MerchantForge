using FluentValidation;
using MerchForge.api.DTOs.BusinessDashboard;
using MerchForge.api.DTOs.Common;
using MerchForge.api.Services.Common;

namespace MerchForge.api.Validators.BusinessDashboard;

public class SaveWebsiteCustomizationDraftRequestValidator : AbstractValidator<SaveWebsiteCustomizationDraftRequest>
{
    public SaveWebsiteCustomizationDraftRequestValidator()
    {
        RuleFor(x => x.Tagline).MaximumLength(150);
        RuleFor(x => x.Description).MaximumLength(1000);
        RuleFor(x => x.LogoUrl).MaximumLength(500);
        RuleFor(x => x.FaviconUrl).MaximumLength(500);

        RuleFor(x => x.ContactEmail)
            .EmailAddress()
            .MaximumLength(255)
            .When(x => !string.IsNullOrWhiteSpace(x.ContactEmail));

        RuleFor(x => x.ContactPhone).MaximumLength(50);

        // Digits/E.164 only, never a full URL -- the wa.me/<number> link is built by
        // the SDK, never stored as a link.
        RuleFor(x => x.WhatsAppNumber)
            .Matches(@"^\+?[0-9]{7,15}$")
            .WithMessage("WhatsApp number must contain only digits (optionally with a leading +).")
            .When(x => !string.IsNullOrWhiteSpace(x.WhatsAppNumber));

        RuleFor(x => x.AddressLine1).MaximumLength(255);
        RuleFor(x => x.AddressLine2).MaximumLength(255);
        RuleFor(x => x.City).MaximumLength(100);
        RuleFor(x => x.State).MaximumLength(100);
        RuleFor(x => x.PostalCode).MaximumLength(20);
        RuleFor(x => x.Country).MaximumLength(100);

        RuleFor(x => x.PrimaryColor)
            .Matches("^#[0-9A-Fa-f]{6}$")
            .WithMessage("Primary color must be a hex value like #1A1A1A.")
            .When(x => !string.IsNullOrWhiteSpace(x.PrimaryColor));

        RuleFor(x => x.SocialLinks!)
            .SetValidator(new SocialLinksDtoValidator())
            .When(x => x.SocialLinks is not null);

        RuleFor(x => x.BusinessHours!)
            .SetValidator(new BusinessHoursDtoValidator())
            .When(x => x.BusinessHours is not null);

        // TemplateFields is validated against the current template's catalogue by
        // WebsiteCustomizationValuesBuilder in the service layer, not here -- same
        // reasoning Product.Metadata's validation lives in ProductMetadataBuilder
        // rather than a validator: the set of allowed keys/types isn't static.
    }
}

public class SocialLinksDtoValidator : AbstractValidator<SocialLinksDto>
{
    public SocialLinksDtoValidator()
    {
        RuleFor(x => x.Facebook).Must(SafeUrlValidator.IsSafe).WithMessage("Enter a valid link.").When(x => !string.IsNullOrWhiteSpace(x.Facebook));
        RuleFor(x => x.Instagram).Must(SafeUrlValidator.IsSafe).WithMessage("Enter a valid link.").When(x => !string.IsNullOrWhiteSpace(x.Instagram));
        RuleFor(x => x.Twitter).Must(SafeUrlValidator.IsSafe).WithMessage("Enter a valid link.").When(x => !string.IsNullOrWhiteSpace(x.Twitter));
        RuleFor(x => x.TikTok).Must(SafeUrlValidator.IsSafe).WithMessage("Enter a valid link.").When(x => !string.IsNullOrWhiteSpace(x.TikTok));
        RuleFor(x => x.YouTube).Must(SafeUrlValidator.IsSafe).WithMessage("Enter a valid link.").When(x => !string.IsNullOrWhiteSpace(x.YouTube));
        RuleFor(x => x.LinkedIn).Must(SafeUrlValidator.IsSafe).WithMessage("Enter a valid link.").When(x => !string.IsNullOrWhiteSpace(x.LinkedIn));
    }
}

public class BusinessHoursDtoValidator : AbstractValidator<BusinessHoursDto>
{
    public BusinessHoursDtoValidator()
    {
        RuleFor(x => x.Monday!).SetValidator(new BusinessHoursDayDtoValidator()).When(x => x.Monday is not null);
        RuleFor(x => x.Tuesday!).SetValidator(new BusinessHoursDayDtoValidator()).When(x => x.Tuesday is not null);
        RuleFor(x => x.Wednesday!).SetValidator(new BusinessHoursDayDtoValidator()).When(x => x.Wednesday is not null);
        RuleFor(x => x.Thursday!).SetValidator(new BusinessHoursDayDtoValidator()).When(x => x.Thursday is not null);
        RuleFor(x => x.Friday!).SetValidator(new BusinessHoursDayDtoValidator()).When(x => x.Friday is not null);
        RuleFor(x => x.Saturday!).SetValidator(new BusinessHoursDayDtoValidator()).When(x => x.Saturday is not null);
        RuleFor(x => x.Sunday!).SetValidator(new BusinessHoursDayDtoValidator()).When(x => x.Sunday is not null);
    }
}

public class BusinessHoursDayDtoValidator : AbstractValidator<BusinessHoursDayDto>
{
    private static readonly System.Text.RegularExpressions.Regex TimeFormat =
        new(@"^([01]\d|2[0-3]):[0-5]\d$", System.Text.RegularExpressions.RegexOptions.Compiled);

    public BusinessHoursDayDtoValidator()
    {
        RuleFor(x => x.Open)
            .NotEmpty()
            .Must(v => TimeFormat.IsMatch(v!))
            .WithMessage("Open must be a 24-hour time like 09:00.")
            .When(x => !x.Closed);

        RuleFor(x => x.Close)
            .NotEmpty()
            .Must(v => TimeFormat.IsMatch(v!))
            .WithMessage("Close must be a 24-hour time like 17:00.")
            .When(x => !x.Closed);

        RuleFor(x => x)
            .Must(x => string.CompareOrdinal(x.Open, x.Close) < 0)
            .WithMessage("Open must be before Close.")
            .When(x => !x.Closed && !string.IsNullOrEmpty(x.Open) && !string.IsNullOrEmpty(x.Close) && TimeFormat.IsMatch(x.Open) && TimeFormat.IsMatch(x.Close));
    }
}
