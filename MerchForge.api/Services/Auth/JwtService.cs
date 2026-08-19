using MerchForge.api.Configurations;
using MerchForge.api.Exceptions.Auth;
using MerchForge.api.Models;
using MerchForge.api.Repositories.Interfaces;
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
    private readonly IUserRepository _userRepository;
    private readonly byte[] _signingKey;

    public JwtService(IOptions<JwtOptions> options, IUserRepository userRepository)
    {
        _options = options.Value;
        _userRepository = userRepository;

        if (string.IsNullOrWhiteSpace(_options.SecretKey))
        {
            throw new JwtConfigurationException();
        }

        _signingKey = Encoding.UTF8.GetBytes(_options.SecretKey);
    }

    public async Task<string> GenerateAccessToken(User user)
    {
        var expiration = GetExpirationTime();

        var systemRole = await _userRepository.GetSystemRoleById(user.SystemRoleId);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.Role, systemRole.ToString()),
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