using AuctionNest.Application.Common.Interfaces;
using Hangfire;
using Microsoft.Extensions.Logging;

namespace AuctionNest.Infrastructure.BackgroundJobs;

public sealed class EndAuctionJob
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<EndAuctionJob> _logger;

    public EndAuctionJob(IUnitOfWork unitOfWork, ILogger<EndAuctionJob> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 3)]
    public async Task ExecuteAsync(Guid auctionId)
    {
        var auction = await _unitOfWork.Auctions.GetWithBidsAsync(auctionId);
        if (auction is null)
        {
            _logger.LogWarning("Auction {Id} not found for ending.", auctionId);
            return;
        }

        // Guard: auction was extended — new end job is already scheduled, skip this one
        if (auction.EndsAt > DateTime.UtcNow.AddSeconds(5))
        {
            _logger.LogInformation(
                "Auction {Id} extended until {EndsAt}, skipping scheduled end.",
                auctionId, auction.EndsAt);
            return;
        }

        var result = auction.End();
        if (result.IsFailure)
        {
            _logger.LogWarning("Could not end auction {Id}: {Error}",
                auctionId, result.Error.Description);
            return;
        }

        await _unitOfWork.SaveChangesAsync();
        _logger.LogInformation("Auction {Id} ended.", auctionId);
    }
}