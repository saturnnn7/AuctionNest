using AuctionNest.Domain.Common;
using MediatR;

namespace AuctionNest.Application.Features.Notifications.Commands.MarkAsRead;

public sealed record MarkNotificationAsReadCommand(Guid NotificationId) : IRequest<Result>;