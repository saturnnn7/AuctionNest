using AuctionNest.Domain.Common;
using MediatR;

namespace AuctionNest.Application.Features.WatchList.Commands.AddToWatchList;

public sealed record AddToWatchListCommand(Guid AuctionId) : IRequest<Result>;