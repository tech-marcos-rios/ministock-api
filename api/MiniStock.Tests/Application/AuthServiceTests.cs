using FluentAssertions;
using MiniStock.Application.DTOs.Auth;
using MiniStock.Application.Interfaces;
using MiniStock.Application.Services;
using MiniStock.Domain.Entities;
using Moq;

namespace MiniStock.Tests.Application;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IUnitOfWork>     _uow   = new();
    private readonly Mock<IJwtService>     _jwt   = new();
    private readonly AuthService           _sut;

    private static readonly Guid RoleId = new("00000000-0000-0000-0000-000000000002");

    public AuthServiceTests()
    {
        _sut = new AuthService(_users.Object, _uow.Object, _jwt.Object);

        // Defaults para JWT service
        _jwt.Setup(j => j.GenerateRefreshToken()).Returns("refresh-token-mock");
        _jwt.Setup(j => j.RefreshTokenExpiresAt()).Returns(DateTime.UtcNow.AddDays(7));
        _jwt.Setup(j => j.AccessTokenExpiresAt()).Returns(DateTime.UtcNow.AddMinutes(60));
        _jwt.Setup(j => j.GenerateAccessToken(It.IsAny<User>())).Returns("access-token-mock");
    }

    // ── Register ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Register_NewEmail_CreatesUserAndReturnsTokens()
    {
        _users.Setup(r => r.ExistsByEmailAsync("marcos@test.com", default)).ReturnsAsync(false);
        _users.Setup(r => r.AddAsync(It.IsAny<User>(), default)).Returns(Task.CompletedTask);
        _uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        // Simula el GetByIdAsync que hace el servicio después de guardar
        _users.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default))
              .ReturnsAsync((Guid id, CancellationToken _) =>
              {
                  var user = User.Create("Marcos", "marcos@test.com", "hash", RoleId);
                  SetRole(user, Role.Create(Role.Names.User));
                  return user;
              });

        var request = new RegisterRequest("Marcos", "marcos@test.com", "Password123!");
        var result  = await _sut.RegisterAsync(request, RoleId);

        result.IsSuccess.Should().BeTrue();
        result.Value!.AccessToken.Should().Be("access-token-mock");
        result.Value.Email.Should().Be("marcos@test.com");
    }

    [Fact]
    public async Task Register_DuplicateEmail_ReturnsFailure()
    {
        _users.Setup(r => r.ExistsByEmailAsync("marcos@test.com", default)).ReturnsAsync(true);

        var request = new RegisterRequest("Marcos", "marcos@test.com", "Password123!");
        var result  = await _sut.RegisterAsync(request, RoleId);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Ya existe");
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── Login ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Login_CorrectCredentials_ReturnsTokens()
    {
        var hash = BCrypt.Net.BCrypt.HashPassword("Pass123!");
        var user = User.Create("Admin", "admin@test.com", hash, RoleId);
        SetRole(user, Role.Create(Role.Names.User));

        _users.Setup(r => r.GetByEmailAsync("admin@test.com", default)).ReturnsAsync(user);
        _uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        var request = new LoginRequest("admin@test.com", "Pass123!");
        var result  = await _sut.LoginAsync(request);

        result.IsSuccess.Should().BeTrue();
        result.Value!.AccessToken.Should().Be("access-token-mock");
        result.Value.Email.Should().Be("admin@test.com");
    }

    [Fact]
    public async Task Login_WrongPassword_ReturnsFailure()
    {
        var hash = BCrypt.Net.BCrypt.HashPassword("CorrectPass!");
        var user = User.Create("Admin", "admin@test.com", hash, RoleId);

        _users.Setup(r => r.GetByEmailAsync("admin@test.com", default)).ReturnsAsync(user);

        var request = new LoginRequest("admin@test.com", "WrongPass!");
        var result  = await _sut.LoginAsync(request);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Credenciales incorrectas.");
    }

    [Fact]
    public async Task Login_UnknownEmail_ReturnsGenericFailure()
    {
        _users.Setup(r => r.GetByEmailAsync(It.IsAny<string>(), default)).ReturnsAsync((User?)null);

        var request = new LoginRequest("unknown@test.com", "Pass!");
        var result  = await _sut.LoginAsync(request);

        // El mensaje no debe revelar si el email existe o no
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Credenciales incorrectas.");
    }

    // ── Refresh ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Refresh_ValidToken_ReturnsNewTokens()
    {
        var user = User.Create("Admin", "admin@test.com", "hash", RoleId);
        SetRole(user, Role.Create(Role.Names.User));
        user.SetRefreshToken("valid-refresh", DateTime.UtcNow.AddDays(7));

        _users.Setup(r => r.GetByRefreshTokenAsync("valid-refresh", default)).ReturnsAsync(user);
        _uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        var request = new RefreshTokenRequest("valid-refresh");
        var result  = await _sut.RefreshAsync(request);

        result.IsSuccess.Should().BeTrue();
        result.Value!.AccessToken.Should().Be("access-token-mock");
    }

    [Fact]
    public async Task Refresh_ExpiredToken_ReturnsFailure()
    {
        var user = User.Create("Admin", "admin@test.com", "hash", RoleId);
        user.SetRefreshToken("expired-token", DateTime.UtcNow.AddSeconds(-1)); // expirado

        _users.Setup(r => r.GetByRefreshTokenAsync("expired-token", default)).ReturnsAsync(user);

        var request = new RefreshTokenRequest("expired-token");
        var result  = await _sut.RefreshAsync(request);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("inválido");
    }

    [Fact]
    public async Task Refresh_TokenNotFound_ReturnsFailure()
    {
        _users.Setup(r => r.GetByRefreshTokenAsync(It.IsAny<string>(), default)).ReturnsAsync((User?)null);

        var result = await _sut.RefreshAsync(new RefreshTokenRequest("nonexistent"));

        result.IsFailure.Should().BeTrue();
    }

    // ── Logout ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Logout_ValidUser_RevokesRefreshToken()
    {
        var user = User.Create("Admin", "admin@test.com", "hash", RoleId);
        user.SetRefreshToken("some-token", DateTime.UtcNow.AddDays(7));

        _users.Setup(r => r.GetByIdAsync(user.Id, default)).ReturnsAsync(user);
        _uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        var result = await _sut.LogoutAsync(user.Id);

        result.IsSuccess.Should().BeTrue();
        user.RefreshToken.Should().BeNull();
    }

    [Fact]
    public async Task Logout_UserNotFound_ReturnsFailure()
    {
        _users.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default)).ReturnsAsync((User?)null);

        var result = await _sut.LogoutAsync(Guid.NewGuid());

        result.IsFailure.Should().BeTrue();
    }

    // ── Helper ────────────────────────────────────────────────────────────────

    private static void SetRole(User user, Role role)
    {
        typeof(User).GetProperty(nameof(User.Role))!.SetValue(user, role);
    }
}
