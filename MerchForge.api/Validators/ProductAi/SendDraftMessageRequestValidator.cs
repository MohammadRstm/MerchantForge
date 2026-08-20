using FluentValidation;
using MerchForge.api.DTOs.ProductAi;

namespace MerchForge.api.Validators.ProductAi;

public class SendDraftMessageRequestValidator : AbstractValidator<SendDraftMessageRequest>
{
    public SendDraftMessageRequestValidator()
    {
        RuleFor(x => x.Message)
            .NotEmpty()
            .WithMessage("Type a message first.")
            // Bounded before it reaches the provider: an unbounded message is billed
            // by the token and gains nothing over a long-but-sane one.
            .MaximumLength(2000);
    }
}
