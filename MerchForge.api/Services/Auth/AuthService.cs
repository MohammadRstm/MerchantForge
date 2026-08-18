using MerchForge.api.Data;
using MerchForge.api.DTOs.Auth;
using MerchForge.api.Enums;
using MerchForge.api.Exceptions.Auth;
using MerchForge.api.Models;
using MerchForge.api.Repositories.Interfaces;
using MerchForge.api.Services.Auth.interfaces;
using MerchForge.api.Services.Invitation.interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace MerchForge.api.Services.Auth;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly IJwtService _jwtService;
    private readonly IRefreshTokenService _refreshTokenService;

    private readonly IInvitationService _invitationService;
    private readonly IBusinessRepository _businessRepository;

    public AuthService(
        IUserRepository userRepository,
        IPasswordHasher<User> passwordHasher,
        IJwtService jwtService,
        IRefreshTokenService refreshTokenService,
        IInvitationService invitationService,
        IBusinessRepository businessRepository)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtService = jwtService;
        _refreshTokenService = refreshTokenService;
        _invitationService = invitationService;
        _businessRepository = businessRepository;
    }
    public async Task<LoginResponse> LoginAsync(
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

        var businessInfo = await _businessRepository.GetUserBusinessAsync(user.Id, cancellationToken);

        return new LoginResponse
        {
            AuthResponse = await CreateAuthResponse(user, refresh_token),
            business = businessInfo,
        };
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

    public async Task<RegistrationResponse> CompleteBusinessOwnerRegistration(
        CompleteBusinessOwnerRegistrationRequest request,
        CancellationToken cancellationToken = default)
    {
        // validate invitation token:
        var tokenHash = _invitationService.HashInvitationToken(request.InvitationToken);

        var invitation = await _invitationService.GetInvitationByHashToken(tokenHash);

        _invitationService.ValidateBusinessOwnerInvitation(invitation);

        var systemRoleId = await _userRepository.GetSystemRoleId(Enums.SystemRole.User, cancellationToken);
        var businessRoleId = await _userRepository.GetBusinessRoleId(BusinessRole.Owner, cancellationToken);

        // create user record
        var owner = new User
        {
            Id = Guid.NewGuid(),
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            SystemRoleId = systemRoleId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        var password = GenerateRandomPassword();

        owner.PasswordHash = _passwordHasher.HashPassword(
            owner,
            password
        );

        // create business
        var business = new Business
        {
            Id = Guid.NewGuid(),
            Name = request.BusinessName,
            OwnerUserId = owner.Id,
            
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        // create business user
        var businessUser = new BusinessUser
        {
            UserId = owner.Id,
            BusinessId = business.Id,

            RoleId = businessRoleId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        invitation.AcceptedAt = DateTime.UtcNow;

        await _userRepository.FinishBusinessOwnerRegistration(owner, business,  businessUser, cancellationToken);

        var (refreshToken, _) =
            await _refreshTokenService.CreateAsync(owner, cancellationToken);

        return await CreateRegistrationResponse(owner, refreshToken, password);
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

    private async Task<RegistrationResponse> CreateRegistrationResponse(
        User user,
        string refreshToken,
        string rawPassword)
    {
        return new RegistrationResponse
        {
            AuthResponse = new AuthResponse
            {
                AccessToken = await _jwtService.GenerateAccessToken(user),
                RefreshToken = refreshToken,
                AccessTokenExpiresAt = _jwtService.GetExpirationTime()
            },
            rawPassword = rawPassword
        };
    }
    private static string GenerateRandomPassword(int length = 16)
    {
        const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        const string lower = "abcdefghijkmnopqrstuvwxyz";
        const string numbers = "23456789";
        const string special = "!@#$%^&*";

        const string all = upper + lower + numbers + special;

        var password = new char[length];

        // Guarantee at least one character from each category
        password[0] = upper[RandomNumberGenerator.GetInt32(upper.Length)];
        password[1] = lower[RandomNumberGenerator.GetInt32(lower.Length)];
        password[2] = numbers[RandomNumberGenerator.GetInt32(numbers.Length)];
        password[3] = special[RandomNumberGenerator.GetInt32(special.Length)];

        for (int i = 4; i < length; i++)
        {
            password[i] = all[RandomNumberGenerator.GetInt32(all.Length)];
        }

        // Shuffle so the first four characters aren't predictable
        for (int i = password.Length - 1; i > 0; i--)
        {
            int j = RandomNumberGenerator.GetInt32(i + 1);
            (password[i], password[j]) = (password[j], password[i]);
        }

        return new string(password);
    }


}