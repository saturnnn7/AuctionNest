using FluentValidation;

namespace AuctionNest.Application.Features.Auctions.Commands.BuyItNow;

public sealed class BuyItNowCommandValidator : AbstractValidator<BuyItNowCommand>
{
    public BuyItNowCommandValidator()
    {
        RuleFor(x => x.AuctionId).NotEmpty();
    }
}