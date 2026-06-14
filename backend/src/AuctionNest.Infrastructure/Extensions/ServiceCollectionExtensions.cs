using AuctionNest.Application.Common.Interfaces;
using AuctionNest.Application.Common.Interfaces.Repositories;
using AuctionNest.Application.Common.Interfaces.Services;
using AuctionNest.Infrastructure.Persistence;
using AuctionNest.Infrastructure.Persistence.Interceptors;
using AuctionNest.Infrastructure.Persistence.Repositories;
using AuctionNest.Infrastructure.Services;
using AuctionNest.Infrastructure.Settings;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using RedLockNet.SERedis;
using RedLockNet.SERedis.Configuration;
using StackExchange.Redis;
using System.Security.Cryptography;

namespace AuctionNest.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ----- Interceptors -----
        services.AddSingleton<AuditInterceptor>();
        services.AddSingleton<OutboxInterceptor>();

        // ----- EF Core -----
        services.AddDbContext<AppDbContext>(options =>
            options
                .UseNpgsql(configuration.GetConnectionString("DefaultConnection"))
                .UseSnakeCaseNamingConvention());

        // ----- Redis -----
        services.AddSingleton<IConnectionMultiplexer>(_ =>
            ConnectionMultiplexer.Connect(
                configuration.GetConnectionString("Redis")!));

        // ----- Redlock -----
        services.AddSingleton<RedLockNet.IDistributedLockFactory>(sp =>
        {
            var multiplexer = sp.GetRequiredService<IConnectionMultiplexer>();
            return RedLockFactory.Create(
                new List<RedLockMultiplexer> { new(multiplexer) });
        });

        // ----- Hangfire -----
        services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(c =>
                c.UseNpgsqlConnection(
                    configuration.GetConnectionString("DefaultConnection"))));

        services.AddHangfireServer();

        // ----- Repositories -----
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IAuctionRepository, AuctionRepository>();
        services.AddScoped<IBidRepository, BidRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IWatchListRepository, WatchListRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // ----- Services -----
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<ICacheService, CacheService>();
        services.AddScoped<IDistributedLockService, DistributedLockService>();
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IJobScheduler, HangfireJobScheduler>();
        services.AddHttpContextAccessor();

        // ----- JWT Settings -----
        services.Configure<JwtSettings>(
            configuration.GetSection(JwtSettings.SectionName));

        // ----- JWT Authentication -----
        var jwtSettings = configuration
            .GetSection(JwtSettings.SectionName)
            .Get<JwtSettings>()!;

        var publicRsa = RSA.Create();
        publicRsa.ImportFromPem(File.ReadAllText(jwtSettings.PublicKeyPath));

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer              = true,
                    ValidIssuer                 = jwtSettings.Issuer,
                    ValidateAudience            = true,
                    ValidAudience               = jwtSettings.Audience,
                    ValidateLifetime            = true,
                    IssuerSigningKey            = new RsaSecurityKey(publicRsa),
                    ValidateIssuerSigningKey    = true,
                    ClockSkew                   = TimeSpan.Zero
                };

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;
            
                        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                            context.Token = accessToken;
            
                        return Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorization();

        return services;
    }
}