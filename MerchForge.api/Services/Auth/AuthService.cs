using MerchForge.api.Data;
using MerchForge.api.DTOs.Auth;
using MerchForge.api.Enums;
using MerchForge.api.Exceptions.Auth;
using MerchForge.api.Models;
using MerchForge.api.Repositories.Interfaces;
using MerchForge.api.Services.Auth.interfaces;
using MerchForge.api.Services.Invitation.interfaces;
using MerchForge.api.Services.Onboarding.interfaces;
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
    private readonly IDomainService _domainService;

    public AuthService(
        IUserRepository userRepository,
        IPasswordHasher<User> passwordHasher,
        IJwtService jwtService,
        IRefreshTokenService refreshTokenService,
        IInvitationService invitationService,
        IBusinessRepository businessRepository,
        IDomainService domainService)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtService = jwtService;
        _refreshTokenService = refreshTokenService;
        _invitationService = invitationService;
        _businessRepository = businessRepository;
        _domainService = domainService;
    }
    public async Task<(LoginResponse Response, string RefreshToken)> LoginAsync(
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

        var (refreshToken, _) =
              await _refreshTokenService.CreateAsync(
                  user,
                  cancellationToken);

        var business =
            await _businessRepository
                .GetUserBusinessAsync(
                    user.Id,
                    cancellationToken);

        var systemRole = await _userRepository.GetSystemRoleById(user.SystemRoleId, cancellationToken);

        var response = new LoginResponse
        {
            AuthResponse = await CreateAuthResponse(user),

            UserId = user.Id,

            FirstName = user.FirstName,

            LastName = user.LastName,

            SystemRole = systemRole.ToString(),

            Business = business
        };

        return (response, refreshToken);
    }

    public async Task<(LoginResponse Response, string RefreshToken)> RefreshAsync(
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

        var user = refreshTokenEntity.User;

        var business =
            await _businessRepository
                .GetUserBusinessAsync(
                    user.Id,
                    cancellationToken);

        var systemRole = await _userRepository.GetSystemRoleById(user.SystemRoleId, cancellationToken);

        var response = new LoginResponse
        {
            AuthResponse = await CreateAuthResponse(user),

            UserId = user.Id,

            FirstName = user.FirstName,

            LastName = user.LastName,

            SystemRole = systemRole.ToString(),

            Business = business
        };

        return (response, newRefreshToken);
    }

    public async Task<(AuthResponse Response, string RefreshToken)> RegisterSuperAdmin(
        RegisterSuperAdminRequest request,
        CancellationToken cancellationToken)
    {
        // Bootstrap-only: this endpoint is intentionally unauthenticated so the very first
        // super admin can be created, but it must never work once one already exists.
        if (await _userRepository.SuperAdminExistsAsync(cancellationToken))
        {
            throw new SuperAdminAlreadyExistsException();
        }

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

        var (refreshToken, _) =
            await _refreshTokenService.CreateAsync(
                superAdmin,
                cancellationToken);

        var response = await CreateAuthResponse(superAdmin);

        return (response, refreshToken);
    }

    public async Task<(RegistrationResponse Response, string RefreshToken)> CompleteBusinessOwnerRegistration(
        CompleteBusinessOwnerRegistrationRequest request,
        CancellationToken cancellationToken = default)
    {
        // validate invitation token:
        var tokenHash = _invitationService.HashInvitationToken(request.InvitationToken);

        var invitation = await _invitationService.GetInvitationByHashToken(tokenHash);

        _invitationService.ValidateBusinessOwnerInvitation(invitation);

        // Fail before creating anything if the chosen domain doesn't exist, rather
        // than partway through building the user/business/category rows below.
        await _domainService.EnsureDomainExistsAsync(request.BusinessDomainId, cancellationToken);

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

        // Snapshot the chosen optional product fields. Built before the business is
        // constructed so an unknown key fails the request before anything is created.
        var metadataShape = await _domainService.BuildMetadataShapeAsync(
            request.BusinessDomainId,
            request.SelectedProductAttributeKeys,
            cancellationToken);

        // create business
        var business = new Business
        {
            Id = Guid.NewGuid(),
            Name = request.BusinessName,
            OwnerUserId = owner.Id,
            BusinessDomainId = request.BusinessDomainId,
            MetadataShape = metadataShape,

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

        // Any category names that don't already exist as platform categories in this
        // domain become private categories owned by the new business. Built before
        // the business is persisted, same as businessUser above — business.Id is
        // already known since it's a client-generated Guid.
        var customCategories = await _domainService.BuildCustomCategoriesAsync(
            business.Id,
            request.BusinessDomainId,
            request.NewCategoryNames,
            cancellationToken);

        await _userRepository.FinishBusinessOwnerRegistration(
            owner, business, businessUser, customCategories, invitation.Id, cancellationToken);

        var (refreshToken, _) =
            await _refreshTokenService.CreateAsync(owner, cancellationToken);

        var response = await CreateRegistrationResponse(owner, password);

        return (response, refreshToken);
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
    User user)
    {
        return new AuthResponse
        {
            AccessToken = await _jwtService.GenerateAccessToken(user),
            AccessTokenExpiresAt = _jwtService.GetExpirationTime()
        };
    }

    private async Task<RegistrationResponse> CreateRegistrationResponse(
        User user,
        string rawPassword)
    {
        return new RegistrationResponse
        {
            AuthResponse = await CreateAuthResponse(user),
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