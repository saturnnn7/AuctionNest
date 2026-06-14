using AuctionNest.Application.Features.Auctions.Common;
using AuctionNest.Domain.Common;
using MediatR;

namespace AuctionNest.Application.Features.Auctions.Commands.PlaceBid;

public sealed record PlaceBidCommand(
    Guid AuctionId,
    decimal Amount,
    string? IdempotencyKey = null) : IRequest<Result<BidDto>>;