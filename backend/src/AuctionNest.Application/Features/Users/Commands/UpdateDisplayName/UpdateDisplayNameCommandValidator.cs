using FluentValidation;

namespace AuctionNest.Application.Features.Users.Commands.UpdateDisplayName;

public sealed class UpdateDisplayNameCommandValidator
    : AbstractValidator<UpdateDisplayNameCommand>
{
    public UpdateDisplayNameCommandValidator()
    {
        RuleFor(x => x.DisplayName)
            .NotEmpty()
            .MinimumLength(2)
            .MaximumLength(50);
    }
}