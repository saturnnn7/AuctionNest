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