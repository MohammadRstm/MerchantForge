using FluentValidation;
using MerchForge.api.DTOs.BusinessDashboard;

namespace MerchForge.api.Validators.BusinessDashboard;

public class SaveProductRequestValidator : AbstractValidator<SaveProductRequest>
{
    public SaveProductRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(255);

        RuleFor(x => x.Description)
            .NotEmpty();

        // Non-negative rather than strictly positive: a free item is legitimate.
        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0)
            // products.Price is decimal(10,2), so anything larger would be silently
            // truncated or throw at the database.
            .LessThan(100_000_000m)
            .WithMessage("'Price' must be less than 100,000,000.");

        RuleFor(x => x.CategoryId)
            .NotEmpty()
            .WithMessage("Select a category.");

        RuleFor(x => x.ImageUrl)
            .MaximumLength(500);

        // Whether each metadata key is enabled for this business, and whether its
        // value matches the declared type, is checked in the service — that needs the
        // business's metadata shape, which this validator has no access to.
    }
}
