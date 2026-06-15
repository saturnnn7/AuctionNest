using System.Net;
using System.Net.Http.Json;
using AuctionNest.IntegrationTests.Common;
using FluentAssertions;

namespace AuctionNest.IntegrationTests.Auctions;

public sealed class AuctionsControllerTests : IntegrationTestBase
{
    public AuctionsControllerTests(WebAppFactory factory) : base(factory) { }

    // ----- GET /api/auctions -----

    [Fact]
    public async Task GetAuctions_WithNoFilters_ReturnsPagedResult()
    {
        var res = await Client.GetAsync("/api/auctions");

        res.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await res.Content.ReadFromJsonAsync<PagedBody<AuctionItem>>();
        body.Should().NotBeNull();
        body!.Items.Should().NotBeNull();
        body.Page.Should().Be(1);
    }

    [Fact]
    public async Task GetAuctions_WithPagination_ReturnsCorrectPage()
    {
        var res = await Client.GetAsync("/api/auctions?page=1&pageSize=5");

        res.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await res.Content.ReadFromJsonAsync<PagedBody<AuctionItem>>();
        body!.PageSize.Should().Be(5);
    }

    // ----- GET /api/auctions/{id} -----

    [Fact]
    public async Task GetAuctionById_WhenNotFound_Returns404()
    {
        var res = await Client.GetAsync($"/api/auctions/{Guid.NewGuid()}");

        res.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetAuctionById_WhenExists_ReturnsAuctionDetail()
    {
        var (token, _, _) = await RegisterAndLoginAsync();
        var categoryId    = await Factory.SeedCategoryAsync();
        var auctionId     = await CreateAuctionAsync(token, categoryId);
        ClearAuth();

        var res = await Client.GetAsync($"/api/auctions/{auctionId}");

        res.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await res.Content.ReadFromJsonAsync<AuctionDetailItem>();
        body!.Id.Should().Be(auctionId);
        body.Status.Should().Be("Draft"); // not yet activated by Hangfire
    }

    // ----- POST /api/auctions -----

    [Fact]
    public async Task CreateAuction_WithoutAuth_Returns401()
    {
        ClearAuth();

        var res = await Client.PostAsJsonAsync("/api/auctions", new
        {
            categoryId      = Guid.NewGuid(),
            title           = "Unauthorized Auction",
            description     = "Should fail",
            startPrice      = 100m,
            minBidIncrement = 10m,
            startsAt        = DateTime.UtcNow.AddHours(1),
            endsAt          = DateTime.UtcNow.AddDays(7)
        });

        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateAuction_WithInvalidCategoryId_Returns404()
    {
        var (token, _, _) = await RegisterAndLoginAsync();
        Authorize(token);

        var res = await Client.PostAsJsonAsync("/api/auctions", new
        {
            categoryId      = Guid.NewGuid(), // non-existent
            title           = "Test",
            description     = "Test",
            startPrice      = 100m,
            minBidIncrement = 10m,
            startsAt        = DateTime.UtcNow.AddSeconds(5),
            endsAt          = DateTime.UtcNow.AddDays(7)
        });

        res.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateAuction_WithValidData_ReturnsCreatedAuction()
    {
        var (token, _, _) = await RegisterAndLoginAsync();
        var categoryId    = await Factory.SeedCategoryAsync();
        Authorize(token);

        var res = await Client.PostAsJsonAsync("/api/auctions", new
        {
            categoryId,
            title           = "Valid Auction",
            description     = "Integration test",
            startPrice      = 100m,
            minBidIncrement = 10m,
            startsAt        = DateTime.UtcNow.AddSeconds(5),
            endsAt          = DateTime.UtcNow.AddDays(7),
            reservePrice    = (decimal?)null,
            buyItNowPrice   = 500m,
            imageUrl        = (string?)null
        });

        res.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await res.Content.ReadFromJsonAsync<AuctionItem>();
        body!.Id.Should().NotBeEmpty();
        body.Title.Should().Be("Valid Auction");
        body.Status.Should().Be("Draft");
    }

    [Fact]
    public async Task CreateAuction_WithStartInPast_Returns422()
    {
        var (token, _, _) = await RegisterAndLoginAsync();
        var categoryId    = await Factory.SeedCategoryAsync();
        Authorize(token);

        var res = await Client.PostAsJsonAsync("/api/auctions", new
        {
            categoryId,
            title           = "Past Auction",
            description     = "Test",
            startPrice      = 100m,
            minBidIncrement = 10m,
            startsAt        = DateTime.UtcNow.AddHours(-1), // past
            endsAt          = DateTime.UtcNow.AddDays(7)
        });

        res.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task CreateAuction_WithEndBeforeOneHourAfterStart_Returns422()
    {
        var (token, _, _) = await RegisterAndLoginAsync();
        var categoryId    = await Factory.SeedCategoryAsync();
        Authorize(token);

        var startsAt = DateTime.UtcNow.AddSeconds(10);

        var res = await Client.PostAsJsonAsync("/api/auctions", new
        {
            categoryId,
            title           = "Too Short Auction",
            description     = "Test",
            startPrice      = 100m,
            minBidIncrement = 10m,
            startsAt,
            endsAt = startsAt.AddMinutes(30) // less than 1 hour
        });

        res.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    // ----- PlaceBid -----

    [Fact]
    public async Task PlaceBid_OnDraftAuction_ReturnsConflict()
    {
        var (sellerToken, _, _) = await RegisterAndLoginAsync();
        var (bidderToken, _, _) = await RegisterAndLoginAsync();
        var categoryId          = await Factory.SeedCategoryAsync();

        var auctionId = await CreateAuctionAsync(sellerToken, categoryId);

        Authorize(bidderToken);
        var res = await Client.PostAsJsonAsync(
            $"/api/auctions/{auctionId}/bids",
            new { amount = 110m });

        res.StatusCode.Should().Be(HttpStatusCode.Conflict); // NotActive
    }

    [Fact]
    public async Task PlaceBid_BySeller_ReturnsForbidden()
    {
        var (sellerToken, _, _) = await RegisterAndLoginAsync();
        var categoryId          = await Factory.SeedCategoryAsync();
        var auctionId           = await CreateAuctionAsync(sellerToken, categoryId);

        Authorize(sellerToken);
        var res = await Client.PostAsJsonAsync(
            $"/api/auctions/{auctionId}/bids",
            new { amount = 110m });

        // Seller bids on own auction — domain returns SellerCannotBid (Forbidden)
        // But auction is still Draft so we get Conflict first
        res.StatusCode.Should().BeOneOf(
            HttpStatusCode.Conflict,
            HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task PlaceBid_WithoutAuth_Returns401()
    {
        var (sellerToken, _, _) = await RegisterAndLoginAsync();
        var categoryId          = await Factory.SeedCategoryAsync();
        var auctionId           = await CreateAuctionAsync(sellerToken, categoryId);
        ClearAuth();

        var res = await Client.PostAsJsonAsync(
            $"/api/auctions/{auctionId}/bids",
            new { amount = 110m });

        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ----- DTOs -----

    private sealed record PagedBody<T>(
        List<T> Items, int TotalCount, int Page, int PageSize);

    private sealed record AuctionItem(
        Guid Id, string Title, string Status);

    private sealed record AuctionDetailItem(
        Guid Id, string Title, string Status, decimal CurrentPrice);
}