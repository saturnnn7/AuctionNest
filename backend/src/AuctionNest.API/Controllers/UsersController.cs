using AuctionNest.API.Common;
using AuctionNest.Application.Features.Users.Commands.UpdateDisplayName;
using AuctionNest.Application.Features.Users.Common;
using AuctionNest.Application.Features.Users.Queries.GetMyProfile;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuctionNest.API.Controllers;

[Route("api/users")]
[Authorize]
public sealed class UsersController : BaseController
{
    private readonly ISender _sender;

    public UsersController(ISender sender) => _sender = sender;

    [HttpGet("me")]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyProfile(CancellationToken ct)
        => HandleResult(await _sender.Send(new GetMyProfileQuery(), ct));

    [HttpPatch("me/display-name")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpdateDisplayName(
        [FromBody] UpdateDisplayNameCommand command,
        CancellationToken ct)
        => HandleResult(await _sender.Send(command, ct));
}