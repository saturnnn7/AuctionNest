using System.Text.Json;
using AuctionNest.API.Hubs;
using AuctionNest.Domain.Entities;
using AuctionNest.Domain.Enums;
using AuctionNest.Domain.Events;
using AuctionNest.Infrastructure.Persistence;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace AuctionNest.API.BackgroundServices;

public sealed class OutboxProcessorService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxProcessorService> _logger;
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(5);

    // Maps EventType string to concrete type for deserialization
    private static readonly Dictionary<string, Type> EventTypes = new()
    {
        { nameof(BidPlacedEvent),       typeof(BidPlacedEvent) },
        { nameof(AuctionExtendedEvent), typeof(AuctionExtendedEvent) },
        { nameof(AuctionEndedEvent),    typeof(AuctionEndedEvent) },
        { nameof(AuctionStartedEvent),  typeof(AuctionStartedEvent) },
        { nameof(AuctionCancelledEvent),typeof(AuctionCancelledEvent) },
        { nameof(BuyItNowUsedEvent),    typeof(BuyItNowUsedEvent) },
    };

    public OutboxProcessorService(
        IServiceScopeFactory scopeFactory,
        ILogger<OutboxProcessorService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Outbox processor started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Outbox processor encountered an unexpected error.");
            }

            await Task.Delay(Interval, stoppingToken);
        }
    }

    private async Task ProcessBatchAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var hub = scope.ServiceProvider.GetRequiredService<IHubContext<AuctionHub>>();

        var messages = await context.OutboxMessages
            .Where(m => m.ProcessedAt == null && m.RetryCount < 3)
            .OrderBy(m => m.CreatedAt)
            .Take(20)
            .ToListAsync(ct);
        
        if (messages.Count == 0) return;

        foreach (var message in messages)
        {
            try
            {
                await DispatchAsync(message, hub, context, ct);
                message.MarkProcessed();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to process outbox message {Id} ({EventType}). Retry: {Retry}",
                    message.Id, message.EventType, message.RetryCount + 1);
                
                message.MarkFailed(ex.Message);
            }
        }

        await context.SaveChangesAsync(ct);
    }

    private async Task DispatchAsync(
        OutboxMessage message,
        IHubContext<AuctionHub> hub,
        AppDbContext context,
        CancellationToken ct)
    {
        if (!EventTypes.TryGetValue(message.EventType, out var eventType))
        {
            _logger.LogWarning("Unknown outbox event type: {EventType}", message.EventType);
            return;
        }

        var domainEvent = JsonSerializer.Deserialize(message.Payload, eventType)!;

        switch (domainEvent)
        {
            case BidPlacedEvent e:
                await HandleBidPlacedAsync(e, hub, context, ct);
                break;

            case AuctionExtendedEvent e:
                await HandleAuctionExtendedAsync(e, hub, ct);
                break;

            case AuctionEndedEvent e:
                await HandleAuctionEndedAsync(e, hub, context, ct);
                break;
        }
    }

    private static async Task HandleBidPlacedAsync(
        BidPlacedEvent e,
        IHubContext<AuctionHub> hub,
        AppDbContext context,
        CancellationToken ct)
    {
        // Push to everyone watching the auction room
        await hub.Clients.Group($"auction:{e.AuctionId}")
            .SendAsync("BidPlaced", new
            {
                e.AuctionId,
                e.BidderId,
                e.Amount,
                e.NewCurrentPrice,
                e.OccurredAt
            }, ct);

        // Notify the outbid user personally
        if (e.PreviousWinnerBidderId.HasValue)
        {
            var notification = Notification.Create(
                e.PreviousWinnerBidderId.Value,
                NotificationType.BidOutbid,
                "You've been outbid!",
                $"Someone placed a higher bid of {e.NewCurrentPrice:F2}.",
                payload: e.AuctionId.ToString());

            context.Notifications.Add(notification);

            await hub.Clients
                .Group($"user:{e.PreviousWinnerBidderId}")
                .SendAsync("NewNotification", new
                {
                    notification.Id,
                    notification.Title,
                    notification.Message,
                    Type = notification.Type.ToString()
                }, ct);
        }
    }

    private static Task HandleAuctionExtendedAsync(
        AuctionExtendedEvent e,
        IHubContext<AuctionHub> hub,
        CancellationToken ct)
        => hub.Clients.Group($"auction:{e.AuctionId}")
            .SendAsync("AuctionExtended", new
            {
                e.AuctionId,
                e.NewEndsAt,
                e.ExtensionCount
            }, ct);

    private static async Task HandleAuctionEndedAsync(
        AuctionEndedEvent e,
        IHubContext<AuctionHub> hub,
        AppDbContext context,
        CancellationToken ct)
    {
        // Push auction ended to the room
        await hub.Clients.Group($"auction:{e.AuctionId}")
            .SendAsync("AuctionEnded", new
            {
                e.AuctionId,
                e.WinnerId,
                e.WinningAmount,
                e.IsReserveMet
            }, ct);

        // Notify winner
        if (e.WinnerId.HasValue && e.IsReserveMet)
        {
            var winnerNotification = Notification.Create(
                e.WinnerId.Value,
                NotificationType.AuctionWon,
                "You won the auction!",
                $"Congratulations! Your bid of {e.WinningAmount:F2} won.",
                payload: e.AuctionId.ToString());

            context.Notifications.Add(winnerNotification);

            await hub.Clients
                .Group($"user:{e.WinnerId}")
                .SendAsync("NewNotification", new
                {
                    winnerNotification.Id,
                    winnerNotification.Title,
                    winnerNotification.Message,
                    Type = winnerNotification.Type.ToString()
                }, ct);
        }
    }
}