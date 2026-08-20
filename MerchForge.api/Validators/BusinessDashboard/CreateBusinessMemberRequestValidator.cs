using FluentValidation;
using MerchForge.api.DTOs.BusinessDashboard;
using MerchForge.api.Enums;

namespace MerchForge.api.Validators.BusinessDashboard;

public class CreateBusinessMemberRequestValidator : AbstractValidator<CreateBusinessMemberRequest>
{
    public CreateBusinessMemberRequestValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.LastName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(255);

        // An allow-list rather than "not Owner": a BusinessRole added later should
        // have to be opted in here, not silently become assignable by every owner.
        RuleFor(x => x.Role)
            .Must(role => role is BusinessRole.Admin or BusinessRole.Member)
            .WithMessage("Choose either Admin or Member.");
    }
}
