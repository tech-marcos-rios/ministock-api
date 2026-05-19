using Microsoft.EntityFrameworkCore;
using MiniStock.Application.Interfaces;
using MiniStock.Domain.Entities;

namespace MiniStock.Infrastructure.Persistence.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context) => _context = context;

    public Task<User?> GetByIdAsync(Guid id, CancellationToken ct) =>
        _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == id, ct);

    public Task<User?> GetByEmailAsync(string email, CancellationToken ct) =>
        _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Email == email, ct);

    public Task<User?> GetByRefreshTokenAsync(string refreshToken, CancellationToken ct) =>
        _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.RefreshToken == refreshToken, ct);

    public Task<bool> ExistsByEmailAsync(string email, CancellationToken ct) =>
        _context.Users.AnyAsync(u => u.Email == email, ct);

    public async Task AddAsync(User user, CancellationToken ct) =>
        await _context.Users.AddAsync(user, ct);

    public void Update(User user) =>
        _context.Users.Update(user);
}
