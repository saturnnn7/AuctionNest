using AuctionNest.Application.Common.Interfaces.Services;

namespace AuctionNest.Infrastructure.Services;

public sealed class DateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}