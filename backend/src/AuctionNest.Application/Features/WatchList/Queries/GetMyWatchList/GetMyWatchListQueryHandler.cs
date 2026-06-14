using AuctionNest.Application.Common.Interfaces;
using AuctionNest.Application.Common.Interfaces.Services;
using AuctionNest.Application.Features.Auctions.Common;
using AuctionNest.Domain.Common;
using AuctionNest.Domain.Errors;
using MediatR;

namespace AuctionNest.Application.Features.WatchList.Queries.GetMyWatchList;

public sealed class GetMyWatchListQueryHandler
    : IRequestHandler<GetMyWatchListQuery, Result<List<AuctionDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public GetMyWatchListQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<List<AuctionDto>>> Handle(
        GetMyWatchListQuery request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        if (userId is null)
            return Result.Failure<List<AuctionDto>>(UserErrors.Unauthorized);

        var items = await _unitOfWork.WatchLists
            .GetByUserIdWithAuctionsAsync(userId.Value, cancellationToken);

        var dtos = items
            .Select(w => AuctionDto.FromEntity(w.Auction))
            .ToList();

        return Result.Success(dtos);
    }
}