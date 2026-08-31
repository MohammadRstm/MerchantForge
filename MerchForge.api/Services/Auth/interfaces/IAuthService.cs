using MerchForge.api.DTOs.Auth;
using MerchForge.api.Models;

namespace MerchForge.api.Services.Auth.interfaces
{
    public interface IAuthService
    {
        Task<(LoginResponse Response, string RefreshToken)> LoginAsync(
            LoginRequest request,
            CancellationToken cancellationToken = default);

        Task<(LoginResponse Response, string RefreshToken)> RefreshAsync(
            string refreshToken,
            CancellationToken cancellationToken = default);

        Task<(RegistrationResponse Response, string RefreshToken)> CompleteBusinessOwnerRegistration(
            CompleteBusinessOwnerRegistrationRequest request,
            CancellationToken cancellationToken = default);

        Task<(RegistrationResponse Response, string RefreshToken)> CompleteBusinessMemberRegistration(
            CompleteBusinessMemberRegistrationRequest request,
            CancellationToken cancellationToken = default);

        Task LogoutAsync(
            string refreshToken,
            CancellationToken cancellationToken = default);

        Task<(AuthResponse Response, string RefreshToken)> RegisterSuperAdmin(
            RegisterSuperAdminRequest request,
            CancellationToken cancellationToken);
    }
}
