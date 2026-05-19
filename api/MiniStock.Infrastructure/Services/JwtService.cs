using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using MiniStock.Application.Interfaces;
using MiniStock.Domain.Entities;

namespace MiniStock.Infrastructure.Services;

public class JwtService : IJwtService
{
    private readonly string _key;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _accessTokenMinutes;
    private readonly int _refreshTokenDays;

    public JwtService(IConfiguration config)
    {
        _key = config["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key no configurada.");
        _issuer = config["Jwt:Issuer"] ?? "ministock-api";
        _audience = config["Jwt:Audience"] ?? "ministock-web";
        _accessTokenMinutes = int.TryParse(config["Jwt:AccessTokenMinutes"], out var m) ? m : 60;
        _refreshTokenDays = int.TryParse(config["Jwt:RefreshTokenDays"], out var d) ? d : 7;
    }

    public string GenerateAccessToken(User user)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Name, user.Name),
            new Claim(ClaimTypes.Role, user.Role.Name),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: AccessTokenExpiresAt(),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }

    public DateTime AccessTokenExpiresAt() => DateTime.UtcNow.AddMinutes(_accessTokenMinutes);
    public DateTime RefreshTokenExpiresAt() => DateTime.UtcNow.AddDays(_refreshTokenDays);
}
