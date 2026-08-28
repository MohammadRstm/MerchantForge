using FluentValidation;
using MerchForge.api.Authorization;
using MerchForge.api.DTOs.CustomerAuth;
using MerchForge.api.Services.CustomerAuth.interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MerchForge.api.Controllers
{
    /// <summary>
    /// Called by every storefront once a customer is authenticated, with the
    /// short-lived customer access token as a Bearer header — not a cookie, so this is
    /// safe under the anonymous, AllowAnyOrigin "Storefront" CORS policy the same way
    /// every other Bearer-authenticated cross-origin call is: a browser only attaches
    /// an Authorization header when JS explicitly sets it, unlike a cookie, which it
    /// attaches automatically to any matching-origin request regardless of the caller.
    /// </summary>
    [Route("api/customer")]
    [ApiController]
    [Authorize(Policy = AuthorizationPolicies.Customer)]
    [EnableCors("Storefront")]
    public class CustomerProfileController : ControllerBase
    {
        private readonly ICustomerAuthService _customerAuthService;
        private readonly IValidator<UpdateCustomerProfileRequest> _updateProfileValidator;

        public CustomerProfileController(
            ICustomerAuthService customerAuthService,
            IValidator<UpdateCustomerProfileRequest> updateProfileValidator)
        {
            _customerAuthService = customerAuthService;
            _updateProfileValidator = updateProfileValidator;
        }

        [HttpGet("profile")]
        public async Task<ActionResult<CustomerProfileResponse>> GetProfile(
            CancellationToken cancellationToken)
        {
            if (!TryGetCustomerId(out var customerId))
            {
                return Unauthorized();
            }

            var response = await _customerAuthService.GetProfileAsync(customerId, cancellationToken);

            return Ok(response);
        }

        [HttpPut("profile")]
        public async Task<ActionResult<CustomerProfileResponse>> UpdateProfile(
            [FromBody] UpdateCustomerProfileRequest request,
            CancellationToken cancellationToken)
        {
            await _updateProfileValidator.ValidateAndThrowAsync(request, cancellationToken);

            if (!TryGetCustomerId(out var customerId))
            {
                return Unauthorized();
            }

            var response = await _customerAuthService.UpdateProfileAsync(customerId, request, cancellationToken);

            return Ok(response);
        }

        private bool TryGetCustomerId(out Guid customerId)
        {
            var id = User.FindFirstValue(ClaimTypes.NameIdentifier);

            return Guid.TryParse(id, out customerId);
        }
    }
}
