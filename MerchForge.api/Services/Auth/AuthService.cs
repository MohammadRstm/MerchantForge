using MerchForge.api.Data;
using MerchForge.api.DTOs.Auth;
using MerchForge.api.Models;
using MerchForge.api.Services.Auth.interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace MerchForge.api.Services.Auth;

public class AuthService : IAuthService
{
    private readonly MerchForgeDbContext _db;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly IJwtService _jwtService;
    private readonly IRefreshTokenService _refreshTokenService;

    public AuthService(
        MerchForgeDbContext db,
        IPasswordHasher<User> passwordHasher,
        IJwtService jwtService,
        IRefreshTokenService refreshTokenService)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _jwtService = jwtService;
        _refreshTokenService = refreshTokenService;
    }

    public async Task<AuthResponse> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default)
    {

        var existingUser = await _db.Users
          .FirstOrDefaultAsync(
          u => u.Email == request.Email,
          cancellationToken);

        if (existingUser is not null)
        {
            throw new InvalidOperationException(
                "A user with this email already exists.");
        }


        var user = new User{
            Id = Guid.NewGuid(),
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
        };

        user.PasswordHash = _passwordHasher.HashPassword(
            user,
            request.Password
        );

        var business = new Business
        {
            Id = Guid.NewGuid(),
            Name = request.BusinessName,
            OwnerUserId = user.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        var businessUser = new BusinessUser
        {
            BusinessId = business.Id,
            UserId = user.Id,
            Role = Enums.BusinessRole.Owner,
            CreatedAt = business.CreatedAt,
        };

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

        var access_token = _jwtService.GenerateAccessToken(user);

        var (refresh_token, _) =
            await _refreshTokenService.CreateAsync(
                user,
                cancellationToken);

        return new AuthResponse
        {
            AccessToken = access_token,
            RefreshToken = refresh_token,
            AccessTokenExpiresAt = _jwtService.GetExpirationTime()
        };
    }

    public async Task<AuthResponse> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(
            u => u.Email == request.Email);

        if (user is null)
        {
            throw new InvalidOperationException(
                "Invalid email or password.");
        }

        var hashedPassword = _passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            request.Password
        );

        if (! hashedPassword.Equals(user.PasswordHash))
        {
            throw new InvalidOperationException(
                "Invalid email or password.");
        }

        var access_token = _jwtService.GenerateAccessToken(user);

        var (refresh_token, _) =
            await _refreshTokenService.CreateAsync(
                user,
                cancellationToken);

        return new AuthResponse
        {
            AccessToken = access_token,
            RefreshToken = refresh_token,
            AccessTokenExpiresAt = _jwtService.GetExpirationTime()
        };
    }

    public async Task<AuthResponse> RefreshAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        var refreshTokenEntity = await _refreshTokenService.GetValidTokenAsync(refreshToken, cancellationToken);

        if(refreshTokenEntity is null)
        {
            throw new InvalidOperationException("Refresh token not found");
        }

        var (newRefreshToken, _) =
             await _refreshTokenService.RotateAsync(
                 refreshTokenEntity,
                 cancellationToken);

        var access_token = _jwtService.GenerateAccessToken(
            refreshTokenEntity.User);

        return new AuthResponse
        {
            RefreshToken = newRefreshToken,
            AccessToken = access_token,
            AccessTokenExpiresAt = _jwtService.GetExpirationTime()
        };
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
}