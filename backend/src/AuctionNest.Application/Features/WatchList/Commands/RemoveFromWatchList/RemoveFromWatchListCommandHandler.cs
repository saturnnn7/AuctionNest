using AuctionNest.Application.Common.Interfaces;
using AuctionNest.Domain.Common;
using AuctionNest.Domain.Errors;
using MediatR;

namespace AuctionNest.Application.Features.WatchList.Commands.RemoveFromWatchList;

public sealed class RemoveFromWatchListCommandHandler
    : IRequestHandler<RemoveFromWatchListCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public RemoveFromWatchListCommandHandler(
        IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(
        RemoveFromWatchListCommand request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        if (userId is null)
            return Result.Failure(UserErrors.Unauthorized);

        var item = await _unitOfWork.WatchLists
            .GetByUserAndAuctionAsync(userId.Value, request.AuctionId, cancellationToken);

        if (item is null)
            return Result.Failure(WatchListErrors.NotWatching);

        _unitOfWork.WatchLists.Remove(item);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}