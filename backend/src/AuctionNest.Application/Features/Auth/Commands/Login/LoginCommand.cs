using AuctionNest.Application.Features.Auth.Common;
using AuctionNest.Domain.Common;
using MediatR;

namespace AuctionNest.Application.Features.Auth.Commands.Login;

public sealed record LoginCommand(
    string UsernameOrEmail,
    string Password) : IRequest<Result<AuthResponse>>;