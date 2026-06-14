using AuctionNest.Domain.Common;
using MediatR;

namespace AuctionNest.Application.Features.WatchList.Commands.RemoveFromWatchList;

public sealed record RemoveFromWatchListCommand(Guid AuctionId) : IRequest<Result>;