using FluentAssertions;
using MiniStock.Domain.Entities;

namespace MiniStock.Tests.Domain;

public class UserTests
{
    private static readonly Guid RoleId = Guid.NewGuid();

    [Fact]
    public void Create_SetsProperties()
    {
        var user = User.Create("Marcos", "marcos@test.com", "hash", RoleId);
        user.Name.Should().Be("Marcos");
        user.Email.Should().Be("marcos@test.com");
        user.PasswordHash.Should().Be("hash");
        user.RoleId.Should().Be(RoleId);
        user.RefreshToken.Should().BeNull();
        user.RefreshTokenExpiresAt.Should().BeNull();
    }

    [Fact]
    public void SetRefreshToken_StoresTokenAndExpiry()
    {
        var user = User.Create("U", "u@test.com", "h", RoleId);
        var expiry = DateTime.UtcNow.AddDays(7);
        user.SetRefreshToken("mytoken", expiry);
        user.RefreshToken.Should().Be("mytoken");
        user.RefreshTokenExpiresAt.Should().BeCloseTo(expiry, TimeSpan.FromSeconds(1));
        user.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void RevokeRefreshToken_ClearsTokenAndExpiry()
    {
        var user = User.Create("U", "u@test.com", "h", RoleId);
        user.SetRefreshToken("mytoken", DateTime.UtcNow.AddDays(7));
        user.RevokeRefreshToken();
        user.RefreshToken.Should().BeNull();
        user.RefreshTokenExpiresAt.Should().BeNull();
    }

    [Fact]
    public void IsRefreshTokenValid_ReturnsTrueWhenTokenMatchesAndNotExpired()
    {
        var user = User.Create("U", "u@test.com", "h", RoleId);
        user.SetRefreshToken("validtoken", DateTime.UtcNow.AddDays(7));
        user.IsRefreshTokenValid("validtoken").Should().BeTrue();
    }

    [Fact]
    public void IsRefreshTokenValid_ReturnsFalseWhenTokenMismatch()
    {
        var user = User.Create("U", "u@test.com", "h", RoleId);
        user.SetRefreshToken("correcttoken", DateTime.UtcNow.AddDays(7));
        user.IsRefreshTokenValid("wrongtoken").Should().BeFalse();
    }

    [Fact]
    public void IsRefreshTokenValid_ReturnsFalseWhenExpired()
    {
        var user = User.Create("U", "u@test.com", "h", RoleId);
        user.SetRefreshToken("expiredtoken", DateTime.UtcNow.AddSeconds(-1));
        user.IsRefreshTokenValid("expiredtoken").Should().BeFalse();
    }
}
