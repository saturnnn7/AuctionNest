using AuctionNest.Application.Common.Interfaces;
using AuctionNest.Application.Common.Interfaces.Services;
using AuctionNest.Application.Features.Auctions.Common;
using AuctionNest.Domain.Common;
using AuctionNest.Domain.Errors;
using MediatR;

namespace AuctionNest.Application.Features.Auctions.Commands.PlaceBid;

public sealed class PlaceBidCommandHandler
    : IRequestHandler<PlaceBidCommand, Result<BidDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IDistributedLockService _lockService;
    private readonly ICacheService _cache;

    public PlaceBidCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        IDistributedLockService lockService,
        ICacheService cache)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _lockService = lockService;
        _cache = cache;
    }

    public async Task<Result<BidDto>> Handle(
        PlaceBidCommand request,
        CancellationToken cancellationToken)
    {
        var bidderId = _currentUser.UserId;
        if (bidderId is null)
            return Result.Failure<BidDto>(UserErrors.Unauthorized);

        // Idempotency: if this exact request was already processed, reject duplicate
        if (request.IdempotencyKey is not null)
        {
            var idempotencyExists = await _cache.ExistsAsync(
                $"bid_idempotency:{request.IdempotencyKey}", cancellationToken);

            if (idempotencyExists)
                return Result.Failure<BidDto>(BidErrors.DuplicateRequest);
        }

        // Acquire distributed lock — prevents race condition when two users bid simultaneously
        await using var distributedLock = await _lockService.AcquireAsync(
            $"auction:{request.AuctionId}",
            TimeSpan.FromSeconds(10),
            cancellationToken);

        if (distributedLock is null)
            return Result.Failure<BidDto>(AuctionErrors.LockNotAcquired);

        // Load auction WITH bids — needed to find current winner and apply anti-snipe
        var auction = await _unitOfWork.Auctions.GetWithBidsAsync(
            request.AuctionId, cancellationToken);

        if (auction is null)
            return Result.Failure<BidDto>(AuctionErrors.NotFound);

        // All business logic lives in the domain — Redlock guarantees single-threaded access here
        var result = auction.PlaceBid(bidderId.Value, request.Amount);
        if (result.IsFailure)
            return Result.Failure<BidDto>(result.Error);

        // SaveChanges: OutboxInterceptor atomically writes BidPlacedEvent to outbox_messages
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Mark idempotency key as used — prevents duplicate bids from retried requests
        if (request.IdempotencyKey is not null)
        {
            await _cache.SetAsync(
                $"bid_idempotency:{request.IdempotencyKey}",
                true,
                TimeSpan.FromHours(24),
                cancellationToken);
        }

        var winningBid = auction.Bids.First(b => b.IsWinning);
        return Result.Success(BidDto.FromEntity(winningBid, auction.CurrentPrice));
    }
}