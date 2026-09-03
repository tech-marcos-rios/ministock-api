using System.Security.Cryptography;
using System.Text;

namespace MiniStock.Domain.Entities;

public class User : BaseEntity
{
    public string Name { get; private set; } = default!;
    public string Email { get; private set; } = default!;
    public string PasswordHash { get; private set; } = default!;
    public Guid RoleId { get; private set; }
    public Role Role { get; private set; } = default!;
    public string? RefreshToken { get; private set; }
    public DateTime? RefreshTokenExpiresAt { get; private set; }

    private User() { }

    public static User Create(string name, string email, string passwordHash, Guid roleId) =>
        new() { Name = name, Email = email, PasswordHash = passwordHash, RoleId = roleId };

    public void SetRefreshToken(string token, DateTime expiresAt)
    {
        RefreshToken = token;
        RefreshTokenExpiresAt = expiresAt;
        SetUpdatedAt();
    }

    public void RevokeRefreshToken()
    {
        RefreshToken = null;
        RefreshTokenExpiresAt = null;
        SetUpdatedAt();
    }

    public bool IsRefreshTokenValid(string token)
    {
        if (RefreshToken is null || RefreshTokenExpiresAt is null)
            return false;

        // Comparación en tiempo constante — == de string corta apenas
        // encuentra el primer byte distinto, filtrando por timing cuánto
        // del token adivinó un atacante. El token ya tiene 512 bits de
        // entropía (CSPRNG), así que el riesgo real es bajo, pero es gratis
        // sacarlo del todo.
        var stored = Encoding.UTF8.GetBytes(RefreshToken);
        var provided = Encoding.UTF8.GetBytes(token);
        var isSameToken = stored.Length == provided.Length && CryptographicOperations.FixedTimeEquals(stored, provided);

        return isSameToken && RefreshTokenExpiresAt > DateTime.UtcNow;
    }
}
