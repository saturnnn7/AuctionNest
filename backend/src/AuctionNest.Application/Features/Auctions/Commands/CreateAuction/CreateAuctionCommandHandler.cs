using AuctionNest.Application.Common.Interfaces;
using AuctionNest.Application.Common.Interfaces.Services;
using AuctionNest.Application.Features.Auctions.Common;
using AuctionNest.Domain.Common;
using AuctionNest.Domain.Entities;
using AuctionNest.Domain.Errors;
using MediatR;

namespace AuctionNest.Application.Features.Auctions.Commands.CreateAuction;

public sealed class CreateAuctionCommandHandler
    : IRequestHandler<CreateAuctionCommand, Result<AuctionDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IJobScheduler _jobScheduler;

    public CreateAuctionCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        IJobScheduler jobScheduler)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _jobScheduler = jobScheduler;
    }

    public async Task<Result<AuctionDto>> Handle(
        CreateAuctionCommand request,
        CancellationToken cancellationToken)
    {
        var sellerId = _currentUser.UserId;
        if (sellerId is null)
            return Result.Failure<AuctionDto>(UserErrors.Unauthorized);

        if (!await _unitOfWork.Categories.ExistsAsync(request.CategoryId, cancellationToken))
            return Result.Failure<AuctionDto>(CategoryErrors.NotFound);

        // Load seller for DTO mapping
        var seller = await _unitOfWork.Users.GetByIdAsync(sellerId.Value, cancellationToken);
        if (seller is null)
            return Result.Failure<AuctionDto>(UserErrors.NotFound);

        var category = await _unitOfWork.Categories.GetByIdAsync(request.CategoryId, cancellationToken);

        var auction = Auction.Create(
            sellerId.Value,
            request.CategoryId,
            request.Title,
            request.Description,
            request.StartPrice,
            request.MinBidIncrement,
            request.StartsAt,
            request.EndsAt,
            request.ReservePrice,
            request.BuyItNowPrice,
            request.ImageUrl);

        await _unitOfWork.Auctions.AddAsync(auction, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Schedule Hangfire jobs — persisted, survive server restarts
        _jobScheduler.ScheduleAuctionActivation(auction.Id, request.StartsAt);
        _jobScheduler.ScheduleAuctionEnd(auction.Id, request.EndsAt);

        // Manual DTO construction since navigation props aren't loaded yet
        return Result.Success(new AuctionDto(
            auction.Id, auction.Title, auction.Description, auction.ImageUrl,
            auction.SellerId, seller.DisplayName,
            auction.CategoryId, category!.Name,
            auction.StartPrice, auction.CurrentPrice, auction.ReservePrice,
            auction.BuyItNowPrice, auction.MinBidIncrement,
            auction.Status.ToString(), auction.StartsAt, auction.EndsAt,
            auction.ExtensionCount, auction.IsReserveMet, auction.IsBuyItNowAvailable,
            auction.CreatedAt));
    }
}