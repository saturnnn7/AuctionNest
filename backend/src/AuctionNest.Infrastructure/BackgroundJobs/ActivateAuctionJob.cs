using AuctionNest.Application.Common.Interfaces;
using Hangfire;
using Microsoft.Extensions.Logging;

namespace AuctionNest.Infrastructure.BackgroundJobs;

public sealed class ActivateAuctionJob
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ActivateAuctionJob> _logger;

    public ActivateAuctionJob(IUnitOfWork unitOfWork, ILogger<ActivateAuctionJob> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 3)]
    public async Task ExecuteAsync(Guid auctionId)
    {
        var auction = await _unitOfWork.Auctions.GetByIdAsync(auctionId);
        if (auction is null)
        {
            _logger.LogWarning("Auction {Id} not found for activation.", auctionId);
            return;
        }

        var result = auction.Activate();
        if (result.IsFailure)
        {
            _logger.LogWarning("Could not activate auction {Id}: {Error}",
                auctionId, result.Error.Description);
            return;
        }

        await _unitOfWork.SaveChangesAsync();
        _logger.LogInformation("Auction {Id} activated.", auctionId);
    }
}