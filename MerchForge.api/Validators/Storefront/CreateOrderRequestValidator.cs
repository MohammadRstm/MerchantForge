using FluentValidation;
using MerchForge.api.DTOs.Storefront;

namespace MerchForge.api.Validators.Storefront;

public class CreateOrderRequestValidator : AbstractValidator<CreateOrderRequest>
{
    public CreateOrderRequestValidator()
    {
        RuleFor(x => x.CustomerName)
            .NotEmpty()
            .MaximumLength(255);

        RuleFor(x => x.CustomerEmail)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(255);

        RuleFor(x => x.CustomerPhone)
            .MaximumLength(50);

        RuleFor(x => x.ShippingAddressLine1)
            .NotEmpty()
            .MaximumLength(255);

        RuleFor(x => x.ShippingAddressLine2)
            .MaximumLength(255);

        RuleFor(x => x.ShippingCity)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.ShippingState)
            .MaximumLength(100);

        RuleFor(x => x.ShippingPostalCode)
            .NotEmpty()
            .MaximumLength(20);

        RuleFor(x => x.ShippingCountry)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.CustomerNotes)
            .MaximumLength(1000);

        RuleFor(x => x.Items)
            .NotEmpty()
            .WithMessage("An order needs at least one item.")
            .Must(items => items.Count <= 100)
            .WithMessage("An order can have at most 100 line items.");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductId)
                .NotEmpty();

            item.RuleFor(i => i.Quantity)
                .GreaterThan(0)
                .WithMessage("Quantity must be at least 1.")
                .LessThanOrEqualTo(1000)
                .WithMessage("Quantity can't exceed 1000 per item.");
        });

        // Product existence/stock/price are all checked in the service, which needs a
        // database round trip this validator has no access to.
    }
}
