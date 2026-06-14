using AuctionNest.Application.Common.Exceptions;
using System.Text.Json;

namespace AuctionNest.API.Common.Middleware;

public sealed class ExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlerMiddleware> _logger;

    public ExceptionHandlerMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlerMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationException ex)
        {
            await WriteResponseAsync(context, StatusCodes.Status422UnprocessableEntity, new
            {
                type = "ValidationError",
                title = "One or more validation errors occurred.",
                status = 422,
                errors = ex.Errors
            });
        }
        catch (OperationCanceledException)
        {
            // Client disconnected — no response needed
            context.Response.StatusCode = 499;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);

            await WriteResponseAsync(context, StatusCodes.Status500InternalServerError, new
            {
                type = "InternalServerError",
                title = "An unexpected error occurred.",
                status = 500
            });
        }
    }

    private static Task WriteResponseAsync(HttpContext context, int statusCode, object body)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        return context.Response.WriteAsync(JsonSerializer.Serialize(body));
    }
}