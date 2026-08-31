using FluentValidation;
using MerchForge.api.DTOs.Auth;

namespace MerchForge.api.Validators.Auth
{
    public class CompleteBusinessOwnerRegistrationRequestValidator : AbstractValidator<CompleteBusinessOwnerRegistrationRequest>
    {
        public CompleteBusinessOwnerRegistrationRequestValidator()
        {
            RuleFor(x => x.FirstName)
                .NotEmpty()
                .MaximumLength(255);

            RuleFor(x => x.LastName)
                .NotEmpty()
                .MaximumLength(255);

            RuleFor(x => x.BusinessName)
                .NotEmpty()
                .MaximumLength(255);

            RuleFor(x => x.Email)
                .NotEmpty()
                .EmailAddress()
                .MaximumLength(255);

            RuleFor(x => x.Password)
                .NotEmpty()
                .MinimumLength(8);

            RuleFor(x => x.InvitationToken)
                .NotEmpty()
                .ToString();

            RuleFor(x => x.BusinessDomainId)
                .NotEmpty()
                .WithMessage("Select a business domain.");

            // Existence/active-state of the domain itself is checked in the service
            // layer (consistent with how a nonexistent businessId/productId is
            // handled elsewhere) — this validator only checks shape, matching every
            // other validator in the codebase staying synchronous and DB-free.
            RuleForEach(x => x.NewCategoryNames)
                .NotEmpty()
                .WithMessage("Category names can't be blank.")
                .MaximumLength(100);

            RuleFor(x => x.NewCategoryNames)
                .Must(names => names.Count <= 20)
                .WithMessage("Add at most 20 custom categories.");

            // Whether each key actually exists in the chosen domain is checked in the
            // service layer, which is where the domain catalogue is read.
            RuleForEach(x => x.SelectedProductAttributeKeys)
                .NotEmpty()
                .MaximumLength(100);
        }
    }
}
