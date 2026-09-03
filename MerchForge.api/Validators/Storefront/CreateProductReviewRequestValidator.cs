using FluentValidation;
using MerchForge.api.DTOs.Storefront;

namespace MerchForge.api.Validators.Storefront;

public class CreateProductReviewRequestValidator : AbstractValidator<CreateProductReviewRequest>
{
    public CreateProductReviewRequestValidator()
    {
        // The rating is the review. A comment on its own is not submittable, which is
        // why there is no NotEmpty on Comment and an unconditional range check here —
        // an omitted Rating deserialises to 0 and is rejected by this same rule.
        RuleFor(x => x.Rating)
            .InclusiveBetween(1, 5);

        // Matches ProductReview.Comment's column length. Optional, so no NotEmpty:
        // rating-only reviews are expected to be the common case.
        RuleFor(x => x.Comment)
            .MaximumLength(2000);
    }
}
