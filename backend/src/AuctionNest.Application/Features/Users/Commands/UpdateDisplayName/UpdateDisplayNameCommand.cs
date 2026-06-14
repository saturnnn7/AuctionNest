using AuctionNest.Domain.Common;
using MediatR;

namespace AuctionNest.Application.Features.Users.Commands.UpdateDisplayName;

public sealed record UpdateDisplayNameCommand(string DisplayName) : IRequest<Result>;