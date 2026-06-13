using AuctionNest.Domain.Common;

namespace AuctionNest.Domain.Events;

public sealed record AuctionEndedEvent(
    Guid EventId,
    DateTime OccurredAt,
    Guid AuctionId,
    Guid? WinnerId,
    decimal? WinningAmount,
    bool IsReserveMet) : IDomainEvent
{
    public static AuctionEndedEvent Create(Guid auctionId,
        Guid? winnerId, decimal? winningAmount, bool isReserveMet)
        => new(Guid.NewGuid(), DateTime.UtcNow,
            auctionId, winnerId, winningAmount, isReserveMet);
}