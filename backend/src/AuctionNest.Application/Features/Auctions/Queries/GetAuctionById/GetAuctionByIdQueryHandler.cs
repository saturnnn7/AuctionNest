using AuctionNest.Application.Common.Interfaces;
using AuctionNest.Application.Features.Auctions.Common;
using AuctionNest.Domain.Common;
using AuctionNest.Domain.Errors;
using MediatR;

namespace AuctionNest.Application.Features.Auctions.Queries.GetAuctionById;

public sealed class GetAuctionByIdQueryHandler
    : IRequestHandler<GetAuctionByIdQuery, Result<AuctionDetailDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetAuctionByIdQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    public async Task<Result<AuctionDetailDto>> Handle(
        GetAuctionByIdQuery request,
        CancellationToken cancellationToken)
    {
        // GetWithBidsAsync loads auction + seller + category + bids
        var auction = await _unitOfWork.Auctions.GetWithBidsAsync(request.Id, cancellationToken);

        if (auction is null)
            return Result.Failure<AuctionDetailDto>(AuctionErrors.NotFound);

        return Result.Success(AuctionDetailDto.FromEntity(auction));
    }
}