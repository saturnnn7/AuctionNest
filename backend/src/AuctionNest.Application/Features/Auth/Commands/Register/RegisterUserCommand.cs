using AuctionNest.Application.Features.Auth.Common;
using AuctionNest.Domain.Common;
using MediatR;

namespace AuctionNest.Application.Features.Auth.Commands.Register;

public sealed record RegisterUserCommand(
    string Username,
    string Email,
    string Password,
    string DisplayName) : IRequest<Result<AuthResponse>>;