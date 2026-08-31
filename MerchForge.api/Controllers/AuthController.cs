using MerchForge.api.Configurations;
using MerchForge.api.DTOs.Auth;
using MerchForge.api.Services.Auth.interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using FluentValidation;
using MerchForge.api.Validators.Auth;
using Microsoft.Extensions.Options;

namespace MerchForge.api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IValidator<LoginRequest> _loginValidator;
        private readonly IValidator<CompleteBusinessOwnerRegistrationRequest> _completeBusinessOwnerRegistrationRequestValidator;
        private readonly IValidator<CompleteBusinessMemberRegistrationRequest> _completeBusinessMemberRegistrationRequestValidator;
        private readonly IValidator<RegisterSuperAdminRequest> _registerSuperAdminValidator;
        private readonly RefreshTokenOptions _refreshTokenOptions;

        public AuthController(
            IAuthService authService,
            IValidator<LoginRequest> loginValidator,
            IValidator<CompleteBusinessOwnerRegistrationRequest> completeBusinessOwnerRegistrationRequestValidator,
            IValidator<CompleteBusinessMemberRegistrationRequest> completeBusinessMemberRegistrationRequestValidator,
            IValidator<RegisterSuperAdminRequest> registerSuperAdminValidator,
            IOptions<RefreshTokenOptions> refreshTokenOptions)
        {
            _authService = authService;
            _loginValidator = loginValidator;
            _completeBusinessOwnerRegistrationRequestValidator = completeBusinessOwnerRegistrationRequestValidator;
            _completeBusinessMemberRegistrationRequestValidator = completeBusinessMemberRegistrationRequestValidator;
            _registerSuperAdminValidator = registerSuperAdminValidator;
            _refreshTokenOptions = refreshTokenOptions.Value;
        }

        [HttpPost("login")]
        [EnableRateLimiting("auth")]
        public async Task<ActionResult<LoginResponse>> Login(
            [FromBody] LoginRequest request,
            CancellationToken cancellationToken)
        {
            await _loginValidator.ValidateAndThrowAsync(request);

            var (response, refreshToken) = await _authService.LoginAsync(
                request,
                cancellationToken);

            SetRefreshTokenCookie(refreshToken);

            return Ok(response);
        }

        [HttpPost("register/superAdmin")]
        [EnableRateLimiting("auth")]
        public async Task<ActionResult<AuthResponse>> RegsiterSuperAdmin(
            [FromBody] RegisterSuperAdminRequest request,
            CancellationToken cancellationToken)
        {
            await _registerSuperAdminValidator.ValidateAndThrowAsync(request, cancellationToken);

            var (response, refreshToken) = await _authService.RegisterSuperAdmin(
                request,
                cancellationToken);

            SetRefreshTokenCookie(refreshToken);

            return Ok(response);
        }

        [HttpPost("refresh")]
        [EnableRateLimiting("auth")]
        public async Task<ActionResult<LoginResponse>> Refresh(
            CancellationToken cancellationToken)
        {
            if (!Request.Cookies.TryGetValue(_refreshTokenOptions.CookieName, out var refreshToken) ||
                string.IsNullOrEmpty(refreshToken))
            {
                return Unauthorized();
            }

            var (response, newRefreshToken) = await _authService.RefreshAsync(
                refreshToken,
                cancellationToken);

            SetRefreshTokenCookie(newRefreshToken);

            return Ok(response);
        }

        [HttpPost("businessOwner/registration")]
        public async Task<ActionResult<RegistrationResponse>> CompleteBusinessOwnerRegistration(
            [FromBody] CompleteBusinessOwnerRegistrationRequest request,
            CancellationToken cancellationToken)
        {
            await _completeBusinessOwnerRegistrationRequestValidator.ValidateAndThrowAsync(request);

            var (response, refreshToken) = await _authService.CompleteBusinessOwnerRegistration(request);

            SetRefreshTokenCookie(refreshToken);

            return Ok(response);
        }

        /// <summary>
        /// A team member setting their own password to finish the account their
        /// business owner already created. Also rate-limited: a stolen invitation
        /// link is otherwise a brute-forceable pre-auth endpoint just like login.
        /// </summary>
        [HttpPost("businessMember/registration")]
        [EnableRateLimiting("auth")]
        public async Task<ActionResult<RegistrationResponse>> CompleteBusinessMemberRegistration(
            [FromBody] CompleteBusinessMemberRegistrationRequest request,
            CancellationToken cancellationToken)
        {
            await _completeBusinessMemberRegistrationRequestValidator.ValidateAndThrowAsync(request, cancellationToken);

            var (response, refreshToken) = await _authService.CompleteBusinessMemberRegistration(request, cancellationToken);

            SetRefreshTokenCookie(refreshToken);

            return Ok(response);
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout(
            CancellationToken cancellationToken)
        {
            if (Request.Cookies.TryGetValue(_refreshTokenOptions.CookieName, out var refreshToken) &&
                !string.IsNullOrEmpty(refreshToken))
            {
                await _authService.LogoutAsync(
                    refreshToken,
                    cancellationToken);
            }

            ClearRefreshTokenCookie();

            return NoContent();
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
