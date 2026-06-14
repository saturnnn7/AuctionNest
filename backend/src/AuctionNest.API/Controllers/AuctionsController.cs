using AuctionNest.API.Common;
using AuctionNest.Application.Features.Auctions.Commands.BuyItNow;
using AuctionNest.Application.Features.Auctions.Commands.CancelAuction;
using AuctionNest.Application.Features.Auctions.Commands.CreateAuction;
using AuctionNest.Application.Features.Auctions.Commands.PlaceBid;
using AuctionNest.Application.Features.Auctions.Common;
using AuctionNest.Application.Features.Auctions.Queries.GetAuctionById;
using AuctionNest.Application.Features.Auctions.Queries.GetAuctions;
using AuctionNest.Application.Common.Models;
using AuctionNest.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuctionNest.API.Controllers;

[Route("api/auctions")]
public sealed class AuctionsController : BaseController
{
    private readonly ISender _sender;

    public AuctionsController(ISender sender) => _sender = sender;

    /// <summary>Returns a paged list of auctions with optional filters.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<AuctionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAuctions(
        [FromQuery] string? search,
        [FromQuery] Guid? categoryId,
        [FromQuery] AuctionStatus? status,
        [FromQuery] decimal? minPrice,
        [FromQuery] decimal? maxPrice,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string sortBy = "endsAt",
        [FromQuery] bool sortDescending = false,
        CancellationToken cancellationToken = default)
    {
        var query = new GetAuctionsQuery(
            search, categoryId, status, minPrice, maxPrice,
            page, pageSize, sortBy, sortDescending);

        var result = await _sender.Send(query, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>Returns full auction details including recent bids.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AuctionDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetAuctionByIdQuery(id), cancellationToken);
        return HandleResult(result);
    }

    /// <summary>Creates a new auction. Hangfire schedules activation and end jobs automatically.</summary>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(AuctionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create(
        [FromBody] CreateAuctionCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>Places a bid on an active auction.</summary>
    [HttpPost("{auctionId:guid}/bids")]
    [Authorize]
    [ProducesResponseType(typeof(BidDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> PlaceBid(
        Guid auctionId,
        [FromBody] PlaceBidRequest request,
        [FromHeader(Name = "X-Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var command = new PlaceBidCommand(auctionId, request.Amount, idempotencyKey);
        var result = await _sender.Send(command, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>Instantly purchases the auction at the Buy It Now price.</summary>
    [HttpPost("{auctionId:guid}/buy-now")]
    [Authorize]
    [ProducesResponseType(typeof(BidDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> BuyItNow(
        Guid auctionId,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new BuyItNowCommand(auctionId), cancellationToken);
        return HandleResult(result);
    }
    
    /// <summary>Cancels the auction. Only the seller can cancel.</summary>
    [HttpDelete("{auctionId:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cancel(
        Guid auctionId,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new CancelAuctionCommand(auctionId), cancellationToken);
        return HandleResult(result);
    }
}

public sealed record PlaceBidRequest(decimal Amount);