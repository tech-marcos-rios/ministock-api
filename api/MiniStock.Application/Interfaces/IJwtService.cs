using MiniStock.Domain.Entities;

namespace MiniStock.Application.Interfaces;

public interface IJwtService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
    DateTime AccessTokenExpiresAt();
    DateTime RefreshTokenExpiresAt();
}
