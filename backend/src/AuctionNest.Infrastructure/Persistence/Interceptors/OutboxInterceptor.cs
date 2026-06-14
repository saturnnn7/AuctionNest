using System.Text.Json;
using AuctionNest.Domain.Common;
using AuctionNest.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace AuctionNest.Infrastructure.Persistence.Interceptors;

// Intercepts SaveChanges, collects all raised Domain Events,
// stores them as OutboxMessage in the SAME transaction with business data.
// A Hangfire job reads OutboxMessages and pushes them to SignalR + creates Notifications.
public sealed class OutboxInterceptor : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is null) return base.SavingChangesAsync(eventData, result, cancellationToken);

        var domainEvents = eventData.Context.ChangeTracker
            .Entries<Entity>()
            .SelectMany(e => e.Entity.GetDomainEvents())
            .ToList();

        foreach (var domainEvent in domainEvents)
        {
            var outboxMessage = OutboxMessage.Create(
                domainEvent.GetType().Name,
                JsonSerializer.Serialize(domainEvent, domainEvent.GetType()));

            eventData.Context.Set<OutboxMessage>().Add(outboxMessage);
        }

        // Clearing events. They're already in the Outbox.
        eventData.Context.ChangeTracker
            .Entries<Entity>()
            .ToList()
            .ForEach(e => e.Entity.ClearDomainEvents());

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}