using MerchForge.api.Data;
using MerchForge.api.DTOs.Auth;
using MerchForge.api.Enums;
using MerchForge.api.Exceptions.Auth;
using MerchForge.api.Models;
using MerchForge.api.Repositories.Interfaces;
using MerchForge.api.Services.Auth.interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace MerchForge.api.Services.Auth;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly IJwtService _jwtService;
    private readonly IRefreshTokenService _refreshTokenService;

    public AuthService(
        IUserRepository userRepository,
        IPasswordHasher<User> passwordHasher,
        IJwtService jwtService,
        IRefreshTokenService refreshTokenService)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtService = jwtService;
        _refreshTokenService = refreshTokenService;
    }
    public async Task<AuthResponse> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);

        if (user is null)
        {
            throw new InvalidCredentialsException();
        }

        var result = _passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            request.Password
        );

        if (result == PasswordVerificationResult.Failed)
        {
            throw new InvalidCredentialsException();
        }

        var (refresh_token, _) =
            await _refreshTokenService.CreateAsync(
                user,
                cancellationToken);

        return await CreateAuthResponse(user, refresh_token);
    }

    public async Task<AuthResponse> RefreshAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        var refreshTokenEntity = await _refreshTokenService.GetValidTokenAsync(refreshToken, cancellationToken);

        if(refreshTokenEntity is null)
        {
            throw new InvalidRefreshTokenException();
        }

        var (newRefreshToken, _) =
             await _refreshTokenService.RotateAsync(
                 refreshTokenEntity,
                 cancellationToken);

        return await CreateAuthResponse(refreshTokenEntity.User, newRefreshToken);
    }

    public async Task<AuthResponse> RegisterSuperAdmin(
        RegisterSuperAdminRequest request,
        CancellationToken cancellationToken)
    {
        // get super admin role id
        var superAdminRoleId = await _userRepository.GetSystemRoleId(SystemRole.SuperAdmin, cancellationToken);

        var superAdmin = new User
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            SystemRoleId = superAdminRoleId,
        };

        superAdmin.PasswordHash = _passwordHasher.HashPassword(
               superAdmin,
               request.Password
        );

        // do this in a transaction later please
        await _userRepository.CreateSuperAdmin(superAdmin, cancellationToken);

        var (refresh_token, _) =
            await _refreshTokenService.CreateAsync(
                superAdmin,
                cancellationToken);


        return await CreateAuthResponse(superAdmin , refresh_token);
    }

    public async Task LogoutAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        var token = await _refreshTokenService.GetValidTokenAsync(
        refreshToken,
        cancellationToken);

        if (token is null)
        {
            return;
        }

        await _refreshTokenService.RevokeAsync(
            token,
            cancellationToken);
    }

    private async Task<AuthResponse> CreateAuthResponse(
    User user,
    string refreshToken)
    {
        return new AuthResponse
        {
            AccessToken = await _jwtService.GenerateAccessToken(user),
            RefreshToken = refreshToken,
            AccessTokenExpiresAt = _jwtService.GetExpirationTime()
        };
    }
}