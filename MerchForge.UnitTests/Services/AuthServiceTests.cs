using FluentAssertions;
using MerchForge.api.DTOs.Auth;
using MerchForge.api.Enums;
using MerchForge.api.Exceptions.Auth;
using MerchForge.api.Exceptions.Invitation;
using MerchForge.api.Models;
using MerchForge.api.Repositories.Interfaces;
using MerchForge.api.Services.Audit.interfaces;
using MerchForge.api.Services.Auth;
using MerchForge.api.Services.Auth.interfaces;
using MerchForge.api.Services.Invitation.interfaces;
using MerchForge.api.Services.Onboarding.interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace MerchForge.UnitTests.Services;

/// <summary>
/// Login and account-provisioning are the highest-stakes, previously completely
/// untested surface in the app (this file was empty before) - a regression here
/// means either a real account can't sign in, or a bug quietly lets one in that
/// shouldn't. Uses the real ASP.NET Core PasswordHasher rather than mocking
/// hashing itself, so a test failure here reflects AuthService's own logic, not a
/// fake standing in for password verification.
/// </summary>
public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly IPasswordHasher<User> _passwordHasher = new PasswordHasher<User>();
    private readonly Mock<IJwtService> _jwtService = new();
    private readonly Mock<IRefreshTokenService> _refreshTokenService = new();
    private readonly Mock<IInvitationService> _invitationService = new();
    private readonly Mock<IBusinessRepository> _businessRepository = new();
    private readonly Mock<IDomainService> _domainService = new();
    private readonly Mock<IAuditLogService> _auditLogService = new();

    private readonly AuthService _service;

    public AuthServiceTests()
    {
        _service = new AuthService(
            _userRepository.Object,
            _passwordHasher,
            _jwtService.Object,
            _refreshTokenService.Object,
            _invitationService.Object,
            _businessRepository.Object,
            _domainService.Object,
            _auditLogService.Object,
            NullLogger<AuthService>.Instance);

        _jwtService.Setup(s => s.GenerateAccessToken(It.IsAny<User>())).ReturnsAsync("access-token");
        _jwtService.Setup(s => s.GetExpirationTime()).Returns(DateTime.UtcNow.AddMinutes(15));
        _refreshTokenService
            .Setup(s => s.CreateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(("refresh-token", new RefreshToken()));
    }

    private User BuildUser(string password)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Ada",
            LastName = "Lovelace",
            Email = "ada@example.com",
            SystemRoleId = Guid.NewGuid(),
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, password);
        return user;
    }

    // ---- LoginAsync ----

    [Fact]
    public async Task Login_succeeds_with_the_correct_password()
    {
        var user = BuildUser("correct-horse");
        _userRepository.Setup(r => r.GetByEmailAsync(user.Email, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _businessRepository
            .Setup(r => r.GetUserBusinessAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((BusinessContextResponse?)null);
        _userRepository
            .Setup(r => r.GetSystemRoleById(user.SystemRoleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(SystemRole.User);

        var (response, refreshToken) = await _service.LoginAsync(new LoginRequest { Email = user.Email, Password = "correct-horse" });

        response.UserId.Should().Be(user.Id);
        response.AuthResponse.AccessToken.Should().Be("access-token");
        refreshToken.Should().Be("refresh-token");
    }

    [Fact]
    public async Task Login_fails_for_an_unknown_email()
    {
        _userRepository
            .Setup(r => r.GetByEmailAsync("nobody@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var act = () => _service.LoginAsync(new LoginRequest { Email = "nobody@example.com", Password = "whatever1" });

        await act.Should().ThrowAsync<InvalidCredentialsException>();
    }

    [Fact]
    public async Task Login_fails_for_the_wrong_password()
    {
        var user = BuildUser("correct-horse");
        _userRepository.Setup(r => r.GetByEmailAsync(user.Email, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var act = () => _service.LoginAsync(new LoginRequest { Email = user.Email, Password = "wrong-password" });

        await act.Should().ThrowAsync<InvalidCredentialsException>();
    }

    [Fact]
    public async Task Login_fails_for_a_disabled_account_even_with_the_correct_password()
    {
        var user = BuildUser("correct-horse");
        user.DisabledAt = DateTime.UtcNow.AddMinutes(-5);
        _userRepository.Setup(r => r.GetByEmailAsync(user.Email, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var act = () => _service.LoginAsync(new LoginRequest { Email = user.Email, Password = "correct-horse" });

        await act.Should().ThrowAsync<AccountDisabledException>();
    }

    [Fact]
    public async Task Login_reports_invalid_credentials_not_account_disabled_when_the_password_is_wrong_for_a_disabled_account()
    {
        // A wrong password against a disabled account must look identical to a wrong
        // password against an active one - otherwise the disabled state leaks to
        // anyone who merely guesses the email, without needing the real password.
        var user = BuildUser("correct-horse");
        user.DisabledAt = DateTime.UtcNow.AddMinutes(-5);
        _userRepository.Setup(r => r.GetByEmailAsync(user.Email, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var act = () => _service.LoginAsync(new LoginRequest { Email = user.Email, Password = "wrong-password" });

        await act.Should().ThrowAsync<InvalidCredentialsException>();
    }

    // ---- RegisterSuperAdmin ----

    [Fact]
    public async Task RegisterSuperAdmin_fails_once_a_super_admin_already_exists()
    {
        _userRepository.Setup(r => r.SuperAdminExistsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var act = () => _service.RegisterSuperAdmin(
            new RegisterSuperAdminRequest { FirstName = "A", LastName = "B", Email = "a@b.com", Password = "password1" },
            CancellationToken.None);

        await act.Should().ThrowAsync<SuperAdminAlreadyExistsException>();
        _userRepository.Verify(r => r.CreateSuperAdmin(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RegisterSuperAdmin_fails_when_the_email_is_already_taken()
    {
        _userRepository.Setup(r => r.SuperAdminExistsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _userRepository
            .Setup(r => r.GetByEmailAsync("a@b.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Email = "a@b.com" });

        var act = () => _service.RegisterSuperAdmin(
            new RegisterSuperAdminRequest { FirstName = "A", LastName = "B", Email = "a@b.com", Password = "password1" },
            CancellationToken.None);

        await act.Should().ThrowAsync<EmailAlreadyExistsException>();
    }

    [Fact]
    public async Task RegisterSuperAdmin_hashes_the_submitted_password_not_a_generated_one()
    {
        _userRepository.Setup(r => r.SuperAdminExistsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _userRepository.Setup(r => r.GetByEmailAsync("a@b.com", It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);
        _userRepository
            .Setup(r => r.GetSystemRoleId(SystemRole.SuperAdmin, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.NewGuid());

        User? created = null;
        _userRepository
            .Setup(r => r.CreateSuperAdmin(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((u, _) => created = u)
            .ReturnsAsync((User u, CancellationToken _) => u);

        await _service.RegisterSuperAdmin(
            new RegisterSuperAdminRequest { FirstName = "A", LastName = "B", Email = "a@b.com", Password = "my-chosen-password" },
            CancellationToken.None);

        created.Should().NotBeNull();
        _passwordHasher.VerifyHashedPassword(created!, created!.PasswordHash, "my-chosen-password")
            .Should().Be(PasswordVerificationResult.Success);
    }

    // ---- CompleteBusinessOwnerRegistration ----

    private static Invitation ValidOwnerInvitation(string email) => new()
    {
        Id = Guid.NewGuid(),
        Email = email,
        Type = InvitationType.BusinessOwner,
        BusinessRole = BusinessRole.Owner,
        SystemRole = SystemRole.User,
        ExpiresAt = DateTime.UtcNow.AddHours(1),
    };

    [Fact]
    public async Task CompleteBusinessOwnerRegistration_hashes_the_owners_own_chosen_password()
    {
        var invitation = ValidOwnerInvitation("owner@example.com");
        _invitationService.Setup(s => s.HashInvitationToken(It.IsAny<string>())).Returns("hash");
        _invitationService
            .Setup(s => s.GetInvitationByHashToken("hash", It.IsAny<CancellationToken>()))
            .ReturnsAsync(invitation);
        _domainService.Setup(s => s.EnsureDomainExistsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _userRepository
            .Setup(r => r.GetByEmailAsync("owner@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        _userRepository.Setup(r => r.GetSystemRoleId(SystemRole.User, It.IsAny<CancellationToken>())).ReturnsAsync(Guid.NewGuid());
        _userRepository.Setup(r => r.GetBusinessRoleId(BusinessRole.Owner, It.IsAny<CancellationToken>())).ReturnsAsync(Guid.NewGuid());
        _domainService
            .Setup(s => s.BuildMetadataShapeAsync(It.IsAny<Guid>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((System.Text.Json.JsonDocument?)null);
        _domainService
            .Setup(s => s.BuildCustomCategoriesAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        User? created = null;
        _userRepository
            .Setup(r => r.FinishBusinessOwnerRegistration(
                It.IsAny<User>(), It.IsAny<Business>(), It.IsAny<BusinessUser>(),
                It.IsAny<IReadOnlyList<Category>>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Callback<User, Business, BusinessUser, IReadOnlyList<Category>, Guid, CancellationToken>(
                (u, _, _, _, _, _) => created = u)
            .Returns(Task.CompletedTask);

        var (response, _) = await _service.CompleteBusinessOwnerRegistration(new CompleteBusinessOwnerRegistrationRequest
        {
            FirstName = "New",
            LastName = "Owner",
            BusinessName = "New Business",
            Email = "owner@example.com",
            Password = "owners-own-password",
            InvitationToken = "raw-token",
            BusinessDomainId = Guid.NewGuid(),
        });

        response.AuthResponse.Should().NotBeNull();
        created.Should().NotBeNull();
        _passwordHasher.VerifyHashedPassword(created!, created!.PasswordHash, "owners-own-password")
            .Should().Be(PasswordVerificationResult.Success);
    }

    [Fact]
    public async Task CompleteBusinessOwnerRegistration_fails_when_the_email_already_has_an_account()
    {
        var invitation = ValidOwnerInvitation("owner@example.com");
        _invitationService.Setup(s => s.HashInvitationToken(It.IsAny<string>())).Returns("hash");
        _invitationService
            .Setup(s => s.GetInvitationByHashToken("hash", It.IsAny<CancellationToken>()))
            .ReturnsAsync(invitation);
        _domainService.Setup(s => s.EnsureDomainExistsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _userRepository
            .Setup(r => r.GetByEmailAsync("owner@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Email = "owner@example.com" });

        var act = () => _service.CompleteBusinessOwnerRegistration(new CompleteBusinessOwnerRegistrationRequest
        {
            FirstName = "New",
            LastName = "Owner",
            BusinessName = "New Business",
            Email = "owner@example.com",
            Password = "owners-own-password",
            InvitationToken = "raw-token",
            BusinessDomainId = Guid.NewGuid(),
        });

        await act.Should().ThrowAsync<EmailAlreadyExistsException>();
    }

    // ---- CompleteBusinessMemberRegistration ----

    private static Invitation ValidMemberInvitation(string email) => new()
    {
        Id = Guid.NewGuid(),
        Email = email,
        Type = InvitationType.BusinessMember,
        BusinessId = Guid.NewGuid(),
        BusinessRole = BusinessRole.Member,
        SystemRole = SystemRole.User,
        ExpiresAt = DateTime.UtcNow.AddHours(1),
    };

    [Fact]
    public async Task CompleteBusinessMemberRegistration_sets_the_password_the_member_chose_for_the_account_the_invitation_email_names()
    {
        var invitation = ValidMemberInvitation("member@example.com");
        var member = BuildUser("some-unusable-generated-password");
        member.Email = invitation.Email;

        _invitationService.Setup(s => s.HashInvitationToken(It.IsAny<string>())).Returns("hash");
        _invitationService
            .Setup(s => s.GetInvitationByHashToken("hash", It.IsAny<CancellationToken>()))
            .ReturnsAsync(invitation);
        _userRepository
            .Setup(r => r.GetByEmailAsync(invitation.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(member);

        string? capturedHash = null;
        _userRepository
            .Setup(r => r.CompleteBusinessMemberRegistration(member.Id, It.IsAny<string>(), invitation.Id, It.IsAny<CancellationToken>()))
            .Callback<Guid, string, Guid, CancellationToken>((_, hash, _, _) => capturedHash = hash)
            .Returns(Task.CompletedTask);

        var (response, refreshToken) = await _service.CompleteBusinessMemberRegistration(
            new CompleteBusinessMemberRegistrationRequest { InvitationToken = "raw-token", Password = "members-own-password" });

        response.AuthResponse.Should().NotBeNull();
        refreshToken.Should().Be("refresh-token");
        capturedHash.Should().NotBeNullOrEmpty();
        _passwordHasher.VerifyHashedPassword(member, capturedHash!, "members-own-password")
            .Should().Be(PasswordVerificationResult.Success);
    }

    [Fact]
    public async Task CompleteBusinessMemberRegistration_fails_for_an_already_accepted_invitation()
    {
        var invitation = ValidMemberInvitation("member@example.com");
        invitation.AcceptedAt = DateTime.UtcNow.AddMinutes(-5);

        _invitationService.Setup(s => s.HashInvitationToken(It.IsAny<string>())).Returns("hash");
        _invitationService
            .Setup(s => s.GetInvitationByHashToken("hash", It.IsAny<CancellationToken>()))
            .ReturnsAsync(invitation);
        _invitationService
            .Setup(s => s.ValidateBusinessMemberInvitation(invitation))
            .Throws(new InvitationAlreadyUsedException());

        var act = () => _service.CompleteBusinessMemberRegistration(
            new CompleteBusinessMemberRegistrationRequest { InvitationToken = "raw-token", Password = "some-password" });

        await act.Should().ThrowAsync<InvitationAlreadyUsedException>();
        _userRepository.Verify(
            r => r.CompleteBusinessMemberRegistration(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
