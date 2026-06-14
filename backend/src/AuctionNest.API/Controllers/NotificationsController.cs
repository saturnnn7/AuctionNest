using AuctionNest.API.Common;
using AuctionNest.Application.Common.Models;
using AuctionNest.Application.Features.Notifications.Commands.MarkAllAsRead;
using AuctionNest.Application.Features.Notifications.Commands.MarkAsRead;
using AuctionNest.Application.Features.Notifications.Common;
using AuctionNest.Application.Features.Notifications.Queries.GetMyNotifications;
using AuctionNest.Application.Features.Notifications.Queries.GetUnreadCount;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuctionNest.API.Controllers;

[Route("api/notifications")]
[Authorize]
public sealed class NotificationsController : BaseController
{
    private readonly ISender _sender;

    public NotificationsController(ISender sender) => _sender = sender;

    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<NotificationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyNotifications(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
        => HandleResult(await _sender.Send(new GetMyNotificationsQuery(page, pageSize), ct));

    [HttpGet("unread-count")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUnreadCount(CancellationToken ct)
        => HandleResult(await _sender.Send(new GetUnreadCountQuery(), ct));

    [HttpPatch("{notificationId:guid}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkAsRead(Guid notificationId, CancellationToken ct)
        => HandleResult(await _sender.Send(new MarkNotificationAsReadCommand(notificationId), ct));

    [HttpPatch("read-all")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MarkAllAsRead(CancellationToken ct)
        => HandleResult(await _sender.Send(new MarkAllAsReadCommand(), ct));
}