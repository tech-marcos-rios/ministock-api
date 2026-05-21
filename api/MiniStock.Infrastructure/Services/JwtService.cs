using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using MiniStock.Application.Interfaces;
using MiniStock.Domain.Entities;

namespace MiniStock.Infrastructure.Services;

/// <summary>
/// Implementación de <see cref="IJwtService"/> que genera y configura tokens JWT
/// para la autenticación de la API.
/// </summary>
/// <remarks>
/// <b>Access token (JWT):</b> token firmado con HMAC-SHA256, vida corta (default 60 min),
/// enviado en el header <c>Authorization: Bearer</c> en cada request. Stateless: el servidor
/// no necesita consultar la BD para validarlo.
///
/// <b>Refresh token:</b> 64 bytes aleatorios generados con <see cref="RandomNumberGenerator"/>
/// (CSPRNG del sistema operativo, no <c>Random</c>). Vida larga (default 7 días).
/// Se almacena en la BD para permitir su revocación (logout). Permite emitir nuevos
/// access tokens sin que el usuario vuelva a hacer login.
///
/// <b>Claims incluidos en el JWT:</b>
/// <list type="bullet">
///   <item><c>sub</c> — ID del usuario (Guid). Usado para extraer <c>userId</c> en los controllers.</item>
///   <item><c>email</c> — Email del usuario.</item>
///   <item><c>name</c> — Nombre para mostrar.</item>
///   <item><c>role</c> — Rol (Admin/User) para autorización basada en roles.</item>
///   <item><c>jti</c> — ID único del token. Permite invalidar tokens específicos si se implementa una blocklist.</item>
/// </list>
///
/// <b>Configuración:</b> todos los parámetros se leen de <c>appsettings.json</c> / variables
/// de entorno para no hardcodear nada sensible en código.
/// </remarks>
public class JwtService : IJwtService
{
    private readonly string _key;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _accessTokenMinutes;
    private readonly int _refreshTokenDays;

    public JwtService(IConfiguration config)
    {
        _key                = config["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key no configurada.");
        _issuer             = config["Jwt:Issuer"] ?? "ministock-api";
        _audience           = config["Jwt:Audience"] ?? "ministock-web";
        _accessTokenMinutes = int.TryParse(config["Jwt:AccessTokenMinutes"], out var m) ? m : 60;
        _refreshTokenDays   = int.TryParse(config["Jwt:RefreshTokenDays"], out var d) ? d : 7;
    }

    /// <inheritdoc />
    public string GenerateAccessToken(User user)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub,   user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Name,  user.Name),
            new Claim(ClaimTypes.Role,               user.Role.Name),
            new Claim(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString())
        };

        var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer:            _issuer,
            audience:          _audience,
            claims:            claims,
            expires:           AccessTokenExpiresAt(),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Usa <see cref="RandomNumberGenerator"/> (CSPRNG) en lugar de <c>Random</c> o <c>Guid</c>
    /// porque estos últimos son predecibles. 64 bytes en Base64 da 512 bits de entropía,
    /// suficiente para resistir ataques de fuerza bruta.
    /// </remarks>
    public string GenerateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }

    /// <inheritdoc />
    public DateTime AccessTokenExpiresAt()  => DateTime.UtcNow.AddMinutes(_accessTokenMinutes);

    /// <inheritdoc />
    public DateTime RefreshTokenExpiresAt() => DateTime.UtcNow.AddDays(_refreshTokenDays);
}
