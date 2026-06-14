using FluentValidation;

namespace AuctionNest.Application.Features.Auth.Commands.Register;

public sealed class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserCommandValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty()
            .MinimumLength(3).WithMessage("Username must be at least 3 characters.")
            .MaximumLength(30).WithMessage("Username must not exceed 30 characters.")
            .Matches(@"^[a-z0-9_]+$")
            .WithMessage("Username can only contain lowercase letters, numbers, and underscores.");

        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress().WithMessage("A valid email address is required.")
            .MaximumLength(254);

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
            .MaximumLength(100)
            .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches(@"[0-9]").WithMessage("Password must contain at least one digit.");

        RuleFor(x => x.DisplayName)
            .NotEmpty()
            .MinimumLength(2)
            .MaximumLength(50);
    }
}