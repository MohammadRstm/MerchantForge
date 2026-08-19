using MerchForge.api.DTOs.Auth;
using MerchForge.api.Models;

namespace MerchForge.api.Services.Auth.interfaces
{
    public interface IAuthService
    {
        Task<LoginResponse> LoginAsync(
            LoginRequest request,
            CancellationToken cancellationToken = default);

        Task<AuthResponse> RefreshAsync(
            string refreshToken,
            CancellationToken cancellationToken = default);

        Task<RegistrationResponse> CompleteBusinessOwnerRegistration(
            CompleteBusinessOwnerRegistrationRequest request,
            CancellationToken cancellationToken = default);

        Task LogoutAsync(
            string refreshToken,
            CancellationToken cancellationToken = default);

        Task<AuthResponse> RegisterSuperAdmin(
            RegisterSuperAdminRequest request,
            CancellationToken cancellationToken);
    }
}
