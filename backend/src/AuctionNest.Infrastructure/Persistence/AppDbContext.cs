using AuctionNest.Domain.Entities;
using AuctionNest.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;

namespace AuctionNest.Infrastructure.Persistence;

public sealed class AppDbContext : DbContext
{
    private readonly AuditInterceptor _auditInterceptor;
    private readonly OutboxInterceptor _outboxInterceptor;

    public AppDbContext(
        DbContextOptions<AppDbContext> options,
        AuditInterceptor auditInterceptor,
        OutboxInterceptor outboxInterceptor) : base(options)
    {
        _auditInterceptor = auditInterceptor;
        _outboxInterceptor = outboxInterceptor;
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Auction> Auctions => Set<Auction>();
    public DbSet<Bid> Bids => Set<Bid>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<WatchList> WatchLists => Set<WatchList>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder
            .AddInterceptors(_auditInterceptor, _outboxInterceptor);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}