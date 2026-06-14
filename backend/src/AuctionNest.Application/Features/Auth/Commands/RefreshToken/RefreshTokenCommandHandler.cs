using AuctionNest.Application.Common.Interfaces;
using AuctionNest.Application.Common.Interfaces.Services;
using AuctionNest.Application.Features.Auth.Common;
using AuctionNest.Domain.Common;
using AuctionNest.Domain.Errors;
using MediatR;

namespace AuctionNest.Application.Features.Auth.Commands.RefreshToken;

public sealed class RefreshTokenCommandHandler
    : IRequestHandler<RefreshTokenCommand, Result<AuthResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJwtService _jwtService;
    private readonly ICacheService _cacheService;

    public RefreshTokenCommandHandler(
        IUnitOfWork unitOfWork,
        IJwtService jwtService,
        ICacheService cacheService)
    {
        _unitOfWork = unitOfWork;
        _jwtService = jwtService;
        _cacheService = cacheService;
    }

    public async Task<Result<AuthResponse>> Handle(
        RefreshTokenCommand request,
        CancellationToken cancellationToken)
    {
        var cacheKey = $"refresh_token:{request.RefreshToken}";

        var userIdString = await _cacheService.GetAsync<string>(cacheKey, cancellationToken);
        if (userIdString is null || !Guid.TryParse(userIdString, out var userId))
            return Result.Failure<AuthResponse>(UserErrors.InvalidCredentials);

        var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
        if (user is null || user.DeletedAt.HasValue)
            return Result.Failure<AuthResponse>(UserErrors.InvalidCredentials);

        // Ротация: удаляем старый токен, выдаём новый
        await _cacheService.DeleteAsync(cacheKey, cancellationToken);

        var accessToken = _jwtService.GenerateAccessToken(user);
        var newRefreshToken = _jwtService.GenerateRefreshToken();

        await _cacheService.SetAsync(
            $"refresh_token:{newRefreshToken}",
            user.Id.ToString(),
            TimeSpan.FromDays(7),
            cancellationToken);

        return Result.Success(new AuthResponse(
            accessToken,
            newRefreshToken,
            user.Id,
            user.Username,
            user.DisplayName,
            user.Role.ToString()));
    }
}