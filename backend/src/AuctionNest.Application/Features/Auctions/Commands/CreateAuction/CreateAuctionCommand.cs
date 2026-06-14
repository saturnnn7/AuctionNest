using AuctionNest.Application.Features.Auctions.Common;
using AuctionNest.Domain.Common;
using MediatR;

namespace AuctionNest.Application.Features.Auctions.Commands.CreateAuction;

public sealed record CreateAuctionCommand(
    Guid CategoryId,
    string Title,
    string Description,
    decimal StartPrice,
    decimal MinBidIncrement,
    DateTime StartsAt,
    DateTime EndsAt,
    decimal? ReservePrice = null,
    decimal? BuyItNowPrice = null,
    string? ImageUrl = null) : IRequest<Result<AuctionDto>>;