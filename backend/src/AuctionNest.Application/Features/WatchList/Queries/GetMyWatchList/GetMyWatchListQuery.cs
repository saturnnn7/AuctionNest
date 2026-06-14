using AuctionNest.Application.Features.Auctions.Common;
using AuctionNest.Domain.Common;
using MediatR;

namespace AuctionNest.Application.Features.WatchList.Queries.GetMyWatchList;

public sealed record GetMyWatchListQuery : IRequest<Result<List<AuctionDto>>>;