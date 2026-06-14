using AuctionNest.Domain.Common;
using MediatR;

namespace AuctionNest.Application.Features.Auctions.Commands.CancelAuction;

public sealed record CancelAuctionCommand(Guid AuctionId) : IRequest<Result>;