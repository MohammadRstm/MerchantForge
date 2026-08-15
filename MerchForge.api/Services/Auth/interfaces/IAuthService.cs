using MerchForge.api.DTOs.Auth;
using MerchForge.api.Models;

namespace MerchForge.api.Services.Auth.interfaces
{
    public interface IAuthService
    {
        Task<AuthResponse> LoginAsync(
            LoginRequest request,
            CancellationToken cancellationToken = default);

        Task<AuthResponse> RefreshAsync(
            string refreshToken,
            CancellationToken cancellationToken = default);

        Task LogoutAsync(
            string refreshToken,
            CancellationToken cancellationToken = default);

        Task<User> RegisterSuperAdmin(
            RegisterSuperAdminRequest request,
            CancellationToken cancellationToken);
    }
}
