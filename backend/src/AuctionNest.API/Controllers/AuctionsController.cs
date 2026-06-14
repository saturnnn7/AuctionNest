using AuctionNest.API.Common;
using AuctionNest.Application.Features.Auctions.Commands.PlaceBid;
using AuctionNest.Application.Features.Auctions.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuctionNest.API.Controllers;

[Route("api/auctions")]
public sealed class AuctionsController : BaseController
{
    private readonly ISender _sender;

    public AuctionsController(ISender sender) => _sender = sender;

    /// <summary>Places a bid on an active auction.</summary>
    /// <remarks>
    /// Protected by distributed lock — only one bid per auction is processed at a time.
    /// Include X-Idempotency-Key header to prevent duplicate bids on network retry.
    /// </remarks>
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
}

// Separate record because AuctionId comes from route, not body
public sealed record PlaceBidRequest(decimal Amount);