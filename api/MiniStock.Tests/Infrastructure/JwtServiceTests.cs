using System.IdentityModel.Tokens.Jwt;
using FluentAssertions;
using MiniStock.Domain.Entities;
using MiniStock.Infrastructure.Services;
using Moq;
using Microsoft.Extensions.Configuration;

namespace MiniStock.Tests.Infrastructure;

public class JwtServiceTests
{
    private static readonly Guid RoleId = Guid.NewGuid();

    private static JwtService BuildService(int accessMinutes = 60, int refreshDays = 7)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"]                = "clave-super-secreta-de-al-menos-32-caracteres-ok",
                ["Jwt:Issuer"]             = "test-issuer",
                ["Jwt:Audience"]           = "test-audience",
                ["Jwt:AccessTokenMinutes"] = accessMinutes.ToString(),
                ["Jwt:RefreshTokenDays"]   = refreshDays.ToString(),
            })
            .Build();
        return new JwtService(config);
    }

    private static User BuildUserWithRole(string roleName = "User")
    {
        var user = User.Create("Test", "test@test.com", "hash", RoleId);
        var role = Role.Create(roleName);
        typeof(User).GetProperty(nameof(User.Role))!.SetValue(user, role);
        return user;
    }

    [Fact]
    public void GenerateAccessToken_ReturnsValidJwt()
    {
        var service = BuildService();
        var user    = BuildUserWithRole();

        var token = service.GenerateAccessToken(user);

        token.Should().NotBeNullOrEmpty();
        var handler = new JwtSecurityTokenHandler();
        handler.CanReadToken(token).Should().BeTrue();
    }

    [Fact]
    public void GenerateAccessToken_ContainsExpectedClaims()
    {
        var service = BuildService();
        var user    = BuildUserWithRole("Admin");

        var token   = service.GenerateAccessToken(user);
        var handler = new JwtSecurityTokenHandler();
        var jwt     = handler.ReadJwtToken(token);

        jwt.Subject.Should().Be(user.Id.ToString());
        jwt.Claims.Should().Contain(c => c.Type == "email" && c.Value == "test@test.com");
        jwt.Claims.Should().Contain(c => c.Value == "Admin");
        jwt.Issuer.Should().Be("test-issuer");
    }

    [Fact]
    public void GenerateAccessToken_ExpiresAfterConfiguredMinutes()
    {
        var service = BuildService(accessMinutes: 30);
        var user    = BuildUserWithRole();
        var before  = DateTime.UtcNow.AddMinutes(29);
        var after   = DateTime.UtcNow.AddMinutes(31);

        var token   = service.GenerateAccessToken(user);
        var handler = new JwtSecurityTokenHandler();
        var jwt     = handler.ReadJwtToken(token);

        jwt.ValidTo.Should().BeAfter(before).And.BeBefore(after);
    }

    [Fact]
    public void GenerateRefreshToken_IsBase64AndSufficientLength()
    {
        var service = BuildService();
        var token   = service.GenerateRefreshToken();

        token.Should().NotBeNullOrEmpty();
        var bytes = Convert.FromBase64String(token); // throws if not valid Base64
        bytes.Should().HaveCount(64);                // 64 bytes de CSPRNG
    }

    [Fact]
    public void GenerateRefreshToken_TwoCallsProduceDifferentTokens()
    {
        var service = BuildService();
        var t1 = service.GenerateRefreshToken();
        var t2 = service.GenerateRefreshToken();
        t1.Should().NotBe(t2);
    }

    [Fact]
    public void AccessTokenExpiresAt_ReflectsConfiguredMinutes()
    {
        var service = BuildService(accessMinutes: 120);
        var before  = DateTime.UtcNow.AddMinutes(119);
        var after   = DateTime.UtcNow.AddMinutes(121);

        service.AccessTokenExpiresAt().Should().BeAfter(before).And.BeBefore(after);
    }

    [Fact]
    public void RefreshTokenExpiresAt_ReflectsConfiguredDays()
    {
        var service = BuildService(refreshDays: 14);
        var before  = DateTime.UtcNow.AddDays(13).AddHours(23);
        var after   = DateTime.UtcNow.AddDays(14).AddHours(1);

        service.RefreshTokenExpiresAt().Should().BeAfter(before).And.BeBefore(after);
    }

    [Fact]
    public void Constructor_MissingJwtKey_Throws()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>()) // sin Jwt:Key
            .Build();

        var act = () => new JwtService(config);

        act.Should().Throw<InvalidOperationException>().WithMessage("*Jwt:Key*");
    }

    [Fact]
    public void Constructor_ShortJwtKey_Throws()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "corta" // menos de 32 chars
            })
            .Build();

        var act = () => new JwtService(config);

        act.Should().Throw<InvalidOperationException>().WithMessage("*32 caracteres*");
    }
}
