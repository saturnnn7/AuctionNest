using AuctionNest.Domain.Common;
using MediatR;

namespace AuctionNest.Application.Features.Notifications.Queries.GetUnreadCount;

public sealed record GetUnreadCountQuery : IRequest<Result<int>>;