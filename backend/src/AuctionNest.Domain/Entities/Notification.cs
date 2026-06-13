using AuctionNest.Domain.Common;
using AuctionNest.Domain.Enums;

namespace AuctionNest.Domain.Entities;

public sealed class Notification : Entity
{
    public Guid UserId { get; private set; }
    public NotificationType Type { get; private set; }
    public string Title { get; private set; } = null!;
    public string Message { get; private set; } = null!;
    public string? Payload { get; private set; }
    public bool IsRead { get; private set; }

    public User User { get; private set; } = null!;

    public static Notification Create(
        Guid userId,
        NotificationType type,
        string title,
        string message,
        string? payload = null)
        => new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Type = type,
            Title = title,
            Message = message,
            Payload = payload,
            IsRead = false
        };

    public void MarkAsRead()
        => IsRead = true;
}