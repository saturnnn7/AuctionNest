using AuctionNest.Application.Common.Models;
using AuctionNest.Application.Features.Notifications.Common;
using AuctionNest.Domain.Common;
using MediatR;

namespace AuctionNest.Application.Features.Notifications.Queries.GetMyNotifications;

public sealed record GetMyNotificationsQuery(
    int Page = 1,
    int PageSize = 20) : IRequest<Result<PagedResponse<NotificationDto>>>;