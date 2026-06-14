namespace AuctionNest.Application.Common.Interfaces.Services;

public interface IJobScheduler
{
    void ScheduleAuctionActivation(Guid auctionId, DateTime activateAt);
    void ScheduleAuctionEnd(Guid auctionId, DateTime endAt);
}