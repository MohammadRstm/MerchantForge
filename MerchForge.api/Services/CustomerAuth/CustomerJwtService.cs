using MerchForge.api.Configurations;
using MerchForge.api.Exceptions.Auth;
using MerchForge.api.Models;
using MerchForge.api.Services.CustomerAuth.interfaces;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace MerchForge.api.Services.CustomerAuth;

/// <summary>
/// Deliberately reuses the platform's existing Jwt:SecretKey/Issuer (see
/// CustomerJwtOptions's own doc comment) — the distinct "Customer" scheme name plus
/// this distinct Audience are what stop a customer token from being accepted by any
/// owner/admin policy, not a distinct secret. Claims deliberately carry no role: a
/// Customer has no SystemRole, unlike User.
/// </summary>
public class CustomerJwtService : ICustomerJwtService
{
    private readonly JwtOptions _jwtOptions;
    private readonly CustomerJwtOptions _customerJwtOptions;
    private readonly byte[] _signingKey;

    public CustomerJwtService(
        IOptions<JwtOptions> jwtOptions,
        IOptions<CustomerJwtOptions> customerJwtOptions)
    {
        _jwtOptions = jwtOptions.Value;
        _customerJwtOptions = customerJwtOptions.Value;

        if (string.IsNullOrWhiteSpace(_jwtOptions.SecretKey))
        {
            throw new JwtConfigurationException();
        }

        _signingKey = Encoding.UTF8.GetBytes(_jwtOptions.SecretKey);
    }

    public string GenerateAccessToken(Customer customer)
    {
        var expiration = GetExpirationTime();

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, customer.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, customer.Email),
            new(ClaimTypes.NameIdentifier, customer.Id.ToString()),
            new(ClaimTypes.Email, customer.Email)
        };

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(_signingKey),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _customerJwtOptions.Audience,
            claims: claims,
            expires: expiration,
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public DateTime GetExpirationTime()
    {
        return DateTime.UtcNow.AddMinutes(
            _customerJwtOptions.AccessTokenExpirationMinutes);
    }
}
