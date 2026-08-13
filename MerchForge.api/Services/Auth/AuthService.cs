using MerchForge.api.Data;
using MerchForge.api.DTOs.Auth;
using MerchForge.api.Exceptions.Auth;
using MerchForge.api.Factory;
using MerchForge.api.Models;
using MerchForge.api.Repositories.Interfaces;
using MerchForge.api.Services.Auth.interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace MerchForge.api.Services.Auth;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IRegistrationFactory _registrationFactory;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly IJwtService _jwtService;
    private readonly IRefreshTokenService _refreshTokenService;

    public AuthService(
        IUserRepository userRepository,
        IPasswordHasher<User> passwordHasher,
        IJwtService jwtService,
        IRegistrationFactory registrationFactory,
        IRefreshTokenService refreshTokenService)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtService = jwtService;
        _registrationFactory = registrationFactory;
        _refreshTokenService = refreshTokenService;
    }

    public async Task<AuthResponse> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default)
    {

        var existingUser = await _userRepository.GetByEmailAsync(request.Email,
        cancellationToken);

        if (existingUser is not null)
        {
            throw new EmailAlreadyExistsException();
        }

        var (user, business, businessUser) = _registrationFactory.Create(request);

        await CreateRegistrationAsync(user, business, businessUser, cancellationToken);

        var (refresh_token, _) =
            await _refreshTokenService.CreateAsync(
                user,
                cancellationToken);

        return CreateAuthResponse(user, refresh_token);
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

        return CreateAuthResponse(user, refresh_token);
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

        return CreateAuthResponse(refreshTokenEntity.User, newRefreshToken);
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

    private async Task CreateRegistrationAsync(
        User user,
        Business business,
        BusinessUser businessUser,
        CancellationToken cancellationToken)
    {
        await using var transaction =
            await _db.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            await _db.Users.AddAsync(user, cancellationToken);
            await _db.Businesses.AddAsync(business, cancellationToken);
            await _db.BusinessUsers.AddAsync(businessUser, cancellationToken);

            await _db.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private AuthResponse CreateAuthResponse(
    User user,
    string refreshToken)
    {
        return new AuthResponse
        {
            AccessToken = _jwtService.GenerateAccessToken(user),
            RefreshToken = refreshToken,
            AccessTokenExpiresAt = _jwtService.GetExpirationTime()
        };
    }
}