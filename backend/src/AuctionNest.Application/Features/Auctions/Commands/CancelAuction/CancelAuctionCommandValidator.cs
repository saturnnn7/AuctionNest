using FluentValidation;

namespace AuctionNest.Application.Features.Auctions.Commands.CancelAuction;

public sealed class CancelAuctionCommandValidator : AbstractValidator<CancelAuctionCommand>
{
    public CancelAuctionCommandValidator()
    {
        RuleFor(x => x.AuctionId).NotEmpty();
    }
}