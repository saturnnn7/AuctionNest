using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using AuctionNest.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;

namespace AuctionNest.Infrastructure.Services;

public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        => _httpContextAccessor = httpContextAccessor;

    public Guid? UserId
    {
        get
        {
            var sub = _httpContextAccessor.HttpContext?.User
                .FindFirstValue(JwtRegisteredClaimNames.Sub);
            return Guid.TryParse(sub, out var id) ? id : null;
        }
    }

    public bool IsAuthenticated
        => _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;
}