using AuctionNest.Domain.Common;
using AuctionNest.Domain.Enums;
using AuctionNest.Domain.Errors;
using AuctionNest.Domain.Events;

namespace AuctionNest.Domain.Entities;

public sealed class Auction : Entity
{
    private readonly List<Bid> _bids = [];

    // ─── Auction constants ─────────────────────────────────────────────────
    public const int AntiSnipeWindowSeconds = 30;
    public const int ExtensionMinutes       = 2;
    public const int MaxExtensions          = 5;

    // ─── Core properties ───────────────────────────────────────────────────
    public string Title { get; private set; } = null!;
    public string Description { get; private set; } = null!;
    public string? ImageUrl { get; private set; }

    public Guid SellerId { get; private set; }
    public Guid CategoryId { get; private set; }

    public decimal StartPrice { get; private set; }
    public decimal CurrentPrice { get; private set; }
    public decimal? ReservePrice { get; private set; }
    public decimal? BuyItNowPrice { get; private set; }
    public decimal MinBidIncrement { get; private set; }

    public AuctionStatus Status { get; private set; }

    public DateTime StartsAt { get; private set; }
    public DateTime EndsAt { get; private set; }
    public int ExtensionCount { get; private set; }

    // ─── Computed ──────────────────────────────────────────────────────────
    public bool IsReserveMet =>
        ReservePrice is null || CurrentPrice >= ReservePrice;

    public bool IsBuyItNowAvailable =>
        BuyItNowPrice.HasValue &&
        Status == AuctionStatus.Active &&
        !_bids.Any();

    // ─── Navigation ────────────────────────────────────────────────────────
    public User Seller { get; private set; } = null!;
    public Category Category { get; private set; } = null!;
    public IReadOnlyCollection<Bid> Bids => _bids.AsReadOnly();
    public ICollection<WatchList> WatchList { get; private set; } = [];

    // ─── Factory ───────────────────────────────────────────────────────────
    public static Auction Create(
        Guid sellerId,
        Guid categoryId,
        string title,
        string description,
        decimal startPrice,
        decimal minBidIncrement,
        DateTime startsAt,
        DateTime endsAt,
        decimal? reservePrice = null,
        decimal? buyItNowPrice = null,
        string? imageUrl = null)
        => new()
        {
            Id = Guid.NewGuid(),
            SellerId = sellerId,
            CategoryId = categoryId,
            Title = title.Trim(),
            Description = description.Trim(),
            StartPrice = startPrice,
            CurrentPrice = startPrice,
            MinBidIncrement = minBidIncrement,
            StartsAt = startsAt,
            EndsAt = endsAt,
            ReservePrice = reservePrice,
            BuyItNowPrice = buyItNowPrice,
            ImageUrl = imageUrl,
            Status = AuctionStatus.Draft,
            ExtensionCount = 0
        };

    // ─── Domain methods ────────────────────────────────────────────────────

    /// <summary>
    /// Activates a Draft auction. Called by Hangfire at StartsAt.
    /// </summary>
    public Result Activate()
    {
        if (Status != AuctionStatus.Draft)
            return Result.Failure(AuctionErrors.InvalidStatusTransition);

        Status = AuctionStatus.Active;
        RaiseDomainEvent(AuctionStartedEvent.Create(Id));
        return Result.Success();
    }

    /// <summary>
    /// Places a bid. Includes anti-snipe logic.
    /// Must be called inside a Redlock to prevent race conditions.
    /// Returns the newly created Bid so the caller can explicitly track it.
    /// </summary>
    public Result<Bid> PlaceBid(Guid bidderId, decimal amount, DateTime? utcNow = null)
    {
        if (Status is not (AuctionStatus.Active or AuctionStatus.Extending))
            return Result.Failure<Bid>(AuctionErrors.NotActive);
    
        if (bidderId == SellerId)
            return Result.Failure<Bid>(AuctionErrors.SellerCannotBid);
    
        var minimumBid = CurrentPrice + MinBidIncrement;
        if (amount < minimumBid)
            return Result.Failure<Bid>(AuctionErrors.BidTooLow(minimumBid));
    
        // Mark previous winner as outbid
        var previousWinner = _bids.FirstOrDefault(b => b.IsWinning);
        previousWinner?.SetNotWinning();
    
        var bid = Bid.Create(Id, bidderId, amount);
        _bids.Add(bid);
        CurrentPrice = amount;
    
        // Anti-snipe: extend if bid placed in last 30 seconds
        var remaining = EndsAt - (utcNow ?? DateTime.UtcNow);
        if (remaining.TotalSeconds <= AntiSnipeWindowSeconds
            && ExtensionCount < MaxExtensions)
        {
            EndsAt = EndsAt.AddMinutes(ExtensionMinutes);
            ExtensionCount++;
            Status = AuctionStatus.Extending;
            RaiseDomainEvent(AuctionExtendedEvent.Create(Id, EndsAt, ExtensionCount));
        }
    
        RaiseDomainEvent(BidPlacedEvent.Create(
            Id, bidderId, amount, CurrentPrice, previousWinner?.BidderId));
    
        return Result.Success(bid);
    }

    /// <summary>
    /// Instant purchase. Only available before any bids are placed.
    /// </summary>
    public Result<Bid> BuyItNow(Guid buyerId)
    {
        if (!IsBuyItNowAvailable)
            return Result.Failure<Bid>(AuctionErrors.BuyItNowNotAvailable);
    
        if (buyerId == SellerId)
            return Result.Failure<Bid>(AuctionErrors.SellerCannotBid);
    
        var bid = Bid.Create(Id, buyerId, BuyItNowPrice!.Value);
        _bids.Add(bid);
        CurrentPrice = BuyItNowPrice.Value;
        Status = AuctionStatus.Ended;
        EndsAt = DateTime.UtcNow;
    
        RaiseDomainEvent(BuyItNowUsedEvent.Create(Id, buyerId, BuyItNowPrice.Value));
        RaiseDomainEvent(AuctionEndedEvent.Create(Id, buyerId, BuyItNowPrice.Value, true));
        return Result.Success(bid);
    }

    /// <summary>
    /// Ends the auction. Called by Hangfire at EndsAt.
    /// </summary>
    public Result End()
    {
        if (Status is not (AuctionStatus.Active or AuctionStatus.Extending))
            return Result.Failure(AuctionErrors.InvalidStatusTransition);

        Status = AuctionStatus.Ended;
        var winner = _bids.OrderByDescending(b => b.Amount).FirstOrDefault();

        RaiseDomainEvent(AuctionEndedEvent.Create(
            Id,
            winner?.BidderId,
            winner?.Amount,
            IsReserveMet));

        return Result.Success();
    }

    /// <summary>
    /// Cancels the auction. Only for Draft/Active, not Ended.
    /// </summary>
    public Result Cancel()
    {
        if (Status is AuctionStatus.Ended or AuctionStatus.Cancelled)
            return Result.Failure(AuctionErrors.InvalidStatusTransition);
    
        Status = AuctionStatus.Cancelled;
        RaiseDomainEvent(AuctionCancelledEvent.Create(Id));
        return Result.Success();
    }
}