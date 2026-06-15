using System.Net;
using System.Net.Http.Json;
using AuctionNest.IntegrationTests.Common;
using FluentAssertions;

namespace AuctionNest.IntegrationTests.Auth;

public sealed class AuthControllerTests : IntegrationTestBase
{
    public AuthControllerTests(WebAppFactory factory) : base(factory) { }

    // ----- Register -----

    [Fact]
    public async Task Register_WithValidData_ReturnsOkWithTokens()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];

        var res = await Client.PostAsJsonAsync("/api/auth/register", new
        {
            username    = $"user_{suffix}",
            email       = $"user_{suffix}@test.com",
            password    = "Test1234!",
            displayName = "Test User"
        });

        res.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await res.Content.ReadFromJsonAsync<AuthBody>();
        body.Should().NotBeNull();
        body!.AccessToken.Should().NotBeNullOrEmpty();
        body.RefreshToken.Should().NotBeNullOrEmpty();
        body.UserId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ReturnsConflict()
    {
        var (_, _, _) = await RegisterAndLoginAsync();

        var suffix = Guid.NewGuid().ToString("N")[..8];
        // reuse same email from previous registration would conflict,
        // so we register twice with same email explicitly:
        var email = $"dup_{suffix}@test.com";

        await Client.PostAsJsonAsync("/api/auth/register", new
        {
            username = $"first_{suffix}", email, password = "Test1234!",
            displayName = "First"
        });

        var res = await Client.PostAsJsonAsync("/api/auth/register", new
        {
            username = $"second_{suffix}", email, password = "Test1234!",
            displayName = "Second"
        });

        res.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Register_WithDuplicateUsername_ReturnsConflict()
    {
        var suffix   = Guid.NewGuid().ToString("N")[..8];
        var username = $"dupuser_{suffix}";

        await Client.PostAsJsonAsync("/api/auth/register", new
        {
            username, email = $"first_{suffix}@test.com",
            password = "Test1234!", displayName = "First"
        });

        var res = await Client.PostAsJsonAsync("/api/auth/register", new
        {
            username, email = $"second_{suffix}@test.com",
            password = "Test1234!", displayName = "Second"
        });

        res.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Register_WithShortPassword_ReturnsUnprocessableEntity()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];

        var res = await Client.PostAsJsonAsync("/api/auth/register", new
        {
            username    = $"user_{suffix}",
            email       = $"user_{suffix}@test.com",
            password    = "123",         // too short
            displayName = "Test User"
        });

        res.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    // ----- Login -----

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsOkWithTokens()
    {
        var (_, _, _) = await RegisterAndLoginAsync();

        // RegisterAndLoginAsync already tests successful login internally.
        // Here we test the Login endpoint independently:
        var suffix = Guid.NewGuid().ToString("N")[..8];
        await Client.PostAsJsonAsync("/api/auth/register", new
        {
            username = $"lg_{suffix}", email = $"lg_{suffix}@test.com",
            password = "Test1234!", displayName = "Login Test"
        });

        var res = await Client.PostAsJsonAsync("/api/auth/login", new
        {
            usernameOrEmail = $"lg_{suffix}@test.com",
            password = "Test1234!"
        });

        res.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await res.Content.ReadFromJsonAsync<AuthBody>();
        body!.AccessToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        await Client.PostAsJsonAsync("/api/auth/register", new
        {
            username = $"wp_{suffix}", email = $"wp_{suffix}@test.com",
            password = "Test1234!", displayName = "Wrong Pass"
        });

        var res = await Client.PostAsJsonAsync("/api/auth/login", new
        {
            usernameOrEmail = $"wp_{suffix}@test.com",
            password = "WrongPassword!"
        });

        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithNonexistentUser_ReturnsUnauthorized()
    {
        var res = await Client.PostAsJsonAsync("/api/auth/login", new
        {
            usernameOrEmail = "nobody@nowhere.com",
            password = "Test1234!"
        });

        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ----- Refresh Token -----

    [Fact]
    public async Task RefreshToken_WithValidToken_ReturnsNewTokenPair()
    {
        var (_, refreshToken, _) = await RegisterAndLoginAsync();

        var res = await Client.PostAsJsonAsync("/api/auth/refresh", new
        {
            refreshToken
        });

        res.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await res.Content.ReadFromJsonAsync<AuthBody>();
        body!.AccessToken.Should().NotBeNullOrEmpty();
        body.RefreshToken.Should().NotBe(refreshToken); // rotated
    }

    [Fact]
    public async Task RefreshToken_WithInvalidToken_ReturnsUnauthorized()
    {
        var res = await Client.PostAsJsonAsync("/api/auth/refresh", new
        {
            refreshToken = "totally-invalid-token"
        });

        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RefreshToken_UsedTwice_ReturnsUnauthorizedOnSecondUse()
    {
        var (_, refreshToken, _) = await RegisterAndLoginAsync();

        // First use — OK
        await Client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken });

        // Second use — token was rotated, old one is gone
        var res = await Client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ----- helper -----
    // private sealed record AuthBody(
    //     string AccessToken, string RefreshToken,
    //     Guid UserId, string Username, string DisplayName, string Role);
}