using AuctionNest.API.Common;
using AuctionNest.Application.Features.Auctions.Common;
using AuctionNest.Application.Features.WatchList.Commands.AddToWatchList;
using AuctionNest.Application.Features.WatchList.Commands.RemoveFromWatchList;
using AuctionNest.Application.Features.WatchList.Queries.GetMyWatchList;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuctionNest.API.Controllers;

[Route("api/watchlist")]
[Authorize]
public sealed class WatchListController : BaseController
{
    private readonly ISender _sender;

    public WatchListController(ISender sender) => _sender = sender;

    [HttpGet]
    [ProducesResponseType(typeof(List<AuctionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyWatchList(CancellationToken ct)
        => HandleResult(await _sender.Send(new GetMyWatchListQuery(), ct));

    [HttpPost("{auctionId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Add(Guid auctionId, CancellationToken ct)
        => HandleResult(await _sender.Send(new AddToWatchListCommand(auctionId), ct));

    [HttpDelete("{auctionId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Remove(Guid auctionId, CancellationToken ct)
        => HandleResult(await _sender.Send(new RemoveFromWatchListCommand(auctionId), ct));
}