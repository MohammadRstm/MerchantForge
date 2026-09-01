using MerchForge.api.Data;
using MerchForge.api.DTOs.CustomerAuth;
using MerchForge.api.Enums;
using MerchForge.api.Exceptions.CustomerAuth;
using MerchForge.api.Models;
using MerchForge.api.Repositories.Interfaces;
using MerchForge.api.Services.Audit.interfaces;
using MerchForge.api.Services.CustomerAuth.interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace MerchForge.api.Services.CustomerAuth;

public class CustomerAuthService : ICustomerAuthService
{
    /// <summary>Deliberately short — this code only ever needs to survive one immediate redirect.</summary>
    private const int ExchangeCodeExpirationSeconds = 60;

    private readonly ICustomerRepository _customerRepository;
    private readonly IPasswordHasher<Customer> _passwordHasher;
    private readonly ICustomerJwtService _customerJwtService;
    private readonly ICustomerRefreshTokenService _customerRefreshTokenService;
    private readonly IAuditLogService _auditLogService;

    // CustomerExchangeCode has no repository of its own, deliberately — same precedent
    // as InvitationService, which talks to the db context directly for this exact kind
    // of "hash-and-store a single-use opaque token" logic rather than adding a
    // repository layer around it.
    private readonly MerchForgeDbContext _db;
    private readonly ILogger<CustomerAuthService> _logger;

    public CustomerAuthService(
        ICustomerRepository customerRepository,
        IPasswordHasher<Customer> passwordHasher,
        ICustomerJwtService customerJwtService,
        ICustomerRefreshTokenService customerRefreshTokenService,
        IAuditLogService auditLogService,
        MerchForgeDbContext db,
        ILogger<CustomerAuthService> logger)
    {
        _customerRepository = customerRepository;
        _passwordHasher = passwordHasher;
        _customerJwtService = customerJwtService;
        _customerRefreshTokenService = customerRefreshTokenService;
        _auditLogService = auditLogService;
        _db = db;
        _logger = logger;
    }

    public async Task<(CustomerSessionResponse Response, string RefreshToken)> SignupAsync(
        CustomerSignupRequest request,
        CancellationToken cancellationToken = default)
    {
        if (await _customerRepository.GetByEmailAsync(request.Email, cancellationToken) is not null)
        {
            throw new CustomerEmailAlreadyExistsException();
        }

        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        customer.PasswordHash = _passwordHasher.HashPassword(customer, request.Password);

        await _customerRepository.AddAsync(customer, cancellationToken);

        var (refreshToken, _) = await _customerRefreshTokenService.CreateAsync(customer, cancellationToken);

        await _auditLogService.LogAsync(
            AuditEventType.Authentication, "CustomerRegistered", $"{customer.FirstName} {customer.LastName} registered.",
            success: true, actorUserId: null, actorDisplayNameOverride: $"{customer.FirstName} {customer.LastName}",
            entityType: "Customer", entityId: customer.Id, cancellationToken: cancellationToken);

        var response = await BuildSessionResponseAsync(customer, request.ReturnUrl, cancellationToken);

        return (response, refreshToken);
    }

    public async Task<(CustomerSessionResponse Response, string RefreshToken)> LoginAsync(
        CustomerLoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var customer = await _customerRepository.GetByEmailAsync(request.Email, cancellationToken);

        if (customer is null)
        {
            _logger.LogWarning("Failed customer login attempt for {Email}.", request.Email);
            await _auditLogService.LogAsync(
                AuditEventType.Authentication, "CustomerLoginFailed", $"Failed customer login attempt for {request.Email}.",
                success: false, actorUserId: null, actorDisplayNameOverride: request.Email,
                cancellationToken: cancellationToken);
            throw new InvalidCustomerCredentialsException();
        }

        var result = _passwordHasher.VerifyHashedPassword(customer, customer.PasswordHash, request.Password);

        if (result == PasswordVerificationResult.Failed)
        {
            _logger.LogWarning("Failed customer login attempt for {Email}.", request.Email);
            await _auditLogService.LogAsync(
                AuditEventType.Authentication, "CustomerLoginFailed", $"Failed customer login attempt for {request.Email}.",
                success: false, actorUserId: null, actorDisplayNameOverride: $"{customer.FirstName} {customer.LastName}",
                entityType: "Customer", entityId: customer.Id, cancellationToken: cancellationToken);
            throw new InvalidCustomerCredentialsException();
        }

        _logger.LogInformation("Customer login succeeded for {Email}.", request.Email);
        await _auditLogService.LogAsync(
            AuditEventType.Authentication, "CustomerLoginSucceeded", $"{customer.FirstName} {customer.LastName} logged in.",
            success: true, actorUserId: null, actorDisplayNameOverride: $"{customer.FirstName} {customer.LastName}",
            entityType: "Customer", entityId: customer.Id, cancellationToken: cancellationToken);

        var (refreshToken, _) = await _customerRefreshTokenService.CreateAsync(customer, cancellationToken);

        var response = await BuildSessionResponseAsync(customer, request.ReturnUrl, cancellationToken);

        return (response, refreshToken);
    }

    public async Task<(CustomerSessionResponse Response, string RefreshToken)> RefreshAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        var tokenEntity = await _customerRefreshTokenService.GetValidTokenAsync(refreshToken, cancellationToken);

        if (tokenEntity is null)
        {
            _logger.LogWarning("Customer refresh attempted with an invalid or expired refresh token.");
            throw new InvalidCustomerRefreshTokenException();
        }

        var (newRefreshToken, _) = await _customerRefreshTokenService.RotateAsync(tokenEntity, cancellationToken);

