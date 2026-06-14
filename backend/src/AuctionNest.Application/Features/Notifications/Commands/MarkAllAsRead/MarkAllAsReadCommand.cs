using AuctionNest.Domain.Common;
using MediatR;

namespace AuctionNest.Application.Features.Notifications.Commands.MarkAllAsRead;

public sealed record MarkAllAsReadCommand : IRequest<Result>;