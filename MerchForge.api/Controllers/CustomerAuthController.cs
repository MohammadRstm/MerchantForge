using FluentValidation;
using MerchForge.api.Configurations;
using MerchForge.api.DTOs.CustomerAuth;
using MerchForge.api.Services.CustomerAuth.interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace MerchForge.api.Controllers
{
    /// <summary>
    /// Signup/login/refresh/logout/silent are only ever called from MerchForgeClient's
    /// own origin (the platform) — a storefront never calls them directly, it only ever
    /// loads /customer/silent as a page navigation (iframe/popup), not a fetch. Exchange
    /// is the one action genuinely called cross-origin, from the storefront itself, and
    /// overrides CORS back to the anonymous, credential-free "Storefront" policy below.
    /// The customerRefreshToken cookie set here never leaves this controller's own
    /// responses — Exchange never reads or sets it.
    /// </summary>
    [Route("api/CustomerAuth")]
    [ApiController]
    [AllowAnonymous]
    [EnableCors("CustomerPlatform")]
    public class CustomerAuthController : ControllerBase
    {
        private readonly ICustomerAuthService _customerAuthService;
        private readonly IValidator<CustomerSignupRequest> _signupValidator;
        private readonly IValidator<CustomerLoginRequest> _loginValidator;
        private readonly IValidator<CustomerExchangeRequest> _exchangeValidator;
        private readonly CustomerRefreshTokenOptions _refreshTokenOptions;

        public CustomerAuthController(
            ICustomerAuthService customerAuthService,
            IValidator<CustomerSignupRequest> signupValidator,
            IValidator<CustomerLoginRequest> loginValidator,
            IValidator<CustomerExchangeRequest> exchangeValidator,
            IOptions<CustomerRefreshTokenOptions> refreshTokenOptions)
        {
            _customerAuthService = customerAuthService;
            _signupValidator = signupValidator;
            _loginValidator = loginValidator;
            _exchangeValidator = exchangeValidator;
            _refreshTokenOptions = refreshTokenOptions.Value;
        }

        [HttpPost("signup")]
        public async Task<ActionResult<CustomerSessionResponse>> Signup(
            [FromBody] CustomerSignupRequest request,
            CancellationToken cancellationToken)
        {
            await _signupValidator.ValidateAndThrowAsync(request, cancellationToken);

            var (response, refreshToken) = await _customerAuthService.SignupAsync(request, cancellationToken);

            SetRefreshTokenCookie(refreshToken);

            return Ok(response);
        }

        [HttpPost("login")]
        public async Task<ActionResult<CustomerSessionResponse>> Login(
            [FromBody] CustomerLoginRequest request,
            CancellationToken cancellationToken)
        {
            await _loginValidator.ValidateAndThrowAsync(request, cancellationToken);

            var (response, refreshToken) = await _customerAuthService.LoginAsync(request, cancellationToken);

            SetRefreshTokenCookie(refreshToken);

            return Ok(response);
        }

        [HttpPost("refresh")]
        public async Task<ActionResult<CustomerSessionResponse>> Refresh(
            CancellationToken cancellationToken)
        {
            return await RefreshFromCookie(cancellationToken);
        }

        /// <summary>
        /// Hit exclusively by the platform's own hidden /customer/silent page (loaded
        /// in a storefront's iframe or popup). Behaviorally identical to Refresh — kept
        /// as its own route so the SDK's silent-renewal chain has a single,
        /// purpose-built endpoint rather than one whose name implies an explicit user
        /// action.
        /// </summary>
        [HttpPost("silent")]
        public async Task<ActionResult<CustomerSessionResponse>> Silent(
            CancellationToken cancellationToken)
        {
            return await RefreshFromCookie(cancellationToken);
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout(
            CancellationToken cancellationToken)
        {
            if (Request.Cookies.TryGetValue(_refreshTokenOptions.CookieName, out var refreshToken) &&
                !string.IsNullOrEmpty(refreshToken))
            {
                await _customerAuthService.LogoutAsync(refreshToken, cancellationToken);
            }

            ClearRefreshTokenCookie();

            return NoContent();
        }

        /// <summary>
        /// Called from the storefront's own origin — overrides the controller-level
        /// CustomerPlatform CORS policy back to the anonymous, credential-free
        /// Storefront one, matching every other public storefront endpoint.
        /// </summary>
        [EnableCors("Storefront")]
        [HttpPost("exchange")]
        public async Task<ActionResult<CustomerSessionResponse>> Exchange(
            [FromBody] CustomerExchangeRequest request,
            CancellationToken cancellationToken)
        {
            await _exchangeValidator.ValidateAndThrowAsync(request, cancellationToken);

            var response = await _customerAuthService.RedeemExchangeCodeAsync(
                request.Code,
                request.ReturnUrl,
                cancellationToken);

            return Ok(response);
        }

        private async Task<ActionResult<CustomerSessionResponse>> RefreshFromCookie(
            CancellationToken cancellationToken)
        {
            if (!Request.Cookies.TryGetValue(_refreshTokenOptions.CookieName, out var refreshToken) ||
                string.IsNullOrEmpty(refreshToken))
            {
                return Unauthorized();
            }

            var (response, newRefreshToken) = await _customerAuthService.RefreshAsync(
                refreshToken,
                cancellationToken);

            SetRefreshTokenCookie(newRefreshToken);

            return Ok(response);
        }

        private void SetRefreshTokenCookie(string refreshToken)
        {
            Response.Cookies.Append(
                _refreshTokenOptions.CookieName,
                refreshToken,
                BuildCookieOptions(DateTimeOffset.UtcNow.AddDays(_refreshTokenOptions.ExpirationDays)));
        }

        private void ClearRefreshTokenCookie()
        {
            Response.Cookies.Delete(
                _refreshTokenOptions.CookieName,
                BuildCookieOptions(null));
        }

        private CookieOptions BuildCookieOptions(DateTimeOffset? expires)
        {
            return new CookieOptions
            {
                HttpOnly = true,
                Secure = _refreshTokenOptions.Secure,
                SameSite = ParseSameSite(_refreshTokenOptions.SameSite),
                Path = _refreshTokenOptions.CookiePath,
                Expires = expires,
            };
        }

        private static SameSiteMode ParseSameSite(string value)
        {
            return Enum.TryParse<SameSiteMode>(value, ignoreCase: true, out var mode)
                ? mode
                : SameSiteMode.Lax;
        }
    }
}
