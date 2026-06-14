using AuctionNest.Application.Common.Interfaces;
using AuctionNest.Application.Common.Interfaces.Services;
using AuctionNest.Application.Features.Auth.Common;
using AuctionNest.Domain.Common;
using AuctionNest.Domain.Errors;
using MediatR;

namespace AuctionNest.Application.Features.Auth.Commands.Login;

public sealed class LoginCommandHandler
    : IRequestHandler<LoginCommand, Result<AuthResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtService _jwtService;
    private readonly ICacheService _cacheService;

    public LoginCommandHandler(
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        IJwtService jwtService,
        ICacheService cacheService)
    {
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _jwtService = jwtService;
        _cacheService = cacheService;
    }

    public async Task<Result<AuthResponse>> Handle(
        LoginCommand request,
        CancellationToken cancellationToken)
    {
        // Поддерживаем вход и по email и по username
        var user = request.UsernameOrEmail.Contains('@')
            ? await _unitOfWork.Users.GetByEmailAsync(request.UsernameOrEmail, cancellationToken)
            : await _unitOfWork.Users.GetByUsernameAsync(request.UsernameOrEmail, cancellationToken);

        // Намеренно одна ошибка для обоих случаев — не раскрываем существует ли юзер
        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
            return Result.Failure<AuthResponse>(UserErrors.InvalidCredentials);

        if (user.DeletedAt.HasValue)
            return Result.Failure<AuthResponse>(UserErrors.InvalidCredentials);

        var accessToken = _jwtService.GenerateAccessToken(user);
        var refreshToken = _jwtService.GenerateRefreshToken();

        await _cacheService.SetAsync(
            $"refresh_token:{refreshToken}",
            user.Id.ToString(),
            TimeSpan.FromDays(7),
            cancellationToken);

        return Result.Success(new AuthResponse(
            accessToken,
            refreshToken,
            user.Id,
            user.Username,
            user.DisplayName,
            user.Role.ToString()));
    }
}