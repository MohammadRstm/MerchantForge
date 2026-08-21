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

        // Mirrors the database CHECK constraint (CK_products_CompareAtPrice_GreaterThanPrice)
        // so a bad value is rejected with a clear message here rather than a raw
        // constraint-violation error from the database.
        RuleFor(x => x.CompareAtPrice)
            .GreaterThan(x => x.Price)
            .WithMessage("'CompareAtPrice' must be greater than 'Price'.")
            .LessThan(100_000_000m)
            .WithMessage("'CompareAtPrice' must be less than 100,000,000.")
            .When(x => x.CompareAtPrice.HasValue);

        RuleFor(x => x.CategoryId)
            .NotEmpty()
            .WithMessage("Select a category.");

        RuleFor(x => x.Images)
            .NotEmpty()
            .WithMessage("At least one product image is required.")
            .Must(images => images.Count <= 5)
            .WithMessage("A product can have at most 5 images.")
            .Must(images => images.Count(i => i.IsMain) == 1)
            .WithMessage("Exactly one image must be marked as the main image.");

        RuleForEach(x => x.Images).ChildRules(image =>
        {
            image.RuleFor(i => i.Url)
                .NotEmpty()
                .WithMessage("Each image needs a URL — upload it first via the image-upload endpoint.")
                .MaximumLength(500);

            image.RuleFor(i => i.AltText)
                .MaximumLength(255);
        });

        RuleFor(x => x.Sku)
            .MaximumLength(100);

        // Mirrors the database CHECK constraint (CK_products_StockQuantity_NonNegative).
        RuleFor(x => x.StockQuantity)
            .GreaterThanOrEqualTo(0)
            .When(x => x.StockQuantity.HasValue);

        RuleForEach(x => x.Tags)
            .NotEmpty()
            .WithMessage("A tag can't be blank.")
            .MaximumLength(50);

        RuleFor(x => x.Tags)
            .Must(tags => tags is null || tags.Count <= 20)
            .WithMessage("A product can have at most 20 tags.");

        // Whether each metadata key is enabled for this business, and whether its
        // value matches the declared type, is checked in the service — that needs the
        // business's metadata shape, which this validator has no access to.
    }
}
