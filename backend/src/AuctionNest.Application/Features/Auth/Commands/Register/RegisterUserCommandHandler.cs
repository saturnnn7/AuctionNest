using AuctionNest.Application.Common.Interfaces;
using AuctionNest.Application.Common.Interfaces.Services;
using AuctionNest.Application.Features.Auth.Common;
using AuctionNest.Domain.Common;
using AuctionNest.Domain.Entities;
using AuctionNest.Domain.Errors;
using MediatR;

namespace AuctionNest.Application.Features.Auth.Commands.Register;

public sealed class RegisterUserCommandHandler
    : IRequestHandler<RegisterUserCommand, Result<AuthResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtService _jwtService;
    private readonly ICacheService _cacheService;

    public RegisterUserCommandHandler(
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
        RegisterUserCommand request,
        CancellationToken cancellationToken)
    {
        if (await _unitOfWork.Users.EmailExistsAsync(request.Email, cancellationToken))
            return Result.Failure<AuthResponse>(UserErrors.EmailAlreadyInUse);

        if (await _unitOfWork.Users.UsernameExistsAsync(request.Username, cancellationToken))
            return Result.Failure<AuthResponse>(UserErrors.UsernameAlreadyInUse);

        var passwordHash = _passwordHasher.Hash(request.Password);
        var user = User.Create(request.Username, request.Email, passwordHash, request.DisplayName);

        await _unitOfWork.Users.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(await IssueTokensAsync(user, cancellationToken));
    }

    private async Task<AuthResponse> IssueTokensAsync(User user, CancellationToken ct)
    {
        var accessToken = _jwtService.GenerateAccessToken(user);
        var refreshToken = _jwtService.GenerateRefreshToken();

        await _cacheService.SetAsync(
            $"refresh_token:{refreshToken}",
            user.Id.ToString(),
            TimeSpan.FromDays(7),
            ct);

        return new AuthResponse(
            accessToken,
            refreshToken,
            user.Id,
            user.Username,
            user.DisplayName,
            user.Role.ToString());
    }
}