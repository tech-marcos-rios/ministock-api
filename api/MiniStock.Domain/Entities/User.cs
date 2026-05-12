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

    public bool IsRefreshTokenValid(string token) =>
        RefreshToken == token && RefreshTokenExpiresAt > DateTime.UtcNow;
}
