using MerchForge.api.DTOs.Auth;
using MerchForge.api.Services.Auth.interfaces;
using Microsoft.AspNetCore.Mvc;

namespace MerchForge.api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<ActionResult<AuthResponse>> Register(
            [FromBody] RegisterRequest request,
            CancellationToken cancellationToken)
        {
            var response = await _authService.RegisterAsync(
                request,
                cancellationToken);

            return Ok(response);
        }

        [HttpPost("login")]
        public async Task<ActionResult<AuthResponse>> Login(
            [FromBody] LoginRequest request,
            CancellationToken cancellationToken)
        {
            var response = await _authService.LoginAsync(
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
