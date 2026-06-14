using FluentValidation;

namespace AuctionNest.Application.Features.Auctions.Commands.PlaceBid;

public sealed class PlaceBidCommandValidator : AbstractValidator<PlaceBidCommand>
{
    public PlaceBidCommandValidator()
    {
        RuleFor(x => x.AuctionId)
            .NotEmpty();

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Bid amount must be greater than zero.");
    }
}