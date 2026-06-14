using AuctionNest.Application.Common.Interfaces;
using AuctionNest.Application.Common.Models;
using AuctionNest.Application.Features.Auctions.Common;
using AuctionNest.Domain.Common;
using MediatR;

namespace AuctionNest.Application.Features.Auctions.Queries.GetAuctions;

public sealed class GetAuctionsQueryHandler
    : IRequestHandler<GetAuctionsQuery, Result<PagedResponse<AuctionDto>>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetAuctionsQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    public async Task<Result<PagedResponse<AuctionDto>>> Handle(
        GetAuctionsQuery request,
        CancellationToken cancellationToken)
    {
        var filter = new AuctionFilterParams(
            request.Search,
            request.CategoryId,
            request.Status,
            request.MinPrice,
            request.MaxPrice,
            request.Page,
            request.PageSize,
            request.SortBy,
            request.SortDescending);

        var (items, totalCount) = await _unitOfWork.Auctions.GetPagedAsync(filter, cancellationToken);

        return Result.Success(new PagedResponse<AuctionDto>
        {
            Items = items.Select(AuctionDto.FromEntity).ToList(),
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        });
    }
}