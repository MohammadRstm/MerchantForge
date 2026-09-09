using FluentValidation;
using MerchForge.api.DTOs.Contact;

namespace MerchForge.api.Validators.Contact
{
    /// <summary>
    /// The only anonymous endpoint in the application that causes an email to be
    /// sent, so this is the boundary that decides what a stranger can put in
    /// front of an administrator.
    ///
    /// Every field is length-capped rather than merely required. Uncapped free
    /// text on a public form is how a contact box becomes a delivery mechanism -
    /// for a mail body megabytes long, or for a wall of content aimed at whoever
    /// opens it. The caps are generous enough for a real enquiry and small
    /// enough that no single message is worth sending.
    /// </summary>
    public class ContactEnquiryRequestValidator : AbstractValidator<ContactEnquiryRequest>
    {
        public ContactEnquiryRequestValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .MaximumLength(120);

            RuleFor(x => x.Email)
                .NotEmpty()
                // The default AspNetCoreCompatible mode only checks for a single
                // '@' with non-empty parts on either side - it accepts something
                // like "person@tld", which has no real domain to reply to. This
                // is the reply-to address at the other end of an anonymous form,
                // so Net4xRegex's stricter check (a domain with an actual dotted
                // structure) is worth the extra strictness here.
                .EmailAddress(FluentValidation.Validators.EmailValidationMode.Net4xRegex)
                .MaximumLength(254);

            RuleFor(x => x.Subject)
                .NotEmpty()
                .MaximumLength(150);

            RuleFor(x => x.Message)
                .NotEmpty()
                // Long enough that "call me" is rejected and the sender is
                // nudged into saying something an admin can actually act on.
                .MinimumLength(20)
                .MaximumLength(4000);
        }
    }
}
