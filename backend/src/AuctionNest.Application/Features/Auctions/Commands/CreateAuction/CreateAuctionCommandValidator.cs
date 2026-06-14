using FluentValidation;

namespace AuctionNest.Application.Features.Auctions.Commands.CreateAuction;

public sealed class CreateAuctionCommandValidator : AbstractValidator<CreateAuctionCommand>
{
    public CreateAuctionCommandValidator()
    {
        RuleFor(x => x.CategoryId)
            .NotEmpty();

        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Description)
            .NotEmpty()
            .MaximumLength(5000);

        RuleFor(x => x.StartPrice)
            .GreaterThan(0).WithMessage("Start price must be greater than zero.");

        RuleFor(x => x.MinBidIncrement)
            .GreaterThan(0).WithMessage("Minimum bid increment must be greater than zero.");

        RuleFor(x => x.StartsAt)
            .GreaterThan(DateTime.UtcNow).WithMessage("Auction must start in the future.");

        RuleFor(x => x.EndsAt)
            .GreaterThan(x => x.StartsAt.AddHours(1))
            .WithMessage("Auction must run for at least 1 hour.");

        RuleFor(x => x.ReservePrice)
            .GreaterThanOrEqualTo(x => x.StartPrice)
            .WithMessage("Reserve price must be at least the start price.")
            .When(x => x.ReservePrice.HasValue);

        RuleFor(x => x.BuyItNowPrice)
            .GreaterThan(x => x.StartPrice)
            .WithMessage("Buy It Now price must be greater than the start price.")
            .When(x => x.BuyItNowPrice.HasValue);

        RuleFor(x => x.ImageUrl)
            .MaximumLength(500)
            .When(x => x.ImageUrl is not null);
    }
}