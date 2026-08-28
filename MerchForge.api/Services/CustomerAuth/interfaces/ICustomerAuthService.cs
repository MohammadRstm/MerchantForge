using MerchForge.api.DTOs.CustomerAuth;

namespace MerchForge.api.Services.CustomerAuth.interfaces
{
    public interface ICustomerAuthService
    {
        Task<(CustomerSessionResponse Response, string RefreshToken)> SignupAsync(
            CustomerSignupRequest request,
            CancellationToken cancellationToken = default);

        Task<(CustomerSessionResponse Response, string RefreshToken)> LoginAsync(
            CustomerLoginRequest request,
            CancellationToken cancellationToken = default);

        Task<(CustomerSessionResponse Response, string RefreshToken)> RefreshAsync(
            string refreshToken,
            CancellationToken cancellationToken = default);

        Task LogoutAsync(
            string refreshToken,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Redeems a one-time exchange code from a storefront's own origin. Returns an
        /// access token only — never sets or touches the customerRefreshToken cookie,
        /// since this call is cross-origin and deliberately credential-free.
        /// </summary>
        Task<CustomerSessionResponse> RedeemExchangeCodeAsync(
            string code,
            string returnUrl,
            CancellationToken cancellationToken = default);

        Task<CustomerProfileResponse> GetProfileAsync(
            Guid customerId,
            CancellationToken cancellationToken = default);

        Task<CustomerProfileResponse> UpdateProfileAsync(
            Guid customerId,
            UpdateCustomerProfileRequest request,
            CancellationToken cancellationToken = default);
    }
}
