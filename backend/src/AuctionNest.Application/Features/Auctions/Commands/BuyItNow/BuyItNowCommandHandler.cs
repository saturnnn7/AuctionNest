using AuctionNest.Application.Common.Interfaces;
using AuctionNest.Application.Common.Interfaces.Services;
using AuctionNest.Application.Features.Auctions.Common;
using AuctionNest.Domain.Common;
using AuctionNest.Domain.Errors;
using MediatR;

namespace AuctionNest.Application.Features.Auctions.Commands.BuyItNow;

public sealed class BuyItNowCommandHandler
    : IRequestHandler<BuyItNowCommand, Result<BidDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IDistributedLockService _lockService;

    public BuyItNowCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        IDistributedLockService lockService)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _lockService = lockService;
    }

    public async Task<Result<BidDto>> Handle(
        BuyItNowCommand request,
        CancellationToken cancellationToken)
    {
        var buyerId = _currentUser.UserId;
        if (buyerId is null)
            return Result.Failure<BidDto>(UserErrors.Unauthorized);

        // Lock to prevent simultaneous bid + BuyItNow race condition
        await using var distributedLock = await _lockService.AcquireAsync(
            $"auction:{request.AuctionId}",
            TimeSpan.FromSeconds(10),
            cancellationToken);

        if (distributedLock is null)
            return Result.Failure<BidDto>(AuctionErrors.LockNotAcquired);

        var auction = await _unitOfWork.Auctions.GetWithBidsAsync(
            request.AuctionId, cancellationToken);

        if (auction is null)
            return Result.Failure<BidDto>(AuctionErrors.NotFound);

        var result = auction.BuyItNow(buyerId.Value);
        if (result.IsFailure)
            return Result.Failure<BidDto>(result.Error);

        await _unitOfWork.Bids.AddAsync(result.Value, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(BidDto.FromEntity(result.Value, auction.CurrentPrice));
    }
}