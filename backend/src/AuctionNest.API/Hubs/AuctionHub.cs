using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace AuctionNest.API.Hubs;

public sealed class AuctionHub : Hub
{
    /// <summary>
    /// Join an auction room to receive real-time updates.
    /// All clients in the room receive BidPlaced, AuctionExtended, AuctionEnded events.
    /// </summary>
    public async Task JoinAuction(string auctionId)
        => await Groups.AddToGroupAsync(Context.ConnectionId, $"auction:{auctionId}");

    public async Task LeaveAuction(string auctionId)
        => await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"auction:{auctionId}");

    /// <summary>
    /// On connect, authenticated users join their personal group
    /// to receive targeted notifications (outbid alerts, etc.).
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        if (Context.User?.Identity?.IsAuthenticated == true)
        {
            var userId = Context.User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (userId is not null)
                await Groups.AddToGroupAsync(Context.ConnectionId, $"user:{userId}");
        }

        await base.OnConnectedAsync();
    }
}