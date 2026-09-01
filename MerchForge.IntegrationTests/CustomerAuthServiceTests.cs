using FluentAssertions;
using MerchForge.api.Configurations;
using MerchForge.api.DTOs.CustomerAuth;
using MerchForge.api.Exceptions.CustomerAuth;
using MerchForge.api.Models;
using MerchForge.api.Repositories.Implementations;
using MerchForge.api.Services.Audit;
using MerchForge.api.Services.CustomerAuth;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace MerchForge.IntegrationTests;

/// <summary>
/// The storefront-shopper auth flow - a completely separate identity system from
/// the business-owner one (see CustomerAuthService's own doc comments), previously
/// untested. Against the real database on purpose: the exchange-code redemption
/// path's atomic claim (ExecuteUpdateAsync) is exactly the kind of thing a mock
/// would happily fake correctly while a real UPDATE ... WHERE does something
/// subtly different under concurrency.
/// </summary>
public class CustomerAuthServiceTests : IClassFixture<CatalogDatabaseFixture>
{
    private readonly CatalogDatabaseFixture _fixture;

    public CustomerAuthServiceTests(CatalogDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    private static CustomerAuthService CreateService(api.Data.MerchForgeDbContext db)
    {
        var customerRepository = new CustomerRepository(db);
        var passwordHasher = new PasswordHasher<Customer>();
        var jwtService = new CustomerJwtService(
            Options.Create(new JwtOptions
            {
                SecretKey = "test-only-signing-key-not-used-anywhere-real-1234567890",
                Issuer = "merchforge-tests",
            }),
            Options.Create(new CustomerJwtOptions { Audience = "merchforge-customers-test", AccessTokenExpirationMinutes = 15 }));
        var refreshTokenService = new CustomerRefreshTokenService(
            new CustomerRefreshTokenRepository(db),
            Options.Create(new CustomerRefreshTokenOptions { ExpirationDays = 30 }));

        var auditLogService = new AuditLogService(new AuditLogRepository(db), NullLogger<AuditLogService>.Instance);

        return new CustomerAuthService(
            customerRepository, passwordHasher, jwtService, refreshTokenService, auditLogService, db,
            NullLogger<CustomerAuthService>.Instance);
    }

    private static CustomerSignupRequest ValidSignup(string email) => new()
    {
        Email = email,
        Password = "correct-horse",
        FirstName = "Jane",
        LastName = "Shopper",
    };

    [Fact]
    public async Task Signup_creates_a_usable_account_and_returns_a_session()
    {
        await using var db = _fixture.CreateContext();
        var service = CreateService(db);
        var email = $"{Guid.NewGuid():N}@example.test";

        var (response, refreshToken) = await service.SignupAsync(ValidSignup(email));

        response.Email.Should().Be(email);
        response.AuthResponse.AccessToken.Should().NotBeNullOrEmpty();
        refreshToken.Should().NotBeNullOrEmpty();

        await using var verify = _fixture.CreateContext();
        var stored = await verify.Customers.AsNoTracking().FirstAsync(c => c.Email == email);
        stored.PasswordHash.Should().NotBe("correct-horse", "must be hashed, never stored raw");
    }

    [Fact]
    public async Task Signup_fails_for_an_email_that_already_has_an_account()
    {
        await using var db = _fixture.CreateContext();
        var service = CreateService(db);
        var email = $"{Guid.NewGuid():N}@example.test";

        await service.SignupAsync(ValidSignup(email));

        var act = () => service.SignupAsync(ValidSignup(email));

        await act.Should().ThrowAsync<CustomerEmailAlreadyExistsException>();
    }

    [Fact]
    public async Task Login_succeeds_with_the_correct_password()
    {
        await using var db = _fixture.CreateContext();
        var service = CreateService(db);
        var email = $"{Guid.NewGuid():N}@example.test";
        await service.SignupAsync(ValidSignup(email));

        var (response, refreshToken) = await service.LoginAsync(
            new CustomerLoginRequest { Email = email, Password = "correct-horse" });

        response.Email.Should().Be(email);
        refreshToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_fails_for_an_unknown_email()
    {
        await using var db = _fixture.CreateContext();
        var service = CreateService(db);

        var act = () => service.LoginAsync(
            new CustomerLoginRequest { Email = $"{Guid.NewGuid():N}@example.test", Password = "whatever1" });

        await act.Should().ThrowAsync<InvalidCustomerCredentialsException>();
    }

    [Fact]
    public async Task Login_fails_for_the_wrong_password()
    {
        await using var db = _fixture.CreateContext();
        var service = CreateService(db);
        var email = $"{Guid.NewGuid():N}@example.test";
        await service.SignupAsync(ValidSignup(email));

        var act = () => service.LoginAsync(new CustomerLoginRequest { Email = email, Password = "wrong-password" });

        await act.Should().ThrowAsync<InvalidCustomerCredentialsException>();
    }

    [Fact]
    public async Task An_exchange_code_can_only_be_redeemed_once()
    {
        await using var db = _fixture.CreateContext();
        var service = CreateService(db);
        var email = $"{Guid.NewGuid():N}@example.test";
        const string returnUrl = "https://storefront.example.test/auth/callback";

        var signup = ValidSignup(email);
        signup.ReturnUrl = returnUrl;
        var (response, _) = await service.SignupAsync(signup);
        var code = response.ExchangeCode ?? throw new InvalidOperationException("Expected an exchange code.");

        var first = await service.RedeemExchangeCodeAsync(code, returnUrl);
        first.Email.Should().Be(email);

        var act = () => service.RedeemExchangeCodeAsync(code, returnUrl);
        await act.Should().ThrowAsync<InvalidExchangeCodeException>("a code must not be redeemable twice");
    }

    [Fact]
    public async Task An_exchange_code_cannot_be_redeemed_against_a_different_returnUrl_than_it_was_minted_for()
    {
        await using var db = _fixture.CreateContext();
        var service = CreateService(db);
        var email = $"{Guid.NewGuid():N}@example.test";

        var signup = ValidSignup(email);
        signup.ReturnUrl = "https://storefront-a.example.test/auth/callback";
        var (response, _) = await service.SignupAsync(signup);
        var code = response.ExchangeCode ?? throw new InvalidOperationException("Expected an exchange code.");

        var act = () => service.RedeemExchangeCodeAsync(code, "https://storefront-b.example.test/auth/callback");

        await act.Should().ThrowAsync<InvalidExchangeCodeException>(
            "a code minted for one storefront must not be replayable against another");
    }

    [Fact]
    public async Task Redeeming_an_unknown_code_fails()
    {
        await using var db = _fixture.CreateContext();
        var service = CreateService(db);

        var act = () => service.RedeemExchangeCodeAsync("not-a-real-code", "https://storefront.example.test/auth/callback");

        await act.Should().ThrowAsync<InvalidExchangeCodeException>();
    }
}
