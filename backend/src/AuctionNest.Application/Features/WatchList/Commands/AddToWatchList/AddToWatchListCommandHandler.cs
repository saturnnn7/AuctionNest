using AuctionNest.Application.Common.Interfaces;
using AuctionNest.Application.Common.Interfaces.Services;
using AuctionNest.Domain.Common;
using AuctionNest.Domain.Errors;
using MediatR;
using WatchListEntry = AuctionNest.Domain.Entities.WatchList;

namespace AuctionNest.Application.Features.WatchList.Commands.AddToWatchList;

public sealed class AddToWatchListCommandHandler : IRequestHandler<AddToWatchListCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public AddToWatchListCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(
        AddToWatchListCommand request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        if (userId is null)
            return Result.Failure(UserErrors.Unauthorized);

        var auction = await _unitOfWork.Auctions
            .GetByIdAsync(request.AuctionId, cancellationToken);

        if (auction is null)
            return Result.Failure(AuctionErrors.NotFound);

        var existing = await _unitOfWork.WatchLists
            .GetByUserAndAuctionAsync(userId.Value, request.AuctionId, cancellationToken);

        if (existing is not null)
            return Result.Failure(WatchListErrors.AlreadyWatching);

        var watchListItem = WatchListEntry.Create(userId.Value, request.AuctionId);
        await _unitOfWork.WatchLists.AddAsync(watchListItem, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}