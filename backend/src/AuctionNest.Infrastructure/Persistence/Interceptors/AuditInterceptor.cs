using AuctionNest.Application.Common.Interfaces.Services;
using AuctionNest.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace AuctionNest.Infrastructure.Persistence.Interceptors;

public sealed class AuditInterceptor : SaveChangesInterceptor
{
    private readonly IDateTimeProvider _dateTime;

    public AuditInterceptor(IDateTimeProvider dateTime) => _dateTime = dateTime;

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is null) return base.SavingChangesAsync(eventData, result, cancellationToken);

        var now = _dateTime.UtcNow;

        foreach (var entry in eventData.Context.ChangeTracker.Entries<Entity>())
        {
            if (entry.State == EntityState.Added)
                entry.Entity.CreatedAt = now;

            if (entry.State is EntityState.Added or EntityState.Modified)
                entry.Entity.UpdatedAt = now;
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}