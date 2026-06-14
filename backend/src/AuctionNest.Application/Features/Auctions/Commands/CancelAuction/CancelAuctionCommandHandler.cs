using AuctionNest.Application.Common.Interfaces;
using AuctionNest.Domain.Common;
using AuctionNest.Domain.Errors;
using MediatR;

namespace AuctionNest.Application.Features.Auctions.Commands.CancelAuction;

public sealed class CancelAuctionCommandHandler : IRequestHandler<CancelAuctionCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public CancelAuctionCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(
        CancelAuctionCommand request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        if (userId is null)
            return Result.Failure(UserErrors.Unauthorized);

        var auction = await _unitOfWork.Auctions.GetByIdAsync(
            request.AuctionId, cancellationToken);

        if (auction is null)
            return Result.Failure(AuctionErrors.NotFound);

        if (auction.SellerId != userId)
            return Result.Failure(AuctionErrors.NotOwner);

        var result = auction.Cancel();
        if (result.IsFailure)
            return result;

        _unitOfWork.Auctions.Update(auction);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}