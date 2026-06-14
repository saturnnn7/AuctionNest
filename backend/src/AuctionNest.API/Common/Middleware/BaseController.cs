using AuctionNest.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace AuctionNest.API.Common;

[ApiController]
public abstract class BaseController : ControllerBase
{
    // Maps Result<T> → 200 OK with value, or appropriate error response
    protected IActionResult HandleResult<T>(Result<T> result)
        => result.IsSuccess ? Ok(result.Value) : HandleError(result.Error);

    // Maps Result → 204 No Content, or appropriate error response
    protected IActionResult HandleResult(Result result)
        => result.IsSuccess ? NoContent() : HandleError(result.Error);

    // Maps Result<T> → 201 Created, or appropriate error response
    protected IActionResult HandleCreatedResult<T>(
        Result<T> result, string actionName, object? routeValues = null)
        => result.IsSuccess
            ? CreatedAtAction(actionName, routeValues, result.Value)
            : HandleError(result.Error);

    private IActionResult HandleError(Error error) => error.Type switch
    {
        ErrorType.NotFound     => NotFound(ToProblem(error)),
        ErrorType.Validation   => UnprocessableEntity(ToProblem(error)),
        ErrorType.Unauthorized => Unauthorized(ToProblem(error)),
        ErrorType.Forbidden    => StatusCode(StatusCodes.Status403Forbidden, ToProblem(error)),
        ErrorType.Conflict     => Conflict(ToProblem(error)),
        ErrorType.Internal     => StatusCode(StatusCodes.Status500InternalServerError, ToProblem(error)),
        _                      => StatusCode(StatusCodes.Status500InternalServerError, ToProblem(error))
    };

    private static object ToProblem(Error error) => new
    {
        type   = error.Type.ToString(),
        title  = error.Code,
        detail = error.Description,
        status = GetStatusCode(error.Type)
    };

    private static int GetStatusCode(ErrorType type) => type switch
    {
        ErrorType.NotFound     => 404,
        ErrorType.Validation   => 422,
        ErrorType.Unauthorized => 401,
        ErrorType.Forbidden    => 403,
        ErrorType.Conflict     => 409,
        _                      => 500
    };
}