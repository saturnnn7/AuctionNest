using AuctionNest.Application.Features.Users.Common;
using AuctionNest.Domain.Common;
using MediatR;

namespace AuctionNest.Application.Features.Users.Queries.GetMyProfile;

public sealed record GetMyProfileQuery : IRequest<Result<UserProfileDto>>;