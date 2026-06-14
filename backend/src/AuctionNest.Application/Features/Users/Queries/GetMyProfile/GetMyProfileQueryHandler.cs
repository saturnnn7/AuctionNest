using AuctionNest.Application.Common.Interfaces;
using AuctionNest.Application.Common.Interfaces.Services;
using AuctionNest.Application.Features.Users.Common;
using AuctionNest.Domain.Common;
using AuctionNest.Domain.Errors;
using MediatR;

namespace AuctionNest.Application.Features.Users.Queries.GetMyProfile;

public sealed class GetMyProfileQueryHandler
    : IRequestHandler<GetMyProfileQuery, Result<UserProfileDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public GetMyProfileQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<UserProfileDto>> Handle(
        GetMyProfileQuery request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        if (userId is null)
            return Result.Failure<UserProfileDto>(UserErrors.Unauthorized);

        var user = await _unitOfWork.Users.GetByIdAsync(userId.Value, cancellationToken);
        if (user is null)
            return Result.Failure<UserProfileDto>(UserErrors.NotFound);

        return Result.Success(UserProfileDto.FromEntity(user));
    }
}