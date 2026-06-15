using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;

namespace AuctionNest.IntegrationTests.Common;

public sealed class WebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("auctionnest_test")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    private readonly RedisContainer _redis = new RedisBuilder()
        .WithImage("redis:7-alpine")
        .Build();

    private string _privateKeyPath = string.Empty;
    private string _publicKeyPath  = string.Empty;

    public async Task InitializeAsync()
    {
        await Task.WhenAll(_postgres.StartAsync(), _redis.StartAsync());
        GenerateTestKeys();
    }

    public new async Task DisposeAsync()
    {
        await Task.WhenAll(
            _postgres.DisposeAsync().AsTask(),
            _redis.DisposeAsync().AsTask());
        CleanupKeys();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");

        // Override only connection strings and key paths.
        // Do NOT override Issuer/Audience — JWT Bearer middleware reads them
        // eagerly at startup (before ConfigureAppConfiguration runs),
        // so we keep them as the defaults from appsettings.json.
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = _postgres.GetConnectionString(),
                ["ConnectionStrings:Redis"]             = _redis.GetConnectionString(),
                ["Jwt:PrivateKeyPath"]                  = _privateKeyPath,
                ["Jwt:PublicKeyPath"]                   = _publicKeyPath,
            });
        });

        // ConfigureTestServices runs AFTER all services are registered,
        // so it correctly overrides the eagerly-configured JWT Bearer options.
        builder.ConfigureTestServices(services =>
        {
            services.Configure<JwtBearerOptions>(
                JwtBearerDefaults.AuthenticationScheme,
                options =>
                {
                    var publicRsa = RSA.Create();
                    publicRsa.ImportFromPem(File.ReadAllText(_publicKeyPath));

                    options.TokenValidationParameters.IssuerSigningKey =
                        new RsaSecurityKey(publicRsa);

                    // Keep these in sync with appsettings.json defaults
                    options.TokenValidationParameters.ValidIssuer   = "auctionnest-api";
                    options.TokenValidationParameters.ValidAudience = "auctionnest-client";
                });
        });
    }

    // Insert via raw Npgsql — bypasses EF Core interceptors that
    // caused "Failed executing DbCommand" in SeedCategoryAsync
    public async Task<Guid> SeedCategoryAsync()
    {
        var categoryId = Guid.NewGuid();

        await using var conn = new NpgsqlConnection(_postgres.GetConnectionString());
        await conn.OpenAsync();

        await using var cmd = conn.CreateCommand();
        cmd.CommandText =
            "INSERT INTO categories (id, name, slug, created_at, updated_at) " +
            "VALUES (@id, @name, @slug, @now, @now)";
        cmd.Parameters.AddWithValue("id",   categoryId);
        cmd.Parameters.AddWithValue("name", "Electronics");
        cmd.Parameters.AddWithValue("slug", $"electronics-{categoryId:N}");
        cmd.Parameters.AddWithValue("now",  DateTime.UtcNow);

        await cmd.ExecuteNonQueryAsync();
        return categoryId;
    }

    private void GenerateTestKeys()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"auctionnest-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        _privateKeyPath = Path.Combine(dir, "private.pem");
        _publicKeyPath  = Path.Combine(dir, "public.pem");

        using var rsa = RSA.Create(2048);
        File.WriteAllText(_privateKeyPath, rsa.ExportRSAPrivateKeyPem());
        File.WriteAllText(_publicKeyPath,  rsa.ExportSubjectPublicKeyInfoPem());
    }

    private void CleanupKeys()
    {
        var dir = Path.GetDirectoryName(_privateKeyPath);
        if (dir is not null && Directory.Exists(dir))
            Directory.Delete(dir, recursive: true);
    }
}