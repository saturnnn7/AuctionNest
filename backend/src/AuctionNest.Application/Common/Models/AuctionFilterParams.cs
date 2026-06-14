using AuctionNest.Domain.Enums;

namespace AuctionNest.Application.Common.Models;

public sealed record AuctionFilterParams(
    string? Search = null,
    Guid? CategoryId = null,
    AuctionStatus? Status = null,
    decimal? MinPrice = null,
    decimal? MaxPrice = null,
    int Page = 1,
    int PageSize = 20,
    string SortBy = "endsAt",
    bool SortDescending = false);