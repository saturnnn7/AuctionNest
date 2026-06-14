using AuctionNest.Application.Common.Interfaces.Repositories;
using AuctionNest.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AuctionNest.Infrastructure.Persistence.Repositories;

public sealed class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(AppDbContext context) : base(context) { }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
        => await DbSet.FirstOrDefaultAsync(u => u.Email == email.ToLower(), ct);

    public async Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default)
        => await DbSet.FirstOrDefaultAsync(u => u.Username == username.ToLower(), ct);

    public async Task<bool> EmailExistsAsync(string email, CancellationToken ct = default)
        => await DbSet.AnyAsync(u => u.Email == email.ToLower(), ct);

    public async Task<bool> UsernameExistsAsync(string username, CancellationToken ct = default)
        => await DbSet.AnyAsync(u => u.Username == username.ToLower(), ct);
}