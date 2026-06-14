using AuctionNest.Domain.Entities;
using AuctionNest.Domain.Enums;
using AuctionNest.Domain.Errors;
using AuctionNest.Domain.Events;
using AuctionNest.UnitTests.Domain.Helpers;
using FluentAssertions;

namespace AuctionNest.UnitTests.Domain.Entities;

public sealed class AuctionTests
{
    // ══════════════════════════════════════════════════════════════
    //  Activate
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public void Activate_DraftAuction_ShouldSucceed()
    {
        var auction = AuctionFactory.CreateDraft();

        var result = auction.Activate();

        result.IsSuccess.Should().BeTrue();
        auction.Status.Should().Be(AuctionStatus.Active);
    }

    [Fact]
    public void Activate_DraftAuction_ShouldRaiseAuctionStartedEvent()
    {
        var auction = AuctionFactory.CreateDraft();

        auction.Activate();

        auction.GetDomainEvents()
            .Should().ContainSingle(e => e is AuctionStartedEvent);
    }

    [Fact]
    public void Activate_AlreadyActiveAuction_ShouldReturnError()
    {
        var auction = AuctionFactory.CreateActive();

        var result = auction.Activate();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AuctionErrors.InvalidStatusTransition);
    }

    [Fact]
    public void Activate_EndedAuction_ShouldReturnError()
    {
        var auction = AuctionFactory.CreateActiveEndingSoon();
        auction.End();

        var result = auction.Activate();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AuctionErrors.InvalidStatusTransition);
    }

    // ══════════════════════════════════════════════════════════════
    //  PlaceBid
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public void PlaceBid_WithValidAmount_ShouldSucceed()
    {
        var auction = AuctionFactory.CreateActive(startPrice: 100m, minBidIncrement: 10m);
        var bidAmount = 110m;

        var result = auction.PlaceBid(AuctionFactory.DefaultBidderId, bidAmount);

        result.IsSuccess.Should().BeTrue();
        result.Value.Amount.Should().Be(bidAmount);
        result.Value.IsWinning.Should().BeTrue();
    }

    [Fact]
    public void PlaceBid_ShouldUpdateCurrentPrice()
    {
        var auction = AuctionFactory.CreateActive(startPrice: 100m, minBidIncrement: 10m);

        auction.PlaceBid(AuctionFactory.DefaultBidderId, 150m);

        auction.CurrentPrice.Should().Be(150m);
    }

    [Fact]
    public void PlaceBid_ShouldRaiseBidPlacedEvent()
    {
        var auction = AuctionFactory.CreateActive();

        auction.PlaceBid(AuctionFactory.DefaultBidderId, 110m);

        auction.GetDomainEvents()
            .Should().Contain(e => e is BidPlacedEvent);
    }

    [Fact]
    public void PlaceBid_WithAmountEqualToMinimum_ShouldSucceed()
    {
        // Minimum = currentPrice + minBidIncrement = 100 + 10 = 110
        var auction = AuctionFactory.CreateActive(startPrice: 100m, minBidIncrement: 10m);

        var result = auction.PlaceBid(AuctionFactory.DefaultBidderId, 110m);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void PlaceBid_WithAmountBelowMinimum_ShouldReturnBidTooLowError()
    {
        var auction = AuctionFactory.CreateActive(startPrice: 100m, minBidIncrement: 10m);

        var result = auction.PlaceBid(AuctionFactory.DefaultBidderId, 105m);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Auction.BidTooLow");
    }

    [Fact]
    public void PlaceBid_BySeller_ShouldReturnSellerCannotBidError()
    {
        var sellerId = Guid.NewGuid();
        var auction  = AuctionFactory.CreateActive(sellerId: sellerId);

        var result = auction.PlaceBid(sellerId, 200m);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AuctionErrors.SellerCannotBid);
    }

    [Fact]
    public void PlaceBid_OnDraftAuction_ShouldReturnNotActiveError()
    {
        var auction = AuctionFactory.CreateDraft();

        var result = auction.PlaceBid(AuctionFactory.DefaultBidderId, 200m);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AuctionErrors.NotActive);
    }

    [Fact]
    public void PlaceBid_OnEndedAuction_ShouldReturnNotActiveError()
    {
        var auction = AuctionFactory.CreateActiveEndingSoon();
        auction.End();

        var result = auction.PlaceBid(AuctionFactory.DefaultBidderId, 200m);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AuctionErrors.NotActive);
    }

    [Fact]
    public void PlaceBid_SecondBid_ShouldMarkFirstBidAsNotWinning()
    {
        var auction   = AuctionFactory.CreateActive(startPrice: 100m, minBidIncrement: 10m);
        var firstBid  = AuctionFactory.DefaultBidderId;
        var secondBid = Guid.NewGuid();

        var firstResult = auction.PlaceBid(firstBid, 110m);

        // Note: ClearDomainEvents is internal, we work with what's raised in total

        auction.PlaceBid(secondBid, 120m);

        firstResult.Value.IsWinning.Should().BeFalse();
    }

    [Fact]
    public void PlaceBid_SecondBid_ShouldSetPreviousWinnerInEvent()
    {
        var auction    = AuctionFactory.CreateActive(startPrice: 100m, minBidIncrement: 10m);
        var firstBidder  = Guid.NewGuid();
        var secondBidder = Guid.NewGuid();

        auction.PlaceBid(firstBidder, 110m);
        auction.PlaceBid(secondBidder, 120m);

        var bidPlacedEvents = auction.GetDomainEvents()
            .OfType<BidPlacedEvent>()
            .ToList();

        // Second BidPlacedEvent should have PreviousWinnerBidderId set
        bidPlacedEvents[1].PreviousWinnerBidderId.Should().Be(firstBidder);
    }

    [Fact]
    public void PlaceBid_InLastThirtySeconds_ShouldExtendAuction()
    {
        // Auction ends in 15 seconds — within 30s anti-snipe window
        var auction = AuctionFactory.CreateActiveEndingSoon(secondsUntilEnd: 15);
        var originalEndsAt = auction.EndsAt;

        auction.PlaceBid(AuctionFactory.DefaultBidderId, 110m);

        auction.EndsAt.Should().BeAfter(originalEndsAt);
        auction.ExtensionCount.Should().Be(1);
        auction.Status.Should().Be(AuctionStatus.Extending);
    }

    [Fact]
    public void PlaceBid_InLastThirtySeconds_ShouldRaiseAuctionExtendedEvent()
    {
        var auction = AuctionFactory.CreateActiveEndingSoon(secondsUntilEnd: 15);

        auction.PlaceBid(AuctionFactory.DefaultBidderId, 110m);

        auction.GetDomainEvents()
            .Should().Contain(e => e is AuctionExtendedEvent);
    }

    [Fact]
    public void PlaceBid_WhenNotInLastThirtySeconds_ShouldNotExtendAuction()
    {
        // Auction ends in 60 seconds — outside 30s window
        var auction = AuctionFactory.CreateActiveEndingSoon(secondsUntilEnd: 60);
        var originalEndsAt = auction.EndsAt;

        auction.PlaceBid(AuctionFactory.DefaultBidderId, 110m);

        auction.EndsAt.Should().Be(originalEndsAt);
        auction.ExtensionCount.Should().Be(0);
    }

    [Fact]
    public void PlaceBid_WhenMaxExtensionsReached_ShouldNotExtendFurther()
    {
        // Reach MaxExtensions first
        var auction = AuctionFactory.CreateDraft(
            endsAt: DateTime.UtcNow.AddSeconds(15));
        auction.Activate();

        var bidder = AuctionFactory.DefaultBidderId;
        decimal amount = 110m;

        // Place Auction.MaxExtensions bids to reach the limit
        for (int i = 0; i < Auction.MaxExtensions; i++)
        {
            var simulatedNow = auction.EndsAt.AddSeconds(-15);
            auction.PlaceBid(bidder, amount, simulatedNow);
            amount += 10m;
        }

        auction.ExtensionCount.Should().Be(Auction.MaxExtensions);
        var endsAtAfterMax = auction.EndsAt;

        // One more bid — should NOT extend
        auction.PlaceBid(bidder, amount, auction.EndsAt.AddSeconds(-15));

        auction.EndsAt.Should().Be(endsAtAfterMax);
        auction.ExtensionCount.Should().Be(Auction.MaxExtensions);
    }

    // ══════════════════════════════════════════════════════════════
    //  BuyItNow
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public void BuyItNow_WhenAvailable_ShouldSucceed()
    {
        var auction = AuctionFactory.CreateActive(buyItNowPrice: 500m);

        var result = auction.BuyItNow(AuctionFactory.DefaultBidderId);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void BuyItNow_ShouldSetCurrentPriceToBuyItNowPrice()
    {
        var auction = AuctionFactory.CreateActive(startPrice: 100m, buyItNowPrice: 500m);

        auction.BuyItNow(AuctionFactory.DefaultBidderId);

        auction.CurrentPrice.Should().Be(500m);
    }

    [Fact]
    public void BuyItNow_ShouldEndAuction()
    {
        var auction = AuctionFactory.CreateActive(buyItNowPrice: 500m);

        auction.BuyItNow(AuctionFactory.DefaultBidderId);

        auction.Status.Should().Be(AuctionStatus.Ended);
    }

    [Fact]
    public void BuyItNow_ShouldRaiseBuyItNowUsedAndAuctionEndedEvents()
    {
        var auction = AuctionFactory.CreateActive(buyItNowPrice: 500m);

        auction.BuyItNow(AuctionFactory.DefaultBidderId);

        var events = auction.GetDomainEvents();
        events.Should().Contain(e => e is BuyItNowUsedEvent);
        events.Should().Contain(e => e is AuctionEndedEvent);
    }

    [Fact]
    public void BuyItNow_AfterBidPlaced_ShouldReturnNotAvailableError()
    {
        var auction = AuctionFactory.CreateActive(
            startPrice: 100m, minBidIncrement: 10m, buyItNowPrice: 500m);

        auction.PlaceBid(AuctionFactory.DefaultBidderId, 110m);
        var result = auction.BuyItNow(Guid.NewGuid());

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AuctionErrors.BuyItNowNotAvailable);
    }

    [Fact]
    public void BuyItNow_WhenNotConfigured_ShouldReturnNotAvailableError()
    {
        // No BuyItNow price set
        var auction = AuctionFactory.CreateActive(buyItNowPrice: null);

        var result = auction.BuyItNow(AuctionFactory.DefaultBidderId);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AuctionErrors.BuyItNowNotAvailable);
    }

    [Fact]
    public void BuyItNow_BySeller_ShouldReturnSellerCannotBidError()
    {
        var sellerId = Guid.NewGuid();
        var auction  = AuctionFactory.CreateActive(buyItNowPrice: 500m, sellerId: sellerId);

        var result = auction.BuyItNow(sellerId);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AuctionErrors.SellerCannotBid);
    }

    [Fact]
    public void BuyItNow_AuctionEndedEvent_ShouldHaveReserveMetTrue()
    {
        var auction = AuctionFactory.CreateActive(buyItNowPrice: 500m);

        auction.BuyItNow(AuctionFactory.DefaultBidderId);

        var endedEvent = auction.GetDomainEvents()
            .OfType<AuctionEndedEvent>()
            .Single();

        endedEvent.IsReserveMet.Should().BeTrue();
        endedEvent.WinnerId.Should().Be(AuctionFactory.DefaultBidderId);
    }

    // ══════════════════════════════════════════════════════════════
    //  Cancel
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public void Cancel_ActiveAuction_ShouldSucceed()
    {
        var auction = AuctionFactory.CreateActive();

        var result = auction.Cancel();

        result.IsSuccess.Should().BeTrue();
        auction.Status.Should().Be(AuctionStatus.Cancelled);
    }

    [Fact]
    public void Cancel_DraftAuction_ShouldSucceed()
    {
        var auction = AuctionFactory.CreateDraft();

        var result = auction.Cancel();

        result.IsSuccess.Should().BeTrue();
        auction.Status.Should().Be(AuctionStatus.Cancelled);
    }

    [Fact]
    public void Cancel_ShouldRaiseAuctionCancelledEvent()
    {
        var auction = AuctionFactory.CreateActive();

        auction.Cancel();

        auction.GetDomainEvents()
            .Should().ContainSingle(e => e is AuctionCancelledEvent);
    }

    [Fact]
    public void Cancel_EndedAuction_ShouldReturnError()
    {
        var auction = AuctionFactory.CreateActiveEndingSoon();
        auction.End();

        var result = auction.Cancel();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AuctionErrors.InvalidStatusTransition);
    }

    [Fact]
    public void Cancel_AlreadyCancelledAuction_ShouldReturnError()
    {
        var auction = AuctionFactory.CreateActive();
        auction.Cancel();

        // Second cancel attempt
        var result = auction.Cancel();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AuctionErrors.InvalidStatusTransition);
    }

    // ══════════════════════════════════════════════════════════════
    //  End
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public void End_ActiveAuction_ShouldSucceed()
    {
        var auction = AuctionFactory.CreateActive();

        var result = auction.End();

        result.IsSuccess.Should().BeTrue();
        auction.Status.Should().Be(AuctionStatus.Ended);
    }

    [Fact]
    public void End_ShouldRaiseAuctionEndedEvent()
    {
        var auction = AuctionFactory.CreateActive();

        auction.End();

        auction.GetDomainEvents()
            .Should().ContainSingle(e => e is AuctionEndedEvent);
    }

    [Fact]
    public void End_WithoutBids_ShouldEndWithNoWinner()
    {
        var auction = AuctionFactory.CreateActive();

        auction.End();

        var endedEvent = auction.GetDomainEvents()
            .OfType<AuctionEndedEvent>()
            .Single();

        endedEvent.WinnerId.Should().BeNull();
        endedEvent.WinningAmount.Should().BeNull();
    }

    [Fact]
    public void End_WithBids_ShouldSetWinnerToHighestBidder()
    {
        var auction = AuctionFactory.CreateActive(startPrice: 100m, minBidIncrement: 10m);
        var winnerId = Guid.NewGuid();

        auction.PlaceBid(AuctionFactory.DefaultBidderId, 110m);
        auction.PlaceBid(winnerId, 150m);
        auction.End();

        var endedEvent = auction.GetDomainEvents()
            .OfType<AuctionEndedEvent>()
            .Single();

        endedEvent.WinnerId.Should().Be(winnerId);
        endedEvent.WinningAmount.Should().Be(150m);
    }

    [Fact]
    public void End_WithBidsAboveReservePrice_IsReserveMetShouldBeTrue()
    {
        var auction = AuctionFactory.CreateActive(
            startPrice: 100m, minBidIncrement: 10m, reservePrice: 120m);

        auction.PlaceBid(AuctionFactory.DefaultBidderId, 130m);
        auction.End();

        var endedEvent = auction.GetDomainEvents()
            .OfType<AuctionEndedEvent>()
            .Single();

        endedEvent.IsReserveMet.Should().BeTrue();
    }

    [Fact]
    public void End_WithBidsBelowReservePrice_IsReserveMetShouldBeFalse()
    {
        var auction = AuctionFactory.CreateActive(
            startPrice: 100m, minBidIncrement: 10m, reservePrice: 500m);

        auction.PlaceBid(AuctionFactory.DefaultBidderId, 110m);
        auction.End();

        var endedEvent = auction.GetDomainEvents()
            .OfType<AuctionEndedEvent>()
            .Single();

        endedEvent.IsReserveMet.Should().BeFalse();
    }

    [Fact]
    public void End_AlreadyEndedAuction_ShouldReturnError()
    {
        var auction = AuctionFactory.CreateActive();
        auction.End();

        var result = auction.End();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AuctionErrors.InvalidStatusTransition);
    }

    // ══════════════════════════════════════════════════════════════
    //  IsReserveMet
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public void IsReserveMet_WithoutReservePrice_ShouldAlwaysBeTrue()
    {
        var auction = AuctionFactory.CreateActive(reservePrice: null);

        auction.IsReserveMet.Should().BeTrue();
    }

    [Fact]
    public void IsReserveMet_WhenCurrentPriceEqualsReservePrice_ShouldBeTrue()
    {
        var auction = AuctionFactory.CreateActive(
            startPrice: 100m, minBidIncrement: 10m, reservePrice: 110m);

        auction.PlaceBid(AuctionFactory.DefaultBidderId, 110m);

        auction.IsReserveMet.Should().BeTrue();
    }

    // ══════════════════════════════════════════════════════════════
    //  IsBuyItNowAvailable
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public void IsBuyItNowAvailable_WhenActiveAndNoBids_ShouldBeTrue()
    {
        var auction = AuctionFactory.CreateActive(buyItNowPrice: 500m);

        auction.IsBuyItNowAvailable.Should().BeTrue();
    }

    [Fact]
    public void IsBuyItNowAvailable_WhenBidExists_ShouldBeFalse()
    {
        var auction = AuctionFactory.CreateActive(
            startPrice: 100m, minBidIncrement: 10m, buyItNowPrice: 500m);

        auction.PlaceBid(AuctionFactory.DefaultBidderId, 110m);

        auction.IsBuyItNowAvailable.Should().BeFalse();
    }

    [Fact]
    public void IsBuyItNowAvailable_WhenNotConfigured_ShouldBeFalse()
    {
        var auction = AuctionFactory.CreateActive(buyItNowPrice: null);

        auction.IsBuyItNowAvailable.Should().BeFalse();
    }
}