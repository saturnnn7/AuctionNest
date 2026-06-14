using AuctionNest.Application.Features.Auctions.Common;
using AuctionNest.Domain.Common;
using MediatR;

namespace AuctionNest.Application.Features.Auctions.Queries.GetAuctionById;

public sealed record GetAuctionByIdQuery(Guid Id) : IRequest<Result<AuctionDetailDto>>;