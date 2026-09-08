using MiniStock.Application.Common;
using MiniStock.Application.DTOs.Auth;

namespace MiniStock.Application.Interfaces;

public interface IAuthService
{
    Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken ct = default);

    Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default);

    Task<Result<AuthResponse>> RefreshAsync(RefreshTokenRequest request, CancellationToken ct = default);

    Task<Result> LogoutAsync(Guid userId, CancellationToken ct = default);
}
