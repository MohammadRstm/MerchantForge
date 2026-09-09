using FluentValidation;
using Hangfire;
using MerchForge.api.DTOs.Contact;
using MerchForge.api.Jobs.Email;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace MerchForge.api.Controllers
{
    /// <summary>
    /// The public contact form.
    ///
    /// This exists because a business owner cannot sign themselves up - accounts
    /// are created by completing an emailed invitation from a SuperAdmin - so
    /// somebody who wants one has no way to ask for it. This is that way.
    ///
    /// It is also the only anonymous endpoint in the application that causes an
    /// email to be sent, which makes it the one worth being careful with: it is
    /// rate limited per IP, every field is validated and length-capped before it
    /// reaches a mail body, and nothing is persisted.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [AllowAnonymous]
    [EnableRateLimiting("contact")]
    public class ContactController : ControllerBase
    {
        private readonly IBackgroundJobClient _backgroundJobClient;
        private readonly IValidator<ContactEnquiryRequest> _validator;

        public ContactController(
            IBackgroundJobClient backgroundJobClient,
            IValidator<ContactEnquiryRequest> validator)
        {
            _backgroundJobClient = backgroundJobClient;
            _validator = validator;
        }

        [HttpPost]
        public async Task<IActionResult> Submit(
            [FromBody] ContactEnquiryRequest request,
            CancellationToken cancellationToken)
        {
            await _validator.ValidateAndThrowAsync(request, cancellationToken);

            // Trimmed here rather than in the validator so the length rules judge
            // what a person actually typed, and so no leading whitespace reaches a
            // subject line or a Reply-To header.
            _backgroundJobClient.Enqueue<SendContactEnquiryJob>(job => job.ExecuteAsync(
                request.Name.Trim(),
                request.Email.Trim(),
                request.Subject.Trim(),
                request.Message.Trim()));

            // Accepted, not Ok: at this point the enquiry is queued and not yet
            // delivered, and saying so is the honest status. The visitor is told
            // it has been received either way - the delivery is ours to worry
            // about, not theirs.
            return Accepted();
        }
    }
}
