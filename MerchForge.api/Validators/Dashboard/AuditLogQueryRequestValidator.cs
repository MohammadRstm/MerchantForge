using FluentValidation;
using MerchForge.api.DTOs.Audit;
using MerchForge.api.Validators.Common;

namespace MerchForge.api.Validators.Dashboard;

public class AuditLogQueryRequestValidator : PagedQueryValidator<AuditLogQueryRequest>
{
    public AuditLogQueryRequestValidator()
    {
        RuleFor(x => x.Actor)
            .MaximumLength(255);

        RuleFor(x => x.EventType)
            .IsInEnum()
            .When(x => x.EventType.HasValue);

        RuleFor(x => x)
            .Must(x => !x.From.HasValue || !x.To.HasValue || x.From <= x.To)
            .WithMessage("From must be before or equal to To.");
    }
}
