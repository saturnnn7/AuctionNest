using AuctionNest.Application.Common.Interfaces;
using AuctionNest.Application.Common.Interfaces.Services;
using AuctionNest.Application.Common.Models;
using AuctionNest.Application.Features.Notifications.Common;
using AuctionNest.Domain.Common;
using AuctionNest.Domain.Errors;
using MediatR;

namespace AuctionNest.Application.Features.Notifications.Queries.GetMyNotifications;

public sealed class GetMyNotificationsQueryHandler
    : IRequestHandler<GetMyNotificationsQuery, Result<PagedResponse<NotificationDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public GetMyNotificationsQueryHandler(
        IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<PagedResponse<NotificationDto>>> Handle(
        GetMyNotificationsQuery request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        if (userId is null)
            return Result.Failure<PagedResponse<NotificationDto>>(UserErrors.Unauthorized);

        var items = await _unitOfWork.Notifications
            .GetByUserIdAsync(userId.Value, request.Page, request.PageSize, cancellationToken);

        var unreadCount = await _unitOfWork.Notifications
            .GetUnreadCountAsync(userId.Value, cancellationToken);

        return Result.Success(new PagedResponse<NotificationDto>
        {
            Items = items.Select(NotificationDto.FromEntity).ToList(),
            TotalCount = unreadCount,
            Page = request.Page,
            PageSize = request.PageSize
        });
    }
}