using MerchForge.api.Configurations;
using MerchForge.api.Exceptions.Auth;
using MerchForge.api.Models;
using MerchForge.api.Services.Auth.interfaces;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace MerchForge.api.Services.Auth;

public class JwtService : IJwtService
{
    private readonly JwtOptions _options;
    private readonly byte[] _signingKey;

    public JwtService(IOptions<JwtOptions> options)
    {
        _options = options.Value;

        if (string.IsNullOrWhiteSpace(_options.SecretKey))
        {
            throw new JwtConfigurationException();
        }

        _signingKey = Encoding.UTF8.GetBytes(_options.SecretKey);
    }

    public string GenerateAccessToken(User user)
    {
        var expiration = GetExpirationTime();

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.Role, user.SystemRole.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email)
        };

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(_signingKey),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: expiration,
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public DateTime GetExpirationTime()
    {
        return DateTime.UtcNow.AddMinutes(
            _options.AccessTokenExpirationMinutes);
    }
}