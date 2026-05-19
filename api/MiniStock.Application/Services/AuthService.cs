using MiniStock.Application.Common;
using MiniStock.Application.DTOs.Auth;
using MiniStock.Application.Interfaces;
using MiniStock.Domain.Entities;

namespace MiniStock.Application.Services;

public class AuthService
{
    private readonly IUserRepository _users;
    private readonly IUnitOfWork _uow;
    private readonly IJwtService _jwt;

    public AuthService(IUserRepository users, IUnitOfWork uow, IJwtService jwt)
    {
        _users = users;
        _uow = uow;
        _jwt = jwt;
    }

    public async Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request, Guid defaultRoleId, CancellationToken ct = default)
    {
        if (await _users.ExistsByEmailAsync(request.Email, ct))
            return Result.Failure<AuthResponse>("Ya existe una cuenta con ese email.");

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
        var user = User.Create(request.Name, request.Email, passwordHash, defaultRoleId);

        var refreshToken = _jwt.GenerateRefreshToken();
        user.SetRefreshToken(refreshToken, _jwt.RefreshTokenExpiresAt());

        await _users.AddAsync(user, ct);
        await _uow.SaveChangesAsync(ct);

        // Recargamos para obtener la navegación Role (necesaria en BuildResponse)
        var saved = await _users.GetByIdAsync(user.Id, ct);
        var accessToken = _jwt.GenerateAccessToken(saved!);
        return Result.Success(BuildResponse(saved!, accessToken, refreshToken));
    }

    public async Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var user = await _users.GetByEmailAsync(request.Email, ct);

        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Result.Failure<AuthResponse>("Credenciales incorrectas.");

        var accessToken = _jwt.GenerateAccessToken(user);
        var refreshToken = _jwt.GenerateRefreshToken();
        user.SetRefreshToken(refreshToken, _jwt.RefreshTokenExpiresAt());

        _users.Update(user);
        await _uow.SaveChangesAsync(ct);

        return Result.Success(BuildResponse(user, accessToken, refreshToken));
    }

    public async Task<Result<AuthResponse>> RefreshAsync(RefreshTokenRequest request, CancellationToken ct = default)
    {
        var user = await _users.GetByRefreshTokenAsync(request.RefreshToken, ct);

        if (user is null || !user.IsRefreshTokenValid(request.RefreshToken))
            return Result.Failure<AuthResponse>("Refresh token inválido o expirado.");

        var accessToken = _jwt.GenerateAccessToken(user);
        var newRefreshToken = _jwt.GenerateRefreshToken();
        user.SetRefreshToken(newRefreshToken, _jwt.RefreshTokenExpiresAt());

        _users.Update(user);
        await _uow.SaveChangesAsync(ct);

        return Result.Success(BuildResponse(user, accessToken, newRefreshToken));
    }

    public async Task<Result> LogoutAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _users.GetByIdAsync(userId, ct);
        if (user is null) return Result.Failure("Usuario no encontrado.");

        user.RevokeRefreshToken();
        _users.Update(user);
        await _uow.SaveChangesAsync(ct);

        return Result.Success();
    }

    private AuthResponse BuildResponse(User user, string accessToken, string refreshToken) =>
        new(accessToken, refreshToken, _jwt.AccessTokenExpiresAt(), user.Name, user.Email, user.Role.Name);
}
