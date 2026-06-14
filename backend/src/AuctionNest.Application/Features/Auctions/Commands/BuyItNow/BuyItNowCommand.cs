using AuctionNest.Application.Features.Auctions.Common;
using AuctionNest.Domain.Common;
using MediatR;

namespace AuctionNest.Application.Features.Auctions.Commands.BuyItNow;

public sealed record BuyItNowCommand(Guid AuctionId) : IRequest<Result<BidDto>>;