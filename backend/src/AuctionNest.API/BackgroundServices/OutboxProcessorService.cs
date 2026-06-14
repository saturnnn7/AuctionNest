using System.Text.Json;
using AuctionNest.API.Hubs;
using AuctionNest.Application.Common.Interfaces.Services;
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

    private static readonly Dictionary<string, Type> EventTypes = new()
    {
        { nameof(BidPlacedEvent),        typeof(BidPlacedEvent) },
        { nameof(AuctionExtendedEvent),  typeof(AuctionExtendedEvent) },
        { nameof(AuctionEndedEvent),     typeof(AuctionEndedEvent) },
        { nameof(AuctionStartedEvent),   typeof(AuctionStartedEvent) },
        { nameof(AuctionCancelledEvent), typeof(AuctionCancelledEvent) },
        { nameof(BuyItNowUsedEvent),     typeof(BuyItNowUsedEvent) },
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

        var context      = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var hub          = scope.ServiceProvider.GetRequiredService<IHubContext<AuctionHub>>();
        var jobScheduler = scope.ServiceProvider.GetRequiredService<IJobScheduler>();

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
                await DispatchAsync(message, hub, context, jobScheduler, ct);
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
        IJobScheduler jobScheduler,
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
                await HandleAuctionExtendedAsync(e, hub, jobScheduler, ct);
                break;

            case AuctionEndedEvent e:
                await HandleAuctionEndedAsync(e, hub, context, ct);
                break;

            case AuctionStartedEvent e:
                await HandleAuctionStartedAsync(e, hub, ct);
                break;

            case AuctionCancelledEvent e:
                await HandleAuctionCancelledAsync(e, hub, ct);
                break;
        }
    }

    // ----- Handlers -----

    private static async Task HandleBidPlacedAsync(
        BidPlacedEvent e,
        IHubContext<AuctionHub> hub,
        AppDbContext context,
        CancellationToken ct)
    {
        // Notify everyone in the auction room
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

    private static async Task HandleAuctionExtendedAsync(
        AuctionExtendedEvent e,
        IHubContext<AuctionHub> hub,
        IJobScheduler jobScheduler,
        CancellationToken ct)
    {
        // Notify everyone in the room about the new end time
        await hub.Clients.Group($"auction:{e.AuctionId}")
            .SendAsync("AuctionExtended", new
            {
                e.AuctionId,
                e.NewEndsAt,
                e.ExtensionCount
            }, ct);

        // Reschedule the end job — old job will detect extension and skip itself
        jobScheduler.ScheduleAuctionEnd(e.AuctionId, e.NewEndsAt);
    }

    private static async Task HandleAuctionEndedAsync(
        AuctionEndedEvent e,
        IHubContext<AuctionHub> hub,
        AppDbContext context,
        CancellationToken ct)
    {
        // Notify everyone in the room
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

        // Notify seller — reserve not met means no sale
        if (!e.IsReserveMet)
        {
            var auction = await context.Auctions.FindAsync([e.AuctionId], ct);
            if (auction is not null)
            {
                var sellerNotification = Notification.Create(
                    auction.SellerId,
                    NotificationType.AuctionEndedSeller,
                    "Auction ended — reserve not met",
                    "Your auction ended but the reserve price was not reached.",
                    payload: e.AuctionId.ToString());

                context.Notifications.Add(sellerNotification);

                await hub.Clients
                    .Group($"user:{auction.SellerId}")
                    .SendAsync("NewNotification", new
                    {
                        sellerNotification.Id,
                        sellerNotification.Title,
                        sellerNotification.Message,
                        Type = sellerNotification.Type.ToString()
                    }, ct);
            }
        }
    }

    private static Task HandleAuctionStartedAsync(
        AuctionStartedEvent e,
        IHubContext<AuctionHub> hub,
        CancellationToken ct)
        => hub.Clients.Group($"auction:{e.AuctionId}")
            .SendAsync("AuctionStarted", new { e.AuctionId, e.OccurredAt }, ct);

    private static Task HandleAuctionCancelledAsync(
        AuctionCancelledEvent e,
        IHubContext<AuctionHub> hub,
        CancellationToken ct)
        => hub.Clients.Group($"auction:{e.AuctionId}")
            .SendAsync("AuctionCancelled", new { e.AuctionId, e.OccurredAt }, ct);
}