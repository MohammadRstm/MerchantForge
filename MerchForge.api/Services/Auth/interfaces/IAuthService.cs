using MerchForge.api.DTOs.Auth;

namespace MerchForge.api.Services.Auth.interfaces
{
    public interface IAuthService
    {
        Task<AuthResponse> RegisterAsync(
            RegisterRequest request,
            CancellationToken cancellationToken = default);

        Task<AuthResponse> LoginAsync(
            LoginRequest request,
            CancellationToken cancellationToken = default);

        Task<AuthResponse> RefreshAsync(
            string refreshToken,
            CancellationToken cancellationToken = default);

        Task LogoutAsync(
            string refreshToken,
            CancellationToken cancellationToken = default);
    }
}
