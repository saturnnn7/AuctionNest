using AuctionNest.Application.Common.Models;
using AuctionNest.Application.Features.Auctions.Common;
using AuctionNest.Domain.Common;
using AuctionNest.Domain.Enums;
using MediatR;

namespace AuctionNest.Application.Features.Auctions.Queries.GetAuctions;

public sealed record GetAuctionsQuery(
    string? Search = null,
    Guid? CategoryId = null,
    AuctionStatus? Status = null,
    decimal? MinPrice = null,
    decimal? MaxPrice = null,
    int Page = 1,
    int PageSize = 20,
    string SortBy = "endsAt",
    bool SortDescending = false) : IRequest<Result<PagedResponse<AuctionDto>>>;