using AuctionNest.Domain.Entities;

namespace AuctionNest.Application.Features.Notifications.Common;

public sealed record NotificationDto(
    Guid Id,
    string Type,
    string Title,
    string Message,
    string? Payload,
    bool IsRead,
    DateTime CreatedAt)
{
    public static NotificationDto FromEntity(Notification n)
        => new(n.Id, n.Type.ToString(), n.Title, n.Message, n.Payload, n.IsRead, n.CreatedAt);
}