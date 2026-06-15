using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;

namespace AuctionNest.IntegrationTests.Common;

public abstract class IntegrationTestBase : IClassFixture<WebAppFactory>
{
    protected readonly HttpClient Client;
    protected readonly WebAppFactory Factory;

    protected IntegrationTestBase(WebAppFactory factory)
    {
        Factory = factory;
        Client  = factory.CreateClient();
    }

    protected void Authorize(string token)
        => Client.DefaultRequestHeaders.Authorization =
               new AuthenticationHeaderValue("Bearer", token);

    protected void ClearAuth()
        => Client.DefaultRequestHeaders.Authorization = null;

    // Register a unique user and return their access token
    protected async Task<(string AccessToken, string RefreshToken, Guid UserId)>
        RegisterAndLoginAsync(string? suffix = null)
    {
        suffix ??= Guid.NewGuid().ToString("N")[..8];

        var registerRes = await Client.PostAsJsonAsync("/api/auth/register", new
        {
            username    = $"user_{suffix}",
            email       = $"user_{suffix}@test.com",
            password    = "Test1234!",
            displayName = $"User {suffix}"
        });

        registerRes.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        var body = await registerRes.Content.ReadFromJsonAsync<AuthBody>();
        return (body!.AccessToken, body.RefreshToken, body.UserId);
    }

    // Create an auction that starts soon (for PlaceBid tests)
    protected async Task<Guid> CreateAuctionAsync(
        string accessToken,
        Guid categoryId,
        int startsInSeconds = 3)
    {
        Authorize(accessToken);

        var res = await Client.PostAsJsonAsync("/api/auctions", new
        {
            categoryId,
            title           = $"Test Auction {Guid.NewGuid():N}",
            description     = "Integration test auction",
            startPrice      = 100m,
            minBidIncrement = 10m,
            startsAt        = DateTime.UtcNow.AddSeconds(startsInSeconds),
            endsAt          = DateTime.UtcNow.AddDays(7),
            reservePrice    = (decimal?)null,
            buyItNowPrice   = 999m,
            imageUrl        = (string?)null
        });

        res.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        var body = await res.Content.ReadFromJsonAsync<AuctionBody>();
        return body!.Id;
    }

    // ----- Internal DTO records (only what tests need) -----

    protected sealed record AuthBody(
        string AccessToken,
        string RefreshToken,
        Guid   UserId,
        string Username,
        string DisplayName,
        string Role);

    protected sealed record AuctionBody(
        Guid   Id,
        string Status,
        string Title);
}