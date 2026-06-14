using AuctionNest.Application.Common.Interfaces.Services;
using AuctionNest.Infrastructure.BackgroundJobs;
using Hangfire;

namespace AuctionNest.Infrastructure.Services;

public sealed class HangfireJobScheduler : IJobScheduler
{
    private readonly IBackgroundJobClient _client;

    public HangfireJobScheduler(IBackgroundJobClient client) => _client = client;

    public void ScheduleAuctionActivation(Guid auctionId, DateTime activateAt)
        => _client.Schedule<ActivateAuctionJob>(
            j => j.ExecuteAsync(auctionId), activateAt);

    public void ScheduleAuctionEnd(Guid auctionId, DateTime endAt)
        => _client.Schedule<EndAuctionJob>(
            j => j.ExecuteAsync(auctionId), endAt);
}