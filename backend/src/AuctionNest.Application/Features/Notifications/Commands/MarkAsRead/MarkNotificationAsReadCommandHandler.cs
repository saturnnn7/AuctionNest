using AuctionNest.Application.Common.Interfaces;
using AuctionNest.Application.Common.Interfaces.Services;
using AuctionNest.Domain.Common;
using AuctionNest.Domain.Errors;
using MediatR;

namespace AuctionNest.Application.Features.Notifications.Commands.MarkAsRead;

public sealed class MarkNotificationAsReadCommandHandler
    : IRequestHandler<MarkNotificationAsReadCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public MarkNotificationAsReadCommandHandler(
        IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(
        MarkNotificationAsReadCommand request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        if (userId is null)
            return Result.Failure(UserErrors.Unauthorized);

        var notification = await _unitOfWork.Notifications
            .GetByIdAsync(request.NotificationId, cancellationToken);

        if (notification is null)
            return Result.Failure(NotificationErrors.NotFound);

        if (notification.UserId != userId)
            return Result.Failure(NotificationErrors.Forbidden);

        notification.MarkAsRead();
        _unitOfWork.Notifications.Update(notification);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}