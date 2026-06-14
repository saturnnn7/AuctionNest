using AuctionNest.Application.Common.Interfaces;
using AuctionNest.Application.Common.Interfaces.Services;
using AuctionNest.Domain.Common;
using AuctionNest.Domain.Errors;
using MediatR;

namespace AuctionNest.Application.Features.Notifications.Commands.MarkAllAsRead;

public sealed class MarkAllAsReadCommandHandler : IRequestHandler<MarkAllAsReadCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public MarkAllAsReadCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(
        MarkAllAsReadCommand request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        if (userId is null)
            return Result.Failure(UserErrors.Unauthorized);

        await _unitOfWork.Notifications.MarkAllAsReadAsync(userId.Value, cancellationToken);
        return Result.Success();
    }
}