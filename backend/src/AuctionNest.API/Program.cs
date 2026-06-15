using System.Text.Json.Serialization;
using AuctionNest.API.BackgroundServices;
using AuctionNest.API.Common.Middleware;
using AuctionNest.API.Hubs;
using AuctionNest.Application.Extensions;
using AuctionNest.Infrastructure;
using AuctionNest.Infrastructure.Extensions;
using Hangfire;
using Microsoft.OpenApi.Models;

// Fix UTC timestamps for Npgsql
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// Production: write RSA keys from env vars to temp files
var privateKeyContent = builder.Configuration["Jwt:PrivateKeyContent"];
var publicKeyContent  = builder.Configuration["Jwt:PublicKeyContent"];

if (!string.IsNullOrEmpty(privateKeyContent) && !string.IsNullOrEmpty(publicKeyContent))
{
    const string keysDir = "/tmp/keys";
    Directory.CreateDirectory(keysDir);
    File.WriteAllText($"{keysDir}/private.pem", privateKeyContent.Replace("\\n", "\n"));
    File.WriteAllText($"{keysDir}/public.pem",  publicKeyContent.Replace("\\n", "\n"));
    builder.Configuration["Jwt:PrivateKeyPath"] = $"{keysDir}/private.pem";
    builder.Configuration["Jwt:PublicKeyPath"]  = $"{keysDir}/public.pem";
}

builder.Services
    .AddApplicationServices()
    .AddInfrastructureServices(builder.Configuration);

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddSignalR();

// OutboxProcessorService runs every 5 seconds, pushing domain events to SignalR clients
builder.Services.AddHostedService<OutboxProcessorService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "AuctionNest API", Version = "v1" });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name        = "Authorization",
        Description = "Enter: Bearer {token}",
        In          = ParameterLocation.Header,
        Type        = SecuritySchemeType.ApiKey,
        Scheme      = "Bearer"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id   = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(o => o.DisplayRequestDuration());

// Global exception handler — must be first
app.UseMiddleware<ExceptionHandlerMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.UseHangfireDashboard("/hangfire");
app.UseStaticFiles();
app.MapControllers();

// SignalR hub endpoint
app.MapHub<AuctionHub>("/hubs/auction");

await app.MigrateDatabaseAsync();

app.Run();

public partial class Program { } // for Integration Tests