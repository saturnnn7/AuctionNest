using AuctionNest.Application.Common.Interfaces;
using AuctionNest.Domain.Common;
using AuctionNest.Domain.Errors;
using MediatR;

namespace AuctionNest.Application.Features.Users.Commands.UpdateDisplayName;

public sealed class UpdateDisplayNameCommandHandler
    : IRequestHandler<UpdateDisplayNameCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public UpdateDisplayNameCommandHandler(
        IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(
        UpdateDisplayNameCommand request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        if (userId is null)
            return Result.Failure(UserErrors.Unauthorized);

        var user = await _unitOfWork.Users.GetByIdAsync(userId.Value, cancellationToken);
        if (user is null)
            return Result.Failure(UserErrors.NotFound);

        user.UpdateDisplayName(request.DisplayName);
        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}