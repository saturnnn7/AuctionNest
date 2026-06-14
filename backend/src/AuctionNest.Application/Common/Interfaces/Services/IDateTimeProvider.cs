namespace AuctionNest.Application.Common.Interfaces.Services;

// Абстракция над DateTime.UtcNow — нужна для unit тестов
// чтобы мокать время (проверка anti-snipe, expiry и т.д.)
public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}