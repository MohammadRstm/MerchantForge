using MerchForge.api.DTOs.Auth;
using MerchForge.api.Services.Auth.interfaces;
using Microsoft.AspNetCore.Mvc;
using FluentValidation;
using MerchForge.api.Validators.Auth;

namespace MerchForge.api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IValidator<LoginRequest> _loginValidator;
        private readonly IValidator<CompleteBusinessOwnerRegistrationRequest> _completeBusinessOwnerRegistrationRequestValidator;

        public AuthController(
            IAuthService authService,
            IValidator<LoginRequest> loginValidator,
            IValidator<CompleteBusinessOwnerRegistrationRequest> completeBusinessOwnerRegistrationRequestValidator)
        {
            _authService = authService;
            _loginValidator = loginValidator;
            _completeBusinessOwnerRegistrationRequestValidator = completeBusinessOwnerRegistrationRequestValidator;
        }

        [HttpPost("login")]
        public async Task<ActionResult<AuthResponse>> Login(
            [FromBody] LoginRequest request,
            CancellationToken cancellationToken)
        {
            await _loginValidator.ValidateAndThrowAsync(request);

            var response = await _authService.LoginAsync(
                request,
                cancellationToken);

            return Ok(response);
        }

        [HttpPost("register/superAdmin")]
        public async Task<ActionResult<AuthResponse>> RegsiterSuperAdmin(
            [FromBody] RegisterSuperAdminRequest request,
            CancellationToken cancellationToken)
        {
            var response = await _authService.RegisterSuperAdmin(
                request,
                cancellationToken);

            return Ok(response);
        }

        [HttpPost("refresh")]
        public async Task<ActionResult<AuthResponse>> Refresh(
            [FromBody] RefreshTokenRequest request,
            CancellationToken cancellationToken)
        {
            var response = await _authService.RefreshAsync(
                request.RefreshToken,
                cancellationToken);
            
            return Ok(response);
        }

        [HttpPost("businessOwner/registration")]
        public async Task<ActionResult<RegistrationResponse>> CompleteBusinessOwnerRegistration(
            [FromBody] CompleteBusinessOwnerRegistrationRequest request,
            CancellationToken cancellationToken)
        {
            await _completeBusinessOwnerRegistrationRequestValidator.ValidateAndThrowAsync(request);

            var response = await _authService.CompleteBusinessOwnerRegistration(request);

            return Ok(response);
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout(
            [FromBody] RefreshTokenRequest request,
            CancellationToken cancellationToken)
        {
            await _authService.LogoutAsync(
                request.RefreshToken,
                cancellationToken);

            return NoContent();
        }
    }
}
