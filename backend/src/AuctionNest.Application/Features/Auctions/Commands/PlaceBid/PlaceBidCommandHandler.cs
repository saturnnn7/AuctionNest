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
    
        if (request.IdempotencyKey is not null)
        {
            var exists = await _cache.ExistsAsync(
                $"bid_idempotency:{request.IdempotencyKey}", cancellationToken);
            if (exists)
                return Result.Failure<BidDto>(BidErrors.DuplicateRequest);
        }
    
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
    
        // Domain method returns the new Bid entity
        var bidResult = auction.PlaceBid(bidderId.Value, request.Amount);
        if (bidResult.IsFailure)
            return Result.Failure<BidDto>(bidResult.Error);
    
        // Explicitly register the new Bid with EF Core as Added
        // EF Core cannot reliably detect new entities added to private field-backed collections
        await _unitOfWork.Bids.AddAsync(bidResult.Value, cancellationToken);
    
        // SaveChanges: AuditInterceptor sets timestamps, OutboxInterceptor writes events atomically
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    
        if (request.IdempotencyKey is not null)
        {
            await _cache.SetAsync(
                $"bid_idempotency:{request.IdempotencyKey}",
                true,
                TimeSpan.FromHours(24),
                cancellationToken);
        }
    
        return Result.Success(BidDto.FromEntity(bidResult.Value, auction.CurrentPrice));
    }
}