        var response = await BuildSessionResponseAsync(tokenEntity.Customer, returnUrl: null, cancellationToken);

        return (response, newRefreshToken);
    }

    public async Task LogoutAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        var tokenEntity = await _customerRefreshTokenService.GetValidTokenAsync(refreshToken, cancellationToken);

        if (tokenEntity is null)
        {
            return;
        }

        await _customerRefreshTokenService.RevokeAsync(tokenEntity, cancellationToken);

        _logger.LogInformation("Customer session revoked (logout) for customer {CustomerId}.", tokenEntity.CustomerId);
        await _auditLogService.LogAsync(
            AuditEventType.Authentication, "CustomerLogout", "Customer logged out.",
            success: true, actorUserId: null, actorDisplayNameOverride: $"{tokenEntity.Customer.FirstName} {tokenEntity.Customer.LastName}",
            entityType: "Customer", entityId: tokenEntity.CustomerId, cancellationToken: cancellationToken);
    }

    public async Task<CustomerSessionResponse> RedeemExchangeCodeAsync(
        string code,
        string returnUrl,
        CancellationToken cancellationToken = default)
    {
        var codeHash = HashOpaqueValue(code);

        // Atomically claim the code: only succeeds if it's still unused, unexpired, and
        // was minted for this exact returnUrl. Closes the race window where two
        // concurrent redemptions of the same code could both pass validation, and is
        // what makes the code non-replayable against a different storefront than the
        // one it was issued to.
        var claimed = await _db.CustomerExchangeCodes
            .Where(c =>
                c.CodeHash == codeHash &&
                c.ReturnUrl == returnUrl &&
                c.UsedAt == null &&
                c.ExpiresAt > DateTime.UtcNow)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(c => c.UsedAt, DateTime.UtcNow),
                cancellationToken);

        if (claimed == 0)
        {
            throw new InvalidExchangeCodeException();
        }

        var exchangeCode = await _db.CustomerExchangeCodes
            .Include(c => c.Customer)
            .FirstAsync(c => c.CodeHash == codeHash, cancellationToken);

        // No ReturnUrl passed through here: redeeming a code must never itself mint
        // another one, and this response is cross-origin/cookie-free by construction
        // (see the interface doc comment), so there is nothing further to hand back
        // beyond the access token and customer identity.
        return await BuildSessionResponseAsync(exchangeCode.Customer, returnUrl: null, cancellationToken);
    }

    public async Task<CustomerProfileResponse> GetProfileAsync(
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        var customer = await _customerRepository.GetByIdAsync(customerId, cancellationToken)
            ?? throw new CustomerNotFoundException();

        return MapToProfileResponse(customer);
    }

    public async Task<CustomerProfileResponse> UpdateProfileAsync(
        Guid customerId,
        UpdateCustomerProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        var customer = await _customerRepository.GetByIdAsync(customerId, cancellationToken)
            ?? throw new CustomerNotFoundException();

        customer.FirstName = request.FirstName;
        customer.LastName = request.LastName;
        customer.Phone = request.Phone;
        customer.AddressLine1 = request.AddressLine1;
        customer.AddressLine2 = request.AddressLine2;
        customer.City = request.City;
        customer.State = request.State;
        customer.PostalCode = request.PostalCode;
        customer.Country = request.Country;
        customer.UpdatedAt = DateTime.UtcNow;

        await _customerRepository.UpdateAsync(customer, cancellationToken);

        return MapToProfileResponse(customer);
    }

    private async Task<CustomerSessionResponse> BuildSessionResponseAsync(
        Customer customer,
        string? returnUrl,
        CancellationToken cancellationToken)
    {
        string? exchangeCode = null;

        if (!string.IsNullOrWhiteSpace(returnUrl))
        {
            exchangeCode = await CreateExchangeCodeAsync(customer, returnUrl, cancellationToken);
        }

        return new CustomerSessionResponse
        {
            AuthResponse = new DTOs.CustomerAuth.CustomerAuthResponse
            {
                AccessToken = _customerJwtService.GenerateAccessToken(customer),
                AccessTokenExpiresAt = _customerJwtService.GetExpirationTime(),
            },
            CustomerId = customer.Id,
            Email = customer.Email,
            FirstName = customer.FirstName,
            LastName = customer.LastName,
            ExchangeCode = exchangeCode,
        };
    }

    private async Task<string> CreateExchangeCodeAsync(
        Customer customer,
        string returnUrl,
        CancellationToken cancellationToken)
    {
        var rawCode = GenerateOpaqueValue();

        var exchangeCode = new CustomerExchangeCode
        {
            Id = Guid.NewGuid(),
            CustomerId = customer.Id,
            CodeHash = HashOpaqueValue(rawCode),
            ReturnUrl = returnUrl,
            ExpiresAt = DateTime.UtcNow.AddSeconds(ExchangeCodeExpirationSeconds),
            CreatedAt = DateTime.UtcNow,
        };

        await _db.CustomerExchangeCodes.AddAsync(exchangeCode, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        return rawCode;
    }

    private static CustomerProfileResponse MapToProfileResponse(Customer customer)
    {
        return new CustomerProfileResponse
        {
            Id = customer.Id,
            Email = customer.Email,
            FirstName = customer.FirstName,
            LastName = customer.LastName,
            Phone = customer.Phone,
            AddressLine1 = customer.AddressLine1,
            AddressLine2 = customer.AddressLine2,
            City = customer.City,
            State = customer.State,
            PostalCode = customer.PostalCode,
            Country = customer.Country,
        };
    }

    private static string GenerateOpaqueValue()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
    }

    private static string HashOpaqueValue(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes);
    }
}
